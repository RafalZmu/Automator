using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Reflection;
using Automator.Application.Automation;
using Automator.Application.Logging;
using Automator.Core.Automation;
using System.Text.Json;
using Automator.Infrastructure.Automation;

if (args.Length > 0 && args[0] == "--automation-child-echo")
{
    Console.WriteLine(JsonSerializer.Serialize(args.Skip(1)));
    return 0;
}
if (args.Length > 0 && args[0] == "--automation-child-stdin")
{
    Console.Write(await Console.In.ReadToEndAsync());
    return 0;
}
if (args.Length > 0 && args[0] == "--automation-child-env")
{
    Console.Write(Environment.GetEnvironmentVariable("AUTOMATOR_WORKFLOW_INPUT_JSON"));
    return 0;
}
if (args.Length > 0 && args[0] == "--automation-child-sleep")
{
    await Task.Delay(int.Parse(args[1]));
    return 0;
}
if (args.Length > 0 && args[0] == "--automation-child-large")
{
    Console.Write(new string('x', int.Parse(args[1])));
    return 0;
}
if (args.Length > 0 && args[0] == "--automation-child-exit") return int.Parse(args[1]);

var checks = new (string Name, Func<Task> Run)[]
{
    ("SQLite automation library persists versioned records and keeps modules isolated", StoresLibraryRecords),
    ("process execution preserves argument boundaries, bounds output and cancels children", RunsBoundedProcesses),
    ("process execution writes bounded structured standard input and closes the stream", PassesStandardInput),
    ("process execution forwards bounded environment variables without shell interpolation", PassesEnvironmentVariables),
    ("HTTP request maps method headers and textual body", MapsRequestFields),
    ("HTTP non-success statuses are returned as ordinary results", ReturnsNonSuccessResult),
    ("HTTP host policy normalizes exact hostnames and rejects other hosts", EnforcesExactHostAllowlist),
    ("HTTP rejects unsafe schemes and embedded URI credentials", RejectsUnsafeUri),
    ("HTTP request body is limited before transport", RejectsOversizedRequest),
    ("HTTP response streaming stops at the configured body limit", BoundsResponseReading),
    ("HTTP maps caller cancellation and request timeout separately", MapsCancellationAndTimeout),
    ("HTTP follows no more than five redirects and rejects loops", LimitsRedirects),
    ("HTTP denies redirects to undeclared hosts", DeniesCrossHostRedirect),
    ("HTTP policy rejects IP literals and DNS answers for non-public addresses", EnforcesResolvedAddressPolicy),
    ("HTTP local-network grant still requires an allowed host", LocalGrantDoesNotBypassHostAllowlist),
    ("HTTP local-network modules cannot share a pool with public-only modules", PartitionsNetworkPolicyPools),
    ("HTTP production pools do not persist cookies between modules", DoesNotShareCookiesAcrossModules),
    ("HTTP rejects non-text response content", RejectsBinaryResponse),
    ("HTTP service logs and exposed headers redact credentials and bodies", RedactsSecrets),
    ("saved API profiles derive an exact host grant and redact injected secrets", RunsApiProfileSafely),
    ("legacy API host lists cannot authorize a cross-host redirect", ApiProfilesRejectCrossHostRedirects),
    ("saved API profiles use profile default JSON unless a run overrides it", ApiProfilesUseProfileDefaultInput),
    ("isolated tests use an in-memory secret manager rather than persistent Windows credentials", KeepsTestSecretsInMemory),
    ("library transfer exports only portable definitions and strips unknown data", AutomationLibraryTransferSpecs.ExportsOnlyPortableDefinitions),
    ("library transfer imports definitions and reports paths and secrets needing repair", AutomationLibraryTransferSpecs.ImportsWithRepairWarnings),
    ("library transfer rejects unsupported envelopes and duplicate records before writing", AutomationLibraryTransferSpecs.RejectsInvalidEnvelopesWithoutPartialWrites),
    ("library transfer rejects invalid record schemas and identity mismatches", AutomationLibraryTransferSpecs.RejectsInvalidDefinitions),
    ("library transfer enforces payload and record count limits", AutomationLibraryTransferSpecs.EnforcesBounds),
};

static async Task KeepsTestSecretsInMemory()
{
    var secrets = new InMemoryAutomationSecretManager();
    await secrets.SetAsync("weather", "api-key", "temporary-test-value", CancellationToken.None);
    Check.True(await secrets.ExistsAsync("weather", "api-key", CancellationToken.None));
    Check.Equal("temporary-test-value", await secrets.GetValueAsync("weather", "api-key", CancellationToken.None));
    await secrets.DeleteAsync("weather", "api-key", CancellationToken.None);
    Check.False(await secrets.ExistsAsync("weather", "api-key", CancellationToken.None));
}

