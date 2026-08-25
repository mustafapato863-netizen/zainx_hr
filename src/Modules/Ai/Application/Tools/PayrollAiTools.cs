using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Workforce.Modules.Ai.Application.Contracts;
using Workforce.Modules.Ai.Domain;
using Workforce.Modules.Payroll.Domain;
using Workforce.Modules.Payroll.Infrastructure;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Ai.Application.Tools;

public sealed class PayrollGetRunSummaryToolHandler : IAiToolHandler
{
    private readonly IPayrollRepository _payrollRepository;

    public AiToolDefinition Definition { get; } = new(
        toolCode: "payroll.get_run_summary",
        descriptionEn: "Retrieve aggregated payroll run summary (gross pay, net pay, statutory deductions) and finalize status.",
        descriptionAr: "استرجاع ملخص مسير الرواتب (إجمالي الرواتب، صافي المستحقات، الاستقطاعات) وحالة الاعتماد.",
        requiredPermission: "payroll.run.read",
        dataClassification: "Confidential",
        inputSchemaJson: "{\"type\":\"object\",\"required\":[\"payrollRunId\"],\"properties\":{\"payrollRunId\":{\"type\":\"string\"}}}"
    );

    public PayrollGetRunSummaryToolHandler(IPayrollRepository payrollRepository)
    {
        _payrollRepository = payrollRepository;
    }

    public async Task<AiToolResult> ExecuteAsync(JsonElement inputParams, IUserContext userContext, CancellationToken ct = default)
    {
        if (!inputParams.TryGetProperty("payrollRunId", out var idProp) || !Guid.TryParse(idProp.GetString(), out var runId))
        {
            return new AiToolResult(false, "{}", AiSourceCategory.PayrollTrace, new(), "Invalid or missing payrollRunId.");
        }

        var run = await _payrollRepository.GetRunByIdAsync(userContext.TenantId, runId, ct);
        if (run == null || (userContext.LegalEntityId.HasValue && run.LegalEntityId != userContext.LegalEntityId.Value))
        {
            return new AiToolResult(false, "{}", AiSourceCategory.PayrollTrace, new(), "Payroll run not found or access denied.");
        }

        // SEMANTIC domain states only: Finalized & OutputsPublished are the immutable historical truth.
        // Approved (5) is NOT final - numeric comparisons like (int)Status >= 5 are forbidden here.
        bool isFinalized = run.Status == PayrollRunStatus.Finalized || run.Status == PayrollRunStatus.OutputsPublished;
        string statusLabel = isFinalized
            ? (run.Status == PayrollRunStatus.OutputsPublished ? "Finalized / Outputs Published (Official Historical Truth)" : "Finalized (Official Historical Truth)")
            : $"Draft / Non-Final ({run.Status})";

        var summary = new
        {
            PayrollRunId = run.Id,
            PeriodId = run.PeriodId,
            RunStatus = statusLabel,
            IsFinalized = isFinalized,
            TotalGross = run.TotalGross,
            TotalNet = run.TotalNet,
            TotalEmployerContributions = run.TotalEmployerContributions,
            EmployeeCount = run.EmployeeCount,
            FinalizedAtUtc = run.FinalizedAtUtc?.ToString("yyyy-MM-dd HH:mm:ss")
        };

        var sourceRefs = new List<SourceReference>
        {
            new SourceReference(
                Guid.NewGuid(),
                Guid.Empty,
                AiSourceCategory.PayrollTrace,
                $"Payroll Run: {run.Id} ({statusLabel})",
                entityType: "PayrollRun",
                entityId: run.Id.ToString(),
                payrollRunId: run.Id,
                metadataJson: JsonSerializer.Serialize(new { isFinalized, status = run.Status.ToString() })
            )
        };

        return new AiToolResult(
            IsSuccess: true,
            OutputJson: JsonSerializer.Serialize(summary),
            SourceCategory: AiSourceCategory.PayrollTrace,
            SourceReferences: sourceRefs
        );
    }
}

public sealed class PayrollGetEmployeeTraceToolHandler : IAiToolHandler
{
    private readonly IPayrollRepository _payrollRepository;

    public AiToolDefinition Definition { get; } = new(
        toolCode: "payroll.get_employee_trace",
        descriptionEn: "Retrieve backend-generated payroll calculation trace, earnings breakdown, and statutory contributions for an employee.",
        descriptionAr: "استرجاع مسار الاحتساب الفعلي لمسير الراتب ومفردات الراتب والاستقطاعات النظامية.",
        requiredPermission: "payroll.run.read",
        dataClassification: "Confidential",
        inputSchemaJson: "{\"type\":\"object\",\"required\":[\"payrollRunId\",\"employmentId\"],\"properties\":{\"payrollRunId\":{\"type\":\"string\"},\"employmentId\":{\"type\":\"string\"}}}"
    );

    public PayrollGetEmployeeTraceToolHandler(IPayrollRepository payrollRepository)
    {
        _payrollRepository = payrollRepository;
    }

