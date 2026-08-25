using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Workforce.Modules.Leave.Application.Contracts;
using Workforce.Modules.Leave.Infrastructure;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Leave.Application.Services;

public sealed class LeaveSelfServiceQueryService : ILeaveSelfServiceQueryContract
{
    private readonly ILeaveRepository _repository;

    public LeaveSelfServiceQueryService(ILeaveRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public Task<IReadOnlyList<LeaveTypeDto>> GetTypesAsync(
        TenantId tenantId,
        LegalEntityId legalEntityId,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return _repository.GetLeaveTypesAsync(tenantId, legalEntityId);
    }

    public Task<IReadOnlyList<LeaveBalanceDto>> GetBalancesAsync(
        TenantId tenantId,
        LegalEntityId legalEntityId,
        Guid employmentId,
        int year,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return _repository.GetLeaveBalancesAsync(tenantId, employmentId, year, legalEntityId);
    }

    public Task<(IReadOnlyList<LeaveRequestDto> Items, int TotalCount)> GetRequestsAsync(
        TenantId tenantId,
        LegalEntityId legalEntityId,
        Guid employmentId,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return _repository.GetLeaveRequestsAsync(tenantId, legalEntityId, employmentId, null, page, pageSize);
    }

    public Task<LeaveRequestDto?> GetRequestByIdAsync(
        TenantId tenantId,
        LegalEntityId legalEntityId,
        Guid requestId,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return _repository.GetLeaveRequestByIdAsync(tenantId, requestId, legalEntityId);
    }
}
