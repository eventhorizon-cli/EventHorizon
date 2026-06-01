using EventHorizon.Configuration;
using Microsoft.Extensions.Options;

namespace EventHorizon.Tests.Configuration;

public sealed class AppConfigurationServiceTests
{
    private sealed class StubOptionsNormalizer : IOptionsNormalizer
    {
        public void NormalizeProviders(ProvidersOptions options) { }
        public void NormalizeMcp(McpOptions options) { }
        public void NormalizeSkills(SkillsOptions options) { }
        public ProviderOptions ResolveActiveProvider(ProvidersOptions options)
            => new ProviderOptions { Name = "default" };
    }

    private sealed class StubOptionsMonitor<T> : IOptionsMonitor<T> where T : class, new()
    {
        private readonly T _value;
        public StubOptionsMonitor(T? value = null) => _value = value ?? new T();
        public T CurrentValue => _value;
        public T Get(string? name) => _value;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    private sealed class StubUserConfigurationFileService : IUserConfigurationFileService
    {
        private readonly List<(AgentOptions, PricingOptions)> _saved = [];
        public string FilePath => "/test/appsettings.json";
        public IReadOnlyList<(AgentOptions, PricingOptions)> Saved => _saved;
        public void EnsureExists() { }
        public void Save(AgentOptions agentOptions, PricingOptions pricingOptions)
            => _saved.Add((agentOptions, pricingOptions));
        public string CreateInitialContent() => "{}";
    }

    private sealed class StubUserProvidersFileService : IUserProvidersFileService
    {
        private readonly List<ProvidersOptions> _saved = [];
        public string FilePath => "/test/providers.json";
        public IReadOnlyList<ProvidersOptions> Saved => _saved;
        public void EnsureExists() { }
        public void Save(ProvidersOptions options) => _saved.Add(options);
    }

    private sealed class StubUserMcpFileService : IUserMcpFileService
    {
        private readonly List<McpOptions> _saved = [];
        public string FilePath => "/test/mcp.json";
        public IReadOnlyList<McpOptions> Saved => _saved;
        public void EnsureExists() { }
        public void Save(McpOptions options) => _saved.Add(options);
    }

    private sealed class StubUserSkillsFileService : IUserSkillsFileService
    {
        private readonly List<SkillsOptions> _saved = [];
        public string FilePath => "/test/skills.json";
        public IReadOnlyList<SkillsOptions> Saved => _saved;
        public void EnsureExists() { }
        public void Save(SkillsOptions options) => _saved.Add(options);
    }

    [Fact]
    public void GetProvidersOptions_Returns_Current_Value()
    {
        // Arrange
        var providersOptions = new ProvidersOptions
        {
            CurrentDefaultProvider = "openai",
            Providers = new Dictionary<string, ProviderOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["openai"] = new ProviderOptions { Name = "openai" }
            }
        };

        var service = new AppConfigurationService(
            new StubOptionsMonitor<AgentOptions>(),
            new StubOptionsMonitor<PricingOptions>(),
            new StubOptionsMonitor<ProvidersOptions>(providersOptions),
            new StubOptionsMonitor<McpOptions>(),
            new StubOptionsMonitor<SkillsOptions>(),
            new StubOptionsNormalizer(),
            new StubUserConfigurationFileService(),
            new StubUserProvidersFileService(),
            new StubUserMcpFileService(),
            new StubUserSkillsFileService());

        // Act
        var result = service.GetProvidersOptions();

        // Assert
        Assert.Equal(providersOptions, result);
        Assert.Equal("openai", result.CurrentDefaultProvider);
    }

    [Fact]
    public void GetMcpOptions_Returns_Current_Value()
    {
        // Arrange
        var mcpOptions = new McpOptions
        {
            Servers = [new McpServerOptions
            {
                Enabled = false,
                Name = "test",
                Url = "https://example.com/mcp",
                Headers = []
            }]
        };

        var service = new AppConfigurationService(
            new StubOptionsMonitor<AgentOptions>(),
            new StubOptionsMonitor<PricingOptions>(),
            new StubOptionsMonitor<ProvidersOptions>(),
            new StubOptionsMonitor<McpOptions>(mcpOptions),
            new StubOptionsMonitor<SkillsOptions>(),
            new StubOptionsNormalizer(),
            new StubUserConfigurationFileService(),
            new StubUserProvidersFileService(),
            new StubUserMcpFileService(),
            new StubUserSkillsFileService());

        // Act
        var result = service.GetMcpOptions();

        // Assert
        Assert.Equal(mcpOptions, result);
        Assert.Single(result.Servers);
        Assert.False(result.Servers[0].Enabled);
        Assert.Equal("test", result.Servers[0].Name);
    }

    [Fact]
    public void GetSkillsOptions_Returns_Current_Value()
    {
        // Arrange
        var skillsOptions = new SkillsOptions
        {
            StoragePath = "/home/user/.eventhorizon/skills",
            Imported = [new ImportedSkillOptions
            {
                Enabled = false,
                Name = "math",
                Path = "/home/user/.eventhorizon/skills/math",
                Description = "Math functions",
                ImportedAt = DateTimeOffset.UtcNow
            }]
        };

        var service = new AppConfigurationService(
            new StubOptionsMonitor<AgentOptions>(),
            new StubOptionsMonitor<PricingOptions>(),
            new StubOptionsMonitor<ProvidersOptions>(),
            new StubOptionsMonitor<McpOptions>(),
            new StubOptionsMonitor<SkillsOptions>(skillsOptions),
            new StubOptionsNormalizer(),
            new StubUserConfigurationFileService(),
            new StubUserProvidersFileService(),
            new StubUserMcpFileService(),
            new StubUserSkillsFileService());

        // Act
        var result = service.GetSkillsOptions();

        // Assert
        Assert.Equal(skillsOptions, result);
        Assert.Single(result.Imported);
        Assert.False(result.Imported[0].Enabled);
        Assert.Equal("math", result.Imported[0].Name);
    }

