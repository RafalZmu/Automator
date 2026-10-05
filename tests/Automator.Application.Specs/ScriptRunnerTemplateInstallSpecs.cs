using System.Text.Json;
using Automator.Application.Automation;
using Automator.Core.Automation;
using Automator.Core.Plugins;

internal static class ScriptRunnerTemplateInstallSpecs
{
    public static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "Automator-template-specs-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var interpreter = Path.Combine(root, "powershell.exe");
        await File.WriteAllTextAsync(interpreter, "test fixture");
        try
        {
            var store = new RecordingLibraryStore();
            var installer = new ScriptRunnerTemplateInstaller(Path.Combine(root, "assets"), interpreter);
            var module = new ScriptRunnerModule(templateInstaller: installer);
            var capabilities = new AutomationCapabilityRegistry(libraryStoreFactory: _ => store);
            await using var context = capabilities.CreateContext(new AutomationModuleDescriptor(module.Id,
                module.Definition.Capabilities.Where(capability => capability.Id == AutomationCapabilityIds.LibraryStorage).ToArray()));
            using var input = JsonDocument.Parse("""{"id":"firebird-3-backup-zip"}""");
            var installed = await module.ExecuteAsync("installTemplate", input.RootElement, module.CreateDefaultSettings(), context, CancellationToken.None);
            Ensure(installed.Status == AutomationStatus.Success, installed.Message);
            var profile = installed.Data.GetProperty("profile");
            var path = profile.GetProperty("scriptPath").GetString()!;
            Ensure(Path.GetDirectoryName(path) == Path.Combine(root, "assets"), "safe asset directory");
            Ensure(Path.GetFileName(path) == "firebird-3-backup-zip.v1.ps1", "stable safe filename");
            Ensure(profile.GetProperty("arguments").GetArrayLength() == 0, "no saved transient values");
            Ensure(!store.LastWrite!.Data.GetRawText().Contains("masterkey", StringComparison.OrdinalIgnoreCase), "no persisted password");
            var script = await File.ReadAllTextAsync(path);
            Ensure(!script.Contains("masterkey", StringComparison.OrdinalIgnoreCase) && !script.Contains("sysdba", StringComparison.OrdinalIgnoreCase), "no credentials in asset");
            await File.WriteAllTextAsync(path, "user modification");
            var duplicate = await module.ExecuteAsync("installTemplate", input.RootElement, module.CreateDefaultSettings(), context, CancellationToken.None);
            Ensure(duplicate.Status == AutomationStatus.Information && duplicate.Data.GetProperty("profile").GetProperty("id").GetString() == profile.GetProperty("id").GetString(), "duplicate returns saved profile");
            Ensure(await File.ReadAllTextAsync(path) == "user modification", "duplicate does not overwrite");
            await context.Library!.DeleteAsync("profiles", profile.GetProperty("id").GetString()!, CancellationToken.None);
            var collision = await module.ExecuteAsync("installTemplate", input.RootElement, module.CreateDefaultSettings(), context, CancellationToken.None);
            Ensure(collision.Status == AutomationStatus.Error && await File.ReadAllTextAsync(path) == "user modification", "orphan asset collision requires repair");
            using var unsafeInput = JsonDocument.Parse("""{"id":"../../escape"}""");
            var unsafeResult = await module.ExecuteAsync("installTemplate", unsafeInput.RootElement, module.CreateDefaultSettings(), context, CancellationToken.None);
            Ensure(unsafeResult.Status == AutomationStatus.Error, "unknown asset rejected");

            var failingStore = new FailingLibraryStore();
            var failingRoot = Path.Combine(root, "failed");
            var failingInstaller = new ScriptRunnerTemplateInstaller(failingRoot, interpreter);
            var failingRegistry = new AutomationCapabilityRegistry(libraryStoreFactory: _ => failingStore);
            await using var failingContext = failingRegistry.CreateContext(new AutomationModuleDescriptor(module.Id,
                module.Definition.Capabilities.Where(capability => capability.Id == AutomationCapabilityIds.LibraryStorage).ToArray()));
            try { await failingInstaller.InstallAsync("firebird-3-backup-zip", module.CreateDefaultSettings(), failingContext.Library!, CancellationToken.None); }
            catch (IOException) { }
            Ensure(!Directory.EnumerateFiles(failingRoot).Any(), "failed save rolls back all staged assets");
            var missing = new ScriptRunnerModule(templateInstaller: new ScriptRunnerTemplateInstaller(Path.Combine(root, "missing"), Path.Combine(root, "missing.exe")));
            var missingResult = await missing.ExecuteAsync("installTemplate", input.RootElement, module.CreateDefaultSettings(), context, CancellationToken.None);
            Ensure(missingResult.Status == AutomationStatus.Error && missingResult.Message.Contains("PowerShell"), "missing interpreter repair error");
            var noAsset = new ScriptRunnerTemplateInstaller(Path.Combine(root, "no-asset"), interpreter, _ => null);
            await RejectAsync(() => noAsset.InstallAsync("firebird-3-backup-zip", module.CreateDefaultSettings(), context.Library!, CancellationToken.None));
            Ensure(!Directory.Exists(Path.Combine(root, "no-asset")), "missing resource makes no writes");
            var partialRoot = Path.Combine(root, "partial");
            var partial = new ScriptRunnerTemplateInstaller(partialRoot, interpreter, _ => new PartialReadStream());
            await RejectAsync(() => partial.InstallAsync("firebird-3-backup-zip", module.CreateDefaultSettings(), context.Library!, CancellationToken.None));
            Ensure(!Directory.EnumerateFiles(partialRoot).Any(), "partial copy leaves no script or staging file");
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private static void Ensure(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static async Task RejectAsync(Func<Task> action)
    {
        try { await action(); }
        catch (IOException) { return; }
        throw new Exception("Expected installation failure");
    }
    private sealed class PartialReadStream : MemoryStream
    {
        public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            await destination.WriteAsync(new byte[] { 1, 2, 3 }, cancellationToken);
            throw new IOException("Fixture partial resource read");
        }
    }
    private sealed class FailingLibraryStore : IAutomationLibraryStore
    {
        public Task<IReadOnlyList<AutomationLibraryRecord>> ListAsync(string moduleId, string collection, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<AutomationLibraryRecord>>([]);
        public Task<AutomationLibraryRecord?> GetAsync(string moduleId, string collection, string id, CancellationToken cancellationToken) => Task.FromResult<AutomationLibraryRecord?>(null);
        public Task UpsertAsync(AutomationLibraryRecord record, CancellationToken cancellationToken) => throw new IOException("Fixture storage failure");
        public Task<bool> DeleteAsync(string moduleId, string collection, string id, CancellationToken cancellationToken) => Task.FromResult(false);
    }
}
