using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Workforce.Modules.Compliance.Domain;
using Workforce.Modules.Compliance.Infrastructure;
using Workforce.Modules.Payroll.Domain;
using Workforce.Modules.Payroll.Domain.CalculationEngine;
using Workforce.SharedKernel.Primitives;
using Xunit;

namespace Architecture.Tests;

public class Phase4StatutoryTemporalTests
{
    private static readonly TenantId Tenant = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly LegalEntityId LegalEntity = new(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    // ------------------------------------------------------------------------
    // Rule Definitions & Seeds
    // ------------------------------------------------------------------------
    private static readonly Guid GosiRuleId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid TaxRuleId = Guid.Parse("10000000-0000-0000-0000-000000000001");

    // Social Insurance Versions:
    // 2024: 2,000 / 12,600 EGP
    private static readonly StatutoryRuleVersion Gosi2024 = new(
        Guid.Parse("20000000-0000-0000-0000-000000000001"),
        GosiRuleId, 1,
        new EffectivePeriod(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)),
        "{\"sourceReference\":\"Law No. 148 of 2019 & NOSI Decree 2024\",\"employeeRate\":0.11,\"employerRate\":0.1875,\"minInsuredMonthly\":2000.00,\"maxInsuredMonthly\":12600.00}",
        "EgyptSocialInsuranceStrategy",
        VerificationStatus.Verified
    );

