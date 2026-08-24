using System;
using System.Collections.Generic;
using System.Text.Json;
using Workforce.Modules.Compliance.Domain;
using Workforce.Modules.Payroll.Domain;
using Workforce.Modules.Payroll.Domain.CalculationEngine;
using Workforce.SharedKernel.Primitives;
using Xunit;
using Xunit.Abstractions;

namespace Architecture.Tests;

public class Phase4GoldenTests
{
    private readonly ITestOutputHelper _output;

    public Phase4GoldenTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private StatutoryRuleVersion GetVerifiedGosiRule()
    {
        return new StatutoryRuleVersion(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            1,
            new EffectivePeriod(new DateOnly(2024, 1, 1)),
            "{\"employeeRate\": 0.11, \"employerRate\": 0.1875, \"minInsuredMonthly\": 2000.00, \"maxInsuredMonthly\": 12600.00}",
            "EgyptSocialInsuranceStrategy",
            VerificationStatus.Verified
        );
    }

    private StatutoryRuleVersion GetVerifiedTaxRule()
    {
        return new StatutoryRuleVersion(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            1,
            new EffectivePeriod(new DateOnly(2024, 1, 1)),
            "{\"personalExemptionYearly\": 20000.00, \"brackets\": [{\"limit\": 40000.00, \"rate\": 0}, {\"limit\": 55000.00, \"rate\": 0.10}, {\"limit\": 70000.00, \"rate\": 0.15}, {\"limit\": 200000.00, \"rate\": 0.20}, {\"limit\": 400000.00, \"rate\": 0.225}, {\"limit\": null, \"rate\": 0.25}]}",
            "EgyptProgressiveIncomeTaxStrategy",
            VerificationStatus.Verified
        );
    }

    [Theory]
    [InlineData("BaseEarning", 25000.00, 22, 22, 0, 0, 0, "[]")]
    [InlineData("MultipleEarnings", 25000.00, 22, 22, 0, 0, 0, "[{\"Type\":\"Housing\",\"Amount\":5000.00},{\"Type\":\"Transportation\",\"Amount\":2000.00}]")]
    [InlineData("ZeroSalary", 0.00, 22, 22, 0, 0, 0, "[]")]
    [InlineData("FractionalBoundary_4", 10000.44, 22, 22, 0, 0, 0, "[]")]
    [InlineData("FractionalBoundary_5", 10000.45, 22, 22, 0, 0, 0, "[]")]
    [InlineData("FractionalBoundary_6", 10000.46, 22, 22, 0, 0, 0, "[]")]
    [InlineData("NegativeInvalidInput", -5000.00, 22, 22, 0, 0, 0, "[]")]
    [InlineData("AttendanceDerivedInput", 25000.00, 22, 11, 0, 0, 0, "[]")] // 50% attendance
    [InlineData("LeaveDerivedInput", 25000.00, 22, 22, 0, 0, 5, "[]")] // 5 days unpaid leave
    public void ExecuteSyntheticMatrix_Twice_EnsuresStrictDeterminism(
        string caseId,
        decimal baseSalary,
        int scheduledDays,
        int workedDays,
        decimal approvedAbsence,
        decimal approvedLeave,
        decimal unpaidLeave,
        string allowancesJson)
    {
        var engine = new DeterministicPayrollEngine();
        var empId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var activeRules = new List<StatutoryRuleVersion> { GetVerifiedGosiRule(), GetVerifiedTaxRule() };

        // We run exactly the same logical inputs through the engine TWICE on completely DIFFERENT runs
        var run1Id = Guid.NewGuid();
        var run2Id = Guid.NewGuid();

        var snapshot1 = new PayrollInputSnapshot(Guid.NewGuid(), run1Id, empId, baseSalary, allowancesJson, scheduledDays, workedDays * 480, approvedAbsence, approvedLeave, unpaidLeave);
        var snapshot2 = new PayrollInputSnapshot(Guid.NewGuid(), run2Id, empId, baseSalary, allowancesJson, scheduledDays, workedDays * 480, approvedAbsence, approvedLeave, unpaidLeave);

        var pr1 = new PayrollRun(run1Id, new TenantId(Guid.NewGuid()), new LegalEntityId(Guid.NewGuid()), Guid.NewGuid(), "Run1", "EGP");
        pr1.LoadInputs(new[] { snapshot1 }, 1);
        pr1.Calculate(engine, activeRules, 2);

        var pr2 = new PayrollRun(run2Id, new TenantId(Guid.NewGuid()), new LegalEntityId(Guid.NewGuid()), Guid.NewGuid(), "Run2", "EGP");
        pr2.LoadInputs(new[] { snapshot2 }, 1);
        pr2.Calculate(engine, activeRules, 2);

        // Prove Output Determinism
        var res1 = System.Linq.Enumerable.First(pr1.EmployeeResults);
        var res2 = System.Linq.Enumerable.First(pr2.EmployeeResults);

        Assert.Equal(res1.GrossPay, res2.GrossPay);
        Assert.Equal(res1.NetPay, res2.NetPay);
        Assert.Equal(res1.EmployerContributions, res2.EmployerContributions);

        // Prove Trace Determinism (ignoring dynamically generated Guids)
        var traceStr1 = JsonSerializer.Serialize(res1.Traces.Select(t => new { t.StepOrder, t.RuleReference, t.Description, t.FormulaApplied, t.InputValuesJson, t.IntermediateAmount, t.RoundingDelta, t.FinalAmount }));
        var traceStr2 = JsonSerializer.Serialize(res2.Traces.Select(t => new { t.StepOrder, t.RuleReference, t.Description, t.FormulaApplied, t.InputValuesJson, t.IntermediateAmount, t.RoundingDelta, t.FinalAmount }));
        Assert.Equal(traceStr1, traceStr2);

        // Prove Fingerprint Determinism (RunId agnostic)
        Assert.True(!string.IsNullOrEmpty(pr1.ReproducibilityHash));
        Assert.Equal(pr1.ReproducibilityHash, pr2.ReproducibilityHash);

        _output.WriteLine($"CASE {caseId} => Gross: {res1.GrossPay:F2} | Net: {res1.NetPay:F2} | Employer: {res1.EmployerContributions:F2}");
        _output.WriteLine($"Fingerprint: {pr1.ReproducibilityHash}");
    }

    [Fact]
    public void UnverifiedRuleBlock_HaltsExecution_WithNoSilentDefaults()
    {
        var engine = new DeterministicPayrollEngine();
        var snapshot = new PayrollInputSnapshot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 25000, "[]", 22, 22 * 480, 0, 0, 0);
        
        var unverifiedRule = new StatutoryRuleVersion(Guid.NewGuid(), Guid.NewGuid(), 1, new EffectivePeriod(new DateOnly(2024, 1, 1)), "{}", "EgyptSocialInsuranceStrategy", VerificationStatus.Unverified);

        var pr = new PayrollRun(Guid.NewGuid(), new TenantId(Guid.NewGuid()), new LegalEntityId(Guid.NewGuid()), Guid.NewGuid(), "Run", "EGP");
        pr.LoadInputs(new[] { snapshot }, 1);

        var ex = Assert.Throws<InvalidOperationException>(() => pr.Calculate(engine, new[] { unverifiedRule }, 2));
        Assert.True(ex.Message.Contains("BLOCKING EXCEPTION"));
        Assert.True(ex.Message.Contains("UNVERIFIED"));
    }
}
