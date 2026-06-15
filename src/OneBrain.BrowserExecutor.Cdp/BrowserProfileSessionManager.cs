using System.Security.Cryptography;
using System.Text;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public enum BrowserProfileKind { Disposable, PersistentControlled, UserProfileWithExplicitConsent }
public enum BrowserStorageScope { Tenant, Company, Person, Worker, Portal, Recipe, Runtime, Temporary }
public enum BrowserProfileCleanupPolicy { DeleteOnClose, KeepControlled, ManualReviewRequired }
public enum BrowserProfileConsentPolicy { NotRequired, ExplicitConsentRequired, Granted }
public enum BrowserSessionState { Created, Launching, Active, Suspended, Closed, Expired, Failed, CleanupPending, Disposed }

public sealed record BrowserProfileId(string Value)
{
    public static BrowserProfileId New(string prefix = "profile") => new($"{prefix}-{Guid.NewGuid():N}");
    public override string ToString() => Value;
}

public sealed record ManagedBrowserSessionId(string Value)
{
    public static ManagedBrowserSessionId New(string prefix = "session") => new($"{prefix}-{Guid.NewGuid():N}");
    public override string ToString() => Value;
}

public sealed record BrowserProfilePolicy(
    BrowserProfileKind Kind,
    BrowserStorageScope Scope,
    BrowserProfileCleanupPolicy CleanupPolicy,
    BrowserProfileConsentPolicy ConsentPolicy,
    bool AllowRealUserProfile,
    string ControlledRootDirectory)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(ControlledRootDirectory))
            errors.Add("ControlledRootDirectory is required.");

        if (Kind == BrowserProfileKind.UserProfileWithExplicitConsent &&
            (!AllowRealUserProfile || ConsentPolicy != BrowserProfileConsentPolicy.Granted))
            errors.Add("Real user profile requires explicit consent.");

        if (Kind == BrowserProfileKind.PersistentControlled &&
            CleanupPolicy == BrowserProfileCleanupPolicy.DeleteOnClose)
            errors.Add("Persistent controlled profile cannot use DeleteOnClose cleanup.");

        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record BrowserSessionPolicy(
    string Owner,
    string CorrelationId,
    TimeSpan? ExpiresAfter,
    BrowserProfileCleanupPolicy CleanupPolicy)
{
    public ContractValidationResult Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Owner))
            errors.Add("Owner is required.");
        if (string.IsNullOrWhiteSpace(CorrelationId))
            errors.Add("CorrelationId is required.");
        return errors.Count == 0 ? ContractValidationResult.Valid : new ContractValidationResult(false, errors);
    }
}

public sealed record BrowserProfileDescriptor(
    BrowserProfileId ProfileId,
    BrowserProfileKind Kind,
    BrowserStorageScope Scope,
    string UserDataDir,
    BrowserProfileCleanupPolicy CleanupPolicy,
    BrowserProfileConsentPolicy ConsentPolicy,
    DateTimeOffset CreatedAtUtc)
{
    public bool IsDisposable => Kind == BrowserProfileKind.Disposable;
}

public sealed record BrowserSessionDescriptor(
    ManagedBrowserSessionId SessionId,
    BrowserProfileId ProfileId,
    string Owner,
    string CorrelationId,
    BrowserSessionState State,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    BrowserProfileCleanupPolicy CleanupPolicy)
{
    public bool IsAlive(DateTimeOffset now) =>
        State == BrowserSessionState.Active &&
        (ExpiresAtUtc is null || ExpiresAtUtc > now);

    public bool CanAcceptModifyingAction(DateTimeOffset now) =>
        IsAlive(now);
}

public sealed record BrowserProfileDiagnostic(
    BrowserProfileId ProfileId,
    ManagedBrowserSessionId? SessionId,
    BrowserProfileKind Kind,
    BrowserSessionState? State,
    string RedactedUserDataDir,
    string Owner,
    string CorrelationId);

public sealed class BrowserProfileManager
{
    private readonly string _controlledRoot;

    public BrowserProfileManager(string? controlledRoot = null)
    {
        _controlledRoot = controlledRoot ?? Path.Combine(Path.GetTempPath(), "onebrain-browser-profiles");
    }

