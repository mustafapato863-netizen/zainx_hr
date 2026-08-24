namespace Workforce.Modules.Payroll.Domain;

/// <summary>
/// Defines the statutory salary tax temporal treatment according to Egyptian Tax Authority (ETA) regulations
/// and Law No. 91 of 2005 Article 8.
/// </summary>
public enum SalaryTaxTemporalTreatment
{
    /// <summary>
    /// Normal salary and regular allowances belonging to the current payroll tax period.
    /// Governed by the statutory income tax rule version active for the current PayrollTaxPeriod.
    /// </summary>
    CurrentPeriodSalary = 1,

    /// <summary>
    /// Arrears, frozen wages, deferred compensation, or retroactive wage settlements belonging to prior entitlement periods (متجمد الأجور والمرتبات).
    /// Under official ETA guidance, arrears are allocated to their respective years/months of entitlement,
    /// recalculated against historical statutory rule versions, and taxed on a differential/incremental basis.
    /// </summary>
    ArrearsFrozenWages = 2,

    /// <summary>
    /// Current-period minor adjustments, regular bonuses, or period corrections belonging to the active tax year.
    /// </summary>
    AdjustmentSettlement = 3
}
