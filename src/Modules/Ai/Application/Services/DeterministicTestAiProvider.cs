using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Workforce.Modules.Ai.Application.Contracts;
using Workforce.Modules.Ai.Domain;

namespace Workforce.Modules.Ai.Application.Services;

/// <summary>
/// Deterministic AI Provider adapter for automated testing, Playwright E2E, and zero-egress offline operation.
/// Emulates LLM tool routing, prompt injection defense, and structured answer synthesis.
/// </summary>
public sealed class DeterministicTestAiProvider : IAiModelProvider
{
    public string ProviderCode => "DeterministicTestEngine-v1";

    public Task<AiModelResponse> GenerateResponseAsync(AiModelPromptRequest request, CancellationToken ct = default)
    {
        var prompt = request.CurrentUserPrompt?.Trim() ?? string.Empty;
        var promptLower = prompt.ToLowerInvariant();

        // 1. Prompt Injection Defense Verification
        // Malicious instructions embedded in prompts or quotes must NEVER trigger unapproved tools or privilege escalation
        bool containsInjection = promptLower.Contains("ignore system") ||
                                promptLower.Contains("ignore previous") ||
                                promptLower.Contains("reveal all salaries") ||
                                promptLower.Contains("reveal salary") ||
                                promptLower.Contains("read another tenant") ||
                                promptLower.Contains("reveal provider key") ||
                                promptLower.Contains("call unrestricted") ||
                                promptLower.Contains("grant admin") ||
                                promptLower.Contains("drop table") ||
                                promptLower.Contains("pwned");

        if (containsInjection)
        {
            return Task.FromResult(new AiModelResponse(
                TextResponse: "I cannot comply with instructions attempting to override system constraints or access unauthorized data. Request processed safely under standard security policies.",
                EstimatedTokensUsed: 35,
                SourceCategory: AiSourceCategory.CompanyData,
                ToolInvocations: null
            ));
        }

        // 2. Check if Arabic prompt
        bool isArabic = prompt.Any(c => c >= 0x0600 && c <= 0x06FF);

        // 3. Tool Routing Decision Engine
        var plans = new List<AiToolInvocationPlan>();
        AiSourceCategory primaryCategory = AiSourceCategory.CompanyData;

        // A. Product Knowledge Questions (Platform behaviors & system guides)
        if (promptLower.Contains("how does") || promptLower.Contains("what happens after") || promptLower.Contains("finalization work") ||
            promptLower.Contains("كيف يعمل") || promptLower.Contains("ماذا يحدث بعد"))
        {
            primaryCategory = AiSourceCategory.ProductKnowledge;
            plans.Add(new AiToolInvocationPlan("product.search_knowledge", JsonSerializer.Serialize(new { query = prompt })));
        }
        // B. Company Policy Questions (Effective-Dated)
        else if (promptLower.Contains("policy") || promptLower.Contains("remote work") || promptLower.Contains("leave policy") ||
                 promptLower.Contains("سياسة") || promptLower.Contains("لائحة") || promptLower.Contains("العمل عن بعد"))
        {
            primaryCategory = AiSourceCategory.CompanyPolicy;
            string? targetDate = promptLower.Contains("may") || promptLower.Contains("مايو") 
                ? "2026-05-15" 
                : promptLower.Contains("august") || promptLower.Contains("أغسطس") 
                    ? "2026-08-15" 
                    : null;

            plans.Add(new AiToolInvocationPlan("policy.search_company_policy", JsonSerializer.Serialize(new { query = prompt, targetDate })));
        }
        // C. Payroll Calculation & Variance Questions
        else if (promptLower.Contains("payroll") || promptLower.Contains("salary") || promptLower.Contains("net pay") ||
                 promptLower.Contains("راتب") || promptLower.Contains("مسير") || promptLower.Contains("خصم") || promptLower.Contains("تأمينات"))
        {
            primaryCategory = AiSourceCategory.PayrollTrace;

            if (promptLower.Contains("why") || promptLower.Contains("variance") || promptLower.Contains("change") || promptLower.Contains("لماذا") || promptLower.Contains("سبب"))
            {
                var runId = Guid.TryParse(request.ContextEntityId, out var rId) ? rId : Guid.Parse("44444444-4444-4444-4444-444444444444");
                var empId = Guid.Parse("11111111-1111-1111-1111-111111111111");

                plans.Add(new AiToolInvocationPlan("payroll.get_run_summary", JsonSerializer.Serialize(new { payrollRunId = runId })));
                plans.Add(new AiToolInvocationPlan("payroll.get_employee_trace", JsonSerializer.Serialize(new { payrollRunId = runId, employmentId = empId })));
            }
            else
            {
                var runId = Guid.TryParse(request.ContextEntityId, out var rId) ? rId : Guid.Parse("44444444-4444-4444-4444-444444444444");
                plans.Add(new AiToolInvocationPlan("payroll.get_run_summary", JsonSerializer.Serialize(new { payrollRunId = runId })));
            }
        }
        // D. Recruitment Questions
        else if (promptLower.Contains("candidate") || promptLower.Contains("requisition") || promptLower.Contains("applicant") ||
                 promptLower.Contains("مرشح") || promptLower.Contains("وظيفة") || promptLower.Contains("متقدم"))
        {
            primaryCategory = AiSourceCategory.CompanyData;
            var candId = Guid.TryParse(request.ContextEntityId, out var cId) ? cId : Guid.Parse("55555555-5555-5555-5555-555555555555");
            plans.Add(new AiToolInvocationPlan("recruitment.get_candidate_summary", JsonSerializer.Serialize(new { candidateId = candId })));
        }
        // E. Reporting Questions
        else if (promptLower.Contains("report") || promptLower.Contains("headcount") || promptLower.Contains("metrics") ||
                 promptLower.Contains("تقرير") || promptLower.Contains("قوة العمل") || promptLower.Contains("إحصائيات"))
        {
            primaryCategory = AiSourceCategory.CompanyData;
            plans.Add(new AiToolInvocationPlan("reports.run_governed_report", JsonSerializer.Serialize(new { reportCode = "HEADCOUNT_SUMMARY" })));
        }
        // F. People Questions (Default)
        else
        {
            primaryCategory = AiSourceCategory.CompanyData;
            plans.Add(new AiToolInvocationPlan("people.search", JsonSerializer.Serialize(new { query = prompt })));
        }

        // Response Synthesis Placeholder (The conversation service will execute the tool and format final answer)
        string initialText = isArabic 
            ? "جاري مراجعة البيانات المعتمدة واستخراج مسار الإثبات..." 
            : "Querying authorized enterprise read models and calculation traces...";

        return Task.FromResult(new AiModelResponse(
            TextResponse: initialText,
            EstimatedTokensUsed: 42,
            SourceCategory: primaryCategory,
            ToolInvocations: plans
        ));
    }
}
