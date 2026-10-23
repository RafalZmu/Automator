using System.Text;
using System.Text.RegularExpressions;

namespace Automator.Application.Automation;

/// <summary>A parameter read from a script's own source. It is informational and never becomes a typed input.</summary>
public sealed record DetectedScriptParameter(
    string Name,
    string Type,
    bool Required,
    string? Description,
    string? DefaultValue,
    IReadOnlyList<string>? Options);

/// <summary>
/// Reads the parameters a script declares without registering it: the PowerShell <c>param()</c> block with its
/// <c>.PARAMETER</c> help, or <c>#  @param name (flags): description</c> header comments for any interpreter.
/// </summary>
public static class ScriptRunnerParameterInspector
{
    public const int MaximumSourceBytes = 256 * 1024;
    public const int MaximumParameters = 64;
    private static readonly Regex ParamStart = new(@"(?im)^[ \t]*(?:\[[^\r\n\]]*\][ \t]*)*param[ \t\r\n]*\(", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex SecretName = new("(?i)token|secret|passw|key|credential", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex VariableName = new(@"^\s*\$([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex MandatoryTrue = new(@"(?i)\bMandatory\b(?!\s*=\s*\$false)", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex ValidateSet = new(@"(?is)ValidateSet\s*\((.*?)\)\s*\]", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex QuotedItem = new(@"'((?:[^']|'')*)'|""([^""]*)""", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex HelpParameter = new(@"(?ims)^[ \t]*#?[ \t]*\.PARAMETER[ \t]+(\S+)[ \t]*\r?\n(.*?)(?=^[ \t]*\.[A-Z]+\b|#>|\z)", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex HeaderParam = new(@"^[ \t]*(?:#|//)[ \t]*@param[ \t]+([A-Za-z_][A-Za-z0-9_-]*)[ \t]*(?:\(([^)]*)\))?[ \t]*(?:[:\-][ \t]*)?(.*)$", RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>Returns the parameters declared in the script at <paramref name="scriptPath"/>. An unreadable or oversized file yields none.</summary>
    public static IReadOnlyList<DetectedScriptParameter> InspectFile(string scriptPath, ScriptRunnerInterpreter interpreter)
    {
        using var stream = new FileStream(scriptPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var buffer = new byte[(int)Math.Min(stream.Length, MaximumSourceBytes)];
        var read = 0;
        while (read < buffer.Length)
        {
            var count = stream.Read(buffer, read, buffer.Length - read);
            if (count == 0) break;
            read += count;
        }
        return Inspect(Encoding.UTF8.GetString(buffer, 0, read), interpreter);
    }

    public static IReadOnlyList<DetectedScriptParameter> Inspect(string source, ScriptRunnerInterpreter interpreter)
    {
        source = source.TrimStart('﻿');
        var found = interpreter == ScriptRunnerInterpreter.Powershell ? InspectPowerShell(source) : new List<DetectedScriptParameter>();
        if (found.Count == 0) found = InspectHeader(source);
        return found.Take(MaximumParameters).ToArray();
    }

    private static List<DetectedScriptParameter> InspectPowerShell(string source)
    {
        var result = new List<DetectedScriptParameter>();
        var start = ParamStart.Match(source);
        if (!start.Success) return result;
        var body = ReadBalanced(source, start.Index + start.Length);
        if (body is null) return result;
        var help = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in HelpParameter.Matches(source))
        {
            var text = Regex.Replace(match.Groups[2].Value, @"\s+", " ").Trim();
            if (text.Length > 0) help[match.Groups[1].Value.TrimStart('-')] = Truncate(text, 512);
        }
        foreach (var segment in SplitTopLevel(body))
        {
            var rest = segment;
            var attributes = new StringBuilder();
            string? type = null;
            while (true)
            {
                rest = rest.TrimStart();
                if (rest.Length == 0 || rest[0] != '[') break;
                var end = FindClosing(rest, 0, '[', ']');
                if (end < 0) break;
                var attribute = rest.Substring(1, end - 1);
                attributes.Append('[').Append(attribute).Append(']');
                if (!attribute.Contains('(', StringComparison.Ordinal)) type = attribute.Trim();
                rest = rest[(end + 1)..];
            }
            var name = VariableName.Match(rest);
            if (!name.Success) continue;
            var attributeText = attributes.ToString();
            string? defaultValue = null;
            var equals = rest.IndexOf('=', StringComparison.Ordinal);
            if (equals >= 0)
            {
                var raw = rest[(equals + 1)..].Trim();
                if (raw.Length > 0 && raw.Length <= 256 && !raw.StartsWith('$') && !raw.StartsWith('(') && !raw.StartsWith('@'))
                    defaultValue = raw.Trim('\'', '"');
            }
            IReadOnlyList<string>? options = null;
            var set = ValidateSet.Match(attributeText);
            if (set.Success)
            {
                var items = QuotedItem.Matches(set.Groups[1].Value).Select(item => item.Groups[1].Success ? item.Groups[1].Value.Replace("''", "'", StringComparison.Ordinal) : item.Groups[2].Value)
                    .Where(item => item.Length > 0 && item.Length <= 128).Distinct(StringComparer.Ordinal).Take(32).ToArray();
                if (items.Length > 0) options = items;
            }
            var kind = options is not null ? "choice"
                : type is not null && (type.Equals("switch", StringComparison.OrdinalIgnoreCase) || type.Equals("bool", StringComparison.OrdinalIgnoreCase)) ? "boolean"
                : "text";
            if (kind == "boolean" || SecretName.IsMatch(name.Groups[1].Value)) defaultValue = null;
            help.TryGetValue(name.Groups[1].Value, out var description);
            result.Add(new DetectedScriptParameter(name.Groups[1].Value, kind, MandatoryTrue.IsMatch(attributeText), description, defaultValue, options));
            if (result.Count >= MaximumParameters) break;
        }
        return result;
    }

    private static List<DetectedScriptParameter> InspectHeader(string source)
    {
        var result = new List<DetectedScriptParameter>();
        foreach (Match match in HeaderParam.Matches(source))
        {
            var flags = match.Groups[2].Success
                ? match.Groups[2].Value.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(flag => flag.ToLowerInvariant()).ToArray()
                : Array.Empty<string>();
            var kind = flags.Contains("flag") || flags.Contains("boolean") || flags.Contains("switch") ? "boolean"
                : flags.Contains("file") ? "file"
                : flags.Contains("folder") || flags.Contains("directory") ? "directory"
                : "text";
            var description = match.Groups[3].Value.Trim();
            result.Add(new DetectedScriptParameter(match.Groups[1].Value, kind, flags.Contains("required"),
                description.Length == 0 ? null : Truncate(description, 512), null, null));
            if (result.Count >= MaximumParameters) break;
        }
        return result;
    }

    /// <summary>Returns the text between the already-consumed opening parenthesis and its match, or null when unbalanced.</summary>
    private static string? ReadBalanced(string text, int from)
    {
        var depth = 1;
        for (var index = from; index < text.Length; index++)
        {
            var current = text[index];
            if (current is '\'' or '"') { index = SkipString(text, index); if (index < 0) return null; continue; }
            if (current == '#') { index = SkipComment(text, index); continue; }
            if (current == '(') depth++;
            else if (current == ')' && --depth == 0) return text.Substring(from, index - from);
        }
        return null;
    }

    private static IEnumerable<string> SplitTopLevel(string body)
    {
        var depth = 0;
        var begin = 0;
        for (var index = 0; index < body.Length; index++)
        {
            var current = body[index];
            if (current is '\'' or '"') { index = SkipString(body, index); if (index < 0) yield break; continue; }
            if (current == '#') { index = SkipComment(body, index); continue; }
            if (current is '(' or '[' or '{') depth++;
            else if (current is ')' or ']' or '}') depth--;
            else if (current == ',' && depth == 0)
            {
                yield return StripComments(body[begin..index]);
                begin = index + 1;
            }
        }
        if (begin < body.Length) yield return StripComments(body[begin..]);
    }

    private static string StripComments(string text)
    {
        var builder = new StringBuilder();
        for (var index = 0; index < text.Length; index++)
        {
            var current = text[index];
            if (current is '\'' or '"')
            {
                var end = SkipString(text, index);
                if (end < 0) { builder.Append(text, index, text.Length - index); break; }
                builder.Append(text, index, end - index + 1);
                index = end;
            }
            else if (current == '#') index = SkipComment(text, index);
            else builder.Append(current);
        }
        return builder.ToString();
    }

    /// <summary>Index of the closing quote of the string that opens at <paramref name="open"/>, or -1 when it never closes.</summary>
    private static int SkipString(string text, int open)
    {
        var quote = text[open];
        for (var index = open + 1; index < text.Length; index++)
        {
            if (quote == '"' && text[index] == '`') { index++; continue; }
            if (text[index] != quote) continue;
            if (quote == '\'' && index + 1 < text.Length && text[index + 1] == '\'') { index++; continue; }
            return index;
        }
        return -1;
    }

    private static int SkipComment(string text, int hash)
    {
        var newline = text.IndexOf('\n', hash);
        return newline < 0 ? text.Length : newline;
    }

    private static int FindClosing(string text, int open, char opener, char closer)
    {
        var depth = 0;
        for (var index = open; index < text.Length; index++)
        {
            var current = text[index];
            if (current is '\'' or '"') { index = SkipString(text, index); if (index < 0) return -1; continue; }
            if (current == opener) depth++;
            else if (current == closer && --depth == 0) return index;
        }
        return -1;
    }

    private static string Truncate(string value, int length) => value.Length <= length ? value : value[..length];
}
