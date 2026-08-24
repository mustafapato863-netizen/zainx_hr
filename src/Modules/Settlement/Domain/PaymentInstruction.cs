using System;
using Workforce.Modules.Payroll.Domain;

namespace Workforce.Modules.Settlement.Domain;

public class PaymentInstruction
{
    public Guid Id { get; private set; }
    public Guid SettlementBatchId { get; private set; }
    public Guid EmploymentId { get; private set; }
    public string BeneficiaryName { get; private set; }
    public string BankCode { get; private set; }
    public string EncryptedAccountOrIban { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentInstructionStatus Status { get; private set; }

    private PaymentInstruction()
    {
        BeneficiaryName = string.Empty;
        BankCode = string.Empty;
        EncryptedAccountOrIban = string.Empty;
    }

    public PaymentInstruction(
        Guid id,
        Guid settlementBatchId,
        Guid employmentId,
        string beneficiaryName,
        string bankCode,
        string encryptedAccountOrIban,
        decimal amount,
        PaymentInstructionStatus status = PaymentInstructionStatus.Pending)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (settlementBatchId == Guid.Empty) throw new ArgumentException("SettlementBatchId cannot be empty.", nameof(settlementBatchId));
        if (employmentId == Guid.Empty) throw new ArgumentException("EmploymentId cannot be empty.", nameof(employmentId));
        if (string.IsNullOrWhiteSpace(beneficiaryName)) throw new ArgumentException("BeneficiaryName is required.", nameof(beneficiaryName));

        Id = id;
        SettlementBatchId = settlementBatchId;
        EmploymentId = employmentId;
        BeneficiaryName = beneficiaryName.Trim();
        BankCode = string.IsNullOrWhiteSpace(bankCode) ? "MISR" : bankCode.Trim();
        EncryptedAccountOrIban = encryptedAccountOrIban.Trim();
        Amount = RoundingPolicy.RoundLine(amount);
        Status = status;
    }

    public void MarkProcessed()
    {
        Status = PaymentInstructionStatus.Processed;
    }
}
