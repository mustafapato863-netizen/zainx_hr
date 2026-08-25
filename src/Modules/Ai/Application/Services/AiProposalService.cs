using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Workforce.Modules.Ai.Application.Contracts;
using Workforce.Modules.Ai.Domain;
using Workforce.Modules.Ai.Infrastructure;
using Workforce.Modules.Audit.Domain;
using Workforce.Modules.Audit.Infrastructure;
using Workforce.SharedKernel.Primitives;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Ai.Application.Services;

public class AiProposalService : IAiProposalService
{
    private readonly IAiRepository _aiRepository;
    private readonly AiActionRegistry _actionRegistry;
    private readonly IAuditRepository _auditRepository;

    public AiProposalService(
        IAiRepository aiRepository,
        AiActionRegistry actionRegistry,
        IAuditRepository auditRepository)
    {
        _aiRepository = aiRepository ?? throw new ArgumentNullException(nameof(aiRepository));
        _actionRegistry = actionRegistry ?? throw new ArgumentNullException(nameof(actionRegistry));
        _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
    }

    public async Task<AiActionProposalDto> CreateProposalAsync(
        CreateProposalRequest request,
        IUserContext userContext,
        CancellationToken ct = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.ActionCode)) throw new ArgumentException("ActionCode is required.", nameof(request));

        var handler = _actionRegistry.GetActionHandler(request.ActionCode);
        if (handler == null)
        {
            throw new InvalidOperationException($"Action '{request.ActionCode}' is not supported or registered.");
        }

        // Authorization check at proposal time
        var requiredPermission = handler.Definition.RequiredPermission;
        if (!userContext.HasPermission(requiredPermission) && !userContext.HasPermission("admin"))
        {
            throw new UnauthorizedAccessException($"Caller lacks required permission '{requiredPermission}'.");
        }

        var validity = TimeSpan.FromMinutes(request.ValidityMinutes > 0 ? request.ValidityMinutes : 15);
        var convId = (request.ConversationId.HasValue && request.ConversationId.Value != Guid.Empty) ? request.ConversationId.Value : Guid.NewGuid();

        var existingConv = await _aiRepository.GetConversationByIdAsync(userContext.TenantId, convId, ct);
        if (existingConv == null)
        {
            var fallbackConv = new Conversation(
                convId,
                userContext.TenantId,
                userContext.LegalEntityId,
                userContext.UserId,
                $"Action: {handler.Definition.ActionCode}",
                request.TargetEntityType,
                request.TargetEntityId
            );
            await _aiRepository.CreateConversationAsync(fallbackConv, ct);
        }

        var proposal = new AiActionProposal(
            Guid.NewGuid(),
            convId,
            userContext.TenantId,
            userContext.LegalEntityId,
            userContext.UserId,
            request.ActionCode,
            request.TargetEntityType,
            request.TargetEntityId,
            request.ExpectedRowVersion,
            request.EffectiveDateUtc,
            request.BeforeSnapshotJson ?? "{}",
            request.AfterSnapshotJson ?? "{}",
            request.ImpactSummaryJson ?? "{}",
            requiredPermission,
            validity
        );

        // Persist proposal (zero mutation on target business domain)
        await _aiRepository.CreateProposalAsync(proposal, ct);

        // Audit proposal creation
        await _auditRepository.RecordAsync(new AuditRecord(
            Guid.NewGuid(),
            userContext.TenantId,
            userContext.LegalEntityId,
            userContext.UserId.Value,
            "User",
            "ai.proposal.created",
            request.TargetEntityType,
            request.TargetEntityId,
            DateTime.UtcNow,
            null,
            null,
            null,
            "AiProposalCreated",
            proposal.BeforeSnapshotJson,
            proposal.AfterSnapshotJson,
            JsonSerializer.Serialize(new
            {
                proposalId = proposal.Id,
                actionCode = proposal.ActionCode,
                conversationId = proposal.ConversationId,
                initiatedVia = "AI"
            }),
            "Internal"
        ), ct);

        return MapDto(proposal);
    }

    public async Task<AiActionProposalDto?> GetProposalAsync(
        Guid proposalId,
        IUserContext userContext,
        CancellationToken ct = default)
    {
        var proposal = await _aiRepository.GetProposalByIdAsync(userContext.TenantId, proposalId, ct);
        return proposal == null ? null : MapDto(proposal);
    }

    public async Task<IReadOnlyList<AiActionProposalDto>> ListProposalsAsync(
        IUserContext userContext,
        int limit = 50,
        CancellationToken ct = default)
    {
        var list = await _aiRepository.ListProposalsAsync(userContext.TenantId, userContext.UserId, limit, ct);
        return list.Select(MapDto).ToList();
    }

    public async Task<AiProposalExecutionResponseDto> ConfirmProposalAsync(
        Guid proposalId,
        ConfirmProposalRequest request,
        IUserContext userContext,
        CancellationToken ct = default)
    {
        var proposal = await _aiRepository.GetProposalByIdAsync(userContext.TenantId, proposalId, ct);
        if (proposal == null)
        {
            throw new KeyNotFoundException($"Proposal '{proposalId}' not found in current tenant scope.");
        }

        // Idempotency: Check if already completed
        var existingExecution = await _aiRepository.GetExecutionByIdempotencyKeyAsync(userContext.TenantId, proposal.IdempotencyKey, ct);
        if (existingExecution != null)
        {
            return new AiProposalExecutionResponseDto(
                proposal.Id,
                proposal.ActionCode,
                existingExecution.Status,
                existingExecution.Status == "Completed",
                existingExecution.ResultPayloadJson,
                null,
                null,
                existingExecution.ExecutedAtUtc
            );
        }

        // Reauthorization at execution time
        if (!userContext.HasPermission(proposal.RequiredPermission) && !userContext.HasPermission("admin"))
        {
            throw new UnauthorizedAccessException($"Execution denied: Caller lacks current permission '{proposal.RequiredPermission}'.");
        }

        // Verify Hash Integrity (Tamper Detection)
        if (!proposal.VerifyHash(proposal.ProposalHash))
        {
            throw new InvalidOperationException("Proposal integrity violation: snapshot or target parameters were tampered with.");
        }

        // Expiry check
        if (proposal.IsExpired(DateTime.UtcNow))
        {
            proposal.MarkExpired();
            await _aiRepository.UpdateProposalAsync(proposal, ct);

            await _auditRepository.RecordAsync(new AuditRecord(
                Guid.NewGuid(),
                userContext.TenantId,
                userContext.LegalEntityId,
                userContext.UserId.Value,
                "User",
                "ai.proposal.expired",
                proposal.TargetEntityType,
                proposal.TargetEntityId,
                DateTime.UtcNow,
                null,
                null,
                null,
                "ProposalExpired",
                null,
                null,
                JsonSerializer.Serialize(new { proposalId = proposal.Id, actionCode = proposal.ActionCode }),
                "Internal"
            ), ct);

            throw new InvalidOperationException("Proposal has expired and cannot be confirmed.");
        }

        if (proposal.Status != ProposalStatus.ReadyForConfirmation)
        {
            throw new InvalidOperationException($"Proposal is in '{proposal.Status}' status and cannot be confirmed.");
        }

        var handler = _actionRegistry.GetActionHandler(proposal.ActionCode);
        if (handler == null)
        {
            throw new InvalidOperationException($"No handler registered for action '{proposal.ActionCode}'.");
        }

        proposal.MarkConfirmed();
        proposal.MarkExecuting();
        await _aiRepository.UpdateProposalAsync(proposal, ct);

        var sw = Stopwatch.StartNew();
        var result = await handler.ExecuteActionAsync(proposal, userContext, ct);
        sw.Stop();

        if (result.IsConcurrencyConflict)
        {
            proposal.MarkStale(result.ErrorMessage ?? "Target entity state changed since proposal creation.");
            await _aiRepository.UpdateProposalAsync(proposal, ct);

            await _auditRepository.RecordAsync(new AuditRecord(
                Guid.NewGuid(),
                userContext.TenantId,
                userContext.LegalEntityId,
                userContext.UserId.Value,
                "User",
                "ai.proposal.stale",
                proposal.TargetEntityType,
                proposal.TargetEntityId,
                DateTime.UtcNow,
                null,
                null,
                null,
                "ConcurrencyConflict",
                null,
                null,
                JsonSerializer.Serialize(new { proposalId = proposal.Id, actionCode = proposal.ActionCode, error = result.ErrorMessage }),
                "Internal"
            ), ct);

            return new AiProposalExecutionResponseDto(
                proposal.Id,
                proposal.ActionCode,
                "Stale",
                false,
                "{}",
                result.ErrorMessage,
                null,
                DateTime.UtcNow
            );
        }

        if (!result.Success)
        {
            proposal.MarkFailed(result.ErrorMessage ?? "Execution failed.");
            await _aiRepository.UpdateProposalAsync(proposal, ct);

            await _auditRepository.RecordAsync(new AuditRecord(
                Guid.NewGuid(),
                userContext.TenantId,
                userContext.LegalEntityId,
                userContext.UserId.Value,
                "User",
                "ai.action.failed",
                proposal.TargetEntityType,
                proposal.TargetEntityId,
                DateTime.UtcNow,
                null,
                null,
                null,
                "ExecutionFailed",
                null,
                null,
                JsonSerializer.Serialize(new { proposalId = proposal.Id, actionCode = proposal.ActionCode, error = result.ErrorMessage }),
                "Internal"
            ), ct);

            return new AiProposalExecutionResponseDto(
                proposal.Id,
                proposal.ActionCode,
                "Failed",
                false,
                "{}",
                result.ErrorMessage,
                null,
                DateTime.UtcNow
            );
        }

        proposal.MarkCompleted();
        await _aiRepository.UpdateProposalAsync(proposal, ct);

        // Record execution record
        var execution = new AiActionExecution(
            Guid.NewGuid(),
            proposal.Id,
            userContext.TenantId,
            userContext.UserId,
            proposal.ActionCode,
            proposal.IdempotencyKey,
            "Completed",
            result.ResultPayloadJson,
            sw.ElapsedMilliseconds
        );
        await _aiRepository.RecordActionExecutionAsync(execution, ct);

        // Audit confirmed execution
        await _auditRepository.RecordAsync(new AuditRecord(
            Guid.NewGuid(),
            userContext.TenantId,
            userContext.LegalEntityId,
            userContext.UserId.Value,
            "User",
            "ai.action.executed",
            proposal.TargetEntityType,
            proposal.TargetEntityId,
            DateTime.UtcNow,
            null,
            null,
            null,
            "AiProposalConfirmedAndExecuted",
            proposal.BeforeSnapshotJson,
            proposal.AfterSnapshotJson,
            JsonSerializer.Serialize(new
            {
                proposalId = proposal.Id,
                actionCode = proposal.ActionCode,
                durationMs = sw.ElapsedMilliseconds,
                initiatedVia = "AI"
            }),
            "Internal"
        ), ct);

        return new AiProposalExecutionResponseDto(
            proposal.Id,
            proposal.ActionCode,
            "Completed",
            true,
            result.ResultPayloadJson,
            null,
            null,
            DateTime.UtcNow
        );
    }

    public async Task<AiActionProposalDto> CancelProposalAsync(
        Guid proposalId,
        CancelProposalRequest request,
        IUserContext userContext,
        CancellationToken ct = default)
    {
        var proposal = await _aiRepository.GetProposalByIdAsync(userContext.TenantId, proposalId, ct);
        if (proposal == null)
        {
            throw new KeyNotFoundException($"Proposal '{proposalId}' not found in current tenant scope.");
        }

        proposal.MarkCancelled();
        await _aiRepository.UpdateProposalAsync(proposal, ct);

        await _auditRepository.RecordAsync(new AuditRecord(
            Guid.NewGuid(),
            userContext.TenantId,
            userContext.LegalEntityId,
            userContext.UserId.Value,
            "User",
            "ai.proposal.cancelled",
            proposal.TargetEntityType,
            proposal.TargetEntityId,
            DateTime.UtcNow,
            null,
            null,
            null,
            "AiProposalCancelled",
            null,
            null,
            JsonSerializer.Serialize(new
            {
                proposalId = proposal.Id,
                actionCode = proposal.ActionCode,
                reason = request?.Reason ?? "User cancelled"
            }),
            "Internal"
        ), ct);

        return MapDto(proposal);
    }

    private static AiActionProposalDto MapDto(AiActionProposal p)
    {
        return new AiActionProposalDto(
            p.Id,
            p.ConversationId,
            p.ActionCode,
            p.TargetEntityType,
            p.TargetEntityId,
            p.Status.ToString(),
            p.ExpectedRowVersion,
            p.EffectiveDateUtc,
            p.BeforeSnapshotJson,
            p.AfterSnapshotJson,
            p.ImpactSummaryJson,
            p.RequiredPermission,
            p.ProposalHash,
            p.CreatedAtUtc,
            p.ExpiresAtUtc,
            p.ConfirmedAtUtc,
            p.CompletedAtUtc,
            p.ErrorMessage
        );
    }
}
