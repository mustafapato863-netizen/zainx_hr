using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Workforce.Modules.Approvals.Domain;
using Workforce.Modules.Approvals.Infrastructure;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Approvals.Api;

[ApiController]
[Route("api/v1/approvals")]
public class ApprovalsController : ControllerBase
{
    private readonly IApprovalsRepository _repository;
    private readonly IUserContext _userContext;

    public ApprovalsController(IApprovalsRepository repository, IUserContext userContext)
    {
        _repository = repository;
        _userContext = userContext;
    }

    [HttpGet("inbox")]
    public async Task<ActionResult<PagedApprovalInboxResponse>> GetInbox(
        [FromQuery] int? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var (items, total) = await _repository.GetApprovalInboxAsync(
            _userContext.TenantId,
            _userContext.UserId.Value,
            status,
            page,
            pageSize
        );

        return Ok(new PagedApprovalInboxResponse(items, total, page, pageSize));
    }

    [HttpGet("requests/{id:guid}")]
    public async Task<ActionResult<ApprovalRequestDetailDto>> GetRequestDetail(Guid id)
    {
        var req = await _repository.GetApprovalRequestByIdAsync(_userContext.TenantId, id);
        if (req == null) return NotFound();
        return Ok(req);
    }

    [HttpPost("requests/{id:guid}/approve")]
    public async Task<IActionResult> ApproveRequest(Guid id, [FromBody] ApprovalDecisionInput input)
    {
        if (!_userContext.HasPermission("approvals.decision.approve") && !_userContext.HasPermission("admin"))
        {
            return Forbid();
        }

        var req = await _repository.GetApprovalRequestEntityByIdAsync(_userContext.TenantId, id);
        if (req == null) return NotFound();

        try
        {
            req.ApproveCurrentStep(_userContext.UserId.Value, input.Notes, input.RowVersion);
            await _repository.SaveApprovalRequestAsync(req);
            return Ok(new { status = req.Status.ToString(), rowVersion = req.RowVersion });
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
    public async Task<IActionResult> RejectRequest(Guid id, [FromBody] ApprovalRejectionInput input)
    {
        if (!_userContext.HasPermission("approvals.decision.reject") && !_userContext.HasPermission("admin"))
        {
            return Forbid();
        }

        var req = await _repository.GetApprovalRequestEntityByIdAsync(_userContext.TenantId, id);
        if (req == null) return NotFound();

        try
        {
            req.RejectCurrentStep(_userContext.UserId.Value, input.Reason, input.RowVersion);
            await _repository.SaveApprovalRequestAsync(req);
            return Ok(new { status = req.Status.ToString(), rowVersion = req.RowVersion });
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

    [HttpPost("requests/{id:guid}/cancel")]
    public async Task<IActionResult> CancelRequest(Guid id, [FromBody] ApprovalCancellationInput input)
    {
        var req = await _repository.GetApprovalRequestEntityByIdAsync(_userContext.TenantId, id);
        if (req == null) return NotFound();

        try
        {
            req.Cancel(_userContext.UserId.Value, input.RowVersion);
            await _repository.SaveApprovalRequestAsync(req);
            return Ok(new { status = req.Status.ToString(), rowVersion = req.RowVersion });
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

public record ApprovalDecisionInput(
    string? Notes,
    uint RowVersion
);

public record ApprovalRejectionInput(
    string Reason,
    uint RowVersion
);

public record ApprovalCancellationInput(
    uint RowVersion
);

public record PagedApprovalInboxResponse(
    IReadOnlyList<ApprovalInboxItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);
