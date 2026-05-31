using System.Text.Json;
using EventHorizon.Configuration;

namespace EventHorizon.Engine.Sessions;

public sealed class FileSessionStore : ISessionStore
{
    private const string SessionDocumentFileName = "session.json";
    private readonly IPathEnvironment _pathEnvironment;

    public FileSessionStore(Configuration.IPathEnvironment pathEnvironment)
    {
        _pathEnvironment = pathEnvironment;
    }

    public async Task SaveAsync(SessionDocument document, CancellationToken cancellationToken)
    {
        var sessionDirectory = GetSessionDirectory(document.Id);
        Directory.CreateDirectory(sessionDirectory);
        var path = GetPath(document.Id);
        var json = JsonSerializer.Serialize(document, Configuration.EventHorizonJsonContext.Default.SessionDocument);
        var tempPath = path + ".tmp";
        await File.WriteAllTextAsync(tempPath, json, cancellationToken).ConfigureAwait(false);
        File.Move(tempPath, path, overwrite: true);
    }

    public async Task<SessionDocument?> LoadAsync(string sessionId, CancellationToken cancellationToken)
    {
        var path = GetPath(sessionId);
        if (!File.Exists(path))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize(json, Configuration.EventHorizonJsonContext.Default.SessionDocument);
    }

    public async Task<IReadOnlyList<SessionSummary>> ListAsync(CancellationToken cancellationToken)
    {
        Dictionary<string, SessionSummary> items = new(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(StoragePath))
        {
            return [];
        }

        foreach (var directory in Directory.EnumerateDirectories(StoragePath, "*", SearchOption.TopDirectoryOnly))
        {
            var file = Path.Combine(directory, SessionDocumentFileName);
            if (!File.Exists(file))
            {
                continue;
            }

            var summary = await ReadSummaryAsync(file, cancellationToken).ConfigureAwait(false);
            if (summary is not null)
            {
                items[summary.Id] = summary;
            }
        }

        return items.Values.OrderByDescending(static item => item.UpdatedAt).ToList();
    }

    public void Delete(string sessionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sessionDirectory = GetSessionDirectory(sessionId);
        if (Directory.Exists(sessionDirectory))
        {
            Directory.Delete(sessionDirectory, recursive: true);
        }
    }

    private string StoragePath
    {
        get
        {
            var storagePath = Path.Combine(_pathEnvironment.HomeDirectory, ".eventhorizon", "sessions");
            Directory.CreateDirectory(storagePath);
            return storagePath;
        }
    }

    private string GetPath(string sessionId) => Path.Combine(GetSessionDirectory(sessionId), SessionDocumentFileName);

    private string GetSessionDirectory(string sessionId) => Path.Combine(StoragePath, sessionId);

    private static SessionSummary? BuildSummary(SessionDocument? document)
    {
        if (document is null)
        {
            return null;
        }

        return new SessionSummary(
            document.Id,
            document.Name,
            document.CreatedAt,
            document.UpdatedAt,
            document.ProviderName,
            document.ProviderType,
            document.Model,
            document.Status,
            document.LastRunId,
            document.Summary,
            document.ChangedFilesCount,
            document.IsTitleGenerated,
            document.WorkspaceRoot);
    }

    private static async Task<SessionSummary?> ReadSummaryAsync(string file, CancellationToken cancellationToken)
    {
        var json = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
        var document = JsonSerializer.Deserialize(json, Configuration.EventHorizonJsonContext.Default.SessionDocument);
        return BuildSummary(document);
    }
}
