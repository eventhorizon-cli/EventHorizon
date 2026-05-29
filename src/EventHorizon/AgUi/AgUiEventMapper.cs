using System.Text.Json;
using EventHorizon.Diff;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace EventHorizon.AGUI;

public sealed class AGUIEventMapper
{
    private const int MaxToolPayloadLength = 4000;

    public AGUIEventEnvelope CreateRunStarted(AGUIRun run, string modelName, string? workingDirectory, JsonElement? options)
        => new()
        {
            Type = "runStarted",
            RunId = run.Id,
            ThreadId = run.ThreadId,
            Status = run.Status,
            Metadata = new
            {
                model = modelName,
                workingDirectory,
                options,
            },
        };

    public AGUIEventEnvelope CreateRunFinished(AGUIRun run, UsageDetails? usage, decimal? costUsd)
        => new()
        {
            Type = "runFinished",
            RunId = run.Id,
            ThreadId = run.ThreadId,
            Status = run.Status,
            Metadata = new
            {
                usage = usage is null
                    ? null
                    : new
                    {
                        input = usage.InputTokenCount ?? usage.InputTextTokenCount,
                        output = usage.OutputTokenCount ?? usage.OutputTextTokenCount,
                        total = usage.TotalTokenCount,
                    },
                costUsd,
            },
        };

    public AGUIEventEnvelope CreateRunFailed(AGUIRun run, string error)
        => new()
        {
            Type = "runError",
            RunId = run.Id,
            ThreadId = run.ThreadId,
            Status = run.Status,
            Error = error,
        };

    public AGUIEventEnvelope CreateError(AGUIRun run, string error)
        => new()
        {
            Type = "error",
            RunId = run.Id,
            ThreadId = run.ThreadId,
            Error = error,
        };

    public AGUIEventEnvelope CreateRunCancelled(AGUIRun run)
        => new()
        {
            Type = "runCancelled",
            RunId = run.Id,
            ThreadId = run.ThreadId,
            Status = run.Status,
        };

    public AGUIEventEnvelope CreateUserMessage(AGUIRun run)
        => new()
        {
            Type = "userMessage",
            RunId = run.Id,
            ThreadId = run.ThreadId,
            MessageId = $"msg_{run.Id}_user",
            Message = new AGUIMessageDescriptor($"msg_{run.Id}_user", "user", run.Task),
        };

    public AGUIEventEnvelope CreateStepStarted(AGUIRun run, string stepId, string title)
        => new()
        {
            Type = "stepStarted",
            RunId = run.Id,
            ThreadId = run.ThreadId,
            StepId = stepId,
            Text = title,
        };

    public AGUIEventEnvelope CreateStepCompleted(AGUIRun run, string stepId, string title)
        => new()
        {
            Type = "stepCompleted",
            RunId = run.Id,
            ThreadId = run.ThreadId,
            StepId = stepId,
            Text = title,
        };

    public AGUIEventEnvelope CreatePlanUpdated(AGUIRun run, IReadOnlyList<string> plan, IReadOnlyList<string> completed)
        => new()
        {
            Type = "plan.updated",
            RunId = run.Id,
            ThreadId = run.ThreadId,
            Metadata = new
            {
                plan,
                completed,
            },
        };

    public AGUIEventEnvelope CreateReasoningSummaryUpdated(AGUIRun run, AGUIReasoningSummary summary)
        => new()
        {
            Type = "reasoning.summary.updated",
            RunId = run.Id,
            ThreadId = run.ThreadId,
            Summary = summary,
        };

    public AGUIEventEnvelope CreateArtifactCreated(AGUIRun run, AGUIArtifactDescriptor artifact)
        => new()
        {
            Type = "artifactCreated",
            RunId = run.Id,
            ThreadId = run.ThreadId,
            ArtifactId = artifact.Id,
            Artifact = artifact,
        };

    public AGUIEventEnvelope CreateArtifactUpdated(AGUIRun run, AGUIArtifactDescriptor artifact)
        => new()
        {
            Type = "artifactUpdated",
            RunId = run.Id,
            ThreadId = run.ThreadId,
            ArtifactId = artifact.Id,
            Artifact = artifact,
        };

