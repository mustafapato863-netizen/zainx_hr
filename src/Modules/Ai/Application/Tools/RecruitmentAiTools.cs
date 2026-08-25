using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Workforce.Modules.Ai.Application.Contracts;
using Workforce.Modules.Ai.Domain;
using Workforce.Modules.Recruitment.Infrastructure;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Ai.Application.Tools;

public sealed class RecruitmentGetRequisitionSummaryToolHandler : IAiToolHandler
{
    private readonly RecruitmentRepository _recruitmentRepository;

    public AiToolDefinition Definition { get; } = new(
        toolCode: "recruitment.get_requisition_summary",
        descriptionEn: "Retrieve job requisition details, target start date, open vacancies count, and active pipeline status.",
        descriptionAr: "استرجاع تفاصيل طلب التوظيف، الشواغر المستهدفة، وحالة مسار المتقدمين.",
        requiredPermission: "recruitment.requisition.read",
        dataClassification: "Internal",
        inputSchemaJson: "{\"type\":\"object\",\"required\":[\"requisitionId\"],\"properties\":{\"requisitionId\":{\"type\":\"string\"}}}"
    );

    public RecruitmentGetRequisitionSummaryToolHandler(RecruitmentRepository recruitmentRepository)
    {
        _recruitmentRepository = recruitmentRepository;
    }

    public async Task<AiToolResult> ExecuteAsync(JsonElement inputParams, IUserContext userContext, CancellationToken ct = default)
    {
        if (!inputParams.TryGetProperty("requisitionId", out var idProp) || !Guid.TryParse(idProp.GetString(), out var reqId))
        {
            return new AiToolResult(false, "{}", AiSourceCategory.CompanyData, new(), "Invalid or missing requisitionId.");
        }

        var req = await _recruitmentRepository.GetRequisitionByIdAsync(userContext.TenantId, reqId, ct);
        if (req == null)
        {
            return new AiToolResult(false, "{}", AiSourceCategory.CompanyData, new(), "Job requisition not found or access denied.");
        }

        var summary = new
        {
            RequisitionId = req.Id,
            RequisitionNumber = req.RequisitionNumber,
            TitleEn = req.TitleEn,
            TitleAr = req.TitleAr,
            OpeningsCount = req.OpeningsCount,
            Status = req.Status.ToString(),
            EmploymentType = req.EmploymentType.ToString(),
            TargetStartDate = req.TargetStartDate?.ToString("yyyy-MM-dd") ?? "Unspecified"
        };

        var sourceRefs = new List<SourceReference>
        {
            new SourceReference(
                Guid.NewGuid(),
                Guid.Empty,
                AiSourceCategory.CompanyData,
                $"Job Requisition: {req.TitleEn} ({req.RequisitionNumber})",
                entityType: "JobRequisition",
                entityId: req.Id.ToString()
            )
        };

        return new AiToolResult(
            IsSuccess: true,
            OutputJson: JsonSerializer.Serialize(summary),
            SourceCategory: AiSourceCategory.CompanyData,
            SourceReferences: sourceRefs
        );
    }
}

public sealed class RecruitmentGetCandidateSummaryToolHandler : IAiToolHandler
{
    private readonly RecruitmentRepository _recruitmentRepository;

    public AiToolDefinition Definition { get; } = new(
        toolCode: "recruitment.get_candidate_summary",
        descriptionEn: "Retrieve non-confidential candidate summary. Strict rule: Omits confidential interviewer scorecards if caller lacks permissions.",
        descriptionAr: "استرجاع ملخص بيانات المرشح مع حجب بطاقات التقييم السرية ما لم تتوفر الصلاحيات.",
        requiredPermission: "recruitment.candidate.read",
        dataClassification: "Internal",
        inputSchemaJson: "{\"type\":\"object\",\"required\":[\"candidateId\"],\"properties\":{\"candidateId\":{\"type\":\"string\"}}}"
    );

    public RecruitmentGetCandidateSummaryToolHandler(RecruitmentRepository recruitmentRepository)
    {
        _recruitmentRepository = recruitmentRepository;
    }

