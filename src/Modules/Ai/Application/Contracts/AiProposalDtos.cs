using System;
using System.Text.Json.Serialization;
using Workforce.Modules.Ai.Domain;

namespace Workforce.Modules.Ai.Application.Contracts;

public record CreateProposalRequest(
    string ActionCode,
    string TargetEntityType,
    string TargetEntityId,
    uint ExpectedRowVersion,
    DateTime? EffectiveDateUtc,
    string? BeforeSnapshotJson,
    string? AfterSnapshotJson,
    string? ImpactSummaryJson,
    Guid? ConversationId = null,
    int ValidityMinutes = 15
);

public record ConfirmProposalRequest(
    string? Reason
);

public record CancelProposalRequest(
    string? Reason
);

public record AiActionProposalDto(
    Guid Id,
    Guid ConversationId,
    string ActionCode,
    string TargetEntityType,
    string TargetEntityId,
    string Status,
    uint ExpectedRowVersion,
    DateTime? EffectiveDateUtc,
    string BeforeSnapshotJson,
    string AfterSnapshotJson,
    string ImpactSummaryJson,
    string RequiredPermission,
    string ProposalHash,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc,
    DateTime? ConfirmedAtUtc,
    DateTime? CompletedAtUtc,
    string? ErrorMessage
);

public record AiProposalExecutionResponseDto(
    Guid ProposalId,
    string ActionCode,
    string Status,
    bool Success,
    string ResultPayloadJson,
    string? ErrorMessage,
    uint? NewRowVersion,
    DateTime ExecutedAtUtc
);
