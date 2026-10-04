using Automator.Application.Logging;
using Automator.Windows.Interop;

namespace Automator.Windows;

/// <summary>Applies native Windows 11 corner geometry and removes the DWM border.</summary>
public sealed class NativeChromeController(IApplicationLog? log = null)
{
    private readonly IApplicationLog _log = log ?? NullApplicationLog.Instance;

    public void Apply(IntPtr window)
    {
        if (window == IntPtr.Zero || !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) return;
        var rounded = WindowsNativeMethods.DwmWindowCornerRound;
        var roundedResult = WindowsNativeMethods.DwmSetWindowAttribute(window,
            WindowsNativeMethods.DwmaWindowCornerPreference, ref rounded, sizeof(uint));
        var noBorder = WindowsNativeMethods.DwmColorNone;
        var borderResult = WindowsNativeMethods.DwmSetWindowAttribute(window,
            WindowsNativeMethods.DwmaBorderColor, ref noBorder, sizeof(uint));
        if (roundedResult < 0 || borderResult < 0)
        {
            _log.Write(ApplicationLogLevel.Warning, "Windows.NativeChromePartial", "DWM did not accept all rounded-corner and border preferences.",
                properties: new Dictionary<string, object?> { ["roundedResult"] = roundedResult, ["borderResult"] = borderResult });
            return;
        }
        _log.Write(ApplicationLogLevel.Information, "Windows.NativeChromeApplied", "Applied Windows rounded corners and suppressed the duplicate DWM border.",
            properties: new Dictionary<string, object?> { ["windowHandle"] = $"0x{unchecked((ulong)window.ToInt64()):X}" });
    }
}
