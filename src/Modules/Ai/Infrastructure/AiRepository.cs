using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Workforce.Modules.Ai.Domain;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Ai.Infrastructure;

public sealed class AiRepository : IAiRepository
{
    private readonly string _connectionString;

    public AiRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    private async Task<NpgsqlConnection> CreateOpenConnectionAsync(CancellationToken ct)
    {
        var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        return conn;
    }

    // =========================================================================
    // 1. CONVERSATIONS
    // =========================================================================

    public async Task CreateConversationAsync(Conversation conversation, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        INSERT INTO ai.conversations (
            id, tenant_id, legal_entity_id, user_id, title, context_entity_type, context_entity_id, created_at_utc, updated_at_utc
        ) VALUES (
            @id, @tenant_id, @legal_entity_id, @user_id, @title, @context_entity_type, @context_entity_id, @created_at_utc, @updated_at_utc
        );
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", conversation.Id);
        cmd.Parameters.AddWithValue("tenant_id", conversation.TenantId.Value);
        cmd.Parameters.AddWithValue("legal_entity_id", (object?)conversation.LegalEntityId?.Value ?? DBNull.Value);
        cmd.Parameters.AddWithValue("user_id", conversation.UserId.Value);
        cmd.Parameters.AddWithValue("title", conversation.Title);
        cmd.Parameters.AddWithValue("context_entity_type", (object?)conversation.ContextEntityType ?? DBNull.Value);
        cmd.Parameters.AddWithValue("context_entity_id", (object?)conversation.ContextEntityId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("created_at_utc", conversation.CreatedAtUtc);
        cmd.Parameters.AddWithValue("updated_at_utc", conversation.UpdatedAtUtc);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<Conversation?> GetConversationByIdAsync(TenantId tenantId, Guid conversationId, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        SELECT id, tenant_id, legal_entity_id, user_id, title, context_entity_type, context_entity_id, created_at_utc, updated_at_utc
        FROM ai.conversations
        WHERE tenant_id = @tenant_id AND id = @id;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenant_id", tenantId.Value);
        cmd.Parameters.AddWithValue("id", conversationId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        return MapConversation(reader);
    }

    public async Task<IReadOnlyList<Conversation>> ListConversationsAsync(TenantId tenantId, UserId userId, int limit = 50, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        SELECT id, tenant_id, legal_entity_id, user_id, title, context_entity_type, context_entity_id, created_at_utc, updated_at_utc
        FROM ai.conversations
        WHERE tenant_id = @tenant_id AND user_id = @user_id
        ORDER BY updated_at_utc DESC
        LIMIT @limit;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenant_id", tenantId.Value);
        cmd.Parameters.AddWithValue("user_id", userId.Value);
        cmd.Parameters.AddWithValue("limit", Math.Min(limit, 100));

        var list = new List<Conversation>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(MapConversation(reader));
        }

        return list;
    }

    public async Task UpdateConversationAsync(Conversation conversation, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        UPDATE ai.conversations
        SET title = @title, updated_at_utc = @updated_at_utc
        WHERE id = @id AND tenant_id = @tenant_id;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", conversation.Id);
        cmd.Parameters.AddWithValue("tenant_id", conversation.TenantId.Value);
        cmd.Parameters.AddWithValue("title", conversation.Title);
        cmd.Parameters.AddWithValue("updated_at_utc", conversation.UpdatedAtUtc);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    // =========================================================================
    // 2. MESSAGES
    // =========================================================================

    public async Task AddMessageAsync(Message message, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        INSERT INTO ai.messages (
            id, conversation_id, sender_role, content, source_category, tokens_used, created_at_utc
        ) VALUES (
            @id, @conversation_id, @sender_role, @content, @source_category, @tokens_used, @created_at_utc
        );
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", message.Id);
        cmd.Parameters.AddWithValue("conversation_id", message.ConversationId);
        cmd.Parameters.AddWithValue("sender_role", message.SenderRole);
        cmd.Parameters.AddWithValue("content", message.Content);
        cmd.Parameters.AddWithValue("source_category", (int)message.SourceCategory);
        cmd.Parameters.AddWithValue("tokens_used", message.TokensUsed);
        cmd.Parameters.AddWithValue("created_at_utc", message.CreatedAtUtc);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<Message>> GetMessagesByConversationIdAsync(Guid conversationId, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        SELECT id, conversation_id, sender_role, content, source_category, tokens_used, created_at_utc
        FROM ai.messages
        WHERE conversation_id = @conversation_id
        ORDER BY created_at_utc ASC;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("conversation_id", conversationId);

        var list = new List<Message>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new Message(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetString(2),
                reader.GetString(3),
                (AiSourceCategory)reader.GetInt32(4),
                reader.GetInt32(5),
                reader.GetDateTime(6)
            ));
        }

        return list;
    }

    // =========================================================================
    // 3. TOOL EXECUTIONS & SOURCE REFERENCES
    // =========================================================================

    public async Task RecordToolExecutionAsync(ToolExecution execution, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        INSERT INTO ai.tool_executions (
            id, message_id, tool_code, input_payload_json, output_payload_json, duration_ms, status, created_at_utc
        ) VALUES (
            @id, @message_id, @tool_code, @input_payload_json, @output_payload_json, @duration_ms, @status, @created_at_utc
        );
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", execution.Id);
        cmd.Parameters.AddWithValue("message_id", execution.MessageId);
        cmd.Parameters.AddWithValue("tool_code", execution.ToolCode);
        cmd.Parameters.AddWithValue("input_payload_json", execution.InputPayloadJson);
        cmd.Parameters.AddWithValue("output_payload_json", execution.OutputPayloadJson);
        cmd.Parameters.AddWithValue("duration_ms", execution.DurationMs);
        cmd.Parameters.AddWithValue("status", execution.Status);
        cmd.Parameters.AddWithValue("created_at_utc", execution.CreatedAtUtc);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task RecordSourceReferenceAsync(SourceReference reference, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        INSERT INTO ai.source_references (
            id, message_id, source_category, title, entity_type, entity_id, policy_code, policy_version, payroll_run_id, metadata_json, retrieved_at_utc
        ) VALUES (
            @id, @message_id, @source_category, @title, @entity_type, @entity_id, @policy_code, @policy_version, @payroll_run_id, @metadata_json, @retrieved_at_utc
        );
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", reference.Id);
        cmd.Parameters.AddWithValue("message_id", reference.MessageId);
        cmd.Parameters.AddWithValue("source_category", (int)reference.SourceCategory);
        cmd.Parameters.AddWithValue("title", reference.Title);
        cmd.Parameters.AddWithValue("entity_type", (object?)reference.EntityType ?? DBNull.Value);
        cmd.Parameters.AddWithValue("entity_id", (object?)reference.EntityId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("policy_code", (object?)reference.PolicyCode ?? DBNull.Value);
        cmd.Parameters.AddWithValue("policy_version", (object?)reference.PolicyVersion ?? DBNull.Value);
        cmd.Parameters.AddWithValue("payroll_run_id", (object?)reference.PayrollRunId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("metadata_json", (object?)reference.MetadataJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("retrieved_at_utc", reference.RetrievedAtUtc);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<ToolExecution>> GetToolExecutionsByMessageIdAsync(Guid messageId, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        SELECT id, message_id, tool_code, input_payload_json, output_payload_json, duration_ms, status, created_at_utc
        FROM ai.tool_executions
        WHERE message_id = @message_id
        ORDER BY created_at_utc ASC;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("message_id", messageId);

        var list = new List<ToolExecution>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new ToolExecution(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetInt64(5),
                reader.GetString(6),
                reader.GetDateTime(7)
            ));
        }

        return list;
    }

    public async Task<IReadOnlyList<SourceReference>> GetSourceReferencesByMessageIdAsync(Guid messageId, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        SELECT id, message_id, source_category, title, entity_type, entity_id, policy_code, policy_version, payroll_run_id, metadata_json, retrieved_at_utc
        FROM ai.source_references
        WHERE message_id = @message_id
        ORDER BY retrieved_at_utc ASC;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("message_id", messageId);

        var list = new List<SourceReference>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new SourceReference(
                reader.GetGuid(0),
                reader.GetGuid(1),
                (AiSourceCategory)reader.GetInt32(2),
                reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetInt32(7),
                reader.IsDBNull(8) ? null : reader.GetGuid(8),
                reader.IsDBNull(9) ? null : reader.GetString(9),
                reader.GetDateTime(10)
            ));
        }

        return list;
    }

    public async Task<IReadOnlyList<ToolExecution>> GetToolExecutionsByConversationIdAsync(Guid conversationId, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        SELECT e.id, e.message_id, e.tool_code, e.input_payload_json, e.output_payload_json, e.duration_ms, e.status, e.created_at_utc
        FROM ai.tool_executions e
        JOIN ai.messages m ON m.id = e.message_id
        WHERE m.conversation_id = @conversation_id
        ORDER BY e.created_at_utc ASC;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("conversation_id", conversationId);

        var list = new List<ToolExecution>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new ToolExecution(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetInt64(5),
                reader.GetString(6),
                reader.GetDateTime(7)
            ));
        }

        return list;
    }

    public async Task<IReadOnlyList<SourceReference>> GetSourceReferencesByConversationIdAsync(Guid conversationId, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        SELECT s.id, s.message_id, s.source_category, s.title, s.entity_type, s.entity_id, s.policy_code, s.policy_version, s.payroll_run_id, s.metadata_json, s.retrieved_at_utc
        FROM ai.source_references s
        JOIN ai.messages m ON m.id = s.message_id
        WHERE m.conversation_id = @conversation_id
        ORDER BY s.retrieved_at_utc ASC;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("conversation_id", conversationId);

        var list = new List<SourceReference>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new SourceReference(
                reader.GetGuid(0),
                reader.GetGuid(1),
                (AiSourceCategory)reader.GetInt32(2),
                reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetInt32(7),
                reader.IsDBNull(8) ? null : reader.GetGuid(8),
                reader.IsDBNull(9) ? null : reader.GetString(9),
                reader.GetDateTime(10)
            ));
        }

