using System.Text.RegularExpressions;

namespace OneBrain.Core.Approval;

public enum DurableRuntimeEnablementScaffoldDecision
{
    Rejected,
    ReadinessPreviewAllowed
}

public enum DurableRuntimeEnablementScaffoldBlocker
{
    MissingRequest,
    MissingExplicitTestOnlyScope,
    ProductRuntimeEnablementRequested,
    ReleaseCommercialReadinessClaimed,
    MissingProductLedgerPathReadiness,
    EmptyProductLedgerPath,
    MissingLedgerBoundaryRoot,
    LedgerPathOutsideLocalBoundary,
    LedgerPathNotLocalOnly,
    LedgerPathClaimsProviderCloudNetwork,
    LedgerPathMissingRedactionPolicy,
    LedgerPathMissingRetentionPolicy,
    LedgerPathMissingFailureReplayEvidence,
    LedgerPathClaimsWormKmsCloud,
    LedgerPathTraversalRejected,
    LedgerPathEnvironmentVariableRejected,
    LedgerPathReservedDeviceNameRejected,
    LedgerPathMixedSeparatorRejected,
    LedgerPathSymlinkJunctionReparsePointRiskUnresolved,
    LedgerPathCanonicalizationMismatchRiskUnresolved,
    MissingRedactionProductWiring,
    RedactionResultMissing,
    RedactionResultRejected,
    RedactionEvidenceMissing,
    RedactionEvidencePolicyMissing,
    RedactionEvidenceHashMismatch,
    RedactionEvidenceContainsRawValues,
    RedactionEvidenceMissingBeforePersistence,
    RedactionEvidenceSecretMarkerRejected,
    RedactionEvidenceReferenceMalformed,
    RedactionEvidenceReferenceDuplicate,
    RedactionEvidenceReferenceStale,
    RedactionEvidenceReferenceInconsistent,
    MissingRuntimeFeatureFlagReadiness,
    RuntimeFeatureFlagNotBlockedByDefault,
    RuntimeFeatureFlagMissingLedgerDependency,
    RuntimeFeatureFlagMissingRedactionDependency,
    RuntimeFeatureFlagMissingAuthorityDependency,
    RuntimeFeatureFlagMissingReplayFailureDependency,
    RuntimeFeatureFlagExternalTrustOverclaim,
    RuntimeFeatureFlagMissingHumanGoEvidence,
    MissingAuthorityWiring,
    AuthorityMissingHumanApproval,
    AuthorityMissingLocalTestOperatorIdentity,
    AuthorityMissingReason,
    AuthorityMissingEvidence,
    AuthorityScopeExceeded,
    AuthorityAttemptsLiveAutomation,
    AuthorityAttemptsProviderCloudKmsWorm,
    AuthorityClaimsRealHumanAuthorization,
    AuthorityClaimsProductionOperatorApproval,
    AuthorityClaimsProductAuthority,
    AuthorityClaimsReleaseApproval,
    MissingReplayFailureEvidence,
    ReplayEvidenceMissing,
    FailureEvidenceMissing,
    ReplayFailureEvidenceReferenceMissing,
    ReplayFailureEvidenceReferenceMalformed,
    ReplayFailureEvidenceReferenceDuplicate,
    ReadModelSnapshotMissing,
    ReplayReadModelConsistencyMissing,
    FailureModeCatalogMissing,
    RollbackNonRollbackClassificationMissing,
    ReplayEvidenceClaimsLiveExecution,
    ReplayEvidenceContainsRawPayload,
    TailDeletionLimitationNotAcknowledged,
    CheckpointLimitationNotAcknowledged,
    NoWormKmsCloudDisclaimerMissing,
    DurableProductRecoveryClaimed
}

