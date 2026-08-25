using System;
using System.Threading;
using System.Threading.Tasks;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Leave.Application.Contracts;

public enum LeaveApprovalDecision
{
    Approved = 1,
    Rejected = 2
}

public enum LeaveApprovalApplicationResult
{
    Applied = 1,
    AlreadyApplied = 2,
    NotFound = 3
}

public sealed record SubmitLeaveRequestCommand(
    TenantId TenantId,
    LegalEntityId LegalEntityId,
    Guid RequestId,
    Guid EmploymentId,
    Guid LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason,
    Guid ApprovalRequestId,
    Guid? AttachmentDocumentId = null,
    Guid? ActorUserId = null);

public sealed record LeaveSubmissionResult(
    Guid RequestId,
    Guid ApprovalRequestId,
    string Status,
    uint RowVersion);

public sealed record ApplyLeaveApprovalDecisionCommand(
    TenantId TenantId,
    LegalEntityId LegalEntityId,
    Guid RequestId,
    Guid ApprovalRequestId,
    LeaveApprovalDecision Decision,
    string? Reason,
    Guid ActorUserId);

public sealed record ApplyLeaveApprovalCancellationCommand(
    TenantId TenantId,
    LegalEntityId LegalEntityId,
    Guid ApprovalRequestId,
    Guid ActorUserId,
    string? Reason);

public interface ILeaveRequestApplicationContract
{
    Task<LeaveSubmissionResult> SubmitAsync(
        SubmitLeaveRequestCommand command,
        CancellationToken ct = default);

    Task<LeaveApprovalApplicationResult> ApplyApprovalDecisionAsync(
        ApplyLeaveApprovalDecisionCommand command,
        CancellationToken ct = default);

    Task<LeaveApprovalApplicationResult> ApplyApprovalCancellationAsync(
        ApplyLeaveApprovalCancellationCommand command,
        CancellationToken ct = default);
}
