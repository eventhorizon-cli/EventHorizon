namespace EventHorizon.Workspace;

public sealed class WorkspaceContext
{
    public WorkspaceContext(string workspaceRoot)
    {
        WorkspaceRoot = Path.GetFullPath(workspaceRoot);
    }

    public string WorkspaceRoot { get; }
}
