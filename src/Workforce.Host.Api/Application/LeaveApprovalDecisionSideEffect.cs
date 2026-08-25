using System;
using System.Threading;
using System.Threading.Tasks;
using Workforce.Modules.Approvals.Application.Contracts;
using Workforce.Modules.Leave.Application.Contracts;

namespace Workforce.Host.Api.Application;

public sealed class LeaveApprovalDecisionSideEffect : IApprovalDecisionSideEffect
{
    private readonly ILeaveRequestApplicationContract _leaveRequests;

    public LeaveApprovalDecisionSideEffect(ILeaveRequestApplicationContract leaveRequests)
    {
        _leaveRequests = leaveRequests;
    }

    public async Task ApplyAsync(ApprovalDecisionSideEffectCommand command, CancellationToken ct = default)
    {
        if (!string.Equals(command.SourceModule, "Leave", StringComparison.OrdinalIgnoreCase))
            return;

        var result = await _leaveRequests.ApplyApprovalDecisionAsync(
            new ApplyLeaveApprovalDecisionCommand(
                command.TenantId,
                command.LegalEntityId,
                command.SourceEntityId,
                command.ApprovalRequestId,
                command.Outcome == ApprovalDecisionOutcome.Approved
                    ? LeaveApprovalDecision.Approved
                    : LeaveApprovalDecision.Rejected,
                command.Reason,
                command.ActorUserId),
            ct);

        if (result == LeaveApprovalApplicationResult.NotFound)
            throw new InvalidOperationException("The Leave approval target no longer exists in the authorized scope.");
    }

    public async Task ApplyCancellationAsync(
        ApprovalCancellationSideEffectCommand command,
        CancellationToken ct = default)
    {
        if (!string.Equals(command.SourceModule, "Leave", StringComparison.OrdinalIgnoreCase))
            return;

        var result = await _leaveRequests.ApplyApprovalCancellationAsync(
            new ApplyLeaveApprovalCancellationCommand(
                command.TenantId,
                command.LegalEntityId,
                command.ApprovalRequestId,
                command.ActorUserId,
                command.Reason),
            ct);

        if (result == LeaveApprovalApplicationResult.NotFound)
            throw new InvalidOperationException("The Leave approval target no longer exists in the authorized scope.");
    }
}
