using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace OneBrain.Core.Approval;

public enum ProductLedgerLocalApprovalOperatorDecisionKind
{
    Approve,
    Reject,
    RequestChanges
}

public enum ProductLedgerLocalApprovalDecisionStoreDecision
{
    Rejected,
    PersistedLocalOnly,
    IdempotentReplay,
    LoadedLocalOnly
}

public enum ProductLedgerLocalApprovalDecisionState
{
    PendingOperatorDecision,
    ApprovedLocalOnly,
    RejectedLocalOnly,
    ChangesRequestedLocalOnly,
    Invalid
}

public enum ProductLedgerLocalApprovalDecisionBlocker
{
    MissingRequest,
    MissingExplicitLocalOnlyStatePersistenceScope,
    MissingCandidate,
    CandidateRejected,
    CandidateNotLocalOnly,
    CandidateNotInternalOnly,
    CandidateNotDefaultOff,
    CandidateNotFailClosed,
    CandidateAllowsPublicUiAction,
    CandidateAllowsProductCommandHandler,
    CandidateAllowsProductiveServiceRegistration,
    CandidatePerformedPhysicalExport,
    CandidatePerformedFileWrite,
    CandidateAllowsProviderCloudNetwork,
    CandidateAllowsDbMigration,
    CandidateAllowsKmsWormExternalTrust,
    CandidateAllowsLiveAutomation,
    CandidateClaimsReleaseCommercial,
    MissingApprovalId,
    MissingCandidateEvidenceHash,
    CandidateEvidenceHashMismatch,
    MissingOperatorDecision,
    MissingEvidenceReferences,
    UnsafeOperatorNote,
    ClaimsPublicUiAction,
    ClaimsProductCommandExecution,
    ClaimsProductCommandHandler,
    ClaimsProductiveServiceRegistration,
    ClaimsPhysicalExport,
    ClaimsFileWriteOutsideApprovalStateStore,
    ClaimsArbitraryPathInput,
    ClaimsProviderCloudNetwork,
    ClaimsDbMigration,
    ClaimsKmsWormExternalTrust,
    ClaimsBrowserCdpWcuOcrRecipesLive,
    ClaimsPilotRun,
    ClaimsReleaseCommercial,
    StoreBoundaryRejected,
    StoreTamperedOrCorrupt,
    ExistingDecisionConflict
}

public sealed record ProductLedgerLocalApprovalDecisionStateStoreOptions(
    string StoreRootPath,
    bool ExplicitLocalOnlyStateStore,
    bool AllowsArbitraryPathInput,
    bool AllowsExport,
    bool AllowsNetwork,
    bool AllowsDb,
    bool AllowsReleaseCommercial);

public sealed record ProductLedgerLocalApprovalDecisionStateRequest(
    bool ExplicitLocalOnlyStatePersistenceScope,
    string? ApprovalId,
    ProductLedgerLocalApprovalExecutionResult? CandidateResult,
    string? CandidateEvidenceHash,
    string? CurrentEvidenceHash,
    ProductLedgerLocalApprovalOperatorDecisionKind? OperatorDecision,
    DateTimeOffset? DecidedAtUtc,
    string? OperatorClassification,
    string? OperatorNote,
    IReadOnlyList<string>? EvidenceReferences,
    bool RequestsPublicUiAction,
    bool RequestsProductCommandExecution,
    bool RequestsProductCommandHandler,
    bool RequestsProductiveServiceRegistration,
    bool RequestsPhysicalExport,
    bool RequestsFileWriteOutsideApprovalStateStore,
    bool ClaimsArbitraryPathInput,
    bool ClaimsProviderCloudNetwork,
    bool ClaimsDbMigration,
    bool ClaimsKmsWormExternalTrust,
    bool ClaimsBrowserCdpWcuOcrRecipesLive,
    bool ClaimsPilotRun,
    bool ClaimsReleaseCommercial);

