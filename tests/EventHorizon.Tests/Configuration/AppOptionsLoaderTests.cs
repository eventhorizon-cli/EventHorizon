using EventHorizon.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EventHorizon.Tests.Configuration;

public sealed class AppOptionsLoaderTests : IDisposable
{
    private readonly string _root;
    private readonly string _originalDirectory;

    public AppOptionsLoaderTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "eventhorizon-config-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        _originalDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_root);
    }

    [Fact]
    public void Host_Binds_Json_Config_And_Command_Line_Overrides_Into_AppOptions()
    {
        File.WriteAllText(Path.Combine(_root, "eventhorizon.json"), """
        {
          "Provider": { "Type": "openai", "Model": "gpt-4.1-mini" },
          "WorkspaceRoot": "./workspace"
        }
        """);

        var command = new EffectiveCommandOptions
        {
            ProviderType = "anthropic",
            Model = "claude-sonnet-4-20250514"
        };

        using var host = EventHorizonHost.Create([], command);
        var options = host.Services.GetRequiredService<IOptions<AppOptions>>().Value;

        Assert.Equal("anthropic", options.Provider.Type);
        Assert.Equal("claude-sonnet-4-20250514", options.Provider.Model);
        Assert.EndsWith(Path.DirectorySeparatorChar + "workspace", options.WorkspaceRoot, StringComparison.Ordinal);
        Assert.Contains("eventhorizon-config-tests", options.WorkspaceRoot, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_originalDirectory);
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}

