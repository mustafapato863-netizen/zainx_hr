using System;
using System.Threading;
using System.Threading.Tasks;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Leave.Application.Contracts;

public sealed record StartLeaveApprovalWorkflowCommand(
    TenantId TenantId,
    LegalEntityId LegalEntityId,
    Guid ApprovalRequestId,
    Guid LeaveRequestId,
    Guid RequesterUserId,
    Guid RequesterEmploymentId,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal DurationDays,
    string Reason);

public interface ILeaveApprovalWorkflowStarter
{
    Task StartAsync(StartLeaveApprovalWorkflowCommand command, CancellationToken ct = default);
    Task CancelStartedWorkflowAsync(TenantId tenantId, LegalEntityId legalEntityId, Guid approvalRequestId, Guid actorUserId, CancellationToken ct = default);
}
