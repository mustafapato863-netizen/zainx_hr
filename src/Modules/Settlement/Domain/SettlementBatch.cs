using System;
using System.Collections.Generic;
using System.Linq;
using Workforce.Modules.Payroll.Domain;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Settlement.Domain;

public class SettlementBatch
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public LegalEntityId LegalEntityId { get; private set; }
    public Guid PayrollRunId { get; private set; }
    public string BatchNumber { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string Currency { get; private set; }
    public DateOnly PaymentDate { get; private set; }
    public SettlementStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public uint RowVersion { get; private set; }

    private readonly List<PaymentInstruction> _instructions = new();
    public IReadOnlyCollection<PaymentInstruction> Instructions => _instructions.AsReadOnly();

    private SettlementBatch()
    {
        BatchNumber = string.Empty;
        Currency = "EGP";
    }

    public SettlementBatch(
        Guid id,
        TenantId tenantId,
        LegalEntityId legalEntityId,
        Guid payrollRunId,
        string batchNumber,
        decimal totalAmount,
        DateOnly paymentDate,
        string currency = "EGP")
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (payrollRunId == Guid.Empty) throw new ArgumentException("PayrollRunId cannot be empty.", nameof(payrollRunId));
        if (string.IsNullOrWhiteSpace(batchNumber)) throw new ArgumentException("BatchNumber is required.", nameof(batchNumber));

        Id = id;
        TenantId = tenantId;
        LegalEntityId = legalEntityId;
        PayrollRunId = payrollRunId;
        BatchNumber = batchNumber.Trim().ToUpperInvariant();
        TotalAmount = RoundingPolicy.RoundLine(totalAmount);
        Currency = string.IsNullOrWhiteSpace(currency) ? "EGP" : currency.Trim().ToUpperInvariant();
        PaymentDate = paymentDate;
        Status = SettlementStatus.Draft;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
        RowVersion = 1;
    }

    public void AddInstruction(PaymentInstruction instruction)
    {
        if (Status != SettlementStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot add instructions to settlement batch in '{Status}' status.");
        }

        _instructions.Add(instruction);
    }

    public void Approve(uint expectedRowVersion)
    {
        VerifyRowVersion(expectedRowVersion);

        // 1:1 Reconciliation Invariant: Sum of instructions must exactly equal TotalAmount
        var instructionsSum = RoundingPolicy.RoundLine(_instructions.Sum(i => i.Amount));
        if (instructionsSum != TotalAmount)
        {
            throw new InvalidOperationException($"Settlement reconciliation failed: Total amount ({TotalAmount:F2}) does not match sum of instructions ({instructionsSum:F2}).");
        }

        Status = SettlementStatus.Approved;
        UpdatedAtUtc = DateTime.UtcNow;
        RowVersion++;
    }

    public void MarkExported(uint expectedRowVersion)
    {
        VerifyRowVersion(expectedRowVersion);

        if (Status != SettlementStatus.Approved && Status != SettlementStatus.Exported)
        {
            throw new InvalidOperationException($"Cannot export settlement batch in '{Status}' status. Must be approved first.");
        }

        Status = SettlementStatus.Exported;
        UpdatedAtUtc = DateTime.UtcNow;
        RowVersion++;
    }

    public void Reconcile(uint expectedRowVersion)
    {
        VerifyRowVersion(expectedRowVersion);

        if (Status != SettlementStatus.Exported)
        {
            throw new InvalidOperationException($"Cannot reconcile settlement batch in '{Status}' status. Must be exported first.");
        }

        foreach (var inst in _instructions)
        {
            inst.MarkProcessed();
        }

        Status = SettlementStatus.Reconciled;
        UpdatedAtUtc = DateTime.UtcNow;
        RowVersion++;
    }

    private void VerifyRowVersion(uint expected)
    {
        if (expected != RowVersion)
        {
            throw new InvalidOperationException("Optimistic concurrency conflict on settlement batch.");
        }
    }
}
