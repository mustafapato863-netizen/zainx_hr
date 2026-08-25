using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Workforce.Modules.Ai.Domain;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Ai.Application.Contracts;

public record AiToolResult(
    bool IsSuccess,
    string OutputJson,
    AiSourceCategory SourceCategory,
    List<SourceReference> SourceReferences,
    string? ErrorMessage = null
);

/// <summary>
/// Contract implemented by all allowlisted, read-only AI tools.
/// </summary>
public interface IAiToolHandler
{
    AiToolDefinition Definition { get; }
    Task<AiToolResult> ExecuteAsync(
        JsonElement inputParams, 
        IUserContext userContext, 
        CancellationToken ct = default);
}
