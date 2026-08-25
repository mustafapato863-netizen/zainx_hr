using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Workforce.Modules.Approvals.Domain;
using Workforce.Modules.Approvals.Infrastructure;
using Workforce.Modules.Leave.Application.Contracts;
using Workforce.Modules.People.Infrastructure;

namespace Workforce.Host.Api.Application;

public sealed class LeaveApprovalWorkflowStarter : ILeaveApprovalWorkflowStarter
{
    private readonly IApprovalsRepository _approvalsRepository;
    private readonly PeopleRepository _peopleRepository;

    public LeaveApprovalWorkflowStarter(
        IApprovalsRepository approvalsRepository,
        PeopleRepository peopleRepository)
    {
        _approvalsRepository = approvalsRepository;
        _peopleRepository = peopleRepository;
    }

    public async Task StartAsync(StartLeaveApprovalWorkflowCommand command, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var managerUserId = await _peopleRepository.GetManagerUserIdAsync(
            command.TenantId,
            command.LegalEntityId,
            command.RequesterEmploymentId,
            ct);
        if (!managerUserId.HasValue)
        {
            throw new InvalidOperationException(
                "Leave submission requires an active manager employment assignment with an explicitly linked approver identity.");
        }

        var request = new ApprovalRequest(
            command.ApprovalRequestId,
            command.TenantId,
            command.LegalEntityId,
            "Leave",
            command.LeaveRequestId,
            "LeaveRequest",
            "Leave request awaiting manager approval",
            command.RequesterUserId,
            command.RequesterEmploymentId,
            JsonSerializer.Serialize(new
            {
                command.LeaveRequestId,
                command.StartDate,
                command.EndDate,
                command.DurationDays,
                command.Reason
            }));
        var step = new ApprovalStep(
            Guid.NewGuid(),
            command.ApprovalRequestId,
            1,
            managerUserId.Value,
            "DirectManager");

        await _approvalsRepository.CreateApprovalWorkflowAsync(request, new[] { step });
    }

    public async Task CancelStartedWorkflowAsync(
        Workforce.SharedKernel.Primitives.TenantId tenantId,
        Workforce.SharedKernel.Primitives.LegalEntityId legalEntityId,
        Guid approvalRequestId,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var request = await _approvalsRepository.GetApprovalRequestEntityByIdAsync(
            tenantId,
            approvalRequestId,
            legalEntityId);
        if (request == null || request.Status != ApprovalStatus.Pending)
            return;

        request.Cancel(actorUserId, request.RowVersion);
        await _approvalsRepository.SaveApprovalRequestAsync(request);
    }
}
