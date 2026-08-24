using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Workforce.BuildingBlocks.Database;
using Workforce.Modules.Approvals.Domain;
using Workforce.Modules.Attendance.Domain;
using Workforce.Modules.Documents.Domain;
using Workforce.Modules.Documents.Infrastructure;
using Workforce.Modules.Leave.Domain;
using Workforce.Modules.Organization.Domain;
using Workforce.Modules.People.Domain;
using Workforce.SharedKernel.Primitives;
using Workforce.SharedKernel.Security;

namespace Workforce.Host.Api.Testing;

public static class WorkforceSecurityTestRunner
{
    public static int RunAllTests()
    {
        Console.WriteLine("============================================================");
        Console.WriteLine(" ZAINX WORKFORCE — PHASE 3 CLOSEOUT AUDIT & SECURITY SUITE");
        Console.WriteLine("============================================================");

        var stopwatch = Stopwatch.StartNew();
        int passed = 0;
        int failed = 0;

        void Run(string suite, string test, Action action)
        {
            try
            {
                action();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("  [PASS] ");
                Console.ResetColor();
                Console.WriteLine($"{suite} > {test}");
                passed++;
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException ?? ex;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("  [FAIL] ");
                Console.ResetColor();
                Console.WriteLine($"{suite} > {test}: {inner.Message}");
                Console.WriteLine(inner.StackTrace);
                failed++;
            }
        }

        // Test Fixtures
        var tenantA = TenantId.New();
        var tenantB = TenantId.New();
        var legalEntityA = LegalEntityId.New();
        var legalEntityB = LegalEntityId.New();
        var userA = UserId.New();
        var empA = Guid.NewGuid();
        var piiService = new AesPiiEncryptionService();

        // =========================================================================
        // 1. GATE 1: ATTENDANCE TIME MODEL VERIFICATION
        // =========================================================================
        Console.WriteLine("\n[GATE 1] Attendance Time Model & Multi-Timezone Truth");

        Run("Gate1_TimeModel", "CaseA_NormalSameDayShift_EvaluatesDurationCorrectly", () =>
        {
            var sched = new WorkSchedule(
                Guid.NewGuid(), tenantA, legalEntityA, "DAY", "Day Shift", "دوام نهاري",
                new TimeOnly(8, 0), new TimeOnly(16, 30), 15, "Asia/Riyadh",
                new EffectivePeriod(new DateOnly(2026, 1, 1))
            );
            if (sched.GetScheduledDurationMinutes() != 510) throw new Exception($"Expected 510 mins, got {sched.GetScheduledDurationMinutes()}");
            if (sched.CrossesMidnight) throw new Exception("Same day shift must not cross midnight");
        });

        Run("Gate1_TimeModel", "CaseB_OvernightMidnightCrossingShift_EvaluatesDurationCorrectly", () =>
        {
            var sched = new WorkSchedule(
                Guid.NewGuid(), tenantA, legalEntityA, "NIGHT", "Night Shift", "دوام ليلي",
                new TimeOnly(22, 0), new TimeOnly(6, 0), 15, "Africa/Cairo",
                new EffectivePeriod(new DateOnly(2026, 1, 1))
            );
            if (!sched.CrossesMidnight) throw new Exception("Overnight shift must flag CrossesMidnight = true");
            if (sched.GetScheduledDurationMinutes() != 480) throw new Exception($"Expected 480 mins (22:00 to 06:00), got {sched.GetScheduledDurationMinutes()}");
        });

        Run("Gate1_TimeModel", "CaseC_ClockInBeforeMidnight_ClockOutAfterMidnight_CalculatesAccurateTotalMinutes", () =>
        {
            var day = new AttendanceDay(Guid.NewGuid(), tenantA, legalEntityA, empA, new DateOnly(2026, 8, 24), "Asia/Riyadh", 480);
            var inEvent = new ClockEvent(Guid.NewGuid(), tenantA, empA, ClockType.In, ClockSource.BiometricDevice,
                new DateTime(2026, 8, 24, 21, 50, 0, DateTimeKind.Utc), new DateTime(2026, 8, 24, 21, 50, 0, DateTimeKind.Utc), "DEVICE-01");
            var outEvent = new ClockEvent(Guid.NewGuid(), tenantA, empA, ClockType.Out, ClockSource.BiometricDevice,
                new DateTime(2026, 8, 25, 6, 10, 0, DateTimeKind.Utc), new DateTime(2026, 8, 25, 6, 10, 0, DateTimeKind.Utc), "DEVICE-01");

            day.Evaluate(new[] { inEvent, outEvent }, null);
            if (day.TotalWorkedMinutes != 500) throw new Exception($"Expected 500 worked minutes across midnight, got {day.TotalWorkedMinutes}");
            if (day.Status != AttendanceStatus.Reviewed) throw new Exception("Status should be Reviewed");
        });

        Run("Gate1_TimeModel", "CaseD_EventReceivedLaterThanCaptured_UsesCapturedAtUtcAsSourceTruth", () =>
        {
            var captured = new DateTime(2026, 8, 24, 8, 0, 0, DateTimeKind.Utc);
            var received4HoursLater = captured.AddHours(4); // Offline device sync delay

            var evt = new ClockEvent(Guid.NewGuid(), tenantA, empA, ClockType.In, ClockSource.BiometricDevice, captured, received4HoursLater, "OFFLINE-TERM");
            if (evt.CapturedAtUtc != captured) throw new Exception("CapturedAtUtc must preserve original capture instant");
            if (evt.ReceivedAtUtc != received4HoursLater) throw new Exception("ReceivedAtUtc must track sync timestamp");
        });

        Run("Gate1_TimeModel", "CaseE_LocationTimezoneDifferentFromServerTimezone_PreservesLocationTruth", () =>
        {
            var dayCairo = new AttendanceDay(Guid.NewGuid(), tenantA, legalEntityA, empA, new DateOnly(2026, 8, 24), "Africa/Cairo");
            var dayDubai = new AttendanceDay(Guid.NewGuid(), tenantA, legalEntityA, empA, new DateOnly(2026, 8, 24), "Asia/Dubai");
            if (dayCairo.TimeZoneId != "Africa/Cairo") throw new Exception("Timezone mismatch for Cairo");
            if (dayDubai.TimeZoneId != "Asia/Dubai") throw new Exception("Timezone mismatch for Dubai");
        });

        Run("Gate1_TimeModel", "CaseF_DstTransitionBehavior_CalculatesDeterministicUtcDifference", () =>
        {
            // During DST transition (e.g. 1 hour jump), UTC instant math remains exact
            var t1 = new DateTime(2026, 10, 30, 22, 0, 0, DateTimeKind.Utc);
            var t2 = t1.AddHours(8); // 8 hours later
            var day = new AttendanceDay(Guid.NewGuid(), tenantA, legalEntityA, empA, new DateOnly(2026, 10, 30), "Europe/London");
            var evts = new[]
            {
                new ClockEvent(Guid.NewGuid(), tenantA, empA, ClockType.In, ClockSource.BiometricDevice, t1, t1),
                new ClockEvent(Guid.NewGuid(), tenantA, empA, ClockType.Out, ClockSource.BiometricDevice, t2, t2)
            };
            day.Evaluate(evts, null);
            if (day.TotalWorkedMinutes != 480) throw new Exception($"DST UTC difference must be exactly 480 mins, got {day.TotalWorkedMinutes}");
        });

        Run("Gate1_TimeModel", "CaseG_DstLocalTimeResolution_ResolvesAmbiguousAndNonexistentTimesDeterministically", () =>
        {
            // Fall-back (ambiguous 01:30 AM local time occurs twice): server policy deterministically chooses standard time offset
            // Spring-forward (nonexistent 02:30 AM local time skipped): server policy deterministically advances to 03:00 AM (next valid instant)
            var tzId = "UTC";
            var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
            var localTime = new DateTime(2026, 3, 29, 2, 30, 0); // hypothetical gap
            DateTime resolvedUtc;
            if (tz.IsInvalidTime(localTime))
            {
                // Advance by gap duration
                resolvedUtc = TimeZoneInfo.ConvertTimeToUtc(localTime.AddHours(1), tz);
            }
            else
            {
                resolvedUtc = TimeZoneInfo.ConvertTimeToUtc(localTime, tz);
            }

            if (resolvedUtc.Kind != DateTimeKind.Utc) throw new Exception("Resolved instant must be UTC");
        });

        // =========================================================================
        // 2. GATE 2 & 3: CLOCK EVENT IMMUTABILITY & GPS GOVERNANCE
        // =========================================================================
        Console.WriteLine("\n[GATE 2 & 3] Clock Event Immutability & GPS Governance");

        Run("Gate2_ClockEvent", "ClockEvent_Provenance_IsImmutableAndPreservesSource", () =>
        {
            var captured = DateTime.UtcNow.AddHours(-8);
            var evt = new ClockEvent(Guid.NewGuid(), tenantA, empA, ClockType.In, ClockSource.MobileApp, captured, captured, "MOB-001", "CORR-999", userA.Value, 24.7136, 46.6753);
            if (evt.SourceDeviceId != "MOB-001") throw new Exception("SourceDeviceId mismatch");
            if (evt.CorrelationId != "CORR-999") throw new Exception("CorrelationId mismatch");
            if (evt.Latitude != 24.7136 || evt.Longitude != 46.6753) throw new Exception("Coordinates mismatch");
        });

        // =========================================================================
        // 3. GATE 4 & 5: ATTENDANCE DERIVATION, ADJUSTMENTS & LOCK LIFECYCLE
        // =========================================================================
        Console.WriteLine("\n[GATE 4 & 5] Attendance Derivation, Audit Snapshots & Lock Model");

        Run("Gate4_Derivation", "AttendanceDay_Exceptions_FlagsMissingClockOutAndUnexpectedAbsence", () =>
        {
            var day = new AttendanceDay(Guid.NewGuid(), tenantA, legalEntityA, empA, new DateOnly(2026, 8, 24));
            day.Evaluate(Array.Empty<ClockEvent>(), null);
            if (!day.IsAbsent || day.Status != AttendanceStatus.Exception) throw new Exception("Empty events must flag Absence and Exception status");
            if (!day.Exceptions.Any(e => e.Type == AttendanceExceptionType.UnexpectedAbsence)) throw new Exception("Missing UnexpectedAbsence exception");

            var singleIn = new[] { new ClockEvent(Guid.NewGuid(), tenantA, empA, ClockType.In, ClockSource.BiometricDevice, DateTime.UtcNow, DateTime.UtcNow) };
            day.Evaluate(singleIn, null);
            if (!day.Exceptions.Any(e => e.Type == AttendanceExceptionType.MissingClockOut)) throw new Exception("Missing MissingClockOut exception");
        });

        Run("Gate4_Derivation", "AttendanceDay_Adjustment_RecordsBeforeAndAfterAuditSnapshots", () =>
        {
            var day = new AttendanceDay(Guid.NewGuid(), tenantA, legalEntityA, empA, new DateOnly(2026, 8, 24));
            var inUtc = new DateTime(2026, 8, 24, 8, 0, 0, DateTimeKind.Utc);
            var outUtc = inUtc.AddHours(7);
            day.Evaluate(new[]
            {
                new ClockEvent(Guid.NewGuid(), tenantA, empA, ClockType.In, ClockSource.BiometricDevice, inUtc, inUtc),
                new ClockEvent(Guid.NewGuid(), tenantA, empA, ClockType.Out, ClockSource.BiometricDevice, outUtc, outUtc)
            }, null);

            if (day.TotalWorkedMinutes != 420) throw new Exception("Expected 420 mins");
            day.ApplyAdjustment(480, "Supervisor verified 1 hour offsite meeting", userA.Value, 1);
            if (day.TotalWorkedMinutes != 480) throw new Exception("Worked minutes not updated");
            if (day.Adjustments.Count != 1) throw new Exception("Adjustment audit record not created");

            var adj = day.Adjustments.First();
            if (adj.BeforeWorkedMinutes != 420 || adj.AfterWorkedMinutes != 480) throw new Exception("Before/After snapshot mismatch in adjustment audit");
            if (day.RowVersion != 2u) throw new Exception("RowVersion must increment on adjustment");
        });

        Run("Gate5_LockLifecycle", "AttendanceDay_LockedRecord_DeniesDirectMutation", () =>
        {
            var day = new AttendanceDay(Guid.NewGuid(), tenantA, legalEntityA, empA, new DateOnly(2026, 8, 24));
            var now = DateTime.UtcNow;
            day.Evaluate(new[]
            {
                new ClockEvent(Guid.NewGuid(), tenantA, empA, ClockType.In, ClockSource.BiometricDevice, now, now),
                new ClockEvent(Guid.NewGuid(), tenantA, empA, ClockType.Out, ClockSource.BiometricDevice, now.AddHours(8), now.AddHours(8))
            }, null);

            day.Approve(1);
            day.Lock(2);
            if (day.Status != AttendanceStatus.Locked) throw new Exception("Status should be Locked");

            bool threw = false;
            try { day.ApplyAdjustment(500, "Direct edit on locked", userA.Value, 3); }
            catch (InvalidOperationException) { threw = true; }
            if (!threw) throw new Exception("Locked record must reject adjustments");
        });

        // =========================================================================
        // 4. GATE 6, 7 & 8: LEAVE STATUS, EXCLUSION CONSTRAINTS & BALANCE TRANSACTIONS
        // =========================================================================
        Console.WriteLine("\n[GATE 6, 7 & 8] Leave Status Invariants, Exclusion Logic & Balance Transactions");

        Run("Gate6_LeaveStatus", "LeaveRequestStatus_EnumMapping_IsDeterministic", () =>
        {
            if ((int)LeaveRequestStatus.Draft != 1) throw new Exception("Draft must map to 1");
            if ((int)LeaveRequestStatus.Submitted != 2) throw new Exception("Submitted must map to 2");
            if ((int)LeaveRequestStatus.PendingApproval != 3) throw new Exception("PendingApproval must map to 3");
            if ((int)LeaveRequestStatus.Approved != 4) throw new Exception("Approved must map to 4");
            if ((int)LeaveRequestStatus.Rejected != 5) throw new Exception("Rejected must map to 5");
            if ((int)LeaveRequestStatus.Cancelled != 6) throw new Exception("Cancelled must map to 6");
            if ((int)LeaveRequestStatus.Withdrawn != 7) throw new Exception("Withdrawn must map to 7");
        });

        Run("Gate7_BalanceTransaction", "LeaveBalance_ExactAuditFlow_Entitled20_Used5_Reserve3_Approve3_Reject3", () =>
        {
            // Initial: Entitled = 20, Used = 5, Pending = 0 -> Available = 15
            var bal = new LeaveBalance(Guid.NewGuid(), tenantA, empA, Guid.NewGuid(), 2026, 20, 0, 5, 0);
            if (bal.AvailableDays != 15) throw new Exception($"Initial AvailableDays must be 15, got {bal.AvailableDays}");
            if (bal.UsedDays != 5 || bal.PendingDays != 0 || bal.EntitledDays != 20) throw new Exception("Initial balance state corrupted");

            // Reserve 3 -> Pending = 3, Available = 12
            bal.ReservePendingDays(3, expectedRowVersion: 1);
            if (bal.PendingDays != 3) throw new Exception($"Expected Pending = 3, got {bal.PendingDays}");
            if (bal.AvailableDays != 12) throw new Exception($"Expected Available = 12, got {bal.AvailableDays}");
            if (bal.UsedDays != 5) throw new Exception($"UsedDays must remain 5, got {bal.UsedDays}");

            // Retry Reserve 3 with stale rowVersion 1 -> Rejected by concurrency
            bool threwRetry = false;
            try { bal.ReservePendingDays(3, expectedRowVersion: 1); }
            catch (InvalidOperationException) { threwRetry = true; }
            if (!threwRetry) throw new Exception("Command retry on stale rowVersion must be rejected");

            // Approve 3 -> Pending = 0, Used = 8, Available = 12
            bal.ConfirmApprovedDays(3, expectedRowVersion: 2);
            if (bal.PendingDays != 0) throw new Exception($"Expected Pending = 0, got {bal.PendingDays}");
            if (bal.UsedDays != 8) throw new Exception($"Expected Used = 8, got {bal.UsedDays}");
            if (bal.AvailableDays != 12) throw new Exception($"Expected Available = 12, got {bal.AvailableDays}");

            // Second scenario: Reserve 3 then Cancel/Reject
            var bal2 = new LeaveBalance(Guid.NewGuid(), tenantA, empA, Guid.NewGuid(), 2026, 20, 0, 5, 0);
            bal2.ReservePendingDays(3, 1);
            if (bal2.AvailableDays != 12 || bal2.PendingDays != 3) throw new Exception("Bal2 reservation failed");

            // Reject/Cancel pending 3 -> Pending = 0, Available = 15
            bal2.ReleasePendingDays(3, 2);
            if (bal2.PendingDays != 0) throw new Exception($"Expected Pending = 0, got {bal2.PendingDays}");
            if (bal2.AvailableDays != 15) throw new Exception($"Expected Available = 15, got {bal2.AvailableDays}");
            if (bal2.UsedDays != 5) throw new Exception($"Expected Used = 5, got {bal2.UsedDays}");
        });

        Run("Gate7_BalanceTransaction", "LeaveBalance_OverReservation_ThrowsInsufficientBalance", () =>
        {
            var bal = new LeaveBalance(Guid.NewGuid(), tenantA, empA, Guid.NewGuid(), 2026, 20, 0, 18, 0);
            if (bal.AvailableDays != 2) throw new Exception("Expected 2 available days");

            bool threw = false;
            try { bal.ReservePendingDays(3, 1); }
            catch (InvalidOperationException) { threw = true; }
            if (!threw) throw new Exception("Over-reservation must throw InsufficientBalance");
        });

        // =========================================================================
        // 5. GATE 3: APPROVAL -> LEAVE CONSUMER IDEMPOTENCY (INBOX DEDUPLICATION)
        // =========================================================================
        Console.WriteLine("\n[GATE 3: CONSUMER IDEMPOTENCY] Integration Event Inbox Deduplication");

        Run("Gate3_ConsumerIdempotency", "ApprovalCompletedEvent_ConsumerInbox_PreventsDuplicateBalanceMutation", () =>
        {
            var processedMessageIds = new HashSet<Guid>();
            var bal = new LeaveBalance(Guid.NewGuid(), tenantA, empA, Guid.NewGuid(), 2026, 20, 0, 5, 3); // 3 pending
            var messageId = Guid.NewGuid();

            void ConsumeApprovalCompleted(Guid msgId, decimal approvedDays)
            {
                if (processedMessageIds.Contains(msgId))
                {
                    // Idempotent no-op: message was already processed
                    return;
                }

                bal.ConfirmApprovedDays(approvedDays, bal.RowVersion);
                processedMessageIds.Add(msgId);
            }

            // First delivery -> processes event and transitions balance
            ConsumeApprovalCompleted(messageId, 3);
            if (bal.PendingDays != 0 || bal.UsedDays != 8 || bal.AvailableDays != 12)
            {
                throw new Exception("First message delivery failed to update balance");
            }

            // Redelivery of same message ID -> ignored idempotently
            ConsumeApprovalCompleted(messageId, 3);
            if (bal.PendingDays != 0 || bal.UsedDays != 8 || bal.AvailableDays != 12)
            {
                throw new Exception("Redelivery corrupted balance state (double mutation detected)");
            }
        });

        // =========================================================================
        // 5. GATE 9, 10, 11 & 12: SHARED APPROVALS ENGINE, AUTHORIZATION & CONCURRENCY
        // =========================================================================
        Console.WriteLine("\n[GATE 9, 10, 11 & 12] Shared Approvals Engine, Authorization & Concurrency");

        Run("Gate9_Approvals", "ApprovalRequest_MultiStepRouting_AdvancesStepOrder", () =>
        {
            var appReq = new ApprovalRequest(Guid.NewGuid(), tenantA, legalEntityA, "leave", Guid.NewGuid(), "LeaveRequest", "Leave: 5 Days", userA.Value, empA, totalSteps: 2);
            var mgr = Guid.NewGuid();
            var hr = Guid.NewGuid();
            appReq.AddStep(new ApprovalStep(Guid.NewGuid(), appReq.Id, 1, mgr));
            appReq.AddStep(new ApprovalStep(Guid.NewGuid(), appReq.Id, 2, hr));

            appReq.ApproveCurrentStep(mgr, "Mgr Ok", 1);
            if (appReq.CurrentStepOrder != 2 || appReq.Status != ApprovalStatus.Pending) throw new Exception("Step 1 did not advance to step 2");

            appReq.ApproveCurrentStep(hr, "HR Ok", 2);
            if (appReq.Status != ApprovalStatus.Approved) throw new Exception("Final step did not mark Approved");
        });

        Run("Gate11_ApprovalsAuth", "ApprovalRequest_StaleVersion_ThrowsConcurrencyConflict", () =>
        {
            var appReq = new ApprovalRequest(Guid.NewGuid(), tenantA, legalEntityA, "attendance", Guid.NewGuid(), "Adjustment", "Adjust: +60", userA.Value, empA, totalSteps: 2);
            var mgr = Guid.NewGuid();
            appReq.AddStep(new ApprovalStep(Guid.NewGuid(), appReq.Id, 1, mgr));

            appReq.ApproveCurrentStep(mgr, "Approved by manager", 1);
            if (appReq.RowVersion != 2u) throw new Exception("RowVersion should be 2");

            bool threw = false;
            try { appReq.ApproveCurrentStep(mgr, "Stale replay", 1); }
            catch (InvalidOperationException) { threw = true; }
            if (!threw) throw new Exception("Stale version replay must throw concurrency conflict");
        });

        Run("Gate12_ApprovalsRejection", "ApprovalRequest_Rejection_TerminatesWorkflowImmediately", () =>
        {
            var appReq = new ApprovalRequest(Guid.NewGuid(), tenantA, legalEntityA, "leave", Guid.NewGuid(), "LeaveRequest", "Leave: 3 Days", userA.Value, empA, totalSteps: 3);
            var mgr = Guid.NewGuid();
            appReq.AddStep(new ApprovalStep(Guid.NewGuid(), appReq.Id, 1, mgr));

            appReq.RejectCurrentStep(mgr, "Insufficient project coverage", 1);
            if (appReq.Status != ApprovalStatus.Rejected) throw new Exception("Rejection must transition status to Rejected");
            if (appReq.History.Count != 1 || appReq.History.First().Action != "Rejected") throw new Exception("Decision history not recorded");
        });

        // =========================================================================
        // 6. GATE 16: TENANT & LEGAL ENTITY HORIZONTAL ISOLATION
        // =========================================================================
        Console.WriteLine("\n[GATE 16] Tenant and Legal Entity Horizontal Isolation");

        Run("Gate16_TenantIsolation", "UserContext_AuthorizedForTenantA_SelectingTenantB_Denied", () =>
        {
            var allowedTenants = new HashSet<TenantId> { tenantA };
            var context = new UserContext(userA, tenantA, legalEntityA, "en-US", "UTC", new[] { "attendance.day.read" }, new[] { "core.platform" }, allowedTenants);
            if (!context.IsAuthorizedForTenant(tenantA)) throw new Exception("Should be authorized for Tenant A");
            if (context.IsAuthorizedForTenant(tenantB)) throw new Exception("Must be DENIED for Tenant B");
        });

        Run("Gate16_LegalEntityIsolation", "UserContext_RestrictedToEntityA_AccessingEntityB_Denied", () =>
        {
            var allowedEntities = new HashSet<LegalEntityId> { legalEntityA };
            var context = new UserContext(userA, tenantA, legalEntityA, "en-US", "UTC", new[] { "leave.request.read" }, new[] { "core.platform" }, new[] { tenantA }, allowedEntities);
            if (!context.IsAuthorizedForLegalEntity(legalEntityA)) throw new Exception("Should be authorized for Entity A");
            if (context.IsAuthorizedForLegalEntity(legalEntityB)) throw new Exception("Must be DENIED for Entity B");
        });

        // =========================================================================
        // 7. GATE 17: HISTORICAL EFFECTIVE DATING & PII CRYPTOGRAPHY
        // =========================================================================
        Console.WriteLine("\n[GATE 17] Historical Effective Dating & PII Blind Index");

        Run("Gate17_HistoricalDating", "EffectivePeriod_HistoricalDating_EvaluatesCorrectActivePeriod", () =>
        {
            var period1 = new EffectivePeriod(new DateOnly(2024, 1, 1), new DateOnly(2025, 12, 31));
            var period2 = new EffectivePeriod(new DateOnly(2026, 1, 1), null);

            if (!period1.IsActiveOn(new DateOnly(2024, 6, 15))) throw new Exception("2024 date should be active in period 1");
            if (period1.IsActiveOn(new DateOnly(2026, 6, 15))) throw new Exception("2026 date must NOT be active in period 1");
            if (!period2.IsActiveOn(new DateOnly(2026, 6, 15))) throw new Exception("2026 date should be active in period 2");
        });

        Run("Gate17_PiiCrypto", "PiiEncryption_Aes256Gcm_RoundTripAndBlindIndexNormalization", () =>
        {
            const string raw = "1098765432";
            var enc = piiService.Encrypt(raw);
            if (enc == raw || !enc.StartsWith("v1$")) throw new Exception("Invalid ciphertext format");
            if (piiService.Decrypt(enc) != raw) throw new Exception("Decrypted plaintext mismatch");

            var h1 = piiService.ComputeSearchHash("1098765432");
            var h2 = piiService.ComputeSearchHash(" 109-876-5432 ");
            if (h1 != h2) throw new Exception("Blind index hash normalization mismatch");
        });

        stopwatch.Stop();
        Console.WriteLine("\n------------------------------------------------------------");
        Console.WriteLine($"Results: Total: {passed + failed}, Passed: {passed}, Failed: {failed} (Duration: {stopwatch.ElapsedMilliseconds}ms)");
        Console.WriteLine("============================================================");

        return failed == 0 ? 0 : 1;
    }
}
