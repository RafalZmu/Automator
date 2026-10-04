using System.Net;
using System.Net.Sockets;
using System.Text;
using Automator.Infrastructure.Automation;

await BrowserAutomationProxySpecs.RunAsync();
await PlaywrightAutomationBrowserServiceSpecs.RunAsync();
Console.WriteLine("Browser automation infrastructure specs passed.");

static class BrowserAutomationProxySpecs
{
    private const string Username = "browser-session-user-01";
    private const string Password = "browser-session-secret-012345678901";
    public static async Task RunAsync()
    {
        await RejectsPrivateDnsAnswersWithoutLocalGrantAsync();
        await SendsHttpToTheVettedAddressOnlyWithLocalGrantAsync();
        await RejectsUnlistedHostBeforeConnectingAsync();
        await RequiresTheSessionCredentialAsync();
        await PinsAndAuthorizesConnectTunnelsAsync();
        await RevokesAnEstablishedTunnelWhenTheProfileGrantChangesAsync();
        await RejectsMismatchedConnectAuthorityAsync();
        await RedirectTargetsAndSubresourcesAreRecheckedAsync();
        await PreservesTheWebSocketUpgradeAfterHostValidationAsync();
    }

    private static async Task RejectsPrivateDnsAnswersWithoutLocalGrantAsync()
    {
        using var upstream = StartListener(IPAddress.Loopback);
        await using var proxy = CreateProxy([IPAddress.Loopback], allowLocalNetwork: false);
        await proxy.StartAsync(CancellationToken.None);

        var response = await SendProxyRequestAsync(proxy, "http://allowed.example/", expectTunnel: false);

        Assert(response.StartsWith("HTTP/1.1 403", StringComparison.Ordinal), "A private DNS answer must be rejected before connection.");
        Assert(!upstream.Pending(), "The proxy must not connect to a denied private address.");
    }

    private static async Task SendsHttpToTheVettedAddressOnlyWithLocalGrantAsync()
    {
        using var upstream = StartListener(IPAddress.Loopback);
        var upstreamTask = AnswerOneHttpRequestAsync(upstream);
        var upstreamPort = ((IPEndPoint)upstream.LocalEndpoint).Port;
        await using var proxy = CreateProxy([IPAddress.Loopback], allowLocalNetwork: true);
        await proxy.StartAsync(CancellationToken.None);

        var response = await SendProxyRequestAsync(proxy, $"http://allowed.example:{upstreamPort}/hello", expectTunnel: false);

        Assert(response.Contains("200 OK", StringComparison.Ordinal) && response.EndsWith("proxy-ok", StringComparison.Ordinal),
            "An explicitly granted local address must be reached through the proxy.");
        Assert((await upstreamTask).Contains("GET /hello HTTP/1.1", StringComparison.Ordinal), "The upstream must receive origin-form HTTP over the vetted socket.");
    }

    private static async Task RejectsUnlistedHostBeforeConnectingAsync()
    {
        using var upstream = StartListener(IPAddress.Loopback);
        await using var proxy = CreateProxy([IPAddress.Loopback], allowLocalNetwork: true);
        await proxy.StartAsync(CancellationToken.None);

        var response = await SendProxyRequestAsync(proxy, "http://sub.allowed.example/", expectTunnel: false);

        Assert(response.StartsWith("HTTP/1.1 403", StringComparison.Ordinal), "A subdomain must not inherit an exact-host grant.");
        Assert(!upstream.Pending(), "An unlisted host must not reach any resolved address.");
    }

    private static async Task RequiresTheSessionCredentialAsync()
    {
        await using var proxy = CreateProxy([IPAddress.Loopback], allowLocalNetwork: true);
        await proxy.StartAsync(CancellationToken.None);

        var response = await SendProxyRequestAsync(proxy, "http://allowed.example/", expectTunnel: false, includeCredential: false);

        Assert(response.StartsWith("HTTP/1.1 407", StringComparison.Ordinal), "The loopback proxy must require its per-session credential.");
    }

