using System.Globalization;

namespace Automator.Core.Launcher;

/// <summary>Validates key names accepted by the platform-neutral opener configuration.</summary>
public static class HotkeyKeyNameValidator
{
    private static readonly HashSet<string> NamedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "RightControl", "Rctrl", "LeftControl", "Lctrl",
        "RightAlt", "Ralt", "LeftAlt", "Lalt",
        "RightShift", "Rshift", "LeftShift", "Lshift", "RightWindows", "Rwin", "LeftWindows", "Lwin",
        "Space", "CapsLock", "Escape", "Slash", "/"
    };

    public static bool IsSupported(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var candidate = name;
        if (NamedKeys.Contains(candidate)) return true;
        if (candidate.Length == 1 && IsAsciiAlphaNumeric(candidate[0])) return true;
        if (candidate.Length is 2 or 3
            && candidate[0] is 'F' or 'f'
            && int.TryParse(candidate.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out var functionKey)
            && functionKey is >= 1 and <= 12
            && string.Equals(candidate, $"F{functionKey}", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return candidate.StartsWith("Key", StringComparison.OrdinalIgnoreCase)
            && uint.TryParse(candidate.AsSpan(3), NumberStyles.None, CultureInfo.InvariantCulture, out var virtualKey)
            && virtualKey is >= 1 and <= byte.MaxValue;
    }

    private static bool IsAsciiAlphaNumeric(char character) =>
        character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9';
}
