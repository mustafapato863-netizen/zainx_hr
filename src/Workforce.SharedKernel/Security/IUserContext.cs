using System.Collections.Generic;
using Workforce.SharedKernel.Primitives;

namespace Workforce.SharedKernel.Security;

public interface IUserContext
{
    UserId UserId { get; }
    TenantId TenantId { get; }
    LegalEntityId? LegalEntityId { get; }
    
    string Culture { get; }
    string Timezone { get; }
    
    IReadOnlySet<string> Permissions { get; }
    IReadOnlySet<string> Entitlements { get; }
    
    bool HasPermission(string permission);
    bool HasEntitlement(string entitlement);
}
