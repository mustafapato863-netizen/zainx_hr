using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Workforce.Modules.Ai.Application.Contracts;
using Workforce.Modules.Ai.Domain;
using Workforce.Modules.Leave.Application.Contracts;
using Workforce.Modules.People.Application.Contracts;
using Workforce.Modules.Recruitment.Contracts;
using Workforce.SharedKernel.Primitives;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Ai.Application.Actions;

public sealed class PeopleChangeLocationActionHandler : IAiActionHandler
{
    private readonly IPeopleAssignmentApplicationContract _peopleContract;

    public string ActionCode => "people.assignment.change_location";

    public AiActionDefinition Definition { get; } = new(
        ActionCode: "people.assignment.change_location",
        Description: "Propose an effective-dated work location change for an employee.",
        TargetModule: "People",
        RequiredPermission: "people.assignment.update",
        InputSchemaJson: "{\"type\":\"object\",\"required\":[\"employmentId\",\"locationId\",\"locationNameEn\",\"effectiveFrom\",\"expectedRowVersion\"],\"properties\":{\"employmentId\":{\"type\":\"string\"},\"locationId\":{\"type\":\"string\"},\"locationNameEn\":{\"type\":\"string\"},\"effectiveFrom\":{\"type\":\"string\",\"format\":\"date\"},\"expectedRowVersion\":{\"type\":\"integer\"}}}",
        Sensitivity: "Internal"
    );

    public PeopleChangeLocationActionHandler(IPeopleAssignmentApplicationContract peopleContract)
    {
        _peopleContract = peopleContract ?? throw new ArgumentNullException(nameof(peopleContract));
    }

    public async Task<AiActionExecutionResult> ExecuteActionAsync(AiActionProposal proposal, IUserContext userContext, CancellationToken ct = default)
    {
        if (!Guid.TryParse(proposal.TargetEntityId, out var empId))
        {
            return new AiActionExecutionResult(false, "Failed", "{}", "Invalid target entity id.", false);
        }

        Guid? locationId = null;
        string locationName = "Default Location";
        DateOnly effectiveDate = DateOnly.FromDateTime(proposal.EffectiveDateUtc ?? DateTime.UtcNow);

        try
        {
            using var doc = JsonDocument.Parse(proposal.AfterSnapshotJson);
            if (doc.RootElement.TryGetProperty("locationId", out var locProp) && Guid.TryParse(locProp.GetString(), out var lid))
            {
                locationId = lid;
            }
            if (doc.RootElement.TryGetProperty("locationNameEn", out var locNameProp))
            {
                locationName = locNameProp.GetString() ?? locationName;
            }
        }
        catch (JsonException)
        {
            // fallback to proposal defaults
        }

        var command = new ChangeAssignmentLocationCommand(
            empId,
            locationId,
            locationName,
            effectiveDate,
            proposal.ExpectedRowVersion,
            userContext.LegalEntityId
        );

        var result = await _peopleContract.ChangeLocationAsync(userContext.TenantId, command, ct);
        if (!result.Success)
        {
            return new AiActionExecutionResult(false, result.IsConcurrencyConflict ? "Stale" : "Failed", "{}", result.Message, result.IsConcurrencyConflict);
        }

        var payload = JsonSerializer.Serialize(new
        {
            employmentId = result.EmploymentId,
            assignmentId = result.AssignmentId,
            newRowVersion = result.NewRowVersion,
            effectiveFrom = effectiveDate.ToString("yyyy-MM-dd"),
            message = result.Message
        });

        return new AiActionExecutionResult(true, "Completed", payload, null, false);
    }
}

public sealed class PeopleChangeManagerActionHandler : IAiActionHandler
{
    private readonly IPeopleAssignmentApplicationContract _peopleContract;

    public string ActionCode => "people.assignment.change_manager";

    public AiActionDefinition Definition { get; } = new(
        ActionCode: "people.assignment.change_manager",
        Description: "Propose an effective-dated manager reassignment for an employee.",
        TargetModule: "People",
        RequiredPermission: "people.assignment.update",
        InputSchemaJson: "{\"type\":\"object\",\"required\":[\"employmentId\",\"managerEmploymentId\",\"effectiveFrom\",\"expectedRowVersion\"],\"properties\":{\"employmentId\":{\"type\":\"string\"},\"managerEmploymentId\":{\"type\":\"string\"},\"managerNameEn\":{\"type\":\"string\"},\"effectiveFrom\":{\"type\":\"string\",\"format\":\"date\"},\"expectedRowVersion\":{\"type\":\"integer\"}}}",
        Sensitivity: "Internal"
    );

