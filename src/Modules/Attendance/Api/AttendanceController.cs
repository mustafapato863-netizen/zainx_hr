using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Workforce.Modules.Attendance.Domain;
using Workforce.Modules.Attendance.Infrastructure;
using Workforce.SharedKernel.Primitives;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Attendance.Api;

[ApiController]
[Route("api/v1/attendance")]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceRepository _repository;
    private readonly IUserContext _userContext;

    public AttendanceController(IAttendanceRepository repository, IUserContext userContext)
    {
        _repository = repository;
        _userContext = userContext;
    }

    [HttpPost("clock")]
    public async Task<IActionResult> RecordClockEvent([FromBody] RecordClockRequest request)
    {
        if (!_userContext.HasPermission("attendance.clock.create") && !_userContext.HasPermission("admin"))
        {
            return Forbid();
        }

        if (!_userContext.LegalEntityId.HasValue)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Legal Entity Context Required",
                Detail = "A legal entity context is required before recording attendance."
            });
        }

        var clockEvent = new ClockEvent(
            Guid.NewGuid(),
            _userContext.TenantId,
            request.EmploymentId,
            request.Type,
            request.Source,
            request.CapturedAtUtc ?? DateTime.UtcNow,
            DateTime.UtcNow,
            request.SourceDeviceId,
            Guid.NewGuid().ToString(),
            _userContext.UserId.Value,
            request.Latitude,
            request.Longitude
        );

        await _repository.RecordClockEventAsync(clockEvent);

        var businessDate = DateOnly.FromDateTime(clockEvent.CapturedAtUtc);
        var legalEntityId = _userContext.LegalEntityId.Value;
        var day = await _repository.GetOrCreateAttendanceDayAsync(
            _userContext.TenantId,
            legalEntityId,
            request.EmploymentId,
            businessDate,
            "UTC"
        );

        if (day != null)
        {
            var dayStartUtc = businessDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var dayEndUtc = businessDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            var allEvents = await _repository.GetClockEventsAsync(_userContext.TenantId, request.EmploymentId, dayStartUtc, dayEndUtc);
            day.Evaluate(allEvents, null);
            await _repository.SaveAttendanceDayAsync(day);
        }

        return Ok(new { clockEventId = clockEvent.Id, status = "Recorded" });
    }

    [HttpGet("days")]
    public async Task<ActionResult<PagedAttendanceDaysResponse>> GetAttendanceDays(
        [FromQuery] string? fromDate,
        [FromQuery] string? toDate,
        [FromQuery] int? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (!HasAnyPermission("attendance.day.read", "attendance.read"))
        {
            return Forbid();
        }

        if (!_userContext.LegalEntityId.HasValue)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Legal Entity Context Required", Detail = "Select an authorized legal entity before reading attendance." });
        }

        DateOnly? from = !string.IsNullOrWhiteSpace(fromDate) && DateOnly.TryParse(fromDate, out var f) ? f : null;
        DateOnly? to = !string.IsNullOrWhiteSpace(toDate) && DateOnly.TryParse(toDate, out var t) ? t : null;

        var (items, total) = await _repository.GetAttendanceDaysAsync(
            _userContext.TenantId,
            _userContext.LegalEntityId,
            from,
            to,
            status,
            page,
            pageSize
        );

        return Ok(new PagedAttendanceDaysResponse(items, total, page, pageSize));
    }

    [HttpGet("days/{id:guid}")]
    public async Task<ActionResult<AttendanceDayDto>> GetAttendanceDayById(Guid id)
    {
        if (!HasAnyPermission("attendance.day.read", "attendance.read"))
        {
            return Forbid();
        }

        if (!_userContext.LegalEntityId.HasValue)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Legal Entity Context Required", Detail = "Select an authorized legal entity before reading an attendance day." });
        }

        var day = await _repository.GetAttendanceDayByIdAsync(_userContext.TenantId, _userContext.LegalEntityId.Value, id);
        if (day == null) return NotFound();
        return Ok(day);
    }

    [HttpPost("days/{id:guid}/adjustments")]
    public async Task<IActionResult> SubmitAdjustment(Guid id, [FromBody] SubmitAdjustmentRequest request)
    {
        if (!_userContext.HasPermission("attendance.adjustment.submit") && !_userContext.HasPermission("admin"))
        {
            return Forbid();
        }

        if (!_userContext.LegalEntityId.HasValue)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Legal Entity Context Required", Detail = "Select an authorized legal entity before adjusting attendance." });
        }

        var day = await _repository.GetAttendanceDayEntityByIdAsync(_userContext.TenantId, _userContext.LegalEntityId.Value, id);
        if (day == null) return NotFound();

        try
        {
            day.ApplyAdjustment(
                request.AdjustedWorkedMinutes,
                request.Reason,
                _userContext.UserId.Value,
                request.RowVersion,
                request.ApprovalRequestId
            );

            await _repository.SaveAttendanceDayAsync(day);
            return Ok(new { status = "Adjusted", totalWorkedMinutes = day.TotalWorkedMinutes, rowVersion = day.RowVersion });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("concurrency", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Optimistic Concurrency Conflict",
                Detail = ex.Message,
                Status = 409
            });
        }
    }

    [HttpPost("days/{id:guid}/approve")]
    public async Task<IActionResult> ApproveDay(Guid id, [FromBody] ConcurrencyActionRequest request)
    {
        if (!_userContext.HasPermission("attendance.day.approve") && !_userContext.HasPermission("admin"))
        {
            return Forbid();
        }

        if (!_userContext.LegalEntityId.HasValue)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Legal Entity Context Required", Detail = "Select an authorized legal entity before approving attendance." });
        }

        var day = await _repository.GetAttendanceDayEntityByIdAsync(_userContext.TenantId, _userContext.LegalEntityId.Value, id);
        if (day == null) return NotFound();

        try
        {
            day.Approve(request.RowVersion);
            await _repository.SaveAttendanceDayAsync(day);
            return Ok(new { status = "Approved", rowVersion = day.RowVersion });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("concurrency", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Optimistic Concurrency Conflict",
                Detail = ex.Message,
                Status = 409
            });
        }
    }

    [HttpGet("schedules")]
    public async Task<ActionResult<IReadOnlyList<WorkSchedule>>> GetSchedules()
    {
        if (!HasAnyPermission("attendance.schedule.read", "attendance.day.read", "attendance.read"))
        {
            return Forbid();
        }

        if (!_userContext.LegalEntityId.HasValue)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Legal Entity Context Required", Detail = "Select an authorized legal entity before reading schedules." });
        }

        var legalEntityId = _userContext.LegalEntityId.Value;
        var schedules = await _repository.GetWorkSchedulesAsync(_userContext.TenantId, legalEntityId);
        return Ok(schedules);
    }

    private bool HasAnyPermission(params string[] permissions)
    {
        if (_userContext.HasPermission("admin")) return true;
        return permissions.Any(_userContext.HasPermission);
    }
}

public record RecordClockRequest(
    Guid EmploymentId,
    ClockType Type,
    ClockSource Source,
    DateTime? CapturedAtUtc = null,
    string? SourceDeviceId = null,
    double? Latitude = null,
    double? Longitude = null
);

public record SubmitAdjustmentRequest(
    int AdjustedWorkedMinutes,
    string Reason,
    uint RowVersion,
    Guid? ApprovalRequestId = null
);

public record ConcurrencyActionRequest(
    uint RowVersion
);

public record PagedAttendanceDaysResponse(
    IReadOnlyList<AttendanceDayDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);
