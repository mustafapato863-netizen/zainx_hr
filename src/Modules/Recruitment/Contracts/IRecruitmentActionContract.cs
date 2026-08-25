using System;
using System.Threading;
using System.Threading.Tasks;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Recruitment.Contracts;

public record MoveApplicationStageCommand(
    Guid ApplicationId,
    Guid TargetStageId,
    string? Reason,
    string? IdempotencyKey,
    uint ExpectedRowVersion,
    LegalEntityId? LegalEntityId
);

public record SubmitRequisitionApprovalCommand(
    Guid RequisitionId,
    uint ExpectedRowVersion,
    LegalEntityId? LegalEntityId
);

public record RecruitmentActionResult(
    bool Success,
    Guid EntityId,
    uint NewRowVersion,
    string Message,
    bool IsConcurrencyConflict
);

public interface IRecruitmentActionContract
{
    Task<RecruitmentActionResult> MoveApplicationStageAsync(
        TenantId tenantId,
        UserId actorUserId,
        MoveApplicationStageCommand command,
        CancellationToken ct = default);

    Task<RecruitmentActionResult> SubmitRequisitionApprovalAsync(
        TenantId tenantId,
        UserId actorUserId,
        SubmitRequisitionApprovalCommand command,
        CancellationToken ct = default);
}
