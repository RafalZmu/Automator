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
        using var derived = JsonDocument.Parse("""{"database":"C:\\Data Files\\live.fdb","gbak":"C:\\Program Files\\Firebird\\gbak.exe","username":"SYSDBA","password":"masterkey"}""");
        var derivedArguments = ScriptRunnerTemplateCatalog.MapArguments(template, derived.RootElement);
        Ensure(derivedArguments[1] == "C:\\Data Files\\live.fbk" && derivedArguments[2] == "C:\\Data Files\\live.zip",
            "empty backup and ZIP paths default to the database path with .fbk and .zip");
        using var relativePath = JsonDocument.Parse("""{"database":"data/live.fdb","backup":"D:\\Backup Files\\live.fbk","archive":"D:\\Backup Files\\live.zip","gbak":"C:\\Program Files\\Firebird\\gbak.exe","username":"SYSDBA","password":"masterkey"}""");
        Throws(() => ScriptRunnerTemplateCatalog.MapArguments(template, relativePath.RootElement), "relative file paths rejected");
        var directoryTemplate = template with { Parameters = [template.Parameters[0] with { Type = ScriptRunnerTemplateParameterType.Directory }, .. template.Parameters.Skip(1)] };
        Throws(() => ScriptRunnerTemplateCatalog.MapArguments(directoryTemplate, relativePath.RootElement), "relative directory paths rejected");
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
        var stale = ScriptRunnerTemplateCatalog.Get("fdb-stale-archive")
            ?? throw new Exception("Stale .fdb archive template is registered");
        Ensure(stale.Version == 1 && stale.Interpreter == ScriptRunnerInterpreter.Powershell && stale.TimeoutSeconds == 3600, "stale archive metadata");
        Ensure(stale.Parameters.Select(parameter => parameter.Type).SequenceEqual(new[]
        {
            ScriptRunnerTemplateParameterType.Directory, ScriptRunnerTemplateParameterType.Text,
            ScriptRunnerTemplateParameterType.Boolean, ScriptRunnerTemplateParameterType.Boolean,
        }), "stale archive parameter types");
        using var staleDefaults = JsonDocument.Parse("""{"source":"D:\\Firebird\\Data"}""");
        Ensure(ScriptRunnerTemplateCatalog.MapArguments(stale, staleDefaults.RootElement).SequenceEqual(new[] { "D:\\Firebird\\Data", "90", "true", "true" }),
            "stale archive defaults include subfolders and keep preview on");
        using var staleExplicit = JsonDocument.Parse("""{"source":"D:\\Firebird\\Data","days":"180","subfolders":false,"preview":false}""");
        Ensure(ScriptRunnerTemplateCatalog.MapArguments(stale, staleExplicit.RootElement).SequenceEqual(new[] { "D:\\Firebird\\Data", "180", "false", "false" }),
            "stale archive explicit values map to positional arguments");
        using var staleRelative = JsonDocument.Parse("""{"source":"Firebird\\Data"}""");
        Throws(() => ScriptRunnerTemplateCatalog.MapArguments(stale, staleRelative.RootElement), "stale archive relative source rejected");
        using var staleTextPreview = JsonDocument.Parse("""{"source":"D:\\Firebird\\Data","preview":"no"}""");
        Throws(() => ScriptRunnerTemplateCatalog.MapArguments(stale, staleTextPreview.RootElement), "stale archive preview must be boolean");
        using var staleAsset = typeof(ScriptRunnerTemplateInstaller).Assembly.GetManifestResourceStream("Automator.Templates.fdb-stale-archive.v1.ps1");
        Ensure(staleAsset is not null, "stale archive asset is embedded");
        var update = ScriptRunnerTemplateCatalog.Get("update-and-restart")
            ?? throw new Exception("Update and restart template is registered");
        Ensure(update.Version == 1 && update.Interpreter == ScriptRunnerInterpreter.Powershell && update.TimeoutSeconds == 600, "update and restart metadata");
        Ensure(update.Parameters.Select(parameter => parameter.Type).SequenceEqual(new[]
        {
            ScriptRunnerTemplateParameterType.Directory, ScriptRunnerTemplateParameterType.Boolean,
        }) && update.Parameters.All(parameter => !parameter.Sensitive), "update and restart parameter types");
        using var updateDefaults = JsonDocument.Parse("""{"repository":"C:\\Automator"}""");
        Ensure(ScriptRunnerTemplateCatalog.MapArguments(update, updateDefaults.RootElement).SequenceEqual(new[] { "C:\\Automator", "false" }),
            "update and restart rebuilds only when changes were pulled by default");
        using var updateForced = JsonDocument.Parse("""{"repository":"C:\\Automator","rebuild":true}""");
        Ensure(ScriptRunnerTemplateCatalog.MapArguments(update, updateForced.RootElement).SequenceEqual(new[] { "C:\\Automator", "true" }),
            "update and restart forced rebuild maps to the second argument");
        using var updateRelative = JsonDocument.Parse("""{"repository":"Automator"}""");
        Throws(() => ScriptRunnerTemplateCatalog.MapArguments(update, updateRelative.RootElement), "update and restart relative checkout rejected");
        using var updateMissing = JsonDocument.Parse("{}");
        Throws(() => ScriptRunnerTemplateCatalog.MapArguments(update, updateMissing.RootElement), "update and restart checkout required");
        using var updateAsset = typeof(ScriptRunnerTemplateInstaller).Assembly.GetManifestResourceStream("Automator.Templates.update-and-restart.v1.ps1");
        Ensure(updateAsset is not null, "update and restart asset is embedded");
        await ScriptRunnerTemplateInstallSpecs.RunAsync();
        await ScriptRunnerTemplateRunSpecs.RunAsync();
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
