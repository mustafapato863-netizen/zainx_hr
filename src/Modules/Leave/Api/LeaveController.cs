using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Workforce.Modules.Leave.Application.Contracts;
using Workforce.Modules.Leave.Domain;
using Workforce.Modules.Leave.Infrastructure;
using Workforce.SharedKernel.Primitives;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Leave.Api;

[ApiController]
[Route("api/v1/leave")]
public class LeaveController : ControllerBase
{
    private readonly ILeaveRepository _repository;
    private readonly IUserContext _userContext;
    private readonly ILeaveRequestApplicationContract _leaveRequests;
    private readonly ILeaveActionContract _leaveActions;
    private readonly ILeaveApprovalWorkflowStarter _approvalWorkflowStarter;

    public LeaveController(
        ILeaveRepository repository,
        IUserContext userContext,
        ILeaveRequestApplicationContract leaveRequests,
        ILeaveActionContract leaveActions,
        ILeaveApprovalWorkflowStarter approvalWorkflowStarter)
    {
        _repository = repository;
        _userContext = userContext;
        _leaveRequests = leaveRequests;
        _leaveActions = leaveActions;
        _approvalWorkflowStarter = approvalWorkflowStarter;
    }

    [HttpGet("types")]
    public async Task<ActionResult<IReadOnlyList<LeaveTypeDto>>> GetLeaveTypes()
    {
        if (!HasAnyPermission("leave.type.read", "leave.request.read", "leave.balance.read"))
        {
            return Forbid();
        }

        if (!_userContext.LegalEntityId.HasValue)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Legal Entity Context Required", Detail = "Select an authorized legal entity before reading leave types." });
        }

