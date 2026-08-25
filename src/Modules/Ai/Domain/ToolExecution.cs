using System;

namespace Workforce.Modules.Ai.Domain;

/// <summary>
/// Audit trail for individual AI tool invocations within a message exchange.
/// </summary>
public sealed class ToolExecution
{
    public Guid Id { get; }
    public Guid MessageId { get; }
    public string ToolCode { get; }
    public string InputPayloadJson { get; }
    public string OutputPayloadJson { get; }
    public long DurationMs { get; }
    public string Status { get; } // Success, Denied, Error
    public DateTime CreatedAtUtc { get; }

    public ToolExecution(
        Guid id,
        Guid messageId,
        string toolCode,
        string inputPayloadJson,
        string outputPayloadJson,
        long durationMs,
        string status = "Success",
        DateTime? createdAtUtc = null)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        MessageId = messageId;
        ToolCode = toolCode ?? throw new ArgumentNullException(nameof(toolCode));
        InputPayloadJson = inputPayloadJson ?? "{}";
        OutputPayloadJson = outputPayloadJson ?? "{}";
        DurationMs = durationMs;
        Status = status ?? "Success";
        CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow;
    }
}
