using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Workforce.Modules.Ai.Application.Actions;
using Workforce.Modules.Ai.Application.Contracts;
using Workforce.Modules.Ai.Application.Services;
using Workforce.Modules.Ai.Domain;
using Workforce.Modules.Ai.Infrastructure;
using Workforce.Modules.Audit.Domain;
using Workforce.Modules.Audit.Infrastructure;
using Workforce.Modules.Leave.Application.Contracts;
using Workforce.Modules.People.Application.Contracts;
using Workforce.Modules.Recruitment.Contracts;
using Workforce.SharedKernel.Primitives;
using Workforce.SharedKernel.Security;
using Xunit;

namespace Architecture.Tests;

public class Phase7BAiActionProposalTests
{
    private static readonly TenantId TenantA = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly TenantId TenantB = new(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly LegalEntityId LegalA = new(Guid.Parse("33333333-3333-3333-3333-333333333333"));
    private static readonly LegalEntityId LegalB = new(Guid.Parse("44444444-4444-4444-4444-444444444444"));
    private static readonly UserId User1 = new(Guid.Parse("55555555-5555-5555-5555-555555555555"));

    // ============================================================
    // 1. PROPOSAL ZERO-EFFECT INVARIANT
    // ============================================================

    [Fact]
    public async Task Section1_ProposalCreation_PersistsMetadataOnly_ProducesZeroBusinessMutation()
    {
        var aiRepo = new InMemoryAiProposalRepository();
        var auditRepo = new InMemoryAuditRepository();
        var fakePeopleContract = new MockPeopleContract();
        var registry = new AiActionRegistry();
        registry.RegisterAction(new PeopleChangeLocationActionHandler(fakePeopleContract));

        var service = new AiProposalService(aiRepo, registry, auditRepo);
        var userContext = new TestUserContext(TenantA, LegalA, User1, new HashSet<string> { "people.assignment.update" });

        var req = new CreateProposalRequest(
            ConversationId: Guid.NewGuid(),
            ActionCode: "people.assignment.change_location",
            TargetEntityType: "Employee",
            TargetEntityId: Guid.NewGuid().ToString(),
            ExpectedRowVersion: 1,
            EffectiveDateUtc: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            BeforeSnapshotJson: "{\"locationNameEn\":\"Alexandria\"}",
            AfterSnapshotJson: "{\"locationNameEn\":\"Cairo\",\"locationId\":\"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa\"}",
            ImpactSummaryJson: "{\"description\":\"Creates new assignment version.\"}",
            ValidityMinutes: 15
        );

        var proposal = await service.CreateProposalAsync(req, userContext);

        Assert.NotNull(proposal);
        Assert.Equal("ReadyForConfirmation", proposal.Status);
        Assert.Equal(0, fakePeopleContract.InvocationCount); // 0 target mutations!
        Assert.Equal(1, auditRepo.Records.Count);
        Assert.Equal("ai.proposal.created", auditRepo.Records[0].ActionCode);
    }

    // ============================================================
    // 2. EXPLICIT CONFIRMATION & EXECUTION ROUTING
    // ============================================================

    [Fact]
    public async Task Section2_ExplicitConfirmation_ExecutesThroughModuleContract_AndAudits()
    {
        var aiRepo = new InMemoryAiProposalRepository();
        var auditRepo = new InMemoryAuditRepository();
        var fakePeopleContract = new MockPeopleContract();
        var registry = new AiActionRegistry();
        registry.RegisterAction(new PeopleChangeLocationActionHandler(fakePeopleContract));

        var service = new AiProposalService(aiRepo, registry, auditRepo);
        var userContext = new TestUserContext(TenantA, LegalA, User1, new HashSet<string> { "people.assignment.update" });

        var targetId = Guid.NewGuid();
        var req = new CreateProposalRequest(
            ConversationId: Guid.NewGuid(),
            ActionCode: "people.assignment.change_location",
            TargetEntityType: "Employee",
            TargetEntityId: targetId.ToString(),
            ExpectedRowVersion: 1,
            EffectiveDateUtc: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            BeforeSnapshotJson: "{\"locationNameEn\":\"Alexandria\"}",
            AfterSnapshotJson: "{\"locationNameEn\":\"Cairo\",\"locationId\":\"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa\"}",
            ImpactSummaryJson: "{\"description\":\"Creates new assignment version.\"}"
        );

        var proposal = await service.CreateProposalAsync(req, userContext);

        var confirmResult = await service.ConfirmProposalAsync(proposal.Id, new ConfirmProposalRequest("Approved by user"), userContext);

        Assert.True(confirmResult.Success);
        Assert.Equal("Completed", confirmResult.Status);
        Assert.Equal(1, fakePeopleContract.InvocationCount); // Executed exactly once through application contract!
        Assert.True(auditRepo.Records.Exists(r => r.ActionCode == "ai.action.executed"));
    }

    // ============================================================
    // 3. TAMPER DETECTION & HASH VERIFICATION
    // ============================================================

    [Fact]
    public async Task Section3_TamperedProposalSnapshot_FailsConfirmation()
    {
        var aiRepo = new InMemoryAiProposalRepository();
        var auditRepo = new InMemoryAuditRepository();
        var fakePeopleContract = new MockPeopleContract();
        var registry = new AiActionRegistry();
        registry.RegisterAction(new PeopleChangeLocationActionHandler(fakePeopleContract));

        var service = new AiProposalService(aiRepo, registry, auditRepo);
        var userContext = new TestUserContext(TenantA, LegalA, User1, new HashSet<string> { "people.assignment.update" });

        var req = new CreateProposalRequest(
            ConversationId: Guid.NewGuid(),
            ActionCode: "people.assignment.change_location",
            TargetEntityType: "Employee",
            TargetEntityId: Guid.NewGuid().ToString(),
            ExpectedRowVersion: 1,
            EffectiveDateUtc: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            BeforeSnapshotJson: "{\"locationNameEn\":\"Alexandria\"}",
            AfterSnapshotJson: "{\"locationNameEn\":\"Cairo\"}",
            ImpactSummaryJson: "{}"
        );

        var proposalDto = await service.CreateProposalAsync(req, userContext);

        // Simulate tampering in storage or malicious request payload:
        var storedProposal = await aiRepo.GetProposalByIdAsync(TenantA, proposalDto.Id);
        Assert.NotNull(storedProposal);

        // Tamper with AfterSnapshotJson using reflection to simulate compromised row
        var propField = typeof(AiActionProposal).GetProperty("AfterSnapshotJson", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        propField?.SetValue(storedProposal, "{\"locationNameEn\":\"MALICIOUS_LOCATION\"}");

        // Attempt confirm
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ConfirmProposalAsync(proposalDto.Id, new ConfirmProposalRequest(null), userContext));

        Assert.True(ex.Message.Contains("integrity", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(0, fakePeopleContract.InvocationCount); // Zero execution
    }

    // ============================================================
    // 4. PROPOSAL EXPIRY REJECTION
    // ============================================================

    [Fact]
    public async Task Section4_ExpiredProposal_CannotExecute()
    {
        var aiRepo = new InMemoryAiProposalRepository();
        var auditRepo = new InMemoryAuditRepository();
        var fakePeopleContract = new MockPeopleContract();
        var registry = new AiActionRegistry();
        registry.RegisterAction(new PeopleChangeLocationActionHandler(fakePeopleContract));

        var service = new AiProposalService(aiRepo, registry, auditRepo);
        var userContext = new TestUserContext(TenantA, LegalA, User1, new HashSet<string> { "people.assignment.update" });

        var req = new CreateProposalRequest(
            ConversationId: Guid.NewGuid(),
            ActionCode: "people.assignment.change_location",
            TargetEntityType: "Employee",
            TargetEntityId: Guid.NewGuid().ToString(),
            ExpectedRowVersion: 1,
            EffectiveDateUtc: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            BeforeSnapshotJson: "{}",
            AfterSnapshotJson: "{}",
            ImpactSummaryJson: "{}"
        );

        var proposalDto = await service.CreateProposalAsync(req, userContext);

        // Fast-forward expiry
        var stored = await aiRepo.GetProposalByIdAsync(TenantA, proposalDto.Id);
        var expiresField = typeof(AiActionProposal).GetProperty("ExpiresAtUtc", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        expiresField?.SetValue(stored, DateTime.UtcNow.AddMinutes(-5));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ConfirmProposalAsync(proposalDto.Id, new ConfirmProposalRequest(null), userContext));

        Assert.True(ex.Message.Contains("expired", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(0, fakePeopleContract.InvocationCount);
    }

    // ============================================================
    // 5. REAUTHORIZATION AT EXECUTION TIME
    // ============================================================

    [Fact]
    public async Task Section5_RevokedPermission_FailsConfirmation()
    {
        var aiRepo = new InMemoryAiProposalRepository();
        var auditRepo = new InMemoryAuditRepository();
        var fakePeopleContract = new MockPeopleContract();
        var registry = new AiActionRegistry();
        registry.RegisterAction(new PeopleChangeLocationActionHandler(fakePeopleContract));

        var service = new AiProposalService(aiRepo, registry, auditRepo);
        var userWithPerms = new TestUserContext(TenantA, LegalA, User1, new HashSet<string> { "people.assignment.update" });

        var req = new CreateProposalRequest(
            ConversationId: Guid.NewGuid(),
            ActionCode: "people.assignment.change_location",
            TargetEntityType: "Employee",
            TargetEntityId: Guid.NewGuid().ToString(),
            ExpectedRowVersion: 1,
            EffectiveDateUtc: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            BeforeSnapshotJson: "{}",
            AfterSnapshotJson: "{}",
            ImpactSummaryJson: "{}"
        );

        var proposalDto = await service.CreateProposalAsync(req, userWithPerms);

        // Admin revokes permission before confirm
        var userRevokedPerms = new TestUserContext(TenantA, LegalA, User1, new HashSet<string>());

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ConfirmProposalAsync(proposalDto.Id, new ConfirmProposalRequest(null), userRevokedPerms));

        Assert.True(ex.Message.Contains("permission", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(0, fakePeopleContract.InvocationCount);
    }

    // ============================================================
    // 6. CONCURRENCY / STALE TARGET REJECTION
    // ============================================================

    [Fact]
    public async Task Section6_ConcurrentTargetChange_ReturnsStale_AndZeroMutation()
    {
        var aiRepo = new InMemoryAiProposalRepository();
        var auditRepo = new InMemoryAuditRepository();
        var fakePeopleContract = new MockPeopleContract { SimulateConcurrencyConflict = true };
        var registry = new AiActionRegistry();
        registry.RegisterAction(new PeopleChangeLocationActionHandler(fakePeopleContract));

        var service = new AiProposalService(aiRepo, registry, auditRepo);
        var userContext = new TestUserContext(TenantA, LegalA, User1, new HashSet<string> { "people.assignment.update" });

        var req = new CreateProposalRequest(
            ConversationId: Guid.NewGuid(),
            ActionCode: "people.assignment.change_location",
            TargetEntityType: "Employee",
            TargetEntityId: Guid.NewGuid().ToString(),
            ExpectedRowVersion: 1,
            EffectiveDateUtc: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            BeforeSnapshotJson: "{}",
            AfterSnapshotJson: "{}",
            ImpactSummaryJson: "{}"
        );

        var proposalDto = await service.CreateProposalAsync(req, userContext);

        var result = await service.ConfirmProposalAsync(proposalDto.Id, new ConfirmProposalRequest(null), userContext);

        Assert.False(result.Success);
        Assert.Equal("Stale", result.Status);
        Assert.True(auditRepo.Records.Exists(r => r.ActionCode == "ai.proposal.stale"));
    }

    // ============================================================
    // 7. IDEMPOTENCY / REPLAY PROTECTION
    // ============================================================

    [Fact]
    public async Task Section7_DoubleConfirmReplay_ExecutesTargetOnlyOnce()
    {
        var aiRepo = new InMemoryAiProposalRepository();
        var auditRepo = new InMemoryAuditRepository();
        var fakePeopleContract = new MockPeopleContract();
        var registry = new AiActionRegistry();
        registry.RegisterAction(new PeopleChangeLocationActionHandler(fakePeopleContract));

        var service = new AiProposalService(aiRepo, registry, auditRepo);
        var userContext = new TestUserContext(TenantA, LegalA, User1, new HashSet<string> { "people.assignment.update" });

        var req = new CreateProposalRequest(
            ConversationId: Guid.NewGuid(),
            ActionCode: "people.assignment.change_location",
            TargetEntityType: "Employee",
            TargetEntityId: Guid.NewGuid().ToString(),
            ExpectedRowVersion: 1,
            EffectiveDateUtc: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            BeforeSnapshotJson: "{}",
            AfterSnapshotJson: "{}",
            ImpactSummaryJson: "{}"
        );

        var proposalDto = await service.CreateProposalAsync(req, userContext);

        // First confirm
        var res1 = await service.ConfirmProposalAsync(proposalDto.Id, new ConfirmProposalRequest(null), userContext);
        Assert.True(res1.Success);
        Assert.Equal("Completed", res1.Status);
        Assert.Equal(1, fakePeopleContract.InvocationCount);

        // Replay / Double Click confirm
        var res2 = await service.ConfirmProposalAsync(proposalDto.Id, new ConfirmProposalRequest(null), userContext);
        Assert.True(res2.Success);
        Assert.Equal("Completed", res2.Status);
        Assert.Equal(1, fakePeopleContract.InvocationCount); // Still exactly 1!
    }

    // ============================================================
    // 8. CANCELLATION LIFECYCLE
    // ============================================================

    [Fact]
    public async Task Section8_CancelledProposal_CannotBeConfirmedLater()
    {
        var aiRepo = new InMemoryAiProposalRepository();
        var auditRepo = new InMemoryAuditRepository();
        var fakePeopleContract = new MockPeopleContract();
        var registry = new AiActionRegistry();
        registry.RegisterAction(new PeopleChangeLocationActionHandler(fakePeopleContract));

        var service = new AiProposalService(aiRepo, registry, auditRepo);
        var userContext = new TestUserContext(TenantA, LegalA, User1, new HashSet<string> { "people.assignment.update" });

        var req = new CreateProposalRequest(
            ConversationId: Guid.NewGuid(),
            ActionCode: "people.assignment.change_location",
            TargetEntityType: "Employee",
            TargetEntityId: Guid.NewGuid().ToString(),
            ExpectedRowVersion: 1,
            EffectiveDateUtc: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            BeforeSnapshotJson: "{}",
            AfterSnapshotJson: "{}",
            ImpactSummaryJson: "{}"
        );

        var proposalDto = await service.CreateProposalAsync(req, userContext);

        // Cancel
        var cancelResult = await service.CancelProposalAsync(proposalDto.Id, new CancelProposalRequest("User changed mind"), userContext);
        Assert.Equal("Cancelled", cancelResult.Status);
        Assert.Equal(0, fakePeopleContract.InvocationCount);

        // Attempt confirm
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ConfirmProposalAsync(proposalDto.Id, new ConfirmProposalRequest(null), userContext));

        Assert.True(ex.Message.Contains("Cancelled", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(0, fakePeopleContract.InvocationCount);
    }

    // ============================================================
    // 9. FORBIDDEN ACTIONS PROHIBITION
    // ============================================================

    [Theory]
    [InlineData("payroll.finalize")]
    [InlineData("payroll.approve")]
    [InlineData("payroll.calculate")]
    [InlineData("recruitment.candidate.auto_hire")]
    [InlineData("admin.grant_permission")]
    [InlineData("execute_sql")]
    [InlineData("execute_http")]
    [InlineData("database_write")]
    public async Task Section9_ForbiddenActions_CannotBeProposed(string forbiddenActionCode)
    {
        var aiRepo = new InMemoryAiProposalRepository();
        var auditRepo = new InMemoryAuditRepository();
        var registry = new AiActionRegistry();

        var service = new AiProposalService(aiRepo, registry, auditRepo);
        var userContext = new TestUserContext(TenantA, LegalA, User1, new HashSet<string> { "admin" });

        var req = new CreateProposalRequest(
            ConversationId: Guid.NewGuid(),
            ActionCode: forbiddenActionCode,
            TargetEntityType: "Generic",
            TargetEntityId: Guid.NewGuid().ToString(),
            ExpectedRowVersion: 1,
            EffectiveDateUtc: null,
            BeforeSnapshotJson: "{}",
            AfterSnapshotJson: "{}",
            ImpactSummaryJson: "{}"
        );

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateProposalAsync(req, userContext));

        Assert.True(ex.Message.Contains("not supported", StringComparison.OrdinalIgnoreCase));
    }

    // ============================================================
    // 10. RECRUITMENT APPLICATION MOVE STAGE ACTION
    // ============================================================

    [Fact]
    public async Task Section10_RecruitmentMoveStage_ExecutesThroughRecruitmentContract()
    {
        var aiRepo = new InMemoryAiProposalRepository();
        var auditRepo = new InMemoryAuditRepository();
        var fakeRecruitmentContract = new MockRecruitmentContract();
        var registry = new AiActionRegistry();
        registry.RegisterAction(new RecruitmentMoveStageActionHandler(fakeRecruitmentContract));

        var service = new AiProposalService(aiRepo, registry, auditRepo);
        var userContext = new TestUserContext(TenantA, LegalA, User1, new HashSet<string> { "recruitment.application.update" });

        var targetStageId = Guid.NewGuid();
        var req = new CreateProposalRequest(
            ConversationId: Guid.NewGuid(),
            ActionCode: "recruitment.application.move_stage",
            TargetEntityType: "Application",
            TargetEntityId: Guid.NewGuid().ToString(),
            ExpectedRowVersion: 2,
            EffectiveDateUtc: null,
            BeforeSnapshotJson: "{\"stageName\":\"Screening\"}",
            AfterSnapshotJson: JsonSerializer.Serialize(new { targetStageId, reason = "Passed initial screening" }),
            ImpactSummaryJson: "{\"impact\":\"Moves application to Interview stage.\"}"
        );

        var proposalDto = await service.CreateProposalAsync(req, userContext);
        Assert.Equal(0, fakeRecruitmentContract.InvocationCount);

        var result = await service.ConfirmProposalAsync(proposalDto.Id, new ConfirmProposalRequest(null), userContext);
        Assert.True(result.Success);
        Assert.Equal("Completed", result.Status);
        Assert.Equal(1, fakeRecruitmentContract.InvocationCount);
    }

    // ============================================================
    // 11. LEAVE CANCEL REQUEST ACTION
    // ============================================================

    [Fact]
    public async Task Section11_LeaveCancelRequest_ExecutesThroughLeaveContract()
    {
        var aiRepo = new InMemoryAiProposalRepository();
        var auditRepo = new InMemoryAuditRepository();
        var fakeLeaveContract = new MockLeaveContract();
        var registry = new AiActionRegistry();
        registry.RegisterAction(new LeaveCancelRequestActionHandler(fakeLeaveContract));

        var service = new AiProposalService(aiRepo, registry, auditRepo);
        var userContext = new TestUserContext(TenantA, LegalA, User1, new HashSet<string> { "leave.request.cancel" });

        var req = new CreateProposalRequest(
            ConversationId: Guid.NewGuid(),
            ActionCode: "leave.request.cancel",
            TargetEntityType: "LeaveRequest",
            TargetEntityId: Guid.NewGuid().ToString(),
            ExpectedRowVersion: 1,
            EffectiveDateUtc: null,
            BeforeSnapshotJson: "{\"status\":\"PendingApproval\"}",
            AfterSnapshotJson: "{\"status\":\"Cancelled\"}",
            ImpactSummaryJson: "{\"impact\":\"Cancels pending leave request.\"}"
        );

        var proposalDto = await service.CreateProposalAsync(req, userContext);
        var result = await service.ConfirmProposalAsync(proposalDto.Id, new ConfirmProposalRequest(null), userContext);

        Assert.True(result.Success);
        Assert.Equal("Completed", result.Status);
        Assert.Equal(1, fakeLeaveContract.InvocationCount);
    }

    // ============================================================
    // TEST DOUBLES & MOCKS
    // ============================================================

    private class InMemoryAiProposalRepository : IAiRepository
    {
        private readonly List<AiActionProposal> _proposals = new();
        private readonly List<AiActionExecution> _executions = new();

        public Task CreateProposalAsync(AiActionProposal proposal, CancellationToken ct = default)
        {
            _proposals.Add(proposal);
            return Task.CompletedTask;
        }

        public Task<AiActionProposal?> GetProposalByIdAsync(TenantId tenantId, Guid proposalId, CancellationToken ct = default)
        {
            var p = _proposals.FirstOrDefault(x => x.TenantId == tenantId && x.Id == proposalId);
            return Task.FromResult(p);
        }

        public Task UpdateProposalAsync(AiActionProposal proposal, CancellationToken ct = default) => Task.CompletedTask;

        public Task<IReadOnlyList<AiActionProposal>> ListProposalsAsync(TenantId tenantId, UserId userId, int limit = 50, CancellationToken ct = default)
        {
            var list = _proposals.Where(x => x.TenantId == tenantId && x.RequestedByUserId == userId).Take(limit).ToList();
            return Task.FromResult<IReadOnlyList<AiActionProposal>>(list);
        }

        public Task RecordActionExecutionAsync(AiActionExecution execution, CancellationToken ct = default)
        {
            _executions.Add(execution);
            return Task.CompletedTask;
        }

        public Task<AiActionExecution?> GetExecutionByIdempotencyKeyAsync(TenantId tenantId, string idempotencyKey, CancellationToken ct = default)
        {
            var e = _executions.FirstOrDefault(x => x.TenantId == tenantId && x.IdempotencyKey == idempotencyKey);
            return Task.FromResult(e);
        }

        // Unused IAiRepository methods
        public Task CreateConversationAsync(Conversation conversation, CancellationToken ct = default) => Task.CompletedTask;
        public Task<Conversation?> GetConversationByIdAsync(TenantId tenantId, Guid conversationId, CancellationToken ct = default) => Task.FromResult<Conversation?>(null);
        public Task<IReadOnlyList<Conversation>> ListConversationsAsync(TenantId tenantId, UserId userId, int limit = 50, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Conversation>>(new List<Conversation>());
        public Task UpdateConversationAsync(Conversation conversation, CancellationToken ct = default) => Task.CompletedTask;
        public Task AddMessageAsync(Message message, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<Message>> GetMessagesByConversationIdAsync(Guid conversationId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Message>>(new List<Message>());
        public Task RecordToolExecutionAsync(ToolExecution execution, CancellationToken ct = default) => Task.CompletedTask;
        public Task RecordSourceReferenceAsync(SourceReference reference, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<ToolExecution>> GetToolExecutionsByMessageIdAsync(Guid messageId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<ToolExecution>>(new List<ToolExecution>());
        public Task<IReadOnlyList<SourceReference>> GetSourceReferencesByMessageIdAsync(Guid messageId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<SourceReference>>(new List<SourceReference>());
        public Task<IReadOnlyList<ToolExecution>> GetToolExecutionsByConversationIdAsync(Guid conversationId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<ToolExecution>>(new List<ToolExecution>());
        public Task<IReadOnlyList<SourceReference>> GetSourceReferencesByConversationIdAsync(Guid conversationId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<SourceReference>>(new List<SourceReference>());
        public Task<int> PurgeConversationsOlderThanAsync(int retentionDays, CancellationToken ct = default) => Task.FromResult(0);
        public Task CreatePolicyAsync(CompanyPolicy policy, CancellationToken ct = default) => Task.CompletedTask;
        public Task<CompanyPolicy?> GetEffectivePolicyAsync(TenantId tenantId, string policyCode, DateTime targetDateUtc, CancellationToken ct = default) => Task.FromResult<CompanyPolicy?>(null);
        public Task<IReadOnlyList<CompanyPolicy>> SearchPoliciesAsync(TenantId tenantId, string? query, DateTime? effectiveAtUtc = null, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<CompanyPolicy>>(new List<CompanyPolicy>());
        public Task CreateProductKnowledgeAsync(ProductKnowledgeArticle article, CancellationToken ct = default) => Task.CompletedTask;
        public Task<ProductKnowledgeArticle?> GetProductKnowledgeByTopicAsync(string topicCode, CancellationToken ct = default) => Task.FromResult<ProductKnowledgeArticle?>(null);
        public Task<IReadOnlyList<ProductKnowledgeArticle>> SearchProductKnowledgeAsync(string query, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<ProductKnowledgeArticle>>(new List<ProductKnowledgeArticle>());
    }

    private class InMemoryAuditRepository : IAuditRepository
    {
        public List<AuditRecord> Records { get; } = new();

        public Task RecordAsync(AuditRecord record, CancellationToken ct = default)
        {
            Records.Add(record);
            return Task.CompletedTask;
        }

        public Task RecordBatchAsync(IEnumerable<AuditRecord> records, CancellationToken ct = default)
        {
            Records.AddRange(records);
            return Task.CompletedTask;
        }

        public Task<PagedAuditResult> SearchAsync(TenantId tenantId, AuditSearchFilter filter, CancellationToken ct = default)
        {
            return Task.FromResult(new PagedAuditResult(Records, Records.Count, 1, 50));
        }

        public Task<AuditRecord?> GetByIdAsync(TenantId tenantId, Guid id, CancellationToken ct = default)
        {
            return Task.FromResult(Records.FirstOrDefault(r => r.TenantId == tenantId && r.Id == id));
        }
    }

    private class MockPeopleContract : IPeopleAssignmentApplicationContract
    {
        public int InvocationCount { get; private set; }
        public bool SimulateConcurrencyConflict { get; set; }

        public Task<AssignmentActionResult> ChangeLocationAsync(TenantId tenantId, ChangeAssignmentLocationCommand command, CancellationToken ct = default)
        {
            InvocationCount++;
            if (SimulateConcurrencyConflict)
            {
                return Task.FromResult(new AssignmentActionResult(false, command.EmploymentId, Guid.Empty, command.ExpectedRowVersion, "Concurrency conflict", true));
            }
            return Task.FromResult(new AssignmentActionResult(true, command.EmploymentId, Guid.NewGuid(), command.ExpectedRowVersion + 1, "Success", false));
        }

        public Task<AssignmentActionResult> ChangeManagerAsync(TenantId tenantId, ChangeAssignmentManagerCommand command, CancellationToken ct = default)
        {
            InvocationCount++;
            if (SimulateConcurrencyConflict)
            {
                return Task.FromResult(new AssignmentActionResult(false, command.EmploymentId, Guid.Empty, command.ExpectedRowVersion, "Concurrency conflict", true));
            }
            return Task.FromResult(new AssignmentActionResult(true, command.EmploymentId, Guid.NewGuid(), command.ExpectedRowVersion + 1, "Success", false));
        }
    }

    private class MockRecruitmentContract : IRecruitmentActionContract
    {
        public int InvocationCount { get; private set; }

        public Task<RecruitmentActionResult> MoveApplicationStageAsync(TenantId tenantId, UserId actorUserId, MoveApplicationStageCommand command, CancellationToken ct = default)
        {
            InvocationCount++;
            return Task.FromResult(new RecruitmentActionResult(true, command.ApplicationId, command.ExpectedRowVersion + 1, "Moved", false));
        }

        public Task<RecruitmentActionResult> SubmitRequisitionApprovalAsync(TenantId tenantId, UserId actorUserId, SubmitRequisitionApprovalCommand command, CancellationToken ct = default)
        {
            InvocationCount++;
            return Task.FromResult(new RecruitmentActionResult(true, command.RequisitionId, command.ExpectedRowVersion + 1, "Submitted", false));
        }
    }

    private class MockLeaveContract : ILeaveActionContract
    {
        public int InvocationCount { get; private set; }

        public Task<LeaveActionResult> CancelLeaveRequestAsync(TenantId tenantId, UserId actorUserId, CancelLeaveRequestCommand command, CancellationToken ct = default)
        {
            InvocationCount++;
            return Task.FromResult(new LeaveActionResult(true, command.LeaveRequestId, command.ExpectedRowVersion + 1, "Cancelled", false));
        }
    }

    private class TestUserContext : IUserContext
    {
        public TenantId TenantId { get; }
        public LegalEntityId? LegalEntityId { get; }
        public UserId UserId { get; }
        public IReadOnlySet<string> Permissions { get; }
        public IReadOnlySet<TenantId> AllowedTenants => new HashSet<TenantId> { TenantId };
        public IReadOnlySet<LegalEntityId> AllowedLegalEntities => LegalEntityId.HasValue ? new HashSet<LegalEntityId> { LegalEntityId.Value } : new HashSet<LegalEntityId>();
        public string Culture => "en";
        public string Timezone => "UTC";
        public IReadOnlySet<string> Entitlements => new HashSet<string>();

        public TestUserContext(TenantId tenantId, LegalEntityId? legalEntityId, UserId userId, HashSet<string> permissions)
        {
            TenantId = tenantId;
            LegalEntityId = legalEntityId;
            UserId = userId;
            Permissions = permissions;
        }

        public bool HasPermission(string permission) => Permissions.Contains(permission) || Permissions.Contains("admin");
        public bool HasEntitlement(string entitlement) => true;
        public bool HasScope(string scope) => true;
        public bool IsAuthorizedForTenant(TenantId tenantId) => tenantId == TenantId;
        public bool IsAuthorizedForLegalEntity(LegalEntityId legalEntityId) => !LegalEntityId.HasValue || legalEntityId == LegalEntityId.Value;
    }
}
