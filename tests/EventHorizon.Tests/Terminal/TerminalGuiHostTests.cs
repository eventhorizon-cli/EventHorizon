using EventHorizon.Terminal;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;

namespace EventHorizon.Tests.Terminal;

public sealed class TerminalGuiHostTests
{
    [Fact]
    public void GetPreferredDriverName_Uses_Ansi_On_MacOS()
    {
        var driverName = TerminalGuiHost.GetPreferredDriverName();

        if (OperatingSystem.IsMacOS())
        {
            Assert.Equal(DriverRegistry.Names.ANSI, driverName);
            return;
        }

        Assert.Null(driverName);
    }

    [Fact]
    public void GetPreferredAppModel_Uses_Inline_On_MacOS()
    {
        var appModel = TerminalGuiHost.GetPreferredAppModel();

        if (OperatingSystem.IsMacOS())
        {
            Assert.Equal(AppModel.Inline, appModel);
            return;
        }

        Assert.Null(appModel);
    }
}

