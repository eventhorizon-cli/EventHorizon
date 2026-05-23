using EventHorizon.Execution;

namespace EventHorizon.Tests.Streaming;

public sealed class StreamingActivityInspectorTests
{
    [Fact]
    public void InspectContents_Emits_Tool_Call_And_Result_Events()
    {
        object[] contents =
        [
            new FakeFunctionCallContent
            {
                Name = "read_file",
                Arguments = "{\"path\":\"src/EventHorizon/Program.cs\"}"
            },
            new FakeFunctionResultContent
            {
                Name = "read_file",
                Result = "public static async Task<int> RunAsync(string[] args)"
            }
        ];

        QueryEvent[] events = StreamingActivityInspector.InspectContents(contents).ToArray();

        Assert.Collection(
            events,
            evt =>
            {
                Assert.Equal(QueryEventKind.ToolCall, evt.Kind);
                Assert.Contains("read_file", evt.Text, StringComparison.Ordinal);
            },
            evt =>
            {
                Assert.Equal(QueryEventKind.ToolResult, evt.Kind);
                Assert.Contains("read_file", evt.Text, StringComparison.Ordinal);
                Assert.Contains("RunAsync", evt.Text, StringComparison.Ordinal);
            });
    }

    private sealed class FakeFunctionCallContent
    {
        public string Name { get; init; } = string.Empty;

        public string Arguments { get; init; } = string.Empty;
    }

    private sealed class FakeFunctionResultContent
    {
        public string Name { get; init; } = string.Empty;

        public string Result { get; init; } = string.Empty;
    }
}

