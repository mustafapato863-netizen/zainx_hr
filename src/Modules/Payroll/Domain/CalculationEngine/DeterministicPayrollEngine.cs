using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Workforce.Modules.Compliance.Domain;

namespace Workforce.Modules.Payroll.Domain.CalculationEngine;

public class DeterministicPayrollEngine : IPayrollCalculationEngine
{
    public string EngineVersion => "2026.4.1-DETERMINISTIC";

    public PayrollEmployeeResult Calculate(
        PayrollInputSnapshot snapshot,
        IReadOnlyList<StatutoryRuleVersion> activeRules,
        out IReadOnlyList<PayrollException> exceptions)
    {
        var exceptionList = new List<PayrollException>();
        var resultId = Guid.NewGuid();

        var scheduledDays = snapshot.ScheduledDays > 0 ? snapshot.ScheduledDays : 22;
        var dailyRate = RoundingPolicy.RoundIntermediate(snapshot.BaseSalaryMonthly / scheduledDays);

        // 1. Base Salary Line & Trace
        var baseAmount = snapshot.BaseSalaryMonthly;
        var baseTrace = new CalculationTrace(
            Guid.NewGuid(), resultId, 1, "BASE_SALARY",
            "Monthly Base Compensation",
            $"{snapshot.BaseSalaryMonthly:F2}",
            $"{{\"baseSalary\": {snapshot.BaseSalaryMonthly}}}",
            baseAmount, 0, baseAmount
        );

        var baseLine = new PayrollLine(
            Guid.NewGuid(), resultId, "BASE_SALARY", "Base Salary", "الراتب الأساسي",
            ComponentCategory.BaseSalary, baseAmount, CalculationType.FixedAmount,
            traceId: baseTrace.Id
        );

        // 2. Allowances
        decimal totalAllowances = 0;
        var allowanceLines = new List<PayrollLine>();
        var allowanceTraces = new List<CalculationTrace>();

        try
        {
            if (!string.IsNullOrWhiteSpace(snapshot.AllowancesJson) && snapshot.AllowancesJson != "[]")
            {
                using var doc = JsonDocument.Parse(snapshot.AllowancesJson);
                int step = 2;
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    var code = el.GetProperty("code").GetString() ?? "ALLOWANCE";
                    var nameEn = el.GetProperty("nameEn").GetString() ?? "Allowance";
                    var nameAr = el.GetProperty("nameAr").GetString() ?? "بدل";
                    var amount = el.GetProperty("amount").GetDecimal();
                    totalAllowances += amount;

                    var trace = new CalculationTrace(
                        Guid.NewGuid(), resultId, step++, code,
                        nameEn, $"{amount:F2}", $"{{\"amount\": {amount}}}",
                        amount, 0, amount
                    );
                    allowanceTraces.Add(trace);

                    var line = new PayrollLine(
                        Guid.NewGuid(), resultId, code, nameEn, nameAr,
                        ComponentCategory.Allowance, amount, CalculationType.FixedAmount,
                        traceId: trace.Id
                    );
                    allowanceLines.Add(line);
                }
            }
        }
        catch (Exception ex)
        {
            exceptionList.Add(new PayrollException(
                Guid.NewGuid(), snapshot.PayrollRunId, snapshot.EmploymentId,
                ExceptionSeverity.Warning, "ALLOWANCE_PARSE_ERROR",
                $"Failed to parse allowance JSON: {ex.Message}",
                "Review employee compensation allowances structure."
            ));
        }

        var grossPay = RoundingPolicy.RoundLine(baseAmount + totalAllowances);
        var totalEarnings = grossPay;

        // 3. Unpaid Leave & Absence Deductions
        var deductionLines = new List<PayrollLine>();
        var deductionTraces = new List<CalculationTrace>();
        decimal totalDeductions = 0;

