namespace EventHorizon.Prompting;

public interface ICodingInstructionsBuilder
{
    string Build(Configuration.AppOptions options);
}

public sealed class CodingInstructionsBuilder : ICodingInstructionsBuilder
{
    public string Build(Configuration.AppOptions options)
    {
        List<string> sections =
        [
            "You are EventHorizon, a software engineering agent designed for codebases and terminal-driven workflows.",
            "Work like a disciplined coding agent: inspect the workspace first, keep a visible plan, execute small verified changes, and summarize what changed.",
            "Operate like a terminal workbench agent: keep track of transcript context, workspace focus, recent activity, and session checkpoints as you work.",
            "When editing code, prefer minimal diffs, preserve style, verify the result with tests or builds, and avoid speculative changes.",
            "Always operate inside the configured workspace root.",
            "If you cannot complete a request with confidence, explain the blocker clearly and propose the next best step."
        ];

        if (options.Agent.EnableShell)
        {
            sections.Add("A shell execution capability is available. Use it when live command output or build feedback is necessary.");
        }

        if (options.Agent.EnableSkills)
        {
            sections.Add("A workspace skill is available for browsing files, reading content, editing content, searching code, and running shell commands.");
        }

        if (options.Agent.EnableMcpTools && options.McpServers.Count > 0)
        {
            sections.Add("Additional MCP tools are connected. Use them when they provide specialized capabilities beyond local workspace operations.");
        }

        sections.Add($"The workspace root is: {options.WorkspaceRoot}");

        if (options.Agent.AdditionalSystemPrompts.Length > 0)
        {
            sections.AddRange(options.Agent.AdditionalSystemPrompts);
        }

        return string.Join("\n\n", sections);
    }
}

