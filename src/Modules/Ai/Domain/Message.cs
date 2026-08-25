using System;
using System.Collections.Generic;

namespace Workforce.Modules.Ai.Domain;

/// <summary>
/// A single conversational message within an AI session.
/// </summary>
public sealed class Message
{
    public Guid Id { get; }
    public Guid ConversationId { get; }
    public string SenderRole { get; } // "User", "Assistant", "System"
    public string Content { get; }
    public AiSourceCategory SourceCategory { get; }
    public int TokensUsed { get; }
    public DateTime CreatedAtUtc { get; }
    public List<ToolExecution> ToolExecutions { get; } = new();
    public List<SourceReference> SourceReferences { get; } = new();

    public Message(
        Guid id,
        Guid conversationId,
        string senderRole,
        string content,
        AiSourceCategory sourceCategory = AiSourceCategory.CompanyData,
        int tokensUsed = 0,
        DateTime? createdAtUtc = null)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        ConversationId = conversationId;
        SenderRole = senderRole ?? "User";
        Content = content ?? string.Empty;
        SourceCategory = sourceCategory;
        TokensUsed = tokensUsed;
        CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow;
    }

    public void AddToolExecution(ToolExecution execution)
    {
        if (execution != null) ToolExecutions.Add(execution);
    }

    public void AddSourceReference(SourceReference reference)
    {
        if (reference != null) SourceReferences.Add(reference);
    }
}
