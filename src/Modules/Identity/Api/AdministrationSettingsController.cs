using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Workforce.Modules.Identity.Domain;
using Workforce.Modules.Identity.Infrastructure;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Identity.Api;

public record SaveSettingRequest(
    string Category,
    string Key,
    string ValueJson,
    DateTime EffectiveStartDate,
    DateTime? EffectiveEndDate
);

public record SaveRetentionPolicyRequest(
    string Module,
    string DataCategory,
    int RetentionDays,
    ExpiryAction ActionOnExpiry,
    bool IsActive,
    DateTime EffectiveStartDate
);

[ApiController]
[Route("api/v1/admin")]
public class AdministrationSettingsController : ControllerBase
{
    private readonly IAdministrationRepository _repository;
    private readonly IUserContext _userContext;

    public AdministrationSettingsController(IAdministrationRepository repository, IUserContext userContext)
    {
        _repository = repository;
        _userContext = userContext;
    }

    [HttpGet("settings")]
    [ProducesResponseType(typeof(IReadOnlyList<PlatformSetting>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSettings(CancellationToken ct)
    {
        var settings = await _repository.ListSettingsAsync(_userContext.TenantId, ct);
        return Ok(settings);
    }

    [HttpPut("settings")]
    [ProducesResponseType(typeof(PlatformSetting), StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveSetting([FromBody] SaveSettingRequest request, CancellationToken ct)
    {
        var setting = new PlatformSetting(
            Guid.NewGuid(),
            _userContext.TenantId,
            request.Category,
            request.Key,
            request.ValueJson,
            request.EffectiveStartDate == default ? DateTime.UtcNow.Date : request.EffectiveStartDate.Date,
            request.EffectiveEndDate?.Date,
            true,
            _userContext.UserId.Value,
            DateTime.UtcNow
        );

        await _repository.SaveSettingAsync(setting, _userContext.UserId.Value, ct);
        return Ok(setting);
    }

    [HttpGet("retention-policies")]
    [ProducesResponseType(typeof(IReadOnlyList<RetentionPolicy>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRetentionPolicies(CancellationToken ct)
    {
        var policies = await _repository.ListRetentionPoliciesAsync(_userContext.TenantId, ct);
        return Ok(policies);
    }

    [HttpPut("retention-policies")]
    [ProducesResponseType(typeof(RetentionPolicy), StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveRetentionPolicy([FromBody] SaveRetentionPolicyRequest request, CancellationToken ct)
    {
        var policy = new RetentionPolicy(
            Guid.NewGuid(),
            _userContext.TenantId,
            request.Module,
            request.DataCategory,
            request.RetentionDays,
            request.ActionOnExpiry,
            request.IsActive,
            request.EffectiveStartDate == default ? DateTime.UtcNow.Date : request.EffectiveStartDate.Date,
            _userContext.UserId.Value
        );

        await _repository.SaveRetentionPolicyAsync(policy, _userContext.UserId.Value, ct);
        return Ok(policy);
    }
}