    public PeopleChangeManagerActionHandler(IPeopleAssignmentApplicationContract peopleContract)
    {
        _peopleContract = peopleContract ?? throw new ArgumentNullException(nameof(peopleContract));
    }

    public async Task<AiActionExecutionResult> ExecuteActionAsync(AiActionProposal proposal, IUserContext userContext, CancellationToken ct = default)
    {
        if (!Guid.TryParse(proposal.TargetEntityId, out var empId))
        {
            return new AiActionExecutionResult(false, "Failed", "{}", "Invalid target entity id.", false);
        }

        Guid? managerId = null;
        string? managerName = null;
        DateOnly effectiveDate = DateOnly.FromDateTime(proposal.EffectiveDateUtc ?? DateTime.UtcNow);

        try
        {
            using var doc = JsonDocument.Parse(proposal.AfterSnapshotJson);
            if (doc.RootElement.TryGetProperty("managerEmploymentId", out var mgrProp) && Guid.TryParse(mgrProp.GetString(), out var mid))
            {
                managerId = mid;
            }
            if (doc.RootElement.TryGetProperty("managerNameEn", out var mgrNameProp))
            {
                managerName = mgrNameProp.GetString();
            }
        }
        catch (JsonException)
        {
            // fallback
        }

        var command = new ChangeAssignmentManagerCommand(
            empId,
            managerId,
            managerName,
            effectiveDate,
            proposal.ExpectedRowVersion,
            userContext.LegalEntityId
        );

        var result = await _peopleContract.ChangeManagerAsync(userContext.TenantId, command, ct);
        if (!result.Success)
        {
            return new AiActionExecutionResult(false, result.IsConcurrencyConflict ? "Stale" : "Failed", "{}", result.Message, result.IsConcurrencyConflict);
        }

        var payload = JsonSerializer.Serialize(new
        {
            employmentId = result.EmploymentId,
            assignmentId = result.AssignmentId,
            newRowVersion = result.NewRowVersion,
            effectiveFrom = effectiveDate.ToString("yyyy-MM-dd"),
            message = result.Message
        });

        return new AiActionExecutionResult(true, "Completed", payload, null, false);
    }
}

public sealed class RecruitmentMoveStageActionHandler : IAiActionHandler
{
    private readonly IRecruitmentActionContract _recruitmentContract;

    public string ActionCode => "recruitment.application.move_stage";

    public AiActionDefinition Definition { get; } = new(
        ActionCode: "recruitment.application.move_stage",
        Description: "Move a candidate application to a designated recruitment pipeline stage.",
        TargetModule: "Recruitment",
        RequiredPermission: "recruitment.application.update",
        InputSchemaJson: "{\"type\":\"object\",\"required\":[\"applicationId\",\"targetStageId\",\"expectedRowVersion\"],\"properties\":{\"applicationId\":{\"type\":\"string\"},\"targetStageId\":{\"type\":\"string\"},\"reason\":{\"type\":\"string\"},\"expectedRowVersion\":{\"type\":\"integer\"}}}",
        Sensitivity: "Internal"
    );

    public RecruitmentMoveStageActionHandler(IRecruitmentActionContract recruitmentContract)
    {
        _recruitmentContract = recruitmentContract ?? throw new ArgumentNullException(nameof(recruitmentContract));
    }

    public async Task<AiActionExecutionResult> ExecuteActionAsync(AiActionProposal proposal, IUserContext userContext, CancellationToken ct = default)
    {
        if (!Guid.TryParse(proposal.TargetEntityId, out var appId))
        {
            return new AiActionExecutionResult(false, "Failed", "{}", "Invalid target entity id.", false);
        }

        Guid targetStageId = Guid.Empty;
        string? reason = "Moved via AI proposal confirmation";

        try
        {
            using var doc = JsonDocument.Parse(proposal.AfterSnapshotJson);
            if (doc.RootElement.TryGetProperty("targetStageId", out var stageProp) && Guid.TryParse(stageProp.GetString(), out var sid))
            {
                targetStageId = sid;
            }
            if (doc.RootElement.TryGetProperty("reason", out var reasonProp))
            {
                reason = reasonProp.GetString();
            }
        }
        catch (JsonException)
        {
            // fallback
        }

        if (targetStageId == Guid.Empty)
        {
            return new AiActionExecutionResult(false, "Failed", "{}", "Target stage ID is required.", false);
        }

        var command = new MoveApplicationStageCommand(
            appId,
            targetStageId,
            reason,
            proposal.IdempotencyKey,
            proposal.ExpectedRowVersion,
            userContext.LegalEntityId
        );

        var result = await _recruitmentContract.MoveApplicationStageAsync(
            userContext.TenantId,
            userContext.UserId,
            command,
            ct);

        if (!result.Success)
        {
            return new AiActionExecutionResult(false, result.IsConcurrencyConflict ? "Stale" : "Failed", "{}", result.Message, result.IsConcurrencyConflict);
        }

        var payload = JsonSerializer.Serialize(new
        {
            applicationId = result.EntityId,
            newRowVersion = result.NewRowVersion,
            message = result.Message
        });

        return new AiActionExecutionResult(true, "Completed", payload, null, false);
    }
}

