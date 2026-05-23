namespace EventHorizon.Configuration;

public sealed class EffectiveCommandOptions
{
    public string Command { get; set; } = "chat";
    public string? Prompt { get; set; }
    public string? Url { get; set; }
    public string? WorkspaceRoot { get; set; }
    public string? ProviderType { get; set; }
    public string? Model { get; set; }
    public string? ConfigFile { get; set; }
}