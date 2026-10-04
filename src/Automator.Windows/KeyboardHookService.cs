using System.Runtime.InteropServices;
using System.Threading;
using Automator.Application.Launcher;
using Automator.Application.Logging;
using Automator.Core.Automation;
using Automator.Core.Launcher;
using Automator.Windows.Interop;

namespace Automator.Windows;

public sealed record LauncherWindowContext(IntPtr Handle, bool Visible, bool RendererFocused, bool NativeDialogActive, string Mode);
public sealed record NavigationKeyRequested(NavigationKeyAction Action, uint VirtualKey);

/// <summary>Owns a low-level keyboard hook and a dedicated Win32 message pump.</summary>
public sealed class KeyboardHookService : IDisposable
{
    private const uint WmQuit = 0x0012;
    private readonly object _sync = new();
    private readonly ManualResetEventSlim _ready = new(false);
    private readonly Func<LauncherNavigationState> _getLauncherState;
    private readonly Func<LauncherWindowContext> _getWindowContext;
    private readonly IApplicationLog _log;
    private readonly WindowsNativeMethods.LowLevelKeyboardProc _callback;
    private readonly HashSet<uint> _downKeys = [];
    private readonly HashSet<uint> _consumedKeyDowns = [];
    private readonly KeyboardInputNormalizer _inputNormalizer = new();
    private Thread? _thread;
    private uint _threadId;
    private IntPtr _hook;
    private uint _openerVirtualKey;
    private string _openerKey;
    private bool _openerCandidate;
    private bool _openerChord;
    private bool _recording;
    private bool _installed;
    private int _installErrorCode;
    private bool _disposed;
    private long _inputSequence;

    public KeyboardHookService(
        string openerKey,
        Func<LauncherNavigationState> getLauncherState,
        Func<LauncherWindowContext> getWindowContext,
        IApplicationLog? log = null)
    {
        if (!Automator.Core.Launcher.HotkeyKeyNameValidator.IsSupported(openerKey))
            throw new ArgumentException("Unsupported opener key.", nameof(openerKey));
        _openerKey = openerKey;
        _openerVirtualKey = KeyNames.ToVirtualKey(openerKey);
        _getLauncherState = getLauncherState ?? throw new ArgumentNullException(nameof(getLauncherState));
        _getWindowContext = getWindowContext ?? throw new ArgumentNullException(nameof(getWindowContext));
        _log = log ?? NullApplicationLog.Instance;
        _callback = HookCallback;
    }

    public event EventHandler? ToggleRequested;
    public event EventHandler<NavigationKeyRequested>? NavigationRequested;
    public event EventHandler<string>? KeyRecorded;
    /// <summary>Receives normalized key transitions. The listener should enqueue work and return promptly.</summary>
    public event Action<KeyInputEvent>? InputReceived;

    public bool IsInstalled => Volatile.Read(ref _installed);
    public int InstallErrorCode => Volatile.Read(ref _installErrorCode);
    public string OpenerKey { get { lock (_sync) return _openerKey; } }
    public bool IsRecording { get { lock (_sync) return _recording; } }

