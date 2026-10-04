using Automator.Application.Launcher;
using System.Text.Json.Serialization;

namespace Automator.Backend;

public sealed record MissingBindingState(string Id, string Name);
public sealed record BindingUiState(
    string Id,
    string Name,
    string TargetPath,
    string Alias,
    string Arguments,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] string? IconDataUrl);

public sealed record BackendUiState(
    int ProtocolVersion,
    string BuildId,
    long Revision,
    int TabRegistryVersion,
    IReadOnlyList<LauncherModuleState> ModuleStates,
    bool Visible,
    int SelectedTab,
    string Query,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] string? PreviousForegroundHwnd,
    string Mode,
    IReadOnlyList<LauncherTabMetadata> Tabs,
    IReadOnlyList<BindingUiState> Bindings,
    IReadOnlyList<BindingUiState> CatalogApps,
    int CatalogTotalCount,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] BindingUiState? AliasEditCandidate,
    string Hotkey,
    string Theme,
    bool StartWithWindows,
    bool SettingsValid,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] string? SettingsError,
    IReadOnlyList<MissingBindingState> MissingBindings,
    bool CatalogLoading,
    bool HookInstalled,
    bool Busy,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] string? Error,
    string MatchKind,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] string? MatchedBindingId);

public sealed record BackendInitializeResult(BackendUiState State, bool HookInstalled, int HookInstallErrorCode, string SessionId);

internal sealed record BackendNotification(string Jsonrpc, string Method, [property: JsonPropertyName("params")] object Parameters);
