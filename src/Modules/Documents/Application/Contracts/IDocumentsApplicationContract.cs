using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Workforce.Modules.Documents.Application.Contracts;

public interface IDocumentsApplicationContract
{
    Task<Guid> UploadCandidateResumeAsync(
        string tenantId,
        string legalEntityId,
        Guid candidateId,
        string fileName,
        string contentType,
        Stream contentStream,
        Guid uploadedByUserId,
        CancellationToken ct = default);

    Task<(Stream ContentStream, string ContentType, string FileName)?> DownloadDocumentAsync(
        string tenantId,
        string? legalEntityId,
        Guid documentId,
        int? versionNumber = null,
        CancellationToken ct = default);
}
