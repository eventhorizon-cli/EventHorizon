using EventHorizon.Configuration;
using Microsoft.Extensions.Options;

namespace EventHorizon.Tests.Configuration;

public sealed class ProviderConfigurationServiceTests
{
    private sealed class StubOptionsNormalizer : IOptionsNormalizer
    {
        private readonly ProviderOptions? _activeProvider;

        public StubOptionsNormalizer(ProviderOptions? activeProvider = null)
        {
            _activeProvider = activeProvider;
        }

        public void NormalizeProviders(ProvidersOptions options)
        {
        }

        public void NormalizeMcp(McpOptions options)
        {
        }

        public void NormalizeSkills(SkillsOptions options)
        {
        }

        public ProviderOptions ResolveActiveProvider(ProvidersOptions options)
        {
            return _activeProvider ?? new ProviderOptions { Name = "default", Type = "openai" };
        }
    }

    private sealed class StubOptionsMonitor : IOptionsMonitor<ProvidersOptions>
    {
        private readonly ProvidersOptions _options;

        public StubOptionsMonitor(ProvidersOptions options)
        {
            _options = options;
        }

        public ProvidersOptions CurrentValue => _options;

        public ProvidersOptions Get(string? name) => _options;

        public IDisposable? OnChange(Action<ProvidersOptions, string?> listener) => null;
    }

    private sealed class StubUserProvidersFileService : IUserProvidersFileService
    {
        private readonly List<ProvidersOptions> _saved = [];

        public string FilePath => "/test/providers.json";

        public IReadOnlyList<ProvidersOptions> Saved => _saved;

        public void EnsureExists()
        {
        }

        public void Save(ProvidersOptions options)
        {
            _saved.Add(options);
        }
    }

    [Fact]
    public void GetConfiguredProviders_Returns_Ordered_List()
    {
        // Arrange
        var optionsMonitor = new StubOptionsMonitor(new ProvidersOptions
        {
            Providers = new Dictionary<string, ProviderOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["Zebra"] = new ProviderOptions { Name = "Zebra", Type = "openai" },
                ["Apple"] = new ProviderOptions { Name = "Apple", Type = "anthropic" },
                ["Mango"] = new ProviderOptions { Name = "Mango", Type = "openai" }
            }
        });

        var service = new ProviderConfigurationService(
            optionsMonitor,
            new StubOptionsNormalizer(),
            new StubUserProvidersFileService());

        // Act
        var providers = service.GetConfiguredProviders();

