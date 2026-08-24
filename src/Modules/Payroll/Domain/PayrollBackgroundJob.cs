using System;

namespace Workforce.Modules.Payroll.Domain;

public enum PayrollJobStatus
{
    Queued = 1,
    Running = 2,
    Completed = 3,
    CompletedWithWarnings = 4,
    Failed = 5
}

public class PayrollBackgroundJob
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid PayrollRunId { get; private set; }
    public string IdempotencyKey { get; private set; }
    public string Operation { get; private set; }
    public PayrollJobStatus Status { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? DiagnosticMetadata { get; private set; }
    public uint RowVersion { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private PayrollBackgroundJob()
    {
        IdempotencyKey = string.Empty;
        Operation = string.Empty;
    }

    public PayrollBackgroundJob(
        Guid id,
        Guid tenantId,
        Guid payrollRunId,
        string idempotencyKey,
        string operation)
    {
        if (id == Guid.Empty) throw new ArgumentException("Job ID cannot be empty.", nameof(id));
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
        if (payrollRunId == Guid.Empty) throw new ArgumentException("Payroll Run ID cannot be empty.", nameof(payrollRunId));
        if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));
        if (string.IsNullOrWhiteSpace(operation)) throw new ArgumentException("Operation is required.", nameof(operation));

        Id = id;
        TenantId = tenantId;
        PayrollRunId = payrollRunId;
        IdempotencyKey = idempotencyKey.Trim();
        Operation = operation.Trim();
        Status = PayrollJobStatus.Queued;
        CreatedAtUtc = DateTime.UtcNow;
        StartedAtUtc = DateTime.UtcNow;
        RowVersion = 1;
    }

    public static PayrollBackgroundJob Reconstitute(
        Guid id,
        Guid tenantId,
        Guid payrollRunId,
        string idempotencyKey,
        string operation,
        PayrollJobStatus status,
        DateTime startedAtUtc,
        DateTime? completedAtUtc,
        string? errorMessage,
        string? diagnosticMetadata,
        uint rowVersion,
        DateTime createdAtUtc)
    {
        return new PayrollBackgroundJob
        {
            Id = id,
            TenantId = tenantId,
            PayrollRunId = payrollRunId,
            IdempotencyKey = idempotencyKey,
            Operation = operation,
            Status = status,
            StartedAtUtc = startedAtUtc,
            CompletedAtUtc = completedAtUtc,
            ErrorMessage = errorMessage,
            DiagnosticMetadata = diagnosticMetadata,
            RowVersion = rowVersion,
            CreatedAtUtc = createdAtUtc
        };
    }

    public void MarkRunning(uint expectedRowVersion)
    {
        if (RowVersion != expectedRowVersion)
        {
            throw new InvalidOperationException($"Concurrency violation: job has been modified. Expected version {expectedRowVersion}, current version {RowVersion}.");
        }

        if (Status != PayrollJobStatus.Queued)
        {
            throw new InvalidOperationException($"Cannot start job in status '{Status}'. Only 'Queued' jobs can be started.");
        }

        Status = PayrollJobStatus.Running;
        StartedAtUtc = DateTime.UtcNow;
        RowVersion++;
    }

    public void MarkCompleted(bool hasWarnings, string? diagnosticMetadata, uint expectedRowVersion)
    {
        if (RowVersion != expectedRowVersion)
        {
            throw new InvalidOperationException($"Concurrency violation: job has been modified. Expected version {expectedRowVersion}, current version {RowVersion}.");
        }

        if (Status != PayrollJobStatus.Running)
        {
            throw new InvalidOperationException($"Cannot complete job in status '{Status}'. Only 'Running' jobs can be completed.");
        }

        Status = hasWarnings ? PayrollJobStatus.CompletedWithWarnings : PayrollJobStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
        DiagnosticMetadata = diagnosticMetadata;
        RowVersion++;
    }

    public void MarkFailed(string errorMessage, string? diagnosticMetadata, uint expectedRowVersion)
    {
        if (RowVersion != expectedRowVersion)
        {
            throw new InvalidOperationException($"Concurrency violation: job has been modified. Expected version {expectedRowVersion}, current version {RowVersion}.");
        }

        Status = PayrollJobStatus.Failed;
        CompletedAtUtc = DateTime.UtcNow;
        ErrorMessage = errorMessage;
        DiagnosticMetadata = diagnosticMetadata;
        RowVersion++;
    }
}
