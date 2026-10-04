using Automator.Core.Automation;

namespace Automator.Windows;

/// <summary>Converts low-level Windows key transitions into stable, layout-neutral events.</summary>
public sealed class KeyboardInputNormalizer
{
    private const uint LlkhfExtended = 0x01;
    private const uint WmKeyDown = 0x0100;
    private const uint WmKeyUp = 0x0101;
    private const uint WmSysKeyDown = 0x0104;
    private const uint WmSysKeyUp = 0x0105;
    private readonly object _heldKeysGate = new();
    private readonly HashSet<uint> _heldKeys = [];

    public KeyInputEvent Normalize(uint virtualKey, uint scanCode, uint flags, bool isDown, bool isRepeat, long sequence)
    {
        var key = NormalizeModifier(virtualKey, flags, scanCode);
        AutomationKeyModifiers modifiers;
        lock (_heldKeysGate)
        {
            if (isDown) _heldKeys.Add(key);
            else _heldKeys.Remove(key);
            modifiers = ReadModifiers();
        }

        return new KeyInputEvent(sequence, CodeFor(key), (int)key, isDown, isRepeat, modifiers);
    }

    public void ClearHeldKeys()
    {
        lock (_heldKeysGate) _heldKeys.Clear();
    }

    public static bool TryGetTransition(uint message, out bool isDown)
    {
        switch (message)
        {
            case WmKeyDown:
            case WmSysKeyDown:
                isDown = true;
                return true;
            case WmKeyUp:
            case WmSysKeyUp:
                isDown = false;
                return true;
            default:
                isDown = false;
                return false;
        }
    }

    private AutomationKeyModifiers ReadModifiers()
    {
        var modifiers = AutomationKeyModifiers.None;
        if (_heldKeys.Contains(0xA0) || _heldKeys.Contains(0xA1)) modifiers |= AutomationKeyModifiers.Shift;
        if (_heldKeys.Contains(0xA2) || _heldKeys.Contains(0xA3)) modifiers |= AutomationKeyModifiers.Control;
        if (_heldKeys.Contains(0xA4) || _heldKeys.Contains(0xA5)) modifiers |= AutomationKeyModifiers.Alt;
        if (_heldKeys.Contains(0x5B) || _heldKeys.Contains(0x5C)) modifiers |= AutomationKeyModifiers.Meta;
        return modifiers;
    }

    private static uint NormalizeModifier(uint key, uint flags, uint scanCode) => key switch
    {
        0x11 => (flags & LlkhfExtended) != 0 ? 0xA3u : 0xA2u,
        0x12 => (flags & LlkhfExtended) != 0 ? 0xA5u : 0xA4u,
        0x10 => scanCode == 0x36 ? 0xA1u : 0xA0u,
        _ => key
    };

    private static string CodeFor(uint key) => key switch
    {
        >= 0x41 and <= 0x5A => $"Key{(char)key}",
        >= 0x30 and <= 0x39 => $"Digit{(char)key}",
        >= 0x60 and <= 0x69 => $"Numpad{key - 0x60}",
        0xA0 => "ShiftLeft",
        0xA1 => "ShiftRight",
        0xA2 => "ControlLeft",
        0xA3 => "ControlRight",
        0xA4 => "AltLeft",
        0xA5 => "AltRight",
        0x5B => "MetaLeft",
        0x5C => "MetaRight",
        0x08 => "Backspace",
        0x09 => "Tab",
        0x0D => "Enter",
        0x1B => "Escape",
        0x20 => "Space",
        0x21 => "PageUp",
        0x22 => "PageDown",
        0x23 => "End",
        0x24 => "Home",
        0x25 => "ArrowLeft",
        0x26 => "ArrowUp",
        0x27 => "ArrowRight",
        0x28 => "ArrowDown",
        0x2D => "Insert",
        0x2E => "Delete",
        0x6A => "NumpadMultiply",
        0x6B => "NumpadAdd",
        0x6D => "NumpadSubtract",
        0x6E => "NumpadDecimal",
        0x6F => "NumpadDivide",
        0x70 => "F1",
        >= 0x71 and <= 0x7B => $"F{key - 0x6F}",
        0xBF => "Slash",
        _ => "Unidentified"
    };
}
