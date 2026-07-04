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
    MissingRedactionProductWiring,
    RedactionResultMissing,
    RedactionResultRejected,
    RedactionEvidenceMissing,
    RedactionEvidencePolicyMissing,
    RedactionEvidenceHashMismatch,
    RedactionEvidenceContainsRawValues,
    RedactionEvidenceMissingBeforePersistence,
    RedactionEvidenceSecretMarkerRejected,
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
    MissingReplayFailureEvidence,
    ReplayEvidenceMissing,
    FailureEvidenceMissing,
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
    bool ClaimsWormKmsCloud);

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
    bool AttemptsProviderCloudKmsWorm);

public sealed record DurableRuntimeReplayFailureEvidenceReadiness(
    bool HasReplayEvidence,
    bool HasFailureEvidence,
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
    private static readonly Regex SecretMarkerPattern = new(
        @"\b(password|token|secret|api[\s_-]?key)\s*[:=]|authorization:|cookie:|bearer\s+|sk-(proj-)?[a-z0-9_-]{8,}|ghp_|github_pat_",
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
}
