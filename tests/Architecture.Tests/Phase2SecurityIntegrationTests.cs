using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Workforce.Modules.Documents.Domain;
using Workforce.Modules.Documents.Infrastructure;
using Workforce.Modules.Organization.Domain;
using Workforce.Modules.People.Domain;
using Workforce.SharedKernel.Primitives;
using Workforce.SharedKernel.Security;

namespace Architecture.Tests;

public static class Assert
{
    public static void True(bool condition, string? message = null)
    {
        if (!condition) throw new InvalidOperationException(message ?? "Expected condition to be true, but was false.");
    }

    public static void False(bool condition, string? message = null)
    {
        if (condition) throw new InvalidOperationException(message ?? "Expected condition to be false, but was true.");
    }

    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected '{expected}', but received '{actual}'.");
        }
    }

    public static void DoesNotContain(string substring, string? target, StringComparison comparison = StringComparison.Ordinal)
    {
        if (target != null && target.Contains(substring, comparison))
        {
            throw new InvalidOperationException($"Expected string NOT to contain '{substring}', but found in '{target}'.");
        }
    }

    public static void NotEqual<T>(T expected, T actual)
    {
        if (EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected value NOT to equal '{expected}', but it was equal.");
        }
    }

    public static void Null(object? obj)
    {
        if (obj != null) throw new InvalidOperationException($"Expected null, but received '{obj}'.");
    }

    public static void NotNull(object? obj)
    {
        if (obj == null) throw new InvalidOperationException("Expected non-null value, but received null.");
    }

    public static T Throws<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T ex)
        {
            return ex;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Expected exception of type '{typeof(T).Name}', but '{ex.GetType().Name}' was thrown.");
        }

        throw new InvalidOperationException($"Expected exception of type '{typeof(T).Name}', but no exception was thrown.");
    }

    public static async Task<T> ThrowsAsync<T>(Func<Task> action) where T : Exception
    {
        try
        {
            await action();
        }
        catch (T ex)
        {
            return ex;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Expected exception of type '{typeof(T).Name}', but '{ex.GetType().Name}' was thrown.");
        }

        throw new InvalidOperationException($"Expected exception of type '{typeof(T).Name}', but no exception was thrown.");
    }
}

public class Phase2SecurityIntegrationTests
{
    private readonly TenantId _tenantA = TenantId.New();
    private readonly TenantId _tenantB = TenantId.New();
    private readonly LegalEntityId _legalEntityA = LegalEntityId.New();
    private readonly LegalEntityId _legalEntityB = LegalEntityId.New();
    private readonly UserId _userA = UserId.New();
    private readonly IPiiEncryptionService _piiService = new AesPiiEncryptionService();

    public void TenantContextAuthority_CaseA_UserAuthorizedForTenantA_SelectingTenantA_ShouldSucceed()
    {
        var allowedTenants = new HashSet<TenantId> { _tenantA };
        var allowedEntities = new HashSet<LegalEntityId> { _legalEntityA };

        var context = new UserContext(
            _userA,
            _tenantA,
            _legalEntityA,
            "en-US",
            "UTC",
            new[] { "people.employee.read" },
            new[] { "core.platform" },
            allowedTenants,
            allowedEntities
        );

        Assert.True(context.IsAuthorizedForTenant(_tenantA));
        Assert.Equal(_tenantA, context.TenantId);
    }

    public void TenantContextAuthority_CaseB_UserAuthorizedOnlyForTenantA_SelectingTenantB_ShouldBeDenied()
    {
        var allowedTenants = new HashSet<TenantId> { _tenantA }; // Only Tenant A

        var context = new UserContext(
            _userA,
            _tenantA,
            _legalEntityA,
            "en-US",
            "UTC",
            new[] { "people.employee.read" },
            new[] { "core.platform" },
            allowedTenants
        );

        // Security check: User is NOT authorized for Tenant B
        Assert.False(context.IsAuthorizedForTenant(_tenantB));
    }

