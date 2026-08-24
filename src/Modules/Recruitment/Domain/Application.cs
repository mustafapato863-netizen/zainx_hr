using System;
using System.Collections.Generic;
using System.Linq;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Recruitment.Domain;

public class Application
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public LegalEntityId LegalEntityId { get; private set; }
    public Guid RequisitionId { get; private set; }
    public Guid CandidateId { get; private set; }
    public Guid PipelineVersionId { get; private set; }
    public Guid CurrentStageId { get; private set; }
    public ApplicationStatus Status { get; private set; }
    public string? Source { get; private set; }
    public DateTime AppliedAtUtc { get; private set; }
    public DateTime? DisposedAtUtc { get; private set; }
    public string? DispositionReason { get; private set; }
    public string? DispositionNote { get; private set; }
    public Guid? HiredPersonId { get; private set; }
    public Guid? HiredEmploymentId { get; private set; }
    public DateTime? HiredAtUtc { get; private set; }
    public uint RowVersion { get; private set; }

    private readonly List<ApplicationStageHistory> _stageHistory = new();
    public IReadOnlyList<ApplicationStageHistory> StageHistory => _stageHistory.OrderBy(h => h.ChangedAtUtc).ToList().AsReadOnly();

    private Application()
    {
        TenantId = default;
        LegalEntityId = default;
    }

    public Application(
        Guid id,
        TenantId tenantId,
        LegalEntityId legalEntityId,
        Guid requisitionId,
        Guid candidateId,
        Guid pipelineVersionId,
        Guid initialStageId,
        string? source = null,
        Guid? createdByUserId = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (tenantId == default || tenantId.Value == Guid.Empty) throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        if (legalEntityId == default || legalEntityId.Value == Guid.Empty) throw new ArgumentException("LegalEntityId cannot be empty.", nameof(legalEntityId));
        if (requisitionId == Guid.Empty) throw new ArgumentException("RequisitionId cannot be empty.", nameof(requisitionId));
        if (candidateId == Guid.Empty) throw new ArgumentException("CandidateId cannot be empty.", nameof(candidateId));
        if (pipelineVersionId == Guid.Empty) throw new ArgumentException("PipelineVersionId cannot be empty.", nameof(pipelineVersionId));
        if (initialStageId == Guid.Empty) throw new ArgumentException("InitialStageId cannot be empty.", nameof(initialStageId));

        Id = id;
        TenantId = tenantId;
        LegalEntityId = legalEntityId;
        RequisitionId = requisitionId;
        CandidateId = candidateId;
        PipelineVersionId = pipelineVersionId;
        CurrentStageId = initialStageId;
        Status = ApplicationStatus.Active;
        Source = source?.Trim();
        AppliedAtUtc = DateTime.UtcNow;
        RowVersion = 1;

        // Record initial history
        _stageHistory.Add(new ApplicationStageHistory(
            Guid.NewGuid(),
            id,
            null,
            initialStageId,
            createdByUserId ?? Guid.Empty,
            AppliedAtUtc,
            "Initial application created",
            null
        ));
    }

    public static Application Reconstitute(
        Guid id,
        TenantId tenantId,
        LegalEntityId legalEntityId,
        Guid requisitionId,
        Guid candidateId,
        Guid pipelineVersionId,
        Guid currentStageId,
        ApplicationStatus status,
        string? source,
        DateTime appliedAtUtc,
        DateTime? disposedAtUtc,
        string? dispositionReason,
        string? dispositionNote,
        Guid? hiredPersonId,
        Guid? hiredEmploymentId,
        DateTime? hiredAtUtc,
        uint rowVersion,
        IEnumerable<ApplicationStageHistory>? stageHistory = null)
    {
        var app = new Application
        {
            Id = id,
            TenantId = tenantId,
            LegalEntityId = legalEntityId,
            RequisitionId = requisitionId,
            CandidateId = candidateId,
            PipelineVersionId = pipelineVersionId,
            CurrentStageId = currentStageId,
            Status = status,
            Source = source,
            AppliedAtUtc = appliedAtUtc,
            DisposedAtUtc = disposedAtUtc,
            DispositionReason = dispositionReason,
            DispositionNote = dispositionNote,
            HiredPersonId = hiredPersonId,
            HiredEmploymentId = hiredEmploymentId,
            HiredAtUtc = hiredAtUtc,
            RowVersion = rowVersion
        };
        if (stageHistory != null)
        {
            app._stageHistory.AddRange(stageHistory);
        }
        return app;
    }

    public void MoveToStage(
        Guid targetStageId,
        Guid actorUserId,
        string? reason,
        string? idempotencyKey,
        uint expectedRowVersion)
    {
        ValidateConcurrency(expectedRowVersion);
        if (Status != ApplicationStatus.Active)
            throw new InvalidOperationException($"Cannot move application in status '{Status}'. Only 'Active' applications can transition stages.");

        if (targetStageId == Guid.Empty) throw new ArgumentException("Target stage ID cannot be empty.", nameof(targetStageId));
        if (targetStageId == CurrentStageId) return; // No-op for identical stage

        // Check if idempotencyKey has already been applied
        if (!string.IsNullOrWhiteSpace(idempotencyKey) && _stageHistory.Any(h => h.IdempotencyKey == idempotencyKey.Trim()))
        {
            return; // Idempotent no-op
        }

        var history = new ApplicationStageHistory(
            Guid.NewGuid(),
            Id,
            CurrentStageId,
            targetStageId,
            actorUserId,
            DateTime.UtcNow,
            reason?.Trim(),
            idempotencyKey?.Trim()
        );

        CurrentStageId = targetStageId;
        _stageHistory.Add(history);
        RowVersion++;
    }

    public void Reject(
        string reasonCode,
        string? reasonNote,
        Guid actorUserId,
        uint expectedRowVersion)
    {
        ValidateConcurrency(expectedRowVersion);
        if (Status != ApplicationStatus.Active)
            throw new InvalidOperationException($"Cannot reject application in status '{Status}'. Must be 'Active'.");

        if (string.IsNullOrWhiteSpace(reasonCode)) throw new ArgumentException("Rejection reason code is required.", nameof(reasonCode));

        Status = ApplicationStatus.Rejected;
        DispositionReason = reasonCode.Trim();
        DispositionNote = reasonNote?.Trim();
        DisposedAtUtc = DateTime.UtcNow;

        _stageHistory.Add(new ApplicationStageHistory(
            Guid.NewGuid(),
            Id,
            CurrentStageId,
            CurrentStageId,
            actorUserId,
            DisposedAtUtc.Value,
            $"Rejected: {reasonCode}" + (string.IsNullOrWhiteSpace(reasonNote) ? "" : $" - {reasonNote}"),
            null
        ));

        RowVersion++;
    }

    public void Withdraw(
        string? reason,
        Guid actorUserId,
        uint expectedRowVersion)
    {
        ValidateConcurrency(expectedRowVersion);
        if (Status != ApplicationStatus.Active)
            throw new InvalidOperationException($"Cannot withdraw application in status '{Status}'. Must be 'Active'.");

        Status = ApplicationStatus.Withdrawn;
        DispositionReason = "WITHDRAWN_BY_CANDIDATE";
        DispositionNote = reason?.Trim();
        DisposedAtUtc = DateTime.UtcNow;

        _stageHistory.Add(new ApplicationStageHistory(
            Guid.NewGuid(),
            Id,
            CurrentStageId,
            CurrentStageId,
            actorUserId,
            DisposedAtUtc.Value,
            $"Withdrawn: {reason}",
            null
        ));

        RowVersion++;
    }

    public void MarkHired(
        Guid personId,
        Guid employmentId,
        Guid actorUserId,
        uint expectedRowVersion)
    {
        ValidateConcurrency(expectedRowVersion);
        if (Status == ApplicationStatus.Hired)
        {
            // Idempotent success if same person/employment
            if (HiredPersonId == personId && HiredEmploymentId == employmentId)
                return;
            throw new InvalidOperationException("Application is already marked as Hired with different Person/Employment records.");
        }

        if (Status != ApplicationStatus.Active)
            throw new InvalidOperationException($"Cannot hire candidate from application in status '{Status}'. Must be 'Active'.");

        if (personId == Guid.Empty) throw new ArgumentException("PersonId cannot be empty.", nameof(personId));
        if (employmentId == Guid.Empty) throw new ArgumentException("EmploymentId cannot be empty.", nameof(employmentId));

        Status = ApplicationStatus.Hired;
        HiredPersonId = personId;
        HiredEmploymentId = employmentId;
        HiredAtUtc = DateTime.UtcNow;

        _stageHistory.Add(new ApplicationStageHistory(
            Guid.NewGuid(),
            Id,
            CurrentStageId,
            CurrentStageId,
            actorUserId,
            HiredAtUtc.Value,
            $"Candidate hired. Linked to Person: {personId}, Employment: {employmentId}",
            null
        ));

        RowVersion++;
    }

    private void ValidateConcurrency(uint expectedRowVersion)
    {
        if (RowVersion != expectedRowVersion)
        {
            throw new InvalidOperationException($"Concurrency conflict: Application has been modified. Expected version {expectedRowVersion}, current version {RowVersion}.");
        }
    }
}

public class ApplicationStageHistory
{
    public Guid Id { get; private set; }
    public Guid ApplicationId { get; private set; }
    public Guid? FromStageId { get; private set; }
    public Guid ToStageId { get; private set; }
    public Guid ChangedByUserId { get; private set; }
    public DateTime ChangedAtUtc { get; private set; }
    public string? Reason { get; private set; }
    public string? IdempotencyKey { get; private set; }

    private ApplicationStageHistory()
    {
    }

    public ApplicationStageHistory(
        Guid id,
        Guid applicationId,
        Guid? fromStageId,
        Guid toStageId,
        Guid changedByUserId,
        DateTime changedAtUtc,
        string? reason,
        string? idempotencyKey)
    {
        Id = id;
        ApplicationId = applicationId;
        FromStageId = fromStageId;
        ToStageId = toStageId;
        ChangedByUserId = changedByUserId;
        ChangedAtUtc = changedAtUtc;
        Reason = reason;
        IdempotencyKey = idempotencyKey;
    }
}
