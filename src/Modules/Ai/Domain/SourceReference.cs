using System;

namespace Workforce.Modules.Ai.Domain;

/// <summary>
/// Value object capturing evidence provenance and attribution.
/// </summary>
public sealed class SourceReference
{
    public Guid Id { get; }
    public Guid MessageId { get; }
    public AiSourceCategory SourceCategory { get; }
    public string Title { get; }
    public string? EntityType { get; }
    public string? EntityId { get; }
    public string? PolicyCode { get; }
    public int? PolicyVersion { get; }
    public Guid? PayrollRunId { get; }
    public string? MetadataJson { get; }
    public DateTime RetrievedAtUtc { get; }

    public SourceReference(
        Guid id,
        Guid messageId,
        AiSourceCategory sourceCategory,
        string title,
        string? entityType = null,
        string? entityId = null,
        string? policyCode = null,
        int? policyVersion = null,
        Guid? payrollRunId = null,
        string? metadataJson = null,
        DateTime? retrievedAtUtc = null)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        MessageId = messageId;
        SourceCategory = sourceCategory;
        Title = title ?? string.Empty;
        EntityType = entityType;
        EntityId = entityId;
        PolicyCode = policyCode;
        PolicyVersion = policyVersion;
        PayrollRunId = payrollRunId;
        MetadataJson = metadataJson ?? "{}";
        RetrievedAtUtc = retrievedAtUtc ?? DateTime.UtcNow;
    }
}
