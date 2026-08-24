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
