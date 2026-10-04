using Automator.Core.Configuration;
using Automator.Core.Launcher;

namespace Automator.Application.Launcher;

public enum LauncherMode
{
    Launcher,
    Catalog,
    Settings,
    RecordingHotkey,
    AliasEditing
}

public sealed record LauncherSnapshot(
    long Revision,
    bool Visible,
    int SelectedTab,
    string Query,
    string? PreviousForegroundHwnd,
    LauncherMode Mode,
    IReadOnlyList<AppBinding> Bindings,
    IReadOnlyList<AppBinding> CatalogApps,
    AppBinding? AliasEditCandidate);

public sealed record LauncherNavigationState(bool Visible, int SelectedTab, LauncherMode Mode);

/// <summary>Holds UI-neutral launcher state and enforces context-sensitive global-key rules.</summary>
public sealed class LauncherSession
{
    private readonly object _sync = new();
    private readonly List<AppBinding> _bindings;
    private LauncherMode _mode = LauncherMode.Launcher;
    private AppBinding? _capturedPointerApp;
    private AppBinding? _aliasEditCandidate;
    private List<AppBinding> _catalogApps = [];
    private string _query = string.Empty;
    private string? _previousForegroundHwnd;
    private int _selectedTab = 1;
    private long _revision;
    private bool _visible;

    public LauncherSession(LauncherSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _bindings = settings.Bindings.Select(Clone).ToList();
    }

    public LauncherSnapshot Snapshot
    {
        get
        {
            lock (_sync)
            {
                return CreateSnapshot();
            }
        }
    }

    public LauncherNavigationState NavigationState
    {
        get
        {
            lock (_sync) return new LauncherNavigationState(_visible, _selectedTab, _mode);
        }
    }

    /// <summary>Opens the launcher after the caller has captured the prior foreground HWND.</summary>
    public void ShowPanel(string? previousForegroundHwnd, AppBinding? capturedPointerApp = null)
    {
        lock (_sync)
        {
            _visible = true;
            _selectedTab = 1;
            _query = string.Empty;
            _mode = LauncherMode.Launcher;
            _previousForegroundHwnd = previousForegroundHwnd;
            _capturedPointerApp = capturedPointerApp is null ? null : Clone(capturedPointerApp);
            _catalogApps = [];
            _aliasEditCandidate = null;
            BumpRevision();
        }
    }

    public void HidePanel()
    {
        lock (_sync)
        {
            if (!_visible) return;
            _visible = false;
            _mode = LauncherMode.Launcher;
            _query = string.Empty;
            _catalogApps = [];
            _aliasEditCandidate = null;
            BumpRevision();
        }
    }

    public bool SelectTabFromGlobalKey(char key, bool launcherNativeForeground)
    {
        var tab = TabKeyParser.Parse(key);
        if (tab is null) return false;

        lock (_sync)
        {
            if (!_visible || !launcherNativeForeground || _mode is not (LauncherMode.Launcher or LauncherMode.Catalog)) return false;
            _selectedTab = tab.Value + 1;
            _query = string.Empty;
            _mode = LauncherMode.Launcher;
            _catalogApps = [];
            _aliasEditCandidate = null;
            BumpRevision();
            return true;
        }
    }

    public bool SelectTab(int tab)
    {
        if (tab is < 1 or > 9) return false;
        lock (_sync)
        {
            if (_selectedTab == tab && _mode == LauncherMode.Launcher) return true;
            _selectedTab = tab;
            _query = string.Empty;
            _mode = LauncherMode.Launcher;
            _catalogApps = [];
            _aliasEditCandidate = null;
            BumpRevision();
            return true;
        }
    }

    public void SetQuery(string query)
    {
        ArgumentNullException.ThrowIfNull(query);
        lock (_sync)
        {
            if (string.Equals(_query, query, StringComparison.Ordinal)) return;
            _query = query;
            BumpRevision();
        }
    }

    public bool OpenCatalog(IEnumerable<AppBinding> catalogApps)
    {
        ArgumentNullException.ThrowIfNull(catalogApps);
        lock (_sync)
        {
            if (!_visible || _mode != LauncherMode.Launcher || _selectedTab != 1) return false;

            var unique = new List<AppBinding>();
            var identities = new HashSet<string>(StringComparer.Ordinal);
            if (_capturedPointerApp is not null && identities.Add(LaunchIdentity(_capturedPointerApp)))
                unique.Add(Clone(_capturedPointerApp));

            foreach (var app in catalogApps)
            {
                if (app is not null && identities.Add(LaunchIdentity(app))) unique.Add(Clone(app));
            }

            _catalogApps = unique;
            _mode = LauncherMode.Catalog;
            _query = string.Empty;
            BumpRevision();
            return true;
        }
    }

    public bool BeginCatalogLoading()
    {
        lock (_sync)
        {
            if (!_visible || _mode != LauncherMode.Launcher || _selectedTab != 1) return false;
            _catalogApps = [];
            _mode = LauncherMode.Catalog;
            _query = string.Empty;
            BumpRevision();
            return true;
        }
    }

