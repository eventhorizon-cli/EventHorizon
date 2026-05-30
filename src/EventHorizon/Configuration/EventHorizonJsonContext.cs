using System.Text.Json.Serialization;

namespace EventHorizon.Configuration;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AppOptions))]
[JsonSerializable(typeof(ProvidersOptions))]
[JsonSerializable(typeof(McpOptions))]
[JsonSerializable(typeof(SkillsOptions))]
[JsonSerializable(typeof(ProviderOptions))]
[JsonSerializable(typeof(Dictionary<string, ProviderOptions>))]
[JsonSerializable(typeof(McpServerOptions))]
[JsonSerializable(typeof(List<McpServerOptions>))]
[JsonSerializable(typeof(ImportedSkillOptions))]
[JsonSerializable(typeof(List<ImportedSkillOptions>))]
[JsonSerializable(typeof(Conversations.ConversationSessionDocument))]
[JsonSerializable(typeof(List<Conversations.ConversationSessionSummary>))]
[JsonSerializable(typeof(Dictionary<string, Pricing.ModelPriceCatalog.ModelCatalogEntry>))]
internal partial class EventHorizonJsonContext : JsonSerializerContext
{
}
