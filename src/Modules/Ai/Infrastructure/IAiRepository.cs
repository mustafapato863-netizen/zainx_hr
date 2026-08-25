using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Workforce.Modules.Ai.Domain;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Ai.Infrastructure;

public interface IAiRepository
{
    // Conversations
    Task CreateConversationAsync(Conversation conversation, CancellationToken ct = default);
    Task<Conversation?> GetConversationByIdAsync(TenantId tenantId, Guid conversationId, CancellationToken ct = default);
    Task<IReadOnlyList<Conversation>> ListConversationsAsync(TenantId tenantId, UserId userId, int limit = 50, CancellationToken ct = default);
    Task UpdateConversationAsync(Conversation conversation, CancellationToken ct = default);

    // Messages
    Task AddMessageAsync(Message message, CancellationToken ct = default);
    Task<IReadOnlyList<Message>> GetMessagesByConversationIdAsync(Guid conversationId, CancellationToken ct = default);

    // Tool Executions & Source References
    Task RecordToolExecutionAsync(ToolExecution execution, CancellationToken ct = default);
    Task RecordSourceReferenceAsync(SourceReference reference, CancellationToken ct = default);
    Task<IReadOnlyList<ToolExecution>> GetToolExecutionsByMessageIdAsync(Guid messageId, CancellationToken ct = default);
    Task<IReadOnlyList<SourceReference>> GetSourceReferencesByMessageIdAsync(Guid messageId, CancellationToken ct = default);

    // Closeout Gate 12: batched conversation-level fetches (N+1 elimination)
    Task<IReadOnlyList<ToolExecution>> GetToolExecutionsByConversationIdAsync(Guid conversationId, CancellationToken ct = default);
    Task<IReadOnlyList<SourceReference>> GetSourceReferencesByConversationIdAsync(Guid conversationId, CancellationToken ct = default);

    // Closeout Gate 10: configurable conversation retention purge
    Task<int> PurgeConversationsOlderThanAsync(int retentionDays, CancellationToken ct = default);

    // Company Policies (Effective-Dated)
    Task CreatePolicyAsync(CompanyPolicy policy, CancellationToken ct = default);
    Task<CompanyPolicy?> GetEffectivePolicyAsync(TenantId tenantId, string policyCode, DateTime targetDateUtc, CancellationToken ct = default);
    Task<IReadOnlyList<CompanyPolicy>> SearchPoliciesAsync(TenantId tenantId, string? query, DateTime? effectiveAtUtc = null, CancellationToken ct = default);

    // Product Knowledge
    Task CreateProductKnowledgeAsync(ProductKnowledgeArticle article, CancellationToken ct = default);
    Task<ProductKnowledgeArticle?> GetProductKnowledgeByTopicAsync(string topicCode, CancellationToken ct = default);
    Task<IReadOnlyList<ProductKnowledgeArticle>> SearchProductKnowledgeAsync(string query, CancellationToken ct = default);

    // Phase 7B: Action Proposals & Executions
    Task CreateProposalAsync(AiActionProposal proposal, CancellationToken ct = default);
    Task<AiActionProposal?> GetProposalByIdAsync(TenantId tenantId, Guid proposalId, CancellationToken ct = default);
    Task UpdateProposalAsync(AiActionProposal proposal, CancellationToken ct = default);
    Task<IReadOnlyList<AiActionProposal>> ListProposalsAsync(TenantId tenantId, UserId userId, int limit = 50, CancellationToken ct = default);
    Task RecordActionExecutionAsync(AiActionExecution execution, CancellationToken ct = default);
    Task<AiActionExecution?> GetExecutionByIdempotencyKeyAsync(TenantId tenantId, string idempotencyKey, CancellationToken ct = default);
}
