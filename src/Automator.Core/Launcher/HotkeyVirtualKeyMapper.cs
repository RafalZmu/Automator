namespace Automator.Core.Launcher;

/// <summary>Maps validated persisted key names to the Win32 virtual-key values both hosts use.</summary>
public static class HotkeyVirtualKeyMapper
{
    public static uint ToVirtualKey(string name)
    {
        if (!HotkeyKeyNameValidator.IsSupported(name))
            throw new ArgumentException($"Unsupported opener key '{name}'.", nameof(name));

        var normalized = name.ToUpperInvariant();
        if (normalized.StartsWith("KEY", StringComparison.Ordinal)
            && uint.TryParse(normalized.AsSpan(3), out var keyCode)) return keyCode;
        if (normalized.Length == 1) return char.ToUpperInvariant(normalized[0]);
        if (normalized[0] == 'F' && int.TryParse(normalized.AsSpan(1), out var functionKey))
            return (uint)(0x70 + functionKey - 1);

        return normalized switch
        {
            "RIGHTCONTROL" or "RCTRL" => 0xA3,
            "LEFTCONTROL" or "LCTRL" => 0xA2,
            "RIGHTALT" or "RALT" => 0xA5,
            "LEFTALT" or "LALT" => 0xA4,
            "RIGHTSHIFT" or "RSHIFT" => 0xA1,
            "LEFTSHIFT" or "LSHIFT" => 0xA0,
            "RIGHTWINDOWS" or "RWIN" => 0x5C,
            "LEFTWINDOWS" or "LWIN" => 0x5B,
            "SPACE" => 0x20,
            "CAPSLOCK" => 0x14,
            "ESCAPE" => 0x1B,
            "SLASH" or "/" => 0xBF,
            _ => throw new ArgumentException($"Unsupported opener key '{name}'.", nameof(name))
        };
    }
}
