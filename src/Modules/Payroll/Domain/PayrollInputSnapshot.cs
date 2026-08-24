using System;

namespace Workforce.Modules.Payroll.Domain;

public class PayrollInputSnapshot
{
    public Guid Id { get; private set; }
    public Guid PayrollRunId { get; private set; }
    public Guid EmploymentId { get; private set; }
    public decimal BaseSalaryMonthly { get; private set; }
    public string AllowancesJson { get; private set; }
    public int ScheduledDays { get; private set; }
    public int VerifiedWorkedMinutes { get; private set; }
    public decimal ApprovedAbsenceDays { get; private set; }
    public decimal ApprovedLeaveDays { get; private set; }
    public decimal UnpaidLeaveDays { get; private set; }
    public DateTime CapturedAtUtc { get; private set; }

    private PayrollInputSnapshot()
    {
        AllowancesJson = "[]";
    }

    public PayrollInputSnapshot(
        Guid id,
        Guid payrollRunId,
        Guid employmentId,
        decimal baseSalaryMonthly,
        string allowancesJson,
        int scheduledDays,
        int verifiedWorkedMinutes,
        decimal approvedAbsenceDays,
        decimal approvedLeaveDays,
        decimal unpaidLeaveDays)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (payrollRunId == Guid.Empty) throw new ArgumentException("PayrollRunId cannot be empty.", nameof(payrollRunId));
        if (employmentId == Guid.Empty) throw new ArgumentException("EmploymentId cannot be empty.", nameof(employmentId));

        Id = id;
        PayrollRunId = payrollRunId;
        EmploymentId = employmentId;
        BaseSalaryMonthly = Math.Max(0, baseSalaryMonthly);
        AllowancesJson = string.IsNullOrWhiteSpace(allowancesJson) ? "[]" : allowancesJson.Trim();
        ScheduledDays = Math.Max(0, scheduledDays);
        VerifiedWorkedMinutes = Math.Max(0, verifiedWorkedMinutes);
        ApprovedAbsenceDays = Math.Max(0, approvedAbsenceDays);
        ApprovedLeaveDays = Math.Max(0, approvedLeaveDays);
        UnpaidLeaveDays = Math.Max(0, unpaidLeaveDays);
        CapturedAtUtc = DateTime.UtcNow;
    }

    public string ToCanonicalFingerprintString()
    {
        // Explicitly excludes volatile PKs, FKs, and timestamps (Id, PayrollRunId, CapturedAtUtc)
        // Includes ONLY the financially meaningful parameters that impact calculation.
        return $"{EmploymentId}|SAL:{BaseSalaryMonthly:F2}|SCH:{ScheduledDays}|WKM:{VerifiedWorkedMinutes}|A_ABS:{ApprovedAbsenceDays:F2}|A_LV:{ApprovedLeaveDays:F2}|U_LV:{UnpaidLeaveDays:F2}|ALW:{AllowancesJson}";
    }
}
