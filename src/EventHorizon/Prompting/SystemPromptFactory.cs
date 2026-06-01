using System.Text;
using EventHorizon.Configuration;
using EventHorizon.Engine.Sessions;

namespace EventHorizon.Prompting;

public interface ISystemPromptFactory
{
    string Build(AgentOptions options, SessionContextSnapshot snapshot);
}

public sealed class SystemPromptFactory : ISystemPromptFactory
{
    public string Build(AgentOptions options, SessionContextSnapshot snapshot)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Role");
        builder.AppendLine($"You are {options.Name}, a software engineering agent for terminal-first coding workflows.");
        builder.AppendLine("You operate directly on the user's workspace through tools and should behave like a careful pair programmer.");
        builder.AppendLine();

        builder.AppendLine("# Working style");
        builder.AppendLine("- Work in explicit phases: inspect, plan, execute, verify, and summarize.");
        builder.AppendLine("- Prefer the smallest correct change set and keep edits easy to review.");
        builder.AppendLine("- When the codebase is unfamiliar, use read/search tools before write tools.");
        builder.AppendLine("- Do not invent files, symbols, or runtime behavior that you have not observed.");
        builder.AppendLine("- After making changes, verify with targeted commands or tests whenever practical.");
        builder.AppendLine("- Stay inside the configured workspace root and explain important trade-offs briefly.");
        builder.AppendLine();

        builder.AppendLine("# Session context");
        builder.AppendLine(snapshot.CurrentDate);
        builder.AppendLine($"Workspace root: {snapshot.WorkspaceRoot}");
        builder.AppendLine();
        builder.AppendLine("## Workspace snapshot");
        builder.AppendLine(snapshot.WorkspaceSummary);
        builder.AppendLine();
        builder.AppendLine("## Git snapshot");
        builder.AppendLine(snapshot.GitStatus);
        builder.AppendLine();
        builder.AppendLine("## Project guidance");
        builder.AppendLine(snapshot.ProjectInstructions);
        builder.AppendLine();


        if (options.AdditionalSystemPrompts.Length > 0)
        {
            builder.AppendLine();
            builder.AppendLine("# Additional guidance");
            foreach (var prompt in options.AdditionalSystemPrompts)
            {
                builder.Append("- ").AppendLine(prompt);
            }
        }

        return builder.ToString().TrimEnd();
    }
}

