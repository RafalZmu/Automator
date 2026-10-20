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
        IAutomationLibrary library, CancellationToken cancellationToken, JsonElement? values = null, bool additionalCopy = false)
    {
        var template = ScriptRunnerTemplateCatalog.Get(id) ?? throw new InvalidDataException("The selected template is not registered.");
        await InstallGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var records = await library.ListAsync(ScriptRunnerModule.ProfileCollection, cancellationToken).ConfigureAwait(false);
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var record in records)
            {
                var existing = JsonSerializer.Deserialize<ScriptRunnerProfile>(record.Data.GetRawText(), ScriptRunnerModule.JsonOptions);
                if (existing is not null) usedNames.Add(existing.Name);
                if (!additionalCopy && existing?.TemplateOrigin is { } origin && origin.Id == template.Id && origin.Version == template.Version)
                {
                    ScriptRunnerModule.Validate(existing);
                    if (!File.Exists(existing.ScriptPath)) throw new IOException("Installed template script is missing. Restore the script or remove its profile and install again.");
                    if (!File.Exists(existing.InterpreterPath)) throw new IOException("Installed template PowerShell interpreter is missing. Repair its saved interpreter path in Script Runner; Automator will not rewrite it.");
                    return (existing, false);
                }
            }
            var interpreter = ResolveInterpreter(settings);
            // Catalog IDs have already been validated; an exact resource name is the only source of bytes.
            var filename = $"{template.AssetId}.v{template.Version}.ps1";
            using var source = _openAsset("Automator.Templates." + filename)
                ?? throw new IOException("Bundled template asset is unavailable. Repair or reinstall Automator.");
            Directory.CreateDirectory(_directory);
            // Read the bundled bytes once so every comparison and the copy use the same content.
            using var bundled = new MemoryStream();
            await source.CopyToAsync(bundled, cancellationToken).ConfigureAwait(false);
            var bundledBytes = bundled.ToArray();
            // An installed script that is byte-identical to the bundled asset is shared. A file that differs (edited by the user, or left
            // by an older build) is never overwritten or reused; the bundled script goes to the next free numbered name beside it.
            var destination = Path.Combine(_directory, filename);
            for (var suffix = 2; File.Exists(destination) && !SameBytes(bundledBytes, destination); suffix++)
                destination = Path.Combine(_directory, $"{template.AssetId}.v{template.Version}.{suffix}.ps1");
            var reuseInstalled = File.Exists(destination);
            var name = template.Name;
            if (usedNames.Contains(name))
            {
                var copyNumber = 2;
                while (usedNames.Contains($"{template.Name} ({copyNumber})")) copyNumber++;
                name = $"{template.Name} ({copyNumber})";
            }
            var staging = Path.Combine(_directory, "." + Guid.NewGuid().ToString("N") + ".tmp");
            var moved = false;
            try
            {
                if (!reuseInstalled)
                {
                    await using (var target = new FileStream(staging, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        await target.WriteAsync(bundledBytes, cancellationToken).ConfigureAwait(false);
                        await target.FlushAsync(cancellationToken).ConfigureAwait(false);
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                    File.Move(staging, destination, overwrite: false);
                    moved = true;
                }
                // Values remembered at install time are validated against the catalog; sensitive inputs are rejected there.
                var profile = new ScriptRunnerProfile("template-" + Guid.NewGuid().ToString("N"), name,
                    template.Interpreter, interpreter, destination, [], _directory, template.OutputMode,
                    template.TimeoutSeconds, new(template.Id, template.Version), Values: values);
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

    private static bool SameBytes(byte[] expected, string path)
    {
        var info = new FileInfo(path);
        return info.Length == expected.Length && expected.AsSpan().SequenceEqual(File.ReadAllBytes(path));
    }

    /// <summary>Confirms a profile still points at the exact registered asset installed under this data root.</summary>
    public bool MatchesInstalledAsset(ScriptRunnerProfile profile)
    {
        var origin = profile.TemplateOrigin;
        if (origin is null) return false;
        var template = ScriptRunnerTemplateCatalog.Get(origin.Id);
        if (template is null || origin.Version != template.Version || profile.Interpreter != template.Interpreter) return false;
        var filename = $"{template.AssetId}.v{template.Version}.ps1";
        string expectedPath;
        string actualPath;
        try
        {
            expectedPath = Path.GetFullPath(Path.Combine(_directory, filename));
            actualPath = Path.GetFullPath(profile.ScriptPath);
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return false;
        }
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!string.Equals(expectedPath, actualPath, comparison) && !IsNumberedInstall(template.AssetId, template.Version, actualPath, comparison)) return false;

        using var source = _openAsset("Automator.Templates." + filename);
        if (source is null || !File.Exists(actualPath)) return false;
        using var expected = new MemoryStream();
        source.CopyTo(expected);
        var info = new FileInfo(actualPath);
        if (info.Length != expected.Length) return false;
        var installedBytes = File.ReadAllBytes(actualPath);
        return expected.ToArray().AsSpan().SequenceEqual(installedBytes);
    }

    /// <summary>True for the numbered file name used when an unrelated script already occupies the plain installed name.</summary>
    private bool IsNumberedInstall(string assetId, int version, string actualPath, StringComparison comparison)
    {
        var name = Path.GetFileName(actualPath);
        var prefix = $"{assetId}.v{version}.";
        if (!string.Equals(Path.GetDirectoryName(actualPath), Path.GetFullPath(_directory).TrimEnd(Path.DirectorySeparatorChar), comparison)
            || !name.StartsWith(prefix, comparison) || !name.EndsWith(".ps1", comparison)) return false;
        var middle = name[prefix.Length..^4];
        return middle.Length > 0 && middle.All(char.IsAsciiDigit);
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
