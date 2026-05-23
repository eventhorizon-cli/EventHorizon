namespace EventHorizon.Configuration;

public sealed class ConversationOptions
{
    public string? StoragePath { get; set; }
    public bool AutoSave { get; set; } = true;
    public string AutoSaveSessionName { get; set; } = "last-session";
}