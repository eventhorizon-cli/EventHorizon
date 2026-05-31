using EventHorizon.DTOs;
using EventHorizon.Providers;
using Microsoft.Extensions.AI;

namespace EventHorizon.Configuration;

internal sealed class ProviderTestingService : IProviderTestingService
{
    private readonly IProviderChatClientFactory _providerChatClientFactory;

    public ProviderTestingService(IProviderChatClientFactory providerChatClientFactory)
    {
        _providerChatClientFactory = providerChatClientFactory;
    }

    public async Task<ProviderTestResponseDTO> TestAsync(ProviderTestRequestDTO request, CancellationToken cancellationToken)
    {
        try
        {
            var provider = request.Provider;
            provider.Name ??= request.Name;
            var client = _providerChatClientFactory.CreateChatClient(provider);
            var response = await client.GetResponseAsync(
                [new ChatMessage(ChatRole.User, "Reply with: ok")],
                new ChatOptions { ModelId = provider.Model },
                cancellationToken).ConfigureAwait(false);
            var models = provider.Models.Count > 0
                ? provider.Models
                : string.IsNullOrWhiteSpace(provider.Model)
                    ? []
                    : [provider.Model];
            return new ProviderTestResponseDTO
            {
                Success = true,
                Message = string.IsNullOrWhiteSpace(response.Text) ? "Connection succeeded." : "Connection succeeded.",
                Models = models,
            };
        }
        catch (Exception ex)
        {
            return new ProviderTestResponseDTO
            {
                Success = false,
                Message = Sanitize(ex.Message),
                Models = Array.Empty<string>(),
            };
        }
    }

    private static string Sanitize(string message)
        => string.IsNullOrWhiteSpace(message)
            ? "Provider test failed."
            : message.Replace("api key", "credentials", StringComparison.OrdinalIgnoreCase);
}
