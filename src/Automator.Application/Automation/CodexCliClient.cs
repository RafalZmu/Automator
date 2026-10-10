using System.Text;
using System.Text.Json;
using Automator.Core.Automation;

namespace Automator.Application.Automation;

/// <summary>Discovers and invokes the installed Codex CLI using structured process arguments.</summary>
public sealed class CodexCliClient
{
    public const int MaximumResultBytes = 256 * 1024;
    private readonly string _requestRoot;
    private readonly string? _launcherPath;
    private readonly IAutomationProcessService _processService;

    public CodexCliClient(string requestRoot, IAutomationProcessService processService, string? launcherPath = null)
    {
        _requestRoot = Path.GetFullPath(requestRoot);
        _processService = processService ?? throw new ArgumentNullException(nameof(processService));
        _launcherPath = launcherPath ?? DiscoverLauncher();
    }

    public bool IsAvailable => _launcherPath is not null;

    public async Task<CodexTaskStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        if (_launcherPath is null) return new(false, "unavailable", "Codex CLI was not found. Install the supported Codex CLI and try again.");
        try
        {
            var result = await RunAsync(["--version"], null, cancellationToken, timeout: TimeSpan.FromSeconds(15)).ConfigureAwait(false);
            var text = (result.Stdout + " " + result.Stderr).Trim();
            if (LooksLikeAuthError(text)) return new(false, "signedOut", "Codex CLI is installed but signed out. Sign in with the Codex CLI, then retry.");
            if (LooksLikeConfigError(text)) return new(false, "configurationError", "Codex CLI configuration could not be read. Correct the local CLI configuration, then retry.");
            if (result.ExitCode != 0) return new(false, "unavailable", "Codex CLI could not be started.");
            var login = await RunAsync(["login", "status"], null, cancellationToken, timeout: TimeSpan.FromSeconds(15)).ConfigureAwait(false);
            var loginText = (login.Stdout + " " + login.Stderr).Trim();
            if (LooksLikeConfigError(loginText))
                return new(false, "configurationError", "Codex CLI configuration could not be read. Correct the local CLI configuration, then retry.");
            if (LooksLikeAuthError(loginText) || login.ExitCode != 0)
                return new(false, "signedOut", "Codex CLI is installed but signed out. Sign in with the Codex CLI, then retry.");
            return new(true, "ready", "Codex CLI is ready.", Bound(text, 128));
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or TimeoutException or InvalidDataException)
        {
            return new(false, "unavailable", "Codex CLI could not be started.");
        }
    }

    public async Task<JsonElement> GenerateAsync(string prompt, CancellationToken cancellationToken)
    {
        if (_launcherPath is null) throw new InvalidOperationException("Codex CLI is unavailable.");
        Directory.CreateDirectory(_requestRoot);
        var requestDirectory = Path.Combine(_requestRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(requestDirectory);
        EnsureNoReparsePath(requestDirectory);
        var schemaPath = Path.Combine(requestDirectory, "schema.json");
        var resultPath = Path.Combine(requestDirectory, "result.json");
        const string schema = """
            {"type":"object","additionalProperties":false,"required":["plan","format","source","scope","inputs","effects"],"properties":{
              "plan":{"type":"string","minLength":1,"maxLength":12000},
              "format":{"type":"string","enum":["python","playwright","workflow"]},
              "source":{"type":"string","minLength":1,"maxLength":65536},
              "scope":{"type":"array","maxItems":32,"items":{"type":"string","minLength":1,"maxLength":256}},
              "inputs":{"type":"array","maxItems":32,"items":{"type":"object","additionalProperties":false,"required":["name","description","type","required"],"properties":{"name":{"type":"string","pattern":"^[A-Za-z][A-Za-z0-9_]{0,63}$"},"description":{"type":"string","maxLength":512},"type":{"type":"string","enum":["string","number","boolean","path","json"]},"required":{"type":"boolean"}}}},
              "effects":{"type":"array","maxItems":32,"items":{"type":"object","additionalProperties":false,"required":["kind","description","target"],"properties":{"kind":{"type":"string","enum":["read","visit","create","modify","overwrite","delete","submit","send","download"]},"description":{"type":"string","maxLength":512},"target":{"type":"string","maxLength":512}}}}
            }}
            """;
        await using (var schemaStream = new FileStream(schemaPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            var schemaBytes = Encoding.UTF8.GetBytes(schema);
            await schemaStream.WriteAsync(schemaBytes, cancellationToken).ConfigureAwait(false);
        }
        await using (new FileStream(resultPath, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { }
        try
        {
            var cliPrompt = "Create a task draft only. Do not execute the requested task or inspect its inputs. Return a plan and source. " + prompt;
            var args = new List<string>
            {
                "--ask-for-approval", "never",
                "exec", "--json", "--ephemeral", "--ignore-user-config",
                "--sandbox", "read-only",
                "--config", "web_search=\"disabled\"",
            };
            foreach (var feature in DisabledAuthoringFeatures)
            {
                args.Add("--disable");
                args.Add(feature);
            }
            args.AddRange(["--output-schema", schemaPath, "--output-last-message", resultPath, "-"]);
            var result = await RunAsync(args, cliPrompt, cancellationToken, requestDirectory,
                timeout: TimeSpan.FromMinutes(20), allowOutputTruncation: true).ConfigureAwait(false);
            if (result.ExitCode != 0)
            {
                var details = result.Stdout + " " + result.Stderr;
                if (LooksLikeAuthError(details)) throw new InvalidOperationException("Codex CLI is signed out.");
                if (LooksLikeConfigError(details)) throw new InvalidOperationException("Codex CLI configuration could not be read.");
                throw new InvalidOperationException("Codex CLI could not generate a draft.");
            }
            var contents = await ReadResultFileAsync(resultPath, cancellationToken).ConfigureAwait(false);
            return ParseStructuredResult(contents, MaximumResultBytes);
        }
        finally
        {
            try { Directory.Delete(requestDirectory, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    public static JsonElement ParseStructuredResult(string json, int maximumBytes)
    {
        if (maximumBytes <= 0 || Encoding.UTF8.GetByteCount(json) > Math.Min(maximumBytes, MaximumResultBytes))
            throw new InvalidDataException("Codex result is missing, truncated, or exceeds the size limit.");
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 24 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("plan", out var plan) || plan.ValueKind != JsonValueKind.String
                || !root.TryGetProperty("format", out var format) || format.ValueKind != JsonValueKind.String
                || !root.TryGetProperty("source", out var source) || source.ValueKind != JsonValueKind.String
                || !root.TryGetProperty("scope", out var scope) || scope.ValueKind != JsonValueKind.Array
                || !root.TryGetProperty("inputs", out var inputs) || inputs.ValueKind != JsonValueKind.Array
                || !root.TryGetProperty("effects", out var effects) || effects.ValueKind != JsonValueKind.Array)
                throw new InvalidDataException("Codex result does not match the required task schema.");
            if (string.IsNullOrWhiteSpace(plan.GetString()) || string.IsNullOrWhiteSpace(source.GetString())
                || plan.GetString()!.Length > 12_000 || source.GetString()!.Length > 65_536 || scope.GetArrayLength() > 32
                || inputs.GetArrayLength() > 32 || effects.GetArrayLength() > 32
                || format.GetString() is not ("python" or "playwright" or "workflow"))
                throw new InvalidDataException("Codex result exceeds a supported task limit.");
            var scopes = scope.EnumerateArray().Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : null).ToArray();
            if (scopes.Any(item => item is null || item.Length is < 1 or > 256 || item.Any(char.IsControl))
                || scopes.Distinct(StringComparer.OrdinalIgnoreCase).Count() != scopes.Length)
                throw new InvalidDataException("Codex result contains an invalid declaration.");
            ValidateInputs(inputs);
            ValidateEffects(effects);
            return root.Clone();
        }
        catch (JsonException exception) { throw new InvalidDataException("Codex result is invalid or truncated JSON.", exception); }
    }

    private async Task<(int? ExitCode, string Stdout, string Stderr)> RunAsync(IReadOnlyList<string> args, string? stdin,
        CancellationToken cancellationToken, string? workingDirectory = null, TimeSpan? timeout = null, bool allowOutputTruncation = false)
    {
        var isPowerShell = string.Equals(Path.GetExtension(_launcherPath), ".ps1", StringComparison.OrdinalIgnoreCase);
        var executable = isPowerShell ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe") : _launcherPath!;
        var arguments = new List<string>();
        if (isPowerShell)
        {
            arguments.AddRange(["-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", _launcherPath!]);
        }
        arguments.AddRange(args);
        var result = await _processService.ExecuteAsync(new AutomationProcessRequest(executable, arguments,
            workingDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), timeout ?? TimeSpan.FromSeconds(15), stdin), cancellationToken).ConfigureAwait(false);
        if (result.TimedOut) throw new TimeoutException("Codex CLI request timed out.");
        if (!allowOutputTruncation && (result.StandardOutputTruncated || result.StandardErrorTruncated))
            throw new InvalidDataException("Codex CLI output exceeded the supported limit.");
        return (result.ExitCode, Bound(result.StandardOutput, 16_384), Bound(result.StandardError, 16_384));
    }

    private static string? DiscoverLauncher()
    {
        var candidates = new List<string>();
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        candidates.Add(Path.Combine(local, "Programs", "Codex", "codex.exe"));
        candidates.Add(Path.Combine(roaming, "npm", "codex.ps1"));
        foreach (var item in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            candidates.Add(Path.Combine(item, "codex.exe"));
            candidates.Add(Path.Combine(item, "codex.ps1"));
        }
        return candidates.Select(Path.GetFullPath).FirstOrDefault(path => File.Exists(path)
            && (string.Equals(Path.GetExtension(path), ".exe", StringComparison.OrdinalIgnoreCase)
                || string.Equals(Path.GetExtension(path), ".ps1", StringComparison.OrdinalIgnoreCase)));
    }

    private static readonly string[] DisabledAuthoringFeatures =
    [
        "apps", "browser_use", "computer_use", "multi_agent", "plugins", "tool_search",
        "tool_call_mcp_elicitation", "tool_suggest", "shell_snapshot", "shell_tool", "unified_exec",
        "code_mode", "code_mode_only", "codex_hooks", "memories", "workspace_dependencies",
        "skill_mcp_dependency_install", "image_generation", "in_app_browser",
    ];

    private static bool LooksLikeAuthError(string text) => text.Contains("not logged in", StringComparison.OrdinalIgnoreCase)
        || text.Contains("sign in", StringComparison.OrdinalIgnoreCase) || text.Contains("authentication required", StringComparison.OrdinalIgnoreCase);
    private static bool LooksLikeConfigError(string text) => text.Contains("config", StringComparison.OrdinalIgnoreCase)
        && (text.Contains("parse", StringComparison.OrdinalIgnoreCase) || text.Contains("invalid", StringComparison.OrdinalIgnoreCase));
    private static string Bound(string value, int max) => value.Length <= max ? value : value[..max];

    private static void ValidateInputs(JsonElement inputs)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var input in inputs.EnumerateArray())
        {
            if (input.ValueKind != JsonValueKind.Object || input.EnumerateObject().Count() != 4
                || !input.TryGetProperty("name", out var name) || name.ValueKind != JsonValueKind.String
                || !input.TryGetProperty("description", out var description) || description.ValueKind != JsonValueKind.String
                || !input.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.String
                || !input.TryGetProperty("required", out var required) || required.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                throw new InvalidDataException("Codex result contains an invalid input declaration.");
            var nameValue = name.GetString()!;
            var descriptionValue = description.GetString()!;
            if (nameValue.Length is < 1 or > 64 || !char.IsAsciiLetter(nameValue[0])
                || nameValue.Skip(1).Any(character => !char.IsAsciiLetterOrDigit(character) && character != '_')
                || descriptionValue.Length > 512 || descriptionValue.Any(char.IsControl)
                || type.GetString() is not ("string" or "number" or "boolean" or "path" or "json")
                || !names.Add(nameValue))
                throw new InvalidDataException("Codex result contains an invalid input declaration.");
        }
    }

    private static void ValidateEffects(JsonElement effects)
    {
        var allowedKinds = new HashSet<string>(["read", "visit", "create", "modify", "overwrite", "delete", "submit", "send", "download"], StringComparer.Ordinal);
        foreach (var effect in effects.EnumerateArray())
        {
            if (effect.ValueKind != JsonValueKind.Object || effect.EnumerateObject().Count() != 3
                || !effect.TryGetProperty("kind", out var kind) || kind.ValueKind != JsonValueKind.String
                || !effect.TryGetProperty("description", out var description) || description.ValueKind != JsonValueKind.String
                || !effect.TryGetProperty("target", out var target) || target.ValueKind != JsonValueKind.String
                || !allowedKinds.Contains(kind.GetString()!) || string.IsNullOrWhiteSpace(description.GetString())
                || string.IsNullOrWhiteSpace(target.GetString()) || description.GetString()!.Length > 512
                || target.GetString()!.Length > 512 || description.GetString()!.Any(char.IsControl) || target.GetString()!.Any(char.IsControl))
                throw new InvalidDataException("Codex result contains an invalid effect declaration.");
        }
    }

    private static async Task<string> ReadResultFileAsync(string path, CancellationToken cancellationToken)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Codex result file is not a regular file.");
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length is <= 0 or > MaximumResultBytes)
            throw new InvalidDataException("Codex result is missing, truncated, or exceeds the size limit.");
        using var reader = new StreamReader(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true));
        return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void EnsureNoReparsePath(string path)
    {
        var root = Path.GetPathRoot(path)!;
        var current = root;
        foreach (var part in Path.GetFullPath(path)[root.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, part);
            if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Codex request storage cannot use a reparse point.");
        }
    }
}
