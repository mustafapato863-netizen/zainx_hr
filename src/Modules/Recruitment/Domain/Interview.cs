using System;
using System.Collections.Generic;
using System.Linq;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Recruitment.Domain;

public class Interview
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public Guid ApplicationId { get; private set; }
    public Guid StageId { get; private set; }
    public string Title { get; private set; }
    public InterviewType InterviewType { get; private set; }
    public DateTime ScheduledStartUtc { get; private set; }
    public DateTime ScheduledEndUtc { get; private set; }
    public string Timezone { get; private set; }
    public string? LocationOrMeetingUrl { get; private set; }
    public InterviewStatus Status { get; private set; }
    public string? InterviewKitJson { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public uint RowVersion { get; private set; }

    private readonly List<InterviewParticipant> _participants = new();
    public IReadOnlyList<InterviewParticipant> Participants => _participants.AsReadOnly();

    private readonly List<ScorecardSubmission> _scorecards = new();
    public IReadOnlyList<ScorecardSubmission> Scorecards => _scorecards.AsReadOnly();

    private Interview()
    {
        TenantId = default;
        Title = string.Empty;
        Timezone = string.Empty;
    }

    public Interview(
        Guid id,
        TenantId tenantId,
        Guid applicationId,
        Guid stageId,
        string title,
        InterviewType interviewType,
        DateTime scheduledStartUtc,
        DateTime scheduledEndUtc,
        string timezone,
        string? locationOrMeetingUrl = null,
        string? interviewKitJson = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (tenantId == default || tenantId.Value == Guid.Empty) throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        if (applicationId == Guid.Empty) throw new ArgumentException("ApplicationId cannot be empty.", nameof(applicationId));
        if (stageId == Guid.Empty) throw new ArgumentException("StageId cannot be empty.", nameof(stageId));
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.", nameof(title));
        if (scheduledEndUtc <= scheduledStartUtc) throw new ArgumentException("End time must be after start time.", nameof(scheduledEndUtc));
        if (string.IsNullOrWhiteSpace(timezone)) throw new ArgumentException("Timezone is required.", nameof(timezone));

        Id = id;
        TenantId = tenantId;
        ApplicationId = applicationId;
        StageId = stageId;
        Title = title.Trim();
        InterviewType = interviewType;
        ScheduledStartUtc = scheduledStartUtc.Kind == DateTimeKind.Utc ? scheduledStartUtc : scheduledStartUtc.ToUniversalTime();
        ScheduledEndUtc = scheduledEndUtc.Kind == DateTimeKind.Utc ? scheduledEndUtc : scheduledEndUtc.ToUniversalTime();
        Timezone = timezone.Trim();
        LocationOrMeetingUrl = locationOrMeetingUrl?.Trim();
        InterviewKitJson = interviewKitJson ?? "{}";
        Status = InterviewStatus.Scheduled;
        CreatedAtUtc = DateTime.UtcNow;
        RowVersion = 1;
    }

    public static Interview Reconstitute(
        Guid id,
        TenantId tenantId,
        Guid applicationId,
        Guid stageId,
        string title,
        InterviewType interviewType,
        DateTime scheduledStartUtc,
        DateTime scheduledEndUtc,
        string timezone,
        string? locationOrMeetingUrl,
        InterviewStatus status,
        string? interviewKitJson,
        DateTime createdAtUtc,
        uint rowVersion,
        IEnumerable<InterviewParticipant>? participants = null,
        IEnumerable<ScorecardSubmission>? scorecards = null)
    {
        var interview = new Interview
        {
            Id = id,
            TenantId = tenantId,
            ApplicationId = applicationId,
            StageId = stageId,
            Title = title,
            InterviewType = interviewType,
            ScheduledStartUtc = scheduledStartUtc,
            ScheduledEndUtc = scheduledEndUtc,
            Timezone = timezone,
            LocationOrMeetingUrl = locationOrMeetingUrl,
            Status = status,
            InterviewKitJson = interviewKitJson,
            CreatedAtUtc = createdAtUtc,
            RowVersion = rowVersion
        };
        if (participants != null) interview._participants.AddRange(participants);
        if (scorecards != null) interview._scorecards.AddRange(scorecards);
        return interview;
    }

    public void AddParticipant(Guid interviewerUserId, InterviewerRole role, bool isRequired = true)
    {
        if (_participants.Any(p => p.InterviewerUserId == interviewerUserId))
            return;

        _participants.Add(new InterviewParticipant(Guid.NewGuid(), Id, interviewerUserId, role, isRequired));
    }

    public void Reschedule(
        DateTime newStartUtc,
        DateTime newEndUtc,
        string timezone,
        string? locationOrMeetingUrl,
        uint expectedRowVersion)
    {
        ValidateConcurrency(expectedRowVersion);
        if (Status != InterviewStatus.Scheduled && Status != InterviewStatus.Rescheduled)
            throw new InvalidOperationException($"Cannot reschedule interview in status '{Status}'.");

        if (newEndUtc <= newStartUtc) throw new ArgumentException("End time must be after start time.", nameof(newEndUtc));

        ScheduledStartUtc = newStartUtc.Kind == DateTimeKind.Utc ? newStartUtc : newStartUtc.ToUniversalTime();
        ScheduledEndUtc = newEndUtc.Kind == DateTimeKind.Utc ? newEndUtc : newEndUtc.ToUniversalTime();
        Timezone = timezone.Trim();
        LocationOrMeetingUrl = locationOrMeetingUrl?.Trim() ?? LocationOrMeetingUrl;
        Status = InterviewStatus.Rescheduled;
        RowVersion++;
    }

    public void Complete(uint expectedRowVersion)
    {
        ValidateConcurrency(expectedRowVersion);
        if (Status != InterviewStatus.Scheduled && Status != InterviewStatus.Rescheduled)
            throw new InvalidOperationException($"Cannot complete interview in status '{Status}'.");

        Status = InterviewStatus.Completed;
        RowVersion++;
    }

    public void Cancel(uint expectedRowVersion)
    {
        ValidateConcurrency(expectedRowVersion);
        if (Status == InterviewStatus.Cancelled || Status == InterviewStatus.Completed)
            throw new InvalidOperationException($"Cannot cancel interview in status '{Status}'.");

        Status = InterviewStatus.Cancelled;
        RowVersion++;
    }

    public void SubmitScorecard(ScorecardSubmission scorecard, uint expectedRowVersion)
    {
        ValidateConcurrency(expectedRowVersion);
        var existing = _scorecards.FirstOrDefault(s => s.InterviewerUserId == scorecard.InterviewerUserId);
        if (existing != null)
        {
            if (existing.IsFinalized)
            {
                throw new InvalidOperationException($"Scorecard already submitted and finalized for interviewer '{scorecard.InterviewerUserId}'. Submitted scorecards are immutable.");
            }
            _scorecards.Remove(existing);
        }
        _scorecards.Add(scorecard);
        RowVersion++;
    }

    private void ValidateConcurrency(uint expectedRowVersion)
    {
        if (RowVersion != expectedRowVersion)
        {
            throw new InvalidOperationException($"Concurrency conflict: Interview has been modified. Expected version {expectedRowVersion}, current version {RowVersion}.");
        }
    }
}

