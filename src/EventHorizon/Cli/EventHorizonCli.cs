using System.CommandLine;
using EventHorizon.Configuration;

namespace EventHorizon.Cli;

public sealed class EventHorizonCli : ICommandOptionsParser
{
    private static readonly HashSet<string> KnownCommands =
    [
        "chat",
        "tui",
        "run",
    ];

    public EffectiveCommandOptions Parse(string[] args)
    {
        var normalizedArgs = NormalizeArgs(args);
        var model = CreateCommandModel(normalizedArgs[0]);
        var result = model.Command.Parse(normalizedArgs[1..], CreateParserConfiguration());
        if (result.Errors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, result.Errors.Select(static error => error.Message)));
        }

        return model.ToOptions(result, normalizedArgs[0]);
    }

    public string GetHelpText()
        => "Commands: chat, tui, run\nOptions: --config|-c, --workspace|-w, --provider, --model|-m";

    private static ParserConfiguration CreateParserConfiguration()
        => new()
        {
            EnablePosixBundling = false,
        };

    private static string[] NormalizeArgs(string[] args)
    {
        if (args.Length == 0)
        {
            return ["tui"];
        }

        if (args[0].StartsWith("-", StringComparison.Ordinal))
        {
            if (string.Equals(args[0], "--tui", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(args[0], "-tui", StringComparison.OrdinalIgnoreCase))
            {
                return ["tui", .. args[1..]];
            }

            if (string.Equals(args[0], "--chat", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(args[0], "-chat", StringComparison.OrdinalIgnoreCase))
            {
                return ["chat", .. args[1..]];
            }

            return ["chat", .. args];
        }

        if (string.Equals(args[0], "tui", StringComparison.OrdinalIgnoreCase))
        {
            return ["tui", .. args[1..]];
        }

        if (KnownCommands.Contains(args[0]))
        {
            return args;
        }

        return ["run", .. args];
    }

    private static CommandModel CreateCommandModel(string commandName)
    {
        Option<string?> configOption = new("--config", ["-c"]) { Description = "Path to an additional configuration file." };
        Option<string?> workspaceOption = new("--workspace", ["-w"]) { Description = "Workspace root directory." };
        Option<string?> providerOption = new("--provider", []) { Description = "Configured provider name, or a provider type override such as openai, azure-openai, anthropic, gemini, or openai-compatible." };
        Option<string?> modelOption = new("--model", ["-m"]) { Description = "Model or deployment name." };

        Command command = new(commandName, $"EventHorizon {commandName} command")
        {
            configOption, workspaceOption, providerOption, modelOption,
        };

        Argument<string[]>? promptArgument = null;
        Option<string?>? promptTextOption = null;
        if (string.Equals(commandName, "run", StringComparison.OrdinalIgnoreCase))
        {
            promptArgument = new Argument<string[]>("prompt")
            {
                Arity = ArgumentArity.ZeroOrMore,
                Description = "Prompt text. If omitted, use --prompt-text.",
            };
            promptTextOption = new Option<string?>("--prompt-text", []) { Description = "Prompt text for scripted execution." };
            command.Add(promptArgument);
            command.Add(promptTextOption);
        }

        return new CommandModel(command, configOption, workspaceOption, providerOption, modelOption, promptArgument, promptTextOption);
    }

    private sealed record CommandModel(
        Command Command,
        Option<string?> ConfigOption,
        Option<string?> WorkspaceOption,
        Option<string?> ProviderOption,
        Option<string?> ModelOption,
        Argument<string[]>? PromptArgument,
        Option<string?>? PromptTextOption)
    {
        public EffectiveCommandOptions ToOptions(ParseResult result, string commandName)
        {
            var promptTokens = PromptArgument is null ? [] : result.GetValue(PromptArgument) ?? [];
            var promptText = PromptTextOption is null ? null : result.GetValue(PromptTextOption);

            return new EffectiveCommandOptions
            {
                Command = commandName,
                ConfigFile = result.GetValue(ConfigOption),
                WorkspaceRoot = result.GetValue(WorkspaceOption),
                Provider = result.GetValue(ProviderOption),
                Model = result.GetValue(ModelOption),
                Prompt = !string.IsNullOrWhiteSpace(promptText)
                    ? promptText
                    : (promptTokens.Length == 0 ? null : string.Join(' ', promptTokens)),
            };
        }
    }
}

