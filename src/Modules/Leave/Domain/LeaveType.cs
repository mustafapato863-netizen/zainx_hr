using System;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Leave.Domain;

public class LeaveType
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public LegalEntityId LegalEntityId { get; private set; }
    public string Code { get; private set; }
    public string NameEn { get; private set; }
    public string NameAr { get; private set; }
    public LeaveCategory Category { get; private set; }
    public bool IsPaid { get; private set; }
    public bool RequiresAttachment { get; private set; }
    public bool AllowHalfDay { get; private set; }
    public bool IsActive { get; private set; }

    private LeaveType() 
    {
        Code = string.Empty;
        NameEn = string.Empty;
        NameAr = string.Empty;
    }

    public LeaveType(
        Guid id,
        TenantId tenantId,
        LegalEntityId legalEntityId,
        string code,
        string nameEn,
        string nameAr,
        LeaveCategory category,
        bool isPaid = true,
        bool requiresAttachment = false,
        bool allowHalfDay = true,
        bool isActive = true)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(nameEn)) throw new ArgumentException("NameEn is required.", nameof(nameEn));

        Id = id;
        TenantId = tenantId;
        LegalEntityId = legalEntityId;
        Code = code.Trim().ToUpperInvariant();
        NameEn = nameEn.Trim();
        NameAr = nameAr.Trim();
        Category = category;
        IsPaid = isPaid;
        RequiresAttachment = requiresAttachment;
        AllowHalfDay = allowHalfDay;
        IsActive = isActive;
    }
}
