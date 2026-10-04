using System.Text.Json;
using Automator.Services;
using Automator.Core.Launcher;
using Automator.Core.Plugins;
using Microsoft.UI.Xaml;

namespace Automator.Plugins;

public sealed class LauncherTabModule(FrameworkElement launcherView, WindowsLauncherService launcher) : IAutomationTabView
{
    public int ContractVersion => AutomationTabContract.CurrentVersion;
    public string Id => "launcher";
    public string Title => "App launcher";

    public FrameworkElement CreateView() => launcherView;

    public async Task<AutomationResult> ExecuteAsync(string actionId, object? input, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(actionId, "launch", StringComparison.Ordinal) || input is not AppBinding binding)
        {
            return new AutomationResult(AutomationTabContract.CurrentVersion, AutomationStatus.Error, "The launcher action is invalid.",
                JsonSerializer.SerializeToElement(new Dictionary<string, string>()), Array.Empty<AutomationAction>());
        }

        try
        {
            var action = await launcher.ActivateAsync(binding, cancellationToken);
            return new AutomationResult(AutomationTabContract.CurrentVersion, AutomationStatus.Success, $"{binding.Name}: {action}.",
                JsonSerializer.SerializeToElement(new Dictionary<string, string> { ["appId"] = binding.Id, ["action"] = action.ToString() }),
                Array.Empty<AutomationAction>());
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception or System.IO.IOException)
        {
            AppLogger.Error("Launcher.ActionFailed", "The launcher action could not activate the bound application.", exception,
                new Dictionary<string, object?>
                {
                    ["appName"] = binding.Name,
                    ["targetPath"] = binding.TargetPath
                });
            return new AutomationResult(AutomationTabContract.CurrentVersion, AutomationStatus.Error, $"Could not open {binding.Name}: {exception.Message}",
                JsonSerializer.SerializeToElement(new Dictionary<string, string> { ["appId"] = binding.Id }),
                Array.Empty<AutomationAction>());
        }
    }
}

public sealed class AutomationTabRegistry
{
    private readonly Dictionary<int, IAutomationTabView> _tabs = [];

    public void Register(int slot, IAutomationTabView tab)
    {
        if (slot is < 1 or > 9) throw new ArgumentOutOfRangeException(nameof(slot));
        if (tab.ContractVersion != AutomationTabContract.CurrentVersion)
            throw new InvalidOperationException($"Tab '{tab.Id}' uses unsupported contract version {tab.ContractVersion}.");
        _tabs[slot] = tab;
    }

    public IAutomationTabView? Get(int slot) => _tabs.GetValueOrDefault(slot);
}
