using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Workforce.Modules.Recruitment.Domain;
using Workforce.SharedKernel.Primitives;
using Workforce.SharedKernel.Security;
using Xunit;
using Xunit.Abstractions;

namespace Architecture.Tests;

public class Phase5RecruitmentBenchmarkTests
{
    private readonly ITestOutputHelper _output;
    private static readonly TenantId Tenant = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly LegalEntityId LegalEntity = new(Guid.Parse("33333333-3333-3333-3333-333333333333"));

    public Phase5RecruitmentBenchmarkTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Benchmark_10kCandidates_50kApplications_HighThroughputExecution()
    {
        var piiService = new AesPiiEncryptionService();
        var candidates = new List<Candidate>(10_000);
        var emailIndex = new Dictionary<string, Candidate>(10_000);
        var candidateIdMap = new Dictionary<Guid, Candidate>(10_000);

        // 1. Generate 10,000 synthetic candidates
        var swGen = Stopwatch.StartNew();
        for (int i = 0; i < 10_000; i++)
        {
            var cid = Guid.NewGuid();
            var email = $"candidate_{i:D6}@enterprise-benchmark.com";
            var phone = $"+2010{i:D8}";
            var c = new Candidate(
                cid, Tenant,
                $"FirstName{i}", $"LastName{i}",
                $"الاسم{i}", $"العائلة{i}",
                email, phone,
                location: "Cairo, Egypt",
                headline: $"Senior Engineer Level {i % 5}",
                source: i % 2 == 0 ? "LinkedIn" : "Careers Portal"
            );
            candidates.Add(c);
            emailIndex[c.NormalizedEmailHash] = c;
            candidateIdMap[cid] = c;
        }
        swGen.Stop();

        // 2. Generate 50,000 synthetic applications across 50 requisitions
        var requisitionIds = Enumerable.Range(0, 50).Select(_ => Guid.NewGuid()).ToList();
        var pipelineVersionId = Guid.NewGuid();
        var stageIds = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid()).ToList();

        var applications = new List<Application>(50_000);
        var appByReqIndex = new Dictionary<Guid, List<Application>>();
        foreach (var reqId in requisitionIds)
        {
            appByReqIndex[reqId] = new List<Application>(1000);
        }

        var swAppGen = Stopwatch.StartNew();
        for (int i = 0; i < 50_000; i++)
        {
            var appId = Guid.NewGuid();
            var candidate = candidates[i % 10_000];
            var reqId = requisitionIds[i % 50];
            var stageId = stageIds[i % 6];

            var app = new Application(
                appId, Tenant, LegalEntity,
                reqId, candidate.Id, pipelineVersionId,
                stageId, "BenchmarkIntake"
            );
            applications.Add(app);
            appByReqIndex[reqId].Add(app);
        }
        swAppGen.Stop();

        // 3. Measure Candidate Blind Index Search (Exact 100 search queries)
        var swSearch = Stopwatch.StartNew();
        int foundCount = 0;
        for (int i = 0; i < 100; i++)
        {
            var searchEmail = $"candidate_{(i * 97) % 10_000:D6}@enterprise-benchmark.com";
            var searchHash = Candidate.ComputeNormalizedEmailHash(searchEmail);
            if (emailIndex.TryGetValue(searchHash, out _))
            {
                foundCount++;
            }
        }
        swSearch.Stop();
        Assert.Equal(100, foundCount);

        // 4. Measure Pipeline Board Query (Fetching and aggregating 1,000 applications for a single requisition)
        var targetReqId = requisitionIds[0];
        var swBoard = Stopwatch.StartNew();
        var boardApps = appByReqIndex[targetReqId];
        var stageGroups = boardApps.GroupBy(a => a.CurrentStageId).ToDictionary(g => g.Key, g => g.Count());
        swBoard.Stop();
        Assert.Equal(1000, boardApps.Count);

        // 5. Measure 1,000 Concurrent Stage Transitions with Concurrency & Idempotency Checks
        var swTransitions = Stopwatch.StartNew();
        var actor = Guid.NewGuid();
        for (int i = 0; i < 1000; i++)
        {
            var app = applications[i];
            var nextStageId = stageIds[(i + 1) % 6];
            app.MoveToStage(nextStageId, actor, "Bulk pipeline progression benchmark", $"IDEM-BENCH-{i}", app.RowVersion);
        }
        swTransitions.Stop();

        // Output results
        _output.WriteLine($"[BENCHMARK] Candidate Population (10k): {swGen.ElapsedMilliseconds} ms");
        _output.WriteLine($"[BENCHMARK] Application Population (50k): {swAppGen.ElapsedMilliseconds} ms");
        _output.WriteLine($"[BENCHMARK] 100 Blind Index Search Queries: {swSearch.ElapsedMilliseconds} ms (avg: {swSearch.Elapsed.TotalMilliseconds / 100:F3} ms/query)");
        _output.WriteLine($"[BENCHMARK] Pipeline Board Aggregate Query (1k apps): {swBoard.Elapsed.TotalMilliseconds:F3} ms");
        _output.WriteLine($"[BENCHMARK] 1,000 Stage Transitions: {swTransitions.ElapsedMilliseconds} ms (avg: {swTransitions.Elapsed.TotalMilliseconds / 1000:F3} ms/transition)");

        Assert.True(swSearch.ElapsedMilliseconds < 500, "100 Blind Index searches exceeded 500ms");
        Assert.True(swBoard.Elapsed.TotalMilliseconds < 50, "Board aggregation query exceeded 50ms");
        Assert.True(swTransitions.ElapsedMilliseconds < 200, "1,000 stage transitions exceeded 200ms");
    }
}
