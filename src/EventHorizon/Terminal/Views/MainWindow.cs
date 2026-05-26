using EventHorizon.Terminal.Dialogs;
using EventHorizon.Terminal.Layout;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace EventHorizon.Terminal.Views;

public sealed class MainWindow : Window
{
    private readonly TerminalState _state;
    private readonly TerminalLayoutManager _layoutManager;

    private readonly HeaderView _header;
    private readonly ChatView _chat;
    private readonly InputView _input;
    private readonly StatusBarView _statusBar;
    private readonly ToolCallsView _tools;
    private readonly PlanView _plan;
    private readonly DiffView _diff;
    private readonly ContextFilesView _context;
    private readonly ErrorView _error;
    private readonly ActivityIndicatorView _activity;

    public MainWindow(
        TerminalState state,
        TerminalEventDispatcher dispatcher,
        DialogService dialogService,
        TerminalLayoutManager layoutManager)
    {
        _state = state;
        _layoutManager = layoutManager;

        Title = "EventHorizon";
        Width = Dim.Fill();
        Height = Dim.Fill();
        Data = state;

        _header = new HeaderView();
        _chat = new ChatView();
        _input = new InputView();
        _statusBar = new StatusBarView();
        _tools = new ToolCallsView();
        _plan = new PlanView();
        _diff = new DiffView();
        _context = new ContextFilesView();
        _error = new ErrorView();
        _activity = new ActivityIndicatorView();

        _input.Submitted += text => dispatcher.HandleUserInputAsync(text, CancellationToken.None);
        _input.ShortcutPressed += dispatcher.HandleShortcutAsync;
        FrameChanged += (_, _) => RefreshLayout();

        Add(_header, _chat, _context, _tools, _plan, _diff, _input, _statusBar, _error, _activity);
    }

    public void ApplyLayout(TerminalLayoutMode mode)
    {
        _state.ActiveLayoutMode = mode;
        switch (mode)
        {
            case TerminalLayoutMode.Expanded:
                ApplyExpandedLayout();
                break;
            case TerminalLayoutMode.Standard:
                ApplyStandardLayout();
                break;
            default:
                ApplyCompactLayout();
                break;
        }
    }

    public void RefreshFromState()
    {
        ApplyTheme();
        _header.Update(_state);
        _chat.Update(_state);
        _tools.Update(_state);
        _plan.Update(_state);
        _diff.Update(_state);
        _context.Update(_state);
        _statusBar.Update(_state);
        _error.Update(_state);
        _activity.Update(_state);
        _input.Update(_state);
        _input.FocusInput();
        SetNeedsDraw();
    }

    private void ApplyTheme()
    {
        var theme = TerminalTheme.Presets.FirstOrDefault(item => string.Equals(item.Name, _state.CurrentThemeName, StringComparison.OrdinalIgnoreCase))
            ?? TerminalTheme.Midnight;

        SchemeName = theme.DefaultScheme;
        _header.SchemeName = theme.ActiveScheme;
        _chat.SchemeName = theme.MutedScheme;
        _context.SchemeName = theme.MutedScheme;
        _tools.SchemeName = theme.WarningScheme;
        _plan.SchemeName = theme.ActiveScheme;
        _diff.SchemeName = theme.MutedScheme;
        _input.SchemeName = theme.ActiveScheme;
        _statusBar.SchemeName = theme.ActiveScheme;
        _error.SchemeName = theme.ErrorScheme;
        _activity.SchemeName = theme.SuccessScheme;
    }

    public void RefreshLayout()
    {
        var mode = _layoutManager.ResolveMode(new TerminalSize(Frame.Width, Frame.Height), _state.ForcedLayoutMode);
        ApplyLayout(mode);
        RefreshFromState();
    }

    private void ApplyExpandedLayout()
    {
        _header.X = 0;
        _header.Y = 0;
        _header.Width = Dim.Fill();

        _chat.X = 0;
        _chat.Y = Pos.Bottom(_header);
        _chat.Width = Dim.Percent(70);
        _chat.Height = Dim.Fill(12);

        _context.X = Pos.Right(_chat);
        _context.Y = Pos.Top(_chat);
        _context.Width = Dim.Fill();
        _context.Height = 10;
        _context.Visible = true;

        _tools.X = Pos.Left(_context);
        _tools.Y = Pos.Bottom(_context);
        _tools.Width = Dim.Width(_context);
        _tools.Height = 10;
        _tools.Visible = true;

        _plan.X = 0;
        _plan.Y = Pos.Bottom(_chat);
        _plan.Width = Dim.Percent(50);
        _plan.Height = 6;
        _plan.Visible = true;

        _diff.X = Pos.Right(_plan);
        _diff.Y = Pos.Top(_plan);
        _diff.Width = Dim.Fill();
        _diff.Height = 6;
        _diff.Visible = true;

        LayoutFooter();
    }

    private void ApplyStandardLayout()
    {
        _header.X = 0;
        _header.Y = 0;
        _header.Width = Dim.Fill();

        _chat.X = 0;
        _chat.Y = Pos.Bottom(_header);
        _chat.Width = Dim.Fill();
        _chat.Height = Dim.Fill(12);

        _tools.X = 0;
        _tools.Y = Pos.Bottom(_chat);
        _tools.Width = Dim.Fill();
        _tools.Height = 5;
        _tools.Visible = true;

        _context.Visible = false;
        _plan.Visible = false;
        _diff.Visible = false;

        LayoutFooter();
    }

    private void ApplyCompactLayout()
    {
        _header.X = 0;
        _header.Y = 0;
        _header.Width = Dim.Fill();

        _chat.X = 0;
        _chat.Y = Pos.Bottom(_header);
        _chat.Width = Dim.Fill();
        _chat.Height = Dim.Fill(9);

        _context.Visible = false;
        _tools.Visible = false;
        _plan.Visible = false;
        _diff.Visible = false;

        LayoutFooter();
    }

    private void LayoutFooter()
    {
        _error.X = 0;
        _error.Y = Pos.AnchorEnd(9);
        _error.Width = Dim.Fill();

        _input.X = 0;
        _input.Y = Pos.AnchorEnd(6);
        _input.Width = Dim.Fill();

        _activity.X = 0;
        _activity.Y = Pos.AnchorEnd(3);
        _activity.Width = Dim.Percent(30);

        _statusBar.X = Pos.Right(_activity);
        _statusBar.Y = Pos.Top(_activity);
        _statusBar.Width = Dim.Fill();
    }

}

