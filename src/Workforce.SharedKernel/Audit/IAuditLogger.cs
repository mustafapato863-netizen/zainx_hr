using System.Threading;
using System.Threading.Tasks;

namespace Workforce.SharedKernel.Audit;

public interface IAuditLogger
{
    Task LogAsync(AuditRecord record, CancellationToken cancellationToken = default);
}
