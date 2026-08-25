using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Workforce.Modules.Ai.Application.Contracts;
using Workforce.Modules.Ai.Domain;
using Workforce.Modules.Reporting.Infrastructure;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Ai.Application.Tools;

public sealed class ReportingRunGovernedReportToolHandler : IAiToolHandler
{
    private readonly IReportingRepository _reportingRepository;

    public AiToolDefinition Definition { get; } = new(
        toolCode: "reports.run_governed_report",
        descriptionEn: "Execute an allowlisted enterprise read-model report (HEADCOUNT_SUMMARY, ATTENDANCE_MONTHLY, LEAVE_UTILIZATION, PAYROLL_RECONCILIATION, RECRUITMENT_FUNNEL, AUDIT_SECURITY_EVENTS). Strictly governed: zero natural-language-to-SQL generation.",
        descriptionAr: "تنفيذ تقرير تشغيلي معتمد (بدون توليد استعلامات SQL حرة).",
        requiredPermission: "reports.read",
        dataClassification: "Internal",
        inputSchemaJson: "{\"type\":\"object\",\"required\":[\"reportCode\"],\"properties\":{\"reportCode\":{\"type\":\"string\"},\"filters\":{\"type\":\"object\"},\"limit\":{\"type\":\"integer\"}}}"
    );

    public ReportingRunGovernedReportToolHandler(IReportingRepository reportingRepository)
    {
        _reportingRepository = reportingRepository;
    }

    public async Task<AiToolResult> ExecuteAsync(JsonElement inputParams, IUserContext userContext, CancellationToken ct = default)
    {
        if (!inputParams.TryGetProperty("reportCode", out var codeProp) || string.IsNullOrWhiteSpace(codeProp.GetString()))
        {
            return new AiToolResult(false, "{}", AiSourceCategory.CompanyData, new(), "Missing required reportCode parameter.");
        }

        var reportCode = codeProp.GetString()!.ToUpperInvariant();
        var filters = new Dictionary<string, string>();

        if (inputParams.TryGetProperty("filters", out var fObj) && fObj.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in fObj.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.String)
                {
                    filters[prop.Name] = prop.Value.GetString()!;
                }
                else
                {
                    filters[prop.Name] = prop.Value.ToString();
                }
            }
        }

        var limit = inputParams.TryGetProperty("limit", out var l) && l.TryGetInt32(out var lim) ? Math.Min(lim, 20) : 10;

        try
        {
            var data = await _reportingRepository.ExecuteReportAsync(
                userContext.TenantId,
                userContext.LegalEntityId,
                reportCode,
                filters,
                page: 1,
                pageSize: limit,
                ct: ct
            );

            var sourceRefs = new List<SourceReference>
            {
                new SourceReference(
                    Guid.NewGuid(),
                    Guid.Empty,
                    AiSourceCategory.CompanyData,
                    $"Governed Report: {reportCode} (Total: {data.TotalCount})",
                    entityType: "ReportDefinition",
                    entityId: reportCode,
                    metadataJson: JsonSerializer.Serialize(new { reportCode, totalCount = data.TotalCount, rowCount = data.Rows.Count })
                )
            };

            var outputObj = new
            {
                ReportCode = reportCode,
                Columns = data.Columns,
                Rows = data.Rows,
                TotalCount = data.TotalCount
            };

            return new AiToolResult(
                IsSuccess: true,
                OutputJson: JsonSerializer.Serialize(outputObj),
                SourceCategory: AiSourceCategory.CompanyData,
                SourceReferences: sourceRefs
            );
        }
        catch (Exception ex)
        {
            return new AiToolResult(false, "{}", AiSourceCategory.CompanyData, new(), $"Governed report execution failed: {ex.Message}");
        }
    }
}
