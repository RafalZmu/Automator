using System.Text.Json;

namespace Automator.Application.Automation;

/// <summary>A JSON library record with a stable collection/id and per-record schema version.</summary>
public sealed record AutomationLibraryRecord(
    string ModuleId,
    string Collection,
    string Id,
    int SchemaVersion,
    JsonElement Data,
    DateTimeOffset UpdatedUtc);

/// <summary>Host-owned persistence boundary. Callers must use the module-bound facade in their service context.</summary>
public interface IAutomationLibraryStore
{
    Task<IReadOnlyList<AutomationLibraryRecord>> ListAsync(string moduleId, string collection, CancellationToken cancellationToken);
    Task<AutomationLibraryRecord?> GetAsync(string moduleId, string collection, string id, CancellationToken cancellationToken);
    Task UpsertAsync(AutomationLibraryRecord record, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string moduleId, string collection, string id, CancellationToken cancellationToken);
}

/// <summary>Module-scoped library access; a tab cannot name another module's data partition.</summary>
public interface IAutomationLibrary
{
    Task<IReadOnlyList<AutomationLibraryRecord>> ListAsync(string collection, CancellationToken cancellationToken);
    Task<AutomationLibraryRecord?> GetAsync(string collection, string id, CancellationToken cancellationToken);
    Task UpsertAsync(string collection, string id, int schemaVersion, JsonElement data, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string collection, string id, CancellationToken cancellationToken);
}
