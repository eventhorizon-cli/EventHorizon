namespace EventHorizon.Workspace;

public interface IWorkspaceContextAccessor
{
    WorkspaceContext WorkspaceContext { get; set; }
}
