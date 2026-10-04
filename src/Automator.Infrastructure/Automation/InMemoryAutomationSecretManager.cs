using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using Automator.Application.Automation;

namespace Automator.Infrastructure.Automation;

/// <summary>Process-local secret store for isolated test runs; it never writes to Windows Credential Manager.</summary>
public sealed class InMemoryAutomationSecretManager : IAutomationSecretManager, IAutomationSecretValueReader
{
    private const int MaximumSecretBytes = 16 * 1024;
    private static readonly Regex ProfileIdPattern = new("^[a-z0-9][a-z0-9._-]{0,63}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex SecretIdPattern = new("^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private readonly ConcurrentDictionary<(string ProfileId, string SecretId), string> _values = new();

    public Task SetAsync(string profileId, string secretId, string value, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateKey(profileId, secretId);
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length == 0 || Encoding.UTF8.GetByteCount(value) > MaximumSecretBytes)
            throw new InvalidDataException("Secret value must be non-empty and at most 16 KiB.");
        _values[(profileId, secretId)] = value;
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string profileId, string secretId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateKey(profileId, secretId);
        return Task.FromResult(_values.ContainsKey((profileId, secretId)));
    }

    public Task DeleteAsync(string profileId, string secretId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateKey(profileId, secretId);
        _values.TryRemove((profileId, secretId), out _);
        return Task.CompletedTask;
    }

    public Task<string?> GetValueAsync(string profileId, string secretId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateKey(profileId, secretId);
        _values.TryGetValue((profileId, secretId), out var value);
        return Task.FromResult(value);
    }

    private static void ValidateKey(string profileId, string secretId)
    {
        if (!ProfileIdPattern.IsMatch(profileId ?? string.Empty) || !SecretIdPattern.IsMatch(secretId ?? string.Empty))
            throw new InvalidDataException("The API secret reference is invalid.");
    }
}
