using System;
using System.Collections.Generic;
using System.Linq;

namespace Workforce.Modules.Payroll.Domain;

public class PayrollEmployeeResult
{
    public Guid Id { get; private set; }
    public Guid PayrollRunId { get; private set; }
    public Guid EmploymentId { get; private set; }
    public decimal GrossPay { get; private set; }
    public decimal NetPay { get; private set; }
    public decimal TotalEarnings { get; private set; }
    public decimal TotalDeductions { get; private set; }
    public decimal EmployerContributions { get; private set; }
    public uint RowVersion { get; private set; }

    private readonly List<PayrollLine> _lines = new();
    public IReadOnlyCollection<PayrollLine> Lines => _lines.AsReadOnly();

    private readonly List<CalculationTrace> _traces = new();
    public IReadOnlyCollection<CalculationTrace> Traces => _traces.AsReadOnly();

    private PayrollEmployeeResult() { }

    public PayrollEmployeeResult(
        Guid id,
        Guid payrollRunId,
        Guid employmentId,
        decimal grossPay,
        decimal netPay,
        decimal totalEarnings,
        decimal totalDeductions,
        decimal employerContributions)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (payrollRunId == Guid.Empty) throw new ArgumentException("PayrollRunId cannot be empty.", nameof(payrollRunId));
        if (employmentId == Guid.Empty) throw new ArgumentException("EmploymentId cannot be empty.", nameof(employmentId));

        Id = id;
        PayrollRunId = payrollRunId;
        EmploymentId = employmentId;
        GrossPay = RoundingPolicy.RoundLine(grossPay);
        NetPay = RoundingPolicy.RoundLine(netPay);
        TotalEarnings = RoundingPolicy.RoundLine(totalEarnings);
        TotalDeductions = RoundingPolicy.RoundLine(totalDeductions);
        EmployerContributions = RoundingPolicy.RoundLine(employerContributions);
        RowVersion = 1;
    }

    public void AddLine(PayrollLine line)
    {
        _lines.Add(line);
    }

    public void AddTrace(CalculationTrace trace)
    {
        _traces.Add(trace);
    }
}
