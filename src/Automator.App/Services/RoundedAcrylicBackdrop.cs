using Automator.Core.Configuration;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Automator.Services;

/// <summary>
/// Hosts Desktop Acrylic on a bounded SystemBackdropElement so the material is clipped
/// to the launcher card instead of filling the rectangular top-level window.
/// </summary>
internal sealed class RoundedAcrylicBackdrop : SystemBackdrop, IDisposable
{
    private DesktopAcrylicController? _controller;
    private SystemBackdropConfiguration? _configuration;
    private AppTheme _theme;
    private bool _isInputActive = true;
    private bool _disposed;

    public event EventHandler<bool>? AttachmentChanged;

    public string State => _controller?.State.ToString() ?? "Pending";

    public RoundedAcrylicBackdrop(AppTheme theme) => _theme = theme;

    public void UpdateTheme(AppTheme theme)
    {
        _theme = theme;
        ApplyTheme();
    }

    public void SetInputActive(bool isActive)
    {
        _isInputActive = isActive;
        if (_configuration is not null) _configuration.IsInputActive = isActive;
    }

    protected override void OnTargetConnected(
        ICompositionSupportsSystemBackdrop connectedTarget,
        XamlRoot xamlRoot)
    {
        base.OnTargetConnected(connectedTarget, xamlRoot);
        try
        {
            _configuration = GetDefaultSystemBackdropConfiguration(connectedTarget, xamlRoot);
            _configuration.IsInputActive = _isInputActive;
            _configuration.Theme = _theme == AppTheme.Dark
                ? SystemBackdropTheme.Dark
                : SystemBackdropTheme.Light;

            _controller = new DesktopAcrylicController { Kind = DesktopAcrylicKind.Thin };
            ApplyTheme();
            _controller.StateChanged += OnControllerStateChanged;
            _controller.AddSystemBackdropTarget(connectedTarget);
            _controller.SetSystemBackdropConfiguration(_configuration);

            AppLogger.Info("Window.BackdropTargetConnected", "Desktop Acrylic attached to the rounded panel card.",
                new Dictionary<string, object?>
                {
                    ["target"] = "SystemBackdropElement",
                    ["state"] = _controller.State.ToString(),
                    ["theme"] = _theme.ToString(),
                    ["kind"] = _controller.Kind.ToString(),
                    ["tintOpacity"] = _controller.TintOpacity,
                    ["luminosityOpacity"] = _controller.LuminosityOpacity
                });
            AttachmentChanged?.Invoke(this, _controller.State == SystemBackdropState.Active);
        }
        catch (Exception exception)
        {
            AppLogger.Error("Window.BackdropTargetFailed", "Could not attach Acrylic to the rounded panel card.", exception);
            ReleaseController(connectedTarget);
            AttachmentChanged?.Invoke(this, false);
        }
    }

    protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop disconnectedTarget)
    {
        ReleaseController(disconnectedTarget);
        base.OnTargetDisconnected(disconnectedTarget);
    }

    private void ApplyTheme()
    {
        if (_configuration is not null)
        {
            _configuration.Theme = _theme == AppTheme.Dark
                ? SystemBackdropTheme.Dark
                : SystemBackdropTheme.Light;
        }

        if (_controller is null) return;
        var isDark = _theme == AppTheme.Dark;
        _controller.TintColor = isDark
            ? Color.FromArgb(0xFF, 0x22, 0x30, 0x38)
            : Color.FromArgb(0xFF, 0xF4, 0xFA, 0xFC);
        _controller.TintOpacity = isDark ? 0.30f : 0.38f;
        _controller.LuminosityOpacity = isDark ? 0.34f : 0.42f;
        _controller.FallbackColor = GetFallbackColor(_theme);
    }

    private void OnControllerStateChanged(object sender, object args)
    {
        var controller = _controller;
        if (controller is null) return;
        var active = controller.State == SystemBackdropState.Active;
        AppLogger.Info("Window.BackdropState", "The rounded-card Desktop Acrylic state changed.",
            new Dictionary<string, object?>
            {
                ["state"] = controller.State.ToString(),
                ["theme"] = _theme.ToString(),
                ["target"] = "SystemBackdropElement"
            });
        AttachmentChanged?.Invoke(this, active);
    }

    private void ReleaseController(ICompositionSupportsSystemBackdrop? target)
    {
        var controller = _controller;
        _controller = null;
        if (controller is null) return;
        controller.StateChanged -= OnControllerStateChanged;
        if (target is not null)
        {
            try { controller.RemoveSystemBackdropTarget(target); }
            catch (Exception exception) { AppLogger.Warning("Window.BackdropTargetReleaseFailed", "Could not detach the Acrylic card target cleanly.", exception); }
        }
        controller.Dispose();
        _configuration = null;
    }

    public static Color GetFallbackColor(AppTheme theme) => theme == AppTheme.Dark
        ? Color.FromArgb(0xFF, 0x24, 0x31, 0x39)
        : Color.FromArgb(0xFF, 0xF1, 0xF7, 0xF9);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ReleaseController(null);
    }
}
