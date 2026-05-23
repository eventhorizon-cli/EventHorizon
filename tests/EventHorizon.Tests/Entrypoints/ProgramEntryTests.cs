using EventHorizon.Configuration;

namespace EventHorizon.Tests.EntryPoints;

[Collection(ConsoleTestCollection.Name)]
public class ProgramEntryTests
{
    [Fact]
    public void ParseArguments_Parses_Command_Options_And_Prompt()
    {
        string[] args =
        [
            "run",
            "--provider", "openai-compatible",
            "--model", "gpt-4.1-mini",
            "--workspace", "/tmp/work",
            "Fix", "the", "failing", "tests"
        ];

        EffectiveCommandOptions options = Program.ParseArguments(args);

        Assert.Equal("run", options.Command);
        Assert.Equal("openai-compatible", options.ProviderType);
        Assert.Equal("gpt-4.1-mini", options.Model);
        Assert.Equal("/tmp/work", options.WorkspaceRoot);
        Assert.Equal("Fix the failing tests", options.Prompt);
    }

    [Fact]
    public void ParseArguments_Defaults_To_Chat_When_No_Command_Is_Provided()
    {
        string[] args = ["--workspace", "/tmp/workspace"];

        EffectiveCommandOptions options = Program.ParseArguments(args);

        Assert.Equal("chat", options.Command);
        Assert.Equal("/tmp/workspace", options.WorkspaceRoot);
    }

    [Fact]
    public async Task RunAsync_Help_Returns_Zero_And_Writes_Help_Text()
    {
        StringWriter writer = new();
        var originalOut = Console.Out;
        Console.SetOut(writer);

        try
        {
            int exitCode = await Program.RunAsync(["--help"]);

            Assert.Equal(0, exitCode);
            Assert.Contains("Commands:", writer.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}


