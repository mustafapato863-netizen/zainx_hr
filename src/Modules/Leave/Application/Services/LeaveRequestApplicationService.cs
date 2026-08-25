using System;
using System.Threading;
using System.Threading.Tasks;
using Workforce.Modules.Leave.Application.Contracts;
using Workforce.Modules.Leave.Domain;
using Workforce.Modules.Leave.Infrastructure;

namespace Workforce.Modules.Leave.Application.Services;

public sealed class LeaveRequestApplicationService : ILeaveRequestApplicationContract
{
    private readonly ILeaveRepository _repository;

    public LeaveRequestApplicationService(ILeaveRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<LeaveSubmissionResult> SubmitAsync(
        SubmitLeaveRequestCommand command,
        CancellationToken ct = default)
    {
        if (command.ApprovalRequestId == Guid.Empty)
            throw new ArgumentException("ApprovalRequestId is required.", nameof(command));
        if (command.RequestId == Guid.Empty)
            throw new ArgumentException("RequestId is required.", nameof(command));
        var durationDays = command.EndDate.DayNumber - command.StartDate.DayNumber + 1;
        var request = new LeaveRequest(
            command.RequestId,
            command.TenantId,
            command.LegalEntityId,
            command.EmploymentId,
            command.LeaveTypeId,
            command.StartDate,
            command.EndDate,
            durationDays,
            command.Reason,
            command.AttachmentDocumentId);

        request.Submit(command.ApprovalRequestId, request.RowVersion);
        await _repository.SaveSubmittedLeaveRequestAsync(request, command.ActorUserId, ct);

        return new LeaveSubmissionResult(
            request.Id,
            command.ApprovalRequestId,
            request.Status.ToString(),
            request.RowVersion);
    }

    public Task<LeaveApprovalApplicationResult> ApplyApprovalDecisionAsync(
        ApplyLeaveApprovalDecisionCommand command,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return _repository.ApplyApprovalDecisionAsync(command, ct);
    }

    public Task<LeaveApprovalApplicationResult> ApplyApprovalCancellationAsync(
        ApplyLeaveApprovalCancellationCommand command,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return _repository.ApplyApprovalCancellationAsync(command, ct);
    }
}
