using System.Text.Json;

namespace OneBrain.BrowserRuntime;

public enum CdpUiLiveStatusFreshness
{
    Fresh,
    Stale,
    Missing,
    Error
}

public sealed record CdpUiLiveStatusRequest(
    string RepositoryRoot,
    string LockfilePath,
    DateTimeOffset? Now = null,
    TimeSpan? FreshWithin = null);

public sealed record CdpUiLiveStatusHealth(
    bool RuntimeConfigured,
    bool ArtifactPinned,
    string HashVerifiedStatus,
    string RuntimeProvider,
    string Source,
    string ExtensionMode,
    bool SystemBrowserAllowed,
    bool BoundaryReadOnly);

public sealed record CdpUiLiveStatusEvidenceSummary(
    string Kind,
    string Status,
    string Decision,
    DateTimeOffset? Timestamp,
    CdpUiLiveStatusFreshness Freshness,
    string LastEvidenceName,
    bool EvidenceAvailable,
    bool CaptureOk,
    bool ScreenshotCaptured,
    int InteractiveElementCount,
    int FrictionSignalCount,
    int ActionMapCount,
    bool RuntimeShutdown,
    bool ProcessExited,
    bool OrphanProcessDetected,
    bool ExtensionUsed,
    bool SystemBrowserUsed,
    bool ExternalNavigationBlocked,
    bool ProductFilesModified,
    string? ErrorCode = null,
    string? ErrorMessage = null);

public sealed record CdpUiLiveStatusSnapshot(
    string Status,
    CdpUiLiveStatusFreshness Freshness,
    string RuntimeProvider,
    string Source,
    bool ArtifactPinned,
    string HashVerifiedStatus,
    DateTimeOffset? LastHealthcheckAt,
    DateTimeOffset? LastBrowserSkillsSessionAt,
    string LastEvidenceName,
    bool EvidenceAvailable,
    bool RuntimeShutdown,
    bool ProcessExited,
    bool OrphanProcessDetected,
    bool ExtensionUsed,
    bool SystemBrowserUsed,
    bool BoundaryReadOnly,
    bool ExternalNavigationBlocked,
    bool ProductFilesModified,
    CdpUiLiveStatusHealth Health,
    CdpUiLiveStatusEvidenceSummary? Healthcheck,
    CdpUiLiveStatusEvidenceSummary? BrowserSkills,
    CdpUiLiveStatusEvidenceSummary? BrowserSkillsSession);

public sealed record CdpUiLiveStatusResult(
    string RuntimeProvider,
    string Source,
    bool ReadOnly,
    bool ExtensionUsed,
    bool SystemBrowserUsed,
    bool BoundaryReadOnly,
    bool ExternalNavigationBlocked,
    bool ProductFilesModified,
    CdpUiLiveStatusSnapshot Snapshot);

public sealed class CdpUiLiveStatusAdapter
{
    private static readonly TimeSpan DefaultFreshWithin = TimeSpan.FromHours(24);

    public CdpUiLiveStatusResult Build(CdpUiLiveStatusRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RepositoryRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.LockfilePath);

        var now = request.Now ?? DateTimeOffset.UtcNow;
        var freshWithin = request.FreshWithin ?? DefaultFreshWithin;
        var runtimeLock = BrowserRuntimeLock.Load(request.LockfilePath);
        var validation = runtimeLock.Validate();
        var artifactPinned = runtimeLock.HasPinnedRuntimeArtifact;
        var health = new CdpUiLiveStatusHealth(
            RuntimeConfigured: validation.IsValid,
            ArtifactPinned: artifactPinned,
            HashVerifiedStatus: artifactPinned ? "verificado" : "pendiente",
            RuntimeProvider: "cloakbrowser",
            Source: "cloakbrowser-cdp-direct",
            ExtensionMode: "legacy/no-default",
            SystemBrowserAllowed: runtimeLock.SystemBrowserAllowed,
            BoundaryReadOnly: true);

        var healthcheck = FindLatestEvidence(
            request.RepositoryRoot,
            "healthcheck",
            "cloakbrowser-cdp-healthcheck-*.redacted.json",
            now,
            freshWithin);
        var browserSkills = FindLatestEvidence(
            request.RepositoryRoot,
            "browser-skills",
            "cloakbrowser-cdp-browser-skills-*.redacted.json",
            now,
            freshWithin,
            fileName => !fileName.StartsWith("cloakbrowser-cdp-browser-skills-session-", StringComparison.OrdinalIgnoreCase));
        var session = FindLatestEvidence(
            request.RepositoryRoot,
            "browser-skills-session",
            "cloakbrowser-cdp-browser-skills-session-*.redacted.json",
            now,
            freshWithin);

