using System;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Audit.Domain;

public class AuditRecord
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public LegalEntityId? LegalEntityId { get; private set; }
    public Guid ActorUserId { get; private set; }
    public string ActorType { get; private set; }
    public string ActionCode { get; private set; }
    public string EntityType { get; private set; }
    public string EntityId { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? ReasonCode { get; private set; }
    public string? ChangesBeforeJson { get; private set; }
    public string? ChangesAfterJson { get; private set; }
    public string? SafeMetadataJson { get; private set; }
    public string DataClassification { get; private set; }

    private AuditRecord()
    {
        ActorType = "User";
        ActionCode = string.Empty;
        EntityType = string.Empty;
        EntityId = string.Empty;
        DataClassification = "Internal";
    }

    public AuditRecord(
        Guid id,
        TenantId tenantId,
        LegalEntityId? legalEntityId,
        Guid actorUserId,
        string actorType,
        string actionCode,
        string entityType,
        string entityId,
        DateTime occurredAtUtc,
        string? correlationId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? reasonCode = null,
        string? changesBeforeJson = null,
        string? changesAfterJson = null,
        string? safeMetadataJson = null,
        string dataClassification = "Internal")
    {
        if (id == Guid.Empty) throw new ArgumentException("Audit ID cannot be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(actionCode)) throw new ArgumentException("Action code cannot be empty.", nameof(actionCode));
        if (string.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("Entity type cannot be empty.", nameof(entityType));
        if (string.IsNullOrWhiteSpace(entityId)) throw new ArgumentException("Entity ID cannot be empty.", nameof(entityId));

        Id = id;
        TenantId = tenantId;
        LegalEntityId = legalEntityId;
        ActorUserId = actorUserId;
        ActorType = string.IsNullOrWhiteSpace(actorType) ? "User" : actorType.Trim();
        ActionCode = actionCode.Trim().ToLowerInvariant();
        EntityType = entityType.Trim();
        EntityId = entityId.Trim();
        OccurredAtUtc = occurredAtUtc == default ? DateTime.UtcNow : occurredAtUtc;
        CorrelationId = correlationId;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        ReasonCode = reasonCode;
        ChangesBeforeJson = changesBeforeJson;
        ChangesAfterJson = changesAfterJson;
        SafeMetadataJson = safeMetadataJson;
        DataClassification = string.IsNullOrWhiteSpace(dataClassification) ? "Internal" : dataClassification.Trim();
    }
}
