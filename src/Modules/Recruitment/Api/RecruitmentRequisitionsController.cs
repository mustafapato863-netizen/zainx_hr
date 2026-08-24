using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Workforce.Modules.Approvals.Domain;
using Workforce.Modules.Approvals.Infrastructure;
using Workforce.Modules.Recruitment.Domain;
using Workforce.Modules.Recruitment.Infrastructure;
using Workforce.SharedKernel.Primitives;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Recruitment.Api;

public record CreateRequisitionRequest(
    Guid OrganizationUnitId,
    Guid? PositionId,
    Guid? LocationId,
    Guid HiringManagerId,
    Guid RecruiterId,
    string RequisitionNumber,
    string TitleEn,
    string TitleAr,
    int OpeningsCount,
    string EmploymentType,
    Guid? PipelineId,
    string? RequisitionReason,
    string? TargetStartDate
);

public record UpdateRequisitionRequest(
    Guid OrganizationUnitId,
    Guid? PositionId,
    Guid? LocationId,
    Guid HiringManagerId,
    Guid RecruiterId,
    string TitleEn,
    string TitleAr,
    int OpeningsCount,
    string EmploymentType,
    string? RequisitionReason,
    string? TargetStartDate,
    uint ExpectedRowVersion
);

public record ConcurrencyActionRequest(uint ExpectedRowVersion);

[ApiController]
[Route("api/v1/recruitment/requisitions")]
public class RecruitmentRequisitionsController : ControllerBase
{
    private readonly IRecruitmentRepository _repository;
    private readonly IApprovalsRepository? _approvalsRepository;
    private readonly IUserContext _userContext;

    public RecruitmentRequisitionsController(
        IRecruitmentRepository repository,
        IUserContext userContext,
        IApprovalsRepository? approvalsRepository = null)
    {
        _repository = repository;
        _userContext = userContext;
        _approvalsRepository = approvalsRepository;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedRecruitmentResult<JobRequisition>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRequisitions(
        [FromQuery] RequisitionStatus? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _repository.QueryRequisitionsAsync(
            _userContext.TenantId,
            _userContext.LegalEntityId,
            status,
            search,
            page,
            pageSize,
            ct
        );
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(JobRequisition), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRequisitionById(Guid id, CancellationToken ct)
    {
        var req = await _repository.GetRequisitionByIdAsync(_userContext.TenantId, id, ct);
        if (req == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Requisition Not Found",
                Detail = $"Job requisition with ID '{id}' was not found in the current tenant.",
                Instance = HttpContext.Request.Path
            });
        }
        return Ok(req);
    }

