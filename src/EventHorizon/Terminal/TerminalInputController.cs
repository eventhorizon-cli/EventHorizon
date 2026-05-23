namespace EventHorizon.Terminal;

public sealed class TerminalInputController
{
    private readonly string _workspaceRoot;
    private int? _historyIndex;
    private string _historyDraft = string.Empty;
    private bool _isReverseSearchActive;
    private string _reverseSearchQuery = string.Empty;
    private int _reverseSearchOffset;
    private string _reverseSearchOriginalBuffer = string.Empty;
    private IReadOnlyList<string> _lastCompletionCandidates = [];

    public TerminalInputController(string workspaceRoot)
    {
        _workspaceRoot = Path.GetFullPath(workspaceRoot);
    }

    public string Buffer { get; private set; } = string.Empty;

    public int CursorIndex { get; private set; }

    public bool IsReverseSearchActive => _isReverseSearchActive;

    public string ReverseSearchQuery => _reverseSearchQuery;

    public string Metadata { get; private set; } = string.Empty;

    public void Initialize(TerminalConversationState state)
    {
        Buffer = state.PendingInput;
        CursorIndex = Math.Clamp(state.PendingInputCursorIndex, 0, Buffer.Length);
        Metadata = state.PendingInputMetadata;
    }

    public void Clear()
    {
        Buffer = string.Empty;
        CursorIndex = 0;
        Metadata = string.Empty;
        _historyIndex = null;
        _historyDraft = string.Empty;
        ExitReverseSearch(restoreOriginalBuffer: false);
        _lastCompletionCandidates = [];
    }

    public TerminalInputResult HandleKey(ConsoleKeyInfo keyInfo, TerminalConversationState state)
    {
        if (state.CommandPalette.IsOpen)
        {
            return HandleCommandPaletteKey(keyInfo, state);
        }

        if ((keyInfo.Modifiers & ConsoleModifiers.Control) != 0)
        {
            return HandleControlKey(keyInfo, state);
        }

        if (keyInfo.Key == ConsoleKey.Tab)
        {
            return Complete(state, reverse: (keyInfo.Modifiers & ConsoleModifiers.Shift) != 0);
        }

        if (_isReverseSearchActive)
        {
            return HandleReverseSearchKey(keyInfo, state);
        }

        return HandleStandardKey(keyInfo, state);
    }

    private TerminalInputResult HandleControlKey(ConsoleKeyInfo keyInfo, TerminalConversationState state)
    {
        return keyInfo.Key switch
        {
            ConsoleKey.D0 => ShowSidebarOverview(state),
            ConsoleKey.L => TerminalInputResult.Refresh("Screen refreshed."),
            ConsoleKey.R => BeginOrAdvanceReverseSearch(state),
            ConsoleKey.F => PrefillFocusCommand(state),
            ConsoleKey.K => OpenCommandPalette(state),
            ConsoleKey.D when Buffer.Length == 0 => TerminalInputResult.EndInput(),
            ConsoleKey.D => DeleteForward(),
            ConsoleKey.A => MoveCursorTo(0),
            ConsoleKey.E => MoveCursorTo(Buffer.Length),
            ConsoleKey.B => MoveCursorBy(-1),
            ConsoleKey.N => MoveCursorBy(1),
            ConsoleKey.P => NavigateHistory(state, -1),
            ConsoleKey.UpArrow => NavigateHistory(state, -1),
            ConsoleKey.DownArrow => NavigateHistory(state, 1),
            ConsoleKey.D1 => FocusPanel(state, TerminalPanelCatalog.Explorer, "Explorer focused."),
            ConsoleKey.D2 => FocusPanel(state, TerminalPanelCatalog.Conversation, "Conversation focused."),
            ConsoleKey.D3 => FocusPanel(state, TerminalPanelCatalog.Activity, "Activity focused."),
            ConsoleKey.D4 => FocusPanel(state, TerminalPanelCatalog.Commands, "Command palette focused."),
            ConsoleKey.D5 => FocusPanel(state, TerminalPanelCatalog.Inspector, "Inspector focused."),
            _ => TerminalInputResult.Noop(),
        };
    }

