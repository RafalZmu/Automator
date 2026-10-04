namespace Automator.Core.Automation;

[Flags]
public enum AutomationKeyModifiers
{
    None = 0,
    Shift = 1 << 0,
    Control = 1 << 1,
    Alt = 1 << 2,
    Meta = 1 << 3,
}

/// <summary>A layout-neutral key transition from the host keyboard adapter.</summary>
public sealed record KeyInputEvent(
    long Sequence,
    string Code,
    int VirtualKey,
    bool IsDown,
    bool IsRepeat,
    AutomationKeyModifiers Modifiers);
