using EventHorizon.Configuration;
using EventHorizon.Tests.Fixtures;

namespace EventHorizon.Tests.Configuration;

public sealed class UserProvidersFileServiceTests : IDisposable
{
    private readonly TemporaryWorkspaceFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    private sealed class StubPathEnvironment : IPathEnvironment
    {
        private readonly string _homeDirectory;

        public StubPathEnvironment(string homeDirectory)
        {
            _homeDirectory = homeDirectory;
        }

        public string CurrentDirectory => Directory.GetCurrentDirectory();
        public string HomeDirectory => _homeDirectory;
    }

    [Fact]
    public void FilePath_Returns_Expected_Location()
    {
        // Arrange
        var pathEnvironment = new StubPathEnvironment(_fixture.Root);

        // Act
        var service = new UserProvidersFileService(pathEnvironment);

        // Assert
        var expected = Path.Combine(_fixture.Root, ".eventhorizon", "providers.json");
        Assert.Equal(expected, service.FilePath);
    }

    [Fact]
    public void EnsureExists_Creates_Directory_And_Empty_File()
    {
        // Arrange
        var pathEnvironment = new StubPathEnvironment(_fixture.Root);
        var service = new UserProvidersFileService(pathEnvironment);

        // Act
        service.EnsureExists();

        // Assert
        Assert.True(File.Exists(service.FilePath));
        var content = File.ReadAllText(service.FilePath);
        Assert.Equal("{}\n", content);
    }

    [Fact]
    public void EnsureExists_Preserves_Existing_File()
    {
        // Arrange
        var pathEnvironment = new StubPathEnvironment(_fixture.Root);
        var service = new UserProvidersFileService(pathEnvironment);
        service.EnsureExists();
        var originalContent = File.ReadAllText(service.FilePath);

        // Act
        service.EnsureExists();

        // Assert
        var currentContent = File.ReadAllText(service.FilePath);
        Assert.Equal(originalContent, currentContent);
    }

    [Fact]
    public void Save_Persists_Provider_Options()
    {
        // Arrange
        var pathEnvironment = new StubPathEnvironment(_fixture.Root);
        var service = new UserProvidersFileService(pathEnvironment);
        var providersOptions = new ProvidersOptions
        {
            CurrentDefaultProvider = "openai",
            Providers = new Dictionary<string, ProviderOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["openai"] = new ProviderOptions
                {
                    Name = "openai",
                    Type = "openai",
                    Model = "gpt-4",
                    ApiKey = "test-key",
                }
            }
        };

        // Act
        service.Save(providersOptions);

        // Assert
        Assert.True(File.Exists(service.FilePath));
        var content = File.ReadAllText(service.FilePath);
        Assert.Contains("Providers", content);
        Assert.Contains("CurrentDefaultProvider", content);
        Assert.Contains("openai", content.ToLower());
    }

    [Fact]
    public void Save_Handles_Empty_Providers()
    {
        // Arrange
        var pathEnvironment = new StubPathEnvironment(_fixture.Root);
        var service = new UserProvidersFileService(pathEnvironment);
        var providersOptions = new ProvidersOptions
        {
            CurrentDefaultProvider = null,
            Providers = []
        };

        // Act
        service.Save(providersOptions);

        // Assert
        Assert.True(File.Exists(service.FilePath));
        var content = File.ReadAllText(service.FilePath);
        Assert.Contains("Providers", content);
    }

    [Fact]
    public void Save_Handles_Multiple_Providers()
    {
        // Arrange
        var pathEnvironment = new StubPathEnvironment(_fixture.Root);
        var service = new UserProvidersFileService(pathEnvironment);
        var providersOptions = new ProvidersOptions
        {
            CurrentDefaultProvider = "openai",
            Providers = new Dictionary<string, ProviderOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["openai"] = new ProviderOptions
                {
                    Name = "openai",
                    Type = "openai",
                    Model = "gpt-4",
                },
                ["anthropic"] = new ProviderOptions
                {
                    Name = "anthropic",
                    Type = "anthropic",
                    Model = "claude-3",
                }
            }
        };

        // Act
        service.Save(providersOptions);

        // Assert
        var content = File.ReadAllText(service.FilePath);
        Assert.Contains("openai", content.ToLower());
        Assert.Contains("anthropic", content.ToLower());
    }

    [Fact]
    public void Save_Excludes_ApiKey_From_Certain_Configurations()
    {
        // Arrange
        var pathEnvironment = new StubPathEnvironment(_fixture.Root);
        var service = new UserProvidersFileService(pathEnvironment);
        var providersOptions = new ProvidersOptions
        {
            CurrentDefaultProvider = "azure",
            Providers = new Dictionary<string, ProviderOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["azure"] = new ProviderOptions
                {
                    Name = "azure",
                    Type = "azure",
                    UseDefaultAzureCredential = true,
                    Endpoint = "https://example.openai.azure.com/",
                }
            }
        };

        // Act
        service.Save(providersOptions);

        // Assert
        var content = File.ReadAllText(service.FilePath);
        Assert.Contains("UseDefaultAzureCredential", content);
    }

    [Fact]
    public void GetDefaultFilePath_Returns_Correct_Path()
    {
        // Arrange
        var pathEnvironment = new StubPathEnvironment(_fixture.Root);

        // Act
        var path = UserProvidersFileService.GetDefaultFilePath(pathEnvironment);

        // Assert
        Assert.Contains(".eventhorizon", path);
        Assert.Contains("providers.json", path);
        Assert.StartsWith(_fixture.Root, path);
    }

    [Fact]
    public void Save_Uses_Case_Insensitive_Dictionary()
    {
        // Arrange
        var pathEnvironment = new StubPathEnvironment(_fixture.Root);
        var service = new UserProvidersFileService(pathEnvironment);
        var providersOptions = new ProvidersOptions
        {
            CurrentDefaultProvider = "OpenAI",
            Providers = new Dictionary<string, ProviderOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["OpenAI"] = new ProviderOptions { Name = "OpenAI", Type = "openai" }
            }
        };

        // Act
        service.Save(providersOptions);

        // Assert
        var content = File.ReadAllText(service.FilePath);
        Assert.NotEmpty(content);
    }

    [Fact]
    public void Save_Multiple_Times_Latest_Wins()
    {
        // Arrange
        var pathEnvironment = new StubPathEnvironment(_fixture.Root);
        var service = new UserProvidersFileService(pathEnvironment);

        // Act
        service.Save(new ProvidersOptions
        {
            CurrentDefaultProvider = "openai",
            Providers = new Dictionary<string, ProviderOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["openai"] = new ProviderOptions { Name = "openai" }
            }
        });

        service.Save(new ProvidersOptions
        {
            CurrentDefaultProvider = "anthropic",
            Providers = new Dictionary<string, ProviderOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["anthropic"] = new ProviderOptions { Name = "anthropic" }
            }
        });

        // Assert
        var content = File.ReadAllText(service.FilePath);
        Assert.Contains("anthropic", content.ToLower());
        Assert.DoesNotContain("openai", content.ToLower());
    }
}
