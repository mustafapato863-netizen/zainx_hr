using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Workforce.SharedKernel.Jobs;

namespace Workforce.Host.Api.Controllers;

[ApiController]
[Route("api/jobs")]
public class JobsController : ControllerBase
{
    // For Phase 1A, we return a mock job to fulfill the contract generation.
    // In later phases, this will read from a distributed cache or database.

    [HttpGet("{jobId}")]
    public IActionResult GetJobStatus(string jobId)
    {
        var mockJob = new JobInfo(
            JobId: jobId,
            Operation: "platform.mock.job",
            EntityId: "test_entity_999",
            Status: JobStatus.Running,
            Progress: new JobProgress(ProgressKind.Determinate, Current: 45, Total: 100, Unit: "percent"),
            StartedAt: DateTimeOffset.UtcNow.AddMinutes(-2),
            UpdatedAt: DateTimeOffset.UtcNow,
            CompletedAt: null,
            Warnings: new List<string>(),
            Error: null,
            CorrelationId: HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString()
        );

        return Ok(mockJob);
    }
}
