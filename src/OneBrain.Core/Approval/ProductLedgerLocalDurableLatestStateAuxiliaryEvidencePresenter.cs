namespace OneBrain.Core.Approval;

public enum ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceDecision
{
    Rejected,
    PresentedAuxiliaryEvidenceNotAuthority
}

public enum ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceState
{
    Pending,
    AuxiliaryEvidenceUnavailable,
    AuxiliaryEvidenceVisibleNotAuthority,
    AuxiliaryEvidenceBlocked
}

public enum ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker
{
    MissingRequest,
    MissingExplicitAuxiliaryEvidenceScope,
    NonDevelopmentMode,
    NonLocalMode,
    NonInternalMode,
    QueryOverrideRejected,
    HeaderOverrideRejected,
    BoundaryRejected,
    MissingCandidate,
    CandidateNotValidated,
    CandidateClaimsAuthority,
    CandidateClaimsLiveAuthority,
    CandidateClaimsProductAuthority,
    CandidateClaimsReadPrecedence,
    CandidateClaimsLatestPointer,
    CandidateClaimsLatestPointerOverwrite,
    CandidateClaimsProduction,
    CandidateClaimsPublicProduct,
    CandidateClaimsShellOrSubprocess,
    CandidateClaimsCommandExecution,
    CandidateClaimsProviderCloudNetwork,
    CandidateClaimsDbMigration,
    CandidateClaimsKmsWormExternalTrust,
    CandidateClaimsBrowserCdpWcuOcrRecipesLive,
    CandidateClaimsPilotRun,
    CandidateClaimsReleaseCommercial,
    CandidateClaimsComplianceCustody,
    CandidateClaimsCloudBackedDurability,
    CandidateNotReadOnly,
    CandidateNotEvidenceOnly,
    MissingEvidenceRefs,
    UnsafeMetadata
}

public sealed record ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceOptions(
    bool ExplicitAuxiliaryEvidenceBoundary,
    bool AllowsReadPrecedence,
    bool AllowsLatestPointer,
    bool AllowsLatestPointerOverwrite,
    bool AllowsAuthority,
    bool AllowsProductAuthority,
    bool AllowsPublicProduct,
    bool AllowsProductionRoute,
    bool AllowsShellOrSubprocess,
    bool AllowsCommandExecution,
    bool AllowsNetwork,
    bool AllowsDb,
    bool AllowsKmsWormExternalTrust,
    bool AllowsReleaseCommercial);

public sealed record ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceRequest(
    bool ExplicitAuxiliaryEvidenceScope,
    bool DevelopmentMode,
    bool LocalMode,
    bool InternalMode,
    ProductLedgerLocalDurableLatestStateReaderCandidateResult? SourceReaderCandidate,
    bool QueryOverridePresent,
    bool HeaderOverridePresent);

public sealed record ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceValidation(
    bool CandidateValidated,
    bool ManifestHashValid,
    bool SnapshotHashValid,
    bool SafeRelativePathsOnly,
    bool StaleAware,
    bool TamperDetected,
    bool CorruptionDetected,
    bool NotAuthority);