        if (snapshot.UnpaidLeaveDays > 0)
        {
            var unpaidDeduction = RoundingPolicy.RoundLine(dailyRate * snapshot.UnpaidLeaveDays);
            totalDeductions += unpaidDeduction;

            var unpaidTrace = new CalculationTrace(
                Guid.NewGuid(), resultId, 10, "DEDUCTION_UNPAID_LEAVE",
                "Unpaid Leave Deduction",
                $"{dailyRate:F4} * {snapshot.UnpaidLeaveDays}",
                $"{{\"dailyRate\": {dailyRate}, \"days\": {snapshot.UnpaidLeaveDays}}}",
                unpaidDeduction, 0, unpaidDeduction
            );
            deductionTraces.Add(unpaidTrace);

            deductionLines.Add(new PayrollLine(
                Guid.NewGuid(), resultId, "DEDUCTION_UNPAID_LEAVE", "Unpaid Leave Deduction", "خصم إجازة غير مدفوعة",
                ComponentCategory.Deduction, unpaidDeduction, CalculationType.DailyRate, dailyRate, snapshot.UnpaidLeaveDays,
                unpaidTrace.Id
            ));
        }

        if (snapshot.ApprovedAbsenceDays > 0)
        {
            var absenceDeduction = RoundingPolicy.RoundLine(dailyRate * snapshot.ApprovedAbsenceDays);
            totalDeductions += absenceDeduction;

            var absenceTrace = new CalculationTrace(
                Guid.NewGuid(), resultId, 11, "DEDUCTION_ABSENCE",
                "Unauthorized Absence Deduction",
                $"{dailyRate:F4} * {snapshot.ApprovedAbsenceDays}",
                $"{{\"dailyRate\": {dailyRate}, \"days\": {snapshot.ApprovedAbsenceDays}}}",
                absenceDeduction, 0, absenceDeduction
            );
            deductionTraces.Add(absenceTrace);

            deductionLines.Add(new PayrollLine(
                Guid.NewGuid(), resultId, "DEDUCTION_ABSENCE", "Absence Deduction", "خصم غياب",
                ComponentCategory.Deduction, absenceDeduction, CalculationType.DailyRate, dailyRate, snapshot.ApprovedAbsenceDays,
                absenceTrace.Id
            ));
        }

        // 4. Egypt Statutory Social Insurance (Law 148 of 2019)
        decimal employerContributions = 0;
        var gosiRule = activeRules.FirstOrDefault(r => r.CalculationStrategyName == "EgyptSocialInsuranceStrategy");
        if (gosiRule != null)
        {
            // Parse parameters
            decimal empRate = 0.11m;
            decimal empyrRate = 0.1875m;
            decimal minInsured = 2000.00m;
            decimal maxInsured = 12600.00m;

            try
            {
                using var pDoc = JsonDocument.Parse(gosiRule.ParametersJson);
                if (pDoc.RootElement.TryGetProperty("employeeRate", out var er)) empRate = er.GetDecimal();
                if (pDoc.RootElement.TryGetProperty("employerRate", out var eyr)) empyrRate = eyr.GetDecimal();
                if (pDoc.RootElement.TryGetProperty("minInsuredMonthly", out var mi)) minInsured = mi.GetDecimal();
                if (pDoc.RootElement.TryGetProperty("maxInsuredMonthly", out var ma)) maxInsured = ma.GetDecimal();
            }
            catch { /* use statutory defaults */ }

            var insurableBase = Math.Min(Math.Max(grossPay, minInsured), maxInsured);
            var gosiEmployee = RoundingPolicy.RoundLine(insurableBase * empRate);
            var gosiEmployer = RoundingPolicy.RoundLine(insurableBase * empyrRate);

            totalDeductions += gosiEmployee;
            employerContributions += gosiEmployer;

            var gosiTrace = new CalculationTrace(
                Guid.NewGuid(), resultId, 20, "EG_GOSI_EMPLOYEE",
                "Egypt Social Insurance (Employee 11%)",
                $"min(max({grossPay:F2}, {minInsured:F2}), {maxInsured:F2}) * {empRate:P2}",
                $"{{\"insurableBase\": {insurableBase}, \"rate\": {empRate}}}",
                gosiEmployee, 0, gosiEmployee
            );
            deductionTraces.Add(gosiTrace);

            deductionLines.Add(new PayrollLine(
                Guid.NewGuid(), resultId, "EG_GOSI_EMPLOYEE", "Social Insurance (Employee)", "تأمينات اجتماعية (حصة الموظف)",
                ComponentCategory.StatutoryDeduction, gosiEmployee, CalculationType.StatutoryFormula, empRate, insurableBase,
                gosiTrace.Id
            ));

            var gosiEmployerTrace = new CalculationTrace(
                Guid.NewGuid(), resultId, 21, "EG_GOSI_EMPLOYER",
                "Egypt Social Insurance (Employer 18.75%)",
                $"{insurableBase:F2} * {empyrRate:P2}",
                $"{{\"insurableBase\": {insurableBase}, \"rate\": {empyrRate}}}",
                gosiEmployer, 0, gosiEmployer
            );
            deductionTraces.Add(gosiEmployerTrace);

            deductionLines.Add(new PayrollLine(
                Guid.NewGuid(), resultId, "EG_GOSI_EMPLOYER", "Social Insurance (Employer)", "تأمينات اجتماعية (حصة الشركة)",
                ComponentCategory.EmployerContribution, gosiEmployer, CalculationType.StatutoryFormula, empyrRate, insurableBase,
                gosiEmployerTrace.Id
            ));
        }

