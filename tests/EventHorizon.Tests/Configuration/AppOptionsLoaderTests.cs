using EventHorizon.Configuration;
using EventHorizon.Workspace;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EventHorizon.Tests.Configuration;

public sealed class AppOptionsLoaderTests : IDisposable
{
    private readonly string _root;
    private readonly string _homeDirectory;
    private readonly string _workspaceDirectory;

    public AppOptionsLoaderTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "eventhorizon-config-tests", Guid.NewGuid().ToString("N"));
        _homeDirectory = Path.Combine(_root, "home");
        _workspaceDirectory = Path.Combine(_root, "workspace");
        Directory.CreateDirectory(_homeDirectory);
        Directory.CreateDirectory(_workspaceDirectory);
    }

    [Fact]
    public void Create_Creates_Default_Home_Config_File_When_Missing()
    {
        using var host = CreateHost();

        var configFilePath = GetDefaultConfigFilePath();

        Assert.True(File.Exists(configFilePath));
        Assert.Contains("\"AGUI\"", File.ReadAllText(configFilePath), StringComparison.Ordinal);
    }

    [Fact]
    public void Create_Loads_Home_Config_And_Applies_CurrentDefaultProvider_Migration()
    {
        WriteHomeConfig("""
        {
          "CurrentProvider": "home",
          "Providers": {
            "home": {
              "Type": "openai",
              "Model": "gpt-home"
            }
          }
        }
        """);

        using var host = CreateHost();
        var options = host.Services.GetRequiredService<IOptions<AppOptions>>().Value;

        Assert.Equal("home", options.CurrentDefaultProvider);
        Assert.Null(options.CurrentProvider);
        Assert.Equal("openai", options.Provider.Type);
        Assert.Equal("gpt-home", options.Provider.Model);
        Assert.Contains(options.Providers, static pair => pair.Key == "home");
    }

    [Fact]
    public void Create_Applies_Config_Precedence_BuiltIn_Home_External_Then_Environment()
    {
        WriteHomeConfig("""
        {
          "CurrentDefaultProvider": "home",
          "Providers": {
            "home": {
              "Type": "openai",
              "Model": "home-model"
            },
            "external": {
              "Type": "openai",
              "Model": "external-model"
            }
          }
        }
        """);

        var externalConfigPath = Path.Combine(_root, "external.json");
        File.WriteAllText(externalConfigPath, """
        {
          "CurrentDefaultProvider": "external",
          "Providers": {
            "external": {
              "Type": "openai",
              "Model": "external-model"
            }
          }
        }
        """);

        const string environmentKey = "EVENTHORIZON__Providers__external__Model";
        var originalValue = Environment.GetEnvironmentVariable(environmentKey);
        Environment.SetEnvironmentVariable(environmentKey, "environment-model");

        try
        {
            using var host = CreateHost(new EffectiveCommandOptions { ConfigFile = externalConfigPath });
            var options = host.Services.GetRequiredService<IOptions<AppOptions>>().Value;

            Assert.Equal("external", options.CurrentDefaultProvider);
            Assert.Equal("environment-model", options.Providers["external"].Model);
            Assert.Equal("environment-model", options.Provider.Model);
        }
        finally
        {
            Environment.SetEnvironmentVariable(environmentKey, originalValue);
        }
    }

    [Fact]
    public async Task CurrentProviderSelectionHostedService_Prompts_And_Persists_Choice_When_CurrentDefaultProvider_Is_Missing()
    {
        WriteHomeConfig("""
        {
          "CurrentDefaultProvider": "",
          "Providers": {
            "openai": {
              "Type": "openai",
              "Model": "gpt-4.1-mini"
            },
            "anthropic": {
              "Type": "anthropic",
              "Model": "claude-sonnet-4-20250514"
            }
          }
        }
        """);

        using var host = CreateHost();
        var originalIn = Console.In;

        try
        {
            Console.SetIn(new StringReader("1" + Environment.NewLine));
            var hostedService = host.Services
                .GetServices<IHostedService>()
                .Single(static service => service is CurrentProviderSelectionHostedService);

            await hostedService.StartAsync(CancellationToken.None);
        }
        finally
        {
            Console.SetIn(originalIn);
        }

        var options = host.Services.GetRequiredService<IOptions<AppOptions>>().Value;
        Assert.Equal("anthropic", options.CurrentDefaultProvider!);
        Assert.Equal("anthropic", options.Provider.Type!);
        var persisted = File.ReadAllText(GetDefaultConfigFilePath());
        Assert.Contains("\"CurrentDefaultProvider\": \"anthropic\"", persisted, StringComparison.Ordinal);
        Assert.DoesNotContain("\"CurrentProvider\"", persisted, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_Does_Not_Read_WorkspaceRoot_From_Config_And_Uses_CurrentDirectory()
    {
        WriteWorkspaceConfig("""
        {
          "WorkspaceRoot": "./ignored-from-config"
        }
        """);

        using var host = CreateHost();

        var workspaceContext = host.Services.GetRequiredService<WorkspaceContext>();
        Assert.Equal(Path.GetFullPath(_workspaceDirectory), workspaceContext.WorkspaceRoot);
    }

    [Fact]
    public void Create_Allows_Command_Line_Workspace_Override()
    {
        var overrideDirectory = Path.Combine(_root, "override-workspace");
        Directory.CreateDirectory(overrideDirectory);

        using var host = CreateHost(new EffectiveCommandOptions { WorkspaceRoot = overrideDirectory });

        var workspaceContext = host.Services.GetRequiredService<WorkspaceContext>();
        Assert.Equal(Path.GetFullPath(overrideDirectory), workspaceContext.WorkspaceRoot);
    }

    [Fact]
    public void ProviderConfigurationService_Persists_CurrentDefaultProvider_To_Home_Config()
    {
        WriteHomeConfig("""
        {
          "Providers": {
            "openai": {
              "Type": "openai",
              "Model": "gpt-4.1-mini"
            },
            "anthropic": {
              "Type": "anthropic",
              "Model": "claude-sonnet-4-20250514"
            }
          }
        }
        """);

        using var host = CreateHost();

        var providerConfigurationService = host.Services.GetRequiredService<IProviderConfigurationService>();
        providerConfigurationService.SetCurrentProvider("anthropic", persist: true);

        var persisted = File.ReadAllText(GetDefaultConfigFilePath());
        Assert.Contains("\"CurrentDefaultProvider\": \"anthropic\"", persisted, StringComparison.Ordinal);
        Assert.DoesNotContain("\"CurrentProvider\"", persisted, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private global::Microsoft.Extensions.Hosting.IHost CreateHost(EffectiveCommandOptions? commandOptions = null)
        => EventHorizonHost.Create([], commandOptions ?? new EffectiveCommandOptions(), new TestPathEnvironment(_workspaceDirectory, _homeDirectory));

    private string GetDefaultConfigFilePath()
        => Path.Combine(_homeDirectory, ".eventhorizon", "appsettings.json");

    private void WriteHomeConfig(string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(GetDefaultConfigFilePath())!);
        File.WriteAllText(GetDefaultConfigFilePath(), content);
    }

    private void WriteWorkspaceConfig(string content)
        => File.WriteAllText(Path.Combine(_workspaceDirectory, "appsettings.json"), content);

    private sealed class TestPathEnvironment : IPathEnvironment
    {
        public TestPathEnvironment(string currentDirectory, string homeDirectory)
        {
            CurrentDirectory = currentDirectory;
            HomeDirectory = homeDirectory;
        }

        public string CurrentDirectory { get; }

        public string HomeDirectory { get; }
    }
}
