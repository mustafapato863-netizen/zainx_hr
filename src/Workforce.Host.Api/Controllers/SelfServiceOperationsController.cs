using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Workforce.Modules.Attendance.Application.Contracts;
using Workforce.Modules.Attendance.Domain;
using Workforce.Modules.Attendance.Infrastructure;
using Workforce.Modules.Documents.Application;
using Workforce.Modules.Documents.Infrastructure;
using Workforce.Modules.Leave.Application.Contracts;
using Workforce.Modules.Leave.Infrastructure;
using Workforce.Modules.People.Infrastructure;
using Workforce.SharedKernel.Primitives;
using Workforce.SharedKernel.Security;

namespace Workforce.Host.Api.Controllers;

/// <summary>
/// Contract-first ESS projections that compose the authoritative People, Leave,
/// and Attendance modules. This controller never accepts an arbitrary employee ID.
/// </summary>
[ApiController]
[Route("api/v1/self-service")]
public sealed class SelfServiceOperationsController : ControllerBase
{
    private readonly PeopleRepository _peopleRepository;
    private readonly ILeaveSelfServiceQueryContract _leaveQueries;
    private readonly ILeaveRequestApplicationContract _leaveRequests;
    private readonly ILeaveApprovalWorkflowStarter _approvalWorkflowStarter;
    private readonly IAttendanceSelfServiceContract _attendance;
    private readonly DocumentsRepository _documentsRepository;
    private readonly IStorageProvider _storageProvider;
    private readonly IUserContext _userContext;

    public SelfServiceOperationsController(
        PeopleRepository peopleRepository,
        ILeaveSelfServiceQueryContract leaveQueries,
        ILeaveRequestApplicationContract leaveRequests,
        ILeaveApprovalWorkflowStarter approvalWorkflowStarter,
        IAttendanceSelfServiceContract attendance,
        DocumentsRepository documentsRepository,
        IStorageProvider storageProvider,
        IUserContext userContext)
    {
        _peopleRepository = peopleRepository;
        _leaveQueries = leaveQueries;
        _leaveRequests = leaveRequests;
        _approvalWorkflowStarter = approvalWorkflowStarter;
        _attendance = attendance;
        _documentsRepository = documentsRepository;
        _storageProvider = storageProvider;
        _userContext = userContext;
    }

