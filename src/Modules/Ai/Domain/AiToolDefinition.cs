using System.Text.Json;

namespace Workforce.Modules.Ai.Domain;

/// <summary>
/// Immutable metadata definition for an allowlisted, read-only AI tool.
/// </summary>
public sealed class AiToolDefinition
{
    public string ToolCode { get; }
    public string DescriptionEn { get; }
    public string DescriptionAr { get; }
    public string RequiredPermission { get; }
    public string DataClassification { get; }
    public string InputSchemaJson { get; }
    public string OutputSchemaJson { get; }
    public int TimeoutSeconds { get; }
    public int MaxResultSizeChars { get; }
    public bool IsReadOnly { get; }

    public AiToolDefinition(
        string toolCode,
        string descriptionEn,
        string descriptionAr,
        string requiredPermission,
        string dataClassification = "Internal",
        string? inputSchemaJson = null,
        string? outputSchemaJson = null,
        int timeoutSeconds = 15,
        int maxResultSizeChars = 10000)
    {
        if (string.IsNullOrWhiteSpace(toolCode))
            throw new ArgumentException("ToolCode cannot be empty.", nameof(toolCode));

        ToolCode = toolCode;
        DescriptionEn = descriptionEn;
        DescriptionAr = descriptionAr;
        RequiredPermission = requiredPermission;
        DataClassification = dataClassification;
        InputSchemaJson = inputSchemaJson ?? "{}";
        OutputSchemaJson = outputSchemaJson ?? "{}";
        TimeoutSeconds = timeoutSeconds;
        MaxResultSizeChars = maxResultSizeChars;
        IsReadOnly = true; // Invariant: All Phase 7A tools are strictly read-only
    }
}