        // 5. Egypt Income Tax (Progressive Brackets)
        var taxRule = activeRules.FirstOrDefault(r => r.CalculationStrategyName == "EgyptProgressiveIncomeTaxStrategy");
        if (taxRule != null)
        {
            decimal monthlyPersonalExemption = 20000.00m / 12.00m; // ~1666.67
            var gosiLine = deductionLines.FirstOrDefault(d => d.ComponentCode == "EG_GOSI_EMPLOYEE");
            var gosiDed = gosiLine?.Amount ?? 0;
            var taxableMonthly = Math.Max(0, grossPay - monthlyPersonalExemption - gosiDed);
            
            // Progressive tax monthly approximation
            decimal monthlyTax = 0;
            if (taxableMonthly > 0)
            {
                if (taxableMonthly <= 2500) monthlyTax = 0;
                else if (taxableMonthly <= 3750) monthlyTax = (taxableMonthly - 2500) * 0.025m;
                else if (taxableMonthly <= 5000) monthlyTax = (1250 * 0.025m) + ((taxableMonthly - 3750) * 0.10m);
                else monthlyTax = (1250 * 0.025m) + (1250 * 0.10m) + ((taxableMonthly - 5000) * 0.15m);
            }

            monthlyTax = RoundingPolicy.RoundLine(monthlyTax);
            totalDeductions += monthlyTax;

            var taxTrace = new CalculationTrace(
                Guid.NewGuid(), resultId, 30, "EG_INCOME_TAX",
                "Egypt Income Tax (Monthly Progressive)",
                $"ProgressiveBracket(TaxableBase = {taxableMonthly:F2})",
                $"{{\"taxableMonthly\": {taxableMonthly}, \"personalExemptionMonthly\": {monthlyPersonalExemption}}}",
                monthlyTax, 0, monthlyTax
            );
            deductionTraces.Add(taxTrace);

            deductionLines.Add(new PayrollLine(
                Guid.NewGuid(), resultId, "EG_INCOME_TAX", "Income Tax", "ضريبة كسب العمل",
                ComponentCategory.StatutoryDeduction, monthlyTax, CalculationType.StatutoryFormula,
                traceId: taxTrace.Id
            ));
        }

        var netPay = RoundingPolicy.RoundLine(totalEarnings - totalDeductions);

        var employeeResult = new PayrollEmployeeResult(
            resultId, snapshot.PayrollRunId, snapshot.EmploymentId,
            grossPay, netPay, totalEarnings, totalDeductions, employerContributions
        );

        employeeResult.AddLine(baseLine);
        employeeResult.AddTrace(baseTrace);

        foreach (var l in allowanceLines) employeeResult.AddLine(l);
        foreach (var t in allowanceTraces) employeeResult.AddTrace(t);
        foreach (var l in deductionLines) employeeResult.AddLine(l);
        foreach (var t in deductionTraces) employeeResult.AddTrace(t);

        exceptions = exceptionList;
        return employeeResult;
    }
}
