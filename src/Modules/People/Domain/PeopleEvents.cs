using System;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.People.Domain;

public record EmployeeCreatedEvent(
    Guid EventId,
    Guid EmploymentId,
    TenantId TenantId,
    LegalEntityId LegalEntityId,
    string EmployeeNumber,
    string FullNameEn,
    string FullNameAr,
    DateTime OccurredAt
);

public record EmploymentStatusChangedEvent(
    Guid EventId,
    Guid EmploymentId,
    TenantId TenantId,
    string OldStatus,
    string NewStatus,
    string? Reason,
    DateTime OccurredAt
);

public record EmployeeAssignmentChangedEvent(
    Guid EventId,
    Guid EmploymentId,
    TenantId TenantId,
    Guid NewAssignmentId,
    Guid OrganizationUnitId,
    string JobTitleEn,
    DateOnly EffectiveFrom,
    DateTime OccurredAt
);
