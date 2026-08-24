using System;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Attendance.Domain;

public class AttendanceException
{
    public Guid Id { get; private set; }
    public Guid AttendanceDayId { get; private set; }
    public TenantId TenantId { get; private set; }
    public Guid EmploymentId { get; private set; }
    public AttendanceExceptionType Type { get; private set; }
    public AttendanceExceptionStatus Status { get; private set; }
    public string Details { get; private set; }
    public string? ResolutionNotes { get; private set; }
    public Guid? ResolvedByUserId { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private AttendanceException()
    {
        Details = string.Empty;
    }

    public AttendanceException(
        Guid id,
        Guid attendanceDayId,
        TenantId tenantId,
        Guid employmentId,
        AttendanceExceptionType type,
        string details)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (attendanceDayId == Guid.Empty) throw new ArgumentException("AttendanceDayId cannot be empty.", nameof(attendanceDayId));

        Id = id;
        AttendanceDayId = attendanceDayId;
        TenantId = tenantId;
        EmploymentId = employmentId;
        Type = type;
        Status = AttendanceExceptionStatus.Open;
        Details = details.Trim();
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Resolve(string notes, Guid resolvedByUserId)
    {
        if (string.IsNullOrWhiteSpace(notes)) throw new ArgumentException("Resolution notes are required.", nameof(notes));
        Status = AttendanceExceptionStatus.Resolved;
        ResolutionNotes = notes.Trim();
        ResolvedByUserId = resolvedByUserId;
        ResolvedAtUtc = DateTime.UtcNow;
    }

    public void Waive(string reason, Guid waivedByUserId)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Waive reason is required.", nameof(reason));
        Status = AttendanceExceptionStatus.Waived;
        ResolutionNotes = reason.Trim();
        ResolvedByUserId = waivedByUserId;
        ResolvedAtUtc = DateTime.UtcNow;
    }
}
