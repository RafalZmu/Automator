using System.Runtime.InteropServices;

namespace Automator.Services;

internal static class NativeMethods
{
    internal const int WH_KEYBOARD_LL = 13;
    internal const int WM_KEYDOWN = 0x0100;
    internal const int WM_KEYUP = 0x0101;
    internal const int WM_SYSKEYDOWN = 0x0104;
    internal const int WM_SYSKEYUP = 0x0105;
    internal const int WM_COMMAND = 0x0111;
    internal const int WM_DESTROY = 0x0002;
    internal const int WM_NULL = 0x0000;
    internal const int WM_USER = 0x0400;
    internal const int WM_LBUTTONUP = 0x0202;
    internal const int WM_RBUTTONUP = 0x0205;
    internal const int GA_ROOT = 2;
    internal const int SW_HIDE = 0;
    internal const int SW_SHOW = 5;
    internal const int SW_MINIMIZE = 6;
    internal const int SW_RESTORE = 9;
    internal const int SW_MAXIMIZE = 3;
    internal const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
    internal const uint WINEVENT_OUTOFCONTEXT = 0x0000;
    internal const uint NIF_MESSAGE = 0x0001;
    internal const uint NIF_ICON = 0x0002;
    internal const uint NIF_TIP = 0x0004;
    internal const uint NIM_ADD = 0x0000;
    internal const uint NIM_DELETE = 0x0002;
    internal const uint TPM_RIGHTBUTTON = 0x0002;
    internal const uint MF_STRING = 0x0000;
    internal const int IDI_APPLICATION = 32512;
    internal const int GWL_STYLE = -16;
    internal const int GWL_EXSTYLE = -20;
    internal const uint DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    internal const uint DWMWA_BORDER_COLOR = 34;
    internal const uint DWMWCP_ROUND = 2;
    internal const uint DWMWA_COLOR_NONE = 0xFFFFFFFE;
    internal const long WS_CAPTION = 0x00C00000L;
    internal const long WS_THICKFRAME = 0x00040000L;
    internal const long WS_POPUP = unchecked((long)0x80000000);
    internal const long WS_EX_TOOLWINDOW = 0x00000080L;
    internal const long WS_EX_TOPMOST = 0x00000008L;
    internal const uint SWP_FRAMECHANGED = 0x0020;
    internal const uint SWP_NOACTIVATE = 0x0010;
    internal const uint SWP_SHOWWINDOW = 0x0040;
    internal static readonly IntPtr HWND_TOPMOST = new(-1);

    [StructLayout(LayoutKind.Sequential)]
    internal struct Point
    {
        public int X;
        public int Y;
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

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct NotifyIconData
    {
        public uint Size;
        public IntPtr Window;
        public uint Id;
        public uint Flags;
        public uint CallbackMessage;
        public IntPtr Icon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Tip;
        public uint State;
        public uint StateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Info;
        public uint TimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string InfoTitle;
        public uint InfoFlags;
        public Guid ItemGuid;
        public IntPtr BalloonIcon;
    }

    internal delegate IntPtr LowLevelKeyboardProc(int code, IntPtr message, IntPtr data);
    internal delegate IntPtr SubclassProc(IntPtr window, uint message, nuint wParam, nint lParam, nuint subclassId, nuint referenceData);
    internal delegate void WinEventProc(IntPtr hook, uint eventType, IntPtr window, int objectId, int childId, uint eventThread, uint eventTime);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr SetWindowsHookEx(int hookType, LowLevelKeyboardProc callback, IntPtr module, uint threadId);
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")]
    internal static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern IntPtr GetModuleHandle(string? moduleName);
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern int GetMessage(out Message message, IntPtr window, uint minimum, uint maximum);
    [DllImport("user32.dll")]
    internal static extern bool TranslateMessage(ref Message message);
    [DllImport("user32.dll")]
    internal static extern bool PeekMessage(out Message message, IntPtr window, uint minimum, uint maximum, uint remove);
    [DllImport("user32.dll")]
    internal static extern IntPtr DispatchMessage(ref Message message);
    [DllImport("user32.dll")]
    internal static extern bool PostThreadMessage(uint threadId, uint message, nuint wParam, nint lParam);
    [DllImport("kernel32.dll")]
    internal static extern uint GetCurrentThreadId();

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

