using System.Net;
using System.Text.Json;
using Automator.Application.Automation;
using Automator.Application.Logging;
using Automator.Core.Automation;
using Automator.Infrastructure.Automation;

static class PlaywrightAutomationBrowserServiceSpecs
{
    public static async Task RunAsync()
    {
        var node = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "nodejs", "node.exe");
        if (!File.Exists(node)) return;
        var root = Path.Combine(Path.GetTempPath(), "automator-browser-service-specs", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var workerPath = Path.Combine(root, "worker.cjs");
        var playwrightRoot = Path.Combine(root, "playwright");
        Directory.CreateDirectory(playwrightRoot);
        await File.WriteAllTextAsync(Path.Combine(playwrightRoot, "package.json"), "{\"name\":\"playwright\",\"version\":\"1.63.0\"}");
        await File.WriteAllTextAsync(Path.Combine(playwrightRoot, "cli.js"), "process.exit(0);");
        await File.WriteAllTextAsync(workerPath, """
            const fs = require('node:fs');
            const path = require('node:path');
            if (process.argv.includes('--status')) { console.log(JSON.stringify({installed:true,ready:true})); process.exit(0); }
            fs.writeFileSync(path.join(process.env.AUTOMATOR_BROWSER_PROFILE_DIR, 'worker.pid'), String(process.pid));
            console.log(JSON.stringify({type:'ready'}));
            const readline = require('node:readline');
            readline.createInterface({input:process.stdin}).on('line', line => {
              const command = JSON.parse(line);
              if (command.action.kind === 'click') return;
              console.log(JSON.stringify({id:command.id,ok:true,result:{currentUrl:'https://allowed.example/',title:'spec-title',text:null,links:[]}}));
            });
            """);
        try
        {
            var library = new ProfileStore();
            await using var service = new PlaywrightAutomationBrowserService(node, workerPath, playwrightRoot,
                Path.Combine(root, "data"), library, new FixedResolver([IPAddress.Parse("93.184.216.34")]), NullApplicationLog.Instance);

            var status = await service.GetRuntimeStatusAsync(CancellationToken.None);
            Assert(status.Installed && status.Ready, "Runtime status must reflect the worker-reported browser availability.");

            var result = await service.ExecuteAsync("sample", new AutomationBrowserAction(AutomationBrowserActionKind.ReadTitle), CancellationToken.None);

            Assert(result.CurrentUrl == "https://allowed.example/" && result.Title == "spec-title", "The host service must use the typed worker response.");

            var pidPath = Path.Combine(root, "data", "browser-profiles", "sample", "worker.pid");
            var firstPid = int.Parse(await File.ReadAllTextAsync(pidPath));
            using (var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(300)))
            {
                try
                {
                    await service.ExecuteAsync("sample", new AutomationBrowserAction(AutomationBrowserActionKind.Click,
                        LocatorKind: AutomationBrowserLocatorKind.Role, Locator: "button"), cancellation.Token);
                    throw new InvalidOperationException("A cancelled browser action should not complete.");
                }
                catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            }
            await WaitForProcessExitAsync(firstPid);
            await service.ExecuteAsync("sample", new AutomationBrowserAction(AutomationBrowserActionKind.ReadTitle), CancellationToken.None);
            var secondPid = int.Parse(await File.ReadAllTextAsync(pidPath));
            Assert(firstPid != secondPid, "Cancellation must terminate the worker and allow a clean replacement session.");
            await service.CloseSessionAsync("sample", CancellationToken.None);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); }
            catch (IOException) { }
        }

        await ProfileGrantChangesCloseWorkerTunnelsAsync(node);
    }

    private static async Task ProfileGrantChangesCloseWorkerTunnelsAsync(string node)
    {
        var root = Path.Combine(Path.GetTempPath(), "automator-browser-policy-specs", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var upstreamPort = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        var workerPath = Path.Combine(root, "worker.cjs");
        var playwrightRoot = Path.Combine(root, "playwright");
        Directory.CreateDirectory(playwrightRoot);
        await File.WriteAllTextAsync(Path.Combine(playwrightRoot, "package.json"), "{\"name\":\"playwright\",\"version\":\"1.63.0\"}");
        await File.WriteAllTextAsync(Path.Combine(playwrightRoot, "cli.js"), "process.exit(0);");
        await File.WriteAllTextAsync(workerPath, """
            const fs = require('node:fs');
            const path = require('node:path');
            const net = require('node:net');
            if (process.argv.includes('--status')) { console.log(JSON.stringify({installed:true,ready:true})); process.exit(0); }
            const profileDir = process.env.AUTOMATOR_BROWSER_PROFILE_DIR;
            const proxy = new URL(process.env.AUTOMATOR_BROWSER_PROXY_SERVER);
            const tunnel = net.connect(Number(proxy.port), proxy.hostname, () => {
              const authorization = Buffer.from(`${process.env.AUTOMATOR_BROWSER_PROXY_USERNAME}:${process.env.AUTOMATOR_BROWSER_PROXY_PASSWORD}`).toString('base64');
              tunnel.write(`CONNECT allowed.example:__PORT__ HTTP/1.1\r\nHost: allowed.example:__PORT__\r\nProxy-Authorization: Basic ${authorization}\r\n\r\n`);
            });
            tunnel.once('data', data => { if (data.toString().startsWith('HTTP/1.1 200')) fs.writeFileSync(path.join(profileDir, 'tunnel-ready'), 'yes'); });
            tunnel.once('close', () => fs.writeFileSync(path.join(profileDir, 'tunnel-closed'), 'yes'));
            console.log(JSON.stringify({type:'ready'}));
            const readline = require('node:readline');
            readline.createInterface({input:process.stdin}).on('line', line => {
              const command = JSON.parse(line);
              console.log(JSON.stringify({id:command.id,ok:true,result:{currentUrl:'https://allowed.example/',title:'spec-title',text:null,links:[]}}));
            });
            """.Replace("__PORT__", upstreamPort.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal));
        try
        {
            var library = new ProfileStore
            {
                Profile = new BrowserAutomationProfile("sample", "Sample", $"http://allowed.example:{upstreamPort}/",
                    ["allowed.example"], true),
            };
            var upstreamAccepted = listener.AcceptTcpClientAsync();
            await using var service = new PlaywrightAutomationBrowserService(node, workerPath, playwrightRoot,
                Path.Combine(root, "data"), library, new FixedResolver([IPAddress.Loopback]), NullApplicationLog.Instance);
            await service.ExecuteAsync("sample", new AutomationBrowserAction(AutomationBrowserActionKind.ReadTitle), CancellationToken.None);
            using var upstream = await upstreamAccepted.WaitAsync(TimeSpan.FromSeconds(5));
            var profileDir = Path.Combine(root, "data", "browser-profiles", "sample");
            await WaitForFileAsync(Path.Combine(profileDir, "tunnel-ready"));

            library.Profile = library.Profile with
            {
                StartUrl = $"http://elsewhere.example:{upstreamPort}/",
                AllowedHosts = ["elsewhere.example"],
            };
            await WaitForFileAsync(Path.Combine(profileDir, "tunnel-closed"));
            var received = new byte[1];
            Assert(await upstream.GetStream().ReadAsync(received).AsTask().WaitAsync(TimeSpan.FromSeconds(2)) == 0,
                "The profile policy watcher must close an already established tunnel after a saved grant changes.");
            await service.CloseSessionAsync("sample", CancellationToken.None);
        }
        finally
        {
            listener.Stop();
            try { Directory.Delete(root, recursive: true); }
            catch (IOException) { }
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static async Task WaitForProcessExitAsync(int processId)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                using var process = System.Diagnostics.Process.GetProcessById(processId);
                if (process.HasExited) return;
            }
            catch (ArgumentException) { return; }
            await Task.Delay(25);
        }
        throw new InvalidOperationException("The cancelled browser worker process was not terminated.");
    }

    private static async Task WaitForFileAsync(string path)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (File.Exists(path)) return;
            await Task.Delay(25);
        }
        throw new InvalidOperationException($"Expected marker was not created: {Path.GetFileName(path)}");
    }

    private sealed class ProfileStore : IAutomationLibraryStore
    {
        public BrowserAutomationProfile Profile { get; set; } = new("sample", "Sample", "https://allowed.example/",
            ["allowed.example"], false);

        public Task<AutomationLibraryRecord?> GetAsync(string moduleId, string collection, string id, CancellationToken cancellationToken)
        {
            var record = new AutomationLibraryRecord(BrowserAutomationModule.IdValue, BrowserAutomationModule.ProfileCollection,
                Profile.Id, BrowserAutomationModule.SettingsVersionValue, JsonSerializer.SerializeToElement(Profile), DateTimeOffset.UtcNow);
            return Task.FromResult<AutomationLibraryRecord?>(id == Profile.Id ? record : null);
        }

        public Task<IReadOnlyList<AutomationLibraryRecord>> ListAsync(string moduleId, string collection, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task UpsertAsync(AutomationLibraryRecord record, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> DeleteAsync(string moduleId, string collection, string id, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class FixedResolver(IPAddress[] addresses) : IHostAddressResolver
    {
        public Task<IReadOnlyList<IPAddress>> ResolveAsync(string host, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<IPAddress>>(addresses);
    }
}
