using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
// using Workforce.Modules.People.Domain;
// using Workforce.Modules.People.Infrastructure;
using Workforce.Modules.Recruitment.Domain;
using Workforce.Modules.Recruitment.Infrastructure;
using Workforce.SharedKernel.Primitives;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Recruitment.Api;

public record CreateApplicationRequest(
    Guid RequisitionId,
    Guid CandidateId,
    string? Source
);

public record MoveApplicationStageRequest(
    Guid TargetStageId,
    string? Reason,
    string? IdempotencyKey,
    uint ExpectedRowVersion
);

public record RejectApplicationRequest(
    string ReasonCode,
    string? ReasonNote,
    uint ExpectedRowVersion
);

public record WithdrawApplicationRequest(
    string? Reason,
    uint ExpectedRowVersion
);

public record HireCandidateRequest(
    DateOnly HireDate,
    string? EmployeeNumber,
    string? NationalIdentifier,
    DateOnly? DateOfBirth,
    string? Gender,
    string? Nationality,
    uint ExpectedRowVersion
);

public record ApplicationDetailDto(
    Application Application,
    Candidate Candidate,
    JobRequisition Requisition,
    IReadOnlyList<ApplicationStageHistory> StageHistory
);

public record PipelineBoardDto(
    Guid RequisitionId,
    string RequisitionTitleEn,
    string RequisitionTitleAr,
    IReadOnlyList<RecruitmentStage> Stages,
    IReadOnlyList<ApplicationSummaryDto> Applications
);

public record ApplicationSummaryDto(
    Guid Id,
    Guid CandidateId,
    string CandidateNameEn,
    string CandidateNameAr,
    string Email,
    string PhoneNumber,
    Guid CurrentStageId,
    ApplicationStatus Status,
    DateTime AppliedAtUtc,
    uint RowVersion
);

[ApiController]
[Route("api/v1/recruitment/applications")]
public class RecruitmentApplicationsController : ControllerBase
{
    private readonly IRecruitmentRepository _repository;
    private readonly Workforce.Modules.People.Application.Contracts.IPeopleHiringContract? _peopleHiringContract;
    private readonly IUserContext _userContext;
    private readonly IPiiEncryptionService _piiEncryptionService;

    public RecruitmentApplicationsController(
        IRecruitmentRepository repository,
        IUserContext userContext,
        Workforce.Modules.People.Application.Contracts.IPeopleHiringContract? peopleHiringContract = null,
        IPiiEncryptionService? piiEncryptionService = null)
    {
        _repository = repository;
        _userContext = userContext;
        _peopleHiringContract = peopleHiringContract;
        _piiEncryptionService = piiEncryptionService ?? new AesPiiEncryptionService();
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedRecruitmentResult<Application>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApplications(
        [FromQuery] Guid? requisitionId,
        [FromQuery] Guid? candidateId,
        [FromQuery] Guid? stageId,
        [FromQuery] ApplicationStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _repository.QueryApplicationsAsync(
            _userContext.TenantId,
            requisitionId,
            candidateId,
            stageId,
            status,
            page,
            pageSize,
            ct
        );
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApplicationDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetApplicationById(Guid id, CancellationToken ct)
    {
        var app = await _repository.GetApplicationByIdAsync(_userContext.TenantId, id, ct);
        if (app == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Application Not Found",
                Detail = $"Application '{id}' was not found in the current tenant.",
                Instance = HttpContext.Request.Path
            });
        }

        var candidate = await _repository.GetCandidateByIdAsync(_userContext.TenantId, app.CandidateId, ct);
        var req = await _repository.GetRequisitionByIdAsync(_userContext.TenantId, app.RequisitionId, ct);

        return Ok(new ApplicationDetailDto(
            app,
            candidate ?? throw new InvalidOperationException("Candidate not found"),
            req ?? throw new InvalidOperationException("Requisition not found"),
            app.StageHistory
        ));
    }

