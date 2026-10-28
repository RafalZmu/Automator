namespace Automator.Application.Launcher;

public enum NavigationKeyAction
{
    None,
    Close,
    SelectTab,
    PreviousTabRow,
    NextTabRow,
    OpenCatalog
}

/// <summary>Determines which unmodified global keys belong to the focused launcher panel.</summary>
public static class NavigationKeyPolicy
{
    /// <param name="textEntryFocused">True while the renderer has a text-entry control focused. Digits, arrows and '/' then belong to the control.</param>
    public static NavigationKeyAction Decide(
        LauncherNavigationState state,
        bool nativeForeground,
        bool rendererFocused,
        bool hasModifier,
        uint virtualKey,
        bool textEntryFocused = false)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (!state.Visible || !nativeForeground || !rendererFocused || hasModifier) return NavigationKeyAction.None;
        if (state.Mode is not (LauncherMode.Launcher or LauncherMode.Catalog)) return NavigationKeyAction.None;
        if (virtualKey == 0x1B) return NavigationKeyAction.Close;
        if (textEntryFocused) return NavigationKeyAction.None;
        if (virtualKey is >= 0x31 and <= 0x39 or >= 0x61 and <= 0x69) return NavigationKeyAction.SelectTab;
        // Up and Down belong to the page unless there is more than one row of tabs to move between.
        if (state.TabRowCount > 1 && virtualKey == 0x26) return NavigationKeyAction.PreviousTabRow;
        if (state.TabRowCount > 1 && virtualKey == 0x28) return NavigationKeyAction.NextTabRow;
        return state.Mode == LauncherMode.Launcher && state.SelectedTab == 1 && virtualKey == 0xBF
            ? NavigationKeyAction.OpenCatalog
            : NavigationKeyAction.None;
    }
}
