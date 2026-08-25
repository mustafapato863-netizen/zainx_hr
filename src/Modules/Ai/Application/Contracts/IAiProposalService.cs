using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Ai.Application.Contracts;

public interface IAiProposalService
{
    Task<AiActionProposalDto> CreateProposalAsync(
        CreateProposalRequest request,
        IUserContext userContext,
        CancellationToken ct = default);

    Task<AiActionProposalDto?> GetProposalAsync(
        Guid proposalId,
        IUserContext userContext,
        CancellationToken ct = default);

    Task<IReadOnlyList<AiActionProposalDto>> ListProposalsAsync(
        IUserContext userContext,
        int limit = 50,
        CancellationToken ct = default);

    Task<AiProposalExecutionResponseDto> ConfirmProposalAsync(
        Guid proposalId,
        ConfirmProposalRequest request,
        IUserContext userContext,
        CancellationToken ct = default);

    Task<AiActionProposalDto> CancelProposalAsync(
        Guid proposalId,
        CancelProposalRequest request,
        IUserContext userContext,
        CancellationToken ct = default);
}
