using System;
using System.Threading;
using System.Threading.Tasks;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Leave.Application.Contracts;

public record CancelLeaveRequestCommand(
    Guid LeaveRequestId,
    uint ExpectedRowVersion,
    LegalEntityId? LegalEntityId
);

public record LeaveActionResult(
    bool Success,
    Guid LeaveRequestId,
    uint NewRowVersion,
    string Message,
    bool IsConcurrencyConflict
);

public interface ILeaveActionContract
{
    Task<LeaveActionResult> CancelLeaveRequestAsync(
        TenantId tenantId,
        UserId actorUserId,
        CancelLeaveRequestCommand command,
        CancellationToken ct = default);
}
