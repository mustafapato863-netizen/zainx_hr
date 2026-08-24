using System;
using System.Collections.Generic;

namespace Workforce.Modules.Compliance.Domain;

public class StatutoryRule
{
    public Guid Id { get; private set; }
    public Jurisdiction Jurisdiction { get; private set; }
    public RuleCategory Category { get; private set; }
    public string Code { get; private set; }
    public string NameEn { get; private set; }
    public string NameAr { get; private set; }
    public string SourceReferenceLaw { get; private set; }
    public bool IsVerified { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private readonly List<StatutoryRuleVersion> _versions = new();
    public IReadOnlyCollection<StatutoryRuleVersion> Versions => _versions.AsReadOnly();

    private StatutoryRule()
    {
        Code = string.Empty;
        NameEn = string.Empty;
        NameAr = string.Empty;
        SourceReferenceLaw = string.Empty;
    }

    public StatutoryRule(
        Guid id,
        Jurisdiction jurisdiction,
        RuleCategory category,
        string code,
        string nameEn,
        string nameAr,
        string sourceReferenceLaw,
        bool isVerified = true)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(nameEn)) throw new ArgumentException("NameEn is required.", nameof(nameEn));
        if (string.IsNullOrWhiteSpace(sourceReferenceLaw)) throw new ArgumentException("SourceReferenceLaw is required.", nameof(sourceReferenceLaw));

        Id = id;
        Jurisdiction = jurisdiction;
        Category = category;
        Code = code.Trim().ToUpperInvariant();
        NameEn = nameEn.Trim();
        NameAr = nameAr.Trim();
        SourceReferenceLaw = sourceReferenceLaw.Trim();
        IsVerified = isVerified;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void AddVersion(StatutoryRuleVersion version)
    {
        _versions.Add(version);
    }
}
