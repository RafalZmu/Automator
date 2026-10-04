param(
    [Parameter(Mandatory = $true)][string]$StatePath,
    [Parameter(Mandatory = $true)][string]$CommandDirectory,
    [Parameter(Mandatory = $true)][string]$ResultDirectory
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @'
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

public static class AutomatorKeyboardTargetCommands
{
    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Explicit, Size = 40)]
    private struct Input
    {
        [FieldOffset(0)] public uint Type;
        [FieldOffset(8)] public KeyboardInput Keyboard;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, Input[] inputs, int inputSize);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    public static string Execute(string line)
    {
        var parts = line.Split(':');
        if (parts[0] == "KEY") Tap(ushort.Parse(parts[1]));
        else if (parts[0] == "CURSOR" && !SetCursorPos(int.Parse(parts[1]), int.Parse(parts[2])))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "SetCursorPos failed.");
        var foreground = GetForegroundWindow();
        return "0x" + foreground.ToInt64().ToString("X");
    }

    private static void Tap(ushort virtualKey)
    {
        var inputs = new[]
        {
            new Input { Type = 1, Keyboard = new KeyboardInput { VirtualKey = virtualKey } },
            new Input { Type = 1, Keyboard = new KeyboardInput { VirtualKey = virtualKey, Flags = 2 } }
        };
        if (SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Input))) != inputs.Length)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "SendInput did not send both keyboard events.");
    }
}
'@

$script:fixtureForm = New-Object System.Windows.Forms.Form
$script:fixtureForm.Text = 'Automator keyboard integration target'
$script:fixtureForm.Width = 420
$script:fixtureForm.Height = 180
$script:fixtureForm.StartPosition = 'CenterScreen'
$script:fixtureForm.ShowInTaskbar = $true
$script:fixtureForm.TopMost = $true
$script:fixtureTextBox = New-Object System.Windows.Forms.TextBox
$script:fixtureTextBox.Location = New-Object System.Drawing.Point(16, 24)
$script:fixtureTextBox.Width = 360
$script:fixtureTextBox.Name = 'KeyboardTargetTextBox'
$script:fixtureTextBox.Add_TextChanged({
    [System.IO.File]::WriteAllText($StatePath, $script:fixtureTextBox.Text)
})
[void]$script:fixtureForm.Controls.Add($script:fixtureTextBox)
$script:fixtureForm.Add_Shown({
    $script:fixtureForm.Activate()
    $script:fixtureTextBox.Focus()
    [Console]::Out.WriteLine("FIXTURE_READY:{0}" -f $script:fixtureForm.Handle.ToInt64())
    [Console]::Out.Flush()
})
$script:commandTimer = New-Object System.Windows.Forms.Timer
$script:commandTimer.Interval = 25
$script:commandTimer.Add_Tick({
    $commands = [System.IO.Directory]::GetFiles($CommandDirectory, 'command-*.txt')
    [Array]::Sort($commands)
    foreach ($commandFile in $commands) {
        $commandName = [System.IO.Path]::GetFileNameWithoutExtension($commandFile)
        $commandId = $commandName.Substring('command-'.Length)
        try {
            $line = [System.IO.File]::ReadAllText($commandFile)
            [System.IO.File]::Delete($commandFile)
            if ($line -eq 'NORMAL') {
                $script:fixtureForm.TopMost = $false
                [void]$script:fixtureForm.Activate()
                [void]$script:fixtureTextBox.Focus()
                $result = [AutomatorKeyboardTargetCommands]::Execute('PROBE')
            }
            else {
                $result = [AutomatorKeyboardTargetCommands]::Execute($line)
            }
            $resultPath = [System.IO.Path]::Combine($ResultDirectory, "result-$commandId.txt")
            $temporaryPath = "$resultPath.tmp"
            [System.IO.File]::WriteAllText($temporaryPath, $result)
            [System.IO.File]::Move($temporaryPath, $resultPath)
        }
        catch {
            $resultPath = [System.IO.Path]::Combine($ResultDirectory, "result-$commandId.txt")
            $temporaryPath = "$resultPath.tmp"
            [System.IO.File]::WriteAllText($temporaryPath, "ERROR:$($_.Exception.Message)")
            [System.IO.File]::Move($temporaryPath, $resultPath)
        }
    }
})
$script:fixtureForm.Add_Shown({ $script:commandTimer.Start() })
[void]$script:fixtureForm.Add_FormClosed({ $script:commandTimer.Stop() })
[System.Windows.Forms.Application]::Run($script:fixtureForm)
