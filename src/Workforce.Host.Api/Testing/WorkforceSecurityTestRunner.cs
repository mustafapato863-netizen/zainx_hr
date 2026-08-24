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
        Console.WriteLine(" ZAINX WORKFORCE — PHASE 3 INTEGRATION & SECURITY SUITE");
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

        // 1. Boundary & Domain Tests
        Console.WriteLine("\n[SUITE] Phase2DomainTests");
        Run("Phase2DomainTests", "OrganizationUnit_EffectivePeriod_ShouldDetectActiveStatus", () =>
        {
            var unit = new OrganizationUnit(
                Guid.NewGuid(),
                tenantA,
                legalEntityA,
                "ENG",
                "Engineering",
                "الهندسة",
                OrganizationUnitType.Department,
                null,
                new EffectivePeriod(new DateOnly(2024, 1, 1), null),
                null
            );
            if (!unit.IsActive) throw new Exception("Unit should be active");
            if (unit.NameAr != "الهندسة") throw new Exception("Arabic name mismatch");
        });

        Run("Phase2DomainTests", "Employment_StateMachine_ShouldTransitionStatus", () =>
        {
            var emp = new Employment(
                Guid.NewGuid(),
                tenantA,
                Guid.NewGuid(),
                legalEntityA,
                "EMP-1001",
                new DateOnly(2024, 1, 1),
                null,
                EmploymentStatus.Draft
            );
            emp.Activate(1);
            if (emp.Status != EmploymentStatus.Active) throw new Exception("Status should be Active");
            emp.Deactivate(2);
            if (emp.Status != EmploymentStatus.Inactive) throw new Exception("Status should be Inactive");
        });

        // 2. Tenant Context Authority & Security Tests
        Console.WriteLine("\n[SUITE] TenantContextAuthorityTests");
        Run("TenantContextAuthority", "CaseA_UserAuthorizedForTenantA_SelectingTenantA_ShouldSucceed", () =>
        {
            var allowedTenants = new HashSet<TenantId> { tenantA };
            var context = new UserContext(userA, tenantA, legalEntityA, "en-US", "UTC", new[] { "people.employee.read" }, new[] { "core.platform" }, allowedTenants);
            if (!context.IsAuthorizedForTenant(tenantA)) throw new Exception("User should be authorized for Tenant A");
        });

        Run("TenantContextAuthority", "CaseB_UserAuthorizedOnlyForTenantA_SelectingTenantB_ShouldBeDenied", () =>
        {
            var allowedTenants = new HashSet<TenantId> { tenantA };
            var context = new UserContext(userA, tenantA, legalEntityA, "en-US", "UTC", new[] { "people.employee.read" }, new[] { "core.platform" }, allowedTenants);
            if (context.IsAuthorizedForTenant(tenantB)) throw new Exception("User must NOT be authorized for Tenant B");
        });

        Run("TenantContextAuthority", "CaseE_MultiTenantUserAuthorizedForAAndB_ShouldAllowBothContexts", () =>
        {
            var allowedTenants = new HashSet<TenantId> { tenantA, tenantB };
            var context = new UserContext(userA, tenantA, legalEntityA, "en-US", "UTC", new[] { "people.employee.read" }, new[] { "core.platform" }, allowedTenants);
            if (!context.IsAuthorizedForTenant(tenantA) || !context.IsAuthorizedForTenant(tenantB)) throw new Exception("Multi-tenant user should be authorized for both A and B");
        });

        Run("TenantContextAuthority", "LegalEntityAuthority_UserRestrictedToEntityA_AccessingEntityB_ShouldBeDenied", () =>
        {
            var allowedEntities = new HashSet<LegalEntityId> { legalEntityA };
            var context = new UserContext(userA, tenantA, legalEntityA, "en-US", "UTC", new[] { "people.employee.read" }, new[] { "core.platform" }, new[] { tenantA }, allowedEntities);
            if (!context.IsAuthorizedForLegalEntity(legalEntityA)) throw new Exception("Should be authorized for Entity A");
            if (context.IsAuthorizedForLegalEntity(legalEntityB)) throw new Exception("Must be DENIED for Entity B");
        });

        // 3. Cryptography & PII Encryption Tests
        Console.WriteLine("\n[SUITE] CryptographicSecurityTests");
        Run("Cryptography", "PiiEncryption_Aes256Gcm_RoundTrip", () =>
        {
            const string raw = "1098765432";
            var enc = piiService.Encrypt(raw);
            if (enc == raw) throw new Exception("Ciphertext must not match plaintext");
            if (!enc.StartsWith("v1$")) throw new Exception("Ciphertext must contain key version v1$");
            var dec = piiService.Decrypt(enc);
            if (dec != raw) throw new Exception("Decrypted value must match raw plaintext");
        });

        Run("Cryptography", "PiiEncryption_NonceUniqueness_FreshNoncePerOperation", () =>
        {
            const string raw = "1098765432";
            var enc1 = piiService.Encrypt(raw);
            var enc2 = piiService.Encrypt(raw);
            if (enc1 == enc2) throw new Exception("Fresh 96-bit nonce must produce distinct ciphertexts for identical plaintext");
            if (piiService.Decrypt(enc1) != raw || piiService.Decrypt(enc2) != raw) throw new Exception("Both ciphertexts must decrypt cleanly");
        });

        Run("Cryptography", "PiiEncryption_TamperedCiphertext_FailsClosed", () =>
        {
            const string raw = "1098765432";
            var enc = piiService.Encrypt(raw);
            var parts = enc.Split('$');
            var bytes = Convert.FromBase64String(parts[1]);
            bytes[^1] ^= 0xFF; // Tamper
            var tampered = $"{parts[0]}${Convert.ToBase64String(bytes)}";

            bool failedClosed = false;
            try { piiService.Decrypt(tampered); }
            catch (CryptographicException) { failedClosed = true; }
            if (!failedClosed) throw new Exception("Tampered ciphertext must fail closed");
        });

        Run("Cryptography", "PiiEncryption_TamperedAuthTag_FailsClosed", () =>
        {
            const string raw = "1098765432";
            var enc = piiService.Encrypt(raw);
            var parts = enc.Split('$');
            var bytes = Convert.FromBase64String(parts[1]);
            bytes[15] ^= 0xAA; // Tamper tag
            var tampered = $"{parts[0]}${Convert.ToBase64String(bytes)}";

            bool failedClosed = false;
            try { piiService.Decrypt(tampered); }
            catch (CryptographicException) { failedClosed = true; }
            if (!failedClosed) throw new Exception("Tampered authentication tag must fail closed");
        });

        Run("Cryptography", "PiiEncryption_WrongKey_FailsDecryption", () =>
        {
            const string raw = "1098765432";
            var enc = piiService.Encrypt(raw);
            var wrongService = new AesPiiEncryptionService(masterKeyBase64: Convert.ToBase64String(new byte[32]));

            bool failedClosed = false;
            try { wrongService.Decrypt(enc); }
            catch (CryptographicException) { failedClosed = true; }
            if (!failedClosed) throw new Exception("Wrong key must fail decryption");
        });

        Run("Cryptography", "PiiBlindIndex_Normalization_DeterministicAcrossFormatting", () =>
        {
            var h1 = piiService.ComputeSearchHash("1098765432");
            var h2 = piiService.ComputeSearchHash(" 109-876-5432 ");
            var h3 = piiService.ComputeSearchHash("109.876.5432");
            var h4 = piiService.ComputeSearchHash("109 876 5432");
            if (h1 != h2 || h1 != h3 || h1 != h4) throw new Exception("Normalization must produce identical blind index");

            var ph1 = piiService.ComputeSearchHash("A123-4567-B");
            var ph2 = piiService.ComputeSearchHash("a123 4567 b");
            if (ph1 != ph2) throw new Exception("Alphanumeric IDs must normalize casing and whitespace");
        });

        Run("Cryptography", "PiiBlindIndex_KeySeparation_DistinctHmacKeyProducesDifferentIndex", () =>
        {
            const string raw = "1098765432";
            var h1 = piiService.ComputeSearchHash(raw);
            var customService = new AesPiiEncryptionService(hmacKeyBase64: Convert.ToBase64String(new byte[32]));
            var h2 = customService.ComputeSearchHash(raw);
            if (h1 == h2) throw new Exception("Separate HMAC key must produce distinct blind index");
        });

        // 4. Documents & Validation Tests
        Console.WriteLine("\n[SUITE] DocumentsValidationTests");
        Run("DocumentSecurity", "MagicBytes_RejectsSpoofedFiles", () =>
        {
            var validPdf = Encoding.ASCII.GetBytes("%PDF-1.4 sample content");
            using (var s = new MemoryStream(validPdf))
            {
                DocumentSecurityValidator.ValidateContentSignatureAsync(s, "contract.pdf").GetAwaiter().GetResult();
            }

            var spoofed = new byte[] { 0x4D, 0x5A, 0x00, 0x00, 0x01 };
            using (var s = new MemoryStream(spoofed))
            {
                bool threw = false;
                try { DocumentSecurityValidator.ValidateContentSignatureAsync(s, "contract.pdf").GetAwaiter().GetResult(); }
                catch (ArgumentException) { threw = true; }
                if (!threw) throw new Exception("Spoofed executable must be rejected");
            }
        });

        Run("DocumentSecurity", "PathTraversal_RejectsMaliciousFileNames", () =>
        {
            bool threw1 = false;
            try { DocumentSecurityValidator.ValidateFileName("../../etc/shadow"); }
            catch (ArgumentException) { threw1 = true; }
            if (!threw1) throw new Exception("Path traversal ../ must be rejected");

            bool threw2 = false;
            try { DocumentSecurityValidator.ValidateFileName("..\\windows\\system32\\cmd.exe"); }
            catch (ArgumentException) { threw2 = true; }
            if (!threw2) throw new Exception("Path traversal ..\\ must be rejected");
        });

        // 5. Phase 3 Attendance Tests
        Console.WriteLine("\n[SUITE] Phase3AttendanceTests");
        Run("Attendance", "ClockEvent_Provenance_IsImmutable", () =>
        {
            var captured = DateTime.UtcNow.AddHours(-8);
            var evt = new ClockEvent(Guid.NewGuid(), tenantA, empA, ClockType.In, ClockSource.BiometricDevice, captured, captured, "TERM-01");
            if (evt.SourceDeviceId != "TERM-01") throw new Exception("Device ID mismatch");
            if (evt.Type != ClockType.In) throw new Exception("ClockType mismatch");
        });

        Run("Attendance", "AttendanceDay_Evaluation_CalculatesMinutes", () =>
        {
            var day = new AttendanceDay(Guid.NewGuid(), tenantA, legalEntityA, empA, new DateOnly(2026, 8, 24), "Africa/Cairo");
            var start = new DateTime(2026, 8, 24, 7, 0, 0, DateTimeKind.Utc);
            var end = new DateTime(2026, 8, 24, 15, 30, 0, DateTimeKind.Utc);
            var evts = new List<ClockEvent>
            {
                new(Guid.NewGuid(), tenantA, empA, ClockType.In, ClockSource.BiometricDevice, start, start),
                new(Guid.NewGuid(), tenantA, empA, ClockType.Out, ClockSource.BiometricDevice, end, end)
            };
            day.Evaluate(evts, null);
            if (day.TotalWorkedMinutes != 510) throw new Exception($"Expected 510 mins, got {day.TotalWorkedMinutes}");
            if (day.Status != AttendanceStatus.Reviewed) throw new Exception("Expected Reviewed status");
        });

        Run("Attendance", "AttendanceDay_Adjustment_AuditHistoryAndConcurrency", () =>
        {
            var day = new AttendanceDay(Guid.NewGuid(), tenantA, legalEntityA, empA, new DateOnly(2026, 8, 24));
            day.ApplyAdjustment(480, "Correction", userA.Value, 1);
            if (day.TotalWorkedMinutes != 480 || day.RowVersion != 2u) throw new Exception("Adjustment failed to update minutes or row version");

            bool threw = false;
            try { day.ApplyAdjustment(500, "Stale", userA.Value, 1); }
            catch (InvalidOperationException) { threw = true; }
            if (!threw) throw new Exception("Optimistic concurrency conflict not thrown on stale version");
        });

        // 6. Phase 3 Leave Tests
        Console.WriteLine("\n[SUITE] Phase3LeaveTests");
        Run("Leave", "LeaveBalance_Reservation_EnforcesSufficientBalance", () =>
        {
            var balance = new LeaveBalance(Guid.NewGuid(), tenantA, empA, Guid.NewGuid(), 2026, 21, 0, 5, 0);
            if (balance.AvailableDays != 16) throw new Exception("Available days mismatch");
            balance.ReservePendingDays(5, 1);
            if (balance.AvailableDays != 11) throw new Exception("Available days after reservation mismatch");

            bool threw = false;
            try { balance.ReservePendingDays(12, 2); }
            catch (InvalidOperationException) { threw = true; }
            if (!threw) throw new Exception("Over-reservation did not throw InsufficientBalance");
        });

        Run("Leave", "LeaveRequest_StateTransitions_Workflow", () =>
        {
            var req = new LeaveRequest(Guid.NewGuid(), tenantA, legalEntityA, empA, Guid.NewGuid(), new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 5), 5.0m, "Vacation");
            if (req.Status != LeaveRequestStatus.Draft) throw new Exception("Request should be Draft");
            req.Submit(Guid.NewGuid(), 1);
            if (req.Status != LeaveRequestStatus.PendingApproval) throw new Exception("Request should be PendingApproval");
            req.Approve(2);
            if (req.Status != LeaveRequestStatus.Approved) throw new Exception("Request should be Approved");
        });

        // 7. Phase 3 Approvals Tests
        Console.WriteLine("\n[SUITE] Phase3ApprovalsTests");
        Run("Approvals", "ApprovalRequest_MultiStepRouting_AdvancesStepOrder", () =>
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

        Run("Approvals", "ApprovalRequest_Rejection_TerminatesWorkflow", () =>
        {
            var appReq = new ApprovalRequest(Guid.NewGuid(), tenantA, legalEntityA, "attendance", Guid.NewGuid(), "Adjustment", "Adjust: +60", userA.Value, empA, totalSteps: 2);
            var mgr = Guid.NewGuid();
            appReq.AddStep(new ApprovalStep(Guid.NewGuid(), appReq.Id, 1, mgr));

            appReq.RejectCurrentStep(mgr, "Rejected by policy", 1);
            if (appReq.Status != ApprovalStatus.Rejected) throw new Exception("Rejection did not mark Rejected");
        });

        stopwatch.Stop();
        Console.WriteLine("\n------------------------------------------------------------");
        Console.WriteLine($"Results: Total: {passed + failed}, Passed: {passed}, Failed: {failed} (Duration: {stopwatch.ElapsedMilliseconds}ms)");
        Console.WriteLine("============================================================");

        return failed == 0 ? 0 : 1;
    }
}
