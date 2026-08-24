using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Workforce.Modules.Recruitment.Domain;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Recruitment.Infrastructure;

public record PagedRecruitmentResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public record DuplicateCandidateMatchDto(
    Guid CandidateId,
    string FirstNameEn,
    string LastNameEn,
    string Email,
    string PhoneNumber,
    string MatchType
);

public interface IRecruitmentRepository
{
    // Requisitions
    Task CreateRequisitionAsync(JobRequisition requisition, CancellationToken ct = default);
    Task UpdateRequisitionAsync(JobRequisition requisition, CancellationToken ct = default);
    Task<JobRequisition?> GetRequisitionByIdAsync(TenantId tenantId, Guid requisitionId, CancellationToken ct = default);
    Task<PagedRecruitmentResult<JobRequisition>> QueryRequisitionsAsync(
        TenantId tenantId,
        LegalEntityId? legalEntityId,
        RequisitionStatus? status,
        string? search,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    // Pipelines
    Task<RecruitmentPipeline?> GetPipelineWithVersionsAsync(TenantId tenantId, Guid pipelineId, CancellationToken ct = default);
    Task<RecruitmentPipelineVersion?> GetPipelineVersionWithStagesAsync(Guid pipelineVersionId, CancellationToken ct = default);
    Task<RecruitmentPipelineVersion?> GetDefaultPipelineVersionAsync(TenantId tenantId, CancellationToken ct = default);
    Task CreatePipelineAsync(RecruitmentPipeline pipeline, CancellationToken ct = default);
    Task CreatePipelineVersionAsync(RecruitmentPipelineVersion version, CancellationToken ct = default);

    // Candidates
    Task CreateCandidateAsync(Candidate candidate, CancellationToken ct = default);
    Task UpdateCandidateAsync(Candidate candidate, CancellationToken ct = default);
    Task<Candidate?> GetCandidateByIdAsync(TenantId tenantId, Guid candidateId, CancellationToken ct = default);
    Task<PagedRecruitmentResult<Candidate>> QueryCandidatesAsync(
        TenantId tenantId,
        string? search,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);
    Task<IReadOnlyList<DuplicateCandidateMatchDto>> FindPotentialDuplicatesAsync(
        TenantId tenantId,
        string email,
        string phoneNumber,
        Guid? excludeCandidateId = null,
        CancellationToken ct = default);

    // Applications
    Task CreateApplicationAsync(Application application, CancellationToken ct = default);
    Task UpdateApplicationAsync(Application application, CancellationToken ct = default);
    Task<Application?> GetApplicationByIdAsync(TenantId tenantId, Guid applicationId, CancellationToken ct = default);
    Task<Application?> GetActiveApplicationForCandidateAsync(TenantId tenantId, Guid requisitionId, Guid candidateId, CancellationToken ct = default);
    Task<PagedRecruitmentResult<Application>> QueryApplicationsAsync(
        TenantId tenantId,
        Guid? requisitionId,
        Guid? candidateId,
        Guid? stageId,
        ApplicationStatus? status,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);
    Task<IReadOnlyList<Application>> GetPipelineBoardApplicationsAsync(TenantId tenantId, Guid requisitionId, CancellationToken ct = default);

    // Interviews
    Task CreateInterviewAsync(Interview interview, CancellationToken ct = default);
    Task UpdateInterviewAsync(Interview interview, CancellationToken ct = default);
    Task<Interview?> GetInterviewByIdAsync(TenantId tenantId, Guid interviewId, CancellationToken ct = default);
    Task<IReadOnlyList<Interview>> GetInterviewsForApplicationAsync(TenantId tenantId, Guid applicationId, CancellationToken ct = default);
    Task<IReadOnlyList<Interview>> QueryInterviewsAsync(
        TenantId tenantId,
        DateTime startUtc,
        DateTime endUtc,
        Guid? interviewerUserId = null,
        CancellationToken ct = default);
    Task SaveScorecardSubmissionAsync(ScorecardSubmission scorecard, CancellationToken ct = default);
    Task<IReadOnlyList<ScorecardSubmission>> GetScorecardsForInterviewAsync(Guid interviewId, CancellationToken ct = default);

    // Offers
    Task CreateOfferAsync(Offer offer, CancellationToken ct = default);
    Task UpdateOfferAsync(Offer offer, CancellationToken ct = default);
    Task<Offer?> GetOfferByIdAsync(TenantId tenantId, Guid offerId, CancellationToken ct = default);
    Task<IReadOnlyList<Offer>> GetOffersForApplicationAsync(TenantId tenantId, Guid applicationId, CancellationToken ct = default);
    Task<Offer?> GetLatestOfferForApplicationAsync(TenantId tenantId, Guid applicationId, CancellationToken ct = default);

    // Outbox
    Task SaveOutboxMessageAsync(TenantId tenantId, string eventType, object payload, CancellationToken ct = default);
}
