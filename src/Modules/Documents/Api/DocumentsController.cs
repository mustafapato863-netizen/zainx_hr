using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Workforce.Modules.Documents.Application;
using Workforce.Modules.Documents.Domain;
using Workforce.Modules.Documents.Infrastructure;
using Workforce.SharedKernel.Primitives;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Documents.Api;

[ApiController]
[Route("api/v1/documents")]
public class DocumentsController : ControllerBase
{
    private readonly DocumentsRepository _repository;
    private readonly IStorageProvider _storageProvider;
    private readonly IUserContext _userContext;

    public DocumentsController(
        DocumentsRepository repository,
        IStorageProvider storageProvider,
        IUserContext userContext)
    {
        _repository = repository;
        _storageProvider = storageProvider;
        _userContext = userContext;
    }

    [HttpGet("types")]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentTypeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDocumentTypes(CancellationToken ct)
    {
        var types = await _repository.ListDocumentTypesAsync(ct);
        return Ok(types);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListDocuments(
        [FromQuery] string ownerType,
        [FromQuery] Guid ownerId,
        CancellationToken ct)
    {
        var userContext = _userContext;
        var docs = await _repository.ListDocumentsAsync(userContext.TenantId, ownerType ?? "Employee", ownerId, ct);
        return Ok(docs);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocument(Guid id, CancellationToken ct)
    {
        var userContext = _userContext;
        var doc = await _repository.GetDocumentDetailsAsync(id, userContext.TenantId, ct);
        if (doc == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Document Not Found",
                Detail = $"No document with ID '{id}' was found.",
                Instance = HttpContext.Request.Path
            });
        }
        return Ok(doc);
    }

    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadDocument(Guid id, [FromQuery] int? version, CancellationToken ct)
    {
        var userContext = _userContext;
        var doc = await _repository.GetDocumentDetailsAsync(id, userContext.TenantId, ct);
        if (doc == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Document Not Found",
                Detail = $"Document '{id}' not found.",
                Instance = HttpContext.Request.Path
            });
        }

        var storageKey = await _repository.GetStorageKeyForDownloadAsync(id, userContext.TenantId, version, ct);
        if (string.IsNullOrEmpty(storageKey))
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "File Not Found",
                Detail = "The requested file payload could not be located.",
                Instance = HttpContext.Request.Path
            });
        }

        var stream = await _storageProvider.ReadAsync(storageKey, ct);
        if (stream == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Storage File Missing",
                Detail = "The file payload is missing from storage.",
                Instance = HttpContext.Request.Path
            });
        }

        var fileName = doc.LatestFileName;
        var contentType = string.IsNullOrEmpty(doc.LatestContentType) ? "application/octet-stream" : doc.LatestContentType;

        return File(stream, contentType, fileName);
    }

    [HttpPost("upload")]
    [ProducesResponseType(typeof(DocumentDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadDocument([FromForm] UploadDocumentFormRequest form, CancellationToken ct)
    {
        var userContext = _userContext;
        if (form.File == null || form.File.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Missing File",
                Detail = "A valid non-empty file payload is required.",
                Instance = HttpContext.Request.Path
            });
        }

        var legalEntity = userContext.LegalEntityId ?? LegalEntityId.New();
        var docId = Guid.NewGuid();
        var versionId = Guid.NewGuid();

        // Calculate SHA-256 Checksum and save to Storage Provider
        string sha256;
        string storageKey;
        await using (var stream = form.File.OpenReadStream())
        {
            using var sha = SHA256.Create();
            var hashBytes = await sha.ComputeHashAsync(stream, ct);
            sha256 = Convert.ToHexString(hashBytes).ToLowerInvariant();

            stream.Position = 0;
            storageKey = await _storageProvider.SaveAsync(stream, userContext.TenantId.Value.ToString(), form.File.FileName, ct);
        }

        DateOnly? expiry = DateOnly.TryParse(form.ExpiryDate, out var exp) ? exp : null;

        var doc = new Document(
            docId,
            userContext.TenantId,
            legalEntity,
            form.OwnerType ?? "Employee",
            form.OwnerId,
            form.DocumentTypeId,
            form.Title,
            expiry,
            userContext.UserId.Value
        );

        var version = new DocumentVersion(
            versionId,
            docId,
            1,
            storageKey,
            form.File.FileName,
            form.File.Length,
            form.File.ContentType,
            sha256,
            userContext.UserId.Value
        );

        await _repository.CreateDocumentWithInitialVersionAsync(doc, version, ct);

        var detail = await _repository.GetDocumentDetailsAsync(docId, userContext.TenantId, ct);
        return CreatedAtAction(nameof(GetDocument), new { id = docId }, detail);
    }
}

public class UploadDocumentFormRequest
{
    public string OwnerType { get; set; } = "Employee";
    public Guid OwnerId { get; set; }
    public Guid DocumentTypeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ExpiryDate { get; set; }
    public IFormFile? File { get; set; }
}
