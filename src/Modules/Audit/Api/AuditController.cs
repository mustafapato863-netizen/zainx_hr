using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Workforce.Modules.Audit.Domain;
using Workforce.Modules.Audit.Infrastructure;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Audit.Api;

public record AuditSearchRequest(
    Guid? ActorUserId = null,
    string? ActionCode = null,
    string? EntityType = null,
    string? EntityId = null,
    string? CorrelationId = null,
    Guid? LegalEntityId = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int Page = 1,
    int PageSize = 50
);

[ApiController]
[Route("api/v1/audit")]
public class AuditController : ControllerBase
{
    private readonly IAuditRepository _repository;
    private readonly IUserContext _userContext;

    public AuditController(IAuditRepository repository, IUserContext userContext)
    {
        _repository = repository;
        _userContext = userContext;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedAuditResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchAudit([FromQuery] AuditSearchRequest request, CancellationToken ct)
    {
        var filter = new AuditSearchFilter(
            request.ActorUserId,
            request.ActionCode,
            request.EntityType,
            request.EntityId,
            request.CorrelationId,
            request.LegalEntityId ?? _userContext.LegalEntityId?.Value,
            request.FromUtc,
            request.ToUtc,
            request.Page,
            request.PageSize
        );

        var result = await _repository.SearchAsync(_userContext.TenantId, filter, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AuditRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAuditById(Guid id, CancellationToken ct)
    {
        var record = await _repository.GetByIdAsync(_userContext.TenantId, id, ct);
        if (record == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Audit Record Not Found",
                Detail = $"No audit record found with ID '{id}' for current tenant.",
                Instance = HttpContext.Request.Path
            });
        }

        return Ok(record);
    }
}
