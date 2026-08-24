using System;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Leave.Domain;

public class LeavePolicy
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public Guid LeaveTypeId { get; private set; }
    public decimal AccrualRatePerYear { get; private set; }
    public decimal MaxCarryForwardDays { get; private set; }
    public int ProbationWaitDays { get; private set; }
    public EffectivePeriod EffectivePeriod { get; private set; }
    public int PolicyVersion { get; private set; }

    private LeavePolicy()
    {
        EffectivePeriod = new EffectivePeriod(DateOnly.FromDateTime(DateTime.UtcNow));
    }

    public LeavePolicy(
        Guid id,
        TenantId tenantId,
        Guid leaveTypeId,
        decimal accrualRatePerYear,
        decimal maxCarryForwardDays,
        int probationWaitDays,
        EffectivePeriod effectivePeriod,
        int policyVersion = 1)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (leaveTypeId == Guid.Empty) throw new ArgumentException("LeaveTypeId cannot be empty.", nameof(leaveTypeId));

        Id = id;
        TenantId = tenantId;
        LeaveTypeId = leaveTypeId;
        AccrualRatePerYear = accrualRatePerYear;
        MaxCarryForwardDays = maxCarryForwardDays;
        ProbationWaitDays = probationWaitDays;
        EffectivePeriod = effectivePeriod;
        PolicyVersion = policyVersion;
    }
}
