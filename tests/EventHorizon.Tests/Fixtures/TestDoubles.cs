using EventHorizon.Workspace;

namespace EventHorizon.Tests.Fixtures;

public sealed class StubWorkspaceContextAccessor : IWorkspaceContextAccessor
{
    public WorkspaceContext WorkspaceContext { get; set; }

    public StubWorkspaceContextAccessor(string workspaceRoot)
    {
        WorkspaceContext = new WorkspaceContext(workspaceRoot);
    }
}
