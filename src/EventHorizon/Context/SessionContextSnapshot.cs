namespace EventHorizon.Context;

public sealed record SessionContextSnapshot(
    string CurrentDate,
    string WorkspaceRoot,
    string WorkspaceSummary,
    string GitStatus,
    string ProjectInstructions);


