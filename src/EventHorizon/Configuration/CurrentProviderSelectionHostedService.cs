using Microsoft.Extensions.Hosting;

namespace EventHorizon.Configuration;

internal sealed class CurrentProviderSelectionHostedService : IHostedService
{
    private readonly IProviderConfigurationService _providerConfigurationService;

    public CurrentProviderSelectionHostedService(IProviderConfigurationService providerConfigurationService)
    {
        _providerConfigurationService = providerConfigurationService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _providerConfigurationService.EnsureCurrentProvider(cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
