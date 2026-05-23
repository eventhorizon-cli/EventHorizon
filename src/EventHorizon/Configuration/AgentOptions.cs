namespace EventHorizon.Configuration;

public sealed class AgentOptions
{
    public string Name { get; set; } = "EventHorizon";
    public string Description { get; set; } = "A coding agent built with Microsoft Agent Framework.";
    public bool EnableSkills { get; set; } = true;
    public bool EnableShell { get; set; } = true;
    public bool EnableMcpTools { get; set; } = true;
    public string[] AdditionalSystemPrompts { get; set; } = [];
}
