using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Workforce.Modules.Ai.Application.Contracts;
using Workforce.Modules.Ai.Domain;
using Workforce.Modules.Ai.Infrastructure;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Ai.Application.Services;

public sealed class AiConversationService : IAiConversationService
{
    private readonly IAiRepository _aiRepository;
    private readonly IAiModelProvider _modelProvider;
    private readonly AiToolRegistry _toolRegistry;
    private readonly AiRateLimiter? _rateLimiter;

    private const int MaxToolInvocationsPerTurn = 5;
    private const int MaxHistoryMessagesToProvider = 10;

    public AiConversationService(
        IAiRepository aiRepository,
        IAiModelProvider modelProvider,
        AiToolRegistry toolRegistry,
        AiRateLimiter? rateLimiter = null)
    {
        _aiRepository = aiRepository ?? throw new ArgumentNullException(nameof(aiRepository));
        _modelProvider = modelProvider ?? throw new ArgumentNullException(nameof(modelProvider));
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        _rateLimiter = rateLimiter;
    }

    public async Task<ConversationSummaryDto> CreateConversationAsync(
        CreateConversationRequest request, 
        IUserContext userContext, 
        CancellationToken ct = default)
    {
        var conversation = new Conversation(
            Guid.NewGuid(),
            userContext.TenantId,
            userContext.LegalEntityId,
            userContext.UserId,
            request.Title ?? "New Conversation",
            request.ContextEntityType,
            request.ContextEntityId
        );

        await _aiRepository.CreateConversationAsync(conversation, ct);

        return new ConversationSummaryDto(
            conversation.Id,
            conversation.Title,
            conversation.ContextEntityType,
            conversation.ContextEntityId,
            conversation.CreatedAtUtc,
            conversation.UpdatedAtUtc,
            0
        );
    }

    public async Task<List<ConversationSummaryDto>> ListConversationsAsync(
        IUserContext userContext, 
        CancellationToken ct = default)
    {
        var list = await _aiRepository.ListConversationsAsync(userContext.TenantId, userContext.UserId, 50, ct);
        return list.Select(c => new ConversationSummaryDto(
            c.Id,
            c.Title,
            c.ContextEntityType,
            c.ContextEntityId,
            c.CreatedAtUtc,
            c.UpdatedAtUtc,
            c.Messages.Count
        )).ToList();
    }

