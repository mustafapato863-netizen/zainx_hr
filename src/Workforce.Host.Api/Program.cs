using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using OpenTelemetry.Trace;
using Workforce.BuildingBlocks.Database;
using Workforce.Host.Api.Middleware;
using Workforce.Modules.Attendance.Infrastructure;
using Workforce.Modules.Approvals.Infrastructure;
using Workforce.Modules.Audit.Infrastructure;
using Workforce.Modules.Compliance.Domain;
using Workforce.Modules.Compliance.Infrastructure;
using Workforce.Modules.Documents.Infrastructure;
using Workforce.Modules.Identity.Infrastructure;
using Workforce.Modules.Integrations.Application;
using Workforce.Modules.Integrations.Infrastructure;
using Workforce.Modules.Leave.Infrastructure;
using Workforce.Modules.Notifications.Infrastructure;
using Workforce.Modules.Organization.Infrastructure;
using Workforce.Modules.Payroll.Domain.CalculationEngine;
using Workforce.Modules.Payroll.Infrastructure;
using Workforce.Modules.People.Infrastructure;
using Workforce.Modules.Reporting.Application;
using Workforce.Modules.Reporting.Infrastructure;
using Workforce.Modules.Settlement.Domain.ExportAdapters;
using Workforce.Modules.Settlement.Infrastructure;
using Workforce.Modules.Recruitment.Domain;
using Workforce.Modules.Recruitment.Infrastructure;
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
            "*",
            "people.employee.read", "people.employee.create", "people.employee.update", "people.employee.reveal_pii",
            "organization.unit.read", "organization.unit.create", "organization.unit.update",
            "documents.read", "documents.upload", "documents.download",
            "attendance.clock.create", "attendance.adjustment.submit", "attendance.day.approve", "attendance.exception.resolve",
            "leave.request.create", "leave.request.approve", "leave.request.reject",
            "approvals.decision.approve", "approvals.decision.reject",
            "payroll.run.read", "payroll.run.create", "payroll.run.calculate", "payroll.run.finalize", "payroll.exceptions.resolve",
            "settlement.batch.read", "settlement.batch.generate", "settlement.batch.approve", "settlement.batch.export",
            "compliance.rules.read",
            "recruitment.requisition.read", "recruitment.requisition.create", "recruitment.requisition.approve",
            "recruitment.candidate.read", "recruitment.candidate.manage",
            "recruitment.application.read", "recruitment.application.move", "recruitment.application.reject",
            "recruitment.interview.manage", "recruitment.scorecard.submit", "recruitment.scorecard.read_all",
            "recruitment.offer.read", "recruitment.offer.read_sensitive", "recruitment.offer.create", "recruitment.offer.approve", "recruitment.offer.issue",
            "recruitment.hire",
            "reports.read", "reports.export",
            "admin.roles.manage", "admin.settings.manage", "admin.retention.manage",
            "integrations.manage",
            "audit.read",
            "admin" 
        },
        new HashSet<string> { "core.platform", "people", "organization", "documents", "attendance", "leave", "approvals", "payroll", "compliance", "settlement", "recruitment", "reports", "admin", "integrations", "notifications", "audit" }
    );
});

// PII Encryption Service (AES-256-GCM + Blind Indexing)
builder.Services.AddSingleton<IPiiEncryptionService, AesPiiEncryptionService>();

// Storage Provider for Documents & Reports
builder.Services.AddSingleton<IStorageProvider, LocalStorageProvider>();

// Module Repositories
builder.Services.AddScoped<OrganizationRepository>(_ => new OrganizationRepository(connectionString));
builder.Services.AddScoped<PeopleRepository>(sp => new PeopleRepository(connectionString, sp.GetRequiredService<IPiiEncryptionService>()));
builder.Services.AddScoped<Workforce.Modules.People.Application.Contracts.IPeopleHiringContract, Workforce.Modules.People.Application.PeopleHiringContract>();
builder.Services.AddScoped<DocumentsRepository>(_ => new DocumentsRepository(connectionString));
builder.Services.AddScoped<Workforce.Modules.Documents.Application.Contracts.IDocumentsApplicationContract, Workforce.Modules.Documents.Application.DocumentsApplicationContract>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<ILeaveRepository, LeaveRepository>();
builder.Services.AddScoped<IApprovalsRepository, ApprovalsRepository>();

// Phase 4 Services
builder.Services.AddScoped<IComplianceRepository, ComplianceRepository>();
builder.Services.AddScoped<IPayrollRepository, PayrollRepository>();
builder.Services.AddScoped<IPayrollCalculationEngine, DeterministicPayrollEngine>();
builder.Services.AddScoped<ISettlementRepository, SettlementRepository>();
builder.Services.AddScoped<IPaymentExportAdapter, NeutralCsvPaymentExportAdapter>();

// Phase 5 Recruitment Services
builder.Services.AddScoped<IRecruitmentRepository>(_ => new RecruitmentRepository(connectionString));
builder.Services.AddScoped<RecruitmentRepository>(_ => new RecruitmentRepository(connectionString));