    [Fact]
    public void Save_Persists_All_Options()
    {
        // Arrange
        var agentOptions = new AgentOptions { Name = "TestAgent" };
        var pricingOptions = new PricingOptions { RefreshOnStartup = true };
        var providersOptions = new ProvidersOptions
        {
            CurrentDefaultProvider = "openai",
            Providers = new Dictionary<string, ProviderOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["openai"] = new ProviderOptions { Name = "openai" }
            }
        };

        var userConfigService = new StubUserConfigurationFileService();
        var userProvidersService = new StubUserProvidersFileService();
        var userMcpService = new StubUserMcpFileService();
        var userSkillsService = new StubUserSkillsFileService();

        var service = new AppConfigurationService(
            new StubOptionsMonitor<AgentOptions>(agentOptions),
            new StubOptionsMonitor<PricingOptions>(pricingOptions),
            new StubOptionsMonitor<ProvidersOptions>(providersOptions),
            new StubOptionsMonitor<McpOptions>(),
            new StubOptionsMonitor<SkillsOptions>(),
            new StubOptionsNormalizer(),
            userConfigService,
            userProvidersService,
            userMcpService,
            userSkillsService);

        var newProviders = new ProvidersOptions
        {
            CurrentDefaultProvider = "anthropic",
            Providers = new Dictionary<string, ProviderOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["anthropic"] = new ProviderOptions { Name = "anthropic" }
            }
        };

        var newMcp = new McpOptions
        {
            Servers =
            [
                new McpServerOptions
                {
                    Enabled = false,
                    Name = "disabled-server",
                    Url = "https://example.com/mcp",
                    Headers = []
                }
            ]
        };

        var newSkills = new SkillsOptions
        {
            Imported =
            [
                new ImportedSkillOptions
                {
                    Enabled = false,
                    Name = "disabled-skill",
                    Path = "/tmp/disabled-skill",
                    Description = "Disabled skill",
                    ImportedAt = DateTimeOffset.UtcNow
                }
            ]
        };

        // Act
        service.Save(newProviders, newMcp, newSkills, CancellationToken.None);

        // Assert
        Assert.Single(userConfigService.Saved);
        Assert.Single(userProvidersService.Saved);
        Assert.Single(userMcpService.Saved);
        Assert.Single(userSkillsService.Saved);
        Assert.False(userMcpService.Saved[0].Servers[0].Enabled);
        Assert.False(userSkillsService.Saved[0].Imported[0].Enabled);
    }

    [Fact]
    public void SetDefaultProvider_Persists_Selection()
    {
        // Arrange
        var providersOptions = new ProvidersOptions
        {
            CurrentDefaultProvider = null,
            Providers = new Dictionary<string, ProviderOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["anthropic"] = new ProviderOptions { Name = "anthropic" }
            }
        };

        var userProvidersService = new StubUserProvidersFileService();

        var service = new AppConfigurationService(
            new StubOptionsMonitor<AgentOptions>(),
            new StubOptionsMonitor<PricingOptions>(),
            new StubOptionsMonitor<ProvidersOptions>(providersOptions),
            new StubOptionsMonitor<McpOptions>(),
            new StubOptionsMonitor<SkillsOptions>(),
            new StubOptionsNormalizer(),
            new StubUserConfigurationFileService(),
            userProvidersService,
            new StubUserMcpFileService(),
            new StubUserSkillsFileService());

        // Act
        service.SetDefaultProvider("anthropic", CancellationToken.None);

        // Assert
        Assert.Equal("anthropic", providersOptions.CurrentDefaultProvider);
        Assert.Single(userProvidersService.Saved);
    }

    [Fact]
    public void SetDefaultProvider_Clears_When_Null()
    {
        // Arrange
        var providersOptions = new ProvidersOptions
        {
            CurrentDefaultProvider = "openai",
            Providers = new Dictionary<string, ProviderOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["openai"] = new ProviderOptions { Name = "openai" }
            }
        };

        var service = new AppConfigurationService(
            new StubOptionsMonitor<AgentOptions>(),
            new StubOptionsMonitor<PricingOptions>(),
            new StubOptionsMonitor<ProvidersOptions>(providersOptions),
            new StubOptionsMonitor<McpOptions>(),
            new StubOptionsMonitor<SkillsOptions>(),
            new StubOptionsNormalizer(),
            new StubUserConfigurationFileService(),
            new StubUserProvidersFileService(),
            new StubUserMcpFileService(),
            new StubUserSkillsFileService());

        // Act
        service.SetDefaultProvider(null, CancellationToken.None);

        // Assert
        Assert.Null(providersOptions.CurrentDefaultProvider);
    }

    [Fact]
    public void Save_Throws_When_Cancelled()
    {
        // Arrange
        var service = new AppConfigurationService(
            new StubOptionsMonitor<AgentOptions>(),
            new StubOptionsMonitor<PricingOptions>(),
            new StubOptionsMonitor<ProvidersOptions>(),
            new StubOptionsMonitor<McpOptions>(),
            new StubOptionsMonitor<SkillsOptions>(),
            new StubOptionsNormalizer(),
            new StubUserConfigurationFileService(),
            new StubUserProvidersFileService(),
            new StubUserMcpFileService(),
            new StubUserSkillsFileService());

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        Assert.Throws<OperationCanceledException>(() =>
            service.Save(new ProvidersOptions(), new McpOptions(), new SkillsOptions(), cts.Token));
    }
}
