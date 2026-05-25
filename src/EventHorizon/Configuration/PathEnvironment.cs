namespace EventHorizon.Configuration;

public sealed class PathEnvironment : IPathEnvironment
{
    public string CurrentDirectory => Directory.GetCurrentDirectory();

    public string HomeDirectory => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
}