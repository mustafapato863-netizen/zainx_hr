using System;
using System.Threading;
using System.Threading.Tasks;
using Workforce.Modules.Leave.Application.Contracts;
using Workforce.Modules.Leave.Infrastructure;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Leave.Application.Services;

public class LeaveActionService : ILeaveActionContract
{
    private readonly ILeaveRepository _repository;

    public LeaveActionService(ILeaveRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<LeaveActionResult> CancelLeaveRequestAsync(
        TenantId tenantId,
        UserId actorUserId,
        CancelLeaveRequestCommand command,
        CancellationToken ct = default)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        try
        {
            var result = await _repository.CancelApprovedLeaveRequestAsync(
                tenantId,
                actorUserId.Value,
                command.LeaveRequestId,
                command.ExpectedRowVersion,
                command.LegalEntityId,
                ct);

            return new LeaveActionResult(
                result.Outcome is LeaveCancellationRepositoryOutcome.Applied or LeaveCancellationRepositoryOutcome.AlreadyCancelled,
                result.RequestId,
                result.NewRowVersion,
                result.Message,
                result.IsConcurrencyConflict);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("concurrency", StringComparison.OrdinalIgnoreCase))
        {
            return new LeaveActionResult(
                false,
                command.LeaveRequestId,
                command.ExpectedRowVersion,
                "Concurrency conflict: leave request was updated by another process.",
                true);
        }
        catch (Exception ex)
        {
            return new LeaveActionResult(
                false,
                command.LeaveRequestId,
                command.ExpectedRowVersion,
                ex.Message,
                false);
        }
    }
}
