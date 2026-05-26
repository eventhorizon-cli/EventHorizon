using EventHorizon.Terminal.Layout;

namespace EventHorizon.Tests.Terminal;

public sealed class TerminalLayoutManagerTests
{
    [Theory]
    [InlineData(160, 45, TerminalLayoutMode.Expanded)]
    [InlineData(120, 32, TerminalLayoutMode.Standard)]
    [InlineData(80, 24, TerminalLayoutMode.Compact)]
    public void ResolveMode_Uses_Expected_Breakpoints(int width, int height, TerminalLayoutMode expected)
    {
        TerminalLayoutManager manager = new();

        var actual = manager.ResolveMode(new TerminalSize(width, height));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ResolveMode_Uses_Forced_Mode_When_Provided()
    {
        TerminalLayoutManager manager = new();

        var actual = manager.ResolveMode(new TerminalSize(10, 10), TerminalLayoutMode.Expanded);

        Assert.Equal(TerminalLayoutMode.Expanded, actual);
    }
}

