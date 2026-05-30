namespace EventHorizon.Configuration;

public sealed class AppOptions
{
    public AGUIOptions AGUI { get; set; } = new();

    public AgentOptions Agent { get; set; } = new();

    public ProviderOptions Provider { get; set; } = new();

    public PricingOptions Pricing { get; set; } = new();

    public ConversationOptions Conversation { get; set; } = new();
}
