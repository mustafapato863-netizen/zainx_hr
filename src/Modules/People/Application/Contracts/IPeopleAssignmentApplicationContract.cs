using System;
using System.Threading;
using System.Threading.Tasks;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.People.Application.Contracts;

public record ChangeAssignmentLocationCommand(
    Guid EmploymentId,
    Guid? LocationId,
    string LocationNameEn,
    DateOnly EffectiveFrom,
    uint ExpectedRowVersion,
    LegalEntityId? LegalEntityId
);

public record ChangeAssignmentManagerCommand(
    Guid EmploymentId,
    Guid? ManagerEmploymentId,
    string? ManagerNameEn,
    DateOnly EffectiveFrom,
    uint ExpectedRowVersion,
    LegalEntityId? LegalEntityId
);

public record AssignmentActionResult(
    bool Success,
    Guid EmploymentId,
    Guid AssignmentId,
    uint NewRowVersion,
    string Message,
    bool IsConcurrencyConflict
);

public interface IPeopleAssignmentApplicationContract
{
    Task<AssignmentActionResult> ChangeLocationAsync(
        TenantId tenantId,
        ChangeAssignmentLocationCommand command,
        CancellationToken ct = default);

    Task<AssignmentActionResult> ChangeManagerAsync(
        TenantId tenantId,
        ChangeAssignmentManagerCommand command,
        CancellationToken ct = default);
}
