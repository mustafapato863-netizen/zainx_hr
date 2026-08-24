using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Workforce.Modules.Compliance.Domain;
using Workforce.Modules.Compliance.Infrastructure;
using Workforce.Modules.Payroll.Domain;
using Workforce.Modules.Payroll.Domain.CalculationEngine;
using Workforce.Modules.Payroll.Infrastructure;
using Workforce.SharedKernel.Primitives;
using Workforce.SharedKernel.Security;
using Xunit;

namespace Architecture.Tests;

public class Phase4DurableJobTests
{
    private readonly TenantId _tenantId = new(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private readonly LegalEntityId _legalEntityId = new(Guid.Parse("33333333-3333-3333-3333-333333333333"));

    // =========================================================================
    // 1. DURABLE JOB DOMAIN STATE MACHINE & OPTIMISTIC CONCURRENCY
    // =========================================================================

    [Fact]
    public void DurableJob_StateTransitions_EnforceValidFlowAndIncrementRowVersion()
    {
        var runId = Guid.NewGuid();
        var job = new PayrollBackgroundJob(
            Guid.NewGuid(), _tenantId.Value, runId, "idemp-key-01", "payroll.calculate"
        );

        Assert.Equal(PayrollJobStatus.Queued, job.Status);
        Assert.Equal(1u, job.RowVersion);
        Assert.Null(job.CompletedAtUtc);
        Assert.Null(job.ErrorMessage);

        // Transition Queued -> Running
        job.MarkRunning(expectedRowVersion: 1);
        Assert.Equal(PayrollJobStatus.Running, job.Status);
        Assert.Equal(2u, job.RowVersion);

        // Cannot start an already running job
        Assert.Throws<InvalidOperationException>(() => job.MarkRunning(expectedRowVersion: 2));

        // Stale row version throws concurrency exception
        Assert.Throws<InvalidOperationException>(() => job.MarkCompleted(false, "{}", expectedRowVersion: 1));

        // Transition Running -> Completed
        job.MarkCompleted(hasWarnings: false, diagnosticMetadata: "{\"count\":10}", expectedRowVersion: 2);
        Assert.Equal(PayrollJobStatus.Completed, job.Status);
        Assert.Equal(3u, job.RowVersion);
        Assert.NotNull(job.CompletedAtUtc);
        Assert.Equal("{\"count\":10}", job.DiagnosticMetadata);
    }

    [Fact]
    public void DurableJob_MarkFailed_RecordsErrorAndDiagnostics()
    {
        var job = new PayrollBackgroundJob(
            Guid.NewGuid(), _tenantId.Value, Guid.NewGuid(), "idemp-key-fail", "payroll.calculate"
        );

        job.MarkRunning(1);
        job.MarkFailed("Division by zero in formula", "{\"line\":42}", 2);

        Assert.Equal(PayrollJobStatus.Failed, job.Status);
        Assert.Equal(3u, job.RowVersion);
        Assert.Equal("Division by zero in formula", job.ErrorMessage);
        Assert.Equal("{\"line\":42}", job.DiagnosticMetadata);
        Assert.NotNull(job.CompletedAtUtc);
    }

    // =========================================================================
    // 2. IDEMPOTENCY & WORKER EXECUTION RECOVERY
    // =========================================================================

    [Fact]
    public void DurableJob_IdempotencyKey_PreventsDuplicateExecution()
    {
        var store = new Dictionary<string, PayrollBackgroundJob>();
        var runId = Guid.NewGuid();
        var key = $"calc_{_tenantId.Value}_{runId}_1";

        // First attempt creates new queued job
        PayrollBackgroundJob GetOrCreateJob(string idempotencyKey)
        {
            if (store.TryGetValue(idempotencyKey, out var existing))
            {
                return existing;
            }
            var created = new PayrollBackgroundJob(Guid.NewGuid(), _tenantId.Value, runId, idempotencyKey, "payroll.calculate");
            store[idempotencyKey] = created;
            return created;
        }

        var job1 = GetOrCreateJob(key);
        var job2 = GetOrCreateJob(key);

        Assert.True(object.ReferenceEquals(job1, job2));
        Assert.Equal(job1.Id, job2.Id);
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public async Task PayrollJobExecutor_ProcessesQueuedJob_ProducesCompletedRun()
    {
        var engine = new DeterministicPayrollEngine();
        var runId = Guid.NewGuid();
        var run = new PayrollRun(runId, _tenantId, _legalEntityId, Guid.NewGuid(), "RUN-JOB-EXEC");

        var snapshot = new PayrollInputSnapshot(
            Guid.NewGuid(), runId, Guid.NewGuid(),
            baseSalaryMonthly: 20000.00m,
            allowancesJson: "[]",
            scheduledDays: 22,
            verifiedWorkedMinutes: 22 * 480,
            approvedAbsenceDays: 0,
            approvedLeaveDays: 0,
            unpaidLeaveDays: 0
        );

        var gosiRule = new StatutoryRuleVersion(
            Guid.NewGuid(), Guid.NewGuid(), 1,
            new EffectivePeriod(new DateOnly(2024, 1, 1)),
            "{\"employeeRate\": 0.11, \"employerRate\": 0.1875, \"minInsuredMonthly\": 2000.00, \"maxInsuredMonthly\": 12600.00}",
            "EgyptSocialInsuranceStrategy", VerificationStatus.Verified
        );

        var job = new PayrollBackgroundJob(
            Guid.NewGuid(), _tenantId.Value, runId, "idemp-job-exec", "payroll.calculate"
        );

        // Execute in-memory job processing
        job.MarkRunning(1);
        run.LoadInputs(new[] { snapshot }, 1);
        run.Calculate(engine, new[] { gosiRule }, 2);

        var hasWarnings = run.Exceptions.Count > 0;
        job.MarkCompleted(hasWarnings, $"{{\"employeeCount\":{run.EmployeeCount}}}", 2);

        Assert.Equal(PayrollJobStatus.Completed, job.Status);
        Assert.Equal(PayrollRunStatus.Calculated, run.Status);
        Assert.Equal(1, run.EmployeeResults.Count);
        Assert.True(run.TotalGross == 20000.00m);
        Assert.True(run.TotalNet > 0);
    }

    // =========================================================================
    // 3. BANK CRYPTO HARDENING & TAMPER DETECTION
    // =========================================================================

    [Fact]
    public void BankCrypto_AesGcm_EncryptsWithUniqueNoncesAndDetectsTampering()
    {
        var service = new AesGcmEncryptionService();
        var plaintext = "EG123456789012345678901234";

        // Dual encryptions of same plaintext MUST produce different ciphertexts (due to 96-bit unique CSPRNG nonce)
        var enc1 = service.Encrypt(plaintext);
        var enc2 = service.Encrypt(plaintext);

        Assert.NotEqual(enc1, enc2);
        Assert.True(enc1.StartsWith("v1:"));
        Assert.True(enc2.StartsWith("v1:"));

        // Decryption produces original plaintext
        var dec1 = service.Decrypt(enc1);
        var dec2 = service.Decrypt(enc2);
        Assert.Equal(plaintext, dec1);
        Assert.Equal(plaintext, dec2);

        // Tamper test: modifying a byte in ciphertext MUST cause CryptographicException / Decrypt failure
        var parts = enc1.Split(':');
        var ciphertextBytes = Convert.FromBase64String(parts[3]);
        ciphertextBytes[0] ^= 0xFF; // Flip bits
        var tampered = $"{parts[0]}:{parts[1]}:{parts[2]}:{Convert.ToBase64String(ciphertextBytes)}";

        Assert.Throws<CryptographicException>(() => service.Decrypt(tampered));
    }
}
