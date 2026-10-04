using Automator.Core.Launcher;

namespace Automator.Application.Launcher;

/// <summary>Allows opener recording only while the owned recording surface is active and foreground.</summary>
public static class HotkeyRecordingPolicy
{
    public static bool ShouldCapture(
        LauncherNavigationState state,
        bool windowContextVisible,
        bool rendererFocused,
        bool nativeDialogActive,
        bool nativeForeground,
        bool contextModeMatches)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.Visible
            && state.Mode == LauncherMode.RecordingHotkey
            && windowContextVisible
            && rendererFocused
            && !nativeDialogActive
            && nativeForeground
            && contextModeMatches;
    }
}
