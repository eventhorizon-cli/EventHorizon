using EventHorizon.Conversations;
using EventHorizon.Terminal.Commands;
using EventHorizon.Terminal.Models;

namespace EventHorizon.Terminal.Dialogs;

public sealed class DialogService
{
    private readonly TerminalCommandRegistry _commandRegistry;
    private readonly TerminalGuiHost _guiHost;

    public DialogService(TerminalCommandRegistry commandRegistry, TerminalGuiHost guiHost)
    {
        _commandRegistry = commandRegistry;
        _guiHost = guiHost;
    }

    public Task<bool> ConfirmAsync(string title, string message)
    {
        ConfirmDialog dialog = new(title, message, _guiHost.RequestStop);
        Run(dialog);
        return Task.FromResult(dialog.Result == 1);
    }

    public Task<T?> SelectAsync<T>(string title, IReadOnlyList<T> items, Func<T, string> label)
    {
        SelectionDialog<T> dialog = new(title, items, label, _guiHost.RequestStop);
        Run(dialog);
        return Task.FromResult(dialog.SelectedItem);
    }

    public Task<string?> PromptAsync(string title, string prompt, string initialValue = "")
    {
        TextInputDialog dialog = new(title, prompt, initialValue, _guiHost.RequestStop);
        Run(dialog);
        return Task.FromResult(dialog.Result == 1 ? dialog.Input.Text : null);
    }

    public Task<ITerminalCommand?> ShowCommandPaletteAsync()
    {
        CommandPaletteDialog dialog = new(_commandRegistry.All, _guiHost.RequestStop);
        Run(dialog);
        return Task.FromResult(dialog.SelectedCommand);
    }

    public Task ShowHelpAsync()
    {
        HelpDialog dialog = new("Commands\n/help       Show help\n/clear      Clear chat\n/model      Switch model\n/session    Manage sessions\n/files      Show context files\n/tools      Show tool calls\n/diff       Show diff\n/plan       Show plan\n/layout     Change layout\n/theme      Change theme\n/cancel     Cancel running task\n/exit       Exit\n\nShortcuts\nCtrl+P      Command palette\nCtrl+H      Help\nCtrl+T      Tools\nCtrl+F      Files\nCtrl+L      Clear\nCtrl+C      Cancel / Exit\nCtrl+D      Exit\nEsc         Close dialog\n\nTips\nType / to run commands.\nEnter sends the prompt.", _guiHost.RequestStop);
        Run(dialog);
        return Task.CompletedTask;
    }

    public Task ShowToolDetailAsync(TerminalToolCall tool)
    {
        Run(new ToolDetailDialog(tool, _guiHost.RequestStop));
        return Task.CompletedTask;
    }

    public Task ShowDiffDetailAsync(TerminalDiffItem diff)
    {
        Run(new DiffDetailDialog(diff, _guiHost.RequestStop));
        return Task.CompletedTask;
    }

    public Task ShowErrorAsync(string message, Exception? exception)
    {
        Run(new ErrorDetailDialog(message, exception, _guiHost.RequestStop));
        return Task.CompletedTask;
    }

    public async Task ShowToolCallsAsync(IReadOnlyList<TerminalToolCall> tools)
    {
        if (tools.Count == 0)
        {
            await ShowErrorAsync("No tool calls are available.", null).ConfigureAwait(false);
            return;
        }

        ToolCallsDialog dialog = new(tools, _guiHost.RequestStop);
        Run(dialog);
        if (dialog.SelectedItem is { } tool)
        {
            await ShowToolDetailAsync(tool).ConfigureAwait(false);
        }
    }

    public async Task ShowDiffsAsync(IReadOnlyList<TerminalDiffItem> diffs)
    {
        if (diffs.Count == 0)
        {
            await ShowErrorAsync("No diffs are available.", null).ConfigureAwait(false);
            return;
        }

        DiffSelectionDialog dialog = new(diffs, _guiHost.RequestStop);
        Run(dialog);
        if (dialog.SelectedItem is { } diff)
        {
            await ShowDiffDetailAsync(diff).ConfigureAwait(false);
        }
    }

    public Task ShowPlanAsync(IReadOnlyList<TerminalPlanItem> plan)
    {
        Run(new PlanDialog(plan, _guiHost.RequestStop));
        return Task.CompletedTask;
    }

    public Task ShowContextFilesAsync(IReadOnlyList<TerminalContextFile> files)
    {
        Run(new ContextFilesDialog(files, _guiHost.RequestStop));
        return Task.CompletedTask;
    }

    public Task<string?> ShowModelSelectionAsync(TerminalState state)
        => SelectAsync("Select Model", BuildModelOptions(state), static value => value);

    public Task<string?> ShowLayoutSelectionAsync(Layout.TerminalLayoutMode currentMode, CancellationToken cancellationToken)
        => SelectAsync("Layout", ["auto", "compact", "standard", "expanded"], static value => value);

    public Task<string?> ShowThemeSelectionAsync(string currentTheme, CancellationToken cancellationToken)
        => SelectAsync("Theme", TerminalTheme.Presets.Select(static theme => theme.Name).ToList(), static value => value);

    public Task<ConversationSessionSummary?> ShowSessionSelectionAsync(IReadOnlyList<ConversationSessionSummary> sessions)
    {
        SessionSelectionDialog dialog = new(sessions, _guiHost.RequestStop);
        Run(dialog);
        return Task.FromResult(dialog.SelectedItem);
    }

    private static List<string> BuildModelOptions(TerminalState state)
    {
        HashSet<string> items = new(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(state.CurrentModel))
        {
            items.Add(state.CurrentModel);
        }

        items.Add("gpt-4.1");
        items.Add("gpt-4.1-mini");
        items.Add("claude-sonnet");
        items.Add("local-model");
        return items.ToList();
    }


    private void Run(global::Terminal.Gui.App.IRunnable dialog)
        => _guiHost.Run(dialog);
}

