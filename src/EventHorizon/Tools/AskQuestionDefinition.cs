namespace EventHorizon.Tools;

public sealed record AskQuestionDefinition(
    string Header,
    string Question,
    bool MultiSelect = false,
    bool AllowFreeformInput = true,
    AskQuestionOption[]? Options = null);
