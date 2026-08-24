using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Workforce.BuildingBlocks.Database;
using Workforce.Modules.Documents.Domain;
using Workforce.Modules.Documents.Infrastructure;
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
        Console.WriteLine(" ZAINX WORKFORCE — PHASE 2 INTEGRATION & SECURITY SUITE");
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

        // 4. Concurrency, Outbox, and Documents Tests
        Console.WriteLine("\n[SUITE] ConcurrencyAndDocumentTests");
        Run("Concurrency", "OptimisticConcurrency_Employment_ThrowsOnVersionMismatch", () =>
        {
            var emp = new Employment(Guid.NewGuid(), tenantA, Guid.NewGuid(), legalEntityA, "EMP-1002", new DateOnly(2024, 1, 1));
            emp.Activate(1);
            if (emp.RowVersion != 2u) throw new Exception("RowVersion should be 2");

            bool threw = false;
            try { emp.UpdateEmploymentDates(new DateOnly(2024, 2, 1), null, 1); }
            catch (InvalidOperationException) { threw = true; }
            if (!threw) throw new Exception("Stale version 1 must throw concurrency conflict");
        });

        Run("DocumentSecurity", "MagicBytes_RejectsSpoofedFiles", () =>
        {
            var validPdf = Encoding.ASCII.GetBytes("%PDF-1.4 sample content");
            using (var s = new MemoryStream(validPdf))
            {
                DocumentSecurityValidator.ValidateContentSignatureAsync(s, "contract.pdf").GetAwaiter().GetResult();
            }

            var spoofed = Encoding.ASCII.GetBytes("MZ\x90\x00executable");
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

        stopwatch.Stop();
        Console.WriteLine("\n------------------------------------------------------------");
        Console.WriteLine($"Results: Total: {passed + failed}, Passed: {passed}, Failed: {failed} (Duration: {stopwatch.ElapsedMilliseconds}ms)");
        Console.WriteLine("============================================================");

        return failed == 0 ? 0 : 1;
    }
}
