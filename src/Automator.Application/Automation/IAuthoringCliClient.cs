using System.Text.Json;

namespace Automator.Application.Automation;

/// <summary>A locally installed AI command-line tool that can draft a task using its own sign-in.</summary>
public interface IAuthoringCliClient
{
    bool IsAvailable { get; }
    Task<CodexTaskStatus> GetStatusAsync(CancellationToken cancellationToken);
    Task<JsonElement> GenerateAsync(string prompt, CancellationToken cancellationToken);
}
