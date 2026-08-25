using System;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Leave.Domain;

public class LeaveBalance
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public Guid EmploymentId { get; private set; }
    public Guid LeaveTypeId { get; private set; }
    public int Year { get; private set; }
    public decimal EntitledDays { get; private set; }
    public decimal AccruedDays { get; private set; }
    public decimal UsedDays { get; private set; }
    public decimal PendingDays { get; private set; }
    public decimal AvailableDays => AccruedDays + EntitledDays - UsedDays - PendingDays;
    public DateTime UpdatedAt { get; private set; }
    public uint RowVersion { get; private set; }

    private LeaveBalance() { }

    internal static LeaveBalance Rehydrate(
        Guid id,
        TenantId tenantId,
        Guid employmentId,
        Guid leaveTypeId,
        int year,
        decimal entitledDays,
        decimal accruedDays,
        decimal usedDays,
        decimal pendingDays,
        DateTime updatedAt,
        uint rowVersion)
    {
        return new LeaveBalance
        {
            Id = id,
            TenantId = tenantId,
            EmploymentId = employmentId,
            LeaveTypeId = leaveTypeId,
            Year = year,
            EntitledDays = entitledDays,
            AccruedDays = accruedDays,
            UsedDays = usedDays,
            PendingDays = pendingDays,
            UpdatedAt = updatedAt,
            RowVersion = rowVersion
        };
    }

    public LeaveBalance(
        Guid id,
        TenantId tenantId,
        Guid employmentId,
        Guid leaveTypeId,
        int year,
        decimal entitledDays,
        decimal accruedDays = 0,
        decimal usedDays = 0,
        decimal pendingDays = 0)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (employmentId == Guid.Empty) throw new ArgumentException("EmploymentId cannot be empty.", nameof(employmentId));
        if (leaveTypeId == Guid.Empty) throw new ArgumentException("LeaveTypeId cannot be empty.", nameof(leaveTypeId));

        Id = id;
        TenantId = tenantId;
        EmploymentId = employmentId;
        LeaveTypeId = leaveTypeId;
        Year = year;
        EntitledDays = entitledDays;
        AccruedDays = accruedDays;
        UsedDays = usedDays;
        PendingDays = pendingDays;
        UpdatedAt = DateTime.UtcNow;
        RowVersion = 1;
    }

    public void ReservePendingDays(decimal days, uint expectedRowVersion)
    {
        VerifyRowVersion(expectedRowVersion);
        if (days <= 0) throw new ArgumentException("Days to reserve must be greater than zero.", nameof(days));
        if (AvailableDays < days)
        {
            throw new InvalidOperationException($"Insufficient leave balance. Requested: {days}, Available: {AvailableDays}.");
        }

        PendingDays += days;
        UpdatedAt = DateTime.UtcNow;
        RowVersion++;
    }

    public void ReleasePendingDays(decimal days, uint expectedRowVersion)
    {
        VerifyRowVersion(expectedRowVersion);
        PendingDays = Math.Max(0, PendingDays - days);
        UpdatedAt = DateTime.UtcNow;
        RowVersion++;
    }

    public void ConfirmApprovedDays(decimal days, uint expectedRowVersion)
    {
        VerifyRowVersion(expectedRowVersion);
        PendingDays = Math.Max(0, PendingDays - days);
        UsedDays += days;
        UpdatedAt = DateTime.UtcNow;
        RowVersion++;
    }

    public void CancelApprovedDays(decimal days, uint expectedRowVersion)
    {
        VerifyRowVersion(expectedRowVersion);
        if (days <= 0) throw new ArgumentException("Days to cancel must be greater than zero.", nameof(days));
        if (UsedDays < days)
        {
            throw new InvalidOperationException($"Cannot cancel more approved leave than has been used. Requested: {days}, Used: {UsedDays}.");
        }

        UsedDays -= days;
        UpdatedAt = DateTime.UtcNow;
        RowVersion++;
    }

    public void AdjustBalance(decimal amountDays, string reason, uint expectedRowVersion)
    {
        VerifyRowVersion(expectedRowVersion);
        EntitledDays += amountDays;
        UpdatedAt = DateTime.UtcNow;
        RowVersion++;
    }

    private void VerifyRowVersion(uint expected)
    {
        if (expected != RowVersion)
        {
            throw new InvalidOperationException("Optimistic concurrency conflict on leave balance.");
        }
    }
}
