using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using OpenTelemetry.Trace;
using Workforce.BuildingBlocks.Database;
using Workforce.Host.Api.Health;
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
using Workforce.Modules.Tenancy.Infrastructure;
using Workforce.Modules.Recruitment.Domain;
using Workforce.Host.Api.Application;
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

builder.Services.AddProblemDetails();
builder.Services.AddSingleton<MigrationReadinessState>();
builder.Services.AddHealthChecks()
    .AddCheck<MigrationReadinessHealthCheck>("database_migrations", tags: new[] { "ready" })
    .AddCheck<DatabaseConnectivityHealthCheck>("database_connectivity", tags: new[] { "ready" });

// Database credentials must come from configuration or the deployment environment.
var connectionString = DatabaseConnectionResolver.Resolve(
    builder.Configuration.GetConnectionString("DefaultConnection"));

// Register NpgsqlDataSource
var dataSource = NpgsqlDataSource.Create(connectionString);
builder.Services.AddSingleton(dataSource);

// Security & User Context Resolution
builder.Services.AddScoped<IUserContextProvider, RequestUserContextProvider>();
builder.Services.AddScoped<IUserContext>(sp =>
{
    var provider = sp.GetRequiredService<IUserContextProvider>();
    return provider.Current ?? throw new InvalidOperationException(
        "No authenticated user context is available for this request.");
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
builder.Services.AddScoped<Workforce.Modules.Attendance.Application.Contracts.IAttendanceSelfServiceContract,
    Workforce.Modules.Attendance.Application.Services.AttendanceSelfServiceService>();
builder.Services.AddScoped<Workforce.Modules.Leave.Application.Contracts.ILeaveSelfServiceQueryContract,
    Workforce.Modules.Leave.Application.Services.LeaveSelfServiceQueryService>();
builder.Services.AddScoped<Workforce.Modules.Leave.Application.Contracts.ILeaveRequestApplicationContract,
    Workforce.Modules.Leave.Application.Services.LeaveRequestApplicationService>();
builder.Services.AddScoped<Workforce.Modules.Leave.Application.Contracts.ILeaveApprovalWorkflowStarter,
    LeaveApprovalWorkflowStarter>();
builder.Services.AddScoped<Workforce.Modules.Approvals.Application.Contracts.IApprovalDecisionSideEffect,
    LeaveApprovalDecisionSideEffect>();

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
builder.Services.AddScoped<TenancyRepository>(_ => new TenancyRepository(connectionString));

// Phase 7A Services: AI Read / Analyze / Explain
builder.Services.AddScoped<Workforce.Modules.Ai.Infrastructure.IAiRepository>(_ => new Workforce.Modules.Ai.Infrastructure.AiRepository(connectionString));
builder.Services.AddSingleton<Workforce.Modules.Ai.Application.Contracts.IAiModelProvider, Workforce.Modules.Ai.Application.Services.DeterministicTestAiProvider>();

// Closeout Gate 8: per-user/tenant AI request rate limit (configurable).
var aiRateLimit = int.TryParse(Environment.GetEnvironmentVariable("ZAINX_AI_RATE_LIMIT_PER_MINUTE"), out var rl) && rl > 0 ? rl : 30;
builder.Services.AddSingleton(new Workforce.Modules.Ai.Application.Services.AiRateLimiter(aiRateLimit));

builder.Services.AddScoped<Workforce.Modules.Ai.Application.Contracts.AiToolRegistry>(sp =>
{
    var registry = new Workforce.Modules.Ai.Application.Contracts.AiToolRegistry();
    var peopleRepo = new PeopleRepository(connectionString, sp.GetRequiredService<IPiiEncryptionService>());
    var attendanceRepo = sp.GetRequiredService<IAttendanceRepository>();
    var leaveRepo = sp.GetRequiredService<ILeaveRepository>();
    var payrollRepo = sp.GetRequiredService<IPayrollRepository>();
    var recruitmentRepo = new RecruitmentRepository(connectionString);
    var reportingRepo = new ReportingRepository(connectionString);
    var auditRepo = new AuditRepository(connectionString);
    var aiRepo = new Workforce.Modules.Ai.Infrastructure.AiRepository(connectionString);

    // 16 Allowlisted Read-Only Tools
    registry.RegisterTool(new Workforce.Modules.Ai.Application.Tools.PeopleSearchToolHandler(peopleRepo));
    registry.RegisterTool(new Workforce.Modules.Ai.Application.Tools.PeopleGetSummaryToolHandler(peopleRepo));
    registry.RegisterTool(new Workforce.Modules.Ai.Application.Tools.AttendanceGetRecordsToolHandler(attendanceRepo));
    registry.RegisterTool(new Workforce.Modules.Ai.Application.Tools.AttendanceGetExceptionsToolHandler(attendanceRepo));
    registry.RegisterTool(new Workforce.Modules.Ai.Application.Tools.LeaveGetBalanceSummaryToolHandler(leaveRepo));
    registry.RegisterTool(new Workforce.Modules.Ai.Application.Tools.LeaveGetRequestSummaryToolHandler(leaveRepo));
    registry.RegisterTool(new Workforce.Modules.Ai.Application.Tools.PayrollGetRunSummaryToolHandler(payrollRepo));
    registry.RegisterTool(new Workforce.Modules.Ai.Application.Tools.PayrollGetEmployeeTraceToolHandler(payrollRepo));
    registry.RegisterTool(new Workforce.Modules.Ai.Application.Tools.PayrollExplainExceptionToolHandler(payrollRepo));
    registry.RegisterTool(new Workforce.Modules.Ai.Application.Tools.RecruitmentGetRequisitionSummaryToolHandler(recruitmentRepo));
    registry.RegisterTool(new Workforce.Modules.Ai.Application.Tools.RecruitmentGetCandidateSummaryToolHandler(recruitmentRepo));
    registry.RegisterTool(new Workforce.Modules.Ai.Application.Tools.RecruitmentGetApplicationTimelineToolHandler(recruitmentRepo));
    registry.RegisterTool(new Workforce.Modules.Ai.Application.Tools.ReportingRunGovernedReportToolHandler(reportingRepo));
    registry.RegisterTool(new Workforce.Modules.Ai.Application.Tools.AuditSearchScopedToolHandler(auditRepo));
    registry.RegisterTool(new Workforce.Modules.Ai.Application.Tools.PolicySearchToolHandler(aiRepo));
    registry.RegisterTool(new Workforce.Modules.Ai.Application.Tools.ProductKnowledgeSearchToolHandler(aiRepo));

    return registry;
});

// Phase 7B: Action Contracts & Registry
builder.Services.AddScoped<Workforce.Modules.People.Application.Contracts.IPeopleAssignmentApplicationContract>(sp =>
{
    var peopleRepo = new PeopleRepository(connectionString, sp.GetRequiredService<IPiiEncryptionService>());
    return new Workforce.Modules.People.Application.Services.PeopleAssignmentApplicationService(peopleRepo);
});

builder.Services.AddScoped<Workforce.Modules.Recruitment.Contracts.IRecruitmentActionContract>(sp =>
{
    var recruitmentRepo = new RecruitmentRepository(connectionString);
    var approvalsRepo = sp.GetService<IApprovalsRepository>();
    return new Workforce.Modules.Recruitment.Services.RecruitmentActionService(recruitmentRepo, approvalsRepo);
});

builder.Services.AddScoped<Workforce.Modules.Leave.Application.Contracts.ILeaveActionContract>(sp =>
{
    var leaveRepo = sp.GetRequiredService<ILeaveRepository>();
    return new Workforce.Modules.Leave.Application.Services.LeaveActionService(leaveRepo);
});

builder.Services.AddScoped<Workforce.Modules.Ai.Application.Contracts.AiActionRegistry>(sp =>
{
    var actionRegistry = new Workforce.Modules.Ai.Application.Contracts.AiActionRegistry();
    var peopleContract = sp.GetRequiredService<Workforce.Modules.People.Application.Contracts.IPeopleAssignmentApplicationContract>();
    var recruitmentContract = sp.GetRequiredService<Workforce.Modules.Recruitment.Contracts.IRecruitmentActionContract>();
    var leaveContract = sp.GetRequiredService<Workforce.Modules.Leave.Application.Contracts.ILeaveActionContract>();

    actionRegistry.RegisterAction(new Workforce.Modules.Ai.Application.Actions.PeopleChangeLocationActionHandler(peopleContract));
    actionRegistry.RegisterAction(new Workforce.Modules.Ai.Application.Actions.PeopleChangeManagerActionHandler(peopleContract));
    actionRegistry.RegisterAction(new Workforce.Modules.Ai.Application.Actions.RecruitmentMoveStageActionHandler(recruitmentContract));
    actionRegistry.RegisterAction(new Workforce.Modules.Ai.Application.Actions.RecruitmentSubmitRequisitionActionHandler(recruitmentContract));
    actionRegistry.RegisterAction(new Workforce.Modules.Ai.Application.Actions.LeaveCancelRequestActionHandler(leaveContract));

    return actionRegistry;
});

builder.Services.AddScoped<Workforce.Modules.Ai.Application.Contracts.IAiProposalService, Workforce.Modules.Ai.Application.Services.AiProposalService>();
builder.Services.AddScoped<Workforce.Modules.Ai.Application.Contracts.IAiConversationService, Workforce.Modules.Ai.Application.Services.AiConversationService>();

// Closeout Gate 10: configurable conversation retention (days; 0 disables purging).
// This is an operational privacy setting - no statutory retention period is implied.
var aiRetentionDays = int.TryParse(Environment.GetEnvironmentVariable("ZAINX_AI_CONVERSATION_RETENTION_DAYS"), out var rd) && rd >= 0 ? rd : 90;

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
    .AddApplicationPart(typeof(Workforce.Modules.Reporting.Api.ReportsController).Assembly)
    .AddApplicationPart(typeof(Workforce.Modules.Ai.Api.AiController).Assembly)
    .AddApplicationPart(typeof(Workforce.Modules.Tenancy.Api.TenancyController).Assembly);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// CORS uses explicit configured origins. Development has an explicit localhost default only.
var configuredCorsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins")
    .GetChildren()
    .Select(section => section.Value)
    .OfType<string>()
    .Concat((builder.Configuration["Cors:AllowedOrigins"] ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    .Concat((Environment.GetEnvironmentVariable("ZAINX_CORS_ALLOWED_ORIGINS") ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

if (configuredCorsOrigins.Length == 0 && (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Test")))
{
    configuredCorsOrigins = new[] { "http://localhost:4200", "http://127.0.0.1:4200" };
}

if (configuredCorsOrigins.Length == 0)
{
    throw new InvalidOperationException(
        "CORS is not configured. Set Cors:AllowedOrigins or ZAINX_CORS_ALLOWED_ORIGINS before starting the API.");
}

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(configuredCorsOrigins)
              .WithHeaders(
                  "Accept",
                  "Accept-Language",
                  "Authorization",
                  "Content-Type",
                  "X-Correlation-ID",
                  "X-Legal-Entity-ID",
                  "X-Tenant-ID",
                  "X-Trace-ID")
              .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS");
    });
});

var app = builder.Build();

// Auto-run schema migrations
try
{
    await MigrationRunner.EnsureMigrationHistoryTableAsync(connectionString);
    await TenancyMigrations.ApplyAsync(connectionString, app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Test"));
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
    await Workforce.Modules.Ai.Infrastructure.AiMigrations.ApplyMigrationsAsync(connectionString);

    // Closeout Gate 10: apply conversation retention on startup.
    if (aiRetentionDays > 0)
    {
        var aiRepoForRetention = new Workforce.Modules.Ai.Infrastructure.AiRepository(connectionString);
        var purged = await aiRepoForRetention.PurgeConversationsOlderThanAsync(aiRetentionDays);
        Console.WriteLine($"[AI RETENTION] Purged {purged} conversation(s) older than {aiRetentionDays} day(s).");
    }
    else
    {
        Console.WriteLine("[AI RETENTION] Conversation retention purge disabled (ZAINX_AI_CONVERSATION_RETENTION_DAYS=0).");
    }

    // Seed compliance rules
    using (var scope = app.Services.CreateScope())
    {
        var complianceRepo = scope.ServiceProvider.GetRequiredService<IComplianceRepository>();
        await complianceRepo.SeedDefaultEgyptRulesAsync();
    }

    Console.WriteLine("[MIGRATIONS] All 17 Phase 1 - 7A Database schemas initialized successfully.");
}
catch (Exception ex)
{
    app.Services.GetRequiredService<MigrationReadinessState>().MarkFailed(ex);
    Console.Error.WriteLine($"[MIGRATIONS] Startup failed; API will not serve requests: {ex.Message}");
    throw;
}

app.Services.GetRequiredService<MigrationReadinessState>().MarkReady();
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
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// OpenAPI Specification Endpoint
app.MapOpenApi();

app.MapControllers();

if (args.Contains("--run-db-benchmark"))
{
    var code = await Workforce.Host.Api.Testing.WorkforceDbBenchmark.RunAsync(app.Services);
    Environment.Exit(code);
}

// Development/test-only seed route. It is never mapped in production and still
// requires the explicit payroll write permission in the local sandbox.
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Test"))
{
    app.MapGet("/api/v1/seed/phase4", async (
        IPayrollRepository payrollRepo,
        IUserContext uCtx) =>
    {
        if (!uCtx.HasPermission("payroll.run.create") && !uCtx.HasPermission("admin"))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Seed Permission Required",
                detail: "The development seed route requires payroll.run.create.");
        }

        var tid = uCtx.TenantId;
        if (!uCtx.LegalEntityId.HasValue)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Legal Entity Context Required",
                detail: "A legal entity context is required before seeding payroll data.");
        }

        var lid = uCtx.LegalEntityId.Value;
        var period = new Workforce.Modules.Payroll.Domain.PayrollPeriod(
            Guid.Parse("44444444-1111-2222-3333-444444444444"),
            tid, lid, "2026-08-MONTHLY",
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), new DateOnly(2026, 8, 31));
        await payrollRepo.CreatePeriodAsync(period);

        var run = new Workforce.Modules.Payroll.Domain.PayrollRun(
            Guid.Parse("55555555-1111-2222-3333-444444444444"),
            tid, lid, period.Id, "RUN-2026-08-STD", "EGP");
        await payrollRepo.CreateRunAsync(run);

        return Results.Ok(new { status = "Seeded Phase 4 baseline period and run", periodId = period.Id, runId = run.Id });
    });
}

app.Run();