    private static async Task PinsAndAuthorizesConnectTunnelsAsync()
    {
        using var upstream = StartListener(IPAddress.Loopback);
        await using (var deniedProxy = CreateProxy([IPAddress.Loopback], allowLocalNetwork: false))
        {
            await deniedProxy.StartAsync(CancellationToken.None);
            var denied = await OpenConnectTunnelAsync(deniedProxy, $"allowed.example:{((IPEndPoint)upstream.LocalEndpoint).Port}");
            Assert(denied.StartsWith("HTTP/1.1 403", StringComparison.Ordinal), "CONNECT must reject private DNS results without an explicit local grant.");
            Assert(!upstream.Pending(), "A rejected HTTPS tunnel must not reach its private address.");
        }

        var upstreamPort = ((IPEndPoint)upstream.LocalEndpoint).Port;
        var echoTask = EchoOneMessageAsync(upstream);
        await using var grantedProxy = new BrowserAutomationProxy(new FixedResolver([IPAddress.Loopback]), ["allowed.example"], true,
            Username, Password);
        await grantedProxy.StartAsync(CancellationToken.None);
        var result = await OpenConnectTunnelAsync(grantedProxy, $"allowed.example:{upstreamPort}", sendPayload: true);
        Assert(result.StartsWith("HTTP/1.1 200 Connection Established", StringComparison.Ordinal), "An explicitly granted HTTPS tunnel must open to the vetted address.");
        Assert(await echoTask == "tunnel-data", "CONNECT bytes must travel only through the established vetted socket.");
    }

    private static async Task PreservesTheWebSocketUpgradeAfterHostValidationAsync()
    {
        using var upstream = StartListener(IPAddress.Loopback);
        var upstreamTask = AnswerOneUpgradeRequestAsync(upstream);
        var upstreamPort = ((IPEndPoint)upstream.LocalEndpoint).Port;
        await using var proxy = new BrowserAutomationProxy(new FixedResolver([IPAddress.Loopback]), ["allowed.example"], true,
            Username, Password);
        await proxy.StartAsync(CancellationToken.None);

        var response = await SendWebSocketUpgradeAsync(proxy, $"ws://allowed.example:{upstreamPort}/socket");
        var observed = await upstreamTask;

        Assert(response.StartsWith("HTTP/1.1 101", StringComparison.Ordinal), "A WebSocket flow to an explicitly allowed host must complete its upgrade through the proxy.");
        Assert(observed.Contains("GET /socket HTTP/1.1", StringComparison.Ordinal) && observed.Contains("Connection: Upgrade", StringComparison.OrdinalIgnoreCase),
            "The vetted upstream must receive the original WebSocket upgrade semantics.");
    }

    private static async Task RejectsMismatchedConnectAuthorityAsync()
    {
        using var upstream = StartListener(IPAddress.Loopback);
        await using var proxy = CreateProxy([IPAddress.Loopback], allowLocalNetwork: true);
        await proxy.StartAsync(CancellationToken.None);

        var response = await SendConnectAsync(proxy, "allowed.example:443", "allowed.example:444");

        Assert(response.StartsWith("HTTP/1.1 403", StringComparison.Ordinal), "CONNECT Host must match the requested host and port exactly.");
        Assert(!upstream.Pending(), "A mismatched CONNECT authority must not reach the upstream.");
    }

    private static async Task RevokesAnEstablishedTunnelWhenTheProfileGrantChangesAsync()
    {
        using var upstream = StartListener(IPAddress.Loopback);
        var upstreamRead = WaitForTunnelCloseAsync(upstream);
        await using var proxy = CreateProxy([IPAddress.Loopback], allowLocalNetwork: true);
        await proxy.StartAsync(CancellationToken.None);
        using var client = await EstablishConnectTunnelAsync(proxy, $"allowed.example:{((IPEndPoint)upstream.LocalEndpoint).Port}");
        var clientStream = client.GetStream();
        proxy.UpdatePolicy([], allowLocalNetwork: false);

        var closed = false;
        try
        {
            var result = new byte[1];
            closed = await clientStream.ReadAsync(result).AsTask().WaitAsync(TimeSpan.FromSeconds(2)) == 0;
        }
        catch (Exception exception) when (exception is IOException or SocketException or ObjectDisposedException) { closed = true; }
        Assert(closed, "A previously established HTTPS tunnel must close as soon as the profile grant is revoked.");
        Assert(await upstreamRead, "The upstream socket must observe the grant revocation as a closed stream.");
    }