    public void TenantContextAuthority_CaseE_MultiTenantUserAuthorizedForAAndB_ShouldAllowBothContexts()
    {
        var allowedTenants = new HashSet<TenantId> { _tenantA, _tenantB };

        var contextA = new UserContext(
            _userA,
            _tenantA,
            _legalEntityA,
            "en-US",
            "UTC",
            new[] { "people.employee.read" },
            new[] { "core.platform" },
            allowedTenants
        );

        var contextB = new UserContext(
            _userA,
            _tenantB,
            _legalEntityB,
            "en-US",
            "UTC",
            new[] { "people.employee.read" },
            new[] { "core.platform" },
            allowedTenants
        );

        Assert.True(contextA.IsAuthorizedForTenant(_tenantA));
        Assert.True(contextA.IsAuthorizedForTenant(_tenantB));
        Assert.True(contextB.IsAuthorizedForTenant(_tenantA));
        Assert.True(contextB.IsAuthorizedForTenant(_tenantB));
    }

    public void LegalEntityAuthority_UserRestrictedToEntityA_AccessingEntityB_ShouldBeDenied()
    {
        var allowedEntities = new HashSet<LegalEntityId> { _legalEntityA };

        var context = new UserContext(
            _userA,
            _tenantA,
            _legalEntityA,
            "en-US",
            "UTC",
            new[] { "people.employee.read" },
            new[] { "core.platform" },
            new[] { _tenantA },
            allowedEntities
        );

        Assert.True(context.IsAuthorizedForLegalEntity(_legalEntityA));
        Assert.False(context.IsAuthorizedForLegalEntity(_legalEntityB));
    }

    public void PiiEncryption_Aes256Gcm_ShouldEncryptAndDecryptAccurately()
    {
        const string rawNationalId = "1098765432";
        var encrypted = _piiService.Encrypt(rawNationalId);
        var blindHash = _piiService.ComputeSearchHash(rawNationalId);
        var masked = _piiService.MaskNationalId(rawNationalId);

        Assert.NotEqual(rawNationalId, encrypted);
        Assert.NotEqual(rawNationalId, blindHash);
        Assert.Equal("109******2", masked);

        // Decrypt must recover exact plaintext
        var decrypted = _piiService.Decrypt(encrypted);
        Assert.Equal(rawNationalId, decrypted);

        // Blind search index must be deterministic for exact query match
        var blindHash2 = _piiService.ComputeSearchHash("1098765432");
        Assert.Equal(blindHash, blindHash2);
    }

    public void MultiTenant_Employment_ShouldEnforceTenantAndLegalEntityIsolation()
    {
        var rawNatId = "1098765432";
        var encryptedNatId = _piiService.Encrypt(rawNatId);
        var hash = _piiService.ComputeSearchHash(rawNatId);
        var masked = _piiService.MaskNationalId(rawNatId);

        var personA = new Person(
            Guid.NewGuid(),
            _tenantA,
            "Tariq",
            "Al-Mansoor",
            "طارق",
            "المنصور",
            new DateOnly(1990, 5, 15),
            "Male",
            "SA",
            encryptedNatId,
            hash,
            masked,
            "tariq@zainx.com",
            "+966500000001"
        );

        var employmentA = new Employment(
            Guid.NewGuid(),
            _tenantA,
            personA.Id,
            _legalEntityA,
            "EMP-1001",
            new DateOnly(2024, 1, 1),
            null,
            EmploymentStatus.Active
        );

        Assert.Equal(_tenantA, employmentA.TenantId);
        Assert.Equal(_legalEntityA, employmentA.LegalEntityId);
        Assert.Equal("109******2", personA.MaskedNationalIdentifier);

        // Cross-tenant verification
        Assert.NotEqual(_tenantB, employmentA.TenantId);
        Assert.NotEqual(_legalEntityB, employmentA.LegalEntityId);
    }

    public void SensitivePII_UserContextPermission_ShouldDenyUnauthorizedUser()
    {
        var unauthorizedContext = new UserContext(
            _userA,
            _tenantA,
            _legalEntityA,
            "en-US",
            "UTC",
            new[] { "people.employee.read" }, // Lacks 'people.employee.reveal_pii'
            new[] { "core.platform" }
        );

        var authorizedContext = new UserContext(
            _userA,
            _tenantA,
            _legalEntityA,
            "en-US",
            "UTC",
            new[] { "people.employee.read", "people.employee.reveal_pii" },
            new[] { "core.platform" }
        );

        Assert.False(unauthorizedContext.HasPermission("people.employee.reveal_pii"));
        Assert.True(authorizedContext.HasPermission("people.employee.reveal_pii"));
    }

