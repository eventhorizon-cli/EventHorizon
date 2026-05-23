using EventHorizon.Commands;

namespace EventHorizon.Tests.Commands;

[Collection(ConsoleTestCollection.Name)]
public sealed class SlashCommandCatalogTests
{
    [Fact]
    public void GetDefinitions_Returns_Expected_Commands_In_Name_Order()
    {
        var catalog = new SlashCommandCatalog();
        var commands = catalog.GetDefinitions();

        Assert.Equal(commands.OrderBy(static command => command.Name, StringComparer.Ordinal).Select(static command => command.Name), commands.Select(static command => command.Name));
        Assert.Contains(commands, static command => command.Name == "/help" && command.Description.Contains("show commands", StringComparison.Ordinal));
        Assert.Contains(commands, static command => command.Name == "/reset" && command.Description.Contains("fresh agent session", StringComparison.Ordinal));
        Assert.Contains(commands, static command => command.Name == "/exit" && command.Description.Contains("leave the console", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TryExecuteAsync_Help_Writes_All_Registered_Commands()
    {
        var catalog = new SlashCommandCatalog();
        StringWriter writer = new();
        TextWriter original = Console.Out;
        Console.SetOut(writer);

        try
        {
            SlashCommandResult result = await catalog.TryExecuteAsync("/help", null!, null!, CancellationToken.None);

            Assert.True(result.Handled);
            Assert.False(result.ExitRequested);

            string output = writer.ToString();
            foreach (SlashCommandDefinition definition in catalog.GetDefinitions())
            {
                Assert.Contains(definition.Name, output, StringComparison.Ordinal);
                Assert.Contains(definition.Description, output, StringComparison.Ordinal);
            }
        }
        finally
        {
            Console.SetOut(original);
        }
    }

    [Fact]
    public async Task TryExecuteAsync_NonSlash_Input_Is_Not_Handled()
    {
        var catalog = new SlashCommandCatalog();
        SlashCommandResult result = await catalog.TryExecuteAsync("hello world", null!, null!, CancellationToken.None);

        Assert.False(result.Handled);
        Assert.False(result.ExitRequested);
    }
}