    [HttpGet("board/{requisitionId:guid}")]
    [ProducesResponseType(typeof(PipelineBoardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPipelineBoard(Guid requisitionId, CancellationToken ct)
    {
        var req = await _repository.GetRequisitionByIdAsync(_userContext.TenantId, requisitionId, ct);
        if (req == null) return NotFound();

        var pipeline = await _repository.GetPipelineWithVersionsAsync(_userContext.TenantId, req.PipelineId, ct);
        var version = await _repository.GetPipelineVersionWithStagesAsync(
            pipeline?.Versions.FirstOrDefault(v => v.VersionNumber == req.PipelineVersion)?.Id ?? Guid.Empty,
            ct
        );
        var stages = version?.Stages ?? Array.Empty<RecruitmentStage>();

        var apps = await _repository.GetPipelineBoardApplicationsAsync(_userContext.TenantId, requisitionId, ct);
        var summaries = new List<ApplicationSummaryDto>();

        foreach (var app in apps)
        {
            var candidate = await _repository.GetCandidateByIdAsync(_userContext.TenantId, app.CandidateId, ct);
            summaries.Add(new ApplicationSummaryDto(
                app.Id,
                app.CandidateId,
                candidate != null ? $"{candidate.FirstNameEn} {candidate.LastNameEn}" : "Unknown",
                candidate != null ? $"{candidate.FirstNameAr} {candidate.LastNameAr}" : "غير معروف",
                candidate?.Email ?? string.Empty,
                candidate?.PhoneNumber ?? string.Empty,
                app.CurrentStageId,
                app.Status,
                app.AppliedAtUtc,
                app.RowVersion
            ));
        }

        return Ok(new PipelineBoardDto(
            req.Id,
            req.TitleEn,
            req.TitleAr,
            stages,
            summaries
        ));
    }

    [HttpPost]
    [ProducesResponseType(typeof(Application), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateApplication([FromBody] CreateApplicationRequest request, CancellationToken ct)
    {
        var req = await _repository.GetRequisitionByIdAsync(_userContext.TenantId, request.RequisitionId, ct);
        if (req == null)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Requisition",
                Detail = $"Job Requisition '{request.RequisitionId}' does not exist."
            });
        }

        if (req.Status != RequisitionStatus.Open)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Requisition Not Open",
                Detail = $"Cannot apply to Job Requisition in status '{req.Status}'. Requisition must be 'Open'."
            });
        }