    public string ControlledRoot => _controlledRoot;

    public BrowserProfileDescriptor CreateProfile(BrowserProfilePolicy policy)
    {
        var validation = policy.Validate();
        if (!validation.IsValid)
            throw new InvalidOperationException(string.Join("; ", validation.Errors));

        Directory.CreateDirectory(policy.ControlledRootDirectory);
        var id = BrowserProfileId.New(policy.Kind == BrowserProfileKind.Disposable ? "disposable" : "controlled");
        var path = policy.Kind switch
        {
            BrowserProfileKind.Disposable => Path.Combine(Path.GetTempPath(), "onebrain-cdp-" + Guid.NewGuid().ToString("N")),
            BrowserProfileKind.PersistentControlled => Path.Combine(policy.ControlledRootDirectory, id.Value),
            BrowserProfileKind.UserProfileWithExplicitConsent => throw new InvalidOperationException("Real user profile launch is not implemented in M7."),
            _ => throw new ArgumentOutOfRangeException(nameof(policy))
        };

        Directory.CreateDirectory(path);
        return new BrowserProfileDescriptor(id, policy.Kind, policy.Scope, path, policy.CleanupPolicy, policy.ConsentPolicy, DateTimeOffset.UtcNow);
    }

    public async Task CleanupProfileAsync(BrowserProfileDescriptor profile)
    {
        if (profile.CleanupPolicy != BrowserProfileCleanupPolicy.DeleteOnClose)
            return;

        await TryDeleteDirectoryAsync(profile.UserDataDir).ConfigureAwait(false);
    }

    public static string RedactPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "";
        var leaf = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(path))).ToLowerInvariant()[..8];
        return $"[REDACTED-PATH:{leaf}:{hash}]";
    }

    private static async Task TryDeleteDirectoryAsync(string path)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            try
            {
                if (!Directory.Exists(path))
                    return;

                Directory.Delete(path, recursive: true);
                return;
            }
            catch
            {
                await Task.Delay(200).ConfigureAwait(false);
            }
        }
    }
}

public sealed class BrowserSessionManager
{
    private readonly Dictionary<ManagedBrowserSessionId, BrowserSessionDescriptor> _sessions = new();

    public BrowserSessionDescriptor CreateSession(BrowserProfileDescriptor profile, BrowserSessionPolicy policy)
    {
        var validation = policy.Validate();
        if (!validation.IsValid)
            throw new InvalidOperationException(string.Join("; ", validation.Errors));

        var session = new BrowserSessionDescriptor(
            ManagedBrowserSessionId.New(),
            profile.ProfileId,
            policy.Owner,
            policy.CorrelationId,
            BrowserSessionState.Created,
            DateTimeOffset.UtcNow,
            policy.ExpiresAfter is null ? null : DateTimeOffset.UtcNow + policy.ExpiresAfter.Value,
            policy.CleanupPolicy);
        _sessions[session.SessionId] = session;
        return session;
    }

    public BrowserSessionDescriptor MarkState(ManagedBrowserSessionId sessionId, BrowserSessionState state)
    {
        var current = Get(sessionId);
        var next = current with { State = state };
        _sessions[sessionId] = next;
        return next;
    }

    public BrowserSessionDescriptor Get(ManagedBrowserSessionId sessionId) =>
        _sessions.TryGetValue(sessionId, out var session)
            ? session
            : throw new KeyNotFoundException($"Unknown browser session: {sessionId}");

    public bool CanAcceptModifyingAction(ManagedBrowserSessionId sessionId, DateTimeOffset now) =>
        Get(sessionId).CanAcceptModifyingAction(now);

    public BrowserProfileDiagnostic Diagnostic(BrowserProfileDescriptor profile, BrowserSessionDescriptor? session = null) =>
        new(
            profile.ProfileId,
            session?.SessionId,
            profile.Kind,
            session?.State,
            BrowserProfileManager.RedactPath(profile.UserDataDir),
            session?.Owner ?? "",
            session?.CorrelationId ?? "");
}
