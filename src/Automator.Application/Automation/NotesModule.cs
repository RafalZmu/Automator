using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Automator.Application.Launcher;
using Automator.Core.Automation;
using Automator.Core.Plugins;

namespace Automator.Application.Automation;

/// <summary>
/// Slot ten stores short markdown notes as one record each in the module's partition of the SQLite library. The renderer lists,
/// saves, and deletes notes through module actions, renders the markdown, and derives the checkbox status from the text, so the
/// host only validates and stores the records.
/// </summary>
public sealed class NotesModule : ILauncherTabModuleProvider
{
    public const string IdValue = "notes";
    public const int SettingsVersionValue = 1;
    public const int SlotValue = 10;
    public const string NoteCollection = "notes";
    public const int MaximumNotes = 50;
    public const int MaximumTitleLength = 128;
    /// <summary>
    /// With <see cref="MaximumNotes"/> this keeps the list result under the 512 KiB structured-data limit even when every character is
    /// a three-byte UTF-8 character, because the result is serialized without escaping non-ASCII text.
    /// </summary>
    public const int MaximumBodyLength = 3000;

    /// <summary>Keeps the published open-task value under the 2048-character module state value limit.</summary>
    public const int MaximumOpenTaskStateLength = 2000;
    public const string OpenTasksStateKey = "openTasks";
    private const int MaximumTaskTextLength = 100;
    private const int MaximumTaskNoteLength = 40;
    private const int MaximumOpenTasksConsidered = 200;

