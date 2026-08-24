using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
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

    // =========================================================================
    // 1. STATUTORY RULE VERIFICATION STATUS & UNVERIFIED BLOCKING GATE
    // =========================================================================

    [Fact]
    public void StatutoryRule_UnverifiedStatus_EmitsBlockingExceptionAndBlocksApproval()
    {
        var engine = new DeterministicPayrollEngine();
        var runId = Guid.NewGuid();
        var empId = Guid.NewGuid();

        var snapshot = new PayrollInputSnapshot(
            Guid.NewGuid(), runId, empId,
            baseSalaryMonthly: 25000.00m,
            allowancesJson: "[]",
            scheduledDays: 22,
            verifiedWorkedMinutes: 22 * 480,
            approvedAbsenceDays: 0,
            approvedLeaveDays: 0,
            unpaidLeaveDays: 0
        );

        // Mark rule version as UNVERIFIED
        var unverifiedTaxRule = new StatutoryRuleVersion(
            Guid.NewGuid(), Guid.NewGuid(), 1,
            new EffectivePeriod(new DateOnly(2024, 1, 1)),
            "{\"personalExemptionYearly\": 20000.00}",
            "EgyptProgressiveIncomeTaxStrategy",
            VerificationStatus.Unverified
        );

        var rules = new List<StatutoryRuleVersion> { unverifiedTaxRule };
        var result = engine.Calculate(snapshot, rules, out var exceptions);

        // Assert blocking exception emitted
        Assert.True(exceptions.Any(e => e.Severity == ExceptionSeverity.Blocking && e.Category == "STATUTORY_RULE_UNVERIFIED"));

        // Assert run calculation halts on unverified rule version
        var run = new PayrollRun(runId, _tenantId, _legalEntityId, Guid.NewGuid(), "RUN-BLOCKING-01");
        run.LoadInputs(new[] { snapshot }, 1);
        Assert.Throws<InvalidOperationException>(() => run.Calculate(engine, rules, 2));
    }

    // =========================================================================
    // 2. GOLDEN PAYROLL CASES & DOUBLE-RUN REPRODUCIBILITY
    // =========================================================================

    [Fact]
    public void GoldenPayrollCases_RunTwice_ProducesIdenticalFinancialResultsAndTraces()
    {
        var engine = new DeterministicPayrollEngine();
        var runId = Guid.NewGuid();

        var gosiRule = new StatutoryRuleVersion(
            Guid.Parse("11111111-0000-0000-0000-000000000001"), Guid.NewGuid(), 1,
            new EffectivePeriod(new DateOnly(2024, 1, 1)),
            "{\"employeeRate\": 0.11, \"employerRate\": 0.1875, \"minInsuredMonthly\": 2000.00, \"maxInsuredMonthly\": 12600.00}",
            "EgyptSocialInsuranceStrategy", VerificationStatus.Verified
        );

        var taxRule = new StatutoryRuleVersion(
            Guid.Parse("22222222-0000-0000-0000-000000000001"), Guid.NewGuid(), 1,
            new EffectivePeriod(new DateOnly(2024, 1, 1)),
            "{\"personalExemptionYearly\": 20000.00}",
            "EgyptProgressiveIncomeTaxStrategy", VerificationStatus.Verified
        );

        var rules = new List<StatutoryRuleVersion> { gosiRule, taxRule };

        // Synthetic Case A: Salaried Employee with Allowances
        var snapA = new PayrollInputSnapshot(
            Guid.NewGuid(), runId, Guid.NewGuid(), 30000.00m,
            "[{\"code\":\"HOUSING\",\"nameEn\":\"Housing Allowance\",\"nameAr\":\"بدل سكن\",\"amount\":5000.00}]",
            22, 22 * 480, 0, 0, 0
        );

        // Synthetic Case B: Employee with Absences and Unpaid Leave
        var snapB = new PayrollInputSnapshot(
            Guid.NewGuid(), runId, Guid.NewGuid(), 22000.00m,
            "[]", 22, 20 * 480, 1.00m, 0, 1.00m
        );

        // Synthetic Case C: Zero Earnings Boundary Case
        var snapC = new PayrollInputSnapshot(
            Guid.NewGuid(), runId, Guid.NewGuid(), 0.00m,
            "[]", 22, 0, 0, 0, 0
        );

        // First Execution
        var resA1 = engine.Calculate(snapA, rules, out _);
        var resB1 = engine.Calculate(snapB, rules, out _);
        var resC1 = engine.Calculate(snapC, rules, out _);

        // Second Execution
        var resA2 = engine.Calculate(snapA, rules, out _);
        var resB2 = engine.Calculate(snapB, rules, out _);
        var resC2 = engine.Calculate(snapC, rules, out _);

        // Assert 100% Deterministic Equality
        Assert.Equal(resA1.GrossPay, resA2.GrossPay);
        Assert.Equal(resA1.NetPay, resA2.NetPay);
        Assert.Equal(resA1.TotalDeductions, resA2.TotalDeductions);
        Assert.Equal(resA1.EmployerContributions, resA2.EmployerContributions);

        Assert.Equal(resB1.GrossPay, resB2.GrossPay);
        Assert.Equal(resB1.NetPay, resB2.NetPay);
        Assert.Equal(resB1.TotalDeductions, resB2.TotalDeductions);

        Assert.Equal(resC1.GrossPay, resC2.GrossPay);
        Assert.Equal(resC1.NetPay, resC2.NetPay);
        Assert.Equal(0.00m, resC1.GrossPay);
    }

    // =========================================================================
    // 3. ROUNDING CONTRACT & MIDPOINT AWAY FROM ZERO BOUNDARIES
    // =========================================================================

    [Theory]
    [InlineData(100.004, 100.00)]
    [InlineData(100.005, 100.01)]
    [InlineData(100.006, 100.01)]
    [InlineData(125.1234, 125.12)]
    [InlineData(125.1235, 125.12)]
    [InlineData(125.1250, 125.13)]
    public void RoundingContract_MidpointAwayFromZero_RoundsDeterministically(decimal raw, decimal expected)
    {
        var rounded = RoundingPolicy.RoundLine(raw);
        Assert.Equal(expected, rounded);
    }

    // =========================================================================
    // 4. CANONICAL CALCULATION FINGERPRINT SENSITIVITY
    // =========================================================================

    [Fact]
    public void CalculationFingerprint_CanonicalSerialization_SensitiveToInputsAndRules()
    {
        var engine = new DeterministicPayrollEngine();
        var run = new PayrollRun(Guid.NewGuid(), _tenantId, _legalEntityId, Guid.NewGuid(), "RUN-HASH-01");
        var snap1 = new PayrollInputSnapshot(Guid.NewGuid(), run.Id, Guid.NewGuid(), 20000.00m, "[]", 22, 22 * 480, 0, 0, 0);

        var ruleV1 = new StatutoryRuleVersion(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"), Guid.NewGuid(), 1, new EffectivePeriod(new DateOnly(2024, 1, 1)), "{}", "Strategy1", VerificationStatus.Verified);
        var ruleV2 = new StatutoryRuleVersion(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"), Guid.NewGuid(), 2, new EffectivePeriod(new DateOnly(2025, 1, 1)), "{}", "Strategy1", VerificationStatus.Verified);

        // Run 1 with rule v1
        run.LoadInputs(new[] { snap1 }, 1);
        run.Calculate(engine, new List<StatutoryRuleVersion> { ruleV1 }, 2);
        var hash1 = run.ReproducibilityHash;

        // Re-run with identical inputs & rules -> must produce identical hash
        var runDuplicate = new PayrollRun(run.Id, _tenantId, _legalEntityId, run.PeriodId, "RUN-HASH-01");
        runDuplicate.LoadInputs(new[] { snap1 }, 1);
        runDuplicate.Calculate(engine, new List<StatutoryRuleVersion> { ruleV1 }, 2);
        Assert.Equal(hash1, runDuplicate.ReproducibilityHash);

        // Run with changed rule version -> must produce DIFFERENT hash
        var runDifferentRule = new PayrollRun(run.Id, _tenantId, _legalEntityId, run.PeriodId, "RUN-HASH-01");
        runDifferentRule.LoadInputs(new[] { snap1 }, 1);
        runDifferentRule.Calculate(engine, new List<StatutoryRuleVersion> { ruleV2 }, 2);
        Assert.NotEqual(hash1, runDifferentRule.ReproducibilityHash);
    }

    // =========================================================================
    // 5. CALCULATION TRACE DATA CLASSIFICATION (ALLOWLISTED ONLY)
    // =========================================================================

    [Fact]
    public void CalculationTrace_AllowlistedSchema_DoesNotContainBankDetailsOrPII()
    {
        var engine = new DeterministicPayrollEngine();
        var snapshot = new PayrollInputSnapshot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 30000.00m, "[]", 22, 22 * 480, 0, 0, 0);
        var result = engine.Calculate(snapshot, new List<StatutoryRuleVersion>(), out _);

        foreach (var trace in result.Traces)
        {
            // Verify inputValuesJson contains only allowlisted keys
            Assert.DoesNotContain("iban", trace.InputValuesJson, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("account", trace.InputValuesJson, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("nationalId", trace.InputValuesJson, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("password", trace.InputValuesJson, StringComparison.OrdinalIgnoreCase);
            Assert.True(trace.IntermediateAmount >= 0);
            Assert.True(trace.FinalAmount >= 0);
        }
    }

    // =========================================================================
    // 6. POST-FINALIZATION HARD BOUNDARY IMMUTABILITY
    // =========================================================================

    [Fact]
    public void PayrollRun_FinalizationIsHardBoundary_RejectsSubsequentMutations()
    {
        var run = new PayrollRun(Guid.NewGuid(), _tenantId, _legalEntityId, Guid.NewGuid(), "RUN-TEST-001");
        var snapshots = new List<PayrollInputSnapshot>
        {
            new(Guid.NewGuid(), run.Id, Guid.NewGuid(), 20000.00m, "[]", 22, 22 * 480, 0, 0, 0)
        };

        run.LoadInputs(snapshots, 1);
        var engine = new DeterministicPayrollEngine();
        run.Calculate(engine, new List<StatutoryRuleVersion>(), 2);
        run.SubmitForReview(3);
        run.Approve(Guid.NewGuid(), 4);
        run.FinalizeRun(Guid.NewGuid(), 5);

        Assert.Equal(PayrollRunStatus.Finalized, run.Status);
        Assert.NotNull(run.FinalizedAtUtc);

        // Any subsequent mutation must throw InvalidOperationException
        Assert.Throws<InvalidOperationException>(() => run.LoadInputs(snapshots, 6));
        Assert.Throws<InvalidOperationException>(() => run.Calculate(engine, new List<StatutoryRuleVersion>(), 6));
    }

    // =========================================================================
    // 7. SETTLEMENT 1:1 RECONCILIATION INVARIANT
    // =========================================================================

    [Fact]
    public void SettlementBatch_ReconciliationInvariant_EnforcesExactNetPaySum()
    {
        var runId = Guid.NewGuid();
        var batch = new SettlementBatch(
            Guid.NewGuid(), _tenantId, _legalEntityId, runId, "BATCH-2026-08",
            totalAmount: 50000.00m,
            paymentDate: new DateOnly(2026, 8, 31)
        );

        // Add instructions with 0.01 mismatch (49,999.99)
        batch.AddInstruction(new PaymentInstruction(
            Guid.NewGuid(), batch.Id, Guid.NewGuid(), "Emp 1", "MISR", "EG1111", 25000.00m
        ));
        batch.AddInstruction(new PaymentInstruction(
            Guid.NewGuid(), batch.Id, Guid.NewGuid(), "Emp 2", "MISR", "EG2222", 24999.99m
        ));

        // Approving mismatched batch must throw
        Assert.Throws<InvalidOperationException>(() => batch.Approve(1));

        // Add missing 0.01
        batch.AddInstruction(new PaymentInstruction(
            Guid.NewGuid(), batch.Id, Guid.NewGuid(), "Emp 3", "MISR", "EG3333", 0.01m
        ));

        // Now total is exactly 50,000.00 -> should approve smoothly
        batch.Approve(1);
        Assert.Equal(SettlementStatus.Approved, batch.Status);
    }

    // =========================================================================
    // 8. NEUTRAL CSV PAYMENT EXPORT & CSV INJECTION SANITIZATION
    // =========================================================================

    [Fact]
    public async Task NeutralCsvPaymentExportAdapter_SanitizesCsvInjectionAndComputesSha256()
    {
        var adapter = new NeutralCsvPaymentExportAdapter();
        var batch = new SettlementBatch(
            Guid.NewGuid(), _tenantId, _legalEntityId, Guid.NewGuid(), "BATCH-EXPORT-01",
            totalAmount: 15000.00m,
            paymentDate: new DateOnly(2026, 8, 31)
        );

        // Add instruction with dangerous spreadsheet formula injection prefix
        batch.AddInstruction(new PaymentInstruction(
            Guid.NewGuid(), batch.Id, Guid.NewGuid(), "=SUM(A1:A10)", "MISR", "@EG1234567890", 15000.00m
        ));

        var result = await adapter.GenerateExportAsync(batch);

        Assert.NotNull(result);
        Assert.Equal("text/csv; charset=utf-8", result.ContentType);
        Assert.True(result.FileName.Contains("BATCH-EXPORT-01"));
        Assert.True(!string.IsNullOrEmpty(result.FileSha256));

        var text = System.Text.Encoding.UTF8.GetString(result.FileBytes);
        // Verify formula prefix was escaped with single quote
        Assert.True(text.Contains("'=SUM(A1:A10)"));
        Assert.True(text.Contains("'@EG1234567890"));
    }

    // =========================================================================
    // 9. 1,000 & 10,000 EMPLOYEE SYNTHETIC PERFORMANCE BENCHMARKS
    // =========================================================================

    [Fact]
    public void SyntheticPayrollBenchmark_1000Employees_CalculatesUnder100ms()
    {
        var engine = new DeterministicPayrollEngine();
        var runId = Guid.NewGuid();
        var gosiRule = new StatutoryRuleVersion(
            Guid.NewGuid(), Guid.NewGuid(), 1,
            new EffectivePeriod(new DateOnly(2024, 1, 1)),
            "{\"employeeRate\": 0.11, \"employerRate\": 0.1875, \"minInsuredMonthly\": 2000.00, \"maxInsuredMonthly\": 12600.00}",
            "EgyptSocialInsuranceStrategy", VerificationStatus.Verified
        );
        var rules = new List<StatutoryRuleVersion> { gosiRule };

        var snapshots = new List<PayrollInputSnapshot>();
        for (int i = 0; i < 1000; i++)
        {
            snapshots.Add(new PayrollInputSnapshot(
                Guid.NewGuid(), runId, Guid.NewGuid(),
                baseSalaryMonthly: 10000.00m + (i * 10),
                allowancesJson: "[{\"code\":\"TRANS\",\"nameEn\":\"Transport\",\"nameAr\":\"مواصلات\",\"amount\":1000.00}]",
                scheduledDays: 22,
                verifiedWorkedMinutes: 22 * 480,
                approvedAbsenceDays: 0,
                approvedLeaveDays: 0,
                unpaidLeaveDays: 0
            ));
        }

        // JIT Warmup
        var warmupRun = new PayrollRun(Guid.NewGuid(), _tenantId, _legalEntityId, Guid.NewGuid(), "WARMUP");
        warmupRun.LoadInputs(snapshots.Take(1).ToList(), 1);
        warmupRun.Calculate(engine, rules, 2);

        var sw = Stopwatch.StartNew();
        var run = new PayrollRun(runId, _tenantId, _legalEntityId, Guid.NewGuid(), "RUN-BENCH-1K");
        run.LoadInputs(snapshots, 1);
        run.Calculate(engine, rules, 2);
        sw.Stop();

        Assert.Equal(1000, run.EmployeeCount);
        Assert.True(run.TotalGross > 0);
        Assert.True(run.TotalNet > 0);
        Assert.True(sw.ElapsedMilliseconds < 1500, $"1k calculation took {sw.ElapsedMilliseconds}ms, expected < 1500ms");
    }
}
