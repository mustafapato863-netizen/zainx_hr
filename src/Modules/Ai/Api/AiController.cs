using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Workforce.Modules.Ai.Application.Contracts;
using Workforce.Modules.Ai.Domain;
using Workforce.Modules.Ai.Infrastructure;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Ai.Api;

[ApiController]
[Route("api/v1/ai")]
public class AiController : ControllerBase
{
    private readonly IAiConversationService _conversationService;
    private readonly AiToolRegistry _toolRegistry;
    private readonly IAiRepository _aiRepository;
    private readonly IAiProposalService _proposalService;
    private readonly AiActionRegistry _actionRegistry;
    private readonly IUserContext _userContext;

    public AiController(
        IAiConversationService conversationService,
        AiToolRegistry toolRegistry,
        IAiRepository aiRepository,
        IAiProposalService proposalService,
        AiActionRegistry actionRegistry,
        IUserContext userContext)
    {
        _conversationService = conversationService ?? throw new ArgumentNullException(nameof(conversationService));
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        _aiRepository = aiRepository ?? throw new ArgumentNullException(nameof(aiRepository));
        _proposalService = proposalService ?? throw new ArgumentNullException(nameof(proposalService));
        _actionRegistry = actionRegistry ?? throw new ArgumentNullException(nameof(actionRegistry));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    [HttpPost("conversations")]
    public async Task<ActionResult<ConversationSummaryDto>> CreateConversation(
        [FromBody] CreateConversationRequest request, 
        CancellationToken ct)
    {
        var summary = await _conversationService.CreateConversationAsync(request, _userContext, ct);
        return CreatedAtAction(nameof(GetConversation), new { id = summary.Id }, summary);
    }

    [HttpGet("conversations")]
    public async Task<ActionResult<List<ConversationSummaryDto>>> ListConversations(CancellationToken ct)
    {
        var list = await _conversationService.ListConversationsAsync(_userContext, ct);
        return Ok(list);
    }

    [HttpGet("conversations/{id:guid}")]
    public async Task<ActionResult<ConversationDetailDto>> GetConversation(Guid id, CancellationToken ct)
    {
        var detail = await _conversationService.GetConversationAsync(id, _userContext, ct);
        if (detail == null) return NotFound(new { error = $"Conversation '{id}' not found." });
        return Ok(detail);
    }

    [HttpPost("conversations/{id:guid}/messages")]
    public async Task<ActionResult<AiMessageResponseDto>> SendMessage(
        Guid id, 
        [FromBody] SendMessageRequest request, 
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request?.Prompt))
        {
            return BadRequest(new { error = "Prompt cannot be empty." });
        }

        try
        {
            var response = await _conversationService.SendMessageAsync(id, request, _userContext, ct);
            return Ok(response);
        }
        catch (Workforce.Modules.Ai.Application.Services.AiRequestLimitExceededException ex)
        {
            // Closeout Gate 8: safe per-user/tenant throttling.
            Response.Headers["Retry-After"] = "60";
            return StatusCode(429, new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception)
        {
            // Closeout Gate 6: no raw exception details or stack traces escape.
            return StatusCode(500, new { error = "AI response processing error. The incident was logged with a correlation ID." });
        }
    }

    [HttpGet("tools")]
    public ActionResult<List<AiToolDefinition>> ListTools()
    {
        var tools = _toolRegistry.GetAuthorizedDefinitions(_userContext.Permissions);
        return Ok(tools);
    }

    [HttpGet("policies")]
    public async Task<ActionResult<IReadOnlyList<CompanyPolicy>>> SearchPolicies(
        [FromQuery] string? query, 
        [FromQuery] DateTime? effectiveDate, 
        CancellationToken ct)
    {
        var policies = await _aiRepository.SearchPoliciesAsync(_userContext.TenantId, query, effectiveDate, ct);
        return Ok(policies);
    }

    // =========================================================================
    // PHASE 7B: PROPOSED / CONFIRMED ACTIONS ENDPOINTS
    // =========================================================================

    [HttpGet("actions")]
    public ActionResult<IReadOnlyList<AiActionDefinition>> ListActions()
    {
        var actions = _actionRegistry.GetAuthorizedActionDefinitions(_userContext.Permissions);
        return Ok(actions);
    }

    [HttpPost("proposals")]
    public async Task<ActionResult<AiActionProposalDto>> CreateProposal(
        [FromBody] CreateProposalRequest request,
        CancellationToken ct)
    {
        try
        {
            var proposal = await _proposalService.CreateProposalAsync(request, _userContext, ct);
            return StatusCode(201, proposal);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("proposals/{id:guid}")]
    public async Task<ActionResult<AiActionProposalDto>> GetProposal(Guid id, CancellationToken ct)
    {
        var proposal = await _proposalService.GetProposalAsync(id, _userContext, ct);
        if (proposal == null) return NotFound(new { error = $"Proposal '{id}' not found." });
        return Ok(proposal);
    }

    [HttpGet("proposals")]
    public async Task<ActionResult<IReadOnlyList<AiActionProposalDto>>> ListProposals(
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        var list = await _proposalService.ListProposalsAsync(_userContext, limit, ct);
        return Ok(list);
    }

    [HttpPost("proposals/{id:guid}/confirm")]
    public async Task<ActionResult<AiProposalExecutionResponseDto>> ConfirmProposal(
        Guid id,
        [FromBody] ConfirmProposalRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _proposalService.ConfirmProposalAsync(id, request ?? new ConfirmProposalRequest(null), _userContext, ct);
            if (result.Status == "Stale")
            {
                return StatusCode(409, result);
            }
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("expired", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(410, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("proposals/{id:guid}/cancel")]
    public async Task<ActionResult<AiActionProposalDto>> CancelProposal(
        Guid id,
        [FromBody] CancelProposalRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _proposalService.CancelProposalAsync(id, request ?? new CancelProposalRequest(null), _userContext, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