    internal IReadOnlyList<AGUIEventEnvelope> MapStreamingUpdate(AGUIRun run, AGUIRunExecutionContext context, AgentResponseUpdate update)
    {
        List<AGUIEventEnvelope> events = [];

        if (!string.IsNullOrEmpty(update.Text))
        {
            if (!context.AssistantMessageStarted)
            {
                context.AssistantMessageStarted = true;
                events.Add(new AGUIEventEnvelope
                {
                    Type = "textMessageStart",
                    RunId = run.Id,
                    ThreadId = run.ThreadId,
                    MessageId = context.AssistantMessageId,
                    Message = new AGUIMessageDescriptor(context.AssistantMessageId, "assistant", string.Empty),
                });
            }

            context.AssistantText.Append(update.Text);
            events.Add(new AGUIEventEnvelope
            {
                Type = "textMessageContent",
                RunId = run.Id,
                ThreadId = run.ThreadId,
                MessageId = context.AssistantMessageId,
                Delta = update.Text,
            });
        }

        foreach (var content in update.Contents)
        {
            if (content is UsageContent)
            {
                continue;
            }

            if (TryReadToolCall(content, out var callId, out var name, out var arguments))
            {
                var state = EnsureToolCallState(context, callId, name, arguments);
                if (!state.StartPublished)
                {
                    state.StartPublished = true;
                    events.Add(new AGUIEventEnvelope
                    {
                        Type = "toolCallStart",
                        RunId = run.Id,
                        ThreadId = run.ThreadId,
                        ToolCallId = state.Id,
                        ToolCallName = state.Name,
                        ToolCall = new AGUIToolCallDescriptor(state.Id, state.Name, state.Arguments, "running"),
                    });
                }

                if (!string.IsNullOrWhiteSpace(arguments))
                {
                    events.Add(new AGUIEventEnvelope
                    {
                        Type = "toolCallArgs",
                        RunId = run.Id,
                        ThreadId = run.ThreadId,
                        ToolCallId = state.Id,
                        ToolCallName = state.Name,
                        Text = arguments,
                        ToolCall = new AGUIToolCallDescriptor(state.Id, state.Name, state.Arguments, "running"),
                    });
                }

                continue;
            }

            if (TryReadToolResult(content, out callId, out name, out var result))
            {
                var state = EnsureToolCallState(context, callId, name, null);
                if (!state.StartPublished)
                {
                    state.StartPublished = true;
                    events.Add(new AGUIEventEnvelope
                    {
                        Type = "toolCallStart",
                        RunId = run.Id,
                        ThreadId = run.ThreadId,
                        ToolCallId = state.Id,
                        ToolCallName = state.Name,
                        ToolCall = new AGUIToolCallDescriptor(state.Id, state.Name, state.Arguments, "running"),
                    });
                }

                if (!state.ResultPublished)
                {
                    state.ResultPublished = true;
                    events.Add(new AGUIEventEnvelope
                    {
                        Type = "toolCallResult",
                        RunId = run.Id,
                        ThreadId = run.ThreadId,
                        ToolCallId = state.Id,
                        ToolCallName = state.Name,
                        Result = Truncate(result),
                        ToolCall = new AGUIToolCallDescriptor(state.Id, state.Name, state.Arguments, "completed", Truncate(result)),
                    });
                    events.Add(new AGUIEventEnvelope
                    {
                        Type = "toolCallEnd",
                        RunId = run.Id,
                        ThreadId = run.ThreadId,
                        ToolCallId = state.Id,
                        ToolCallName = state.Name,
                        ToolCall = new AGUIToolCallDescriptor(state.Id, state.Name, state.Arguments, "completed", Truncate(result)),
                    });
                }
            }
        }

        return events;
    }

    internal IReadOnlyList<AGUIEventEnvelope> CompleteAssistantMessage(AGUIRun run, AGUIRunExecutionContext context)
    {
        if (!context.AssistantMessageStarted)
        {
            return [];
        }

        return
        [
            new AGUIEventEnvelope
            {
                Type = "textMessageEnd",
                RunId = run.Id,
                ThreadId = run.ThreadId,
                MessageId = context.AssistantMessageId,
                Text = context.AssistantText.ToString(),
                Message = new AGUIMessageDescriptor(context.AssistantMessageId, "assistant", context.AssistantText.ToString()),
            }
        ];
    }

