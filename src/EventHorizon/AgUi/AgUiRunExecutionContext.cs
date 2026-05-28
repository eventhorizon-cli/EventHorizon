using System.Text;

namespace EventHorizon.AGUI;

internal sealed class AGUIRunExecutionContext
{
    public string AssistantMessageId { get; } = $"msg_{Guid.NewGuid():N}";

    public string ExecutionStepId { get; } = $"step_{Guid.NewGuid():N}";

    public StringBuilder AssistantText { get; } = new();

    public Dictionary<string, ToolCallState> ToolCalls { get; } = new(StringComparer.Ordinal);

    public bool AssistantMessageStarted { get; set; }

    public sealed class ToolCallState
    {
        public required string Id { get; init; }

        public required string Name { get; init; }

        public string? Arguments { get; set; }

        public bool StartPublished { get; set; }

        public bool ResultPublished { get; set; }
    }
}

