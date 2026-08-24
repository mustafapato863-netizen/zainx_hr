using System;
using System.Collections.Generic;
using System.Linq;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Approvals.Domain;

public class ApprovalRequest
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public LegalEntityId LegalEntityId { get; private set; }
    public string SourceModule { get; private set; }
    public Guid SourceEntityId { get; private set; }
    public string WorkflowType { get; private set; }
    public string Title { get; private set; }
    public int CurrentStepOrder { get; private set; }
    public int TotalSteps { get; private set; }
    public ApprovalStatus Status { get; private set; }
    public Guid RequesterUserId { get; private set; }
    public Guid RequesterEmploymentId { get; private set; }
    public string? PayloadSnapshotJson { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public uint RowVersion { get; private set; }

    private readonly List<ApprovalStep> _steps = new();
    public IReadOnlyCollection<ApprovalStep> Steps => _steps.AsReadOnly();

    private readonly List<ApprovalDecisionHistory> _history = new();
    public IReadOnlyCollection<ApprovalDecisionHistory> History => _history.AsReadOnly();

    private ApprovalRequest()
    {
        SourceModule = string.Empty;
        WorkflowType = string.Empty;
        Title = string.Empty;
    }

    public ApprovalRequest(
        Guid id,
        TenantId tenantId,
        LegalEntityId legalEntityId,
        string sourceModule,
        Guid sourceEntityId,
        string workflowType,
        string title,
        Guid requesterUserId,
        Guid requesterEmploymentId,
        string? payloadSnapshotJson = null,
        int totalSteps = 1)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (sourceEntityId == Guid.Empty) throw new ArgumentException("SourceEntityId cannot be empty.", nameof(sourceEntityId));
        if (string.IsNullOrWhiteSpace(sourceModule)) throw new ArgumentException("SourceModule is required.", nameof(sourceModule));
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.", nameof(title));

        Id = id;
        TenantId = tenantId;
        LegalEntityId = legalEntityId;
        SourceModule = sourceModule.Trim();
        SourceEntityId = sourceEntityId;
        WorkflowType = workflowType.Trim();
        Title = title.Trim();
        RequesterUserId = requesterUserId;
        RequesterEmploymentId = requesterEmploymentId;
        PayloadSnapshotJson = payloadSnapshotJson;
        CurrentStepOrder = 1;
        TotalSteps = Math.Max(1, totalSteps);
        Status = ApprovalStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        RowVersion = 1;
    }

    public void AddStep(ApprovalStep step)
    {
        _steps.Add(step);
    }

    public void ApproveCurrentStep(Guid actorUserId, string? notes, uint expectedRowVersion)
    {
        VerifyRowVersion(expectedRowVersion);

        if (Status != ApprovalStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot approve request in '{Status}' state.");
        }

        var currentStep = _steps.FirstOrDefault(s => s.StepOrder == CurrentStepOrder);
        if (currentStep != null)
        {
            currentStep.Approve(actorUserId, notes, currentStep.RowVersion);
        }

        _history.Add(new ApprovalDecisionHistory(
            Guid.NewGuid(), Id, CurrentStepOrder, actorUserId, "Approved", notes
        ));

        if (CurrentStepOrder >= TotalSteps)
        {
            Status = ApprovalStatus.Approved;
        }
        else
        {
            CurrentStepOrder++;
        }

        UpdatedAt = DateTime.UtcNow;
        RowVersion++;
    }

    public void RejectCurrentStep(Guid actorUserId, string reason, uint expectedRowVersion)
    {
        VerifyRowVersion(expectedRowVersion);

        if (Status != ApprovalStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot reject request in '{Status}' state.");
        }

        var currentStep = _steps.FirstOrDefault(s => s.StepOrder == CurrentStepOrder);
        if (currentStep != null)
        {
            currentStep.Reject(actorUserId, reason, currentStep.RowVersion);
        }

        _history.Add(new ApprovalDecisionHistory(
            Guid.NewGuid(), Id, CurrentStepOrder, actorUserId, "Rejected", reason
        ));

        Status = ApprovalStatus.Rejected;
        UpdatedAt = DateTime.UtcNow;
        RowVersion++;
    }

    public void Cancel(Guid actorUserId, uint expectedRowVersion)
    {
        VerifyRowVersion(expectedRowVersion);

        if (Status != ApprovalStatus.Pending)
        {
            throw new InvalidOperationException("Only pending requests can be cancelled.");
        }

        _history.Add(new ApprovalDecisionHistory(
            Guid.NewGuid(), Id, CurrentStepOrder, actorUserId, "Cancelled", "Cancelled by requester."
        ));

        Status = ApprovalStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
        RowVersion++;
    }

    private void VerifyRowVersion(uint expected)
    {
        if (expected != RowVersion)
        {
            throw new InvalidOperationException("Optimistic concurrency conflict on approval request.");
        }
    }
}
