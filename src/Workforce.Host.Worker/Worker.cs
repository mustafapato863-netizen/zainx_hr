using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Workforce.Modules.Integrations.Application;
using Workforce.Modules.Integrations.Infrastructure;
using Workforce.Modules.Payroll.Infrastructure;
using Workforce.Modules.Reporting.Application;
using Workforce.Modules.Reporting.Infrastructure;
using Workforce.SharedKernel.Security;

namespace Workforce.Host.Worker;

public class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<Worker> _logger;

    public Worker(IServiceScopeFactory scopeFactory, ILogger<Worker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Workforce Enterprise Background Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var workDone = false;

            try
            {
                using var scope = _scopeFactory.CreateScope();

                // 1. Process Payroll background calculation jobs
                var payrollExecutor = scope.ServiceProvider.GetRequiredService<IPayrollJobExecutor>();
                var payrollProcessed = await payrollExecutor.ProcessNextJobAsync(stoppingToken);
                if (payrollProcessed) workDone = true;

                // 2. Process Outbound Integrations delivery queue
                var integrationsRepo = scope.ServiceProvider.GetRequiredService<IIntegrationsRepository>();
                var webhookAdapter = scope.ServiceProvider.GetRequiredService<IOutboundIntegrationAdapter>();
                var encryptionService = scope.ServiceProvider.GetRequiredService<IPiiEncryptionService>();

                var pendingDeliveries = await integrationsRepo.GetPendingDeliveriesAsync(10, stoppingToken);
                foreach (var delivery in pendingDeliveries)
                {
                    var connector = await integrationsRepo.GetConnectorByIdAsync(delivery.TenantId, delivery.ConnectorId, stoppingToken);
                    if (connector != null && connector.IsActive)
                    {
                        string? secret = null;
                        if (!string.IsNullOrWhiteSpace(connector.EncryptedCredentials))
                        {
                            try { secret = encryptionService.Decrypt(connector.EncryptedCredentials); }
                            catch { }
                        }

                        var result = await webhookAdapter.DeliverAsync(connector, delivery, secret, stoppingToken);
                        delivery.RecordAttempt(result.Succeeded, result.StatusCode, result.ResponseOrError);
                        await integrationsRepo.UpdateDeliveryStatusAsync(delivery, stoppingToken);
                        workDone = true;
                    }
                }

                // 3. Process Report Export jobs
                var reportingRepo = scope.ServiceProvider.GetRequiredService<IReportingRepository>();
                var exportEngine = scope.ServiceProvider.GetRequiredService<IReportingExportEngine>();
                var pendingReportJobs = await reportingRepo.GetPendingReportJobsAsync(5, stoppingToken);
                foreach (var reportJob in pendingReportJobs)
                {
                    await exportEngine.ProcessExportJobAsync(reportJob, stoppingToken);
                    workDone = true;
                }

                if (!workDone)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Background Worker processing loop.");
                await Task.Delay(5000, stoppingToken);
            }
        }

        _logger.LogInformation("Workforce Enterprise Background Worker stopped.");
    }
}
