using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Architecture.Tests;

public static class Program
{
    public static int Main(string[] args)
    {
        Console.WriteLine("============================================================");
        Console.WriteLine(" ZAINX WORKFORCE — PHASE 2 INTEGRATION & SECURITY SUITE");
        Console.WriteLine("============================================================");

        var stopwatch = Stopwatch.StartNew();
        int passed = 0;
        int failed = 0;

        void RunTest(string suiteName, string testName, Action action)
        {
            try
            {
                action();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("  [PASS] ");
                Console.ResetColor();
                Console.WriteLine($"{suiteName} > {testName}");
                passed++;
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException ?? ex;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("  [FAIL] ");
                Console.ResetColor();
                Console.WriteLine($"{suiteName} > {testName}: {inner.Message}");
                Console.WriteLine(inner.StackTrace);
                failed++;
            }
        }

        // 1. Boundary Tests
        Console.WriteLine("\n[SUITE] BoundaryTests");
        var boundary = new BoundaryTests();
        RunTest("BoundaryTests", "SharedKernel_ShouldNotReference_ModulesOrHost", () => boundary.SharedKernel_ShouldNotReference_ModulesOrHost());
        RunTest("BoundaryTests", "BuildingBlocks_ShouldNotReference_ModulesOrHost", () => boundary.BuildingBlocks_ShouldNotReference_ModulesOrHost());

        // 2. Phase 2 Domain Tests
        Console.WriteLine("\n[SUITE] Phase2DomainTests");
        var domain = new Phase2DomainTests();
        RunTest("Phase2DomainTests", "OrganizationUnit_EffectivePeriod_ShouldDetectActiveStatus", () => domain.OrganizationUnit_EffectivePeriod_ShouldDetectActiveStatus());
        RunTest("Phase2DomainTests", "OrganizationUnit_Creation_ShouldSetInitialState", () => domain.OrganizationUnit_Creation_ShouldSetInitialState());
        RunTest("Phase2DomainTests", "Employment_StateMachine_ShouldTransitionStatus", () => domain.Employment_StateMachine_ShouldTransitionStatus());
        RunTest("Phase2DomainTests", "EmploymentAssignment_TemporalDating_ShouldValidateCurrent", () => domain.EmploymentAssignment_TemporalDating_ShouldValidateCurrent());
        RunTest("Phase2DomainTests", "Document_StateManagement_ShouldSupportLifecycle", () => domain.Document_StateManagement_ShouldSupportLifecycle());
        RunTest("Phase2DomainTests", "Modules_ShouldNotReferenceDownstreamPayrollOrCompliance", () => domain.Modules_ShouldNotReferenceDownstreamPayrollOrCompliance());

        // 3. Phase 2 Security & Cryptography Integration Tests
        Console.WriteLine("\n[SUITE] Phase2SecurityIntegrationTests");
        var security = new Phase2SecurityIntegrationTests();
        RunTest("Phase2SecurityIntegrationTests", "TenantContextAuthority_CaseA_UserAuthorizedForTenantA_SelectingTenantA_ShouldSucceed", () => security.TenantContextAuthority_CaseA_UserAuthorizedForTenantA_SelectingTenantA_ShouldSucceed());
        RunTest("Phase2SecurityIntegrationTests", "TenantContextAuthority_CaseB_UserAuthorizedOnlyForTenantA_SelectingTenantB_ShouldBeDenied", () => security.TenantContextAuthority_CaseB_UserAuthorizedOnlyForTenantA_SelectingTenantB_ShouldBeDenied());
        RunTest("Phase2SecurityIntegrationTests", "TenantContextAuthority_CaseE_MultiTenantUserAuthorizedForAAndB_ShouldAllowBothContexts", () => security.TenantContextAuthority_CaseE_MultiTenantUserAuthorizedForAAndB_ShouldAllowBothContexts());
        RunTest("Phase2SecurityIntegrationTests", "LegalEntityAuthority_UserRestrictedToEntityA_AccessingEntityB_ShouldBeDenied", () => security.LegalEntityAuthority_UserRestrictedToEntityA_AccessingEntityB_ShouldBeDenied());
        RunTest("Phase2SecurityIntegrationTests", "PiiEncryption_Aes256Gcm_ShouldEncryptAndDecryptAccurately", () => security.PiiEncryption_Aes256Gcm_ShouldEncryptAndDecryptAccurately());
        RunTest("Phase2SecurityIntegrationTests", "PiiEncryption_NonceUniqueness_RepeatedEncryptionShouldProduceDifferentCiphertexts", () => security.PiiEncryption_NonceUniqueness_RepeatedEncryptionShouldProduceDifferentCiphertexts());
        RunTest("Phase2SecurityIntegrationTests", "PiiEncryption_TamperedCiphertext_ShouldFailClosed", () => security.PiiEncryption_TamperedCiphertext_ShouldFailClosed());
        RunTest("Phase2SecurityIntegrationTests", "PiiEncryption_TamperedAuthTag_ShouldFailClosed", () => security.PiiEncryption_TamperedAuthTag_ShouldFailClosed());
        RunTest("Phase2SecurityIntegrationTests", "PiiEncryption_WrongKey_ShouldFailToDecrypt", () => security.PiiEncryption_WrongKey_ShouldFailToDecrypt());
        RunTest("Phase2SecurityIntegrationTests", "PiiBlindIndex_Normalization_ShouldBeDeterministicAcrossFormatting", () => security.PiiBlindIndex_Normalization_ShouldBeDeterministicAcrossFormatting());
        RunTest("Phase2SecurityIntegrationTests", "PiiBlindIndex_KeySeparation_DifferentHmacKeyShouldProduceDifferentIndex", () => security.PiiBlindIndex_KeySeparation_DifferentHmacKeyShouldProduceDifferentIndex());
        RunTest("Phase2SecurityIntegrationTests", "MultiTenant_Employment_ShouldEnforceTenantAndLegalEntityIsolation", () => security.MultiTenant_Employment_ShouldEnforceTenantAndLegalEntityIsolation());
        RunTest("Phase2SecurityIntegrationTests", "SensitivePII_UserContextPermission_ShouldDenyUnauthorizedUser", () => security.SensitivePII_UserContextPermission_ShouldDenyUnauthorizedUser());
        RunTest("Phase2SecurityIntegrationTests", "EffectiveDate_Integrity_ShouldPreventEndBeforeStart", () => security.EffectiveDate_Integrity_ShouldPreventEndBeforeStart());
        RunTest("Phase2SecurityIntegrationTests", "Assignment_Timeline_ClosingCurrent_ShouldTransitionCleanly", () => security.Assignment_Timeline_ClosingCurrent_ShouldTransitionCleanly());
        RunTest("Phase2SecurityIntegrationTests", "OptimisticConcurrency_Employment_ShouldThrowOnVersionMismatch", () => security.OptimisticConcurrency_Employment_ShouldThrowOnVersionMismatch());
        RunTest("Phase2SecurityIntegrationTests", "DocumentSecurity_MagicBytes_ShouldRejectSpoofedFiles", () => security.DocumentSecurity_MagicBytes_ShouldRejectSpoofedFiles().GetAwaiter().GetResult());
        RunTest("Phase2SecurityIntegrationTests", "DocumentSecurity_PathTraversal_ShouldRejectMaliciousFileNames", () => security.DocumentSecurity_PathTraversal_ShouldRejectMaliciousFileNames());
        RunTest("Phase2SecurityIntegrationTests", "DocumentVersion_Replacement_ShouldPreserveHistoryModel", () => security.DocumentVersion_Replacement_ShouldPreserveHistoryModel());

        stopwatch.Stop();
        Console.WriteLine("\n------------------------------------------------------------");
        Console.WriteLine($"Results: Total: {passed + failed}, Passed: {passed}, Failed: {failed} (Duration: {stopwatch.ElapsedMilliseconds}ms)");
        Console.WriteLine("============================================================");

        return failed == 0 ? 0 : 1;
    }
}
