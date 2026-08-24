using System;

namespace Workforce.Modules.Payroll.Domain;

public static class RoundingPolicy
{
    public const int DefaultDecimals = 2;
    public const int IntermediateDecimals = 4;

    public static decimal RoundLine(decimal amount, int decimals = DefaultDecimals)
    {
        return Math.Round(amount, decimals, MidpointRounding.AwayFromZero);
    }

    public static decimal RoundIntermediate(decimal amount)
    {
        return Math.Round(amount, IntermediateDecimals, MidpointRounding.AwayFromZero);
    }
}
