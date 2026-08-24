using System;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.People.Domain;

public class Person
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public string FirstNameEn { get; private set; }
    public string LastNameEn { get; private set; }
    public string FirstNameAr { get; private set; }
    public string LastNameAr { get; private set; }
    public DateOnly DateOfBirth { get; private set; }
    public string Gender { get; private set; }
    public string Nationality { get; private set; }
    
    // Encrypted PII persistence
    public string NationalIdentifierEncrypted { get; private set; }
    public string NationalIdentifierHash { get; private set; }
    public string MaskedNationalIdentifier { get; private set; }

    public string PrimaryEmail { get; private set; }
    public string PhoneNumber { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public string FullNameEn => $"{FirstNameEn} {LastNameEn}".Trim();
    public string FullNameAr => $"{FirstNameAr} {LastNameAr}".Trim();

    private Person()
    {
        FirstNameEn = string.Empty;
        LastNameEn = string.Empty;
        FirstNameAr = string.Empty;
        LastNameAr = string.Empty;
        Gender = string.Empty;
        Nationality = string.Empty;
        NationalIdentifierEncrypted = string.Empty;
        NationalIdentifierHash = string.Empty;
        MaskedNationalIdentifier = string.Empty;
        PrimaryEmail = string.Empty;
        PhoneNumber = string.Empty;
    }

    public Person(
        Guid id,
        TenantId tenantId,
        string firstNameEn,
        string lastNameEn,
        string firstNameAr,
        string lastNameAr,
        DateOnly dateOfBirth,
        string gender,
        string nationality,
        string nationalIdentifierEncrypted,
        string nationalIdentifierHash,
        string maskedNationalIdentifier,
        string primaryEmail,
        string phoneNumber)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(firstNameEn)) throw new ArgumentException("English first name is required.", nameof(firstNameEn));
        if (string.IsNullOrWhiteSpace(lastNameEn)) throw new ArgumentException("English last name is required.", nameof(lastNameEn));
        if (string.IsNullOrWhiteSpace(firstNameAr)) throw new ArgumentException("Arabic first name is required.", nameof(firstNameAr));
        if (string.IsNullOrWhiteSpace(lastNameAr)) throw new ArgumentException("Arabic last name is required.", nameof(lastNameAr));
        if (string.IsNullOrWhiteSpace(nationalIdentifierEncrypted)) throw new ArgumentException("Encrypted national identifier is required.", nameof(nationalIdentifierEncrypted));

        Id = id;
        TenantId = tenantId;
        FirstNameEn = firstNameEn.Trim();
        LastNameEn = lastNameEn.Trim();
        FirstNameAr = firstNameAr.Trim();
        LastNameAr = lastNameAr.Trim();
        DateOfBirth = dateOfBirth;
        Gender = gender?.Trim() ?? "Unspecified";
        Nationality = nationality?.Trim() ?? "SA";
        NationalIdentifierEncrypted = nationalIdentifierEncrypted;
        NationalIdentifierHash = nationalIdentifierHash;
        MaskedNationalIdentifier = maskedNationalIdentifier;
        PrimaryEmail = primaryEmail?.Trim().ToLowerInvariant() ?? string.Empty;
        PhoneNumber = phoneNumber?.Trim() ?? string.Empty;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePersonalDetails(
        string firstNameEn,
        string lastNameEn,
        string firstNameAr,
        string lastNameAr,
        DateOnly dateOfBirth,
        string gender,
        string nationality,
        string nationalIdentifierEncrypted,
        string nationalIdentifierHash,
        string maskedNationalIdentifier,
        string primaryEmail,
        string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(firstNameEn)) throw new ArgumentException("English first name is required.", nameof(firstNameEn));
        if (string.IsNullOrWhiteSpace(lastNameEn)) throw new ArgumentException("English last name is required.", nameof(lastNameEn));
        if (string.IsNullOrWhiteSpace(firstNameAr)) throw new ArgumentException("Arabic first name is required.", nameof(firstNameAr));
        if (string.IsNullOrWhiteSpace(lastNameAr)) throw new ArgumentException("Arabic last name is required.", nameof(lastNameAr));

        FirstNameEn = firstNameEn.Trim();
        LastNameEn = lastNameEn.Trim();
        FirstNameAr = firstNameAr.Trim();
        LastNameAr = lastNameAr.Trim();
        DateOfBirth = dateOfBirth;
        Gender = gender?.Trim() ?? Gender;
        Nationality = nationality?.Trim() ?? Nationality;
        if (!string.IsNullOrWhiteSpace(nationalIdentifierEncrypted))
        {
            NationalIdentifierEncrypted = nationalIdentifierEncrypted;
            NationalIdentifierHash = nationalIdentifierHash;
            MaskedNationalIdentifier = maskedNationalIdentifier;
        }
        PrimaryEmail = primaryEmail?.Trim().ToLowerInvariant() ?? PrimaryEmail;
        PhoneNumber = phoneNumber?.Trim() ?? PhoneNumber;
        UpdatedAt = DateTime.UtcNow;
    }
}
