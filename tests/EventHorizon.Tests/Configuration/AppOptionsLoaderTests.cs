using EventHorizon.AGUI.Controllers;
using EventHorizon.AGUI.DTOs;
using EventHorizon.Configuration;
using EventHorizon.EntryPoints;
using EventHorizon.Providers;
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
        var content = File.ReadAllText(configFilePath);

        Assert.True(File.Exists(configFilePath));
        Assert.Contains("\"AGUI\"", content, StringComparison.Ordinal);
        Assert.DoesNotContain("\"CurrentDefaultProvider\"", content, StringComparison.Ordinal);
        Assert.Contains("\"Providers\": {}", content, StringComparison.Ordinal);
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
    public void Create_Applies_Config_Precedence_BuiltIn_Then_Home_File()
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

        using var host = CreateHost();
        var options = host.Services.GetRequiredService<IOptions<AppOptions>>().Value;

        Assert.Equal("home", options.CurrentDefaultProvider);
        Assert.Equal("home-model", options.Providers["home"].Model);
        Assert.Equal("home-model", options.Provider.Model);
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

    [Fact]
    public async Task SaveAsync_Does_Not_Create_Legacy_Default_Provider_When_Named_Providers_Are_Present()
    {
        using var host = CreateHost();
        var configurationService = host.Services.GetRequiredService<IAppConfigurationService>();

        var saved = await configurationService.SaveAsync(new AppOptions
        {
            AGUI = new AGUIOptions(),
            Agent = new AgentOptions(),
            Provider = new ProviderOptions
            {
                Type = "openai",
                Model = "gpt-4.1-mini",
            },
            CurrentDefaultProvider = "custom-openai",
            Providers = new Dictionary<string, ProviderOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["custom-openai"] = new()
                {
                    Type = "openai-compatible",
                    Model = "my-model",
                    Endpoint = "https://example.com/v1",
                },
            },
            Pricing = new PricingOptions(),
            Conversation = new ConversationOptions(),
            Skills = new SkillCatalogOptions(),
        }, CancellationToken.None);

        Assert.Equal(["custom-openai"], saved.Providers.Keys.OrderBy(static key => key, StringComparer.OrdinalIgnoreCase));
        Assert.DoesNotContain(saved.Providers, static pair => pair.Key.Equals("default", StringComparison.OrdinalIgnoreCase));

        var persisted = File.ReadAllText(GetDefaultConfigFilePath());
        Assert.Contains("\"custom-openai\"", persisted, StringComparison.Ordinal);
        Assert.DoesNotContain("\"default\"", persisted, StringComparison.Ordinal);
        Assert.DoesNotContain("\"gpt-4.1-mini\"", persisted, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveAsync_Preserves_Existing_ApiKey_When_Request_Does_Not_Provide_One()
    {
        using var host = CreateHost();
        var configurationService = host.Services.GetRequiredService<IAppConfigurationService>();

        await configurationService.SaveAsync(new AppOptions
        {
            AGUI = new AGUIOptions(),
            Agent = new AgentOptions(),
            Provider = new ProviderOptions(),
            CurrentDefaultProvider = "openai",
            Providers = new Dictionary<string, ProviderOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["openai"] = new()
                {
                    Type = "openai",
                    Model = "gpt-4.1-mini",
                    ApiKey = "secret-key",
                },
            },
            Pricing = new PricingOptions(),
            Conversation = new ConversationOptions(),
            Skills = new SkillCatalogOptions(),
        }, CancellationToken.None);

        var saved = await configurationService.SaveAsync(new AppOptions
        {
            AGUI = new AGUIOptions(),
            Agent = new AgentOptions(),
            Provider = new ProviderOptions(),
            CurrentDefaultProvider = "openai",
            Providers = new Dictionary<string, ProviderOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["openai"] = new()
                {
                    Type = "openai",
                    Model = "gpt-4.1-mini",
                    ApiKey = null,
                },
            },
            Pricing = new PricingOptions(),
            Conversation = new ConversationOptions(),
            Skills = new SkillCatalogOptions(),
        }, CancellationToken.None);

        Assert.Equal("secret-key", saved.Providers["openai"].ApiKey);

        var persisted = File.ReadAllText(GetDefaultConfigFilePath());
        Assert.Contains("\"ApiKey\": \"secret-key\"", persisted, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_Loads_Provider_ApiKey_From_Configuration_Response_Model()
    {
        WriteHomeConfig("""
        {
          "CurrentDefaultProvider": "openai",
          "Providers": {
            "openai": {
              "Type": "openai",
              "Model": "gpt-4.1-mini",
              "ApiKey": "secret-key"
            }
          }
        }
        """);

        using var host = CreateHost();
        var options = host.Services.GetRequiredService<IOptions<AppOptions>>().Value;

        Assert.Equal("secret-key", options.Providers["openai"].ApiKey);
    }

    [Fact]
    public async Task SaveAsync_Initializes_Runtime_After_Provider_Is_Configured_Post_Startup()
    {
        using var host = CreateHost();
        var runtime = host.Services.GetRequiredService<IEventHorizonRuntime>();

        _ = Assert.Throws<InvalidOperationException>(() => _ = runtime.Agent);

        var controller = new ConfigurationController(
            host.Services.GetRequiredService<IAppConfigurationService>(),
            host.Services.GetRequiredService<IUserConfigurationFileService>(),
            host.Services.GetRequiredService<IEventHorizonRuntimeInitializer>(),
            host.Services.GetRequiredService<IConversationAgentManager>());

        await controller.SaveAsync(new SaveAppConfigurationRequestDTO
        {
            CurrentDefaultProvider = "openai",
            Providers =
            [
                new NamedProviderConfigurationDTO
                {
                    Name = "openai",
                    Provider = new ProviderOptions
                    {
                        Type = "openai-compatible",
                        Model = "gpt-4.1-mini",
                        ApiKey = "test-key",
                        Endpoint = "https://example.com/v1",
                        UseDefaultAzureCredential = false,
                    },
                },
            ],
            McpServers = [],
            Skills = new SkillCatalogOptions(),
        }, CancellationToken.None);

        Assert.NotNull(runtime.Agent);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private global::Microsoft.Extensions.Hosting.IHost CreateHost()
        => global::EventHorizon.Program.BuildHost([], new TestPathEnvironment(_workspaceDirectory, _homeDirectory));

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
