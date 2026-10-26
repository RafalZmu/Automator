using System.Text.Json;
using Automator.Core.Automation;
using Automator.Core.Plugins;

namespace Automator.Application.Automation;

/// <summary>Scheduler adapter that exposes Browser Automation's Playwright tags and individual tests as run targets.</summary>
public sealed class PlaywrightTestsSavedProfileHandler(IAutomationLibraryStore library, IAutomationProcessService processes)
    : IAutomationSavedProfileHandler
{
    public const string Module = "playwright-tests";
    public string ModuleId => Module;

    public Task<IReadOnlyList<AutomationSavedProfileSummary>> ListProfilesAsync(CancellationToken cancellationToken) =>
        new PlaywrightTestExplorer(library, processes).ListScheduleTargetsAsync(cancellationToken);

    public async Task<AutomationProfileExecutionOutput> ExecuteAsync(string profileId, JsonElement? input,
        AutomationExecutionMetadata metadata, CancellationToken cancellationToken)
    {
        var started = System.Diagnostics.Stopwatch.StartNew();
        var result = await new PlaywrightTestExplorer(library, processes).RunScheduleTargetAsync(profileId, cancellationToken).ConfigureAwait(false);
        return new AutomationProfileExecutionOutput(result.Data,
            new AutomationExecutionSummary(result.Status, result.Status == AutomationStatus.Success ? "playwright.completed" : "playwright.failed",
                (long)started.Elapsed.TotalMilliseconds));
    }
}
