using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Workforce.Modules.Approvals.Application.Contracts;
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
    private readonly IApprovalDecisionSideEffect? _decisionSideEffect;

    public ApprovalsController(
        IApprovalsRepository repository,
        IUserContext userContext,
        IApprovalDecisionSideEffect? decisionSideEffect = null)
    {
        _repository = repository;
        _userContext = userContext;
        _decisionSideEffect = decisionSideEffect;
    }

    [HttpGet("inbox")]
    public async Task<ActionResult<PagedApprovalInboxResponse>> GetInbox(
        [FromQuery] int? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (!HasAnyPermission("approvals.inbox.read", "approvals.decision.approve", "approvals.decision.reject"))
        {
            return Forbid();
        }

        if (!_userContext.LegalEntityId.HasValue)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Legal Entity Context Required", Detail = "Select an authorized legal entity before reading the approval inbox." });
        }

        var (items, total) = await _repository.GetApprovalInboxAsync(
            _userContext.TenantId,
            _userContext.LegalEntityId.Value,
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
        if (!HasAnyPermission("approvals.inbox.read", "approvals.decision.approve", "approvals.decision.reject"))
        {
            return Forbid();
        }

        if (!_userContext.LegalEntityId.HasValue)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Legal Entity Context Required", Detail = "Select an authorized legal entity before reading an approval request." });
        }

        var req = await _repository.GetApprovalRequestByIdAsync(_userContext.TenantId, id, _userContext.LegalEntityId.Value);
        if (req == null) return NotFound();
        return Ok(req);
    }

    [HttpPost("requests/{id:guid}/approve")]
    public async Task<IActionResult> ApproveRequest(Guid id, [FromBody] ApprovalDecisionInput input)
    {
        if (!HasAnyPermission("approvals.action.execute", "approvals.decision.approve"))
        {
            return Forbid();
        }

        if (!_userContext.LegalEntityId.HasValue)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Legal Entity Context Required", Detail = "Select an authorized legal entity before approving an approval request." });
        }

        var req = await _repository.GetApprovalRequestEntityByIdAsync(_userContext.TenantId, id, _userContext.LegalEntityId.Value);
        if (req == null) return NotFound();

        if (!_userContext.HasPermission("admin") && !await _repository.IsCurrentApproverAsync(
                _userContext.TenantId,
                _userContext.LegalEntityId.Value,
                id,
                _userContext.UserId.Value))
        {
            return Forbid();
        }

        try
        {
            req.ApproveCurrentStep(_userContext.UserId.Value, input.Notes, input.RowVersion);
            if (req.Status == ApprovalStatus.Approved && _decisionSideEffect != null)
            {
                await _decisionSideEffect.ApplyAsync(
                    new ApprovalDecisionSideEffectCommand(
                        req.TenantId,
                        req.LegalEntityId,
                        req.SourceModule,
                        req.SourceEntityId,
                        req.Id,
                        ApprovalDecisionOutcome.Approved,
                        input.Notes,
                        _userContext.UserId.Value));
            }
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
        if (!HasAnyPermission("approvals.action.execute", "approvals.decision.reject"))
        {
            return Forbid();
        }

        if (!_userContext.LegalEntityId.HasValue)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Legal Entity Context Required", Detail = "Select an authorized legal entity before rejecting an approval request." });
        }

        var req = await _repository.GetApprovalRequestEntityByIdAsync(_userContext.TenantId, id, _userContext.LegalEntityId.Value);
        if (req == null) return NotFound();

        if (!_userContext.HasPermission("admin") && !await _repository.IsCurrentApproverAsync(
                _userContext.TenantId,
                _userContext.LegalEntityId.Value,
                id,
                _userContext.UserId.Value))
        {
            return Forbid();
        }

        try
        {
            req.RejectCurrentStep(_userContext.UserId.Value, input.Reason, input.RowVersion);
            if (_decisionSideEffect != null)
            {
                await _decisionSideEffect.ApplyAsync(
                    new ApprovalDecisionSideEffectCommand(
                        req.TenantId,
                        req.LegalEntityId,
                        req.SourceModule,
                        req.SourceEntityId,
                        req.Id,
                        ApprovalDecisionOutcome.Rejected,
                        input.Reason,
                        _userContext.UserId.Value));
            }
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
        var hasApprovalCancellationPermission = HasAnyPermission("approvals.action.execute", "approvals.decision.cancel");
        var hasSelfLeaveCancellationPermission = _userContext.HasPermission("self.leave.cancel");
        if (!hasApprovalCancellationPermission && !hasSelfLeaveCancellationPermission)
        {
            return Forbid();
        }

        if (!_userContext.LegalEntityId.HasValue)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Legal Entity Context Required", Detail = "Select an authorized legal entity before cancelling an approval request." });
        }

        var req = await _repository.GetApprovalRequestEntityByIdAsync(_userContext.TenantId, id, _userContext.LegalEntityId.Value);
        if (req == null) return NotFound();
        if (!hasApprovalCancellationPermission &&
            (!hasSelfLeaveCancellationPermission || !string.Equals(req.SourceModule, "Leave", StringComparison.OrdinalIgnoreCase)))
        {
            return Forbid();
        }
        if (req.RequesterUserId != _userContext.UserId.Value && !_userContext.HasPermission("admin"))
        {
            return Forbid();
        }

        try
        {
            req.Cancel(_userContext.UserId.Value, input.RowVersion);
            if (_decisionSideEffect != null)
            {
                await _decisionSideEffect.ApplyCancellationAsync(
                    new ApprovalCancellationSideEffectCommand(
                        req.TenantId,
                        req.LegalEntityId,
                        req.SourceModule,
                        req.SourceEntityId,
                        req.Id,
                        input.Reason,
                        _userContext.UserId.Value));
            }
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

    [HttpPost("requests/{id:guid}/delegate")]
    [ProducesResponseType(typeof(ApprovalDelegationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApprovalDelegationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DelegateRequest(Guid id, [FromBody] ApprovalDelegationInput input)
    {
        if (!HasAnyPermission("approvals.delegation.manage", "approvals.action.execute"))
        {
            return Forbid();
        }

        if (!_userContext.LegalEntityId.HasValue)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Legal Entity Context Required",
                Detail = "Select an authorized legal entity before delegating an approval request."
            });
        }

        if (input.DelegateToUserId == Guid.Empty || input.DelegateToUserId == _userContext.UserId.Value)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Delegation Target",
                Detail = "delegateToUserId must identify a different non-empty user."
            });
        }

        if (string.IsNullOrWhiteSpace(input.Reason))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Delegation Reason Required",
                Detail = "A reason is required for the approval audit trail."
            });
        }

        var expiresAt = input.ExpiresAtUtc?.ToUniversalTime();
        if (expiresAt.HasValue && expiresAt <= DateTime.UtcNow)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Delegation Expiry",
                Detail = "Delegation expiry must be in the future."
            });
        }

        var request = await _repository.GetApprovalRequestEntityByIdAsync(
            _userContext.TenantId,
            id,
            _userContext.LegalEntityId.Value);
        if (request == null) return NotFound();

        var isAdministrator = _userContext.HasPermission("admin");
        if (!isAdministrator && !await _repository.IsCurrentApproverAsync(
                _userContext.TenantId,
                _userContext.LegalEntityId.Value,
                id,
                _userContext.UserId.Value))
        {
            return Forbid();
        }

        var delegation = await _repository.CreateDelegationAsync(
            _userContext.TenantId,
            _userContext.LegalEntityId.Value,
            id,
            _userContext.UserId.Value,
            input.DelegateToUserId,
            input.Reason,
            expiresAt,
            isAdministrator);
        if (delegation == null)
        {
            return Forbid();
        }

        Response.Headers["X-Approval-Delegation-Id"] = delegation.Delegation.Id.ToString();
        return delegation.AlreadyExists
            ? Ok(delegation.Delegation)
            : StatusCode(StatusCodes.Status201Created, delegation.Delegation);
    }

    private bool HasAnyPermission(params string[] permissions)
    {
        if (_userContext.HasPermission("admin")) return true;
        return permissions.Any(_userContext.HasPermission);
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
    uint RowVersion,
    string? Reason = null
);

public record ApprovalDelegationInput(
    Guid DelegateToUserId,
    string Reason,
    DateTime? ExpiresAtUtc = null
);

public record PagedApprovalInboxResponse(
    IReadOnlyList<ApprovalInboxItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);
