using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Automator.Core.Automation;
using Automator.Core.Plugins;

namespace Automator.Application.Automation;

/// <summary>Host-owned Playwright Test project discovery, metadata, file creation, and execution.</summary>
internal sealed class PlaywrightTestExplorer(IAutomationLibrary library, IAutomationProcessService processes)
{
    public PlaywrightTestExplorer(IAutomationLibraryStore libraryStore, IAutomationProcessService processService)
        : this(new BrowserLibraryFacade(libraryStore), processService) { }

    private const string ExplorerCollection = "test-explorer";
    private const string ProjectRecordId = "active-project";
    private const string TagCollection = "test-tags";
    private const int SchemaVersion = 1;
    private const int MaximumDiscoveredTests = 2000;
    private const int MaximumTagRecords = 4096;
    private const int MaximumTagsPerTest = 16;
    private const int MaximumTagLength = 48;
    private static readonly TimeSpan DiscoveryTimeout = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan MaximumRunTimeout = TimeSpan.FromMinutes(30);
    private static readonly Regex TestListLine = new(
        "^(?:\\[(?<project>[^\\]]{1,128})\\]\\s*[›>]\\s*)?(?<file>.+?\\.(?:spec|test)\\.[jt]s)(?::(?<line>\\d{1,8})(?::(?<column>\\d{1,8}))?)?\\s*[›>]\\s*(?<titles>.+)$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex SafeTag = new("^[A-Za-z0-9][A-Za-z0-9 ._-]{0,47}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    public static string? ConfiguredManagedProjectRoot => GetConfiguredManagedProjectRoot();

    public async Task<string> CreateCodexTaskAsync(string draftId, string source, CancellationToken cancellationToken)
    {
        CodexTaskService.ValidateDraftId(draftId);
        if (source is null || Encoding.UTF8.GetByteCount(source) > 256 * 1024)
            throw new InvalidDataException("Generated Playwright source is missing or exceeds the 256 KiB limit.");
        var root = GetConfiguredManagedProjectRoot() ?? throw new InvalidDataException("Automator's managed Playwright project is unavailable.");
        await EnsureManagedProjectAsync(root, cancellationToken).ConfigureAwait(false);
        var relative = $"tests/codex/{draftId}.spec.ts";
        var target = ResolveProjectFile(root, relative, inspectExistingSegments: true);
        EnsureNoReparsePoints(root, relative, includeFile: false);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        EnsureNoReparsePoints(root, relative, includeFile: false);
        try
        {
            await using var stream = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, true);
            await stream.WriteAsync(Encoding.UTF8.GetBytes(source), cancellationToken).ConfigureAwait(false);
        }
        catch (IOException) when (File.Exists(target))
        {
            throw new InvalidDataException("A generated Playwright file already exists for this draft.");
        }
        return relative;
    }

