namespace Workforce.Modules.Documents.Domain;

public class DocumentType
{
    public Guid Id { get; private set; }
    public string Code { get; private set; }
    public string NameEn { get; private set; }
    public string NameAr { get; private set; }
    public bool IsRequired { get; private set; }
    public bool RequiresExpiryDate { get; private set; }
    public string AllowedMimeTypes { get; private set; }
    public long MaxSizeBytes { get; private set; }

    private DocumentType()
    {
        Code = string.Empty;
        NameEn = string.Empty;
        NameAr = string.Empty;
        AllowedMimeTypes = string.Empty;
    }

    public DocumentType(
        Guid id,
        string code,
        string nameEn,
        string nameAr,
        bool isRequired = false,
        bool requiresExpiryDate = false,
        string allowedMimeTypes = "application/pdf,image/png,image/jpeg",
        long maxSizeBytes = 10485760) // 10MB default
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code cannot be empty.", nameof(code));
        if (string.IsNullOrWhiteSpace(nameEn)) throw new ArgumentException("English name cannot be empty.", nameof(nameEn));
        if (string.IsNullOrWhiteSpace(nameAr)) throw new ArgumentException("Arabic name cannot be empty.", nameof(nameAr));

        Id = id;
        Code = code.Trim().ToUpperInvariant();
        NameEn = nameEn.Trim();
        NameAr = nameAr.Trim();
        IsRequired = isRequired;
        RequiresExpiryDate = requiresExpiryDate;
        AllowedMimeTypes = allowedMimeTypes.Trim();
        MaxSizeBytes = maxSizeBytes;
    }
}
