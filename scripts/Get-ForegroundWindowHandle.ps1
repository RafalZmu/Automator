$ErrorActionPreference = 'Stop'

$nativeMethods = @"
using System;
using System.Runtime.InteropServices;

public static class AutomatorForegroundProbe
{
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();
}
"@

Add-Type -TypeDefinition $nativeMethods -Language CSharp
$handle = [AutomatorForegroundProbe]::GetForegroundWindow()
if ($handle -eq [IntPtr]::Zero) {
    throw 'GetForegroundWindow returned a null HWND.'
}

Write-Output ('0x{0:X}' -f $handle.ToInt64())
