using EventHorizon.Diff;

namespace EventHorizon.AGUI;

public sealed class AGUICodeAgentEventMapper
{
    internal IReadOnlyList<AGUIEventEnvelope> CreateToolStartExtensions(AGUIRun run, AGUIRunExecutionContext.ToolCallState toolCall)
    {
        List<AGUIEventEnvelope> events = [];
        switch (toolCall.Name)
        {
            case "open_file":
            case "read_file":
                events.Add(CreateCustomEvent(run, "file.read", toolCall.Id, new
                {
                    toolCall.Name,
                    toolCall.Arguments,
                }));
                break;
            case "apply_patch":
            case "insert_edit_into_file":
                events.Add(CreateCustomEvent(run, "file.write", toolCall.Id, new
                {
                    toolCall.Name,
                    toolCall.Arguments,
                }));
                break;
            case "run_in_terminal":
                events.Add(CreateCustomEvent(run, "command.started", toolCall.Id, new
                {
                    toolCall.Name,
                    toolCall.Arguments,
                }));
                if (IsTestCommand(toolCall.Arguments))
                {
                    events.Add(CreateCustomEvent(run, "test.started", toolCall.Id, new
                    {
                        toolCall.Arguments,
                    }));
                }

                break;
        }

        return events;
    }

    internal IReadOnlyList<AGUIEventEnvelope> CreateToolResultExtensions(AGUIRun run, AGUIRunExecutionContext.ToolCallState toolCall, string? resultText)
    {
        List<AGUIEventEnvelope> events = [];
        switch (toolCall.Name)
        {
            case "run_in_terminal":
                if (!string.IsNullOrWhiteSpace(resultText))
                {
                    events.Add(CreateCustomEvent(run, "command.output", toolCall.Id, new
                    {
                        Output = Truncate(resultText, 4000),
                    }));
                }

                events.Add(CreateCustomEvent(run, "command.completed", toolCall.Id, new
                {
                    toolCall.Arguments,
                }));
                if (IsTestCommand(toolCall.Arguments))
                {
                    events.Add(CreateCustomEvent(run, "test.completed", toolCall.Id, new
                    {
                        Output = Truncate(resultText, 4000),
                    }));
                }

                break;
            case "get_errors":
                if (!string.IsNullOrWhiteSpace(resultText))
                {
                    events.Add(CreateCustomEvent(run, "diagnostic.created", toolCall.Id, new
                    {
                        Diagnostics = Truncate(resultText, 4000),
                    }));
                }

                break;
        }

        return events;
    }

    internal IReadOnlyList<AGUIEventEnvelope> CreateChangeEvents(AGUIRun run, IReadOnlyList<FileChange> changes)
    {
        List<AGUIEventEnvelope> events = [];
        foreach (var change in changes)
        {
            var type = change.Status switch
            {
                "added" => "file.created",
                "deleted" => "file.deleted",
                "renamed" => "file.renamed",
                _ => "file.modified",
            };
            events.Add(CreateCustomEvent(run, type, change.Path, new
            {
                runId = run.Id,
                sessionId = run.SessionId,
                path = change.Path,
                oldPath = change.OldPath,
                status = change.Status,
                additions = change.Additions,
                deletions = change.Deletions,
                binary = change.Binary,
            }));
        }

        return events;
    }

    internal AGUIEventEnvelope CreateDiffGenerated(AGUIRun run, IReadOnlyList<FileChange> changes)
        => CreateCustomEvent(run, "diff.generated", $"artifact_{run.Id}", new
        {
            runId = run.Id,
            sessionId = run.SessionId,
            count = changes.Count,
            additions = changes.Sum(static change => change.Additions),
            deletions = changes.Sum(static change => change.Deletions),
            files = changes.Select(static change => new
            {
                path = change.Path,
                oldPath = change.OldPath,
                status = change.Status,
                additions = change.Additions,
                deletions = change.Deletions,
                binary = change.Binary,
            }).ToArray(),
        });

    private static AGUIEventEnvelope CreateCustomEvent(AGUIRun run, string type, string id, object data)
        => new()
        {
            Type = type,
            RunId = run.Id,
            ThreadId = run.ThreadId,
            ArtifactId = id,
            Metadata = data,
        };

    private static bool IsTestCommand(string? arguments)
        => !string.IsNullOrWhiteSpace(arguments) &&
           arguments.Contains("dotnet test", StringComparison.OrdinalIgnoreCase);

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..(maxLength - 1)] + "…";
    }
}

