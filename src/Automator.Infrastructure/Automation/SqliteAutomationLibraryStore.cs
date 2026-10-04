using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Automator.Application.Automation;

namespace Automator.Infrastructure.Automation;

/// <summary>Versioned SQLite library using the inbox winsqlite3 provider on Windows.</summary>
public sealed class SqliteAutomationLibraryStore : IAutomationLibraryStore
{
    public const int CurrentSchemaVersion = 1;
    public const int MaximumRecordBytes = 1 * 1024 * 1024;

    private static readonly Regex StableModuleId = new("^[a-z][a-z0-9.-]{0,63}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex StableCollection = new("^[a-z][a-z0-9.-]{0,63}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex StableRecordId = new("^[A-Za-z0-9._-]{1,128}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private readonly string _databasePath;
    private readonly SemaphoreSlim _operationGate = new(1, 1);

    public SqliteAutomationLibraryStore(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        _databasePath = Path.GetFullPath(databasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(_databasePath)!);
    }

    public Task<IReadOnlyList<AutomationLibraryRecord>> ListAsync(
        string moduleId, string collection, CancellationToken cancellationToken) =>
        RunAsync(() =>
        {
            ValidateKeys(moduleId, collection, "list");
            using var database = OpenDatabase();
            EnsureSchema(database.Handle);
            using var statement = database.Prepare("SELECT module_id, collection_name, record_id, schema_version, payload_json, updated_utc FROM library_records WHERE module_id = ?1 AND collection_name = ?2 ORDER BY updated_utc DESC, record_id COLLATE NOCASE;");
            statement.BindText(1, moduleId);
            statement.BindText(2, collection);
            var records = new List<AutomationLibraryRecord>();
            while (statement.Step() == SqliteNative.Row) records.Add(ReadRecord(statement.Handle));
            return (IReadOnlyList<AutomationLibraryRecord>)records;
        }, cancellationToken);

    public Task<AutomationLibraryRecord?> GetAsync(
        string moduleId, string collection, string id, CancellationToken cancellationToken) =>
        RunAsync<AutomationLibraryRecord?>(() =>
        {
            ValidateKeys(moduleId, collection, id);
            using var database = OpenDatabase();
            EnsureSchema(database.Handle);
            using var statement = database.Prepare("SELECT module_id, collection_name, record_id, schema_version, payload_json, updated_utc FROM library_records WHERE module_id = ?1 AND collection_name = ?2 AND record_id = ?3;");
            BindKeys(statement, moduleId, collection, id);
            return statement.Step() == SqliteNative.Row ? ReadRecord(statement.Handle) : null;
        }, cancellationToken);

    public Task UpsertAsync(AutomationLibraryRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        ValidateKeys(record.ModuleId, record.Collection, record.Id);
        if (record.SchemaVersion <= 0) throw new InvalidDataException("Library record schema version must be positive.");
        if (record.Data.ValueKind == JsonValueKind.Undefined)
            throw new InvalidDataException("Library record data must contain a JSON value.");
        var json = record.Data.GetRawText();
        if (Encoding.UTF8.GetByteCount(json) > MaximumRecordBytes)
            throw new InvalidDataException("Library record data exceeds the 1 MiB limit.");

        return RunAsync(() =>
        {
            using var database = OpenDatabase();
            EnsureSchema(database.Handle);
            using var statement = database.Prepare("INSERT INTO library_records(module_id, collection_name, record_id, schema_version, payload_json, updated_utc) VALUES(?1, ?2, ?3, ?4, ?5, ?6) ON CONFLICT(module_id, collection_name, record_id) DO UPDATE SET schema_version = excluded.schema_version, payload_json = excluded.payload_json, updated_utc = excluded.updated_utc;");
            BindKeys(statement, record.ModuleId, record.Collection, record.Id);
            statement.BindInt(4, record.SchemaVersion);
            statement.BindText(5, json);
            statement.BindText(6, record.UpdatedUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
            statement.ExpectDone();
            return true;
        }, cancellationToken);
    }

    public Task<bool> DeleteAsync(
        string moduleId, string collection, string id, CancellationToken cancellationToken) =>
        RunAsync(() =>
        {
            ValidateKeys(moduleId, collection, id);
            using var database = OpenDatabase();
            EnsureSchema(database.Handle);
            using var statement = database.Prepare("DELETE FROM library_records WHERE module_id = ?1 AND collection_name = ?2 AND record_id = ?3;");
            BindKeys(statement, moduleId, collection, id);
            statement.ExpectDone();
            return SqliteNative.sqlite3_changes(database.Handle) > 0;
        }, cancellationToken);

    private async Task<T> RunAsync<T>(Func<T> operation, CancellationToken cancellationToken)
    {
        await _operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await Task.Run(operation, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _operationGate.Release();
        }
    }

    private Database OpenDatabase()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("The Automator SQLite store requires the Windows winsqlite3 system library.");
        var result = SqliteNative.sqlite3_open_v2(_databasePath, out var handle,
            SqliteNative.OpenReadWrite | SqliteNative.OpenCreate | SqliteNative.OpenFullMutex, IntPtr.Zero);
        if (result != SqliteNative.Ok)
        {
            var message = handle == IntPtr.Zero ? "Could not open the SQLite library." : SqliteNative.ErrorMessage(handle);
            if (handle != IntPtr.Zero) SqliteNative.sqlite3_close_v2(handle);
            throw new IOException($"SQLite error {result}: {message}");
        }
        SqliteNative.sqlite3_busy_timeout(handle, 5_000);
        return new Database(handle);
    }

    private static void EnsureSchema(IntPtr database)
    {
        SqliteNative.Execute(database, "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; CREATE TABLE IF NOT EXISTS library_schema(id INTEGER PRIMARY KEY CHECK(id=1), version INTEGER NOT NULL); INSERT OR IGNORE INTO library_schema(id, version) VALUES(1, 1); CREATE TABLE IF NOT EXISTS library_records(module_id TEXT NOT NULL, collection_name TEXT NOT NULL, record_id TEXT NOT NULL, schema_version INTEGER NOT NULL CHECK(schema_version > 0), payload_json TEXT NOT NULL, updated_utc TEXT NOT NULL, PRIMARY KEY(module_id, collection_name, record_id)); CREATE INDEX IF NOT EXISTS library_records_updated ON library_records(module_id, collection_name, updated_utc DESC);");
        using var statement = new Statement(database, "SELECT version FROM library_schema WHERE id=1;");
        if (statement.Step() != SqliteNative.Row)
            throw new InvalidDataException("SQLite library schema metadata is missing.");
        var version = SqliteNative.sqlite3_column_int(statement.Handle, 0);
        if (version != CurrentSchemaVersion)
            throw new InvalidDataException($"SQLite library schema version {version} is not supported.");
    }

    private static AutomationLibraryRecord ReadRecord(IntPtr statement)
    {
        using var document = JsonDocument.Parse(SqliteNative.ColumnText(statement, 4));
        var updated = DateTimeOffset.Parse(SqliteNative.ColumnText(statement, 5), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        return new AutomationLibraryRecord(SqliteNative.ColumnText(statement, 0), SqliteNative.ColumnText(statement, 1),
            SqliteNative.ColumnText(statement, 2), SqliteNative.sqlite3_column_int(statement, 3),
            document.RootElement.Clone(), updated);
    }

    private static void BindKeys(Statement statement, string moduleId, string collection, string id)
    {
        statement.BindText(1, moduleId);
        statement.BindText(2, collection);
        statement.BindText(3, id);
    }

    private static void ValidateKeys(string moduleId, string collection, string id)
    {
        if (!StableModuleId.IsMatch(moduleId)) throw new ArgumentException("Module id is invalid.", nameof(moduleId));
        if (!StableCollection.IsMatch(collection)) throw new ArgumentException("Library collection is invalid.", nameof(collection));
        if (!StableRecordId.IsMatch(id)) throw new ArgumentException("Library record id is invalid.", nameof(id));
    }

    private sealed class Database(IntPtr handle) : IDisposable
    {
        public IntPtr Handle { get; } = handle;
        public Statement Prepare(string sql) => new(Handle, sql);
        public void Dispose() => SqliteNative.sqlite3_close_v2(Handle);
    }

    private sealed class Statement : IDisposable
    {
        public Statement(IntPtr database, string sql)
        {
            var result = SqliteNative.sqlite3_prepare_v2(database, sql, -1, out var handle, IntPtr.Zero);
            if (result != SqliteNative.Ok) throw new IOException($"SQLite error {result}: {SqliteNative.ErrorMessage(database)}");
            Handle = handle;
        }

        public IntPtr Handle { get; }
        public int Step()
        {
            var result = SqliteNative.sqlite3_step(Handle);
            if (result is not (SqliteNative.Row or SqliteNative.Done))
                throw new IOException($"SQLite query failed with result code {result}.");
            return result;
        }

        public void ExpectDone()
        {
            if (Step() != SqliteNative.Done) throw new InvalidDataException("SQLite operation returned an unexpected row.");
        }

        public void BindText(int index, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            var result = SqliteNative.sqlite3_bind_text(Handle, index, bytes, bytes.Length, new IntPtr(-1));
            if (result != SqliteNative.Ok) throw new IOException($"SQLite text binding failed with result code {result}.");
        }

        public void BindInt(int index, int value)
        {
            var result = SqliteNative.sqlite3_bind_int(Handle, index, value);
            if (result != SqliteNative.Ok) throw new IOException($"SQLite integer binding failed with result code {result}.");
        }

        public void Dispose() => SqliteNative.sqlite3_finalize(Handle);
    }

    private static class SqliteNative
    {
        private const string Library = "winsqlite3.dll";
        internal const int Ok = 0;
        internal const int Row = 100;
        internal const int Done = 101;
        internal const int OpenReadWrite = 0x00000002;
        internal const int OpenCreate = 0x00000004;
        internal const int OpenFullMutex = 0x00010000;

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "sqlite3_open_v2", CharSet = CharSet.Ansi)]
        internal static extern int sqlite3_open_v2([MarshalAs(UnmanagedType.LPUTF8Str)] string filename, out IntPtr database, int flags, IntPtr vfs);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "sqlite3_close_v2")]
        internal static extern int sqlite3_close_v2(IntPtr database);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "sqlite3_prepare_v2", CharSet = CharSet.Ansi)]
        internal static extern int sqlite3_prepare_v2(IntPtr database, [MarshalAs(UnmanagedType.LPUTF8Str)] string sql, int byteCount, out IntPtr statement, IntPtr tail);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "sqlite3_step")]
        internal static extern int sqlite3_step(IntPtr statement);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "sqlite3_finalize")]
        internal static extern int sqlite3_finalize(IntPtr statement);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "sqlite3_bind_text")]
        internal static extern int sqlite3_bind_text(IntPtr statement, int index, byte[] value, int byteCount, IntPtr destructor);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "sqlite3_bind_int")]
        internal static extern int sqlite3_bind_int(IntPtr statement, int index, int value);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "sqlite3_column_text")]
        internal static extern IntPtr sqlite3_column_text(IntPtr statement, int column);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "sqlite3_column_bytes")]
        internal static extern int sqlite3_column_bytes(IntPtr statement, int column);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "sqlite3_column_int")]
        internal static extern int sqlite3_column_int(IntPtr statement, int column);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "sqlite3_changes")]
        internal static extern int sqlite3_changes(IntPtr database);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "sqlite3_busy_timeout")]
        internal static extern int sqlite3_busy_timeout(IntPtr database, int milliseconds);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "sqlite3_errmsg")]
        private static extern IntPtr sqlite3_errmsg(IntPtr database);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "sqlite3_exec", CharSet = CharSet.Ansi)]
        private static extern int sqlite3_exec(IntPtr database, [MarshalAs(UnmanagedType.LPUTF8Str)] string sql, IntPtr callback, IntPtr state, out IntPtr errorMessage);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "sqlite3_free")]
        private static extern void sqlite3_free(IntPtr value);

        internal static string ErrorMessage(IntPtr database) => Marshal.PtrToStringUTF8(sqlite3_errmsg(database)) ?? "Unknown SQLite error.";

        internal static string ColumnText(IntPtr statement, int column)
        {
            var pointer = sqlite3_column_text(statement, column);
            var length = sqlite3_column_bytes(statement, column);
            if (pointer == IntPtr.Zero || length == 0) return string.Empty;
            var bytes = new byte[length];
            Marshal.Copy(pointer, bytes, 0, length);
            return Encoding.UTF8.GetString(bytes);
        }

        internal static void Execute(IntPtr database, string sql)
        {
            var result = sqlite3_exec(database, sql, IntPtr.Zero, IntPtr.Zero, out var errorPointer);
            if (result == Ok) return;
            var message = errorPointer == IntPtr.Zero ? ErrorMessage(database) : Marshal.PtrToStringUTF8(errorPointer) ?? ErrorMessage(database);
            if (errorPointer != IntPtr.Zero) sqlite3_free(errorPointer);
            throw new IOException($"SQLite error {result}: {message}");
        }
    }
}
