$ErrorActionPreference = 'Stop'

Add-Type -TypeDefinition @'
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

public static class AutomatorTestKeyInput
{
    [StructLayout(LayoutKind.Sequential)]
    public struct KeyboardInput
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Explicit, Size = 40)]
    public struct Input
    {
        [FieldOffset(0)] public uint Type;
        [FieldOffset(8)] public KeyboardInput Keyboard;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, Input[] inputs, int inputSize);

    public static void Tap(ushort virtualKey)
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

[Console]::Out.WriteLine('KEY_SENDER_READY')
[Console]::Out.Flush()

while ($line = [Console]::In.ReadLine()) {
    $parts = $line.Split(':')
    $commandId = $parts[0]
    try {
        [AutomatorTestKeyInput]::Tap([UInt16]$parts[1])
        [Console]::Out.WriteLine("KEY_DONE:{0}", $commandId)
    }
    catch {
        [Console]::Out.WriteLine("KEY_ERROR:{0}:{1}", $commandId, $_.Exception.Message)
    }
    [Console]::Out.Flush()
}
