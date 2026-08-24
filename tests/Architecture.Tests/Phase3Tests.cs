using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Workforce.Modules.Approvals.Domain;
using Workforce.Modules.Attendance.Domain;
using Workforce.Modules.Leave.Domain;
using Workforce.SharedKernel.Primitives;
using Xunit;

namespace Architecture.Tests;

public class Phase3Tests
{
    private static readonly TenantId TestTenantId = new(Guid.NewGuid());
    private static readonly LegalEntityId TestLegalEntityId = new(Guid.NewGuid());
    private static readonly Guid TestEmpId = Guid.NewGuid();

    // =========================================================================
    // 1. ATTENDANCE DOMAIN & CONCURRENCY TESTS
    // =========================================================================

    [Fact]
    public void Attendance_ClockEvent_Provenance_IsImmutableAndPreservesSource()
    {
        var capturedAt = DateTime.UtcNow.AddHours(-8);
        var receivedAt = DateTime.UtcNow.AddHours(-7).AddMinutes(58);
        var correlationId = Guid.NewGuid().ToString();

        var clockEvent = new ClockEvent(
            Guid.NewGuid(),
            TestTenantId,
            TestEmpId,
            ClockType.In,
            ClockSource.BiometricDevice,
            capturedAt,
            receivedAt,
            sourceDeviceId: "BIO-TERMINAL-01",
            correlationId: correlationId,
            actorUserId: Guid.NewGuid(),
            latitude: 30.0444,
            longitude: 31.2357
        );

        Assert.Equal(ClockSource.BiometricDevice, clockEvent.Source);
        Assert.Equal("BIO-TERMINAL-01", clockEvent.SourceDeviceId);
        Assert.Equal(correlationId, clockEvent.CorrelationId);
    }

    [Fact]
    public void Attendance_AttendanceDay_Evaluation_CalculatesWorkedMinutesWithoutPayrollMath()
    {
        var day = new AttendanceDay(
            Guid.NewGuid(),
            TestTenantId,
            TestLegalEntityId,
            TestEmpId,
            new DateOnly(2026, 8, 24),
            "Africa/Cairo",
            scheduledMinutes: 480
        );

        var startUtc = new DateTime(2026, 8, 24, 7, 0, 0, DateTimeKind.Utc); // 9 AM Cairo
        var endUtc = new DateTime(2026, 8, 24, 15, 30, 0, DateTimeKind.Utc); // 5:30 PM Cairo

        var events = new List<ClockEvent>
        {
            new(Guid.NewGuid(), TestTenantId, TestEmpId, ClockType.In, ClockSource.BiometricDevice, startUtc, startUtc),
            new(Guid.NewGuid(), TestTenantId, TestEmpId, ClockType.Out, ClockSource.BiometricDevice, endUtc, endUtc)
        };

        day.Evaluate(events, null);

        Assert.Equal(510, day.TotalWorkedMinutes);
        Assert.False(day.IsAbsent);
        Assert.Equal(AttendanceStatus.Reviewed, day.Status);
    }

    [Fact]
    public void Attendance_AttendanceDay_Exceptions_FlagsMissingClockOutAndUnexpectedAbsence()
    {
        var day = new AttendanceDay(
            Guid.NewGuid(),
            TestTenantId,
            TestLegalEntityId,
            TestEmpId,
            new DateOnly(2026, 8, 24),
            "Africa/Cairo"
        );

        // Scenario 1: Absence (no events)
        day.Evaluate(new List<ClockEvent>(), null);
        Assert.True(day.IsAbsent);
        Assert.Equal(AttendanceStatus.Exception, day.Status);
        Assert.True(day.Exceptions.Any(e => e.Type == AttendanceExceptionType.UnexpectedAbsence));

        // Scenario 2: Clock-in without clock-out
        var startUtc = new DateTime(2026, 8, 24, 7, 0, 0, DateTimeKind.Utc);
        var inOnly = new List<ClockEvent>
        {
            new(Guid.NewGuid(), TestTenantId, TestEmpId, ClockType.In, ClockSource.MobileApp, startUtc, startUtc)
        };

        day.Evaluate(inOnly, null);
        Assert.Equal(AttendanceStatus.Exception, day.Status);
        Assert.True(day.Exceptions.Any(e => e.Type == AttendanceExceptionType.MissingClockOut));
    }

