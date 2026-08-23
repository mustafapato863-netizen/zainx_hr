namespace Workforce.Modules.Organization.Domain;

public record EffectivePeriod
{
    public DateOnly EffectiveFrom { get; init; }
    public DateOnly? EffectiveTo { get; init; }

    public EffectivePeriod(DateOnly effectiveFrom, DateOnly? effectiveTo = null)
    {
        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
        {
            throw new ArgumentException("EffectiveTo date cannot be earlier than EffectiveFrom date.");
        }

        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
    }

    public bool IsActiveAt(DateOnly date)
    {
        return date >= EffectiveFrom && (!EffectiveTo.HasValue || date <= EffectiveTo.Value);
    }
}