static async Task RunsBoundedProcesses()
{
    var service = new LocalProcessExecutionService();
    var (executable, prefix) = ChildExecutable();
    var echo = await service.ExecuteAsync(new AutomationProcessRequest(executable,
        [.. prefix, "--automation-child-echo", "argument with spaces", "semi;colon"],
        Directory.GetCurrentDirectory(), TimeSpan.FromSeconds(5)), CancellationToken.None);
    Check.Equal(0, echo.ExitCode);
    using (var output = JsonDocument.Parse(echo.StandardOutput))
    {
        Check.Equal("argument with spaces", output.RootElement[0].GetString());
        Check.Equal("semi;colon", output.RootElement[1].GetString());
    }
    Check.False(echo.TimedOut);

    var nonzero = await service.ExecuteAsync(new AutomationProcessRequest(executable,
        [.. prefix, "--automation-child-exit", "7"], Directory.GetCurrentDirectory(), TimeSpan.FromSeconds(5)), CancellationToken.None);
    Check.Equal(7, nonzero.ExitCode);

    var large = await service.ExecuteAsync(new AutomationProcessRequest(executable,
        [.. prefix, "--automation-child-large", "300000"], Directory.GetCurrentDirectory(), TimeSpan.FromSeconds(5)), CancellationToken.None);
    Check.True(large.StandardOutput.Length <= AutomationProcessLimits.MaximumOutputCharacters);
    Check.True(large.StandardOutputTruncated);

    var timeout = await service.ExecuteAsync(new AutomationProcessRequest(executable,
        [.. prefix, "--automation-child-sleep", "10000"], Directory.GetCurrentDirectory(), TimeSpan.FromMilliseconds(100)), CancellationToken.None);
    Check.True(timeout.TimedOut);

    using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(75));
    await Check.ThrowsAsync<OperationCanceledException>(() => service.ExecuteAsync(new AutomationProcessRequest(executable,
        [.. prefix, "--automation-child-sleep", "30000"], Directory.GetCurrentDirectory(), TimeSpan.FromSeconds(5)), cancellation.Token));
}

static (string Executable, string[] PrefixArguments) ChildExecutable()
{
    var executable = Environment.ProcessPath ?? throw new InvalidOperationException("The test process path is unavailable.");
    var prefix = Path.GetFileNameWithoutExtension(executable).Equals("dotnet", StringComparison.OrdinalIgnoreCase)
        ? new[] { Assembly.GetEntryAssembly()!.Location }
        : [];
    return (executable, prefix);
}

static async Task PassesStandardInput()
{
    var service = new LocalProcessExecutionService();
    var (executable, prefix) = ChildExecutable();
    const string input = "{\"workflow\":{\"step\":3},\"items\":[\"a b\",\"c\"]}";
    var result = await service.ExecuteAsync(new AutomationProcessRequest(executable,
        [.. prefix, "--automation-child-stdin"], Directory.GetCurrentDirectory(), TimeSpan.FromSeconds(5), input),
        CancellationToken.None);
    Check.Equal(0, result.ExitCode);
    Check.Equal(input, result.StandardOutput);

    var tooLarge = new string('x', AutomationProcessLimits.MaximumStandardInputCharacters + 1);
    await Check.ThrowsAsync<InvalidDataException>(() => service.ExecuteAsync(new AutomationProcessRequest(executable,
        [.. prefix, "--automation-child-stdin"], Directory.GetCurrentDirectory(), TimeSpan.FromSeconds(5), tooLarge),
        CancellationToken.None));
}

static async Task PassesEnvironmentVariables()
{
    var service = new LocalProcessExecutionService();
    var (executable, prefix) = ChildExecutable();
    const string json = "{\"name\":\"A B; $x\"}";
    var result = await service.ExecuteAsync(new AutomationProcessRequest(executable,
        [.. prefix, "--automation-child-env"], Directory.GetCurrentDirectory(), TimeSpan.FromSeconds(5),
        EnvironmentVariables: new Dictionary<string, string> { ["AUTOMATOR_WORKFLOW_INPUT_JSON"] = json }), CancellationToken.None);
    Check.Equal(json, result.StandardOutput);
    await Check.ThrowsAsync<InvalidDataException>(() => service.ExecuteAsync(new AutomationProcessRequest(executable,
        [.. prefix, "--automation-child-env"], Directory.GetCurrentDirectory(), TimeSpan.FromSeconds(5),
        EnvironmentVariables: new Dictionary<string, string> { ["BAD=KEY"] = "x" }), CancellationToken.None));
}