    private TerminalInputResult HandleCommandPaletteKey(ConsoleKeyInfo keyInfo, TerminalConversationState state)
    {
        IReadOnlyList<TerminalPaletteItem> filtered = CurrentPaletteItems(state);

        switch (keyInfo.Key)
        {
            case ConsoleKey.Escape:
                state.CloseCommandPalette();
                ApplyMetadata("Command palette closed.");
                return TerminalInputResult.Changed("Command palette closed.");
            case ConsoleKey.Enter:
                if (filtered.Count == 0)
                {
                    return TerminalInputResult.Changed("No command matches the current filter.");
                }

                TerminalPaletteItem selected = filtered[Math.Clamp(state.CommandPalette.SelectedIndex, 0, filtered.Count - 1)];
                state.CloseCommandPalette();
                ApplyMetadata($"palette selected: {selected.CommandText}");
                return TerminalInputResult.Submit(selected.CommandText);
            case ConsoleKey.UpArrow:
                state.MoveCommandPaletteSelection(-1, filtered.Count);
                ApplyMetadata(BuildPaletteMetadata(state, filtered.Count));
                return TerminalInputResult.Changed(string.Empty);
            case ConsoleKey.DownArrow:
                state.MoveCommandPaletteSelection(1, filtered.Count);
                ApplyMetadata(BuildPaletteMetadata(state, filtered.Count));
                return TerminalInputResult.Changed(string.Empty);
            case ConsoleKey.Backspace:
                if (state.CommandPalette.Query.Length == 0)
                {
                    return TerminalInputResult.Changed(string.Empty);
                }

                state.SetCommandPaletteQuery(state.CommandPalette.Query[..^1]);
                ApplyMetadata(BuildPaletteMetadata(state, CurrentPaletteItems(state).Count));
                return TerminalInputResult.Changed(string.Empty);
            case ConsoleKey.P when (keyInfo.Modifiers & ConsoleModifiers.Control) != 0:
                state.MoveCommandPaletteSelection(-1, filtered.Count);
                ApplyMetadata(BuildPaletteMetadata(state, filtered.Count));
                return TerminalInputResult.Changed(string.Empty);
            case ConsoleKey.N when (keyInfo.Modifiers & ConsoleModifiers.Control) != 0:
                state.MoveCommandPaletteSelection(1, filtered.Count);
                ApplyMetadata(BuildPaletteMetadata(state, filtered.Count));
                return TerminalInputResult.Changed(string.Empty);
            default:
                if (!char.IsControl(keyInfo.KeyChar))
                {
                    state.SetCommandPaletteQuery(state.CommandPalette.Query + keyInfo.KeyChar);
                    ApplyMetadata(BuildPaletteMetadata(state, CurrentPaletteItems(state).Count));
                    return TerminalInputResult.Changed(string.Empty);
                }

                return TerminalInputResult.Noop();
        }
    }

    private TerminalInputResult HandleStandardKey(ConsoleKeyInfo keyInfo, TerminalConversationState state)
    {
        switch (keyInfo.Key)
        {
            case ConsoleKey.Enter:
                string submitted = Buffer.Trim();
                state.TrackInput(submitted);
                Clear();
                return TerminalInputResult.Submit(submitted);
            case ConsoleKey.UpArrow:
                return NavigateHistory(state, -1);
            case ConsoleKey.DownArrow:
                return NavigateHistory(state, 1);
            case ConsoleKey.PageUp:
                return ScrollConversation(state, 8, "Scrolled conversation up.");
            case ConsoleKey.PageDown:
                return ScrollConversation(state, -8, "Scrolled conversation down.");
            case ConsoleKey.LeftArrow:
                return MoveCursorBy(-1);
            case ConsoleKey.RightArrow:
                return MoveCursorBy(1);
            case ConsoleKey.Home:
                return MoveCursorTo(0);
            case ConsoleKey.End:
                return MoveCursorTo(Buffer.Length);
            case ConsoleKey.Backspace:
                return Backspace();
            case ConsoleKey.Delete:
                return DeleteForward();
            case ConsoleKey.Escape:
                Clear();
                return TerminalInputResult.Changed("Input cleared.");
            default:
                if (!char.IsControl(keyInfo.KeyChar))
                {
                    Insert(keyInfo.KeyChar);
                    return TerminalInputResult.Changed(string.Empty);
                }

                return TerminalInputResult.Noop();
        }
    }

