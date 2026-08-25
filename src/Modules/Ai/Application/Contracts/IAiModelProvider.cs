using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Workforce.Modules.Ai.Domain;

namespace Workforce.Modules.Ai.Application.Contracts;

public record AiModelPromptRequest(
    string SystemInstructions,
    List<Message> ConversationHistory,
    string CurrentUserPrompt,
    List<AiToolDefinition> AvailableTools,
    string? ContextEntityType = null,
    string? ContextEntityId = null
);

public record AiToolInvocationPlan(
    string ToolCode,
    string InputParametersJson
);

public record AiModelResponse(
    string TextResponse,
    int EstimatedTokensUsed,
    AiSourceCategory SourceCategory,
    List<AiToolInvocationPlan>? ToolInvocations = null
);

/// <summary>
/// Pluggable AI Model Provider abstraction (supports local deterministic provider, on-prem models, and cloud LLMs).
/// </summary>
public interface IAiModelProvider
{
    string ProviderCode { get; }
    Task<AiModelResponse> GenerateResponseAsync(
        AiModelPromptRequest request,
        CancellationToken ct = default);
}
