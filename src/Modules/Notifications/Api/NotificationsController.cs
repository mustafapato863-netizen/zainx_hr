using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Workforce.Modules.Notifications.Domain;
using Workforce.Modules.Notifications.Infrastructure;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Notifications.Api;

public record UpdatePreferenceRequest(
    string Category,
    bool AllowEmail,
    bool AllowInApp,
    bool AllowPush
);

public record UpdateTemplateRequest(
    string Subject,
    string BodyTemplate,
    string AllowedVariablesJson,
    bool IsActive
);

[ApiController]
[Route("api/v1/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationsRepository _repository;
    private readonly IUserContext _userContext;

    public NotificationsController(INotificationsRepository repository, IUserContext userContext)
    {
        _repository = repository;
        _userContext = userContext;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedNotificationsResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListNotifications(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _repository.ListNotificationsAsync(_userContext.TenantId, _userContext.UserId.Value, unreadOnly, page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
    {
        var count = await _repository.GetUnreadCountAsync(_userContext.TenantId, _userContext.UserId.Value, ct);
        return Ok(new { unreadCount = count });
    }

    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
    {
        var success = await _repository.MarkAsReadAsync(_userContext.TenantId, _userContext.UserId.Value, id, ct);
        return Ok(new { success });
    }

    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
    {
        var count = await _repository.MarkAllAsReadAsync(_userContext.TenantId, _userContext.UserId.Value, ct);
        return Ok(new { markedReadCount = count });
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ArchiveNotification(Guid id, CancellationToken ct)
    {
        var success = await _repository.ArchiveAsync(_userContext.TenantId, _userContext.UserId.Value, id, ct);
        return Ok(new { success });
    }

    [HttpGet("preferences")]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationPreference>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPreferences(CancellationToken ct)
    {
        var prefs = await _repository.GetPreferencesAsync(_userContext.TenantId, _userContext.UserId.Value, ct);
        return Ok(prefs);
    }

    [HttpPut("preferences")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePreference([FromBody] UpdatePreferenceRequest request, CancellationToken ct)
    {
        var pref = new NotificationPreference(
            Guid.NewGuid(),
            _userContext.TenantId,
            _userContext.UserId.Value,
            request.Category,
            request.AllowEmail,
            request.AllowInApp,
            request.AllowPush
        );

        await _repository.SavePreferenceAsync(pref, ct);
        return Ok(pref);
    }

    [HttpGet("templates")]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationTemplate>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTemplates(CancellationToken ct)
    {
        var templates = await _repository.ListTemplatesAsync(_userContext.TenantId, ct);
        return Ok(templates);
    }

    [HttpPut("templates/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTemplate(Guid id, [FromBody] UpdateTemplateRequest request, CancellationToken ct)
    {
        var templates = await _repository.ListTemplatesAsync(_userContext.TenantId, ct);
        var target = ((List<NotificationTemplate>)templates).Find(t => t.Id == id);
        if (target == null)
        {
            return NotFound();
        }

        target.Update(request.Subject, request.BodyTemplate, request.AllowedVariablesJson, request.IsActive);
        await _repository.SaveTemplateAsync(target, ct);
        return Ok(target);
    }
}
