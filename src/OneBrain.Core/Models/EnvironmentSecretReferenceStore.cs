using System.Text;

namespace OneBrain.Core.Models;

public sealed class EnvironmentSecretReferenceStore : ISecretReferenceStore
{
    public const string StoreId = "environment";

    private readonly IReadOnlyDictionary<string, string?> _values;

    public EnvironmentSecretReferenceStore(IReadOnlyDictionary<string, string?>? values = null)
    {
        _values = values ?? Environment.GetEnvironmentVariables()
            .Cast<System.Collections.DictionaryEntry>()
            .ToDictionary(
                entry => entry.Key?.ToString() ?? string.Empty,
                entry => entry.Value?.ToString(),
                StringComparer.OrdinalIgnoreCase);
    }

    public ValueTask<SecretReference> StoreAsync(
        string logicalName,
        ReadOnlyMemory<byte> secret,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Environment-backed secrets are read-only.");

    public ValueTask<SecretLease?> OpenAsync(
        SecretReference reference,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(reference);
        if (!string.Equals(reference.StoreId, StoreId, StringComparison.Ordinal))
            return ValueTask.FromResult<SecretLease?>(null);
        if (!IsValidEnvironmentKey(reference.SecretId))
            throw new ArgumentException("Environment secret reference id is invalid.", nameof(reference));
        if (!_values.TryGetValue(reference.SecretId, out var value) || string.IsNullOrWhiteSpace(value))
            return ValueTask.FromResult<SecretLease?>(null);

        return ValueTask.FromResult<SecretLease?>(
            new SecretLease(Encoding.UTF8.GetBytes(value)));
    }

    public ValueTask<bool> DeleteAsync(
        SecretReference reference,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(reference);
        return ValueTask.FromResult(false);
    }

    private static bool IsValidEnvironmentKey(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length <= 160 &&
        value.All(character => char.IsAsciiLetterOrDigit(character) || character == '_');
}

public sealed class CompositeSecretReferenceStore : ISecretReferenceStore, IDisposable
{
    private readonly IReadOnlyDictionary<string, ISecretReferenceStore> _stores;
    private readonly string? _defaultStoreId;
    private bool _disposed;

    public CompositeSecretReferenceStore(
        IReadOnlyDictionary<string, ISecretReferenceStore> stores,
        string? defaultStoreId = null)
    {
        ArgumentNullException.ThrowIfNull(stores);
        if (stores.Count == 0)
            throw new ArgumentException("At least one secret store is required.", nameof(stores));

        _stores = new Dictionary<string, ISecretReferenceStore>(stores, StringComparer.Ordinal);
        _defaultStoreId = string.IsNullOrWhiteSpace(defaultStoreId) ? null : defaultStoreId;
        if (_defaultStoreId is not null && !_stores.ContainsKey(_defaultStoreId))
            throw new ArgumentException("The default secret store is not registered.", nameof(defaultStoreId));
    }

    public ValueTask<SecretReference> StoreAsync(
        string logicalName,
        ReadOnlyMemory<byte> secret,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (_defaultStoreId is null)
            throw new InvalidOperationException("No writable default secret store is configured.");
        return _stores[_defaultStoreId].StoreAsync(logicalName, secret, cancellationToken);
    }

    public ValueTask<SecretLease?> OpenAsync(
        SecretReference reference,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(reference);
        return _stores.TryGetValue(reference.StoreId, out var store)
            ? store.OpenAsync(reference, cancellationToken)
            : ValueTask.FromResult<SecretLease?>(null);
    }

    public ValueTask<bool> DeleteAsync(
        SecretReference reference,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(reference);
        return _stores.TryGetValue(reference.StoreId, out var store)
            ? store.DeleteAsync(reference, cancellationToken)
            : ValueTask.FromResult(false);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        foreach (var store in _stores.Values.Distinct())
        {
            if (store is IDisposable disposable)
                disposable.Dispose();
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(CompositeSecretReferenceStore));
    }
}