    public void CompleteCatalogLoading(IEnumerable<AppBinding> catalogApps)
    {
        ArgumentNullException.ThrowIfNull(catalogApps);
        lock (_sync)
        {
            if (_mode != LauncherMode.Catalog) return;
            var unique = new List<AppBinding>();
            var identities = new HashSet<string>(StringComparer.Ordinal);
            if (_capturedPointerApp is not null && identities.Add(LaunchIdentity(_capturedPointerApp)))
                unique.Add(Clone(_capturedPointerApp));
            foreach (var app in catalogApps)
            {
                if (app is not null && identities.Add(LaunchIdentity(app))) unique.Add(Clone(app));
            }
            _catalogApps = unique;
            BumpRevision();
        }
    }

    public bool AddCatalogApp(AppBinding app)
    {
        ArgumentNullException.ThrowIfNull(app);
        lock (_sync)
        {
            if (_mode != LauncherMode.Catalog) return false;
            var identity = LaunchIdentity(app);
            if (_catalogApps.Any(existing => string.Equals(LaunchIdentity(existing), identity, StringComparison.Ordinal))) return true;
            _catalogApps.Add(Clone(app));
            BumpRevision();
            return true;
        }
    }

    public void EnterMode(LauncherMode mode)
    {
        lock (_sync)
        {
            if (_mode == mode) return;
            _mode = mode;
            if (mode != LauncherMode.Catalog) _catalogApps = [];
            if (mode != LauncherMode.AliasEditing) _aliasEditCandidate = null;
            BumpRevision();
        }
    }

    public bool BeginAliasEdit(AppBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        lock (_sync)
        {
            if (!_visible) return false;
            _aliasEditCandidate = Clone(binding);
            _mode = LauncherMode.AliasEditing;
            _catalogApps = [];
            BumpRevision();
            return true;
        }
    }

    public AliasValidationError? SaveAlias(string alias)
    {
        ArgumentNullException.ThrowIfNull(alias);
        lock (_sync)
        {
            if (_mode != LauncherMode.AliasEditing || _aliasEditCandidate is null)
                throw new InvalidOperationException("There is no app binding being edited.");

            var candidate = Clone(_aliasEditCandidate);
            var existingAliases = _bindings
                .Where(binding => !string.Equals(binding.Id, candidate.Id, StringComparison.Ordinal))
                .Select(binding => binding.Alias);
            var validationError = AliasValidator.Validate(alias, existingAliases);
            if (validationError is not null) return validationError;

            candidate.Alias = alias.Trim();
            var existingIndex = _bindings.FindIndex(binding => string.Equals(binding.Id, candidate.Id, StringComparison.Ordinal));
            if (existingIndex >= 0) _bindings[existingIndex] = candidate;
            else _bindings.Add(candidate);

            _aliasEditCandidate = null;
            _mode = LauncherMode.Launcher;
            _query = string.Empty;
            BumpRevision();
            return null;
        }
    }

    public AliasMatch ResolveQuery()
    {
        lock (_sync)
        {
            if (!_visible || _mode != LauncherMode.Launcher || _selectedTab != 1)
                return new AliasMatch(AliasMatchKind.None);

            return AliasMatcher.Resolve(_query, _bindings);
        }
    }

    public void ReplaceBindings(IEnumerable<AppBinding> bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        var replacement = bindings.Select(Clone).ToList();
        lock (_sync)
        {
            _bindings.Clear();
            _bindings.AddRange(replacement);
            BumpRevision();
        }
    }

    public bool RemoveBinding(string bindingId)
    {
        ArgumentNullException.ThrowIfNull(bindingId);
        lock (_sync)
        {
            if (_bindings.RemoveAll(binding => string.Equals(binding.Id, bindingId, StringComparison.Ordinal)) == 0)
                return false;
            if (_aliasEditCandidate?.Id == bindingId)
            {
                _aliasEditCandidate = null;
                _mode = LauncherMode.Launcher;
            }
            BumpRevision();
            return true;
        }
    }

    private LauncherSnapshot CreateSnapshot() => new(
        _revision,
        _visible,
        _selectedTab,
        _query,
        _previousForegroundHwnd,
        _mode,
        _bindings.Select(Clone).ToArray(),
        _catalogApps.Select(Clone).ToArray(),
        _aliasEditCandidate is null ? null : Clone(_aliasEditCandidate));

    private void BumpRevision() => _revision = checked(_revision + 1);

    private static AppBinding Clone(AppBinding binding) => new(
        binding.Id,
        binding.Name,
        binding.TargetPath,
        binding.Alias,
        binding.Arguments);

    private static string NormalizePath(string path)
    {
        try { return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); }
        catch (ArgumentException) { return path.Trim(); }
    }

    private static string LaunchIdentity(AppBinding binding) =>
        $"{NormalizePath(binding.TargetPath).ToUpperInvariant()}\0{binding.Arguments}";
}
