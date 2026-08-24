using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Workforce.Modules.Settlement.Domain.ExportAdapters;

public class NeutralCsvPaymentExportAdapter : IPaymentExportAdapter
{
    public ExportFormat Format => ExportFormat.NeutralCsv;

    public Task<ExportResult> GenerateExportAsync(SettlementBatch batch, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine("BatchNumber,PaymentDate,Currency,BeneficiaryName,BankCode,AccountOrIban,Amount");

        foreach (var inst in batch.Instructions)
        {
            // Redact/mask account for neutral export standard display
            var masked = inst.EncryptedAccountOrIban.Length > 4
                ? $"****{inst.EncryptedAccountOrIban[^4..]}"
                : "****";

            sb.AppendLine($"\"{batch.BatchNumber}\",\"{batch.PaymentDate:yyyy-MM-dd}\",\"{batch.Currency}\",\"{inst.BeneficiaryName}\",\"{inst.BankCode}\",\"{masked}\",{inst.Amount:F2}");
        }

        var csvString = sb.ToString();
        var bytes = Encoding.UTF8.GetBytes(csvString);

        using var sha = SHA256.Create();
        var hash = Convert.ToHexString(sha.ComputeHash(bytes));

        var fileName = $"SETTLEMENT_{batch.BatchNumber}_{batch.PaymentDate:yyyyMMdd}.csv";

        return Task.FromResult(new ExportResult(
            "text/csv; charset=utf-8",
            fileName,
            bytes,
            hash
        ));
    }
}
