using System.Collections.Generic;
using Workforce.SharedKernel.Primitives;

namespace Workforce.SharedKernel.Security;

public class UserContext : IUserContext
{
    public UserId UserId { get; }
    public TenantId TenantId { get; }
    public LegalEntityId? LegalEntityId { get; }
    
    public string Culture { get; }
    public string Timezone { get; }
    
    public IReadOnlySet<string> Permissions { get; }
    public IReadOnlySet<string> Entitlements { get; }

    public UserContext(
        UserId userId, 
        TenantId tenantId, 
        LegalEntityId? legalEntityId, 
        string culture, 
        string timezone, 
        IEnumerable<string> permissions, 
        IEnumerable<string> entitlements)
    {
        UserId = userId;
        TenantId = tenantId;
        LegalEntityId = legalEntityId;
        Culture = culture;
        Timezone = timezone;
        Permissions = new HashSet<string>(permissions);
        Entitlements = new HashSet<string>(entitlements);
    }

    public bool HasPermission(string permission)
    {
        return Permissions.Contains(permission);
    }

    public bool HasEntitlement(string entitlement)
    {
        return Entitlements.Contains(entitlement);
    }
}
