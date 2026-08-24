using System;

namespace Workforce.Modules.Payroll.Domain;

public class PayrollException
{
    public Guid Id { get; private set; }
    public Guid PayrollRunId { get; private set; }
    public Guid EmploymentId { get; private set; }
    public ExceptionSeverity Severity { get; private set; }
    public string Category { get; private set; }
    public string Reason { get; private set; }
    public string ResolutionGuidance { get; private set; }
    public ExceptionStatus Status { get; private set; }
    public Guid? ResolvedByUserId { get; private set; }
    public string? ResolutionNote { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private PayrollException()
    {
        Category = string.Empty;
        Reason = string.Empty;
        ResolutionGuidance = string.Empty;
    }

    public PayrollException(
        Guid id,
        Guid payrollRunId,
        Guid employmentId,
        ExceptionSeverity severity,
        string category,
        string reason,
        string resolutionGuidance,
        ExceptionStatus status = ExceptionStatus.Open)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (payrollRunId == Guid.Empty) throw new ArgumentException("PayrollRunId cannot be empty.", nameof(payrollRunId));

        Id = id;
        PayrollRunId = payrollRunId;
        EmploymentId = employmentId;
        Severity = severity;
        Category = category.Trim();
        Reason = reason.Trim();
        ResolutionGuidance = resolutionGuidance.Trim();
        Status = status;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Resolve(Guid actorUserId, string resolutionNote)
    {
        Status = ExceptionStatus.Resolved;
        ResolvedByUserId = actorUserId;
        ResolutionNote = resolutionNote?.Trim();
    }

    public void Waive(Guid actorUserId, string justification)
    {
        if (Severity == ExceptionSeverity.Blocking)
        {
            throw new InvalidOperationException("Blocking compliance/calculation exceptions cannot be waived without formal adjustment.");
        }

        Status = ExceptionStatus.Waived;
        ResolvedByUserId = actorUserId;
        ResolutionNote = justification?.Trim();
    }
}