    [HttpGet("leave/balances")]
    [ProducesResponseType(typeof(IReadOnlyList<LeaveBalanceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLeaveBalances([FromQuery] int? year, CancellationToken ct)
    {
        if (!HasAnyPermission("self.leave.read")) return Forbid();
        if (!TryGetLegalEntity(out var legalEntity, out var legalEntityError)) return legalEntityError!;
        if (!TryGetYear(year, out var effectiveYear, out var yearError)) return yearError!;

        var employmentId = await GetLinkedEmploymentOrNullAsync(legalEntity, ct);
        if (!employmentId.HasValue) return IdentityLinkRequired();

        var balances = await _leaveQueries.GetBalancesAsync(
            _userContext.TenantId,
            legalEntity,
            employmentId.Value,
            effectiveYear,
            ct);
        return Ok(balances);
    }

    [HttpGet("leave/types")]
    [ProducesResponseType(typeof(IReadOnlyList<LeaveTypeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeaveTypes(CancellationToken ct)
    {
        if (!HasAnyPermission("self.leave.read")) return Forbid();
        if (!TryGetLegalEntity(out var legalEntity, out var legalEntityError)) return legalEntityError!;
        var linkedEmploymentId = await GetLinkedEmploymentOrNullAsync(legalEntity, ct).ConfigureAwait(false);
        if (!linkedEmploymentId.HasValue)
            return IdentityLinkRequired();

        return Ok(await _leaveQueries.GetTypesAsync(_userContext.TenantId, legalEntity, ct));
    }

    [HttpGet("leave/requests")]
    [ProducesResponseType(typeof(PagedSelfServiceLeaveRequestsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLeaveRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!HasAnyPermission("self.leave.read")) return Forbid();
        if (!TryGetLegalEntity(out var legalEntity, out var legalEntityError)) return legalEntityError!;

        if (page < 1 || pageSize is < 1 or > 100)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Pagination",
                Detail = "page must be at least 1 and pageSize must be between 1 and 100."
            });
        }

        var employmentId = await GetLinkedEmploymentOrNullAsync(legalEntity, ct);
        if (!employmentId.HasValue) return IdentityLinkRequired();

        var result = await _leaveQueries.GetRequestsAsync(
            _userContext.TenantId,
            legalEntity,
            employmentId.Value,
            page,
            pageSize,
            ct);

        return Ok(new PagedSelfServiceLeaveRequestsResponse(result.Items, result.TotalCount, page, pageSize));
    }

    [HttpPost("leave/requests")]
    [ProducesResponseType(typeof(SelfServiceLeaveRequestResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SubmitLeaveRequest(
        [FromBody] SelfServiceLeaveRequestInput input,
        CancellationToken ct)
    {
        if (!HasAnyPermission("self.leave.request")) return Forbid();
        if (!TryGetLegalEntity(out var legalEntity, out var legalEntityError)) return legalEntityError!;

        if (!DateOnly.TryParse(input.StartDate, out var startDate) || !DateOnly.TryParse(input.EndDate, out var endDate))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Leave Dates",
                Detail = "StartDate and EndDate must use the yyyy-MM-dd format."
            });
        }
        if (endDate < startDate)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Leave Date Range",
                Detail = "EndDate cannot be earlier than StartDate."
            });
        }

        var employmentId = await GetLinkedEmploymentOrNullAsync(legalEntity, ct);
        if (!employmentId.HasValue) return IdentityLinkRequired();

        var leaveRequestId = Guid.NewGuid();
        var approvalRequestId = Guid.NewGuid();
        var durationDays = endDate.DayNumber - startDate.DayNumber + 1;
        try
        {
            await _approvalWorkflowStarter.StartAsync(
                new StartLeaveApprovalWorkflowCommand(
                    _userContext.TenantId,
                    legalEntity,
                    approvalRequestId,
                    leaveRequestId,
                    _userContext.UserId.Value,
                    employmentId.Value,
                    startDate,
                    endDate,
                    durationDays,
                    input.Reason));

            var result = await _leaveRequests.SubmitAsync(
                new SubmitLeaveRequestCommand(
                    _userContext.TenantId,
                    legalEntity,
                    leaveRequestId,
                    employmentId.Value,
                    input.LeaveTypeId,
                    startDate,
                    endDate,
                    input.Reason,
                    approvalRequestId,
                    input.AttachmentDocumentId,
                    _userContext.UserId.Value),
                ct);

            return StatusCode(StatusCodes.Status201Created, new SelfServiceLeaveRequestResponse(
                result.RequestId,
                result.ApprovalRequestId,
                result.Status,
                result.RowVersion));
        }
        catch (Exception ex)
        {
            try
            {
                await _approvalWorkflowStarter.CancelStartedWorkflowAsync(
                    _userContext.TenantId,
                    legalEntity,
                    approvalRequestId,
                    _userContext.UserId.Value,
                    ct);
            }
            catch
            {
                // Preserve the original failure; the cancelled approval remains auditable for reconciliation.
            }

            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Leave Request Cannot Be Submitted",
                Detail = ex.Message
            });
        }
    }

    [HttpGet("attendance/today")]
    [ProducesResponseType(typeof(AttendanceDayDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTodayAttendance([FromQuery] string? date, CancellationToken ct)
    {
        if (!HasAnyPermission("self.attendance.read")) return Forbid();
        if (!TryGetLegalEntity(out var legalEntity, out var legalEntityError)) return legalEntityError!;

        var businessDate = DateOnly.FromDateTime(DateTime.UtcNow);
        if (!string.IsNullOrWhiteSpace(date) && !DateOnly.TryParse(date, out businessDate))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Attendance Date",
                Detail = "date must use the yyyy-MM-dd format."
            });
        }

        var employmentId = await GetLinkedEmploymentOrNullAsync(legalEntity, ct);
        if (!employmentId.HasValue) return IdentityLinkRequired();

        var day = await _attendance.GetTodayAsync(
            _userContext.TenantId,
            legalEntity,
            employmentId.Value,
            businessDate,
            ct);
        return day == null ? NoContent() : Ok(day);
    }

    [HttpPost("attendance/clock")]
    [ProducesResponseType(typeof(SelfServiceClockResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordClock(
        [FromBody] SelfServiceClockRequest request,
        CancellationToken ct)
    {
        if (!HasAnyPermission("self.attendance.clock")) return Forbid();
        if (!TryGetLegalEntity(out var legalEntity, out var legalEntityError)) return legalEntityError!;

        var employmentId = await GetLinkedEmploymentOrNullAsync(legalEntity, ct);
        if (!employmentId.HasValue) return IdentityLinkRequired();

        var result = await _attendance.RecordClockAsync(
            _userContext.TenantId,
            legalEntity,
            _userContext.UserId,
            new RecordSelfServiceClockCommand(
                employmentId.Value,
                request.Type,
                request.Source,
                null,
                request.SourceDeviceId,
                request.Latitude,
                request.Longitude),
            ct);

        return Ok(new SelfServiceClockResponse(
            result.ClockEventId,
            result.AttendanceDayId,
            result.Status,
            result.RowVersion));
    }

    [HttpGet("documents")]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocuments(CancellationToken ct)
    {
        if (!HasAnyPermission("self.documents.read")) return Forbid();
        if (!TryGetLegalEntity(out var legalEntity, out var legalEntityError)) return legalEntityError!;

        var employmentId = await GetLinkedEmploymentOrNullAsync(legalEntity, ct);
        if (!employmentId.HasValue) return IdentityLinkRequired();

        return Ok(await _documentsRepository.ListDocumentsAsync(
            _userContext.TenantId,
            "Employee",
            employmentId.Value,
            legalEntity,
            ct));
    }

    [HttpGet("documents/{id:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadDocument(Guid id, [FromQuery] int? version, CancellationToken ct)
    {
        if (!HasAnyPermission("self.documents.read")) return Forbid();
        if (!TryGetLegalEntity(out var legalEntity, out var legalEntityError)) return legalEntityError!;

        var employmentId = await GetLinkedEmploymentOrNullAsync(legalEntity, ct);
        if (!employmentId.HasValue) return IdentityLinkRequired();

        var detail = await _documentsRepository.GetDocumentDetailsAsync(
            id,
            _userContext.TenantId,
            legalEntity,
            ct);
        if (detail == null || !string.Equals(detail.OwnerType, "Employee", StringComparison.OrdinalIgnoreCase) || detail.OwnerId != employmentId.Value)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Document Not Found",
                Detail = "The requested document is not attached to the current employee identity.",
                Instance = HttpContext.Request.Path
            });
        }

        var storageKey = await _documentsRepository.GetStorageKeyForDownloadAsync(
            id,
            _userContext.TenantId,
            legalEntity,
            version,
            ct);
        if (string.IsNullOrWhiteSpace(storageKey)) return NotFound();

        var stream = await _storageProvider.ReadAsync(storageKey, ct);
        if (stream == null) return NotFound();

        var targetVersion = version.HasValue
            ? detail.Versions.FirstOrDefault(item => item.VersionNumber == version.Value)
            : detail.Versions.OrderByDescending(item => item.VersionNumber).FirstOrDefault();
        if (targetVersion == null)
        {
            await stream.DisposeAsync();
            return NotFound();
        }

        await _documentsRepository.RecordAccessAsync(
            id,
            _userContext.TenantId,
            legalEntity,
            _userContext.UserId.Value,
            "self-service-download",
            targetVersion.VersionNumber,
            ct);

        return File(
            stream,
            string.IsNullOrWhiteSpace(targetVersion.ContentType) ? "application/octet-stream" : targetVersion.ContentType,
            DocumentSecurityValidator.SanitizeFileName(targetVersion.FileName));
    }

    private async Task<Guid?> GetLinkedEmploymentOrNullAsync(LegalEntityId legalEntity, CancellationToken ct)
    {
        return await _peopleRepository.GetLinkedEmploymentIdAsync(
            _userContext.TenantId,
            legalEntity,
            _userContext.UserId.Value,
            ct);
    }

    private bool HasAnyPermission(params string[] permissions)
    {
        if (_userContext.HasPermission("admin")) return true;
        return permissions.Any(_userContext.HasPermission);
    }

    private bool TryGetLegalEntity(out LegalEntityId legalEntity, out IActionResult? error)
    {
        if (_userContext.LegalEntityId.HasValue)
        {
            legalEntity = _userContext.LegalEntityId.Value;
            error = null;
            return true;
        }

        legalEntity = default;
        error = BadRequest(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Legal Entity Context Required",
            Detail = "Select an authorized legal entity before using self-service."
        });
        return false;
    }

    private static bool TryGetYear(int? year, out int effectiveYear, out BadRequestObjectResult? error)
    {
        effectiveYear = year ?? DateTime.UtcNow.Year;
        if (effectiveYear is >= 2000 and <= 2100)
        {
            error = null;
            return true;
        }

        error = new BadRequestObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid Leave Year",
            Detail = "year must be between 2000 and 2100."
        });
        return false;
    }

    private NotFoundObjectResult IdentityLinkRequired()
    {
        return NotFound(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Employee Identity Link Required",
            Detail = "The current authenticated user is not explicitly linked to an employment in this legal-entity scope. Ask an administrator to configure the link."
        });
    }
}

public sealed record SelfServiceClockRequest(
    ClockType Type,
    ClockSource Source = ClockSource.WebPortal,
    string? SourceDeviceId = null,
    double? Latitude = null,
    double? Longitude = null);

public sealed record SelfServiceClockResponse(
    Guid ClockEventId,
    Guid AttendanceDayId,
    string Status,
    uint RowVersion);

public sealed record PagedSelfServiceLeaveRequestsResponse(
    IReadOnlyList<LeaveRequestDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record SelfServiceLeaveRequestInput(
    Guid LeaveTypeId,
    string StartDate,
    string EndDate,
    string Reason,
    Guid? AttachmentDocumentId = null);

public sealed record SelfServiceLeaveRequestResponse(
    Guid RequestId,
    Guid ApprovalRequestId,
    string Status,
    uint RowVersion);
