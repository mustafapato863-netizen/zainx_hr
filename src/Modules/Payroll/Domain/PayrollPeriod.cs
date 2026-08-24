using System;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Payroll.Domain;

public class PayrollPeriod
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public LegalEntityId LegalEntityId { get; private set; }
    public string Code { get; private set; }
    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }
    public DateOnly PaymentDate { get; private set; }
    public bool IsActive { get; private set; }

    private PayrollPeriod()
    {
        Code = string.Empty;
    }

    public PayrollPeriod(
        Guid id,
        TenantId tenantId,
        LegalEntityId legalEntityId,
        string code,
        DateOnly periodStart,
        DateOnly periodEnd,
        DateOnly paymentDate,
        bool isActive = true)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code is required.", nameof(code));
        if (periodEnd < periodStart) throw new ArgumentException("PeriodEnd cannot precede PeriodStart.", nameof(periodEnd));

        Id = id;
        TenantId = tenantId;
        LegalEntityId = legalEntityId;
        Code = code.Trim().ToUpperInvariant();
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        PaymentDate = paymentDate;
        IsActive = isActive;
    }
}