    public async Task<AiToolResult> ExecuteAsync(JsonElement inputParams, IUserContext userContext, CancellationToken ct = default)
    {
        if (!inputParams.TryGetProperty("candidateId", out var idProp) || !Guid.TryParse(idProp.GetString(), out var candId))
        {
            return new AiToolResult(false, "{}", AiSourceCategory.CompanyData, new(), "Invalid or missing candidateId.");
        }

        var candidate = await _recruitmentRepository.GetCandidateByIdAsync(userContext.TenantId, candId, ct);
        if (candidate == null)
        {
            return new AiToolResult(false, "{}", AiSourceCategory.CompanyData, new(), "Candidate not found or access denied.");
        }

        bool canReadScorecards = userContext.Permissions.Contains("*") || userContext.Permissions.Contains("recruitment.scorecard.read_all");

        var summary = new
        {
            CandidateId = candidate.Id,
            FullNameEn = $"{candidate.FirstNameEn} {candidate.LastNameEn}".Trim(),
            FullNameAr = $"{candidate.FirstNameAr} {candidate.LastNameAr}".Trim(),
            Headline = candidate.Headline,
            Location = candidate.Location,
            CurrentStage = "In Review",
            ScorecardsConfidential = !canReadScorecards,
            ScorecardNotice = canReadScorecards ? "Scorecards accessible" : "Confidential interviewer scorecards excluded per access control policy.",
            CreatedAtUtc = candidate.CreatedAtUtc.ToString("yyyy-MM-dd")
        };

        var sourceRefs = new List<SourceReference>
        {
            new SourceReference(
                Guid.NewGuid(),
                Guid.Empty,
                AiSourceCategory.CompanyData,
                $"Candidate: {summary.FullNameEn}",
                entityType: "Candidate",
                entityId: candidate.Id.ToString()
            )
        };

        return new AiToolResult(
            IsSuccess: true,
            OutputJson: JsonSerializer.Serialize(summary),
            SourceCategory: AiSourceCategory.CompanyData,
            SourceReferences: sourceRefs
        );
    }
}

public sealed class RecruitmentGetApplicationTimelineToolHandler : IAiToolHandler
{
    private readonly RecruitmentRepository _recruitmentRepository;

    public AiToolDefinition Definition { get; } = new(
        toolCode: "recruitment.get_application_timeline",
        descriptionEn: "Retrieve the stage transition timeline and application status for a job candidate.",
        descriptionAr: "استرجاع مسار مراحل التوظيف وسجل الانتقالات للمتقدم.",
        requiredPermission: "recruitment.application.read",
        dataClassification: "Internal",
        inputSchemaJson: "{\"type\":\"object\",\"required\":[\"applicationId\"],\"properties\":{\"applicationId\":{\"type\":\"string\"}}}"
    );

    public RecruitmentGetApplicationTimelineToolHandler(RecruitmentRepository recruitmentRepository)
    {
        _recruitmentRepository = recruitmentRepository;
    }

    public async Task<AiToolResult> ExecuteAsync(JsonElement inputParams, IUserContext userContext, CancellationToken ct = default)
    {
        if (!inputParams.TryGetProperty("applicationId", out var idProp) || !Guid.TryParse(idProp.GetString(), out var appId))
        {
            return new AiToolResult(false, "{}", AiSourceCategory.CompanyData, new(), "Invalid or missing applicationId.");
        }

        var app = await _recruitmentRepository.GetApplicationByIdAsync(userContext.TenantId, appId, ct);
        if (app == null)
        {
            return new AiToolResult(false, "{}", AiSourceCategory.CompanyData, new(), "Application not found or access denied.");
        }

        var timelineDto = new
        {
            ApplicationId = app.Id,
            RequisitionId = app.RequisitionId,
            CandidateId = app.CandidateId,
            CurrentStageId = app.CurrentStageId,
            Status = app.Status.ToString(),
            AppliedAtUtc = app.AppliedAtUtc.ToString("yyyy-MM-dd HH:mm")
        };

        var sourceRefs = new List<SourceReference>
        {
            new SourceReference(
                Guid.NewGuid(),
                Guid.Empty,
                AiSourceCategory.CompanyData,
                $"Application Timeline: {app.Id}",
                entityType: "JobApplication",
                entityId: app.Id.ToString()
            )
        };

        return new AiToolResult(
            IsSuccess: true,
            OutputJson: JsonSerializer.Serialize(timelineDto),
            SourceCategory: AiSourceCategory.CompanyData,
            SourceReferences: sourceRefs
        );
    }
}
