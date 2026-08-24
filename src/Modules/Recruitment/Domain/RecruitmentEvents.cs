using System;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Recruitment.Domain;

public record RequisitionApprovedEvent(Guid RequisitionId, Guid TenantId, Guid LegalEntityId, string TitleEn);
public record RequisitionOpenedEvent(Guid RequisitionId, Guid TenantId, Guid LegalEntityId, string TitleEn);
public record ApplicationCreatedEvent(Guid ApplicationId, Guid RequisitionId, Guid CandidateId, Guid TenantId);
public record ApplicationStageChangedEvent(Guid ApplicationId, Guid FromStageId, Guid ToStageId, Guid ChangedByUserId);
public record ApplicationRejectedEvent(Guid ApplicationId, string ReasonCode, Guid ChangedByUserId);
public record InterviewScheduledEvent(Guid InterviewId, Guid ApplicationId, DateTime ScheduledStartUtc, string Timezone);
public record InterviewCompletedEvent(Guid InterviewId, Guid ApplicationId);
public record ScorecardSubmittedEvent(Guid ScorecardId, Guid InterviewId, Guid InterviewerUserId, ScorecardRecommendation Recommendation);
public record OfferApprovedEvent(Guid OfferId, Guid ApplicationId, Guid CandidateId);
public record OfferIssuedEvent(Guid OfferId, Guid ApplicationId, Guid CandidateId);
public record OfferAcceptedEvent(Guid OfferId, Guid ApplicationId, Guid CandidateId);
public record CandidateHiredEvent(Guid ApplicationId, Guid CandidateId, Guid PersonId, Guid EmploymentId, Guid TenantId, Guid LegalEntityId);
