using System;
using System.Threading;
using System.Threading.Tasks;
using Workforce.Modules.People.Application.Contracts;
using Workforce.Modules.People.Domain;
using Workforce.Modules.People.Infrastructure;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.People.Application.Services;

public class PeopleAssignmentApplicationService : IPeopleAssignmentApplicationContract
{
    private readonly PeopleRepository _repository;

    public PeopleAssignmentApplicationService(PeopleRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<AssignmentActionResult> ChangeLocationAsync(
        TenantId tenantId,
        ChangeAssignmentLocationCommand command,
        CancellationToken ct = default)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        var profile = await _repository.GetEmployeeProfileAsync(command.EmploymentId, tenantId, command.LegalEntityId, ct);
        if (profile == null)
        {
            return new AssignmentActionResult(false, command.EmploymentId, Guid.Empty, 0, "Employee not found or access denied.", false);
        }

        var currentAssignment = profile.CurrentAssignment;
        if (currentAssignment == null)
        {
            return new AssignmentActionResult(false, command.EmploymentId, Guid.Empty, profile.RowVersion, "Employee has no active current assignment.", false);
        }

        var newAssignmentId = Guid.NewGuid();
        var newAssignment = new EmploymentAssignment(
            newAssignmentId,
            command.EmploymentId,
            currentAssignment.OrganizationUnitId,
            currentAssignment.JobTitleEn,
            currentAssignment.JobTitleAr,
            command.EffectiveFrom,
            null,
            currentAssignment.PositionId,
            command.LocationId,
            currentAssignment.ManagerEmploymentId,
            true
        );

        var success = await _repository.ChangeAssignmentAsync(
            command.EmploymentId,
            newAssignment,
            command.ExpectedRowVersion,
            command.LegalEntityId,
            ct);

        if (!success)
        {
            return new AssignmentActionResult(
                false,
                command.EmploymentId,
                Guid.Empty,
                profile.RowVersion,
                "Concurrency conflict or unauthorized: employee data was updated by another process.",
                true);
        }

        var updatedProfile = await _repository.GetEmployeeProfileAsync(command.EmploymentId, tenantId, command.LegalEntityId, ct);
        return new AssignmentActionResult(
            true,
            command.EmploymentId,
            newAssignmentId,
            updatedProfile?.RowVersion ?? (command.ExpectedRowVersion + 1),
            "Location updated successfully.",
            false);
    }

    public async Task<AssignmentActionResult> ChangeManagerAsync(
        TenantId tenantId,
        ChangeAssignmentManagerCommand command,
        CancellationToken ct = default)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        var profile = await _repository.GetEmployeeProfileAsync(command.EmploymentId, tenantId, command.LegalEntityId, ct);
        if (profile == null)
        {
            return new AssignmentActionResult(false, command.EmploymentId, Guid.Empty, 0, "Employee not found or access denied.", false);
        }

        var currentAssignment = profile.CurrentAssignment;
        if (currentAssignment == null)
        {
            return new AssignmentActionResult(false, command.EmploymentId, Guid.Empty, profile.RowVersion, "Employee has no active current assignment.", false);
        }

        var newAssignmentId = Guid.NewGuid();
        var newAssignment = new EmploymentAssignment(
            newAssignmentId,
            command.EmploymentId,
            currentAssignment.OrganizationUnitId,
            currentAssignment.JobTitleEn,
            currentAssignment.JobTitleAr,
            command.EffectiveFrom,
            null,
            currentAssignment.PositionId,
            currentAssignment.LocationId,
            command.ManagerEmploymentId,
            true
        );

        var success = await _repository.ChangeAssignmentAsync(
            command.EmploymentId,
            newAssignment,
            command.ExpectedRowVersion,
            command.LegalEntityId,
            ct);

        if (!success)
        {
            return new AssignmentActionResult(
                false,
                command.EmploymentId,
                Guid.Empty,
                profile.RowVersion,
                "Concurrency conflict or unauthorized: employee data was updated by another process.",
                true);
        }

        var updatedProfile = await _repository.GetEmployeeProfileAsync(command.EmploymentId, tenantId, command.LegalEntityId, ct);
        return new AssignmentActionResult(
            true,
            command.EmploymentId,
            newAssignmentId,
            updatedProfile?.RowVersion ?? (command.ExpectedRowVersion + 1),
            "Manager updated successfully.",
            false);
    }
}
