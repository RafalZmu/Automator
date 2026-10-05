using System.Text.Json;
using System.Text.Json.Serialization;
using Automator.Application.Automation;

internal static class ScriptRunnerTemplateSpecs
{
    public static async Task RunAsync()
    {
        var template = ScriptRunnerTemplateCatalog.Get("firebird-3-backup-zip")
            ?? throw new Exception("Firebird template is registered");
        Ensure(template.Version == 1, "Firebird template version");
        Ensure(template.Parameters.Count > 0, "Firebird parameters exist");
        ScriptRunnerTemplateCatalog.Validate([template]);
        Throws(() => ScriptRunnerTemplateCatalog.Validate([template, template]), "duplicate template IDs");
        Throws(() => ScriptRunnerTemplateCatalog.Validate([
            template with { Parameters = [template.Parameters[0], template.Parameters[1] with { Key = template.Parameters[0].Key }] }
        ]), "duplicate parameter keys");
        Throws(() => ScriptRunnerTemplateCatalog.Validate([
            template with { Parameters = [template.Parameters[0] with { Key = null! }] }
        ]), "null parameter key");
        Throws(() => ScriptRunnerTemplateCatalog.Validate([
            template with { Parameters = [template.Parameters[0], template.Parameters[1] with { ArgumentIndex = 0 }] }
        ]), "duplicate argument positions");
        using var values = JsonDocument.Parse("""{"database":"C:\\Data Files\\live.fdb","backup":"D:\\Backup Files\\live.fbk","archive":"D:\\Backup Files\\live.zip","gbak":"C:\\Program Files\\Firebird\\gbak.exe","username":"SYSDBA","password":"masterkey"}""");
        var arguments = ScriptRunnerTemplateCatalog.MapArguments(template, values.RootElement);
        Ensure(arguments.Count == 6 && arguments[0] == "C:\\Data Files\\live.fdb" && arguments[5] == "masterkey",
            "paths and transient values map to individual arguments");
        using var unknown = JsonDocument.Parse("""{"scriptPath":"C:\\other.ps1"}""");
        Throws(() => ScriptRunnerTemplateCatalog.MapArguments(template, unknown.RootElement), "unknown renderer values");
        using var badType = JsonDocument.Parse("""{"database":42}""");
        Throws(() => ScriptRunnerTemplateCatalog.MapArguments(template, badType.RootElement), "invalid renderer value type");
        using var oversized = JsonDocument.Parse("{\"database\":\"" + new string('x', 8193) + "\"}");
        Throws(() => ScriptRunnerTemplateCatalog.MapArguments(template, oversized.RootElement), "oversized submitted value");

        const string legacy = """
            {"id":"legacy","name":"Legacy","interpreter":"powershell","interpreterPath":"C:\\PowerShell\\pwsh.exe","scriptPath":"C:\\Scripts\\legacy.ps1","arguments":[],"workingDirectory":"C:\\Scripts","outputMode":"text","timeoutSeconds":30}
            """;
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        var oldProfile = JsonSerializer.Deserialize<ScriptRunnerProfile>(legacy, options)!;
        Ensure(oldProfile.TemplateOrigin is null, "legacy profile origin remains absent");
        ScriptRunnerModule.Validate(oldProfile);
        var serialized = JsonSerializer.Serialize(oldProfile, options);
        Ensure(!serialized.Contains("templateOrigin", StringComparison.Ordinal), "legacy serialization omits new metadata");
        var roundTrip = JsonSerializer.Deserialize<ScriptRunnerProfile>(serialized, options)!;
        Ensure(roundTrip.Id == oldProfile.Id && roundTrip.ScriptPath == oldProfile.ScriptPath &&
            roundTrip.Arguments.SequenceEqual(oldProfile.Arguments) && roundTrip.TemplateOrigin is null,
            "legacy profile round-trips");
        await ScriptRunnerTemplateInstallSpecs.RunAsync();
    }

    private static void Ensure(bool value, string label)
    {
        if (!value) throw new Exception(label);
    }

    private static void Throws(Action action, string label)
    {
        try { action(); }
        catch (InvalidDataException) { return; }
        throw new Exception($"Expected validation rejection: {label}");
    }
}