    private static async Task RedirectTargetsAndSubresourcesAreRecheckedAsync()
    {
        using var upstream = StartListener(IPAddress.Loopback);
        var redirectTask = AnswerRedirectAsync(upstream);
        var port = ((IPEndPoint)upstream.LocalEndpoint).Port;
        await using var proxy = CreateProxy([IPAddress.Loopback], allowLocalNetwork: true);
        await proxy.StartAsync(CancellationToken.None);

        var redirectResponse = await SendProxyRequestAsync(proxy, $"http://allowed.example:{port}/redirect", expectTunnel: false);
        var forbiddenRedirect = await SendProxyRequestAsync(proxy, $"http://evil.example:{port}/redirect-target", expectTunnel: false);

        Assert(redirectResponse.Contains("302 Found", StringComparison.Ordinal) && await redirectTask == "GET /redirect HTTP/1.1",
            "The allowed origin must be reachable before its redirect is evaluated.");
        Assert(forbiddenRedirect.StartsWith("HTTP/1.1 403", StringComparison.Ordinal),
            "A redirect or subresource to a new unlisted host must be rejected by the live proxy boundary.");
        Assert(!upstream.Pending(), "The denied redirect target must not reach the previously allowed server.");
    }

    private static BrowserAutomationProxy CreateProxy(IPAddress[] addresses, bool allowLocalNetwork) =>
        new(new FixedResolver(addresses), ["allowed.example"], allowLocalNetwork, Username, Password);

    private static TcpListener StartListener(IPAddress address)
    {
        var listener = new TcpListener(address, 0);
        listener.Start();
        return listener;
    }

