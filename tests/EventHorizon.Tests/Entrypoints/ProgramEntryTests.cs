
using EventHorizon.Configuration;

namespace EventHorizon.Tests.EntryPoints;

[Collection(ConsoleTestCollection.Name)]
public class ProgramEntryTests
{
    [Fact]
    public void ParseArguments_Parses_Config_And_Uses_Agui_Startup_Mode()
    {
        string[] args = ["--config", "samples/openai-compatible.eventhorizon.json"];

        var options = Program.ParseArguments(args);

        Assert.Equal(EffectiveCommandOptions.StartupMode, options.Command);
        Assert.Equal("samples/openai-compatible.eventhorizon.json", options.ConfigFile);
    }

    [Fact]
    public void ParseArguments_Defaults_To_Agui_When_No_Arguments_Are_Provided()
    {
        var options = Program.ParseArguments([]);

        Assert.Equal(EffectiveCommandOptions.StartupMode, options.Command);
    }

    [Fact]
    public void ParseArguments_Rejects_Unsupported_Arguments()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Program.ParseArguments(["--workspace", "/tmp/workspace"]));

        Assert.Contains("Unsupported argument", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ParseArguments_Rejects_Missing_Config_Value()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Program.ParseArguments(["--config"]));

        Assert.Contains("Missing value", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_Help_Returns_Zero_And_Writes_Help_Text()
    {
        StringWriter writer = new();
        var originalOut = Console.Out;
        Console.SetOut(writer);

        try
        {
            var exitCode = await Program.RunAsync(["--help"]);

            Assert.Equal(0, exitCode);
            Assert.Contains("Usage:", writer.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}


