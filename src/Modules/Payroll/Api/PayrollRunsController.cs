using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Workforce.Modules.Compliance.Domain;
using Workforce.Modules.Compliance.Infrastructure;
using Workforce.Modules.Payroll.Domain;
using Workforce.Modules.Payroll.Domain.CalculationEngine;
using Workforce.Modules.Payroll.Infrastructure;
using Workforce.SharedKernel.Primitives;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Payroll.Api;

public record CreatePayrollPeriodRequest(
    string Code,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    DateOnly PaymentDate
);

public record CreatePayrollRunRequest(
    Guid PeriodId,
    string Code,
    string Currency
);

public record LoadInputsRequest(
    List<PayrollInputSnapshotDto> Snapshots,
    uint ExpectedRowVersion
);

public record PayrollInputSnapshotDto(
    Guid EmploymentId,
    decimal BaseSalaryMonthly,
    string AllowancesJson,
    int ScheduledDays,
    int VerifiedWorkedMinutes,
    decimal ApprovedAbsenceDays,
    decimal ApprovedLeaveDays,
    decimal UnpaidLeaveDays
);

public record CalculateRunRequest(uint ExpectedRowVersion);
public record FinalizeRunRequest(uint ExpectedRowVersion);

public record PayrollPeriodDto(
    Guid Id,
    string Code,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    DateOnly PaymentDate,
    bool IsActive
);

public record PayrollRunDto(
    Guid Id,
    Guid PeriodId,
    string Code,
    string Status,
    string Currency,
    decimal TotalGross,
    decimal TotalNet,
    decimal TotalEmployerContributions,
    int EmployeeCount,
    string ReproducibilityHash,
    DateTime? FinalizedAtUtc,
    uint RowVersion
);

[ApiController]
[Route("api/v1/payroll")]
public class PayrollRunsController : ControllerBase
{
    private readonly IPayrollRepository _repository;
    private readonly IComplianceRepository _complianceRepository;
    private readonly IPayrollCalculationEngine _calculationEngine;
    private readonly IUserContext _userContext;

    public PayrollRunsController(
        IPayrollRepository repository,
        IComplianceRepository complianceRepository,
        IPayrollCalculationEngine calculationEngine,
        IUserContext userContext)
    {
        _repository = repository;
        _complianceRepository = complianceRepository;
        _calculationEngine = calculationEngine;
        _userContext = userContext;
    }

