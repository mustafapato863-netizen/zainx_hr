using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Documents.Domain;

public class Document
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public LegalEntityId LegalEntityId { get; private set; }
    public string OwnerType { get; private set; }
    public Guid OwnerId { get; private set; }
    public Guid DocumentTypeId { get; private set; }
    public string Title { get; private set; }
    public DocumentStatus Status { get; private set; }
    public DateOnly? ExpiryDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }

    private Document()
    {
        OwnerType = string.Empty;
        Title = string.Empty;
    }

    public Document(
        Guid id,
        TenantId tenantId,
        LegalEntityId legalEntityId,
        string ownerType,
        Guid ownerId,
        Guid documentTypeId,
        string title,
        DateOnly? expiryDate,
        Guid createdBy)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (ownerId == Guid.Empty) throw new ArgumentException("OwnerId cannot be empty.", nameof(ownerId));
        if (documentTypeId == Guid.Empty) throw new ArgumentException("DocumentTypeId cannot be empty.", nameof(documentTypeId));
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title cannot be empty.", nameof(title));

        Id = id;
        TenantId = tenantId;
        LegalEntityId = legalEntityId;
        OwnerType = ownerType.Trim();
        OwnerId = ownerId;
        DocumentTypeId = documentTypeId;
        Title = title.Trim();
        Status = DocumentStatus.Active;
        ExpiryDate = expiryDate;
        CreatedAt = DateTime.UtcNow;
        CreatedBy = createdBy;
    }

    public void Archive()
    {
        Status = DocumentStatus.Archived;
    }

    public void MarkExpired()
    {
        Status = DocumentStatus.Expired;
    }
}
