using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace Automator.Infrastructure.Automation;

/// <summary>Loopback-only HTTP proxy that pins browser sockets to addresses checked against a profile grant.</summary>
public sealed class BrowserAutomationProxy : IAsyncDisposable
{
    private const int MaximumHeaderBytes = 32 * 1024;
    private static readonly byte[] HeaderTerminator = "\r\n\r\n"u8.ToArray();
    private readonly IHostAddressResolver _resolver;
    private readonly string _username;
    private readonly string _password;
    private readonly SemaphoreSlim _connectionSlots = new(64, 64);
    private readonly object _policyGate = new();
    private readonly ConcurrentDictionary<int, Task> _connections = new();
    private readonly CancellationTokenSource _shutdown = new();
    private CancellationTokenSource _policyLifetime = new();
    private NetworkPolicy _policy;
    private TcpListener? _listener;
    private Task? _acceptLoop;
    private int _connectionId;
    private int _disposed;

    public BrowserAutomationProxy(IHostAddressResolver resolver, IReadOnlyList<string> allowedHosts, bool allowLocalNetwork,
        string username, string password)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        ArgumentNullException.ThrowIfNull(allowedHosts);
        if (allowedHosts.Count == 0) throw new ArgumentException("At least one exact host is required.", nameof(allowedHosts));
        _policy = CreatePolicy(allowedHosts, allowLocalNetwork);
        _username = username ?? throw new ArgumentNullException(nameof(username));
        _password = password ?? throw new ArgumentNullException(nameof(password));
        if (_username.Length is < 16 or > 128 || _password.Length is < 32 or > 256)
            throw new ArgumentException("Proxy credentials must be high-entropy session values.");
    }

    public int Port => _listener?.LocalEndpoint is IPEndPoint endpoint
        ? endpoint.Port
        : throw new InvalidOperationException("The browser proxy has not started.");

    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        if (_listener is not null) return Task.CompletedTask;
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start(64);
        _listener = listener;
        _acceptLoop = AcceptLoopAsync(listener, _shutdown.Token);
        return Task.CompletedTask;
    }

    public string ProxyServer => $"http://127.0.0.1:{Port}";
    public string Username => _username;
    public string Password => _password;

    /// <summary>Refreshes the saved profile grant and closes existing tunnels when it changes.</summary>
    public void UpdatePolicy(IReadOnlyList<string> allowedHosts, bool allowLocalNetwork)
    {
        var policy = CreatePolicy(allowedHosts, allowLocalNetwork);
        lock (_policyGate)
        {
            if (string.Equals(_policy.Signature, policy.Signature, StringComparison.Ordinal)) return;
            _policy = policy;
            var previous = _policyLifetime;
            _policyLifetime = new CancellationTokenSource();
            previous.Cancel();
            previous.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _shutdown.Cancel();
        lock (_policyGate)
        {
            _policyLifetime.Cancel();
            _policyLifetime.Dispose();
        }
        _listener?.Stop();
        if (_acceptLoop is not null)
        {
            try { await _acceptLoop.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
        }
        var active = _connections.Values.ToArray();
        if (active.Length > 0)
        {
            try { await Task.WhenAll(active).WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false); }
            catch (Exception exception) when (exception is OperationCanceledException or TimeoutException or SocketException or IOException) { }
        }
        _shutdown.Dispose();
    }

    private async Task AcceptLoopAsync(TcpListener listener, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try { await _connectionSlots.WaitAsync(cancellationToken).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
            TcpClient client;
            try { client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false); }
            catch (OperationCanceledException) { _connectionSlots.Release(); break; }
            catch (ObjectDisposedException) { _connectionSlots.Release(); break; }
            catch (SocketException) when (cancellationToken.IsCancellationRequested) { _connectionSlots.Release(); break; }
            catch (SocketException) { _connectionSlots.Release(); continue; }
            CancellationTokenSource connectionLifetime;
            NetworkPolicy policy;
            lock (_policyGate)
            {
                policy = _policy;
                connectionLifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _policyLifetime.Token);
            }
            var id = Interlocked.Increment(ref _connectionId);
            var task = HandleClientAsync(client, policy, connectionLifetime.Token);
            _connections[id] = task;
            _ = task.ContinueWith(completed =>
            {
                _connections.TryRemove(id, out var removed);
                connectionLifetime.Dispose();
                _connectionSlots.Release();
            }, CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
    }

    private async Task HandleClientAsync(TcpClient client, NetworkPolicy policy, CancellationToken cancellationToken)
    {
        using (client)
        {
            client.NoDelay = true;
            try
            {
                var stream = client.GetStream();
                var headerBytes = await ReadHeaderAsync(stream, cancellationToken).ConfigureAwait(false);
                if (headerBytes is null) return;
                if (!TryParseRequest(headerBytes, out var request))
                {
                    await WriteErrorAsync(stream, 400, "Bad Request", cancellationToken).ConfigureAwait(false);
                    return;
                }
                if (!HasValidCredential(request.Headers))
                {
                    await WriteErrorAsync(stream, 407, "Proxy Authentication Required", cancellationToken).ConfigureAwait(false);
                    return;
                }
                if (request.Method.Equals("CONNECT", StringComparison.OrdinalIgnoreCase))
                {
                    await HandleConnectAsync(client, stream, request, policy, cancellationToken).ConfigureAwait(false);
                    return;
                }
                await HandleHttpAsync(client, stream, request, policy, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            catch (IOException) { }
            catch (SocketException) { }
        }
    }

    private async Task HandleConnectAsync(TcpClient client, NetworkStream clientStream, ProxyRequest request,
        NetworkPolicy policy, CancellationToken cancellationToken)
    {
        if (!TryParseAuthority(request.Target, out var host, out var port) || !IsAllowedHost(host, policy) ||
            !request.Headers.TryGetValue("Host", out var hostHeader) || !TryParseAuthority(hostHeader, out var headerHost, out var headerPort) ||
            !string.Equals(NormalizeHost(host), NormalizeHost(headerHost), StringComparison.OrdinalIgnoreCase) || headerPort != port)
        {
            await WriteErrorAsync(clientStream, 403, "Forbidden", cancellationToken).ConfigureAwait(false);
            return;
        }
        var address = await ResolveAndValidateAsync(host, policy, cancellationToken).ConfigureAwait(false);
        if (address is null)
        {
            await WriteErrorAsync(clientStream, 403, "Forbidden", cancellationToken).ConfigureAwait(false);
            return;
        }
        using var upstream = new TcpClient(address.AddressFamily);
        await upstream.ConnectAsync(address, port, cancellationToken).ConfigureAwait(false);
        await clientStream.WriteAsync("HTTP/1.1 200 Connection Established\r\n\r\n"u8.ToArray(), cancellationToken).ConfigureAwait(false);
        await TunnelAsync(clientStream, upstream.GetStream(), cancellationToken).ConfigureAwait(false);
    }

    private async Task HandleHttpAsync(TcpClient client, NetworkStream clientStream, ProxyRequest request,
        NetworkPolicy policy, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(request.Target, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "ws") || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment) ||
            uri.Port is < 1 or > 65535 || !IsAllowedHost(uri.DnsSafeHost, policy) || !RequestHostMatches(request.Headers, uri))
        {
            await WriteErrorAsync(clientStream, 403, "Forbidden", cancellationToken).ConfigureAwait(false);
            return;
        }
        var address = await ResolveAndValidateAsync(uri.DnsSafeHost, policy, cancellationToken).ConfigureAwait(false);
        if (address is null)
        {
            await WriteErrorAsync(clientStream, 403, "Forbidden", cancellationToken).ConfigureAwait(false);
            return;
        }

        using var upstream = new TcpClient(address.AddressFamily);
        await upstream.ConnectAsync(address, uri.Port, cancellationToken).ConfigureAwait(false);
        var upstreamStream = upstream.GetStream();
        var path = string.IsNullOrEmpty(uri.PathAndQuery) ? "/" : uri.PathAndQuery;
        var isUpgrade = request.Headers.TryGetValue("Connection", out var connectionHeader) &&
            connectionHeader.Split(',').Any(value => value.Trim().Equals("Upgrade", StringComparison.OrdinalIgnoreCase)) &&
            request.Headers.ContainsKey("Upgrade");
        var output = new StringBuilder().Append(request.Method).Append(' ').Append(path).Append(' ').Append(request.Version).Append("\r\n");
        foreach (var header in request.Headers)
        {
            if (header.Key.Equals("Proxy-Authorization", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Proxy-Connection", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Keep-Alive", StringComparison.OrdinalIgnoreCase)) continue;
            output.Append(header.Key).Append(": ").Append(header.Value).Append("\r\n");
        }
        output.Append(isUpgrade ? "Connection: Upgrade\r\n\r\n" : "Connection: close\r\n\r\n");
        await upstreamStream.WriteAsync(Encoding.ASCII.GetBytes(output.ToString()), cancellationToken).ConfigureAwait(false);
        await TunnelAsync(clientStream, upstreamStream, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IPAddress?> ResolveAndValidateAsync(string host, NetworkPolicy policy, CancellationToken cancellationToken)
    {
        IReadOnlyList<IPAddress> addresses;
        try
        {
            addresses = IPAddress.TryParse(host, out var literal)
                ? [literal]
                : await _resolver.ResolveAsync(host, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { throw; }
        catch { return null; }
        if (addresses.Count == 0 || addresses.Any(IsAlwaysDisallowedAddress) ||
            (!policy.AllowLocalNetwork && addresses.Any(IsNonPublicAddress))) return null;
        // The TCP connection below uses this exact resolver result; it performs no second DNS lookup.
        return addresses[0];
    }

    private static bool IsAllowedHost(string host, NetworkPolicy policy)
    {
        try { return policy.AllowedHosts.Contains(NormalizeHost(host)); }
        catch (ArgumentException) { return false; }
    }

    private bool HasValidCredential(IReadOnlyDictionary<string, string> headers)
    {
        if (!headers.TryGetValue("Proxy-Authorization", out var value) || !value.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase)) return false;
        try
        {
            var supplied = Convert.FromBase64String(value[6..].Trim());
            var expected = Encoding.UTF8.GetBytes($"{_username}:{_password}");
            return supplied.Length == expected.Length && CryptographicOperations.FixedTimeEquals(supplied, expected);
        }
        catch (FormatException) { return false; }
    }

    private static async Task<byte[]?> ReadHeaderAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[1];
        while (buffer.Length <= MaximumHeaderBytes)
        {
            var read = await stream.ReadAsync(chunk, cancellationToken).ConfigureAwait(false);
            if (read == 0) return buffer.Length == 0 ? null : null;
            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            var bytes = buffer.GetBuffer().AsSpan(0, checked((int)buffer.Length));
            var end = bytes.IndexOf(HeaderTerminator);
            if (end >= 0) return bytes.ToArray();
        }
        return null;
    }

    private static bool TryParseRequest(byte[] bytes, out ProxyRequest request)
    {
        request = default;
        var text = Encoding.ASCII.GetString(bytes);
        var lines = text.Split("\r\n", StringSplitOptions.None);
        var first = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (first.Length != 3 || first[0].Length > 16 || first[1].Length > 8192 || first[2] is not ("HTTP/1.0" or "HTTP/1.1")) return false;
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines.Skip(1).TakeWhile(line => line.Length > 0))
        {
            var colon = line.IndexOf(':');
            if (colon <= 0) return false;
            var key = line[..colon].Trim();
            if (key.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-')) return false;
            if (!headers.TryAdd(key, line[(colon + 1)..].Trim())) return false;
        }
        request = new ProxyRequest(first[0], first[1], first[2], headers);
        return true;
    }

    private static bool RequestHostMatches(IReadOnlyDictionary<string, string> headers, Uri uri)
    {
        if (!headers.TryGetValue("Host", out var value)) return false;
        if (!TryParseAuthority(value, out var host, out var port)) return false;
        return string.Equals(NormalizeHost(host), NormalizeHost(uri.DnsSafeHost), StringComparison.OrdinalIgnoreCase) && port == uri.Port;
    }

    private static bool TryParseAuthority(string authority, out string host, out int port)
    {
        host = string.Empty;
        port = 0;
        if (string.IsNullOrWhiteSpace(authority) || authority.Contains('@') || !Uri.TryCreate("http://" + authority, UriKind.Absolute, out var uri) ||
            !string.IsNullOrEmpty(uri.UserInfo) || uri.AbsolutePath != "/" || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)) return false;
        host = uri.DnsSafeHost;
        port = uri.IsDefaultPort ? 80 : uri.Port;
        if (authority.StartsWith("[", StringComparison.Ordinal)) port = uri.IsDefaultPort ? 80 : uri.Port;
        return host.Length > 0 && port is > 0 and <= 65535;
    }

    private static async Task TunnelAsync(Stream left, Stream right, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var leftToRight = CopyAsync(left, right, cts.Token);
        var rightToLeft = CopyAsync(right, left, cts.Token);
        await Task.WhenAny(leftToRight, rightToLeft).ConfigureAwait(false);
        cts.Cancel();
        try { await Task.WhenAll(leftToRight, rightToLeft).ConfigureAwait(false); }
        catch (Exception exception) when (exception is OperationCanceledException or IOException or SocketException or ObjectDisposedException) { }
    }

    private static async Task CopyAsync(Stream source, Stream destination, CancellationToken cancellationToken)
    {
        var buffer = new byte[32 * 1024];
        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0) return;
            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task WriteErrorAsync(Stream stream, int status, string reason, CancellationToken cancellationToken)
    {
        var challenge = status == 407 ? "Proxy-Authenticate: Basic realm=\"Automator session\"\r\n" : string.Empty;
        var response = Encoding.ASCII.GetBytes($"HTTP/1.1 {status} {reason}\r\n{challenge}Content-Length: 0\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(response, cancellationToken).ConfigureAwait(false);
    }

    private static string NormalizeHost(string host)
    {
        var normalized = host.Trim().TrimEnd('.');
        if (IPAddress.TryParse(normalized, out var address)) return address.ToString().ToLowerInvariant();
        if (normalized.Length is 0 or > 253 || normalized.Contains('*') || normalized.Any(char.IsWhiteSpace) || normalized.Contains(':'))
            throw new ArgumentException("Host is invalid.", nameof(host));
        return new IdnMapping().GetAscii(normalized).ToLowerInvariant();
    }

    private static NetworkPolicy CreatePolicy(IReadOnlyList<string> allowedHosts, bool allowLocalNetwork)
    {
        ArgumentNullException.ThrowIfNull(allowedHosts);
        var normalized = allowedHosts.Select(NormalizeHost).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var hosts = new HashSet<string>(normalized, StringComparer.OrdinalIgnoreCase);
        var signature = string.Join("\n", allowLocalNetwork ? "local" : "public", string.Join("\n", normalized.Order(StringComparer.OrdinalIgnoreCase)));
        return new NetworkPolicy(hosts, allowLocalNetwork, signature);
    }

    private static bool IsAlwaysDisallowedAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        return IPAddress.Any.Equals(address) || IPAddress.IPv6Any.Equals(address) || address.IsIPv6Multicast ||
            (address.AddressFamily == AddressFamily.InterNetwork && address.GetAddressBytes()[0] >= 224);
    }

    private static bool IsNonPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) return IsNonPublicAddress(address.MapToIPv4());
        if (IPAddress.IsLoopback(address)) return true;
        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var a = bytes[0]; var b = bytes[1]; var c = bytes[2];
            return a == 0 || a == 10 || a == 127 || a >= 224 || (a == 169 && b == 254) ||
                (a == 172 && b is >= 16 and <= 31) || (a == 192 && b == 168) ||
                (a == 100 && b is >= 64 and <= 127) || (a == 192 && b == 0) ||
                (a == 192 && b == 88 && c == 99) || (a == 198 && b is 18 or 19) ||
                (a == 198 && b == 51 && c == 100) || (a == 203 && b == 0 && c == 113);
        }
        if (address.AddressFamily != AddressFamily.InterNetworkV6) return true;
        return bytes[0] is < 0x20 or > 0x3f || bytes[0] is 0xfc or 0xfd ||
            (bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0d && bytes[3] == 0xb8);
    }

    private readonly record struct ProxyRequest(string Method, string Target, string Version, IReadOnlyDictionary<string, string> Headers);
    private sealed record NetworkPolicy(HashSet<string> AllowedHosts, bool AllowLocalNetwork, string Signature);
}
