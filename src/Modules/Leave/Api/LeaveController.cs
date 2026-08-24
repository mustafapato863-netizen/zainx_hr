using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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

    public LeaveController(ILeaveRepository repository, IUserContext userContext)
    {
        _repository = repository;
        _userContext = userContext;
    }

    [HttpGet("types")]
    public async Task<ActionResult<IReadOnlyList<LeaveTypeDto>>> GetLeaveTypes()
    {
        var legalEntityId = _userContext.LegalEntityId ?? new LegalEntityId(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        var types = await _repository.GetLeaveTypesAsync(_userContext.TenantId, legalEntityId);
        return Ok(types);
    }

    [HttpGet("balances")]
    public async Task<ActionResult<IReadOnlyList<LeaveBalanceDto>>> GetLeaveBalances(
        [FromQuery] Guid? employmentId, [FromQuery] int? year)
    {
        var empId = employmentId ?? Guid.Parse("11111111-1111-1111-1111-111111111111");
        var y = year ?? DateTime.UtcNow.Year;
        var balances = await _repository.GetLeaveBalancesAsync(_userContext.TenantId, empId, y);
        return Ok(balances);
    }

    [HttpGet("requests")]
    public async Task<ActionResult<PagedLeaveRequestsResponse>> GetLeaveRequests(
        [FromQuery] Guid? employmentId,
        [FromQuery] int? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var (items, total) = await _repository.GetLeaveRequestsAsync(
            _userContext.TenantId,
            _userContext.LegalEntityId,
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
        var req = await _repository.GetLeaveRequestByIdAsync(_userContext.TenantId, id);
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

        var legalEntityId = _userContext.LegalEntityId ?? new LegalEntityId(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        var request = new LeaveRequest(
            Guid.NewGuid(),
            _userContext.TenantId,
            legalEntityId,
            input.EmploymentId,
            input.LeaveTypeId,
            start,
            end,
            input.DurationDays,
            input.Reason,
            input.AttachmentDocumentId
        );

        // Submit immediately with approval request ID
        var approvalRequestId = Guid.NewGuid();
        request.Submit(approvalRequestId, 1);

        try
        {
            await _repository.SaveLeaveRequestAsync(request);
            return Ok(new { id = request.Id, status = request.Status.ToString(), approvalRequestId });
        }
        catch (Exception ex) when (ex.Message.Contains("ex_leave_request_no_overlap", StringComparison.OrdinalIgnoreCase) ||
                                  ex.InnerException?.Message.Contains("ex_leave_request_no_overlap", StringComparison.OrdinalIgnoreCase) == true)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Leave Request Overlap Conflict",
                Detail = "An approved or pending leave request already exists for this employee within the requested date range.",
                Status = 409
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

        var req = await _repository.GetLeaveRequestEntityByIdAsync(_userContext.TenantId, id);
        if (req == null) return NotFound();

        try
        {
            req.Approve(input.RowVersion);
            await _repository.SaveLeaveRequestAsync(req);
            return Ok(new { status = "Approved", rowVersion = req.RowVersion });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("concurrency", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Optimistic Concurrency Conflict",
                Detail = ex.Message,
                Status = 409
            });
        }
    }

    [HttpPost("requests/{id:guid}/reject")]
    public async Task<IActionResult> RejectRequest(Guid id, [FromBody] LeaveRejectionRequest input)
    {
        if (!_userContext.HasPermission("leave.request.reject") && !_userContext.HasPermission("admin"))
        {
            return Forbid();
        }

        var req = await _repository.GetLeaveRequestEntityByIdAsync(_userContext.TenantId, id);
        if (req == null) return NotFound();

        try
        {
            req.Reject(input.Reason, input.RowVersion);
            await _repository.SaveLeaveRequestAsync(req);
            return Ok(new { status = "Rejected", rowVersion = req.RowVersion });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("concurrency", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Optimistic Concurrency Conflict",
                Detail = ex.Message,
                Status = 409
            });
        }
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

public record PagedLeaveRequestsResponse(
    IReadOnlyList<LeaveRequestDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);
