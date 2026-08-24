using System.Collections.Generic;
using Workforce.SharedKernel.Primitives;

namespace Workforce.SharedKernel.Security;

public class UserContext : IUserContext
{
    public UserId UserId { get; }
    public TenantId TenantId { get; }
    public LegalEntityId? LegalEntityId { get; }
    
    public IReadOnlySet<TenantId> AllowedTenants { get; }
    public IReadOnlySet<LegalEntityId> AllowedLegalEntities { get; }
    
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
        IEnumerable<string> entitlements,
        IEnumerable<TenantId>? allowedTenants = null,
        IEnumerable<LegalEntityId>? allowedLegalEntities = null)
    {
        UserId = userId;
        TenantId = tenantId;
        LegalEntityId = legalEntityId;
        Culture = culture;
        Timezone = timezone;
        Permissions = new HashSet<string>(permissions);
        Entitlements = new HashSet<string>(entitlements);
        
        var tenantSet = new HashSet<TenantId>(allowedTenants ?? new[] { tenantId });
        tenantSet.Add(tenantId);
        AllowedTenants = tenantSet;

        var entitySet = new HashSet<LegalEntityId>(allowedLegalEntities ?? (legalEntityId.HasValue ? new[] { legalEntityId.Value } : System.Array.Empty<LegalEntityId>()));
        if (legalEntityId.HasValue)
        {
            entitySet.Add(legalEntityId.Value);
        }
        AllowedLegalEntities = entitySet;
    }

    public bool HasPermission(string permission)
    {
        return Permissions.Contains(permission);
    }

    public bool HasEntitlement(string entitlement)
    {
        return Entitlements.Contains(entitlement);
    }

    public bool IsAuthorizedForTenant(TenantId tenantId)
    {
        return AllowedTenants.Contains(tenantId);
    }

    public bool IsAuthorizedForLegalEntity(LegalEntityId legalEntityId)
    {
        return AllowedLegalEntities.Contains(legalEntityId);
    }
}
