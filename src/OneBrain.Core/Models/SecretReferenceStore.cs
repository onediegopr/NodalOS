using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace OneBrain.Core.Models;

public sealed class SecretLease : IDisposable
{
    private byte[]? _buffer;

    public SecretLease(byte[] buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        _buffer = buffer;
    }

    public ReadOnlyMemory<byte> Bytes => _buffer ?? throw new ObjectDisposedException(nameof(SecretLease));

    public void Dispose()
    {
        var buffer = Interlocked.Exchange(ref _buffer, null);
        if (buffer is not null)
            CryptographicOperations.ZeroMemory(buffer);
    }
}

public interface ISecretReferenceStore
{
    ValueTask<SecretReference> StoreAsync(
        string logicalName,
        ReadOnlyMemory<byte> secret,
        CancellationToken cancellationToken = default);

    ValueTask<SecretLease?> OpenAsync(
        SecretReference reference,
        CancellationToken cancellationToken = default);

    ValueTask<bool> DeleteAsync(
        SecretReference reference,
        CancellationToken cancellationToken = default);
}

public sealed class EphemeralSecretReferenceStore : ISecretReferenceStore, IDisposable
{
    private readonly ConcurrentDictionary<string, byte[]> _secrets = new(StringComparer.Ordinal);
    private bool _disposed;

    public ValueTask<SecretReference> StoreAsync(
        string logicalName,
        ReadOnlyMemory<byte> secret,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(logicalName);
        if (secret.IsEmpty)
            throw new ArgumentException("Secret cannot be empty.", nameof(secret));

        var id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(logicalName.Trim())))
            .ToLowerInvariant();
        var copy = secret.ToArray();
        _secrets.AddOrUpdate(
            id,
            copy,
            (_, existing) =>
            {
                CryptographicOperations.ZeroMemory(existing);
                return copy;
            });
        return ValueTask.FromResult(new SecretReference("ephemeral", id));
    }

    public ValueTask<SecretLease?> OpenAsync(
        SecretReference reference,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(reference);
        if (!string.Equals(reference.StoreId, "ephemeral", StringComparison.Ordinal))
            return ValueTask.FromResult<SecretLease?>(null);

        return ValueTask.FromResult(
            _secrets.TryGetValue(reference.SecretId, out var secret)
                ? new SecretLease(secret.ToArray())
                : null);
    }

    public ValueTask<bool> DeleteAsync(
        SecretReference reference,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(reference);
        if (!string.Equals(reference.StoreId, "ephemeral", StringComparison.Ordinal))
            return ValueTask.FromResult(false);

        if (!_secrets.TryRemove(reference.SecretId, out var secret))
            return ValueTask.FromResult(false);
        CryptographicOperations.ZeroMemory(secret);
        return ValueTask.FromResult(true);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        foreach (var secret in _secrets.Values)
            CryptographicOperations.ZeroMemory(secret);
        _secrets.Clear();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(EphemeralSecretReferenceStore));
    }
}

public sealed class WindowsDpapiSecretReferenceStore : ISecretReferenceStore
{
    public const string StoreId = "windows-dpapi";

    private static readonly byte[] OptionalEntropy =
        SHA256.HashData("NODAL OS model secret references v1"u8.ToArray());

    private readonly string _rootDirectory;

    public WindowsDpapiSecretReferenceStore(string? rootDirectory = null)
    {
        _rootDirectory = rootDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NodalOS",
            "secrets");
    }

    public async ValueTask<SecretReference> StoreAsync(
        string logicalName,
        ReadOnlyMemory<byte> secret,
        CancellationToken cancellationToken = default)
    {
        EnsureWindows();
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(logicalName);
        if (secret.IsEmpty)
            throw new ArgumentException("Secret cannot be empty.", nameof(secret));

        Directory.CreateDirectory(_rootDirectory);
        var id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(logicalName.Trim())))
            .ToLowerInvariant();
        var path = ResolvePath(id);
        var plaintext = secret.ToArray();
        byte[] protectedBytes;
        try
        {
            protectedBytes = ProtectedData.Protect(plaintext, OptionalEntropy, DataProtectionScope.CurrentUser);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }

        var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            await File.WriteAllBytesAsync(tempPath, protectedBytes, cancellationToken).ConfigureAwait(false);
            File.Move(tempPath, path, overwrite: true);
            TryHide(path);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(protectedBytes);
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }

        return new SecretReference(StoreId, id);
    }

    public async ValueTask<SecretLease?> OpenAsync(
        SecretReference reference,
        CancellationToken cancellationToken = default)
    {
        EnsureWindows();
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(reference);
        if (!string.Equals(reference.StoreId, StoreId, StringComparison.Ordinal))
            return null;

        var path = ResolvePath(reference.SecretId);
        if (!File.Exists(path))
            return null;

        var protectedBytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        try
        {
            var plaintext = ProtectedData.Unprotect(protectedBytes, OptionalEntropy, DataProtectionScope.CurrentUser);
            return new SecretLease(plaintext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(protectedBytes);
        }
    }

    public ValueTask<bool> DeleteAsync(
        SecretReference reference,
        CancellationToken cancellationToken = default)
    {
        EnsureWindows();
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(reference);
        if (!string.Equals(reference.StoreId, StoreId, StringComparison.Ordinal))
            return ValueTask.FromResult(false);

        var path = ResolvePath(reference.SecretId);
        if (!File.Exists(path))
            return ValueTask.FromResult(false);
        File.Delete(path);
        return ValueTask.FromResult(true);
    }

    private string ResolvePath(string secretId)
    {
        if (string.IsNullOrWhiteSpace(secretId) || secretId.Any(value => !Uri.IsHexDigit(value)))
            throw new ArgumentException("Secret reference id is invalid.", nameof(secretId));
        return Path.Combine(_rootDirectory, secretId.ToLowerInvariant() + ".bin");
    }

    private static void TryHide(string path)
    {
        try
        {
            File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Hidden);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void EnsureWindows()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Windows DPAPI secret storage requires Windows.");
    }
}
