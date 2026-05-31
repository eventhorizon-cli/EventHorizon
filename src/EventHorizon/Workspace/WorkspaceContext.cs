namespace EventHorizon.Workspace;

public sealed class WorkspaceContext
{
    public WorkspaceContext(string workspaceRoot)
    {
        WorkspaceRoot = workspaceRoot;
    }

    public string WorkspaceRoot
    {
        get => _workspaceRoot;
        set => _workspaceRoot = Path.GetFullPath(value);
    }

    private string _workspaceRoot = string.Empty;
}