    [Fact]
    public void Attendance_AttendanceDay_Adjustment_RecordsBeforeAndAfterAuditSnapshots()
    {
        var day = new AttendanceDay(
            Guid.NewGuid(),
            TestTenantId,
            TestLegalEntityId,
            TestEmpId,
            new DateOnly(2026, 8, 24)
        );

        var actorUserId = Guid.NewGuid();
        day.ApplyAdjustment(480, "Manager approved timesheet correction", actorUserId, expectedRowVersion: 1);

        Assert.Equal(480, day.TotalWorkedMinutes);
        Assert.Equal(1, day.Adjustments.Count);

        var adj = day.Adjustments.First();
        Assert.Equal(0, adj.BeforeWorkedMinutes);
        Assert.Equal(480, adj.AfterWorkedMinutes);
        Assert.Equal(actorUserId, adj.ActorUserId);
        Assert.Equal(2u, day.RowVersion);
    }

    [Fact]
    public void Attendance_AttendanceDay_OptimisticConcurrency_ThrowsOnVersionConflict()
    {
        var day = new AttendanceDay(
            Guid.NewGuid(),
            TestTenantId,
            TestLegalEntityId,
            TestEmpId,
            new DateOnly(2026, 8, 24)
        );

        Assert.Throws<InvalidOperationException>(() =>
            day.ApplyAdjustment(480, "Stale update", Guid.NewGuid(), expectedRowVersion: 999)
        );
    }

    [Fact]
    public void Attendance_WorkSchedule_MidnightCrossing_CalculatesDurationCorrectly()
    {
        var schedule = new WorkSchedule(
            Guid.NewGuid(),
            TestTenantId,
            TestLegalEntityId,
            "NIGHT-SHIFT",
            "Night Shift 22:00 to 06:00",
            "شفت ليلي",
            new TimeOnly(22, 0),
            new TimeOnly(6, 0),
            gracePeriodMinutes: 15,
            timeZoneId: "Asia/Riyadh",
            effectivePeriod: new EffectivePeriod(new DateOnly(2026, 1, 1))
        );

        Assert.True(schedule.CrossesMidnight);
        Assert.Equal(480, schedule.GetScheduledDurationMinutes());
    }

    [Fact]
    public void Attendance_TimeModel_ClockInBeforeMidnight_ClockOutAfterMidnight_CalculatesAccurateTotalMinutes()
    {
        var day = new AttendanceDay(Guid.NewGuid(), TestTenantId, TestLegalEntityId, TestEmpId, new DateOnly(2026, 8, 24), "Asia/Riyadh", 480);
        var inEvent = new ClockEvent(Guid.NewGuid(), TestTenantId, TestEmpId, ClockType.In, ClockSource.BiometricDevice,
            new DateTime(2026, 8, 24, 21, 50, 0, DateTimeKind.Utc), new DateTime(2026, 8, 24, 21, 50, 0, DateTimeKind.Utc), "DEVICE-01");
        var outEvent = new ClockEvent(Guid.NewGuid(), TestTenantId, TestEmpId, ClockType.Out, ClockSource.BiometricDevice,
            new DateTime(2026, 8, 25, 6, 10, 0, DateTimeKind.Utc), new DateTime(2026, 8, 25, 6, 10, 0, DateTimeKind.Utc), "DEVICE-01");

        day.Evaluate(new[] { inEvent, outEvent }, null);
        Assert.Equal(500, day.TotalWorkedMinutes);
        Assert.Equal(AttendanceStatus.Reviewed, day.Status);
    }

    [Fact]
    public void Attendance_TimeModel_EventReceivedLaterThanCaptured_UsesCapturedAtUtcAsSourceTruth()
    {
        var captured = new DateTime(2026, 8, 24, 8, 0, 0, DateTimeKind.Utc);
        var received4HoursLater = captured.AddHours(4);

        var evt = new ClockEvent(Guid.NewGuid(), TestTenantId, TestEmpId, ClockType.In, ClockSource.BiometricDevice, captured, received4HoursLater, "OFFLINE-TERM");
        Assert.Equal(captured, evt.CapturedAtUtc);
        Assert.Equal(received4HoursLater, evt.ReceivedAtUtc);
    }

