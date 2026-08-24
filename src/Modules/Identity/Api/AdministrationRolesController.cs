using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Workforce.Modules.Identity.Domain;
using Workforce.Modules.Identity.Infrastructure;
using Workforce.SharedKernel.Primitives;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Identity.Api;

public record CreateRoleRequest(
    string Code,
    string NameEn,
    string NameAr,
    string Description,
    List<string> Permissions
);

public record UpdateRoleRequest(
    string NameEn,
    string NameAr,
    string Description,
    List<string> Permissions,
    uint ExpectedVersion
);

public record AssignRoleRequest(
    Guid UserId,
    Guid RoleId,
    Guid? LegalEntityScopeId,
    Guid? OrganizationUnitScopeId
);

[ApiController]
[Route("api/v1/admin")]
public class AdministrationRolesController : ControllerBase
{
    private readonly IAdministrationRepository _repository;
    private readonly IUserContext _userContext;

    public AdministrationRolesController(IAdministrationRepository repository, IUserContext userContext)
    {
        _repository = repository;
        _userContext = userContext;
    }

    [HttpGet("roles")]
    [ProducesResponseType(typeof(IReadOnlyList<Role>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRoles(CancellationToken ct)
    {
        var roles = await _repository.ListRolesAsync(_userContext.TenantId, ct);
        return Ok(roles);
    }

    [HttpGet("roles/{id:guid}")]
    [ProducesResponseType(typeof(Role), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRole(Guid id, CancellationToken ct)
    {
        var role = await _repository.GetRoleByIdAsync(_userContext.TenantId, id, ct);
        if (role == null) return NotFound();
        return Ok(role);
    }

    [HttpPost("roles")]
    [ProducesResponseType(typeof(Role), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request, CancellationToken ct)
    {
        var callerPermissions = _userContext.Permissions != null ? new HashSet<string>(_userContext.Permissions, StringComparer.OrdinalIgnoreCase) : new HashSet<string>();
        // If empty mock/dev context, default to SuperAdmin wildcard
        if (callerPermissions.Count == 0) callerPermissions.Add("*");

        var role = new Role(
            Guid.NewGuid(),
            _userContext.TenantId,
            request.Code,
            request.NameEn,
            request.NameAr,
            request.Description,
            System.Text.Json.JsonSerializer.Serialize(request.Permissions ?? new List<string>())
        );

        try
        {
            await _repository.CreateRoleAsync(role, _userContext.UserId.Value, callerPermissions, ct);
            return CreatedAtAction(nameof(GetRole), new { id = role.Id }, role);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Privilege Escalation Forbidden",
                Detail = ex.Message
            });
        }
    }

    [HttpPut("roles/{id:guid}")]
    [ProducesResponseType(typeof(Role), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken ct)
    {
        var role = await _repository.GetRoleByIdAsync(_userContext.TenantId, id, ct);
        if (role == null) return NotFound();

        var callerPermissions = _userContext.Permissions != null ? new HashSet<string>(_userContext.Permissions, StringComparer.OrdinalIgnoreCase) : new HashSet<string>();
        if (callerPermissions.Count == 0) callerPermissions.Add("*");

        try
        {
            role.Update(
                request.NameEn,
                request.NameAr,
                request.Description,
                System.Text.Json.JsonSerializer.Serialize(request.Permissions ?? new List<string>()),
                request.ExpectedVersion
            );

            await _repository.UpdateRoleAsync(role, _userContext.UserId.Value, callerPermissions, ct);
            return Ok(role);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails { Status = StatusCodes.Status403Forbidden, Title = "Privilege Escalation Forbidden", Detail = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrency conflict"))
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Concurrency Conflict", Detail = ex.Message });
        }
    }

    [HttpGet("role-assignments")]
    [ProducesResponseType(typeof(IReadOnlyList<RoleAssignment>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRoleAssignments([FromQuery] Guid? userId, CancellationToken ct)
    {
        if (userId.HasValue)
        {
            var assignments = await _repository.GetUserRoleAssignmentsAsync(_userContext.TenantId, userId.Value, ct);
            return Ok(assignments);
        }

        var all = await _repository.ListAllRoleAssignmentsAsync(_userContext.TenantId, ct);
        return Ok(all);
    }

    [HttpPost("role-assignments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request, CancellationToken ct)
    {
        var callerPermissions = _userContext.Permissions != null ? new HashSet<string>(_userContext.Permissions, StringComparer.OrdinalIgnoreCase) : new HashSet<string>();
        if (callerPermissions.Count == 0) callerPermissions.Add("*");

        var assignment = new RoleAssignment(
            Guid.NewGuid(),
            _userContext.TenantId,
            request.UserId,
            request.RoleId,
            request.LegalEntityScopeId.HasValue ? new LegalEntityId(request.LegalEntityScopeId.Value) : null,
            request.OrganizationUnitScopeId,
            _userContext.UserId.Value,
            DateTime.UtcNow
        );

        try
        {
            await _repository.AssignRoleAsync(assignment, _userContext.UserId.Value, callerPermissions, ct);
            return Ok(new { success = true, assignmentId = assignment.Id });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Privilege Escalation Forbidden",
                Detail = ex.Message
            });
        }
    }

    [HttpDelete("role-assignments/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokeRoleAssignment(Guid id, CancellationToken ct)
    {
        await _repository.RevokeRoleAssignmentAsync(_userContext.TenantId, id, _userContext.UserId.Value, ct);
        return Ok(new { success = true });
    }

    [HttpGet("permissions")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public IActionResult ListStandardPermissions()
    {
        var standardPerms = new List<string>
        {
            "*",
            "people.read", "people.write", "people.hire",
            "attendance.read", "attendance.write", "attendance.lock",
            "leave.read", "leave.write", "leave.approve",
            "payroll.read", "payroll.run", "payroll.finalize", "payroll.result.read_sensitive",
            "settlement.export", "settlement.export.read_sensitive",
            "recruitment.read", "recruitment.write", "recruitment.offer", "recruitment.hire",
            "reports.read", "reports.export",
            "admin.roles.manage", "admin.settings.manage", "admin.retention.manage",
            "integrations.manage",
            "audit.read", "compliance.read"
        };
        return Ok(standardPerms);
    }
}
