using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Workforce.Modules.Attendance.Infrastructure;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Attendance.Api;

[ApiController]
[Route("api/v1/attendance/exceptions")]
public class AttendanceExceptionsController : ControllerBase
{
    private readonly IAttendanceRepository _repository;
    private readonly IUserContext _userContext;

    public AttendanceExceptionsController(IAttendanceRepository repository, IUserContext userContext)
    {
        _repository = repository;
        _userContext = userContext;
    }

    [HttpGet]
    public async Task<ActionResult<PagedExceptionsResponse>> GetExceptionsQueue(
        [FromQuery] int? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var (items, total) = await _repository.GetExceptionsQueueAsync(
            _userContext.TenantId,
            status,
            page,
            pageSize
        );

        return Ok(new PagedExceptionsResponse(items, total, page, pageSize));
    }

    [HttpPost("{id:guid}/resolve")]
    public async Task<IActionResult> ResolveException(Guid id, [FromBody] ResolveExceptionRequest request)
    {
        if (!_userContext.HasPermission("attendance.exception.resolve") && !_userContext.HasPermission("admin"))
        {
            return Forbid();
        }

        var success = await _repository.ResolveExceptionAsync(
            _userContext.TenantId,
            id,
            request.Notes,
            _userContext.UserId.Value
        );

        if (!success) return NotFound();
        return Ok(new { status = "Resolved" });
    }
}

public record ResolveExceptionRequest(
    string Notes
);

public record PagedExceptionsResponse(
    IReadOnlyList<AttendanceExceptionDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);
