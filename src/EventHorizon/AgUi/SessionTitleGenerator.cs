using System.Text;
using EventHorizon.Conversations;
using EventHorizon.Providers;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace EventHorizon.AGUI;

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

    public async Task<string?> TryGenerateAsync(ConversationSessionDocument document, CancellationToken cancellationToken)
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
                new(ChatRole.System, "请根据下面这段对话内容，为这个会话生成一个简短标题。要求：只输出标题，不要解释。标题要简洁明确。中文标题控制在 8 到 20 个字左右。不要使用引号。不要包含“标题：”前缀。"),
                new(ChatRole.User, $"对话内容：\n{excerpt}"),
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

    private static string BuildExcerpt(ConversationSessionDocument document)
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

    private static string BuildFallbackTitle(ConversationSessionDocument document)
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

