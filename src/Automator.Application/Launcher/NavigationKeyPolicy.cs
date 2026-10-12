namespace Automator.Application.Launcher;

public enum NavigationKeyAction
{
    None,
    Close,
    SelectTab,
    OpenCatalog
}

/// <summary>Determines which unmodified global keys belong to the focused launcher panel.</summary>
public static class NavigationKeyPolicy
{
    public static NavigationKeyAction Decide(
        LauncherNavigationState state,
        bool nativeForeground,
        bool rendererFocused,
        bool hasModifier,
        uint virtualKey)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (!state.Visible || !nativeForeground || !rendererFocused || hasModifier) return NavigationKeyAction.None;
        if (state.Mode is not (LauncherMode.Launcher or LauncherMode.Catalog)) return NavigationKeyAction.None;
        if (virtualKey == 0x1B) return NavigationKeyAction.Close;
        if (virtualKey is >= 0x31 and <= 0x39 or >= 0x61 and <= 0x69) return NavigationKeyAction.SelectTab;
        return state.Mode == LauncherMode.Launcher && state.SelectedTab == 1 && virtualKey == 0xBF
            ? NavigationKeyAction.OpenCatalog
            : NavigationKeyAction.None;
    }
}
