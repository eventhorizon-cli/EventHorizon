using EventHorizon.Workspace;
using Microsoft.Extensions.AI;

namespace EventHorizon.Tools;

public sealed class ToolCatalog : IToolCatalogFactory
{
    public IReadOnlyList<AITool> Create(IWorkspaceService workspaceService)
    {
        List<AITool> tools =
        [
            AIFunctionFactory.Create(workspaceService.ApplyPatch, name: "apply_patch"),
            AIFunctionFactory.Create(workspaceService.CreateFile, name: "create_file"),
            AIFunctionFactory.Create(workspaceService.FileSearch, name: "file_search"),
            AIFunctionFactory.Create(workspaceService.GrepSearch, name: "grep_search"),
            AIFunctionFactory.Create(workspaceService.InsertEditIntoFile, name: "insert_edit_into_file"),
            AIFunctionFactory.Create(workspaceService.ListDir, name: "list_dir"),
            AIFunctionFactory.Create(workspaceService.OpenFile, name: "open_file"),
            AIFunctionFactory.Create(workspaceService.ReadFileTool, name: "read_file"),
            AIFunctionFactory.Create(workspaceService.SemanticSearch, name: "semantic_search"),
            AIFunctionFactory.Create(workspaceService.ValidateCvesAsync, name: "validate_cves"),
            AIFunctionFactory.Create(workspaceService.GetErrorsAsync, name: "get_errors"),
            AIFunctionFactory.Create(workspaceService.GetTerminalOutput, name: "get_terminal_output"),
            AIFunctionFactory.Create(workspaceService.RunInTerminalAsync, name: "run_in_terminal"),
        ];

        return tools.OrderBy(static tool => tool.Name, StringComparer.Ordinal).ToArray();
    }
}
