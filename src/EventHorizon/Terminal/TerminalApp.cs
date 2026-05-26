using EventHorizon.Configuration;
using EventHorizon.Providers;
using EventHorizon.Terminal.Layout;
using EventHorizon.Terminal.Models;
using EventHorizon.Terminal.Views;
using EventHorizon.Workspace;
using Terminal.Gui.ViewBase;

namespace EventHorizon.Terminal;

public sealed class TerminalApp
{
    private readonly TerminalState _state;
    private readonly IEventHorizonRuntime _runtime;
    private readonly AppOptions _options;
    private readonly WorkspaceService _workspaceService;
    private readonly TerminalEventDispatcher _dispatcher;
    private readonly Dialogs.DialogService _dialogService;
    private readonly TerminalLayoutManager _layoutManager;
    private readonly TerminalResizeObserver _resizeObserver;
    private readonly TerminalGuiHost _guiHost;

    public TerminalApp(
        TerminalState state,
        IEventHorizonRuntime runtime,
        AppOptions options,
        WorkspaceService workspaceService,
        TerminalEventDispatcher dispatcher,
        Dialogs.DialogService dialogService,
        TerminalLayoutManager layoutManager,
        TerminalResizeObserver resizeObserver,
        TerminalGuiHost guiHost)
    {
        _state = state;
        _runtime = runtime;
        _options = options;
        _workspaceService = workspaceService;
        _dispatcher = dispatcher;
        _dialogService = dialogService;
        _layoutManager = layoutManager;
        _resizeObserver = resizeObserver;
        _guiHost = guiHost;
    }

    public Task RunAsync(CancellationToken cancellationToken)
    {
        InitializeState();
        _guiHost.Initialize();

        try
        {
            MainWindow window = new(_state, _dispatcher, _dialogService, _layoutManager)
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
            };

            _dispatcher.AttachMainWindow(window);
            _resizeObserver.Attach(window);
            _resizeObserver.Resized += (_, size) =>
            {
                _state.ActiveLayoutMode = _layoutManager.ResolveMode(size, _state.ForcedLayoutMode);
                window.ApplyLayout(_state.ActiveLayoutMode);
                window.RefreshFromState();
            };

            cancellationToken.Register(() => _guiHost.Invoke(() => _guiHost.RequestStop(window)));
            window.RefreshLayout();
            _guiHost.Run(window);
        }
        finally
        {
            _guiHost.Shutdown();
        }

        return Task.CompletedTask;
    }

    private void InitializeState()
    {
        var workspaceRoot = _workspaceService.WorkspaceRoot;
        _state.CurrentModel = string.IsNullOrWhiteSpace(_runtime.ModelName) ? _options.Provider.Model : _runtime.ModelName;
        _state.ProviderType = _options.Provider.Type;
        _state.CurrentSession = "main";
        _state.CurrentWorkingDirectory = workspaceRoot;
        _state.GitBranch = ParseGitBranch(_runtime.ContextSnapshot.GitStatus);
        _state.Status = TerminalRunStatus.WaitingForInput;
        _state.LastStatusMessage = "Ready. Type a prompt or use / for commands.";
        _state.ReplaceContextFiles(WorkspaceExplorerSnapshotBuilder.Build(workspaceRoot, focusedPath: null));
        _state.ReplacePlan(
        [
            new TerminalPlanItem { Title = "Inspect workspace", Status = TerminalPlanItemStatus.Completed },
            new TerminalPlanItem { Title = "Wait for user input", Status = TerminalPlanItemStatus.InProgress },
        ]);
        _state.ReplaceDiffs(ParseDiffsSafe());
    }

    private IReadOnlyList<TerminalDiffItem> ParseDiffsSafe()
    {
        try
        {
            var output = _workspaceService.RunShellAsync("git --no-pager status --short | cat", 15, CancellationToken.None).GetAwaiter().GetResult();
            return output
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(static line => !line.StartsWith("##", StringComparison.Ordinal))
                .Select(static line => new TerminalDiffItem
                {
                    Path = line.Length > 3 ? line[3..].Trim() : line,
                    Kind = line.Length > 0 && line[0] == 'A' ? TerminalDiffKind.Added : TerminalDiffKind.Modified,
                    Summary = line,
                })
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static string ParseGitBranch(string gitStatus)
    {
        var branchLine = gitStatus.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(static line => line.StartsWith("##", StringComparison.Ordinal));
        if (string.IsNullOrWhiteSpace(branchLine))
        {
            return "-";
        }

        var trimmed = branchLine[2..].Trim();
        var separator = trimmed.IndexOf("...", StringComparison.Ordinal);
        return separator >= 0 ? trimmed[..separator] : trimmed;
    }
}

