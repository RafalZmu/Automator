namespace Automator.Core.Launcher;

public sealed record AppBinding
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;

    public AppBinding() { }

    public AppBinding(string id, string name, string targetPath, string alias, string arguments = "")
    {
        Id = id;
        Name = name;
        TargetPath = targetPath;
        Alias = alias;
        Arguments = arguments;
    }
}

public enum AliasMatchKind
{
    None,
    Partial,
    Exact,
    AmbiguousExact
}

public sealed record AliasMatch(AliasMatchKind Kind, AppBinding? Binding = null);

public static class AliasMatcher
{
    public static AliasMatch Resolve(string input, IEnumerable<AppBinding> bindings)
    {
        var query = input.Trim();
        if (query.Length == 0)
        {
            return new AliasMatch(AliasMatchKind.None);
        }

        var matches = bindings
            .Where(binding => binding.Alias.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var exact = matches
            .Where(binding => string.Equals(binding.Alias, query, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (exact.Length > 1 || (exact.Length == 1 && matches.Length > 1))
        {
            return new AliasMatch(AliasMatchKind.AmbiguousExact);
        }

        if (exact.Length == 1)
        {
            return new AliasMatch(AliasMatchKind.Exact, exact[0]);
        }

        return matches.Length > 0
            ? new AliasMatch(AliasMatchKind.Partial)
            : new AliasMatch(AliasMatchKind.None);
    }
}

public enum AliasValidationError
{
    Empty,
    LettersOnly,
    Duplicate
}

public static class AliasValidator
{
    public static AliasValidationError? Validate(string? alias, IEnumerable<string> existingAliases)
    {
        var candidate = alias?.Trim() ?? string.Empty;
        if (candidate.Length == 0)
        {
            return AliasValidationError.Empty;
        }

        if (candidate.Any(character => character is < 'A' or > 'Z' && character is < 'a' or > 'z'))
        {
            return AliasValidationError.LettersOnly;
        }

        return existingAliases.Any(existing => string.Equals(existing, candidate, StringComparison.OrdinalIgnoreCase))
            ? AliasValidationError.Duplicate
            : null;
    }
}

public static class TabKeyParser
{
    public static int? Parse(char key) => key is >= '1' and <= '9' ? key - '1' : null;
}

public enum LaunchAction
{
    Minimize,
    RestoreAndMaximize,
    StartAndMaximize
}

public static class LaunchActionResolver
{
    public static LaunchAction Resolve(bool isRunning, bool isForeground, bool isMinimized)
    {
        if (!isRunning)
        {
            return LaunchAction.StartAndMaximize;
        }

        return isForeground && !isMinimized
            ? LaunchAction.Minimize
            : LaunchAction.RestoreAndMaximize;
    }
}
