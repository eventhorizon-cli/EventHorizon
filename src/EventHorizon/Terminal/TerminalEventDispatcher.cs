using EventHorizon.Configuration;
using EventHorizon.Execution;
using EventHorizon.Terminal.Commands;
using EventHorizon.Terminal.Dialogs;
using EventHorizon.Terminal.Events;
using EventHorizon.Terminal.Layout;
using EventHorizon.Terminal.Models;
using EventHorizon.Terminal.Session;
using EventHorizon.Terminal.Views;
using Microsoft.Extensions.AI;
using Terminal.Gui.Input;

namespace EventHorizon.Terminal;

public sealed class TerminalEventDispatcher
{
    private readonly TerminalState _state;
    private readonly ITerminalAgentAdapter _agentAdapter;
    private readonly DialogService _dialogs;
    private readonly TerminalCommandRegistry _commandRegistry;
    private readonly AppOptions _options;
    private readonly ITerminalSessionService _sessionService;
    private readonly QueryEngine _queryEngine;
    private readonly TerminalGuiHost _guiHost;
    private MainWindow? _mainWindow;

    public TerminalEventDispatcher(
        TerminalState state,
        ITerminalAgentAdapter agentAdapter,
        DialogService dialogs,
        TerminalCommandRegistry commandRegistry,
        AppOptions options,
        ITerminalSessionService sessionService,
        QueryEngine queryEngine,
        TerminalGuiHost guiHost)
    {
        _state = state;
        _agentAdapter = agentAdapter;
        _dialogs = dialogs;
        _commandRegistry = commandRegistry;
        _options = options;
        _sessionService = sessionService;
        _queryEngine = queryEngine;
        _guiHost = guiHost;
    }

    public void AttachMainWindow(MainWindow mainWindow)
        => _mainWindow = mainWindow;

