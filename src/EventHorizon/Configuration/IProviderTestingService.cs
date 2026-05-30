using EventHorizon.AGUI.DTOs;

namespace EventHorizon.Configuration;

public interface IProviderTestingService
{
    Task<ProviderTestResponseDTO> TestAsync(ProviderTestRequestDTO request, CancellationToken cancellationToken);
}