    public void EffectiveDate_Integrity_ShouldPreventEndBeforeStart()
    {
        var validPeriod = new EffectivePeriod(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));
        Assert.True(validPeriod.EffectiveTo >= validPeriod.EffectiveFrom);

        Assert.Throws<ArgumentException>(() =>
            new EffectivePeriod(new DateOnly(2024, 12, 31), new DateOnly(2024, 1, 1))
        );
    }

    public void Assignment_Timeline_ClosingCurrent_ShouldTransitionCleanly()
    {
        var initial = new EmploymentAssignment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Junior Engineer",
            "مهندس مبتدئ",
            new DateOnly(2024, 1, 1),
            null,
            null,
            null,
            null,
            true
        );

        Assert.True(initial.IsCurrent);
        Assert.Null(initial.EffectiveTo);

        // Close when promoting
        var promotionDate = new DateOnly(2024, 7, 1);
        initial.CloseAssignment(promotionDate.AddDays(-1));

        Assert.False(initial.IsCurrent);
        Assert.Equal(new DateOnly(2024, 6, 30), initial.EffectiveTo);
    }

    public void OptimisticConcurrency_Employment_ShouldThrowOnVersionMismatch()
    {
        var emp = new Employment(
            Guid.NewGuid(),
            _tenantA,
            Guid.NewGuid(),
            _legalEntityA,
            "EMP-1002",
            new DateOnly(2024, 1, 1)
        );

        Assert.Equal(1u, emp.RowVersion);

        // Simulating concurrent update: expecting version 1 increments to version 2
        emp.Activate(expectedRowVersion: 1);
        Assert.Equal(2u, emp.RowVersion);

        // Submitting with stale version 1 must throw concurrency conflict
        Assert.Throws<InvalidOperationException>(() =>
            emp.UpdateEmploymentDates(new DateOnly(2024, 2, 1), null, expectedRowVersion: 1)
        );
    }

    public async Task DocumentSecurity_MagicBytes_ShouldRejectSpoofedFiles()
    {
        // Valid PDF magic bytes (%PDF-1.4)
        var validPdfBytes = Encoding.ASCII.GetBytes("%PDF-1.4 sample pdf content");
        using var validPdfStream = new MemoryStream(validPdfBytes);
        await DocumentSecurityValidator.ValidateContentSignatureAsync(validPdfStream, "contract.pdf");

        // Spoofed executable renamed to .pdf
        var maliciousExeBytes = Encoding.ASCII.GetBytes("MZ\x90\x00\x03\x00fake_executable");
        using var maliciousStream = new MemoryStream(maliciousExeBytes);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            DocumentSecurityValidator.ValidateContentSignatureAsync(maliciousStream, "contract.pdf")
        );
    }

    public void DocumentSecurity_PathTraversal_ShouldRejectMaliciousFileNames()
    {
        Assert.Throws<ArgumentException>(() =>
            DocumentSecurityValidator.ValidateFileName("../../etc/shadow")
        );

        Assert.Throws<ArgumentException>(() =>
            DocumentSecurityValidator.ValidateFileName("..\\..\\windows\\system32\\cmd.exe")
        );

        Assert.Throws<ArgumentException>(() =>
            DocumentSecurityValidator.ValidateFileName("malicious.exe")
        );

        // Valid file name passes
        DocumentSecurityValidator.ValidateFileName("employment_contract.pdf");
    }

    public void DocumentVersion_Replacement_ShouldPreserveHistoryModel()
    {
        var docId = Guid.NewGuid();
        var v1 = new DocumentVersion(
            Guid.NewGuid(),
            docId,
            1,
            "tenant-1/v1_key.pdf",
            "national_id.pdf",
            102400,
            "application/pdf",
            "sha256-v1",
            Guid.NewGuid()
        );

        var v2 = new DocumentVersion(
            Guid.NewGuid(),
            docId,
            2,
            "tenant-1/v2_key.pdf",
            "national_id_renewed.pdf",
            105600,
            "application/pdf",
            "sha256-v2",
            Guid.NewGuid()
        );

        Assert.Equal(1, v1.VersionNumber);
        Assert.Equal(2, v2.VersionNumber);
        Assert.NotEqual(v1.Sha256Checksum, v2.Sha256Checksum);
        Assert.NotEqual(v1.StorageKey, v2.StorageKey);
    }
}
