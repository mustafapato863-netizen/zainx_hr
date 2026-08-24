using System;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Compliance.Domain;

public class StatutoryRuleVersion
{
    public Guid Id { get; private set; }
    public Guid RuleId { get; private set; }
    public int VersionNumber { get; private set; }
    public EffectivePeriod EffectivePeriod { get; private set; }
    public string ParametersJson { get; private set; }
    public string CalculationStrategyName { get; private set; }
    public VerificationStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private StatutoryRuleVersion()
    {
        ParametersJson = "{}";
        CalculationStrategyName = string.Empty;
        EffectivePeriod = new EffectivePeriod(DateOnly.FromDateTime(DateTime.UtcNow));
    }

    public StatutoryRuleVersion(
        Guid id,
        Guid ruleId,
        int versionNumber,
        EffectivePeriod effectivePeriod,
        string parametersJson,
        string calculationStrategyName,
        VerificationStatus status = VerificationStatus.Verified)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (ruleId == Guid.Empty) throw new ArgumentException("RuleId cannot be empty.", nameof(ruleId));
        if (versionNumber <= 0) throw new ArgumentException("VersionNumber must be positive.", nameof(versionNumber));
        if (string.IsNullOrWhiteSpace(calculationStrategyName)) throw new ArgumentException("CalculationStrategyName is required.", nameof(calculationStrategyName));

        Id = id;
        RuleId = ruleId;
        VersionNumber = versionNumber;
        EffectivePeriod = effectivePeriod;
        ParametersJson = string.IsNullOrWhiteSpace(parametersJson) ? "{}" : parametersJson.Trim();
        CalculationStrategyName = calculationStrategyName.Trim();
        Status = status;
        CreatedAtUtc = DateTime.UtcNow;
    }
}
