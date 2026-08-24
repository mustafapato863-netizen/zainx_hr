using System;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Attendance.Domain;

public class AttendanceAdjustment
{
    public Guid Id { get; private set; }
    public Guid AttendanceDayId { get; private set; }
    public TenantId TenantId { get; private set; }
    public Guid EmploymentId { get; private set; }
    public int AdjustedWorkedMinutes { get; private set; }
    public string Reason { get; private set; }
    public Guid ActorUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public int BeforeWorkedMinutes { get; private set; }
    public int AfterWorkedMinutes { get; private set; }
    public Guid? ApprovalRequestId { get; private set; }

    private AttendanceAdjustment()
    {
        Reason = string.Empty;
    }

    public AttendanceAdjustment(
        Guid id,
        Guid attendanceDayId,
        TenantId tenantId,
        Guid employmentId,
        int adjustedWorkedMinutes,
        string reason,
        Guid actorUserId,
        int beforeWorkedMinutes,
        int afterWorkedMinutes,
        Guid? approvalRequestId = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (attendanceDayId == Guid.Empty) throw new ArgumentException("AttendanceDayId cannot be empty.", nameof(attendanceDayId));
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Adjustment reason is required.", nameof(reason));

        Id = id;
        AttendanceDayId = attendanceDayId;
        TenantId = tenantId;
        EmploymentId = employmentId;
        AdjustedWorkedMinutes = adjustedWorkedMinutes;
        Reason = reason.Trim();
        ActorUserId = actorUserId;
        CreatedAtUtc = DateTime.UtcNow;
        BeforeWorkedMinutes = beforeWorkedMinutes;
        AfterWorkedMinutes = afterWorkedMinutes;
        ApprovalRequestId = approvalRequestId;
    }
}
