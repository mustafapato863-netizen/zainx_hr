using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Workforce.Modules.Payroll.Domain;
using Workforce.Modules.Payroll.Infrastructure;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Payroll.Api;

public record PayrollExceptionDto(
    Guid Id,
    Guid PayrollRunId,
    Guid EmploymentId,
    string Severity,
    string Category,
    string Reason,
    string ResolutionGuidance,
    string Status,
    Guid? ResolvedByUserId,
    string? ResolutionNote
);

public record ResolveExceptionRequest(string Note);
public record WaiveExceptionRequest(string Justification);

[ApiController]
[Route("api/v1/payroll/runs/{runId:guid}/exceptions")]
public class PayrollExceptionsController : ControllerBase
{
    private readonly IPayrollRepository _repository;
    private readonly IUserContext _userContext;

    public PayrollExceptionsController(IPayrollRepository repository, IUserContext userContext)
    {
        _repository = repository;
        _userContext = userContext;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PayrollExceptionDto>>> GetExceptions(Guid runId, CancellationToken ct)
    {
        var run = await _repository.GetRunByIdAsync(_userContext.TenantId, runId, ct);
        if (run == null) return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Payroll run '{runId}' not found." });

        var exceptions = await _repository.GetExceptionsByRunAsync(runId, ct);
        var dtos = new List<PayrollExceptionDto>();
        foreach (var ex in exceptions)
        {
            dtos.Add(new PayrollExceptionDto(
                ex.Id, ex.PayrollRunId, ex.EmploymentId, ex.Severity.ToString(),
                ex.Category, ex.Reason, ex.ResolutionGuidance, ex.Status.ToString(),
                ex.ResolvedByUserId, ex.ResolutionNote
            ));
        }

        return Ok(dtos);
    }

    [HttpPost("{exceptionId:guid}/resolve")]
    public async Task<IActionResult> ResolveException(Guid runId, Guid exceptionId, [FromBody] ResolveExceptionRequest req, CancellationToken ct)
    {
        var run = await _repository.GetRunByIdAsync(_userContext.TenantId, runId, ct);
        if (run == null) return NotFound();

        var exceptions = await _repository.GetExceptionsByRunAsync(runId, ct);
        var target = ((List<PayrollException>)exceptions).Find(e => e.Id == exceptionId);
        if (target == null) return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Exception '{exceptionId}' not found." });

        target.Resolve(_userContext.UserId.Value, req.Note);
        await _repository.UpdateExceptionAsync(target, ct);

        return Ok(new { status = target.Status.ToString(), resolvedBy = _userContext.UserId.Value });
    }

    [HttpPost("{exceptionId:guid}/waive")]
    public async Task<IActionResult> WaiveException(Guid runId, Guid exceptionId, [FromBody] WaiveExceptionRequest req, CancellationToken ct)
    {
        var run = await _repository.GetRunByIdAsync(_userContext.TenantId, runId, ct);
        if (run == null) return NotFound();

        var exceptions = await _repository.GetExceptionsByRunAsync(runId, ct);
        var target = ((List<PayrollException>)exceptions).Find(e => e.Id == exceptionId);
        if (target == null) return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Exception '{exceptionId}' not found." });

        try
        {
            target.Waive(_userContext.UserId.Value, req.Justification);
            await _repository.UpdateExceptionAsync(target, ct);
            return Ok(new { status = target.Status.ToString(), waivedBy = _userContext.UserId.Value });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Title = "Conflict", Detail = ex.Message });
        }
    }
}
