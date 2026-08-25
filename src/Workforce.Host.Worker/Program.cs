using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Workforce.BuildingBlocks.Database;
using Workforce.Modules.Documents.Infrastructure;
using Workforce.Host.Worker;
using Workforce.Modules.Audit.Infrastructure;
using Workforce.Modules.Compliance.Infrastructure;
using Workforce.Modules.Identity.Infrastructure;
using Workforce.Modules.Integrations.Application;
using Workforce.Modules.Integrations.Infrastructure;
using Workforce.Modules.Notifications.Infrastructure;
using Workforce.Modules.Payroll.Domain.CalculationEngine;
using Workforce.Modules.Payroll.Infrastructure;
using Workforce.Modules.Reporting.Application;
using Workforce.Modules.Reporting.Infrastructure;
using Workforce.SharedKernel.Security;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = DatabaseConnectionResolver.Resolve(
    builder.Configuration.GetSection("Database:ConnectionString").Value
    ?? builder.Configuration.GetConnectionString("Default")
    ?? builder.Configuration.GetConnectionString("DefaultConnection"));

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
var dataSource = dataSourceBuilder.Build();

builder.Services.AddSingleton(dataSource);
builder.Services.AddSingleton<IPiiEncryptionService, AesPiiEncryptionService>();
builder.Services.AddSingleton<IStorageProvider, LocalStorageProvider>();

// Core Scoped Repositories & Services
builder.Services.AddScoped<IPayrollRepository, PayrollRepository>();
builder.Services.AddScoped<IComplianceRepository, ComplianceRepository>();
builder.Services.AddScoped<IPayrollCalculationEngine, DeterministicPayrollEngine>();
builder.Services.AddScoped<IPayrollJobExecutor, PayrollJobExecutor>();

// Phase 6 Services
builder.Services.AddScoped<IAuditRepository>(_ => new AuditRepository(connectionString));
builder.Services.AddScoped<INotificationsRepository>(_ => new NotificationsRepository(connectionString));
builder.Services.AddScoped<IIntegrationsRepository>(_ => new IntegrationsRepository(connectionString));
builder.Services.AddScoped<IOutboundIntegrationAdapter, GenericWebhookAdapter>();
builder.Services.AddScoped<IAdministrationRepository>(sp => new AdministrationRepository(connectionString, sp.GetRequiredService<IAuditRepository>()));
builder.Services.AddScoped<IReportingRepository>(_ => new ReportingRepository(connectionString));
builder.Services.AddScoped<IReportingExportEngine, ReportingExportEngine>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