static async Task StoresLibraryRecords()
{
    var directory = Path.Combine(Path.GetTempPath(), "AutomatorLibrarySpecs", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    var path = Path.Combine(directory, "library.db");
    try
    {
        using var document = JsonDocument.Parse("{\"scriptPath\":\"C:\\\\tools\\\\job.py\",\"timeoutSeconds\":30}");
        var record = new AutomationLibraryRecord("scripts", "profiles", "python-job", 1,
            document.RootElement.Clone(), DateTimeOffset.FromUnixTimeSeconds(1_782_000_000));
        var store = new SqliteAutomationLibraryStore(path);
        await store.UpsertAsync(record, CancellationToken.None);

        var loaded = await store.GetAsync("scripts", "profiles", "python-job", CancellationToken.None);
        Check.True(loaded is not null);
        Check.Equal(1, loaded!.SchemaVersion);
        Check.Equal("C:\\tools\\job.py", loaded.Data.GetProperty("scriptPath").GetString());
        Check.Equal(record.UpdatedUtc, loaded.UpdatedUtc);

        await store.UpsertAsync(record with { Data = JsonDocument.Parse("{\"scriptPath\":\"updated.py\"}").RootElement.Clone() }, CancellationToken.None);
        var records = await store.ListAsync("scripts", "profiles", CancellationToken.None);
        Check.Equal(1, records.Count);
        Check.Equal("updated.py", records[0].Data.GetProperty("scriptPath").GetString());
        Check.Equal(0, (await store.ListAsync("api", "profiles", CancellationToken.None)).Count);

        Check.True(await store.DeleteAsync("scripts", "profiles", "python-job", CancellationToken.None));
        Check.False(await store.DeleteAsync("scripts", "profiles", "python-job", CancellationToken.None));
        Check.Equal(0, (await store.ListAsync("scripts", "profiles", CancellationToken.None)).Count);
    }
    finally
    {
        try { Directory.Delete(directory, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}

var failures = 0;
foreach (var (name, run) in checks)
{
    try
    {
        await run();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception exception)
    {
        failures++;
        Console.WriteLine($"FAIL {name}: {exception.Message}");
    }
}

Console.WriteLine($"{checks.Length - failures}/{checks.Length} checks passed");
return failures == 0 ? 0 : 1;

static async Task MapsRequestFields()
{
    CapturedRequest? captured = null;
    var handler = new DelegateHandler(async (request, cancellationToken) =>
    {
        captured = await CapturedRequest.ReadAsync(request, cancellationToken);
        return TextResponse(HttpStatusCode.OK, "accepted");
    });
    var adapter = CreateService(handler).ForModule("form-module", Policy("api.example"));
    var result = await adapter.SendAsync(new AutomationHttpRequest(
        "https://api.example/submit", "POST",
        new Dictionary<string, string> { ["X-Test"] = "present", ["Content-Type"] = "application/json" },
        "{\"ok\":true}"), CancellationToken.None);

    Check.Equal("POST", captured!.Method);
    Check.Equal("present", captured.Headers["X-Test"]);
    Check.Equal("application/json", captured.Headers["Content-Type"]);
    Check.Equal("{\"ok\":true}", captured.Body);
    Check.Equal("accepted", result.BodyText);
}

static async Task ReturnsNonSuccessResult()
{
    var adapter = CreateService(new DelegateHandler((_, _) => Task.FromResult(TextResponse(HttpStatusCode.BadRequest, "invalid"))))
        .ForModule("status-module", Policy("api.example"));
    var result = await adapter.SendAsync(Request("https://api.example/missing"), CancellationToken.None);
    Check.Equal(400, result.StatusCode);
    Check.Equal("invalid", result.BodyText);
}

static async Task EnforcesExactHostAllowlist()
{
    var handler = new DelegateHandler((_, _) => Task.FromResult(TextResponse(HttpStatusCode.OK, "matched")));
    var service = CreateService(handler);
    var adapter = service.ForModule("host-module", Policy("EXAMPLE.test."));
    var result = await adapter.SendAsync(Request("https://Example.Test./resource"), CancellationToken.None);
    Check.Equal("matched", result.BodyText);

    var denied = await Check.ThrowsServiceAsync(() => adapter.SendAsync(Request("https://sub.example.test/resource"), CancellationToken.None));
    Check.Equal(AutomationServiceErrorCategory.HostNotAllowed, denied.Category);
    Check.Equal(1, handler.CallCount);
}

static async Task RejectsUnsafeUri()
{
    var handler = new DelegateHandler((_, _) => Task.FromResult(TextResponse(HttpStatusCode.OK, "not reached")));
    var adapter = CreateService(handler).ForModule("uri-module", Policy("api.example"));
    var unsupported = await Check.ThrowsServiceAsync(() => adapter.SendAsync(Request("file:///C:/secret"), CancellationToken.None));
    Check.Equal(AutomationServiceErrorCategory.UnsupportedScheme, unsupported.Category);
    var credentials = await Check.ThrowsServiceAsync(() => adapter.SendAsync(Request("https://user:password@api.example/data"), CancellationToken.None));
    Check.Equal(AutomationServiceErrorCategory.InvalidResponse, credentials.Category);
    Check.Equal(0, handler.CallCount);
}

static async Task RejectsOversizedRequest()
{
    var handler = new DelegateHandler((_, _) => Task.FromResult(TextResponse(HttpStatusCode.OK, "not reached")));
    var adapter = CreateService(handler).ForModule("size-module", Policy("api.example"));
    var tooLarge = new AutomationHttpRequest("https://api.example/upload", "POST", new Dictionary<string, string>(), new string('x', 1_048_577));
    var failure = await Check.ThrowsServiceAsync(() => adapter.SendAsync(tooLarge, CancellationToken.None));
    Check.Equal(AutomationServiceErrorCategory.RequestTooLarge, failure.Category);
    Check.Equal(0, handler.CallCount);
}

static async Task BoundsResponseReading()
{
    var stream = new CountingStream(new byte[4 * 1024 * 1024 + 40]);
    var handler = new DelegateHandler((_, _) =>
    {
        var content = new StreamContent(stream);
        content.Headers.TryAddWithoutValidation("Content-Type", "text/plain; charset=utf-8");
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
    });
    var adapter = CreateService(handler).ForModule("size-module", Policy("api.example"));
    var failure = await Check.ThrowsServiceAsync(() => adapter.SendAsync(Request("https://api.example/large"), CancellationToken.None));
    Check.Equal(AutomationServiceErrorCategory.ResponseTooLarge, failure.Category);
    Check.True(stream.BytesRead <= 4 * 1024 * 1024 + 1);
}

static async Task MapsCancellationAndTimeout()
{
    var cancelHandler = new DelegateHandler((_, token) => WaitForCancellation(token));
    var cancelAdapter = CreateService(cancelHandler).ForModule("cancel-module", Policy("api.example"));
    using var cancellation = new CancellationTokenSource();
    var canceledRequest = cancelAdapter.SendAsync(Request("https://api.example/wait"), cancellation.Token);
    cancellation.CancelAfter(TimeSpan.FromMilliseconds(30));
    var canceled = await Check.ThrowsServiceAsync(() => canceledRequest);
    Check.Equal(AutomationServiceErrorCategory.Canceled, canceled.Category);

    var timeoutHandler = new DelegateHandler((_, token) => WaitForCancellation(token));
    var timeoutAdapter = CreateService(timeoutHandler, requestTimeout: TimeSpan.FromSeconds(5))
        .ForModule("timeout-module", Policy("api.example"), TimeSpan.FromMilliseconds(35));
    var timedOut = await Check.ThrowsServiceAsync(() => timeoutAdapter.SendAsync(Request("https://api.example/wait"), CancellationToken.None));
    Check.Equal(AutomationServiceErrorCategory.TimedOut, timedOut.Category);
}

static async Task LimitsRedirects()
{
    var handler = new DelegateHandler((request, _) =>
    {
        var count = int.Parse(request.RequestUri!.Segments[^1].TrimEnd('/'));
        var response = count <= 6
            ? new HttpResponseMessage(HttpStatusCode.Redirect) { Headers = { Location = new Uri($"/step/{count + 1}", UriKind.Relative) } }
            : TextResponse(HttpStatusCode.OK, "complete");
        return Task.FromResult(response);
    });
    var adapter = CreateService(handler).ForModule("redirect-module", Policy("api.example"));
    var failure = await Check.ThrowsServiceAsync(() => adapter.SendAsync(Request("https://api.example/step/1"), CancellationToken.None));
    Check.Equal(AutomationServiceErrorCategory.InvalidResponse, failure.Category);
    Check.Equal(6, handler.CallCount);

    var loopHandler = new DelegateHandler((_, _) => Task.FromResult(
        new HttpResponseMessage(HttpStatusCode.Redirect) { Headers = { Location = new Uri("/loop", UriKind.Relative) } }));
    var loopAdapter = CreateService(loopHandler).ForModule("loop-module", Policy("api.example"));
    var loop = await Check.ThrowsServiceAsync(() => loopAdapter.SendAsync(Request("https://api.example/loop"), CancellationToken.None));
    Check.Equal(AutomationServiceErrorCategory.InvalidResponse, loop.Category);
    Check.Equal(1, loopHandler.CallCount);
}

static async Task DeniesCrossHostRedirect()
{
    var handler = new DelegateHandler((_, _) => Task.FromResult(
        new HttpResponseMessage(HttpStatusCode.Redirect) { Headers = { Location = new Uri("https://other.example/steal") } }));
    var adapter = CreateService(handler).ForModule("redirect-module", Policy("api.example"));
    var failure = await Check.ThrowsServiceAsync(() => adapter.SendAsync(Request("https://api.example/start"), CancellationToken.None));
    Check.Equal(AutomationServiceErrorCategory.HostNotAllowed, failure.Category);
    Check.Equal(1, handler.CallCount);
}

static async Task EnforcesResolvedAddressPolicy()
{
    var literalHandler = new DelegateHandler((_, _) => Task.FromResult(TextResponse(HttpStatusCode.OK, "not reached")));
    var literalAdapter = CreateService(literalHandler).ForModule("ip-module", Policy("127.0.0.1"));
    var literalFailure = await Check.ThrowsServiceAsync(() => literalAdapter.SendAsync(Request("http://127.0.0.1/"), CancellationToken.None));
    Check.Equal(AutomationServiceErrorCategory.NetworkNotAllowed, literalFailure.Category);
    Check.Equal(0, literalHandler.CallCount);

    var privateResolver = new FixedHostAddressResolver(_ => [IPAddress.Parse("192.168.4.10")]);
    var privateAdapter = CreateService(new DelegateHandler((_, _) => Task.FromResult(TextResponse(HttpStatusCode.OK, "not reached"))), privateResolver)
        .ForModule("dns-module", Policy("api.example"));
    var privateFailure = await Check.ThrowsServiceAsync(() => privateAdapter.SendAsync(Request("https://api.example/"), CancellationToken.None));
    Check.Equal(AutomationServiceErrorCategory.NetworkNotAllowed, privateFailure.Category);

    var linkLocalResolver = new FixedHostAddressResolver(_ => [IPAddress.Parse("169.254.3.4")]);
    var linkLocalAdapter = CreateService(new DelegateHandler((_, _) => Task.FromResult(TextResponse(HttpStatusCode.OK, "not reached"))), linkLocalResolver)
        .ForModule("dns-module", Policy("api.example"));
    var linkLocalFailure = await Check.ThrowsServiceAsync(() => linkLocalAdapter.SendAsync(Request("https://api.example/"), CancellationToken.None));
    Check.Equal(AutomationServiceErrorCategory.NetworkNotAllowed, linkLocalFailure.Category);

    foreach (var mappedAddress in new[] { "::ffff:0.0.0.0", "::ffff:224.0.0.1" })
    {
        var mappedResolver = new FixedHostAddressResolver(_ => [IPAddress.Parse(mappedAddress)]);
        var mappedHandler = new DelegateHandler((_, _) => Task.FromResult(TextResponse(HttpStatusCode.OK, "not reached")));
        var mappedAdapter = CreateService(mappedHandler, mappedResolver)
            .ForModule("local-mapped-module", new AutomationHttpPolicy(["api.example"], AllowLocalNetwork: true));
        var mappedFailure = await Check.ThrowsServiceAsync(() => mappedAdapter.SendAsync(Request("https://api.example/"), CancellationToken.None));
        Check.Equal(AutomationServiceErrorCategory.NetworkNotAllowed, mappedFailure.Category);
        Check.Equal(0, mappedHandler.CallCount);
    }

    var publicResolver = new FixedHostAddressResolver(_ => [IPAddress.Parse("93.184.216.34")]);
    var publicHandler = new DelegateHandler((_, _) => Task.FromResult(TextResponse(HttpStatusCode.OK, "public")));
    var publicAdapter = CreateService(publicHandler, publicResolver).ForModule("dns-module", Policy("api.example"));
    var publicResult = await publicAdapter.SendAsync(Request("https://api.example/"), CancellationToken.None);
    Check.Equal("public", publicResult.BodyText);
}

static async Task LocalGrantDoesNotBypassHostAllowlist()
{
    var resolver = new FixedHostAddressResolver(_ => [IPAddress.Parse("192.168.4.10")]);
    var handler = new DelegateHandler((_, _) => Task.FromResult(TextResponse(HttpStatusCode.OK, "local")));
    var service = CreateService(handler, resolver).ForModule("local-module", new AutomationHttpPolicy(["intranet.example"], AllowLocalNetwork: true));
    var hostFailure = await Check.ThrowsServiceAsync(() => service.SendAsync(Request("http://other.example/"), CancellationToken.None));
    Check.Equal(AutomationServiceErrorCategory.HostNotAllowed, hostFailure.Category);

    var localAllowed = CreateService(handler, resolver).ForModule("local-module", new AutomationHttpPolicy(["intranet.example"], AllowLocalNetwork: true));
    var localResult = await localAllowed.SendAsync(Request("http://intranet.example/"), CancellationToken.None);
    Check.Equal("local", localResult.BodyText);
}

static async Task PartitionsNetworkPolicyPools()
{
    var publicHandler = new DelegateHandler((_, _) => Task.FromResult(TextResponse(HttpStatusCode.OK, "public-pool")));
    var localHandler = new DelegateHandler((_, _) => Task.FromResult(TextResponse(HttpStatusCode.OK, "local-pool")));
    var service = new SharedHttpService(
        new HttpClient(publicHandler) { Timeout = Timeout.InfiniteTimeSpan },
        new HttpClient(localHandler) { Timeout = Timeout.InfiniteTimeSpan },
        new SequencedHostAddressResolver(IPAddress.Loopback, IPAddress.Parse("93.184.216.34")),
        new RecordingLog());
    var local = service.ForModule("local-module", new AutomationHttpPolicy(["shared.example"], AllowLocalNetwork: true));
    var publicOnly = service.ForModule("public-module", Policy("shared.example"));

    var localResult = await local.SendAsync(Request("https://shared.example/resource"), CancellationToken.None);
    var publicResult = await publicOnly.SendAsync(Request("https://shared.example/resource"), CancellationToken.None);

    Check.Equal("local-pool", localResult.BodyText);
    Check.Equal("public-pool", publicResult.BodyText);
    Check.Equal(1, localHandler.CallCount);
    Check.Equal(1, publicHandler.CallCount);
}

static async Task DoesNotShareCookiesAcrossModules()
{
    var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
    var host = "cookie.example";
    var resolver = new FixedHostAddressResolver(_ => [IPAddress.Loopback]);
    using var publicClient = SharedHttpService.CreateProductionClient(resolver, allowLocalNetwork: false);
    using var localClient = SharedHttpService.CreateProductionClient(resolver, allowLocalNetwork: true);
    using var service = new SharedHttpService(publicClient, localClient, resolver, new RecordingLog());
    var observedRequests = new List<string>();
    var server = CaptureTwoRequestsAsync(listener, observedRequests, stop.Token);
    var first = service.ForModule("first-module", new AutomationHttpPolicy([host], AllowLocalNetwork: true));
    var second = service.ForModule("second-module", new AutomationHttpPolicy([host], AllowLocalNetwork: true));

    await first.SendAsync(Request($"http://{host}:{port}/first"), stop.Token);
    await second.SendAsync(Request($"http://{host}:{port}/second"), stop.Token);
    await server.WaitAsync(TimeSpan.FromSeconds(5));
    listener.Stop();

    Check.Equal(2, observedRequests.Count);
    Check.False(observedRequests[1].Contains("Cookie: session=do-not-share", StringComparison.OrdinalIgnoreCase));
}

static async Task CaptureTwoRequestsAsync(TcpListener listener, List<string> observedRequests, CancellationToken cancellationToken)
{
    for (var index = 0; index < 2; index++)
    {
        using var socket = await listener.AcceptSocketAsync(cancellationToken);
        await using var stream = new NetworkStream(socket, ownsSocket: false);
        using var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var request = new StringBuilder();
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) is { Length: > 0 }) request.AppendLine(line);
        observedRequests.Add(request.ToString());

        var response = Encoding.ASCII.GetBytes(
            "HTTP/1.1 200 OK\r\nContent-Length: 2\r\nContent-Type: text/plain; charset=utf-8\r\nSet-Cookie: session=do-not-share; Path=/\r\nConnection: close\r\n\r\nok");
        await stream.WriteAsync(response, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }
}

static async Task RejectsBinaryResponse()
{
    var handler = new DelegateHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new ByteArrayContent([0, 255, 1]),
    }));
    var adapter = CreateService(handler).ForModule("binary-module", Policy("api.example"));
    var failure = await Check.ThrowsServiceAsync(() => adapter.SendAsync(Request("https://api.example/image"), CancellationToken.None));
    Check.Equal(AutomationServiceErrorCategory.InvalidResponse, failure.Category);
}

