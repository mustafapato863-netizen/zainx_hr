namespace Workforce.Modules.Leave.Domain;

public enum LeaveCategory
{
    Annual = 1,
    Sick = 2,
    Maternity = 3,
    Paternity = 4,
    Unpaid = 5,
    Emergency = 6,
    Hajj = 7,
    Compassionate = 8
}

public enum LeaveRequestStatus
{
    Draft = 1,
    Submitted = 2,
    PendingApproval = 3,
    Approved = 4,
    Rejected = 5,
    Cancelled = 6,
    Withdrawn = 7
}

public enum DurationUnit
{
    Days = 1,
    HalfDays = 2,
    Hours = 3
}
