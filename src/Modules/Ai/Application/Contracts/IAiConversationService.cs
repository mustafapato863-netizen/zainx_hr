using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Workforce.Modules.Ai.Domain;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Ai.Application.Contracts;

public record CreateConversationRequest(
    string? Title = null,
    string? ContextEntityType = null,
    string? ContextEntityId = null
);

public record SendMessageRequest(
    string Prompt
);

public record AiMessageResponseDto(
    Guid MessageId,
    string SenderRole,
    string Content,
    AiSourceCategory SourceCategory,
    int TokensUsed,
    DateTime CreatedAtUtc,
    List<SourceReferenceDto> Sources,
    List<ToolExecutionDto> ToolExecutions
);

public record SourceReferenceDto(
    Guid Id,
    string SourceCategory,
    string Title,
    string? EntityType,
    string? EntityId,
    string? PolicyCode,
    int? PolicyVersion,
    Guid? PayrollRunId,
    string MetadataJson,
    DateTime RetrievedAtUtc
);

public record ToolExecutionDto(
    Guid Id,
    string ToolCode,
    long DurationMs,
    string Status,
    DateTime CreatedAtUtc
);

public record ConversationSummaryDto(
    Guid Id,
    string Title,
    string? ContextEntityType,
    string? ContextEntityId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    int MessageCount
);

public record ConversationDetailDto(
    Guid Id,
    string Title,
    string? ContextEntityType,
    string? ContextEntityId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    List<AiMessageResponseDto> Messages
);

public interface IAiConversationService
{
    Task<ConversationSummaryDto> CreateConversationAsync(
        CreateConversationRequest request, 
        IUserContext userContext, 
        CancellationToken ct = default);

    Task<List<ConversationSummaryDto>> ListConversationsAsync(
        IUserContext userContext, 
        CancellationToken ct = default);

    Task<ConversationDetailDto?> GetConversationAsync(
        Guid conversationId, 
        IUserContext userContext, 
        CancellationToken ct = default);

    Task<AiMessageResponseDto> SendMessageAsync(
        Guid conversationId, 
        SendMessageRequest request, 
        IUserContext userContext, 
        CancellationToken ct = default);
}
