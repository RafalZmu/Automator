param(
    [Parameter(Mandatory = $true)][int]$X,
    [Parameter(Mandatory = $true)][int]$Y
)

$ErrorActionPreference = 'Stop'

Add-Type -TypeDefinition @'
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

public static class AutomatorTestCursor
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetCursorPos(int x, int y);

    public static void MoveTo(int x, int y)
    {
        if (!SetCursorPos(x, y))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not move the test cursor.");
    }
}
'@

[AutomatorTestCursor]::MoveTo($X, $Y)
