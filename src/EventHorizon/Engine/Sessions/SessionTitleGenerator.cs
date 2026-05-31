using System.Text;
using EventHorizon.Engine.Sessions;
using EventHorizon.Providers;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace EventHorizon.Engine.Sessions;

internal sealed class SessionTitleGenerator : ISessionTitleGenerator
{
    private const int MaxTranscriptCharacters = 800;
    private readonly IProviderResolutionService _providerResolutionService;
    private readonly IProviderChatClientFactory _chatClientFactory;
    private readonly ILogger<SessionTitleGenerator> _logger;

    public SessionTitleGenerator(
        IProviderResolutionService providerResolutionService,
        IProviderChatClientFactory chatClientFactory,
        ILogger<SessionTitleGenerator> logger)
    {
        _providerResolutionService = providerResolutionService;
        _chatClientFactory = chatClientFactory;
        _logger = logger;
    }

    public async Task<string?> TryGenerateAsync(SessionDocument document, CancellationToken cancellationToken)
    {
        if (document.IsTitleManuallyEdited)
        {
            return document.Name;
        }

        var excerpt = BuildExcerpt(document);
        if (string.IsNullOrWhiteSpace(excerpt))
        {
            return document.Name;
        }

        var resolved = _providerResolutionService.TryResolveForSession(document);
        if (resolved is null)
        {
            return BuildFallbackTitle(document);
        }

        try
        {
            var client = _chatClientFactory.CreateChatClient(resolved.Provider);
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, "Based on the following conversation excerpt, generate a concise title for this session. Requirements: Output only the title without any explanation. The title should be clear and to the point. For Chinese titles, aim for around 8 to 20 characters. Do not use quotation marks or include a 'Title:' prefix."),
                new(ChatRole.User, $"Session excerpt:\n{excerpt}"),
            };
            var response = await client.GetResponseAsync(messages, new ChatOptions { ModelId = resolved.Model }, cancellationToken).ConfigureAwait(false);
            var title = response.Text?.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                return BuildFallbackTitle(document);
            }

            return TrimTitle(title);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate AI session title for session {SessionId}.", document.Id);
            return BuildFallbackTitle(document);
        }
    }

    private static string BuildExcerpt(SessionDocument document)
    {
        var builder = new StringBuilder();
        foreach (var entry in document.Transcript.Take(6))
        {
            builder.Append(entry.Role).Append(": ").AppendLine(entry.Text.Trim());
            if (builder.Length >= MaxTranscriptCharacters)
            {
                break;
            }
        }

        var value = builder.ToString().Trim();
        return value.Length <= MaxTranscriptCharacters ? value : value[..MaxTranscriptCharacters];
    }

    private static string BuildFallbackTitle(SessionDocument document)
    {
        var firstUserMessage = document.Transcript.FirstOrDefault(static entry => string.Equals(entry.Role, "user", StringComparison.OrdinalIgnoreCase))?.Text;
        if (string.IsNullOrWhiteSpace(firstUserMessage))
        {
            return document.Name;
        }

        var normalized = string.Join(' ', firstUserMessage
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return TrimTitle(normalized.Length <= 20 ? normalized : normalized[..20]);
    }

    private static string TrimTitle(string title)
        => title.Trim().Trim('"', '\'', '“', '”');
}
