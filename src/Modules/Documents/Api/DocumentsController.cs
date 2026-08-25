using System.Security.Cryptography;
using System.Linq;
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDocumentTypes(CancellationToken ct)
    {
        if (!HasAnyPermission("documents.types.read", "documents.read", "documents.file.read"))
        {
            return AccessDenied("documents.types.read");
        }

        var types = await _repository.ListDocumentTypesAsync(ct);
        return Ok(types);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListDocuments(
        [FromQuery] string ownerType,
        [FromQuery] Guid ownerId,
        CancellationToken ct)
    {
        var userContext = _userContext;
        if (!HasAnyPermission("documents.read", "documents.file.read"))
        {
            return AccessDenied("documents.read");
        }

        var docs = await _repository.ListDocumentsAsync(
            userContext.TenantId,
            ownerType ?? "Employee",
            ownerId,
            userContext.LegalEntityId,
            ct);
        return Ok(docs);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDocument(Guid id, CancellationToken ct)
    {
        var userContext = _userContext;
        if (!HasAnyPermission("documents.read", "documents.file.read"))
        {
            return AccessDenied("documents.read");
        }

        var doc = await _repository.GetDocumentDetailsAsync(id, userContext.TenantId, userContext.LegalEntityId, ct);
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DownloadDocument(Guid id, [FromQuery] int? version, CancellationToken ct)
    {
        var userContext = _userContext;
        if (!HasAnyPermission("documents.download", "documents.file.read"))
        {
            return AccessDenied("documents.download");
        }

        if (!userContext.LegalEntityId.HasValue)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Legal Entity Context Required",
                Detail = "An authorized legal entity is required to download a document.",
                Instance = HttpContext.Request.Path
            });
        }

        var doc = await _repository.GetDocumentDetailsAsync(id, userContext.TenantId, userContext.LegalEntityId, ct);
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

        var storageKey = await _repository.GetStorageKeyForDownloadAsync(id, userContext.TenantId, userContext.LegalEntityId, version, ct);
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

        var targetVersion = version.HasValue
            ? doc.Versions.FirstOrDefault(v => v.VersionNumber == version.Value)
            : doc.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
        if (targetVersion == null)
        {
            await stream.DisposeAsync();
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Document Version Not Found",
                Detail = "The requested document version could not be located.",
                Instance = HttpContext.Request.Path
            });
        }

        await _repository.RecordAccessAsync(
            id,
            userContext.TenantId,
            userContext.LegalEntityId.Value,
            userContext.UserId.Value,
            "download",
            targetVersion.VersionNumber,
            ct);

        var fileName = DocumentSecurityValidator.SanitizeFileName(targetVersion.FileName);
        var contentType = string.IsNullOrEmpty(targetVersion.ContentType) ? "application/octet-stream" : targetVersion.ContentType;

        return File(stream, contentType, fileName);
    }

    [HttpPost("upload")]
    [ProducesResponseType(typeof(DocumentDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UploadDocument([FromForm] UploadDocumentFormRequest form, CancellationToken ct)
    {
        var userContext = _userContext;
        if (!HasAnyPermission("documents.upload", "documents.employee_document.manage"))
        {
            return AccessDenied("documents.upload");
        }

        if (!userContext.LegalEntityId.HasValue)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Legal Entity Context Required",
                Detail = "An authorized legal entity is required to upload a document.",
                Instance = HttpContext.Request.Path
            });
        }

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

        var documentType = await _repository.GetDocumentTypeAsync(form.DocumentTypeId, ct);
        if (documentType == null)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Unknown Document Type",
                Detail = "The selected document type is not configured in the active document policy.",
                Instance = HttpContext.Request.Path
            });
        }

        if (documentType.MaxSizeBytes > 0 && form.File.Length > documentType.MaxSizeBytes)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Document Size Exceeds Policy",
                Detail = $"The selected document type allows at most {documentType.MaxSizeBytes} bytes.",
                Instance = HttpContext.Request.Path
            });
        }

        if (documentType.RequiresExpiryDate && !DateOnly.TryParse(form.ExpiryDate, out _))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Expiry Date Required",
                Detail = "The selected document type requires an expiry date.",
                Instance = HttpContext.Request.Path
            });
        }

        if (!IsAllowedMimeType(documentType.AllowedMimeTypes, form.File.ContentType))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "MIME Type Not Allowed",
                Detail = "The uploaded MIME type is not allowed for the selected document type.",
                Instance = HttpContext.Request.Path
            });
        }

        try
        {
            DocumentSecurityValidator.ValidateFileName(form.File.FileName);
            await using var validationStream = form.File.OpenReadStream();
            await DocumentSecurityValidator.ValidateContentSignatureAsync(validationStream, form.File.FileName, ct);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Document Payload",
                Detail = ex.Message,
                Instance = HttpContext.Request.Path
            });
        }

        var legalEntity = userContext.LegalEntityId.Value;
        var docId = Guid.NewGuid();
        var versionId = Guid.NewGuid();

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

        await _repository.RecordAccessAsync(
            docId,
            userContext.TenantId,
            legalEntity,
            userContext.UserId.Value,
            "upload",
            1,
            ct);

        var detail = await _repository.GetDocumentDetailsAsync(docId, userContext.TenantId, userContext.LegalEntityId, ct);
        return CreatedAtAction(nameof(GetDocument), new { id = docId }, detail);
    }

    [HttpPost("{id:guid}/versions")]
    [ProducesResponseType(typeof(DocumentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddDocumentVersion(Guid id, [FromForm] AddVersionFormRequest form, CancellationToken ct)
    {
        var userContext = _userContext;
        if (!HasAnyPermission("documents.replace", "documents.employee_document.manage"))
        {
            return AccessDenied("documents.replace");
        }

        if (!userContext.LegalEntityId.HasValue)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Legal Entity Context Required",
                Detail = "An authorized legal entity is required to replace a document version.",
                Instance = HttpContext.Request.Path
            });
        }

        var doc = await _repository.GetDocumentDetailsAsync(id, userContext.TenantId, userContext.LegalEntityId, ct);
        if (doc == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Document Not Found",
                Detail = $"No document with ID '{id}' was found for this tenant.",
                Instance = HttpContext.Request.Path
            });
        }

        if (form.File == null || form.File.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Missing File",
                Detail = "A valid replacement file is required.",
                Instance = HttpContext.Request.Path
            });
        }

        var documentType = await _repository.GetDocumentTypeAsync(doc.DocumentTypeId, ct);
        if (documentType != null)
        {
            if (documentType.MaxSizeBytes > 0 && form.File.Length > documentType.MaxSizeBytes)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Document Size Exceeds Policy",
                    Detail = $"The selected document type allows at most {documentType.MaxSizeBytes} bytes.",
                    Instance = HttpContext.Request.Path
                });
            }

            if (!IsAllowedMimeType(documentType.AllowedMimeTypes, form.File.ContentType))
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "MIME Type Not Allowed",
                    Detail = "The uploaded MIME type is not allowed for the selected document type.",
                    Instance = HttpContext.Request.Path
                });
            }
        }

        try
        {
            DocumentSecurityValidator.ValidateFileName(form.File.FileName);
            await using var validationStream = form.File.OpenReadStream();
            await DocumentSecurityValidator.ValidateContentSignatureAsync(validationStream, form.File.FileName, ct);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Document Payload",
                Detail = ex.Message,
                Instance = HttpContext.Request.Path
            });
        }

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

        var newVer = new DocumentVersion(
            Guid.NewGuid(),
            id,
            doc.LatestVersionNumber + 1,
            storageKey,
            form.File.FileName,
            form.File.Length,
            form.File.ContentType,
            sha256,
            userContext.UserId.Value
        );

        await _repository.AddDocumentVersionAsync(id, userContext.TenantId, newVer, ct);

        await _repository.RecordAccessAsync(
            id,
            userContext.TenantId,
            userContext.LegalEntityId.Value,
            userContext.UserId.Value,
            "replace",
            newVer.VersionNumber,
            ct);

        var updatedDoc = await _repository.GetDocumentDetailsAsync(id, userContext.TenantId, userContext.LegalEntityId, ct);
        return Ok(updatedDoc);
    }

    [HttpGet("expiring")]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListExpiringDocuments([FromQuery] int days = 30, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        if (!HasAnyPermission("documents.read", "documents.file.read"))
        {
            return AccessDenied("documents.read");
        }

        if (!userContextHasLegalEntity(out var legalEntity, out var error)) return error!;

        var boundedDays = Math.Clamp(days, 0, 3650);
        return Ok(await _repository.ListExpiringDocumentsAsync(
            _userContext.TenantId,
            legalEntity,
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow).AddDays(boundedDays),
            page,
            pageSize,
            ct));
    }

    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ArchiveDocument(Guid id, CancellationToken ct)
    {
        if (!HasAnyPermission("documents.archive", "documents.employee_document.manage"))
        {
            return AccessDenied("documents.archive");
        }

        if (!userContextHasLegalEntity(out var legalEntity, out var error)) return error!;

        var archived = await _repository.ArchiveDocumentAsync(
            id,
            _userContext.TenantId,
            legalEntity,
            _userContext.UserId.Value,
            ct);
        return archived
            ? NoContent()
            : NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Active Document Not Found",
                Detail = "No active document with this ID exists in the current tenant and legal-entity scope.",
                Instance = HttpContext.Request.Path
            });
    }

    private bool userContextHasLegalEntity(out LegalEntityId legalEntity, out IActionResult? error)
    {
        if (_userContext.LegalEntityId.HasValue)
        {
            legalEntity = _userContext.LegalEntityId.Value;
            error = null;
            return true;
        }

        legalEntity = default;
        error = BadRequest(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Legal Entity Context Required",
            Detail = "Select an authorized legal entity before using document operations.",
            Instance = HttpContext.Request.Path
        });
        return false;
    }

    private static bool IsAllowedMimeType(string configuredTypes, string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType)) return false;
        var normalized = contentType.Split(';', 2)[0].Trim();
        return configuredTypes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(type => string.Equals(type, normalized, StringComparison.OrdinalIgnoreCase));
    }

    private bool HasAnyPermission(params string[] permissions)
    {
        if (_userContext.HasPermission("admin")) return true;
        foreach (var permission in permissions)
        {
            if (_userContext.HasPermission(permission)) return true;
        }

        return false;
    }

    private IActionResult AccessDenied(string permission)
    {
        return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Access Denied",
            Detail = $"The current user does not have permission '{permission}'.",
            Instance = HttpContext.Request.Path
        });
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

public class AddVersionFormRequest
{
    public IFormFile? File { get; set; }
}