public sealed record DurableRuntimeEnablementScaffoldRequest(
    bool ExplicitTestOnlyScope,
    bool ProductRuntimeEnablementRequested,
    bool ReleaseCommercialReadinessClaimed,
    DurableRuntimeProductLedgerPathReadiness? ProductLedgerPath,
    DurableRuntimeRedactionProductWiringReadiness? RedactionProductWiring,
    DurableRuntimeFeatureFlagProductReadiness? RuntimeFeatureFlag,
    DurableRuntimeAuthorityWiringReadiness? AuthorityWiring,
    DurableRuntimeReplayFailureEvidenceReadiness? ReplayFailureEvidence);

public sealed record DurableRuntimeProductLedgerPathReadiness(
    string? ProposedLedgerPath,
    string? LocalBoundaryRoot,
    bool LocalOnly,
    bool HasRedactionPolicy,
    bool HasRetentionPolicy,
    bool HasFailureReplayEvidence,
    bool ClaimsProviderCloudNetwork,
    bool ClaimsWormKmsCloud,
    bool HasNoSymlinkJunctionReparsePointEvidence,
    bool HasCanonicalRealPathEvidence);

public sealed record DurableRuntimeRedactionProductWiringReadiness(
    DurableAuditTrailAppendOnlyMinimalRequest? CandidateAppendRequest,
    RedactionBeforePersistenceResult? RedactionResult,
    string? ExpectedCandidateHash,
    string? ExplicitPolicyId,
    bool EvidencePresent);

public sealed record DurableRuntimeFeatureFlagProductReadiness(
    bool BlockedByDefault,
    bool ProductLedgerPathApproved,
    bool RedactionProductWiringApproved,
    bool AuthorityWiringApproved,
    bool ReplayFailureEvidenceApproved,
    bool NoExternalTrustOverclaim,
    bool HumanGoEvidencePresent);

public sealed record DurableRuntimeAuthorityWiringReadiness(
    bool HumanApprovalPresent,
    string? LocalTestOperatorIdentity,
    string? Reason,
    IReadOnlyList<string>? EvidenceReferences,
    string? Scope,
    bool AttemptsLiveAutomationAuthority,
    bool AttemptsProviderCloudKmsWorm,
    bool ClaimsRealHumanAuthorization,
    bool ClaimsProductionOperatorApproval,
    bool ClaimsProductAuthority,
    bool ClaimsReleaseApproval);

public sealed record DurableRuntimeReplayFailureEvidenceReadiness(
    bool HasReplayEvidence,
    bool HasFailureEvidence,
    IReadOnlyList<string>? EvidenceReferences,
    bool HasReadModelSnapshot,
    bool HasReplayReadModelConsistencyCheck,
    bool HasFailureModeCatalog,
    bool HasRollbackAndNonRollbackClassification,
    bool ClaimsLiveReplayExecution,
    bool ContainsRawPayloadEvidence,
    bool AcknowledgesTailDeletionLimitation,
    bool AcknowledgesCheckpointLimitation,
    bool HasNoWormKmsCloudDisclaimer,
    bool ClaimsDurableProductRecovery);

public sealed record DurableRuntimeEnablementScaffoldResult(
    DurableRuntimeEnablementScaffoldDecision Decision,
    IReadOnlyList<DurableRuntimeEnablementScaffoldBlocker> Blockers,
    IReadOnlyList<string> Warnings,
    bool ReadinessPreviewAllowed,
    bool ProductRuntimeEnabled,
    bool ProductLedgerPathActive,
    bool ProductServiceRegistrationAllowed,
    bool ProductCommandHandlersAllowed,
    bool UiProductActionsAllowed,
    bool ProviderCloudNetworkAllowed,
    bool KmsWormCloudAllowed,
    bool LiveAutomationAllowed,
    bool ReleaseCommercialReady,
    string StatusText);

public sealed class DurableRuntimeEnablementSafetyScaffold
{
    public const string RequiredScope = "durable-runtime-test-only";
    public const string NoProductRuntimeEnablementStatus = "NO_PRODUCT_RUNTIME_ENABLEMENT";

