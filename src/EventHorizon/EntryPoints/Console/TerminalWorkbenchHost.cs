using System.Text;
using EventHorizon.Configuration;
using EventHorizon.Diagnostics;
using EventHorizon.Execution;
using EventHorizon.Pricing;
using EventHorizon.Providers;
using EventHorizon.Terminal;
using EventHorizon.Terminal.Commands;
using EventHorizon.Terminal.Session;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace EventHorizon.EntryPoints.Console;

public sealed class TerminalWorkbenchHost
{
    private readonly IEventHorizonRuntime _runtime;
    private readonly AppOptions _options;
    private readonly QueryEngine _queryEngine;
    private readonly TerminalRuntimeContext _runtimeContext;
    private readonly TerminalWorkbenchComposer _composer;
    private readonly ITerminalLayoutRenderer _renderer;
    private readonly ITerminalWindowSizeMonitor _windowSizeMonitor;
    private readonly ISessionUsageTracker _usageTracker;
    private readonly TerminalCommandDispatcher _commandDispatcher;
    private readonly TerminalInputController _inputController;
    private string _status = "Ready. Describe a change, ask for a review, or use /help.";
    private int _pendingResizeRefresh;

    public TerminalRuntimeContext RuntimeContext => _runtimeContext;

    public TerminalWorkbenchHost(
        IEventHorizonRuntime runtime,
        IOptions<AppOptions> options,
        QueryEngine queryEngine,
        ISessionUsageTracker usageTracker,
        ITerminalSessionService sessionService,
        IRunErrorLogWriter errorLogWriter,
        TerminalWorkbenchComposer composer,
        ITerminalLayoutRenderer renderer,
        ITerminalWindowSizeMonitor windowSizeMonitor,
        TerminalCommandDispatcher commandDispatcher)
    {
        _runtime = runtime;
        _options = options.Value;
        _queryEngine = queryEngine;
        _usageTracker = usageTracker;
        _runtimeContext = new TerminalRuntimeContext(_options, usageTracker, sessionService, errorLogWriter);
        _inputController = new TerminalInputController(_options.WorkspaceRoot);
        _composer = composer;
        _renderer = renderer;
        _windowSizeMonitor = windowSizeMonitor;
        _commandDispatcher = commandDispatcher;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        _inputController.Initialize(_runtimeContext.State);
        ApplyPendingInputState();
        SubscribeToWindowSizeChanges();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Render();
                var input = ReadInput(cancellationToken);
                if (input is null)
                {
                    System.Console.WriteLine();
                    break;
                }

                if (string.IsNullOrWhiteSpace(input))
                {
                    if (_runtimeContext.State.ShowLaunchpad)
                    {
                        _runtimeContext.State.DismissLaunchpad();
                        _status = TerminalLaunchpad.BuildWorkbenchStatusLine(_options);
                        continue;
                    }

                    _status = "Ready. Type a prompt or use /help for commands.";
                    continue;
                }

                try
                {
                    if (TerminalCommand.Parse(input).IsSlashCommand)
                    {
                        var shouldExit = await HandleCommandAsync(input, cancellationToken).ConfigureAwait(false);
                        if (shouldExit)
                        {
                            break;
                        }

                        continue;
                    }

                    await RunPromptAsync(input.Trim(), cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    _runtimeContext.State.AddActivity("cancel", "Cancelled the active run");
                    _status = "Cancelled the active run.";
                }
                catch (Exception ex)
                {
                    _runtimeContext.RecordError("Request failed", ex);
                    _status = $"Request failed. Open {TerminalCommandCatalog.GetSidebarModeLabel(TerminalSidebarModeCatalog.Errors)} for details.";
                }
            }
        }
        finally
        {
            UnsubscribeFromWindowSizeChanges();
        }
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await _runtimeContext.RefreshSavedSessionsAsync(cancellationToken).ConfigureAwait(false);
        await SyncEngineWithStateAsync(cancellationToken, ensureSession: false).ConfigureAwait(false);
        _runtimeContext.State.SetActivePanel(TerminalPanelCatalog.Conversation);
        _runtimeContext.State.AddActivity(
            "session",
            "Workbench ready",
            $"Workspace {_options.WorkspaceRoot}");
        _status = TerminalLaunchpad.BuildLaunchpadStatusLine(_options);
    }

    private async Task<bool> HandleCommandAsync(string input, CancellationToken cancellationToken)
    {
        _runtimeContext.State.DismissLaunchpad();
        _runtimeContext.State.TrackCommand(input);
        var result = await _commandDispatcher
            .DispatchAsync(input, _runtimeContext, cancellationToken)
            .ConfigureAwait(false);

        _inputController.Initialize(_runtimeContext.State);
        ApplyPendingInputState();
        await SyncEngineWithStateAsync(cancellationToken, ensureSession: false).ConfigureAwait(false);
        _status = result.StatusMessage;
        return result.ShouldExit;
    }

    private async Task RunPromptAsync(string prompt, CancellationToken cancellationToken)
    {
        _runtimeContext.State.DismissLaunchpad();
        _runtimeContext.State.ResetConversationScroll();
        _runtimeContext.State.SetActivePanel(TerminalPanelCatalog.Conversation);
        _runtimeContext.State.AddMessage("user", prompt);
        _runtimeContext.State.AddActivity("prompt", "Submitted prompt", Summarize(prompt, 96));
        _runtimeContext.State.AddActivity("response", "Awaiting model stream", _runtime.ModelName);
        _status = $"Running on {_runtime.ModelName}…";

        await SyncEngineWithStateAsync(cancellationToken, ensureSession: true).ConfigureAwait(false);
        Render();

        var assistantBuffer = new StringBuilder();
        var firstAssistantDeltaReceived = false;
        await foreach (var evt in _queryEngine.SubmitAsync(prompt, cancellationToken).ConfigureAwait(false))
        {
            switch (evt.Kind)
            {
                case QueryEventKind.UserMessage:
                    break;
                case QueryEventKind.ToolCall:
                    _runtimeContext.State.AddActivity("tool", "Tool call", evt.Text);
                    _runtimeContext.State.SetActivePanel(TerminalPanelCatalog.Activity);
                    Render(isStreaming: true, assistantPreview: assistantBuffer.ToString());
                    break;
                case QueryEventKind.ToolResult:
                    _runtimeContext.State.AddActivity("tool-result", "Tool result", evt.Text);
                    _runtimeContext.State.SetActivePanel(TerminalPanelCatalog.Activity);
                    Render(isStreaming: true, assistantPreview: assistantBuffer.ToString());
                    break;
                case QueryEventKind.AssistantDelta:
                    if (!firstAssistantDeltaReceived)
                    {
                        firstAssistantDeltaReceived = true;
                        _runtimeContext.State.AddActivity("response", "First tokens received");
                    }

                    assistantBuffer.Append(evt.Text);
                    _runtimeContext.State.SetAssistantPreview(assistantBuffer.ToString());
                    _status = BuildStreamingStatus(assistantBuffer.Length);
                    Render(isStreaming: true, assistantPreview: assistantBuffer.ToString());
                    break;
                case QueryEventKind.Completed:
                    var assistantText = assistantBuffer.Length == 0 ? evt.Text : assistantBuffer.ToString();
                    if (!string.IsNullOrWhiteSpace(assistantText))
                    {
                        _runtimeContext.State.AddMessage("assistant", assistantText);
                    }

                    _runtimeContext.State.SetAssistantPreview(assistantText);
                    _runtimeContext.State.TotalUsage.InputTokenCount = _usageTracker.TotalUsage.InputTokenCount;
                    _runtimeContext.State.TotalUsage.InputTextTokenCount = _usageTracker.TotalUsage.InputTextTokenCount;
                    _runtimeContext.State.TotalUsage.OutputTokenCount = _usageTracker.TotalUsage.OutputTokenCount;
                    _runtimeContext.State.TotalUsage.OutputTextTokenCount = _usageTracker.TotalUsage.OutputTextTokenCount;
                    _runtimeContext.State.TotalUsage.TotalTokenCount = _usageTracker.TotalUsage.TotalTokenCount;
                    _runtimeContext.State.TotalCost = _usageTracker.TotalCost;
                    _runtimeContext.State.AddActivity("response", "Response complete", BuildUsageLine(evt.Usage, evt.CostUsd));
                    _runtimeContext.State.SetActivePanel(TerminalPanelCatalog.Conversation);
                    _status = BuildCompletedStatus(evt.Usage, evt.CostUsd);
                    await TryAutoSaveAsync(cancellationToken).ConfigureAwait(false);
                    Render();
                    break;
            }
        }
    }

    private async Task SyncEngineWithStateAsync(CancellationToken cancellationToken, bool ensureSession)
    {
        var session = _runtimeContext.SessionService.CurrentSession;
        if (ensureSession && session is null)
        {
            session = await _runtimeContext.SessionService.EnsureSessionAsync(cancellationToken).ConfigureAwait(false);
        }

        _queryEngine.LoadConversationState(session, _runtimeContext.State.Transcript.Select(MapConversationEntry));
    }

    private async Task TryAutoSaveAsync(CancellationToken cancellationToken)
    {
        if (!_options.Conversation.AutoSave)
        {
            return;
        }

        await _runtimeContext.SessionService
            .SaveAsync(_options.Conversation.AutoSaveSessionName, _runtimeContext.State, cancellationToken)
            .ConfigureAwait(false);
        await _runtimeContext.RefreshSavedSessionsAsync(cancellationToken).ConfigureAwait(false);
        _runtimeContext.State.AddActivity("save", "Autosaved session snapshot", _options.Conversation.AutoSaveSessionName);
    }

    private void Render(bool isStreaming = false, string? assistantPreview = null)
    {
        int animationFrameIndex = PeekLaunchpadAnimationFrameIndex();
        _renderer.Render(_composer.Compose(
            _options,
            _runtimeContext.State,
            _usageTracker,
            _status,
            isStreaming,
            assistantPreview,
            animationFrameIndex));
        _lastRenderedAnimationFrame = ShouldAnimateLaunchpad() ? animationFrameIndex : -1;
    }

    private string? ReadInput(CancellationToken cancellationToken)
    {
        if (System.Console.IsInputRedirected || System.Console.IsOutputRedirected)
        {
            var redirectedInput = System.Console.ReadLine();
            if (redirectedInput is not null)
            {
                _runtimeContext.State.TrackInput(redirectedInput.Trim());
            }

            _runtimeContext.State.SetPendingInputState(string.Empty, 0, string.Empty);
            return redirectedInput;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            if (TryRefreshForResize())
            {
                continue;
            }

            var canCheckKeyAvailable = TryGetKeyAvailable(out var keyAvailable);
            if (canCheckKeyAvailable && !keyAvailable)
            {
                if (ShouldAnimateLaunchpad())
                {
                    var animationFrameIndex = PeekLaunchpadAnimationFrameIndex();
                    if (animationFrameIndex != _lastRenderedAnimationFrame)
                    {
                        Render();
                    }
                }

                Thread.Sleep(30);
                continue;
            }

            var keyInfo = System.Console.ReadKey(intercept: true);
            var previousPanel = _runtimeContext.State.ActivePanelId;
            var result = _inputController.HandleKey(keyInfo, _runtimeContext.State);

            if (!result.HasChanges)
            {
                continue;
            }

            ApplyPendingInputState();

            if (!string.IsNullOrWhiteSpace(result.StatusMessage))
            {
                _status = result.StatusMessage;
            }

            if (result.ForceFullRefresh)
            {
                _renderer.Reset();
            }

            if (!string.Equals(previousPanel, _runtimeContext.State.ActivePanelId, StringComparison.OrdinalIgnoreCase))
            {
                _runtimeContext.State.AddActivity("panel", "Panel focus changed", _runtimeContext.State.ActivePanelId);
            }

            if (result.EndOfInput)
            {
                return null;
            }

            if (result.SubmitInput)
            {
                return result.SubmittedText;
            }

            Render();
        }

        throw new OperationCanceledException(cancellationToken);
    }

    private void ApplyPendingInputState()
        => _runtimeContext.State.SetPendingInputState(_inputController.Buffer, _inputController.CursorIndex, _inputController.Metadata);

    private void SubscribeToWindowSizeChanges()
    {
        _windowSizeMonitor.SizeChanged += OnWindowSizeChanged;
        _windowSizeMonitor.Start();
    }

    private void UnsubscribeFromWindowSizeChanges()
        => _windowSizeMonitor.SizeChanged -= OnWindowSizeChanged;

    private void OnWindowSizeChanged(object? sender, EventArgs e)
        => Interlocked.Exchange(ref _pendingResizeRefresh, 1);

    private bool TryRefreshForResize()
    {
        if (Interlocked.Exchange(ref _pendingResizeRefresh, 0) == 0)
        {
            return false;
        }

        _renderer.Reset();
        Render();
        return true;
    }

    private int _lastRenderedAnimationFrame = -1;

    private static int PeekLaunchpadAnimationFrameIndex()
        => TerminalMascotAnimator.GetFrameIndex(DateTimeOffset.UtcNow);

    private bool ShouldAnimateLaunchpad()
        => _runtimeContext.State.ShowLaunchpad
            && !System.Console.IsInputRedirected
            && !System.Console.IsOutputRedirected;

    private static bool TryGetKeyAvailable(out bool keyAvailable)
    {
        try
        {
            keyAvailable = System.Console.KeyAvailable;
            return true;
        }
        catch
        {
            keyAvailable = false;
            return false;
        }
    }

    private static ConversationEntry MapConversationEntry(TerminalMessage message)
        => new(ParseRole(message.Role), message.Text);

    private static ChatRole ParseRole(string role)
        => string.Equals(role, "assistant", StringComparison.OrdinalIgnoreCase) ? ChatRole.Assistant : ChatRole.User;

    private static string BuildStreamingStatus(int characterCount)
        => characterCount <= 0
            ? "Streaming response…"
            : $"Streaming response… {characterCount} chars received";

    private static string BuildCompletedStatus(UsageDetails? usage, decimal? costUsd)
    {
        var usageLine = BuildUsageLine(usage, costUsd);
        return $"Ready. {usageLine}";
    }

    private static string BuildUsageLine(UsageDetails? usage, decimal? costUsd)
    {
        if (usage is null)
        {
            return "No usage data returned by the model provider.";
        }

        var input = usage.InputTokenCount ?? usage.InputTextTokenCount ?? 0;
        var output = usage.OutputTokenCount ?? usage.OutputTextTokenCount ?? 0;
        var total = usage.TotalTokenCount ?? input + output;
        return costUsd is { } cost
            ? $"{total:N0} tokens ({input:N0} in · {output:N0} out) · est. {cost} USD"
            : $"{total:N0} tokens ({input:N0} in · {output:N0} out)";
    }

    private static string Summarize(string text, int maxLength)
    {
        var compact = text.Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace('\n', ' ')
            .Trim();
        return compact.Length <= maxLength ? compact : compact[..(maxLength - 1)] + "…";
    }
}