// Phase 6 Services: Audit, Notifications, Integrations, Administration, Reporting
builder.Services.AddScoped<IAuditRepository>(_ => new AuditRepository(connectionString));
builder.Services.AddScoped<INotificationsRepository>(_ => new NotificationsRepository(connectionString));
builder.Services.AddScoped<IIntegrationsRepository>(_ => new IntegrationsRepository(connectionString));
builder.Services.AddScoped<IOutboundIntegrationAdapter, GenericWebhookAdapter>();
builder.Services.AddScoped<IAdministrationRepository>(sp => new AdministrationRepository(connectionString, sp.GetRequiredService<IAuditRepository>()));
builder.Services.AddScoped<IReportingRepository>(_ => new ReportingRepository(connectionString));
builder.Services.AddScoped<IReportingExportEngine, ReportingExportEngine>();

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    })
    .AddApplicationPart(typeof(Workforce.Modules.People.Api.PeopleController).Assembly)
    .AddApplicationPart(typeof(Workforce.Modules.Organization.Api.OrganizationController).Assembly)
    .AddApplicationPart(typeof(Workforce.Modules.Documents.Api.DocumentsController).Assembly)
    .AddApplicationPart(typeof(Workforce.Modules.Attendance.Api.AttendanceController).Assembly)
    .AddApplicationPart(typeof(Workforce.Modules.Leave.Api.LeaveController).Assembly)
    .AddApplicationPart(typeof(Workforce.Modules.Approvals.Api.ApprovalsController).Assembly)
    .AddApplicationPart(typeof(Workforce.Modules.Payroll.Api.PayrollRunsController).Assembly)
    .AddApplicationPart(typeof(Workforce.Modules.Settlement.Api.SettlementController).Assembly)
    .AddApplicationPart(typeof(Workforce.Modules.Compliance.Api.ComplianceRulesController).Assembly)
    .AddApplicationPart(typeof(Workforce.Modules.Recruitment.Api.RecruitmentRequisitionsController).Assembly)
    .AddApplicationPart(typeof(Workforce.Modules.Audit.Api.AuditController).Assembly)
    .AddApplicationPart(typeof(Workforce.Modules.Notifications.Api.NotificationsController).Assembly)
    .AddApplicationPart(typeof(Workforce.Modules.Integrations.Api.IntegrationsController).Assembly)
    .AddApplicationPart(typeof(Workforce.Modules.Identity.Api.AdministrationRolesController).Assembly)
    .AddApplicationPart(typeof(Workforce.Modules.Reporting.Api.ReportsController).Assembly);

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
    await ComplianceMigrations.ApplyAsync(dataSource);
    await PayrollMigrations.ApplyAsync(dataSource);
    await SettlementMigrations.ApplyAsync(dataSource);
    await RecruitmentMigrations.ApplyAsync(connectionString);
    await AuditMigrations.ApplyMigrationsAsync(connectionString);
    await NotificationsMigrations.ApplyMigrationsAsync(connectionString);
    await IntegrationsMigrations.ApplyMigrationsAsync(connectionString);
    await AdministrationMigrations.ApplyMigrationsAsync(connectionString);
    await ReportingMigrations.ApplyMigrationsAsync(connectionString);

    // Seed compliance rules
    using (var scope = app.Services.CreateScope())
    {
        var complianceRepo = scope.ServiceProvider.GetRequiredService<IComplianceRepository>();
        await complianceRepo.SeedDefaultEgyptRulesAsync();
    }

    Console.WriteLine("[MIGRATIONS] All 16 Phase 1 - 6 Database schemas initialized successfully.");
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
app.MapHealthChecks("/health/ready");

// OpenAPI Specification Endpoint
app.MapOpenApi();

app.MapControllers();

if (args.Contains("--run-db-benchmark"))
{
    var code = await Workforce.Host.Api.Testing.WorkforceDbBenchmark.RunAsync(app.Services);
    Environment.Exit(code);
}

// Seed initial test data for Phase 4 if needed
app.MapGet("/api/v1/seed/phase4", async (
    IPayrollRepository payrollRepo,
    IUserContext uCtx) =>
{
    var tid = uCtx.TenantId;
    var lid = uCtx.LegalEntityId ?? new LegalEntityId(Guid.Parse("33333333-3333-3333-3333-333333333333"));

    var period = new Workforce.Modules.Payroll.Domain.PayrollPeriod(
        Guid.Parse("44444444-1111-2222-3333-444444444444"),
        tid, lid, "2026-08-MONTHLY",
        new DateOnly(2026, 8, 1),
        new DateOnly(2026, 8, 31),
        new DateOnly(2026, 8, 31)
    );
    await payrollRepo.CreatePeriodAsync(period);

    var run = new Workforce.Modules.Payroll.Domain.PayrollRun(
        Guid.Parse("55555555-1111-2222-3333-444444444444"),
        tid, lid, period.Id, "RUN-2026-08-STD", "EGP"
    );
    await payrollRepo.CreateRunAsync(run);

    return Results.Ok(new { status = "Seeded Phase 4 baseline period and run", periodId = period.Id, runId = run.Id });
});

app.Run();