    private static readonly Regex ProviderCloudNetworkPathPattern = new(
        @"^(https?|s3|gs|az|ftp)://|\\\\|(^|[\\/])(cloud|provider|network|bucket|blob|s3|gcs|azure)([\\/]|$)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex EnvironmentVariablePathPattern = new(
        @"%[A-Za-z_][A-Za-z0-9_]*%|\$[A-Za-z_][A-Za-z0-9_]*|\$\{[A-Za-z_][A-Za-z0-9_]*\}",
        RegexOptions.Compiled);
    private static readonly Regex ReservedWindowsDevicePathPattern = new(
        @"(^|[\\/])(con|prn|aux|nul|com[1-9]|lpt[1-9])(\.|[\\/]|$)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex SecretMarkerPattern = new(
        @"\b(password|token|secret|api[\s_-]?key)\s*[:=]|authorization:|cookie:|bearer\s+|sk-(proj-)?[a-z0-9_-]{8,}|ghp_|github_pat_",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex StaleEvidenceReferencePattern = new(
        @"(^|[\/\\._-])(stale|expired|old|superseded)([\/\\._-]|$)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex InconsistentEvidenceReferencePattern = new(
        @"(^|[\/\\._-])(inconsistent|mismatch|conflict)([\/\\._-]|$)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex LiveAutomationClaimPattern = new(
        @"browser/cdp[\s._-]+live|cdp[\s._-]+live|wcu\s*/\s*ocr[\s._-]+live|ocr[\s._-]+live|recipes?[\s._-]+live|live[\s._-]+automation|live[\s._-]+execution",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex RealHumanAuthorityClaimPattern = new(
        @"real\s+human\s+(authorization|approval)|production\s+operator\s+approval|operator\s+authority|auth\s+approved",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ProductAuthorityClaimPattern = new(
        @"product\s+authority|runtime\s+approval\s+real|runtime\s+enabled|enablement\s+approved|product\s+policy",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ReleaseApprovalClaimPattern = new(
        @"release\s+approval|commercial\s+approval|release-ready|commercial-ready|production-ready",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public DurableRuntimeEnablementScaffoldResult Evaluate(DurableRuntimeEnablementScaffoldRequest? request)
    {
        var blockers = new List<DurableRuntimeEnablementScaffoldBlocker>();
        if (request is null)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.MissingRequest);
            return Result(blockers);
        }

        AddTopLevelBlockers(request, blockers);
        AddProductLedgerPathBlockers(request.ProductLedgerPath, blockers);
        AddRedactionProductWiringBlockers(request.RedactionProductWiring, blockers);
        AddRuntimeFeatureFlagBlockers(request.RuntimeFeatureFlag, blockers);
        AddAuthorityWiringBlockers(request.AuthorityWiring, blockers);
        AddReplayFailureEvidenceBlockers(request.ReplayFailureEvidence, blockers);

        return Result(blockers);
    }

    private static void AddTopLevelBlockers(
        DurableRuntimeEnablementScaffoldRequest request,
        List<DurableRuntimeEnablementScaffoldBlocker> blockers)
    {
        if (!request.ExplicitTestOnlyScope)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.MissingExplicitTestOnlyScope);
        }

        if (request.ProductRuntimeEnablementRequested)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.ProductRuntimeEnablementRequested);
        }

