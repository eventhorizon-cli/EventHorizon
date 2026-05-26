using System.ClientModel;
using Azure.AI.OpenAI;
using Azure.Identity;
using Google.GenAI;
using Microsoft.Extensions.AI;
using OpenAI;

namespace EventHorizon.Providers;

public sealed class ProviderChatClientFactory : IProviderChatClientFactory
{
    public IChatClient CreateChatClient(Configuration.ProviderOptions options)
    {
        var providerType = options.Type?.Trim().ToLowerInvariant() ?? "openai";

        return providerType switch
        {
            "azure-openai" => CreateAzureOpenAiChatClient(options),
            "gemini" => CreateGeminiChatClient(options),
            "openai-compatible" => CreateOpenAiCompatibleChatClient(options),
            _ => CreateOpenAiChatClient(options),
        };
    }


    private static IChatClient CreateOpenAiChatClient(Configuration.ProviderOptions options)
    {
        var apiKey = options.ApiKey ?? throw new InvalidOperationException("OPENAI_API_KEY is required for the openai provider.");
        var model = options.Model ?? throw new InvalidOperationException("A model is required for the openai provider.");
        return new OpenAIClient(apiKey).GetChatClient(model).AsIChatClient();
    }

    private static IChatClient CreateOpenAiCompatibleChatClient(Configuration.ProviderOptions options)
    {
        var apiKey = options.ApiKey ?? "not-needed";
        var endpoint = options.Endpoint ?? throw new InvalidOperationException("An endpoint is required for the openai-compatible provider.");
        var model = options.Model ?? throw new InvalidOperationException("A model is required for the openai-compatible provider.");
        return new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions { Endpoint = new Uri(endpoint) }).GetChatClient(model).AsIChatClient();
    }

    private static IChatClient CreateAzureOpenAiChatClient(Configuration.ProviderOptions options)
    {
        var endpoint = options.Endpoint ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is required for the azure-openai provider.");
        var deployment = options.Deployment ?? options.Model ?? throw new InvalidOperationException("AZURE_OPENAI_DEPLOYMENT_NAME is required for the azure-openai provider.");
        var client = string.IsNullOrWhiteSpace(options.ApiKey)
            ? new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
            : new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(options.ApiKey));
        return client.GetChatClient(deployment).AsIChatClient();
    }

    private static IChatClient CreateGeminiChatClient(Configuration.ProviderOptions options)
    {
        var apiKey = options.ApiKey ?? throw new InvalidOperationException("GOOGLE_GENAI_API_KEY is required for the gemini provider.");
        var model = options.Model ?? throw new InvalidOperationException("A model is required for the gemini provider.");
        return new Client(vertexAI: false, apiKey: apiKey).AsIChatClient(model);
    }
}

