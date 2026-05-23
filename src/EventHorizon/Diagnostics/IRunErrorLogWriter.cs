namespace EventHorizon.Diagnostics;

public interface IRunErrorLogWriter
{
    string LogFilePath { get; }

    void Write(string category, Exception exception, IReadOnlyDictionary<string, string?>? metadata = null);
}

