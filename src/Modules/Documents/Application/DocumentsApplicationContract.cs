using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Workforce.Modules.Documents.Application.Contracts;
using Workforce.Modules.Documents.Domain;
using Workforce.Modules.Documents.Infrastructure;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Documents.Application;

public class DocumentsApplicationContract : IDocumentsApplicationContract
{
    private static readonly Guid ResumeDocTypeId = Guid.Parse("a6666666-6666-6666-6666-666666666666");
    private readonly DocumentsRepository _repository;
    private readonly IStorageProvider _storageProvider;
    private readonly IMalwareScanner _malwareScanner;

    public DocumentsApplicationContract(
        DocumentsRepository repository,
        IStorageProvider storageProvider,
        IMalwareScanner? malwareScanner = null)
    {
        _repository = repository;
        _storageProvider = storageProvider;
        _malwareScanner = malwareScanner ?? new PassThroughMalwareScanner();
    }

    public async Task<Guid> UploadCandidateResumeAsync(
        string tenantId,
        string legalEntityId,
        Guid candidateId,
        string fileName,
        string contentType,
        Stream contentStream,
        Guid uploadedByUserId,
        CancellationToken ct = default)
    {
        // 1. Validate File Name
        DocumentSecurityValidator.ValidateFileName(fileName);

        // 2. Validate Content Magic Bytes / Signature
        await DocumentSecurityValidator.ValidateContentSignatureAsync(contentStream, fileName, ct);

        // 3. Malware Scan
        var scanResult = await _malwareScanner.ScanAsync(contentStream, fileName, ct);
        if (!scanResult.IsClean)
        {
            throw new InvalidOperationException($"Malware scan failed: {scanResult.ThreatName ?? "Unsafe file detected"}");
        }

        // 4. Calculate SHA256 checksum and ensure position reset
        contentStream.Position = 0;
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(contentStream, ct);
        var checksum = Convert.ToHexString(hashBytes);
        contentStream.Position = 0;

        // 5. Store File via Storage Provider
        var sanitizedFileName = DocumentSecurityValidator.SanitizeFileName(fileName);
        var storageKey = await _storageProvider.SaveAsync(contentStream, tenantId, sanitizedFileName, ct);

        // 6. Persist Document Aggregate & Initial Version
        var docId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var tId = new TenantId(Guid.Parse(tenantId));
        var leId = new LegalEntityId(Guid.Parse(legalEntityId));

        var document = new Document(
            docId,
            tId,
            leId,
            "Candidate",
            candidateId,
            ResumeDocTypeId,
            $"Resume - {sanitizedFileName}",
            null,
            uploadedByUserId
        );

        var version = new DocumentVersion(
            versionId,
            docId,
            1,
            storageKey,
            sanitizedFileName,
            contentStream.Length,
            contentType,
            checksum,
            uploadedByUserId
        );

        await _repository.CreateDocumentWithInitialVersionAsync(document, version, ct);

        return docId;
    }

    public async Task<(Stream ContentStream, string ContentType, string FileName)?> DownloadDocumentAsync(
        string tenantId,
        string? legalEntityId,
        Guid documentId,
        int? versionNumber = null,
        CancellationToken ct = default)
    {
        var tId = new TenantId(Guid.Parse(tenantId));
        LegalEntityId? leId = !string.IsNullOrEmpty(legalEntityId) ? new LegalEntityId(Guid.Parse(legalEntityId)) : null;

        var detail = await _repository.GetDocumentDetailsAsync(documentId, tId, leId, ct);
        if (detail == null)
        {
            return null; // Tenant isolation denied or not found
        }

        var storageKey = await _repository.GetStorageKeyForDownloadAsync(documentId, tId, leId, versionNumber, ct);
        if (string.IsNullOrEmpty(storageKey))
        {
            return null;
        }

        var stream = await _storageProvider.ReadAsync(storageKey, ct);
        if (stream == null)
        {
            return null;
        }

        var targetVer = versionNumber.HasValue 
            ? detail.Versions.Find(v => v.VersionNumber == versionNumber.Value)
            : detail.Versions.Count > 0 ? detail.Versions[^1] : null;

        var contentType = targetVer?.ContentType ?? detail.LatestContentType;
        var fileName = targetVer?.FileName ?? detail.LatestFileName;

        return (stream, contentType, fileName);
    }
}
