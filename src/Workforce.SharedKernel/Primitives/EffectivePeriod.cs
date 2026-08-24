using System;

namespace Workforce.SharedKernel.Primitives;

public record EffectivePeriod
{
    public DateOnly EffectiveFrom { get; init; }
    public DateOnly? EffectiveTo { get; init; }

    public EffectivePeriod(DateOnly effectiveFrom, DateOnly? effectiveTo = null)
    {
        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
        {
            throw new ArgumentException("EffectiveTo date cannot be earlier than EffectiveFrom date.", nameof(effectiveTo));
        }

        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
    }

    public bool IsActiveOn(DateOnly date) => IsActiveAt(date);

    public bool IsActiveAt(DateOnly date)
    {
        if (date < EffectiveFrom) return false;
        if (EffectiveTo.HasValue && date > EffectiveTo.Value) return false;
        return true;
    }

    public bool OverlapsWith(EffectivePeriod other)
    {
        var otherEnd = other.EffectiveTo ?? DateOnly.MaxValue;
        var thisEnd = EffectiveTo ?? DateOnly.MaxValue;
        return EffectiveFrom <= otherEnd && other.EffectiveFrom <= thisEnd;
    }
}