public sealed record ProductLedgerLocalApprovalDecisionSnapshot(
    ProductLedgerLocalApprovalDecisionStoreDecision Decision,
    ProductLedgerLocalApprovalDecisionState State,
    IReadOnlyList<ProductLedgerLocalApprovalDecisionBlocker> Blockers,
    string ApprovalId,
    string CandidateActionKind,
    string CandidateEvidenceHash,
    string CandidateEvidenceHashPrefix,
    string DecisionHashPrefix,
    string OperatorDecision,
    string OperatorClassification,
    string RedactedOperatorNote,
    IReadOnlyList<string> EvidenceReferences,
    bool LocalOnly,
    bool InternalOnly,
    bool DefaultOff,
    bool FailClosed,
    bool ProductCommandExecuted,
    bool PublicUiActionAvailable,
    bool ProductCommandHandlerAvailable,
    bool ProductiveServiceRegistrationAvailable,
    bool PhysicalExportCreated,
    bool FileWriteOutsideApprovalStateStorePerformed,
    bool ProviderCloudNetworkAvailable,
    bool DbMigrationAvailable,
    bool KmsWormExternalTrustAvailable,
    bool BrowserCdpWcuOcrRecipesLiveAvailable,
    bool PilotRunAvailable,
    bool ReleaseCommercialReady,
    string StatusText)
{
    public static ProductLedgerLocalApprovalDecisionSnapshot PendingPreviewOnly { get; } =
        new(
            Decision: ProductLedgerLocalApprovalDecisionStoreDecision.Rejected,
            State: ProductLedgerLocalApprovalDecisionState.PendingOperatorDecision,
            Blockers: [],
            ApprovalId: "approval-state.pending-preview-only",
            CandidateActionKind: ProductLedgerInternalCommandKind.ViewLedgerReadiness.ToString(),
            CandidateEvidenceHash: string.Empty,
            CandidateEvidenceHashPrefix: "none",
            DecisionHashPrefix: "none",
            OperatorDecision: "PendingOperatorDecision",
            OperatorClassification: "local-internal-operator",
            RedactedOperatorNote: "No operator decision persisted yet.",
            EvidenceReferences: ["product-ledger-local-approval-execution-final-local-only-readiness-packet"],
            LocalOnly: true,
            InternalOnly: true,
            DefaultOff: true,
            FailClosed: true,
            ProductCommandExecuted: false,
            PublicUiActionAvailable: false,
            ProductCommandHandlerAvailable: false,
            ProductiveServiceRegistrationAvailable: false,
            PhysicalExportCreated: false,
            FileWriteOutsideApprovalStateStorePerformed: false,
            ProviderCloudNetworkAvailable: false,
            DbMigrationAvailable: false,
            KmsWormExternalTrustAvailable: false,
            BrowserCdpWcuOcrRecipesLiveAvailable: false,
            PilotRunAvailable: false,
            ReleaseCommercialReady: false,
            StatusText: ProductLedgerLocalApprovalDecisionStateStore.PendingStatus);
}

public sealed class ProductLedgerLocalApprovalDecisionStateStore
{
    public const string PendingStatus =
        "PRODUCT_LEDGER_LOCAL_APPROVAL_DECISION_PENDING LOCAL_ONLY INTERNAL_ONLY DEFAULT_OFF FAIL_CLOSED NO_COMMAND_EXECUTION NO_PUBLIC_UI_ACTION NO_PRODUCT_COMMAND_HANDLER NO_EXPORT NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public const string PersistedStatus =
        "PRODUCT_LEDGER_LOCAL_APPROVAL_DECISION_STATE_PERSISTED_LOCAL_ONLY INTERNAL_ONLY DEFAULT_OFF FAIL_CLOSED NO_COMMAND_EXECUTION NO_PUBLIC_UI_ACTION NO_PRODUCT_COMMAND_HANDLER NO_EXPORT NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public const string RejectedStatus =
        "PRODUCT_LEDGER_LOCAL_APPROVAL_DECISION_STATE_REJECTED FAIL_CLOSED NO_COMMAND_EXECUTION NO_PUBLIC_UI_ACTION NO_PRODUCT_COMMAND_HANDLER NO_EXPORT NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    private const string StoreFileName = "product-ledger-local-approval-state.json";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    static ProductLedgerLocalApprovalDecisionStateStore()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    }

    private static readonly Regex EmailLike = new(
        @"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex WindowsPathLike = new(
        @"(?i)\b([a-z]:\\|\\\\)[^\s""'<>|]+");

    private readonly ProductLedgerLocalApprovalDecisionStateStoreOptions options;

    public ProductLedgerLocalApprovalDecisionStateStore(
        ProductLedgerLocalApprovalDecisionStateStoreOptions options)
    {
        this.options = options;
    }

