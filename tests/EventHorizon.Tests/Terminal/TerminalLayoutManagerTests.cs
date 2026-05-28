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

    [Fact]
    public void Build_Expanded_Creates_All_Workbench_Regions()
    {
        TerminalLayoutManager manager = new();

        var layout = manager.Build(new TerminalSize(160, 45));

        Assert.Equal(TerminalLayoutMode.Expanded, layout.Mode);
        Assert.True(layout[TerminalRegionId.Chat].Width > 0);
        Assert.True(layout[TerminalRegionId.Context].Width > 0);
        Assert.True(layout[TerminalRegionId.Tools].Width > 0);
        Assert.True(layout[TerminalRegionId.Plan].Width > 0);
        Assert.True(layout[TerminalRegionId.Diff].Width > 0);
        Assert.True(layout[TerminalRegionId.Input].Height > 0);
        Assert.True(layout[TerminalRegionId.StatusBar].Height > 0);
    }

    [Fact]
    public void Build_Compact_Hides_Secondary_Sidebars()
    {
        TerminalLayoutManager manager = new();

        var layout = manager.Build(new TerminalSize(80, 24));

        Assert.Equal(TerminalLayoutMode.Compact, layout.Mode);
        Assert.True(layout[TerminalRegionId.Chat].Height > 0);
        Assert.Equal(TerminalRect.Empty, layout[TerminalRegionId.Context]);
        Assert.Equal(TerminalRect.Empty, layout[TerminalRegionId.Tools]);
        Assert.Equal(TerminalRect.Empty, layout[TerminalRegionId.Plan]);
        Assert.Equal(TerminalRect.Empty, layout[TerminalRegionId.Diff]);
    }
}

