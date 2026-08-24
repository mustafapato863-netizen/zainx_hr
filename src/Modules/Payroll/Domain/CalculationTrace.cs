using System;

namespace Workforce.Modules.Payroll.Domain;

public class CalculationTrace
{
    public Guid Id { get; private set; }
    public Guid EmployeeResultId { get; private set; }
    public int StepOrder { get; private set; }
    public string RuleReference { get; private set; }
    public string Description { get; private set; }
    public string FormulaApplied { get; private set; }
    public string InputValuesJson { get; private set; }
    public decimal IntermediateAmount { get; private set; }
    public decimal RoundingDelta { get; private set; }
    public decimal FinalAmount { get; private set; }

    private CalculationTrace()
    {
        RuleReference = string.Empty;
        Description = string.Empty;
        FormulaApplied = string.Empty;
        InputValuesJson = "{}";
    }

    public CalculationTrace(
        Guid id,
        Guid employeeResultId,
        int stepOrder,
        string ruleReference,
        string description,
        string formulaApplied,
        string inputValuesJson,
        decimal intermediateAmount,
        decimal roundingDelta,
        decimal finalAmount)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));

        Id = id;
        EmployeeResultId = employeeResultId;
        StepOrder = stepOrder;
        RuleReference = ruleReference.Trim();
        Description = description.Trim();
        FormulaApplied = formulaApplied.Trim();
        InputValuesJson = string.IsNullOrWhiteSpace(inputValuesJson) ? "{}" : inputValuesJson.Trim();
        IntermediateAmount = intermediateAmount;
        RoundingDelta = roundingDelta;
        FinalAmount = finalAmount;
    }
}
