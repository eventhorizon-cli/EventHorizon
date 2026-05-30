using EventHorizon.Configuration;
using EventHorizon.EntryPoints;
using EventHorizon.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace EventHorizon.Tests.EntryPoints;

public sealed class SharedHostTests : IDisposable
{
    private readonly string _root;
    private readonly string _homeDirectory;
    private readonly string _workspaceDirectory;

    public SharedHostTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "eventhorizon-shared-host-tests", Guid.NewGuid().ToString("N"));
        _homeDirectory = Path.Combine(_root, "home");
        _workspaceDirectory = Path.Combine(_root, "workspace");
        Directory.CreateDirectory(_homeDirectory);
        Directory.CreateDirectory(_workspaceDirectory);
    }

    [Fact]
    public void Create_Registers_AGUI_And_Runtime_Services_In_Same_Host()
    {
        using var host = global::EventHorizon.Program.BuildHost([], new TestPathEnvironment(_workspaceDirectory, _homeDirectory));

        var application = host.Services.GetRequiredService<IEventHorizonApplication>();
        var runner = host.Services.GetRequiredService<IAGUIServerRunner>();
        var runtime = host.Services.GetRequiredService<IEventHorizonRuntime>();
        var sessionService = host.Services.GetRequiredService<EventHorizon.AGUI.IAGUISessionService>();
        var runService = host.Services.GetRequiredService<EventHorizon.AGUI.RunService>();

        Assert.NotNull(application);
        Assert.NotNull(runner);
        Assert.NotNull(runtime);
        Assert.NotNull(sessionService);
        Assert.NotNull(runService);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

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
