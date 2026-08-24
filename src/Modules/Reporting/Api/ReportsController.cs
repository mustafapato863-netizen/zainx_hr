using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Workforce.Modules.Documents.Infrastructure;
using Workforce.Modules.Reporting.Application;
using Workforce.Modules.Reporting.Domain;
using Workforce.Modules.Reporting.Infrastructure;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Reporting.Api;

public record RunReportRequest(
    Dictionary<string, string>? Filters = null,
    int Page = 1,
    int PageSize = 50
);

public record QueueExportRequest(
    Dictionary<string, string>? Filters = null,
    string OutputFormat = "CSV",
    string? IdempotencyKey = null
);

public record CreateSavedViewRequest(
    string ViewName,
    bool IsTenantShared,
    string FiltersJson,
    string ColumnsJson,
    string SortJson,
    string GroupingJson
);

[ApiController]
[Route("api/v1/reports")]
public class ReportsController : ControllerBase
{
    private readonly IReportingRepository _repository;
    private readonly IReportingExportEngine _exportEngine;
    private readonly IStorageProvider _storageProvider;
    private readonly IUserContext _userContext;

    public ReportsController(
        IReportingRepository repository,
        IReportingExportEngine exportEngine,
        IStorageProvider storageProvider,
        IUserContext userContext)
    {
        _repository = repository;
        _exportEngine = exportEngine;
        _storageProvider = storageProvider;
        _userContext = userContext;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ReportDefinition>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListReports(CancellationToken ct)
    {
        var reports = await _repository.ListDefinitionsAsync(ct);
        return Ok(reports);
    }

    [HttpGet("{reportCode}")]
    [ProducesResponseType(typeof(ReportDefinition), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReport(string reportCode, CancellationToken ct)
    {
        var def = await _repository.GetDefinitionAsync(reportCode, ct);
        if (def == null) return NotFound();
        return Ok(def);
    }

    [HttpPost("{reportCode}/run")]
    [ProducesResponseType(typeof(ReportExecutionData), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RunReport(string reportCode, [FromBody] RunReportRequest request, CancellationToken ct)
    {
        var def = await _repository.GetDefinitionAsync(reportCode, ct);
        if (def == null) return NotFound();

        // Check required permissions
        var requiredPerms = def.GetRequiredPermissions();
        if (requiredPerms.Count > 0 && !HasAllPermissions(requiredPerms))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Report Access Forbidden",
                Detail = $"You do not have the required permissions to run report '{def.NameEn}'."
            });
        }

        var filters = request.Filters ?? new Dictionary<string, string>();
        var result = await _repository.ExecuteReportAsync(
            _userContext.TenantId,
            _userContext.LegalEntityId,
            reportCode,
            filters,
            request.Page,
            request.PageSize,
            ct);

        return Ok(result);
    }

    [HttpPost("{reportCode}/export")]
    [ProducesResponseType(typeof(ReportExecutionJob), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> QueueExport(string reportCode, [FromBody] QueueExportRequest request, CancellationToken ct)
    {
        var def = await _repository.GetDefinitionAsync(reportCode, ct);
        if (def == null) return NotFound();

        var requiredPerms = def.GetRequiredPermissions();
        if (requiredPerms.Count > 0 && !HasAllPermissions(requiredPerms))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Export Forbidden",
                Detail = $"You do not have the required permissions to export report '{def.NameEn}'."
            });
        }

        var job = new ReportExecutionJob(
            Guid.NewGuid(),
            _userContext.TenantId,
            _userContext.LegalEntityId,
            reportCode,
            _userContext.UserId.Value,
            System.Text.Json.JsonSerializer.Serialize(request.Filters ?? new Dictionary<string, string>()),
            request.OutputFormat,
            request.IdempotencyKey
        );

        await _repository.CreateReportJobAsync(job, ct);

        // Process synchronously or let background worker pick it up
        await _exportEngine.ProcessExportJobAsync(job, ct);

        return Accepted($"/api/v1/reports/jobs/{job.Id}", job);
    }

    [HttpGet("jobs/{jobId:guid}")]
    [ProducesResponseType(typeof(ReportExecutionJob), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJobStatus(Guid jobId, CancellationToken ct)
    {
        var job = await _repository.GetReportJobAsync(_userContext.TenantId, jobId, ct);
        if (job == null) return NotFound();
        return Ok(job);
    }

    [HttpGet("jobs/{jobId:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadJobArtifact(Guid jobId, CancellationToken ct)
    {
        var job = await _repository.GetReportJobAsync(_userContext.TenantId, jobId, ct);
        if (job == null || job.Status != ReportJobStatus.Completed || string.IsNullOrWhiteSpace(job.StorageKey))
        {
            return NotFound(new ProblemDetails { Status = 404, Title = "Report Export Artifact Not Available" });
        }

        var stream = await _storageProvider.ReadAsync(job.StorageKey, ct);
        if (stream == null) return NotFound();

        Response.Headers.Append("X-ZainX-Checksum-SHA256", job.Sha256Checksum);
        var filename = $"{job.ReportCode.ToLowerInvariant()}_{job.Id:N}.{job.OutputFormat.ToLowerInvariant()}";
        return File(stream, "text/csv; charset=utf-8", filename);
    }

    [HttpGet("{reportCode}/saved-views")]
    [ProducesResponseType(typeof(IReadOnlyList<SavedReportView>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSavedViews(string reportCode, CancellationToken ct)
    {
        var views = await _repository.ListSavedViewsAsync(_userContext.TenantId, reportCode, _userContext.UserId.Value, ct);
        return Ok(views);
    }

    [HttpPost("{reportCode}/saved-views")]
    [ProducesResponseType(typeof(SavedReportView), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSavedView(string reportCode, [FromBody] CreateSavedViewRequest request, CancellationToken ct)
    {
        var view = new SavedReportView(
            Guid.NewGuid(),
            _userContext.TenantId,
            _userContext.LegalEntityId,
            reportCode,
            request.ViewName,
            request.IsTenantShared,
            _userContext.UserId.Value,
            request.FiltersJson,
            request.ColumnsJson,
            request.SortJson,
            request.GroupingJson
        );

        await _repository.SaveReportViewAsync(view, ct);
        return Ok(view);
    }

    [HttpDelete("{reportCode}/saved-views/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteSavedView(string reportCode, Guid id, CancellationToken ct)
    {
        var success = await _repository.DeleteSavedViewAsync(_userContext.TenantId, id, _userContext.UserId.Value, ct);
        return Ok(new { success });
    }

    private bool HasAllPermissions(HashSet<string> requiredPerms)
    {
        var userPerms = _userContext.Permissions != null ? new HashSet<string>(_userContext.Permissions, StringComparer.OrdinalIgnoreCase) : new HashSet<string>();
        if (userPerms.Count == 0 || userPerms.Contains("*")) return true;

        foreach (var req in requiredPerms)
        {
            if (!userPerms.Contains(req)) return false;
        }

        return true;
    }
}
