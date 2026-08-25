using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Ai.Domain;

public enum ProposalStatus
{
    Draft,
    ReadyForConfirmation,
    Confirmed,
    Executing,
    Completed,
    Cancelled,
    Expired,
    Rejected,
    Stale,
    Failed
}

public sealed class AiActionProposal
{
    public Guid Id { get; private set; }
    public Guid ConversationId { get; private set; }
    public TenantId TenantId { get; private set; }
    public LegalEntityId? LegalEntityId { get; private set; }
    public UserId RequestedByUserId { get; private set; }
    public string ActionCode { get; private set; }
    public string TargetEntityType { get; private set; }
    public string TargetEntityId { get; private set; }
    public ProposalStatus Status { get; private set; }
    public uint ExpectedRowVersion { get; private set; }
    public DateTime? EffectiveDateUtc { get; private set; }
    public string BeforeSnapshotJson { get; private set; }
    public string AfterSnapshotJson { get; private set; }
    public string ImpactSummaryJson { get; private set; }
    public string RequiredPermission { get; private set; }
    public string IdempotencyKey { get; private set; }
    public string ProposalHash { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? ConfirmedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? ErrorMessage { get; private set; }

    private AiActionProposal()
    {
        ActionCode = string.Empty;
        TargetEntityType = string.Empty;
        TargetEntityId = string.Empty;
        BeforeSnapshotJson = "{}";
        AfterSnapshotJson = "{}";
        ImpactSummaryJson = "{}";
        RequiredPermission = string.Empty;
        IdempotencyKey = string.Empty;
        ProposalHash = string.Empty;
    }

    public AiActionProposal(
        Guid id,
        Guid conversationId,
        TenantId tenantId,
        LegalEntityId? legalEntityId,
        UserId requestedByUserId,
        string actionCode,
        string targetEntityType,
        string targetEntityId,
        uint expectedRowVersion,
        DateTime? effectiveDateUtc,
        string beforeSnapshotJson,
        string afterSnapshotJson,
        string impactSummaryJson,
        string requiredPermission,
        TimeSpan validityPeriod)
    {
        if (id == Guid.Empty) throw new ArgumentException("Proposal id cannot be empty.", nameof(id));
        if (conversationId == Guid.Empty) throw new ArgumentException("Conversation id cannot be empty.", nameof(conversationId));
        if (string.IsNullOrWhiteSpace(actionCode)) throw new ArgumentException("ActionCode is required.", nameof(actionCode));
        if (string.IsNullOrWhiteSpace(targetEntityType)) throw new ArgumentException("TargetEntityType is required.", nameof(targetEntityType));
        if (string.IsNullOrWhiteSpace(targetEntityId)) throw new ArgumentException("TargetEntityId is required.", nameof(targetEntityId));

        Id = id;
        ConversationId = conversationId;
        TenantId = tenantId;
        LegalEntityId = legalEntityId;
        RequestedByUserId = requestedByUserId;
        ActionCode = actionCode.Trim().ToLowerInvariant();
        TargetEntityType = targetEntityType.Trim();
        TargetEntityId = targetEntityId.Trim();
        ExpectedRowVersion = expectedRowVersion;
        EffectiveDateUtc = effectiveDateUtc;
        BeforeSnapshotJson = string.IsNullOrWhiteSpace(beforeSnapshotJson) ? "{}" : beforeSnapshotJson;
        AfterSnapshotJson = string.IsNullOrWhiteSpace(afterSnapshotJson) ? "{}" : afterSnapshotJson;
        ImpactSummaryJson = string.IsNullOrWhiteSpace(impactSummaryJson) ? "{}" : impactSummaryJson;
        RequiredPermission = requiredPermission.Trim();
        Status = ProposalStatus.ReadyForConfirmation;
        CreatedAtUtc = DateTime.UtcNow;
        ExpiresAtUtc = CreatedAtUtc.Add(validityPeriod <= TimeSpan.Zero ? TimeSpan.FromMinutes(15) : validityPeriod);
        IdempotencyKey = $"{tenantId.Value:N}_{ActionCode}_{targetEntityId}_{id:N}";
        ProposalHash = ComputeProposalHash();
    }

    public string ComputeProposalHash()
    {
        var raw = $"{TenantId.Value}|{LegalEntityId?.Value}|{ActionCode}|{TargetEntityType}|{TargetEntityId}|{ExpectedRowVersion}|{(EffectiveDateUtc.HasValue ? EffectiveDateUtc.Value.ToString("O") : "null")}|{BeforeSnapshotJson}|{AfterSnapshotJson}|{RequiredPermission}";
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }

    public bool VerifyHash(string expectedHash)
    {
        var current = ComputeProposalHash();
        return string.Equals(current, expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsExpired(DateTime currentUtc) => currentUtc > ExpiresAtUtc;

    public void MarkConfirmed()
    {
        if (Status != ProposalStatus.ReadyForConfirmation)
        {
            throw new InvalidOperationException($"Cannot confirm proposal in '{Status}' status.");
        }
        if (IsExpired(DateTime.UtcNow))
        {
            Status = ProposalStatus.Expired;
            throw new InvalidOperationException("Cannot confirm an expired proposal.");
        }

        Status = ProposalStatus.Confirmed;
        ConfirmedAtUtc = DateTime.UtcNow;
    }

    public void MarkExecuting()
    {
        if (Status != ProposalStatus.Confirmed && Status != ProposalStatus.ReadyForConfirmation)
        {
            throw new InvalidOperationException($"Cannot execute proposal in '{Status}' status.");
        }
        Status = ProposalStatus.Executing;
    }

    public void MarkCompleted()
    {
        if (Status != ProposalStatus.Executing && Status != ProposalStatus.Confirmed)
        {
            throw new InvalidOperationException($"Cannot complete proposal in '{Status}' status.");
        }
        Status = ProposalStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
    }

    public void MarkCancelled()
    {
        if (Status == ProposalStatus.Completed || Status == ProposalStatus.Executing)
        {
            throw new InvalidOperationException($"Cannot cancel proposal in '{Status}' status.");
        }
        Status = ProposalStatus.Cancelled;
    }

    public void MarkStale(string reason = "Target entity state changed since proposal was generated.")
    {
        Status = ProposalStatus.Stale;
        ErrorMessage = reason;
    }

    public void MarkExpired()
    {
        Status = ProposalStatus.Expired;
        ErrorMessage = "Proposal expired before confirmation.";
    }

    public void MarkFailed(string errorMessage)
    {
        Status = ProposalStatus.Failed;
        ErrorMessage = errorMessage;
    }
}

public sealed class AiActionExecution
{
    public Guid Id { get; private set; }
    public Guid ProposalId { get; private set; }
    public TenantId TenantId { get; private set; }
    public UserId ActorUserId { get; private set; }
    public string ActionCode { get; private set; }
    public string IdempotencyKey { get; private set; }
    public string Status { get; private set; }
    public string ResultPayloadJson { get; private set; }
    public long DurationMs { get; private set; }
    public DateTime ExecutedAtUtc { get; private set; }

    public AiActionExecution(
        Guid id,
        Guid proposalId,
        TenantId tenantId,
        UserId actorUserId,
        string actionCode,
        string idempotencyKey,
        string status,
        string resultPayloadJson,
        long durationMs)
    {
        Id = id;
        ProposalId = proposalId;
        TenantId = tenantId;
        ActorUserId = actorUserId;
        ActionCode = actionCode;
        IdempotencyKey = idempotencyKey;
        Status = status;
        ResultPayloadJson = resultPayloadJson;
        DurationMs = durationMs;
        ExecutedAtUtc = DateTime.UtcNow;
    }
}
