using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Phase-0 OpenTelemetry Instrumentation
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

var app = builder.Build();

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

// Configure OpenAPI
app.MapOpenApi();

// Phase-0 Health (Liveness) Probe
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

// Phase-0 Readiness Probe (Verifies PostgreSQL 18 live connectivity)
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

// Standardized RFC 7807 ProblemDetails Error Endpoint for Testing
app.MapGet("/test-error", (HttpContext http) =>
{
    var correlationId = http.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
    var traceId = http.Items["TraceId"]?.ToString() ?? Guid.NewGuid().ToString("N");

    var problemDetails = new ProblemDetails
    {
        Status = StatusCodes.Status500InternalServerError,
        Type = "https://zainx.com/errors/internal-error",
        Title = "Test Internal Server Error",
        Detail = "A controlled simulation of an application fault adhering to RFC 7807 Problem Details.",
        Instance = http.Request.Path
    };

    problemDetails.Extensions["correlationId"] = correlationId;
    problemDetails.Extensions["traceId"] = traceId;

    return Results.Problem(problemDetails);
})
.WithName("GetTestError")
.WithSummary("Simulated failure returning RFC 7807 ProblemDetails");

app.Run();
