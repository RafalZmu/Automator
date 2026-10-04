using System.Net;
using System.Net.Sockets;

namespace Automator.Infrastructure.Automation;

public interface IHostAddressResolver
{
    Task<IReadOnlyList<IPAddress>> ResolveAsync(string host, CancellationToken cancellationToken);
}

public sealed class HostAddressResolver : IHostAddressResolver
{
    public async Task<IReadOnlyList<IPAddress>> ResolveAsync(string host, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        if (IPAddress.TryParse(host, out var literal)) return [literal];
        return await Dns.GetHostAddressesAsync(host, cancellationToken).ConfigureAwait(false);
    }
}
