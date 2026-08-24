using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Workforce.Modules.Payroll.Domain;
using Workforce.Modules.Payroll.Infrastructure;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Payroll.Api;

public record PayrollEmployeeResultSummaryDto(
    Guid Id,
    Guid PayrollRunId,
    Guid EmploymentId,
    decimal GrossPay,
    decimal NetPay,
    decimal TotalEarnings,
    decimal TotalDeductions,
    decimal EmployerContributions
);

public record PayrollLineDto(
    Guid Id,
    string ComponentCode,
    string NameEn,
    string NameAr,
    string Category,
    decimal Amount,
    string CalculationType,
    decimal Rate,
    decimal HoursOrDays,
    Guid? TraceId
);

public record CalculationTraceDto(
    Guid Id,
    int StepOrder,
    string RuleReference,
    string Description,
    string FormulaApplied,
    string InputValuesJson,
    decimal IntermediateAmount,
    decimal RoundingDelta,
    decimal FinalAmount
);

public record PayrollEmployeeResultDetailDto(
    Guid Id,
    Guid PayrollRunId,
    Guid EmploymentId,
    decimal GrossPay,
    decimal NetPay,
    decimal TotalEarnings,
    decimal TotalDeductions,
    decimal EmployerContributions,
    IReadOnlyList<PayrollLineDto> Lines,
    IReadOnlyList<CalculationTraceDto> Traces
);

[ApiController]
[Route("api/v1/payroll/runs/{runId:guid}/results")]
public class PayrollResultsController : ControllerBase
{
    private readonly IPayrollRepository _repository;
    private readonly IUserContext _userContext;

    public PayrollResultsController(IPayrollRepository repository, IUserContext userContext)
    {
        _repository = repository;
        _userContext = userContext;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PayrollEmployeeResultSummaryDto>>> GetResults(Guid runId, CancellationToken ct)
    {
        var run = await _repository.GetRunByIdAsync(_userContext.TenantId, runId, ct);
        if (run == null) return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Payroll run '{runId}' not found." });

        var results = await _repository.GetEmployeeResultsAsync(runId, ct);
        var dtos = new List<PayrollEmployeeResultSummaryDto>();
        foreach (var r in results)
        {
            dtos.Add(new PayrollEmployeeResultSummaryDto(
                r.Id, r.PayrollRunId, r.EmploymentId, r.GrossPay, r.NetPay,
                r.TotalEarnings, r.TotalDeductions, r.EmployerContributions
            ));
        }

        return Ok(dtos);
    }

    [HttpGet("{employmentId:guid}")]
    public async Task<ActionResult<PayrollEmployeeResultDetailDto>> GetResultDetail(Guid runId, Guid employmentId, CancellationToken ct)
    {
        var run = await _repository.GetRunByIdAsync(_userContext.TenantId, runId, ct);
        if (run == null) return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Payroll run '{runId}' not found." });

        var detail = await _repository.GetEmployeeResultDetailAsync(runId, employmentId, ct);
        if (detail == null) return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Employee result for '{employmentId}' not found in run '{runId}'." });

        var lines = new List<PayrollLineDto>();
        foreach (var l in detail.Lines)
        {
            lines.Add(new PayrollLineDto(
                l.Id, l.ComponentCode, l.NameEn, l.NameAr, l.Category.ToString(),
                l.Amount, l.CalculationType.ToString(), l.Rate, l.HoursOrDays, l.TraceId
            ));
        }

        var traces = new List<CalculationTraceDto>();
        foreach (var t in detail.Traces)
        {
            traces.Add(new CalculationTraceDto(
                t.Id, t.StepOrder, t.RuleReference, t.Description, t.FormulaApplied,
                t.InputValuesJson, t.IntermediateAmount, t.RoundingDelta, t.FinalAmount
            ));
        }

        return Ok(new PayrollEmployeeResultDetailDto(
            detail.Id, detail.PayrollRunId, detail.EmploymentId, detail.GrossPay, detail.NetPay,
            detail.TotalEarnings, detail.TotalDeductions, detail.EmployerContributions,
            lines, traces
        ));
    }
}