    [HttpGet("periods")]
    public async Task<ActionResult<IReadOnlyList<PayrollPeriodDto>>> GetPeriods(CancellationToken ct)
    {
        var legalEntityId = _userContext.LegalEntityId ?? new LegalEntityId(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        var periods = await _repository.GetPeriodsAsync(_userContext.TenantId, legalEntityId, ct);
        var dtos = new List<PayrollPeriodDto>();
        foreach (var p in periods)
        {
            dtos.Add(new PayrollPeriodDto(p.Id, p.Code, p.PeriodStart, p.PeriodEnd, p.PaymentDate, p.IsActive));
        }
        return Ok(dtos);
    }

    [HttpPost("periods")]
    public async Task<ActionResult<PayrollPeriodDto>> CreatePeriod([FromBody] CreatePayrollPeriodRequest req, CancellationToken ct)
    {
        var legalEntityId = _userContext.LegalEntityId ?? new LegalEntityId(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        var period = new PayrollPeriod(
            Guid.NewGuid(), _userContext.TenantId, legalEntityId,
            req.Code, req.PeriodStart, req.PeriodEnd, req.PaymentDate
        );

        await _repository.CreatePeriodAsync(period, ct);
        return Created($"/api/v1/payroll/periods/{period.Id}", new PayrollPeriodDto(
            period.Id, period.Code, period.PeriodStart, period.PeriodEnd, period.PaymentDate, period.IsActive
        ));
    }

    [HttpGet("runs")]
    public async Task<ActionResult<IReadOnlyList<PayrollRunDto>>> GetRuns(CancellationToken ct)
    {
        var legalEntityId = _userContext.LegalEntityId ?? new LegalEntityId(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        var runs = await _repository.GetRunsAsync(_userContext.TenantId, legalEntityId, ct);
        var dtos = new List<PayrollRunDto>();
        foreach (var r in runs)
        {
            dtos.Add(new PayrollRunDto(
                r.Id, r.PeriodId, r.Code, r.Status.ToString(), r.Currency,
                r.TotalGross, r.TotalNet, r.TotalEmployerContributions,
                r.EmployeeCount, r.ReproducibilityHash, r.FinalizedAtUtc, r.RowVersion
            ));
        }
        return Ok(dtos);
    }

    [HttpGet("runs/{id:guid}")]
    public async Task<ActionResult<PayrollRunDto>> GetRunById(Guid id, CancellationToken ct)
    {
        var r = await _repository.GetRunByIdAsync(_userContext.TenantId, id, ct);
        if (r == null) return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Payroll run '{id}' not found." });

        return Ok(new PayrollRunDto(
            r.Id, r.PeriodId, r.Code, r.Status.ToString(), r.Currency,
            r.TotalGross, r.TotalNet, r.TotalEmployerContributions,
            r.EmployeeCount, r.ReproducibilityHash, r.FinalizedAtUtc, r.RowVersion
        ));
    }

    [HttpPost("runs")]
    public async Task<ActionResult<PayrollRunDto>> CreateRun([FromBody] CreatePayrollRunRequest req, CancellationToken ct)
    {
        var legalEntityId = _userContext.LegalEntityId ?? new LegalEntityId(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        var run = new PayrollRun(
            Guid.NewGuid(), _userContext.TenantId, legalEntityId,
            req.PeriodId, req.Code, req.Currency
        );

        await _repository.CreateRunAsync(run, ct);
        return Created($"/api/v1/payroll/runs/{run.Id}", new PayrollRunDto(
            run.Id, run.PeriodId, run.Code, run.Status.ToString(), run.Currency,
            run.TotalGross, run.TotalNet, run.TotalEmployerContributions,
            run.EmployeeCount, run.ReproducibilityHash, run.FinalizedAtUtc, run.RowVersion
        ));
    }

    [HttpPost("runs/{id:guid}/load-inputs")]
    public async Task<IActionResult> LoadInputs(Guid id, [FromBody] LoadInputsRequest req, CancellationToken ct)
    {
        var run = await _repository.GetRunByIdAsync(_userContext.TenantId, id, ct);
        if (run == null) return NotFound();

        var snapshots = new List<PayrollInputSnapshot>();
        foreach (var s in req.Snapshots)
        {
            snapshots.Add(new PayrollInputSnapshot(
                Guid.NewGuid(), run.Id, s.EmploymentId, s.BaseSalaryMonthly,
                s.AllowancesJson, s.ScheduledDays, s.VerifiedWorkedMinutes,
                s.ApprovedAbsenceDays, s.ApprovedLeaveDays, s.UnpaidLeaveDays
            ));
        }

        try
        {
            run.LoadInputs(snapshots, req.ExpectedRowVersion);
            await _repository.SaveSnapshotsAsync(run.Id, snapshots, ct);
            await _repository.UpdateRunAsync(run, ct);
            return Ok(new { status = run.Status.ToString(), employeeCount = run.EmployeeCount, rowVersion = run.RowVersion });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Title = "Conflict", Detail = ex.Message });
        }
    }

    [HttpPost("runs/{id:guid}/calculate")]
    public async Task<IActionResult> Calculate(Guid id, [FromBody] CalculateRunRequest req, CancellationToken ct)
    {
        var run = await _repository.GetRunByIdAsync(_userContext.TenantId, id, ct);
        if (run == null) return NotFound();

        var snapshots = await _repository.GetSnapshotsByRunAsync(run.Id, ct);
        if (snapshots.Count == 0)
        {
            return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = "No input snapshots loaded for this run." });
        }

        // Fetch active statutory compliance rules for the legal entity
        var activeRules = new List<StatutoryRuleVersion>();
        var gosi = await _complianceRepository.GetActiveRuleVersionAsync("EG_SOCIAL_INSURANCE", DateOnly.FromDateTime(DateTime.UtcNow), ct);
        var tax = await _complianceRepository.GetActiveRuleVersionAsync("EG_INCOME_TAX", DateOnly.FromDateTime(DateTime.UtcNow), ct);
        if (gosi != null) activeRules.Add(gosi);
        if (tax != null) activeRules.Add(tax);

        try
        {
            run.LoadInputs(snapshots, run.RowVersion);
            run.Calculate(_calculationEngine, activeRules, req.ExpectedRowVersion);

            await _repository.SaveResultsAndTracesAsync(run, ct);

            return Accepted(new
            {
                jobId = $"job_{Guid.NewGuid():N}",
                operation = "payroll.calculate",
                status = "completed",
                totalGross = run.TotalGross,
                totalNet = run.TotalNet,
                employeeCount = run.EmployeeCount,
                reproducibilityHash = run.ReproducibilityHash,
                rowVersion = run.RowVersion
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Title = "Conflict", Detail = ex.Message });
        }
    }

    [HttpPost("runs/{id:guid}/finalize")]
    public async Task<IActionResult> FinalizeRun(Guid id, [FromBody] FinalizeRunRequest req, CancellationToken ct)
    {
        var run = await _repository.GetRunByIdAsync(_userContext.TenantId, id, ct);
        if (run == null) return NotFound();

        try
        {
            if (run.Status == PayrollRunStatus.Calculated)
            {
                run.SubmitForReview(run.RowVersion);
            }
            if (run.Status == PayrollRunStatus.UnderReview)
            {
                run.Approve(Guid.NewGuid(), run.RowVersion);
            }

            run.FinalizeRun(_userContext.UserId.Value, req.ExpectedRowVersion);
            await _repository.UpdateRunAsync(run, ct);

            return Ok(new
            {
                status = run.Status.ToString(),
                finalizedAtUtc = run.FinalizedAtUtc,
                rowVersion = run.RowVersion
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Title = "Conflict", Detail = ex.Message });
        }
    }
}