    public ProductLedgerLocalApprovalDecisionSnapshot Persist(
        ProductLedgerLocalApprovalDecisionStateRequest? request)
    {
        var blockers = ValidateRequest(request);
        if (blockers.Count > 0 || request is null)
        {
            return SnapshotRejected(blockers);
        }

        var envelope = EnvelopeFrom(request);
        var existing = File.Exists(StateFilePath()) ? ReadEnvelope() : null;
        if (existing?.Decision == ProductLedgerLocalApprovalDecisionStoreDecision.LoadedLocalOnly)
        {
            if (SameDecision(existing, envelope))
            {
                return existing with
                {
                    Decision = ProductLedgerLocalApprovalDecisionStoreDecision.IdempotentReplay,
                    StatusText = PersistedStatus
                };
            }

            return existing with
            {
                Decision = ProductLedgerLocalApprovalDecisionStoreDecision.Rejected,
                State = ProductLedgerLocalApprovalDecisionState.Invalid,
                Blockers = [ProductLedgerLocalApprovalDecisionBlocker.ExistingDecisionConflict],
                StatusText = RejectedStatus
            };
        }

        if (existing?.Blockers.Count > 0
            && existing.Blockers.Contains(ProductLedgerLocalApprovalDecisionBlocker.StoreTamperedOrCorrupt))
        {
            return existing;
        }

        Directory.CreateDirectory(options.StoreRootPath);
        File.WriteAllText(StateFilePath(), JsonSerializer.Serialize(envelope, JsonOptions), Encoding.UTF8);
        return SnapshotFrom(envelope, ProductLedgerLocalApprovalDecisionStoreDecision.PersistedLocalOnly, []);
    }

    public ProductLedgerLocalApprovalDecisionSnapshot Read()
    {
        if (!BoundaryAllowed())
        {
            return SnapshotRejected([ProductLedgerLocalApprovalDecisionBlocker.StoreBoundaryRejected]);
        }

        if (!File.Exists(StateFilePath()))
        {
            return ProductLedgerLocalApprovalDecisionSnapshot.PendingPreviewOnly;
        }

        return ReadEnvelope();
    }

