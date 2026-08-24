using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Recruitment.Domain;

public class Candidate
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public string FirstNameEn { get; private set; }
    public string LastNameEn { get; private set; }
    public string FirstNameAr { get; private set; }
    public string LastNameAr { get; private set; }
    public string Email { get; private set; }
    public string PhoneNumber { get; private set; }
    public string? Location { get; private set; }
    public string? Headline { get; private set; }
    public string? Source { get; private set; }
    public Guid? ResumeDocumentId { get; private set; }
    public string? SkillsJson { get; private set; }
    public string NormalizedEmailHash { get; private set; }
    public string NormalizedPhoneHash { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public uint RowVersion { get; private set; }

    private Candidate()
    {
        TenantId = default;
        FirstNameEn = string.Empty;
        LastNameEn = string.Empty;
        FirstNameAr = string.Empty;
        LastNameAr = string.Empty;
        Email = string.Empty;
        PhoneNumber = string.Empty;
        NormalizedEmailHash = string.Empty;
        NormalizedPhoneHash = string.Empty;
    }

    public Candidate(
        Guid id,
        TenantId tenantId,
        string firstNameEn,
        string lastNameEn,
        string firstNameAr,
        string lastNameAr,
        string email,
        string phoneNumber,
        string? location = null,
        string? headline = null,
        string? source = null,
        Guid? resumeDocumentId = null,
        string? skillsJson = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (tenantId == default || tenantId.Value == Guid.Empty) throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(firstNameEn)) throw new ArgumentException("English first name is required.", nameof(firstNameEn));
        if (string.IsNullOrWhiteSpace(lastNameEn)) throw new ArgumentException("English last name is required.", nameof(lastNameEn));
        if (string.IsNullOrWhiteSpace(firstNameAr)) throw new ArgumentException("Arabic first name is required.", nameof(firstNameAr));
        if (string.IsNullOrWhiteSpace(lastNameAr)) throw new ArgumentException("Arabic last name is required.", nameof(lastNameAr));
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.", nameof(email));

        Id = id;
        TenantId = tenantId;
        FirstNameEn = firstNameEn.Trim();
        LastNameEn = lastNameEn.Trim();
        FirstNameAr = firstNameAr.Trim();
        LastNameAr = lastNameAr.Trim();
        Email = email.Trim().ToLowerInvariant();
        PhoneNumber = phoneNumber?.Trim() ?? string.Empty;
        Location = location?.Trim();
        Headline = headline?.Trim();
        Source = source?.Trim();
        ResumeDocumentId = resumeDocumentId;
        SkillsJson = skillsJson ?? "[]";
        NormalizedEmailHash = ComputeNormalizedEmailHash(Email);
        NormalizedPhoneHash = ComputeNormalizedPhoneHash(PhoneNumber);
        CreatedAtUtc = DateTime.UtcNow;
        RowVersion = 1;
    }

    public static Candidate Reconstitute(
        Guid id,
        TenantId tenantId,
        string firstNameEn,
        string lastNameEn,
        string firstNameAr,
        string lastNameAr,
        string email,
        string phoneNumber,
        string? location,
        string? headline,
        string? source,
        Guid? resumeDocumentId,
        string? skillsJson,
        string normalizedEmailHash,
        string normalizedPhoneHash,
        DateTime createdAtUtc,
        uint rowVersion)
    {
        return new Candidate
        {
            Id = id,
            TenantId = tenantId,
            FirstNameEn = firstNameEn,
            LastNameEn = lastNameEn,
            FirstNameAr = firstNameAr,
            LastNameAr = lastNameAr,
            Email = email,
            PhoneNumber = phoneNumber,
            Location = location,
            Headline = headline,
            Source = source,
            ResumeDocumentId = resumeDocumentId,
            SkillsJson = skillsJson,
            NormalizedEmailHash = normalizedEmailHash,
            NormalizedPhoneHash = normalizedPhoneHash,
            CreatedAtUtc = createdAtUtc,
            RowVersion = rowVersion
        };
    }

    public void UpdateDetails(
        string firstNameEn,
        string lastNameEn,
        string firstNameAr,
        string lastNameAr,
        string email,
        string phoneNumber,
        string? location,
        string? headline,
        string? source,
        Guid? resumeDocumentId,
        string? skillsJson,
        uint expectedRowVersion)
    {
        if (RowVersion != expectedRowVersion)
        {
            throw new InvalidOperationException($"Concurrency conflict: Candidate has been modified. Expected version {expectedRowVersion}, current version {RowVersion}.");
        }

        if (string.IsNullOrWhiteSpace(firstNameEn)) throw new ArgumentException("English first name is required.", nameof(firstNameEn));
        if (string.IsNullOrWhiteSpace(lastNameEn)) throw new ArgumentException("English last name is required.", nameof(lastNameEn));
        if (string.IsNullOrWhiteSpace(firstNameAr)) throw new ArgumentException("Arabic first name is required.", nameof(firstNameAr));
        if (string.IsNullOrWhiteSpace(lastNameAr)) throw new ArgumentException("Arabic last name is required.", nameof(lastNameAr));
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.", nameof(email));

        FirstNameEn = firstNameEn.Trim();
        LastNameEn = lastNameEn.Trim();
        FirstNameAr = firstNameAr.Trim();
        LastNameAr = lastNameAr.Trim();
        Email = email.Trim().ToLowerInvariant();
        PhoneNumber = phoneNumber?.Trim() ?? string.Empty;
        Location = location?.Trim();
        Headline = headline?.Trim();
        Source = source?.Trim();
        ResumeDocumentId = resumeDocumentId ?? ResumeDocumentId;
        SkillsJson = skillsJson ?? SkillsJson;
        NormalizedEmailHash = ComputeNormalizedEmailHash(Email);
        NormalizedPhoneHash = ComputeNormalizedPhoneHash(PhoneNumber);
        RowVersion++;
    }

    public void AttachResume(Guid resumeDocumentId, uint expectedRowVersion)
    {
        if (RowVersion != expectedRowVersion)
        {
            throw new InvalidOperationException($"Concurrency conflict: Candidate has been modified. Expected version {expectedRowVersion}, current version {RowVersion}.");
        }
        ResumeDocumentId = resumeDocumentId;
        RowVersion++;
    }

    public static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return string.Empty;
        var trimmed = email.Trim().ToLowerInvariant();
        var parts = trimmed.Split('@');
        if (parts.Length != 2) return trimmed;

        var user = parts[0];
        var domain = parts[1];

        // Standard normalization: strip dots and sub-addressing (+tag) for common mail providers
        if (domain == "gmail.com" || domain == "googlemail.com")
        {
            var plusIdx = user.IndexOf('+');
            if (plusIdx >= 0) user = user.Substring(0, plusIdx);
            user = user.Replace(".", "");
        }

        return $"{user}@{domain}";
    }

    public static string NormalizePhoneNumber(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return string.Empty;
        // Keep digits only, handle leading 00 or +
        var digitsOnly = Regex.Replace(phone, @"\D", "");
        if (digitsOnly.StartsWith("00")) digitsOnly = digitsOnly.Substring(2);
        return digitsOnly;
    }

    public static string ComputeNormalizedEmailHash(string email)
    {
        var normalized = NormalizeEmail(email);
        if (string.IsNullOrEmpty(normalized)) return string.Empty;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string ComputeNormalizedPhoneHash(string phone)
    {
        var normalized = NormalizePhoneNumber(phone);
        if (string.IsNullOrEmpty(normalized)) return string.Empty;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
