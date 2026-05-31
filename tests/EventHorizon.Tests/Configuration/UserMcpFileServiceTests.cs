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
                    Name = "test-server",
                    Command = "node",
                    Arguments = ["server.js"],
                    Enabled = true,
                    Url = null,
                    EnvironmentVariables = []
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
                    Name = "claude-server",
                    Command = "python",
                    Arguments = ["-m", "mcp.server.claude"],
                    Enabled = true,
                    Url = null,
                    EnvironmentVariables = new Dictionary<string, string>
                    {
                        ["API_KEY"] = "secret",
                        ["DEBUG"] = "false"
                    }
                }
            ]
        };

        // Act
        service.Save(mcpOptions);

        // Assert
        var content = File.ReadAllText(service.FilePath);
        Assert.Contains("claude-server", content);
        Assert.Contains("python", content);
        Assert.Contains("mcp.server.claude", content);
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
                    Name = "server1",
                    Command = "node",
                    Arguments = [],
                    Enabled = true,
                    EnvironmentVariables = []
                },
                new McpServerOptions
                {
                    Name = "server2",
                    Command = "python",
                    Arguments = [],
                    Enabled = false,
                    EnvironmentVariables = []
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
                    Name = "first",
                    Command = "node",
                    Arguments = [],
                    Enabled = true,
                    EnvironmentVariables = []
                }
            ]
        });

        service.Save(new McpOptions
        {
            Servers =
            [
                new McpServerOptions
                {
                    Name = "second",
                    Command = "python",
                    Arguments = [],
                    Enabled = true,
                    EnvironmentVariables = []
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
                    Name = "alpha",
                    Command = "node",
                    Arguments = [],
                    Enabled = true,
                    EnvironmentVariables = []
                },
                new McpServerOptions
                {
                    Name = "beta",
                    Command = "python",
                    Arguments = [],
                    Enabled = true,
                    EnvironmentVariables = []
                },
                new McpServerOptions
                {
                    Name = "gamma",
                    Command = "go",
                    Arguments = [],
                    Enabled = false,
                    EnvironmentVariables = []
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
