using System.Text;
using System.Text.Json;
using Automator.Application.Logging;

namespace Automator.Windows;

/// <summary>Writes one structured record per line; stdout is reserved for the RPC transport.</summary>
public sealed class FileApplicationLog : IApplicationLog, IDisposable
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    private readonly object _sync = new();
    private readonly StreamWriter _writer;
    private readonly string _source;
    private readonly string _buildId;
    private readonly string _sessionId;
    private bool _disposed;

    public FileApplicationLog(string directory, string source, string buildId, string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        Directory.CreateDirectory(directory);
        _source = source;
        _buildId = buildId;
        _sessionId = sessionId;
        var path = Path.Combine(directory, $"Automator-{DateTime.Now:yyyyMMdd}-{Environment.ProcessId}-{sessionId}.log");
        _writer = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite), new UTF8Encoding(false))
        {
            AutoFlush = true
        };
        LogPath = path;
        Write(ApplicationLogLevel.Information, "Process.LogStarted", "Structured process logging started.",
            properties: new Dictionary<string, object?> { ["logPath"] = path });
    }

    public string LogPath { get; }

    public void Write(ApplicationLogLevel level, string eventName, string message, Exception? exception = null,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
        var record = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["timestampUtc"] = DateTimeOffset.UtcNow,
            ["level"] = level.ToString(),
            ["event"] = eventName,
            ["message"] = message,
            ["source"] = _source,
            ["processId"] = Environment.ProcessId,
            ["sessionId"] = _sessionId,
            ["buildId"] = _buildId
        };
        if (exception is not null) record["exception"] = new { type = exception.GetType().FullName, exception.Message, exception.StackTrace };
        if (properties is not null) record["properties"] = properties;
        var line = JsonSerializer.Serialize(record, Options);
        lock (_sync)
        {
            if (_disposed) return;
            _writer.WriteLine(line);
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;
            _writer.Dispose();
        }
    }
}