        if (request.ReleaseCommercialReadinessClaimed)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.ReleaseCommercialReadinessClaimed);
        }
    }

    private static void AddProductLedgerPathBlockers(
        DurableRuntimeProductLedgerPathReadiness? readiness,
        List<DurableRuntimeEnablementScaffoldBlocker> blockers)
    {
        if (readiness is null)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.MissingProductLedgerPathReadiness);
            return;
        }

        if (string.IsNullOrWhiteSpace(readiness.ProposedLedgerPath))
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.EmptyProductLedgerPath);
        }

        if (string.IsNullOrWhiteSpace(readiness.LocalBoundaryRoot))
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.MissingLedgerBoundaryRoot);
        }

        if (!readiness.LocalOnly)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.LedgerPathNotLocalOnly);
        }

        if (readiness.ClaimsProviderCloudNetwork || ContainsProviderCloudNetworkPath(readiness.ProposedLedgerPath))
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.LedgerPathClaimsProviderCloudNetwork);
        }

        if (readiness.ClaimsWormKmsCloud)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.LedgerPathClaimsWormKmsCloud);
        }

        if (!readiness.HasNoSymlinkJunctionReparsePointEvidence)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.LedgerPathSymlinkJunctionReparsePointRiskUnresolved);
        }

        if (!readiness.HasCanonicalRealPathEvidence)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.LedgerPathCanonicalizationMismatchRiskUnresolved);
        }

        AddPathLexemeBlockers(readiness.ProposedLedgerPath, blockers);

        if (!readiness.HasRedactionPolicy)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.LedgerPathMissingRedactionPolicy);
        }

        if (!readiness.HasRetentionPolicy)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.LedgerPathMissingRetentionPolicy);
        }

        if (!readiness.HasFailureReplayEvidence)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.LedgerPathMissingFailureReplayEvidence);
        }

        if (!string.IsNullOrWhiteSpace(readiness.ProposedLedgerPath)
            && !string.IsNullOrWhiteSpace(readiness.LocalBoundaryRoot)
            && !IsUnderBoundary(readiness.ProposedLedgerPath, readiness.LocalBoundaryRoot))
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.LedgerPathOutsideLocalBoundary);
        }
    }

    private static void AddRedactionProductWiringBlockers(
        DurableRuntimeRedactionProductWiringReadiness? readiness,
        List<DurableRuntimeEnablementScaffoldBlocker> blockers)
    {
        if (readiness is null)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.MissingRedactionProductWiring);
            return;
        }

        if (!readiness.EvidencePresent)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.RedactionEvidenceMissing);
        }

        if (string.IsNullOrWhiteSpace(readiness.ExplicitPolicyId))
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.RedactionEvidencePolicyMissing);
        }

        var redaction = readiness.RedactionResult;
        if (redaction is null)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.RedactionResultMissing);
            return;
        }

        if (redaction.Decision != RedactionBeforePersistenceDecision.Allowed || !redaction.Succeeded)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.RedactionResultRejected);
        }

        if (redaction.Evidence is null)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.RedactionEvidenceMissing);
            return;
        }

        if (string.IsNullOrWhiteSpace(redaction.Evidence.PolicyId))
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.RedactionEvidencePolicyMissing);
        }

        if (!redaction.Evidence.CompletedBeforePersistence)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.RedactionEvidenceMissingBeforePersistence);
        }

        if (redaction.Evidence.ContainsRawValues)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.RedactionEvidenceContainsRawValues);
        }

        if (ContainsSecretMarker(readiness.CandidateAppendRequest)
            || ContainsSecretMarker(redaction.SafeRequest))
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.RedactionEvidenceSecretMarkerRejected);
        }

        AddEvidenceReferenceBlockers(readiness.CandidateAppendRequest?.EvidenceReferences, blockers);
        AddEvidenceReferenceBlockers(redaction.SafeRequest?.EvidenceReferences, blockers);

        var expectedHash = !string.IsNullOrWhiteSpace(readiness.ExpectedCandidateHash)
            ? readiness.ExpectedCandidateHash
            : RedactionBeforePersistenceService.ComputeCandidateHash(readiness.CandidateAppendRequest);
        if (string.IsNullOrWhiteSpace(redaction.Evidence.CandidateHash)
            || !string.Equals(redaction.Evidence.CandidateHash, expectedHash, StringComparison.Ordinal)
            || !string.Equals(
                RedactionBeforePersistenceService.ComputeCandidateHash(redaction.SafeRequest),
                expectedHash,
                StringComparison.Ordinal))
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.RedactionEvidenceHashMismatch);
        }
    }

    private static void AddRuntimeFeatureFlagBlockers(
        DurableRuntimeFeatureFlagProductReadiness? readiness,
        List<DurableRuntimeEnablementScaffoldBlocker> blockers)
    {
        if (readiness is null)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.MissingRuntimeFeatureFlagReadiness);
            return;
        }

        if (!readiness.BlockedByDefault)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.RuntimeFeatureFlagNotBlockedByDefault);
        }

        if (!readiness.ProductLedgerPathApproved)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.RuntimeFeatureFlagMissingLedgerDependency);
        }

        if (!readiness.RedactionProductWiringApproved)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.RuntimeFeatureFlagMissingRedactionDependency);
        }

        if (!readiness.AuthorityWiringApproved)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.RuntimeFeatureFlagMissingAuthorityDependency);
        }

        if (!readiness.ReplayFailureEvidenceApproved)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.RuntimeFeatureFlagMissingReplayFailureDependency);
        }

        if (!readiness.NoExternalTrustOverclaim)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.RuntimeFeatureFlagExternalTrustOverclaim);
        }

        if (!readiness.HumanGoEvidencePresent)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.RuntimeFeatureFlagMissingHumanGoEvidence);
        }
    }

    private static void AddAuthorityWiringBlockers(
        DurableRuntimeAuthorityWiringReadiness? readiness,
        List<DurableRuntimeEnablementScaffoldBlocker> blockers)
    {
        if (readiness is null)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.MissingAuthorityWiring);
            return;
        }

        if (!readiness.HumanApprovalPresent)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.AuthorityMissingHumanApproval);
        }

        if (string.IsNullOrWhiteSpace(readiness.LocalTestOperatorIdentity))
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.AuthorityMissingLocalTestOperatorIdentity);
        }

        if (string.IsNullOrWhiteSpace(readiness.Reason))
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.AuthorityMissingReason);
        }

        if (readiness.EvidenceReferences is null
            || readiness.EvidenceReferences.Count == 0
            || readiness.EvidenceReferences.Any(string.IsNullOrWhiteSpace))
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.AuthorityMissingEvidence);
        }

        if (!string.Equals(readiness.Scope, RequiredScope, StringComparison.Ordinal))
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.AuthorityScopeExceeded);
        }

        if (readiness.AttemptsLiveAutomationAuthority)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.AuthorityAttemptsLiveAutomation);
        }

        if (readiness.AttemptsProviderCloudKmsWorm)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.AuthorityAttemptsProviderCloudKmsWorm);
        }

        if (readiness.ClaimsRealHumanAuthorization || ContainsRealHumanAuthorityClaim(readiness))
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.AuthorityClaimsRealHumanAuthorization);
        }

        if (readiness.ClaimsProductionOperatorApproval)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.AuthorityClaimsProductionOperatorApproval);
        }

        if (readiness.ClaimsProductAuthority || ContainsProductAuthorityClaim(readiness))
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.AuthorityClaimsProductAuthority);
        }

        if (readiness.ClaimsReleaseApproval || ContainsReleaseApprovalClaim(readiness))
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.AuthorityClaimsReleaseApproval);
        }

        if (ContainsLiveAutomationClaim(readiness))
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.AuthorityAttemptsLiveAutomation);
        }
    }

    private static void AddReplayFailureEvidenceBlockers(
        DurableRuntimeReplayFailureEvidenceReadiness? readiness,
        List<DurableRuntimeEnablementScaffoldBlocker> blockers)
    {
        if (readiness is null)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.MissingReplayFailureEvidence);
            return;
        }

        if (!readiness.HasReplayEvidence)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.ReplayEvidenceMissing);
        }

        if (!readiness.HasFailureEvidence)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.FailureEvidenceMissing);
        }

        AddReplayFailureEvidenceReferenceBlockers(readiness.EvidenceReferences, blockers);

        if (!readiness.HasReadModelSnapshot)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.ReadModelSnapshotMissing);
        }

        if (!readiness.HasReplayReadModelConsistencyCheck)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.ReplayReadModelConsistencyMissing);
        }

        if (!readiness.HasFailureModeCatalog)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.FailureModeCatalogMissing);
        }

        if (!readiness.HasRollbackAndNonRollbackClassification)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.RollbackNonRollbackClassificationMissing);
        }

        if (readiness.ClaimsLiveReplayExecution)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.ReplayEvidenceClaimsLiveExecution);
        }

        if (readiness.ContainsRawPayloadEvidence)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.ReplayEvidenceContainsRawPayload);
        }

        if (!readiness.AcknowledgesTailDeletionLimitation)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.TailDeletionLimitationNotAcknowledged);
        }

        if (!readiness.AcknowledgesCheckpointLimitation)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.CheckpointLimitationNotAcknowledged);
        }

        if (!readiness.HasNoWormKmsCloudDisclaimer)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.NoWormKmsCloudDisclaimerMissing);
        }

        if (readiness.ClaimsDurableProductRecovery)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.DurableProductRecoveryClaimed);
        }
    }

    private static void AddReplayFailureEvidenceReferenceBlockers(
        IReadOnlyList<string>? evidenceReferences,
        List<DurableRuntimeEnablementScaffoldBlocker> blockers)
    {
        if (evidenceReferences is null || evidenceReferences.Count == 0)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.ReplayFailureEvidenceReferenceMissing);
            return;
        }

        if (evidenceReferences.Any(reference => string.IsNullOrWhiteSpace(reference) || Uri.TryCreate(reference, UriKind.Absolute, out _)))
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.ReplayFailureEvidenceReferenceMalformed);
        }

        if (evidenceReferences
            .Where(reference => !string.IsNullOrWhiteSpace(reference))
            .GroupBy(reference => reference, StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1))
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.ReplayFailureEvidenceReferenceDuplicate);
        }

        if (evidenceReferences.Any(reference => !string.IsNullOrWhiteSpace(reference) && LiveAutomationClaimPattern.IsMatch(reference)))
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.ReplayEvidenceClaimsLiveExecution);
        }
    }

    private static DurableRuntimeEnablementScaffoldResult Result(
        IReadOnlyList<DurableRuntimeEnablementScaffoldBlocker> blockers)
    {
        var distinct = blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
        var previewAllowed = distinct.Length == 0;
        return new DurableRuntimeEnablementScaffoldResult(
            Decision: previewAllowed
                ? DurableRuntimeEnablementScaffoldDecision.ReadinessPreviewAllowed
                : DurableRuntimeEnablementScaffoldDecision.Rejected,
            Blockers: distinct,
            Warnings: previewAllowed
                ? ["Readiness preview only; product enablement remains blocked."]
                : ["Fail-closed scaffold result; product enablement remains blocked."],
            ReadinessPreviewAllowed: previewAllowed,
            ProductRuntimeEnabled: false,
            ProductLedgerPathActive: false,
            ProductServiceRegistrationAllowed: false,
            ProductCommandHandlersAllowed: false,
            UiProductActionsAllowed: false,
            ProviderCloudNetworkAllowed: false,
            KmsWormCloudAllowed: false,
            LiveAutomationAllowed: false,
            ReleaseCommercialReady: false,
            StatusText: NoProductRuntimeEnablementStatus);
    }

    private static bool IsUnderBoundary(string path, string boundaryRoot)
    {
        var fullPath = EnsureTrailingSeparator(Path.GetFullPath(path));
        var fullRoot = EnsureTrailingSeparator(Path.GetFullPath(boundaryRoot));
        return fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static string EnsureTrailingSeparator(string path) =>
        path.EndsWith(Path.DirectorySeparatorChar)
            || path.EndsWith(Path.AltDirectorySeparatorChar)
                ? path
                : path + Path.DirectorySeparatorChar;

    private static bool ContainsProviderCloudNetworkPath(string? path) =>
        !string.IsNullOrWhiteSpace(path) && ProviderCloudNetworkPathPattern.IsMatch(path);

    private static void AddPathLexemeBlockers(
        string? path,
        List<DurableRuntimeEnablementScaffoldBlocker> blockers)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (path.Contains("..", StringComparison.Ordinal))
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.LedgerPathTraversalRejected);
        }

        if (EnvironmentVariablePathPattern.IsMatch(path))
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.LedgerPathEnvironmentVariableRejected);
        }

        if (ReservedWindowsDevicePathPattern.IsMatch(path))
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.LedgerPathReservedDeviceNameRejected);
        }

        if (path.Contains('\\', StringComparison.Ordinal) && path.Contains('/', StringComparison.Ordinal))
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.LedgerPathMixedSeparatorRejected);
        }
    }

    private static void AddEvidenceReferenceBlockers(
        IReadOnlyList<string>? evidenceReferences,
        List<DurableRuntimeEnablementScaffoldBlocker> blockers)
    {
        if (evidenceReferences is null || evidenceReferences.Count == 0)
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.RedactionEvidenceReferenceMalformed);
            return;
        }

        if (evidenceReferences.Any(reference => string.IsNullOrWhiteSpace(reference) || Uri.TryCreate(reference, UriKind.Absolute, out _)))
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.RedactionEvidenceReferenceMalformed);
        }

        if (evidenceReferences
            .Where(reference => !string.IsNullOrWhiteSpace(reference))
            .GroupBy(reference => reference, StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1))
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.RedactionEvidenceReferenceDuplicate);
        }

        if (evidenceReferences.Any(reference => !string.IsNullOrWhiteSpace(reference) && StaleEvidenceReferencePattern.IsMatch(reference)))
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.RedactionEvidenceReferenceStale);
        }

        if (evidenceReferences.Any(reference => !string.IsNullOrWhiteSpace(reference) && InconsistentEvidenceReferencePattern.IsMatch(reference)))
        {
            blockers.Add(DurableRuntimeEnablementScaffoldBlocker.RedactionEvidenceReferenceInconsistent);
        }
    }

    private static bool ContainsSecretMarker(DurableAuditTrailAppendOnlyMinimalRequest? request)
    {
        if (request is null)
        {
            return false;
        }

        return new[] { request.EventKind, request.ActorReference, request.ApprovalReference, request.RawPayload }
            .Concat(request.EvidenceReferences ?? [])
            .Concat(request.Metadata?.Keys ?? [])
            .Concat(request.Metadata?.Values ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Any(value => SecretMarkerPattern.IsMatch(value!));
    }

    private static bool ContainsLiveAutomationClaim(DurableRuntimeAuthorityWiringReadiness readiness) =>
        AuthorityText(readiness).Any(value => LiveAutomationClaimPattern.IsMatch(value));

    private static bool ContainsRealHumanAuthorityClaim(DurableRuntimeAuthorityWiringReadiness readiness) =>
        AuthorityText(readiness).Any(value => RealHumanAuthorityClaimPattern.IsMatch(value));

    private static bool ContainsProductAuthorityClaim(DurableRuntimeAuthorityWiringReadiness readiness) =>
        AuthorityText(readiness).Any(value => ProductAuthorityClaimPattern.IsMatch(value));

    private static bool ContainsReleaseApprovalClaim(DurableRuntimeAuthorityWiringReadiness readiness) =>
        AuthorityText(readiness).Any(value => ReleaseApprovalClaimPattern.IsMatch(value));

    private static IEnumerable<string> AuthorityText(DurableRuntimeAuthorityWiringReadiness readiness) =>
        new[] { readiness.LocalTestOperatorIdentity, readiness.Reason, readiness.Scope }
            .Concat(readiness.EvidenceReferences ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))!;
}
