using System;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Recruitment.Domain;

public class Offer
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public LegalEntityId LegalEntityId { get; private set; }
    public Guid ApplicationId { get; private set; }
    public Guid CandidateId { get; private set; }
    public int OfferVersionNumber { get; private set; }
    public string TitleEn { get; private set; }
    public string TitleAr { get; private set; }
    public DateOnly ProposedStartDate { get; private set; }
    public decimal BaseSalaryMonthly { get; private set; }
    public string Currency { get; private set; }
    public string? AllowancesJson { get; private set; }
    public string? ConditionsNote { get; private set; }
    public OfferStatus Status { get; private set; }
    public Guid? ApprovalRequestId { get; private set; }
    public DateTime? IssuedAtUtc { get; private set; }
    public DateTime? AcceptedAtUtc { get; private set; }
    public DateOnly? ExpiryDate { get; private set; }
    public Guid? OfferDocumentId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public uint RowVersion { get; private set; }

    private Offer()
    {
        TenantId = default;
        LegalEntityId = default;
        TitleEn = string.Empty;
        TitleAr = string.Empty;
        Currency = string.Empty;
    }

    public Offer(
        Guid id,
        TenantId tenantId,
        LegalEntityId legalEntityId,
        Guid applicationId,
        Guid candidateId,
        int offerVersionNumber,
        string titleEn,
        string titleAr,
        DateOnly proposedStartDate,
        decimal baseSalaryMonthly,
        string currency,
        string? allowancesJson = null,
        string? conditionsNote = null,
        DateOnly? expiryDate = null,
        Guid? offerDocumentId = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (tenantId == default || tenantId.Value == Guid.Empty) throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        if (legalEntityId == default || legalEntityId.Value == Guid.Empty) throw new ArgumentException("LegalEntityId cannot be empty.", nameof(legalEntityId));
        if (applicationId == Guid.Empty) throw new ArgumentException("ApplicationId cannot be empty.", nameof(applicationId));
        if (candidateId == Guid.Empty) throw new ArgumentException("CandidateId cannot be empty.", nameof(candidateId));
        if (offerVersionNumber <= 0) throw new ArgumentException("Offer version number must be positive.", nameof(offerVersionNumber));
        if (string.IsNullOrWhiteSpace(titleEn)) throw new ArgumentException("English title is required.", nameof(titleEn));
        if (string.IsNullOrWhiteSpace(titleAr)) throw new ArgumentException("Arabic title is required.", nameof(titleAr));
        if (baseSalaryMonthly < 0) throw new ArgumentException("Base salary cannot be negative.", nameof(baseSalaryMonthly));
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required.", nameof(currency));

        Id = id;
        TenantId = tenantId;
        LegalEntityId = legalEntityId;
        ApplicationId = applicationId;
        CandidateId = candidateId;
        OfferVersionNumber = offerVersionNumber;
        TitleEn = titleEn.Trim();
        TitleAr = titleAr.Trim();
        ProposedStartDate = proposedStartDate;
        BaseSalaryMonthly = baseSalaryMonthly;
        Currency = currency.Trim().ToUpperInvariant();
        AllowancesJson = allowancesJson ?? "[]";
        ConditionsNote = conditionsNote?.Trim();
        ExpiryDate = expiryDate;
        OfferDocumentId = offerDocumentId;
        Status = OfferStatus.Draft;
        CreatedAtUtc = DateTime.UtcNow;
        RowVersion = 1;
    }

    public static Offer Reconstitute(
        Guid id,
        TenantId tenantId,
        LegalEntityId legalEntityId,
        Guid applicationId,
        Guid candidateId,
        int offerVersionNumber,
        string titleEn,
        string titleAr,
        DateOnly proposedStartDate,
        decimal baseSalaryMonthly,
        string currency,
        string? allowancesJson,
        string? conditionsNote,
        OfferStatus status,
        Guid? approvalRequestId,
        DateTime? issuedAtUtc,
        DateTime? acceptedAtUtc,
        DateOnly? expiryDate,
        Guid? offerDocumentId,
        DateTime createdAtUtc,
        uint rowVersion)
    {
        return new Offer
        {
            Id = id,
            TenantId = tenantId,
            LegalEntityId = legalEntityId,
            ApplicationId = applicationId,
            CandidateId = candidateId,
            OfferVersionNumber = offerVersionNumber,
            TitleEn = titleEn,
            TitleAr = titleAr,
            ProposedStartDate = proposedStartDate,
            BaseSalaryMonthly = baseSalaryMonthly,
            Currency = currency,
            AllowancesJson = allowancesJson,
            ConditionsNote = conditionsNote,
            Status = status,
            ApprovalRequestId = approvalRequestId,
            IssuedAtUtc = issuedAtUtc,
            AcceptedAtUtc = acceptedAtUtc,
            ExpiryDate = expiryDate,
            OfferDocumentId = offerDocumentId,
            CreatedAtUtc = createdAtUtc,
            RowVersion = rowVersion
        };
    }

    public void UpdateTerms(
        string titleEn,
        string titleAr,
        DateOnly proposedStartDate,
        decimal baseSalaryMonthly,
        string currency,
        string? allowancesJson,
        string? conditionsNote,
        DateOnly? expiryDate,
        Guid? offerDocumentId,
        uint expectedRowVersion)
    {
        ValidateConcurrency(expectedRowVersion);
        if (Status != OfferStatus.Draft)
            throw new InvalidOperationException($"Cannot edit terms of offer in status '{Status}'. Only 'Draft' offers can be edited directly.");

        TitleEn = titleEn.Trim();
        TitleAr = titleAr.Trim();
        ProposedStartDate = proposedStartDate;
        BaseSalaryMonthly = baseSalaryMonthly;
        Currency = currency.Trim().ToUpperInvariant();
        AllowancesJson = allowancesJson ?? AllowancesJson;
        ConditionsNote = conditionsNote?.Trim();
        ExpiryDate = expiryDate;
        OfferDocumentId = offerDocumentId ?? OfferDocumentId;
        RowVersion++;
    }

    public void SubmitForApproval(Guid approvalRequestId, uint expectedRowVersion)
    {
        ValidateConcurrency(expectedRowVersion);
        if (Status != OfferStatus.Draft)
            throw new InvalidOperationException($"Cannot submit offer in status '{Status}'. Must be 'Draft'.");

        Status = OfferStatus.PendingApproval;
        ApprovalRequestId = approvalRequestId;
        RowVersion++;
    }

    public void Approve(uint expectedRowVersion)
    {
        ValidateConcurrency(expectedRowVersion);
        if (Status != OfferStatus.PendingApproval)
            throw new InvalidOperationException($"Cannot approve offer in status '{Status}'. Must be 'PendingApproval'.");

        Status = OfferStatus.Approved;
        RowVersion++;
    }

    public void Issue(uint expectedRowVersion)
    {
        ValidateConcurrency(expectedRowVersion);
        if (Status != OfferStatus.Approved)
            throw new InvalidOperationException($"Cannot issue offer in status '{Status}'. Must be 'Approved'.");

        Status = OfferStatus.Issued;
        IssuedAtUtc = DateTime.UtcNow;
        RowVersion++;
    }

    public void Accept(uint expectedRowVersion)
    {
        ValidateConcurrency(expectedRowVersion);
        if (Status != OfferStatus.Issued)
            throw new InvalidOperationException($"Cannot accept offer in status '{Status}'. Must be 'Issued'.");

        Status = OfferStatus.Accepted;
        AcceptedAtUtc = DateTime.UtcNow;
        RowVersion++;
    }

    public void Decline(uint expectedRowVersion)
    {
        ValidateConcurrency(expectedRowVersion);
        if (Status != OfferStatus.Issued)
            throw new InvalidOperationException($"Cannot decline offer in status '{Status}'. Must be 'Issued'.");

        Status = OfferStatus.Declined;
        RowVersion++;
    }

    public void Withdraw(uint expectedRowVersion)
    {
        ValidateConcurrency(expectedRowVersion);
        if (Status == OfferStatus.Accepted || Status == OfferStatus.Declined || Status == OfferStatus.Withdrawn)
            throw new InvalidOperationException($"Cannot withdraw offer in terminal status '{Status}'.");

        Status = OfferStatus.Withdrawn;
        RowVersion++;
    }

    private void ValidateConcurrency(uint expectedRowVersion)
    {
        if (RowVersion != expectedRowVersion)
        {
            throw new InvalidOperationException($"Concurrency conflict: Offer has been modified. Expected version {expectedRowVersion}, current version {RowVersion}.");
        }
    }
}
