using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Trace;
using Workforce.BuildingBlocks.Database;
using Workforce.Host.Api.Middleware;
using Workforce.Modules.Documents.Infrastructure;
using Workforce.Modules.Organization.Infrastructure;
using Workforce.Modules.People.Infrastructure;
using Workforce.SharedKernel.Primitives;
using Workforce.SharedKernel.Security;

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
        new HashSet<string> { "people.employee.read", "people.employee.create", "people.employee.update", "people.employee.reveal_pii", "organization.unit.read", "organization.unit.create", "organization.unit.update", "documents.read", "documents.upload", "documents.download", "admin" },
        new HashSet<string> { "core.platform", "people", "organization", "documents" }
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

// Controllers with cross-assembly discovery
builder.Services.AddControllers()
    .AddApplicationPart(typeof(Workforce.Modules.Organization.Api.OrganizationController).Assembly)
    .AddApplicationPart(typeof(Workforce.Modules.People.Api.PeopleController).Assembly)
    .AddApplicationPart(typeof(Workforce.Modules.Documents.Api.DocumentsController).Assembly);

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

// Platform Context Resolution
app.UseMiddleware<TenantResolutionMiddleware>();

// Configure OpenAPI & Controllers
app.MapOpenApi();
app.MapControllers();

// Health (Liveness) Probe
app.MapGet("/health", (HttpContext http) =>
{
    var correlationId = http.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
    var traceId = http.Items["TraceId"]?.ToString() ?? Guid.NewGuid().ToString("N");

    using var activity = activitySource.StartActivity("HealthCheck");
    activity?.SetTag("health.status", "Healthy");

    return Results.Ok(new
    {
        status = "Healthy",
        timestamp = DateTime.UtcNow.ToString("o"),
        version = "1.0.0",
        correlationId = correlationId,
        traceId = traceId
    });
})
.WithName("GetHealth")
.WithSummary("System health and liveness probe");

// Readiness Probe (Verifies PostgreSQL connectivity)
app.MapGet("/health/ready", async (HttpContext http) =>
{
    var correlationId = http.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
    var traceId = http.Items["TraceId"]?.ToString() ?? Guid.NewGuid().ToString("N");

    using var activity = activitySource.StartActivity("DatabaseReadinessCheck");

    string dbStatus;
    string pgVersion = "Unknown";
    try
    {
        await using var conn = new Npgsql.NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = new Npgsql.NpgsqlCommand("SELECT version();", conn);
        var res = await cmd.ExecuteScalarAsync();
        pgVersion = res?.ToString() ?? "Unknown";
        dbStatus = "Connected";
        activity?.SetTag("db.status", "Connected");
    }
    catch (Exception ex)
    {
        dbStatus = $"Failed: {ex.Message}";
        activity?.SetTag("db.status", "Failed");
        return Results.Json(new
        {
            status = "Unhealthy",
            database = dbStatus,
            timestamp = DateTime.UtcNow.ToString("o"),
            correlationId = correlationId,
            traceId = traceId
        }, statusCode: 503);
    }

    return Results.Ok(new
    {
        status = "Ready",
        database = dbStatus,
        postgresVersion = pgVersion,
        timestamp = DateTime.UtcNow.ToString("o"),
        correlationId = correlationId,
        traceId = traceId
    });
})
.WithName("GetReadiness")
.WithSummary("System readiness probe verifying database connectivity");

app.Run();
