namespace Workforce.Modules.Documents.Domain;

public class DocumentVersion
{
    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public int VersionNumber { get; private set; }
    public string StorageKey { get; private set; }
    public string FileName { get; private set; }
    public long FileSize { get; private set; }
    public string ContentType { get; private set; }
    public string Sha256Checksum { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public Guid UploadedBy { get; private set; }

    private DocumentVersion()
    {
        StorageKey = string.Empty;
        FileName = string.Empty;
        ContentType = string.Empty;
        Sha256Checksum = string.Empty;
    }

    public DocumentVersion(
        Guid id,
        Guid documentId,
        int versionNumber,
        string storageKey,
        string fileName,
        long fileSize,
        string contentType,
        string sha256Checksum,
        Guid uploadedBy)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (documentId == Guid.Empty) throw new ArgumentException("DocumentId cannot be empty.", nameof(documentId));
        if (string.IsNullOrWhiteSpace(storageKey)) throw new ArgumentException("StorageKey is required.", nameof(storageKey));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("FileName is required.", nameof(fileName));
        if (fileSize <= 0) throw new ArgumentException("FileSize must be greater than zero.", nameof(fileSize));

        Id = id;
        DocumentId = documentId;
        VersionNumber = versionNumber;
        StorageKey = storageKey;
        FileName = fileName.Trim();
        FileSize = fileSize;
        ContentType = contentType?.Trim().ToLowerInvariant() ?? "application/octet-stream";
        Sha256Checksum = sha256Checksum?.Trim() ?? string.Empty;
        UploadedAt = DateTime.UtcNow;
        UploadedBy = uploadedBy;
    }
}
