using System;
using System.Threading;
using System.Threading.Tasks;
using Workforce.Modules.Attendance.Application.Contracts;
using Workforce.Modules.Attendance.Domain;
using Workforce.Modules.Attendance.Infrastructure;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Attendance.Application.Services;

public sealed class AttendanceSelfServiceService : IAttendanceSelfServiceContract
{
    private readonly IAttendanceRepository _repository;

    public AttendanceSelfServiceService(IAttendanceRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public Task<AttendanceDayDto?> GetTodayAsync(
        TenantId tenantId,
        LegalEntityId legalEntityId,
        Guid employmentId,
        DateOnly businessDate,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return _repository.GetAttendanceDayForEmploymentAsync(tenantId, legalEntityId, employmentId, businessDate);
    }

    public async Task<SelfServiceClockResult> RecordClockAsync(
        TenantId tenantId,
        LegalEntityId legalEntityId,
        UserId actorUserId,
        RecordSelfServiceClockCommand command,
        CancellationToken ct = default)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        if (command.EmploymentId == Guid.Empty) throw new ArgumentException("EmploymentId is required.", nameof(command));

        var capturedAtUtc = command.CapturedAtUtc ?? DateTime.UtcNow;
        var clockEvent = new ClockEvent(
            Guid.NewGuid(),
            tenantId,
            command.EmploymentId,
            command.Type,
            command.Source,
            capturedAtUtc,
            DateTime.UtcNow,
            command.SourceDeviceId,
            Guid.NewGuid().ToString(),
            actorUserId.Value,
            command.Latitude,
            command.Longitude);

        await _repository.RecordClockEventAsync(clockEvent);

        var businessDate = DateOnly.FromDateTime(capturedAtUtc);
        var day = await _repository.GetOrCreateAttendanceDayAsync(
            tenantId,
            legalEntityId,
            command.EmploymentId,
            businessDate,
            "UTC");

        if (day == null)
        {
            throw new InvalidOperationException("The attendance day could not be initialized.");
        }

        var dayStartUtc = businessDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEndUtc = businessDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        var events = await _repository.GetClockEventsAsync(tenantId, command.EmploymentId, dayStartUtc, dayEndUtc);
        day.Evaluate(events, null);
        await _repository.SaveAttendanceDayAsync(day);

        var persisted = await _repository.GetAttendanceDayForEmploymentAsync(
            tenantId,
            legalEntityId,
            command.EmploymentId,
            businessDate);

        if (persisted == null)
        {
            throw new InvalidOperationException("The attendance day was not persisted.");
        }

        return new SelfServiceClockResult(
            clockEvent.Id,
            persisted.Id,
            "Recorded",
            persisted.RowVersion);
    }
}
