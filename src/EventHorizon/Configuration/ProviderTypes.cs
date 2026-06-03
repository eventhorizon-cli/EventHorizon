namespace EventHorizon.Configuration;

public static class ProviderTypes
{
    public const string OpenAi = "openai";

    public const string OpenAiChatCompletions = "openai-chat-completions";

    public const string OpenAiResponses = "openai-responses";

    public const string OpenAiCompatible = "openai-compatible";

    public const string OpenAiCompatibleChatCompletions = "openai-compatible-chat-completions";

    public const string OpenAiCompatibleResponses = "openai-compatible-responses";

    public const string AzureOpenAi = "azure-openai";

    public const string AzureOpenAiChatCompletions = "azure-openai-chat-completions";

    public const string AzureOpenAiResponses = "azure-openai-responses";

    public const string Anthropic = "anthropic";

    public const string Gemini = "gemini";

    public static string Normalize(string? providerType)
    {
        var normalized = string.IsNullOrWhiteSpace(providerType)
            ? OpenAiChatCompletions
            : providerType.Trim().ToLowerInvariant();

        return normalized switch
        {
            OpenAi => OpenAiChatCompletions,
            OpenAiCompatible => OpenAiCompatibleChatCompletions,
            AzureOpenAi => AzureOpenAiChatCompletions,
            _ => normalized,
        };
    }

    public static string? GetProviderFamily(string? providerType)
        => Normalize(providerType) switch
        {
            OpenAiChatCompletions or OpenAiResponses => OpenAi,
            OpenAiCompatibleChatCompletions or OpenAiCompatibleResponses => OpenAiCompatible,
            AzureOpenAiChatCompletions or AzureOpenAiResponses => AzureOpenAi,
            Anthropic => Anthropic,
            Gemini => Gemini,
            _ => null,
        };

    public static bool IsAzureOpenAi(string? providerType)
        => string.Equals(GetProviderFamily(providerType), AzureOpenAi, StringComparison.Ordinal);

    public static bool SupportsApiTypeSelection(string? providerType)
        => GetProviderFamily(providerType) is OpenAi or OpenAiCompatible or AzureOpenAi;

    public static bool UsesResponsesApi(string? providerType)
        => Normalize(providerType) is OpenAiResponses or OpenAiCompatibleResponses or AzureOpenAiResponses;

    public static bool IsSupported(string? providerType)
        => Normalize(providerType) is
            OpenAiChatCompletions or
            OpenAiResponses or
            OpenAiCompatibleChatCompletions or
            OpenAiCompatibleResponses or
            AzureOpenAiChatCompletions or
            AzureOpenAiResponses or
            Anthropic or
            Gemini;
}
