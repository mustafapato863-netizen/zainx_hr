namespace Workforce.Modules.Compliance.Domain;

public enum Jurisdiction
{
    Egypt = 1,
    SaudiArabia = 2,
    UnitedArabEmirates = 3,
    Universal = 99
}

public enum RuleCategory
{
    IncomeTax = 1,
    SocialInsurance = 2,
    LaborLaw = 3,
    StatutoryDeduction = 4,
    EmployerContribution = 5
}

public enum VerificationStatus
{
    Verified = 1,
    Unverified = 2,
    PendingReview = 3,
    Deprecated = 4
}

public enum StatutoryApplicabilityBasis
{
    /// <summary>
    /// Evaluated on the payroll earnings period (e.g. Social Insurance / GOSI contribution month).
    /// </summary>
    PayrollPeriod = 1,

    /// <summary>
    /// Alias for PayrollPeriod.
    /// </summary>
    ContributionPeriod = 1,

    /// <summary>
    /// Evaluated on the salary tax period (e.g. Income Tax salary withholding for current-period salary).
    /// </summary>
    PayrollTaxPeriod = 2,

    /// <summary>
    /// Alias for PayrollTaxPeriod.
    /// </summary>
    TaxPeriod = 2,

    /// <summary>
    /// Evaluated on the historical entitlement period (e.g. Arrears and Frozen Wages / متجمد الأجور والمرتبات recalculation).
    /// </summary>
    EntitlementPeriod = 3,

    /// <summary>
    /// Evaluated on the actual disbursement/payment date (used strictly where statute explicitly mandates payment event).
    /// </summary>
    PaymentDate = 4,

    /// <summary>
    /// Evaluated on effective business settlement date.
    /// </summary>
    EffectiveBusinessDate = 5
}
