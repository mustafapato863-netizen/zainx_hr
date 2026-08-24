using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Workforce.Modules.Settlement.Domain.ExportAdapters;

public record ExportResult(
    string ContentType,
    string FileName,
    byte[] FileBytes,
    string FileSha256
);

public interface IPaymentExportAdapter
{
    ExportFormat Format { get; }
    Task<ExportResult> GenerateExportAsync(SettlementBatch batch, CancellationToken ct = default);
}
