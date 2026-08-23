using System.Text.Json.Serialization;

namespace Workforce.SharedKernel.Jobs;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProgressKind
{
    Indeterminate,
    Determinate
}

public record JobProgress(
    ProgressKind Kind,
    long? Current = null,
    long? Total = null,
    string? MessageKey = null,
    string? Unit = null);
