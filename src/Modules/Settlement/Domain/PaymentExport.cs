using System;

namespace Workforce.Modules.Settlement.Domain;

public class PaymentExport
{
    public Guid Id { get; private set; }
    public Guid SettlementBatchId { get; private set; }
    public ExportFormat Format { get; private set; }
    public string StoragePath { get; private set; }
    public string FileSha256 { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public int DownloadCount { get; private set; }

    private PaymentExport()
    {
        StoragePath = string.Empty;
        FileSha256 = string.Empty;
    }

    public PaymentExport(
        Guid id,
        Guid settlementBatchId,
        ExportFormat format,
        string storagePath,
        string fileSha256)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (settlementBatchId == Guid.Empty) throw new ArgumentException("SettlementBatchId cannot be empty.", nameof(settlementBatchId));

        Id = id;
        SettlementBatchId = settlementBatchId;
        Format = format;
        StoragePath = storagePath.Trim();
        FileSha256 = fileSha256.Trim();
        CreatedAtUtc = DateTime.UtcNow;
        DownloadCount = 0;
    }

    public void RecordDownload()
    {
        DownloadCount++;
    }
}