    public async Task<(AutomationStatus Status, JsonElement Output, string Message)> RunCodexTaskAsync(
        string relativePath, string expectedSource, JsonElement? input, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(relativePath) || !IsValidTestPath(relativePath) || !relativePath.StartsWith("tests/codex/", StringComparison.Ordinal)
            || !Path.GetFileName(relativePath).EndsWith(".spec.ts", StringComparison.Ordinal))
            throw new InvalidDataException("Generated Playwright path is invalid.");
        var root = GetConfiguredManagedProjectRoot() ?? throw new InvalidDataException("Automator's managed Playwright project is unavailable.");
        var target = ResolveProjectFile(root, relativePath, inspectExistingSegments: true);
        EnsureNoReparsePoints(root, relativePath, includeFile: true);
        var info = new FileInfo(target);
        if (!info.Exists || info.Length > 256 * 1024) throw new InvalidDataException("Generated Playwright source is unavailable or too large.");
        var actualSource = await File.ReadAllTextAsync(target, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(actualSource, expectedSource, StringComparison.Ordinal))
            throw new InvalidDataException("The managed Playwright source changed after review. Review and approve the current source before running it.");
        var runner = ResolveRunner(root);
        if (!runner.Available) throw new InvalidDataException(runner.Message);
        var env = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AUTOMATOR_WORKFLOW_INPUT_JSON"] = input?.GetRawText() ?? "{}"
        };
        var process = await processes.ExecuteAsync(new AutomationProcessRequest(runner.NodePath!,
            [runner.CliPath!, "test", "--workers=1", "--reporter=json", relativePath], root,
            TimeSpan.FromMinutes(10), EnvironmentVariables: env), cancellationToken).ConfigureAwait(false);
        var status = process.TimedOut ? AutomationStatus.Warning : process.ExitCode == 0 ? AutomationStatus.Success : AutomationStatus.Error;
        var message = process.TimedOut ? "Generated Playwright task exceeded its 10-minute limit."
            : process.ExitCode == 0 ? "Generated Playwright task completed." : "Generated Playwright task failed.";
        var output = JsonSerializer.SerializeToElement(new { exitCode = process.ExitCode, timedOut = process.TimedOut,
            stdout = process.StandardOutput, stderr = process.StandardError, stdoutTruncated = process.StandardOutputTruncated,
            stderrTruncated = process.StandardErrorTruncated, durationMilliseconds = process.DurationMilliseconds });
        return (status, output, message);
    }

    public async Task<string> ReadCodexTaskSourceAsync(string relativePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(relativePath) || !IsValidTestPath(relativePath) || !relativePath.StartsWith("tests/codex/", StringComparison.Ordinal)
            || !Path.GetFileName(relativePath).EndsWith(".spec.ts", StringComparison.Ordinal))
            throw new InvalidDataException("Generated Playwright path is invalid.");
        var root = GetConfiguredManagedProjectRoot() ?? throw new InvalidDataException("Automator's managed Playwright project is unavailable.");
        var target = ResolveProjectFile(root, relativePath, inspectExistingSegments: true);
        EnsureNoReparsePoints(root, relativePath, includeFile: true);
        var info = new FileInfo(target);
        if (!info.Exists || info.Length > 256 * 1024) throw new InvalidDataException("Generated Playwright source is unavailable or too large.");
        return await File.ReadAllTextAsync(target, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AutomationResult> GetStateAsync(CancellationToken cancellationToken)
    {
        var root = await GetProjectRootAsync(cancellationToken).ConfigureAwait(false);
        var tags = root is null ? [] : await ReadTagRecordsAsync(root, cancellationToken).ConfigureAwait(false);
        var runner = root is null ? RunnerState.MissingProject : ResolveRunner(root);
        return Result(AutomationStatus.Success,
            root is null ? "Choose a Playwright project folder to browse tests." : runner.Message,
            new { state = MakeState(root, [], tags, [], runner) });
    }

    public async Task<AutomationResult> SetProjectAsync(JsonElement input, CancellationToken cancellationToken)
    {
        var requested = ReadString(input, "projectRoot", 4096);
        string root;
        try { root = Path.GetFullPath(requested); }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        { throw new InvalidDataException("Choose a valid Playwright project folder."); }
        if (!Directory.Exists(root)) throw new InvalidDataException("The selected Playwright project folder does not exist.");

        await library.UpsertAsync(ExplorerCollection, ProjectRecordId, SchemaVersion,
            JsonSerializer.SerializeToElement(new ProjectSettings(root), JsonOptions), cancellationToken).ConfigureAwait(false);
        var runner = ResolveRunner(root);
        return Result(AutomationStatus.Success, runner.Message, new { state = MakeState(root, [],
            await ReadTagRecordsAsync(root, cancellationToken).ConfigureAwait(false), [], runner) });
    }

    public async Task<AutomationResult> DiscoverAsync(CancellationToken cancellationToken)
    {
        var root = await RequireProjectRootAsync(cancellationToken).ConfigureAwait(false);
        var runner = ResolveRunner(root);
        if (!runner.Available)
            return Result(AutomationStatus.Warning, runner.Message,
                new { state = MakeState(root, [], await ReadTagRecordsAsync(root, cancellationToken).ConfigureAwait(false), [], runner) });

        var processResult = await processes.ExecuteAsync(new AutomationProcessRequest(runner.NodePath!,
            [runner.CliPath!, "test", "--list"], root, DiscoveryTimeout), cancellationToken).ConfigureAwait(false);
        if (processResult.TimedOut)
            return Result(AutomationStatus.Warning, "Playwright test discovery exceeded its 90-second limit. Check the project's test configuration and refresh again.",
                new { state = MakeState(root, [], await ReadTagRecordsAsync(root, cancellationToken).ConfigureAwait(false), [], runner) });
        if (processResult.ExitCode != 0)
            return Result(AutomationStatus.Error, "Playwright could not list tests. Check the selected project's configuration and dependencies.",
                new { state = MakeState(root, [], await ReadTagRecordsAsync(root, cancellationToken).ConfigureAwait(false), [], runner),
                    output = BoundMessage(processResult.StandardError, processResult.StandardOutput) });

        var tests = ParseTestList(processResult.StandardOutput, root).Take(MaximumDiscoveredTests + 1).ToArray();
        var truncated = tests.Length > MaximumDiscoveredTests;
        if (truncated) tests = tests[..MaximumDiscoveredTests];
        var savedTags = await ReadTagRecordsAsync(root, cancellationToken).ConfigureAwait(false);
        var currentIds = tests.Select(test => test.Id).ToHashSet(StringComparer.Ordinal);
        var stale = savedTags.Where(record => !currentIds.Contains(record.Identity.Id)).ToArray();
        var state = MakeState(root, tests, savedTags, stale, runner);
        var countMessage = $"Found {tests.Length} Playwright test{(tests.Length == 1 ? string.Empty : "s")} in {state.Files.Count} file{(state.Files.Count == 1 ? string.Empty : "s")}.";
        if (truncated) countMessage += " The display is limited to the first 2,000 tests.";
        if (processResult.StandardOutputTruncated) countMessage += " Playwright discovery output was truncated.";
        return Result(AutomationStatus.Success, countMessage, new { state });
    }

    public async Task<AutomationResult> CreateSectionAsync(JsonElement input, CancellationToken cancellationToken)
    {
        var root = await RequireProjectRootAsync(cancellationToken).ConfigureAwait(false);
        var relativeFile = ReadSectionPath(input);
        var testName = ReadTestName(input);
        var target = ResolveProjectFile(root, relativeFile, inspectExistingSegments: true);
        EnsureNoReparsePoints(root, relativeFile, includeFile: false);
        var parent = Path.GetDirectoryName(target)!;
        Directory.CreateDirectory(parent);
        EnsureNoReparsePoints(root, relativeFile, includeFile: false);
        const string header = "import { test } from '@playwright/test';\n\n";
        var source = header + MakeTestSource(testName);
        try
        {
            await using var stream = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true);
            await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            await writer.WriteAsync(source.AsMemory(), cancellationToken).ConfigureAwait(false);
        }
        catch (IOException) when (File.Exists(target))
        {
            throw new InvalidDataException("A test file already exists at that path. Refresh the explorer and add a test to the existing section instead.");
        }
        return Result(AutomationStatus.Success, $"Created {relativeFile}.", new { file = relativeFile, testName });
    }

    public async Task<AutomationResult> AppendTestAsync(JsonElement input, CancellationToken cancellationToken)
    {
        var root = await RequireProjectRootAsync(cancellationToken).ConfigureAwait(false);
        var relativeFile = ReadSectionPath(input);
        var testName = ReadTestName(input);
        var target = ResolveProjectFile(root, relativeFile, inspectExistingSegments: true);
        if (!File.Exists(target)) throw new InvalidDataException("That section file no longer exists. Refresh the explorer first.");
        EnsureNoReparsePoints(root, relativeFile, includeFile: true);
        var source = await File.ReadAllTextAsync(target, cancellationToken).ConfigureAwait(false);
        if (source.Length > 2 * 1024 * 1024) throw new InvalidDataException("This test file is too large to edit from Automator.");
        var needsImport = !Regex.IsMatch(source,
            "import\\s+\\{[^}]*\\btest\\b[^}]*\\}\\s+from\\s+['\"]@playwright/test['\"]|require\\(['\"]@playwright/test['\"]\\)",
            RegexOptions.CultureInvariant);
        var updated = source.TrimEnd() + "\n\n" + MakeTestSource(testName);
        if (needsImport) updated = "import { test } from '@playwright/test';\n\n" + updated;
        await File.WriteAllTextAsync(target, updated, new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
        return Result(AutomationStatus.Success, $"Added a test to {relativeFile}.", new { file = relativeFile, testName });
    }

    public async Task<AutomationResult> SaveTagsAsync(JsonElement input, CancellationToken cancellationToken)
    {
        var root = await RequireProjectRootAsync(cancellationToken).ConfigureAwait(false);
        var id = ReadString(input, "testId", 96);
        var runner = ResolveRunner(root);
        if (!runner.Available) return Result(AutomationStatus.Warning, runner.Message, new { });
        var discovered = await DiscoverTestsAsync(root, runner, cancellationToken).ConfigureAwait(false);
        var test = discovered.Tests.FirstOrDefault(candidate => string.Equals(candidate.Id, id, StringComparison.Ordinal));
        if (test is null) throw new InvalidDataException("That test is no longer in the selected project. Refresh before editing its tags.");

        var tags = ReadTags(input);
        var recordId = TestRecordId(root, test.Id);
        if (tags.Count == 0)
        {
            await library.DeleteAsync(TagCollection, recordId, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var record = new TestTagRecord(root, test, tags);
            await library.UpsertAsync(TagCollection, recordId, SchemaVersion,
                JsonSerializer.SerializeToElement(record, JsonOptions), cancellationToken).ConfigureAwait(false);
        }
        return Result(AutomationStatus.Success, tags.Count == 0 ? "Removed tags from this test." : "Saved test tags.", new { testId = id, tags });
    }

    public async Task<AutomationResult> RemoveStaleTagsAsync(CancellationToken cancellationToken)
    {
        var root = await RequireProjectRootAsync(cancellationToken).ConfigureAwait(false);
        var runner = ResolveRunner(root);
        if (!runner.Available) return Result(AutomationStatus.Warning, runner.Message, new { removed = 0 });
        var discovered = await DiscoverTestsAsync(root, runner, cancellationToken).ConfigureAwait(false);
        var currentIds = discovered.Tests.Select(test => test.Id).ToHashSet(StringComparer.Ordinal);
        var records = await ReadTagRecordsAsync(root, cancellationToken).ConfigureAwait(false);
        var stale = records.Where(record => !currentIds.Contains(record.Identity.Id)).ToArray();
        foreach (var record in stale)
            await library.DeleteAsync(TagCollection, TestRecordId(root, record.Identity.Id), cancellationToken).ConfigureAwait(false);
        return Result(AutomationStatus.Success, stale.Length == 0 ? "There are no stale tag records." : $"Removed {stale.Length} stale tag record{(stale.Length == 1 ? string.Empty : "s")}.", new { removed = stale.Length });
    }

    public async Task<AutomationResult> RunAsync(JsonElement input, CancellationToken cancellationToken)
    {
        var root = await RequireProjectRootAsync(cancellationToken).ConfigureAwait(false);
        var runner = ResolveRunner(root);
        if (!runner.Available) return Result(AutomationStatus.Warning, runner.Message, new { });
        var discovered = await DiscoverTestsAsync(root, runner, cancellationToken).ConfigureAwait(false);
        var kind = ReadString(input, "kind", 16);
        IReadOnlyList<string> arguments;
        string displayTarget;
        string? temporaryList = null;
        var timeoutSeconds = 600;
        if (input.ValueKind == JsonValueKind.Object && input.TryGetProperty("timeoutSeconds", out var timeoutValue)
            && (!timeoutValue.TryGetInt32(out timeoutSeconds) || timeoutSeconds is < 1 or > 1800))
            throw new InvalidDataException("Playwright run timeout must be from 1 to 1,800 seconds.");
        switch (kind)
        {
            case "file":
            {
                var relativeFile = ReadSectionPath(input);
                var selected = discovered.Tests.Where(test => string.Equals(test.File, relativeFile, StringComparison.OrdinalIgnoreCase)).ToArray();
                if (selected.Length == 0)
                    throw new InvalidDataException("That test file has no discovered tests. Refresh the explorer first.");
                temporaryList = await WriteTestListAsync(selected.Select(test => test.ListEntry), cancellationToken).ConfigureAwait(false);
                arguments = [runner.CliPath!, "test", "--workers=1", "--reporter=json", "--test-list", temporaryList];
                displayTarget = relativeFile;
                break;
            }
            case "test":
            {
                var id = ReadString(input, "testId", 96);
                var test = discovered.Tests.FirstOrDefault(candidate => string.Equals(candidate.Id, id, StringComparison.Ordinal));
                if (test is null) throw new InvalidDataException("That test is no longer discovered. Refresh the explorer first.");
                temporaryList = await WriteTestListAsync([test.ListEntry], cancellationToken).ConfigureAwait(false);
                arguments = [runner.CliPath!, "test", "--workers=1", "--reporter=json", "--test-list", temporaryList];
                displayTarget = test.TitlePath[^1];
                break;
            }
            case "tag":
            {
                var tag = NormalizeTag(ReadString(input, "tag", MaximumTagLength));
                var taggedIds = (await ReadTagRecordsAsync(root, cancellationToken).ConfigureAwait(false))
                    .Where(record => record.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                    .Select(record => record.Identity.Id).ToHashSet(StringComparer.Ordinal);
                var selected = discovered.Tests.Where(test => taggedIds.Contains(test.Id)).ToArray();
                if (selected.Length == 0) return Result(AutomationStatus.Information, $"No current tests have the “{tag}” tag.", new { tag, selectedTests = 0 });
                temporaryList = await WriteTestListAsync(selected.Select(test => test.ListEntry), cancellationToken).ConfigureAwait(false);
                arguments = [runner.CliPath!, "test", "--workers=1", "--reporter=json", "--test-list", temporaryList];
                displayTarget = $"tag {tag}";
                break;
            }
            default:
                throw new InvalidDataException("Choose a file, test, or tag group to run.");
        }

        try
        {
            var process = await processes.ExecuteAsync(new AutomationProcessRequest(runner.NodePath!, arguments, root,
                TimeSpan.FromSeconds(Math.Min(timeoutSeconds, (int)MaximumRunTimeout.TotalSeconds))), cancellationToken).ConfigureAwait(false);
            if (process.TimedOut)
                return Result(AutomationStatus.Warning, $"Playwright run for {displayTarget} exceeded its {timeoutSeconds}-second limit.",
                    new { timedOut = true, exitCode = (int?)null, durationMilliseconds = process.DurationMilliseconds, summary = (RunSummary?)null, tests = Array.Empty<PlaywrightTestOutcome>(), outputTruncated = process.StandardOutputTruncated || process.StandardErrorTruncated });

            var report = ParseJsonReport(process.StandardOutput, discovered.Tests);
            var status = process.ExitCode == 0 ? AutomationStatus.Success : AutomationStatus.Error;
            var summary = report.Summary;
            var message = summary is null
                ? process.ExitCode == 0 ? $"Playwright completed {displayTarget}; per-test summary was unavailable." : $"Playwright failed while running {displayTarget} (exit code {process.ExitCode})."
                : $"{displayTarget}: {summary.Passed} passed, {summary.Failed} failed, {summary.Skipped} skipped{(summary.Interrupted > 0 ? $", {summary.Interrupted} interrupted" : string.Empty)}.";
            return Result(status, message, new
            {
                timedOut = false,
                exitCode = process.ExitCode,
                durationMilliseconds = process.DurationMilliseconds,
                summary,
                tests = report.Tests,
                outputTruncated = process.StandardOutputTruncated || process.StandardErrorTruncated,
            });
        }
        finally
        {
            if (temporaryList is not null) TryDelete(temporaryList);
        }
    }

    private async Task<(IReadOnlyList<PlaywrightTestIdentity> Tests, bool Truncated)> DiscoverTestsAsync(
        string root, RunnerState runner, CancellationToken cancellationToken)
    {
        var process = await processes.ExecuteAsync(new AutomationProcessRequest(runner.NodePath!,
            [runner.CliPath!, "test", "--list"], root, DiscoveryTimeout), cancellationToken).ConfigureAwait(false);
        if (process.TimedOut) throw new InvalidDataException("Playwright test discovery exceeded its 90-second limit.");
        if (process.ExitCode != 0) throw new InvalidDataException("Playwright could not list tests. Check the selected project's configuration and dependencies.");
        var tests = ParseTestList(process.StandardOutput, root).Take(MaximumDiscoveredTests + 1).ToArray();
        return tests.Length > MaximumDiscoveredTests ? (tests[..MaximumDiscoveredTests], true) : (tests, process.StandardOutputTruncated);
    }

    private static IReadOnlyList<PlaywrightTestIdentity> ParseTestList(string output, string root)
    {
        var parsed = new Dictionary<string, PlaywrightTestIdentity>(StringComparer.Ordinal);
        foreach (var raw in output.Split('\n'))
        {
            var line = StripAnsi(raw).Trim();
            var match = TestListLine.Match(line);
            if (!match.Success) continue;
            var file = NormalizeListedFile(root, match.Groups["file"].Value);
            if (file is null) continue;
            var lineNumber = ReadPositiveInt(match.Groups["line"].Value);
            var column = ReadNullablePositiveInt(match.Groups["column"].Value);
            var titlePath = Regex.Split(match.Groups["titles"].Value, "\\s*[›>]\\s*")
                .Select(title => title.Trim()).Where(title => title.Length is > 0 and <= 512).Take(24).ToArray();
            if (titlePath.Length == 0) continue;
            var project = match.Groups["project"].Success ? match.Groups["project"].Value.Trim() : null;
            var listEntry = $"{(project is null ? string.Empty : $"[{project}] › ")}{file}{(lineNumber > 0 ? $":{lineNumber}{(column is null ? string.Empty : $":{column}")}" : string.Empty)} › {string.Join(" › ", titlePath)}";
            var identity = new PlaywrightTestIdentity(
                MakeTestId(root, project, file, lineNumber, column, titlePath), project, file, lineNumber, column,
                Array.AsReadOnly(titlePath), listEntry, Array.Empty<string>());
            parsed[identity.Id] = identity;
        }
        return Array.AsReadOnly(parsed.Values.Take(MaximumDiscoveredTests + 1).ToArray());
    }

    private static PlaywrightTestExplorerState MakeState(string? root, IReadOnlyList<PlaywrightTestIdentity> tests,
        IReadOnlyList<TestTagRecord> tagRecords, IReadOnlyList<TestTagRecord> stale, RunnerState runner)
    {
        var tagsById = tagRecords.Where(record => root is not null && SamePath(record.ProjectRoot, root))
            .ToDictionary(record => record.Identity.Id, record => record.Tags, StringComparer.Ordinal);
        var taggedTests = tests.Select(test => test with
        {
            Tags = tagsById.TryGetValue(test.Id, out var tags) ? tags : Array.Empty<string>(),
        }).ToArray();
        var files = taggedTests.GroupBy(test => test.File, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.CurrentCultureIgnoreCase)
            .Select(group => new PlaywrightTestFileSection(group.Key, group.ToArray())).ToArray();
        var currentStale = stale.Where(record => root is not null && SamePath(record.ProjectRoot, root))
            .Select(record => new StaleTagSummary(record.Identity.File, record.Identity.TitlePath.LastOrDefault() ?? "Untitled test", record.Tags))
            .Take(256).ToArray();
        var distinctTags = tagRecords.Where(record => root is not null && SamePath(record.ProjectRoot, root))
            .SelectMany(record => record.Tags).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag, StringComparer.CurrentCultureIgnoreCase).Take(128).ToArray();
        var managedRoot = GetConfiguredManagedProjectRoot();
        return new PlaywrightTestExplorerState(root, root is not null && managedRoot is not null && SamePath(root, managedRoot), files, distinctTags, currentStale,
            new PlaywrightRunnerSummary(runner.Available, runner.Version, runner.Message));
    }

    private async Task<IReadOnlyList<TestTagRecord>> ReadTagRecordsAsync(string root, CancellationToken cancellationToken)
    {
        var rows = await library.ListAsync(TagCollection, cancellationToken).ConfigureAwait(false);
        return rows.Where(row => row.SchemaVersion == SchemaVersion).Take(MaximumTagRecords)
            .Select(row => TryReadTagRecord(row.Data)).Where(record => record is not null)
            .Cast<TestTagRecord>().Where(record => SamePath(record.ProjectRoot, root)).ToArray();
    }

    private static TestTagRecord? TryReadTagRecord(JsonElement data)
    {
        try
        {
            var record = data.Deserialize<TestTagRecord>(JsonOptions);
            if (record is null || !Path.IsPathFullyQualified(record.ProjectRoot) || record.Identity is null
                || string.IsNullOrWhiteSpace(record.Identity.Id) || !IsValidTestPath(record.Identity.File)
                || record.Identity.TitlePath is null || record.Identity.TitlePath.Count is < 1 or > 24
                || record.Tags is null || record.Tags.Count > MaximumTagsPerTest
                || record.Tags.Any(tag => !SafeTag.IsMatch(tag))) return null;
            return record;
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException or NotSupportedException) { return null; }
    }

    private async Task<string?> GetProjectRootAsync(CancellationToken cancellationToken)
    {
        var record = await library.GetAsync(ExplorerCollection, ProjectRecordId, cancellationToken).ConfigureAwait(false);
        if (record is not null && record.SchemaVersion == SchemaVersion)
        {
            try
            {
                var settings = record.Data.Deserialize<ProjectSettings>(JsonOptions);
                if (settings is { ProjectRoot.Length: > 0 } && Path.IsPathFullyQualified(settings.ProjectRoot))
                {
                    var savedRoot = Path.GetFullPath(settings.ProjectRoot);
                    var managedRoot = GetConfiguredManagedProjectRoot();
                    if (managedRoot is not null && SamePath(savedRoot, managedRoot))
                        await EnsureManagedProjectAsync(savedRoot, cancellationToken).ConfigureAwait(false);
                    return savedRoot;
                }
            }
            catch (Exception exception) when (exception is JsonException or ArgumentException or NotSupportedException) { }
        }

        var defaultRoot = GetConfiguredManagedProjectRoot();
        if (defaultRoot is null) return null;
        await EnsureManagedProjectAsync(defaultRoot, cancellationToken).ConfigureAwait(false);
        await library.UpsertAsync(ExplorerCollection, ProjectRecordId, SchemaVersion,
            JsonSerializer.SerializeToElement(new ProjectSettings(defaultRoot), JsonOptions), cancellationToken).ConfigureAwait(false);
        return defaultRoot;
    }

    private static string? GetConfiguredManagedProjectRoot()
    {
        var configured = Environment.GetEnvironmentVariable("AUTOMATOR_PLAYWRIGHT_PROJECT_ROOT");
        if (string.IsNullOrWhiteSpace(configured) || !Path.IsPathFullyQualified(configured)) return null;
        try { return Path.GetFullPath(configured); }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException) { return null; }
    }

    private static async Task EnsureManagedProjectAsync(string root, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(root);
        const string exampleRelativePath = "tests/example.spec.ts";
        var examplePath = Path.Combine(root, "tests", "example.spec.ts");
        Directory.CreateDirectory(Path.GetDirectoryName(examplePath)!);
        EnsureNoReparsePoints(root, exampleRelativePath, includeFile: false);
        if (!File.Exists(examplePath))
        {
            const string exampleSource = "import { expect, test } from '@playwright/test';\n\n"
                + "test('Automator example test', async () => {\n  expect(2 + 2).toBe(4);\n});\n";
            try
            {
                await using var stream = new FileStream(examplePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true);
                await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
                await writer.WriteAsync(exampleSource.AsMemory(), cancellationToken).ConfigureAwait(false);
            }
            catch (IOException) when (File.Exists(examplePath)) { }
        }

        var moduleRoot = Environment.GetEnvironmentVariable("AUTOMATOR_PLAYWRIGHT_MODULE_ROOT");
        if (string.IsNullOrWhiteSpace(moduleRoot) || !Path.IsPathFullyQualified(moduleRoot)) return;
        string modulePath;
        try { modulePath = Path.GetFullPath(moduleRoot); }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException) { return; }
        var testEntry = Path.Combine(modulePath, "test.js");
        var packageFile = Path.Combine(modulePath, "package.json");
        if (!File.Exists(testEntry) || !File.Exists(packageFile)) return;

        var adapterRoot = Path.Combine(root, "node_modules", "@playwright", "test");
        var markerPath = Path.Combine(adapterRoot, ".automator-runtime.json");
        EnsureNoReparsePoints(root, "node_modules/@playwright/test/index.js", includeFile: false);
        if (Directory.Exists(adapterRoot) && !File.Exists(markerPath)) return;
        Directory.CreateDirectory(adapterRoot);
        using var package = JsonDocument.Parse(await File.ReadAllTextAsync(packageFile, cancellationToken).ConfigureAwait(false));
        var version = package.RootElement.TryGetProperty("version", out var versionNode) && versionNode.ValueKind == JsonValueKind.String
            ? versionNode.GetString() ?? "bundled"
            : "bundled";
        await File.WriteAllTextAsync(Path.Combine(adapterRoot, "package.json"), JsonSerializer.Serialize(new
        {
            name = "@playwright/test",
            version,
            type = "module",
            exports = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal)
            {
                ["."] = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["import"] = "./index.mjs",
                    ["require"] = "./index.cjs",
                },
            },
        }), new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(adapterRoot, "index.cjs"),
            $"module.exports = require({JsonSerializer.Serialize(testEntry)});\n", new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
        var esmEntry = Path.Combine(modulePath, "test.mjs");
        await File.WriteAllTextAsync(Path.Combine(adapterRoot, "index.mjs"),
            $"export * from {JsonSerializer.Serialize(new Uri(esmEntry).AbsoluteUri)};\nexport {{ default }} from {JsonSerializer.Serialize(new Uri(esmEntry).AbsoluteUri)};\n",
            new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(markerPath, JsonSerializer.Serialize(new { moduleRoot = modulePath, version }),
            new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> RequireProjectRootAsync(CancellationToken cancellationToken)
    {
        var root = await GetProjectRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null || !Directory.Exists(root)) throw new InvalidDataException("Choose an existing Playwright project folder first.");
        return root;
    }

    private static RunnerState ResolveRunner(string root)
    {
        var configuredNode = Environment.GetEnvironmentVariable("AUTOMATOR_BROWSER_NODE_PATH");
        var nodePath = !string.IsNullOrWhiteSpace(configuredNode) ? configuredNode : Path.Combine(AppContext.BaseDirectory, "node.exe");
        if (!File.Exists(nodePath)) return RunnerState.Unavailable("Automator's Node.js runtime is unavailable. Repair or reinstall Automator, then refresh.");

        var candidates = new[]
        {
            (Cli: Path.Combine(root, "node_modules", "@playwright", "test", "cli.js"), Package: Path.Combine(root, "node_modules", "@playwright", "test", "package.json")),
        };
        var managedRoot = GetConfiguredManagedProjectRoot();
        if (managedRoot is not null && SamePath(root, managedRoot))
        {
            var moduleRoot = Environment.GetEnvironmentVariable("AUTOMATOR_PLAYWRIGHT_MODULE_ROOT");
            if (!string.IsNullOrWhiteSpace(moduleRoot) && Path.IsPathFullyQualified(moduleRoot))
            {
                try
                {
                    var module = Path.GetFullPath(moduleRoot);
                    var cli = Path.Combine(module, "cli.js");
                    var package = Path.Combine(module, "package.json");
                    if (File.Exists(cli) && File.Exists(package))
                    {
                        using var packageJson = JsonDocument.Parse(File.ReadAllText(package));
                        var version = packageJson.RootElement.TryGetProperty("version", out var versionNode)
                            && versionNode.ValueKind == JsonValueKind.String ? versionNode.GetString() : null;
                        return new RunnerState(true, nodePath, cli, version,
                            version is null ? "Automator's Playwright Test runner is ready." : $"Automator's Playwright Test {version} is ready.");
                    }
                }
                catch (Exception exception) when (exception is IOException or JsonException or ArgumentException or NotSupportedException) { }
            }
            return RunnerState.Unavailable("Automator's Playwright Test runtime is unavailable. Repair or reinstall Automator, then refresh.");
        }

        foreach (var candidate in candidates)
        {
            var cli = Path.GetFullPath(candidate.Cli);
            if (!IsContained(root, cli) || !File.Exists(cli) || !File.Exists(candidate.Package)) continue;
            string? version = null;
            try
            {
                using var package = JsonDocument.Parse(File.ReadAllText(candidate.Package));
                if (package.RootElement.TryGetProperty("version", out var versionNode) && versionNode.ValueKind == JsonValueKind.String)
                    version = versionNode.GetString();
            }
            catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException) { }
            return new RunnerState(true, nodePath, cli, version,
                version is null ? "Project Playwright Test runner found." : $"Project Playwright Test {version} ready.");
        }
        return RunnerState.Unavailable("This project has no local Playwright Test runner. Install @playwright/test in the project yourself, then refresh; Automator will not change project dependencies.");
    }

    private static IReadOnlyList<string> ReadTags(JsonElement input)
    {
        if (!input.TryGetProperty("tags", out var value) || value.ValueKind != JsonValueKind.Array || value.GetArrayLength() > MaximumTagsPerTest)
            throw new InvalidDataException("Provide at most 16 test tags.");
        var tags = new List<string>();
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String) throw new InvalidDataException("Test tags must be text labels.");
            var tag = NormalizeTag(item.GetString()!);
            if (!tags.Contains(tag, StringComparer.OrdinalIgnoreCase)) tags.Add(tag);
        }
        return Array.AsReadOnly(tags.ToArray());
    }

    private static string NormalizeTag(string tag)
    {
        var normalized = tag.Trim();
        if (!SafeTag.IsMatch(normalized) || normalized.Length > MaximumTagLength)
            throw new InvalidDataException("Tags must be 1–48 letters, numbers, spaces, dots, underscores, or dashes.");
        return normalized;
    }

    private static async Task<string> WriteTestListAsync(IEnumerable<string> entries, CancellationToken cancellationToken)
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Automator", "playwright-run-lists");
        Directory.CreateDirectory(directory);
        var file = Path.Combine(directory, $"{Guid.NewGuid():N}.txt");
        var content = string.Join(Environment.NewLine, entries) + Environment.NewLine;
        await File.WriteAllTextAsync(file, content, new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
        return file;
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static (RunSummary? Summary, IReadOnlyList<PlaywrightTestOutcome> Tests) ParseJsonReport(
        string output, IReadOnlyList<PlaywrightTestIdentity> discovered)
    {
        try
        {
            using var document = JsonDocument.Parse(output);
            var outcomes = new List<PlaywrightTestOutcome>();
            WalkSuites(document.RootElement, string.Empty, outcomes);
            if (outcomes.Count > 256) outcomes = outcomes.Take(256).ToList();
            var summary = new RunSummary(outcomes.Count(test => test.Status == "passed"),
                outcomes.Count(test => test.Status is "failed" or "timedOut"),
                outcomes.Count(test => test.Status == "skipped"), outcomes.Count(test => test.Status == "interrupted"));
            return (summary, outcomes);
        }
        catch (JsonException)
        {
            _ = discovered;
            return (null, Array.Empty<PlaywrightTestOutcome>());
        }
    }

    private static void WalkSuites(JsonElement suite, string currentFile, List<PlaywrightTestOutcome> outcomes)
    {
        if (suite.ValueKind != JsonValueKind.Object) return;
        if (suite.TryGetProperty("file", out var fileNode) && fileNode.ValueKind == JsonValueKind.String)
            currentFile = fileNode.GetString() ?? currentFile;
        if (suite.TryGetProperty("specs", out var specs) && specs.ValueKind == JsonValueKind.Array)
        {
            foreach (var spec in specs.EnumerateArray())
            {
                var title = TryString(spec, "title") ?? "Untitled test";
                var line = TryInt(spec, "line");
                if (!spec.TryGetProperty("tests", out var tests) || tests.ValueKind != JsonValueKind.Array) continue;
                foreach (var test in tests.EnumerateArray())
                {
                    var project = TryString(test, "projectName");
                    string? status = null;
                    if (test.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array && results.GetArrayLength() > 0)
                        status = TryString(results[results.GetArrayLength() - 1], "status");
                    status ??= TryString(test, "status");
                    if (status is null) continue;
                    outcomes.Add(new PlaywrightTestOutcome(title, currentFile, line, project, status));
                }
            }
        }
        if (suite.TryGetProperty("suites", out var nested) && nested.ValueKind == JsonValueKind.Array)
            foreach (var child in nested.EnumerateArray()) WalkSuites(child, currentFile, outcomes);
    }

    private static int ReadPositiveInt(string value) => int.TryParse(value, out var result) && result > 0 ? result : 0;
    private static int? ReadNullablePositiveInt(string value) => ReadPositiveInt(value) is var result && result > 0 ? result : null;

    private static string? NormalizeListedFile(string root, string file)
    {
        try
        {
            var candidate = Path.GetFullPath(Path.IsPathRooted(file) ? file : Path.Combine(root, file.Replace('/', Path.DirectorySeparatorChar)));
            if (!IsContained(root, candidate)) return null;
            var relative = Path.GetRelativePath(root, candidate).Replace('\\', '/');
            return IsValidTestPath(relative) ? relative : null;
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException) { return null; }
    }

    private static string ReadSectionPath(JsonElement input)
    {
        var value = ReadString(input, "file", 512).Trim().Replace('\\', '/');
        if (!IsValidSectionPath(value)) throw new InvalidDataException("Use a project-relative *.spec.ts or *.test.ts filename.");
        return value;
    }

    private static bool IsValidSectionPath(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 512 || value.StartsWith('/')
            || Path.IsPathRooted(value) || value.Contains(':') || value.Contains('\0')) return false;
        var parts = value.Split('/');
        if (parts.Any(part => part.Length == 0 || part is "." or ".." || part.EndsWith(' ') || part.EndsWith('.')
            || part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || part.IndexOfAny(['<', '>', ':', '"', '|', '?', '*']) >= 0)) return false;
        return value.EndsWith(".spec.ts", StringComparison.OrdinalIgnoreCase)
            || value.EndsWith(".test.ts", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValidTestPath(string value) => IsValidSectionPath(value)
        || value.EndsWith(".spec.js", StringComparison.OrdinalIgnoreCase)
        || value.EndsWith(".test.js", StringComparison.OrdinalIgnoreCase);

    private static string ResolveProjectFile(string root, string relativeFile, bool inspectExistingSegments)
    {
        var target = Path.GetFullPath(Path.Combine(root, relativeFile.Replace('/', Path.DirectorySeparatorChar)));
        if (!IsContained(root, target)) throw new InvalidDataException("The test filename must stay inside the selected project folder.");
        if (inspectExistingSegments) EnsureNoReparsePoints(root, relativeFile, includeFile: File.Exists(target));
        return target;
    }

    private static void EnsureNoReparsePoints(string root, string relativePath, bool includeFile)
    {
        var parts = relativePath.Split('/');
        var current = root;
        var limit = includeFile ? parts.Length : parts.Length - 1;
        for (var index = 0; index < limit; index++)
        {
            current = Path.Combine(current, parts[index]);
            if (!File.Exists(current) && !Directory.Exists(current)) continue;
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Automator will not create or edit test files through a symbolic link or junction.");
        }
    }

    private static bool IsContained(string root, string target)
    {
        var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedTarget = Path.GetFullPath(target);
        return normalizedTarget.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedTarget, normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static string MakeTestSource(string testName) =>
        $"test({JsonSerializer.Serialize(testName)}, async ({{ page }}) => {{\n  await page.goto('about:blank');\n}});\n";

    private static string ReadTestName(JsonElement input)
    {
        var value = ReadString(input, "testName", 256).Trim();
        if (value.Length == 0 || value.Any(char.IsControl)) throw new InvalidDataException("Enter a test name up to 256 characters.");
        return value;
    }

    private static string ReadString(JsonElement input, string name, int maximumLength)
    {
        if (input.ValueKind != JsonValueKind.Object || !input.TryGetProperty(name, out var value)
            || value.ValueKind != JsonValueKind.String)
            throw new InvalidDataException($"A {name} value is required.");
        var text = value.GetString()!;
        if (string.IsNullOrWhiteSpace(text) || text.Length > maximumLength)
            throw new InvalidDataException($"The {name} value is empty or too long.");
        return text;
    }

    private static string TestRecordId(string root, string identityId) => HashId($"tag|{NormalizeRoot(root)}|{identityId}");

    private static string MakeTestId(string root, string? project, string file, int line, int? column, IReadOnlyList<string> titlePath) =>
        HashId($"test|{NormalizeRoot(root)}|{project}|{file.ToLowerInvariant()}|{line}|{column}|{string.Join("\u001f", titlePath)}");

    private static string HashId(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant()[..40];
    private static string NormalizeRoot(string root) => Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToLowerInvariant();
    private static bool SamePath(string first, string second) => string.Equals(NormalizeRoot(first), NormalizeRoot(second), StringComparison.OrdinalIgnoreCase);
    private static string StripAnsi(string value) => Regex.Replace(value, "\\u001b\\[[0-?]*[ -/]*[@-~]", string.Empty, RegexOptions.CultureInvariant);
    private static string? TryString(JsonElement item, string name) => item.ValueKind == JsonValueKind.Object && item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static int TryInt(JsonElement item, string name) => item.ValueKind == JsonValueKind.Object && item.TryGetProperty(name, out var value) && value.TryGetInt32(out var result) ? result : 0;

    private static string BoundMessage(string error, string output)
    {
        var value = string.IsNullOrWhiteSpace(error) ? output : error;
        value = StripAnsi(value).Trim();
        return value.Length <= 768 ? value : value[..768];
    }

    private static AutomationResult Result(AutomationStatus status, string message, object data) => new(
        AutomationTabContract.CurrentVersion, status, message, JsonSerializer.SerializeToElement(data, JsonOptions), []);

    private sealed record ProjectSettings(string ProjectRoot);
    private sealed record TestTagRecord(string ProjectRoot, PlaywrightTestIdentity Identity, IReadOnlyList<string> Tags);
    private sealed record RunnerState(bool Available, string? NodePath, string? CliPath, string? Version, string Message)
    {
        public static RunnerState MissingProject => Unavailable("Choose a Playwright project folder to browse its tests.");
        public static RunnerState Unavailable(string message) => new(false, null, null, null, message);
    }
    private sealed record RunSummary(int Passed, int Failed, int Skipped, int Interrupted);
    private sealed record PlaywrightTestOutcome(string Title, string File, int Line, string? Project, string Status);
    private sealed record PlaywrightRunnerSummary(bool Available, string? Version, string Message);
    private sealed record StaleTagSummary(string File, string Title, IReadOnlyList<string> Tags);
    private sealed record PlaywrightTestFileSection(string Path, IReadOnlyList<PlaywrightTestIdentity> Tests);
    private sealed record PlaywrightTestExplorerState(string? ProjectRoot, bool IsManagedProject, IReadOnlyList<PlaywrightTestFileSection> Files,
        IReadOnlyList<string> Tags, IReadOnlyList<StaleTagSummary> StaleTags, PlaywrightRunnerSummary Runner);
    private sealed record PlaywrightTestIdentity(string Id, string? Project, string File, int Line, int? Column,
        IReadOnlyList<string> TitlePath, string ListEntry, IReadOnlyList<string> Tags);

    private sealed class BrowserLibraryFacade(IAutomationLibraryStore store) : IAutomationLibrary
    {
        private const string Module = "browser-automation";
        public Task<IReadOnlyList<AutomationLibraryRecord>> ListAsync(string collection, CancellationToken cancellationToken) =>
            store.ListAsync(Module, collection, cancellationToken);
        public Task<AutomationLibraryRecord?> GetAsync(string collection, string id, CancellationToken cancellationToken) =>
            store.GetAsync(Module, collection, id, cancellationToken);
        public Task UpsertAsync(string collection, string id, int schemaVersion, JsonElement data, CancellationToken cancellationToken) =>
            store.UpsertAsync(new AutomationLibraryRecord(Module, collection, id, schemaVersion, data, DateTimeOffset.UtcNow), cancellationToken);
        public Task<bool> DeleteAsync(string collection, string id, CancellationToken cancellationToken) =>
            store.DeleteAsync(Module, collection, id, cancellationToken);
    }
}
