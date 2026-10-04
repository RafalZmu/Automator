using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using Automator.Application.Automation;
using Automator.Application.Logging;
using Automator.Core.Automation;

namespace Automator.Infrastructure.Automation;

/// <summary>One host-owned HTTP transport. Module adapters provide distinct host policy and diagnostic identity.</summary>
public sealed class SharedHttpService : IDisposable
{
    public const int MaximumRequestBodyBytes = 1 * 1024 * 1024;
    public const int MaximumResponseBodyBytes = 4 * 1024 * 1024;
    public const int MaximumRedirects = 5;
    public static readonly TimeSpan DefaultRequestTimeout = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(10);

    private static readonly HttpRequestOptionsKey<AutomationHttpPolicy> PolicyOption = new("Automator.HttpPolicy");
    private static readonly HashSet<string> ExposedResponseHeaderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "cache-control", "content-language", "date", "etag", "expires", "last-modified", "retry-after", "x-request-id"
    };
    private static readonly HashSet<string> ForbiddenRequestHeaderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "connection", "content-length", "host", "keep-alive", "proxy-authorization", "proxy-connection", "te", "trailer", "transfer-encoding", "upgrade"
    };

    private readonly HttpClient _publicNetworkClient;
    private readonly HttpClient _localNetworkClient;
    private readonly IHostAddressResolver _resolver;
    private readonly IApplicationLog _log;
    private readonly TimeSpan _requestTimeout;

    public SharedHttpService(HttpClient publicNetworkClient, HttpClient localNetworkClient, IHostAddressResolver resolver,
        IApplicationLog log, TimeSpan? requestTimeout = null)
    {
        _publicNetworkClient = publicNetworkClient ?? throw new ArgumentNullException(nameof(publicNetworkClient));
        _localNetworkClient = localNetworkClient ?? throw new ArgumentNullException(nameof(localNetworkClient));
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _requestTimeout = requestTimeout ?? DefaultRequestTimeout;
        if (_requestTimeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(requestTimeout));
    }

    /// <summary>Creates one host-owned production transport with an isolated network-policy connection pool.</summary>
    public static HttpClient CreateProductionClient(IHostAddressResolver resolver, bool allowLocalNetwork)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false,
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.All,
            ConnectTimeout = ConnectTimeout,
            ConnectCallback = async (context, cancellationToken) =>
            {
                if (!context.InitialRequestMessage.Options.TryGetValue(PolicyOption, out var policy))
                    throw new HttpRequestException("The request has no host policy.");
                if (policy.AllowLocalNetwork != allowLocalNetwork)
                    throw new HttpRequestException("The request was routed through the wrong network-policy pool.",
                        new AutomationServiceException(AutomationServiceErrorCategory.NetworkNotAllowed, "The destination address is not allowed by the module policy."));

                var host = NormalizeHost(context.DnsEndPoint.Host);
                if (!NormalizeAllowedHosts(policy).Contains(host))
                    throw new HttpRequestException("The destination host is not allowed.",
                        new AutomationServiceException(AutomationServiceErrorCategory.HostNotAllowed, "The destination host is not allowed."));

                var addresses = await ResolveAddressesAsync(resolver, context.DnsEndPoint.Host, cancellationToken).ConfigureAwait(false);
                ValidateAddresses(addresses, policy.AllowLocalNetwork);
                Exception? lastException = null;
                foreach (var address in addresses)
                {
                    var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                    try
                    {
                        await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), cancellationToken).ConfigureAwait(false);
                        return new NetworkStream(socket, ownsSocket: true);
                    }
                    catch (Exception exception) when (exception is SocketException or OperationCanceledException)
                    {
                        socket.Dispose();
                        if (exception is OperationCanceledException) throw;
                        lastException = exception;
                    }
                }

                throw new HttpRequestException("No resolved address accepted the connection.", lastException);
            }
        };

        return new HttpClient(handler, disposeHandler: true) { Timeout = Timeout.InfiniteTimeSpan };
    }

    public IAutomationHttpClient ForModule(string moduleId, AutomationHttpPolicy policy, TimeSpan? requestTimeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);
        ArgumentNullException.ThrowIfNull(policy);
        if (requestTimeout is { } timeout && (timeout <= TimeSpan.Zero || timeout > TimeSpan.FromHours(1)))
            throw new ArgumentOutOfRangeException(nameof(requestTimeout));
        return new ScopedHttpClient(this, moduleId, policy, requestTimeout);
    }

    private async Task<AutomationHttpResult> SendAsync(string moduleId, AutomationHttpPolicy policy,
        AutomationHttpRequest request, TimeSpan? requestTimeout, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var startedAt = Stopwatch.GetTimestamp();
        var methodName = SafeMethodName(request.Method);
        string? hostName = null;
        int? statusCode = null;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(requestTimeout ?? _requestTimeout);

        try
        {
            var original = ParseAndValidateUri(request.Uri);
            hostName = NormalizeHost(original.DnsSafeHost);
            var allowedHosts = NormalizeAllowedHosts(policy);
            ValidateAllowedHost(original, allowedHosts);

            if (request.Body is not null && Encoding.UTF8.GetByteCount(request.Body) > MaximumRequestBodyBytes)
                throw Error(AutomationServiceErrorCategory.RequestTooLarge, "The request body exceeds the 1 MiB limit.");
            var bodyBytes = request.Body is null ? null : Encoding.UTF8.GetBytes(request.Body);

            HttpMethod method;
            try { method = new HttpMethod(request.Method.Trim().ToUpperInvariant()); }
            catch (Exception exception) when (exception is FormatException or ArgumentException)
            {
                throw Error(AutomationServiceErrorCategory.InvalidResponse, "The HTTP method is invalid.");
            }

            var seenUris = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var currentUri = original;
            var currentMethod = method;
            var currentBody = bodyBytes;
            var currentHeaders = request.Headers.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
            var redirects = 0;

            while (true)
            {
                timeout.Token.ThrowIfCancellationRequested();
                ValidateAllowedHost(currentUri, allowedHosts);
                await ValidateNetworkAddressAsync(currentUri, policy, timeout.Token).ConfigureAwait(false);
                var uriKey = currentUri.GetComponents(UriComponents.AbsoluteUri, UriFormat.UriEscaped);
                if (!seenUris.Add(uriKey))
                    throw Error(AutomationServiceErrorCategory.InvalidResponse, "The HTTP response contains a redirect loop.");

                using var message = CreateRequest(currentUri, currentMethod, currentHeaders, currentBody, policy);
                var client = policy.AllowLocalNetwork ? _localNetworkClient : _publicNetworkClient;
                using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
                statusCode = (int)response.StatusCode;

                if (IsRedirect(response.StatusCode) && response.Headers.Location is { } location)
                {
                    if (redirects >= MaximumRedirects)
                        throw Error(AutomationServiceErrorCategory.InvalidResponse, "The HTTP redirect limit was exceeded.");
                    if (!Uri.TryCreate(currentUri, location, out var nextUri))
                        throw Error(AutomationServiceErrorCategory.InvalidResponse, "The HTTP redirect URI is invalid.");
                    nextUri = ParseAndValidateUri(nextUri.AbsoluteUri);
                    ValidateAllowedHost(nextUri, allowedHosts);

                    if (!SameOrigin(currentUri, nextUri))
                    {
                        currentHeaders.Remove("Authorization");
                        currentHeaders.Remove("Cookie");
                        currentHeaders.Remove("Proxy-Authorization");
                    }
                    if (response.StatusCode == HttpStatusCode.SeeOther ||
                        ((response.StatusCode is HttpStatusCode.MovedPermanently or HttpStatusCode.Redirect) &&
                            string.Equals(currentMethod.Method, "POST", StringComparison.OrdinalIgnoreCase)))
                    {
                        currentMethod = HttpMethod.Get;
                        currentBody = null;
                        currentHeaders.Remove("Content-Type");
                        currentHeaders.Remove("Content-Encoding");
                    }

                    currentUri = nextUri;
                    redirects++;
                    continue;
                }

                var contentType = response.Content.Headers.ContentType;
                var contentTypeText = contentType?.ToString() ?? string.Empty;
                var responseBytes = await ReadTextBodyAsync(response.Content, contentType, timeout.Token).ConfigureAwait(false);
                var bodyText = DecodeText(responseBytes, contentType);
                var exposedHeaders = GetExposedHeaders(response);
                WriteLog(moduleId, hostName, methodName, startedAt, statusCode, null);
                return new AutomationHttpResult(statusCode.Value, exposedHeaders, contentTypeText, bodyText);
            }
        }
        catch (AutomationServiceException exception)
        {
            WriteLog(moduleId, hostName, methodName, startedAt, statusCode, exception.Category);
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            var failure = Error(AutomationServiceErrorCategory.Canceled, "The HTTP request was canceled.");
            WriteLog(moduleId, hostName, methodName, startedAt, statusCode, failure.Category);
            throw failure;
        }
        catch (OperationCanceledException)
        {
            var failure = Error(AutomationServiceErrorCategory.TimedOut, "The HTTP request timed out.");
            WriteLog(moduleId, hostName, methodName, startedAt, statusCode, failure.Category);
            throw failure;
        }
        catch (HttpRequestException exception)
        {
            var policyFailure = FindServiceException(exception);
            var failure = policyFailure ?? Error(AutomationServiceErrorCategory.TransportFailure, "The HTTP transport failed.");
            WriteLog(moduleId, hostName, methodName, startedAt, statusCode, failure.Category);
            throw failure;
        }
        catch (Exception exception) when (exception is UriFormatException or DecoderFallbackException or ArgumentException)
        {
            var failure = Error(AutomationServiceErrorCategory.InvalidResponse, "The HTTP request or response was invalid.");
            WriteLog(moduleId, hostName, methodName, startedAt, statusCode, failure.Category);
            throw failure;
        }
    }

    public void Dispose()
    {
        _publicNetworkClient.Dispose();
        if (!ReferenceEquals(_publicNetworkClient, _localNetworkClient)) _localNetworkClient.Dispose();
    }

    private async Task ValidateNetworkAddressAsync(Uri uri, AutomationHttpPolicy policy, CancellationToken cancellationToken)
    {
        IReadOnlyList<IPAddress> addresses;
        if (IPAddress.TryParse(uri.DnsSafeHost, out var literal))
            addresses = [literal];
        else
            addresses = await ResolveAddressesAsync(_resolver, uri.DnsSafeHost, cancellationToken).ConfigureAwait(false);

        ValidateAddresses(addresses, policy.AllowLocalNetwork);
    }

    private static async Task<IReadOnlyList<IPAddress>> ResolveAddressesAsync(IHostAddressResolver resolver, string host, CancellationToken cancellationToken)
    {
        try
        {
            var addresses = await resolver.ResolveAsync(host, cancellationToken).ConfigureAwait(false);
            if (addresses.Count == 0)
                throw Error(AutomationServiceErrorCategory.TransportFailure, "The destination host did not resolve to an address.");
            return addresses;
        }
        catch (AutomationServiceException) { throw; }
        catch (OperationCanceledException) { throw; }
        catch (Exception)
        {
            throw Error(AutomationServiceErrorCategory.TransportFailure, "The destination host could not be resolved.");
        }
    }

    private static void ValidateAddresses(IReadOnlyList<IPAddress> addresses, bool allowLocalNetwork)
    {
        if (addresses.Count == 0)
            throw Error(AutomationServiceErrorCategory.TransportFailure, "The destination host did not resolve to an address.");
        if (addresses.Any(IsAlwaysDisallowedAddress) || (!allowLocalNetwork && addresses.Any(IsNonPublicAddress)))
            throw Error(AutomationServiceErrorCategory.NetworkNotAllowed, "The destination address is not allowed by the module policy.");
    }

    private static HttpRequestMessage CreateRequest(Uri uri, HttpMethod method, IReadOnlyDictionary<string, string> headers,
        byte[]? body, AutomationHttpPolicy policy)
    {
        var message = new HttpRequestMessage(method, uri);
        message.Options.Set(PolicyOption, policy);
        if (body is not null) message.Content = new ByteArrayContent(body);

        try
        {
            foreach (var (name, value) in headers)
            {
                if (ForbiddenRequestHeaderNames.Contains(name))
                    throw Error(AutomationServiceErrorCategory.InvalidResponse, "A transport-controlled HTTP header is not allowed.");
                if (message.Headers.TryAddWithoutValidation(name, value)) continue;
                message.Content ??= new ByteArrayContent([]);
                if (!message.Content.Headers.TryAddWithoutValidation(name, value))
                    throw Error(AutomationServiceErrorCategory.InvalidResponse, "An HTTP header is invalid.");
            }
            return message;
        }
        catch
        {
            message.Dispose();
            throw;
        }
    }

    private static async Task<byte[]> ReadTextBodyAsync(HttpContent content, MediaTypeHeaderValue? contentType, CancellationToken cancellationToken)
    {
        var mediaType = contentType?.MediaType;
        if (!IsTextualMediaType(mediaType))
        {
            if (content.Headers.ContentLength == 0) return [];
            throw Error(AutomationServiceErrorCategory.InvalidResponse, "The HTTP response is not textual.");
        }

        await using var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var bufferStream = new MemoryStream();
        var buffer = new byte[16 * 1024];
        while (true)
        {
            var countToRead = (int)Math.Min(buffer.Length, MaximumResponseBodyBytes - bufferStream.Length + 1);
            var read = await stream.ReadAsync(buffer.AsMemory(0, countToRead), cancellationToken).ConfigureAwait(false);
            if (read == 0) break;
            if (bufferStream.Length + read > MaximumResponseBodyBytes)
                throw Error(AutomationServiceErrorCategory.ResponseTooLarge, "The HTTP response body exceeds the 4 MiB limit.");
            await bufferStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }
        return bufferStream.ToArray();
    }

    private static string DecodeText(byte[] bytes, MediaTypeHeaderValue? contentType)
    {
        try
        {
            var encoding = string.IsNullOrWhiteSpace(contentType?.CharSet)
                ? new UTF8Encoding(false, true)
                : Encoding.GetEncoding(contentType.CharSet.Trim('"'), EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
            return encoding.GetString(bytes);
        }
        catch (Exception exception) when (exception is ArgumentException or DecoderFallbackException)
        {
            throw Error(AutomationServiceErrorCategory.InvalidResponse, "The HTTP response text encoding is invalid.");
        }
    }

    private static bool IsTextualMediaType(string? mediaType)
    {
        if (string.IsNullOrWhiteSpace(mediaType)) return false;
        var normalized = mediaType.Trim().ToLowerInvariant();
        return normalized.StartsWith("text/", StringComparison.Ordinal) ||
            normalized is "application/json" or "application/xml" or "application/javascript" or "application/x-www-form-urlencoded" ||
            normalized.EndsWith("+json", StringComparison.Ordinal) || normalized.EndsWith("+xml", StringComparison.Ordinal);
    }

    private static IReadOnlyDictionary<string, string> GetExposedHeaders(HttpResponseMessage response)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in response.Headers.Concat(response.Content.Headers))
        {
            if (!ExposedResponseHeaderNames.Contains(header.Key)) continue;
            headers[header.Key] = string.Join(",", header.Value);
        }
        return headers;
    }

    private static Uri ParseAndValidateUri(string uriText)
    {
        if (!Uri.TryCreate(uriText, UriKind.Absolute, out var uri))
            throw Error(AutomationServiceErrorCategory.InvalidResponse, "The HTTP request URI is invalid.");
        return ParseAndValidateUri(uri);
    }

    private static Uri ParseAndValidateUri(Uri uri)
    {
        if (!uri.IsAbsoluteUri)
            throw Error(AutomationServiceErrorCategory.InvalidResponse, "The HTTP request URI must be absolute.");
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            throw Error(AutomationServiceErrorCategory.UnsupportedScheme, "Only HTTP and HTTPS destinations are allowed.");
        if (!string.IsNullOrEmpty(uri.UserInfo))
            throw Error(AutomationServiceErrorCategory.InvalidResponse, "URI user information is not allowed.");
        return uri;
    }

    private static void ValidateAllowedHost(Uri uri, HashSet<string> allowedHosts)
    {
        var host = NormalizeHost(uri.DnsSafeHost);
        if (!allowedHosts.Contains(host))
            throw Error(AutomationServiceErrorCategory.HostNotAllowed, "The destination host is not allowed by the module policy.");
    }

    private static HashSet<string> NormalizeAllowedHosts(AutomationHttpPolicy policy)
    {
        if (policy.AllowedHosts is null || policy.AllowedHosts.Count == 0)
            throw Error(AutomationServiceErrorCategory.HostNotAllowed, "The module has no allowed HTTP hosts.");
        var hosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var configuredHost in policy.AllowedHosts)
        {
            if (string.IsNullOrWhiteSpace(configuredHost) || configuredHost.Contains('*'))
                throw Error(AutomationServiceErrorCategory.HostNotAllowed, "The module HTTP host policy is invalid.");
            try { hosts.Add(NormalizeHost(configuredHost)); }
            catch (ArgumentException) { throw Error(AutomationServiceErrorCategory.HostNotAllowed, "The module HTTP host policy is invalid."); }
        }
        return hosts;
    }

    private static string NormalizeHost(string host)
    {
        var normalized = host.Trim().TrimEnd('.');
        if (IPAddress.TryParse(normalized, out var address)) return address.ToString().ToLowerInvariant();
        return new IdnMapping().GetAscii(normalized).ToLowerInvariant();
    }

    private static bool IsRedirect(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.MovedPermanently or HttpStatusCode.Redirect or HttpStatusCode.SeeOther or
            HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect;

    private static bool SameOrigin(Uri left, Uri right) =>
        string.Equals(left.Scheme, right.Scheme, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(NormalizeHost(left.DnsSafeHost), NormalizeHost(right.DnsSafeHost), StringComparison.OrdinalIgnoreCase) &&
        left.Port == right.Port;

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
            var first = bytes[0];
            var second = bytes[1];
            var third = bytes[2];
            return first == 0 || first == 10 || first == 127 || first >= 224 ||
                (first == 169 && second == 254) || (first == 172 && second is >= 16 and <= 31) ||
                (first == 192 && second == 168) || (first == 100 && second is >= 64 and <= 127) ||
                (first == 192 && second == 0) || (first == 192 && second == 88 && third == 99) ||
                (first == 198 && second is 18 or 19) || (first == 198 && second == 51 && third == 100) ||
                (first == 203 && second == 0 && third == 113);
        }

        if (address.AddressFamily != AddressFamily.InterNetworkV6) return true;
        var firstOctet = bytes[0];
        var secondOctet = bytes[1];
        var globalUnicast = firstOctet is >= 0x20 and <= 0x3f;
        var uniqueLocal = firstOctet is 0xfc or 0xfd;
        var documentation = firstOctet == 0x20 && secondOctet == 0x01 && bytes[2] == 0x0d && bytes[3] == 0xb8;
        return !globalUnicast || uniqueLocal || documentation;
    }

    private static AutomationServiceException? FindServiceException(HttpRequestException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
            if (current is AutomationServiceException serviceException) return serviceException;
        return null;
    }

    private void WriteLog(string moduleId, string? host, string method, long startedAt, int? statusCode,
        AutomationServiceErrorCategory? errorCategory)
    {
        var properties = new Dictionary<string, object?>
        {
            ["moduleId"] = moduleId,
            ["method"] = method,
            ["durationMs"] = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds,
        };
        if (host is not null) properties["host"] = host;
        if (statusCode.HasValue) properties["statusCode"] = statusCode.Value;
        if (errorCategory.HasValue) properties["errorCategory"] = errorCategory.Value.ToString();
        _log.Write(errorCategory.HasValue ? ApplicationLogLevel.Warning : ApplicationLogLevel.Information,
            errorCategory.HasValue ? "automation.http.failed" : "automation.http.completed",
            errorCategory.HasValue ? "An automation HTTP request failed." : "An automation HTTP request completed.",
            properties: properties);
    }

    private static string SafeMethodName(string method)
    {
        if (string.IsNullOrWhiteSpace(method)) return "invalid";
        return method.Length <= 16 && method.All(character => char.IsAsciiLetter(character) || character == '-')
            ? method.ToUpperInvariant()
            : "invalid";
    }

    private static AutomationServiceException Error(AutomationServiceErrorCategory category, string message) => new(category, message);

    private sealed class ScopedHttpClient(
        SharedHttpService service,
        string moduleId,
        AutomationHttpPolicy policy,
        TimeSpan? requestTimeout) : IAutomationHttpClient
    {
        public Task<AutomationHttpResult> SendAsync(AutomationHttpRequest request, CancellationToken cancellationToken) =>
            service.SendAsync(moduleId, policy, request, requestTimeout, cancellationToken);
    }
}