        // Assert
        Assert.Equal(3, providers.Count);
        Assert.Equal("Apple", providers[0].Name);
        Assert.Equal("Mango", providers[1].Name);
        Assert.Equal("Zebra", providers[2].Name);
    }

    [Fact]
    public void GetConfiguredProviders_Uses_Default_Type_OpenAI()
    {
        // Arrange
        var optionsMonitor = new StubOptionsMonitor(new ProvidersOptions
        {
            Providers = new Dictionary<string, ProviderOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["test"] = new ProviderOptions { Name = "test", Type = null }
            }
        });

        var service = new ProviderConfigurationService(
            optionsMonitor,
            new StubOptionsNormalizer(),
            new StubUserProvidersFileService());

        // Act
        var providers = service.GetConfiguredProviders();

        // Assert
        Assert.Single(providers);
        Assert.Equal("openai", providers[0].Type);
    }

    [Fact]
    public void GetConfiguredProviders_Uses_Deployment_Over_Model()
    {
        // Arrange
        var optionsMonitor = new StubOptionsMonitor(new ProvidersOptions
        {
            Providers = new Dictionary<string, ProviderOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["azure"] = new ProviderOptions
                {
                    Name = "azure",
                    Type = "azure",
                    Model = "gpt-4",
                    Deployment = "my-deployment"
                }
            }
        });

        var service = new ProviderConfigurationService(
            optionsMonitor,
            new StubOptionsNormalizer(),
            new StubUserProvidersFileService());

        // Act
        var providers = service.GetConfiguredProviders();

        // Assert
        Assert.Single(providers);
        Assert.Equal("my-deployment", providers[0].Model);
    }

    [Fact]
    public void GetEffectiveProviderName_Returns_Set_Default()
    {
        // Arrange
        var optionsMonitor = new StubOptionsMonitor(new ProvidersOptions
        {
            CurrentDefaultProvider = "openai",
            Providers = new Dictionary<string, ProviderOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["openai"] = new ProviderOptions { Name = "openai" }
            }
        });

        var service = new ProviderConfigurationService(
            optionsMonitor,
            new StubOptionsNormalizer(),
            new StubUserProvidersFileService());

        // Act
        var name = service.GetEffectiveProviderName();

        // Assert
        Assert.Equal("openai", name);
    }

    [Fact]
    public void GetEffectiveProviderName_Returns_Only_Provider_When_Single()
    {
        // Arrange
        var optionsMonitor = new StubOptionsMonitor(new ProvidersOptions
        {
            CurrentDefaultProvider = null,
            Providers = new Dictionary<string, ProviderOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["only"] = new ProviderOptions { Name = "only" }
            }
        });

        var service = new ProviderConfigurationService(
            optionsMonitor,
            new StubOptionsNormalizer(),
            new StubUserProvidersFileService());

        // Act
        var name = service.GetEffectiveProviderName();

        // Assert
        Assert.Equal("only", name);
    }

    [Fact]
    public void GetEffectiveProviderName_Returns_Null_For_Multiple_Without_Default()
    {
        // Arrange
        var optionsMonitor = new StubOptionsMonitor(new ProvidersOptions
        {
            CurrentDefaultProvider = null,
            Providers = new Dictionary<string, ProviderOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["first"] = new ProviderOptions { Name = "first" },
                ["second"] = new ProviderOptions { Name = "second" }
            }
        });

        var service = new ProviderConfigurationService(
            optionsMonitor,
            new StubOptionsNormalizer(),
            new StubUserProvidersFileService());

        // Act
        var name = service.GetEffectiveProviderName();

        // Assert
        Assert.Null(name);
    }

    [Fact]
    public void SetCurrentProvider_Updates_Provider_Without_Persist()
    {
        // Arrange
        var options = new ProvidersOptions
        {
            CurrentDefaultProvider = null,
            Providers = new Dictionary<string, ProviderOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["openai"] = new ProviderOptions { Name = "openai" }
            }
        };
        var optionsMonitor = new StubOptionsMonitor(options);
        var fileService = new StubUserProvidersFileService();

        var service = new ProviderConfigurationService(
            optionsMonitor,
            new StubOptionsNormalizer(),
            fileService);

        // Act
        service.SetCurrentProvider("openai", persist: false);

        // Assert
        Assert.Equal("openai", options.CurrentDefaultProvider);
        Assert.Empty(fileService.Saved);
    }

    [Fact]
    public void SetCurrentProvider_Persists_When_Requested()
    {
        // Arrange
        var options = new ProvidersOptions
        {
            CurrentDefaultProvider = null,
            Providers = new Dictionary<string, ProviderOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["anthropic"] = new ProviderOptions { Name = "anthropic" }
            }
        };
        var optionsMonitor = new StubOptionsMonitor(options);
        var fileService = new StubUserProvidersFileService();

        var service = new ProviderConfigurationService(
            optionsMonitor,
            new StubOptionsNormalizer(),
            fileService);

        // Act
        service.SetCurrentProvider("anthropic", persist: true);

        // Assert
        Assert.Equal("anthropic", options.CurrentDefaultProvider);
        Assert.Single(fileService.Saved);
    }

    [Fact]
    public void SetCurrentProvider_Throws_For_Unknown_Provider()
    {
        // Arrange
        var optionsMonitor = new StubOptionsMonitor(new ProvidersOptions
        {
            Providers = new Dictionary<string, ProviderOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["openai"] = new ProviderOptions { Name = "openai" }
            }
        });

        var service = new ProviderConfigurationService(
            optionsMonitor,
            new StubOptionsNormalizer(),
            new StubUserProvidersFileService());

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            service.SetCurrentProvider("unknown", persist: false));

        Assert.Contains("Unknown provider", ex.Message);
        Assert.Contains("openai", ex.Message);
    }

    [Fact]
    public void EnsureCurrentProvider_Does_Nothing_When_Already_Set()
    {
        // Arrange
        var optionsMonitor = new StubOptionsMonitor(new ProvidersOptions
        {
            CurrentDefaultProvider = "openai",
            Providers = new Dictionary<string, ProviderOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["openai"] = new ProviderOptions { Name = "openai" }
            }
        });

        var fileService = new StubUserProvidersFileService();
        var service = new ProviderConfigurationService(
            optionsMonitor,
            new StubOptionsNormalizer(),
            fileService);

        // Act
        service.EnsureCurrentProvider(CancellationToken.None);

        // Assert
        Assert.Empty(fileService.Saved);
    }

    [Fact]
    public void EnsureCurrentProvider_Sets_Single_Provider()
    {
        // Arrange
        var options = new ProvidersOptions
        {
            CurrentDefaultProvider = null,
            Providers = new Dictionary<string, ProviderOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["anthropic"] = new ProviderOptions { Name = "anthropic", Type = "anthropic" }
            }
        };
        var optionsMonitor = new StubOptionsMonitor(options);
        var fileService = new StubUserProvidersFileService();

        var service = new ProviderConfigurationService(
            optionsMonitor,
            new StubOptionsNormalizer(),
            fileService);

        // Act
        service.EnsureCurrentProvider(CancellationToken.None);

        // Assert
        Assert.Equal("anthropic", options.CurrentDefaultProvider);
        Assert.Single(fileService.Saved);
    }

    [Fact]
    public void GetActiveProvider_Delegates_To_Normalizer()
    {
        // Arrange
        var options = new ProvidersOptions
        {
            Providers = new Dictionary<string, ProviderOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["test"] = new ProviderOptions { Name = "test" }
            }
        };
        var optionsMonitor = new StubOptionsMonitor(options);

        var activeProvider = new ProviderOptions { Name = "active" };
        var normalizer = new StubOptionsNormalizer(activeProvider);

        var service = new ProviderConfigurationService(
            optionsMonitor,
            normalizer,
            new StubUserProvidersFileService());

        // Act
        var result = service.GetActiveProvider();

        // Assert
        Assert.Equal("active", result.Name);
    }
}