    private TerminalInputResult HandleReverseSearchKey(ConsoleKeyInfo keyInfo, TerminalConversationState state)
    {
        switch (keyInfo.Key)
        {
            case ConsoleKey.Enter:
                ExitReverseSearch(restoreOriginalBuffer: false);
                return TerminalInputResult.Changed("Reverse search accepted.");
            case ConsoleKey.Escape:
                ExitReverseSearch(restoreOriginalBuffer: true);
                return TerminalInputResult.Changed("Reverse search cancelled.");
            case ConsoleKey.Backspace:
                if (_reverseSearchQuery.Length > 0)
                {
                    _reverseSearchQuery = _reverseSearchQuery[..^1];
                    _reverseSearchOffset = 0;
                    ApplyReverseSearch(state);
                    return TerminalInputResult.Changed(string.Empty);
                }

                ExitReverseSearch(restoreOriginalBuffer: true);
                return TerminalInputResult.Changed("Reverse search cancelled.");
            case ConsoleKey.UpArrow:
                _reverseSearchOffset++;
                ApplyReverseSearch(state);
                return TerminalInputResult.Changed(string.Empty);
            case ConsoleKey.DownArrow:
                _reverseSearchOffset = Math.Max(0, _reverseSearchOffset - 1);
                ApplyReverseSearch(state);
                return TerminalInputResult.Changed(string.Empty);
            case ConsoleKey.R when (keyInfo.Modifiers & ConsoleModifiers.Control) != 0:
                _reverseSearchOffset++;
                ApplyReverseSearch(state);
                return TerminalInputResult.Changed(string.Empty);
            default:
                if (!char.IsControl(keyInfo.KeyChar))
                {
                    _reverseSearchQuery += keyInfo.KeyChar;
                    _reverseSearchOffset = 0;
                    ApplyReverseSearch(state);
                    return TerminalInputResult.Changed(string.Empty);
                }

                return TerminalInputResult.Noop();
        }
    }

    private TerminalInputResult BeginOrAdvanceReverseSearch(TerminalConversationState state)
    {
        if (!_isReverseSearchActive)
        {
            _isReverseSearchActive = true;
            _reverseSearchQuery = string.Empty;
            _reverseSearchOffset = 0;
            _reverseSearchOriginalBuffer = Buffer;
            ApplyMetadata("reverse-i-search: type to filter recent prompts and commands");
            return TerminalInputResult.Changed("Reverse search started.");
        }

        _reverseSearchOffset++;
        ApplyReverseSearch(state);
        return TerminalInputResult.Changed(string.Empty);
    }

    private void ApplyReverseSearch(TerminalConversationState state)
    {
        if (string.IsNullOrEmpty(_reverseSearchQuery))
        {
            Buffer = _reverseSearchOriginalBuffer;
            CursorIndex = Buffer.Length;
            ApplyMetadata("reverse-i-search: type to filter recent prompts and commands");
            return;
        }

        IReadOnlyList<string> matches = state.InputHistory
            .Where(entry => entry.Contains(_reverseSearchQuery, StringComparison.OrdinalIgnoreCase))
            .Reverse()
            .ToList();

        if (matches.Count == 0)
        {
            Buffer = _reverseSearchOriginalBuffer;
            CursorIndex = Buffer.Length;
            ApplyMetadata($"reverse-i-search `{_reverseSearchQuery}`: no match");
            return;
        }

        string match = matches[Math.Min(_reverseSearchOffset, matches.Count - 1)];
        Buffer = match;
        CursorIndex = Buffer.Length;
        ApplyMetadata($"reverse-i-search `{_reverseSearchQuery}`: {Summarize(match, 72)}");
    }

    private void ExitReverseSearch(bool restoreOriginalBuffer)
    {
        if (restoreOriginalBuffer)
        {
            Buffer = _reverseSearchOriginalBuffer;
            CursorIndex = Buffer.Length;
        }

        _isReverseSearchActive = false;
        _reverseSearchQuery = string.Empty;
        _reverseSearchOffset = 0;
        _reverseSearchOriginalBuffer = string.Empty;
        Metadata = string.Empty;
    }

