using EventHorizon.AGUI;
using EventHorizon.Configuration;
using EventHorizon.Context;
using EventHorizon.Conversations;
using EventHorizon.Execution;
using EventHorizon.Pricing;
using EventHorizon.Prompting;
using EventHorizon.Providers;
using EventHorizon.Workspace;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EventHorizon;

internal sealed class Startup(IConfiguration configuration)
{
    public void ConfigureServices(IServiceCollection services)
    {
        var pathEnvironment = ResolvePathEnvironment();
        services
            .AddEventHorizonConfiguration(pathEnvironment)
            .AddEventHorizonWorkspace()
            .AddEventHorizonContext()
            .AddEventHorizonConversations()
            .AddEventHorizonPrompting()
            .AddEventHorizonProviders()
            .AddEventHorizonPricing()
            .AddEventHorizonExecution()
            .AddEventHorizonAGUI();
    }

    public void Configure(WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<AppOptions>>().Value;
        if (options.AGUI.Urls is null || options.AGUI.Urls.Count == 0)
        {
            throw new InvalidOperationException("AGUI:Urls configuration is required.");
        }

        app.Urls.Clear();
        foreach (var url in options.AGUI.Urls)
        {
            app.Urls.Add(url);
        }

        var runtime = app.Services.GetRequiredService<IEventHorizonRuntime>();
        AGUIEndpoints.Map(app, options.AGUI, runtime);
    }

    private IPathEnvironment ResolvePathEnvironment()
        => configuration.GetSection(nameof(PathEnvironment)).Get<ConfiguredPathEnvironment>()?.ToPathEnvironment()
           ?? new PathEnvironment();

    private sealed class ConfiguredPathEnvironment
    {
        public string? CurrentDirectory { get; set; }

        public string? HomeDirectory { get; set; }

        public IPathEnvironment ToPathEnvironment()
            => new ConfiguredPathEnvironmentValue(
                string.IsNullOrWhiteSpace(CurrentDirectory) ? Environment.CurrentDirectory : CurrentDirectory,
                string.IsNullOrWhiteSpace(HomeDirectory)
                    ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                    : HomeDirectory);
    }

    private sealed class ConfiguredPathEnvironmentValue : IPathEnvironment
    {
        public ConfiguredPathEnvironmentValue(string currentDirectory, string homeDirectory)
        {
            CurrentDirectory = currentDirectory;
            HomeDirectory = homeDirectory;
        }

        public string CurrentDirectory { get; }

        public string HomeDirectory { get; }
    }
}