static async Task RedactsSecrets()
{
    var log = new RecordingLog();
    var handler = new DelegateHandler((_, _) =>
    {
        var response = TextResponse(HttpStatusCode.OK, "response-private-body");
        response.Headers.TryAddWithoutValidation("Set-Cookie", "session=secret-cookie");
        return Task.FromResult(response);
    });
    var adapter = CreateService(handler, log: log).ForModule("secret-module", Policy("api.example"));
    var request = new AutomationHttpRequest("https://api.example/?token=query-secret", "POST",
        new Dictionary<string, string> { ["Authorization"] = "Bearer header-secret", ["Cookie"] = "session=request-secret" },
        "request-private-body");
    var result = await adapter.SendAsync(request, CancellationToken.None);
    var logged = string.Join(" ", log.Entries.Select(entry => entry.Message + " " + string.Join(" ", entry.Properties.Values)));

    Check.False(logged.Contains("header-secret", StringComparison.Ordinal));
    Check.False(logged.Contains("request-secret", StringComparison.Ordinal));
    Check.False(logged.Contains("query-secret", StringComparison.Ordinal));
    Check.False(logged.Contains("request-private-body", StringComparison.Ordinal));
    Check.False(logged.Contains("response-private-body", StringComparison.Ordinal));
    Check.False(result.Headers.Keys.Any(name => name.Equals("set-cookie", StringComparison.OrdinalIgnoreCase)));
}

