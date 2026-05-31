using EventHorizon.Workspace;

namespace EventHorizon.Tools;

public interface IToolCatalogFactory
{
    IReadOnlyList<ToolDescriptor> Create(IWorkspaceService workspaceService);
}
