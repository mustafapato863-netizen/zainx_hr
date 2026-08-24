using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Workforce.Modules.Compliance.Domain;
using Workforce.Modules.Compliance.Infrastructure;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Compliance.Api;

public record StatutoryRuleDto(
    string Code,
    string NameEn,
    string NameAr,
    string Jurisdiction,
    string Category,
    string SourceReferenceLaw,
    bool IsVerified
);

[ApiController]
[Route("api/v1/compliance/rules")]
public class ComplianceRulesController : ControllerBase
{
    private readonly IComplianceRepository _repository;
    private readonly IUserContext _userContext;

    public ComplianceRulesController(IComplianceRepository repository, IUserContext userContext)
    {
        _repository = repository;
        _userContext = userContext;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StatutoryRuleDto>>> GetRules(
        [FromQuery] Jurisdiction jurisdiction = Jurisdiction.Egypt,
        CancellationToken ct = default)
    {
        var rules = await _repository.GetRulesByJurisdictionAsync(jurisdiction, ct);
        var dtos = new List<StatutoryRuleDto>();
        foreach (var r in rules)
        {
            dtos.Add(new StatutoryRuleDto(
                r.Code,
                r.NameEn,
                r.NameAr,
                r.Jurisdiction.ToString(),
                r.Category.ToString(),
                r.SourceReferenceLaw,
                r.IsVerified
            ));
        }

        return Ok(dtos);
    }
}
