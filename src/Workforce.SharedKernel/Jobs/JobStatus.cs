using System.Text.Json.Serialization;

namespace Workforce.SharedKernel.Jobs;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum JobStatus
{
    Queued,
    Running,
    Completed,
    CompletedWithWarnings,
    Failed,
    Cancelled
}
