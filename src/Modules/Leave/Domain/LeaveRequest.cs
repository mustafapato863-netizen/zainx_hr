using System;
using System.Collections.Generic;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Leave.Domain;

public class LeaveRequest
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public LegalEntityId LegalEntityId { get; private set; }
    public Guid EmploymentId { get; private set; }
    public Guid LeaveTypeId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public decimal DurationDays { get; private set; }
    public int DurationMinutes { get; private set; }
    public LeaveRequestStatus Status { get; private set; }
    public string Reason { get; private set; }
    public Guid? AttachmentDocumentId { get; private set; }
    public Guid? ApprovalRequestId { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public uint RowVersion { get; private set; }

    public IReadOnlyList<LeaveYearSegment> GetYearSegments()
    {
        var segments = new List<LeaveYearSegment>();
        var cursor = StartDate;
        while (cursor <= EndDate)
        {
            var segmentEnd = new DateOnly(cursor.Year, 12, 31);
            if (segmentEnd > EndDate)
                segmentEnd = EndDate;

            segments.Add(new LeaveYearSegment(
                cursor.Year,
                cursor,
                segmentEnd,
                segmentEnd.DayNumber - cursor.DayNumber + 1));

            cursor = segmentEnd.AddDays(1);
        }

        return segments;
    }

    private LeaveRequest()
    {
        Reason = string.Empty;
    }

    internal static LeaveRequest Rehydrate(
        Guid id,
        TenantId tenantId,
        LegalEntityId legalEntityId,
        Guid employmentId,
        Guid leaveTypeId,
        DateOnly startDate,
        DateOnly endDate,
        decimal durationDays,
        string reason,
        Guid? attachmentDocumentId,
        LeaveRequestStatus status,
        Guid? approvalRequestId,
        string? rejectionReason,
        DateTime createdAt,
        DateTime updatedAt,
        uint rowVersion)
    {
        return new LeaveRequest
        {
            Id = id,
            TenantId = tenantId,
            LegalEntityId = legalEntityId,
            EmploymentId = employmentId,
            LeaveTypeId = leaveTypeId,
            StartDate = startDate,
            EndDate = endDate,
            DurationDays = durationDays,
            DurationMinutes = (int)(durationDays * 480),
            Reason = reason,
            AttachmentDocumentId = attachmentDocumentId,
            Status = status,
            ApprovalRequestId = approvalRequestId,
            RejectionReason = rejectionReason,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            RowVersion = rowVersion
        };
    }

    public LeaveRequest(
        Guid id,
        TenantId tenantId,
        LegalEntityId legalEntityId,
        Guid employmentId,
        Guid leaveTypeId,
        DateOnly startDate,
        DateOnly endDate,
        decimal durationDays,
        string reason,
        Guid? attachmentDocumentId = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (employmentId == Guid.Empty) throw new ArgumentException("EmploymentId cannot be empty.", nameof(employmentId));
        if (leaveTypeId == Guid.Empty) throw new ArgumentException("LeaveTypeId cannot be empty.", nameof(leaveTypeId));
        if (endDate < startDate) throw new ArgumentException("End date cannot be earlier than start date.", nameof(endDate));
        if (durationDays <= 0) throw new ArgumentException("Duration days must be greater than zero.", nameof(durationDays));

        Id = id;
        TenantId = tenantId;
        LegalEntityId = legalEntityId;
        EmploymentId = employmentId;
        LeaveTypeId = leaveTypeId;
        StartDate = startDate;
        EndDate = endDate;
        DurationDays = durationDays;
        DurationMinutes = (int)(durationDays * 480); // Standard 8hr day in integer minutes
        Status = LeaveRequestStatus.Draft;
        Reason = reason.Trim();
        AttachmentDocumentId = attachmentDocumentId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        RowVersion = 1;
    }

    public void Submit(Guid approvalRequestId, uint expectedRowVersion)
    {
        VerifyRowVersion(expectedRowVersion);
        if (Status != LeaveRequestStatus.Draft)
        {
            throw new InvalidOperationException("Only draft requests can be submitted.");
        }

        ApprovalRequestId = approvalRequestId;
        Status = LeaveRequestStatus.PendingApproval;
        UpdatedAt = DateTime.UtcNow;
        RowVersion++;
    }

    public void Approve(uint expectedRowVersion)
    {
        VerifyRowVersion(expectedRowVersion);
        if (Status != LeaveRequestStatus.PendingApproval && Status != LeaveRequestStatus.Submitted)
        {
            throw new InvalidOperationException("Only pending requests can be approved.");
        }

        Status = LeaveRequestStatus.Approved;
        UpdatedAt = DateTime.UtcNow;
        RowVersion++;
    }

    public void Reject(string reason, uint expectedRowVersion)
    {
        VerifyRowVersion(expectedRowVersion);
        if (Status != LeaveRequestStatus.PendingApproval && Status != LeaveRequestStatus.Submitted)
        {
            throw new InvalidOperationException("Only pending requests can be rejected.");
        }

        Status = LeaveRequestStatus.Rejected;
        RejectionReason = string.IsNullOrWhiteSpace(reason) ? "Rejected by approver." : reason.Trim();
        UpdatedAt = DateTime.UtcNow;
        RowVersion++;
    }

    public void Cancel(uint expectedRowVersion)
    {
        VerifyRowVersion(expectedRowVersion);
        if (Status == LeaveRequestStatus.Cancelled || Status == LeaveRequestStatus.Rejected)
        {
            throw new InvalidOperationException("Cannot cancel an already rejected or cancelled request.");
        }

        Status = LeaveRequestStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
        RowVersion++;
    }

    private void VerifyRowVersion(uint expected)
    {
        if (expected != RowVersion)
        {
            throw new InvalidOperationException("Optimistic concurrency conflict on leave request.");
        }
    }
}

public sealed record LeaveYearSegment(
    int Year,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal Days);
