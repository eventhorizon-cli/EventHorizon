using EventHorizon.Terminal;

namespace EventHorizon.Tests.Terminal;

public sealed class TerminalInputControllerTests : IDisposable
{
    private readonly string _workspaceRoot;

    public TerminalInputControllerTests()
    {
        _workspaceRoot = Path.Combine(Path.GetTempPath(), "eventhorizon-input-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspaceRoot);
        Directory.CreateDirectory(Path.Combine(_workspaceRoot, "src", "Nested"));
        File.WriteAllText(Path.Combine(_workspaceRoot, "src", "Program.cs"), "class Program {}\n");
        File.WriteAllText(Path.Combine(_workspaceRoot, "src", "Nested", "Feature.cs"), "class Feature {}\n");
    }

    [Fact]
    public void Up_Down_Navigate_Through_Input_History_And_Restore_Draft()
    {
        TerminalConversationState state = new();
        state.TrackInput("first prompt");
        state.TrackInput("second prompt");
        TerminalInputController controller = new(_workspaceRoot);
        controller.Initialize(state);

        controller.HandleKey(Key(ConsoleKey.UpArrow), state);
        Assert.Equal("second prompt", controller.Buffer);

        controller.HandleKey(Key(ConsoleKey.UpArrow), state);
        Assert.Equal("first prompt", controller.Buffer);

        controller.HandleKey(Key(ConsoleKey.DownArrow), state);
        Assert.Equal("second prompt", controller.Buffer);

        controller.HandleKey(Key(ConsoleKey.DownArrow), state);
        Assert.Equal(string.Empty, controller.Buffer);
    }

    [Fact]
    public void Tab_Completes_Slash_Command_And_Focus_Path()
    {
        TerminalConversationState state = new();
        TerminalInputController controller = new(_workspaceRoot);
        controller.Initialize(state);

        foreach (char ch in "/fo")
        {
            controller.HandleKey(Key(ConsoleKey.A, ch), state);
        }

        controller.HandleKey(Key(ConsoleKey.Tab), state);
        Assert.Equal("/focus ", controller.Buffer);

        foreach (char ch in "src/Pro")
        {
            controller.HandleKey(Key(ConsoleKey.A, ch), state);
        }

        controller.HandleKey(Key(ConsoleKey.Tab), state);
        Assert.Equal("/focus src/Program.cs", controller.Buffer);
        Assert.Equal(TerminalPanelCatalog.Explorer, state.ActivePanelId);
    }

    [Fact]
    public void CtrlF_Prefills_Focus_And_Panel_Shortcut_Changes_Active_Panel()
    {
        TerminalConversationState state = new();
        TerminalInputController controller = new(_workspaceRoot);
        controller.Initialize(state);

        controller.HandleKey(Ctrl(ConsoleKey.F), state);
        Assert.Equal("/focus ", controller.Buffer);
        Assert.Equal(TerminalPanelCatalog.Explorer, state.ActivePanelId);

        controller.HandleKey(Ctrl(ConsoleKey.D3, '3'), state);
        Assert.Equal(TerminalPanelCatalog.Activity, state.ActivePanelId);
    }

    [Fact]
    public void CtrlK_Opens_Command_Palette_Overlay()
    {
        TerminalConversationState state = new();
        TerminalInputController controller = new(_workspaceRoot);
        controller.Initialize(state);

        TerminalInputResult result = controller.HandleKey(Ctrl(ConsoleKey.K), state);

        Assert.True(state.CommandPalette.IsOpen);
        Assert.Equal(TerminalSidebarModeCatalog.Commands, state.SidebarMode);
        Assert.Contains("Command palette opened", result.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Command_Palette_Filters_And_Submits_Selected_Command()
    {
        TerminalConversationState state = new();
        TerminalInputController controller = new(_workspaceRoot);
        controller.Initialize(state);

        controller.HandleKey(Ctrl(ConsoleKey.K), state);
        controller.HandleKey(Key(ConsoleKey.S, 's'), state);
        controller.HandleKey(Key(ConsoleKey.I, 'i'), state);
        controller.HandleKey(Key(ConsoleKey.D, 'd'), state);
        controller.HandleKey(Key(ConsoleKey.E, 'e'), state);
        TerminalInputResult result = controller.HandleKey(Key(ConsoleKey.Enter), state);

        Assert.False(state.CommandPalette.IsOpen);
        Assert.True(result.SubmitInput);
        Assert.Equal("/sidebar", result.SubmittedText);
    }

    [Fact]
    public void Focus_Actions_Dismiss_Launchpad()
    {
        TerminalConversationState state = new();
        TerminalInputController controller = new(_workspaceRoot);
        controller.Initialize(state);

        Assert.True(state.ShowLaunchpad);

        controller.HandleKey(Ctrl(ConsoleKey.F), state);
        Assert.False(state.ShowLaunchpad);

        state.ReopenLaunchpad();
        controller.HandleKey(Ctrl(ConsoleKey.D2, '2'), state);

        Assert.False(state.ShowLaunchpad);
        Assert.Equal(TerminalPanelCatalog.Conversation, state.ActivePanelId);
    }

    [Fact]
    public void CtrlR_Reverse_Search_Loads_Matching_History_Item()
    {
        TerminalConversationState state = new();
        state.TrackInput("summarize repository");
        state.TrackInput("fix failing tests");
        state.TrackInput("/focus src/Program.cs");
        TerminalInputController controller = new(_workspaceRoot);
        controller.Initialize(state);

        controller.HandleKey(Ctrl(ConsoleKey.R), state);
        controller.HandleKey(Key(ConsoleKey.F, 'f'), state);
        controller.HandleKey(Key(ConsoleKey.I, 'i'), state);
        controller.HandleKey(Key(ConsoleKey.X, 'x'), state);

        Assert.Equal("fix failing tests", controller.Buffer);
        Assert.Contains("reverse-i-search `fix`", controller.Metadata, StringComparison.Ordinal);
    }

    [Fact]
    public void PageUp_And_PageDown_Scroll_Conversation_Transcript()
    {
        TerminalConversationState state = new();
        TerminalInputController controller = new(_workspaceRoot);
        controller.Initialize(state);

        controller.HandleKey(Key(ConsoleKey.PageUp), state);
        Assert.Equal(8, state.ConversationScrollOffset);
        Assert.Equal(TerminalPanelCatalog.Conversation, state.ActivePanelId);

        controller.HandleKey(Key(ConsoleKey.PageDown), state);
        Assert.Equal(0, state.ConversationScrollOffset);
    }

    [Fact]
    public void Left_And_Right_Arrow_Move_The_Cursor_And_Insert_At_The_Current_Position()
    {
        TerminalConversationState state = new();
        TerminalInputController controller = new(_workspaceRoot);
        controller.Initialize(state);

        foreach (char ch in "hello")
        {
            controller.HandleKey(Key(ConsoleKey.A, ch), state);
        }

        controller.HandleKey(Key(ConsoleKey.LeftArrow), state);
        controller.HandleKey(Key(ConsoleKey.LeftArrow), state);

        Assert.Equal(3, controller.CursorIndex);

        controller.HandleKey(Key(ConsoleKey.X, 'X'), state);

        Assert.Equal("helXlo", controller.Buffer);
        Assert.Equal(4, controller.CursorIndex);

        controller.HandleKey(Key(ConsoleKey.RightArrow), state);
        Assert.Equal(5, controller.CursorIndex);

        controller.HandleKey(Key(ConsoleKey.End), state);
        controller.HandleKey(Key(ConsoleKey.Y, 'Y'), state);

        Assert.Equal("helXloY", controller.Buffer);
        Assert.Equal(controller.Buffer.Length, controller.CursorIndex);
    }

    [Fact]
    public void Home_And_End_Move_The_Cursor_To_Line_Boundaries()
    {
        TerminalConversationState state = new();
        TerminalInputController controller = new(_workspaceRoot);
        controller.Initialize(state);

        foreach (char ch in "sample")
        {
            controller.HandleKey(Key(ConsoleKey.A, ch), state);
        }

        controller.HandleKey(Key(ConsoleKey.Home), state);
        Assert.Equal(0, controller.CursorIndex);

        controller.HandleKey(Key(ConsoleKey.Z, 'Z'), state);
        Assert.Equal("Zsample", controller.Buffer);
        Assert.Equal(1, controller.CursorIndex);

        controller.HandleKey(Key(ConsoleKey.End), state);
        Assert.Equal(controller.Buffer.Length, controller.CursorIndex);
    }

    public void Dispose()
    {
        if (Directory.Exists(_workspaceRoot))
        {
            Directory.Delete(_workspaceRoot, recursive: true);
        }
    }

    private static ConsoleKeyInfo Key(ConsoleKey key, char keyChar = '\0')
        => new(keyChar, key, shift: false, alt: false, control: false);

    private static ConsoleKeyInfo Ctrl(ConsoleKey key, char keyChar = '\0')
        => new(keyChar, key, shift: false, alt: false, control: true);
}

