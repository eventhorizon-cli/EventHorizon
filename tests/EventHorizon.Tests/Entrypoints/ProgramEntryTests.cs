using EventHorizon.Configuration;

namespace EventHorizon.Tests.EntryPoints;

[Collection(ConsoleTestCollection.Name)]
public class ProgramEntryTests
{
    [Fact]
    public void BuildHost_Accepts_CommandLine_Arguments()
    {
        using var host = global::EventHorizon.Program.BuildHost(
            ["--urls", "http://127.0.0.1:0"],
            new TestPathEnvironment(Path.GetTempPath(), Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)));

        Assert.NotNull(host.Services);
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
