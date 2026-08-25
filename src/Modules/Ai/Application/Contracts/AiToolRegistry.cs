using System;
using System.Collections.Generic;
using System.Linq;
using Workforce.Modules.Ai.Domain;

namespace Workforce.Modules.Ai.Application.Contracts;

/// <summary>
/// Central registry of all allowlisted, strictly read-only AI tools.
/// </summary>
public sealed class AiToolRegistry
{
    private readonly Dictionary<string, IAiToolHandler> _handlers = new(StringComparer.OrdinalIgnoreCase);

    public void RegisterTool(IAiToolHandler handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        _handlers[handler.Definition.ToolCode] = handler;
    }

    public IAiToolHandler? GetHandler(string toolCode)
    {
        if (string.IsNullOrWhiteSpace(toolCode)) return null;
        _handlers.TryGetValue(toolCode, out var handler);
        return handler;
    }

    public IReadOnlyList<AiToolDefinition> GetAllDefinitions()
    {
        return _handlers.Values.Select(h => h.Definition).ToList();
    }

    public IReadOnlyList<AiToolDefinition> GetAuthorizedDefinitions(IReadOnlySet<string> userPermissions)
    {
        if (userPermissions == null) return Array.Empty<AiToolDefinition>();
        
        bool isSuperAdmin = userPermissions.Contains("*") || userPermissions.Contains("admin");

        return _handlers.Values
            .Where(h => isSuperAdmin || userPermissions.Contains(h.Definition.RequiredPermission))
            .Select(h => h.Definition)
            .ToList();
    }
}
