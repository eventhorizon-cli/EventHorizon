
namespace EventHorizon.Tests.EntryPoints;

[Collection(ConsoleTestCollection.Name)]
public class ProgramEntryTests
{
    [Fact]
    public void EnsureNoArguments_Allows_Empty_Arguments()
    {
        Program.EnsureNoArguments([]);
    }

    [Fact]
    public void EnsureNoArguments_Rejects_Any_Arguments()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Program.EnsureNoArguments(["--config", "foo.json"]));

        Assert.Contains("Unsupported arguments", error.Message, StringComparison.Ordinal);
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
