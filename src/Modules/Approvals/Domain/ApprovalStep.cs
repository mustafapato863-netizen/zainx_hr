using System;

namespace Workforce.Modules.Approvals.Domain;

public class ApprovalStep
{
    public Guid Id { get; private set; }
    public Guid ApprovalRequestId { get; private set; }
    public int StepOrder { get; private set; }
    public Guid? AssignedApproverUserId { get; private set; }
    public string? AssignedRole { get; private set; }
    public ApprovalStepStatus Status { get; private set; }
    public DateTime? DecidedAtUtc { get; private set; }
    public Guid? DecidedByUserId { get; private set; }
    public string? DecisionReason { get; private set; }
    public uint RowVersion { get; private set; }

    private ApprovalStep() { }

    public ApprovalStep(
        Guid id,
        Guid approvalRequestId,
        int stepOrder,
        Guid? assignedApproverUserId = null,
        string? assignedRole = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (approvalRequestId == Guid.Empty) throw new ArgumentException("ApprovalRequestId cannot be empty.", nameof(approvalRequestId));

        Id = id;
        ApprovalRequestId = approvalRequestId;
        StepOrder = stepOrder;
        AssignedApproverUserId = assignedApproverUserId;
        AssignedRole = assignedRole;
        Status = ApprovalStepStatus.Pending;
        RowVersion = 1;
    }

    public void Approve(Guid actorUserId, string? notes, uint expectedRowVersion)
    {
        VerifyRowVersion(expectedRowVersion);
        Status = ApprovalStepStatus.Approved;
        DecidedByUserId = actorUserId;
        DecisionReason = notes?.Trim();
        DecidedAtUtc = DateTime.UtcNow;
        RowVersion++;
    }

    public void Reject(Guid actorUserId, string reason, uint expectedRowVersion)
    {
        VerifyRowVersion(expectedRowVersion);
        Status = ApprovalStepStatus.Rejected;
        DecidedByUserId = actorUserId;
        DecisionReason = string.IsNullOrWhiteSpace(reason) ? "Rejected" : reason.Trim();
        DecidedAtUtc = DateTime.UtcNow;
        RowVersion++;
    }

    private void VerifyRowVersion(uint expected)
    {
        if (expected != RowVersion)
        {
            throw new InvalidOperationException("Optimistic concurrency conflict on approval step.");
        }
    }
}

public class ApprovalDecisionHistory
{
    public Guid Id { get; private set; }
    public Guid ApprovalRequestId { get; private set; }
    public int StepOrder { get; private set; }
    public Guid ActorUserId { get; private set; }
    public string Action { get; private set; }
    public string? Reason { get; private set; }
    public DateTime TimestampUtc { get; private set; }

    private ApprovalDecisionHistory()
    {
        Action = string.Empty;
    }

    public ApprovalDecisionHistory(
        Guid id,
        Guid approvalRequestId,
        int stepOrder,
        Guid actorUserId,
        string action,
        string? reason = null)
    {
        Id = id;
        ApprovalRequestId = approvalRequestId;
        StepOrder = stepOrder;
        ActorUserId = actorUserId;
        Action = action;
        Reason = reason?.Trim();
        TimestampUtc = DateTime.UtcNow;
    }
}
