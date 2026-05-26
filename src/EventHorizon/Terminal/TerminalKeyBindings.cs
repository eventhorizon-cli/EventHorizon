using Terminal.Gui.Input;

namespace EventHorizon.Terminal;

public static class TerminalKeyBindings
{
    public static Key Submit => Key.Enter;

    public static Key InsertNewLine => Key.J.WithCtrl;

    public static Key CancelOrExit => Key.C.WithCtrl;

    public static Key Exit => Key.D.WithCtrl;

    public static Key Clear => Key.L.WithCtrl;

    public static Key CommandPalette => Key.P.WithCtrl;


    public static Key Help => Key.H.WithCtrl;

    public static Key Tools => Key.T.WithCtrl;

    public static Key Files => Key.F.WithCtrl;

    public static Key Refresh => Key.R.WithCtrl;

    public static Key Escape => Key.Esc;
}

