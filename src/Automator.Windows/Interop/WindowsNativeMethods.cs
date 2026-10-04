using System.Runtime.InteropServices;

namespace Automator.Windows.Interop;

internal static class WindowsNativeMethods
{
    internal const int WhKeyboardLl = 13;
    internal const int WmKeyDown = 0x0100;
    internal const int WmKeyUp = 0x0101;
    internal const int WmSysKeyDown = 0x0104;
    internal const int WmSysKeyUp = 0x0105;
    internal const uint WmQuit = 0x0012;
    internal const uint PmNoRemove = 0x0000;
    internal const int GaRoot = 2;
    internal const int SwMinimize = 6;
    internal const int SwRestore = 9;
    internal const int SwMaximize = 3;
    internal const uint EventSystemForeground = 0x0003;
    internal const uint WineventOutOfContext = 0x0000;
    internal const uint DwmaWindowCornerPreference = 33;
    internal const uint DwmaBorderColor = 34;
    internal const uint DwmWindowCornerRound = 2;
    internal const uint DwmColorNone = 0xFFFFFFFE;
    internal const uint LlkhfExtended = 0x01;

    [StructLayout(LayoutKind.Sequential)]
    internal struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Message
    {
        public IntPtr Window;
        public uint Id;
        public nuint WParam;
        public nint LParam;
        public uint Time;
        public Point Point;
        public uint Private;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct KeyboardHookData
    {
        public uint VirtualKeyCode;
        public uint ScanCode;
        public uint Flags;
        public uint Time;
        public nuint ExtraInfo;
    }

    internal delegate IntPtr LowLevelKeyboardProc(int code, IntPtr message, IntPtr data);
    internal delegate void WinEventProc(IntPtr hook, uint eventType, IntPtr window, int objectId, int childId, uint eventThread, uint eventTime);
    internal delegate bool EnumWindowsProc(IntPtr window, IntPtr data);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr SetWindowsHookEx(int hookType, LowLevelKeyboardProc callback, IntPtr module, uint threadId);
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")]
    internal static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern int GetMessage(out Message message, IntPtr window, uint minimum, uint maximum);
    [DllImport("user32.dll")]
    internal static extern bool PeekMessage(out Message message, IntPtr window, uint minimum, uint maximum, uint remove);
    [DllImport("user32.dll")]
    internal static extern bool TranslateMessage(ref Message message);
    [DllImport("user32.dll")]
    internal static extern IntPtr DispatchMessage(ref Message message);
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool PostThreadMessage(uint threadId, uint message, nuint wParam, nint lParam);
    [DllImport("kernel32.dll")]
    internal static extern uint GetCurrentThreadId();
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern IntPtr GetModuleHandle(string? moduleName);

    [DllImport("user32.dll")]
    internal static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    internal static extern short GetAsyncKeyState(int virtualKey);
    [DllImport("user32.dll")]
    internal static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll")]
    internal static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")]
    internal static extern bool IsIconic(IntPtr window);
    [DllImport("user32.dll")]
    internal static extern bool IsWindow(IntPtr window);
    [DllImport("user32.dll")]
    internal static extern bool ShowWindowAsync(IntPtr window, int command);
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool BringWindowToTop(IntPtr window);
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool AttachThreadInput(uint attachingThreadId, uint attachedThreadId, bool attach);
    [DllImport("user32.dll")]
    internal static extern bool EnumWindows(EnumWindowsProc callback, IntPtr data);
    [DllImport("user32.dll")]
    internal static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")]
    internal static extern IntPtr WindowFromPoint(Point point);
    [DllImport("user32.dll")]
    internal static extern IntPtr GetAncestor(IntPtr window, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    internal static extern int GetWindowText(IntPtr window, System.Text.StringBuilder text, int maximumCount);
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr module, WinEventProc callback, uint processId, uint threadId, uint flags);
    [DllImport("user32.dll")]
    internal static extern bool UnhookWinEvent(IntPtr hook);

    [DllImport("dwmapi.dll", PreserveSig = true)]
    internal static extern int DwmSetWindowAttribute(IntPtr window, uint attribute, ref uint value, uint valueSize);
}
