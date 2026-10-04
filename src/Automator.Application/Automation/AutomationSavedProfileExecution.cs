using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Automator.Core.Plugins;

namespace Automator.Application.Automation;

public enum AutomationExecutionOrigin
{
    Manual,
    Workflow,
    Scheduled,
}

public sealed record AutomationExecutionMetadata(AutomationExecutionOrigin Origin, string CorrelationId);

/// <summary>Safe display metadata for a saved profile; grants, secrets, and profile data are omitted.</summary>
public sealed record AutomationSavedProfileSummary(string ProfileId, string Name);

/// <summary>Summary fields are safe for local run history; free-form messages and outputs are excluded.</summary>
public sealed record AutomationExecutionSummary(
    AutomationStatus Status,
    string Category,
    long DurationMilliseconds);

/// <summary>Raw structured output is transient and must not be persisted as history.</summary>
public sealed record AutomationProfileExecutionOutput(
    JsonElement Output,
    AutomationExecutionSummary Summary);

/// <summary>Host-registered implementation for one bundled module's saved profile format.</summary>
public interface IAutomationSavedProfileHandler
{
    string ModuleId { get; }

    Task<IReadOnlyList<AutomationSavedProfileSummary>> ListProfilesAsync(CancellationToken cancellationToken);

    Task<AutomationProfileExecutionOutput> ExecuteAsync(
        string profileId,
        JsonElement? input,
        AutomationExecutionMetadata metadata,
        CancellationToken cancellationToken);
}

/// <summary>
/// Executes only saved-profile handlers registered by the host. Renderer action IDs never enter
/// this registry, keeping cross-tab calls separate from active-module RPC dispatch.
/// </summary>
public sealed class AutomationSavedProfileExecutor
{
    public const int MaximumStructuredDataBytes = 512 * 1024;
    private static readonly Regex StableModuleId = new("^[a-z][a-z0-9.-]{0,63}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex StableProfileId = new("^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex SafeCategory = new("^[a-z][a-z0-9._-]{0,63}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private readonly IReadOnlyDictionary<string, IAutomationSavedProfileHandler> _handlers;

    public AutomationSavedProfileExecutor(IEnumerable<IAutomationSavedProfileHandler> handlers)
    {
        ArgumentNullException.ThrowIfNull(handlers);
        var entries = handlers.ToArray();
        foreach (var handler in entries)
        {
            if (handler is null || !StableModuleId.IsMatch(handler.ModuleId))
                throw new InvalidOperationException("A saved-profile handler must have a valid module id.");
        }

        if (entries.Select(handler => handler.ModuleId).Distinct(StringComparer.Ordinal).Count() != entries.Length)
            throw new InvalidOperationException("Only one saved-profile handler can be registered per module.");
        _handlers = entries.ToDictionary(handler => handler.ModuleId, StringComparer.Ordinal);
    }

    public async Task<AutomationProfileExecutionOutput> RunAsync(
        string moduleId,
        string profileId,
        JsonElement? input,
        AutomationExecutionMetadata metadata,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!StableModuleId.IsMatch(moduleId)) throw new InvalidDataException("The profile module id is invalid.");
        if (!StableProfileId.IsMatch(profileId)) throw new InvalidDataException("The saved profile id is invalid.");
        ValidateMetadata(metadata);
        ValidateInput(input);
        if (!_handlers.TryGetValue(moduleId, out var handler))
            throw new InvalidOperationException($"No saved-profile handler is registered for module '{moduleId}'.");

        var result = await handler.ExecuteAsync(profileId, input?.Clone(), metadata, cancellationToken).ConfigureAwait(false);
        ValidateResult(result);
        return result with { Output = result.Output.Clone() };
    }

    public async Task<IReadOnlyList<AutomationSavedProfileSummary>> ListProfilesAsync(
        string moduleId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!StableModuleId.IsMatch(moduleId)) throw new InvalidDataException("The profile module id is invalid.");
        if (!_handlers.TryGetValue(moduleId, out var handler))
            throw new InvalidOperationException($"No saved-profile handler is registered for module '{moduleId}'.");

        var profiles = await handler.ListProfilesAsync(cancellationToken).ConfigureAwait(false);
        if (profiles is null || profiles.Count > 1024)
            throw new InvalidOperationException("A saved-profile handler returned an invalid profile catalog.");

        var ids = new HashSet<string>(StringComparer.Ordinal);
        var safeProfiles = new List<AutomationSavedProfileSummary>(profiles.Count);
        foreach (var profile in profiles)
        {
            if (profile is null || !StableProfileId.IsMatch(profile.ProfileId)
                || string.IsNullOrWhiteSpace(profile.Name) || profile.Name.Length > 128
                || profile.Name.Any(char.IsControl) || !ids.Add(profile.ProfileId))
                throw new InvalidOperationException("A saved-profile handler returned invalid profile metadata.");
            safeProfiles.Add(new AutomationSavedProfileSummary(profile.ProfileId, profile.Name));
        }

        return Array.AsReadOnly(safeProfiles.OrderBy(profile => profile.Name, StringComparer.CurrentCultureIgnoreCase).ToArray());
    }

    private static void ValidateMetadata(AutomationExecutionMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        if (!Enum.IsDefined(metadata.Origin)
            || string.IsNullOrWhiteSpace(metadata.CorrelationId)
            || metadata.CorrelationId.Length > 128
            || metadata.CorrelationId.Any(char.IsControl))
            throw new InvalidDataException("The execution origin or correlation id is invalid.");
    }

    private static void ValidateInput(JsonElement? input)
    {
        if (input is not { } value) return;
        if (value.ValueKind == JsonValueKind.Undefined
            || Encoding.UTF8.GetByteCount(value.GetRawText()) > MaximumStructuredDataBytes)
            throw new InvalidDataException("Saved-profile input is missing or too large.");
    }

    private static void ValidateResult(AutomationProfileExecutionOutput result)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(result.Summary);
        if (result.Output.ValueKind == JsonValueKind.Undefined
            || Encoding.UTF8.GetByteCount(result.Output.GetRawText()) > MaximumStructuredDataBytes)
            throw new InvalidOperationException("A saved-profile handler returned missing or oversized structured output.");
        if (!Enum.IsDefined(result.Summary.Status)
            || !SafeCategory.IsMatch(result.Summary.Category)
            || result.Summary.DurationMilliseconds < 0)
            throw new InvalidOperationException("A saved-profile handler returned an invalid safe summary.");
    }
}