    public bool Start(TimeSpan timeout)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_thread is not null) return _ready.Wait(timeout) && IsInstalled;
        _thread = new Thread(HookThread) { IsBackground = true, Name = "Automator keyboard hook" };
        _thread.Start();
        return _ready.Wait(timeout) && IsInstalled;
    }

    public void SetOpener(string key)
    {
        if (!Automator.Core.Launcher.HotkeyKeyNameValidator.IsSupported(key))
            throw new ArgumentException("Unsupported opener key.", nameof(key));
        var virtualKey = KeyNames.ToVirtualKey(key);
        lock (_sync)
        {
            _openerKey = key;
            _openerVirtualKey = virtualKey;
            _openerCandidate = false;
            _openerChord = false;
        }
    }

    public void StartRecording()
    {
        lock (_sync)
        {
            _openerCandidate = false;
            _openerChord = false;
            _recording = true;
        }
    }

    public void CancelRecording()
    {
        lock (_sync) _recording = false;
    }

    private void HookThread()
    {
        _threadId = WindowsNativeMethods.GetCurrentThreadId();
        WindowsNativeMethods.PeekMessage(out _, IntPtr.Zero, 0, 0, 0);
        try
        {
            _hook = WindowsNativeMethods.SetWindowsHookEx(
                WindowsNativeMethods.WhKeyboardLl, _callback, WindowsNativeMethods.GetModuleHandle(null), 0);
            _installed = _hook != IntPtr.Zero;
            if (!_installed)
            {
                _installErrorCode = Marshal.GetLastWin32Error();
                _log.Write(ApplicationLogLevel.Error, "Keyboard.HookInstallFailed", "The global low-level keyboard hook did not install.",
                    properties: new Dictionary<string, object?> { ["win32Error"] = _installErrorCode });
                return;
            }

            _log.Write(ApplicationLogLevel.Information, "Keyboard.HookInstalled", "The global keyboard hook is ready.",
                properties: new Dictionary<string, object?> { ["threadId"] = _threadId, ["openerKey"] = OpenerKey });
            _ready.Set();
            while (true)
            {
                var result = WindowsNativeMethods.GetMessage(out var message, IntPtr.Zero, 0, 0);
                if (result <= 0) break;
                WindowsNativeMethods.TranslateMessage(ref message);
                WindowsNativeMethods.DispatchMessage(ref message);
            }
        }
        catch (Exception exception)
        {
            _installErrorCode = Marshal.GetLastWin32Error();
            _log.Write(ApplicationLogLevel.Error, "Keyboard.HookThreadFailed", "The keyboard hook message loop failed.", exception,
                new Dictionary<string, object?> { ["win32Error"] = _installErrorCode });
        }
        finally
        {
            _installed = false;
            if (_hook != IntPtr.Zero)
            {
                if (!WindowsNativeMethods.UnhookWindowsHookEx(_hook))
                    _log.Write(ApplicationLogLevel.Warning, "Keyboard.UnhookFailed", "Windows could not remove the keyboard hook.",
                        properties: new Dictionary<string, object?> { ["win32Error"] = Marshal.GetLastWin32Error() });
                _hook = IntPtr.Zero;
            }
            _ready.Set();
        }
    }

    private IntPtr HookCallback(int code, IntPtr message, IntPtr data)
    {
        try
        {
            if (code >= 0 && ProcessKey(message.ToInt32(), Marshal.PtrToStructure<WindowsNativeMethods.KeyboardHookData>(data)))
                return new IntPtr(1);
        }
        catch (Exception exception)
        {
            ThreadPool.QueueUserWorkItem(_ => _log.Write(ApplicationLogLevel.Error, "Keyboard.CallbackFailed",
                "An exception occurred in the keyboard hook callback.", exception));
        }

        return WindowsNativeMethods.CallNextHookEx(_hook, code, message, data);
    }

    private bool ProcessKey(int message, WindowsNativeMethods.KeyboardHookData data)
    {
        if (!KeyboardInputNormalizer.TryGetTransition((uint)message, out var isDown)) return false;
        var key = NormalizeModifier(data.VirtualKeyCode, data.Flags, data.ScanCode);
        var repeat = false;
        var recording = false;
        var opener = 0u;
        var openerName = string.Empty;
        var windowContext = _getWindowContext();

        lock (_sync)
        {
            if (isDown && _consumedKeyDowns.Contains(key)) return true;
            if (isDown)
            {
                repeat = !_downKeys.Add(key);
                if (repeat && _consumedKeyDowns.Contains(key)) return true;
            }
            else
            {
                _downKeys.Remove(key);
                if (_consumedKeyDowns.Remove(key)) return true;
            }
            recording = _recording;
            opener = _openerVirtualKey;
            openerName = _openerKey;
        }

        var inputEvent = _inputNormalizer.Normalize(data.VirtualKeyCode, data.ScanCode, data.Flags,
            isDown, repeat, Interlocked.Increment(ref _inputSequence));
        try
        {
            InputReceived?.Invoke(inputEvent);
        }
        catch (Exception exception)
        {
            QueueLog(ApplicationLogLevel.Warning, "Keyboard.InputSubscriberFailed",
                "A keyboard input listener failed; launcher key handling continued.",
                new Dictionary<string, object?> { ["exceptionType"] = exception.GetType().Name });
        }

        if (recording && isDown && !repeat)
        {
            var launcherState = _getLauncherState();
            var nativeForeground = windowContext.Handle != IntPtr.Zero
                && WindowsNativeMethods.GetForegroundWindow() == windowContext.Handle;
            var modeMatches = string.Equals(windowContext.Mode, "recordingHotkey", StringComparison.Ordinal);
            if (!HotkeyRecordingPolicy.ShouldCapture(launcherState, windowContext.Visible,
                    windowContext.RendererFocused, windowContext.NativeDialogActive, nativeForeground, modeMatches))
                return false;

            var recorded = KeyNames.FromVirtualKey(key);
            if (Automator.Core.Launcher.HotkeyKeyNameValidator.IsSupported(recorded))
            {
                lock (_sync)
                {
                    _recording = false;
                    _openerCandidate = false;
                    _openerChord = false;
                }
                QueueLog(ApplicationLogLevel.Information, "Keyboard.KeyRecorded", "Recorded an opener key.",
                    new Dictionary<string, object?> { ["key"] = recorded });
                KeyRecorded?.Invoke(this, recorded);
            }
            return false;
        }

        if (key == opener)
        {
            if (isDown && !repeat)
            {
                lock (_sync)
                {
                    _openerCandidate = !recording && !HasOtherModifierPressed(opener);
                    _openerChord = false;
                }
            }
            else if (!isDown)
            {
                var toggle = false;
                lock (_sync)
                {
                    toggle = _openerCandidate && !_openerChord && !recording && !windowContext.NativeDialogActive
                        && !HasOtherModifierPressed(opener);
                    _openerCandidate = false;
                    _openerChord = false;
                }
                if (toggle)
                {
                    QueueLog(ApplicationLogLevel.Information, "Keyboard.OpenerTapped", "The configured opener was tapped alone.",
                        new Dictionary<string, object?> { ["openerKey"] = openerName });
                    ToggleRequested?.Invoke(this, EventArgs.Empty);
                }
            }
            return false;
        }

        if (isDown && !repeat)
        {
            lock (_sync)
            {
                if (_openerCandidate) _openerChord = true;
            }
        }

        if (!isDown || repeat || !IsNavigationKey(key)) return false;
        var context = windowContext;
        if (context.NativeDialogActive) return false;
        var foreground = context.Visible && context.RendererFocused && context.Handle != IntPtr.Zero
            && WindowsNativeMethods.GetForegroundWindow() == context.Handle;
        var hasModifier = IsAnyModifierPressed();
        var action = NavigationKeyPolicy.Decide(_getLauncherState(), foreground,
            context.RendererFocused, hasModifier, key);
        if (action == NavigationKeyAction.None) return false;

        lock (_sync) _consumedKeyDowns.Add(key);
        QueueLog(ApplicationLogLevel.Information, "Keyboard.NavigationConsumed", "Consumed an unmodified key for the focused launcher.",
            new Dictionary<string, object?> { ["virtualKey"] = key, ["action"] = action });
        NavigationRequested?.Invoke(this, new NavigationKeyRequested(action, key));
        return true;
    }

    private static bool IsNavigationKey(uint key) => key == 0x1B || key == 0xBF
        || key is >= 0x31 and <= 0x39 or >= 0x61 and <= 0x69;

    private static bool IsAnyModifierPressed() => ModifierKeys.Any(IsKeyDown);

    private static bool HasOtherModifierPressed(uint openerKey) => ModifierKeys
        .Where(key => key != openerKey)
        .Any(IsKeyDown);

    private static bool IsKeyDown(uint key) => (WindowsNativeMethods.GetAsyncKeyState((int)key) & 0x8000) != 0;

    private void QueueLog(ApplicationLogLevel level, string eventName, string message, IReadOnlyDictionary<string, object?>? properties = null) =>
        ThreadPool.QueueUserWorkItem(_ => _log.Write(level, eventName, message, properties: properties));

    private static readonly uint[] ModifierKeys = [0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0x5B, 0x5C];

    private static uint NormalizeModifier(uint key, uint flags, uint scanCode) => key switch
    {
        0x11 => (flags & WindowsNativeMethods.LlkhfExtended) != 0 ? 0xA3u : 0xA2u,
        0x12 => (flags & WindowsNativeMethods.LlkhfExtended) != 0 ? 0xA5u : 0xA4u,
        0x10 => scanCode == 0x36 ? 0xA1u : 0xA0u,
        _ => key
    };

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_threadId != 0) WindowsNativeMethods.PostThreadMessage(_threadId, WmQuit, 0, 0);
        _thread?.Join(TimeSpan.FromSeconds(2));
        _inputNormalizer.ClearHeldKeys();
        _ready.Dispose();
    }
}

internal static class KeyNames
{
    public static uint ToVirtualKey(string name)
        => HotkeyVirtualKeyMapper.ToVirtualKey(name);

    public static string FromVirtualKey(uint key) => key switch
    {
        0xA3 => "RightControl", 0xA2 => "LeftControl", 0xA5 => "RightAlt", 0xA4 => "LeftAlt",
        0xA1 => "RightShift", 0xA0 => "LeftShift", 0x5B => "LeftWindows", 0x5C => "RightWindows",
        0x20 => "Space", 0x14 => "CapsLock", 0x1B => "Escape", 0xBF => "/",
        >= 0x70 and <= 0x7B => $"F{key - 0x6F}",
        >= 0x30 and <= 0x39 => ((char)key).ToString(),
        >= 0x60 and <= 0x69 => ((char)('0' + key - 0x60)).ToString(),
        >= 0x41 and <= 0x5A => ((char)key).ToString(),
        _ => $"Key{key}"
    };
}
