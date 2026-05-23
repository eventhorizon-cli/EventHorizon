using EventHorizon.Configuration;

namespace EventHorizon.Terminal;

internal static class TerminalLaunchpad
{
    public static TerminalPanelViewModel BuildPanel(AppOptions options, TerminalConversationState state, int animationFrameIndex)
    {
        var summary = DescribeConnection(options);
        List<string> lines =
        [
            .. TerminalMascotAnimator.GetFrameLines(animationFrameIndex),
            string.Empty,
            summary.IsReady
                ? "Welcome aboard. Your current model connection looks ready."
                : "Before you start, pick or finish configuring a model connection.",
            $"Provider    {summary.ProviderType}",
            $"Model       {summary.ModelName}",
            $"Status      {(summary.IsReady ? "[ready]" : "[needs setup]")} {summary.StatusDetail}",
            $"Auth        {summary.AuthenticationDetail}",
            $"Workspace   {options.WorkspaceRoot}",
        ];

        if (!string.IsNullOrWhiteSpace(summary.EndpointDisplay))
        {
            lines.Add($"Endpoint    {summary.EndpointDisplay}");
        }

        lines.Add(string.Empty);
        lines.Add(summary.IsReady ? "Next" : "Setup");
        lines.AddRange(summary.GuidanceLines.Select(static line => $"- {line}"));

        if (!string.IsNullOrWhiteSpace(state.FocusedPath))
        {
            lines.Add(string.Empty);
            lines.Add($"Focus       {state.FocusedPath}");
        }

        return new TerminalPanelViewModel
        {
            PanelId = "launchpad",
            Title = summary.IsReady ? "Launchpad" : "Launchpad / Setup",
            IsActive = true,
            Lines = lines
        };
    }

    public static string BuildLaunchpadStatusLine(AppOptions options)
    {
        var summary = DescribeConnection(options);
        return summary.IsReady
            ? $"Launchpad ready · {summary.ProviderType}/{summary.ModelName}. Press Enter to open the workbench or type a prompt."
            : $"Launchpad · {summary.ProviderType}/{summary.ModelName}. Finish model setup before sending prompts.";
    }

    public static string BuildWorkbenchStatusLine(AppOptions options)
    {
        var summary = DescribeConnection(options);
        return summary.IsReady
            ? $"Workbench ready on {summary.ProviderType}/{summary.ModelName}. Describe a change, ask for a review, or use /help."
            : $"Workbench opened, but the current {summary.ProviderType} connection still needs setup. Use /help for guidance.";
    }

