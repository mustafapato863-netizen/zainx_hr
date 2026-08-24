using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using OpenTelemetry.Trace;
using Workforce.BuildingBlocks.Database;
using Workforce.Host.Api.Middleware;
using Workforce.Modules.Attendance.Infrastructure;
using Workforce.Modules.Approvals.Infrastructure;
using Workforce.Modules.Documents.Infrastructure;
using Workforce.Modules.Leave.Infrastructure;
using Workforce.Modules.Organization.Infrastructure;
using Workforce.Modules.People.Infrastructure;
using Workforce.SharedKernel.Primitives;
using Workforce.SharedKernel.Security;

if (args.Contains("--run-security-tests"))
{
    var code = Workforce.Host.Api.Testing.WorkforceSecurityTestRunner.RunAllTests();
    Environment.Exit(code);
}

var builder = WebApplication.CreateBuilder(args);

// OpenTelemetry Instrumentation
var activitySource = new ActivitySource("Workforce.Platform", "1.0.0");

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("Workforce.Platform")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

// Environment-based Database Connection
var dbHost = Environment.GetEnvironmentVariable("ZAINX_DB_HOST") ?? "127.0.0.1";
var dbPort = Environment.GetEnvironmentVariable("ZAINX_DB_PORT") ?? "55432";
var dbUser = Environment.GetEnvironmentVariable("ZAINX_DB_USER") ?? "zainx";
var dbPass = Environment.GetEnvironmentVariable("ZAINX_DB_PASSWORD") ?? "123456";
var dbName = Environment.GetEnvironmentVariable("ZAINX_DB_NAME") ?? "zainx_workforce";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPass}";

// Register NpgsqlDataSource
var dataSource = NpgsqlDataSource.Create(connectionString);
builder.Services.AddSingleton(dataSource);

// Security & User Context Resolution
builder.Services.AddScoped<IUserContextProvider, DefaultUserContextProvider>();
builder.Services.AddScoped<IUserContext>(sp =>
{
    var provider = sp.GetRequiredService<IUserContextProvider>();
    if (provider.Current != null) return provider.Current;

    // Fallback default context for development / testing
    return new UserContext(
        new UserId(Guid.Parse("11111111-1111-1111-1111-111111111111")),
        new TenantId(Guid.Parse("22222222-2222-2222-2222-222222222222")),
        new LegalEntityId(Guid.Parse("33333333-3333-3333-3333-333333333333")),
        "en-US",
        "UTC",
        new HashSet<string> 
        { 
            "people.employee.read", "people.employee.create", "people.employee.update", "people.employee.reveal_pii",
            "organization.unit.read", "organization.unit.create", "organization.unit.update",
            "documents.read", "documents.upload", "documents.download",
            "attendance.clock.create", "attendance.adjustment.submit", "attendance.day.approve", "attendance.exception.resolve",
            "leave.request.create", "leave.request.approve", "leave.request.reject",
            "approvals.decision.approve", "approvals.decision.reject",
            "admin" 
        },
        new HashSet<string> { "core.platform", "people", "organization", "documents", "attendance", "leave", "approvals" }
    );
});

// PII Encryption Service (AES-256-GCM + Blind Indexing)
builder.Services.AddSingleton<IPiiEncryptionService, AesPiiEncryptionService>();

// Storage Provider for Documents
builder.Services.AddSingleton<IStorageProvider, LocalStorageProvider>();

// Module Repositories
builder.Services.AddScoped<OrganizationRepository>(_ => new OrganizationRepository(connectionString));
builder.Services.AddScoped<PeopleRepository>(sp => new PeopleRepository(connectionString, sp.GetRequiredService<IPiiEncryptionService>()));
builder.Services.AddScoped<DocumentsRepository>(_ => new DocumentsRepository(connectionString));
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<ILeaveRepository, LeaveRepository>();
builder.Services.AddScoped<IApprovalsRepository, ApprovalsRepository>();