    private TerminalInputResult NavigateHistory(TerminalConversationState state, int direction)
    {
        if (state.InputHistory.Count == 0)
        {
            return TerminalInputResult.Changed("No input history yet.");
        }

        if (_historyIndex is null)
        {
            _historyDraft = Buffer;
            _historyIndex = direction < 0 ? state.InputHistory.Count - 1 : 0;
        }
        else
        {
            _historyIndex = Math.Clamp(_historyIndex.Value + direction, 0, state.InputHistory.Count);
        }

        if (_historyIndex >= state.InputHistory.Count)
        {
            _historyIndex = null;
            Buffer = _historyDraft;
            CursorIndex = Buffer.Length;
            ApplyMetadata("Returned to the current draft.");
            return TerminalInputResult.Changed(string.Empty);
        }

        Buffer = state.InputHistory[_historyIndex.Value];
        CursorIndex = Buffer.Length;
        ApplyMetadata($"history {_historyIndex.Value + 1}/{state.InputHistory.Count}");
        return TerminalInputResult.Changed(string.Empty);
    }

    private TerminalInputResult Complete(TerminalConversationState state, bool reverse)
    {
        CompletionResult completion;

        if (Buffer.StartsWith("/", StringComparison.Ordinal))
        {
            completion = TryCompleteSlashCommand(reverse);
            if (!completion.HasValue)
            {
                completion = TryCompleteFocusPath(reverse);
            }
        }
        else
        {
            completion = TryCompletePathToken(reverse);
        }

        if (!completion.HasValue)
        {
            ApplyMetadata("No completion available.");
            return TerminalInputResult.Changed(string.Empty);
        }

        Buffer = completion.Buffer;
        CursorIndex = Buffer.Length;
        ApplyMetadata(completion.Metadata);
        if (completion.PanelToFocus is not null)
        {
            state.DismissLaunchpad();
            state.SetActivePanel(completion.PanelToFocus);
        }

        return TerminalInputResult.Changed(string.Empty);
    }

