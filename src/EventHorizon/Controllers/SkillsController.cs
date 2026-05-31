using EventHorizon.Configuration;
using EventHorizon.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace EventHorizon.Controllers;

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
    public ActionResult<SkillRemoveResponseDTO> RemoveGlobal(string skillName, CancellationToken cancellationToken)
    {
        var result = _skillService.RemoveGlobal(skillName, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpDelete("sessions/{sessionId}/{skillName}")]
    public async Task<ActionResult<SkillRemoveResponseDTO>> RemoveSessionAsync(string sessionId, string skillName, CancellationToken cancellationToken)
    {
        var result = await _skillService.RemoveSessionAsync(sessionId, skillName, cancellationToken).ConfigureAwait(false);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
