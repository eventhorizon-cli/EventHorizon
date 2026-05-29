using System.Text.Json.Serialization;

namespace EventHorizon.Configuration;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AppOptions))]
[JsonSerializable(typeof(ProviderOptions))]
[JsonSerializable(typeof(McpServerOptions))]
[JsonSerializable(typeof(SkillCatalogOptions))]
[JsonSerializable(typeof(ImportedSkillOptions))]
[JsonSerializable(typeof(Conversations.ConversationSessionDocument))]
[JsonSerializable(typeof(List<Conversations.ConversationSessionSummary>))]
[JsonSerializable(typeof(Dictionary<string, Pricing.ModelPriceCatalog.ModelCatalogEntry>))]
internal partial class EventHorizonJsonContext : JsonSerializerContext
{
}