public class InterviewParticipant
{
    public Guid Id { get; private set; }
    public Guid InterviewId { get; private set; }
    public Guid InterviewerUserId { get; private set; }
    public InterviewerRole Role { get; private set; }
    public bool IsRequired { get; private set; }

    private InterviewParticipant()
    {
    }

    public InterviewParticipant(
        Guid id,
        Guid interviewId,
        Guid interviewerUserId,
        InterviewerRole role,
        bool isRequired = true)
    {
        Id = id;
        InterviewId = interviewId;
        InterviewerUserId = interviewerUserId;
        Role = role;
        IsRequired = isRequired;
    }
}

public class ScorecardSubmission
{
    public Guid Id { get; private set; }
    public Guid InterviewId { get; private set; }
    public Guid ApplicationId { get; private set; }
    public Guid InterviewerUserId { get; private set; }
    public string RatingsJson { get; private set; }
    public string? Strengths { get; private set; }
    public string? Concerns { get; private set; }
    public ScorecardRecommendation Recommendation { get; private set; }
    public bool IsFinalized { get; private set; }
    public DateTime SubmittedAtUtc { get; private set; }
    public uint RowVersion { get; private set; }

    private ScorecardSubmission()
    {
        RatingsJson = "{}";
    }

    public ScorecardSubmission(
        Guid id,
        Guid interviewId,
        Guid applicationId,
        Guid interviewerUserId,
        string ratingsJson,
        string? strengths,
        string? concerns,
        ScorecardRecommendation recommendation,
        bool isFinalized = true)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (interviewId == Guid.Empty) throw new ArgumentException("InterviewId cannot be empty.", nameof(interviewId));
        if (applicationId == Guid.Empty) throw new ArgumentException("ApplicationId cannot be empty.", nameof(applicationId));
        if (interviewerUserId == Guid.Empty) throw new ArgumentException("InterviewerUserId cannot be empty.", nameof(interviewerUserId));

        Id = id;
        InterviewId = interviewId;
        ApplicationId = applicationId;
        InterviewerUserId = interviewerUserId;
        RatingsJson = ratingsJson ?? "{}";
        Strengths = strengths?.Trim();
        Concerns = concerns?.Trim();
        Recommendation = recommendation;
        IsFinalized = isFinalized;
        SubmittedAtUtc = DateTime.UtcNow;
        RowVersion = 1;
    }

    public static ScorecardSubmission Reconstitute(
        Guid id,
        Guid interviewId,
        Guid applicationId,
        Guid interviewerUserId,
        string ratingsJson,
        string? strengths,
        string? concerns,
        ScorecardRecommendation recommendation,
        bool isFinalized,
        DateTime submittedAtUtc,
        uint rowVersion)
    {
        return new ScorecardSubmission
        {
            Id = id,
            InterviewId = interviewId,
            ApplicationId = applicationId,
            InterviewerUserId = interviewerUserId,
            RatingsJson = ratingsJson,
            Strengths = strengths,
            Concerns = concerns,
            Recommendation = recommendation,
            IsFinalized = isFinalized,
            SubmittedAtUtc = submittedAtUtc,
            RowVersion = rowVersion
        };
    }
}
