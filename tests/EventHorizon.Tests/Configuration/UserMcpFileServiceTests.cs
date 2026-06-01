using EventHorizon.Configuration;
using EventHorizon.Tests.Fixtures;

namespace EventHorizon.Tests.Configuration;

public sealed class UserMcpFileServiceTests : IDisposable
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
        var service = new UserMcpFileService(pathEnvironment);

        // Assert
        var expected = Path.Combine(_fixture.Root, ".eventhorizon", "mcp.json");
        Assert.Equal(expected, service.FilePath);
    }

    [Fact]
    public void EnsureExists_Creates_Directory_And_Empty_File()
    {
        // Arrange
        var pathEnvironment = new StubPathEnvironment(_fixture.Root);
        var service = new UserMcpFileService(pathEnvironment);

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
        var service = new UserMcpFileService(pathEnvironment);
        service.EnsureExists();
        var originalContent = File.ReadAllText(service.FilePath);

        // Act
        service.EnsureExists();

        // Assert
        var currentContent = File.ReadAllText(service.FilePath);
        Assert.Equal(originalContent, currentContent);
    }

    [Fact]
    public void Save_Persists_MCP_Servers()
    {
        // Arrange
        var pathEnvironment = new StubPathEnvironment(_fixture.Root);
        var service = new UserMcpFileService(pathEnvironment);
        var mcpOptions = new McpOptions
        {
            Servers =
            [
                new McpServerOptions
                {
                    Enabled = true,
                    Name = "test-server",
                    Url = "https://example.com/mcp",
                    Headers = []
                }
            ]
        };

        // Act
        service.Save(mcpOptions);

        // Assert
        Assert.True(File.Exists(service.FilePath));
        var content = File.ReadAllText(service.FilePath);
        Assert.Contains("McpServers", content);
        Assert.Contains("Servers", content);
        Assert.Contains("test-server", content);
    }

    [Fact]
    public void Save_Handles_Empty_Servers()
    {
        // Arrange
        var pathEnvironment = new StubPathEnvironment(_fixture.Root);
        var service = new UserMcpFileService(pathEnvironment);
        var mcpOptions = new McpOptions { Servers = [] };

        // Act
        service.Save(mcpOptions);

        // Assert
        Assert.True(File.Exists(service.FilePath));
        var content = File.ReadAllText(service.FilePath);
        Assert.Contains("McpServers", content);
    }

    [Fact]
    public void Save_Includes_All_Server_Properties()
    {
        // Arrange
        var pathEnvironment = new StubPathEnvironment(_fixture.Root);
        var service = new UserMcpFileService(pathEnvironment);
        var mcpOptions = new McpOptions
        {
            Servers =
            [
                new McpServerOptions
                {
                    Enabled = false,
                    Name = "claude-server",
                    Url = "https://claude.example.com/mcp",
                    Headers = new Dictionary<string, string>
                    {
                        ["Authorization"] = "Bearer secret",
                        ["X-Debug"] = "false"
                    }
                }
            ]
        };

        // Act
        service.Save(mcpOptions);

        // Assert
        var content = File.ReadAllText(service.FilePath);
        Assert.Contains("claude-server", content);
        Assert.Contains("https://claude.example.com/mcp", content);
        Assert.Contains("Authorization", content);
        Assert.Contains("Enabled", content);
        Assert.Contains("false", content);
    }

    [Fact]
    public void Save_Handles_Multiple_Servers()
    {
        // Arrange
        var pathEnvironment = new StubPathEnvironment(_fixture.Root);
        var service = new UserMcpFileService(pathEnvironment);
        var mcpOptions = new McpOptions
        {
            Servers =
            [
                new McpServerOptions
                {
                    Enabled = true,
                    Name = "server1",
                    Url = "https://example.com/server1",
                    Headers = []
                },
                new McpServerOptions
                {
                    Enabled = false,
                    Name = "server2",
                    Url = "https://example.com/server2",
                    Headers = []
                }
            ]
        };

        // Act
        service.Save(mcpOptions);

        // Assert
        var content = File.ReadAllText(service.FilePath);
        Assert.Contains("server1", content);
        Assert.Contains("server2", content);
    }

    [Fact]
    public void GetDefaultFilePath_Returns_Correct_Path()
    {
        // Arrange
        var pathEnvironment = new StubPathEnvironment(_fixture.Root);

        // Act
        var path = UserMcpFileService.GetDefaultFilePath(pathEnvironment);

        // Assert
        Assert.Contains(".eventhorizon", path);
        Assert.Contains("mcp.json", path);
        Assert.StartsWith(_fixture.Root, path);
    }

    [Fact]
    public void Save_Multiple_Times_Latest_Wins()
    {
        // Arrange
        var pathEnvironment = new StubPathEnvironment(_fixture.Root);
        var service = new UserMcpFileService(pathEnvironment);

        // Act
        service.Save(new McpOptions
        {
            Servers =
            [
                new McpServerOptions
                {
                    Enabled = true,
                    Name = "first",
                    Url = "https://example.com/first",
                    Headers = []
                }
            ]
        });

        service.Save(new McpOptions
        {
            Servers =
            [
                new McpServerOptions
                {
                    Enabled = false,
                    Name = "second",
                    Url = "https://example.com/second",
                    Headers = []
                }
            ]
        });

        // Assert
        var content = File.ReadAllText(service.FilePath);
        Assert.Contains("second", content);
        Assert.DoesNotContain("first", content);
    }

    [Fact]
    public void Save_Preserves_Server_Order()
    {
        // Arrange
        var pathEnvironment = new StubPathEnvironment(_fixture.Root);
        var service = new UserMcpFileService(pathEnvironment);
        var mcpOptions = new McpOptions
        {
            Servers =
            [
                new McpServerOptions
                {
                    Enabled = true,
                    Name = "alpha",
                    Url = "https://example.com/alpha",
                    Headers = []
                },
                new McpServerOptions
                {
                    Enabled = true,
                    Name = "beta",
                    Url = "https://example.com/beta",
                    Headers = []
                },
                new McpServerOptions
                {
                    Enabled = false,
                    Name = "gamma",
                    Url = "https://example.com/gamma",
                    Headers = []
                }
            ]
        };

        // Act
        service.Save(mcpOptions);

        // Assert
        var content = File.ReadAllText(service.FilePath);
        var alphaIndex = content.IndexOf("alpha");
        var betaIndex = content.IndexOf("beta");
        var gammaIndex = content.IndexOf("gamma");

        Assert.True(alphaIndex < betaIndex && betaIndex < gammaIndex);
    }
}