public sealed record ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceResult(
    ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceDecision Decision,
    ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceState State,
    IReadOnlyList<ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker> Blockers,
    string AuxiliaryEvidenceId,
    DateTimeOffset CreatedAtUtc,
    string SourceReaderCandidateId,
    string SourceManifestId,
    string SourceManifestRelativePath,
    string SourceManifestHashPrefix,
    string SourceManifestCheckpointHashPrefix,
    IReadOnlyList<string> SourceSnapshotIds,
    IReadOnlyList<string> SourceSnapshotRelativePaths,
    IReadOnlyList<string> SourceSnapshotHashPrefixes,
    IReadOnlyList<string> SourceEvidenceRefs,
    ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceValidation Validation,
    string ValidationState,
    string StaleState,
    string TamperState,
    string CorruptionState,
    string Classification,
    IReadOnlyList<string> NegativeFlags,
    bool LocalOnly,
    bool InternalOnly,
    bool DevelopmentOnly,
    bool ReadOnly,
    bool AuxiliaryEvidenceOnly,
    bool Authority,
    bool LiveAuthority,
    bool ProductAuthority,
    bool ReadPrecedence,
    bool LatestPointer,
    bool LatestPointerOverwrite,
    bool ProductionAllowed,
    bool PublicProductAllowed,
    bool ShellAllowed,
    bool CommandExecutionAllowed,
    bool ProviderCloudNetworkAvailable,
    bool DbMigrationAvailable,
    bool KmsWormExternalTrustAvailable,
    bool BrowserCdpWcuOcrRecipesLiveAvailable,
    bool PilotRunAvailable,
    bool ReleaseCommercialReady,
    bool ComplianceCustody,
    bool CloudBackedDurability,
    string SafeNextStep,
    string StatusText)
{
    public static ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceResult Pending { get; } =
        new(
            Decision: ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceDecision.Rejected,
            State: ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceState.Pending,
            Blockers: [],
            AuxiliaryEvidenceId: "durable-latest-state-auxiliary-evidence.pending",
            CreatedAtUtc: DateTimeOffset.UnixEpoch,
            SourceReaderCandidateId: "none",
            SourceManifestId: "none",
            SourceManifestRelativePath: string.Empty,
            SourceManifestHashPrefix: "none",
            SourceManifestCheckpointHashPrefix: "none",
            SourceSnapshotIds: [],
            SourceSnapshotRelativePaths: [],
            SourceSnapshotHashPrefixes: [],
            SourceEvidenceRefs: ["product-ledger-durable-latest-state-auxiliary-evidence-pending"],
            Validation: ProductLedgerLocalDurableLatestStateAuxiliaryEvidencePresenter.PendingValidation,
            ValidationState: "pending",
            StaleState: "stale-aware-pending",
            TamperState: "not-evaluated",
            CorruptionState: "not-evaluated",
            Classification: ProductLedgerLocalDurableLatestStateAuxiliaryEvidencePresenter.Classification,
            NegativeFlags: ProductLedgerLocalDurableLatestStateAuxiliaryEvidencePresenter.NegativeFlags,
            LocalOnly: true,
            InternalOnly: true,
            DevelopmentOnly: true,
            ReadOnly: true,
            AuxiliaryEvidenceOnly: true,
            Authority: false,
            LiveAuthority: false,
            ProductAuthority: false,
            ReadPrecedence: false,
            LatestPointer: false,
            LatestPointerOverwrite: false,
            ProductionAllowed: false,
            PublicProductAllowed: false,
            ShellAllowed: false,
            CommandExecutionAllowed: false,
            ProviderCloudNetworkAvailable: false,
            DbMigrationAvailable: false,
            KmsWormExternalTrustAvailable: false,
            BrowserCdpWcuOcrRecipesLiveAvailable: false,
            PilotRunAvailable: false,
            ReleaseCommercialReady: false,
            ComplianceCustody: false,
            CloudBackedDurability: false,
            SafeNextStep: "explicit-go-required-before-read-precedence-latest-pointer-or-product-authority",
            StatusText: ProductLedgerLocalDurableLatestStateAuxiliaryEvidencePresenter.PendingStatus);
}

public sealed class ProductLedgerLocalDurableLatestStateAuxiliaryEvidencePresenter
{
    public const string Classification =
        "LOCAL_INTERNAL_DEV_ONLY_AUXILIARY_EVIDENCE_NOT_PRECEDENCE_NOT_AUTHORITY";

    public const string ScopeId =
        "LocalDurableLatestStateAuxiliaryEvidenceNotPrecedenceNotAuthority";

    public const string PendingStatus =
        "PRODUCT_LEDGER_DURABLE_LATEST_STATE_AUXILIARY_EVIDENCE_PENDING LOCAL_ONLY INTERNAL_ONLY DEVELOPMENT_ONLY READ_ONLY AUXILIARY_EVIDENCE_ONLY NOT_AUTHORITY NO_READ_PRECEDENCE NO_LATEST_POINTER NOT_PUBLIC_PRODUCT";