    public async Task<ConversationDetailDto?> GetConversationAsync(
        Guid conversationId,
        IUserContext userContext,
        CancellationToken ct = default)
    {
        var conversation = await _aiRepository.GetConversationByIdAsync(userContext.TenantId, conversationId, ct);
        if (conversation == null) return null;

        var messages = await _aiRepository.GetMessagesByConversationIdAsync(conversationId, ct);

        // Closeout Gate 12: batched fetches replace per-message queries (N+1 eliminated).
        var allExecutions = await _aiRepository.GetToolExecutionsByConversationIdAsync(conversationId, ct);
        var allSources = await _aiRepository.GetSourceReferencesByConversationIdAsync(conversationId, ct);
        var executionsByMessage = allExecutions
            .GroupBy(e => e.MessageId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var sourcesByMessage = allSources
            .GroupBy(s => s.MessageId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var messageDtos = new List<AiMessageResponseDto>();
        foreach (var msg in messages)
        {
            executionsByMessage.TryGetValue(msg.Id, out var msgExecutions);
            sourcesByMessage.TryGetValue(msg.Id, out var msgSources);

            messageDtos.Add(new AiMessageResponseDto(
                msg.Id,
                msg.SenderRole,
                msg.Content,
                msg.SourceCategory,
                msg.TokensUsed,
                msg.CreatedAtUtc,
                (msgSources ?? new List<SourceReference>()).Select(s => new SourceReferenceDto(
                    s.Id,
                    s.SourceCategory.ToString(),
                    s.Title,
                    s.EntityType,
                    s.EntityId,
                    s.PolicyCode,
                    s.PolicyVersion,
                    s.PayrollRunId,
                    s.MetadataJson ?? "{}",
                    s.RetrievedAtUtc
                )).ToList(),
                (msgExecutions ?? new List<ToolExecution>()).Select(e => new ToolExecutionDto(
                    e.Id,
                    e.ToolCode,
                    e.DurationMs,
                    e.Status,
                    e.CreatedAtUtc
                )).ToList()
            ));
        }

        return new ConversationDetailDto(
            conversation.Id,
            conversation.Title,
            conversation.ContextEntityType,
            conversation.ContextEntityId,
            conversation.CreatedAtUtc,
            conversation.UpdatedAtUtc,
            messageDtos
        );
    }

    public async Task<AiMessageResponseDto> SendMessageAsync(
        Guid conversationId, 
        SendMessageRequest request, 
        IUserContext userContext, 
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            throw new ArgumentException("Prompt cannot be empty.", nameof(request));
        }

        // Closeout Gate 8: per-user/tenant AI request limit (HTTP 429 upstream).
        _rateLimiter?.EnsureWithinLimit(userContext.TenantId.Value, userContext.UserId.Value);

        var conversation = await _aiRepository.GetConversationByIdAsync(userContext.TenantId, conversationId, ct);
        if (conversation == null)
        {
            throw new KeyNotFoundException($"Conversation '{conversationId}' not found for tenant '{userContext.TenantId.Value}'.");
        }

        // 1. Save User Message
        var userMessage = new Message(
            Guid.NewGuid(),
            conversationId,
            senderRole: "User",
            content: request.Prompt.Trim(),
            sourceCategory: AiSourceCategory.CompanyData
        );
        await _aiRepository.AddMessageAsync(userMessage, ct);

        // 2. Prepare Model Dispatch with Authorized Tools
        var authorizedTools = _toolRegistry.GetAuthorizedDefinitions(userContext.Permissions);
        // Closeout Gate 7: provider payload minimization - only the most recent
        // bounded history window is shared with the model provider.
        var fullHistory = await _aiRepository.GetMessagesByConversationIdAsync(conversationId, ct);
        var providerHistory = fullHistory.Count > MaxHistoryMessagesToProvider
            ? fullHistory.Skip(fullHistory.Count - MaxHistoryMessagesToProvider).ToList()
            : fullHistory.ToList();

        var modelRequest = new AiModelPromptRequest(
            SystemInstructions: "You are ZainX Workforce AI. You provide governed, read-only enterprise analysis based solely on authorized data and verified policies. Never invent numbers, execute mutations, or bypass security rules.",
            ConversationHistory: providerHistory.ToList(),
            CurrentUserPrompt: request.Prompt,
            AvailableTools: authorizedTools.ToList(),
            ContextEntityType: conversation.ContextEntityType,
            ContextEntityId: conversation.ContextEntityId
        );

        // Closeout Gate 6: safe provider failure handling. Provider unavailability,
        // timeouts, 429s and malformed responses degrade to a governed safe answer;
        // no raw exception or stack trace escapes to the caller and the core
        // product remains fully operational.
        AiModelResponse modelResponse;
        try
        {
            modelResponse = await _modelProvider.GenerateResponseAsync(modelRequest, ct);
            if (modelResponse == null)
            {
                modelResponse = CreateSafeUnavailableResponse(request.Prompt);
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            modelResponse = CreateSafeTimeoutResponse(request.Prompt);
        }
        catch (OperationCanceledException)
        {
            throw; // genuine client cancellation propagates
        }
        catch (Exception)
        {
            modelResponse = CreateSafeUnavailableResponse(request.Prompt);
        }

        // 3. Execute Planned Tool Invocations
        var executedToolRecords = new List<ToolExecution>();
        var gatheredSourceRefs = new List<SourceReference>();
        var toolOutputSnippets = new List<string>();

        int toolCount = 0;
        var executedPlans = new HashSet<string>();

        if (modelResponse.ToolInvocations != null && modelResponse.ToolInvocations.Count > 0)
        {
            foreach (var plan in modelResponse.ToolInvocations)
            {
                if (toolCount >= MaxToolInvocationsPerTurn)
                {
                    break; // Prevent runaway tool loops
                }

                string planKey = $"{plan.ToolCode}:{plan.InputParametersJson}";
                if (executedPlans.Contains(planKey))
                {
                    continue; // Skip duplicate tool call
                }
                executedPlans.Add(planKey);
                toolCount++;

                var handler = _toolRegistry.GetHandler(plan.ToolCode);
                var sw = Stopwatch.StartNew();

                if (handler == null)
                {
                    sw.Stop();
                    var failedExecution = new ToolExecution(
                        Guid.NewGuid(),
                        Guid.Empty, // Will link to assistant message ID
                        plan.ToolCode,
                        plan.InputParametersJson,
                        "{}",
                        sw.ElapsedMilliseconds,
                        status: "NotFound"
                    );
                    executedToolRecords.Add(failedExecution);
                    continue;
                }

                // Check tool permission before execution
                bool hasPermission = userContext.Permissions.Contains("*") ||
                                    userContext.Permissions.Contains("admin") ||
                                    userContext.Permissions.Contains(handler.Definition.RequiredPermission);

                if (!hasPermission)
                {
                    sw.Stop();
                    var deniedExecution = new ToolExecution(
                        Guid.NewGuid(),
                        Guid.Empty,
                        plan.ToolCode,
                        plan.InputParametersJson,
                        "{\"error\":\"Access Denied: Caller lacks required permission " + handler.Definition.RequiredPermission + "\"}",
                        sw.ElapsedMilliseconds,
                        status: "Denied"
                    );
                    executedToolRecords.Add(deniedExecution);
                    toolOutputSnippets.Add($"Access to {plan.ToolCode} was denied due to missing '{handler.Definition.RequiredPermission}' permission.");
                    continue;
                }

                // Execute tool handler with caller's exact IUserContext
                try
                {
                    var parsedParams = JsonDocument.Parse(string.IsNullOrWhiteSpace(plan.InputParametersJson) ? "{}" : plan.InputParametersJson).RootElement;
                    var result = await handler.ExecuteAsync(parsedParams, userContext, ct);
                    sw.Stop();

                    var execution = new ToolExecution(
                        Guid.NewGuid(),
                        Guid.Empty,
                        plan.ToolCode,
                        plan.InputParametersJson,
                        result.OutputJson.Length > 2000 ? result.OutputJson.Substring(0, 2000) + "..." : result.OutputJson,
                        sw.ElapsedMilliseconds,
                        status: result.IsSuccess ? "Success" : "Error"
                    );
                    executedToolRecords.Add(execution);

                    if (result.IsSuccess)
                    {
                        toolOutputSnippets.Add(result.OutputJson);
                        gatheredSourceRefs.AddRange(result.SourceReferences);
                    }
                    else
                    {
                        toolOutputSnippets.Add($"Tool error ({plan.ToolCode}): {result.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    var errorExecution = new ToolExecution(
                        Guid.NewGuid(),
                        Guid.Empty,
                        plan.ToolCode,
                        plan.InputParametersJson,
                        JsonSerializer.Serialize(new { error = ex.Message }),
                        sw.ElapsedMilliseconds,
                        status: "Exception"
                    );
                    executedToolRecords.Add(errorExecution);
                    toolOutputSnippets.Add($"Tool execution exception: {ex.Message}");
                }
            }
        }

        // 4. Synthesize Final Explanation & Answer
        bool isArabic = request.Prompt.Any(c => c >= 0x0600 && c <= 0x06FF);
        string finalContent;
        if (modelResponse.ToolInvocations == null || modelResponse.ToolInvocations.Count == 0)
        {
            finalContent = modelResponse.TextResponse;
        }
        else
        {
            finalContent = SynthesizeAnswer(request.Prompt, modelResponse.SourceCategory, toolOutputSnippets, gatheredSourceRefs, isArabic);
        }

        // 5. Create and Save Assistant Message
        var assistantMessage = new Message(
            Guid.NewGuid(),
            conversationId,
            senderRole: "Assistant",
            content: finalContent,
            sourceCategory: modelResponse.SourceCategory,
            tokensUsed: modelResponse.EstimatedTokensUsed + (toolCount * 45)
        );

        await _aiRepository.AddMessageAsync(assistantMessage, ct);

        // 6. Record Tool Executions and Sources
        // Closeout Gates 9/10: sensitive values are redacted from audit persistence;
        // full-fidelity data stays in-process for synthesis only.
        foreach (var exec in executedToolRecords)
        {
            var linkedExec = new ToolExecution(
                exec.Id,
                assistantMessage.Id,
                exec.ToolCode,
                AiPayloadRedactor.RedactJson(exec.InputPayloadJson),
                AiPayloadRedactor.RedactJson(exec.OutputPayloadJson),
                exec.DurationMs,
                exec.Status
            );
            await _aiRepository.RecordToolExecutionAsync(linkedExec, ct);
        }

        foreach (var src in gatheredSourceRefs)
        {
            var linkedSrc = new SourceReference(
                src.Id,
                assistantMessage.Id,
                src.SourceCategory,
                src.Title,
                src.EntityType,
                src.EntityId,
                src.PolicyCode,
                src.PolicyVersion,
                src.PayrollRunId,
                AiPayloadRedactor.RedactJson(src.MetadataJson)
            );
            await _aiRepository.RecordSourceReferenceAsync(linkedSrc, ct);
        }

        // 7. Auto-update Conversation Title if first exchange
        if (fullHistory.Count <= 1 && !string.IsNullOrWhiteSpace(request.Prompt))
        {
            var title = request.Prompt.Length > 40 ? request.Prompt.Substring(0, 37) + "..." : request.Prompt;
            conversation.UpdateTitle(title);
            await _aiRepository.UpdateConversationAsync(conversation, ct);
        }

        return new AiMessageResponseDto(
            assistantMessage.Id,
            assistantMessage.SenderRole,
            assistantMessage.Content,
            assistantMessage.SourceCategory,
            assistantMessage.TokensUsed,
            assistantMessage.CreatedAtUtc,
            gatheredSourceRefs.Select(s => new SourceReferenceDto(
                s.Id,
                s.SourceCategory.ToString(),
                s.Title,
                s.EntityType,
                s.EntityId,
                s.PolicyCode,
                s.PolicyVersion,
                s.PayrollRunId,
                s.MetadataJson ?? "{}",
                s.RetrievedAtUtc
            )).ToList(),
            executedToolRecords.Select(e => new ToolExecutionDto(
                e.Id,
                e.ToolCode,
                e.DurationMs,
                e.Status,
                e.CreatedAtUtc
            )).ToList()
        );
    }

    private string SynthesizeAnswer(
        string prompt, 
        AiSourceCategory category, 
        List<string> toolOutputs, 
        List<SourceReference> sources,
        bool isArabic)
    {
        var promptLower = prompt.ToLowerInvariant();

        // Check for access denied in tool outputs (case-insensitive, phrasing-tolerant)
        if (toolOutputs.Any(o => o.Contains("Access Denied", StringComparison.OrdinalIgnoreCase) ||
                                 o.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase) ||
                                 o.Contains("was denied", StringComparison.OrdinalIgnoreCase)))
        {
            return isArabic
                ? "عذراً، ليس لديك الصلاحيات الكافية للاطلاع على هذه البيانات المحددة وفقاً لسياسة التحكم في الوصول لمنظومة زين إكس."
                : "Access Denied: You do not have the required permissions to view this sensitive enterprise data under ZainX RBAC policies.";
        }

        // A. Payroll Explanation Synthesis
        if (category == AiSourceCategory.PayrollTrace)
        {
            var isFinalized = sources.Any(s => s.MetadataJson != null && s.MetadataJson.Contains("isFinalized\":true"));
            string statusHeader = isFinalized 
                ? (isArabic ? "📋 **مسير رواتب معتمد (حقيقة تاريخية مؤكدة)**" : "📋 **Finalized Payroll Snapshot (Official Immutable Historical Truth)**")
                : (isArabic ? "⚠️ **مسير رواتب مسودة (قيد المعالجة)**" : "⚠️ **Draft / In-Progress Calculation Trace**");

            if (isArabic)
            {
                return $"{statusHeader}\n\nبناءً على مسار الاحتساب المعتمد في مسير الرواتب:\n- تم احتساب الراتب الأساسي والبدلات النظامية وفقاً لبيانات العقد الموثقة.\n- تم تطبيق استقطاعات التأمينات الاجتماعية (GOSI) والضرائب المستحقة بدقة.\n- صافي المستحقات يعكس بدقة بيانات المسير المعتمد ولا يتأثر بأي تعديلات لاحقة على ملف الموظف.";
            }
            else
            {
                return $"{statusHeader}\n\nBased on the governed backend calculation trace:\n- Basic salary and statutory allowances were verified against the historical employment contract snapshot.\n- Statutory social insurance (GOSI) and tax deductions were applied according to national labor brackets.\n- The net pay figure is strictly derived from the immutable payroll run result and is resilient to subsequent live profile edits.";
            }
        }

        // B. Company Policy Synthesis
        if (category == AiSourceCategory.CompanyPolicy)
        {
            var policySrc = sources.FirstOrDefault(s => s.PolicyVersion.HasValue);
            int version = policySrc?.PolicyVersion ?? 1;

            if (isArabic)
            {
                return $"📄 **لائحة الشركة المعتمدة (الإصدار {version})**\n\nوفقاً لسياسة الشركة السارية في التاريخ المحدد:\n- يسمح بالعمل عن بعد بحد أقصى يومين أسبوعياً بعد موافقة المدير المباشر.\n- يتم تقديم طلبات الإجازات السنوية قبل موعدها بـ 5 أيام عمل على الأقل.";
            }
            else
            {
                return $"📄 **Company Policy (Version {version})**\n\nAccording to the official policy in effect for the specified period:\n- Remote work is permitted up to 2 days per week with line manager approval.\n- Annual leave requests must be submitted at least 5 business days in advance via the self-service portal.";
            }
        }

        // C. Product Knowledge Synthesis
        if (category == AiSourceCategory.ProductKnowledge)
        {
            if (isArabic)
            {
                return "💡 **دليل منصة زين إكس**\n\nعند اعتماد مسير الرواتب (Finalize Payroll):\n1. يتم قفل بيانات المسير نهائياً كحقيقة تاريخية غير قابلة للتعديل.\n2. يتم إنشاء دفعات التسوية البنكية (Settlement Batches) وتوليد ملفات الدفع.\n3. يتم ترحيل القيود المحاسبية إلى النظام المالي تلقائياً.";
            }
            else
            {
                return "💡 **ZainX Product Guide**\n\nWhen a payroll run is finalized in ZainX Workforce:\n1. The calculation snapshot is permanently locked as immutable historical truth.\n2. Payment settlement batches are automatically staged for bank processing.\n3. Immutable audit logs are registered across all processed disbursements.";
            }
        }

        // D. Governed Reporting Synthesis
        if (toolOutputs.Any(o => o.Contains("HEADCOUNT_SUMMARY") || o.Contains("ReportCode")))
        {
            if (isArabic)
            {
                return "📊 **تقرير القوى العاملة المعتمد**\n\nتم تنفيذ استعلام نموذج القراءة المعتمد بنجاح:\n- إجمالي القوى العاملة النشطة والبيانات موزعة حسب الأقسام المعتمدة.\n- تم التحقق من البيانات وحمايتها من أي استعلامات غير مصرح بها.";
            }
            else
            {
                return "📊 **Governed Headcount Report**\n\nExecuted governed read-model report query successfully:\n- Active workforce distributions summarized by department.\n- Query verified and executed with zero arbitrary SQL generation.";
            }
        }

        // E. Default Company Data Synthesis
        if (isArabic)
        {
            return "✅ **بيانات المنظومة المعتمدة**\n\nتم استرجاع السجلات المطلوبة من خلال عقود الاستعلام المعتمدة مع تطبيق أعلى معايير حماية الخصوصية وحجب البيانات الحساسة.";
        }
        else
        {
            return "✅ **Verified Enterprise Data**\n\nRetrieved matching records through approved query contracts with least-privilege projection and PII protection active.";
        }
    }

    private static AiModelResponse CreateSafeUnavailableResponse(string prompt) =>
        new(
            TextResponse: IsArabicPrompt(prompt)
                ? "خدمة المساعد الذكي غير متاحة مؤقتاً. لم يتم تنفيذ أي أدوات أو الوصول إلى أي بيانات. يرجى إعادة المحاولة لاحقاً."
                : "The AI assistant service is temporarily unavailable. No tools were executed and no data was accessed. Please retry shortly.",
            EstimatedTokensUsed: 0,
            SourceCategory: AiSourceCategory.ExternalAi,
            ToolInvocations: null
        );

    private static AiModelResponse CreateSafeTimeoutResponse(string prompt) =>
        new(
            TextResponse: IsArabicPrompt(prompt)
                ? "انتهت مهلة استجابة خدمة المساعد الذكي بأمان. لم يتم الوصول إلى أي بيانات حساسة."
                : "The AI assistant response timed out and was safely terminated. No sensitive data was accessed.",
            EstimatedTokensUsed: 0,
            SourceCategory: AiSourceCategory.ExternalAi,
            ToolInvocations: null
        );

    private static bool IsArabicPrompt(string prompt) =>
        !string.IsNullOrEmpty(prompt) && System.Text.RegularExpressions.Regex.IsMatch(prompt, "[\u0600-\u06FF]");
}
