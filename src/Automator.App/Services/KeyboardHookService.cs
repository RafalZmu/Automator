using System.Runtime.InteropServices;
using Automator.Core.Launcher;

namespace Automator.Services;

public sealed class KeyboardHookService : IDisposable
{
    private const uint VK_CONTROL = 0x11;
    private const uint VK_MENU = 0x12;
    private const uint LLKHF_EXTENDED = 0x01;
    private readonly object _sync = new();
    private readonly HashSet<uint> _downKeys = [];
    private readonly HashSet<uint> _pressedDuringTap = [];
    private readonly AutoResetEvent _stopSignal = new(false);
    private readonly ManualResetEventSlim _hookReady = new(false);
    private readonly NativeMethods.LowLevelKeyboardProc _callback;
    private HotkeyTapRecognizer _recognizer;
    private Thread? _thread;
    private IntPtr _hook;
    private uint _threadId;
    private uint _openerVirtualKey;
    private int _hookInstalled;
    private int _hookInstallErrorCode;
    private bool _recording;
    private int _navigationKeysEnabled;
    private bool _disposed;

    public event EventHandler? ToggleRequested;
    public event EventHandler<GlobalKeyEventArgs>? KeyPressed;
    public event EventHandler<string>? KeyRecorded;

    public KeyboardHookService(string openerKey)
    {
        OpenerKey = openerKey;
        _openerVirtualKey = KeyNames.ToVirtualKey(openerKey);
        _recognizer = new HotkeyTapRecognizer(openerKey);
        _callback = HookCallback;
    }

    public string OpenerKey { get; private set; }
    public bool IsHookInstalled => Volatile.Read(ref _hookInstalled) != 0;
    public int HookInstallErrorCode => Volatile.Read(ref _hookInstallErrorCode);
    public bool NavigationKeysEnabled
    {
        get => Volatile.Read(ref _navigationKeysEnabled) != 0;
        set => Volatile.Write(ref _navigationKeysEnabled, value ? 1 : 0);
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_thread is not null)
        {
            AppLogger.Warning("Keyboard.StartIgnored", "The keyboard hook service was asked to start twice.");
            return;
        }

