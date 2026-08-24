using System;

namespace Workforce.Modules.Payroll.Domain;

public class PayrollLine
{
    public Guid Id { get; private set; }
    public Guid EmployeeResultId { get; private set; }
    public string ComponentCode { get; private set; }
    public string NameEn { get; private set; }
    public string NameAr { get; private set; }
    public ComponentCategory Category { get; private set; }
    public decimal Amount { get; private set; }
    public CalculationType CalculationType { get; private set; }
    public decimal Rate { get; private set; }
    public decimal HoursOrDays { get; private set; }
    public Guid? TraceId { get; private set; }

    private PayrollLine()
    {
        ComponentCode = string.Empty;
        NameEn = string.Empty;
        NameAr = string.Empty;
    }

    public PayrollLine(
        Guid id,
        Guid employeeResultId,
        string componentCode,
        string nameEn,
        string nameAr,
        ComponentCategory category,
        decimal amount,
        CalculationType calculationType,
        decimal rate = 0,
        decimal hoursOrDays = 0,
        Guid? traceId = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(componentCode)) throw new ArgumentException("ComponentCode is required.", nameof(componentCode));
        if (string.IsNullOrWhiteSpace(nameEn)) throw new ArgumentException("NameEn is required.", nameof(nameEn));

        Id = id;
        EmployeeResultId = employeeResultId;
        ComponentCode = componentCode.Trim().ToUpperInvariant();
        NameEn = nameEn.Trim();
        NameAr = nameAr.Trim();
        Category = category;
        Amount = RoundingPolicy.RoundLine(amount);
        CalculationType = calculationType;
        Rate = rate;
        HoursOrDays = hoursOrDays;
        TraceId = traceId;
    }
}
