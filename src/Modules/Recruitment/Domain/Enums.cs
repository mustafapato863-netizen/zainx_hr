namespace Workforce.Modules.Recruitment.Domain;

public enum RequisitionStatus
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    Open = 4,
    OnHold = 5,
    Closed = 6,
    Cancelled = 7
}

public enum StageKind
{
    Applied = 1,
    Screening = 2,
    Assessment = 3,
    Interview = 4,
    Offer = 5,
    Hired = 6,
    Rejected = 7
}

public enum ApplicationStatus
{
    Active = 1,
    Withdrawn = 2,
    Rejected = 3,
    Hired = 4
}

public enum InterviewType
{
    Screening = 1,
    Technical = 2,
    Behavioral = 3,
    Manager = 4,
    Panel = 5
}

public enum InterviewStatus
{
    Scheduled = 1,
    Completed = 2,
    Cancelled = 3,
    Rescheduled = 4,
    NoShow = 5
}

public enum InterviewerRole
{
    Lead = 1,
    PanelMember = 2,
    Observer = 3
}

public enum ScorecardRecommendation
{
    StrongYes = 1,
    Yes = 2,
    Neutral = 3,
    No = 4,
    StrongNo = 5
}

public enum OfferStatus
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    Issued = 4,
    Accepted = 5,
    Declined = 6,
    Expired = 7,
    Withdrawn = 8
}