    public async Task<AiToolResult> ExecuteAsync(JsonElement inputParams, IUserContext userContext, CancellationToken ct = default)
    {
        // Enforce sensitive payroll read permission
        bool isSuperAdmin = userContext.Permissions.Contains("*") || userContext.Permissions.Contains("admin");
        if (!isSuperAdmin && !userContext.Permissions.Contains("payroll.result.read_sensitive") && !userContext.Permissions.Contains("payroll.run.read"))
        {
            return new AiToolResult(false, "{}", AiSourceCategory.PayrollTrace, new(), "Unauthorized: Missing 'payroll.result.read_sensitive' permission.");
        }

        if (!inputParams.TryGetProperty("payrollRunId", out var runProp) || !Guid.TryParse(runProp.GetString(), out var runId) ||
            !inputParams.TryGetProperty("employmentId", out var empProp) || !Guid.TryParse(empProp.GetString(), out var empId))
        {
            return new AiToolResult(false, "{}", AiSourceCategory.PayrollTrace, new(), "Invalid payrollRunId or employmentId.");
        }

        var run = await _payrollRepository.GetRunByIdAsync(userContext.TenantId, runId, ct);
        if (run == null || (userContext.LegalEntityId.HasValue && run.LegalEntityId != userContext.LegalEntityId.Value))
        {
            return new AiToolResult(false, "{}", AiSourceCategory.PayrollTrace, new(), "Payroll run not found or access denied.");
        }

        var result = await _payrollRepository.GetEmployeeResultDetailAsync(runId, empId, ct);
        if (result == null)
        {
            return new AiToolResult(false, "{}", AiSourceCategory.PayrollTrace, new(), "Employee payroll result not found in this run.");
        }

        // SEMANTIC domain states only: Approved must never be represented as final.
        bool isFinalized = run.Status == PayrollRunStatus.Finalized || run.Status == PayrollRunStatus.OutputsPublished;

        var traceDto = new
        {
            PayrollRunId = run.Id,
            EmploymentId = result.EmploymentId,
            IsFinalized = isFinalized,
            DataProvenance = isFinalized ? "Finalized Payroll Snapshot (Immutable Historical Truth)" : "Draft Calculation Trace",
            GrossPay = result.GrossPay,
            TotalEarnings = result.TotalEarnings,
            TotalDeductions = result.TotalDeductions,
            EmployerContributions = result.EmployerContributions,
            NetPay = result.NetPay
        };

        var sourceRefs = new List<SourceReference>
        {
            new SourceReference(
                Guid.NewGuid(),
                Guid.Empty,
                AiSourceCategory.PayrollTrace,
                $"Calculation Trace: Employment {empId} in Run {runId}",
                entityType: "PayrollEmployeeResult",
                entityId: result.Id.ToString(),
                payrollRunId: run.Id,
                metadataJson: JsonSerializer.Serialize(new { isFinalized, netPay = result.NetPay, grossPay = result.GrossPay })
            )
        };

        return new AiToolResult(
            IsSuccess: true,
            OutputJson: JsonSerializer.Serialize(traceDto),
            SourceCategory: AiSourceCategory.PayrollTrace,
            SourceReferences: sourceRefs
        );
    }
}

public sealed class PayrollExplainExceptionToolHandler : IAiToolHandler
{
    private readonly IPayrollRepository _payrollRepository;

    public AiToolDefinition Definition { get; } = new(
        toolCode: "payroll.explain_exception",
        descriptionEn: "Explain anomalies, attendance deduction penalties, or calculation exceptions recorded during payroll processing.",
        descriptionAr: "شرح استثناءات وفروقات احتساب الرواتب والخصومات النظامية أو الجزاءات.",
        requiredPermission: "payroll.exceptions.resolve",
        dataClassification: "Confidential",
        inputSchemaJson: "{\"type\":\"object\",\"required\":[\"payrollRunId\"],\"properties\":{\"payrollRunId\":{\"type\":\"string\"}}}"
    );

    public PayrollExplainExceptionToolHandler(IPayrollRepository payrollRepository)
    {
        _payrollRepository = payrollRepository;
    }

    public async Task<AiToolResult> ExecuteAsync(JsonElement inputParams, IUserContext userContext, CancellationToken ct = default)
    {
        if (!inputParams.TryGetProperty("payrollRunId", out var idProp) || !Guid.TryParse(idProp.GetString(), out var runId))
        {
            return new AiToolResult(false, "{}", AiSourceCategory.PayrollTrace, new(), "Invalid or missing payrollRunId.");
        }

        var run = await _payrollRepository.GetRunByIdAsync(userContext.TenantId, runId, ct);
        if (run == null || (userContext.LegalEntityId.HasValue && run.LegalEntityId != userContext.LegalEntityId.Value))
        {
            return new AiToolResult(false, "{}", AiSourceCategory.PayrollTrace, new(), "Payroll run not found or access denied.");
        }

        var exceptions = await _payrollRepository.GetExceptionsByRunAsync(runId, ct);

        var projections = new List<object>();
        var sourceRefs = new List<SourceReference>();

        foreach (var ex in exceptions)
        {
            projections.Add(new
            {
                ExceptionId = ex.Id,
                EmploymentId = ex.EmploymentId,
                Category = ex.Category,
                Severity = ex.Severity.ToString(),
                Reason = ex.Reason,
                ResolutionGuidance = ex.ResolutionGuidance,
                Status = ex.Status.ToString()
            });

            sourceRefs.Add(new SourceReference(
                Guid.NewGuid(),
                Guid.Empty,
                AiSourceCategory.PayrollTrace,
                $"Payroll Exception: {ex.Category} - {ex.Reason}",
                entityType: "PayrollException",
                entityId: ex.Id.ToString(),
                payrollRunId: runId
            ));
        }

        return new AiToolResult(
            IsSuccess: true,
            OutputJson: JsonSerializer.Serialize(projections),
            SourceCategory: AiSourceCategory.PayrollTrace,
            SourceReferences: sourceRefs
        );
    }
}
