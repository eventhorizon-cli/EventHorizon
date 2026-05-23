using System.Text;
using EventHorizon.Configuration;

namespace EventHorizon.Diagnostics;

public sealed class RunErrorLogWriter : IRunErrorLogWriter
{
    private readonly object _syncLock = new();

    public RunErrorLogWriter(IPathEnvironment pathEnvironment)
    {
        string logDirectory = Path.Combine(pathEnvironment.HomeDirectory, ".eventhorizon", "logs");
        string timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss-fff", System.Globalization.CultureInfo.InvariantCulture);
        LogFilePath = Path.Combine(logDirectory, $"run-{timestamp}.log");
    }

    public string LogFilePath { get; }

    public void Write(string category, Exception exception, IReadOnlyDictionary<string, string?>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        string? directory = Path.GetDirectoryName(LogFilePath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        Directory.CreateDirectory(directory);

        var builder = new StringBuilder();
        builder.AppendLine("=== EventHorizon error ===");
        builder.Append("timestamp: ").AppendLine(DateTimeOffset.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
        builder.Append("category: ").AppendLine(string.IsNullOrWhiteSpace(category) ? "unknown" : category);
        if (metadata is not null)
        {
            foreach ((string key, string? value) in metadata.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
            {
                builder.Append(key).Append(": ").AppendLine(value ?? string.Empty);
            }
        }

        builder.AppendLine();
        builder.AppendLine(exception.ToString());
        builder.AppendLine();

        lock (_syncLock)
        {
            File.AppendAllText(LogFilePath, builder.ToString(), Encoding.UTF8);
        }
    }
}