        var candidate = await _repository.GetCandidateByIdAsync(_userContext.TenantId, request.CandidateId, ct);
        if (candidate == null)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Candidate",
                Detail = $"Candidate '{request.CandidateId}' does not exist."
            });
        }

        // Enforce active application invariant
        var existingActive = await _repository.GetActiveApplicationForCandidateAsync(_userContext.TenantId, request.RequisitionId, request.CandidateId, ct);
        if (existingActive != null)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Active Application Already Exists",
                Detail = $"Candidate '{candidate.Email}' already has an active application for requisition '{req.TitleEn}'."
            });
        }

        var pipelineVersion = await _repository.GetDefaultPipelineVersionAsync(_userContext.TenantId, ct);
        var initialStage = pipelineVersion?.Stages.FirstOrDefault(s => s.StageKind == StageKind.Applied) 
                           ?? pipelineVersion?.Stages.FirstOrDefault(s => s.StageOrder == 1)
                           ?? throw new InvalidOperationException("No initial stage configured for pipeline.");

        var app = new Application(
            Guid.NewGuid(),
            _userContext.TenantId,
            req.LegalEntityId,
            req.Id,
            candidate.Id,
            pipelineVersion!.Id,
            initialStage.Id,
            request.Source,
            _userContext.UserId.Value
        );

        await _repository.CreateApplicationAsync(app, ct);
        await _repository.SaveOutboxMessageAsync(_userContext.TenantId, "ApplicationCreated", new ApplicationCreatedEvent(app.Id, req.Id, candidate.Id, app.TenantId.Value), ct);

        return CreatedAtAction(nameof(GetApplicationById), new { id = app.Id }, app);
    }

    [HttpPost("{id:guid}/move-stage")]
    [ProducesResponseType(typeof(Application), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> MoveStage(Guid id, [FromBody] MoveApplicationStageRequest request, CancellationToken ct)
    {
        var app = await _repository.GetApplicationByIdAsync(_userContext.TenantId, id, ct);
        if (app == null) return NotFound();

        var fromStageId = app.CurrentStageId;
        try
        {
            app.MoveToStage(
                request.TargetStageId,
                _userContext.UserId.Value,
                request.Reason,
                request.IdempotencyKey,
                request.ExpectedRowVersion
            );

            await _repository.UpdateApplicationAsync(app, ct);
            if (fromStageId != app.CurrentStageId)
            {
                await _repository.SaveOutboxMessageAsync(_userContext.TenantId, "ApplicationStageChanged", new ApplicationStageChangedEvent(app.Id, fromStageId, app.CurrentStageId, _userContext.UserId.Value), ct);
            }

            return Ok(app);
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

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(Application), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectApplicationRequest request, CancellationToken ct)
    {
        var app = await _repository.GetApplicationByIdAsync(_userContext.TenantId, id, ct);
        if (app == null) return NotFound();

        try
        {
            app.Reject(request.ReasonCode, request.ReasonNote, _userContext.UserId.Value, request.ExpectedRowVersion);
            await _repository.UpdateApplicationAsync(app, ct);
            await _repository.SaveOutboxMessageAsync(_userContext.TenantId, "ApplicationRejected", new ApplicationRejectedEvent(app.Id, request.ReasonCode, _userContext.UserId.Value), ct);

            return Ok(app);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrency conflict"))
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Concurrency Conflict", Detail = ex.Message });
        }
    }

    [HttpPost("{id:guid}/withdraw")]
    [ProducesResponseType(typeof(Application), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Withdraw(Guid id, [FromBody] WithdrawApplicationRequest request, CancellationToken ct)
    {
        var app = await _repository.GetApplicationByIdAsync(_userContext.TenantId, id, ct);
        if (app == null) return NotFound();

        try
        {
            app.Withdraw(request.Reason, _userContext.UserId.Value, request.ExpectedRowVersion);
            await _repository.UpdateApplicationAsync(app, ct);
            return Ok(app);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrency conflict"))
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Concurrency Conflict", Detail = ex.Message });
        }
    }

    [HttpPost("{id:guid}/hire")]
    [ProducesResponseType(typeof(Application), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> HireCandidate(Guid id, [FromBody] HireCandidateRequest request, CancellationToken ct)
    {
        var app = await _repository.GetApplicationByIdAsync(_userContext.TenantId, id, ct);
        if (app == null) return NotFound();

        if (app.Status == ApplicationStatus.Hired && app.HiredPersonId.HasValue && app.HiredEmploymentId.HasValue)
        {
            // Idempotent return of already hired application
            return Ok(app);
        }

        var candidate = await _repository.GetCandidateByIdAsync(_userContext.TenantId, app.CandidateId, ct);
        var req = await _repository.GetRequisitionByIdAsync(_userContext.TenantId, app.RequisitionId, ct);

        if (candidate == null || req == null)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Invalid Application References" });
        }

        var plainNatId = string.IsNullOrWhiteSpace(request.NationalIdentifier) ? "1000000000" : request.NationalIdentifier;
        var encryptedNatId = _piiEncryptionService.Encrypt(plainNatId);
        var natIdHash = _piiEncryptionService.ComputeSearchHash(plainNatId);
        var maskedNatId = _piiEncryptionService.MaskNationalId(plainNatId);
        var dob = request.DateOfBirth ?? new DateOnly(1990, 1, 1);

        Guid personId = Guid.Empty;
        Guid employmentId = Guid.Empty;

        if (_peopleHiringContract != null)
        {
            var command = new Workforce.Modules.People.Application.Contracts.HirePersonCommand
            {
                IdempotencyKey = app.Id, // Application Id ensures idempotency per application
                FirstNameEn = candidate.FirstNameEn,
                LastNameEn = candidate.LastNameEn,
                FirstNameAr = candidate.FirstNameAr,
                LastNameAr = candidate.LastNameAr,
                DateOfBirth = dob,
                Gender = request.Gender ?? "Unspecified",
                Nationality = request.Nationality ?? "SA",
                EncryptedNationalId = encryptedNatId,
                NationalIdHash = natIdHash,
                MaskedNationalId = maskedNatId,
                Email = candidate.Email,
                PhoneNumber = candidate.PhoneNumber,
                LegalEntityId = app.LegalEntityId.Value,
                EmployeeNumber = request.EmployeeNumber,
                HireDate = request.HireDate,
                OrganizationUnitId = req.OrganizationUnitId,
                TitleEn = req.TitleEn,
                TitleAr = req.TitleAr,
                PositionId = req.PositionId,
                LocationId = req.LocationId,
                HiringManagerId = req.HiringManagerId
            };

            var hireResult = await _peopleHiringContract.HireAsync(_userContext.TenantId.Value.ToString(), command, ct);
            personId = hireResult.PersonId;
            employmentId = hireResult.EmploymentId;
        }
        else 
        {
            personId = Guid.NewGuid();
            employmentId = Guid.NewGuid();
        }

        try
        {
            app.MarkHired(personId, employmentId, _userContext.UserId.Value, request.ExpectedRowVersion);
            await _repository.UpdateApplicationAsync(app, ct);
            await _repository.SaveOutboxMessageAsync(_userContext.TenantId, "CandidateHired", new CandidateHiredEvent(
                app.Id,
                candidate.Id,
                personId,
                employmentId,
                _userContext.TenantId.Value,
                app.LegalEntityId.Value
            ), ct);

            return Ok(app);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrency conflict"))
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Concurrency Conflict", Detail = ex.Message });
        }
    }
}