    public const string PresentedStatus =
        "PRODUCT_LEDGER_DURABLE_LATEST_STATE_AUXILIARY_EVIDENCE_PRESENTED LOCAL_ONLY INTERNAL_ONLY DEVELOPMENT_ONLY READ_ONLY AUXILIARY_EVIDENCE_ONLY NOT_AUTHORITY NOT_LIVE_AUTHORITY NOT_PRODUCT_AUTHORITY STALE_AWARE TAMPER_AWARE CORRUPTION_AWARE NO_READ_PRECEDENCE NO_LATEST_POINTER NO_LATEST_POINTER_OVERWRITE NO_PUBLIC_PRODUCT_PATH NO_PRODUCTION_ROUTE NO_COMMAND_EXECUTION NO_SHELL_SUBPROCESS NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public const string RejectedStatus =
        "PRODUCT_LEDGER_DURABLE_LATEST_STATE_AUXILIARY_EVIDENCE_REJECTED FAIL_CLOSED READ_ONLY AUXILIARY_EVIDENCE_ONLY NOT_AUTHORITY NO_READ_PRECEDENCE NO_LATEST_POINTER NO_PUBLIC_PRODUCT_PATH NO_PRODUCTION_ROUTE NO_COMMAND_EXECUTION NO_SHELL_SUBPROCESS NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public static ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceValidation PendingValidation { get; } =
        new(
            CandidateValidated: false,
            ManifestHashValid: false,
            SnapshotHashValid: false,
            SafeRelativePathsOnly: false,
            StaleAware: true,
            TamperDetected: false,
            CorruptionDetected: false,
            NotAuthority: true);

    public static IReadOnlyList<string> NegativeFlags { get; } =
    [
        "auxiliary evidence only",
        "not authority",
        "not live authority",
        "not product authority",
        "no read precedence",
        "no latest pointer",
        "no latest pointer overwrite",
        "stale-aware",
        "not public/product",
        "not production",
        "no shell/subprocess",
        "no command execution",
        "no cloud/network/DB",
        "no KMS/WORM/compliance custody",
        "no Browser/CDP/WCU/OCR/Recipes live",
        "no Pilot /run",
        "no release/commercial",
        "read-only"
    ];

    private readonly ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceOptions options;
    private ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceResult? lastEvidence;

    public ProductLedgerLocalDurableLatestStateAuxiliaryEvidencePresenter(
        ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceOptions options)
    {
        this.options = options;
    }

    public ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceResult Read() =>
        lastEvidence ?? ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceResult.Pending;

