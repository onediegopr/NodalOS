using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;
using OneBrain.Core.Models;
using OneBrain.Core.Runtime;

namespace OneBrain.AgentOperations.Core.Workspace;

public enum NodalOsWorkspaceSelectionState
{
    NotConfigured,
    Ready,
    InvalidRoot,
    SecretUnavailable,
    CorruptMetadata,
    FailedClosed
}

public sealed record NodalOsWorkspaceSelectionScanSummary(
    string ScanDecision,
    int FilesRead,
    int FilesSkipped,
    long TotalBytesRead,
    bool Truncated,
    IReadOnlyDictionary<string, int> ExtensionCounts,
    string EvidenceDigest,
    string PlanDecision,
    IReadOnlyList<string> PlanSteps,
    IReadOnlyList<string> ReviewBlockers,
    DateTimeOffset ValidatedAt);

public sealed record NodalOsPersistedWorkspaceSelection(
    int SchemaVersion,
    NodalOsWorkspaceLocalModel Workspace,
    SecretReference RootPathReference,
    NodalOsWorkspaceSelectionScanSummary LastScan,
    DateTimeOffset SelectedAt,
    DateTimeOffset UpdatedAt);

public sealed record NodalOsWorkspaceSelectionSnapshot(
    string Decision,
    bool Accepted,
    NodalOsWorkspaceSelectionState State,
    NodalOsWorkspaceLocalModel? Workspace,
    string? WorkspaceId,
    string? DisplayNameRedacted,
    string? RootPathHintRedacted,
    string? RootPathFingerprint,
    string? PathJailBindingId,
    bool Persisted,
    bool Rehydrated,
    string ScanDecision,
    int FilesRead,
    int FilesSkipped,
    long TotalBytesRead,
    bool Truncated,
    IReadOnlyDictionary<string, int> ExtensionCounts,
    string EvidenceDigest,
    string PlanDecision,
    IReadOnlyList<string> PlanSteps,
    IReadOnlyList<string> ReviewBlockers,
    DateTimeOffset? SelectedAt,
    DateTimeOffset? LastValidatedAt,
    bool RealFilesystemRead,
    bool WorkspaceFilesystemMutated,
    bool AppConfigurationMutated,
    bool NetworkUsed,
    bool SecretsExcluded,
    bool ProductAuthorityGranted);

public sealed class NodalOsWorkspaceSelectionService
{
    public const int CurrentSchemaVersion = 1;

    private const int MaximumMetadataBytes = 1024 * 1024;
    private readonly string _metadataFilePath;
    private readonly ISecretReferenceStore _rootReferenceStore;
    private readonly BoundedWorkspaceUnderstandingService _scanner;
    private readonly BoundedWorkspacePlanningContextService _planning;
    private readonly NodalOsWorkspaceValidator _workspaceValidator;
    private readonly NodalOsRedactionService _redaction;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public NodalOsWorkspaceSelectionService(
        string metadataFilePath,
        ISecretReferenceStore rootReferenceStore,
        BoundedWorkspaceUnderstandingService? scanner = null,
        BoundedWorkspacePlanningContextService? planning = null,
        NodalOsWorkspaceValidator? workspaceValidator = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(metadataFilePath);
        ArgumentNullException.ThrowIfNull(rootReferenceStore);

        _metadataFilePath = Path.GetFullPath(metadataFilePath);
        _rootReferenceStore = rootReferenceStore;
        _scanner = scanner ?? new BoundedWorkspaceUnderstandingService();
        _planning = planning ?? new BoundedWorkspacePlanningContextService();
        _workspaceValidator = workspaceValidator ?? new NodalOsWorkspaceValidator();
        _redaction = new NodalOsRedactionService();
    }

    public string MetadataFilePath => _metadataFilePath;

