using System;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Ai.Domain;

/// <summary>
/// Effective-dated company policy entity with strict versioning.
/// </summary>
public sealed class CompanyPolicy
{
    public Guid Id { get; }
    public TenantId TenantId { get; }
    public string PolicyCode { get; }
    public string TitleEn { get; }
    public string TitleAr { get; }
    public int Version { get; }
    public DateTime EffectiveFromUtc { get; }
    public DateTime? EffectiveToUtc { get; }
    public string ContentEn { get; }
    public string ContentAr { get; }
    public string Classification { get; }
    public bool IsActive { get; }

    public CompanyPolicy(
        Guid id,
        TenantId tenantId,
        string policyCode,
        string titleEn,
        string titleAr,
        int version,
        DateTime effectiveFromUtc,
        DateTime? effectiveToUtc,
        string contentEn,
        string contentAr,
        string classification = "Internal",
        bool isActive = true)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        TenantId = tenantId;
        PolicyCode = policyCode ?? throw new ArgumentNullException(nameof(policyCode));
        TitleEn = titleEn ?? throw new ArgumentNullException(nameof(titleEn));
        TitleAr = titleAr ?? throw new ArgumentNullException(nameof(titleAr));
        Version = version <= 0 ? 1 : version;
        EffectiveFromUtc = effectiveFromUtc;
        EffectiveToUtc = effectiveToUtc;
        ContentEn = contentEn ?? string.Empty;
        ContentAr = contentAr ?? string.Empty;
        Classification = classification;
        IsActive = isActive;
    }

    /// <summary>
    /// Evaluates whether this policy version was effective at the given timestamp.
    /// </summary>
    public bool IsEffectiveAt(DateTime targetUtc)
    {
        if (targetUtc < EffectiveFromUtc) return false;
        if (EffectiveToUtc.HasValue && targetUtc > EffectiveToUtc.Value) return false;
        return true;
    }
}
