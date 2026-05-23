namespace EventHorizon.Configuration;

public sealed class AppOptions
{
    public string WorkspaceRoot { get; set; } = Directory.GetCurrentDirectory();

    public AgentOptions Agent { get; set; } = new();

    public ProviderOptions Provider { get; set; } = new();

    public ProtocolOptions Protocol { get; set; } = new();

    public PricingOptions Pricing { get; set; } = new();

    public ConversationOptions Conversation { get; set; } = new();

    public List<McpServerOptions> McpServers { get; set; } = [];
}
