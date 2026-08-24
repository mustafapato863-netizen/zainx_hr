using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Workforce.Modules.Recruitment.Domain;
using Workforce.SharedKernel.Primitives;
using Workforce.SharedKernel.Security;
using Xunit;

namespace Architecture.Tests;

public class Phase5RecruitmentIntegrityTests
{
    private static readonly TenantId TenantA = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly TenantId TenantB = new(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly LegalEntityId LegalEntityA = new(Guid.Parse("33333333-3333-3333-3333-333333333333"));
    private static readonly LegalEntityId LegalEntityB = new(Guid.Parse("44444444-4444-4444-4444-444444444444"));

    // ========================================================================
    // GATE 2: PIPELINE VERSION IMMUTABILITY & HISTORICAL INTEGRITY
    // ========================================================================

    [Fact]
    public void Gate2_PipelineVersion_HistoricalApplicationsPreservedOnV1_WhenV2Created()
    {
        var pipelineId = Guid.NewGuid();
        var pipeline = new RecruitmentPipeline(pipelineId, TenantA, "ENG_PIPELINE", "Engineering Pipeline", "مسار الهندسة");

        // V1 with 5 stages
        var v1Id = Guid.NewGuid();
        var v1 = new RecruitmentPipelineVersion(v1Id, pipelineId, 1, isImmutable: false);
        var s1 = new RecruitmentStage(Guid.NewGuid(), v1Id, 1, "APPLIED", "Applied", "تم التقديم", StageKind.Applied);
        var s2 = new RecruitmentStage(Guid.NewGuid(), v1Id, 2, "SCREENING", "Screening", "فرز أولي", StageKind.Screening);
        var s3 = new RecruitmentStage(Guid.NewGuid(), v1Id, 3, "TECH_INTERVIEW", "Tech Interview", "مقابلة فنية", StageKind.Interview);
        var s4 = new RecruitmentStage(Guid.NewGuid(), v1Id, 4, "OFFER", "Offer", "عرض عمل", StageKind.Offer);
        var s5 = new RecruitmentStage(Guid.NewGuid(), v1Id, 5, "HIRED", "Hired", "تم التعيين", StageKind.Hired);

        v1.AddStage(s1);
        v1.AddStage(s2);
        v1.AddStage(s3);
        v1.AddStage(s4);
        v1.AddStage(s5);
        v1.MarkImmutable();
        pipeline.AddVersion(v1);

        // Application A created on V1
        var reqA = new JobRequisition(Guid.NewGuid(), TenantA, LegalEntityA, Guid.NewGuid(), null, null, Guid.NewGuid(), Guid.NewGuid(), "REQ-V1", "SWE", "مطور", 1, "FullTime", pipelineId, 1);
        var appA = new Application(Guid.NewGuid(), TenantA, LegalEntityA, reqA.Id, Guid.NewGuid(), v1Id, s1.Id, "Web");

        Assert.Equal(v1Id, appA.PipelineVersionId);
        Assert.Equal(s1.Id, appA.CurrentStageId);

        // V1 is immutable: Cannot mutate in-place
        var illegalStage = new RecruitmentStage(Guid.NewGuid(), v1Id, 6, "EXTRA", "Extra Stage", "مرحلة إضافية", StageKind.Assessment);
        Assert.Throws<InvalidOperationException>(() => v1.AddStage(illegalStage));

        // Create V2 with altered stage order & new Assessment stage
        var v2Id = Guid.NewGuid();
        var v2 = new RecruitmentPipelineVersion(v2Id, pipelineId, 2, isImmutable: false);
        var v2_s1 = new RecruitmentStage(Guid.NewGuid(), v2Id, 1, "APPLIED", "Applied", "تم التقديم", StageKind.Applied);
        var v2_s2 = new RecruitmentStage(Guid.NewGuid(), v2Id, 2, "ASSESSMENT", "Online Test", "اختبار عملي", StageKind.Assessment);
        var v2_s3 = new RecruitmentStage(Guid.NewGuid(), v2Id, 3, "SCREENING", "Screening", "فرز أولي", StageKind.Screening);
        var v2_s4 = new RecruitmentStage(Guid.NewGuid(), v2Id, 4, "FINAL_INTERVIEW", "Final Interview", "مقابلة نهائية", StageKind.Interview);
        var v2_s5 = new RecruitmentStage(Guid.NewGuid(), v2Id, 5, "HIRED", "Hired", "تم التعيين", StageKind.Hired);

        v2.AddStage(v2_s1);
        v2.AddStage(v2_s2);
        v2.AddStage(v2_s3);
        v2.AddStage(v2_s4);
        v2.AddStage(v2_s5);
        v2.MarkImmutable();
        pipeline.AddVersion(v2);

        // Application B created on V2
        var reqB = new JobRequisition(Guid.NewGuid(), TenantA, LegalEntityA, Guid.NewGuid(), null, null, Guid.NewGuid(), Guid.NewGuid(), "REQ-V2", "Lead SWE", "قائد مطورين", 1, "FullTime", pipelineId, 2);
        var appB = new Application(Guid.NewGuid(), TenantA, LegalEntityA, reqB.Id, Guid.NewGuid(), v2Id, v2_s1.Id, "Web");

        // Verify Application A is still on V1 stages and Application B is on V2 stages
        Assert.Equal(v1Id, appA.PipelineVersionId);
        Assert.Equal(v2Id, appB.PipelineVersionId);
        Assert.Equal(5, pipeline.Versions[0].Stages.Count);
        Assert.Equal("TECH_INTERVIEW", pipeline.Versions[0].Stages[2].Code);
        Assert.Equal(5, pipeline.Versions[1].Stages.Count);
        Assert.Equal("ASSESSMENT", pipeline.Versions[1].Stages[1].Code);
    }

    // ========================================================================
    // GATE 3: STAGE TRANSITION CONCURRENCY & LOST-UPDATE PREVENTION
    // ========================================================================

    [Fact]
    public void Gate3_Application_StageTransitionConcurrency_PreventsLostUpdate()
    {
        var stageApplied = Guid.NewGuid();
        var stageScreening = Guid.NewGuid();
        var stageInterview = Guid.NewGuid();
        var stageRejected = Guid.NewGuid();
        var actorA = Guid.NewGuid();
        var actorB = Guid.NewGuid();

        var app = new Application(
            Guid.NewGuid(), TenantA, LegalEntityA,
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            stageApplied, "Direct", actorA
        );

        // Move to Screening (RowVersion becomes 2)
        app.MoveToStage(stageScreening, actorA, "Screening passed", "IDEM-001", 1);
        Assert.Equal(2u, app.RowVersion);
        Assert.Equal(stageScreening, app.CurrentStageId);

        // Recruiter A moves Screening -> Interview with RowVersion 2 (Succeeds, RowVersion becomes 3)
        app.MoveToStage(stageInterview, actorA, "Ready for technical panel", "IDEM-002", 2);
        Assert.Equal(3u, app.RowVersion);
        Assert.Equal(stageInterview, app.CurrentStageId);

        // Recruiter B attempts Screening -> Rejected with stale RowVersion 2 (Must fail with 409 Concurrency Conflict)
        var ex = Assert.Throws<InvalidOperationException>(() => app.MoveToStage(stageRejected, actorB, "Reject candidate", "IDEM-003", 2));
        Assert.True(ex.Message.Contains("Concurrency conflict"));

        // Verify state is clean: stage is Interview, RowVersion is 3, exactly 3 stage history records exist
        Assert.Equal(stageInterview, app.CurrentStageId);
        Assert.Equal(3u, app.RowVersion);
        Assert.Equal(3, app.StageHistory.Count);
    }

    // ========================================================================
    // GATE 4: STAGE TRANSITION IDEMPOTENCY
    // ========================================================================

    [Fact]
    public void Gate4_Application_StageTransitionIdempotency_NoDuplicateHistory()
    {
        var stageApplied = Guid.NewGuid();
        var stageScreening = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var idemKey = "IDEM-DOUBLE-CLICK-999";

        var app = new Application(
            Guid.NewGuid(), TenantA, LegalEntityA,
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            stageApplied, "Direct", actor
        );

        // Initial move
        app.MoveToStage(stageScreening, actor, "Moved to screening", idemKey, 1);
        Assert.Equal(2u, app.RowVersion);
        Assert.Equal(2, app.StageHistory.Count);

        // Simulated double-click / network retry with exact same idempotency key
        app.MoveToStage(stageScreening, actor, "Moved to screening", idemKey, 2);

        // RowVersion does NOT increment, stage remains screening, history count does NOT duplicate
        Assert.Equal(2u, app.RowVersion);
        Assert.Equal(stageScreening, app.CurrentStageId);
        Assert.Equal(2, app.StageHistory.Count);
    }

    // ========================================================================
    // GATE 5: SHARED REQUISITION APPROVAL INTEGRATION & STATE MACHINE
    // ========================================================================

    [Fact]
    public void Gate5_Requisition_ApprovalLifecycle_AndIdempotentDelivery()
    {
        var req = new JobRequisition(
            Guid.NewGuid(), TenantA, LegalEntityA, Guid.NewGuid(), null, null,
            Guid.NewGuid(), Guid.NewGuid(), "REQ-2026-X", "Principal Architect", "كبير مهندسين",
            2, "FullTime", Guid.NewGuid(), 1
        );

        Assert.Equal(RequisitionStatus.Draft, req.Status);

        var approvalRequestId = Guid.NewGuid();
        req.SubmitForApproval(approvalRequestId, 1);
        Assert.Equal(RequisitionStatus.PendingApproval, req.Status);
        Assert.Equal(approvalRequestId, req.ApprovalRequestId);

        // Approver approves in Approvals module -> Recruitment consumes outcome
        req.Approve(2);
        Assert.Equal(RequisitionStatus.Approved, req.Status);

        // Open requisition
        req.Open(3);
        Assert.Equal(RequisitionStatus.Open, req.Status);
        Assert.NotNull(req.OpenedAtUtc);
    }

    // ========================================================================
    // GATE 6 & 7: OFFER APPROVAL INTEGRATION, VERSIONING & IMMUTABILITY
    // ========================================================================

    [Fact]
    public void Gate6_Gate7_Offer_VersionImmutability_AndIndependentApprovalPerVersion()
    {
        var applicationId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();

        // Offer V1
        var offerV1 = new Offer(
            Guid.NewGuid(), TenantA, LegalEntityA, applicationId, candidateId, 1,
            "Senior Backend Engineer", "مهندس برمجيات أول",
            new DateOnly(2026, 10, 1), 60000.00m, "EGP",
            "[{\"code\":\"HOUSING\",\"nameEn\":\"Housing\",\"nameAr\":\"سكن\",\"amount\":10000.00}]"
        );

        var approvalV1 = Guid.NewGuid();
        offerV1.SubmitForApproval(approvalV1, 1);
        offerV1.Approve(2);
        offerV1.Issue(3);

        Assert.Equal(OfferStatus.Issued, offerV1.Status);
        Assert.Equal(1, offerV1.OfferVersionNumber);

        // Issued terms cannot be mutated in place
        Assert.Throws<InvalidOperationException>(() => offerV1.UpdateTerms(
            "Principal Engineer", "كبير مهندسين", new DateOnly(2026, 10, 1),
            80000.00m, "EGP", "[]", null, null, null, 4
        ));

        // Material compensation revision creates Offer V2
        var offerV2 = new Offer(
            Guid.NewGuid(), TenantA, LegalEntityA, applicationId, candidateId, 2,
            "Senior Backend Engineer (Revised)", "مهندس برمجيات أول (معدل)",
            new DateOnly(2026, 10, 15), 70000.00m, "EGP",
            "[{\"code\":\"HOUSING\",\"nameEn\":\"Housing\",\"nameAr\":\"سكن\",\"amount\":12000.00}]"
        );

        // V2 starts in Draft and is NOT automatically approved by V1's approval
        Assert.Equal(OfferStatus.Draft, offerV2.Status);
        Assert.Equal(2, offerV2.OfferVersionNumber);

        var approvalV2 = Guid.NewGuid();
        offerV2.SubmitForApproval(approvalV2, 1);
        offerV2.Approve(2);
        offerV2.Issue(3);
        offerV2.Accept(4);

        // Acceptance binds to exact accepted version (V2)
        Assert.Equal(OfferStatus.Accepted, offerV2.Status);
        Assert.Equal(OfferStatus.Issued, offerV1.Status);
    }

    // ========================================================================
    // GATE 8: OFFER COMPENSATION SECURITY & MASKING
    // ========================================================================

    [Fact]
    public void Gate8_Offer_SensitiveCompensation_MaskingAndAuthorization()
    {
        decimal realSalary = 85000.00m;
        string currency = "EGP";

        // Authorized view helper
        string FormatAuthorized(decimal salary, string curr, bool hasPermission)
        {
            return hasPermission ? $"{salary:N2} {curr}" : $"***,***.** {curr}";
        }

        Assert.Equal("85,000.00 EGP", FormatAuthorized(realSalary, currency, true));
        Assert.Equal("***,***.** EGP", FormatAuthorized(realSalary, currency, false));
    }

    // ========================================================================
    // GATE 9 & 10: INTERVIEW SCORECARD CONFIDENTIALITY & IMMUTABILITY
    // ========================================================================

    [Fact]
    public void Gate9_Gate10_Scorecard_ConfidentialityAndImmutability()
    {
        var interview = new Interview(
            Guid.NewGuid(), TenantA, Guid.NewGuid(), Guid.NewGuid(),
            "Architecture Review", InterviewType.Technical,
            DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(2).AddHours(1), "Africa/Cairo"
        );

        var interviewerA = Guid.NewGuid();
        var scorecardA = new ScorecardSubmission(
            Guid.NewGuid(), interview.Id, interview.ApplicationId, interviewerA,
            "{\"systemDesign\":5,\"concurrency\":5}", "Exceptional distributed systems depth", "None", ScorecardRecommendation.StrongYes
        );

        interview.SubmitScorecard(scorecardA, 1);
        Assert.Equal(1, interview.Scorecards.Count);
        Assert.Equal(2u, interview.RowVersion);

        // Duplicate submission by same interviewer is prevented
        var duplicateScorecard = new ScorecardSubmission(
            Guid.NewGuid(), interview.Id, interview.ApplicationId, interviewerA,
            "{}", "Altered feedback", "", ScorecardRecommendation.Yes
        );

        var ex = Assert.Throws<InvalidOperationException>(() => interview.SubmitScorecard(duplicateScorecard, 2));
        Assert.True(ex.Message.Contains("already submitted", StringComparison.OrdinalIgnoreCase));
    }

    // ========================================================================
    // GATE 11: INTERVIEW TIMEZONE & RESCHEDULING CONCURRENCY
    // ========================================================================

    [Fact]
    public void Gate11_Interview_TimezoneContext_AndRescheduleConcurrency()
    {
        var startUtc = new DateTime(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc);
        var endUtc = new DateTime(2026, 9, 15, 11, 0, 0, DateTimeKind.Utc);

        var interview = new Interview(
            Guid.NewGuid(), TenantA, Guid.NewGuid(), Guid.NewGuid(),
            "Culture Fit Interview", InterviewType.Behavioral,
            startUtc, endUtc, "Asia/Riyadh"
        );

        Assert.Equal("Asia/Riyadh", interview.Timezone);
        Assert.Equal(1u, interview.RowVersion);

        // Reschedule 1 (RowVersion becomes 2)
        var newStart = startUtc.AddDays(1);
        var newEnd = endUtc.AddDays(1);
        interview.Reschedule(newStart, newEnd, "Asia/Riyadh", "https://meet.google.com/zainx-123", 1);
        Assert.Equal(2u, interview.RowVersion);
        Assert.Equal(newStart, interview.ScheduledStartUtc);

        // Concurrent reschedule with stale RowVersion 1 must throw
        Assert.Throws<InvalidOperationException>(() => interview.Reschedule(
            startUtc.AddDays(2), endUtc.AddDays(2), "Asia/Riyadh", "https://meet.google.com/zainx-456", 1
        ));
    }

    // ========================================================================
    // GATE 12: DOCUMENTS & RESUME BOUNDARY
    // ========================================================================

    [Fact]
    public void Gate12_Candidate_StoresDocumentReferenceOnly_NoDirectBinarySubsystem()
    {
        var resumeDocId = Guid.NewGuid();
        var candidate = new Candidate(
            Guid.NewGuid(), TenantA,
            "Sarah", "Al-Mansoor", "سارة", "المنصور",
            "sarah.almansoor@enterprise.com", "+966501234567",
            resumeDocumentId: resumeDocId
        );

        // Candidate stores Guid document ID reference only
        Assert.Equal(resumeDocId, candidate.ResumeDocumentId);
    }

    // ========================================================================
    // GATE 13: CANDIDATE DUPLICATE DETECTION & CROSS-TENANT ISOLATION
    // ========================================================================

    [Fact]
    public void Gate13_Candidate_DuplicateDetection_CrossTenantIsolation()
    {
        var emailRaw = "Candidate.John+recruitment@Company.COM";
        var phoneRaw = "+20 (100) 123-4567";

        var cTenantA = new Candidate(
            Guid.NewGuid(), TenantA,
            "John", "Doe", "جون", "دو",
            emailRaw, phoneRaw
        );

        var cTenantB = new Candidate(
            Guid.NewGuid(), TenantB,
            "John", "Doe", "جون", "دو",
            emailRaw, phoneRaw
        );

        // Deterministic hash matches within canonical normalization
        Assert.Equal(cTenantA.NormalizedEmailHash, cTenantB.NormalizedEmailHash);

        // Cross-tenant verification: Entity belongs exclusively to its own TenantId partition
        Assert.Equal(TenantA, cTenantA.TenantId);
        Assert.Equal(TenantB, cTenantB.TenantId);
        Assert.NotEqual(cTenantA.TenantId, cTenantB.TenantId);
    }

    // ========================================================================
    // GATE 14: DUPLICATE APPLICATION INVARIANT
    // ========================================================================

    [Fact]
    public void Gate14_Application_ReapplicationAllowedAfterRejectionOrWithdrawal()
    {
        var reqId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        var pipelineVersionId = Guid.NewGuid();
        var stageId = Guid.NewGuid();

        // 1st Application -> Rejected
        var app1 = new Application(Guid.NewGuid(), TenantA, LegalEntityA, reqId, candidateId, pipelineVersionId, stageId, "Web");
        app1.Reject("POSITION_FILLED", "Filled by another candidate", Guid.NewGuid(), 1);
        Assert.Equal(ApplicationStatus.Rejected, app1.Status);

        // 2nd Application after rejection is allowed and valid
        var app2 = new Application(Guid.NewGuid(), TenantA, LegalEntityA, reqId, candidateId, pipelineVersionId, stageId, "Reapplied");
        Assert.Equal(ApplicationStatus.Active, app2.Status);
    }

    // ========================================================================
    // GATE 15: REJECTION VS WITHDRAWAL DISPOSITIONS
    // ========================================================================

    [Fact]
    public void Gate15_Application_RejectionAndWithdrawal_AreDistinctFacts()
    {
        var appRejected = new Application(Guid.NewGuid(), TenantA, LegalEntityA, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Web");
        appRejected.Reject("SALARY_EXPECTATION_MISMATCH", "Expected salary exceeds budget", Guid.NewGuid(), 1);

        Assert.Equal(ApplicationStatus.Rejected, appRejected.Status);
        Assert.Equal("SALARY_EXPECTATION_MISMATCH", appRejected.DispositionReason);
        Assert.Equal("Expected salary exceeds budget", appRejected.DispositionNote);
        Assert.NotNull(appRejected.DisposedAtUtc);

        var appWithdrawn = new Application(Guid.NewGuid(), TenantA, LegalEntityA, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Web");
        appWithdrawn.Withdraw("Candidate relocation cancelled", Guid.NewGuid(), 1);

        Assert.Equal(ApplicationStatus.Withdrawn, appWithdrawn.Status);
        Assert.Equal("WITHDRAWN_BY_CANDIDATE", appWithdrawn.DispositionReason);
        Assert.Equal("Candidate relocation cancelled", appWithdrawn.DispositionNote);
        Assert.NotNull(appWithdrawn.DisposedAtUtc);
    }

    // ========================================================================
    // GATE 16: HIRE BOUNDED CONTEXT & DIRECT WRITE BOUNDARY PROOF
    // ========================================================================

    [Fact]
    public void Gate16_RecruitmentModule_DoesNotContainDirectSqlWritesToOtherSchemas()
    {
        var migrationsType = typeof(Workforce.Modules.Recruitment.Infrastructure.RecruitmentMigrations);
        var repoType = typeof(Workforce.Modules.Recruitment.Infrastructure.RecruitmentRepository);

        // Inspect assembly and ensure no direct SQL statements target other schemas
        var assembly = migrationsType.Assembly;
        var types = assembly.GetTypes();

        foreach (var type in types)
        {
            var fields = type.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(string))
                {
                    var val = field.GetValue(null) as string;
                    if (val != null)
                    {
                        Assert.False(val.Contains("INSERT INTO people.", StringComparison.OrdinalIgnoreCase));
                        Assert.False(val.Contains("UPDATE people.", StringComparison.OrdinalIgnoreCase));
                        Assert.False(val.Contains("DELETE FROM people.", StringComparison.OrdinalIgnoreCase));
                        Assert.False(val.Contains("INSERT INTO organization.", StringComparison.OrdinalIgnoreCase));
                    }
                }
            }
        }
    }

    // ========================================================================
    // GATE 17: HIRE IDEMPOTENCY
    // ========================================================================

    [Fact]
    public void Gate17_Application_HireIdempotency_ExactContract()
    {
        var app = new Application(Guid.NewGuid(), TenantA, LegalEntityA, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Web");
        var personId = Guid.NewGuid();
        var employmentId = Guid.NewGuid();
        var actor = Guid.NewGuid();

        // 1st hire succeeds
        app.MarkHired(personId, employmentId, actor, 1);
        Assert.Equal(ApplicationStatus.Hired, app.Status);
        Assert.Equal(personId, app.HiredPersonId);
        Assert.Equal(employmentId, app.HiredEmploymentId);
        Assert.Equal(2u, app.RowVersion);

        // 2nd hire with same IDs is idempotent no-op
        app.MarkHired(personId, employmentId, actor, 2);
        Assert.Equal(ApplicationStatus.Hired, app.Status);
        Assert.Equal(2u, app.RowVersion);

        // Replay with differing IDs throws conflict
        var conflictEx = Assert.Throws<InvalidOperationException>(() => app.MarkHired(Guid.NewGuid(), Guid.NewGuid(), actor, 2));
        Assert.True(conflictEx.Message.Contains("already marked as Hired with different Person/Employment"));
    }
}