    [HttpPost]
    [ProducesResponseType(typeof(JobRequisition), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRequisition([FromBody] CreateRequisitionRequest request, CancellationToken ct)
    {
        var defaultPipeline = await _repository.GetDefaultPipelineVersionAsync(_userContext.TenantId, ct);
        var pipelineId = request.PipelineId ?? defaultPipeline?.PipelineId ?? Guid.Parse("a0000000-0000-0000-0000-000000000001");
        var pipelineVersion = defaultPipeline?.VersionNumber ?? 1;

        var targetStart = DateOnly.TryParse(request.TargetStartDate, out var parsedDate) ? parsedDate : (DateOnly?)null;
        var req = new JobRequisition(
            Guid.NewGuid(),
            _userContext.TenantId,
            _userContext.LegalEntityId ?? LegalEntityId.New(),
            request.OrganizationUnitId,
            request.PositionId,
            request.LocationId,
            request.HiringManagerId,
            request.RecruiterId,
            request.RequisitionNumber,
            request.TitleEn,
            request.TitleAr,
            request.OpeningsCount,
            request.EmploymentType,
            pipelineId,
            pipelineVersion,
            request.RequisitionReason,
            targetStart
        );

        await _repository.CreateRequisitionAsync(req, ct);
        return CreatedAtAction(nameof(GetRequisitionById), new { id = req.Id }, req);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(JobRequisition), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateRequisition(Guid id, [FromBody] UpdateRequisitionRequest request, CancellationToken ct)
    {
        var req = await _repository.GetRequisitionByIdAsync(_userContext.TenantId, id, ct);
        if (req == null) return NotFound();

        try
        {
            var targetStart = DateOnly.TryParse(request.TargetStartDate, out var parsedDate) ? parsedDate : (DateOnly?)null;
            req.UpdateDetails(
                request.TitleEn,
                request.TitleAr,
                request.OpeningsCount,
                request.EmploymentType,
                request.OrganizationUnitId,
                request.PositionId,
                request.LocationId,
                request.HiringManagerId,
                request.RecruiterId,
                targetStart,
                request.RequisitionReason,
                request.ExpectedRowVersion
            );

            await _repository.UpdateRequisitionAsync(req, ct);
            return Ok(req);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrency conflict"))
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Concurrency Conflict",
                Detail = ex.Message,
                Instance = HttpContext.Request.Path
            });
        }
    }

    [HttpPost("{id:guid}/submit-approval")]
    [ProducesResponseType(typeof(JobRequisition), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitForApproval(Guid id, [FromBody] ConcurrencyActionRequest request, CancellationToken ct)
    {
        var req = await _repository.GetRequisitionByIdAsync(_userContext.TenantId, id, ct);
        if (req == null) return NotFound();

        try
        {
            var approvalId = Guid.NewGuid();
            if (_approvalsRepository != null)
            {
                var approvalReq = new ApprovalRequest(
                    approvalId,
                    _userContext.TenantId,
                    req.LegalEntityId,
                    "RECRUITMENT",
                    req.Id,
                    "JOB_REQUISITION",
                    req.TitleEn,
                    _userContext.UserId.Value,
                    Guid.Empty,
                    null,
                    1
                );
                await _approvalsRepository.SaveApprovalRequestAsync(approvalReq);
            }

            req.SubmitForApproval(approvalId, request.ExpectedRowVersion);
            await _repository.UpdateRequisitionAsync(req, ct);
            return Ok(req);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrency conflict"))
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Concurrency Conflict",
                Detail = ex.Message,
                Instance = HttpContext.Request.Path
            });
        }
    }

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(JobRequisition), StatusCodes.Status200OK)]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ConcurrencyActionRequest request, CancellationToken ct)
    {
        var req = await _repository.GetRequisitionByIdAsync(_userContext.TenantId, id, ct);
        if (req == null) return NotFound();

        try
        {
            req.Approve(request.ExpectedRowVersion);
            await _repository.UpdateRequisitionAsync(req, ct);
            await _repository.SaveOutboxMessageAsync(_userContext.TenantId, "RequisitionApproved", new RequisitionApprovedEvent(req.Id, req.TenantId.Value, req.LegalEntityId.Value, req.TitleEn), ct);
            return Ok(req);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrency conflict"))
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Concurrency Conflict", Detail = ex.Message });
        }
    }

    [HttpPost("{id:guid}/open")]
    [ProducesResponseType(typeof(JobRequisition), StatusCodes.Status200OK)]
    public async Task<IActionResult> Open(Guid id, [FromBody] ConcurrencyActionRequest request, CancellationToken ct)
    {
        var req = await _repository.GetRequisitionByIdAsync(_userContext.TenantId, id, ct);
        if (req == null) return NotFound();

        try
        {
            req.Open(request.ExpectedRowVersion);
            await _repository.UpdateRequisitionAsync(req, ct);
            await _repository.SaveOutboxMessageAsync(_userContext.TenantId, "RequisitionOpened", new RequisitionOpenedEvent(req.Id, req.TenantId.Value, req.LegalEntityId.Value, req.TitleEn), ct);
            return Ok(req);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrency conflict"))
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Concurrency Conflict", Detail = ex.Message });
        }
    }

    [HttpPost("{id:guid}/hold")]
    [ProducesResponseType(typeof(JobRequisition), StatusCodes.Status200OK)]
    public async Task<IActionResult> PutOnHold(Guid id, [FromBody] ConcurrencyActionRequest request, CancellationToken ct)
    {
        var req = await _repository.GetRequisitionByIdAsync(_userContext.TenantId, id, ct);
        if (req == null) return NotFound();

        try
        {
            req.PutOnHold(request.ExpectedRowVersion);
            await _repository.UpdateRequisitionAsync(req, ct);
            return Ok(req);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrency conflict"))
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Concurrency Conflict", Detail = ex.Message });
        }
    }

    [HttpPost("{id:guid}/close")]
    [ProducesResponseType(typeof(JobRequisition), StatusCodes.Status200OK)]
    public async Task<IActionResult> Close(Guid id, [FromBody] ConcurrencyActionRequest request, CancellationToken ct)
    {
        var req = await _repository.GetRequisitionByIdAsync(_userContext.TenantId, id, ct);
        if (req == null) return NotFound();

        try
        {
            req.Close(request.ExpectedRowVersion);
            await _repository.UpdateRequisitionAsync(req, ct);
            return Ok(req);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrency conflict"))
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Concurrency Conflict", Detail = ex.Message });
        }
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(JobRequisition), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] ConcurrencyActionRequest request, CancellationToken ct)
    {
        var req = await _repository.GetRequisitionByIdAsync(_userContext.TenantId, id, ct);
        if (req == null) return NotFound();

        try
        {
            req.Cancel(request.ExpectedRowVersion);
            await _repository.UpdateRequisitionAsync(req, ct);
            return Ok(req);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrency conflict"))
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Concurrency Conflict", Detail = ex.Message });
        }
    }
}
