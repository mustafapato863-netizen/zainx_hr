using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Workforce.Modules.Compliance.Domain;
using Workforce.Modules.Compliance.Infrastructure;
using Workforce.Modules.Payroll.Domain;
using Workforce.Modules.Payroll.Domain.CalculationEngine;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Payroll.Infrastructure;

public interface IPayrollJobExecutor
{
    Task<bool> ProcessNextJobAsync(CancellationToken ct = default);
}

public class PayrollJobExecutor : IPayrollJobExecutor
{
    private readonly IPayrollRepository _repository;
    private readonly IComplianceRepository _complianceRepository;
    private readonly IPayrollCalculationEngine _calculationEngine;
    private readonly ILogger<PayrollJobExecutor> _logger;

    public PayrollJobExecutor(
        IPayrollRepository repository,
        IComplianceRepository complianceRepository,
        IPayrollCalculationEngine calculationEngine,
        ILogger<PayrollJobExecutor> logger)
    {
        _repository = repository;
        _complianceRepository = complianceRepository;
        _calculationEngine = calculationEngine;
        _logger = logger;
    }

    public async Task<bool> ProcessNextJobAsync(CancellationToken ct = default)
    {
        var job = await _repository.ClaimNextQueuedJobAsync(ct);
        if (job == null) return false;

        try
        {
            var run = await _repository.GetRunByIdAsync(new TenantId(job.TenantId), job.PayrollRunId, ct);
            if (run == null)
            {
                throw new InvalidOperationException($"Payroll run '{job.PayrollRunId}' not found for claimed job '{job.Id}'.");
            }

            var snapshots = await _repository.GetSnapshotsByRunAsync(run.Id, ct);
            if (snapshots.Count == 0)
            {
                throw new InvalidOperationException($"No input snapshots found for payroll run '{run.Id}'.");
            }

            // Resolve statutory effective date from the payroll period
            var period = await _repository.GetPeriodByIdAsync(new TenantId(job.TenantId), run.PeriodId, ct);
            if (period == null)
            {
                throw new InvalidOperationException($"Payroll period '{run.PeriodId}' not found for run '{run.Id}'.");
            }

            // Fetch active compliance rules using governed statutory applicability bases:
            // EG_SOCIAL_INSURANCE -> ContributionPeriod (period.PeriodEnd)
            // EG_INCOME_TAX -> PayrollTaxPeriod (period.PeriodEnd)
            var activeRules = new List<StatutoryRuleVersion>();
            
            var gosi = await _complianceRepository.GetActiveRuleVersionForPeriodAsync("EG_SOCIAL_INSURANCE", period.PeriodStart, period.PeriodEnd, period.PaymentDate, ct);
            if (gosi == null)
            {
                throw new InvalidOperationException($"BLOCKING COMPLIANCE EXCEPTION: No applicable statutory rule version found for 'EG_SOCIAL_INSURANCE' on contribution period {period.PeriodStart:yyyy-MM-dd}..{period.PeriodEnd:yyyy-MM-dd}. Stale fallback is forbidden.");
            }
            if (gosi.Status != VerificationStatus.Verified)
            {
                throw new InvalidOperationException($"BLOCKING COMPLIANCE EXCEPTION: Applicable statutory rule version for 'EG_SOCIAL_INSURANCE' (v{gosi.VersionNumber}) is UNVERIFIED.");
            }
            activeRules.Add(gosi);

            var tax = await _complianceRepository.GetActiveRuleVersionForPeriodAsync("EG_INCOME_TAX", period.PeriodStart, period.PeriodEnd, period.PaymentDate, ct);
            if (tax == null)
            {
                throw new InvalidOperationException($"BLOCKING COMPLIANCE EXCEPTION: No applicable statutory rule version found for 'EG_INCOME_TAX' on salary tax period {period.PeriodStart:yyyy-MM-dd}..{period.PeriodEnd:yyyy-MM-dd}. Stale fallback is forbidden.");
            }
            if (tax.Status != VerificationStatus.Verified)
            {
                throw new InvalidOperationException($"BLOCKING COMPLIANCE EXCEPTION: Applicable statutory rule version for 'EG_INCOME_TAX' (v{tax.VersionNumber}) is UNVERIFIED.");
            }
            activeRules.Add(tax);

            // Inspect snapshots for any historical arrears / frozen wages (متجمد الأجور والمرتبات)
            // and load the required historical statutory rule versions
            foreach (var snapshot in snapshots)
            {
                if (!string.IsNullOrWhiteSpace(snapshot.AllowancesJson) && snapshot.AllowancesJson != "[]")
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(snapshot.AllowancesJson);
                        foreach (var el in doc.RootElement.EnumerateArray())
                        {
                            var code = el.TryGetProperty("code", out var cProp) ? cProp.GetString() ?? "" : "";
                            bool isArrears = false;
                            if (el.TryGetProperty("temporalTreatment", out var ttProp))
                            {
                                var ttStr = ttProp.GetString();
                                if (string.Equals(ttStr, "ArrearsFrozenWages", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(ttStr, "FROZEN_WAGES", StringComparison.OrdinalIgnoreCase) ||
                                    (ttProp.ValueKind == JsonValueKind.Number && ttProp.GetInt32() == (int)SalaryTaxTemporalTreatment.ArrearsFrozenWages))
                                {
                                    isArrears = true;
                                }
                            }
                            else if (code.StartsWith("ARREARS_", StringComparison.OrdinalIgnoreCase) ||
                                     code.StartsWith("FROZEN_", StringComparison.OrdinalIgnoreCase))
                            {
                                isArrears = true;
                            }

                            if (isArrears)
                            {
                                DateOnly entStart = new DateOnly(2024, 1, 1);
                                DateOnly entEnd = new DateOnly(2024, 1, 31);
                                if (el.TryGetProperty("entitlementPeriodStart", out var esProp) && esProp.ValueKind == JsonValueKind.String)
                                {
                                    if (DateOnly.TryParse(esProp.GetString(), out var parsedEs)) entStart = parsedEs;
                                }
                                if (el.TryGetProperty("entitlementPeriodEnd", out var eeProp) && eeProp.ValueKind == JsonValueKind.String)
                                {
                                    if (DateOnly.TryParse(eeProp.GetString(), out var parsedEe)) entEnd = parsedEe;
                                }

                                var histTax = await _complianceRepository.GetActiveRuleVersionForEntitlementPeriodAsync("EG_INCOME_TAX", entStart, entEnd, ct);
                                if (histTax != null && activeRules.All(r => r.Id != histTax.Id))
                                {
                                    activeRules.Add(histTax);
                                }

                                var histGosi = await _complianceRepository.GetActiveRuleVersionForEntitlementPeriodAsync("EG_SOCIAL_INSURANCE", entStart, entEnd, ct);
                                if (histGosi != null && activeRules.All(r => r.Id != histGosi.Id))
                                {
                                    activeRules.Add(histGosi);
                                }
                            }
                        }
                    }
                    catch { }
                }
            }

            // Execute deterministic calculation
            run.LoadInputs(snapshots, run.RowVersion);
            run.Calculate(_calculationEngine, activeRules, run.RowVersion);

            // Atomically persist calculation results, lines, traces, and exceptions
            await _repository.SaveResultsAndTracesAsync(run, ct);

            // Mark job complete
            var hasWarnings = run.Exceptions.Count > 0;
            job.MarkCompleted(hasWarnings, $"{{\"employeeCount\":{run.EmployeeCount},\"exceptions\":{run.Exceptions.Count}}}", job.RowVersion);
            await _repository.UpdateJobAsync(job, ct);

            _logger.LogInformation("Successfully executed payroll calculation job '{JobId}' for run '{RunId}'. Status: Completed.", job.Id, run.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute payroll calculation job '{JobId}'.", job.Id);
            job.MarkFailed(ex.Message, null, job.RowVersion);
            await _repository.UpdateJobAsync(job, ct);
            throw;
        }
    }
}
