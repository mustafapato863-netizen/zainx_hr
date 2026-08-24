using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Workforce.Modules.Compliance.Domain;

namespace Workforce.Modules.Payroll.Domain.CalculationEngine;

public class DeterministicPayrollEngine : IPayrollCalculationEngine
{
    public string EngineVersion => "1.0.0-ETA-Article8-Arrears";

    private class ArrearsItem
    {
        public string Code { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateOnly EntitlementPeriodStart { get; set; }
        public DateOnly EntitlementPeriodEnd { get; set; }
        public decimal HistoricalBaseSalaryMonthly { get; set; }
        public string SourceReason { get; set; } = string.Empty;
    }

    public PayrollEmployeeResult Calculate(
        PayrollInputSnapshot snapshot,
        IReadOnlyList<StatutoryRuleVersion> activeRules,
        out IReadOnlyList<PayrollException> exceptions)
    {
        var exceptionList = new List<PayrollException>();
        var resultId = Guid.NewGuid();

        // 1. Base Salary & Scheduled Working Days Calculation
        var scheduledDays = snapshot.ScheduledDays > 0 ? snapshot.ScheduledDays : 30;
        var dailyRate = RoundingPolicy.RoundLine(snapshot.BaseSalaryMonthly / scheduledDays);
        var baseAmount = RoundingPolicy.RoundLine(snapshot.BaseSalaryMonthly);

        var baseTrace = new CalculationTrace(
            Guid.NewGuid(), resultId, 1, "BASE_SALARY",
            "Monthly Base Salary",
            $"{snapshot.BaseSalaryMonthly:F2} (Scheduled: {scheduledDays} days)",
            $"{{\"baseSalary\":{snapshot.BaseSalaryMonthly:F2},\"scheduledDays\":{scheduledDays}}}",
            baseAmount, 0, baseAmount
        );

        var baseLine = new PayrollLine(
            Guid.NewGuid(), resultId, "BASE_SALARY", "Base Salary", "الراتب الأساسي",
            ComponentCategory.BaseSalary, baseAmount, CalculationType.FixedAmount,
            traceId: baseTrace.Id
        );

        // 2. Allowances & Temporal Treatment Classification
        decimal currentPeriodAllowances = 0;
        var allowanceLines = new List<PayrollLine>();
        var allowanceTraces = new List<CalculationTrace>();
        var arrearsItems = new List<ArrearsItem>();

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

                    // Check for Egyptian Tax Authority Arrears / Frozen Wages temporal attribution
                    bool isArrears = false;
                    if (el.TryGetProperty("temporalTreatment", out var ttProp))
                    {
                        var ttStr = ttProp.GetString();
                        if (string.Equals(ttStr, "ArrearsFrozenWages", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(ttStr, "FROZEN_WAGES", StringComparison.OrdinalIgnoreCase) ||
                            (ttProp.ValueKind == JsonValueKind.Number && ttProp.GetInt32() == (int)SalaryTaxTemporalTreatment.ArrearsFrozenWages))
                        {
                            isArrears = true;
                        }
                    }
                    else if (code.StartsWith("ARREARS_", StringComparison.OrdinalIgnoreCase) ||
                             code.StartsWith("FROZEN_", StringComparison.OrdinalIgnoreCase))
                    {
                        isArrears = true;
                    }

                    if (isArrears)
                    {
                        DateOnly entStart = new DateOnly(2024, 1, 1);
                        DateOnly entEnd = new DateOnly(2024, 1, 31);

                        if (el.TryGetProperty("entitlementPeriodStart", out var esProp) && esProp.ValueKind == JsonValueKind.String)
                        {
                            if (DateOnly.TryParse(esProp.GetString(), out var parsedEs)) entStart = parsedEs;
                        }
                        if (el.TryGetProperty("entitlementPeriodEnd", out var eeProp) && eeProp.ValueKind == JsonValueKind.String)
                        {
                            if (DateOnly.TryParse(eeProp.GetString(), out var parsedEe)) entEnd = parsedEe;
                        }

                        decimal histBase = 0;
                        if (el.TryGetProperty("historicalBaseSalary", out var hbProp) && hbProp.ValueKind == JsonValueKind.Number)
                        {
                            histBase = hbProp.GetDecimal();
                        }

                        string reason = el.TryGetProperty("sourceReason", out var srProp) ? srProp.GetString() ?? "Arrears Settlement" : "Arrears Settlement";

                        arrearsItems.Add(new ArrearsItem
                        {
                            Code = code,
                            NameEn = nameEn,
                            NameAr = nameAr,
                            Amount = amount,
                            EntitlementPeriodStart = entStart,
                            EntitlementPeriodEnd = entEnd,
                            HistoricalBaseSalaryMonthly = histBase,
                            SourceReason = reason
                        });
                    }
                    else
                    {
                        currentPeriodAllowances += amount;
                    }

                    var trace = new CalculationTrace(
                        Guid.NewGuid(), resultId, step++, code,
                        nameEn, $"{amount:F2}", $"{{\"componentCode\":\"{code}\",\"amount\":{amount:F2},\"isArrears\":{isArrears}}}",
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

        var totalArrearsEarnings = arrearsItems.Sum(a => a.Amount);
        var currentPeriodGross = RoundingPolicy.RoundLine(baseAmount + currentPeriodAllowances);
        var grossPay = RoundingPolicy.RoundLine(currentPeriodGross + totalArrearsEarnings);
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
                $"{{\"dailyRate\":{dailyRate:F4},\"unpaidDays\":{snapshot.UnpaidLeaveDays}}}",
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
                $"{{\"dailyRate\":{dailyRate:F4},\"absenceDays\":{snapshot.ApprovedAbsenceDays}}}",
                absenceDeduction, 0, absenceDeduction
            );
            deductionTraces.Add(absenceTrace);

            deductionLines.Add(new PayrollLine(
                Guid.NewGuid(), resultId, "DEDUCTION_ABSENCE", "Absence Deduction", "خصم غياب",
                ComponentCategory.Deduction, absenceDeduction, CalculationType.DailyRate, dailyRate, snapshot.ApprovedAbsenceDays,
                absenceTrace.Id
            ));
        }

        // 4. Egypt Statutory Social Insurance (Law 148 of 2019) for Current Period
        decimal employerContributions = 0;
        var gosiRule = activeRules.FirstOrDefault(r => r.CalculationStrategyName == "EgyptSocialInsuranceStrategy");
        decimal gosiEmployee = 0;

        if (gosiRule != null)
        {
            if (gosiRule.Status != VerificationStatus.Verified)
            {
                exceptionList.Add(new PayrollException(
                    Guid.NewGuid(), snapshot.PayrollRunId, snapshot.EmploymentId,
                    ExceptionSeverity.Blocking, "STATUTORY_RULE_UNVERIFIED",
                    "Social Insurance statutory rule version is marked UNVERIFIED. Cannot calculate payroll using unverified regulatory parameters.",
                    "Verify regulatory decree in official gazette and update rule verification status."
                ));
            }

            decimal empRate = 0.11m;
            decimal empyrRate = 0.1875m;
            decimal minInsured = 2000.00m;
            decimal maxInsured = 12600.00m;
            string gosiSourceRef = "Law No. 148 of 2019";

            try
            {
                using var pDoc = JsonDocument.Parse(gosiRule.ParametersJson);
                if (pDoc.RootElement.TryGetProperty("sourceReference", out var sr)) gosiSourceRef = sr.GetString() ?? gosiSourceRef;
                if (pDoc.RootElement.TryGetProperty("employeeRate", out var er)) empRate = er.GetDecimal();
                if (pDoc.RootElement.TryGetProperty("employerRate", out var eyr)) empyrRate = eyr.GetDecimal();
                if (pDoc.RootElement.TryGetProperty("minInsuredMonthly", out var mi)) minInsured = mi.GetDecimal();
                if (pDoc.RootElement.TryGetProperty("maxInsuredMonthly", out var ma)) maxInsured = ma.GetDecimal();
            }
            catch (Exception ex)
            {
                exceptionList.Add(new PayrollException(
                    Guid.NewGuid(), snapshot.PayrollRunId, snapshot.EmploymentId,
                    ExceptionSeverity.Blocking, "STATUTORY_RULE_CORRUPT",
                    $"Failed to parse Social Insurance parameters: {ex.Message}",
                    "Update statutory rule parameters JSON."
                ));
            }

            var insurableBase = Math.Min(Math.Max(currentPeriodGross, minInsured), maxInsured);
            gosiEmployee = RoundingPolicy.RoundLine(insurableBase * empRate);
            var gosiEmployer = RoundingPolicy.RoundLine(insurableBase * empyrRate);

            totalDeductions += gosiEmployee;
            employerContributions += gosiEmployer;

            var gosiTrace = new CalculationTrace(
                Guid.NewGuid(), resultId, 20, "EG_GOSI_EMPLOYEE",
                $"Egypt Social Insurance Employee ({gosiSourceRef} - Bounds: {minInsured:N0} to {maxInsured:N0} EGP)",
                $"min(max({currentPeriodGross:F2}, {minInsured:F2}), {maxInsured:F2}) * {empRate:P2}",
                $"{{\"insurableBase\":{insurableBase:F2},\"employeeRate\":{empRate:F4},\"minInsured\":{minInsured:F2},\"maxInsured\":{maxInsured:F2},\"ruleVersion\":{gosiRule.VersionNumber}}}",
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
                $"Egypt Social Insurance Employer ({gosiSourceRef} - {empyrRate:P2})",
                $"{insurableBase:F2} * {empyrRate:P2}",
                $"{{\"insurableBase\":{insurableBase:F2},\"employerRate\":{empyrRate:F4},\"ruleVersion\":{gosiRule.VersionNumber}}}",
                gosiEmployer, 0, gosiEmployer
            );
            deductionTraces.Add(gosiEmployerTrace);

            deductionLines.Add(new PayrollLine(
                Guid.NewGuid(), resultId, "EG_GOSI_EMPLOYER", "Social Insurance (Employer)", "تأمينات اجتماعية (حصة الشركة)",
                ComponentCategory.EmployerContribution, gosiEmployer, CalculationType.StatutoryFormula, empyrRate, insurableBase,
                gosiEmployerTrace.Id
            ));
        }

        // 5. Egypt Income Tax — Current Period Salary (Article 8 Matrix)
        var taxRule = activeRules.FirstOrDefault(r => r.CalculationStrategyName == "EgyptProgressiveIncomeTaxStrategy");
        if (taxRule != null)
        {
            if (taxRule.Status != VerificationStatus.Verified)
            {
                exceptionList.Add(new PayrollException(
                    Guid.NewGuid(), snapshot.PayrollRunId, snapshot.EmploymentId,
                    ExceptionSeverity.Blocking, "STATUTORY_RULE_UNVERIFIED",
                    "Income Tax statutory rule version is marked UNVERIFIED. Cannot calculate payroll using unverified regulatory parameters.",
                    "Verify tax bracket schedules in official gazette and update rule verification status."
                ));
            }

            decimal yearlyExemption = 20000.00m;
            string taxSourceRef = "Income Tax Law No. 91 of 2005";
            string selectedBandName = "Band 1";
            decimal monthlyTax = 0;
            decimal normalizedAnnualTaxBase = 0;
            decimal totalAnnualTax = 0;

            try
            {
                using var pDoc = JsonDocument.Parse(taxRule.ParametersJson);
                if (pDoc.RootElement.TryGetProperty("sourceReference", out var sr)) taxSourceRef = sr.GetString() ?? taxSourceRef;
                if (pDoc.RootElement.TryGetProperty("personalExemptionYearly", out var pey)) yearlyExemption = pey.GetDecimal();

                decimal monthlyPersonalExemption = yearlyExemption / 12.00m;
                var unroundedTaxableMonthly = Math.Max(0, currentPeriodGross - monthlyPersonalExemption - gosiEmployee);
                var unroundedAnnualTaxable = unroundedTaxableMonthly * 12.00m;

                // Statutory Tax-Base Normalization (Law 91/2005 Article 8: Round down to nearest 10 EGP)
                normalizedAnnualTaxBase = StatutoryTaxBaseNormalization.NormalizeAnnualTaxBase(unroundedAnnualTaxable);

                totalAnnualTax = EvaluateArticle8Tax(normalizedAnnualTaxBase, taxRule, out selectedBandName);
                monthlyTax = RoundingPolicy.RoundLine(totalAnnualTax / 12.00m);
            }
            catch (Exception ex)
            {
                exceptionList.Add(new PayrollException(
                    Guid.NewGuid(), snapshot.PayrollRunId, snapshot.EmploymentId,
                    ExceptionSeverity.Blocking, "STATUTORY_RULE_CORRUPT",
                    $"Failed to parse Income Tax parameters: {ex.Message}",
                    "Update statutory rule parameters JSON."
                ));
            }

            totalDeductions += monthlyTax;

            decimal monthlyExemp = yearlyExemption / 12.00m;
            var finalTaxableMonthly = Math.Max(0, currentPeriodGross - monthlyExemp - gosiEmployee);

            var taxTrace = new CalculationTrace(
                Guid.NewGuid(), resultId, 30, "EG_INCOME_TAX",
                $"Egypt Income Tax ({taxSourceRef} - {selectedBandName} - Exemption: {yearlyExemption:N0} EGP/yr - Base: {normalizedAnnualTaxBase:N0} EGP)",
                $"Article8Matrix({selectedBandName}, AnnualNormalizedBase = {normalizedAnnualTaxBase:F2}, AnnualTax = {totalAnnualTax:F2})",
                $"{{\"taxableMonthly\":{finalTaxableMonthly:F2},\"annualTaxableNormalized\":{normalizedAnnualTaxBase:F2},\"annualTax\":{totalAnnualTax:F2},\"selectedBand\":\"{selectedBandName}\",\"personalExemptionYearly\":{yearlyExemption:F2},\"ruleVersion\":{taxRule.VersionNumber},\"statutoryRounding\":\"FloorNearest10\"}}",
                monthlyTax, 0, monthlyTax
            );
            deductionTraces.Add(taxTrace);

            deductionLines.Add(new PayrollLine(
                Guid.NewGuid(), resultId, "EG_INCOME_TAX", "Income Tax", "ضريبة كسب العمل",
                ComponentCategory.StatutoryDeduction, monthlyTax, CalculationType.StatutoryFormula,
                traceId: taxTrace.Id
            ));
        }

        // 6. Egyptian Tax Authority (ETA) Arrears & Frozen Wages Recalculation (متجمد الأجور والمرتبات)
        int arrearsStep = 40;
        foreach (var arrears in arrearsItems)
        {
            // Find historical income tax rule active on the entitlement period end
            var historicalTaxRule = activeRules.FirstOrDefault(r =>
                r.CalculationStrategyName == "EgyptProgressiveIncomeTaxStrategy" &&
                r.EffectivePeriod.IsActiveOn(arrears.EntitlementPeriodEnd));

            if (historicalTaxRule == null)
            {
                exceptionList.Add(new PayrollException(
                    Guid.NewGuid(), snapshot.PayrollRunId, snapshot.EmploymentId,
                    ExceptionSeverity.Blocking, "STATUTORY_RULE_MISSING",
                    $"BLOCKING COMPLIANCE EXCEPTION: No statutory income tax rule found for historical entitlement period {arrears.EntitlementPeriodStart:yyyy-MM-dd}..{arrears.EntitlementPeriodEnd:yyyy-MM-dd}. Stale fallback is forbidden.",
                    "Seed and verify statutory tax rules for all historical entitlement periods."
                ));
                continue;
            }

            if (historicalTaxRule.Status != VerificationStatus.Verified)
            {
                exceptionList.Add(new PayrollException(
                    Guid.NewGuid(), snapshot.PayrollRunId, snapshot.EmploymentId,
                    ExceptionSeverity.Blocking, "STATUTORY_RULE_UNVERIFIED",
                    $"BLOCKING COMPLIANCE EXCEPTION: Statutory rule version for entitlement period {arrears.EntitlementPeriodStart:yyyy-MM-dd}..{arrears.EntitlementPeriodEnd:yyyy-MM-dd} (v{historicalTaxRule.VersionNumber}) is UNVERIFIED.",
                    "Verify regulatory parameters in official gazette."
                ));
                continue;
            }

            // Find historical social insurance rule for the entitlement period
            var historicalGosiRule = activeRules.FirstOrDefault(r =>
                r.CalculationStrategyName == "EgyptSocialInsuranceStrategy" &&
                r.EffectivePeriod.IsActiveOn(arrears.EntitlementPeriodEnd));

            decimal historicalYearlyExemption = 15000.00m;
            string historicalTaxSourceRef = "Income Tax Law";
            try
            {
                using var hDoc = JsonDocument.Parse(historicalTaxRule.ParametersJson);
                if (hDoc.RootElement.TryGetProperty("sourceReference", out var hsr)) historicalTaxSourceRef = hsr.GetString() ?? historicalTaxSourceRef;
                if (hDoc.RootElement.TryGetProperty("personalExemptionYearly", out var hpey)) historicalYearlyExemption = hpey.GetDecimal();
            }
            catch { }

            decimal historicalMonthlyExemption = historicalYearlyExemption / 12.00m;
            decimal historicalGosiEmp = 0;
            if (historicalGosiRule != null && historicalGosiRule.Status == VerificationStatus.Verified)
            {
                try
                {
                    using var gDoc = JsonDocument.Parse(historicalGosiRule.ParametersJson);
                    var er = gDoc.RootElement.GetProperty("employeeRate").GetDecimal();
                    var mi = gDoc.RootElement.GetProperty("minInsuredMonthly").GetDecimal();
                    var ma = gDoc.RootElement.GetProperty("maxInsuredMonthly").GetDecimal();
                    var histBase = arrears.HistoricalBaseSalaryMonthly > 0 ? arrears.HistoricalBaseSalaryMonthly : arrears.Amount;
                    var insBase = Math.Min(Math.Max(histBase, mi), ma);
                    historicalGosiEmp = RoundingPolicy.RoundLine(insBase * er);
                }
                catch { }
            }

            // Calculate Differential/Incremental Tax according to ETA Arrears Recalculation Rules:
            // Incremental Tax = Tax(HistoricalBase + Arrears) - Tax(HistoricalBase)
            decimal differentialMonthlyTax = 0;
            decimal differentialAnnualTax = 0;
            decimal combinedNormalizedBase = 0;
            string histBandName = "Band 1";

            if (arrears.HistoricalBaseSalaryMonthly > 0)
            {
                var histTaxableMonthly = Math.Max(0, arrears.HistoricalBaseSalaryMonthly - historicalMonthlyExemption - historicalGosiEmp);
                var histNormalized = StatutoryTaxBaseNormalization.NormalizeAnnualTaxBase(histTaxableMonthly * 12.00m);
                var histAnnualTax = EvaluateArticle8Tax(histNormalized, historicalTaxRule, out _);

                var combTaxableMonthly = Math.Max(0, arrears.HistoricalBaseSalaryMonthly + arrears.Amount - historicalMonthlyExemption - historicalGosiEmp);
                combinedNormalizedBase = StatutoryTaxBaseNormalization.NormalizeAnnualTaxBase(combTaxableMonthly * 12.00m);
                var combAnnualTax = EvaluateArticle8Tax(combinedNormalizedBase, historicalTaxRule, out histBandName);

                differentialAnnualTax = Math.Max(0, combAnnualTax - histAnnualTax);
                differentialMonthlyTax = RoundingPolicy.RoundLine(differentialAnnualTax / 12.00m);
            }
            else
            {
                var taxableMonthly = Math.Max(0, arrears.Amount - historicalMonthlyExemption - historicalGosiEmp);
                combinedNormalizedBase = StatutoryTaxBaseNormalization.NormalizeAnnualTaxBase(taxableMonthly * 12.00m);
                differentialAnnualTax = EvaluateArticle8Tax(combinedNormalizedBase, historicalTaxRule, out histBandName);
                differentialMonthlyTax = RoundingPolicy.RoundLine(differentialAnnualTax / 12.00m);
            }

            totalDeductions += differentialMonthlyTax;

            var arrearsTrace = new CalculationTrace(
                Guid.NewGuid(), resultId, arrearsStep++, "EG_INCOME_TAX",
                $"ETA Arrears Tax (متجمد أجور) - Entitlement: {arrears.EntitlementPeriodStart:yyyy-MM-dd}..{arrears.EntitlementPeriodEnd:yyyy-MM-dd} ({historicalTaxSourceRef} - {histBandName})",
                $"DifferentialTax(Arrears = {arrears.Amount:F2}, EntitlementBase = {combinedNormalizedBase:F2}, Rule = v{historicalTaxRule.VersionNumber})",
                $"{{\"sourceEarning\":\"{arrears.Code}\",\"entitlementPeriodStart\":\"{arrears.EntitlementPeriodStart:yyyy-MM-dd}\",\"entitlementPeriodEnd\":\"{arrears.EntitlementPeriodEnd:yyyy-MM-dd}\",\"statutoryRuleVersion\":{historicalTaxRule.VersionNumber},\"arrearsAmount\":{arrears.Amount:F2},\"historicalTaxableBase\":{combinedNormalizedBase:F2},\"taxDifferential\":{differentialMonthlyTax:F2},\"sourceReference\":\"{historicalTaxSourceRef}\"}}",
                differentialMonthlyTax, 0, differentialMonthlyTax
            );
            deductionTraces.Add(arrearsTrace);

            deductionLines.Add(new PayrollLine(
                Guid.NewGuid(), resultId, "EG_INCOME_TAX_ARREARS", $"Income Tax Arrears ({arrears.EntitlementPeriodStart:yyyy-MM})", $"ضريبة متجمد أجور ({arrears.EntitlementPeriodStart:yyyy-MM})",
                ComponentCategory.StatutoryDeduction, differentialMonthlyTax, CalculationType.StatutoryFormula,
                traceId: arrearsTrace.Id
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

    private static decimal EvaluateArticle8Tax(decimal normalizedAnnualTaxBase, StatutoryRuleVersion taxRule, out string selectedBandName)
    {
        selectedBandName = "Band 1";
        decimal totalAnnualTax = 0;

        using var pDoc = JsonDocument.Parse(taxRule.ParametersJson);
        if (pDoc.RootElement.TryGetProperty("incomeBands", out var bandsEl) && bandsEl.ValueKind == JsonValueKind.Array)
        {
            JsonElement? matchedBand = null;
            foreach (var band in bandsEl.EnumerateArray())
            {
                var minIncome = band.GetProperty("minAnnualIncome").GetDecimal();
                decimal? maxIncome = band.TryGetProperty("maxAnnualIncome", out var maxProp) && maxProp.ValueKind == JsonValueKind.Number 
                    ? maxProp.GetDecimal() 
                    : null;

                bool isMatch;
                if (minIncome == 0)
                {
                    isMatch = normalizedAnnualTaxBase <= (maxIncome ?? decimal.MaxValue);
                }
                else
                {
                    isMatch = normalizedAnnualTaxBase > minIncome && normalizedAnnualTaxBase <= (maxIncome ?? decimal.MaxValue);
                }

                if (isMatch)
                {
                    matchedBand = band;
                    selectedBandName = band.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "Band" : "Band";
                    break;
                }
            }

            if (!matchedBand.HasValue)
            {
                var lastBand = bandsEl.EnumerateArray().Last();
                matchedBand = lastBand;
                selectedBandName = lastBand.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "High Income Band" : "High Income Band";
            }

            if (matchedBand.HasValue && matchedBand.Value.TryGetProperty("tranches", out var tranchesEl))
            {
                foreach (var tr in tranchesEl.EnumerateArray())
                {
                    var from = tr.GetProperty("from").GetDecimal();
                    decimal? to = tr.TryGetProperty("to", out var toProp) && toProp.ValueKind == JsonValueKind.Number 
                        ? toProp.GetDecimal() 
                        : null;
                    var rate = tr.GetProperty("rate").GetDecimal();

                    if (normalizedAnnualTaxBase > from)
                    {
                        var upper = to.HasValue ? Math.Min(normalizedAnnualTaxBase, to.Value) : normalizedAnnualTaxBase;
                        var taxableInTranche = upper - from;
                        if (taxableInTranche > 0)
                        {
                            totalAnnualTax += taxableInTranche * rate;
                        }
                    }
                }
            }
        }
        else if (pDoc.RootElement.TryGetProperty("brackets", out var bracketsEl) && bracketsEl.ValueKind == JsonValueKind.Array)
        {
            decimal previousLimit = 0;
            foreach (var b in bracketsEl.EnumerateArray())
            {
                decimal from = 0;
                decimal? to = null;

                if (b.TryGetProperty("from", out var fromProp))
                {
                    from = fromProp.GetDecimal();
                    if (b.TryGetProperty("to", out var toProp) && toProp.ValueKind == JsonValueKind.Number)
                    {
                        to = toProp.GetDecimal();
                    }
                }
                else if (b.TryGetProperty("limit", out var limitProp))
                {
                    from = previousLimit;
                    if (limitProp.ValueKind == JsonValueKind.Number)
                    {
                        to = limitProp.GetDecimal();
                        previousLimit = to.Value;
                    }
                }

                var rate = b.GetProperty("rate").GetDecimal();

                if (normalizedAnnualTaxBase > from)
                {
                    var upper = to.HasValue ? Math.Min(normalizedAnnualTaxBase, to.Value) : normalizedAnnualTaxBase;
                    var taxableInBracket = upper - from;
                    if (taxableInBracket > 0)
                    {
                        totalAnnualTax += taxableInBracket * rate;
                    }
                }
            }
        }

        return totalAnnualTax;
    }
}
