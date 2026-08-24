using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Workforce.Modules.Documents.Infrastructure;
using Workforce.Modules.Reporting.Domain;
using Workforce.Modules.Reporting.Infrastructure;

namespace Workforce.Modules.Reporting.Application;

public interface IReportingExportEngine
{
    Task ProcessExportJobAsync(ReportExecutionJob job, CancellationToken ct = default);
}

public class ReportingExportEngine : IReportingExportEngine
{
    private readonly IReportingRepository _repository;
    private readonly IStorageProvider _storageProvider;

    public ReportingExportEngine(IReportingRepository repository, IStorageProvider storageProvider)
    {
        _repository = repository;
        _storageProvider = storageProvider;
    }

    public async Task ProcessExportJobAsync(ReportExecutionJob job, CancellationToken ct = default)
    {
        job.MarkRunning();
        await _repository.UpdateReportJobAsync(job, ct);

        try
        {
            var filters = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(job.FiltersJson))
            {
                try
                {
                    filters = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(job.FiltersJson) ?? new();
                }
                catch { }
            }

            // Retrieve all rows for the export
            var data = await _repository.ExecuteReportAsync(job.TenantId, job.LegalEntityId, job.ReportCode, filters, 1, 50000, ct);

            using var memoryStream = new MemoryStream();
            using (var writer = new StreamWriter(memoryStream, Encoding.UTF8, leaveOpen: true))
            {
                // Write CSV Header
                await writer.WriteLineAsync(string.Join(",", data.Columns));

                // Write CSV Rows with Formula Injection Protection
                foreach (var row in data.Rows)
                {
                    var values = new List<string>();
                    foreach (var col in data.Columns)
                    {
                        var val = row.TryGetValue(col, out var obj) && obj != null ? obj.ToString() : string.Empty;
                        values.Add(SanitizeCsvField(val));
                    }
                    await writer.WriteLineAsync(string.Join(",", values));
                }
            }

            memoryStream.Position = 0;
            var fileSizeBytes = memoryStream.Length;

            // Compute SHA-256
            using var sha256 = SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(memoryStream, ct);
            var sha256Checksum = Convert.ToHexString(hashBytes).ToLowerInvariant();

            // Save to storage
            memoryStream.Position = 0;
            var fileName = $"{job.ReportCode.ToLowerInvariant()}_{job.Id:N}.csv";
            var storageKey = await _storageProvider.SaveAsync(memoryStream, job.TenantId.Value.ToString(), fileName, ct);

            job.MarkCompleted(storageKey, fileSizeBytes, sha256Checksum, data.Rows.Count);
            await _repository.UpdateReportJobAsync(job, ct);
        }
        catch (Exception ex)
        {
            job.MarkFailed(ex.Message);
            await _repository.UpdateReportJobAsync(job, ct);
        }
    }

    public static string SanitizeCsvField(string? field)
    {
        if (string.IsNullOrEmpty(field)) return "\"\"";

        var val = field;

        // Prevent CSV Formula Injection
        if (val.StartsWith('=') || val.StartsWith('+') || val.StartsWith('-') || val.StartsWith('@') || val.StartsWith('\t') || val.StartsWith('\r'))
        {
            val = "'" + val;
        }

        // Escape double quotes
        if (val.Contains('"') || val.Contains(',') || val.Contains('\n') || val.Contains('\r'))
        {
            val = "\"" + val.Replace("\"", "\"\"") + "\"";
        }

        return val;
    }
}
