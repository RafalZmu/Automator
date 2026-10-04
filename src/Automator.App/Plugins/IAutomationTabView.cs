using Automator.Core.Plugins;
using Microsoft.UI.Xaml;

namespace Automator.Plugins;

/// <summary>A built-in tab hosted by the WinUI shell. External plugin loading is intentionally deferred.</summary>
public interface IAutomationTabView : IAutomationTab
{
    FrameworkElement CreateView();
    Task<AutomationResult> ExecuteAsync(string actionId, object? input, CancellationToken cancellationToken = default);
}
