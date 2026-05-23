using EventHorizon.Configuration;
using EventHorizon.Workspace;

namespace EventHorizon.Tools;

public interface IToolCatalogFactory
{
    IReadOnlyList<ToolDescriptor> Create(WorkspaceService workspaceService, AppOptions options);
}
