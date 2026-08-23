using System;
using System.Collections.Generic;

namespace Workforce.SharedKernel.Jobs;

public record JobInfo(
    string JobId,
    string Operation,
    string? EntityId,
    JobStatus Status,
    JobProgress Progress,
    DateTimeOffset StartedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<string> Warnings,
    string? Error,
    string CorrelationId);
