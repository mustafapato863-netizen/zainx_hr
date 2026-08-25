using System;
using System.Threading;
using System.Threading.Tasks;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Approvals.Application.Contracts;

public enum ApprovalDecisionOutcome
{
    Approved = 1,
    Rejected = 2
}

public sealed record ApprovalDecisionSideEffectCommand(
    TenantId TenantId,
    LegalEntityId LegalEntityId,
    string SourceModule,
    Guid SourceEntityId,
    Guid ApprovalRequestId,
    ApprovalDecisionOutcome Outcome,
    string? Reason,
    Guid ActorUserId);

public sealed record ApprovalCancellationSideEffectCommand(
    TenantId TenantId,
    LegalEntityId LegalEntityId,
    string SourceModule,
    Guid SourceEntityId,
    Guid ApprovalRequestId,
    string? Reason,
    Guid ActorUserId);

public interface IApprovalDecisionSideEffect
{
    Task ApplyAsync(ApprovalDecisionSideEffectCommand command, CancellationToken ct = default);
    Task ApplyCancellationAsync(ApprovalCancellationSideEffectCommand command, CancellationToken ct = default);
}