static async Task RunsApiProfileSafely()
{
    var directory = Path.Combine(Path.GetTempPath(), "AutomatorApiSpecs", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    var log = new RecordingLog();
    var secrets = new FixtureSecretValueReader("api-secret-value");
    CapturedRequest? captured = null;
    var handler = new DelegateHandler(async (request, cancellationToken) =>
    {
        captured = await CapturedRequest.ReadAsync(request, cancellationToken);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"echo\":\"api-secret-value\",\"input\":\"private-request\"}", Encoding.UTF8, "application/json")
        };
        response.Headers.TryAddWithoutValidation("X-Request-Id", "api-secret-value");
        return response;
    });
    using var service = CreateService(handler, log: log);
    var library = new SqliteAutomationLibraryStore(Path.Combine(directory, "library.db"));
    try
    {
        var profile = new ApiProfile("api-profile", "Test API", "POST", "https://api.example/v1", false,
            new Dictionary<string, string> { ["X-Test-Value"] = "ordinary" },
            new Dictionary<string, string> { ["Authorization"] = "token-ref" },
            "{\"payload\":{{input}}}", ApiResponseMode.Json, 3);
        var serializerOptions = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web);
        serializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
        await library.UpsertAsync(new AutomationLibraryRecord(ApiModule.IdValue, ApiModule.ProfileCollection,
            profile.Id, ApiModule.SettingsVersionValue, JsonSerializer.SerializeToElement(profile, serializerOptions), DateTimeOffset.UtcNow), CancellationToken.None);
        var runner = new AutomationApiProfileRunner(library, secrets, service, log);
        using var input = JsonDocument.Parse("{\"message\":\"private-request\"}");
        var response = await runner.RunProfileAsync(profile.Id, input.RootElement, CancellationToken.None);

        Check.Equal("api-secret-value", captured!.Headers["Authorization"]);
        Check.Equal("ordinary", captured.Headers["X-Test-Value"]);
        Check.Equal("{\"payload\":{\"message\":\"private-request\"}}", captured.Body);
        Check.True(response.BodyText.Contains("[redacted]", StringComparison.Ordinal));
        Check.False(response.BodyText.Contains("api-secret-value", StringComparison.Ordinal));
        Check.True(response.BodyText.Contains("private-request", StringComparison.Ordinal));
        Check.Equal("[redacted]", response.Headers["X-Request-Id"]);
        var logged = string.Join(" ", log.Entries.Select(entry => entry.Message + " " + string.Join(" ", entry.Properties.Values)));
        Check.False(logged.Contains("api-secret-value", StringComparison.Ordinal));
        Check.False(logged.Contains("private-request", StringComparison.Ordinal));

    }
    finally
    {
        try { Directory.Delete(directory, recursive: true); } catch { }
    }
}

