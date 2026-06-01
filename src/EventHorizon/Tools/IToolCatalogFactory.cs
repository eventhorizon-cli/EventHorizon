using EventHorizon.Workspace;
using Microsoft.Extensions.AI;

namespace EventHorizon.Tools;

public interface IToolCatalogFactory
{
    IReadOnlyList<AITool> Create(IWorkspaceService workspaceService);
}
