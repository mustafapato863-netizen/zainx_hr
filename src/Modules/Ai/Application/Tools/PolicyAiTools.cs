using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Workforce.Modules.Ai.Application.Contracts;
using Workforce.Modules.Ai.Domain;
using Workforce.Modules.Ai.Infrastructure;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Ai.Application.Tools;

public sealed class PolicySearchToolHandler : IAiToolHandler
{
    private readonly IAiRepository _aiRepository;

    public AiToolDefinition Definition { get; } = new(
        toolCode: "policy.search_company_policy",
        descriptionEn: "Search tenant-specific company policies, remote work guidelines, leave rules, and statutory benefits with strict effective-date versioning.",
        descriptionAr: "البحث في لوائح وسياسات الشركة المعتمدة مع مراعاة التواريخ السارية والإصدارات.",
        requiredPermission: "core.platform",
        dataClassification: "Internal",
        inputSchemaJson: "{\"type\":\"object\",\"properties\":{\"query\":{\"type\":\"string\"},\"targetDate\":{\"type\":\"string\"}}}"
    );

    public PolicySearchToolHandler(IAiRepository aiRepository)
    {
        _aiRepository = aiRepository;
    }

    public async Task<AiToolResult> ExecuteAsync(JsonElement inputParams, IUserContext userContext, CancellationToken ct = default)
    {
        string? query = inputParams.TryGetProperty("query", out var q) ? q.GetString() : null;
        DateTime? targetDate = null;

        if (inputParams.TryGetProperty("targetDate", out var dProp) && DateTime.TryParse(dProp.GetString(), out var d))
        {
            targetDate = DateTime.SpecifyKind(d, DateTimeKind.Utc);
        }

        var policies = await _aiRepository.SearchPoliciesAsync(userContext.TenantId, query, targetDate, ct);

        var projections = new List<object>();
        var sourceRefs = new List<SourceReference>();

        foreach (var p in policies)
        {
            projections.Add(new
            {
                PolicyId = p.Id,
                PolicyCode = p.PolicyCode,
                TitleEn = p.TitleEn,
                TitleAr = p.TitleAr,
                Version = p.Version,
                EffectiveFrom = p.EffectiveFromUtc.ToString("yyyy-MM-dd"),
                EffectiveTo = p.EffectiveToUtc?.ToString("yyyy-MM-dd") ?? "Indefinite",
                ContentEn = p.ContentEn,
                ContentAr = p.ContentAr,
                Classification = p.Classification
            });

            sourceRefs.Add(new SourceReference(
                Guid.NewGuid(),
                Guid.Empty,
                AiSourceCategory.CompanyPolicy,
                $"Policy: {p.TitleEn} (v{p.Version})",
                entityType: "CompanyPolicy",
                entityId: p.Id.ToString(),
                policyCode: p.PolicyCode,
                policyVersion: p.Version,
                metadataJson: JsonSerializer.Serialize(new { p.PolicyCode, p.Version, p.Classification })
            ));
        }

        return new AiToolResult(
            IsSuccess: true,
            OutputJson: JsonSerializer.Serialize(projections),
            SourceCategory: AiSourceCategory.CompanyPolicy,
            SourceReferences: sourceRefs
        );
    }
}
