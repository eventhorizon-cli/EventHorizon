using System.Text.Json.Serialization;

namespace EventHorizon.Configuration;

public sealed class AppOptions
{
    public AGUIOptions AGUI { get; set; } = new();

    public AgentOptions Agent { get; set; } = new();

    public ProviderOptions Provider { get; set; } = new();

    public string? CurrentDefaultProvider { get; set; }

    [JsonIgnore]
    public string? CurrentProvider { get; set; }

    public Dictionary<string, ProviderOptions> Providers { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public PricingOptions Pricing { get; set; } = new();

    public ConversationOptions Conversation { get; set; } = new();

    public List<McpServerOptions> McpServers { get; set; } = [];

    public SkillCatalogOptions Skills { get; set; } = new();
}
