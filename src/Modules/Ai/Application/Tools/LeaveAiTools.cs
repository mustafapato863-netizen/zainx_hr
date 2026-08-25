using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Workforce.Modules.Ai.Application.Contracts;
using Workforce.Modules.Ai.Domain;
using Workforce.Modules.Leave.Infrastructure;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Ai.Application.Tools;

public sealed class LeaveGetBalanceSummaryToolHandler : IAiToolHandler
{
    private readonly ILeaveRepository _leaveRepository;

    public AiToolDefinition Definition { get; } = new(
        toolCode: "leave.get_balance_summary",
        descriptionEn: "Retrieve leave entitlement, consumed days, and remaining available balances for an employee.",
        descriptionAr: "استرجاع أرصدة الإجازات المستحقة والمستهلكة والمتبقية للموظف.",
        requiredPermission: "leave.request.create",
        dataClassification: "Internal",
        inputSchemaJson: "{\"type\":\"object\",\"required\":[\"employmentId\"],\"properties\":{\"employmentId\":{\"type\":\"string\"},\"year\":{\"type\":\"integer\"}}}"
    );

    public LeaveGetBalanceSummaryToolHandler(ILeaveRepository leaveRepository)
    {
        _leaveRepository = leaveRepository;
    }

    public async Task<AiToolResult> ExecuteAsync(JsonElement inputParams, IUserContext userContext, CancellationToken ct = default)
    {
        if (!inputParams.TryGetProperty("employmentId", out var idProp) || !Guid.TryParse(idProp.GetString(), out var empId))
        {
            return new AiToolResult(false, "{}", AiSourceCategory.CompanyData, new(), "Invalid or missing employmentId.");
        }

        var year = inputParams.TryGetProperty("year", out var y) && y.TryGetInt32(out var yr) ? yr : DateTime.UtcNow.Year;
        var balances = await _leaveRepository.GetLeaveBalancesAsync(userContext.TenantId, empId, year, userContext.LegalEntityId);

        var projections = new List<object>();
        var sourceRefs = new List<SourceReference>();

        foreach (var b in balances)
        {
            projections.Add(new
            {
                LeaveTypeName = b.LeaveTypeNameEn,
                LeaveTypeNameAr = b.LeaveTypeNameAr,
                Year = b.Year,
                EntitledDays = b.EntitledDays,
                AccruedDays = b.AccruedDays,
                UsedDays = b.UsedDays,
                PendingDays = b.PendingDays,
                AvailableDays = b.AvailableDays
            });

            sourceRefs.Add(new SourceReference(
                Guid.NewGuid(),
                Guid.Empty,
                AiSourceCategory.CompanyData,
                $"Leave Balance: {b.LeaveTypeNameEn} ({year})",
                entityType: "LeaveBalance",
                entityId: b.Id.ToString()
            ));
        }

        return new AiToolResult(
            IsSuccess: true,
            OutputJson: JsonSerializer.Serialize(projections),
            SourceCategory: AiSourceCategory.CompanyData,
            SourceReferences: sourceRefs
        );
    }
}

public sealed class LeaveGetRequestSummaryToolHandler : IAiToolHandler
{
    private readonly ILeaveRepository _leaveRepository;

    public AiToolDefinition Definition { get; } = new(
        toolCode: "leave.get_request_summary",
        descriptionEn: "Retrieve leave request history, approval status, and date intervals for an employee.",
        descriptionAr: "استرجاع سجل طلبات الإجازات وحالة الموافقات والتواريخ.",
        requiredPermission: "leave.request.create",
        dataClassification: "Internal",
        inputSchemaJson: "{\"type\":\"object\",\"properties\":{\"employmentId\":{\"type\":\"string\"},\"status\":{\"type\":\"integer\"}}}"
    );

    public LeaveGetRequestSummaryToolHandler(ILeaveRepository leaveRepository)
    {
        _leaveRepository = leaveRepository;
    }

    public async Task<AiToolResult> ExecuteAsync(JsonElement inputParams, IUserContext userContext, CancellationToken ct = default)
    {
        Guid? empId = null;
        int? status = null;

        if (inputParams.TryGetProperty("employmentId", out var idProp) && Guid.TryParse(idProp.GetString(), out var eid)) empId = eid;
        if (inputParams.TryGetProperty("status", out var sProp) && sProp.TryGetInt32(out var s)) status = s;

        var result = await _leaveRepository.GetLeaveRequestsAsync(
            userContext.TenantId, 
            userContext.LegalEntityId, 
            empId, 
            status, 
            page: 1, 
            pageSize: 20);

        var projections = new List<object>();
        var sourceRefs = new List<SourceReference>();

        foreach (var req in result.Items)
        {
            projections.Add(new
            {
                RequestId = req.Id,
                EmploymentId = req.EmploymentId,
                StartDate = req.StartDate,
                EndDate = req.EndDate,
                DurationDays = req.DurationDays,
                Status = req.Status,
                Reason = req.Reason
            });

            sourceRefs.Add(new SourceReference(
                Guid.NewGuid(),
                Guid.Empty,
                AiSourceCategory.CompanyData,
                $"Leave Request: {req.StartDate} to {req.EndDate}",
                entityType: "LeaveRequest",
                entityId: req.Id.ToString()
            ));
        }

        return new AiToolResult(
            IsSuccess: true,
            OutputJson: JsonSerializer.Serialize(projections),
            SourceCategory: AiSourceCategory.CompanyData,
            SourceReferences: sourceRefs
        );
    }
}
