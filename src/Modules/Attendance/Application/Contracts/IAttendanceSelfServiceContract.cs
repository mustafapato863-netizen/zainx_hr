using System;
using System.Threading;
using System.Threading.Tasks;
using Workforce.Modules.Attendance.Infrastructure;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Attendance.Application.Contracts;

public record RecordSelfServiceClockCommand(
    Guid EmploymentId,
    Domain.ClockType Type,
    Domain.ClockSource Source,
    DateTime? CapturedAtUtc = null,
    string? SourceDeviceId = null,
    double? Latitude = null,
    double? Longitude = null);

public record SelfServiceClockResult(
    Guid ClockEventId,
    Guid AttendanceDayId,
    string Status,
    uint RowVersion);

public interface IAttendanceSelfServiceContract
{
    Task<AttendanceDayDto?> GetTodayAsync(
        TenantId tenantId,
        LegalEntityId legalEntityId,
        Guid employmentId,
        DateOnly businessDate,
        CancellationToken ct = default);

    Task<SelfServiceClockResult> RecordClockAsync(
        TenantId tenantId,
        LegalEntityId legalEntityId,
        UserId actorUserId,
        RecordSelfServiceClockCommand command,
        CancellationToken ct = default);
}
