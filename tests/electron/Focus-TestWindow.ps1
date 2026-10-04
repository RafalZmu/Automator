param([Parameter(Mandatory = $true)][string]$WindowHandleHex)
$ErrorActionPreference = 'Stop'

Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

public static class AutomatorTestWindowFocus
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Point { public int X; public int Y; }
    [StructLayout(LayoutKind.Sequential)]
    public struct Message
    {
        public IntPtr Window;
        public uint Id;
        public UIntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public Point Point;
        public uint Private;
    }

    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] public static extern bool PeekMessage(out Message message, IntPtr window, uint min, uint max, uint remove);
    [DllImport("user32.dll", SetLastError = true)] public static extern bool AttachThreadInput(uint current, uint target, bool attach);
    [DllImport("user32.dll")] public static extern bool ShowWindowAsync(IntPtr window, int command);
    [DllImport("user32.dll", SetLastError = true)] public static extern bool BringWindowToTop(IntPtr window);
    [DllImport("user32.dll", SetLastError = true)] public static extern bool SetForegroundWindow(IntPtr window);

    public static bool Activate(IntPtr target, out bool attached, out int attachError)
    {
        var currentThread = GetCurrentThreadId();
        Message message;
        PeekMessage(out message, IntPtr.Zero, 0, 0, 0);
        var foreground = GetForegroundWindow();
        uint foregroundProcessId;
        var foregroundThread = foreground == IntPtr.Zero ? 0 : GetWindowThreadProcessId(foreground, out foregroundProcessId);
        attached = false;
        attachError = 0;
        if (foregroundThread != 0 && foregroundThread != currentThread)
        {
            attached = AttachThreadInput(currentThread, foregroundThread, true);
            if (!attached) attachError = Marshal.GetLastWin32Error();
        }
        try
        {
            ShowWindowAsync(target, 9);
            BringWindowToTop(target);
            SetForegroundWindow(target);
        }
        finally
        {
            if (attached && !AttachThreadInput(currentThread, foregroundThread, false))
                attachError = Marshal.GetLastWin32Error();
        }
        return GetForegroundWindow() == target;
    }
}
'@

$target = [IntPtr]::new([Convert]::ToInt64($WindowHandleHex.Substring(2), 16))
for ($attempt = 0; $attempt -lt 8; $attempt++) {
    $attached = $false
    $attachError = 0
    if ([AutomatorTestWindowFocus]::Activate($target, [ref]$attached, [ref]$attachError)) {
        Write-Output "FOREGROUND_CONFIRMED:$WindowHandleHex"
        exit 0
    }
    Start-Sleep -Milliseconds 75
}
$actual = [AutomatorTestWindowFocus]::GetForegroundWindow()
throw "Could not activate owned test window $WindowHandleHex; actual foreground is 0x$($actual.ToInt64().ToString('X'))."