    [Fact]
    public void Attendance_TimeModel_DstTransition_CalculatesDeterministicUtcDifference()
    {
        var t1 = new DateTime(2026, 10, 30, 22, 0, 0, DateTimeKind.Utc);
        var t2 = t1.AddHours(8);
        var day = new AttendanceDay(Guid.NewGuid(), TestTenantId, TestLegalEntityId, TestEmpId, new DateOnly(2026, 10, 30), "Europe/London");
        var evts = new[]
        {
            new ClockEvent(Guid.NewGuid(), TestTenantId, TestEmpId, ClockType.In, ClockSource.BiometricDevice, t1, t1),
            new ClockEvent(Guid.NewGuid(), TestTenantId, TestEmpId, ClockType.Out, ClockSource.BiometricDevice, t2, t2)
        };
        day.Evaluate(evts, null);
        Assert.Equal(480, day.TotalWorkedMinutes);
    }

    // =========================================================================
    // 2. LEAVE DOMAIN & BALANCE CONSTRAINTS
    // =========================================================================

    [Fact]
    public void Leave_LeaveBalance_Reservation_EnforcesSufficientBalance()
    {
        var leaveTypeId = Guid.NewGuid();
        var balance = new LeaveBalance(
            Guid.NewGuid(),
            TestTenantId,
            TestEmpId,
            leaveTypeId,
            2026,
            entitledDays: 21,
            accruedDays: 0,
            usedDays: 5,
            pendingDays: 0
        );

        Assert.Equal(16, balance.AvailableDays);

        // Reserve 5 days
        balance.ReservePendingDays(5, expectedRowVersion: 1);
        Assert.Equal(5, balance.PendingDays);
        Assert.Equal(11, balance.AvailableDays);

        // Over-reservation
        Assert.Throws<InvalidOperationException>(() =>
            balance.ReservePendingDays(12, expectedRowVersion: 2)
        );
    }

    [Fact]
    public void Leave_LeaveBalance_ConfirmApprovedDays_DeductsFromUsedAndReleasesPending()
    {
        var leaveTypeId = Guid.NewGuid();
        var balance = new LeaveBalance(
            Guid.NewGuid(),
            TestTenantId,
            TestEmpId,
            leaveTypeId,
            2026,
            entitledDays: 21,
            pendingDays: 4,
            usedDays: 2
        );

        balance.ConfirmApprovedDays(4, expectedRowVersion: 1);

        Assert.Equal(0, balance.PendingDays);
        Assert.Equal(6, balance.UsedDays);
        Assert.Equal(15, balance.AvailableDays);
    }

    [Fact]
    public void Leave_LeaveRequest_StateTransitions_DraftToPendingToApproved()
    {
        var req = new LeaveRequest(
            Guid.NewGuid(),
            TestTenantId,
            TestLegalEntityId,
            TestEmpId,
            Guid.NewGuid(),
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 5),
            durationDays: 5.0m,
            reason: "Annual vacation"
        );

        Assert.Equal(LeaveRequestStatus.Draft, req.Status);
        Assert.Equal(2400, req.DurationMinutes);

        var approvalReqId = Guid.NewGuid();
        req.Submit(approvalReqId, expectedRowVersion: 1);
        Assert.Equal(LeaveRequestStatus.PendingApproval, req.Status);
        Assert.Equal(approvalReqId, req.ApprovalRequestId);

