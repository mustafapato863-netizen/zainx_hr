using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Workforce.Modules.Compliance.Domain;
using Workforce.Modules.Payroll.Domain;
using Workforce.Modules.Payroll.Domain.CalculationEngine;
using Workforce.Modules.Settlement.Domain;
using Workforce.Modules.Settlement.Domain.ExportAdapters;
using Workforce.SharedKernel.Primitives;
using Xunit;

namespace Architecture.Tests;

public class Phase4Tests
{
    private readonly TenantId _tenantId = new(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private readonly LegalEntityId _legalEntityId = new(Guid.Parse("33333333-3333-3333-3333-333333333333"));

    [Fact]
    public void DeterministicPayrollEngine_CalculatesExpectedGrossNetAndTraces()
    {
        var engine = new DeterministicPayrollEngine();
        var runId = Guid.NewGuid();
        var empId = Guid.NewGuid();

        var snapshot = new PayrollInputSnapshot(
            Guid.NewGuid(), runId, empId,
            baseSalaryMonthly: 30000.00m,
            allowancesJson: "[{\"code\":\"HOUSING\",\"nameEn\":\"Housing Allowance\",\"nameAr\":\"بدل سكن\",\"amount\":5000.00}]",
            scheduledDays: 22,
            verifiedWorkedMinutes: 22 * 480,
            approvedAbsenceDays: 0,
            approvedLeaveDays: 0,
            unpaidLeaveDays: 0
        );

        var taxRuleVersion = new StatutoryRuleVersion(
            Guid.NewGuid(), Guid.NewGuid(), 1,
            new EffectivePeriod(new DateOnly(2024, 1, 1)),
            "{\"personalExemptionYearly\": 20000.00}",
            "EgyptProgressiveIncomeTaxStrategy"
        );

        var gosiRuleVersion = new StatutoryRuleVersion(
            Guid.NewGuid(), Guid.NewGuid(), 1,
            new EffectivePeriod(new DateOnly(2024, 1, 1)),
            "{\"employeeRate\": 0.11, \"employerRate\": 0.1875, \"minInsuredMonthly\": 2000.00, \"maxInsuredMonthly\": 12600.00}",
            "EgyptSocialInsuranceStrategy"
        );

        var rules = new List<StatutoryRuleVersion> { taxRuleVersion, gosiRuleVersion };
        var result = engine.Calculate(snapshot, rules, out var exceptions);

        Assert.True(exceptions.Count == 0);
        Assert.Equal(35000.00m, result.GrossPay); // 30,000 + 5,000
        Assert.Equal(35000.00m, result.TotalEarnings);

        // Egypt GOSI: capped at 12,600 * 0.11 = 1,386.00
        // Egypt Employer GOSI: 12,600 * 0.1875 = 2,362.50
        Assert.Equal(2362.50m, result.EmployerContributions);

        Assert.True(result.Lines.Count > 0);
        Assert.True(result.Traces.Count > 0);
        Assert.True(result.NetPay > 0);
        Assert.True(result.NetPay < result.GrossPay);
    }

    [Fact]
    public void PayrollRun_FinalizationIsHardBoundary_RejectsSubsequentMutations()
    {
        var run = new PayrollRun(Guid.NewGuid(), _tenantId, _legalEntityId, Guid.NewGuid(), "RUN-TEST-001");
        var empId = Guid.NewGuid();

        var snapshots = new List<PayrollInputSnapshot>
        {
            new(Guid.NewGuid(), run.Id, empId, 20000.00m, "[]", 22, 22 * 480, 0, 0, 0)
        };

        run.LoadInputs(snapshots, 1);
        Assert.Equal(PayrollRunStatus.InputsLoaded, run.Status);

        var engine = new DeterministicPayrollEngine();
        run.Calculate(engine, new List<StatutoryRuleVersion>(), 2);
        Assert.Equal(PayrollRunStatus.Calculated, run.Status);
        Assert.True(!string.IsNullOrEmpty(run.ReproducibilityHash));

        run.SubmitForReview(3);
        Assert.Equal(PayrollRunStatus.UnderReview, run.Status);

        var approvalId = Guid.NewGuid();
        run.Approve(approvalId, 4);
        Assert.Equal(PayrollRunStatus.Approved, run.Status);

        var finalizerId = Guid.NewGuid();
        run.FinalizeRun(finalizerId, 5);
        Assert.Equal(PayrollRunStatus.Finalized, run.Status);
        Assert.NotNull(run.FinalizedAtUtc);
        Assert.Equal(finalizerId, run.FinalizedByUserId);

        // Attempting to mutate a finalized run must fail
        Assert.Throws<InvalidOperationException>(() => run.LoadInputs(snapshots, 6));
        Assert.Throws<InvalidOperationException>(() => run.Calculate(engine, new List<StatutoryRuleVersion>(), 6));
    }

    [Fact]
    public void SettlementBatch_ReconciliationInvariant_EnforcesExactNetPaySum()
    {
        var runId = Guid.NewGuid();
        var batch = new SettlementBatch(
            Guid.NewGuid(), _tenantId, _legalEntityId, runId, "BATCH-2026-08",
            totalAmount: 50000.00m,
            paymentDate: new DateOnly(2026, 8, 31)
        );

        // Add instructions that sum to 49,000.00 (mismatched!)
        batch.AddInstruction(new PaymentInstruction(
            Guid.NewGuid(), batch.Id, Guid.NewGuid(), "Emp 1", "MISR", "EG1111", 25000.00m
        ));
        batch.AddInstruction(new PaymentInstruction(
            Guid.NewGuid(), batch.Id, Guid.NewGuid(), "Emp 2", "MISR", "EG2222", 24000.00m
        ));

        // Approving mismatched batch must throw
        Assert.Throws<InvalidOperationException>(() => batch.Approve(1));

        // Add missing 1,000.00
        batch.AddInstruction(new PaymentInstruction(
            Guid.NewGuid(), batch.Id, Guid.NewGuid(), "Emp 3", "MISR", "EG3333", 1000.00m
        ));

        // Now total is 50,000.00 -> should approve smoothly
        batch.Approve(1);
        Assert.Equal(SettlementStatus.Approved, batch.Status);
    }

    [Fact]
    public async Task NeutralCsvPaymentExportAdapter_GeneratesValidFileAndSha256()
    {
        var adapter = new NeutralCsvPaymentExportAdapter();
        var batch = new SettlementBatch(
            Guid.NewGuid(), _tenantId, _legalEntityId, Guid.NewGuid(), "BATCH-EXPORT-01",
            totalAmount: 15000.00m,
            paymentDate: new DateOnly(2026, 8, 31)
        );

        batch.AddInstruction(new PaymentInstruction(
            Guid.NewGuid(), batch.Id, Guid.NewGuid(), "Ahmed Hassan", "MISR", "EG1234567890", 15000.00m
        ));

        var result = await adapter.GenerateExportAsync(batch);

        Assert.NotNull(result);
        Assert.Equal("text/csv; charset=utf-8", result.ContentType);
        Assert.True(result.FileName.Contains("BATCH-EXPORT-01"));
        Assert.True(!string.IsNullOrEmpty(result.FileSha256));
        Assert.True(result.FileBytes.Length > 0);
    }
}