    // 2025: 2,300 / 14,500 EGP
    private static readonly StatutoryRuleVersion Gosi2025 = new(
        Guid.Parse("20000000-0000-0000-0000-000000000002"),
        GosiRuleId, 2,
        new EffectivePeriod(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31)),
        "{\"sourceReference\":\"Law No. 148 of 2019 & NOSI Decree 2025\",\"employeeRate\":0.11,\"employerRate\":0.1875,\"minInsuredMonthly\":2300.00,\"maxInsuredMonthly\":14500.00}",
        "EgyptSocialInsuranceStrategy",
        VerificationStatus.Verified
    );

    // 2026: 2,700 / 16,700 EGP
    private static readonly StatutoryRuleVersion Gosi2026 = new(
        Guid.Parse("20000000-0000-0000-0000-000000000003"),
        GosiRuleId, 3,
        new EffectivePeriod(new DateOnly(2026, 1, 1), null),
        "{\"sourceReference\":\"Law No. 148 of 2019 & NOSI Decree 2026\",\"employeeRate\":0.11,\"employerRate\":0.1875,\"minInsuredMonthly\":2700.00,\"maxInsuredMonthly\":16700.00}",
        "EgyptSocialInsuranceStrategy",
        VerificationStatus.Verified
    );

    // Income Tax Complete Article 8 Matrices:
    // v1: Law 30 of 2023 (Effective 2023-07-01 to 2024-02-29) - Exemption 15,000 EGP/yr, 6 Multi-Tier Bands
    private static readonly string Law30Json = """
    {
      "sourceReference": "Law No. 91 of 2005 as amended by Law No. 30 of 2023",
      "officialGazette": "Official Gazette Vol 24 bis, 15 June 2023",
      "personalExemptionYearly": 15000.00,
      "statutoryRounding": "RoundDownToNearest10",
      "incomeBands": [
        {
          "bandIndex": 1,
          "name": "Band 1: Up to 600,000 EGP",
          "minAnnualIncome": 0,
          "maxAnnualIncome": 600000.00,
          "tranches": [
            { "trancheIndex": 1, "from": 0, "to": 30000.00, "rate": 0.00 },
            { "trancheIndex": 2, "from": 30000.00, "to": 45000.00, "rate": 0.10 },
            { "trancheIndex": 3, "from": 45000.00, "to": 60000.00, "rate": 0.15 },
            { "trancheIndex": 4, "from": 60000.00, "to": 200000.00, "rate": 0.20 },
            { "trancheIndex": 5, "from": 200000.00, "to": 400000.00, "rate": 0.225 },
            { "trancheIndex": 6, "from": 400000.00, "to": null, "rate": 0.25 }
          ]
        },
        {
          "bandIndex": 2,
          "name": "Band 2: Over 600,000 to 700,000 EGP",
          "minAnnualIncome": 600000.00,
          "maxAnnualIncome": 700000.00,
          "tranches": [
            { "trancheIndex": 2, "from": 0, "to": 45000.00, "rate": 0.10 },
            { "trancheIndex": 3, "from": 45000.00, "to": 60000.00, "rate": 0.15 },
            { "trancheIndex": 4, "from": 60000.00, "to": 200000.00, "rate": 0.20 },
            { "trancheIndex": 5, "from": 200000.00, "to": 400000.00, "rate": 0.225 },
            { "trancheIndex": 6, "from": 400000.00, "to": null, "rate": 0.25 }
          ]
        },
        {
          "bandIndex": 3,
          "name": "Band 3: Over 700,000 to 800,000 EGP",
          "minAnnualIncome": 700000.00,
          "maxAnnualIncome": 800000.00,
          "tranches": [
            { "trancheIndex": 3, "from": 0, "to": 60000.00, "rate": 0.15 },
            { "trancheIndex": 4, "from": 60000.00, "to": 200000.00, "rate": 0.20 },
            { "trancheIndex": 5, "from": 200000.00, "to": 400000.00, "rate": 0.225 },
            { "trancheIndex": 6, "from": 400000.00, "to": null, "rate": 0.25 }
          ]
        },
        {
          "bandIndex": 4,
          "name": "Band 4: Over 800,000 to 900,000 EGP",
          "minAnnualIncome": 800000.00,
          "maxAnnualIncome": 900000.00,
          "tranches": [
            { "trancheIndex": 4, "from": 0, "to": 200000.00, "rate": 0.20 },
            { "trancheIndex": 5, "from": 200000.00, "to": 400000.00, "rate": 0.225 },
            { "trancheIndex": 6, "from": 400000.00, "to": null, "rate": 0.25 }
          ]
        },
        {
          "bandIndex": 5,
          "name": "Band 5: Over 900,000 to 1,000,000 EGP",
          "minAnnualIncome": 900000.00,
          "maxAnnualIncome": 1000000.00,
          "tranches": [
            { "trancheIndex": 5, "from": 0, "to": 400000.00, "rate": 0.225 },
            { "trancheIndex": 6, "from": 400000.00, "to": null, "rate": 0.25 }
          ]
        },
        {
          "bandIndex": 6,
          "name": "Band 6: Over 1,000,000 EGP",
          "minAnnualIncome": 1000000.00,
          "maxAnnualIncome": null,
          "tranches": [
            { "trancheIndex": 6, "from": 0, "to": null, "rate": 0.25 }
          ]
        }
      ]
    }
    """;

    // v2: Law 7 of 2024 (Effective 2024-03-01 onwards) - Exemption 20,000 EGP/yr, Complete Article 8 Matrix (7 Tranches up to 27.5%)
    private static readonly string Law7Json = """
    {
      "sourceReference": "Law No. 91 of 2005 as amended by Law No. 7 of 2024",
      "officialGazette": "Official Gazette Issue 7 bis (a), 21 February 2024",
      "personalExemptionYearly": 20000.00,
      "statutoryRounding": "RoundDownToNearest10",
      "incomeBands": [
        {
          "bandIndex": 1,
          "name": "Band 1: Up to 600,000 EGP",
          "minAnnualIncome": 0,
          "maxAnnualIncome": 600000.00,
          "tranches": [
            { "trancheIndex": 1, "from": 0, "to": 40000.00, "rate": 0.00 },
            { "trancheIndex": 2, "from": 40000.00, "to": 55000.00, "rate": 0.10 },
            { "trancheIndex": 3, "from": 55000.00, "to": 70000.00, "rate": 0.15 },
            { "trancheIndex": 4, "from": 70000.00, "to": 200000.00, "rate": 0.20 },
            { "trancheIndex": 5, "from": 200000.00, "to": 400000.00, "rate": 0.225 },
            { "trancheIndex": 6, "from": 400000.00, "to": 1200000.00, "rate": 0.25 },
            { "trancheIndex": 7, "from": 1200000.00, "to": null, "rate": 0.275 }
          ]
        },
        {
          "bandIndex": 2,
          "name": "Band 2: Over 600,000 to 700,000 EGP",
          "minAnnualIncome": 600000.00,
          "maxAnnualIncome": 700000.00,
          "tranches": [
            { "trancheIndex": 2, "from": 0, "to": 55000.00, "rate": 0.10 },
            { "trancheIndex": 3, "from": 55000.00, "to": 70000.00, "rate": 0.15 },
            { "trancheIndex": 4, "from": 70000.00, "to": 200000.00, "rate": 0.20 },
            { "trancheIndex": 5, "from": 200000.00, "to": 400000.00, "rate": 0.225 },
            { "trancheIndex": 6, "from": 400000.00, "to": 1200000.00, "rate": 0.25 },
            { "trancheIndex": 7, "from": 1200000.00, "to": null, "rate": 0.275 }
          ]
        },
        {
          "bandIndex": 3,
          "name": "Band 3: Over 700,000 to 800,000 EGP",
          "minAnnualIncome": 700000.00,
          "maxAnnualIncome": 800000.00,
          "tranches": [
            { "trancheIndex": 3, "from": 0, "to": 70000.00, "rate": 0.15 },
            { "trancheIndex": 4, "from": 70000.00, "to": 200000.00, "rate": 0.20 },
            { "trancheIndex": 5, "from": 200000.00, "to": 400000.00, "rate": 0.225 },
            { "trancheIndex": 6, "from": 400000.00, "to": 1200000.00, "rate": 0.25 },
            { "trancheIndex": 7, "from": 1200000.00, "to": null, "rate": 0.275 }
          ]
        },
        {
          "bandIndex": 4,
          "name": "Band 4: Over 800,000 to 900,000 EGP",
          "minAnnualIncome": 800000.00,
          "maxAnnualIncome": 900000.00,
          "tranches": [
            { "trancheIndex": 4, "from": 0, "to": 200000.00, "rate": 0.20 },
            { "trancheIndex": 5, "from": 200000.00, "to": 400000.00, "rate": 0.225 },
            { "trancheIndex": 6, "from": 400000.00, "to": 1200000.00, "rate": 0.25 },
            { "trancheIndex": 7, "from": 1200000.00, "to": null, "rate": 0.275 }
          ]
        },
        {
          "bandIndex": 5,
          "name": "Band 5: Over 900,000 to 1,200,000 EGP",
          "minAnnualIncome": 900000.00,
          "maxAnnualIncome": 1200000.00,
          "tranches": [
            { "trancheIndex": 5, "from": 0, "to": 400000.00, "rate": 0.225 },
            { "trancheIndex": 6, "from": 400000.00, "to": 1200000.00, "rate": 0.25 },
            { "trancheIndex": 7, "from": 1200000.00, "to": null, "rate": 0.275 }
          ]
        },
        {
          "bandIndex": 6,
          "name": "Band 6: Over 1,200,000 EGP",
          "minAnnualIncome": 1200000.00,
          "maxAnnualIncome": null,
          "tranches": [
            { "trancheIndex": 6, "from": 0, "to": 1200000.00, "rate": 0.25 },
            { "trancheIndex": 7, "from": 1200000.00, "to": null, "rate": 0.275 }
          ]
        }
      ]
    }
    """;

    private static readonly StatutoryRuleVersion TaxPreLaw7 = new(
        Guid.Parse("30000000-0000-0000-0000-000000000001"),
        TaxRuleId, 1,
        new EffectivePeriod(new DateOnly(2023, 7, 1), new DateOnly(2024, 2, 29)),
        Law30Json,
        "EgyptProgressiveIncomeTaxStrategy",
        VerificationStatus.Verified
    );

    private static readonly StatutoryRuleVersion TaxPostLaw7 = new(
        Guid.Parse("30000000-0000-0000-0000-000000000002"),
        TaxRuleId, 2,
        new EffectivePeriod(new DateOnly(2024, 3, 1), null),
        Law7Json,
        "EgyptProgressiveIncomeTaxStrategy",
        VerificationStatus.Verified
    );

    // ========================================================================
    // 1. Article 8 High-Income Bands Multi-Tier Tranche Calculations (Law 7/2024)
    // ========================================================================

    [Theory]
    // Band 1: <= 600k
    [InlineData(40000.00, "Band 1", 7019.79)]
    // Band 2: > 600k to 700k
    [InlineData(58000.00, "Band 2", 11853.13)]
    // Band 3: > 700k to 800k
    [InlineData(66000.00, "Band 3", 14082.29)]
    // Band 4: > 800k to 900k
    [InlineData(75000.00, "Band 4", 16623.96)]
    // Band 5: > 900k to 1.2M
    [InlineData(90000.00, "Band 5", 20790.63)]
    // Band 6: > 1.2M
    [InlineData(150000.00, "Band 6", 37786.35)]
    public void Law7_Article8_HighIncomeBands_MultiTierTrancheCalculation(
        decimal monthlyGross,
        string expectedBandPrefix,
        decimal expectedMonthlyTax)
    {
        var engine = new DeterministicPayrollEngine();
        var snapshot = new PayrollInputSnapshot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), monthlyGross, "[]", 22, 22 * 480, 0, 0, 0);

        var result = engine.Calculate(snapshot, new[] { Gosi2026, TaxPostLaw7 }, out var exceptions);

        Assert.True(exceptions.Count == 0);
        var taxLine = result.Lines.Single(l => l.ComponentCode == "EG_INCOME_TAX");
        var taxTrace = result.Traces.Single(t => t.RuleReference == "EG_INCOME_TAX");

        Assert.Equal(expectedMonthlyTax, taxLine.Amount);
        Assert.True(taxTrace.Description.Contains(expectedBandPrefix));
    }

    // ========================================================================
    // 2. Article 8 Statutory Normalization (Rounding Down to Nearest 10 EGP)
    // ========================================================================

    [Theory]
    [InlineData(45678.95, 45670.00)]
    [InlineData(599999.99, 599990.00)]
    [InlineData(600008.00, 600000.00)] // Rounds down to 600,000 -> remains in Band 1!
    [InlineData(600012.00, 600010.00)] // Rounds down to 600,010 -> falls into Band 2!
    [InlineData(700009.50, 700000.00)] // Rounds down to 700,000 -> remains in Band 2!
    [InlineData(1200009.99, 1200000.00)] // Rounds down to 1,200,000 -> remains in Band 5!
    public void StatutoryTaxBaseNormalization_RoundsDownToNearestTenEGP(decimal rawAnnual, decimal expectedNormalized)
    {
        var normalized = StatutoryTaxBaseNormalization.NormalizeAnnualTaxBase(rawAnnual);
        Assert.Equal(expectedNormalized, normalized);
    }

    // ========================================================================
    // 3. SYNTHETIC GOLDEN TEMPORAL CASES (A THROUGH G)
    // ========================================================================

    [Fact]
    public void GoldenCaseA_January2024_OrdinarySalary_PaidNormally_UsesLaw30()
    {
        // Case A: January 2024 ordinary salary paid normally
        // Entitlement: 2024-01-01..2024-01-31, Paid: 2024-01-31
        // Temporal Treatment: CurrentPeriodSalary
        // Selected Versions: Law 30/2023 (v1) + 2024 GOSI (v1)
        var engine = new DeterministicPayrollEngine();
        var snapshot = new PayrollInputSnapshot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 30000.00m, "[]", 22, 22 * 480, 0, 0, 0);

        var result = engine.Calculate(snapshot, new[] { Gosi2024, TaxPreLaw7 }, out var exceptions);

        Assert.True(exceptions.Count == 0);
        var taxLine = result.Lines.Single(l => l.ComponentCode == "EG_INCOME_TAX");
        var gosiLine = result.Lines.Single(l => l.ComponentCode == "EG_GOSI_EMPLOYEE");

        Assert.Equal(5052.58m, taxLine.Amount);
        Assert.Equal(1386.00m, gosiLine.Amount); // 11% of 12,600 max
    }

    [Fact]
    public void GoldenCaseB_March2024_OrdinarySalary_AfterLaw7Boundary_UsesLaw7()
    {
        // Case B: March 2024 ordinary salary after Law-7 boundary
        // Entitlement: 2024-03-01..2024-03-31, Paid: 2024-03-31
        // Temporal Treatment: CurrentPeriodSalary
        // Selected Versions: Law 7/2024 (v2) + 2024 GOSI (v1)
        var engine = new DeterministicPayrollEngine();
        var snapshot = new PayrollInputSnapshot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 30000.00m, "[]", 22, 22 * 480, 0, 0, 0);

        var result = engine.Calculate(snapshot, new[] { Gosi2024, TaxPostLaw7 }, out var exceptions);

        Assert.True(exceptions.Count == 0);
        var taxLine = result.Lines.Single(l => l.ComponentCode == "EG_INCOME_TAX");
        Assert.Equal(4792.17m, taxLine.Amount); // Exemption 20k/yr, 0-40k @ 0%
    }

    [Fact]
    public void GoldenCaseC_December2025_OrdinaryPayroll_PaidJanuary2026_Uses2025TaxPeriod()
    {
        // Case C: December 2025 ordinary payroll paid January 2026
        // Entitlement: 2025-12-01..2025-12-31, Paid: 2026-01-05
        // Temporal Treatment: CurrentPeriodSalary (Governed by Dec 2025 PayrollTaxPeriod)
        // Selected Versions: Law 7/2024 (v2) + 2025 GOSI (v2)
        var engine = new DeterministicPayrollEngine();
        var snapshot = new PayrollInputSnapshot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 40000.00m, "[]", 22, 22 * 480, 0, 0, 0);

        var result = engine.Calculate(snapshot, new[] { Gosi2025, TaxPostLaw7 }, out var exceptions);

        Assert.True(exceptions.Count == 0);
        var gosiLine = result.Lines.Single(l => l.ComponentCode == "EG_GOSI_EMPLOYEE");
        var taxLine = result.Lines.Single(l => l.ComponentCode == "EG_INCOME_TAX");

        Assert.Equal(1595.00m, gosiLine.Amount); // 11% of 14,500 cap for 2025
        Assert.Equal(7080.42m, taxLine.Amount);
    }

    [Fact]
    public void GoldenCaseD_January2024_FrozenSalary_PaidIn2026_RecalculatedUnderLaw30()
    {
        // Case D: January 2024 frozen salary paid in 2026 (متجمد الأجور والمرتبات)
        // Payment Period: 2026-02-01..2026-02-28, Paid: 2026-02-28
        // Entitlement Period: 2024-01-01..2024-01-31
        // Temporal Treatment: ArrearsFrozenWages
        // ETA Rule: Must NOT calculate under 2026 rules! Must recalculate under Law 30/2023 (v1) and 2024 GOSI!
        var arrearsJson = """
        [
          {
            "code": "ARREARS_FROZEN_2024",
            "nameEn": "Frozen Salary 2024",
            "nameAr": "متجمد أجور 2024",
            "amount": 30000.00,
            "temporalTreatment": "ArrearsFrozenWages",
            "entitlementPeriodStart": "2024-01-01",
            "entitlementPeriodEnd": "2024-01-31",
            "sourceReason": "Court Settlement for 2024 Frozen Wages"
          }
        ]
        """;

        var engine = new DeterministicPayrollEngine();
        // Current salary is 0, pure arrears run in 2026
        var snapshot = new PayrollInputSnapshot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0m, arrearsJson, 22, 22 * 480, 0, 0, 0);

        // Active rules provided contains both current 2026 rules and historical 2024 rules
        var allRules = new[] { Gosi2026, TaxPostLaw7, Gosi2024, TaxPreLaw7 };

        var result = engine.Calculate(snapshot, allRules, out var exceptions);

        Assert.True(exceptions.Count == 0);
        var arrearsTaxLine = result.Lines.Single(l => l.ComponentCode == "EG_INCOME_TAX_ARREARS");
        var arrearsTrace = result.Traces.Single(t => t.RuleReference == "EG_INCOME_TAX" && t.Description.Contains("ETA Arrears Tax"));

        // Tax MUST match the Law 30/2023 entitlement calculation (5,052.58 EGP)
        Assert.Equal(5052.58m, arrearsTaxLine.Amount);
        Assert.True(arrearsTrace.InputValuesJson.Contains("\"statutoryRuleVersion\":1"));
        Assert.True(arrearsTrace.InputValuesJson.Contains("Law No. 30 of 2023"));
    }

    [Fact]
    public void GoldenCaseE_MultiYear_FrozenSalary_PaidIn2026_AllocatedAcrossRespectiveYears()
    {
        // Case E: Multi-year frozen salary paid as one amount in 2026
        // Part 1: Jan 2024 arrears (30,000 EGP) -> Law 30/2023 v1 (5,052.58 EGP)
        // Part 2: Jan 2025 arrears (30,000 EGP) -> Law 7/2024 v2 with 2025 GOSI (4,749.17 EGP)
        var multiYearArrearsJson = """
        [
          {
            "code": "ARREARS_2024",
            "nameEn": "Arrears 2024",
            "nameAr": "متجمد 2024",
            "amount": 30000.00,
            "temporalTreatment": "ArrearsFrozenWages",
            "entitlementPeriodStart": "2024-01-01",
            "entitlementPeriodEnd": "2024-01-31"
          },
          {
            "code": "ARREARS_2025",
            "nameEn": "Arrears 2025",
            "nameAr": "متجمد 2025",
            "amount": 30000.00,
            "temporalTreatment": "ArrearsFrozenWages",
            "entitlementPeriodStart": "2025-01-01",
            "entitlementPeriodEnd": "2025-01-31"
          }
        ]
        """;

        var engine = new DeterministicPayrollEngine();
        var snapshot = new PayrollInputSnapshot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 25000.00m, multiYearArrearsJson, 22, 22 * 480, 0, 0, 0);

        var allRules = new[] { Gosi2026, TaxPostLaw7, Gosi2025, Gosi2024, TaxPreLaw7 };

        var result = engine.Calculate(snapshot, allRules, out var exceptions);

        Assert.True(exceptions.Count == 0);
        var arrearsTaxLines = result.Lines.Where(l => l.ComponentCode == "EG_INCOME_TAX_ARREARS").ToList();
        Assert.Equal(2, arrearsTaxLines.Count);

        var tax2024 = arrearsTaxLines.Single(l => l.NameEn.Contains("2024-01"));
        var tax2025 = arrearsTaxLines.Single(l => l.NameEn.Contains("2025-01"));

        Assert.Equal(5052.58m, tax2024.Amount); // Law 30/2023 with 2024 GOSI (12.6k cap)
        Assert.Equal(4745.29m, tax2025.Amount); // Law 7/2024 with 2025 GOSI (14.5k cap)
    }

    [Fact]
    public void GoldenCaseF_CurrentPeriodAdjustment_UsesCurrentTaxPeriod()
    {
        // Case F: Current-period adjustment
        var adjustmentJson = """
        [
          {
            "code": "PERFORMANCE_BONUS",
            "nameEn": "Performance Bonus",
            "nameAr": "مكافأة أداء",
            "amount": 5000.00,
            "temporalTreatment": "AdjustmentSettlement"
          }
        ]
        """;

        var engine = new DeterministicPayrollEngine();
        var snapshot = new PayrollInputSnapshot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 35000.00m, adjustmentJson, 22, 22 * 480, 0, 0, 0);

        var result = engine.Calculate(snapshot, new[] { Gosi2026, TaxPostLaw7 }, out var exceptions);

        Assert.True(exceptions.Count == 0);
        var taxLine = result.Lines.Single(l => l.ComponentCode == "EG_INCOME_TAX");
        Assert.Equal(7019.79m, taxLine.Amount); // Taxed on total current period earnings 40,000
    }

    [Fact]
    public void GoldenCaseG_MissingHistoricalVerifiedRule_ThrowsBlockingException()
    {
        // Case G: Missing historical verified rule -> BLOCKING
        var oldArrearsJson = """
        [
          {
            "code": "ARREARS_2022",
            "nameEn": "Ancient Arrears 2022",
            "nameAr": "متجمد قديم 2022",
            "amount": 20000.00,
            "temporalTreatment": "ArrearsFrozenWages",
            "entitlementPeriodStart": "2022-01-01",
            "entitlementPeriodEnd": "2022-01-31"
          }
        ]
        """;

        var engine = new DeterministicPayrollEngine();
        var snapshot = new PayrollInputSnapshot(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 25000.00m, oldArrearsJson, 22, 22 * 480, 0, 0, 0);

        // Only modern rules provided; 2022 rule is missing
        var modernRules = new[] { Gosi2026, TaxPostLaw7 };

        var result = engine.Calculate(snapshot, modernRules, out var exceptions);

        Assert.True(exceptions.Count > 0);
        var blocking = exceptions.Single(e => e.Severity == ExceptionSeverity.Blocking);
        Assert.Equal("STATUTORY_RULE_MISSING", blocking.Category);
        Assert.True(blocking.Reason.Contains("BLOCKING COMPLIANCE EXCEPTION"));
        Assert.True(blocking.Reason.Contains("2022-01-01"));
    }

    // ========================================================================
    // 4. Temporal Exclusion Constraint & Domain Validation
    // ========================================================================

    [Fact]
    public void StatutoryRule_VerifiedOverlappingVersions_Rejected()
    {
        var rule = new StatutoryRule(
            Guid.NewGuid(), Jurisdiction.Egypt, RuleCategory.StatutoryDeduction,
            "EG_TEST_RULE", "Test Rule", "قاعدة تجريبية", "Labor Law",
            StatutoryApplicabilityBasis.PayrollPeriod, isVerified: true
        );

        var v1 = new StatutoryRuleVersion(
            Guid.NewGuid(), rule.Id, 1,
            new EffectivePeriod(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)),
            "{}", "Strategy1", VerificationStatus.Verified
        );
        rule.AddVersion(v1);

        var v2Overlapping = new StatutoryRuleVersion(
            Guid.NewGuid(), rule.Id, 2,
            new EffectivePeriod(new DateOnly(2024, 6, 1), new DateOnly(2025, 5, 31)),
            "{}", "Strategy1", VerificationStatus.Verified
        );

        var ex = Assert.Throws<InvalidOperationException>(() => rule.AddVersion(v2Overlapping));
        Assert.True(ex.Message.Contains("Temporal violation"));
    }

    [Fact]
    public void StatutoryRule_DraftOverlap_AllowedPriorToVerification()
    {
        var rule = new StatutoryRule(
            Guid.NewGuid(), Jurisdiction.Egypt, RuleCategory.StatutoryDeduction,
            "EG_TEST_DRAFT", "Test Draft", "مسودة تجريبية", "Labor Law",
            StatutoryApplicabilityBasis.PayrollPeriod, isVerified: true
        );

        var v1Verified = new StatutoryRuleVersion(
            Guid.NewGuid(), rule.Id, 1,
            new EffectivePeriod(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)),
            "{}", "Strategy1", VerificationStatus.Verified
        );
        rule.AddVersion(v1Verified);

        // Draft / Unverified version overlapping is permitted during authoring
        var v2Draft = new StatutoryRuleVersion(
            Guid.NewGuid(), rule.Id, 2,
            new EffectivePeriod(new DateOnly(2024, 6, 1), new DateOnly(2025, 5, 31)),
            "{}", "Strategy1", VerificationStatus.Unverified
        );
        rule.AddVersion(v2Draft);

        Assert.Equal(2, rule.Versions.Count);
    }

    // ========================================================================
    // 5. Unverified Rule Halts Calculation (No Stale Fallback)
    // ========================================================================

    [Fact]
    public void UnverifiedStatutoryVersion_ThrowsBlockingException_NeverFallsBack()
    {
        var unverifiedRule = new StatutoryRuleVersion(
            Guid.NewGuid(), TaxRuleId, 3,
            new EffectivePeriod(new DateOnly(2026, 1, 1), null),
            Law7Json,
            "EgyptProgressiveIncomeTaxStrategy",
            VerificationStatus.Unverified
        );

        var engine = new DeterministicPayrollEngine();
        var run = new PayrollRun(Guid.NewGuid(), Tenant, LegalEntity, Guid.NewGuid(), "RUN-2026-UNVERIFIED");
        var snapshot = new PayrollInputSnapshot(Guid.NewGuid(), run.Id, Guid.NewGuid(), 25000.00m, "[]", 22, 22 * 480, 0, 0, 0);
        run.LoadInputs(new[] { snapshot }, 1);

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            run.Calculate(engine, new[] { unverifiedRule }, 2);
        });

        Assert.True(ex.Message.Contains("BLOCKING EXCEPTION"));
        Assert.True(ex.Message.Contains("UNVERIFIED"));
    }
}
