namespace Workforce.Modules.Documents.Application;

public class DocumentSummaryDto
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string LegalEntityId { get; set; } = string.Empty;
    public string OwnerType { get; set; } = "Employee";
    public Guid OwnerId { get; set; }
    public Guid DocumentTypeId { get; set; }
    public string DocumentTypeCode { get; set; } = string.Empty;
    public string DocumentTypeNameEn { get; set; } = string.Empty;
    public string DocumentTypeNameAr { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public string? ExpiryDate { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public int LatestVersionNumber { get; set; }
    public string LatestFileName { get; set; } = string.Empty;
    public long LatestFileSize { get; set; }
    public string LatestContentType { get; set; } = string.Empty;
}

public class DocumentDetailDto : DocumentSummaryDto
{
    public List<DocumentVersionDto> Versions { get; set; } = new();
}

public class DocumentVersionDto
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int VersionNumber { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string Sha256Checksum { get; set; } = string.Empty;
    public string UploadedAt { get; set; } = string.Empty;
    public Guid UploadedBy { get; set; }
}

public class DocumentTypeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool RequiresExpiryDate { get; set; }
    public string AllowedMimeTypes { get; set; } = string.Empty;
    public long MaxSizeBytes { get; set; }
}