    private IReadOnlyList<ProductLedgerLocalApprovalDecisionBlocker> ValidateRequest(
        ProductLedgerLocalApprovalDecisionStateRequest? request)
    {
        var blockers = new List<ProductLedgerLocalApprovalDecisionBlocker>();
        if (request is null)
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.MissingRequest);
            return blockers;
        }

        if (!BoundaryAllowed())
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.StoreBoundaryRejected);
        }

        if (!request.ExplicitLocalOnlyStatePersistenceScope)
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.MissingExplicitLocalOnlyStatePersistenceScope);
        }

        if (string.IsNullOrWhiteSpace(request.ApprovalId))
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.MissingApprovalId);
        }

        if (string.IsNullOrWhiteSpace(request.CandidateEvidenceHash)
            || string.IsNullOrWhiteSpace(request.CurrentEvidenceHash))
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.MissingCandidateEvidenceHash);
        }
        else if (!string.Equals(
            request.CandidateEvidenceHash.Trim(),
            request.CurrentEvidenceHash.Trim(),
            StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.CandidateEvidenceHashMismatch);
        }

        if (request.OperatorDecision is null)
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.MissingOperatorDecision);
        }

        if (request.EvidenceReferences is null || request.EvidenceReferences.Count == 0
            || request.EvidenceReferences.Any(string.IsNullOrWhiteSpace))
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.MissingEvidenceReferences);
        }

        if (ContainsSecretLike(request.OperatorNote))
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.UnsafeOperatorNote);
        }

        AddCandidateBlockers(request.CandidateResult, blockers);
        AddAuthorityBlockers(request, blockers);
        return blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
    }

    private static void AddCandidateBlockers(
        ProductLedgerLocalApprovalExecutionResult? candidate,
        List<ProductLedgerLocalApprovalDecisionBlocker> blockers)
    {
        if (candidate is null)
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.MissingCandidate);
            return;
        }

        if (candidate.Decision != ProductLedgerLocalApprovalExecutionDecision.CompletedReadOnlyInMemory
            || candidate.Blockers.Count > 0)
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.CandidateRejected);
        }

        if (!candidate.LocalOnly)
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.CandidateNotLocalOnly);
        }

        if (!candidate.InternalOnly)
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.CandidateNotInternalOnly);
        }

        if (!candidate.DefaultOff)
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.CandidateNotDefaultOff);
        }

        if (!candidate.FailClosed)
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.CandidateNotFailClosed);
        }

        if (candidate.PublicUiActionAvailable)
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.CandidateAllowsPublicUiAction);
        }

        if (candidate.ProductCommandHandlerAvailable)
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.CandidateAllowsProductCommandHandler);
        }

        if (candidate.ProductiveServiceRegistrationAvailable)
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.CandidateAllowsProductiveServiceRegistration);
        }

        if (candidate.PhysicalExportCreated)
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.CandidatePerformedPhysicalExport);
        }

        if (candidate.FileWritePerformed)
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.CandidatePerformedFileWrite);
        }

        if (candidate.ProviderCloudNetworkAvailable)
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.CandidateAllowsProviderCloudNetwork);
        }

        if (candidate.DbMigrationAvailable)
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.CandidateAllowsDbMigration);
        }

        if (candidate.KmsWormExternalTrustAvailable)
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.CandidateAllowsKmsWormExternalTrust);
        }

        if (candidate.BrowserCdpWcuOcrRecipesLiveAvailable)
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.CandidateAllowsLiveAutomation);
        }

        if (candidate.ReleaseCommercialReady)
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.CandidateClaimsReleaseCommercial);
        }
    }

    private static void AddAuthorityBlockers(
        ProductLedgerLocalApprovalDecisionStateRequest request,
        List<ProductLedgerLocalApprovalDecisionBlocker> blockers)
    {
        if (request.RequestsPublicUiAction)
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.ClaimsPublicUiAction);
        }

        if (request.RequestsProductCommandExecution)
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.ClaimsProductCommandExecution);
        }

        if (request.RequestsProductCommandHandler)
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.ClaimsProductCommandHandler);
        }

        if (request.RequestsProductiveServiceRegistration)
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.ClaimsProductiveServiceRegistration);
        }

        if (request.RequestsPhysicalExport)
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.ClaimsPhysicalExport);
        }

        if (request.RequestsFileWriteOutsideApprovalStateStore)
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.ClaimsFileWriteOutsideApprovalStateStore);
        }

        if (request.ClaimsArbitraryPathInput)
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.ClaimsArbitraryPathInput);
        }

        if (request.ClaimsProviderCloudNetwork)
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.ClaimsProviderCloudNetwork);
        }

        if (request.ClaimsDbMigration)
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.ClaimsDbMigration);
        }

        if (request.ClaimsKmsWormExternalTrust)
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.ClaimsKmsWormExternalTrust);
        }

        if (request.ClaimsBrowserCdpWcuOcrRecipesLive)
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.ClaimsBrowserCdpWcuOcrRecipesLive);
        }

        if (request.ClaimsPilotRun)
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.ClaimsPilotRun);
        }

        if (request.ClaimsReleaseCommercial)
        {
            blockers.Add(ProductLedgerLocalApprovalDecisionBlocker.ClaimsReleaseCommercial);
        }
    }

    private ProductLedgerLocalApprovalDecisionEnvelope EnvelopeFrom(
        ProductLedgerLocalApprovalDecisionStateRequest request)
    {
        var state = request.OperatorDecision switch
        {
            ProductLedgerLocalApprovalOperatorDecisionKind.Approve => ProductLedgerLocalApprovalDecisionState.ApprovedLocalOnly,
            ProductLedgerLocalApprovalOperatorDecisionKind.Reject => ProductLedgerLocalApprovalDecisionState.RejectedLocalOnly,
            ProductLedgerLocalApprovalOperatorDecisionKind.RequestChanges => ProductLedgerLocalApprovalDecisionState.ChangesRequestedLocalOnly,
            _ => ProductLedgerLocalApprovalDecisionState.Invalid
        };
        var candidate = request.CandidateResult!;
        var envelope = new ProductLedgerLocalApprovalDecisionEnvelope(
            SchemaVersion: 1,
            State: state,
            ApprovalId: request.ApprovalId!.Trim(),
            CandidateActionKind: candidate.CandidateActionKind.ToString(),
            CandidateEvidenceHash: request.CandidateEvidenceHash!.Trim(),
            OperatorDecision: request.OperatorDecision!.Value.ToString(),
            DecidedAtUtc: request.DecidedAtUtc ?? DateTimeOffset.UnixEpoch,
            OperatorClassification: SafeTrim(request.OperatorClassification, "local-internal-operator"),
            RedactedOperatorNote: Redact(request.OperatorNote),
            EvidenceReferences: request.EvidenceReferences!
                .Select(evidence => evidence.Trim())
                .OrderBy(evidence => evidence, StringComparer.Ordinal)
                .ToArray(),
            LocalOnly: true,
            InternalOnly: true,
            DefaultOff: true,
            FailClosed: true,
            ProductCommandExecuted: false,
            PublicUiActionAvailable: false,
            ProductCommandHandlerAvailable: false,
            ProductiveServiceRegistrationAvailable: false,
            PhysicalExportCreated: false,
            FileWriteOutsideApprovalStateStorePerformed: false,
            ProviderCloudNetworkAvailable: false,
            DbMigrationAvailable: false,
            KmsWormExternalTrustAvailable: false,
            BrowserCdpWcuOcrRecipesLiveAvailable: false,
            PilotRunAvailable: false,
            ReleaseCommercialReady: false,
            DecisionHash: string.Empty);
        return envelope with { DecisionHash = Hash(envelope with { DecisionHash = string.Empty }) };
    }

    private ProductLedgerLocalApprovalDecisionSnapshot ReadEnvelope()
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<ProductLedgerLocalApprovalDecisionEnvelope>(
                File.ReadAllText(StateFilePath(), Encoding.UTF8),
                JsonOptions);
            if (envelope is null)
            {
                return SnapshotRejected([ProductLedgerLocalApprovalDecisionBlocker.StoreTamperedOrCorrupt]);
            }

            var actual = Hash(envelope with { DecisionHash = string.Empty });
            return string.Equals(actual, envelope.DecisionHash, StringComparison.Ordinal)
                ? SnapshotFrom(envelope, ProductLedgerLocalApprovalDecisionStoreDecision.LoadedLocalOnly, [])
                : SnapshotRejected([ProductLedgerLocalApprovalDecisionBlocker.StoreTamperedOrCorrupt]);
        }
        catch (JsonException)
        {
            return SnapshotRejected([ProductLedgerLocalApprovalDecisionBlocker.StoreTamperedOrCorrupt]);
        }
        catch (IOException)
        {
            return SnapshotRejected([ProductLedgerLocalApprovalDecisionBlocker.StoreTamperedOrCorrupt]);
        }
        catch (UnauthorizedAccessException)
        {
            return SnapshotRejected([ProductLedgerLocalApprovalDecisionBlocker.StoreTamperedOrCorrupt]);
        }
    }

    private static bool SameDecision(
        ProductLedgerLocalApprovalDecisionSnapshot existing,
        ProductLedgerLocalApprovalDecisionEnvelope envelope) =>
        string.Equals(existing.ApprovalId, envelope.ApprovalId, StringComparison.Ordinal)
        && string.Equals(existing.CandidateActionKind, envelope.CandidateActionKind, StringComparison.Ordinal)
        && string.Equals(existing.CandidateEvidenceHash, envelope.CandidateEvidenceHash, StringComparison.Ordinal)
        && string.Equals(existing.OperatorDecision, envelope.OperatorDecision, StringComparison.Ordinal)
        && string.Equals(existing.DecisionHashPrefix, Prefix(envelope.DecisionHash), StringComparison.Ordinal);

    private ProductLedgerLocalApprovalDecisionSnapshot SnapshotFrom(
        ProductLedgerLocalApprovalDecisionEnvelope envelope,
        ProductLedgerLocalApprovalDecisionStoreDecision decision,
        IReadOnlyList<ProductLedgerLocalApprovalDecisionBlocker> blockers) =>
        new(
            Decision: blockers.Count == 0 ? decision : ProductLedgerLocalApprovalDecisionStoreDecision.Rejected,
            State: blockers.Count == 0 ? envelope.State : ProductLedgerLocalApprovalDecisionState.Invalid,
            Blockers: blockers.OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray(),
            ApprovalId: envelope.ApprovalId,
            CandidateActionKind: envelope.CandidateActionKind,
            CandidateEvidenceHash: envelope.CandidateEvidenceHash,
            CandidateEvidenceHashPrefix: Prefix(envelope.CandidateEvidenceHash),
            DecisionHashPrefix: Prefix(envelope.DecisionHash),
            OperatorDecision: envelope.OperatorDecision,
            OperatorClassification: envelope.OperatorClassification,
            RedactedOperatorNote: envelope.RedactedOperatorNote,
            EvidenceReferences: envelope.EvidenceReferences,
            LocalOnly: envelope.LocalOnly,
            InternalOnly: envelope.InternalOnly,
            DefaultOff: envelope.DefaultOff,
            FailClosed: envelope.FailClosed,
            ProductCommandExecuted: envelope.ProductCommandExecuted,
            PublicUiActionAvailable: envelope.PublicUiActionAvailable,
            ProductCommandHandlerAvailable: envelope.ProductCommandHandlerAvailable,
            ProductiveServiceRegistrationAvailable: envelope.ProductiveServiceRegistrationAvailable,
            PhysicalExportCreated: envelope.PhysicalExportCreated,
            FileWriteOutsideApprovalStateStorePerformed: envelope.FileWriteOutsideApprovalStateStorePerformed,
            ProviderCloudNetworkAvailable: envelope.ProviderCloudNetworkAvailable,
            DbMigrationAvailable: envelope.DbMigrationAvailable,
            KmsWormExternalTrustAvailable: envelope.KmsWormExternalTrustAvailable,
            BrowserCdpWcuOcrRecipesLiveAvailable: envelope.BrowserCdpWcuOcrRecipesLiveAvailable,
            PilotRunAvailable: envelope.PilotRunAvailable,
            ReleaseCommercialReady: envelope.ReleaseCommercialReady,
            StatusText: blockers.Count == 0 ? PersistedStatus : RejectedStatus);

    private static ProductLedgerLocalApprovalDecisionSnapshot SnapshotRejected(
        IReadOnlyList<ProductLedgerLocalApprovalDecisionBlocker> blockers) =>
        ProductLedgerLocalApprovalDecisionSnapshot.PendingPreviewOnly with
        {
            Decision = ProductLedgerLocalApprovalDecisionStoreDecision.Rejected,
            State = ProductLedgerLocalApprovalDecisionState.Invalid,
            Blockers = blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray(),
            StatusText = RejectedStatus
        };

    private bool BoundaryAllowed()
    {
        if (!options.ExplicitLocalOnlyStateStore
            || options.AllowsArbitraryPathInput
            || options.AllowsExport
            || options.AllowsNetwork
            || options.AllowsDb
            || options.AllowsReleaseCommercial
            || string.IsNullOrWhiteSpace(options.StoreRootPath))
        {
            return false;
        }

        var full = Path.GetFullPath(options.StoreRootPath);
        return full.IndexOf("..", StringComparison.Ordinal) < 0
            && !Path.GetPathRoot(full)!.Equals(full, StringComparison.OrdinalIgnoreCase);
    }

    private string StateFilePath() =>
        Path.Combine(Path.GetFullPath(options.StoreRootPath), StoreFileName);

    private static string Hash(ProductLedgerLocalApprovalDecisionEnvelope envelope)
    {
        var json = JsonSerializer.Serialize(envelope, JsonOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }

    private static string Redact(string? value)
    {
        var safe = SafeTrim(value, "no operator note supplied");
        safe = EmailLike.Replace(safe, "[redacted-email]");
        safe = WindowsPathLike.Replace(safe, "[redacted-path]");
        return safe.Length <= 160 ? safe : safe[..160];
    }

    private static bool ContainsSecretLike(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim();
        return normalized.Contains("password=", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("secret=", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("token=", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("api_key", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("bearer ", StringComparison.OrdinalIgnoreCase);
    }

    private static string SafeTrim(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string Prefix(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "none" : value[..Math.Min(12, value.Length)];

    private sealed record ProductLedgerLocalApprovalDecisionEnvelope(
        int SchemaVersion,
        ProductLedgerLocalApprovalDecisionState State,
        string ApprovalId,
        string CandidateActionKind,
        string CandidateEvidenceHash,
        string OperatorDecision,
        DateTimeOffset DecidedAtUtc,
        string OperatorClassification,
        string RedactedOperatorNote,
        IReadOnlyList<string> EvidenceReferences,
        bool LocalOnly,
        bool InternalOnly,
        bool DefaultOff,
        bool FailClosed,
        bool ProductCommandExecuted,
        bool PublicUiActionAvailable,
        bool ProductCommandHandlerAvailable,
        bool ProductiveServiceRegistrationAvailable,
        bool PhysicalExportCreated,
        bool FileWriteOutsideApprovalStateStorePerformed,
        bool ProviderCloudNetworkAvailable,
        bool DbMigrationAvailable,
        bool KmsWormExternalTrustAvailable,
        bool BrowserCdpWcuOcrRecipesLiveAvailable,
        bool PilotRunAvailable,
        bool ReleaseCommercialReady,
        string DecisionHash);
}
