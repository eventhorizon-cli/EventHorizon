namespace EventHorizon.Configuration;

public sealed class AppOptions
{
    public AGUIOptions AgUi { get; set; } = new();

    public AgentOptions Agent { get; set; } = new();

    public ProviderOptions Provider { get; set; } = new();

    public string? CurrentProvider { get; set; }

    public Dictionary<string, ProviderOptions> Providers { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public PricingOptions Pricing { get; set; } = new();

    public ConversationOptions Conversation { get; set; } = new();

    public List<McpServerOptions> McpServers { get; set; } = [];
}
