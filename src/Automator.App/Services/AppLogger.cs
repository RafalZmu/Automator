using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Automator.Services;

/// <summary>Writes newline-delimited structured logs to the current user's Automator log folder.</summary>
public static class AppLogger
{
    private sealed record LogRecord(
        DateTimeOffset TimestampUtc,
        string Level,
        string Event,
        string Message,
        int ProcessId,
        int ThreadId,
        IReadOnlyDictionary<string, object?>? Properties,
        string? Exception);

    private static readonly object Sync = new();
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };
    private static Mutex? _writerMutex;
    private static string? _logDirectory;
    private static bool _initialized;

    public static string? LogDirectory
    {
        get { lock (Sync) return _logDirectory; }
    }

    public static string? CurrentLogFile
    {
        get
        {
            lock (Sync)
            {
                if (_logDirectory is null) return null;
                return Path.Combine(_logDirectory, $"automator-{DateTime.Now:yyyy-MM-dd}.jsonl");
            }
        }
    }

    public static void Initialize()
    {
        lock (Sync)
        {
            if (_initialized) return;
            try
            {
                _logDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Automator",
                    "Logs");
                Directory.CreateDirectory(_logDirectory);
                try { _writerMutex = new Mutex(false, @"Local\Automator.LogWriter"); }
                catch (Exception exception) { Debug.WriteLine($"Automator log mutex unavailable: {exception}"); }
                _initialized = true;
                RemoveExpiredLogs();
                WriteCore("INFO", "Logging.Initialized", "Automator logging is ready.", null,
                    new Dictionary<string, object?> { ["logDirectory"] = _logDirectory });
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"Automator logger initialization failed: {exception}");
            }
        }
    }

    public static void Info(string eventName, string message, IReadOnlyDictionary<string, object?>? properties = null) =>
        Write("INFO", eventName, message, null, properties);

    public static void Warning(string eventName, string message, Exception? exception = null,
        IReadOnlyDictionary<string, object?>? properties = null) =>
        Write("WARN", eventName, message, exception, properties);

    public static void Error(string eventName, string message, Exception? exception = null,
        IReadOnlyDictionary<string, object?>? properties = null) =>
        Write("ERROR", eventName, message, exception, properties);

    public static void Critical(string eventName, string message, Exception? exception = null,
        IReadOnlyDictionary<string, object?>? properties = null) =>
        Write("CRITICAL", eventName, message, exception, properties);

    private static void Write(string level, string eventName, string message, Exception? exception,
        IReadOnlyDictionary<string, object?>? properties)
    {
        if (!_initialized) Initialize();
        WriteCore(level, eventName, message, exception, properties);
    }

    private static void WriteCore(string level, string eventName, string message, Exception? exception,
        IReadOnlyDictionary<string, object?>? properties)
    {
        lock (Sync)
        {
            if (_logDirectory is null) return;
            var mutexAcquired = false;
            var writerMutex = _writerMutex;
            try
            {
                if (writerMutex is not null)
                {
                    try
                    {
                        mutexAcquired = writerMutex.WaitOne(TimeSpan.FromSeconds(2));
                    }
                    catch (AbandonedMutexException)
                    {
                        mutexAcquired = true;
                    }

                    if (!mutexAcquired) Debug.WriteLine($"Automator log writer lock timed out for {eventName}; appending without the cross-process lock.");
                }

                var now = DateTimeOffset.Now;
                var record = new LogRecord(
                    now.ToUniversalTime(),
                    level,
                    eventName,
                    message,
                    Environment.ProcessId,
                    Environment.CurrentManagedThreadId,
                    properties,
                    exception?.ToString());
                var line = JsonSerializer.Serialize(record, JsonOptions) + Environment.NewLine;
                var bytes = Encoding.UTF8.GetBytes(line);
                var path = Path.Combine(_logDirectory, $"automator-{now:yyyy-MM-dd}.jsonl");
                using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }
            catch (Exception loggingException)
            {
                Debug.WriteLine($"Automator log write failed for {eventName}: {loggingException}");
            }
            finally
            {
                if (mutexAcquired)
                {
                    try { writerMutex!.ReleaseMutex(); }
                    catch (ApplicationException releaseException) { Debug.WriteLine($"Automator log mutex release failed: {releaseException}"); }
                }
            }
        }
    }

    private static void RemoveExpiredLogs()
    {
        if (_logDirectory is null) return;
        var cutoff = DateTime.UtcNow.AddDays(-30);
        try
        {
            foreach (var path in Directory.EnumerateFiles(_logDirectory, "automator-*.jsonl"))
            {
                if (File.GetLastWriteTimeUtc(path) < cutoff) File.Delete(path);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"Automator log retention cleanup failed: {exception}");
        }
    }
}
