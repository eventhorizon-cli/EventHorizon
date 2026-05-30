using EventHorizon.AGUI.DTOs;
using EventHorizon.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace EventHorizon.AGUI.Controllers;

[ApiController]
[Route("api/skills")]
public sealed class SkillsController : ControllerBase
{
    private readonly ISkillService _skillService;

    public SkillsController(ISkillService skillService)
    {
        _skillService = skillService;
    }

    [HttpPost("import")]
    public async Task<ActionResult<SkillImportResponseDTO>> ImportAsync(ImportSkillRequestDTO request, CancellationToken cancellationToken)
        => Ok(await _skillService.ImportAsync(request, cancellationToken).ConfigureAwait(false));

    [HttpDelete("global/{skillName}")]
    public async Task<ActionResult<SkillRemoveResponseDTO>> RemoveGlobalAsync(string skillName, CancellationToken cancellationToken)
    {
        var result = await _skillService.RemoveGlobalAsync(skillName, cancellationToken).ConfigureAwait(false);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpDelete("sessions/{sessionId}/{skillName}")]
    public async Task<ActionResult<SkillRemoveResponseDTO>> RemoveSessionAsync(string sessionId, string skillName, CancellationToken cancellationToken)
    {
        var result = await _skillService.RemoveSessionAsync(sessionId, skillName, cancellationToken).ConfigureAwait(false);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
