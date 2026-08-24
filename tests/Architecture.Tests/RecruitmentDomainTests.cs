using System;
using System.Linq;
using Workforce.Modules.Recruitment.Domain;
using Workforce.SharedKernel.Primitives;
using Xunit;

namespace Architecture.Tests;

public class RecruitmentDomainTests
{
    private static readonly TenantId Tenant = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly LegalEntityId LegalEntity = new(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    // ========================================================================
    // 1. REQUISITION STATE MACHINE & CONCURRENCY
    // ========================================================================

    [Fact]
    public void JobRequisition_FullLifecycleTransitions_WorkAsExpected()
    {
        var req = new JobRequisition(
            Guid.NewGuid(),
            Tenant,
            LegalEntity,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "REQ-2026-001",
            "Senior Backend Engineer",
            "مهندس برمجيات خلفية أول",
            2,
            "FullTime",
            Guid.NewGuid(),
            1
        );

        Xunit.Assert.Equal(RequisitionStatus.Draft, req.Status);
        Xunit.Assert.Equal(1u, req.RowVersion);

        // Submit for approval
        var approvalId = Guid.NewGuid();
        req.SubmitForApproval(approvalId, 1);
        Xunit.Assert.Equal(RequisitionStatus.PendingApproval, req.Status);
        Xunit.Assert.Equal(approvalId, req.ApprovalRequestId);
        Xunit.Assert.Equal(2u, req.RowVersion);

        // Approve
        req.Approve(2);
        Xunit.Assert.Equal(RequisitionStatus.Approved, req.Status);
        Xunit.Assert.Equal(3u, req.RowVersion);

        // Open
        req.Open(3);
        Xunit.Assert.Equal(RequisitionStatus.Open, req.Status);
        Xunit.Assert.NotNull(req.OpenedAtUtc);
        Xunit.Assert.Equal(4u, req.RowVersion);

        // Hold
        req.PutOnHold(4);
        Xunit.Assert.Equal(RequisitionStatus.OnHold, req.Status);
        Xunit.Assert.Equal(5u, req.RowVersion);

        // Re-open
        req.Open(5);
        Xunit.Assert.Equal(RequisitionStatus.Open, req.Status);
        Xunit.Assert.Equal(6u, req.RowVersion);

        // Close
        req.Close(6);
        Xunit.Assert.Equal(RequisitionStatus.Closed, req.Status);
        Xunit.Assert.NotNull(req.ClosedAtUtc);
        Xunit.Assert.Equal(7u, req.RowVersion);
    }

    [Fact]
    public void JobRequisition_StaleRowVersion_ThrowsConcurrencyConflict()
    {
        var req = new JobRequisition(
            Guid.NewGuid(), Tenant, LegalEntity, Guid.NewGuid(), null, null,
            Guid.NewGuid(), Guid.NewGuid(), "REQ-002", "DevOps", "عمليات التطوير", 1, "FullTime", Guid.NewGuid(), 1
        );

        // Current version is 1, submitting with version 0 must throw
        var ex = global::Xunit.Assert.Throws<InvalidOperationException>(() => req.SubmitForApproval(Guid.NewGuid(), 0));
        global::Xunit.Assert.Contains("Concurrency conflict", ex.Message);
    }

    // ========================================================================
    // 2. CANDIDATE DUPLICATE DETECTION & NORMALIZATION
    // ========================================================================

    [Fact]
    public void Candidate_EmailAndPhone_DeterministicNormalization()
    {
        var c1 = new Candidate(
            Guid.NewGuid(), Tenant,
            "Ahmed", "Hassan", "أحمد", "حسن",
            "ahmed.hassan+test@gmail.com",
            "+20 100 123 4567"
        );

        var c2 = new Candidate(
            Guid.NewGuid(), Tenant,
            "Ahmed", "Hassan", "أحمد", "حسن",
            "ahmedhassan@gmail.com",
            "00201001234567"
        );

        // Gmail normalization strips dots and +tag, Phone normalization strips spaces and standardizes 00/+
        Xunit.Assert.Equal(c1.NormalizedEmailHash, c2.NormalizedEmailHash);
        Xunit.Assert.Equal(c1.NormalizedPhoneHash, c2.NormalizedPhoneHash);
        Xunit.Assert.True(!string.IsNullOrEmpty(c1.NormalizedEmailHash));
        Xunit.Assert.True(!string.IsNullOrEmpty(c1.NormalizedPhoneHash));
    }

    // ========================================================================
    // 3. APPLICATION STAGE MOVEMENT, IDEMPOTENCY & CONCURRENCY
    // ========================================================================

    [Fact]
    public void Application_MoveToStage_RecordsImmutableHistory_AndRespectsIdempotency()
    {
        var stage1 = Guid.NewGuid();
        var stage2 = Guid.NewGuid();
        var stage3 = Guid.NewGuid();
        var actor = Guid.NewGuid();

        var app = new Application(
            Guid.NewGuid(), Tenant, LegalEntity,
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            stage1, "CareerSite", actor
        );

        Xunit.Assert.Equal(stage1, app.CurrentStageId);
        Xunit.Assert.Equal(1u, app.RowVersion);
        Xunit.Assert.True(app.StageHistory.Count == 1); // Initial created history

        // Move to Screening (Stage 2) with idempotency key
        var idemKey = "KEY-TRANSITION-123";
        app.MoveToStage(stage2, actor, "Passed initial resume check", idemKey, 1);

        Xunit.Assert.Equal(stage2, app.CurrentStageId);
        Xunit.Assert.Equal(2u, app.RowVersion);
        Xunit.Assert.Equal(2, app.StageHistory.Count);

        var latestHistory = app.StageHistory.Last();
        Xunit.Assert.Equal(stage1, latestHistory.FromStageId);
        Xunit.Assert.Equal(stage2, latestHistory.ToStageId);
        Xunit.Assert.Equal(idemKey, latestHistory.IdempotencyKey);

        // Replaying same transition with same idempotency key must be no-op (idempotent)
        app.MoveToStage(stage2, actor, "Passed initial resume check", idemKey, 2);
        Xunit.Assert.Equal(2u, app.RowVersion);
        Xunit.Assert.Equal(2, app.StageHistory.Count);

        // Stale transition must throw concurrency exception
        var ex = global::Xunit.Assert.Throws<InvalidOperationException>(() => app.MoveToStage(stage3, actor, "Next stage", "KEY-456", 1));
        global::Xunit.Assert.Contains("Concurrency conflict", ex.Message);
    }

    [Fact]
    public void Application_RejectionAndWithdrawal_AreSeparateDispositions()
    {
        var app1 = new Application(Guid.NewGuid(), Tenant, LegalEntity, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "LinkedIn");
        app1.Reject("EXPERIENCE_MISMATCH", "Insufficient senior experience", Guid.NewGuid(), 1);

        Xunit.Assert.Equal(ApplicationStatus.Rejected, app1.Status);
        Xunit.Assert.Equal("EXPERIENCE_MISMATCH", app1.DispositionReason);
        Xunit.Assert.Equal("Insufficient senior experience", app1.DispositionNote);

        var app2 = new Application(Guid.NewGuid(), Tenant, LegalEntity, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Referral");
        app2.Withdraw("Accepted another offer", Guid.NewGuid(), 1);

        Xunit.Assert.Equal(ApplicationStatus.Withdrawn, app2.Status);
        Xunit.Assert.Equal("WITHDRAWN_BY_CANDIDATE", app2.DispositionReason);
        Xunit.Assert.Equal("Accepted another offer", app2.DispositionNote);
    }

    // ========================================================================
    // 4. INTERVIEWS, TIMEZONES & SCORECARDS
    // ========================================================================

    [Fact]
    public void Interview_StoresUtcAndExplicitTimezone_SupportsRescheduling()
    {
        var startLocal = new DateTime(2026, 9, 1, 14, 0, 0, DateTimeKind.Utc);
        var endLocal = new DateTime(2026, 9, 1, 15, 0, 0, DateTimeKind.Utc);

        var interview = new Interview(
            Guid.NewGuid(), Tenant, Guid.NewGuid(), Guid.NewGuid(),
            "System Architecture Panel", InterviewType.Technical,
            startLocal, endLocal, "Africa/Cairo"
        );

        Xunit.Assert.Equal(InterviewStatus.Scheduled, interview.Status);
        Xunit.Assert.Equal("Africa/Cairo", interview.Timezone);
        Xunit.Assert.Equal(1u, interview.RowVersion);

        // Reschedule
        var newStart = startLocal.AddDays(2);
        var newEnd = endLocal.AddDays(2);
        interview.Reschedule(newStart, newEnd, "Africa/Cairo", "https://meet.google.com/xyz", 1);

        Xunit.Assert.Equal(InterviewStatus.Rescheduled, interview.Status);
        Xunit.Assert.Equal(newStart, interview.ScheduledStartUtc);
        Xunit.Assert.Equal(2u, interview.RowVersion);

        // Scorecard submission
        var scorecard = new ScorecardSubmission(
            Guid.NewGuid(), interview.Id, interview.ApplicationId, Guid.NewGuid(),
            "{\"systemDesign\":5,\"coding\":4}", "Strong deep-dive knowledge", "None", ScorecardRecommendation.StrongYes
        );

        interview.SubmitScorecard(scorecard, 2);
        Xunit.Assert.True(interview.Scorecards.Count == 1);
        Xunit.Assert.Equal(ScorecardRecommendation.StrongYes, interview.Scorecards.First().Recommendation);
        Xunit.Assert.Equal(3u, interview.RowVersion);
    }

    // ========================================================================
    // 5. OFFER STATE MACHINE, VERSIONING & APPROVAL
    // ========================================================================

    [Fact]
    public void Offer_FullLifecycle_WithApprovalAndAcceptance()
    {
        var offer = new Offer(
            Guid.NewGuid(), Tenant, LegalEntity, Guid.NewGuid(), Guid.NewGuid(), 1,
            "Staff Engineer", "مهندس أول متميز",
            new DateOnly(2026, 10, 1), 75000.00m, "EGP",
            "[{\"name\":\"Housing\",\"amount\":15000}]", "Subject to background check"
        );

        Xunit.Assert.Equal(OfferStatus.Draft, offer.Status);
        Xunit.Assert.Equal(1, offer.OfferVersionNumber);
        Xunit.Assert.Equal(75000.00m, offer.BaseSalaryMonthly);
        Xunit.Assert.Equal(1u, offer.RowVersion);

        // Submit for approval
        var approvalId = Guid.NewGuid();
        offer.SubmitForApproval(approvalId, 1);
        Xunit.Assert.Equal(OfferStatus.PendingApproval, offer.Status);
        Xunit.Assert.Equal(2u, offer.RowVersion);

        // Approve
        offer.Approve(2);
        Xunit.Assert.Equal(OfferStatus.Approved, offer.Status);
        Xunit.Assert.Equal(3u, offer.RowVersion);

        // Issue
        offer.Issue(3);
        Xunit.Assert.Equal(OfferStatus.Issued, offer.Status);
        Xunit.Assert.NotNull(offer.IssuedAtUtc);
        Xunit.Assert.Equal(4u, offer.RowVersion);

        // Accept
        offer.Accept(4);
        Xunit.Assert.Equal(OfferStatus.Accepted, offer.Status);
        Xunit.Assert.NotNull(offer.AcceptedAtUtc);
        Xunit.Assert.Equal(5u, offer.RowVersion);
    }

    // ========================================================================
    // 6. HIRE HANDOFF IDEMPOTENCY
    // ========================================================================

    [Fact]
    public void Application_MarkHired_IdempotentExecution()
    {
        var app = new Application(
            Guid.NewGuid(), Tenant, LegalEntity,
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), "Direct"
        );

        var personId = Guid.NewGuid();
        var employmentId = Guid.NewGuid();
        var actor = Guid.NewGuid();

        // First hire
        app.MarkHired(personId, employmentId, actor, 1);
        Xunit.Assert.Equal(ApplicationStatus.Hired, app.Status);
        Xunit.Assert.Equal(personId, app.HiredPersonId);
        Xunit.Assert.Equal(employmentId, app.HiredEmploymentId);
        Xunit.Assert.Equal(2u, app.RowVersion);

        // Idempotent second hire with same IDs must succeed without error
        app.MarkHired(personId, employmentId, actor, 2);
        Xunit.Assert.Equal(ApplicationStatus.Hired, app.Status);
        Xunit.Assert.Equal(2u, app.RowVersion);

        // Attempting to hire with DIFFERENT IDs must throw conflict
        var ex = global::Xunit.Assert.Throws<InvalidOperationException>(() => app.MarkHired(Guid.NewGuid(), Guid.NewGuid(), actor, 2));
        global::Xunit.Assert.Contains("already marked as Hired with different Person/Employment", ex.Message);
    }
}