        var snapshotFreshness = MergeFreshness(healthcheck, session);
        var status = snapshotFreshness switch
        {
            CdpUiLiveStatusFreshness.Fresh => "listo",
            CdpUiLiveStatusFreshness.Stale => "revisar",
            CdpUiLiveStatusFreshness.Missing => "sin captura reciente",
            CdpUiLiveStatusFreshness.Error => "revisar verificación CDP",
            _ => "revisar"
        };
        var lastEvidence = session ?? browserSkills ?? healthcheck;
        var snapshot = new CdpUiLiveStatusSnapshot(
            Status: status,
            Freshness: snapshotFreshness,
            RuntimeProvider: "cloakbrowser",
            Source: "cloakbrowser-cdp-direct",
            ArtifactPinned: artifactPinned,
            HashVerifiedStatus: health.HashVerifiedStatus,
            LastHealthcheckAt: healthcheck?.Timestamp,
            LastBrowserSkillsSessionAt: session?.Timestamp,
            LastEvidenceName: lastEvidence?.LastEvidenceName ?? "sin-evidencia-cdp",
            EvidenceAvailable: lastEvidence?.EvidenceAvailable ?? false,
            RuntimeShutdown: healthcheck?.RuntimeShutdown ?? session?.RuntimeShutdown ?? false,
            ProcessExited: healthcheck?.ProcessExited ?? session?.ProcessExited ?? false,
            OrphanProcessDetected: healthcheck?.OrphanProcessDetected ?? session?.OrphanProcessDetected ?? false,
            ExtensionUsed: false,
            SystemBrowserUsed: false,
            BoundaryReadOnly: true,
            ExternalNavigationBlocked: session?.ExternalNavigationBlocked ?? browserSkills?.ExternalNavigationBlocked ?? healthcheck?.ExternalNavigationBlocked ?? true,
            ProductFilesModified: false,
            Health: health,
            Healthcheck: healthcheck,
            BrowserSkills: browserSkills,
            BrowserSkillsSession: session);

