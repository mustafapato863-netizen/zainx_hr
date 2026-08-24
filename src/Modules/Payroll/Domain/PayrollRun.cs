using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Workforce.Modules.Compliance.Domain;
using Workforce.Modules.Payroll.Domain.CalculationEngine;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Payroll.Domain;

public class PayrollRun
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public LegalEntityId LegalEntityId { get; private set; }
    public Guid PeriodId { get; private set; }
    public string Code { get; private set; }
    public PayrollRunStatus Status { get; private set; }
    public string Currency { get; private set; }
    public decimal TotalGross { get; private set; }
    public decimal TotalNet { get; private set; }
    public decimal TotalEmployerContributions { get; private set; }
    public int EmployeeCount { get; private set; }
    public string ReproducibilityHash { get; private set; }
    public Guid? ApprovalRequestId { get; private set; }
    public DateTime? FinalizedAtUtc { get; private set; }
    public Guid? FinalizedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public uint RowVersion { get; private set; }

    private readonly List<PayrollInputSnapshot> _inputSnapshots = new();
    public IReadOnlyCollection<PayrollInputSnapshot> InputSnapshots => _inputSnapshots.AsReadOnly();

    private readonly List<PayrollEmployeeResult> _employeeResults = new();
    public IReadOnlyCollection<PayrollEmployeeResult> EmployeeResults => _employeeResults.AsReadOnly();

    private readonly List<PayrollException> _exceptions = new();
    public IReadOnlyCollection<PayrollException> Exceptions => _exceptions.AsReadOnly();

    private PayrollRun()
    {
        Code = string.Empty;
        Currency = "EGP";
        ReproducibilityHash = string.Empty;
    }

    public PayrollRun(
        Guid id,
        TenantId tenantId,
        LegalEntityId legalEntityId,
        Guid periodId,
        string code,
        string currency = "EGP")
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (periodId == Guid.Empty) throw new ArgumentException("PeriodId cannot be empty.", nameof(periodId));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code is required.", nameof(code));

        Id = id;
        TenantId = tenantId;
        LegalEntityId = legalEntityId;
        PeriodId = periodId;
        Code = code.Trim().ToUpperInvariant();
        Status = PayrollRunStatus.Draft;
        Currency = string.IsNullOrWhiteSpace(currency) ? "EGP" : currency.Trim().ToUpperInvariant();
        TotalGross = 0;
        TotalNet = 0;
        TotalEmployerContributions = 0;
        EmployeeCount = 0;
        ReproducibilityHash = string.Empty;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
        RowVersion = 1;
    }

    public void LoadInputs(IEnumerable<PayrollInputSnapshot> snapshots, uint expectedRowVersion)
    {
        VerifyRowVersion(expectedRowVersion);
        VerifyNotFinalized();

        if (Status != PayrollRunStatus.Draft && Status != PayrollRunStatus.InputsLoaded)
        {
            throw new InvalidOperationException($"Cannot load inputs in '{Status}' status.");
        }

        _inputSnapshots.Clear();
        _inputSnapshots.AddRange(snapshots);
        EmployeeCount = _inputSnapshots.Count;
        Status = PayrollRunStatus.InputsLoaded;
        UpdatedAtUtc = DateTime.UtcNow;
        RowVersion++;
    }

    public void Calculate(
        IPayrollCalculationEngine engine,
        IReadOnlyList<StatutoryRuleVersion> activeRules,
        uint expectedRowVersion)
    {
        VerifyRowVersion(expectedRowVersion);
        VerifyNotFinalized();

        if (Status != PayrollRunStatus.InputsLoaded && Status != PayrollRunStatus.Calculated)
        {
            throw new InvalidOperationException($"Cannot execute calculation in '{Status}' status. Must load inputs first.");
        }

        _employeeResults.Clear();
        _exceptions.RemoveAll(e => e.Status == ExceptionStatus.Open);

        decimal runGross = 0;
        decimal runNet = 0;
        decimal runEmployer = 0;

        foreach (var snapshot in _inputSnapshots)
        {
            var result = engine.Calculate(snapshot, activeRules, out var empExceptions);
            _employeeResults.Add(result);
            _exceptions.AddRange(empExceptions);

            runGross += result.GrossPay;
            runNet += result.NetPay;
            runEmployer += result.EmployerContributions;
        }

        TotalGross = RoundingPolicy.RoundLine(runGross);
        TotalNet = RoundingPolicy.RoundLine(runNet);
        TotalEmployerContributions = RoundingPolicy.RoundLine(runEmployer);

        // Compute Reproducibility Fingerprint SHA-256
        var hashInput = $"{Id}:{TotalGross:F2}:{TotalNet:F2}:{EmployeeCount}:{engine.EngineVersion}";
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(hashInput));
        ReproducibilityHash = Convert.ToHexString(bytes);

        Status = PayrollRunStatus.Calculated;
        UpdatedAtUtc = DateTime.UtcNow;
        RowVersion++;
    }

    public void SubmitForReview(uint expectedRowVersion)
    {
        VerifyRowVersion(expectedRowVersion);
        VerifyNotFinalized();

        if (Status != PayrollRunStatus.Calculated)
        {
            throw new InvalidOperationException($"Cannot submit for review from '{Status}' status. Must calculate first.");
        }

        if (_exceptions.Any(e => e.Severity == ExceptionSeverity.Blocking && e.Status == ExceptionStatus.Open))
        {
            throw new InvalidOperationException("Cannot submit payroll for review with unresolved blocking exceptions.");
        }

        Status = PayrollRunStatus.UnderReview;
        UpdatedAtUtc = DateTime.UtcNow;
        RowVersion++;
    }

    public void Approve(Guid approvalRequestId, uint expectedRowVersion)
    {
        VerifyRowVersion(expectedRowVersion);
        VerifyNotFinalized();

        if (Status != PayrollRunStatus.UnderReview)
        {
            throw new InvalidOperationException($"Cannot approve payroll run in '{Status}' status.");
        }

        if (_exceptions.Any(e => e.Severity == ExceptionSeverity.Blocking && e.Status == ExceptionStatus.Open))
        {
            throw new InvalidOperationException("Cannot approve payroll run with open blocking exceptions.");
        }

        ApprovalRequestId = approvalRequestId;
        Status = PayrollRunStatus.Approved;
        UpdatedAtUtc = DateTime.UtcNow;
        RowVersion++;
    }

    public void FinalizeRun(Guid finalizerUserId, uint expectedRowVersion)
    {
        VerifyRowVersion(expectedRowVersion);
        VerifyNotFinalized();

        if (Status != PayrollRunStatus.Approved)
        {
            throw new InvalidOperationException($"Only approved payroll runs can be finalized. Current status: '{Status}'.");
        }

        if (_exceptions.Any(e => e.Severity == ExceptionSeverity.Blocking && e.Status == ExceptionStatus.Open))
        {
            throw new InvalidOperationException("Cannot finalize payroll run with unresolved blocking exceptions.");
        }

        Status = PayrollRunStatus.Finalized;
        FinalizedAtUtc = DateTime.UtcNow;
        FinalizedByUserId = finalizerUserId;
        UpdatedAtUtc = DateTime.UtcNow;
        RowVersion++;
    }

    public void PublishOutputs(uint expectedRowVersion)
    {
        VerifyRowVersion(expectedRowVersion);

        if (Status != PayrollRunStatus.Finalized)
        {
            throw new InvalidOperationException($"Cannot publish outputs for non-finalized payroll run. Status: '{Status}'.");
        }

        Status = PayrollRunStatus.OutputsPublished;
        UpdatedAtUtc = DateTime.UtcNow;
        RowVersion++;
    }

    private void VerifyNotFinalized()
    {
        if (Status == PayrollRunStatus.Finalized || Status == PayrollRunStatus.OutputsPublished)
        {
            throw new InvalidOperationException("FINALIZATION IS A HARD BOUNDARY: Finalized payroll runs are permanently immutable and cannot be modified.");
        }
    }

    private void VerifyRowVersion(uint expected)
    {
        if (expected != RowVersion)
        {
            throw new InvalidOperationException("Optimistic concurrency conflict on payroll run.");
        }
    }
}
