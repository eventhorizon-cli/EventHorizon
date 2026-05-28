using EventHorizon.Configuration;

namespace EventHorizon.Cli;

public sealed class EventHorizonCli : ICommandOptionsParser
{
    public EffectiveCommandOptions Parse(string[] args)
    {
        string? configFile = null;

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (!string.Equals(argument, "--config", StringComparison.Ordinal) &&
                !string.Equals(argument, "-c", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Unsupported argument '{argument}'. Only --config is supported.");
            }

            if (index == args.Length - 1)
            {
                throw new InvalidOperationException("Missing value for --config.");
            }

            configFile = args[++index];
        }

        return new EffectiveCommandOptions
        {
            Command = EffectiveCommandOptions.StartupMode,
            ConfigFile = configFile,
        };
    }

    public string GetHelpText()
        => "Usage: EventHorizon [--config <path>]\nStarts the AGUI server.\nOptions: --config|-c";
}