    public ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceResult Present(
        ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceRequest? request)
    {
        var blockers = new List<ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker>();
        if (request is null)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.MissingRequest);
            return Remember(Rejected(blockers, null));
        }

        AddBoundaryBlockers(request, blockers);
        AddCandidateBlockers(request.SourceReaderCandidate, blockers);

        var result = blockers.Count == 0
            ? Presented(request.SourceReaderCandidate!)
            : Rejected(blockers, request.SourceReaderCandidate);
        return Remember(result);
    }

    private ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceResult Remember(
        ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceResult result)
    {
        lastEvidence = result;
        return result;
    }

    private void AddBoundaryBlockers(
        ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceRequest request,
        List<ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker> blockers)
    {
        if (!request.ExplicitAuxiliaryEvidenceScope)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.MissingExplicitAuxiliaryEvidenceScope);
        }

        if (!request.DevelopmentMode)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.NonDevelopmentMode);
        }

        if (!request.LocalMode)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.NonLocalMode);
        }

        if (!request.InternalMode)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.NonInternalMode);
        }

        if (request.QueryOverridePresent)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.QueryOverrideRejected);
        }

        if (request.HeaderOverridePresent)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.HeaderOverrideRejected);
        }

        if (!options.ExplicitAuxiliaryEvidenceBoundary
            || options.AllowsReadPrecedence
            || options.AllowsLatestPointer
            || options.AllowsLatestPointerOverwrite
            || options.AllowsAuthority
            || options.AllowsProductAuthority
            || options.AllowsPublicProduct
            || options.AllowsProductionRoute
            || options.AllowsShellOrSubprocess
            || options.AllowsCommandExecution
            || options.AllowsNetwork
            || options.AllowsDb
            || options.AllowsKmsWormExternalTrust
            || options.AllowsReleaseCommercial)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.BoundaryRejected);
        }
    }

    private static void AddCandidateBlockers(
        ProductLedgerLocalDurableLatestStateReaderCandidateResult? candidate,
        List<ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker> blockers)
    {
        if (candidate is null)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.MissingCandidate);
            return;
        }

        if (candidate.Decision != ProductLedgerLocalDurableLatestStateReaderCandidateDecision.ValidatedCandidateNotAuthority
            || candidate.State != ProductLedgerLocalDurableLatestStateReaderCandidateState.CandidateValidatedNotAuthority
            || !string.Equals(candidate.Classification, ProductLedgerLocalDurableLatestStateReaderCandidateValidator.Classification, StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.CandidateNotValidated);
        }

        if (!candidate.ReadOnly)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.CandidateNotReadOnly);
        }

        if (!candidate.CandidateEvidenceOnly)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.CandidateNotEvidenceOnly);
        }

        AddClaimBlockers(candidate, blockers);

        if (candidate.SourceEvidenceRefs.Count == 0)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.MissingEvidenceRefs);
        }

        if (!candidate.Validation.SafeRelativePathsOnly
            || HasUnsafeText(candidate.SourceManifestRelativePath)
            || candidate.SourceSnapshotRelativePaths.Any(HasUnsafeText)
            || candidate.SourceEvidenceRefs.Any(HasUnsafeText))
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.UnsafeMetadata);
        }
    }

    private static void AddClaimBlockers(
        ProductLedgerLocalDurableLatestStateReaderCandidateResult candidate,
        List<ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker> blockers)
    {
        if (candidate.Authority)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.CandidateClaimsAuthority);
        }

        if (candidate.LiveAuthority)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.CandidateClaimsLiveAuthority);
        }

        if (candidate.ProductAuthority)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.CandidateClaimsProductAuthority);
        }

        if (candidate.ReadPrecedence)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.CandidateClaimsReadPrecedence);
        }

        if (candidate.LatestPointer)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.CandidateClaimsLatestPointer);
        }

        if (candidate.LatestPointerOverwrite)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.CandidateClaimsLatestPointerOverwrite);
        }

        if (candidate.ProductionAllowed)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.CandidateClaimsProduction);
        }

        if (candidate.PublicProductAllowed)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.CandidateClaimsPublicProduct);
        }

        if (candidate.ShellAllowed)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.CandidateClaimsShellOrSubprocess);
        }

        if (candidate.CommandExecutionAllowed)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.CandidateClaimsCommandExecution);
        }

        if (candidate.ProviderCloudNetworkAvailable)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.CandidateClaimsProviderCloudNetwork);
        }

        if (candidate.DbMigrationAvailable)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.CandidateClaimsDbMigration);
        }

        if (candidate.KmsWormExternalTrustAvailable)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.CandidateClaimsKmsWormExternalTrust);
        }

        if (candidate.BrowserCdpWcuOcrRecipesLiveAvailable)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.CandidateClaimsBrowserCdpWcuOcrRecipesLive);
        }

        if (candidate.PilotRunAvailable)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.CandidateClaimsPilotRun);
        }

        if (candidate.ReleaseCommercialReady)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.CandidateClaimsReleaseCommercial);
        }

        if (candidate.ComplianceCustody)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.CandidateClaimsComplianceCustody);
        }

        if (candidate.CloudBackedDurability)
        {
            blockers.Add(ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker.CandidateClaimsCloudBackedDurability);
        }
    }

    private static ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceResult Presented(
        ProductLedgerLocalDurableLatestStateReaderCandidateResult candidate)
    {
        var evidenceRefs = candidate.SourceEvidenceRefs
            .Append("product-ledger-durable-latest-state-auxiliary-evidence-visible-not-authority")
            .Distinct(StringComparer.Ordinal)
            .OrderBy(evidence => evidence, StringComparer.Ordinal)
            .ToArray();
        return BaseResult(
            decision: ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceDecision.PresentedAuxiliaryEvidenceNotAuthority,
            state: ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceState.AuxiliaryEvidenceVisibleNotAuthority,
            blockers: [],
            candidate: candidate,
            evidenceRefs: evidenceRefs,
            validation: new ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceValidation(
                CandidateValidated: true,
                ManifestHashValid: candidate.Validation.ManifestHashValid,
                SnapshotHashValid: candidate.Validation.SnapshotHashValid,
                SafeRelativePathsOnly: candidate.Validation.SafeRelativePathsOnly,
                StaleAware: candidate.Validation.StaleAware,
                TamperDetected: candidate.Validation.TamperDetected,
                CorruptionDetected: candidate.Validation.CorruptionDetected,
                NotAuthority: true),
            validationState: "candidate-validated-auxiliary-evidence-only",
            staleState: candidate.StaleState,
            tamperState: candidate.TamperState,
            corruptionState: candidate.CorruptionState,
            statusText: PresentedStatus);
    }

    private static ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceResult Rejected(
        IReadOnlyList<ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker> blockers,
        ProductLedgerLocalDurableLatestStateReaderCandidateResult? candidate)
    {
        var distinct = blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
        var evidenceRefs = (candidate?.SourceEvidenceRefs ?? [])
            .Append("product-ledger-durable-latest-state-auxiliary-evidence-rejected")
            .Distinct(StringComparer.Ordinal)
            .OrderBy(evidence => evidence, StringComparer.Ordinal)
            .ToArray();
        return BaseResult(
            decision: ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceDecision.Rejected,
            state: candidate is null
                ? ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceState.AuxiliaryEvidenceUnavailable
                : ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceState.AuxiliaryEvidenceBlocked,
            blockers: distinct,
            candidate: candidate,
            evidenceRefs: evidenceRefs,
            validation: PendingValidation,
            validationState: "rejected-fail-closed",
            staleState: candidate?.StaleState ?? "stale-aware-rejected",
            tamperState: candidate?.TamperState ?? "not-evaluated",
            corruptionState: candidate?.CorruptionState ?? "not-evaluated",
            statusText: RejectedStatus);
    }

    private static ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceResult BaseResult(
        ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceDecision decision,
        ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceState state,
        IReadOnlyList<ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceBlocker> blockers,
        ProductLedgerLocalDurableLatestStateReaderCandidateResult? candidate,
        IReadOnlyList<string> evidenceRefs,
        ProductLedgerLocalDurableLatestStateAuxiliaryEvidenceValidation validation,
        string validationState,
        string staleState,
        string tamperState,
        string corruptionState,
        string statusText) =>
        new(
            Decision: decision,
            State: state,
            Blockers: blockers,
            AuxiliaryEvidenceId: $"durable-latest-state-auxiliary-evidence.{(candidate?.CandidateId ?? "none")}",
            CreatedAtUtc: candidate?.CreatedAtUtc ?? DateTimeOffset.UnixEpoch,
            SourceReaderCandidateId: candidate?.CandidateId ?? "none",
            SourceManifestId: candidate?.SourceManifestId ?? "none",
            SourceManifestRelativePath: candidate?.SourceManifestRelativePath ?? string.Empty,
            SourceManifestHashPrefix: candidate?.SourceManifestHashPrefix ?? "none",
            SourceManifestCheckpointHashPrefix: candidate?.SourceManifestCheckpointHashPrefix ?? "none",
            SourceSnapshotIds: candidate?.SourceSnapshotIds ?? [],
            SourceSnapshotRelativePaths: candidate?.SourceSnapshotRelativePaths ?? [],
            SourceSnapshotHashPrefixes: candidate?.SourceSnapshotHashPrefixes ?? [],
            SourceEvidenceRefs: evidenceRefs,
            Validation: validation,
            ValidationState: validationState,
            StaleState: staleState,
            TamperState: tamperState,
            CorruptionState: corruptionState,
            Classification: Classification,
            NegativeFlags: NegativeFlags,
            LocalOnly: true,
            InternalOnly: true,
            DevelopmentOnly: true,
            ReadOnly: true,
            AuxiliaryEvidenceOnly: true,
            Authority: false,
            LiveAuthority: false,
            ProductAuthority: false,
            ReadPrecedence: false,
            LatestPointer: false,
            LatestPointerOverwrite: false,
            ProductionAllowed: false,
            PublicProductAllowed: false,
            ShellAllowed: false,
            CommandExecutionAllowed: false,
            ProviderCloudNetworkAvailable: false,
            DbMigrationAvailable: false,
            KmsWormExternalTrustAvailable: false,
            BrowserCdpWcuOcrRecipesLiveAvailable: false,
            PilotRunAvailable: false,
            ReleaseCommercialReady: false,
            ComplianceCustody: false,
            CloudBackedDurability: false,
            SafeNextStep: "explicit-go-required-before-read-precedence-latest-pointer-or-product-authority",
            StatusText: statusText);

    private static bool HasUnsafeText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var normalized = value.Trim();
        return normalized.Contains('\\', StringComparison.Ordinal)
            || normalized.Contains("..", StringComparison.Ordinal)
            || normalized.Contains(':', StringComparison.Ordinal)
            || normalized.Contains("://", StringComparison.Ordinal)
            || normalized.StartsWith("/", StringComparison.Ordinal)
            || normalized.Contains("password=", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("secret=", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("token=", StringComparison.OrdinalIgnoreCase);
    }
}
