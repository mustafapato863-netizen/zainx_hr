namespace Workforce.Modules.Approvals.Domain;

public enum ApprovalStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4,
    Expired = 5
}

public enum ApprovalStepStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Skipped = 4
}

public enum ApproverType
{
    DirectManager = 1,
    SpecificUser = 2,
    PermissionRole = 3
}