    internal static TerminalProviderConnectionSummary DescribeConnection(AppOptions options)
    {
        var providerType = string.IsNullOrWhiteSpace(options.Provider.Type)
            ? "openai"
            : options.Provider.Type.Trim().ToLowerInvariant();
        var modelName = options.Provider.Model ?? options.Provider.Deployment ?? "(not configured)";
        var endpointDisplay = Summarize(options.Provider.Endpoint, 56);

        return providerType switch
        {
            "azure-openai" => new TerminalProviderConnectionSummary(
                IsReady: !string.IsNullOrWhiteSpace(options.Provider.Endpoint)
                    && !string.IsNullOrWhiteSpace(options.Provider.Deployment ?? options.Provider.Model)
                    && (options.Provider.UseDefaultAzureCredential || !string.IsNullOrWhiteSpace(options.Provider.ApiKey)),
                ProviderType: providerType,
                ModelName: modelName,
                StatusDetail: !string.IsNullOrWhiteSpace(options.Provider.Endpoint)
                    ? "Azure OpenAI endpoint detected"
                    : "missing endpoint or deployment",
                AuthenticationDetail: options.Provider.UseDefaultAzureCredential
                    ? "DefaultAzureCredential"
                    : (!string.IsNullOrWhiteSpace(options.Provider.ApiKey) ? "API key configured" : "API key missing"),
                EndpointDisplay: endpointDisplay,
                GuidanceLines: !string.IsNullOrWhiteSpace(options.Provider.Endpoint)
                    && !string.IsNullOrWhiteSpace(options.Provider.Deployment ?? options.Provider.Model)
                    && (options.Provider.UseDefaultAzureCredential || !string.IsNullOrWhiteSpace(options.Provider.ApiKey))
                    ?
                    [
                        "Press Enter to expand into the full workbench.",
                        "Type a prompt now if you want to start immediately.",
                        "To switch deployment, restart with --model or --config <path>.",
                        "Use /help once inside the workbench to browse commands and shortcuts."
                    ]
                    :
                    [
                        "Set AZURE_OPENAI_ENDPOINT and AZURE_OPENAI_DEPLOYMENT_NAME.",
                        "Provide AZURE_OPENAI_API_KEY or keep DefaultAzureCredential enabled.",
                        "Restart with --config <path> if you keep multiple provider presets.",
                        "Press Enter after setup to expand into the full workbench."
                    ]),
            "anthropic" => new TerminalProviderConnectionSummary(
                IsReady: !string.IsNullOrWhiteSpace(options.Provider.ApiKey) && !string.IsNullOrWhiteSpace(options.Provider.Model),
                ProviderType: providerType,
                ModelName: modelName,
                StatusDetail: !string.IsNullOrWhiteSpace(options.Provider.ApiKey)
                    ? "Anthropic credentials detected"
                    : "missing ANTHROPIC_API_KEY",
                AuthenticationDetail: !string.IsNullOrWhiteSpace(options.Provider.ApiKey) ? "API key configured" : "API key missing",
                EndpointDisplay: endpointDisplay,
                GuidanceLines: !string.IsNullOrWhiteSpace(options.Provider.ApiKey) && !string.IsNullOrWhiteSpace(options.Provider.Model)
                    ?
                    [
                        "Press Enter to expand into the full workbench once the connection looks right.",
                        "Type a prompt right away to start coding on the active workspace.",
                        "Use --provider anthropic --model claude-sonnet-4-20250514 on restart to switch models.",
                        "Use /help once inside the workbench to browse commands and shortcuts."
                    ]
                    :
                    [
                        "Set ANTHROPIC_API_KEY before sending prompts.",
                        "Choose a Claude model with --model or a config file on restart.",
                        "Use --config <path> if you keep multiple provider presets.",
                        "Press Enter after setup to expand into the full workbench."
                    ]),
            "gemini" => new TerminalProviderConnectionSummary(
                IsReady: !string.IsNullOrWhiteSpace(options.Provider.ApiKey) && !string.IsNullOrWhiteSpace(options.Provider.Model),
                ProviderType: providerType,
                ModelName: modelName,
                StatusDetail: !string.IsNullOrWhiteSpace(options.Provider.ApiKey)
                    ? "Gemini credentials detected"
                    : "missing GOOGLE_GENAI_API_KEY",
                AuthenticationDetail: !string.IsNullOrWhiteSpace(options.Provider.ApiKey) ? "API key configured" : "API key missing",
                EndpointDisplay: endpointDisplay,
                GuidanceLines: !string.IsNullOrWhiteSpace(options.Provider.ApiKey) && !string.IsNullOrWhiteSpace(options.Provider.Model)
                    ?
                    [
                        "Press Enter to expand into the full workbench.",
                        "Type a prompt to start immediately.",
                        "Use --provider gemini --model gemini-2.5-flash on restart to switch models.",
                        "Use /help once inside the workbench to browse commands and shortcuts."
                    ]
                    :
                    [
                        "Set GOOGLE_GENAI_API_KEY before sending prompts.",
                        "Choose a Gemini model with --model or a config file on restart.",
                        "Use --config <path> if you keep multiple provider presets.",
                        "Press Enter after setup to expand into the full workbench."
                    ]),
            "openai-compatible" => new TerminalProviderConnectionSummary(
                IsReady: !string.IsNullOrWhiteSpace(options.Provider.Endpoint) && !string.IsNullOrWhiteSpace(options.Provider.Model),
                ProviderType: providerType,
                ModelName: modelName,
                StatusDetail: !string.IsNullOrWhiteSpace(options.Provider.Endpoint)
                    ? "Custom endpoint detected"
                    : "missing OPENAI-compatible endpoint",
                AuthenticationDetail: !string.IsNullOrWhiteSpace(options.Provider.ApiKey) ? "API key configured" : "API key optional / missing",
                EndpointDisplay: endpointDisplay,
                GuidanceLines: !string.IsNullOrWhiteSpace(options.Provider.Endpoint) && !string.IsNullOrWhiteSpace(options.Provider.Model)
                    ?
                    [
                        "Press Enter to expand into the full workbench when ready.",
                        "Type a prompt to start immediately against the configured endpoint.",
                        "API keys are optional for some local providers such as Ollama.",
                        "Use --config <path> if you keep multiple provider presets."
                    ]
                    :
                    [
                        "Set OPENAI_COMPATIBLE_ENDPOINT and OPENAI_COMPATIBLE_MODEL for gateways or local runtimes.",
                        "API keys are optional for some local providers such as Ollama.",
                        "Restart with --config <path> if you keep multiple provider presets.",
                        "Press Enter after setup to expand into the full workbench."
                    ]),
            _ => new TerminalProviderConnectionSummary(
                IsReady: !string.IsNullOrWhiteSpace(options.Provider.ApiKey) && !string.IsNullOrWhiteSpace(options.Provider.Model),
                ProviderType: providerType,
                ModelName: modelName,
                StatusDetail: !string.IsNullOrWhiteSpace(options.Provider.ApiKey)
                    ? "OpenAI credentials detected"
                    : "missing OPENAI_API_KEY",
                AuthenticationDetail: !string.IsNullOrWhiteSpace(options.Provider.ApiKey) ? "API key configured" : "API key missing",
                EndpointDisplay: endpointDisplay,
                GuidanceLines: !string.IsNullOrWhiteSpace(options.Provider.ApiKey) && !string.IsNullOrWhiteSpace(options.Provider.Model)
                    ?
                    [
                        "Press Enter to expand into the full workbench.",
                        "Type a prompt to start immediately on the current workspace.",
                        "Use --provider, --model, or --config <path> on restart to switch connections.",
                        "Use /help once inside the workbench to browse commands and shortcuts."
                    ]
                    :
                    [
                        "Set OPENAI_API_KEY before sending prompts.",
                        "Choose a model with --model or a config file on restart.",
                        "Use --provider, --model, or --config <path> to switch connections.",
                        "Press Enter after setup to expand into the full workbench."
                    ])
        };
    }

    private static string Summarize(string? text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string compact = text.Trim();
        return compact.Length <= maxLength ? compact : compact[..(maxLength - 1)] + "…";
    }
}