    internal IReadOnlyList<AGUIEventEnvelope> CompleteOpenToolCalls(AGUIRun run, AGUIRunExecutionContext context, string error)
    {
        List<AGUIEventEnvelope> events = [];
        foreach (var state in context.ToolCalls.Values.Where(static state => state.StartPublished && !state.ResultPublished))
        {
            events.Add(new AGUIEventEnvelope
            {
                Type = "toolCallFailed",
                RunId = run.Id,
                ThreadId = run.ThreadId,
                ToolCallId = state.Id,
                ToolCallName = state.Name,
                Error = error,
            });
            events.Add(new AGUIEventEnvelope
            {
                Type = "toolCallEnd",
                RunId = run.Id,
                ThreadId = run.ThreadId,
                ToolCallId = state.Id,
                ToolCallName = state.Name,
                Error = error,
                ToolCall = new AGUIToolCallDescriptor(state.Id, state.Name, state.Arguments, "failed"),
            });
        }

        return events;
    }

    public AGUIArtifactDescriptor CreateChangesArtifact(IReadOnlyList<FileChange> changes)
        => new(
            Id: $"artifact_changes_{Guid.NewGuid():N}",
            Kind: "changes",
            Label: "Workspace changes",
            Path: null,
            Metadata: new
            {
                count = changes.Count,
                files = changes,
            });

    private static AGUIRunExecutionContext.ToolCallState EnsureToolCallState(AGUIRunExecutionContext context, string callId, string name, string? arguments)
    {
        if (!context.ToolCalls.TryGetValue(callId, out var state))
        {
            state = new AGUIRunExecutionContext.ToolCallState
            {
                Id = callId,
                Name = name,
                Arguments = arguments,
            };
            context.ToolCalls[callId] = state;
        }
        else if (!string.IsNullOrWhiteSpace(arguments))
        {
            state.Arguments = arguments;
        }

        return state;
    }

    private static bool TryReadToolCall(object content, out string callId, out string name, out string? arguments)
    {
        var typeName = content.GetType().Name;
        if (!typeName.Contains("FunctionCall", StringComparison.OrdinalIgnoreCase) &&
            !typeName.Contains("ToolCall", StringComparison.OrdinalIgnoreCase))
        {
            callId = string.Empty;
            name = string.Empty;
            arguments = null;
            return false;
        }

        callId = ReadString(content, "CallId") ?? $"tool_{Guid.NewGuid():N}";
        name = ReadString(content, "Name") ?? ReadString(content, "FunctionName") ?? "tool";
        arguments = SerializeValue(ReadProperty(content, "Arguments") ?? ReadProperty(content, "Input") ?? ReadProperty(content, "ArgumentsJson"));
        return true;
    }

    private static bool TryReadToolResult(object content, out string callId, out string name, out string? result)
    {
        var typeName = content.GetType().Name;
        if (!typeName.Contains("FunctionResult", StringComparison.OrdinalIgnoreCase) &&
            !typeName.Contains("ToolResult", StringComparison.OrdinalIgnoreCase))
        {
            callId = string.Empty;
            name = string.Empty;
            result = null;
            return false;
        }

        callId = ReadString(content, "CallId") ?? $"tool_{Guid.NewGuid():N}";
        name = ReadString(content, "Name") ?? ReadString(content, "FunctionName") ?? callId;
        result = SerializeValue(ReadProperty(content, "Result") ?? ReadProperty(content, "Text") ?? ReadProperty(content, "Output") ?? ReadProperty(content, "Value"));
        return true;
    }

    private static object? ReadProperty(object source, string propertyName)
        => source.GetType().GetProperty(propertyName)?.GetValue(source);

    private static string? ReadString(object source, string propertyName)
        => ReadProperty(source, propertyName) as string;

    private static string? SerializeValue(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is string text)
        {
            return Truncate(text);
        }

        if (value is JsonElement element)
        {
            return Truncate(element.GetRawText());
        }

        try
        {
            return Truncate(JsonSerializer.Serialize(value));
        }
        catch
        {
            return Truncate(value.ToString());
        }
    }

    private static string? Truncate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length <= MaxToolPayloadLength)
        {
            return value;
        }

        return value[..(MaxToolPayloadLength - 1)] + "…";
    }
}

