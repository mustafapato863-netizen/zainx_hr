using System;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Compliance.Domain;

public class ComplianceValidation
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public Guid PayrollRunId { get; private set; }
    public Guid EmploymentId { get; private set; }
    public Guid RuleVersionId { get; private set; }
    public bool IsPassed { get; private set; }
    public string Severity { get; private set; }
    public string Message { get; private set; }
    public DateTime EvaluatedAtUtc { get; private set; }

    private ComplianceValidation()
    {
        Severity = "INFO";
        Message = string.Empty;
    }

    public ComplianceValidation(
        Guid id,
        TenantId tenantId,
        Guid payrollRunId,
        Guid employmentId,
        Guid ruleVersionId,
        bool isPassed,
        string severity,
        string message)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (payrollRunId == Guid.Empty) throw new ArgumentException("PayrollRunId cannot be empty.", nameof(payrollRunId));
        if (employmentId == Guid.Empty) throw new ArgumentException("EmploymentId cannot be empty.", nameof(employmentId));
        if (ruleVersionId == Guid.Empty) throw new ArgumentException("RuleVersionId cannot be empty.", nameof(ruleVersionId));

        Id = id;
        TenantId = tenantId;
        PayrollRunId = payrollRunId;
        EmploymentId = employmentId;
        RuleVersionId = ruleVersionId;
        IsPassed = isPassed;
        Severity = string.IsNullOrWhiteSpace(severity) ? "INFO" : severity.Trim().ToUpperInvariant();
        Message = message?.Trim() ?? string.Empty;
        EvaluatedAtUtc = DateTime.UtcNow;
    }
}
