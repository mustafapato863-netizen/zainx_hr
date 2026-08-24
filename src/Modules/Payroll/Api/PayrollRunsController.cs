using System;
using System.Collections.Concurrent;
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

public record CalculateRunRequest(uint ExpectedRowVersion, string? IdempotencyKey = null);
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

public record BackgroundJobDto(
    string JobId,
    string Operation,
    string Status,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    string? Error
);

[ApiController]
[Route("api/v1")]
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

    [HttpGet("jobs/{jobId}")]
    public async Task<ActionResult<BackgroundJobDto>> GetJobStatus(string jobId, CancellationToken ct)
    {
        if (!Guid.TryParse(jobId, out var id))
        {
            return BadRequest(new ProblemDetails { Title = "Invalid Job ID", Detail = "Job ID must be a valid GUID." });
        }

        var job = await _repository.GetJobByIdAsync(_userContext.TenantId, id, ct);
        if (job != null)
        {
            var statusStr = job.Status switch
            {
                PayrollJobStatus.Queued => "queued",
                PayrollJobStatus.Running => "running",
                PayrollJobStatus.Completed => "completed",
                PayrollJobStatus.CompletedWithWarnings => "completed_with_warnings",
                PayrollJobStatus.Failed => "failed",
                _ => job.Status.ToString().ToLowerInvariant()
            };

            return Ok(new BackgroundJobDto(
                job.Id.ToString(),
                job.Operation,
                statusStr,
                job.StartedAtUtc,
                job.CompletedAtUtc,
                job.ErrorMessage
            ));
        }

        return NotFound(new ProblemDetails { Title = "Job Not Found", Detail = $"Job '{jobId}' was not found." });
    }

    [HttpGet("payroll/periods")]
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

    [HttpPost("payroll/periods")]
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

    [HttpGet("payroll/runs")]
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

    [HttpGet("payroll/runs/{id:guid}")]
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

    [HttpPost("payroll/runs")]
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

    [HttpPost("payroll/runs/{id:guid}/load-inputs")]
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

    [HttpPost("payroll/runs/{id:guid}/calculate")]
    public async Task<IActionResult> Calculate(Guid id, [FromBody] CalculateRunRequest req, CancellationToken ct)
    {
        if (!_userContext.HasPermission("payroll.run.calculate") && !_userContext.HasPermission("admin"))
        {
            return Forbid();
        }

        var run = await _repository.GetRunByIdAsync(_userContext.TenantId, id, ct);
        if (run == null) return NotFound();

        var snapshots = await _repository.GetSnapshotsByRunAsync(run.Id, ct);
        if (snapshots.Count == 0)
        {
            return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = "No input snapshots loaded for this run." });
        }

        var idempotencyKey = string.IsNullOrWhiteSpace(req.IdempotencyKey)
            ? $"calc_{run.TenantId.Value}_{run.Id}_{req.ExpectedRowVersion}"
            : req.IdempotencyKey.Trim();

        var existingJob = await _repository.GetJobByIdempotencyKeyAsync(_userContext.TenantId, idempotencyKey, ct);
        if (existingJob != null)
        {
            var existingStatus = existingJob.Status switch
            {
                PayrollJobStatus.Queued => "queued",
                PayrollJobStatus.Running => "running",
                PayrollJobStatus.Completed => "completed",
                PayrollJobStatus.CompletedWithWarnings => "completed_with_warnings",
                PayrollJobStatus.Failed => "failed",
                _ => existingJob.Status.ToString().ToLowerInvariant()
            };

            return Accepted(new
            {
                jobId = existingJob.Id.ToString(),
                operation = existingJob.Operation,
                status = existingStatus
            });
        }

        var job = new PayrollBackgroundJob(
            Guid.NewGuid(),
            _userContext.TenantId.Value,
            run.Id,
            idempotencyKey,
            "payroll.calculate"
        );

        await _repository.CreateJobAsync(job, ct);

        return Accepted(new
        {
            jobId = job.Id.ToString(),
            operation = job.Operation,
            status = "queued"
        });
    }

    [HttpPost("payroll/runs/{id:guid}/finalize")]
    public async Task<IActionResult> FinalizeRun(Guid id, [FromBody] FinalizeRunRequest req, CancellationToken ct)
    {
        if (!_userContext.HasPermission("payroll.run.finalize") && !_userContext.HasPermission("admin"))
        {
            return Forbid();
        }

        var run = await _repository.GetRunByIdAsync(_userContext.TenantId, id, ct);
        if (run == null) return NotFound();

        // Segregation of Duties Check: Finalizer cannot be the one who created/calculated the run if strict SoD is enforced
        if (run.FinalizedByUserId.HasValue && run.FinalizedByUserId.Value == _userContext.UserId.Value)
        {
            return StatusCode(403, new ProblemDetails
            {
                Title = "Segregation of Duties Violation",
                Detail = "Under active financial segregation of duties policy, the user who initiated the run cannot execute final approval."
            });
        }

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
