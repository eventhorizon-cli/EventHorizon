using EventHorizon.Configuration;
using EventHorizon.Engine.Sessions;
using EventHorizon.Prompting;

namespace EventHorizon.Tests.Prompting;

public sealed class SystemPromptFactoryTests
{
    [Fact]
    public void Build_Produces_Structured_Sections_And_Additional_Guidance()
    {
        AgentOptions options = new()
        {
            Name = "EventHorizon",
            AdditionalSystemPrompts = ["Keep answers concise."]
        };
        SessionContextSnapshot snapshot = new(
            CurrentDate: "Today's date is 2026-05-20.",
            WorkspaceRoot: "/workspace/demo",
            WorkspaceSummary: "Top-level entries:\n- src/",
            GitStatus: "## main",
            ProjectInstructions: "[README.md]\nFollow the repo conventions.");
        var promptFactory = new SystemPromptFactory();
        var prompt = promptFactory.Build(options, snapshot);

        Assert.Contains("# Role", prompt, StringComparison.Ordinal);
        Assert.Contains("# Working style", prompt, StringComparison.Ordinal);
        Assert.Contains("# Session context", prompt, StringComparison.Ordinal);
        Assert.Contains("# Additional guidance", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("# Tooling contract", prompt, StringComparison.Ordinal);
        Assert.Contains("Keep answers concise.", prompt, StringComparison.Ordinal);
    }
}