    public async Task HandleUserInputAsync(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (text.StartsWith("/", StringComparison.Ordinal))
        {
            await ExecuteCommandAsync(text, cancellationToken).ConfigureAwait(false);
            return;
        }

        _state.CurrentInput = string.Empty;
        _state.AddInputHistory(text);
        await UpdateOnUiAsync(() =>
        {
            _state.AddMessage(TerminalMessageRole.User, text);
            _state.Status = TerminalRunStatus.Thinking;
            _state.LastStatusMessage = "Thinking...";
            _state.ClearError();
            Refresh();
        }).ConfigureAwait(false);

        _state.CurrentRunCancellation?.Dispose();
        _state.CurrentRunCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var runCancellationToken = _state.CurrentRunCancellation.Token;

        var session = await _sessionService.EnsureSessionAsync(runCancellationToken).ConfigureAwait(false);
        _queryEngine.LoadConversationState(session, MapConversationHistory(_state));

        try
        {
            await foreach (var agentEvent in _agentAdapter.SendAsync(text, runCancellationToken).ConfigureAwait(false))
            {
                await UpdateOnUiAsync(() => ApplyEvent(agentEvent)).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            await UpdateOnUiAsync(() =>
            {
                _state.Status = TerminalRunStatus.Cancelled;
                _state.IsStreaming = false;
                _state.IsToolRunning = false;
                foreach (var toolCall in _state.ToolCalls.Where(static tool => tool.Status == TerminalToolCallStatus.Running))
                {
                    toolCall.Status = TerminalToolCallStatus.Cancelled;
                    toolCall.FinishedAt = DateTimeOffset.UtcNow;
                }

                _state.LastStatusMessage = "Cancelled.";
                Refresh();
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await UpdateOnUiAsync(() =>
            {
                _state.SetError(ex.Message, ex);
                _state.AddMessage(TerminalMessageRole.Error, ex.Message);
                Refresh();
            }).ConfigureAwait(false);
        }
        finally
        {
            _state.CurrentRunCancellation?.Dispose();
            _state.CurrentRunCancellation = null;
        }
    }

    public async Task HandleShortcutAsync(Key key)
    {
        if (key == TerminalKeyBindings.CommandPalette)
        {
            var command = await _dialogs.ShowCommandPaletteAsync().ConfigureAwait(false);
            if (command is not null)
            {
                await ExecuteCommandAsync(command.Name, CancellationToken.None).ConfigureAwait(false);
            }

            return;
        }

        if (key == TerminalKeyBindings.Help)
        {
            await ExecuteCommandAsync("/help", CancellationToken.None).ConfigureAwait(false);
            return;
        }

        if (key == TerminalKeyBindings.Tools)
        {
            await ExecuteCommandAsync("/tools", CancellationToken.None).ConfigureAwait(false);
            return;
        }

        if (key == TerminalKeyBindings.Files)
        {
            await ExecuteCommandAsync("/files", CancellationToken.None).ConfigureAwait(false);
            return;
        }

        if (key == TerminalKeyBindings.Clear)
        {
            await ExecuteCommandAsync("/clear", CancellationToken.None).ConfigureAwait(false);
            return;
        }

        if (key == TerminalKeyBindings.CancelOrExit)
        {
            if (_state.CurrentRunCancellation is not null)
            {
                await CancelAsync().ConfigureAwait(false);
            }
            else
            {
                await RequestExitAsync(CancellationToken.None).ConfigureAwait(false);
            }

            return;
        }

        if (key == TerminalKeyBindings.Exit)
        {
            await RequestExitAsync(CancellationToken.None).ConfigureAwait(false);
            return;
        }

        if (key == TerminalKeyBindings.Refresh)
        {
            await UpdateOnUiAsync(Refresh).ConfigureAwait(false);
        }
    }

    public async Task CancelAsync()
    {
        _state.CurrentRunCancellation?.Cancel();
        await UpdateOnUiAsync(() =>
        {
            _state.Status = TerminalRunStatus.Cancelled;
            _state.LastStatusMessage = "Cancellation requested.";
            Refresh();
        }).ConfigureAwait(false);
    }

    public async Task RequestExitAsync(CancellationToken cancellationToken)
    {
        var confirmed = await _dialogs.ConfirmAsync("Exit", "Exit EventHorizon?").ConfigureAwait(false);
        if (!confirmed || _mainWindow is null)
        {
            return;
        }

        await UpdateOnUiAsync(() =>
        {
            _state.ExitRequested = true;
            _guiHost.RequestStop(_mainWindow);
        }).ConfigureAwait(false);
    }

    public async Task ShowSessionsAsync(CancellationToken cancellationToken)
    {
        var sessions = await _sessionService.ListAsync(cancellationToken).ConfigureAwait(false);
        var selected = await _dialogs.ShowSessionSelectionAsync(sessions).ConfigureAwait(false);
        if (selected is null)
        {
            return;
        }

        var restored = await _sessionService.RestoreAsync(selected.Id, cancellationToken).ConfigureAwait(false);
        await UpdateOnUiAsync(() =>
        {
            ApplyRestoredState(restored.State, selected.Name);
            _queryEngine.LoadConversationState(_sessionService.CurrentSession, MapConversationHistory(_state));
            Refresh();
        }).ConfigureAwait(false);
    }

    public void ApplyLayoutSelection(string? selection)
    {
        _state.ForcedLayoutMode = selection?.ToLowerInvariant() switch
        {
            "compact" => TerminalLayoutMode.Compact,
            "standard" => TerminalLayoutMode.Standard,
            "expanded" => TerminalLayoutMode.Expanded,
            _ => null,
        };
        Refresh();
    }

    private async Task ExecuteCommandAsync(string input, CancellationToken cancellationToken)
    {
        var commandName = input.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? input;
        if (!_commandRegistry.TryGet(commandName, out var command) || command is null || _mainWindow is null)
        {
            await _dialogs.ShowErrorAsync($"Unknown command: {commandName}", null).ConfigureAwait(false);
            return;
        }

        TerminalCommandContext context = new()
        {
            State = _state,
            Dialogs = _dialogs,
            Dispatcher = this,
            MainWindow = _mainWindow,
            Options = _options,
        };

        await command.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
        await UpdateOnUiAsync(Refresh).ConfigureAwait(false);
    }


    private void ApplyEvent(TerminalAgentEvent agentEvent)
    {
        switch (agentEvent)
        {
            case AssistantDelta assistantDelta:
                _state.Status = TerminalRunStatus.Streaming;
                _state.IsStreaming = true;
                _state.UpsertStreamingAssistantMessage(assistantDelta.Text);
                break;
            case AssistantMessageCompleted completed:
                _state.Status = TerminalRunStatus.WaitingForInput;
                _state.IsStreaming = false;
                _state.CompleteStreamingAssistantMessage(completed.Message);
                _state.TotalTokens = completed.TotalTokens;
                _state.LastCostUsd = completed.CostUsd;
                break;
            case ToolCallStarted toolCallStarted:
                _state.Status = TerminalRunStatus.ToolRunning;
                _state.IsToolRunning = true;
                _state.ToolCalls.Add(new TerminalToolCall
                {
                    Id = toolCallStarted.Id,
                    Name = toolCallStarted.Name,
                    Description = toolCallStarted.Description,
                    ArgumentsSummary = toolCallStarted.ArgumentsSummary,
                    Status = TerminalToolCallStatus.Running,
                    StartedAt = DateTimeOffset.UtcNow,
                });
                break;
            case ToolCallOutput toolCallOutput:
                var tool = _state.ToolCalls.LastOrDefault(item => item.Id == toolCallOutput.Id);
                if (tool is not null)
                {
                    tool.Output = string.IsNullOrWhiteSpace(tool.Output)
                        ? toolCallOutput.Output
                        : tool.Output + Environment.NewLine + toolCallOutput.Output;
                }

                break;
            case ToolCallFinished toolCallFinished:
                var finishedTool = _state.ToolCalls.LastOrDefault(item => item.Id == toolCallFinished.Id);
                if (finishedTool is not null)
                {
                    finishedTool.Status = toolCallFinished.Success ? TerminalToolCallStatus.Succeeded : TerminalToolCallStatus.Failed;
                    finishedTool.Error = toolCallFinished.Error;
                    finishedTool.FinishedAt = DateTimeOffset.UtcNow;
                }

                _state.IsToolRunning = _state.ToolCalls.Any(static item => item.Status == TerminalToolCallStatus.Running);
                break;
            case PlanUpdated planUpdated:
                _state.ReplacePlan(planUpdated.Items);
                break;
            case DiffUpdated diffUpdated:
                _state.ReplaceDiffs(diffUpdated.Items);
                break;
            case ContextFilesUpdated contextFilesUpdated:
                _state.ReplaceContextFiles(contextFilesUpdated.Items);
                break;
            case StatusChanged statusChanged:
                _state.Status = statusChanged.Status;
                _state.LastStatusMessage = statusChanged.Detail;
                break;
            case AgentError agentError:
                _state.SetError(agentError.Message, agentError.Exception);
                _state.AddMessage(TerminalMessageRole.Error, agentError.Message);
                break;
        }

        Refresh();
    }

    private void ApplyRestoredState(TerminalState restoredState, string sessionName)
    {
        _state.SessionId = restoredState.SessionId;
        _state.CreatedAt = restoredState.CreatedAt;
        _state.ConversationId = restoredState.ConversationId;
        _state.ClearChat();
        foreach (var message in restoredState.Messages)
        {
            _state.Messages.Add(message);
        }

        _state.ReplacePlan(restoredState.Plan);
        _state.ReplaceDiffs(restoredState.Diffs);
        _state.ReplaceContextFiles(restoredState.ContextFiles);
        _state.TotalTokens = restoredState.TotalTokens;
        _state.LastCostUsd = restoredState.LastCostUsd;
        _state.CurrentSession = sessionName;
    }

    private IEnumerable<ConversationEntry> MapConversationHistory(TerminalState state)
    {
        foreach (var message in state.Messages)
        {
            yield return new ConversationEntry(
                message.Role == TerminalMessageRole.Assistant ? ChatRole.Assistant : ChatRole.User,
                message.Content);
        }
    }

    private Task UpdateOnUiAsync(Action action)
    {
        _guiHost.Invoke(action);
        return Task.CompletedTask;
    }

    private void Refresh()
        => _mainWindow?.RefreshFromState();
}

