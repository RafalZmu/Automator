using System.Text.RegularExpressions;
using System.Text.Json;
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
    IReadOnlyList<string>? Options = null);

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
    private static readonly Regex KeyPattern = new("^[a-z][a-z0-9_]{0,63}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly IReadOnlyList<ScriptRunnerTemplateDescriptor> Templates =
    [
        new("firebird-3-backup-zip", 1, "Firebird 3 database backup and ZIP",
            "Create a Firebird database backup and ZIP archive.", ["database", "backup", "firebird"],
            ScriptRunnerInterpreter.Powershell, "firebird-3-backup-zip", ScriptRunnerOutputMode.Text, 3600,
            [
                new("database", "Target database", ScriptRunnerTemplateParameterType.File, true, 0),
                new("backup", "Backup file", ScriptRunnerTemplateParameterType.File, true, 1),
                new("archive", "ZIP file", ScriptRunnerTemplateParameterType.File, true, 2),
                new("gbak", "gbak executable", ScriptRunnerTemplateParameterType.File, true, 3),
                new("username", "Username", ScriptRunnerTemplateParameterType.Text, true, 4),
                new("password", "Password", ScriptRunnerTemplateParameterType.Text, true, 5, Sensitive: true),
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
                template.Parameters is null || template.Parameters.Count > AutomationProcessLimits.MaximumArgumentCount - 1)
                throw new InvalidDataException("Template descriptor is invalid or duplicated.");
            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < template.Parameters.Count; index++)
            {
                var parameter = template.Parameters[index];
                if (parameter is null || parameter.Key is null || !KeyPattern.IsMatch(parameter.Key) || !keys.Add(parameter.Key) ||
                    !Bounded(parameter.Label, 128) ||
                    parameter.Description is not null && !Bounded(parameter.Description, 512) ||
                    !Enum.IsDefined(parameter.Type) || parameter.ArgumentIndex != index ||
                    parameter.Sensitive && parameter.Type != ScriptRunnerTemplateParameterType.Text ||
                    parameter.Sensitive && parameter.DefaultValue is not null ||
                    parameter.DefaultValue is not null && !ValidDefault(parameter) ||
                    parameter.Type == ScriptRunnerTemplateParameterType.Choice && (parameter.Options is null || parameter.Options.Count is < 1 or > 32 ||
                        parameter.Options.Any(option => !Bounded(option, 128)) ||
                        parameter.Options.Distinct(StringComparer.Ordinal).Count() != parameter.Options.Count) ||
                    parameter.Type != ScriptRunnerTemplateParameterType.Choice && parameter.Options is not null)
                    throw new InvalidDataException("Template parameter or argument mapping is invalid.");
            }
        }
    }

    /// <summary>Validate untrusted run values and return one process argument per declared position.</summary>
    public static IReadOnlyList<string> MapArguments(ScriptRunnerTemplateDescriptor template, JsonElement values)
    {
        Validate([template]);
        if (values.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Template values must be an object.");
        var known = template.Parameters.Select(parameter => parameter.Key).ToHashSet(StringComparer.Ordinal);
        foreach (var property in values.EnumerateObject())
            if (!known.Contains(property.Name)) throw new InvalidDataException("Template value key is unknown.");
        var arguments = new string[template.Parameters.Count];
        foreach (var parameter in template.Parameters)
        {
            var hasValue = values.TryGetProperty(parameter.Key, out var value) && value.ValueKind != JsonValueKind.Null;
            if (!hasValue && parameter.DefaultValue is null)
            {
                if (parameter.Required) throw new InvalidDataException($"{parameter.Label} is required.");
                arguments[parameter.ArgumentIndex] = string.Empty;
                continue;
            }
            if (hasValue)
            {
                if (parameter.Type == ScriptRunnerTemplateParameterType.Boolean)
                {
                    if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                        throw new InvalidDataException($"{parameter.Label} must be a boolean.");
                    arguments[parameter.ArgumentIndex] = value.GetBoolean() ? "true" : "false";
                    continue;
                }
                if (value.ValueKind != JsonValueKind.String) throw new InvalidDataException($"{parameter.Label} must be text.");
                var input = value.GetString()!;
                var requiresAbsolutePath = parameter.Type is ScriptRunnerTemplateParameterType.File or ScriptRunnerTemplateParameterType.Directory;
                if (requiresAbsolutePath && !Path.IsPathFullyQualified(input))
                    throw new InvalidDataException($"{parameter.Label} must be an absolute path.");
                if (input.Length > AutomationProcessLimits.MaximumArgumentLength || parameter.Required && string.IsNullOrWhiteSpace(input) ||
                    parameter.Type == ScriptRunnerTemplateParameterType.Choice && !(parameter.Options?.Contains(input) ?? false))
                    throw new InvalidDataException($"{parameter.Label} is invalid.");
                arguments[parameter.ArgumentIndex] = input;
            }
            else arguments[parameter.ArgumentIndex] = parameter.DefaultValue is bool boolean
                ? boolean ? "true" : "false" : (string)parameter.DefaultValue!;
        }
        return arguments;
    }

    private static bool ValidDefault(ScriptRunnerTemplateParameter parameter)
    {
        if (parameter.Type == ScriptRunnerTemplateParameterType.Boolean) return parameter.DefaultValue is bool;
        return parameter.DefaultValue is string value && !string.IsNullOrWhiteSpace(value) &&
            value.Length <= AutomationProcessLimits.MaximumArgumentLength &&
            (parameter.Type != ScriptRunnerTemplateParameterType.Choice || parameter.Options?.Contains(value) == true);
    }

    private static bool ValidId(string? value) => value is not null && IdPattern.IsMatch(value);
    private static bool Bounded(string? value, int max) => !string.IsNullOrWhiteSpace(value) && value.Length <= max;
}
