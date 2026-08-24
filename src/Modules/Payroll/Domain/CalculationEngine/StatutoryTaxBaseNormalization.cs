using System;

namespace Workforce.Modules.Payroll.Domain.CalculationEngine;

/// <summary>
/// Implements statutory tax base normalization rules.
/// Egypt Income Tax Law No. 91 of 2005 Article 8 (and amendments by Law 30/2023 & Law 7/2024):
/// "يقرب صافي الدخل السنوي عند حساب الضريبة إلى أقرب عشرة جنيهات أقل"
/// (Annual net income is rounded down to the nearest EGP 10).
/// </summary>
public static class StatutoryTaxBaseNormalization
{
    public static decimal NormalizeAnnualTaxBase(decimal annualTaxableIncome)
    {
        if (annualTaxableIncome <= 0) return 0m;
        
        // Floor to the nearest EGP 10
        return Math.Floor(annualTaxableIncome / 10m) * 10m;
    }
}
