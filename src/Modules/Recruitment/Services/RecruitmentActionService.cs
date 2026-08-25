using System;
using System.Threading;
using System.Threading.Tasks;
using Workforce.Modules.Approvals.Domain;
using Workforce.Modules.Approvals.Infrastructure;
using Workforce.Modules.Recruitment.Contracts;
using Workforce.Modules.Recruitment.Domain;
using Workforce.Modules.Recruitment.Infrastructure;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Recruitment.Services;

public class RecruitmentActionService : IRecruitmentActionContract
{
    private readonly IRecruitmentRepository _repository;
    private readonly IApprovalsRepository? _approvalsRepository;

    public RecruitmentActionService(
        IRecruitmentRepository repository,
        IApprovalsRepository? approvalsRepository = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _approvalsRepository = approvalsRepository;
    }

    public async Task<RecruitmentActionResult> MoveApplicationStageAsync(
        TenantId tenantId,
        UserId actorUserId,
        MoveApplicationStageCommand command,
        CancellationToken ct = default)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        var app = await _repository.GetApplicationByIdAsync(tenantId, command.ApplicationId, ct);
        if (app == null || (command.LegalEntityId.HasValue && app.LegalEntityId != command.LegalEntityId.Value))
        {
            return new RecruitmentActionResult(false, command.ApplicationId, 0, "Application not found or access denied.", false);
        }

        var fromStageId = app.CurrentStageId;
        try
        {
            app.MoveToStage(
                command.TargetStageId,
                actorUserId.Value,
                command.Reason,
                command.IdempotencyKey,
                command.ExpectedRowVersion
            );

            await _repository.UpdateApplicationAsync(app, ct);
            if (fromStageId != app.CurrentStageId)
            {
                await _repository.SaveOutboxMessageAsync(
                    tenantId,
                    "ApplicationStageChanged",
                    new ApplicationStageChangedEvent(app.Id, fromStageId, app.CurrentStageId, actorUserId.Value),
                    ct);
            }

            var updatedApp = await _repository.GetApplicationByIdAsync(tenantId, command.ApplicationId, ct);
            return new RecruitmentActionResult(
                true,
                app.Id,
                updatedApp?.RowVersion ?? (command.ExpectedRowVersion + 1),
                "Application moved to target stage successfully.",
                false);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrency conflict", StringComparison.OrdinalIgnoreCase))
        {
            return new RecruitmentActionResult(
                false,
                app.Id,
                app.RowVersion,
                "Concurrency conflict: application was updated by another process.",
                true);
        }
        catch (Exception ex)
        {
            return new RecruitmentActionResult(
                false,
                app.Id,
                app.RowVersion,
                ex.Message,
                false);
        }
    }

    public async Task<RecruitmentActionResult> SubmitRequisitionApprovalAsync(
        TenantId tenantId,
        UserId actorUserId,
        SubmitRequisitionApprovalCommand command,
        CancellationToken ct = default)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        var req = await _repository.GetRequisitionByIdAsync(tenantId, command.RequisitionId, ct);
        if (req == null || (command.LegalEntityId.HasValue && req.LegalEntityId != command.LegalEntityId.Value))
        {
            return new RecruitmentActionResult(false, command.RequisitionId, 0, "Requisition not found or access denied.", false);
        }

        try
        {
            var approvalId = Guid.NewGuid();
            if (_approvalsRepository != null)
            {
                var approvalReq = new ApprovalRequest(
                    approvalId,
                    tenantId,
                    req.LegalEntityId,
                    "RECRUITMENT",
                    req.Id,
                    "JOB_REQUISITION",
                    req.TitleEn,
                    actorUserId.Value,
                    Guid.Empty,
                    null,
                    1
                );
                await _approvalsRepository.SaveApprovalRequestAsync(approvalReq);
            }

            req.SubmitForApproval(approvalId, command.ExpectedRowVersion);
            await _repository.UpdateRequisitionAsync(req, ct);

            var updatedReq = await _repository.GetRequisitionByIdAsync(tenantId, command.RequisitionId, ct);
            return new RecruitmentActionResult(
                true,
                req.Id,
                updatedReq?.RowVersion ?? (command.ExpectedRowVersion + 1),
                "Job requisition submitted for approval successfully.",
                false);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrency conflict", StringComparison.OrdinalIgnoreCase))
        {
            return new RecruitmentActionResult(
                false,
                req.Id,
                req.RowVersion,
                "Concurrency conflict: job requisition was updated by another process.",
                true);
        }
        catch (Exception ex)
        {
            return new RecruitmentActionResult(
                false,
                req.Id,
                req.RowVersion,
                ex.Message,
                false);
        }
    }
}
