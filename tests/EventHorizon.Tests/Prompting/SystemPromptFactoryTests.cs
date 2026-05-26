using EventHorizon.Configuration;
using EventHorizon.Context;
using EventHorizon.Prompting;
using EventHorizon.Tools;
using Microsoft.Extensions.AI;

namespace EventHorizon.Tests.Prompting;

public sealed class SystemPromptFactoryTests
{
    [Fact]
    public void Build_Produces_Structured_Sections_And_Tool_Metadata()
    {
        AppOptions options = new()
        {
            Agent = new AgentOptions
            {
                Name = "EventHorizon",
                AdditionalSystemPrompts = ["Keep answers concise."]
            }
        };
        SessionContextSnapshot snapshot = new(
            CurrentDate: "Today's date is 2026-05-20.",
            WorkspaceRoot: "/workspace/demo",
            WorkspaceSummary: "Top-level entries:\n- src/",
            GitStatus: "## main",
            ProjectInstructions: "[README.md]\nFollow the repo conventions.");
        ToolDescriptor[] tools =
        [
            new(
                "read_file",
                "Read a file from the workspace.",
                IsReadOnly: true,
                IsConcurrencySafe: true,
                AIFunctionFactory.Create(() => "ok", name: "read_file", description: "Read a file."))
        ];

        var promptFactory = new SystemPromptFactory();
        var prompt = promptFactory.Build(options, snapshot, tools);

        Assert.Contains("# Role", prompt, StringComparison.Ordinal);
        Assert.Contains("# Working style", prompt, StringComparison.Ordinal);
        Assert.Contains("# Session context", prompt, StringComparison.Ordinal);
        Assert.Contains("# Tooling contract", prompt, StringComparison.Ordinal);
        Assert.Contains("# Additional guidance", prompt, StringComparison.Ordinal);
        Assert.Contains("- read_file [readOnly=true, concurrencySafe=true]: Read a file from the workspace.", prompt, StringComparison.Ordinal);
        Assert.Contains("Keep answers concise.", prompt, StringComparison.Ordinal);
    }
}