    private static readonly Regex FencePattern = new(@"^\s*(`{3,}|~{3,})", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex UntickedTaskPattern = new(@"^\s*(?:[-*+]|\d+[.)])\s+\[ \](?:\s(.*))?$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex LineBreakPattern = new(@"\r\n|\r|\n", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex IdPattern = new("^[a-z0-9][a-z0-9._-]{0,63}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
    private static readonly AutomationCapabilityRequirement LibraryCapability = new(AutomationCapabilityIds.LibraryStorage, 1);

    public AutomationModuleDefinition Definition { get; } = new(SlotValue, IdValue, "Quick notes", "notebook", "notes", false,
        1, SettingsVersionValue, [LibraryCapability],
        [
            new("listNotes", 1, [LibraryCapability]),
            new("saveNote", 1, [LibraryCapability]),
            new("deleteNote", 1, [LibraryCapability]),
        ]);
    public int ContractVersion => Definition.ContractVersion;
    public string Id => Definition.Id;
    public string Title => Definition.Title;
    public LauncherModuleState CreateInitialState() => new(Id, LauncherTabRegistry.Version,
        new Dictionary<string, string>(StringComparer.Ordinal) { ["status"] = "ready" });

    public JsonElement CreateDefaultSettings() => JsonSerializer.SerializeToElement(new { });

    public JsonElement MigrateSettings(int fromVersion, JsonElement value) =>
        throw new InvalidOperationException($"Quick notes settings version {fromVersion} cannot be migrated.");

    public async ValueTask<AutomationResult> ExecuteAsync(string actionId, JsonElement input, JsonElement moduleSettings,
        AutomationServicesContext services, CancellationToken cancellationToken)
    {
        try
        {
            return actionId switch
            {
                "listNotes" => await ListAsync(services, cancellationToken).ConfigureAwait(false),
                "saveNote" => await SaveAsync(input, services, cancellationToken).ConfigureAwait(false),
                "deleteNote" => await DeleteAsync(input, services, cancellationToken).ConfigureAwait(false),
                _ => Error("Unknown Quick notes action."),
            };
        }
        catch (OperationCanceledException) { throw; }
        catch (NoteValidationException exception) { return Error(exception.Message); }
        catch (Exception exception) when (exception is JsonException or InvalidDataException or InvalidOperationException)
        {
            return Error("The note data is invalid.");
        }
    }

    private static async Task<AutomationResult> ListAsync(AutomationServicesContext services, CancellationToken cancellationToken)
    {
        var records = await services.Library!.ListAsync(NoteCollection, cancellationToken).ConfigureAwait(false);
        var notes = records.Select(TryReadNote).Where(note => note is not null).Cast<StoredNote>()
            .OrderBy(note => note.Created, StringComparer.Ordinal).ThenBy(note => note.Id, StringComparer.Ordinal)
            .Select(note => new { id = note.Id, title = note.Title, body = note.Body }).ToArray();
        return Result(AutomationStatus.Success, $"{notes.Length} note{(notes.Length == 1 ? string.Empty : "s")}.", new { notes });
    }

    private static async Task<AutomationResult> SaveAsync(JsonElement input, AutomationServicesContext services, CancellationToken cancellationToken)
    {
        var id = ReadString(input, "id");
        if (id is null || !IdPattern.IsMatch(id)) throw new NoteValidationException("The note needs a valid key.");
        var title = ReadString(input, "title")?.Trim();
        if (string.IsNullOrWhiteSpace(title) || title.Length > MaximumTitleLength || title.Any(char.IsControl))
            throw new NoteValidationException($"The note needs a title of at most {MaximumTitleLength} characters.");
        var body = ReadString(input, "body") ?? string.Empty;
        if (body.Length > MaximumBodyLength || body.Any(character => char.IsControl(character) && character is not ('\t' or '\n' or '\r')))
            throw new NoteValidationException($"The note text can have at most {MaximumBodyLength} characters.");

        var previous = await services.Library!.GetAsync(NoteCollection, id, cancellationToken).ConfigureAwait(false);
        var created = previous is null ? null : TryReadNote(previous)?.Created;
        if (previous is null)
        {
            var count = (await services.Library.ListAsync(NoteCollection, cancellationToken).ConfigureAwait(false)).Count;
            if (count >= MaximumNotes) throw new NoteValidationException($"At most {MaximumNotes} notes can be saved.");
        }

        var stored = new StoredNote(id, title, body, created ?? DateTimeOffset.UtcNow.ToString("O"));
        await services.Library.UpsertAsync(NoteCollection, id, SettingsVersionValue,
            JsonSerializer.SerializeToElement(stored, JsonOptions), cancellationToken).ConfigureAwait(false);
        return Result(AutomationStatus.Success, "Note saved.", new { note = new { id, title, body } });
    }

    private static async Task<AutomationResult> DeleteAsync(JsonElement input, AutomationServicesContext services, CancellationToken cancellationToken)
    {
        var id = ReadString(input, "id");
        if (id is null || !IdPattern.IsMatch(id)) throw new NoteValidationException("The note needs a valid key.");
        var deleted = await services.Library!.DeleteAsync(NoteCollection, id, cancellationToken).ConfigureAwait(false);
        return Result(deleted ? AutomationStatus.Success : AutomationStatus.Information,
            deleted ? "Note deleted." : "The note was already removed.", new { id, deleted });
    }

    private static StoredNote? TryReadNote(AutomationLibraryRecord record)
    {
        try
        {
            var note = record.Data.Deserialize<StoredNote>(JsonOptions);
            return note is { Id: not null, Title: not null, Body: not null, Created: not null } && note.Id == record.Id
                && IdPattern.IsMatch(note.Id) && note.Title.Length <= MaximumTitleLength && note.Body.Length <= MaximumBodyLength
                ? note : null;
        }
        catch (JsonException) { return null; }
    }

    private static string? ReadString(JsonElement input, string name) =>
        input.ValueKind == JsonValueKind.Object && input.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() : null;

    private static AutomationResult Error(string message) => Result(AutomationStatus.Error, message, new { });

    private static AutomationResult Result(AutomationStatus status, string message, object data) => new(
        AutomationTabContract.CurrentVersion, status, message, JsonSerializer.SerializeToElement(data, JsonOptions), []);

    /// <summary>
    /// Builds the JSON the launch panel shows as open tasks: every unticked markdown checkbox outside fenced code, oldest note first.
    /// The text is shortened and the list cut so the value always fits one module state value; <c>total</c> still counts every task.
    /// </summary>
    public static string CreateOpenTaskStateValue(IEnumerable<AutomationLibraryRecord> records)
    {
        var tasks = new List<(string Note, string Text)>();
        var total = 0;
        var notes = records.Select(TryReadNote).Where(note => note is not null).Cast<StoredNote>()
            .OrderBy(note => note.Created, StringComparer.Ordinal).ThenBy(note => note.Id, StringComparer.Ordinal);
        foreach (var note in notes)
        {
            foreach (var text in ReadOpenTasks(note.Body))
            {
                total++;
                if (tasks.Count < MaximumOpenTasksConsidered) tasks.Add((Shorten(note.Title, MaximumTaskNoteLength), Shorten(text, MaximumTaskTextLength)));
            }
        }

        var kept = new List<object>();
        var json = Serialize(total, kept);
        foreach (var (noteTitle, text) in tasks)
        {
            kept.Add(new { note = noteTitle, text });
            var candidate = Serialize(total, kept);
            if (candidate.Length > MaximumOpenTaskStateLength)
            {
                kept.RemoveAt(kept.Count - 1);
                break;
            }
            json = candidate;
        }
        return json;
    }

    private static string Serialize(int total, List<object> tasks) =>
        JsonSerializer.Serialize(new { total, tasks }, JsonOptions);

    private static IEnumerable<string> ReadOpenTasks(string body)
    {
        char? fence = null;
        foreach (var line in LineBreakPattern.Split(body))
        {
            var fenceMatch = FencePattern.Match(line);
            if (fence is null)
            {
                if (fenceMatch.Success) { fence = fenceMatch.Groups[1].Value[0]; continue; }
            }
            else
            {
                if (fenceMatch.Success && fenceMatch.Groups[1].Value[0] == fence
                    && line.Trim().Replace("`", string.Empty).Replace("~", string.Empty).Length == 0) fence = null;
                continue;
            }

            var task = UntickedTaskPattern.Match(line);
            if (!task.Success) continue;
            var text = string.Join(' ', task.Groups[1].Value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            if (text.Length > 0) yield return text;
        }
    }

    private static string Shorten(string value, int limit)
    {
        if (value.Length <= limit) return value;
        var end = char.IsHighSurrogate(value[limit - 2]) ? limit - 2 : limit - 1;
        return value[..end] + "\u2026";
    }

    private sealed record StoredNote(string Id, string Title, string Body, string Created);

    private sealed class NoteValidationException(string message) : Exception(message);
}
