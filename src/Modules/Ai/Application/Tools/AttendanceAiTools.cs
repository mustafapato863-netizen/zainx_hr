using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Workforce.Modules.Ai.Application.Contracts;
using Workforce.Modules.Ai.Domain;
using Workforce.Modules.Attendance.Infrastructure;
using Workforce.SharedKernel.Primitives;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Ai.Application.Tools;

public sealed class AttendanceGetRecordsToolHandler : IAiToolHandler
{
    private readonly IAttendanceRepository _attendanceRepository;

    public AiToolDefinition Definition { get; } = new(
        toolCode: "attendance.get_records",
        descriptionEn: "Retrieve aggregated attendance day logs, hours worked, and overtime for active employees within a date range.",
        descriptionAr: "استرجاع سجلات الحضور اليومية وساعات العمل والإضافي للموظفين.",
        requiredPermission: "attendance.clock.create",
        dataClassification: "Internal",
        inputSchemaJson: "{\"type\":\"object\",\"properties\":{\"fromDate\":{\"type\":\"string\"},\"toDate\":{\"type\":\"string\"},\"limit\":{\"type\":\"integer\"}}}"
    );

    public AttendanceGetRecordsToolHandler(IAttendanceRepository attendanceRepository)
    {
        _attendanceRepository = attendanceRepository;
    }

    public async Task<AiToolResult> ExecuteAsync(JsonElement inputParams, IUserContext userContext, CancellationToken ct = default)
    {
        DateOnly? fromDate = null;
        DateOnly? toDate = null;

        if (inputParams.TryGetProperty("fromDate", out var f) && DateOnly.TryParse(f.GetString(), out var fd)) fromDate = fd;
        if (inputParams.TryGetProperty("toDate", out var t) && DateOnly.TryParse(t.GetString(), out var td)) toDate = td;

        var result = await _attendanceRepository.GetAttendanceDaysAsync(
            userContext.TenantId, 
            userContext.LegalEntityId, 
            fromDate, 
            toDate, 
            status: null, 
            page: 1, 
            pageSize: 20);

        var projections = new List<object>();
        var sourceRefs = new List<SourceReference>();

        foreach (var item in result.Items)
        {
            projections.Add(new
            {
                AttendanceId = item.Id,
                EmploymentId = item.EmploymentId,
                BusinessDate = item.BusinessDate,
                Status = item.Status,
                ScheduledMinutes = item.ScheduledMinutes,
                WorkedMinutes = item.TotalWorkedMinutes,
                LateMinutes = item.LateMinutes,
                IsAbsent = item.IsAbsent
            });

            sourceRefs.Add(new SourceReference(
                Guid.NewGuid(),
                Guid.Empty,
                AiSourceCategory.CompanyData,
                $"Attendance Record: {item.BusinessDate}",
                entityType: "AttendanceDay",
                entityId: item.Id.ToString()
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

public sealed class AttendanceGetExceptionsToolHandler : IAiToolHandler
{
    private readonly IAttendanceRepository _attendanceRepository;

    public AiToolDefinition Definition { get; } = new(
        toolCode: "attendance.get_exceptions",
        descriptionEn: "Retrieve pending attendance exceptions such as unapproved lateness, missing clock-outs, and absence penalties.",
        descriptionAr: "استرجاع استثناءات الحضور المعلقة مثل التأخير غير المعتمد والانقطاع.",
        requiredPermission: "attendance.exception.resolve",
        dataClassification: "Internal",
        inputSchemaJson: "{\"type\":\"object\",\"properties\":{\"limit\":{\"type\":\"integer\"}}}"
    );

    public AttendanceGetExceptionsToolHandler(IAttendanceRepository attendanceRepository)
    {
        _attendanceRepository = attendanceRepository;
    }

    public async Task<AiToolResult> ExecuteAsync(JsonElement inputParams, IUserContext userContext, CancellationToken ct = default)
    {
        var result = await _attendanceRepository.GetExceptionsQueueAsync(userContext.TenantId, status: null, page: 1, pageSize: 20);

        var projections = new List<object>();
        var sourceRefs = new List<SourceReference>();

        foreach (var item in result.Items)
        {
            projections.Add(new
            {
                ExceptionId = item.Id,
                AttendanceDayId = item.AttendanceDayId,
                EmploymentId = item.EmploymentId,
                ExceptionType = item.Type,
                Status = item.Status,
                Details = item.Details,
                CreatedAtUtc = item.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm")
            });

            sourceRefs.Add(new SourceReference(
                Guid.NewGuid(),
                Guid.Empty,
                AiSourceCategory.CompanyData,
                $"Attendance Exception: {item.Type}",
                entityType: "AttendanceException",
                entityId: item.Id.ToString()
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
