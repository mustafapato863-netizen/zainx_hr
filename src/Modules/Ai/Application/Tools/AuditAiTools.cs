using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Workforce.Modules.Ai.Application.Contracts;
using Workforce.Modules.Ai.Domain;
using Workforce.Modules.Audit.Infrastructure;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Ai.Application.Tools;

public sealed class AuditSearchScopedToolHandler : IAiToolHandler
{
    private readonly IAuditRepository _auditRepository;

    public AiToolDefinition Definition { get; } = new(
        toolCode: "audit.search_scoped",
        descriptionEn: "Search chronological security and operational audit records for the tenant. PII-sanitized: passwords, tokens, and raw IBANs are strictly omitted.",
        descriptionAr: "البحث في سجلات التدقيق والعمليات الأمنية مع تنقية البيانات الحساسة.",
        requiredPermission: "audit.read",
        dataClassification: "Restricted",
        inputSchemaJson: "{\"type\":\"object\",\"properties\":{\"actionCode\":{\"type\":\"string\"},\"entityType\":{\"type\":\"string\"},\"entityId\":{\"type\":\"string\"},\"limit\":{\"type\":\"integer\"}}}"
    );

    public AuditSearchScopedToolHandler(IAuditRepository auditRepository)
    {
        _auditRepository = auditRepository;
    }

    public async Task<AiToolResult> ExecuteAsync(JsonElement inputParams, IUserContext userContext, CancellationToken ct = default)
    {
        // Defense-in-depth: enforce restricted audit read permission inside the handler itself,
        // mirroring the service-layer authorization gate.
        bool isSuperAdmin = userContext.Permissions.Contains("*") || userContext.Permissions.Contains("admin");
        if (!isSuperAdmin && !userContext.Permissions.Contains("audit.read"))
        {
            return new AiToolResult(false, "{}", AiSourceCategory.CompanyData, new(),
                "Unauthorized: Missing 'audit.read' permission for scoped audit search.");
        }

        string? actionCode = inputParams.TryGetProperty("actionCode", out var ac) ? ac.GetString() : null;
        string? entityType = inputParams.TryGetProperty("entityType", out var et) ? et.GetString() : null;
        string? entityId = inputParams.TryGetProperty("entityId", out var ei) ? ei.GetString() : null;
        int limit = inputParams.TryGetProperty("limit", out var l) && l.TryGetInt32(out var lim) ? Math.Min(lim, 20) : 10;

        var filter = new AuditSearchFilter(
            ActionCode: actionCode,
            EntityType: entityType,
            EntityId: entityId,
            Page: 1,
            PageSize: limit
        );

        var result = await _auditRepository.SearchAsync(userContext.TenantId, filter, ct);

        var projections = new List<object>();
        var sourceRefs = new List<SourceReference>();

        foreach (var rec in result.Items)
        {
            projections.Add(new
            {
                AuditId = rec.Id,
                ActionCode = rec.ActionCode,
                EntityType = rec.EntityType,
                EntityId = rec.EntityId,
                ActorUserId = rec.ActorUserId,
                ActorType = rec.ActorType,
                OccurredAtUtc = rec.OccurredAtUtc.ToString("yyyy-MM-dd HH:mm:ss"),
                CorrelationId = rec.CorrelationId,
                DataClassification = rec.DataClassification
            });

            sourceRefs.Add(new SourceReference(
                Guid.NewGuid(),
                Guid.Empty,
                AiSourceCategory.CompanyData,
                $"Audit Log: {rec.ActionCode} on {rec.EntityType}",
                entityType: "AuditRecord",
                entityId: rec.Id.ToString()
            ));
        }

        return new AiToolResult(
            IsSuccess: true,
            OutputJson: JsonSerializer.Serialize(projections),
            SourceCategory: AiSourceCategory.CompanyData,
            SourceReferences: sourceRefs
        );
    }
}
