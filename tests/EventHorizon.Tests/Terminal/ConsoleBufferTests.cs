using EventHorizon.Terminal.Console;

namespace EventHorizon.Tests.Terminal;

public sealed class ConsoleBufferTests
{
    [Fact]
    public void Write_Stores_Double_Width_Runes_Using_A_Continuation_Cell()
    {
        ConsoleBuffer buffer = new(6, 1);

        buffer.Write(0, 0, "测试", ConsoleStyle.Default);

        Assert.Equal("测", buffer.GetCell(0, 0).Text);
        Assert.True(buffer.GetCell(1, 0).IsContinuation);
        Assert.Equal("试", buffer.GetCell(2, 0).Text);
        Assert.True(buffer.GetCell(3, 0).IsContinuation);
    }

    [Fact]
    public void Write_Mixes_Ascii_And_Cjk_Without_Shifting_Following_Cells()
    {
        ConsoleBuffer buffer = new(6, 1);

        buffer.Write(0, 0, "A测B", ConsoleStyle.Default);

        Assert.Equal("A", buffer.GetCell(0, 0).Text);
        Assert.Equal("测", buffer.GetCell(1, 0).Text);
        Assert.True(buffer.GetCell(2, 0).IsContinuation);
        Assert.Equal("B", buffer.GetCell(3, 0).Text);
    }
}

