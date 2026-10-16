using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Automator.Core.Automation;

namespace Automator.Application.Automation;

public enum ScriptRunnerTemplateParameterType { Text, File, Directory, Boolean, Choice }

public sealed record ScriptRunnerTemplateParameter(
    string Key,
    string Label,
    ScriptRunnerTemplateParameterType Type,
    bool Required,
    int ArgumentIndex,
    string? Description = null,
    bool Sensitive = false,
    object? DefaultValue = null,
    IReadOnlyList<string>? Options = null,
    string? Example = null,
    // Catalog-only: an empty value becomes the path of another input with this extension (for example the .fdb path with .fbk).
    [property: JsonIgnore] string? DefaultFromKey = null,
    [property: JsonIgnore] string? DefaultFromExtension = null);

public sealed record ScriptRunnerTemplateDescriptor(
    string Id,
    int Version,
    string Name,
    string Description,
    IReadOnlyList<string> Tags,
    ScriptRunnerInterpreter Interpreter,
    string AssetId,
    ScriptRunnerOutputMode OutputMode,
    int TimeoutSeconds,
    IReadOnlyList<ScriptRunnerTemplateParameter> Parameters);

/// <summary>Application-owned metadata for bundled scripts. The renderer cannot register assets.</summary>
public static partial class ScriptRunnerTemplateCatalog
{
    private static readonly Regex IdPattern = new("^[a-z0-9][a-z0-9._-]{0,63}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly IReadOnlyList<ScriptRunnerTemplateDescriptor> Templates =
    [
        new("firebird-3-backup", 1, "Firebird 3 database backup",
            "Create a Firebird database backup.", ["database", "backup", "firebird"],
            ScriptRunnerInterpreter.Powershell, "firebird-3-backup", ScriptRunnerOutputMode.Text, 3600,
            [
                new("database", "Target database", ScriptRunnerTemplateParameterType.File, true, 0, "The .fdb file to back up.", Example: @"C:\Data\company.fdb"),
                new("backup", "Backup file", ScriptRunnerTemplateParameterType.File, false, 1, "Where to write the .fbk backup. Leave empty to save it beside the .fdb file with the same name. It may not exist yet.", Example: @"D:\Backups\company.fbk", DefaultFromKey: "database", DefaultFromExtension: ".fbk"),
                new("gbak", "gbak executable", ScriptRunnerTemplateParameterType.File, true, 2, "Firebird's backup tool.", Example: @"C:\Program Files\Firebird\Firebird_3_0\gbak.exe"),
                new("username", "Username", ScriptRunnerTemplateParameterType.Text, true, 3, Example: "SYSDBA"),
                new("password", "Password", ScriptRunnerTemplateParameterType.Text, true, 4, Sensitive: true),
            ]),
        new("firebird-3-backup-zip", 1, "Firebird 3 database backup and ZIP",
            "Create a Firebird database backup and ZIP archive.", ["database", "backup", "firebird"],
            ScriptRunnerInterpreter.Powershell, "firebird-3-backup-zip", ScriptRunnerOutputMode.Text, 3600,
            [
                new("database", "Target database", ScriptRunnerTemplateParameterType.File, true, 0, "The .fdb file to back up.", Example: @"C:\Data\company.fdb"),
                new("backup", "Backup file", ScriptRunnerTemplateParameterType.File, false, 1, "Where to write the .fbk backup. Leave empty to save it beside the .fdb file with the same name. It may not exist yet.", Example: @"D:\Backups\company.fbk", DefaultFromKey: "database", DefaultFromExtension: ".fbk"),
                new("archive", "ZIP file", ScriptRunnerTemplateParameterType.File, false, 2, "Where to write the ZIP of the backup. Leave empty to save it beside the .fdb file, named after the database.", Example: @"D:\Backups\company.zip", DefaultFromKey: "database", DefaultFromExtension: ".zip"),
                new("gbak", "gbak executable", ScriptRunnerTemplateParameterType.File, true, 3, "Firebird's backup tool.", Example: @"C:\Program Files\Firebird\Firebird_3_0\gbak.exe"),
                new("username", "Username", ScriptRunnerTemplateParameterType.Text, true, 4, Example: "SYSDBA"),
                new("password", "Password", ScriptRunnerTemplateParameterType.Text, true, 5, Sensitive: true),
            ]),
        new("fdb-stale-archive", 1, "Archive stale .fdb databases",
            "Zip each .fdb not accessed for a number of days into <name>.fdb.zip beside it, then delete the original. Preview only is on by default.",
            ["database", "archive", "firebird"],
            ScriptRunnerInterpreter.Powershell, "fdb-stale-archive", ScriptRunnerOutputMode.Text, 3600,
            [
                new("source", "Source folder", ScriptRunnerTemplateParameterType.Directory, true, 0, "Searched for .fdb files.", Example: @"D:\Databases"),
                new("days", "Days not accessed", ScriptRunnerTemplateParameterType.Text, true, 1, "Files not accessed for this many days are archived.", DefaultValue: "90", Example: "90"),
                new("subfolders", "Include subfolders", ScriptRunnerTemplateParameterType.Boolean, true, 2, DefaultValue: true),
                new("preview", "Preview only (change nothing)", ScriptRunnerTemplateParameterType.Boolean, true, 3, DefaultValue: true),
            ]),
        new("update-and-restart", 1, "Update Automator and restart",
            "Pull the latest changes from the remote into an Automator checkout, rebuild the packaged app, and restart it. The pull happens first and a failed pull leaves the running app alone.",
            ["automator", "update", "git", "build"],
            ScriptRunnerInterpreter.Powershell, "update-and-restart", ScriptRunnerOutputMode.Text, 600,
            [
                new("repository", "Automator checkout", ScriptRunnerTemplateParameterType.Directory, true, 0, "The git checkout that Automator was built from.", Example: @"C:\Automator"),
                new("rebuild", "Rebuild even if nothing new was pulled", ScriptRunnerTemplateParameterType.Boolean, true, 1, "Off: an up-to-date checkout is left alone and Automator keeps running.", DefaultValue: false),
            ])
    ];

    static ScriptRunnerTemplateCatalog() => Validate(Templates);

    public static IReadOnlyList<ScriptRunnerTemplateDescriptor> All => Templates;
    public static ScriptRunnerTemplateDescriptor? Get(string id) => Templates.FirstOrDefault(item => item.Id == id);

    public static void Validate(IReadOnlyList<ScriptRunnerTemplateDescriptor> templates)
    {
        if (templates is null || templates.Count > 64) throw new InvalidDataException("Template catalog is invalid.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var template in templates)
        {
            if (template is null || !ValidId(template.Id) || !ids.Add(template.Id) || template.Version < 1 ||
                !Bounded(template.Name, 128) || !Bounded(template.Description, 1024) ||
                !Enum.IsDefined(template.Interpreter) || !Enum.IsDefined(template.OutputMode) ||
                !ValidId(template.AssetId) || template.TimeoutSeconds is < 1 or > 3600 ||
                template.Tags is null || template.Tags.Count > 16 ||
                template.Tags.Any(tag => !Bounded(tag, 64)) ||
                template.Tags.Distinct(StringComparer.Ordinal).Count() != template.Tags.Count ||
                template.Parameters is null)
                throw new InvalidDataException("Template descriptor is invalid or duplicated.");
            ScriptRunnerParameters.ValidateDefinitions(template.Parameters, allowSensitive: true, maximumCount: AutomationProcessLimits.MaximumArgumentCount - 1);
        }
    }

    /// <summary>Validate untrusted run values and return one process argument per declared position.</summary>
    public static IReadOnlyList<string> MapArguments(ScriptRunnerTemplateDescriptor template, JsonElement values)
    {
        Validate([template]);
        return ScriptRunnerParameters.Resolve(template.Parameters, values, enforceRequired: true);
    }

    private static bool ValidId(string? value) => value is not null && IdPattern.IsMatch(value);
    private static bool Bounded(string? value, int max) => !string.IsNullOrWhiteSpace(value) && value.Length <= max;
}
