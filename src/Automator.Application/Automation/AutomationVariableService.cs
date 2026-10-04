using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Automator.Application.Automation;

public sealed record AutomationVariableSnapshot(int Version, IReadOnlyDictionary<string, JsonElement> Values,
    IReadOnlyList<string> MigrationConflicts);

public interface IAutomationVariableProvider
{
    Task<AutomationVariableSnapshot> GetAsync(CancellationToken cancellationToken);
}

/// <summary>Host-owned ordinary JSON values. Credential storage is deliberately separate.</summary>
public sealed class AutomationVariableService(IAutomationLibraryStore library) : IAutomationVariableProvider
{
    public const string ModuleId = "host-variables";
    public const string Collection = "values";
    public const string RecordId = "current";
    public const int MaximumBytes = 64 * 1024;
    private static readonly Regex Name = new("^[A-Za-z_][A-Za-z0-9_.-]{0,191}$", RegexOptions.CultureInvariant);
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<AutomationVariableSnapshot> GetAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try { return await ReadAndMigrateAsync(cancellationToken).ConfigureAwait(false); }
        finally { _gate.Release(); }
    }

    public async Task<AutomationVariableSnapshot> SetAsync(IReadOnlyDictionary<string, JsonElement> values, CancellationToken cancellationToken)
    {
        Validate(values);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await SaveAsync(values, cancellationToken).ConfigureAwait(false);
            return await ReadAndMigrateAsync(cancellationToken).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    public static void Validate(IReadOnlyDictionary<string, JsonElement> values)
    {
        if (values is null || values.Count > 256) throw new InvalidDataException("Global variables allow at most 256 entries.");
        foreach (var (name, value) in values)
        {
            if (!Name.IsMatch(name) || name is "input" or "variables" or "__proto__" or "constructor" or "prototype"
                || name.StartsWith("secret.", StringComparison.OrdinalIgnoreCase) || name.StartsWith("system.", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("A global variable name is invalid or reserved.");
            if (value.ValueKind == JsonValueKind.Undefined) throw new InvalidDataException("A global variable must contain a JSON value.");
        }
        if (Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(values)) > MaximumBytes)
            throw new InvalidDataException("Global variables exceed the 64 KiB limit.");
    }

    public static void ValidateReferences(IReadOnlyDictionary<string, string>? references)
    {
        if (references is null || references.Count > 64) throw new InvalidDataException("Workflow variable references are invalid.");
        foreach (var (alias, name) in references)
            if (!Regex.IsMatch(alias, "^[A-Za-z_][A-Za-z0-9_.-]{0,63}$", RegexOptions.CultureInvariant)
                || name is null || !Name.IsMatch(name) || name is "input" or "variables" or "__proto__" or "constructor" or "prototype"
                || name.StartsWith("secret.", StringComparison.OrdinalIgnoreCase) || name.StartsWith("system.", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("A workflow variable reference is invalid.");
    }

    private async Task<AutomationVariableSnapshot> ReadAndMigrateAsync(CancellationToken cancellationToken)
    {
        var current = await library.GetAsync(ModuleId, Collection, RecordId, cancellationToken).ConfigureAwait(false);
        var values = current is null ? new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            : current.SchemaVersion != 1 ? throw new InvalidDataException("Global variable schema is unsupported.")
            : current.Data.Deserialize<Dictionary<string, JsonElement>>() ?? throw new InvalidDataException("Global variables are invalid.");
        Validate(values);
        var conflicts = new List<string>();
        var workflows = await library.ListAsync("workflows", "profiles", cancellationToken).ConfigureAwait(false);
        foreach (var record in workflows)
        {
            if (record.SchemaVersion != 1 || record.Data.ValueKind != JsonValueKind.Object) continue;
            var node = JsonNode.Parse(record.Data.GetRawText())!.AsObject();
            if (node["variables"] is not JsonObject local || local.Count == 0) continue;
            var references = node["variableReferences"] as JsonObject ?? new JsonObject();
            var changed = false;
            foreach (var (key, value) in local.ToArray())
            {
                var name = $"workflow.{record.Id}.{key}";
                var candidate = JsonSerializer.SerializeToElement(value);
                if (values.TryGetValue(name, out var existing) && !JsonNode.DeepEquals(JsonNode.Parse(existing.GetRawText()), value))
                { conflicts.Add(name); continue; }
                var next = new Dictionary<string, JsonElement>(values, StringComparer.Ordinal) { [name] = candidate };
                try { Validate(next); }
                catch (InvalidDataException) { conflicts.Add(name); continue; }
                values = next;
                // Save the value before removing the legacy value; interruption can safely retry.
                await SaveAsync(values, cancellationToken).ConfigureAwait(false);
                references[key] = name;
                local.Remove(key);
                changed = true;
            }
            if (changed)
            {
                node["variableReferences"] = references;
                await library.UpsertAsync(record with { Data = JsonSerializer.SerializeToElement(node), UpdatedUtc = DateTimeOffset.UtcNow }, cancellationToken).ConfigureAwait(false);
            }
        }
        return new(1, values, conflicts.Distinct(StringComparer.Ordinal).Take(256).ToArray());
    }

    private Task SaveAsync(IReadOnlyDictionary<string, JsonElement> values, CancellationToken cancellationToken) =>
        library.UpsertAsync(new(ModuleId, Collection, RecordId, 1, JsonSerializer.SerializeToElement(values), DateTimeOffset.UtcNow), cancellationToken);
}

public static class AutomationVariableInterpolation
{
    private static readonly Regex Placeholder = new(@"\{\{variables\.([A-Za-z_][A-Za-z0-9_.-]{0,191})\}\}", RegexOptions.CultureInvariant);
    public static string Expand(string text, IReadOnlyDictionary<string, JsonElement> values)
    {
        var builder = new StringBuilder();
        var offset = 0;
        var bytes = 0;
        void Append(string value)
        {
            bytes = checked(bytes + Encoding.UTF8.GetByteCount(value));
            if (bytes > 1024 * 1024) throw new InvalidDataException("Expanded content exceeds the 1 MiB limit.");
            builder.Append(value);
        }
        foreach (Match match in Placeholder.Matches(text))
        {
            Append(text[offset..match.Index]);
            if (!values.TryGetValue(match.Groups[1].Value, out var value))
                throw new InvalidDataException("A referenced global variable is missing. Check Variables in Options.");
            Append(value.ValueKind == JsonValueKind.String ? value.GetString()! : value.GetRawText());
            offset = match.Index + match.Length;
        }
        Append(text[offset..]);
        return builder.ToString();
    }
}
