namespace Workforce.Modules.Payroll.Domain;

public enum PayrollRunStatus
{
    Draft = 1,
    InputsLoaded = 2,
    Calculated = 3,
    UnderReview = 4,
    Approved = 5,
    Finalized = 6,
    OutputsPublished = 7
}

public enum ComponentCategory
{
    BaseSalary = 1,
    Allowance = 2,
    VariableEarning = 3,
    Deduction = 4,
    StatutoryDeduction = 5,
    EmployerContribution = 6
}

public enum CalculationType
{
    FixedAmount = 1,
    HourlyRate = 2,
    DailyRate = 3,
    PercentageOfBase = 4,
    StatutoryFormula = 5
}

public enum ExceptionSeverity
{
    Info = 1,
    Warning = 2,
    Blocking = 3
}

public enum ExceptionStatus
{
    Open = 1,
    Resolved = 2,
    Waived = 3
}
