using System.Text.Json.Serialization;
using EventHorizon.Engine.Sessions;

namespace EventHorizon.Configuration;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AgentOptions))]
[JsonSerializable(typeof(PricingOptions))]
[JsonSerializable(typeof(ProvidersOptions))]
[JsonSerializable(typeof(McpOptions))]
[JsonSerializable(typeof(SkillsOptions))]
[JsonSerializable(typeof(ProviderOptions))]
[JsonSerializable(typeof(Dictionary<string, ProviderOptions>))]
[JsonSerializable(typeof(McpServerOptions))]
[JsonSerializable(typeof(List<McpServerOptions>))]
[JsonSerializable(typeof(ImportedSkillOptions))]
[JsonSerializable(typeof(List<ImportedSkillOptions>))]
[JsonSerializable(typeof(SessionDocument))]
[JsonSerializable(typeof(List<SessionSummary>))]
[JsonSerializable(typeof(Dictionary<string, Pricing.ModelPriceCatalog.ModelCatalogEntry>))]
internal partial class EventHorizonJsonContext : JsonSerializerContext
{
}
