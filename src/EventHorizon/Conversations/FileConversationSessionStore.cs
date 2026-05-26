using System.Text.Json;

namespace EventHorizon.Conversations;

public sealed class FileConversationSessionStore : IConversationSessionStore
{
    private readonly string _storagePath;

    public FileConversationSessionStore(string storagePath)
    {
        _storagePath = Path.GetFullPath(storagePath);
        Directory.CreateDirectory(_storagePath);
    }

    public async Task SaveAsync(ConversationSessionDocument document, CancellationToken cancellationToken)
    {
        var path = GetPath(document.Id);
        var json = JsonSerializer.Serialize(document, Configuration.EventHorizonJsonContext.Default.ConversationSessionDocument);
        await File.WriteAllTextAsync(path, json, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ConversationSessionDocument?> LoadAsync(string sessionId, CancellationToken cancellationToken)
    {
        var path = GetPath(sessionId);
        if (!File.Exists(path))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize(json, Configuration.EventHorizonJsonContext.Default.ConversationSessionDocument);
    }

    public async Task<IReadOnlyList<ConversationSessionSummary>> ListAsync(CancellationToken cancellationToken)
    {
        List<ConversationSessionSummary> items = [];
        foreach (var file in Directory.EnumerateFiles(_storagePath, "*.json", SearchOption.TopDirectoryOnly))
        {
            var json = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
            var document = JsonSerializer.Deserialize(json, Configuration.EventHorizonJsonContext.Default.ConversationSessionDocument);
            if (document is not null)
            {
                items.Add(new ConversationSessionSummary(document.Id, document.Name, document.UpdatedAt, document.ProviderType, document.Model));
            }
        }

        return items.OrderByDescending(static item => item.UpdatedAt).ToList();
    }

    private string GetPath(string sessionId) => Path.Combine(_storagePath, sessionId + ".json");
}