    [DllImport("user32.dll")]
    internal static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")]
    internal static extern IntPtr WindowFromPoint(Point point);
    [DllImport("user32.dll")]
    internal static extern IntPtr GetAncestor(IntPtr window, uint flags);
    [DllImport("user32.dll")]
    internal static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    internal static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll")]
    internal static extern bool IsIconic(IntPtr window);
    [DllImport("user32.dll")]
    internal static extern bool IsWindow(IntPtr window);
    [DllImport("user32.dll")]
    internal static extern bool ShowWindowAsync(IntPtr window, int command);
    [DllImport("user32.dll")]
    internal static extern bool ShowWindow(IntPtr window, int command);
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool BringWindowToTop(IntPtr window);
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool AttachThreadInput(uint attachingThreadId, uint attachedThreadId, bool attach);
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr SetFocus(IntPtr window);
    [DllImport("user32.dll")]
    internal static extern IntPtr GetFocus();
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr SetActiveWindow(IntPtr window);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern IntPtr FindWindow(string? className, string windowName);
    [DllImport("user32.dll")]
    internal static extern bool AllowSetForegroundWindow(uint processId);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    internal static extern IntPtr GetWindowLongPtr(IntPtr window, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    internal static extern IntPtr SetWindowLongPtr(IntPtr window, int index, IntPtr value);
    [DllImport("user32.dll")]
    internal static extern bool EnumWindows(EnumWindowsProc callback, IntPtr data);
    internal delegate bool EnumWindowsProc(IntPtr window, IntPtr data);
    [DllImport("user32.dll")]
    internal static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    internal static extern int GetWindowText(IntPtr window, System.Text.StringBuilder text, int maximumCount);
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool SetWindowRgn(IntPtr window, IntPtr region, bool redraw);
    [DllImport("user32.dll")]
    internal static extern bool GetWindowRect(IntPtr window, out Rect rectangle);
    [DllImport("user32.dll")]
    internal static extern uint GetDpiForWindow(IntPtr window);
    [DllImport("gdi32.dll", SetLastError = true)]
    internal static extern IntPtr CreateRoundRectRgn(int left, int top, int right, int bottom, int ellipseWidth, int ellipseHeight);
    [DllImport("gdi32.dll", SetLastError = true)]
    internal static extern bool DeleteObject(IntPtr handle);
    [DllImport("dwmapi.dll", PreserveSig = true)]
    internal static extern int DwmSetWindowAttribute(IntPtr window, uint attribute, ref uint value, uint valueSize);
    [DllImport("user32.dll")]
    internal static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [DllImport("user32.dll")]
    internal static extern IntPtr MonitorFromPoint(Point point, uint flags);

    [StructLayout(LayoutKind.Sequential)]
    internal struct Rect { public int Left; public int Top; public int Right; public int Bottom; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct MonitorInfo
    {
        public int Size;
        public Rect Monitor;
        public Rect Work;
        public uint Flags;
    }

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr module, WinEventProc callback, uint processId, uint threadId, uint flags);
    [DllImport("user32.dll")]
    internal static extern bool UnhookWinEvent(IntPtr hook);
    [DllImport("comctl32.dll", SetLastError = true)]
    internal static extern bool SetWindowSubclass(IntPtr window, SubclassProc callback, nuint subclassId, nuint referenceData);
    [DllImport("comctl32.dll", SetLastError = true)]
    internal static extern bool RemoveWindowSubclass(IntPtr window, SubclassProc callback, nuint subclassId);
    [DllImport("comctl32.dll")]
    internal static extern IntPtr DefSubclassProc(IntPtr window, uint message, nuint wParam, nint lParam);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern bool Shell_NotifyIcon(uint message, ref NotifyIconData data);
    [DllImport("user32.dll")]
    internal static extern IntPtr LoadIcon(IntPtr instance, IntPtr iconName);
    [DllImport("user32.dll")]
    internal static extern IntPtr CreatePopupMenu();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    internal static extern bool AppendMenu(IntPtr menu, uint flags, nuint id, string text);
    [DllImport("user32.dll")]
    internal static extern uint TrackPopupMenu(IntPtr menu, uint flags, int x, int y, int reserved, IntPtr window, IntPtr rectangle);
    [DllImport("user32.dll")]
    internal static extern bool DestroyMenu(IntPtr menu);
    [DllImport("user32.dll")]
    internal static extern bool PostMessage(IntPtr window, uint message, nuint wParam, nint lParam);
}