    public async ValueTask<NodalOsWorkspaceSelectionSnapshot> SelectAsync(
        string? rootPath,
        string? displayName = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rootPath) || rootPath.Length > 2048)
            return Failure("BLOCKED_WORKSPACE_SELECTION_INVALID_ROOT", NodalOsWorkspaceSelectionState.InvalidRoot, "Workspace root is missing or too long.");

        string canonicalRoot;
        try
        {
            canonicalRoot = Path.GetFullPath(rootPath.Trim());
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return Failure("BLOCKED_WORKSPACE_SELECTION_INVALID_ROOT", NodalOsWorkspaceSelectionState.InvalidRoot, "Workspace root is invalid.");
        }

        BoundedWorkspaceScanResult scan;
        try
        {
            scan = await _scanner.ScanAsync(
                    new BoundedWorkspaceScanRequest(canonicalRoot),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return Failure("BLOCKED_WORKSPACE_SELECTION_SCAN_FAILED_CLOSED", NodalOsWorkspaceSelectionState.FailedClosed, "Workspace validation failed closed.");
        }

        if (scan.Decision != BoundedWorkspaceScanDecision.Accepted)
            return FromRejectedScan(scan, appConfigurationMutated: false);

        var workspaceId = $"workspace-{scan.RootFingerprint[..16]}";
        var missionId = $"mission-{scan.RootFingerprint[..12]}-workspace-understanding";
        var planning = _planning.Build(scan, workspaceId, missionId);
        if (!planning.Accepted || planning.MissionPlan is null)
        {
            return Failure(
                "BLOCKED_WORKSPACE_SELECTION_PLAN_NOT_READY",
                NodalOsWorkspaceSelectionState.FailedClosed,
                planning.Blockers.DefaultIfEmpty("Workspace planning context is not ready.").ToArray(),
                scan);
        }

        var now = DateTimeOffset.UtcNow;
        var safeDisplayName = ResolveDisplayName(canonicalRoot, displayName);
        var workspace = BuildWorkspace(workspaceId, safeDisplayName, scan, planning, now);
        var validation = _workspaceValidator.ValidateWorkspace(workspace);
        if (!validation.IsValid)
        {
            return Failure(
                "BLOCKED_WORKSPACE_SELECTION_MODEL_INVALID",
                NodalOsWorkspaceSelectionState.FailedClosed,
                ["Workspace metadata did not pass the canonical validation boundary."],
                scan);
        }

        NodalOsPersistedWorkspaceSelection? previous = null;
        try
        {
            previous = await TryReadDocumentAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            previous = null;
        }

        var plaintext = Encoding.UTF8.GetBytes(canonicalRoot);
        SecretReference newRootReference;
        try
        {
            newRootReference = await _rootReferenceStore.StoreAsync(
                    $"workspace-root:{scan.RootFingerprint}:{Guid.NewGuid():N}",
                    plaintext,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return Failure("BLOCKED_WORKSPACE_SELECTION_PROTECTED_ROOT_STORE_FAILED", NodalOsWorkspaceSelectionState.SecretUnavailable, "Protected local root storage is unavailable.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }

        var scanSummary = BuildScanSummary(scan, planning, now);
        var document = new NodalOsPersistedWorkspaceSelection(
            CurrentSchemaVersion,
            workspace,
            newRootReference,
            scanSummary,
            now,
            now);

        try
        {
            await WriteDocumentAsync(document, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            await BestEffortDeleteSecretAsync(newRootReference).ConfigureAwait(false);
            throw;
        }
        catch
        {
            await BestEffortDeleteSecretAsync(newRootReference).ConfigureAwait(false);
            return Failure("BLOCKED_WORKSPACE_SELECTION_METADATA_PERSISTENCE_FAILED", NodalOsWorkspaceSelectionState.FailedClosed, "Workspace metadata persistence failed closed.");
        }

        if (previous is not null && !SameReference(previous.RootPathReference, newRootReference))
            await BestEffortDeleteSecretAsync(previous.RootPathReference).ConfigureAwait(false);

        return Success(document, scan, planning, rehydrated: false, appConfigurationMutated: true, now);
    }

    public async ValueTask<NodalOsWorkspaceSelectionSnapshot> GetCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_metadataFilePath))
            return NotConfigured();

        NodalOsPersistedWorkspaceSelection? document;
        try
        {
            document = await ReadDocumentAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return Failure("BLOCKED_WORKSPACE_SELECTION_METADATA_CORRUPT", NodalOsWorkspaceSelectionState.CorruptMetadata, "Persisted workspace metadata is unavailable or invalid.", persisted: true);
        }

        if (document is null || document.SchemaVersion != CurrentSchemaVersion)
            return Failure("BLOCKED_WORKSPACE_SELECTION_METADATA_CORRUPT", NodalOsWorkspaceSelectionState.CorruptMetadata, "Persisted workspace metadata schema is invalid.", persisted: true);

        var validation = _workspaceValidator.ValidateWorkspace(document.Workspace);
        if (!validation.IsValid || Path.IsPathRooted(document.Workspace.LocalRootPathRedacted))
        {
            return Failure("BLOCKED_WORKSPACE_SELECTION_METADATA_CORRUPT", NodalOsWorkspaceSelectionState.CorruptMetadata, "Persisted workspace metadata failed the canonical validation boundary.", persisted: true);
        }

        using var lease = await OpenRootLeaseAsync(document.RootPathReference, cancellationToken).ConfigureAwait(false);
        if (lease is null)
            return Failure("BLOCKED_WORKSPACE_SELECTION_PROTECTED_ROOT_UNAVAILABLE", NodalOsWorkspaceSelectionState.SecretUnavailable, "The protected local root reference could not be opened.", persisted: true, workspace: document.Workspace);

        string rootPath;
        try
        {
            rootPath = Encoding.UTF8.GetString(lease.Bytes.Span);
        }
        catch
        {
            return Failure("BLOCKED_WORKSPACE_SELECTION_PROTECTED_ROOT_INVALID", NodalOsWorkspaceSelectionState.SecretUnavailable, "The protected local root reference is invalid.", persisted: true, workspace: document.Workspace);
        }

        BoundedWorkspaceScanResult scan;
        try
        {
            scan = await _scanner.ScanAsync(new BoundedWorkspaceScanRequest(rootPath), cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return Failure("BLOCKED_WORKSPACE_SELECTION_REVALIDATION_FAILED_CLOSED", NodalOsWorkspaceSelectionState.FailedClosed, "Workspace revalidation failed closed.", persisted: true, workspace: document.Workspace);
        }

        if (scan.Decision != BoundedWorkspaceScanDecision.Accepted)
            return FromRejectedScan(scan, appConfigurationMutated: false, persisted: true, workspace: document.Workspace);
        if (!string.Equals(scan.RootFingerprint, document.Workspace.RootPathFingerprint, StringComparison.Ordinal))
            return Failure("BLOCKED_WORKSPACE_SELECTION_ROOT_FINGERPRINT_CHANGED", NodalOsWorkspaceSelectionState.InvalidRoot, "Workspace root identity changed after selection.", scan, persisted: true, workspace: document.Workspace);

        var planning = _planning.Build(
            scan,
            document.Workspace.WorkspaceId,
            $"mission-{scan.RootFingerprint[..12]}-workspace-understanding");
        if (!planning.Accepted || planning.MissionPlan is null)
        {
            return Failure(
                "BLOCKED_WORKSPACE_SELECTION_PLAN_NOT_READY",
                NodalOsWorkspaceSelectionState.FailedClosed,
                planning.Blockers.DefaultIfEmpty("Workspace planning context is not ready.").ToArray(),
                scan,
                persisted: true,
                workspace: document.Workspace);
        }

        return Success(document, scan, planning, rehydrated: true, appConfigurationMutated: false, DateTimeOffset.UtcNow);
    }

    public async ValueTask<NodalOsWorkspaceSelectionSnapshot> ClearAsync(
        CancellationToken cancellationToken = default)
    {
        NodalOsPersistedWorkspaceSelection? document = null;
        try
        {
            document = await TryReadDocumentAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
        }

        try
        {
            if (File.Exists(_metadataFilePath))
                File.Delete(_metadataFilePath);
        }
        catch
        {
            return Failure("BLOCKED_WORKSPACE_SELECTION_CLEAR_FAILED", NodalOsWorkspaceSelectionState.FailedClosed, "Workspace selection could not be cleared safely.", persisted: true);
        }

        if (document is not null)
            await BestEffortDeleteSecretAsync(document.RootPathReference).ConfigureAwait(false);

        return NotConfigured(appConfigurationMutated: true);
    }

    private async ValueTask<NodalOsPersistedWorkspaceSelection?> TryReadDocumentAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_metadataFilePath))
            return null;
        return await ReadDocumentAsync(cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<NodalOsPersistedWorkspaceSelection?> ReadDocumentAsync(CancellationToken cancellationToken)
    {
        var info = new FileInfo(_metadataFilePath);
        if (!info.Exists || info.Length is <= 0 or > MaximumMetadataBytes)
            throw new InvalidDataException("Workspace metadata size is invalid.");

        var json = await File.ReadAllTextAsync(_metadataFilePath, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<NodalOsPersistedWorkspaceSelection>(json, JsonOptions);
    }

    private async ValueTask WriteDocumentAsync(
        NodalOsPersistedWorkspaceSelection document,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_metadataFilePath)
            ?? throw new InvalidOperationException("Workspace metadata directory is unavailable.");
        Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(document, JsonOptions);
        if (json.Length > MaximumMetadataBytes)
            throw new InvalidDataException("Workspace metadata exceeds the bounded size.");

        var tempPath = $"{_metadataFilePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await File.WriteAllTextAsync(
                    tempPath,
                    json,
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                    cancellationToken)
                .ConfigureAwait(false);
            File.Move(tempPath, _metadataFilePath, overwrite: true);
            TryHide(_metadataFilePath);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    private async ValueTask<SecretLease?> OpenRootLeaseAsync(
        SecretReference reference,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _rootReferenceStore.OpenAsync(reference, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private async ValueTask BestEffortDeleteSecretAsync(SecretReference reference)
    {
        try
        {
            await _rootReferenceStore.DeleteAsync(reference).ConfigureAwait(false);
        }
        catch
        {
        }
    }

    private NodalOsWorkspaceLocalModel BuildWorkspace(
        string workspaceId,
        string displayName,
        BoundedWorkspaceScanResult scan,
        BoundedWorkspacePlanningProjectionResult planning,
        DateTimeOffset now)
    {
        var shortFingerprint = scan.RootFingerprint[..16];
        return new NodalOsWorkspaceLocalModel
        {
            WorkspaceId = workspaceId,
            DisplayNameRedacted = displayName,
            DescriptionRedacted = "Selected local workspace with bounded read-only evidence and reviewed planning context.",
            LocalRootPathRedacted = $"local-workspace://{shortFingerprint}",
            RootPathFingerprint = scan.RootFingerprint,
            Status = NodalOsWorkspaceStatus.ActiveReadOnly,
            PrivacyMode = NodalOsWorkspacePrivacyMode.LocalOnly,
            ReadOnlyPreview = true,
            RuntimeExecutionAllowed = false,
            CloudSyncAllowed = false,
            LlmProviderCallsAllowed = false,
            CanAuthorizeExecution = false,
            PathJailBindingId = $"jail-{shortFingerprint}",
            ActiveMissionRefs = [],
            EvidenceRefs = [Evidence(scan)],
            TimelineRefs = [$"timeline:workspace-selection:{shortFingerprint}"],
            UiStateRef = "mission-control:workspace-selection",
            ImportWizardRef = "workspace-selection:v1",
            GuardrailSummaryRedacted =
            [
                "Workspace root is protected outside the product surface.",
                "Selection performs bounded reads only and never mutates workspace files.",
                "Cloud, external network, provider calls and product authority remain disabled."
            ],
            AllowedCapabilitiesRedacted = ["filesystem.read", "workspace.scan.bounded", "planning.preview"],
            DisabledCapabilitiesRedacted = ["filesystem.write", "terminal.execute", "network", "cloud sync", "product authority"],
            NextSafeStepsRedacted = planning.MissionPlan?.Steps.Select(step => step.Intent).Take(8).ToArray()
                ?? ["Review the selected workspace before creating a mission."],
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private string ResolveDisplayName(string canonicalRoot, string? requestedDisplayName)
    {
        var candidate = string.IsNullOrWhiteSpace(requestedDisplayName)
            ? Path.GetFileName(canonicalRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            : requestedDisplayName;
        candidate = SafeRuntimeText.Sanitize(candidate, 80);
        if (candidate.Length == 0 || _redaction.ContainsSensitiveContent(candidate))
            return "Selected Local Workspace";
        return candidate;
    }

    private static NodalOsEvidenceBridgeRef Evidence(BoundedWorkspaceScanResult scan) => new()
    {
        EvidenceId = $"evidence:workspace-selection:{scan.EvidenceDigest}",
        Kind = "workspace-selection-digest",
        Ref = null,
        Hash = scan.EvidenceDigest,
        SourceKind = NodalOsEvidenceBridgeSourceKind.AgentOperation,
        UseKind = NodalOsEvidenceBridgeUseKind.VerificationSupport,
        Authority = NodalOsEvidenceBridgeAuthority.SupportsVerificationOnly,
        Sensitivity = NodalOsEvidenceSensitivity.NonSensitive,
        RedactionState = NodalOsEvidenceRedactionState.NotRequired,
        LedgerRef = $"workspace-selection:{scan.RootFingerprint[..16]}",
        Provenance = "Explicit local workspace selection validated through the bounded read-only scanner.",
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static NodalOsWorkspaceSelectionScanSummary BuildScanSummary(
        BoundedWorkspaceScanResult scan,
        BoundedWorkspacePlanningProjectionResult planning,
        DateTimeOffset validatedAt) => new(
        ScanDecision: scan.Decision.ToString(),
        FilesRead: scan.FilesRead,
        FilesSkipped: scan.FilesSkipped,
        TotalBytesRead: scan.TotalBytesRead,
        Truncated: scan.Truncated,
        ExtensionCounts: scan.ExtensionCounts,
        EvidenceDigest: scan.EvidenceDigest,
        PlanDecision: planning.Decision,
        PlanSteps: planning.MissionPlan?.Steps.Select(step => step.Intent).Take(20).ToArray() ?? [],
        ReviewBlockers: planning.Blockers.Take(20).ToArray(),
        ValidatedAt: validatedAt);

    private static NodalOsWorkspaceSelectionSnapshot Success(
        NodalOsPersistedWorkspaceSelection document,
        BoundedWorkspaceScanResult scan,
        BoundedWorkspacePlanningProjectionResult planning,
        bool rehydrated,
        bool appConfigurationMutated,
        DateTimeOffset validatedAt) => new(
        Decision: rehydrated
            ? "GO_REAL_LOCAL_WORKSPACE_REHYDRATED"
            : "GO_REAL_LOCAL_WORKSPACE_SELECTED_AND_PERSISTED",
        Accepted: true,
        State: NodalOsWorkspaceSelectionState.Ready,
        Workspace: document.Workspace,
        WorkspaceId: document.Workspace.WorkspaceId,
        DisplayNameRedacted: document.Workspace.DisplayNameRedacted,
        RootPathHintRedacted: document.Workspace.LocalRootPathRedacted,
        RootPathFingerprint: document.Workspace.RootPathFingerprint,
        PathJailBindingId: document.Workspace.PathJailBindingId,
        Persisted: true,
        Rehydrated: rehydrated,
        ScanDecision: scan.Decision.ToString(),
        FilesRead: scan.FilesRead,
        FilesSkipped: scan.FilesSkipped,
        TotalBytesRead: scan.TotalBytesRead,
        Truncated: scan.Truncated,
        ExtensionCounts: scan.ExtensionCounts,
        EvidenceDigest: scan.EvidenceDigest,
        PlanDecision: planning.Decision,
        PlanSteps: planning.MissionPlan?.Steps.Select(step => step.Intent).Take(20).ToArray() ?? [],
        ReviewBlockers: planning.Blockers.Take(20).ToArray(),
        SelectedAt: document.SelectedAt,
        LastValidatedAt: validatedAt,
        RealFilesystemRead: scan.RealFilesystemRead,
        WorkspaceFilesystemMutated: false,
        AppConfigurationMutated: appConfigurationMutated,
        NetworkUsed: false,
        SecretsExcluded: scan.SecretsExcluded,
        ProductAuthorityGranted: false);

    private static NodalOsWorkspaceSelectionSnapshot NotConfigured(bool appConfigurationMutated = false) => new(
        Decision: "WORKSPACE_SELECTION_REQUIRED",
        Accepted: false,
        State: NodalOsWorkspaceSelectionState.NotConfigured,
        Workspace: null,
        WorkspaceId: null,
        DisplayNameRedacted: null,
        RootPathHintRedacted: null,
        RootPathFingerprint: null,
        PathJailBindingId: null,
        Persisted: false,
        Rehydrated: false,
        ScanDecision: "NotStarted",
        FilesRead: 0,
        FilesSkipped: 0,
        TotalBytesRead: 0,
        Truncated: false,
        ExtensionCounts: new Dictionary<string, int>(),
        EvidenceDigest: string.Empty,
        PlanDecision: "WORKSPACE_SELECTION_REQUIRED",
        PlanSteps: [],
        ReviewBlockers: ["Select a local workspace to begin bounded understanding."],
        SelectedAt: null,
        LastValidatedAt: null,
        RealFilesystemRead: false,
        WorkspaceFilesystemMutated: false,
        AppConfigurationMutated: appConfigurationMutated,
        NetworkUsed: false,
        SecretsExcluded: true,
        ProductAuthorityGranted: false);

    private static NodalOsWorkspaceSelectionSnapshot FromRejectedScan(
        BoundedWorkspaceScanResult scan,
        bool appConfigurationMutated,
        bool persisted = false,
        NodalOsWorkspaceLocalModel? workspace = null) => new(
        Decision: "BLOCKED_WORKSPACE_SELECTION_SCAN_REJECTED",
        Accepted: false,
        State: NodalOsWorkspaceSelectionState.InvalidRoot,
        Workspace: workspace,
        WorkspaceId: workspace?.WorkspaceId,
        DisplayNameRedacted: workspace?.DisplayNameRedacted,
        RootPathHintRedacted: workspace?.LocalRootPathRedacted,
        RootPathFingerprint: workspace?.RootPathFingerprint,
        PathJailBindingId: workspace?.PathJailBindingId,
        Persisted: persisted,
        Rehydrated: false,
        ScanDecision: scan.Decision.ToString(),
        FilesRead: scan.FilesRead,
        FilesSkipped: scan.FilesSkipped,
        TotalBytesRead: scan.TotalBytesRead,
        Truncated: scan.Truncated,
        ExtensionCounts: scan.ExtensionCounts,
        EvidenceDigest: scan.EvidenceDigest,
        PlanDecision: "BLOCKED_WORKSPACE_SELECTION_SCAN_REJECTED",
        PlanSteps: [],
        ReviewBlockers: scan.Findings.DefaultIfEmpty("Workspace root was not accepted.").Take(20).ToArray(),
        SelectedAt: null,
        LastValidatedAt: DateTimeOffset.UtcNow,
        RealFilesystemRead: scan.RealFilesystemRead,
        WorkspaceFilesystemMutated: false,
        AppConfigurationMutated: appConfigurationMutated,
        NetworkUsed: false,
        SecretsExcluded: scan.SecretsExcluded,
        ProductAuthorityGranted: false);

    private static NodalOsWorkspaceSelectionSnapshot Failure(
        string decision,
        NodalOsWorkspaceSelectionState state,
        string blocker,
        BoundedWorkspaceScanResult? scan = null,
        bool persisted = false,
        NodalOsWorkspaceLocalModel? workspace = null) =>
        Failure(decision, state, [blocker], scan, persisted, workspace);

    private static NodalOsWorkspaceSelectionSnapshot Failure(
        string decision,
        NodalOsWorkspaceSelectionState state,
        IReadOnlyList<string> blockers,
        BoundedWorkspaceScanResult? scan = null,
        bool persisted = false,
        NodalOsWorkspaceLocalModel? workspace = null) => new(
        Decision: decision,
        Accepted: false,
        State: state,
        Workspace: workspace,
        WorkspaceId: workspace?.WorkspaceId,
        DisplayNameRedacted: workspace?.DisplayNameRedacted,
        RootPathHintRedacted: workspace?.LocalRootPathRedacted,
        RootPathFingerprint: workspace?.RootPathFingerprint,
        PathJailBindingId: workspace?.PathJailBindingId,
        Persisted: persisted,
        Rehydrated: false,
        ScanDecision: scan?.Decision.ToString() ?? "FailedClosed",
        FilesRead: scan?.FilesRead ?? 0,
        FilesSkipped: scan?.FilesSkipped ?? 0,
        TotalBytesRead: scan?.TotalBytesRead ?? 0,
        Truncated: scan?.Truncated ?? false,
        ExtensionCounts: scan?.ExtensionCounts ?? new Dictionary<string, int>(),
        EvidenceDigest: scan?.EvidenceDigest ?? string.Empty,
        PlanDecision: decision,
        PlanSteps: [],
        ReviewBlockers: blockers.Select(value => SafeRuntimeText.Sanitize(value, 240)).Distinct(StringComparer.Ordinal).Take(20).ToArray(),
        SelectedAt: null,
        LastValidatedAt: null,
        RealFilesystemRead: scan?.RealFilesystemRead ?? false,
        WorkspaceFilesystemMutated: false,
        AppConfigurationMutated: false,
        NetworkUsed: false,
        SecretsExcluded: scan?.SecretsExcluded ?? true,
        ProductAuthorityGranted: false);

    private static bool SameReference(SecretReference left, SecretReference right) =>
        string.Equals(left.StoreId, right.StoreId, StringComparison.Ordinal) &&
        string.Equals(left.SecretId, right.SecretId, StringComparison.Ordinal);

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
}
