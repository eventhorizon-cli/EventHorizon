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
    public async Task<ActionResult<SkillImportResponse>> ImportAsync(ImportSkillRequest request, CancellationToken cancellationToken)
        => Ok(await _skillService.ImportAsync(request, cancellationToken).ConfigureAwait(false));
}

