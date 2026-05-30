using EventHorizon.Configuration;
using EventHorizon.EntryPoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventHorizon.AGUI;

public sealed class AGUIServerRunner : IAGUIServerRunner
{
    private readonly AppOptions _options;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger<AGUIServerRunner> _logger;

    public AGUIServerRunner(
        AppOptions options,
        IHostApplicationLifetime hostApplicationLifetime,
        ILogger<AGUIServerRunner> logger)
    {
        _options = options;
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
    }

    public Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting EventHorizon AG-UI server on {Urls}. API base path: {ApiBasePath}. Raw AG-UI endpoint: {RawEndpointPath}.",
            string.Join(", ", _options.AGUI.Urls),
            _options.AGUI.ApiBasePath,
            _options.AGUI.RawEndpointPath);

        if (cancellationToken.CanBeCanceled)
        {
            cancellationToken.Register(_hostApplicationLifetime.StopApplication);
        }

        return Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }
}
