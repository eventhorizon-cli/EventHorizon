namespace EventHorizon.Configuration;

public interface IPathEnvironment
{
    string CurrentDirectory { get; }

    string HomeDirectory { get; }
}
