using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Workforce.Modules.Recruitment.Domain;
using Workforce.Modules.Recruitment.Infrastructure;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Recruitment.Api;

[ApiController]
[Route("api/v1/recruitment/pipelines")]
public class RecruitmentPipelinesController : ControllerBase
{
    private readonly IRecruitmentRepository _repository;
    private readonly IUserContext _userContext;

    public RecruitmentPipelinesController(IRecruitmentRepository repository, IUserContext userContext)
    {
        _repository = repository;
        _userContext = userContext;
    }

    [HttpGet("default")]
    [ProducesResponseType(typeof(RecruitmentPipelineVersion), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDefaultPipeline(CancellationToken ct)
    {
        var version = await _repository.GetDefaultPipelineVersionAsync(_userContext.TenantId, ct);
        if (version == null) return NotFound();
        return Ok(version);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RecruitmentPipeline), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPipelineById(Guid id, CancellationToken ct)
    {
        var pipeline = await _repository.GetPipelineWithVersionsAsync(_userContext.TenantId, id, ct);
        if (pipeline == null) return NotFound();
        return Ok(pipeline);
    }

    [HttpGet("versions/{versionId:guid}")]
    [ProducesResponseType(typeof(RecruitmentPipelineVersion), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPipelineVersionById(Guid versionId, CancellationToken ct)
    {
        var version = await _repository.GetPipelineVersionWithStagesAsync(versionId, ct);
        if (version == null) return NotFound();
        return Ok(version);
    }
}