// Controllers
builder.Services.AddControllers();

// CORS for local web dev
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Auto-run schema migrations
try
{
    await MigrationRunner.EnsureMigrationHistoryTableAsync(connectionString);
    await OrganizationMigrations.ApplyMigrationsAsync(connectionString);
    await PeopleMigrations.ApplyMigrationsAsync(connectionString);
    await DocumentsMigrations.ApplyMigrationsAsync(connectionString);
    await AttendanceMigrations.ApplyAsync(dataSource);
    await LeaveMigrations.ApplyAsync(dataSource);
    await ApprovalsMigrations.ApplyAsync(dataSource);
    Console.WriteLine("[MIGRATIONS] Database schemas initialized successfully.");
}
catch (Exception ex)
{
    Console.WriteLine($"[MIGRATIONS] Migration notice/warning: {ex.Message}");
}

app.UseCors();

// Correlation ID & Trace ID Middleware
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();
    var traceId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

    context.Response.Headers["X-Correlation-ID"] = correlationId;
    context.Response.Headers["X-Trace-ID"] = traceId;
    context.Items["CorrelationId"] = correlationId;
    context.Items["TraceId"] = traceId;

    await next();
});

// Tenant Context Resolution Middleware
app.UseMiddleware<TenantResolutionMiddleware>();

// Health Checks
app.MapHealthChecks("/health");

// OpenAPI Specification Endpoint
app.MapOpenApi();

app.MapControllers();

// Seed initial test data for Attendance, Leave, and Approvals if needed
app.MapGet("/api/v1/seed/phase3", async (
    IAttendanceRepository attRepo,
    ILeaveRepository leaveRepo,
    IApprovalsRepository appRepo,
    IUserContext uCtx) =>
{
    var tid = uCtx.TenantId;
    var lid = uCtx.LegalEntityId ?? new LegalEntityId(Guid.Parse("33333333-3333-3333-3333-333333333333"));

    // 1. Seed Work Schedule
    var schedule = new Workforce.Modules.Attendance.Domain.WorkSchedule(
        Guid.Parse("12121212-1212-1212-1212-121212121212"),
        tid, lid, "STD-9TO5", "Standard Shift 9 to 5", "الدوام القياسي ٩ إلى ٥",
        new TimeOnly(9, 0), new TimeOnly(17, 0), 15, "Africa/Cairo",
        new EffectivePeriod(new DateOnly(2024, 1, 1), null)
    );
    await attRepo.SaveWorkScheduleAsync(schedule);

    // 2. Seed Leave Types
    var annualLeave = new Workforce.Modules.Leave.Domain.LeaveType(
        Guid.Parse("aaaaaaaa-1111-2222-3333-444444444444"),
        tid, lid, "ANNUAL", "Annual Leave", "الإجازة السنوية",
        Workforce.Modules.Leave.Domain.LeaveCategory.Annual, true, false, true
    );
    await leaveRepo.SaveLeaveTypeAsync(annualLeave);

    var sickLeave = new Workforce.Modules.Leave.Domain.LeaveType(
        Guid.Parse("bbbbbbbb-1111-2222-3333-444444444444"),
        tid, lid, "SICK", "Sick Leave", "الإجازة المرضية",
        Workforce.Modules.Leave.Domain.LeaveCategory.Sick, true, true, false
    );
    await leaveRepo.SaveLeaveTypeAsync(sickLeave);

    // 3. Seed Default Balance
    var defaultEmpId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    await leaveRepo.GetOrCreateLeaveBalanceAsync(tid, defaultEmpId, annualLeave.Id, DateTime.UtcNow.Year, 21.00m);
    await leaveRepo.GetOrCreateLeaveBalanceAsync(tid, defaultEmpId, sickLeave.Id, DateTime.UtcNow.Year, 14.00m);

    return Results.Ok(new { status = "Seeded Phase 3 baseline data" });
});

app.Run();
