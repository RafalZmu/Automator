namespace Automator.Core.Launcher;

public sealed class HotkeyTapRecognizer
{
    private readonly string _openerKey;
    private bool _candidate;
    private bool _otherKeyPressed;

    public HotkeyTapRecognizer(string openerKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(openerKey);
        _openerKey = openerKey;
    }

    public bool KeyDown(string key, bool isRepeat = false)
    {
        if (string.Equals(key, _openerKey, StringComparison.OrdinalIgnoreCase))
        {
            if (!isRepeat)
            {
                _candidate = true;
                _otherKeyPressed = false;
            }

            return false;
        }

        if (_candidate)
        {
            _otherKeyPressed = true;
        }

        return false;
    }

    public bool KeyUp(string key)
    {
        if (!string.Equals(key, _openerKey, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var shouldToggle = _candidate && !_otherKeyPressed;
        _candidate = false;
        _otherKeyPressed = false;
        return shouldToggle;
    }
}
