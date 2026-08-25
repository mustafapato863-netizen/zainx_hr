using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Workforce.Modules.Leave.Infrastructure;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Leave.Application.Contracts;

public interface ILeaveSelfServiceQueryContract
{
    Task<IReadOnlyList<LeaveTypeDto>> GetTypesAsync(
        TenantId tenantId,
        LegalEntityId legalEntityId,
        CancellationToken ct = default);

    Task<IReadOnlyList<LeaveBalanceDto>> GetBalancesAsync(
        TenantId tenantId,
        LegalEntityId legalEntityId,
        Guid employmentId,
        int year,
        CancellationToken ct = default);

    Task<(IReadOnlyList<LeaveRequestDto> Items, int TotalCount)> GetRequestsAsync(
        TenantId tenantId,
        LegalEntityId legalEntityId,
        Guid employmentId,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<LeaveRequestDto?> GetRequestByIdAsync(
        TenantId tenantId,
        LegalEntityId legalEntityId,
        Guid requestId,
        CancellationToken ct = default);
}
