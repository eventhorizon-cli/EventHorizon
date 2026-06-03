using EventHorizon.Configuration;

namespace EventHorizon.Tests.Configuration;

public sealed class ProviderTypesTests
{
    [Theory]
    [InlineData(null, ProviderTypes.OpenAiChatCompletions)]
    [InlineData("", ProviderTypes.OpenAiChatCompletions)]
    [InlineData("openai", ProviderTypes.OpenAiChatCompletions)]
    [InlineData("openai-compatible", ProviderTypes.OpenAiCompatibleChatCompletions)]
    [InlineData("azure-openai", ProviderTypes.AzureOpenAiChatCompletions)]
    [InlineData(ProviderTypes.OpenAiResponses, ProviderTypes.OpenAiResponses)]
    [InlineData(ProviderTypes.OpenAiCompatibleResponses, ProviderTypes.OpenAiCompatibleResponses)]
    [InlineData(ProviderTypes.AzureOpenAiResponses, ProviderTypes.AzureOpenAiResponses)]
    public void Normalize_Returns_Expected_Type(string? providerType, string expected)
    {
        var normalized = ProviderTypes.Normalize(providerType);

        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData(ProviderTypes.OpenAiChatCompletions, ProviderTypes.OpenAi)]
    [InlineData(ProviderTypes.OpenAiResponses, ProviderTypes.OpenAi)]
    [InlineData(ProviderTypes.OpenAiCompatibleChatCompletions, ProviderTypes.OpenAiCompatible)]
    [InlineData(ProviderTypes.OpenAiCompatibleResponses, ProviderTypes.OpenAiCompatible)]
    [InlineData(ProviderTypes.AzureOpenAiChatCompletions, ProviderTypes.AzureOpenAi)]
    [InlineData(ProviderTypes.AzureOpenAiResponses, ProviderTypes.AzureOpenAi)]
    [InlineData(ProviderTypes.Anthropic, ProviderTypes.Anthropic)]
    [InlineData(ProviderTypes.Gemini, ProviderTypes.Gemini)]
    public void GetProviderFamily_Returns_Expected_Family(string providerType, string expected)
    {
        var family = ProviderTypes.GetProviderFamily(providerType);

        Assert.Equal(expected, family);
    }

    [Theory]
    [InlineData(ProviderTypes.OpenAiChatCompletions, false)]
    [InlineData(ProviderTypes.OpenAiResponses, true)]
    [InlineData(ProviderTypes.OpenAiCompatibleChatCompletions, false)]
    [InlineData(ProviderTypes.OpenAiCompatibleResponses, true)]
    [InlineData(ProviderTypes.AzureOpenAiChatCompletions, false)]
    [InlineData(ProviderTypes.AzureOpenAiResponses, true)]
    [InlineData(ProviderTypes.Anthropic, false)]
    public void UsesResponsesApi_Returns_Expected_Result(string providerType, bool expected)
    {
        var usesResponsesApi = ProviderTypes.UsesResponsesApi(providerType);

        Assert.Equal(expected, usesResponsesApi);
    }
}

