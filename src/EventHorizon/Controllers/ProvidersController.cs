using EventHorizon.Configuration;
using EventHorizon.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace EventHorizon.Controllers;

[ApiController]
[Route("api/providers")]
public sealed class ProvidersController : ControllerBase
{
    private readonly IProviderTestingService _providerTestingService;

    public ProvidersController(IProviderTestingService providerTestingService)
    {
        _providerTestingService = providerTestingService;
    }

    [HttpPost("test")]
    public async Task<ActionResult<ProviderTestResponseDTO>> TestAsync(ProviderTestRequestDTO request, CancellationToken cancellationToken)
        => Ok(await _providerTestingService.TestAsync(request, cancellationToken).ConfigureAwait(false));
}
