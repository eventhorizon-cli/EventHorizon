namespace EventHorizon.Configuration;

public interface IProviderTestingService
{
    Task<ProviderTestResponse> TestAsync(ProviderTestRequest request, CancellationToken cancellationToken);
}