        return list;
    }

    // Closeout Gate 10: retention purge. Deletes conversations older than the
    // configured window; messages/executions/source references cascade.
    public async Task<int> PurgeConversationsOlderThanAsync(int retentionDays, CancellationToken ct = default)
    {
        if (retentionDays <= 0) return 0;

        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        DELETE FROM ai.conversations
        WHERE updated_at_utc < NOW() - (@days * INTERVAL '1 day');
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("days", retentionDays);

        return await cmd.ExecuteNonQueryAsync(ct);
    }

    // =========================================================================
    // 4. COMPANY POLICIES (TEMPORAL VERSIONING)
    // =========================================================================

    public async Task CreatePolicyAsync(CompanyPolicy policy, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        INSERT INTO ai.company_policies (
            id, tenant_id, policy_code, title_en, title_ar, version, effective_from_utc, effective_to_utc, content_en, content_ar, classification, is_active
        ) VALUES (
            @id, @tenant_id, @policy_code, @title_en, @title_ar, @version, @effective_from_utc, @effective_to_utc, @content_en, @content_ar, @classification, @is_active
        );
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", policy.Id);
        cmd.Parameters.AddWithValue("tenant_id", policy.TenantId.Value);
        cmd.Parameters.AddWithValue("policy_code", policy.PolicyCode);
        cmd.Parameters.AddWithValue("title_en", policy.TitleEn);
        cmd.Parameters.AddWithValue("title_ar", policy.TitleAr);
        cmd.Parameters.AddWithValue("version", policy.Version);
        cmd.Parameters.AddWithValue("effective_from_utc", policy.EffectiveFromUtc);
        cmd.Parameters.AddWithValue("effective_to_utc", (object?)policy.EffectiveToUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("content_en", policy.ContentEn);
        cmd.Parameters.AddWithValue("content_ar", policy.ContentAr);
        cmd.Parameters.AddWithValue("classification", policy.Classification);
        cmd.Parameters.AddWithValue("is_active", policy.IsActive);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<CompanyPolicy?> GetEffectivePolicyAsync(TenantId tenantId, string policyCode, DateTime targetDateUtc, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        SELECT id, tenant_id, policy_code, title_en, title_ar, version, effective_from_utc, effective_to_utc, content_en, content_ar, classification, is_active
        FROM ai.company_policies
        WHERE tenant_id = @tenant_id 
          AND policy_code = @policy_code 
          AND is_active = true
          AND effective_from_utc <= @target_date
          AND (effective_to_utc IS NULL OR effective_to_utc >= @target_date)
        ORDER BY version DESC
        LIMIT 1;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenant_id", tenantId.Value);
        cmd.Parameters.AddWithValue("policy_code", policyCode);
        cmd.Parameters.AddWithValue("target_date", targetDateUtc);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        return MapPolicy(reader);
    }

    public async Task<IReadOnlyList<CompanyPolicy>> SearchPoliciesAsync(TenantId tenantId, string? query, DateTime? effectiveAtUtc = null, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        var targetDate = effectiveAtUtc ?? DateTime.UtcNow;

        var sql = """
        SELECT id, tenant_id, policy_code, title_en, title_ar, version, effective_from_utc, effective_to_utc, content_en, content_ar, classification, is_active
        FROM ai.company_policies
        WHERE tenant_id = @tenant_id 
          AND is_active = true
          AND effective_from_utc <= @target_date
          AND (effective_to_utc IS NULL OR effective_to_utc >= @target_date)
        """;

        if (!string.IsNullOrWhiteSpace(query))
        {
            sql += " AND (title_en ILIKE @q OR title_ar ILIKE @q OR content_en ILIKE @q OR policy_code ILIKE @q)";
        }

        sql += " ORDER BY policy_code, version DESC LIMIT 20;";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenant_id", tenantId.Value);
        cmd.Parameters.AddWithValue("target_date", targetDate);
        if (!string.IsNullOrWhiteSpace(query))
        {
            cmd.Parameters.AddWithValue("q", $"%{query}%");
        }

        var list = new List<CompanyPolicy>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(MapPolicy(reader));
        }

        return list;
    }

    // =========================================================================
    // 5. PRODUCT KNOWLEDGE
    // =========================================================================

    public async Task CreateProductKnowledgeAsync(ProductKnowledgeArticle article, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        INSERT INTO ai.product_knowledge (
            id, topic_code, title_en, title_ar, content_en, content_ar, category, tags_json
        ) VALUES (
            @id, @topic_code, @title_en, @title_ar, @content_en, @content_ar, @category, @tags_json
        ) ON CONFLICT (topic_code) DO UPDATE 
        SET title_en = @title_en, title_ar = @title_ar, content_en = @content_en, content_ar = @content_ar, category = @category;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", article.Id);
        cmd.Parameters.AddWithValue("topic_code", article.TopicCode);
        cmd.Parameters.AddWithValue("title_en", article.TitleEn);
        cmd.Parameters.AddWithValue("title_ar", article.TitleAr);
        cmd.Parameters.AddWithValue("content_en", article.ContentEn);
        cmd.Parameters.AddWithValue("content_ar", article.ContentAr);
        cmd.Parameters.AddWithValue("category", article.Category);
        cmd.Parameters.AddWithValue("tags_json", article.TagsJson);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<ProductKnowledgeArticle?> GetProductKnowledgeByTopicAsync(string topicCode, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        SELECT id, topic_code, title_en, title_ar, content_en, content_ar, category, tags_json
        FROM ai.product_knowledge
        WHERE topic_code = @topic_code;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("topic_code", topicCode);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        return new ProductKnowledgeArticle(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetString(6),
            reader.GetString(7)
        );
    }

    public async Task<IReadOnlyList<ProductKnowledgeArticle>> SearchProductKnowledgeAsync(string query, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        var sql = """
        SELECT id, topic_code, title_en, title_ar, content_en, content_ar, category, tags_json
        FROM ai.product_knowledge
        """;

        if (!string.IsNullOrWhiteSpace(query))
        {
            sql += " WHERE title_en ILIKE @q OR title_ar ILIKE @q OR content_en ILIKE @q OR topic_code ILIKE @q";
        }

        sql += " ORDER BY category, topic_code LIMIT 20;";

        await using var cmd = new NpgsqlCommand(sql, conn);
        if (!string.IsNullOrWhiteSpace(query))
        {
            cmd.Parameters.AddWithValue("q", $"%{query}%");
        }

        var list = new List<ProductKnowledgeArticle>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new ProductKnowledgeArticle(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetString(7)
            ));
        }

        return list;
    }

    // =========================================================================
    // 5. PHASE 7B: ACTION PROPOSALS & EXECUTIONS
    // =========================================================================

    public async Task CreateProposalAsync(AiActionProposal proposal, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        INSERT INTO ai.action_proposals (
            id, conversation_id, tenant_id, legal_entity_id, requested_by_user_id, action_code,
            target_entity_type, target_entity_id, status, expected_row_version, effective_date_utc,
            before_snapshot_json, after_snapshot_json, impact_summary_json, required_permission,
            idempotency_key, proposal_hash, created_at_utc, expires_at_utc, confirmed_at_utc, completed_at_utc, error_message
        ) VALUES (
            @id, @conversation_id, @tenant_id, @legal_entity_id, @requested_by_user_id, @action_code,
            @target_entity_type, @target_entity_id, @status, @expected_row_version, @effective_date_utc,
            @before_snapshot_json, @after_snapshot_json, @impact_summary_json, @required_permission,
            @idempotency_key, @proposal_hash, @created_at_utc, @expires_at_utc, @confirmed_at_utc, @completed_at_utc, @error_message
        );
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", proposal.Id);
        cmd.Parameters.AddWithValue("conversation_id", proposal.ConversationId);
        cmd.Parameters.AddWithValue("tenant_id", proposal.TenantId.Value);
        cmd.Parameters.AddWithValue("legal_entity_id", (object?)proposal.LegalEntityId?.Value ?? DBNull.Value);
        cmd.Parameters.AddWithValue("requested_by_user_id", proposal.RequestedByUserId.Value);
        cmd.Parameters.AddWithValue("action_code", proposal.ActionCode);
        cmd.Parameters.AddWithValue("target_entity_type", proposal.TargetEntityType);
        cmd.Parameters.AddWithValue("target_entity_id", proposal.TargetEntityId);
        cmd.Parameters.AddWithValue("status", proposal.Status.ToString());
        cmd.Parameters.AddWithValue("expected_row_version", (long)proposal.ExpectedRowVersion);
        cmd.Parameters.AddWithValue("effective_date_utc", (object?)proposal.EffectiveDateUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("before_snapshot_json", proposal.BeforeSnapshotJson);
        cmd.Parameters.AddWithValue("after_snapshot_json", proposal.AfterSnapshotJson);
        cmd.Parameters.AddWithValue("impact_summary_json", proposal.ImpactSummaryJson);
        cmd.Parameters.AddWithValue("required_permission", proposal.RequiredPermission);
        cmd.Parameters.AddWithValue("idempotency_key", proposal.IdempotencyKey);
        cmd.Parameters.AddWithValue("proposal_hash", proposal.ProposalHash);
        cmd.Parameters.AddWithValue("created_at_utc", proposal.CreatedAtUtc);
        cmd.Parameters.AddWithValue("expires_at_utc", proposal.ExpiresAtUtc);
        cmd.Parameters.AddWithValue("confirmed_at_utc", (object?)proposal.ConfirmedAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("completed_at_utc", (object?)proposal.CompletedAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("error_message", (object?)proposal.ErrorMessage ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<AiActionProposal?> GetProposalByIdAsync(TenantId tenantId, Guid proposalId, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        SELECT id, conversation_id, tenant_id, legal_entity_id, requested_by_user_id, action_code,
               target_entity_type, target_entity_id, status, expected_row_version, effective_date_utc,
               before_snapshot_json, after_snapshot_json, impact_summary_json, required_permission,
               idempotency_key, proposal_hash, created_at_utc, expires_at_utc, confirmed_at_utc, completed_at_utc, error_message
        FROM ai.action_proposals
        WHERE tenant_id = @tenant_id AND id = @id
        LIMIT 1;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenant_id", tenantId.Value);
        cmd.Parameters.AddWithValue("id", proposalId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return MapProposal(reader);
        }

        return null;
    }

    public async Task UpdateProposalAsync(AiActionProposal proposal, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        UPDATE ai.action_proposals
        SET status = @status,
            confirmed_at_utc = @confirmed_at_utc,
            completed_at_utc = @completed_at_utc,
            error_message = @error_message
        WHERE tenant_id = @tenant_id AND id = @id;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenant_id", proposal.TenantId.Value);
        cmd.Parameters.AddWithValue("id", proposal.Id);
        cmd.Parameters.AddWithValue("status", proposal.Status.ToString());
        cmd.Parameters.AddWithValue("confirmed_at_utc", (object?)proposal.ConfirmedAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("completed_at_utc", (object?)proposal.CompletedAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("error_message", (object?)proposal.ErrorMessage ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<AiActionProposal>> ListProposalsAsync(TenantId tenantId, UserId userId, int limit = 50, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        SELECT id, conversation_id, tenant_id, legal_entity_id, requested_by_user_id, action_code,
               target_entity_type, target_entity_id, status, expected_row_version, effective_date_utc,
               before_snapshot_json, after_snapshot_json, impact_summary_json, required_permission,
               idempotency_key, proposal_hash, created_at_utc, expires_at_utc, confirmed_at_utc, completed_at_utc, error_message
        FROM ai.action_proposals
        WHERE tenant_id = @tenant_id AND requested_by_user_id = @user_id
        ORDER BY created_at_utc DESC
        LIMIT @limit;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenant_id", tenantId.Value);
        cmd.Parameters.AddWithValue("user_id", userId.Value);
        cmd.Parameters.AddWithValue("limit", Math.Min(limit, 100));

        var list = new List<AiActionProposal>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(MapProposal(reader));
        }

        return list;
    }

    public async Task RecordActionExecutionAsync(AiActionExecution execution, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        INSERT INTO ai.action_executions (
            id, proposal_id, tenant_id, actor_user_id, action_code, idempotency_key, status, result_payload_json, duration_ms, executed_at_utc
        ) VALUES (
            @id, @proposal_id, @tenant_id, @actor_user_id, @action_code, @idempotency_key, @status, @result_payload_json, @duration_ms, @executed_at_utc
        )
        ON CONFLICT (idempotency_key) DO NOTHING;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", execution.Id);
        cmd.Parameters.AddWithValue("proposal_id", execution.ProposalId);
        cmd.Parameters.AddWithValue("tenant_id", execution.TenantId.Value);
        cmd.Parameters.AddWithValue("actor_user_id", execution.ActorUserId.Value);
        cmd.Parameters.AddWithValue("action_code", execution.ActionCode);
        cmd.Parameters.AddWithValue("idempotency_key", execution.IdempotencyKey);
        cmd.Parameters.AddWithValue("status", execution.Status);
        cmd.Parameters.AddWithValue("result_payload_json", execution.ResultPayloadJson);
        cmd.Parameters.AddWithValue("duration_ms", execution.DurationMs);
        cmd.Parameters.AddWithValue("executed_at_utc", execution.ExecutedAtUtc);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<AiActionExecution?> GetExecutionByIdempotencyKeyAsync(TenantId tenantId, string idempotencyKey, CancellationToken ct = default)
    {
        await using var conn = await CreateOpenConnectionAsync(ct);
        const string sql = """
        SELECT id, proposal_id, tenant_id, actor_user_id, action_code, idempotency_key, status, result_payload_json, duration_ms, executed_at_utc
        FROM ai.action_executions
        WHERE tenant_id = @tenant_id AND idempotency_key = @idempotency_key
        LIMIT 1;
        """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenant_id", tenantId.Value);
        cmd.Parameters.AddWithValue("idempotency_key", idempotencyKey);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return new AiActionExecution(
                reader.GetGuid(0),
                reader.GetGuid(1),
                new TenantId(reader.GetGuid(2)),
                new UserId(reader.GetGuid(3)),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetString(7),
                reader.GetInt64(8)
            );
        }

        return null;
    }

    // =========================================================================
    // MAPPERS
    // =========================================================================

    private static Conversation MapConversation(NpgsqlDataReader r)
    {
        return new Conversation(
            r.GetGuid(0),
            new TenantId(r.GetGuid(1)),
            r.IsDBNull(2) ? null : new LegalEntityId(r.GetGuid(2)),
            new UserId(r.GetGuid(3)),
            r.GetString(4),
            r.IsDBNull(5) ? null : r.GetString(5),
            r.IsDBNull(6) ? null : r.GetString(6),
            r.GetDateTime(7),
            r.GetDateTime(8)
        );
    }

    private static CompanyPolicy MapPolicy(NpgsqlDataReader r)
    {
        return new CompanyPolicy(
            r.GetGuid(0),
            new TenantId(r.GetGuid(1)),
            r.GetString(2),
            r.GetString(3),
            r.GetString(4),
            r.GetInt32(5),
            r.GetDateTime(6),
            r.IsDBNull(7) ? null : r.GetDateTime(7),
            r.GetString(8),
            r.GetString(9),
            r.GetString(10),
            r.GetBoolean(11)
        );
    }

    private static AiActionProposal MapProposal(NpgsqlDataReader r)
    {
        var id = r.GetGuid(0);
        var conversationId = r.GetGuid(1);
        var tenantId = new TenantId(r.GetGuid(2));
        var legalEntityId = r.IsDBNull(3) ? (LegalEntityId?)null : new LegalEntityId(r.GetGuid(3));
        var userId = new UserId(r.GetGuid(4));
        var actionCode = r.GetString(5);
        var targetEntityType = r.GetString(6);
        var targetEntityId = r.GetString(7);
        var statusStr = r.GetString(8);
        var expectedRowVersion = (uint)r.GetInt64(9);
        var effectiveDate = r.IsDBNull(10) ? null : (DateTime?)r.GetDateTime(10);
        var beforeSnapshot = r.GetString(11);
        var afterSnapshot = r.GetString(12);
        var impactSummary = r.GetString(13);
        var requiredPermission = r.GetString(14);
        var idempotencyKey = r.GetString(15);
        var proposalHash = r.GetString(16);
        var createdAt = r.GetDateTime(17);
        var expiresAt = r.GetDateTime(18);
        var confirmedAt = r.IsDBNull(19) ? null : (DateTime?)r.GetDateTime(19);
        var completedAt = r.IsDBNull(20) ? null : (DateTime?)r.GetDateTime(20);
        var errorMessage = r.IsDBNull(21) ? null : r.GetString(21);

        var status = Enum.TryParse<ProposalStatus>(statusStr, true, out var st) ? st : ProposalStatus.ReadyForConfirmation;

        var validity = expiresAt - createdAt;
        var proposal = new AiActionProposal(
            id,
            conversationId,
            tenantId,
            legalEntityId,
            userId,
            actionCode,
            targetEntityType,
            targetEntityId,
            expectedRowVersion,
            effectiveDate,
            beforeSnapshot,
            afterSnapshot,
            impactSummary,
            requiredPermission,
            validity
        );

        if (status == ProposalStatus.Confirmed) proposal.MarkConfirmed();
        else if (status == ProposalStatus.Completed)
        {
            proposal.MarkConfirmed();
            proposal.MarkExecuting();
            proposal.MarkCompleted();
        }
        else if (status == ProposalStatus.Cancelled) proposal.MarkCancelled();
        else if (status == ProposalStatus.Stale) proposal.MarkStale(errorMessage ?? "Stale");
        else if (status == ProposalStatus.Expired) proposal.MarkExpired();
        else if (status == ProposalStatus.Failed) proposal.MarkFailed(errorMessage ?? "Failed");

        return proposal;
    }
}
