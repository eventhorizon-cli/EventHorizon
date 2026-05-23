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
        string path = GetPath(document.Id);
        string json = JsonSerializer.Serialize(document, Configuration.EventHorizonJsonContext.Default.ConversationSessionDocument);
        await File.WriteAllTextAsync(path, json, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ConversationSessionDocument?> LoadAsync(string sessionId, CancellationToken cancellationToken)
    {
        string path = GetPath(sessionId);
        if (!File.Exists(path))
        {
            return null;
        }

        string json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize(json, Configuration.EventHorizonJsonContext.Default.ConversationSessionDocument);
    }

    public async Task<IReadOnlyList<ConversationSessionSummary>> ListAsync(CancellationToken cancellationToken)
    {
        List<ConversationSessionSummary> items = [];
        foreach (string file in Directory.EnumerateFiles(_storagePath, "*.json", SearchOption.TopDirectoryOnly))
        {
            string json = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
            ConversationSessionDocument? document = JsonSerializer.Deserialize(json, Configuration.EventHorizonJsonContext.Default.ConversationSessionDocument);
            if (document is not null)
            {
                items.Add(new ConversationSessionSummary(document.Id, document.Name, document.UpdatedAt, document.ProviderType, document.Model));
            }
        }

        return items.OrderByDescending(static item => item.UpdatedAt).ToList();
    }

    private string GetPath(string sessionId) => Path.Combine(_storagePath, sessionId + ".json");
}