    private static async Task<string> SendProxyRequestAsync(BrowserAutomationProxy proxy, string uri, bool expectTunnel, bool includeCredential = true)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, proxy.Port);
        await using var stream = client.GetStream();
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Username}:{Password}"));
        var auth = includeCredential ? $"Proxy-Authorization: Basic {credentials}\r\n" : string.Empty;
        var authority = new Uri(uri).Authority;
        var request = $"GET {uri} HTTP/1.1\r\nHost: {authority}\r\n{auth}Connection: close\r\n\r\n";
        await stream.WriteAsync(Encoding.ASCII.GetBytes(request));
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        var builder = new StringBuilder();
        var buffer = new char[512];
        while (true)
        {
            var count = await reader.ReadAsync(buffer);
            if (count == 0) break;
            builder.Append(buffer, 0, count);
            if (expectTunnel && builder.ToString().Contains("\r\n\r\n", StringComparison.Ordinal)) break;
        }
        return builder.ToString();
    }

    private static async Task<string> OpenConnectTunnelAsync(BrowserAutomationProxy proxy, string authority, bool sendPayload = false)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, proxy.Port);
        await using var stream = client.GetStream();
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Username}:{Password}"));
        var request = $"CONNECT {authority} HTTP/1.1\r\nHost: {authority}\r\nProxy-Authorization: Basic {credentials}\r\n\r\n";
        await stream.WriteAsync(Encoding.ASCII.GetBytes(request));
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        var status = await reader.ReadLineAsync() ?? string.Empty;
        while (!string.IsNullOrEmpty(await reader.ReadLineAsync())) { }
        if (sendPayload && status.StartsWith("HTTP/1.1 200", StringComparison.Ordinal))
        {
            await stream.WriteAsync("tunnel-data"u8.ToArray());
            var bytes = new byte[11];
            await stream.ReadExactlyAsync(bytes);
        }
        return status;
    }

    private static async Task<string> SendConnectAsync(BrowserAutomationProxy proxy, string authority, string hostHeader)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, proxy.Port);
        await using var stream = client.GetStream();
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Username}:{Password}"));
        var request = $"CONNECT {authority} HTTP/1.1\r\nHost: {hostHeader}\r\nProxy-Authorization: Basic {credentials}\r\n\r\n";
        await stream.WriteAsync(Encoding.ASCII.GetBytes(request));
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        return await reader.ReadLineAsync() ?? string.Empty;
    }

    private static async Task<TcpClient> EstablishConnectTunnelAsync(BrowserAutomationProxy proxy, string authority)
    {
        var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, proxy.Port);
        var stream = client.GetStream();
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Username}:{Password}"));
        var request = $"CONNECT {authority} HTTP/1.1\r\nHost: {authority}\r\nProxy-Authorization: Basic {credentials}\r\n\r\n";
        await stream.WriteAsync(Encoding.ASCII.GetBytes(request));
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        var status = await reader.ReadLineAsync() ?? string.Empty;
        while (!string.IsNullOrEmpty(await reader.ReadLineAsync())) { }
        if (!status.StartsWith("HTTP/1.1 200", StringComparison.Ordinal))
        {
            client.Dispose();
            throw new InvalidOperationException("The test tunnel did not connect.");
        }
        return client;
    }

    private static async Task<string> SendWebSocketUpgradeAsync(BrowserAutomationProxy proxy, string uri)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, proxy.Port);
        await using var stream = client.GetStream();
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Username}:{Password}"));
        var request = $"GET {uri} HTTP/1.1\r\nHost: {new Uri(uri).Authority}\r\nConnection: Upgrade\r\nUpgrade: websocket\r\nProxy-Authorization: Basic {credentials}\r\n\r\n";
        await stream.WriteAsync(Encoding.ASCII.GetBytes(request));
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    private static async Task<string> AnswerOneHttpRequestAsync(TcpListener listener)
    {
        using var client = await listener.AcceptTcpClientAsync();
        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        var requestLine = await reader.ReadLineAsync() ?? string.Empty;
        while (!string.IsNullOrEmpty(await reader.ReadLineAsync())) { }
        var payload = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 8\r\nConnection: close\r\n\r\nproxy-ok");
        await stream.WriteAsync(payload);
        return requestLine;
    }

    private static async Task<string> AnswerOneUpgradeRequestAsync(TcpListener listener)
    {
        using var client = await listener.AcceptTcpClientAsync();
        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        var builder = new StringBuilder();
        string? line;
        while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync())) builder.AppendLine(line);
        await stream.WriteAsync("HTTP/1.1 101 Switching Protocols\r\nConnection: Upgrade\r\nUpgrade: websocket\r\n\r\n"u8.ToArray());
        return builder.ToString();
    }

    private static async Task<string> AnswerRedirectAsync(TcpListener listener)
    {
        using var client = await listener.AcceptTcpClientAsync();
        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        var requestLine = await reader.ReadLineAsync() ?? string.Empty;
        while (!string.IsNullOrEmpty(await reader.ReadLineAsync())) { }
        await stream.WriteAsync("HTTP/1.1 302 Found\r\nLocation: http://evil.example/redirect-target\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"u8.ToArray());
        return requestLine;
    }

    private static async Task<string> EchoOneMessageAsync(TcpListener listener)
    {
        using var client = await listener.AcceptTcpClientAsync();
        await using var stream = client.GetStream();
        var data = new byte[11];
        await stream.ReadExactlyAsync(data);
        await stream.WriteAsync(data);
        return Encoding.ASCII.GetString(data);
    }

    private static async Task<bool> WaitForTunnelCloseAsync(TcpListener listener)
    {
        using var client = await listener.AcceptTcpClientAsync();
        await using var stream = client.GetStream();
        var payload = new byte[1];
        return await stream.ReadAsync(payload).AsTask().WaitAsync(TimeSpan.FromSeconds(3)) == 0;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class FixedResolver(IPAddress[] addresses) : IHostAddressResolver
    {
        public Task<IReadOnlyList<IPAddress>> ResolveAsync(string host, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<IPAddress>>(addresses);
    }
}
