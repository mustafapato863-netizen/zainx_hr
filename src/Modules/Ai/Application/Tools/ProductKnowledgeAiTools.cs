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

public sealed class ProductKnowledgeSearchToolHandler : IAiToolHandler
{
    private readonly IAiRepository _aiRepository;

    public AiToolDefinition Definition { get; } = new(
        toolCode: "product.search_knowledge",
        descriptionEn: "Search curated ZainX product knowledge explaining core system behaviors, payroll finalization rules, recruitment pipelines, and approval steps.",
        descriptionAr: "البحث في دليل استخدام منصة زين إكس لشرح آلية عمل النظام وسير العمليات.",
        requiredPermission: "core.platform",
        dataClassification: "Public",
        inputSchemaJson: "{\"type\":\"object\",\"properties\":{\"query\":{\"type\":\"string\"}}}"
    );

    public ProductKnowledgeSearchToolHandler(IAiRepository aiRepository)
    {
        _aiRepository = aiRepository;
    }

    public async Task<AiToolResult> ExecuteAsync(JsonElement inputParams, IUserContext userContext, CancellationToken ct = default)
    {
        string query = inputParams.TryGetProperty("query", out var q) ? q.GetString() ?? string.Empty : string.Empty;

        var articles = await _aiRepository.SearchProductKnowledgeAsync(query, ct);

        var projections = new List<object>();
        var sourceRefs = new List<SourceReference>();

        foreach (var art in articles)
        {
            projections.Add(new
            {
                TopicCode = art.TopicCode,
                TitleEn = art.TitleEn,
                TitleAr = art.TitleAr,
                Category = art.Category,
                ContentEn = art.ContentEn,
                ContentAr = art.ContentAr
            });

            sourceRefs.Add(new SourceReference(
                Guid.NewGuid(),
                Guid.Empty,
                AiSourceCategory.ProductKnowledge,
                $"Product Guide: {art.TitleEn}",
                entityType: "ProductKnowledgeArticle",
                entityId: art.TopicCode
            ));
        }

        return new AiToolResult(
            IsSuccess: true,
            OutputJson: JsonSerializer.Serialize(projections),
            SourceCategory: AiSourceCategory.ProductKnowledge,
            SourceReferences: sourceRefs
        );
    }
}
