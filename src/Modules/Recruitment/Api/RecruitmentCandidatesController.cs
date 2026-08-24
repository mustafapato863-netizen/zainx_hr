using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Workforce.Modules.Recruitment.Domain;
using Workforce.Modules.Recruitment.Infrastructure;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Recruitment.Api;

public record CreateCandidateRequest(
    string FirstNameEn,
    string LastNameEn,
    string FirstNameAr,
    string LastNameAr,
    string Email,
    string PhoneNumber,
    string? Location,
    string? Headline,
    string? Source,
    Guid? ResumeDocumentId,
    string? SkillsJson
);

public record UpdateCandidateRequest(
    string FirstNameEn,
    string LastNameEn,
    string FirstNameAr,
    string LastNameAr,
    string Email,
    string PhoneNumber,
    string? Location,
    string? Headline,
    string? Source,
    Guid? ResumeDocumentId,
    string? SkillsJson,
    uint ExpectedRowVersion
);

public record CheckDuplicateCandidatesRequest(
    string Email,
    string PhoneNumber,
    Guid? ExcludeCandidateId
);

[ApiController]
[Route("api/v1/recruitment/candidates")]
public class RecruitmentCandidatesController : ControllerBase
{
    private readonly IRecruitmentRepository _repository;
    private readonly IUserContext _userContext;

    public RecruitmentCandidatesController(IRecruitmentRepository repository, IUserContext userContext)
    {
        _repository = repository;
        _userContext = userContext;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedRecruitmentResult<Candidate>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCandidates(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _repository.QueryCandidatesAsync(_userContext.TenantId, search, page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Candidate), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCandidateById(Guid id, CancellationToken ct)
    {
        var candidate = await _repository.GetCandidateByIdAsync(_userContext.TenantId, id, ct);
        if (candidate == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Candidate Not Found",
                Detail = $"Candidate with ID '{id}' was not found in the current tenant.",
                Instance = HttpContext.Request.Path
            });
        }
        return Ok(candidate);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Candidate), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCandidate([FromBody] CreateCandidateRequest request, CancellationToken ct)
    {
        var candidate = new Candidate(
            Guid.NewGuid(),
            _userContext.TenantId,
            request.FirstNameEn,
            request.LastNameEn,
            request.FirstNameAr,
            request.LastNameAr,
            request.Email,
            request.PhoneNumber,
            request.Location,
            request.Headline,
            request.Source,
            request.ResumeDocumentId,
            request.SkillsJson
        );

        await _repository.CreateCandidateAsync(candidate, ct);
        return CreatedAtAction(nameof(GetCandidateById), new { id = candidate.Id }, candidate);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(Candidate), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCandidate(Guid id, [FromBody] UpdateCandidateRequest request, CancellationToken ct)
    {
        var candidate = await _repository.GetCandidateByIdAsync(_userContext.TenantId, id, ct);
        if (candidate == null) return NotFound();

        try
        {
            candidate.UpdateDetails(
                request.FirstNameEn,
                request.LastNameEn,
                request.FirstNameAr,
                request.LastNameAr,
                request.Email,
                request.PhoneNumber,
                request.Location,
                request.Headline,
                request.Source,
                request.ResumeDocumentId,
                request.SkillsJson,
                request.ExpectedRowVersion
            );

            await _repository.UpdateCandidateAsync(candidate, ct);
            return Ok(candidate);
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

    [HttpPost("check-duplicates")]
    [ProducesResponseType(typeof(IReadOnlyList<DuplicateCandidateMatchDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckDuplicates([FromBody] CheckDuplicateCandidatesRequest request, CancellationToken ct)
    {
        var duplicates = await _repository.FindPotentialDuplicatesAsync(
            _userContext.TenantId,
            request.Email,
            request.PhoneNumber,
            request.ExcludeCandidateId,
            ct
        );
        return Ok(duplicates);
    }
}
