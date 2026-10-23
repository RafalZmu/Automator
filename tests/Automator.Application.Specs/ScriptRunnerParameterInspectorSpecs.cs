using Automator.Application.Automation;

internal static class ScriptRunnerParameterInspectorSpecs
{
    public static Task RunAsync()
    {
        PowerShellParamBlock();
        HeaderComments();
        NoDeclarations();
        return Task.CompletedTask;
    }

    private static void PowerShellParamBlock()
    {
        const string source = """
            <#
            .SYNOPSIS
            Archives things.
            .PARAMETER Source
            Folder to scan,
            including subfolders.
            .PARAMETER Mode
            How to archive.
            #>
            [CmdletBinding()]
            param(
                [Parameter(Mandatory = $true)] [string] $Source, # don't break on this apostrophe
                [ValidateSet('fast', 'safe')] [string] $Mode = 'safe',
                [int] $Days = 30,
                [switch] $DryRun
            )
            Write-Output "param(not a block)"
            """;
        var found = ScriptRunnerParameterInspector.Inspect(source, ScriptRunnerInterpreter.Powershell);
        Ensure(found.Count == 4, $"expected four parameters, found {found.Count}");
        Ensure(found[0].Name == "Source" && found[0].Required && found[0].Type == "text" && found[0].Description == "Folder to scan, including subfolders.", "mandatory text parameter with help");
        Ensure(found[1].Type == "choice" && found[1].Options!.SequenceEqual(new[] { "fast", "safe" }) && found[1].DefaultValue == "safe" && !found[1].Required, "ValidateSet becomes a choice with its default");
        Ensure(found[2].Name == "Days" && found[2].DefaultValue == "30", "numeric default is kept");
        Ensure(found[3].Type == "boolean" && !found[3].Required, "switch becomes a boolean");
    }

    private static void HeaderComments()
    {
        const string source = "#!/usr/bin/env python\n# @param input (file, required): CSV to read\n# @param verbose (flag) - print more\n# @param label\nprint('hi')\n";
        var found = ScriptRunnerParameterInspector.Inspect(source, ScriptRunnerInterpreter.Python);
        Ensure(found.Count == 3, $"expected three header parameters, found {found.Count}");
        Ensure(found[0].Type == "file" && found[0].Required && found[0].Description == "CSV to read", "file parameter header");
        Ensure(found[1].Type == "boolean" && found[1].Description == "print more", "flag header");
        Ensure(found[2].Type == "text" && found[2].Description is null, "bare header");
    }

    private static void NoDeclarations()
    {
        Ensure(ScriptRunnerParameterInspector.Inspect("Write-Host 'hi'", ScriptRunnerInterpreter.Powershell).Count == 0, "script without declarations");
        Ensure(ScriptRunnerParameterInspector.Inspect("[CmdletBinding()] param([string] $A, [string] $ApiKey = 'x')", ScriptRunnerInterpreter.Powershell) is { Count: 2 } inline && inline[1].DefaultValue is null, "same-line attribute before param and no secret defaults");
        Ensure(ScriptRunnerParameterInspector.Inspect("param(\n  $Broken", ScriptRunnerInterpreter.Powershell).Count == 0, "unbalanced param block");
    }

    private static void Ensure(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