public sealed class RecruitmentSubmitRequisitionActionHandler : IAiActionHandler
{
    private readonly IRecruitmentActionContract _recruitmentContract;

    public string ActionCode => "recruitment.requisition.submit";

    public AiActionDefinition Definition { get; } = new(
        ActionCode: "recruitment.requisition.submit",
        Description: "Submit an open job requisition into the Shared Approvals workflow.",
        TargetModule: "Recruitment",
        RequiredPermission: "recruitment.requisition.submit",
        InputSchemaJson: "{\"type\":\"object\",\"required\":[\"requisitionId\",\"expectedRowVersion\"],\"properties\":{\"requisitionId\":{\"type\":\"string\"},\"expectedRowVersion\":{\"type\":\"integer\"}}}",
        Sensitivity: "Internal"
    );

    public RecruitmentSubmitRequisitionActionHandler(IRecruitmentActionContract recruitmentContract)
    {
        _recruitmentContract = recruitmentContract ?? throw new ArgumentNullException(nameof(recruitmentContract));
    }

    public async Task<AiActionExecutionResult> ExecuteActionAsync(AiActionProposal proposal, IUserContext userContext, CancellationToken ct = default)
    {
        if (!Guid.TryParse(proposal.TargetEntityId, out var reqId))
        {
            return new AiActionExecutionResult(false, "Failed", "{}", "Invalid target entity id.", false);
        }

        var command = new SubmitRequisitionApprovalCommand(
            reqId,
            proposal.ExpectedRowVersion,
            userContext.LegalEntityId
        );

        var result = await _recruitmentContract.SubmitRequisitionApprovalAsync(
            userContext.TenantId,
            userContext.UserId,
            command,
            ct);

        if (!result.Success)
        {
            return new AiActionExecutionResult(false, result.IsConcurrencyConflict ? "Stale" : "Failed", "{}", result.Message, result.IsConcurrencyConflict);
        }

        var payload = JsonSerializer.Serialize(new
        {
            requisitionId = result.EntityId,
            newRowVersion = result.NewRowVersion,
            message = result.Message
        });

        return new AiActionExecutionResult(true, "Completed", payload, null, false);
    }
}

public sealed class LeaveCancelRequestActionHandler : IAiActionHandler
{
    private readonly ILeaveActionContract _leaveContract;

    public string ActionCode => "leave.request.cancel";

    public AiActionDefinition Definition { get; } = new(
        ActionCode: "leave.request.cancel",
        Description: "Cancel a pending or submitted leave request.",
        TargetModule: "Leave",
        RequiredPermission: "leave.request.cancel",
        InputSchemaJson: "{\"type\":\"object\",\"required\":[\"leaveRequestId\",\"expectedRowVersion\"],\"properties\":{\"leaveRequestId\":{\"type\":\"string\"},\"expectedRowVersion\":{\"type\":\"integer\"}}}",
        Sensitivity: "Internal"
    );

    public LeaveCancelRequestActionHandler(ILeaveActionContract leaveContract)
    {
        _leaveContract = leaveContract ?? throw new ArgumentNullException(nameof(leaveContract));
    }

    public async Task<AiActionExecutionResult> ExecuteActionAsync(AiActionProposal proposal, IUserContext userContext, CancellationToken ct = default)
    {
        if (!Guid.TryParse(proposal.TargetEntityId, out var reqId))
        {
            return new AiActionExecutionResult(false, "Failed", "{}", "Invalid target entity id.", false);
        }

        var command = new CancelLeaveRequestCommand(
            reqId,
            proposal.ExpectedRowVersion,
            userContext.LegalEntityId
        );

        var result = await _leaveContract.CancelLeaveRequestAsync(
            userContext.TenantId,
            userContext.UserId,
            command,
            ct);

        if (!result.Success)
        {
            return new AiActionExecutionResult(false, result.IsConcurrencyConflict ? "Stale" : "Failed", "{}", result.Message, result.IsConcurrencyConflict);
        }

        var payload = JsonSerializer.Serialize(new
        {
            leaveRequestId = result.LeaveRequestId,
            newRowVersion = result.NewRowVersion,
            message = result.Message
        });

        return new AiActionExecutionResult(true, "Completed", payload, null, false);
    }
}
