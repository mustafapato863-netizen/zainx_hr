using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Workforce.Modules.Recruitment.Domain;
using Workforce.Modules.Recruitment.Infrastructure;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Recruitment.Api;

public record ScheduleInterviewRequest(
    Guid ApplicationId,
    Guid StageId,
    string Title,
    InterviewType InterviewType,
    DateTime ScheduledStartUtc,
    DateTime ScheduledEndUtc,
    string Timezone,
    string? LocationOrMeetingUrl,
    string? InterviewKitJson,
    IReadOnlyList<InterviewParticipantDto>? Participants
);

public record InterviewParticipantDto(
    Guid InterviewerUserId,
    InterviewerRole Role,
    bool IsRequired
);

public record RescheduleInterviewRequest(
    DateTime NewStartUtc,
    DateTime NewEndUtc,
    string Timezone,
    string? LocationOrMeetingUrl,
    uint ExpectedRowVersion
);

public record SubmitScorecardRequest(
    string RatingsJson,
    string? Strengths,
    string? Concerns,
    ScorecardRecommendation Recommendation,
    uint ExpectedRowVersion
);

[ApiController]
[Route("api/v1/recruitment/interviews")]
public class RecruitmentInterviewsController : ControllerBase
{
    private readonly IRecruitmentRepository _repository;
    private readonly IUserContext _userContext;

    public RecruitmentInterviewsController(IRecruitmentRepository repository, IUserContext userContext)
    {
        _repository = repository;
        _userContext = userContext;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Interview>), StatusCodes.Status200OK)]
    public async Task<IActionResult> QueryInterviews(
        [FromQuery] DateTime? startUtc,
        [FromQuery] DateTime? endUtc,
        [FromQuery] Guid? interviewerUserId,
        CancellationToken ct = default)
    {
        var from = startUtc ?? DateTime.UtcNow.AddMonths(-1);
        var to = endUtc ?? DateTime.UtcNow.AddMonths(2);

        var interviews = await _repository.QueryInterviewsAsync(
            _userContext.TenantId,
            from,
            to,
            interviewerUserId,
            ct
        );
        return Ok(interviews);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Interview), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInterviewById(Guid id, CancellationToken ct)
    {
        var interview = await _repository.GetInterviewByIdAsync(_userContext.TenantId, id, ct);
        if (interview == null) return NotFound();
        return Ok(interview);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Interview), StatusCodes.Status201Created)]
    public async Task<IActionResult> ScheduleInterview([FromBody] ScheduleInterviewRequest request, CancellationToken ct)
    {
        var interview = new Interview(
            Guid.NewGuid(),
            _userContext.TenantId,
            request.ApplicationId,
            request.StageId,
            request.Title,
            request.InterviewType,
            request.ScheduledStartUtc,
            request.ScheduledEndUtc,
            request.Timezone,
            request.LocationOrMeetingUrl,
            request.InterviewKitJson
        );

        if (request.Participants != null)
        {
            foreach (var p in request.Participants)
            {
                interview.AddParticipant(p.InterviewerUserId, p.Role, p.IsRequired);
            }
        }

        await _repository.CreateInterviewAsync(interview, ct);
        await _repository.SaveOutboxMessageAsync(_userContext.TenantId, "InterviewScheduled", new InterviewScheduledEvent(
            interview.Id,
            interview.ApplicationId,
            interview.ScheduledStartUtc,
            interview.Timezone
        ), ct);

        return CreatedAtAction(nameof(GetInterviewById), new { id = interview.Id }, interview);
    }

    [HttpPost("{id:guid}/reschedule")]
    [ProducesResponseType(typeof(Interview), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RescheduleInterview(Guid id, [FromBody] RescheduleInterviewRequest request, CancellationToken ct)
    {
        var interview = await _repository.GetInterviewByIdAsync(_userContext.TenantId, id, ct);
        if (interview == null) return NotFound();

        try
        {
            interview.Reschedule(
                request.NewStartUtc,
                request.NewEndUtc,
                request.Timezone,
                request.LocationOrMeetingUrl,
                request.ExpectedRowVersion
            );

            await _repository.UpdateInterviewAsync(interview, ct);
            return Ok(interview);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrency conflict"))
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Concurrency Conflict", Detail = ex.Message });
        }
    }

    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(typeof(Interview), StatusCodes.Status200OK)]
    public async Task<IActionResult> CompleteInterview(Guid id, [FromBody] ConcurrencyActionRequest request, CancellationToken ct)
    {
        var interview = await _repository.GetInterviewByIdAsync(_userContext.TenantId, id, ct);
        if (interview == null) return NotFound();

        try
        {
            interview.Complete(request.ExpectedRowVersion);
            await _repository.UpdateInterviewAsync(interview, ct);
            await _repository.SaveOutboxMessageAsync(_userContext.TenantId, "InterviewCompleted", new InterviewCompletedEvent(interview.Id, interview.ApplicationId), ct);
            return Ok(interview);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrency conflict"))
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Concurrency Conflict", Detail = ex.Message });
        }
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(Interview), StatusCodes.Status200OK)]
    public async Task<IActionResult> CancelInterview(Guid id, [FromBody] ConcurrencyActionRequest request, CancellationToken ct)
    {
        var interview = await _repository.GetInterviewByIdAsync(_userContext.TenantId, id, ct);
        if (interview == null) return NotFound();

        try
        {
            interview.Cancel(request.ExpectedRowVersion);
            await _repository.UpdateInterviewAsync(interview, ct);
            return Ok(interview);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrency conflict"))
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Concurrency Conflict", Detail = ex.Message });
        }
    }

    [HttpPost("{id:guid}/scorecard")]
    [ProducesResponseType(typeof(ScorecardSubmission), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitScorecard(Guid id, [FromBody] SubmitScorecardRequest request, CancellationToken ct)
    {
        var interview = await _repository.GetInterviewByIdAsync(_userContext.TenantId, id, ct);
        if (interview == null) return NotFound();

        var scorecard = new ScorecardSubmission(
            Guid.NewGuid(),
            interview.Id,
            interview.ApplicationId,
            _userContext.UserId.Value,
            request.RatingsJson,
            request.Strengths,
            request.Concerns,
            request.Recommendation,
            isFinalized: true
        );

        await _repository.SaveScorecardSubmissionAsync(scorecard, ct);
        await _repository.SaveOutboxMessageAsync(_userContext.TenantId, "ScorecardSubmitted", new ScorecardSubmittedEvent(
            scorecard.Id,
            interview.Id,
            _userContext.UserId.Value,
            scorecard.Recommendation
        ), ct);

        return Ok(scorecard);
    }

    [HttpGet("{id:guid}/scorecards")]
    [ProducesResponseType(typeof(IReadOnlyList<ScorecardSubmission>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetScorecards(Guid id, CancellationToken ct)
    {
        var interview = await _repository.GetInterviewByIdAsync(_userContext.TenantId, id, ct);
        if (interview == null) return NotFound();

        var scorecards = await _repository.GetScorecardsForInterviewAsync(id, ct);

        // Interviewer Confidentiality Check:
        // If current user is an interviewer, they can only see their own scorecard unless they have the manage/read_all permission.
        var isRecruiterOrManager = _userContext.HasPermission("recruitment.scorecard.read_all") 
                                   || _userContext.HasPermission("admin");

        if (!isRecruiterOrManager)
        {
            scorecards = scorecards.Where(s => s.InterviewerUserId == _userContext.UserId.Value).ToList();
        }

        return Ok(scorecards);
    }
}
