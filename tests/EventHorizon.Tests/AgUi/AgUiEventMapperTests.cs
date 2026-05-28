using System.Text.Json;
using EventHorizon.AGUI;
using Microsoft.Extensions.AI;

namespace EventHorizon.Tests.AGUI;

public sealed class AGUIEventMapperTests
{
    [Fact]
    public void CreateRunStarted_Uses_Official_Run_Event_Name_And_Metadata()
    {
        AGUIEventMapper mapper = new();
        var run = new AGUIRun
        {
            Id = "run_123",
            ThreadId = "thread_123",
            Task = "Fix the build",
            WorkingDirectory = "src/EventHorizon",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        run.MarkRunning(AGUIRunStates.Executing);

        var @event = mapper.CreateRunStarted(run, "gpt-4.1-mini", run.WorkingDirectory, JsonDocument.Parse("{\"mode\":\"safe\"}").RootElement.Clone());

        Assert.Equal("runStarted", @event.Type);
        Assert.Equal(run.Id, @event.RunId);
        Assert.Equal(run.ThreadId, @event.ThreadId);
        Assert.Equal(AGUIRunStates.Running, @event.Status);
        Assert.NotNull(@event.Metadata);
    }

    [Fact]
    public void CreateRunFinished_Includes_Usage_Metadata()
    {
        AGUIEventMapper mapper = new();
        var run = new AGUIRun
        {
            Id = "run_123",
            ThreadId = "thread_123",
            Task = "Fix the build",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        run.MarkCompleted();
        var usage = new UsageDetails
        {
            InputTokenCount = 11,
            OutputTokenCount = 7,
            TotalTokenCount = 18,
        };

        var @event = mapper.CreateRunFinished(run, usage, 0.0123m);

        Assert.Equal("runFinished", @event.Type);
        Assert.Equal(AGUIRunStates.Completed, @event.Status);
        Assert.NotNull(@event.Metadata);
    }

    [Fact]
    public void CreateReasoningSummaryUpdated_Does_Not_Expose_Internal_Chain_Of_Thought()
    {
        AGUIEventMapper mapper = new();
        var run = new AGUIRun
        {
            Id = "run_123",
            ThreadId = "thread_123",
            Task = "Fix the build",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var summary = new AGUIReasoningSummary(
            run.Task,
            ["Inspect the project", "Apply a targeted fix"],
            ["Loaded the workspace context"],
            "Run the tests.",
            [],
            ["Keep the summary high level."]);

        var @event = mapper.CreateReasoningSummaryUpdated(run, summary);

        Assert.Equal("reasoning.summary.updated", @event.Type);
        Assert.Same(summary, @event.Summary);
        Assert.Null(@event.Text);
    }
}