        var legalEntityId = _userContext.LegalEntityId.Value;
        var types = await _repository.GetLeaveTypesAsync(_userContext.TenantId, legalEntityId);
        return Ok(types);
    }

    [HttpGet("balances")]
    public async Task<ActionResult<IReadOnlyList<LeaveBalanceDto>>> GetLeaveBalances(
        [FromQuery] Guid? employmentId, [FromQuery] int? year)
    {
        if (!HasAnyPermission("leave.balance.read", "leave.request.read"))
        {
            return Forbid();
        }

        if (!employmentId.HasValue || employmentId.Value == Guid.Empty)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Employment Context Required", Detail = "employmentId must be supplied explicitly until employee self-service identity mapping is configured." });
        }

        var empId = employmentId.Value;
        var y = year ?? DateTime.UtcNow.Year;
        if (!_userContext.LegalEntityId.HasValue)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Legal Entity Context Required", Detail = "Select an authorized legal entity before reading leave balances." });
        }

        var balances = await _repository.GetLeaveBalancesAsync(_userContext.TenantId, empId, y, _userContext.LegalEntityId.Value);
        return Ok(balances);
    }

    [HttpGet("requests")]
    public async Task<ActionResult<PagedLeaveRequestsResponse>> GetLeaveRequests(
        [FromQuery] Guid? employmentId,
        [FromQuery] int? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (!HasAnyPermission("leave.request.read", "leave.balance.read", "leave.request.create"))
        {
            return Forbid();
        }

        if (!_userContext.LegalEntityId.HasValue)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Legal Entity Context Required", Detail = "Select an authorized legal entity before reading leave requests." });
        }

        var (items, total) = await _repository.GetLeaveRequestsAsync(
            _userContext.TenantId,
            _userContext.LegalEntityId.Value,
            employmentId,
            status,
            page,
            pageSize
        );

        return Ok(new PagedLeaveRequestsResponse(items, total, page, pageSize));
    }

    [HttpGet("requests/{id:guid}")]
    public async Task<ActionResult<LeaveRequestDto>> GetLeaveRequestById(Guid id)
    {
        if (!HasAnyPermission("leave.request.read", "leave.request.approve", "leave.request.reject"))
        {
            return Forbid();
        }

        if (!_userContext.LegalEntityId.HasValue)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Legal Entity Context Required", Detail = "Select an authorized legal entity before reading a leave request." });
        }

        var req = await _repository.GetLeaveRequestByIdAsync(_userContext.TenantId, id, _userContext.LegalEntityId.Value);
        if (req == null) return NotFound();
        return Ok(req);
    }

    [HttpPost("requests")]
    public async Task<IActionResult> CreateLeaveRequest([FromBody] CreateLeaveRequestInput input)
    {
        if (!_userContext.HasPermission("leave.request.create") && !_userContext.HasPermission("admin"))
        {
            return Forbid();
        }

        if (!DateOnly.TryParse(input.StartDate, out var start) || !DateOnly.TryParse(input.EndDate, out var end))
        {
            return BadRequest(new ProblemDetails { Title = "Invalid Date Format", Detail = "Expected yyyy-MM-dd." });
        }

        if (end < start)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid Date Range", Detail = "End date cannot be earlier than start date." });
        }

        if (!_userContext.LegalEntityId.HasValue)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Legal Entity Context Required", Detail = "A legal entity context is required before creating a leave request." });
        }

        var legalEntityId = _userContext.LegalEntityId.Value;
        var leaveRequestId = Guid.NewGuid();
        var approvalRequestId = Guid.NewGuid();
        var durationDays = end.DayNumber - start.DayNumber + 1;

        try
        {
            await _approvalWorkflowStarter.StartAsync(
                new StartLeaveApprovalWorkflowCommand(
                    _userContext.TenantId,
                    legalEntityId,
                    approvalRequestId,
                    leaveRequestId,
                    _userContext.UserId.Value,
                    input.EmploymentId,
                    start,
                    end,
                    durationDays,
                    input.Reason));

            var result = await _leaveRequests.SubmitAsync(
                new SubmitLeaveRequestCommand(
                    _userContext.TenantId,
                    legalEntityId,
                    leaveRequestId,
                    input.EmploymentId,
                    input.LeaveTypeId,
                    start,
                    end,
                    input.Reason,
                    approvalRequestId,
                    input.AttachmentDocumentId,
                    _userContext.UserId.Value));
            return Ok(new { id = result.RequestId, status = result.Status, approvalRequestId = result.ApprovalRequestId, rowVersion = result.RowVersion });
        }
        catch (Exception ex) when (ex.Message.Contains("ex_leave_request_no_overlap", StringComparison.OrdinalIgnoreCase) ||
                                   ex.InnerException?.Message.Contains("ex_leave_request_no_overlap", StringComparison.OrdinalIgnoreCase) == true)
        {
            try
            {
                await _approvalWorkflowStarter.CancelStartedWorkflowAsync(
                    _userContext.TenantId,
                    legalEntityId,
                    approvalRequestId,
                    _userContext.UserId.Value);
            }
            catch
            {
                // Preserve the overlap response. The workflow remains auditable for reconciliation.
            }

            return Conflict(new ProblemDetails
            {
                Title = "Leave Request Overlap Conflict",
                Detail = "An approved or pending leave request already exists for this employee within the requested date range.",
                Status = 409
            });
        }
        catch (InvalidOperationException ex)
        {
            try
            {
                await _approvalWorkflowStarter.CancelStartedWorkflowAsync(
                    _userContext.TenantId,
                    legalEntityId,
                    approvalRequestId,
                    _userContext.UserId.Value);
            }
            catch
            {
                // Preserve the original failure. A cancelled-workflow reconciliation is auditable in Approvals.
            }

            return Conflict(new ProblemDetails
            {
                Title = "Leave Request Cannot Be Submitted",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    [HttpPost("requests/{id:guid}/approve")]
    public async Task<IActionResult> ApproveRequest(Guid id, [FromBody] LeaveDecisionRequest input)
    {
        if (!_userContext.HasPermission("leave.request.approve") && !_userContext.HasPermission("admin"))
        {
            return Forbid();
        }

        if (!_userContext.LegalEntityId.HasValue)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Legal Entity Context Required", Detail = "Select an authorized legal entity before approving a leave request." });
        }

        return Conflict(new ProblemDetails
        {
            Title = "Universal Approval Required",
            Detail = "Leave decisions must be performed through the linked Universal Approval request.",
            Status = StatusCodes.Status409Conflict
        });
    }

    [HttpPost("requests/{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelRequest(Guid id, [FromBody] LeaveCancellationRequest input, CancellationToken ct)
    {
        if (!_userContext.HasPermission("leave.request.cancel") && !_userContext.HasPermission("admin"))
        {
            return Forbid();
        }

        if (!_userContext.LegalEntityId.HasValue)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Legal Entity Context Required",
                Detail = "Select an authorized legal entity before cancelling a leave request."
            });
        }

        var result = await _leaveActions.CancelLeaveRequestAsync(
            _userContext.TenantId,
            _userContext.UserId,
            new CancelLeaveRequestCommand(id, input.RowVersion, _userContext.LegalEntityId.Value),
            ct);

        if (result.IsConcurrencyConflict)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Optimistic Concurrency Conflict",
                Detail = result.Message
            });
        }

        if (!result.Success)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Leave Request Cannot Be Cancelled",
                Detail = result.Message
            });
        }

        return Ok(new { id = result.LeaveRequestId, status = "Cancelled", rowVersion = result.NewRowVersion });
    }

    [HttpPost("requests/{id:guid}/reject")]
    public async Task<IActionResult> RejectRequest(Guid id, [FromBody] LeaveRejectionRequest input)
    {
        if (!_userContext.HasPermission("leave.request.reject") && !_userContext.HasPermission("admin"))
        {
            return Forbid();
        }

        if (!_userContext.LegalEntityId.HasValue)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Legal Entity Context Required", Detail = "Select an authorized legal entity before rejecting a leave request." });
        }

        return Conflict(new ProblemDetails
        {
            Title = "Universal Approval Required",
            Detail = "Leave decisions must be performed through the linked Universal Approval request.",
            Status = StatusCodes.Status409Conflict
        });
    }

    private bool HasAnyPermission(params string[] permissions)
    {
        if (_userContext.HasPermission("admin")) return true;
        return permissions.Any(_userContext.HasPermission);
    }
}

public record CreateLeaveRequestInput(
    Guid EmploymentId,
    Guid LeaveTypeId,
    string StartDate,
    string EndDate,
    decimal DurationDays,
    string Reason,
    Guid? AttachmentDocumentId = null
);

public record LeaveDecisionRequest(
    uint RowVersion
);

public record LeaveRejectionRequest(
    string Reason,
    uint RowVersion
);

public record LeaveCancellationRequest(
    uint RowVersion
);

    public record PagedLeaveRequestsResponse(
    IReadOnlyList<LeaveRequestDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);