    private CompletionResult TryCompleteSlashCommand(bool reverse)
    {
        if (Buffer.Contains(' ', StringComparison.Ordinal))
        {
            return CompletionResult.Empty;
        }

        var current = Buffer;
        IReadOnlyList<string> candidates = TerminalCommandCatalog.GetCommandNames()
            .Where(command => command.StartsWith(current, StringComparison.OrdinalIgnoreCase))
            .OrderBy(static command => command, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return ApplyCandidates(current, string.Empty, candidates, reverse, addTrailingSpace: true);
    }

    private CompletionResult TryCompleteFocusPath(bool reverse)
    {
        const string prefix = "/focus ";
        if (!Buffer.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return CompletionResult.Empty;
        }

        var partialPath = Buffer[prefix.Length..];
        IReadOnlyList<string> candidates = GetPathCandidates(partialPath);
        return ApplyCandidates(partialPath, prefix, candidates, reverse, addTrailingSpace: false, panelToFocus: TerminalPanelCatalog.Explorer);
    }

    private CompletionResult TryCompletePathToken(bool reverse)
    {
        int start = Buffer.LastIndexOf(' ');
        string token = start < 0 ? Buffer : Buffer[(start + 1)..];
        if (string.IsNullOrWhiteSpace(token) || !(token.Contains('/') || token.Contains('.') || token.Contains('_') || token.Contains('-')))
        {
            return CompletionResult.Empty;
        }

        string prefix = start < 0 ? string.Empty : Buffer[..(start + 1)];
        IReadOnlyList<string> candidates = GetPathCandidates(token);
        return ApplyCandidates(token, prefix, candidates, reverse, addTrailingSpace: false, panelToFocus: TerminalPanelCatalog.Explorer);
    }

    private CompletionResult ApplyCandidates(string currentToken, string prefix, IReadOnlyList<string> candidates, bool reverse, bool addTrailingSpace, string? panelToFocus = null)
    {
        if (candidates.Count == 0)
        {
            _lastCompletionCandidates = [];
            return CompletionResult.Empty;
        }

        IReadOnlyList<string> orderedCandidates = reverse ? candidates.Reverse().ToList() : candidates;
        string completedToken;
        string metadata;

        if (_lastCompletionCandidates.SequenceEqual(orderedCandidates, StringComparer.OrdinalIgnoreCase))
        {
            completedToken = orderedCandidates[0];
            metadata = BuildCandidateMetadata(orderedCandidates);
        }
        else if (orderedCandidates.Count == 1)
        {
            completedToken = orderedCandidates[0];
            metadata = $"completed: {orderedCandidates[0]}";
        }
        else
        {
            string shared = FindCommonPrefix(orderedCandidates);
            completedToken = shared.Length > currentToken.Length ? shared : currentToken;
            metadata = BuildCandidateMetadata(orderedCandidates);
        }

        _lastCompletionCandidates = orderedCandidates;
        string suffix = addTrailingSpace && !completedToken.EndsWith(' ') ? " " : string.Empty;
        return new CompletionResult(true, prefix + completedToken + suffix, metadata, panelToFocus);
    }

    private IReadOnlyList<string> GetPathCandidates(string partialPath)
    {
        string normalized = partialPath.Replace('\\', '/');
        var directoryPart = string.Empty;
        var searchPart = normalized;
        int slashIndex = normalized.LastIndexOf('/');
        if (slashIndex >= 0)
        {
            directoryPart = normalized[..(slashIndex + 1)];
            searchPart = normalized[(slashIndex + 1)..];
        }

        string directoryOnDisk = Path.Combine(_workspaceRoot, directoryPart.Replace('/', Path.DirectorySeparatorChar));
        if (!Directory.Exists(directoryOnDisk))
        {
            return [];
        }

        IEnumerable<string> directories = Directory.EnumerateDirectories(directoryOnDisk)
            .Select(path => Path.GetFileName(path) + "/");
        IEnumerable<string> files = Directory.EnumerateFiles(directoryOnDisk)
            .Select(static path => Path.GetFileName(path)!);

        return directories
            .Concat(files)
            .Where(name => name.StartsWith(searchPart, StringComparison.OrdinalIgnoreCase))
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .Select(name => directoryPart + name)
            .ToList();
    }

    private static string FindCommonPrefix(IReadOnlyList<string> candidates)
    {
        if (candidates.Count == 0)
        {
            return string.Empty;
        }

        var prefix = candidates[0];
        for (int index = 1; index < candidates.Count; index++)
        {
            int max = Math.Min(prefix.Length, candidates[index].Length);
            var match = 0;
            while (match < max && char.ToUpperInvariant(prefix[match]) == char.ToUpperInvariant(candidates[index][match]))
            {
                match++;
            }

            prefix = prefix[..match];
            if (prefix.Length == 0)
            {
                break;
            }
        }

        return prefix;
    }

    private static string BuildCandidateMetadata(IReadOnlyList<string> candidates)
        => candidates.Count switch
        {
            0 => string.Empty,
            <= 4 => "completions: " + string.Join(" · ", candidates),
            _ => "completions: " + string.Join(" · ", candidates.Take(4)) + $" · +{candidates.Count - 4} more"
        };

    private TerminalInputResult PrefillFocusCommand(TerminalConversationState state)
    {
        state.DismissLaunchpad();
        state.CloseCommandPalette();
        Buffer = "/focus ";
        CursorIndex = Buffer.Length;
        state.SetActivePanel(TerminalPanelCatalog.Explorer);
        ApplyMetadata("Focus the explorer on a file or folder, then press Enter.");
        return TerminalInputResult.Changed("Focus command ready.");
    }

    private TerminalInputResult OpenCommandPalette(TerminalConversationState state)
    {
        state.DismissLaunchpad();
        state.OpenCommandPalette();
        state.SetSidebarMode(TerminalSidebarModeCatalog.Commands);
        ApplyMetadata(BuildPaletteMetadata(state, CurrentPaletteItems(state).Count));
        return TerminalInputResult.Changed("Command palette opened.");
    }

    private TerminalInputResult ShowSidebarOverview(TerminalConversationState state)
    {
        state.DismissLaunchpad();
        state.SetSidebarMode(TerminalSidebarModeCatalog.Overview);
        ApplyMetadata("Sidebar switched to overview.");
        return TerminalInputResult.Changed("Sidebar switched to overview.");
    }

    private TerminalInputResult FocusPanel(TerminalConversationState state, string panelId, string status)
    {
        state.DismissLaunchpad();
        state.CloseCommandPalette();
        state.SetActivePanel(panelId);
        ApplyMetadata($"Active panel: {panelId}");
        return TerminalInputResult.Changed(status);
    }

    private TerminalInputResult ScrollConversation(TerminalConversationState state, int offset, string status)
    {
        state.DismissLaunchpad();
        state.SetActivePanel(TerminalPanelCatalog.Conversation);
        state.ScrollConversation(offset);
        ApplyMetadata($"conversation scroll offset: {state.ConversationScrollOffset}");
        return TerminalInputResult.Changed(status);
    }

    private TerminalInputResult Backspace()
    {
        if (CursorIndex == 0)
        {
            return TerminalInputResult.Noop();
        }

        Buffer = Buffer.Remove(CursorIndex - 1, 1);
        CursorIndex--;
        _historyIndex = null;
        Metadata = string.Empty;
        _lastCompletionCandidates = [];
        return TerminalInputResult.Changed(string.Empty);
    }

    private TerminalInputResult DeleteForward()
    {
        if (CursorIndex >= Buffer.Length)
        {
            return TerminalInputResult.Noop();
        }

        Buffer = Buffer.Remove(CursorIndex, 1);
        _historyIndex = null;
        Metadata = string.Empty;
        _lastCompletionCandidates = [];
        return TerminalInputResult.Changed(string.Empty);
    }

    private TerminalInputResult MoveCursorBy(int offset)
        => MoveCursorTo(CursorIndex + offset);

    private TerminalInputResult MoveCursorTo(int target)
    {
        CursorIndex = Math.Clamp(target, 0, Buffer.Length);
        return TerminalInputResult.Changed(string.Empty);
    }

    private void Insert(char value)
    {
        Buffer = Buffer.Insert(CursorIndex, value.ToString());
        CursorIndex++;
        _historyIndex = null;
        Metadata = string.Empty;
        _lastCompletionCandidates = [];
    }

    private void ApplyMetadata(string metadata)
    {
        Metadata = metadata;
    }

    private static IReadOnlyList<TerminalPaletteItem> CurrentPaletteItems(TerminalConversationState state)
        => TerminalCommandCatalog.FilterPaletteItems(
            TerminalCommandCatalog.BuildPaletteItems(state.CommandHistory, state.SavedSessions, state.FocusedPath, state.SidebarMode),
            state.CommandPalette.Query);

    private static string BuildPaletteMetadata(TerminalConversationState state, int itemCount)
        => itemCount <= 0
            ? $"palette `{state.CommandPalette.Query}` · no matches"
            : $"palette `{(string.IsNullOrWhiteSpace(state.CommandPalette.Query) ? "all" : state.CommandPalette.Query)}` · {itemCount} matches";

    private static string Summarize(string text, int maxLength)
        => text.Length <= maxLength ? text : text[..(maxLength - 1)] + "…";

    private readonly record struct CompletionResult(bool HasValue, string Buffer, string Metadata, string? PanelToFocus = null)
    {
        public static CompletionResult Empty => new(false, string.Empty, string.Empty);
    }
}

public readonly struct TerminalInputResult
{
    public TerminalInputResult(bool hasChanges, bool submitInput, bool endOfInput, bool forceFullRefresh, string submittedText, string statusMessage)
    {
        HasChanges = hasChanges;
        SubmitInput = submitInput;
        EndOfInput = endOfInput;
        ForceFullRefresh = forceFullRefresh;
        SubmittedText = submittedText;
        StatusMessage = statusMessage;
    }

    public bool HasChanges { get; }

    public bool SubmitInput { get; }

    public bool EndOfInput { get; }

    public bool ForceFullRefresh { get; }

    public string SubmittedText { get; }

    public string StatusMessage { get; }

    public bool IsNoop => !HasChanges && !SubmitInput && !EndOfInput && !ForceFullRefresh;

    public static TerminalInputResult Noop() => new(false, false, false, false, string.Empty, string.Empty);

    public static TerminalInputResult Changed(string statusMessage) => new(true, false, false, false, string.Empty, statusMessage);

    public static TerminalInputResult Submit(string input) => new(true, true, false, false, input, string.Empty);

    public static TerminalInputResult Refresh(string statusMessage) => new(true, false, false, true, string.Empty, statusMessage);

    public static TerminalInputResult EndInput() => new(true, false, true, false, string.Empty, "Input stream closed.");
}

