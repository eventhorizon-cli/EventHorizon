using EventHorizon.Configuration;

namespace EventHorizon.Cli;

public interface ICommandOptionsParser
{
    EffectiveCommandOptions Parse(string[] args);

    string GetHelpText();
}
