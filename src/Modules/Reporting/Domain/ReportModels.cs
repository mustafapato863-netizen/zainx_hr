using System;
using System.Collections.Generic;
using System.Text.Json;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Reporting.Domain;

public enum ReportExecutionMode
{
    SynchronousOnly = 1,
    AsynchronousOnly = 2,
    Hybrid = 3
}

public enum ReportJobStatus
{
    Queued = 1,
    Running = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5
}

public class ReportDefinition
{
    public string ReportCode { get; private set; }
    public string NameEn { get; private set; }
    public string NameAr { get; private set; }
    public string Domain { get; private set; }
    public string DescriptionEn { get; private set; }
    public string DescriptionAr { get; private set; }
    public string AllowedFiltersJson { get; private set; }
    public string AllowedColumnsJson { get; private set; }
    public string RequiredPermissionsJson { get; private set; }
    public string DataClassification { get; private set; }
    public string SupportedFormatsJson { get; private set; }
    public ReportExecutionMode ExecutionMode { get; private set; }
    public int Version { get; private set; }

    private ReportDefinition()
    {
        ReportCode = string.Empty;
        NameEn = string.Empty;
        NameAr = string.Empty;
        Domain = string.Empty;
        DescriptionEn = string.Empty;
        DescriptionAr = string.Empty;
        AllowedFiltersJson = "[]";
        AllowedColumnsJson = "[]";
        RequiredPermissionsJson = "[]";
        DataClassification = "Internal";
        SupportedFormatsJson = "[\"CSV\", \"JSON\"]";
    }

    public ReportDefinition(
        string reportCode,
        string nameEn,
        string nameAr,
        string domain,
        string descriptionEn,
        string descriptionAr,
        string allowedFiltersJson,
        string allowedColumnsJson,
        string requiredPermissionsJson,
        string dataClassification = "Internal",
        string supportedFormatsJson = "[\"CSV\", \"JSON\"]",
        ReportExecutionMode executionMode = ReportExecutionMode.Hybrid,
        int version = 1)
    {
        if (string.IsNullOrWhiteSpace(reportCode)) throw new ArgumentException("Report code cannot be empty.", nameof(reportCode));
        if (string.IsNullOrWhiteSpace(nameEn)) throw new ArgumentException("Report name (EN) cannot be empty.", nameof(nameEn));

        ReportCode = reportCode.Trim().ToUpperInvariant();
        NameEn = nameEn.Trim();
        NameAr = string.IsNullOrWhiteSpace(nameAr) ? nameEn.Trim() : nameAr.Trim();
        Domain = domain.Trim();
        DescriptionEn = descriptionEn?.Trim() ?? string.Empty;
        DescriptionAr = descriptionAr?.Trim() ?? string.Empty;
        AllowedFiltersJson = string.IsNullOrWhiteSpace(allowedFiltersJson) ? "[]" : allowedFiltersJson.Trim();
        AllowedColumnsJson = string.IsNullOrWhiteSpace(allowedColumnsJson) ? "[]" : allowedColumnsJson.Trim();
        RequiredPermissionsJson = string.IsNullOrWhiteSpace(requiredPermissionsJson) ? "[]" : requiredPermissionsJson.Trim();
        DataClassification = string.IsNullOrWhiteSpace(dataClassification) ? "Internal" : dataClassification.Trim();
        SupportedFormatsJson = string.IsNullOrWhiteSpace(supportedFormatsJson) ? "[\"CSV\", \"JSON\"]" : supportedFormatsJson.Trim();
        ExecutionMode = executionMode;
        Version = version;
    }

    public HashSet<string> GetRequiredPermissions()
    {
        try
        {
            return JsonSerializer.Deserialize<HashSet<string>>(RequiredPermissionsJson) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}

public class SavedReportView
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public LegalEntityId? LegalEntityId { get; private set; }
    public string ReportCode { get; private set; }
    public string ViewName { get; private set; }
    public bool IsTenantShared { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public string FiltersJson { get; private set; }
    public string ColumnsJson { get; private set; }
    public string SortJson { get; private set; }
    public string GroupingJson { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public SavedReportView(
        Guid id,
        TenantId tenantId,
        LegalEntityId? legalEntityId,
        string reportCode,
        string viewName,
        bool isTenantShared,
        Guid ownerUserId,
        string filtersJson = "{}",
        string columnsJson = "[]",
        string sortJson = "[]",
        string groupingJson = "[]")
    {
        Id = id;
        TenantId = tenantId;
        LegalEntityId = legalEntityId;
        ReportCode = reportCode.Trim().ToUpperInvariant();
        ViewName = viewName.Trim();
        IsTenantShared = isTenantShared;
        OwnerUserId = ownerUserId;
        FiltersJson = string.IsNullOrWhiteSpace(filtersJson) ? "{}" : filtersJson.Trim();
        ColumnsJson = string.IsNullOrWhiteSpace(columnsJson) ? "[]" : columnsJson.Trim();
        SortJson = string.IsNullOrWhiteSpace(sortJson) ? "[]" : sortJson.Trim();
        GroupingJson = string.IsNullOrWhiteSpace(groupingJson) ? "[]" : groupingJson.Trim();
        CreatedAtUtc = DateTime.UtcNow;
    }
}

public class ReportExecutionJob
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public LegalEntityId? LegalEntityId { get; private set; }
    public string ReportCode { get; private set; }
    public ReportJobStatus Status { get; private set; }
    public Guid RequestedByUserId { get; private set; }
    public DateTime RequestedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string FiltersJson { get; private set; }
    public string OutputFormat { get; private set; }
    public string? StorageKey { get; private set; }
    public long FileSizeBytes { get; private set; }
    public string? Sha256Checksum { get; private set; }
    public string? ErrorMessage { get; private set; }
    public long RowCount { get; private set; }
    public string? IdempotencyKey { get; private set; }

    public ReportExecutionJob(
        Guid id,
        TenantId tenantId,
        LegalEntityId? legalEntityId,
        string reportCode,
        Guid requestedByUserId,
        string filtersJson = "{}",
        string outputFormat = "CSV",
        string? idempotencyKey = null)
    {
        Id = id;
        TenantId = tenantId;
        LegalEntityId = legalEntityId;
        ReportCode = reportCode.Trim().ToUpperInvariant();
        Status = ReportJobStatus.Queued;
        RequestedByUserId = requestedByUserId;
        RequestedAtUtc = DateTime.UtcNow;
        FiltersJson = string.IsNullOrWhiteSpace(filtersJson) ? "{}" : filtersJson.Trim();
        OutputFormat = string.IsNullOrWhiteSpace(outputFormat) ? "CSV" : outputFormat.Trim().ToUpperInvariant();
        IdempotencyKey = idempotencyKey;
    }

    public void MarkRunning()
    {
        Status = ReportJobStatus.Running;
    }

    public void MarkCompleted(string storageKey, long fileSizeBytes, string sha256Checksum, long rowCount)
    {
        Status = ReportJobStatus.Completed;
        StorageKey = storageKey;
        FileSizeBytes = fileSizeBytes;
        Sha256Checksum = sha256Checksum;
        RowCount = rowCount;
        CompletedAtUtc = DateTime.UtcNow;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = ReportJobStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAtUtc = DateTime.UtcNow;
    }
}
