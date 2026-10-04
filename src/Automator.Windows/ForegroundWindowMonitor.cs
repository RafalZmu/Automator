using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Automator.Application.Logging;
using Automator.Windows.Interop;

namespace Automator.Windows;

/// <summary>Owns a message-pump thread for the out-of-context WinEvent foreground hook.</summary>
public sealed class ForegroundWindowMonitor : IDisposable
{
    private readonly IApplicationLog _log;
    private readonly ConcurrentDictionary<IntPtr, long> _lastForeground = new();
    private readonly ManualResetEventSlim _ready = new(false);
    private readonly WindowsNativeMethods.WinEventProc _callback;
    private Thread? _thread;
    private IntPtr _hook;
    private uint _threadId;
    private bool _installed;
    private bool _disposed;

    public ForegroundWindowMonitor(IApplicationLog? log = null)
    {
        _log = log ?? NullApplicationLog.Instance;
        _callback = OnForegroundChanged;
    }

    public bool Start(TimeSpan timeout)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_thread is not null) return _ready.Wait(timeout) && _installed;

        _thread = new Thread(HookThread) { IsBackground = true, Name = "Automator foreground history" };
        _thread.Start();
        return _ready.Wait(timeout) && _installed;
    }

    public long GetLastForegroundTimestamp(IntPtr window) => _lastForeground.TryGetValue(window, out var timestamp) ? timestamp : 0;

    public IReadOnlyDictionary<IntPtr, long> SnapshotRecency() => new Dictionary<IntPtr, long>(_lastForeground);

    private void HookThread()
    {
        _threadId = WindowsNativeMethods.GetCurrentThreadId();
        WindowsNativeMethods.PeekMessage(out _, IntPtr.Zero, 0, 0, 0);
        _hook = WindowsNativeMethods.SetWinEventHook(
            WindowsNativeMethods.EventSystemForeground,
            WindowsNativeMethods.EventSystemForeground,
            IntPtr.Zero,
            _callback,
            0,
            0,
            WindowsNativeMethods.WineventOutOfContext);
        _installed = _hook != IntPtr.Zero;
        if (!_installed)
        {
            _log.Write(ApplicationLogLevel.Error, "Windows.ForegroundHookFailed", "SetWinEventHook failed.",
                properties: new Dictionary<string, object?> { ["win32Error"] = Marshal.GetLastWin32Error() });
        }
        else
        {
            _log.Write(ApplicationLogLevel.Information, "Windows.ForegroundHookInstalled", "Foreground recency monitoring is running.",
                properties: new Dictionary<string, object?> { ["threadId"] = _threadId });
        }

        _ready.Set();
        if (!_installed) return;

        try
        {
            while (true)
            {
                var result = WindowsNativeMethods.GetMessage(out var message, IntPtr.Zero, 0, 0);
                if (result <= 0) break;
                WindowsNativeMethods.TranslateMessage(ref message);
                WindowsNativeMethods.DispatchMessage(ref message);
            }
        }
        finally
        {
            if (_hook != IntPtr.Zero)
            {
                WindowsNativeMethods.UnhookWinEvent(_hook);
                _hook = IntPtr.Zero;
            }
        }
    }

    private void OnForegroundChanged(IntPtr hook, uint eventType, IntPtr window, int objectId, int childId, uint eventThread, uint eventTime)
    {
        if (window != IntPtr.Zero) _lastForeground[window] = Stopwatch.GetTimestamp();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_threadId != 0)
            WindowsNativeMethods.PostThreadMessage(_threadId, WindowsNativeMethods.WmQuit, 0, 0);
        _thread?.Join(TimeSpan.FromSeconds(2));
        _ready.Dispose();
    }
}
