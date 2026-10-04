using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Automator.Application.Automation;

namespace Automator.Infrastructure.Automation;

/// <summary>Internal host-only read path. This is intentionally not exposed through tab capabilities.</summary>
public interface IAutomationSecretValueReader
{
    Task<string?> GetValueAsync(string profileId, string secretId, CancellationToken cancellationToken);
}

/// <summary>Windows Credential Manager storage for opaque API-profile secret references.</summary>
public sealed class WindowsCredentialSecretManager : IAutomationSecretManager, IAutomationSecretValueReader
{
    private const int MaximumSecretBytes = 16 * 1024;
    private const int CredentialBlobLimit = 5 * 512;
    private const int ChunkPayloadBytes = 2_400;
    private const int MaximumChunks = (MaximumSecretBytes + ChunkPayloadBytes - 1) / ChunkPayloadBytes;
    private const uint GenericCredential = 1;
    private const uint PersistLocalMachine = 2;
    private const int ErrorNotFound = 1168;
    private static readonly Regex ProfileIdPattern = new("^[a-z0-9][a-z0-9._-]{0,63}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex SecretIdPattern = new("^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public Task SetAsync(string profileId, string secretId, string value, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateKey(profileId, secretId);
        ArgumentNullException.ThrowIfNull(value);
        var secretBytes = Encoding.UTF8.GetBytes(value);
        if (secretBytes.Length is 0 or > MaximumSecretBytes)
        {
            CryptographicOperations.ZeroMemory(secretBytes);
            throw new InvalidDataException("Secret value must be non-empty and at most 16 KiB.");
        }

        var baseTarget = Target(profileId, secretId);
        var version = Guid.NewGuid().ToString("N");
        var chunkCount = (secretBytes.Length + ChunkPayloadBytes - 1) / ChunkPayloadBytes;
        var oldManifestBytes = ReadCredential(baseTarget);
        SecretManifest? oldManifest = null;
        if (oldManifestBytes is not null)
        {
            try { oldManifest = ParseManifest(oldManifestBytes); }
            finally { CryptographicOperations.ZeroMemory(oldManifestBytes); }
        }

        var manifestWritten = false;
        try
        {
            for (var index = 0; index < chunkCount; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var offset = index * ChunkPayloadBytes;
                var count = Math.Min(ChunkPayloadBytes, secretBytes.Length - offset);
                var chunk = secretBytes.AsSpan(offset, count).ToArray();
                try { WriteCredential(ChunkTarget(baseTarget, version, index), chunk); }
                finally { CryptographicOperations.ZeroMemory(chunk); }
            }

            var manifest = JsonSerializer.SerializeToUtf8Bytes(new SecretManifest(version, chunkCount));
            try
            {
                WriteCredential(baseTarget, manifest);
                manifestWritten = true;
            }
            finally { CryptographicOperations.ZeroMemory(manifest); }
        }
        catch (OperationCanceledException)
        {
            if (!manifestWritten) TryDeleteChunks(baseTarget, version, chunkCount);
            throw;
        }
        catch
        {
            if (!manifestWritten) TryDeleteChunks(baseTarget, version, chunkCount);
            throw new IOException("Windows Credential Manager could not store this API secret.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secretBytes);
        }
        if (oldManifest is not null && oldManifest.Version != version)
            TryDeleteChunks(baseTarget, oldManifest.Version, oldManifest.Count);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string profileId, string secretId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateKey(profileId, secretId);
        var value = ReadCredential(Target(profileId, secretId));
        if (value is null) return Task.FromResult(false);
        try
        {
            var manifest = ParseManifest(value);
            for (var index = 0; index < manifest.Count; index++)
            {
                var chunk = ReadCredential(ChunkTarget(Target(profileId, secretId), manifest.Version, index));
                if (chunk is null) return Task.FromResult(false);
                CryptographicOperations.ZeroMemory(chunk);
            }
            return Task.FromResult(true);
        }
        finally { CryptographicOperations.ZeroMemory(value); }
    }

    public Task DeleteAsync(string profileId, string secretId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateKey(profileId, secretId);
        var baseTarget = Target(profileId, secretId);
        var manifestBytes = ReadCredential(baseTarget);
        if (manifestBytes is not null)
        {
            try
            {
                var manifest = ParseManifest(manifestBytes);
                for (var index = 0; index < manifest.Count; index++) DeleteCredential(ChunkTarget(baseTarget, manifest.Version, index));
            }
            finally { CryptographicOperations.ZeroMemory(manifestBytes); }
        }
        DeleteCredential(baseTarget);
        return Task.CompletedTask;
    }

    public Task<string?> GetValueAsync(string profileId, string secretId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateKey(profileId, secretId);
        var baseTarget = Target(profileId, secretId);
        var manifestBytes = ReadCredential(baseTarget);
        if (manifestBytes is null) return Task.FromResult<string?>(null);
        try
        {
            var manifest = ParseManifest(manifestBytes);
            using var payload = new MemoryStream();
            for (var index = 0; index < manifest.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var chunk = ReadCredential(ChunkTarget(baseTarget, manifest.Version, index))
                    ?? throw new InvalidDataException("An API credential is incomplete.");
                try
                {
                    if (payload.Length + chunk.Length > MaximumSecretBytes)
                        throw new InvalidDataException("An API credential exceeds the supported size.");
                    payload.Write(chunk);
                }
                finally { CryptographicOperations.ZeroMemory(chunk); }
            }

            var complete = payload.ToArray();
            try { return Task.FromResult<string?>(StrictUtf8.GetString(complete)); }
            finally { CryptographicOperations.ZeroMemory(complete); }
        }
        finally { CryptographicOperations.ZeroMemory(manifestBytes); }
    }

    private static void ValidateKey(string profileId, string secretId)
    {
        if (!ProfileIdPattern.IsMatch(profileId ?? string.Empty) || !SecretIdPattern.IsMatch(secretId ?? string.Empty))
            throw new InvalidDataException("The API secret reference is invalid.");
    }

    private static string Target(string profileId, string secretId) => $"Automator/api/{profileId}/{secretId}";
    private static string ChunkTarget(string baseTarget, string version, int index) => $"{baseTarget}/{version}/{index:D2}";

    private static SecretManifest ParseManifest(byte[] bytes)
    {
        try
        {
            var manifest = JsonSerializer.Deserialize<SecretManifest>(bytes);
            if (manifest is null || !Guid.TryParseExact(manifest.Version, "N", out _) || manifest.Count is < 1 or > MaximumChunks)
                throw new InvalidDataException("An API credential reference is invalid.");
            return manifest;
        }
        catch (JsonException) { throw new InvalidDataException("An API credential reference is invalid."); }
    }

    private static void TryDeleteChunks(string baseTarget, string version, int count)
    {
        for (var index = 0; index < count; index++)
        {
            try { DeleteCredential(ChunkTarget(baseTarget, version, index)); }
            catch { /* Credential cleanup is best-effort after the manifest points to a complete version. */ }
        }
    }

    private static void WriteCredential(string target, byte[] blob)
    {
        if (blob.Length > CredentialBlobLimit)
            throw new InvalidDataException("A Credential Manager entry exceeds its supported size.");
        var targetPointer = Marshal.StringToHGlobalUni(target);
        var blobPointer = Marshal.AllocHGlobal(blob.Length);
        try
        {
            Marshal.Copy(blob, 0, blobPointer, blob.Length);
            var credential = new NativeCredential
            {
                Type = GenericCredential,
                TargetName = targetPointer,
                CredentialBlobSize = (uint)blob.Length,
                CredentialBlob = blobPointer,
                Persist = PersistLocalMachine,
            };
            if (!CredWriteW(ref credential, 0))
                throw new IOException("Windows Credential Manager rejected an API secret operation.");
        }
        finally
        {
            Marshal.Copy(new byte[blob.Length], 0, blobPointer, blob.Length);
            Marshal.FreeHGlobal(blobPointer);
            Marshal.FreeHGlobal(targetPointer);
        }
    }

    private static byte[]? ReadCredential(string target)
    {
        if (!CredReadW(target, GenericCredential, 0, out var credentialPointer))
        {
            if (Marshal.GetLastWin32Error() == ErrorNotFound) return null;
            throw new IOException("Windows Credential Manager could not read an API secret.");
        }

        try
        {
            var credential = Marshal.PtrToStructure<NativeCredential>(credentialPointer);
            if (credential.CredentialBlobSize > CredentialBlobLimit)
                throw new InvalidDataException("A Credential Manager entry has an invalid size.");
            var value = new byte[checked((int)credential.CredentialBlobSize)];
            if (value.Length > 0) Marshal.Copy(credential.CredentialBlob, value, 0, value.Length);
            return value;
        }
        finally { CredFree(credentialPointer); }
    }

    private static void DeleteCredential(string target)
    {
        if (CredDeleteW(target, GenericCredential, 0)) return;
        if (Marshal.GetLastWin32Error() != ErrorNotFound)
            throw new IOException("Windows Credential Manager could not remove an API secret.");
    }

    private sealed record SecretManifest(string Version, int Count);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeCredential
    {
        public uint Flags;
        public uint Type;
        public IntPtr TargetName;
        public IntPtr Comment;
        public long LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public IntPtr TargetAlias;
        public IntPtr UserName;
    }

    [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredWriteW(ref NativeCredential credential, uint flags);

    [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredReadW(string targetName, uint type, uint flags, out IntPtr credential);

    [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredDeleteW(string targetName, uint type, uint flags);

    [DllImport("advapi32.dll", EntryPoint = "CredFree")]
    private static extern void CredFree(IntPtr buffer);
}