        AppLogger.Info("Keyboard.ThreadStarting", "Starting the low-level keyboard hook thread.",
            new Dictionary<string, object?> { ["openerKey"] = OpenerKey });
        _thread = new Thread(HookThread) { IsBackground = true, Name = "Automator keyboard hook" };
        _thread.Start();
    }

    public bool WaitForInstallation(TimeSpan timeout) =>
        _hookReady.Wait(timeout) && IsHookInstalled;

    public void SetOpener(string key)
    {
        var virtualKey = KeyNames.ToVirtualKey(key);
        lock (_sync)
        {
            OpenerKey = key;
            _openerVirtualKey = virtualKey;
            _recognizer = new HotkeyTapRecognizer(key);
        }
    }

    public void StartRecording()
    {
        lock (_sync) _recording = true;
    }

    private void HookThread()
    {
        _threadId = NativeMethods.GetCurrentThreadId();
        AppLogger.Info("Keyboard.ThreadStarted", "The keyboard hook thread entered its message loop.",
            new Dictionary<string, object?> { ["threadId"] = _threadId });
        try
        {
            NativeMethods.PeekMessage(out _, IntPtr.Zero, 0, 0, 0);
            _hook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, _callback, IntPtr.Zero, 0);
            if (_hook == IntPtr.Zero)
            {
                Volatile.Write(ref _hookInstallErrorCode, Marshal.GetLastWin32Error());
                AppLogger.Error("Keyboard.HookInstallFailed", "SetWindowsHookEx returned a null hook.", properties:
                    new Dictionary<string, object?> { ["win32Error"] = HookInstallErrorCode });
                _hookReady.Set();
                return;
            }

            Volatile.Write(ref _hookInstalled, 1);
            AppLogger.Info("Keyboard.HookInstalled", "The low-level keyboard hook was installed.",
                new Dictionary<string, object?> { ["hookHandle"] = $"0x{_hook.ToInt64():X}" });
            _hookReady.Set();

            while (!_stopSignal.WaitOne(0))
            {
                var result = NativeMethods.GetMessage(out var message, IntPtr.Zero, 0, 0);
                if (result <= 0)
                {
                    if (result < 0)
                        AppLogger.Error("Keyboard.MessageLoopFailed", "GetMessage failed in the keyboard hook thread.", properties:
                            new Dictionary<string, object?> { ["win32Error"] = Marshal.GetLastWin32Error() });
                    break;
                }

                NativeMethods.TranslateMessage(ref message);
                NativeMethods.DispatchMessage(ref message);
            }
        }
        catch (Exception exception)
        {
            AppLogger.Error("Keyboard.ThreadFailed", "The keyboard hook thread failed.", exception);
        }
        finally
        {
            Volatile.Write(ref _hookInstalled, 0);
            if (_hook != IntPtr.Zero)
            {
                if (!NativeMethods.UnhookWindowsHookEx(_hook))
                    AppLogger.Warning("Keyboard.UnhookFailed", "Windows could not remove the low-level keyboard hook.",
                        properties: new Dictionary<string, object?> { ["win32Error"] = Marshal.GetLastWin32Error() });
                _hook = IntPtr.Zero;
            }

            if (!_hookReady.IsSet) _hookReady.Set();
            AppLogger.Info("Keyboard.ThreadStopped", "The keyboard hook thread has stopped.");
        }
    }

    private IntPtr HookCallback(int code, IntPtr message, IntPtr data)
    {
        try
        {
            if (code >= 0)
            {
                var messageId = message.ToInt32();
                if (messageId is NativeMethods.WM_KEYDOWN or NativeMethods.WM_SYSKEYDOWN or NativeMethods.WM_KEYUP or NativeMethods.WM_SYSKEYUP)
                {
                    var keyData = Marshal.PtrToStructure<NativeMethods.KeyboardHookData>(data);
                    var virtualKey = NormalizeModifierKey(keyData.VirtualKeyCode, keyData.Flags);
                    if (ProcessKey(virtualKey, messageId is NativeMethods.WM_KEYDOWN or NativeMethods.WM_SYSKEYDOWN))
                        return new IntPtr(1);
                }
            }
        }
        catch (Exception exception)
        {
            AppLogger.Error("Keyboard.CallbackFailed", "An exception occurred while processing a keyboard hook event.", exception,
                new Dictionary<string, object?> { ["hookCode"] = code, ["message"] = message.ToInt32() });
        }

        return NativeMethods.CallNextHookEx(_hook, code, message, data);
    }

    private static uint NormalizeModifierKey(uint virtualKey, uint flags) => virtualKey switch
    {
        VK_CONTROL => (flags & LLKHF_EXTENDED) != 0 ? 0xA3u : 0xA2u,
        VK_MENU => (flags & LLKHF_EXTENDED) != 0 ? 0xA5u : 0xA4u,
        _ => virtualKey
    };

    private bool ProcessKey(uint virtualKey, bool isDown)
    {
        bool isRepeat;
        uint opener;
        bool recording;
        HotkeyTapRecognizer recognizer;
        string openerName;
        lock (_sync)
        {
            opener = _openerVirtualKey;
            recording = _recording;
            recognizer = _recognizer;
            openerName = OpenerKey;
            isRepeat = isDown && _downKeys.Contains(virtualKey);
            if (isDown) _downKeys.Add(virtualKey);
            else _downKeys.Remove(virtualKey);
        }

        var keyName = KeyNames.FromVirtualKey(virtualKey);
        if (recording && !isDown && !KeyNames.IsModifier(virtualKey))
        {
            lock (_sync) _recording = false;
            AppLogger.Info("Keyboard.KeyRecorded", "Recorded a new opener key.", new Dictionary<string, object?> { ["key"] = keyName });
            KeyRecorded?.Invoke(this, keyName);
            return false;
        }

        if (virtualKey == opener)
        {
            if (isDown) recognizer.KeyDown(openerName, isRepeat);
            else if (recognizer.KeyUp(openerName))
            {
                AppLogger.Info("Keyboard.OpenerTapRecognized", "The configured opener key was released as a tap.",
                    new Dictionary<string, object?> { ["openerKey"] = openerName });
                ToggleRequested?.Invoke(this, EventArgs.Empty);
            }
            return false;
        }

        if (isDown)
        {
            recognizer.KeyDown(keyName, isRepeat);
            var navigationKey = virtualKey == 0x1B || virtualKey == 0xBF
                || virtualKey is >= 0x31 and <= 0x39
                || virtualKey is >= 0x61 and <= 0x69;
            if (NavigationKeysEnabled && navigationKey)
            {
                AppLogger.Info("Keyboard.NavigationKey", "Consumed a navigation key while the panel was open.",
                    new Dictionary<string, object?> { ["key"] = keyName });
                KeyPressed?.Invoke(this, new GlobalKeyEventArgs(keyName, virtualKey));
                return true;
            }
        }

        return false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        AppLogger.Info("Keyboard.Disposing", "Stopping the keyboard hook service.");
        if (_threadId != 0) NativeMethods.PostThreadMessage(_threadId, 0x0012, 0, 0);
        _stopSignal.Set();
        _thread?.Join(TimeSpan.FromSeconds(2));
        _stopSignal.Dispose();
        _hookReady.Dispose();
    }
}

public sealed class GlobalKeyEventArgs(string key, uint virtualKey) : EventArgs
{
    public string Key { get; } = key;
    public uint VirtualKey { get; } = virtualKey;
}

internal static class KeyNames
{
    public static uint ToVirtualKey(string name)
        => HotkeyVirtualKeyMapper.ToVirtualKey(name);

    public static string FromVirtualKey(uint key) => key switch
    {
        0xA3 => "RightControl", 0xA2 => "LeftControl", 0xA5 => "RightAlt", 0xA4 => "LeftAlt",
        0x20 => "Space", 0x14 => "CapsLock", 0x1B => "Escape", 0xBF => "/",
        >= 0x70 and <= 0x7B => $"F{key - 0x6F}",
        >= 0x30 and <= 0x39 => ((char)key).ToString(),
        >= 0x60 and <= 0x69 => ((char)('0' + key - 0x60)).ToString(),
        >= 0x41 and <= 0x5A => ((char)key).ToString(),
        _ => $"Key{key}"
    };

    public static bool IsModifier(uint key) => key is 0xA0 or 0xA1 or 0xA2 or 0xA3 or 0xA4 or 0xA5 or 0x5B or 0x5C;
}
