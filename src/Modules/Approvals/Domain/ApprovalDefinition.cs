using System;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Approvals.Domain;

public class ApprovalDefinition
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public string ModuleName { get; private set; }
    public string WorkflowType { get; private set; }
    public string NameEn { get; private set; }
    public string NameAr { get; private set; }
    public int StepsCount { get; private set; }
    public bool IsActive { get; private set; }

    private ApprovalDefinition()
    {
        ModuleName = string.Empty;
        WorkflowType = string.Empty;
        NameEn = string.Empty;
        NameAr = string.Empty;
    }

    public ApprovalDefinition(
        Guid id,
        TenantId tenantId,
        string moduleName,
        string workflowType,
        string nameEn,
        string nameAr,
        int stepsCount = 1,
        bool isActive = true)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(moduleName)) throw new ArgumentException("ModuleName is required.", nameof(moduleName));
        if (string.IsNullOrWhiteSpace(workflowType)) throw new ArgumentException("WorkflowType is required.", nameof(workflowType));

        Id = id;
        TenantId = tenantId;
        ModuleName = moduleName.Trim();
        WorkflowType = workflowType.Trim();
        NameEn = nameEn.Trim();
        NameAr = nameAr.Trim();
        StepsCount = Math.Max(1, stepsCount);
        IsActive = isActive;
    }
}
