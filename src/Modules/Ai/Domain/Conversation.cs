using System;
using System.Collections.Generic;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Ai.Domain;

/// <summary>
/// Aggregate root representing an interactive AI conversation session.
/// </summary>
public sealed class Conversation
{
    public Guid Id { get; }
    public TenantId TenantId { get; }
    public LegalEntityId? LegalEntityId { get; }
    public UserId UserId { get; }
    public string Title { get; private set; }
    public string? ContextEntityType { get; }
    public string? ContextEntityId { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime UpdatedAtUtc { get; private set; }
    public List<Message> Messages { get; } = new();

    public Conversation(
        Guid id,
        TenantId tenantId,
        LegalEntityId? legalEntityId,
        UserId userId,
        string title,
        string? contextEntityType = null,
        string? contextEntityId = null,
        DateTime? createdAtUtc = null,
        DateTime? updatedAtUtc = null)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        TenantId = tenantId;
        LegalEntityId = legalEntityId;
        UserId = userId;
        Title = string.IsNullOrWhiteSpace(title) ? "New Conversation" : title;
        ContextEntityType = contextEntityType;
        ContextEntityId = contextEntityId;
        CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow;
        UpdatedAtUtc = updatedAtUtc ?? CreatedAtUtc;
    }

    public void AddMessage(Message message)
    {
        if (message != null)
        {
            Messages.Add(message);
            UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    public void UpdateTitle(string newTitle)
    {
        if (!string.IsNullOrWhiteSpace(newTitle))
        {
            Title = newTitle;
            UpdatedAtUtc = DateTime.UtcNow;
        }
    }
}
