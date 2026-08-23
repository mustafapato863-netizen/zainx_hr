using System;
using System.Text.Json;
using Workforce.SharedKernel.Primitives;

namespace Workforce.SharedKernel.Audit;

public record AuditRecord(
    Guid Id,
    UserId ActorId,
    TenantId TenantId,
    LegalEntityId? LegalEntityId,
    string Action,
    string EntityName,
    string EntityId,
    DateTimeOffset Timestamp,
    string CorrelationId,
    JsonDocument? BeforeState = null,
    JsonDocument? AfterState = null);
