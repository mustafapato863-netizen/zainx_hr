using System.Collections.Generic;
using Workforce.Modules.Compliance.Domain;

namespace Workforce.Modules.Payroll.Domain.CalculationEngine;

public interface IPayrollCalculationEngine
{
    string EngineVersion { get; }
    PayrollEmployeeResult Calculate(
        PayrollInputSnapshot snapshot,
        IReadOnlyList<StatutoryRuleVersion> activeRules,
        out IReadOnlyList<PayrollException> exceptions);
}
