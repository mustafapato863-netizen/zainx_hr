using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Workforce.Modules.Ai.Domain;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Ai.Application.Contracts;

public record AiActionDefinition(
    string ActionCode,
    string Description,
    string TargetModule,
    string RequiredPermission,
    string InputSchemaJson,
    string Sensitivity,
    bool RequiresConfirmation = true,
    string EffectiveDatePolicy = "RequiredForTemporal",
    string ConcurrencyPolicy = "StrictOptimisticLock",
    string IdempotencyPolicy = "ExactMatchReplay"
);

public record AiActionExecutionResult(
    bool Success,
    string Status,
    string ResultPayloadJson,
    string? ErrorMessage,
    bool IsConcurrencyConflict
);

public interface IAiActionHandler
{
    string ActionCode { get; }
    AiActionDefinition Definition { get; }
    Task<AiActionExecutionResult> ExecuteActionAsync(AiActionProposal proposal, IUserContext userContext, CancellationToken ct = default);
}

public class AiActionRegistry
{
    private readonly ConcurrentDictionary<string, IAiActionHandler> _actions = new(StringComparer.OrdinalIgnoreCase);

    public void RegisterAction(IAiActionHandler handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        _actions[handler.ActionCode] = handler;
    }

    public IAiActionHandler? GetActionHandler(string actionCode)
    {
        if (string.IsNullOrWhiteSpace(actionCode)) return null;
        _actions.TryGetValue(actionCode.Trim(), out var handler);
        return handler;
    }

    public bool HasAction(string actionCode)
    {
        if (string.IsNullOrWhiteSpace(actionCode)) return false;
        return _actions.ContainsKey(actionCode.Trim());
    }

    public IReadOnlyList<AiActionDefinition> GetAuthorizedActionDefinitions(IEnumerable<string> userPermissions)
    {
        var perms = userPermissions as IReadOnlyCollection<string> ?? userPermissions.ToList();
        var hasAdmin = perms.Any(p => string.Equals(p, "admin", StringComparison.OrdinalIgnoreCase));
        return _actions.Values
            .Where(a => hasAdmin || perms.Any(p => string.Equals(p, a.Definition.RequiredPermission, StringComparison.OrdinalIgnoreCase)))
            .Select(a => a.Definition)
            .ToList();
    }
}
