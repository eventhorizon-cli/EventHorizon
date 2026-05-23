namespace EventHorizon.Tools;

public sealed record AskQuestionOption(
    string Label,
    string? Description = null);

public sealed record AskQuestionDefinition(
    string Header,
    string Question,
    bool MultiSelect = false,
    bool AllowFreeformInput = true,
    AskQuestionOption[]? Options = null);

