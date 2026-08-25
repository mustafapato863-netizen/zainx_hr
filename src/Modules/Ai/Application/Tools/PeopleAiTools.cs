using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Workforce.Modules.Ai.Application.Contracts;
using Workforce.Modules.Ai.Domain;
using Workforce.Modules.People.Infrastructure;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Ai.Application.Tools;

public sealed class PeopleSearchToolHandler : IAiToolHandler
{
    private readonly PeopleRepository _peopleRepository;

    public AiToolDefinition Definition { get; } = new(
        toolCode: "people.search",
        descriptionEn: "Search active directory employees by name, department, or job title with least-privilege projection.",
        descriptionAr: "البحث في دليل الموظفين بالاسم أو القسم أو المسمى الوظيفي مع حماية البيانات الحساسة.",
        requiredPermission: "people.employee.read",
        dataClassification: "Internal",
        inputSchemaJson: "{\"type\":\"object\",\"properties\":{\"query\":{\"type\":\"string\"},\"department\":{\"type\":\"string\"},\"limit\":{\"type\":\"integer\"}}}"
    );

    public PeopleSearchToolHandler(PeopleRepository peopleRepository)
    {
        _peopleRepository = peopleRepository;
    }

    public async Task<AiToolResult> ExecuteAsync(JsonElement inputParams, IUserContext userContext, CancellationToken ct = default)
    {
        var query = inputParams.TryGetProperty("query", out var q) ? q.GetString() : null;
        var limit = inputParams.TryGetProperty("limit", out var l) && l.TryGetInt32(out var lim) ? Math.Min(lim, 20) : 10;

        var pagedResult = await _peopleRepository.QueryDirectoryAsync(
            userContext.TenantId,
            userContext.LegalEntityId,
            query,
            departmentId: null,
            status: null,
            pageNumber: 1,
            pageSize: limit,
            ct: ct
        );

        var projections = new List<object>();
        var sourceRefs = new List<SourceReference>();

        foreach (var emp in pagedResult.Items)
        {
            projections.Add(new
            {
                EmployeeId = emp.Id,
                EmployeeNumber = emp.EmployeeNumber,
                FullNameEn = emp.FullNameEn,
                FullNameAr = emp.FullNameAr,
                JobTitleEn = emp.JobTitleEn,
                DepartmentEn = emp.DepartmentNameEn,
                EmploymentStatus = emp.Status,
                HireDate = emp.HireDate
            });

            sourceRefs.Add(new SourceReference(
                Guid.NewGuid(),
                Guid.Empty,
                AiSourceCategory.CompanyData,
                $"Employee: {emp.FullNameEn} ({emp.EmployeeNumber})",
                entityType: "Employee",
                entityId: emp.Id.ToString()
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

public sealed class PeopleGetSummaryToolHandler : IAiToolHandler
{
    private readonly PeopleRepository _peopleRepository;

    public AiToolDefinition Definition { get; } = new(
        toolCode: "people.get_summary",
        descriptionEn: "Retrieve a summary profile of an employee, omitting national ID, bank IBAN, and personal contact info.",
        descriptionAr: "استرجاع ملخص الملف التعريفي للموظف مع حجب رقم الهوية والحساب البنكي.",
        requiredPermission: "people.employee.read",
        dataClassification: "Internal",
        inputSchemaJson: "{\"type\":\"object\",\"required\":[\"employeeId\"],\"properties\":{\"employeeId\":{\"type\":\"string\"}}}"
    );

    public PeopleGetSummaryToolHandler(PeopleRepository peopleRepository)
    {
        _peopleRepository = peopleRepository;
    }

    public async Task<AiToolResult> ExecuteAsync(JsonElement inputParams, IUserContext userContext, CancellationToken ct = default)
    {
        if (!inputParams.TryGetProperty("employeeId", out var idProp) || !Guid.TryParse(idProp.GetString(), out var empId))
        {
            return new AiToolResult(false, "{}", AiSourceCategory.CompanyData, new(), "Invalid or missing employeeId.");
        }

        var profile = await _peopleRepository.GetEmployeeProfileAsync(empId, userContext.TenantId, userContext.LegalEntityId, ct);

        if (profile == null)
        {
            return new AiToolResult(false, "{}", AiSourceCategory.CompanyData, new(), "Employee not found or unauthorized.");
        }

        var safeSummary = new
        {
            EmployeeId = profile.Id,
            EmployeeNumber = profile.EmployeeNumber,
            FullNameEn = $"{profile.FirstNameEn} {profile.LastNameEn}".Trim(),
            FullNameAr = $"{profile.FirstNameAr} {profile.LastNameAr}".Trim(),
            EmploymentStatus = profile.Status.ToString(),
            HireDate = profile.HireDate,
            WorkLocation = "Headquarters"
        };

        var sourceRefs = new List<SourceReference>
        {
            new SourceReference(
                Guid.NewGuid(),
                Guid.Empty,
                AiSourceCategory.CompanyData,
                $"Employee Profile: {safeSummary.FullNameEn}",
                entityType: "Employee",
                entityId: profile.Id.ToString()
            )
        };

        return new AiToolResult(
            IsSuccess: true,
            OutputJson: JsonSerializer.Serialize(safeSummary),
            SourceCategory: AiSourceCategory.CompanyData,
            SourceReferences: sourceRefs
        );
    }
}
