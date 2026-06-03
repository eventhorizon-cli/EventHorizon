using System.ClientModel;
using Azure.AI.OpenAI;
using Azure.Identity;
using EventHorizon.Configuration;
using Google.GenAI;
using Microsoft.Extensions.AI;
using OpenAI;

#pragma warning disable OPENAI001

namespace EventHorizon.Providers;

public sealed class ProviderChatClientFactory : IProviderChatClientFactory
{
    public IChatClient CreateChatClient(ProviderOptions options)
    {
        var providerType = ProviderTypes.Normalize(options.Type);

        return providerType switch
        {
            ProviderTypes.AzureOpenAiChatCompletions => CreateAzureOpenAiChatClient(options),
            ProviderTypes.AzureOpenAiResponses => CreateAzureOpenAiResponsesClient(options),
            ProviderTypes.Gemini => CreateGeminiChatClient(options),
            ProviderTypes.OpenAiChatCompletions => CreateOpenAiChatClient(options),
            ProviderTypes.OpenAiCompatibleChatCompletions => CreateOpenAiCompatibleChatClient(options),
            ProviderTypes.OpenAiCompatibleResponses => CreateOpenAiCompatibleResponsesClient(options),
            ProviderTypes.OpenAiResponses => CreateOpenAiResponsesClient(options),
            _ => throw new ArgumentOutOfRangeException(nameof(providerType), providerType, null)
        };
    }


    private static IChatClient CreateOpenAiChatClient(ProviderOptions options)
    {
        var apiKey = options.ApiKey ?? throw new InvalidOperationException("Provider.ApiKey is required for the openai provider.");
        var model = options.Model ?? throw new InvalidOperationException("A model is required for the openai provider.");
        return new OpenAIClient(apiKey).GetChatClient(model).AsIChatClient();
    }

    private static IChatClient CreateOpenAiResponsesClient(ProviderOptions options)
    {
        var apiKey = options.ApiKey ?? throw new InvalidOperationException("Provider.ApiKey is required for the openai provider.");
        var model = options.Model ?? throw new InvalidOperationException("A model is required for the openai provider.");
        return new OpenAIClient(apiKey).GetResponsesClient().AsIChatClient(model);
    }

    private static IChatClient CreateOpenAiCompatibleChatClient(ProviderOptions options)
    {
        var apiKey = options.ApiKey ?? "not-needed";
        var endpoint = options.Endpoint ?? throw new InvalidOperationException("An endpoint is required for the openai-compatible provider.");
        var model = options.Model ?? throw new InvalidOperationException("A model is required for the openai-compatible provider.");
        return new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions { Endpoint = new Uri(endpoint) }).GetChatClient(model).AsIChatClient();
    }

    private static IChatClient CreateOpenAiCompatibleResponsesClient(ProviderOptions options)
    {
        var apiKey = options.ApiKey ?? "not-needed";
        var endpoint = options.Endpoint ?? throw new InvalidOperationException("An endpoint is required for the openai-compatible provider.");
        var model = options.Model ?? throw new InvalidOperationException("A model is required for the openai-compatible provider.");
        return new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions { Endpoint = new Uri(endpoint) }).GetResponsesClient().AsIChatClient(model);
    }

    private static IChatClient CreateAzureOpenAiChatClient(ProviderOptions options)
    {
        var endpoint = options.Endpoint ?? throw new InvalidOperationException("Provider.Endpoint is required for the azure-openai provider.");
        var deployment = options.Deployment ?? options.Model ?? throw new InvalidOperationException("Provider.Deployment or Provider.Model is required for the azure-openai provider.");
        var client = string.IsNullOrWhiteSpace(options.ApiKey)
            ? new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
            : new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(options.ApiKey));
        return client.GetChatClient(deployment).AsIChatClient();
    }

    private static IChatClient CreateAzureOpenAiResponsesClient(ProviderOptions options)
    {
        var endpoint = options.Endpoint ?? throw new InvalidOperationException("Provider.Endpoint is required for the azure-openai provider.");
        var deployment = options.Deployment ?? options.Model ?? throw new InvalidOperationException("Provider.Deployment or Provider.Model is required for the azure-openai provider.");
        var client = string.IsNullOrWhiteSpace(options.ApiKey)
            ? new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
            : new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(options.ApiKey));
        return client.GetResponsesClient().AsIChatClient(deployment);
    }

    private static IChatClient CreateGeminiChatClient(ProviderOptions options)
    {
        var apiKey = options.ApiKey ?? throw new InvalidOperationException("Provider.ApiKey is required for the gemini provider.");
        var model = options.Model ?? throw new InvalidOperationException("A model is required for the gemini provider.");
        return new Client(vertexAI: false, apiKey: apiKey).AsIChatClient(model);
    }
}

#pragma warning restore OPENAI001

