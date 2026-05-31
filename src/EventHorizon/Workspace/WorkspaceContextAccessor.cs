using EventHorizon.Configuration;

namespace EventHorizon.Workspace;

public sealed class WorkspaceContextAccessor : IWorkspaceContextAccessor
{
    private static readonly AsyncLocal<WorkspaceContextHolder> _workspaceContextCurrent = new();
    private readonly WorkspaceContext _defaultContext;

    public WorkspaceContextAccessor(IPathEnvironment pathEnvironment)
    {
        _defaultContext = new WorkspaceContext(pathEnvironment.CurrentDirectory);
    }

    public WorkspaceContext WorkspaceContext
    {
        get => _workspaceContextCurrent.Value?.Context ?? _defaultContext;
        set
        {
            var holder = _workspaceContextCurrent.Value;
            if (holder is not null)
            {
                holder.Context = null;
            }

            _workspaceContextCurrent.Value = new WorkspaceContextHolder
            {
                Context = value,
            };
        }
    }

    private sealed class WorkspaceContextHolder
    {
        public WorkspaceContext? Context;
    }
}