        req.Approve(expectedRowVersion: 2);
        Assert.Equal(LeaveRequestStatus.Approved, req.Status);
    }

    [Fact]
    public void Leave_LeaveRequest_Rejection_StoresRejectionReason()
    {
        var req = new LeaveRequest(
            Guid.NewGuid(),
            TestTenantId,
            TestLegalEntityId,
            TestEmpId,
            Guid.NewGuid(),
            new DateOnly(2026, 9, 10),
            new DateOnly(2026, 9, 12),
            durationDays: 3.0m,
            reason: "Conference attendance"
        );

        req.Submit(Guid.NewGuid(), expectedRowVersion: 1);
        req.Reject("Department staffing minimum not met", expectedRowVersion: 2);

        Assert.Equal(LeaveRequestStatus.Rejected, req.Status);
        Assert.Equal("Department staffing minimum not met", req.RejectionReason);
    }

    // =========================================================================
    // 3. APPROVAL ENGINE DOMAIN & ROUTING TESTS
    // =========================================================================

    [Fact]
    public void Approvals_ApprovalRequest_MultiStepRouting_AdvancesStepOrder()
    {
        var approvalReq = new ApprovalRequest(
            Guid.NewGuid(),
            TestTenantId,
            TestLegalEntityId,
            "leave",
            Guid.NewGuid(),
            "LeaveRequestApproval",
            "Leave Request: 5 Days",
            requesterUserId: Guid.NewGuid(),
            requesterEmploymentId: TestEmpId,
            totalSteps: 2
        );

        var managerUserId = Guid.NewGuid();
        var hrUserId = Guid.NewGuid();

        approvalReq.AddStep(new ApprovalStep(Guid.NewGuid(), approvalReq.Id, 1, managerUserId));
        approvalReq.AddStep(new ApprovalStep(Guid.NewGuid(), approvalReq.Id, 2, hrUserId));

        Assert.Equal(1, approvalReq.CurrentStepOrder);
        Assert.Equal(ApprovalStatus.Pending, approvalReq.Status);

        // Step 1: Manager approves
        approvalReq.ApproveCurrentStep(managerUserId, "Approved by Manager", expectedRowVersion: 1);
        Assert.Equal(2, approvalReq.CurrentStepOrder);
        Assert.Equal(ApprovalStatus.Pending, approvalReq.Status);

        // Step 2: HR approves (Final step)
        approvalReq.ApproveCurrentStep(hrUserId, "Approved by HR", expectedRowVersion: 2);
        Assert.Equal(ApprovalStatus.Approved, approvalReq.Status);
        Assert.Equal(2, approvalReq.History.Count);
    }

    [Fact]
    public void Approvals_ApprovalRequest_Rejection_TerminatesWorkflow()
    {
        var approvalReq = new ApprovalRequest(
            Guid.NewGuid(),
            TestTenantId,
            TestLegalEntityId,
            "attendance",
            Guid.NewGuid(),
            "AttendanceAdjustmentApproval",
            "Adjustment: +60 Mins",
            requesterUserId: Guid.NewGuid(),
            requesterEmploymentId: TestEmpId,
            totalSteps: 2
        );

        var managerUserId = Guid.NewGuid();
        approvalReq.AddStep(new ApprovalStep(Guid.NewGuid(), approvalReq.Id, 1, managerUserId));

        approvalReq.RejectCurrentStep(managerUserId, "Insufficient explanation", expectedRowVersion: 1);

        Assert.Equal(ApprovalStatus.Rejected, approvalReq.Status);
        Assert.Equal("Insufficient explanation", approvalReq.History.Last().Reason);
    }

    [Fact]
    public void Approvals_ApprovalRequest_OptimisticConcurrency_ThrowsOnConflict()
    {
        var approvalReq = new ApprovalRequest(
            Guid.NewGuid(),
            TestTenantId,
            TestLegalEntityId,
            "leave",
            Guid.NewGuid(),
            "LeaveRequestApproval",
            "Leave Request",
            requesterUserId: Guid.NewGuid(),
            requesterEmploymentId: TestEmpId
        );

        Assert.Throws<InvalidOperationException>(() =>
            approvalReq.ApproveCurrentStep(Guid.NewGuid(), "Approved", expectedRowVersion: 999)
        );
    }

    // =========================================================================
    // 4. BOUNDARY & NON-CONTAMINATION TESTS
    // =========================================================================

    [Fact]
    public void Phase3Modules_DoNotReferenceDownstreamPayrollOrCompliance()
    {
        var baseDir = AppContext.BaseDirectory;
        var solutionRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));

        var forbiddenDownstreamTerms = new[]
        {
            "Workforce.Modules.Payroll",
            "Workforce.Modules.Compliance",
            "Workforce.Modules.Settlement",
            "Workforce.Modules.Recruitment",
            "Workforce.Modules.Reporting",
            "Workforce.Modules.Ai"
        };

        var phase3Projects = new[]
        {
            Path.Combine(solutionRoot, "src", "Modules", "Attendance", "Workforce.Modules.Attendance.csproj"),
            Path.Combine(solutionRoot, "src", "Modules", "Leave", "Workforce.Modules.Leave.csproj"),
            Path.Combine(solutionRoot, "src", "Modules", "Approvals", "Workforce.Modules.Approvals.csproj")
        };

        foreach (var projPath in phase3Projects)
        {
            Assert.True(File.Exists(projPath), $"Project file not found: {projPath}");

            var content = File.ReadAllText(projPath);
            foreach (var forbidden in forbiddenDownstreamTerms)
            {
                Assert.DoesNotContain(forbidden, content, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
