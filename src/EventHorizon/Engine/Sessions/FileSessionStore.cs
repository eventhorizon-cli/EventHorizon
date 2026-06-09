using System.Text.Json;
using EventHorizon.Configuration;

namespace EventHorizon.Engine.Sessions;

public sealed class FileSessionStore : ISessionStore
{
    private const string SessionDocumentFileName = "session.json";
    private const string WorkspaceDocumentFileName = "workspace.json";
    private readonly IPathEnvironment _pathEnvironment;

    public FileSessionStore(IPathEnvironment pathEnvironment)
    {
        _pathEnvironment = pathEnvironment;
    }

    public async Task SaveWorkspaceAsync(WorkspaceDocument workspace, CancellationToken cancellationToken)
    {
        var workspaceDirectory = GetWorkspaceDirectory(workspace.Id);
        Directory.CreateDirectory(workspaceDirectory);
        var path = GetWorkspacePath(workspace.Id);
        var json = JsonSerializer.Serialize(workspace, Configuration.EventHorizonJsonContext.Default.WorkspaceDocument);
        var tempPath = path + ".tmp";
        await File.WriteAllTextAsync(tempPath, json, cancellationToken).ConfigureAwait(false);
        File.Move(tempPath, path, overwrite: true);
    }

    public async Task<WorkspaceDocument?> LoadWorkspaceAsync(string workspaceId, CancellationToken cancellationToken)
    {
        var path = GetWorkspacePath(workspaceId);
        if (!File.Exists(path))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize(json, Configuration.EventHorizonJsonContext.Default.WorkspaceDocument);
    }

    public async Task<IReadOnlyList<WorkspaceDocument>> ListWorkspacesAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(WorkspaceStoragePath))
        {
            return [];
        }

        List<WorkspaceDocument> workspaces = [];
        foreach (var directory in Directory.EnumerateDirectories(WorkspaceStoragePath, "*", SearchOption.TopDirectoryOnly))
        {
            var file = Path.Combine(directory, WorkspaceDocumentFileName);
            if (!File.Exists(file))
            {
                continue;
            }

            var json = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
            var workspace = JsonSerializer.Deserialize(json, Configuration.EventHorizonJsonContext.Default.WorkspaceDocument);
            if (workspace is not null)
            {
                workspaces.Add(workspace);
            }
        }

        return workspaces.OrderByDescending(static item => item.UpdatedAt).ToList();
    }

    public void DeleteWorkspace(string workspaceId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var workspaceDirectory = GetWorkspaceDirectory(workspaceId);
        if (Directory.Exists(workspaceDirectory))
        {
            Directory.Delete(workspaceDirectory, recursive: true);
        }
    }

    public async Task SaveAsync(SessionDocument document, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(document.WorkspaceId))
        {
            throw new InvalidOperationException("Session workspace id is required.");
        }

        var sessionDirectory = GetWorkspaceSessionDirectory(document.WorkspaceId, document.Id);
        Directory.CreateDirectory(sessionDirectory);
        var path = Path.Combine(sessionDirectory, SessionDocumentFileName);
        var json = JsonSerializer.Serialize(document, Configuration.EventHorizonJsonContext.Default.SessionDocument);
        var tempPath = path + ".tmp";
        await File.WriteAllTextAsync(tempPath, json, cancellationToken).ConfigureAwait(false);
        File.Move(tempPath, path, overwrite: true);
    }

    public async Task<SessionDocument?> LoadAsync(string sessionId, CancellationToken cancellationToken)
    {
        var path = ResolveWorkspaceSessionPath(sessionId);
        if (path is null)
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize(json, Configuration.EventHorizonJsonContext.Default.SessionDocument);
    }

    public async Task<IReadOnlyList<SessionSummary>> ListAsync(CancellationToken cancellationToken)
    {
        Dictionary<string, SessionSummary> items = new(StringComparer.OrdinalIgnoreCase);
        if (Directory.Exists(WorkspaceStoragePath))
        {
            foreach (var workspaceDirectory in Directory.EnumerateDirectories(WorkspaceStoragePath, "*", SearchOption.TopDirectoryOnly))
            {
                var workspace = await ReadWorkspaceAsync(Path.Combine(workspaceDirectory, WorkspaceDocumentFileName), cancellationToken).ConfigureAwait(false);
                if (workspace is null)
                {
                    continue;
                }

                foreach (var sessionDirectory in Directory.EnumerateDirectories(workspaceDirectory, "*", SearchOption.TopDirectoryOnly))
                {
                    var file = Path.Combine(sessionDirectory, SessionDocumentFileName);
                    if (!File.Exists(file))
                    {
                        continue;
                    }

                    var summary = await ReadSummaryAsync(file, workspace, cancellationToken).ConfigureAwait(false);
                    if (summary is not null)
                    {
                        items[summary.Id] = summary;
                    }
                }
            }
        }

        return items.Values.OrderByDescending(static item => item.UpdatedAt).ToList();
    }

    public void Delete(string sessionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sessionDirectory = ResolveWorkspaceSessionDirectory(sessionId);
        if (Directory.Exists(sessionDirectory))
        {
            Directory.Delete(sessionDirectory, recursive: true);
        }
    }

    private string WorkspaceStoragePath
    {
        get
        {
            var storagePath = Path.Combine(_pathEnvironment.HomeDirectory, ".eventhorizon", "workspaces");
            Directory.CreateDirectory(storagePath);
            return storagePath;
        }
    }

    private string GetWorkspacePath(string workspaceId) => Path.Combine(GetWorkspaceDirectory(workspaceId), WorkspaceDocumentFileName);

    private string GetWorkspaceDirectory(string workspaceId) => Path.Combine(WorkspaceStoragePath, workspaceId);

    private string GetWorkspaceSessionDirectory(string workspaceId, string sessionId)
        => Path.Combine(GetWorkspaceDirectory(workspaceId), sessionId);

    private string? ResolveWorkspaceSessionPath(string sessionId)
    {
        var directory = ResolveWorkspaceSessionDirectory(sessionId);
        return directory is null ? null : Path.Combine(directory, SessionDocumentFileName);
    }

    private string? ResolveWorkspaceSessionDirectory(string sessionId)
    {
        if (!Directory.Exists(WorkspaceStoragePath))
        {
            return null;
        }

        foreach (var workspaceDirectory in Directory.EnumerateDirectories(WorkspaceStoragePath, "*", SearchOption.TopDirectoryOnly))
        {
            var sessionDirectory = Path.Combine(workspaceDirectory, sessionId);
            if (File.Exists(Path.Combine(sessionDirectory, SessionDocumentFileName)) && File.Exists(Path.Combine(workspaceDirectory, WorkspaceDocumentFileName)))
            {
                return sessionDirectory;
            }
        }

        return null;
    }

    private static SessionSummary? BuildSummary(SessionDocument? document, WorkspaceDocument? workspace = null)
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
            document.WorkspaceId,
            workspace?.Name,
            workspace?.WorkspaceRoot);
    }

    private static async Task<SessionSummary?> ReadSummaryAsync(string file, WorkspaceDocument? workspace, CancellationToken cancellationToken)
    {
        var json = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
        var document = JsonSerializer.Deserialize(json, Configuration.EventHorizonJsonContext.Default.SessionDocument);
        return BuildSummary(document, workspace);
    }

    private static async Task<WorkspaceDocument?> ReadWorkspaceAsync(string file, CancellationToken cancellationToken)
    {
        if (!File.Exists(file))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize(json, Configuration.EventHorizonJsonContext.Default.WorkspaceDocument);
    }
}
