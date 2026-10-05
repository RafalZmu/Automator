using System.Text.Json;

namespace Automator.Application.Automation;

/// <summary>Installs only registered, embedded assets; never accepts renderer paths or script content.</summary>
public sealed class ScriptRunnerTemplateInstaller
{
    private static readonly SemaphoreSlim InstallGate = new(1, 1);
    private readonly string _directory;
    private readonly string? _interpreter;
    private readonly Func<string, Stream?> _openAsset;

    public ScriptRunnerTemplateInstaller(string? directory = null, string? interpreter = null,
        Func<string, Stream?>? openAsset = null)
    {
        _directory = directory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Automator", "script-templates");
        _interpreter = interpreter;
        _openAsset = openAsset ?? (name => typeof(ScriptRunnerTemplateInstaller).Assembly.GetManifestResourceStream(name));
        if (!Path.IsPathFullyQualified(_directory)) throw new ArgumentException("Template data directory must be absolute.");
    }

    public async Task<(ScriptRunnerProfile Profile, bool Installed)> InstallAsync(string id, JsonElement settings,
        IAutomationLibrary library, CancellationToken cancellationToken)
    {
        var template = ScriptRunnerTemplateCatalog.Get(id) ?? throw new InvalidDataException("The selected template is not registered.");
        await InstallGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var records = await library.ListAsync(ScriptRunnerModule.ProfileCollection, cancellationToken).ConfigureAwait(false);
            foreach (var record in records)
            {
                var existing = JsonSerializer.Deserialize<ScriptRunnerProfile>(record.Data.GetRawText(), ScriptRunnerModule.JsonOptions);
                if (existing?.TemplateOrigin is { } origin && origin.Id == template.Id && origin.Version == template.Version)
                {
                    ScriptRunnerModule.Validate(existing);
                    if (!File.Exists(existing.ScriptPath)) throw new IOException("Installed template script is missing. Restore the script or remove its profile and install again.");
                    return (existing, false);
                }
            }
            var interpreter = ResolveInterpreter(settings);
            // Catalog IDs have already been validated; an exact resource name is the only source of bytes.
            var filename = $"{template.AssetId}.v{template.Version}.ps1";
            var destination = Path.Combine(_directory, filename);
            using var source = _openAsset("Automator.Templates." + filename)
                ?? throw new IOException("Bundled template asset is unavailable. Repair or reinstall Automator.");
            Directory.CreateDirectory(_directory);
            if (File.Exists(destination)) throw new IOException("Template script already exists. Preserve or move it before installing again; Automator will not overwrite it.");
            var staging = Path.Combine(_directory, "." + Guid.NewGuid().ToString("N") + ".tmp");
            var moved = false;
            try
            {
                await using (var target = new FileStream(staging, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    await source.CopyToAsync(target, cancellationToken).ConfigureAwait(false);
                    await target.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
                cancellationToken.ThrowIfCancellationRequested();
                File.Move(staging, destination, overwrite: false);
                moved = true;
                var profile = new ScriptRunnerProfile("template-" + Guid.NewGuid().ToString("N"), template.Name,
                    template.Interpreter, interpreter, destination, [], _directory, template.OutputMode,
                    template.TimeoutSeconds, new(template.Id, template.Version));
                ScriptRunnerModule.Validate(profile);
                await library.UpsertAsync(ScriptRunnerModule.ProfileCollection, profile.Id, ScriptRunnerModule.SettingsVersionValue,
                    JsonSerializer.SerializeToElement(profile, ScriptRunnerModule.JsonOptions), cancellationToken).ConfigureAwait(false);
                return (profile, true);
            }
            catch
            {
                if (moved) File.Delete(destination);
                throw;
            }
            finally { if (File.Exists(staging)) File.Delete(staging); }
        }
        finally { InstallGate.Release(); }
    }

    private string ResolveInterpreter(JsonElement settings)
    {
        var path = _interpreter;
        if (path is null && settings.ValueKind == JsonValueKind.Object && settings.TryGetProperty("interpreterDefaults", out var defaults)
            && defaults.ValueKind == JsonValueKind.Object && defaults.TryGetProperty("powershell", out var configured)
            && configured.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(configured.GetString()))
            path = configured.GetString();
        path ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");
        if (!Path.IsPathFullyQualified(path) || !File.Exists(path))
            throw new InvalidDataException("PowerShell is unavailable. Configure an existing absolute PowerShell interpreter path in Script Runner settings.");
        return path;
    }
}
