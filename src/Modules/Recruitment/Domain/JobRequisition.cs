using System;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Recruitment.Domain;

public class JobRequisition
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public LegalEntityId LegalEntityId { get; private set; }
    public Guid OrganizationUnitId { get; private set; }
    public Guid? PositionId { get; private set; }
    public Guid? LocationId { get; private set; }
    public Guid HiringManagerId { get; private set; }
    public Guid RecruiterId { get; private set; }
    public string RequisitionNumber { get; private set; }
    public string TitleEn { get; private set; }
    public string TitleAr { get; private set; }
    public int OpeningsCount { get; private set; }
    public string EmploymentType { get; private set; }
    public Guid PipelineId { get; private set; }
    public int PipelineVersion { get; private set; }
    public RequisitionStatus Status { get; private set; }
    public Guid? ApprovalRequestId { get; private set; }
    public string? RequisitionReason { get; private set; }
    public DateOnly? TargetStartDate { get; private set; }
    public DateTime? OpenedAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public uint RowVersion { get; private set; }

    private JobRequisition()
    {
        TenantId = default;
        LegalEntityId = default;
        RequisitionNumber = string.Empty;
        TitleEn = string.Empty;
        TitleAr = string.Empty;
        EmploymentType = string.Empty;
    }

    public JobRequisition(
        Guid id,
        TenantId tenantId,
        LegalEntityId legalEntityId,
        Guid organizationUnitId,
        Guid? positionId,
        Guid? locationId,
        Guid hiringManagerId,
        Guid recruiterId,
        string requisitionNumber,
        string titleEn,
        string titleAr,
        int openingsCount,
        string employmentType,
        Guid pipelineId,
        int pipelineVersion,
        string? requisitionReason = null,
        DateOnly? targetStartDate = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (tenantId == default || tenantId.Value == Guid.Empty) throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));
        if (legalEntityId == default || legalEntityId.Value == Guid.Empty) throw new ArgumentException("LegalEntityId cannot be empty.", nameof(legalEntityId));
        if (organizationUnitId == Guid.Empty) throw new ArgumentException("OrganizationUnitId cannot be empty.", nameof(organizationUnitId));
        if (hiringManagerId == Guid.Empty) throw new ArgumentException("HiringManagerId cannot be empty.", nameof(hiringManagerId));
        if (recruiterId == Guid.Empty) throw new ArgumentException("RecruiterId cannot be empty.", nameof(recruiterId));
        if (string.IsNullOrWhiteSpace(titleEn)) throw new ArgumentException("English title is required.", nameof(titleEn));
        if (string.IsNullOrWhiteSpace(titleAr)) throw new ArgumentException("Arabic title is required.", nameof(titleAr));
        if (openingsCount <= 0) throw new ArgumentException("Openings count must be at least 1.", nameof(openingsCount));

        Id = id;
        TenantId = tenantId;
        LegalEntityId = legalEntityId;
        OrganizationUnitId = organizationUnitId;
        PositionId = positionId;
        LocationId = locationId;
        HiringManagerId = hiringManagerId;
        RecruiterId = recruiterId;
        RequisitionNumber = requisitionNumber.Trim();
        TitleEn = titleEn.Trim();
        TitleAr = titleAr.Trim();
        OpeningsCount = openingsCount;
        EmploymentType = string.IsNullOrWhiteSpace(employmentType) ? "FullTime" : employmentType.Trim();
        PipelineId = pipelineId;
        PipelineVersion = pipelineVersion;
        RequisitionReason = requisitionReason?.Trim();
        TargetStartDate = targetStartDate;
        Status = RequisitionStatus.Draft;
        CreatedAtUtc = DateTime.UtcNow;
        RowVersion = 1;
    }

    public static JobRequisition Reconstitute(
        Guid id,
        TenantId tenantId,
        LegalEntityId legalEntityId,
        Guid organizationUnitId,
        Guid? positionId,
        Guid? locationId,
        Guid hiringManagerId,
        Guid recruiterId,
        string requisitionNumber,
        string titleEn,
        string titleAr,
        int openingsCount,
        string employmentType,
        Guid pipelineId,
        int pipelineVersion,
        RequisitionStatus status,
        Guid? approvalRequestId,
        string? requisitionReason,
        DateOnly? targetStartDate,
        DateTime? openedAtUtc,
        DateTime? closedAtUtc,
        DateTime createdAtUtc,
        uint rowVersion)
    {
        return new JobRequisition
        {
            Id = id,
            TenantId = tenantId,
            LegalEntityId = legalEntityId,
            OrganizationUnitId = organizationUnitId,
            PositionId = positionId,
            LocationId = locationId,
            HiringManagerId = hiringManagerId,
            RecruiterId = recruiterId,
            RequisitionNumber = requisitionNumber,
            TitleEn = titleEn,
            TitleAr = titleAr,
            OpeningsCount = openingsCount,
            EmploymentType = employmentType,
            PipelineId = pipelineId,
            PipelineVersion = pipelineVersion,
            Status = status,
            ApprovalRequestId = approvalRequestId,
            RequisitionReason = requisitionReason,
            TargetStartDate = targetStartDate,
            OpenedAtUtc = openedAtUtc,
            ClosedAtUtc = closedAtUtc,
            CreatedAtUtc = createdAtUtc,
            RowVersion = rowVersion
        };
    }

    public void UpdateDetails(
        string titleEn,
        string titleAr,
        int openingsCount,
        string employmentType,
        Guid organizationUnitId,
        Guid? positionId,
        Guid? locationId,
        Guid hiringManagerId,
        Guid recruiterId,
        DateOnly? targetStartDate,
        string? requisitionReason,
        uint expectedRowVersion)
    {
        ValidateConcurrency(expectedRowVersion);
        if (Status != RequisitionStatus.Draft)
            throw new InvalidOperationException($"Cannot edit requisition details in status '{Status}'. Only 'Draft' requisitions can be edited.");

        if (string.IsNullOrWhiteSpace(titleEn)) throw new ArgumentException("English title is required.", nameof(titleEn));
        if (string.IsNullOrWhiteSpace(titleAr)) throw new ArgumentException("Arabic title is required.", nameof(titleAr));
        if (openingsCount <= 0) throw new ArgumentException("Openings count must be at least 1.", nameof(openingsCount));

        TitleEn = titleEn.Trim();
        TitleAr = titleAr.Trim();
        OpeningsCount = openingsCount;
        EmploymentType = employmentType.Trim();
        OrganizationUnitId = organizationUnitId;
        PositionId = positionId;
        LocationId = locationId;
        HiringManagerId = hiringManagerId;
        RecruiterId = recruiterId;
        TargetStartDate = targetStartDate;
        RequisitionReason = requisitionReason?.Trim();
        RowVersion++;
    }

    public void SubmitForApproval(Guid approvalRequestId, uint expectedRowVersion)
    {
        ValidateConcurrency(expectedRowVersion);
        if (Status != RequisitionStatus.Draft)
            throw new InvalidOperationException($"Cannot submit requisition in status '{Status}'. Must be in 'Draft' status.");

        Status = RequisitionStatus.PendingApproval;
        ApprovalRequestId = approvalRequestId;
        RowVersion++;
    }

    public void Approve(uint expectedRowVersion)
    {
        ValidateConcurrency(expectedRowVersion);
        if (Status != RequisitionStatus.PendingApproval)
            throw new InvalidOperationException($"Cannot approve requisition in status '{Status}'. Must be 'PendingApproval'.");

        Status = RequisitionStatus.Approved;
        RowVersion++;
    }

    public void Open(uint expectedRowVersion)
    {
        ValidateConcurrency(expectedRowVersion);
        if (Status != RequisitionStatus.Approved && Status != RequisitionStatus.OnHold)
            throw new InvalidOperationException($"Cannot open requisition in status '{Status}'. Must be 'Approved' or 'OnHold'.");

        Status = RequisitionStatus.Open;
        OpenedAtUtc ??= DateTime.UtcNow;
        RowVersion++;
    }

    public void PutOnHold(uint expectedRowVersion)
    {
        ValidateConcurrency(expectedRowVersion);
        if (Status != RequisitionStatus.Open)
            throw new InvalidOperationException($"Cannot put requisition on hold from status '{Status}'. Must be 'Open'.");

        Status = RequisitionStatus.OnHold;
        RowVersion++;
    }

    public void Close(uint expectedRowVersion)
    {
        ValidateConcurrency(expectedRowVersion);
        if (Status != RequisitionStatus.Open && Status != RequisitionStatus.OnHold)
            throw new InvalidOperationException($"Cannot close requisition in status '{Status}'. Must be 'Open' or 'OnHold'.");

        Status = RequisitionStatus.Closed;
        ClosedAtUtc = DateTime.UtcNow;
        RowVersion++;
    }

    public void Cancel(uint expectedRowVersion)
    {
        ValidateConcurrency(expectedRowVersion);
        if (Status == RequisitionStatus.Closed || Status == RequisitionStatus.Cancelled)
            throw new InvalidOperationException($"Cannot cancel requisition in terminal status '{Status}'.");

        Status = RequisitionStatus.Cancelled;
        ClosedAtUtc = DateTime.UtcNow;
        RowVersion++;
    }

    private void ValidateConcurrency(uint expectedRowVersion)
    {
        if (RowVersion != expectedRowVersion)
        {
            throw new InvalidOperationException($"Concurrency conflict: Requisition has been modified. Expected version {expectedRowVersion}, current version {RowVersion}.");
        }
    }
}
