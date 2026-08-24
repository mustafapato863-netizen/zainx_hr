using System;
using System.Collections.Generic;
using System.Linq;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Attendance.Domain;

public class AttendanceDay
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public LegalEntityId LegalEntityId { get; private set; }
    public Guid EmploymentId { get; private set; }
    public DateOnly BusinessDate { get; private set; }
    public string TimeZoneId { get; private set; }
    public AttendanceStatus Status { get; private set; }
    public DateTime? ScheduledStartUtc { get; private set; }
    public DateTime? ScheduledEndUtc { get; private set; }
    public int ScheduledMinutes { get; private set; }
    public DateTime? FirstClockInUtc { get; private set; }
    public DateTime? LastClockOutUtc { get; private set; }
    public int TotalWorkedMinutes { get; private set; }
    public int LateMinutes { get; private set; }
    public int EarlyDepartureMinutes { get; private set; }
    public bool IsAbsent { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public uint RowVersion { get; private set; }

    private readonly List<AttendanceException> _exceptions = new();
    public IReadOnlyCollection<AttendanceException> Exceptions => _exceptions.AsReadOnly();

    private readonly List<AttendanceAdjustment> _adjustments = new();
    public IReadOnlyCollection<AttendanceAdjustment> Adjustments => _adjustments.AsReadOnly();

    private AttendanceDay()
    {
        TimeZoneId = "UTC";
    }

    public AttendanceDay(
        Guid id,
        TenantId tenantId,
        LegalEntityId legalEntityId,
        Guid employmentId,
        DateOnly businessDate,
        string timeZoneId = "UTC",
        int scheduledMinutes = 480)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (employmentId == Guid.Empty) throw new ArgumentException("EmploymentId cannot be empty.", nameof(employmentId));

        Id = id;
        TenantId = tenantId;
        LegalEntityId = legalEntityId;
        EmploymentId = employmentId;
        BusinessDate = businessDate;
        TimeZoneId = string.IsNullOrWhiteSpace(timeZoneId) ? "UTC" : timeZoneId.Trim();
        ScheduledMinutes = scheduledMinutes;
        Status = AttendanceStatus.Open;
        TotalWorkedMinutes = 0;
        LateMinutes = 0;
        EarlyDepartureMinutes = 0;
        IsAbsent = false;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        RowVersion = 1;
    }

    public void Evaluate(IEnumerable<ClockEvent> events, WorkSchedule? schedule)
    {
        if (Status == AttendanceStatus.Locked)
        {
            throw new InvalidOperationException("Cannot re-evaluate a locked attendance day.");
        }

        var sortedEvents = events.OrderBy(e => e.CapturedAtUtc).ToList();
        var ins = sortedEvents.Where(e => e.Type == ClockType.In).ToList();
        var outs = sortedEvents.Where(e => e.Type == ClockType.Out).ToList();

        FirstClockInUtc = ins.FirstOrDefault()?.CapturedAtUtc;
        LastClockOutUtc = outs.LastOrDefault()?.CapturedAtUtc;

        // Clear previous open exceptions
        _exceptions.RemoveAll(e => e.Status == AttendanceExceptionStatus.Open);

        if (schedule != null)
        {
            ScheduledMinutes = schedule.GetScheduledDurationMinutes();
        }

        if (FirstClockInUtc == null && LastClockOutUtc == null)
        {
            // Employee did not clock in or out
            IsAbsent = true;
            TotalWorkedMinutes = 0;
            LateMinutes = 0;
            EarlyDepartureMinutes = 0;
            Status = AttendanceStatus.Exception;
            _exceptions.Add(new AttendanceException(
                Guid.NewGuid(), Id, TenantId, EmploymentId, AttendanceExceptionType.UnexpectedAbsence, "Employee has no recorded clock events for scheduled day."
            ));
            UpdatedAt = DateTime.UtcNow;
            return;
        }

        IsAbsent = false;

        if (FirstClockInUtc != null && LastClockOutUtc == null)
        {
            Status = AttendanceStatus.Exception;
            _exceptions.Add(new AttendanceException(
                Guid.NewGuid(), Id, TenantId, EmploymentId, AttendanceExceptionType.MissingClockOut, "Employee clocked in but has no registered clock-out event."
            ));
            TotalWorkedMinutes = 0;
        }
        else if (FirstClockInUtc == null && LastClockOutUtc != null)
        {
            Status = AttendanceStatus.Exception;
            _exceptions.Add(new AttendanceException(
                Guid.NewGuid(), Id, TenantId, EmploymentId, AttendanceExceptionType.MissingClockIn, "Employee clocked out but has no registered clock-in event."
            ));
            TotalWorkedMinutes = 0;
        }
        else if (FirstClockInUtc != null && LastClockOutUtc != null)
        {
            var rawMinutes = (int)(LastClockOutUtc.Value - FirstClockInUtc.Value).TotalMinutes;
            TotalWorkedMinutes = Math.Max(0, rawMinutes);

            if (_exceptions.Count == 0)
            {
                Status = AttendanceStatus.Reviewed;
            }
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public void ApplyAdjustment(int adjustedMinutes, string reason, Guid actorUserId, uint expectedRowVersion, Guid? approvalRequestId = null)
    {
        VerifyRowVersion(expectedRowVersion);

        if (Status == AttendanceStatus.Locked)
        {
            throw new InvalidOperationException("Cannot adjust a locked attendance record without formal unlocking.");
        }

        var before = TotalWorkedMinutes;
        var after = Math.Max(0, adjustedMinutes);

        var adj = new AttendanceAdjustment(
            Guid.NewGuid(), Id, TenantId, EmploymentId, adjustedMinutes, reason, actorUserId, before, after, approvalRequestId
        );
        _adjustments.Add(adj);

        TotalWorkedMinutes = after;
        Status = AttendanceStatus.Reviewed;
        UpdatedAt = DateTime.UtcNow;
        RowVersion++;
    }

    public void Approve(uint expectedRowVersion)
    {
        VerifyRowVersion(expectedRowVersion);
        if (_exceptions.Any(e => e.Status == AttendanceExceptionStatus.Open))
        {
            throw new InvalidOperationException("Cannot approve attendance day with unresolved exceptions.");
        }

        Status = AttendanceStatus.Approved;
        UpdatedAt = DateTime.UtcNow;
        RowVersion++;
    }

    public void Lock(uint expectedRowVersion)
    {
        VerifyRowVersion(expectedRowVersion);
        if (Status != AttendanceStatus.Approved)
        {
            throw new InvalidOperationException("Only approved attendance days can be locked.");
        }

        Status = AttendanceStatus.Locked;
        UpdatedAt = DateTime.UtcNow;
        RowVersion++;
    }

    private void VerifyRowVersion(uint expected)
    {
        if (expected != RowVersion)
        {
            throw new InvalidOperationException("Optimistic concurrency conflict: The attendance record was modified by another operation.");
        }
    }
}
