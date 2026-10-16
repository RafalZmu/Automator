using System.Text.Json;
using System.Text.RegularExpressions;
using Automator.Core.Automation;

namespace Automator.Application.Automation;

/// <summary>Typed parameter definitions and values shared by bundled templates and user script profiles.</summary>
public static class ScriptRunnerParameters
{
    public const int MaximumProfileParameters = 32;
    private static readonly Regex KeyPattern = new("^[a-z][a-z0-9_]{0,63}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>Validates declared parameters. Argument positions follow declaration order.</summary>
    public static void ValidateDefinitions(IReadOnlyList<ScriptRunnerTemplateParameter>? parameters, bool allowSensitive, int maximumCount)
    {
        if (parameters is null) return;
        if (parameters.Count > maximumCount) throw new InvalidDataException("Too many script parameters are declared.");
        var keys = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < parameters.Count; index++)
        {
            var parameter = parameters[index];
            if (parameter is null || parameter.Key is null || !KeyPattern.IsMatch(parameter.Key) || !keys.Add(parameter.Key)
                || string.IsNullOrWhiteSpace(parameter.Label) || parameter.Label.Length > 128
                || parameter.Description is not null && (string.IsNullOrWhiteSpace(parameter.Description) || parameter.Description.Length > 512)
                || parameter.Example is not null && (string.IsNullOrWhiteSpace(parameter.Example) || parameter.Example.Length > 256)
                || !Enum.IsDefined(parameter.Type) || parameter.ArgumentIndex != index
                || parameter.Sensitive && (!allowSensitive || parameter.Type != ScriptRunnerTemplateParameterType.Text || parameter.DefaultValue is not null)
                || parameter.Type == ScriptRunnerTemplateParameterType.Choice && !ValidChoices(parameter.Options)
                || parameter.Type != ScriptRunnerTemplateParameterType.Choice && parameter.Options is not null
                || parameter.DefaultValue is not null && !ValidDefault(parameter))
                throw new InvalidDataException("A script parameter definition is invalid.");
        }
    }

    /// <summary>Maps values to one process argument per parameter. Missing values use defaults.</summary>
    public static IReadOnlyList<string> Resolve(IReadOnlyList<ScriptRunnerTemplateParameter> parameters, JsonElement values, bool enforceRequired)
    {
        if (values.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Parameter values must be an object.");
        var known = parameters.Select(parameter => parameter.Key).ToHashSet(StringComparer.Ordinal);
        foreach (var property in values.EnumerateObject())
            if (!known.Contains(property.Name)) throw new InvalidDataException("Parameter value key is unknown.");
        var arguments = new string[parameters.Count];
        foreach (var parameter in parameters)
        {
            if (!values.TryGetProperty(parameter.Key, out var value) || value.ValueKind == JsonValueKind.Null)
            {
                var fallback = DefaultText(parameter.DefaultValue);
                if (fallback is null && parameter.Required && enforceRequired) throw new InvalidDataException($"{parameter.Label} is required.");
                arguments[parameter.ArgumentIndex] = fallback ?? string.Empty;
                continue;
            }
            arguments[parameter.ArgumentIndex] = ConvertValue(parameter, value, enforceRequired);
        }
        foreach (var parameter in parameters)
        {
            if (parameter.DefaultFromKey is null || !string.IsNullOrWhiteSpace(arguments[parameter.ArgumentIndex])) continue;
            var source = parameters.FirstOrDefault(item => item.Key == parameter.DefaultFromKey);
            if (source is not null && !string.IsNullOrWhiteSpace(arguments[source.ArgumentIndex]))
                arguments[parameter.ArgumentIndex] = Path.ChangeExtension(arguments[source.ArgumentIndex], parameter.DefaultFromExtension);
        }
        return arguments;
    }

    /// <summary>Checks values that will be remembered with a profile. Gaps in required values are allowed until a run.</summary>
    public static void ValidateSavedValues(IReadOnlyList<ScriptRunnerTemplateParameter> parameters, JsonElement values)
    {
        foreach (var parameter in parameters.Where(parameter => parameter.Sensitive))
            if (values.ValueKind == JsonValueKind.Object && values.TryGetProperty(parameter.Key, out _))
                throw new InvalidDataException($"{parameter.Label} is sensitive and is not saved with the profile.");
        Resolve(parameters, values, enforceRequired: false);
    }

    private static string ConvertValue(ScriptRunnerTemplateParameter parameter, JsonElement value, bool enforceRequired)
    {
        if (parameter.Type == ScriptRunnerTemplateParameterType.Boolean)
        {
            if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) throw new InvalidDataException($"{parameter.Label} must be a boolean.");
            return value.GetBoolean() ? "true" : "false";
        }
        if (value.ValueKind != JsonValueKind.String) throw new InvalidDataException($"{parameter.Label} must be text.");
        var input = value.GetString()!;
        if (input.Length > AutomationProcessLimits.MaximumArgumentLength) throw new InvalidDataException($"{parameter.Label} is invalid.");
        if (string.IsNullOrWhiteSpace(input))
        {
            if (parameter.Required && enforceRequired) throw new InvalidDataException($"{parameter.Label} is required.");
            return string.Empty;
        }
        if (parameter.Type is ScriptRunnerTemplateParameterType.File or ScriptRunnerTemplateParameterType.Directory && !Path.IsPathFullyQualified(input))
            throw new InvalidDataException($"{parameter.Label} must be an absolute path.");
        if (parameter.Type == ScriptRunnerTemplateParameterType.Choice && !(parameter.Options?.Contains(input) ?? false))
            throw new InvalidDataException($"{parameter.Label} is invalid.");
        return input;
    }

    private static string? DefaultText(object? value) => value switch
    {
        bool boolean => boolean ? "true" : "false",
        string text => text,
        JsonElement { ValueKind: JsonValueKind.True } => "true",
        JsonElement { ValueKind: JsonValueKind.False } => "false",
        JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
        _ => null,
    };

    private static bool ValidDefault(ScriptRunnerTemplateParameter parameter)
    {
        var text = DefaultText(parameter.DefaultValue);
        if (text is null) return false;
        if (parameter.Type == ScriptRunnerTemplateParameterType.Boolean) return text is "true" or "false";
        // Text-like inputs take text defaults only; a boolean default on them is a declaration mistake.
        if (parameter.DefaultValue is bool or JsonElement { ValueKind: JsonValueKind.True or JsonValueKind.False }) return false;
        return !string.IsNullOrWhiteSpace(text) && text.Length <= AutomationProcessLimits.MaximumArgumentLength
            && (parameter.Type != ScriptRunnerTemplateParameterType.Choice || parameter.Options?.Contains(text) == true);
    }

    private static bool ValidChoices(IReadOnlyList<string>? options) => options is { Count: >= 1 and <= 32 }
        && options.All(option => !string.IsNullOrWhiteSpace(option) && option.Length <= 128)
        && options.Distinct(StringComparer.Ordinal).Count() == options.Count;
}