        return new CdpUiLiveStatusResult(
            RuntimeProvider: "cloakbrowser",
            Source: "cloakbrowser-cdp-direct",
            ReadOnly: true,
            ExtensionUsed: false,
            SystemBrowserUsed: false,
            BoundaryReadOnly: true,
            ExternalNavigationBlocked: true,
            ProductFilesModified: false,
            Snapshot: snapshot);
    }

    private static CdpUiLiveStatusEvidenceSummary? FindLatestEvidence(
        string repositoryRoot,
        string kind,
        string searchPattern,
        DateTimeOffset now,
        TimeSpan freshWithin,
        Func<string, bool>? fileNamePredicate = null)
    {
        var artifactsRoot = Path.Combine(repositoryRoot, "artifacts", "local-verification");
        if (!Directory.Exists(artifactsRoot))
        {
            return Missing(kind);
        }

        var latest = Directory
            .EnumerateFiles(artifactsRoot, searchPattern, SearchOption.TopDirectoryOnly)
            .Where(path => path.EndsWith(".redacted.json", StringComparison.OrdinalIgnoreCase))
            .Where(path => fileNamePredicate?.Invoke(Path.GetFileName(path)) ?? true)
            .Select(path => new FileInfo(path))
            .OrderByDescending(info => info.LastWriteTimeUtc)
            .FirstOrDefault();
        if (latest is null)
        {
            return Missing(kind);
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(latest.FullName));
            var root = document.RootElement;
            var timestamp = GetDateTime(root, "timestamp")
                ?? GetDateTime(root, "createdAt")
                ?? new DateTimeOffset(latest.LastWriteTimeUtc);
            var freshness = now - timestamp <= freshWithin
                ? CdpUiLiveStatusFreshness.Fresh
                : CdpUiLiveStatusFreshness.Stale;
            return new CdpUiLiveStatusEvidenceSummary(
                Kind: kind,
                Status: GetString(root, "status") ?? "available",
                Decision: GetString(root, "decision") ?? "available",
                Timestamp: timestamp,
                Freshness: freshness,
                LastEvidenceName: latest.Name,
                EvidenceAvailable: true,
                CaptureOk: GetBool(root, "captureOk", defaultValue: kind == "healthcheck"),
                ScreenshotCaptured: GetBool(root, "screenshotCaptured"),
                InteractiveElementCount: GetInt(root, "interactiveElements", "interactiveElementCount"),
                FrictionSignalCount: GetInt(root, "frictionSignals"),
                ActionMapCount: GetInt(root, "actionMap", "actionMapEntries"),
                RuntimeShutdown: GetBool(root, "runtimeShutdown", defaultValue: GetBool(root, "shutdownOk")),
                ProcessExited: GetBool(root, "processExited"),
                OrphanProcessDetected: GetBool(root, "orphanProcessDetected"),
                ExtensionUsed: GetBool(root, "extensionUsed"),
                SystemBrowserUsed: GetBool(root, "systemBrowserUsed"),
                ExternalNavigationBlocked: GetBool(root, "externalNavigationBlocked", defaultValue: true),
                ProductFilesModified: GetBool(root, "productFilesModified", "filesModified", false));
        }
        catch (JsonException exception)
        {
            return Error(kind, latest.Name, exception.Message);
        }
        catch (IOException exception)
        {
            return Error(kind, latest.Name, exception.Message);
        }
    }

    private static CdpUiLiveStatusFreshness MergeFreshness(
        CdpUiLiveStatusEvidenceSummary? healthcheck,
        CdpUiLiveStatusEvidenceSummary? session)
    {
        if (healthcheck?.Freshness == CdpUiLiveStatusFreshness.Error || session?.Freshness == CdpUiLiveStatusFreshness.Error)
        {
            return CdpUiLiveStatusFreshness.Error;
        }

        if (healthcheck?.Freshness == CdpUiLiveStatusFreshness.Missing && session?.Freshness == CdpUiLiveStatusFreshness.Missing)
        {
            return CdpUiLiveStatusFreshness.Missing;
        }

        if (healthcheck?.Freshness == CdpUiLiveStatusFreshness.Fresh && session?.Freshness == CdpUiLiveStatusFreshness.Fresh)
        {
            return CdpUiLiveStatusFreshness.Fresh;
        }

        return CdpUiLiveStatusFreshness.Stale;
    }

    private static CdpUiLiveStatusEvidenceSummary Missing(string kind) =>
        new(
            Kind: kind,
            Status: "missing",
            Decision: "missing",
            Timestamp: null,
            Freshness: CdpUiLiveStatusFreshness.Missing,
            LastEvidenceName: "sin-evidencia-cdp",
            EvidenceAvailable: false,
            CaptureOk: false,
            ScreenshotCaptured: false,
            InteractiveElementCount: 0,
            FrictionSignalCount: 0,
            ActionMapCount: 0,
            RuntimeShutdown: false,
            ProcessExited: false,
            OrphanProcessDetected: false,
            ExtensionUsed: false,
            SystemBrowserUsed: false,
            ExternalNavigationBlocked: true,
            ProductFilesModified: false);

    private static CdpUiLiveStatusEvidenceSummary Error(string kind, string fileName, string message) =>
        new(
            Kind: kind,
            Status: "error",
            Decision: "error",
            Timestamp: null,
            Freshness: CdpUiLiveStatusFreshness.Error,
            LastEvidenceName: fileName,
            EvidenceAvailable: false,
            CaptureOk: false,
            ScreenshotCaptured: false,
            InteractiveElementCount: 0,
            FrictionSignalCount: 0,
            ActionMapCount: 0,
            RuntimeShutdown: false,
            ProcessExited: false,
            OrphanProcessDetected: false,
            ExtensionUsed: false,
            SystemBrowserUsed: false,
            ExternalNavigationBlocked: true,
            ProductFilesModified: false,
            ErrorCode: "EVIDENCE_READ_ERROR",
            ErrorMessage: message);

    private static string? GetString(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static DateTimeOffset? GetDateTime(JsonElement root, string propertyName)
    {
        var value = GetString(root, propertyName);
        return DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
    }

    private static bool GetBool(JsonElement root, string propertyName, bool defaultValue = false) =>
        root.TryGetProperty(propertyName, out var property)
            ? property.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => defaultValue
            }
            : defaultValue;

    private static bool GetBool(JsonElement root, string firstName, string secondName, bool defaultValue) =>
        root.TryGetProperty(firstName, out _)
            ? GetBool(root, firstName, defaultValue)
            : GetBool(root, secondName, defaultValue);

    private static int GetInt(JsonElement root, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (!root.TryGetProperty(name, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var value))
            {
                return value;
            }

            if (property.ValueKind == JsonValueKind.Object)
            {
                if (property.TryGetProperty("Count", out var count)
                    && count.ValueKind == JsonValueKind.Number
                    && count.TryGetInt32(out var countValue))
                {
                    return countValue;
                }

                if (property.TryGetProperty("EntriesCount", out var entries)
                    && entries.ValueKind == JsonValueKind.Number
                    && entries.TryGetInt32(out var entriesValue))
                {
                    return entriesValue;
                }
            }
        }

        var bridgeSummaryValue = GetUiBridgeSummaryInt(root, propertyNames);
        if (bridgeSummaryValue.HasValue)
        {
            return bridgeSummaryValue.Value;
        }

        return 0;
    }

    private static int? GetUiBridgeSummaryInt(JsonElement root, params string[] propertyNames)
    {
        if (!root.TryGetProperty("uiBridgeModel", out var bridge)
            || bridge.ValueKind != JsonValueKind.Object
            || !bridge.TryGetProperty("Summary", out var summary)
            || summary.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var candidate = propertyNames.Any(name => name is "interactiveElements" or "interactiveElementCount")
            ? "ElementCount"
            : propertyNames.Any(name => name is "frictionSignals")
                ? "FrictionCount"
                : propertyNames.Any(name => name is "actionMap" or "actionMapEntries")
                    ? "ActionMapCount"
                    : null;
        return candidate is not null
            && summary.TryGetProperty(candidate, out var property)
            && property.ValueKind == JsonValueKind.Number
            && property.TryGetInt32(out var value)
                ? value
                : null;
    }
}
