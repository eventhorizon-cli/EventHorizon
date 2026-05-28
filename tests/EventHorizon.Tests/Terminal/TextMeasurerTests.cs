using EventHorizon.Terminal.Rendering;

namespace EventHorizon.Tests.Terminal;

public sealed class TextMeasurerTests
{
    [Fact]
    public void GetDisplayWidth_Ignores_Ansi_Escape_Sequences()
    {
        TextMeasurer sut = new();

        var width = sut.GetDisplayWidth("\u001b[31mHello\u001b[0m");

        Assert.Equal(5, width);
    }

    [Fact]
    public void GetDisplayWidth_Treats_Cjk_As_Double_Width()
    {
        TextMeasurer sut = new();

        var width = sut.GetDisplayWidth("你好");

        Assert.Equal(4, width);
    }

    [Fact]
    public void Truncate_Appends_Ellipsis_When_Text_Exceeds_Max_Width()
    {
        TextMeasurer sut = new();

        var value = sut.Truncate("abcdef", 5);

        Assert.Equal("abcd…", value);
    }

    [Fact]
    public void Wrap_Splits_Text_Into_Multiple_Lines()
    {
        TextMeasurer sut = new();

        var lines = sut.Wrap("abcdef", 3);

        Assert.Equal(["abc", "def"], lines);
    }
}