static async Task ApiProfilesRejectCrossHostRedirects()
{
    var directory = Path.Combine(Path.GetTempPath(), "AutomatorApiSpecs", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    var log = new RecordingLog();
    var handler = new DelegateHandler((request, _) => Task.FromResult(request.RequestUri!.Host == "api.example"
        ? new HttpResponseMessage(HttpStatusCode.Redirect) { Headers = { Location = new Uri("https://other.example/redirected") } }
        : TextResponse(HttpStatusCode.OK, "unexpected")));
    using var service = CreateService(handler, log: log);
    var library = new SqliteAutomationLibraryStore(Path.Combine(directory, "library.db"));
    try
    {
        using var legacyProfile = JsonDocument.Parse("""
            {"id":"api-legacy","name":"Legacy","method":"GET","url":"https://api.example/v1","allowedHosts":["api.example","other.example"],"allowLocalNetwork":false,"headers":{},"secretHeaders":{},"bodyTemplate":null,"responseMode":"json","timeoutSeconds":3}
            """);
        await library.UpsertAsync(new AutomationLibraryRecord(ApiModule.IdValue, ApiModule.ProfileCollection,
            "api-legacy", ApiModule.SettingsVersionValue, legacyProfile.RootElement.Clone(), DateTimeOffset.UtcNow), CancellationToken.None);
        var runner = new AutomationApiProfileRunner(library, new FixtureSecretValueReader("unused"), service, log);
        var exception = await Check.ThrowsServiceAsync(() => runner.RunProfileAsync("api-legacy", null, CancellationToken.None));
        Check.Equal(AutomationServiceErrorCategory.HostNotAllowed, exception.Category);
        Check.Equal(1, handler.CallCount);
    }
    finally
    {
        try { Directory.Delete(directory, recursive: true); } catch { }
    }
}

static async Task ApiProfilesUseProfileDefaultInput()
{
    var directory = Path.Combine(Path.GetTempPath(), "AutomatorApiSpecs", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    CapturedRequest? captured = null;
    var handler = new DelegateHandler(async (request, cancellationToken) =>
    {
        captured = await CapturedRequest.ReadAsync(request, cancellationToken);
        return TextResponse(HttpStatusCode.OK, "ok");
    });
    using var service = CreateService(handler);
    var library = new SqliteAutomationLibraryStore(Path.Combine(directory, "library.db"));
    try
    {
        using var profile = JsonDocument.Parse("""
            {"id":"api-default","name":"Default","method":"POST","url":"https://api.example/v1","allowedHosts":["api.example"],"allowLocalNetwork":false,"headers":{},"secretHeaders":{},"bodyTemplate":null,"defaultInput":{"query":"saved"},"responseMode":"json","timeoutSeconds":3}
            """);
        await library.UpsertAsync(new AutomationLibraryRecord(ApiModule.IdValue, ApiModule.ProfileCollection,
            "api-default", ApiModule.SettingsVersionValue, profile.RootElement.Clone(), DateTimeOffset.UtcNow), CancellationToken.None);
        var runner = new AutomationApiProfileRunner(library, new FixtureSecretValueReader("unused"), service, new RecordingLog());
        await runner.RunProfileAsync("api-default", null, CancellationToken.None);
        Check.Equal("{\"query\":\"saved\"}", captured!.Body);
        using var overrideInput = JsonDocument.Parse("{\"query\":\"one-off\"}");
        await runner.RunProfileAsync("api-default", overrideInput.RootElement, CancellationToken.None);
        Check.Equal("{\"query\":\"one-off\"}", captured!.Body);
    }
    finally
    {
        try { Directory.Delete(directory, recursive: true); } catch { }
    }
}

static SharedHttpService CreateService(DelegateHandler handler, IHostAddressResolver? resolver = null,
    RecordingLog? log = null, TimeSpan? requestTimeout = null) =>
    new(new HttpClient(handler, disposeHandler: false) { Timeout = Timeout.InfiniteTimeSpan },
        new HttpClient(handler, disposeHandler: false) { Timeout = Timeout.InfiniteTimeSpan },
        resolver ?? new FixedHostAddressResolver(_ => [IPAddress.Parse("93.184.216.34")]),
        log ?? new RecordingLog(), requestTimeout);

static AutomationHttpPolicy Policy(params string[] hosts) => new(hosts);

static AutomationHttpRequest Request(string uri) => new(uri, "GET", new Dictionary<string, string>(), null);

static HttpResponseMessage TextResponse(HttpStatusCode statusCode, string body)
{
    var response = new HttpResponseMessage(statusCode) { Content = new StringContent(body, Encoding.UTF8, "text/plain") };
    response.Headers.TryAddWithoutValidation("X-Request-Id", "req-1");
    return response;
}

static Task<HttpResponseMessage> WaitForCancellation(CancellationToken cancellationToken) =>
    Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ContinueWith<HttpResponseMessage>(
        static _ => throw new OperationCanceledException(), cancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

sealed class DelegateHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
{
    public int CallCount { get; private set; }
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        return send(request, cancellationToken);
    }
}

sealed class FixedHostAddressResolver(Func<string, IReadOnlyList<IPAddress>> resolve) : IHostAddressResolver
{
    public Task<IReadOnlyList<IPAddress>> ResolveAsync(string host, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(resolve(host));
    }
}

sealed class FixtureSecretValueReader(string value) : IAutomationSecretValueReader
{
    public Task<string?> GetValueAsync(string profileId, string secretId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<string?>(value);
    }
}

sealed class SequencedHostAddressResolver(params IPAddress[] addresses) : IHostAddressResolver
{
    private int _next;
    public Task<IReadOnlyList<IPAddress>> ResolveAsync(string host, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var index = Interlocked.Increment(ref _next) - 1;
        return Task.FromResult<IReadOnlyList<IPAddress>>([addresses[Math.Min(index, addresses.Length - 1)]]);
    }
}

sealed class RecordingLog : IApplicationLog
{
    public List<(string Message, IReadOnlyDictionary<string, object?> Properties)> Entries { get; } = [];
    public void Write(ApplicationLogLevel level, string eventName, string message, Exception? exception = null,
        IReadOnlyDictionary<string, object?>? properties = null) =>
        Entries.Add((message, properties ?? new Dictionary<string, object?>()));
}

sealed class CapturedRequest(string method, IReadOnlyDictionary<string, string> headers, string? body)
{
    public string Method { get; } = method;
    public IReadOnlyDictionary<string, string> Headers { get; } = headers;
    public string? Body { get; } = body;
    public static async Task<CapturedRequest> ReadAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in request.Headers) headers[header.Key] = string.Join(",", header.Value);
        if (request.Content is not null)
            foreach (var header in request.Content.Headers) headers[header.Key] = string.Join(",", header.Value);
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        return new CapturedRequest(request.Method.Method, headers, body);
    }
}

sealed class CountingStream(byte[] bytes) : MemoryStream(bytes)
{
    public long BytesRead { get; private set; }
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var read = base.ReadAsync(buffer, cancellationToken);
        BytesRead += read.Result;
        return read;
    }
}

static class Check
{
    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
    }
    public static void True(bool actual)
    {
        if (!actual) throw new InvalidOperationException("Expected true.");
    }
    public static void False(bool actual)
    {
        if (actual) throw new InvalidOperationException("Expected false.");
    }
    public static async Task<AutomationServiceException> ThrowsServiceAsync(Func<Task> action)
    {
        try { await action(); }
        catch (AutomationServiceException exception) { return exception; }
        throw new InvalidOperationException("Expected AutomationServiceException.");
    }
    public static async Task ThrowsAsync<TException>(Func<Task> action) where TException : Exception
    {
        try { await action(); }
        catch (TException) { return; }
        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }
}
