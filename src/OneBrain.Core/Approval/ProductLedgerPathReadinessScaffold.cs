using System.Text.RegularExpressions;

namespace OneBrain.Core.Approval;

public enum ProductLedgerPathReadinessDecision
{
    Rejected,
    ReadinessPreviewAllowed
}

public enum ProductLedgerPathBlocker
{
    MissingRequest,
    MissingExplicitTestOnlyMode,
    ProductWriteRequested,
    RuntimeEnablementRequested,
    ReleaseCommercialReadinessClaimed,
    ExternalTrustClaimed,
    WormKmsCloudClaimed,
    MissingCanonicalizationRiskPreview,
    EmptyPath,
    RelativePathWithoutExplicitHandling,
    PathTraversalRisk,
    MixedSeparatorRisk,
    UncNetworkPathRisk,
    WindowsReservedDeviceNameRisk,
    EnvironmentVariableExpansionRisk,
    DriveRelativePathRisk,
    LongPathPrefixAmbiguity,
    CasingNormalizationMismatch,
    UnicodeNormalizationMismatch,
    TrailingDotOrSpaceRisk,
    AlternateDataStreamRisk,
    LocalTempClaimedAsProductLedgerPath,
    ProductLedgerReadyClaimWithoutProductPolicy,
    CanonicalPathEvidenceMissing,
    JailBoundaryEvidenceMissing,
    PathAppearsInsideButCanonicalOutside,
    TocTouMitigationMissing,
    MissingReparsePointRiskPreview,
    SymlinkRiskUnresolved,
    JunctionRiskUnresolved,
    ReparsePointRiskUnresolved,
    HardlinkOrMountAliasRiskUnresolved,
    MissingRedactionPolicyEvidence,
    MissingRetentionPolicyEvidence,
    MissingReplayFailureEvidence,
    MissingRollbackClassification,
    MissingAuthorityReadinessPreview,
    MissingHumanApprovalEvidence,
    HumanGoTreatedAsProductAuthority,
    MissingOperatorIdentityEvidence,
    MissingLocalOperatorSessionEvidence,
    StaleApprovalEvidence,
    ApprovalForDifferentScope,
    ApprovalForDifferentLedgerPath,
    ApprovalForDifferentRuntimeFlag,
    ApprovalReplayOrTamperRisk,
    ApprovalAfterRiskChanges,
    ApprovalMissingEvidenceRefs,
    ApprovalAttemptsProviderCloudKmsWormExternalTrust,
    ApprovalAttemptsLiveAutomation,
    ApprovalAttemptsReleaseCommercial
}

public sealed record ProductLedgerPathReadinessRequest(
    bool ExplicitTestOnlyMode,
    bool NoProductWriteAssertion,
    bool NoRuntimeEnablementAssertion,
    bool NoReleaseCommercialAssertion,
    bool ClaimsExternalTrust,
    bool ClaimsWormKmsCloud,
    CanonicalizationRiskPreview? Canonicalization,
    ReparsePointRiskPreview? ReparsePointRisk,
    AuthorityReadinessPreview? Authority,
    bool HasRedactionPolicyEvidence,
    bool HasRetentionPolicyEvidence,
    bool HasReplayFailureEvidence,
    bool HasRollbackNonRollbackClassification);

public sealed record CanonicalizationRiskPreview(
    string? CandidatePath,
    bool RelativePathExplicitlyHandled,
    bool HasCanonicalPathEvidence,
    bool HasJailBoundaryEvidence,
    bool CanonicalPathInsideJail,
    bool HasTocTouMitigationEvidence,
    bool CasingNormalizationMismatch,
    bool UnicodeNormalizationMismatch,
    bool ClaimsLocalTempAsProductLedgerPath,
    bool ClaimsProductLedgerReadyWithoutProductPolicy);

public sealed record ReparsePointRiskPreview(
    bool HasSymlinkJunctionReparseEvidence,
    bool SymlinkRiskUnresolved,
    bool JunctionRiskUnresolved,
    bool ReparsePointRiskUnresolved,
    bool HardlinkOrMountAliasRiskUnresolved);

public sealed record AuthorityReadinessPreview(
    bool HasHumanApprovalEvidence,
    bool TreatsHumanGoAsProductAuthority,
    string? OperatorIdentityEvidence,
    string? LocalOperatorSessionEvidence,
    IReadOnlyList<string>? EvidenceReferences,
    bool ApprovalIsStale,
    bool ApprovalForDifferentScope,
    bool ApprovalForDifferentLedgerPath,
    bool ApprovalForDifferentRuntimeFlag,
    bool ApprovalReplayOrTamperRisk,
    bool ApprovalAfterRiskChanges,
    bool ApprovalAttemptsProviderCloudKmsWormExternalTrust,
    bool ApprovalAttemptsLiveAutomation,
    bool ApprovalAttemptsReleaseCommercial);

public sealed record ProductLedgerPathReadinessResult(
    ProductLedgerPathReadinessDecision Decision,
    IReadOnlyList<ProductLedgerPathBlocker> Blockers,
    bool ReadinessPreviewAllowed,
    bool ProductLedgerPathActive,
    bool ProductLedgerWriteAllowed,
    bool ProductRuntimeEnabled,
    bool ProductServiceRegistrationAllowed,
    bool ProductCommandHandlersAllowed,
    bool UiProductActionsAllowed,
    bool DbProviderCloudNetworkAllowed,
    bool KmsWormExternalTrustAllowed,
    bool LiveAutomationAllowed,
    bool ReleaseCommercialReady,
    string StatusText);

public sealed class ProductLedgerPathReadinessScaffold
{
    public const string ReadinessPreviewOnlyStatus =
        "READINESS_PREVIEW_ONLY DISABLED_TEST_ONLY NO_PRODUCT_LEDGER_WRITE NO_PRODUCT_RUNTIME_ENABLEMENT NO_RELEASE_COMMERCIAL NO_EXTERNAL_TRUST NO_WORM_KMS_CLOUD";

    private static readonly Regex EnvironmentVariablePathPattern = new(
        @"%[A-Za-z_][A-Za-z0-9_]*%|\$[A-Za-z_][A-Za-z0-9_]*|\$\{[A-Za-z_][A-Za-z0-9_]*\}",
        RegexOptions.Compiled);

    private static readonly Regex ReservedWindowsDevicePathPattern = new(
        @"(^|[\\/])(con|prn|aux|nul|com[1-9]|lpt[1-9])(\.|[\\/]|$)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex DriveRelativePathPattern = new(
        @"^[A-Za-z]:(?![\\/])",
        RegexOptions.Compiled);

    public ProductLedgerPathReadinessResult Evaluate(ProductLedgerPathReadinessRequest? request)
    {
        var blockers = new List<ProductLedgerPathBlocker>();
        if (request is null)
        {
            blockers.Add(ProductLedgerPathBlocker.MissingRequest);
            return Result(blockers);
        }

        AddTopLevelBlockers(request, blockers);
        AddCanonicalizationBlockers(request.Canonicalization, blockers);
        AddReparseBlockers(request.ReparsePointRisk, blockers);
        AddPolicyEvidenceBlockers(request, blockers);
        AddAuthorityBlockers(request.Authority, blockers);

        return Result(blockers);
    }

    private static void AddTopLevelBlockers(
        ProductLedgerPathReadinessRequest request,
        List<ProductLedgerPathBlocker> blockers)
    {
        if (!request.ExplicitTestOnlyMode)
        {
            blockers.Add(ProductLedgerPathBlocker.MissingExplicitTestOnlyMode);
        }

        if (!request.NoProductWriteAssertion)
        {
            blockers.Add(ProductLedgerPathBlocker.ProductWriteRequested);
        }

        if (!request.NoRuntimeEnablementAssertion)
        {
            blockers.Add(ProductLedgerPathBlocker.RuntimeEnablementRequested);
        }

        if (!request.NoReleaseCommercialAssertion)
        {
            blockers.Add(ProductLedgerPathBlocker.ReleaseCommercialReadinessClaimed);
        }

        if (request.ClaimsExternalTrust)
        {
            blockers.Add(ProductLedgerPathBlocker.ExternalTrustClaimed);
        }

        if (request.ClaimsWormKmsCloud)
        {
            blockers.Add(ProductLedgerPathBlocker.WormKmsCloudClaimed);
        }
    }

    private static void AddCanonicalizationBlockers(
        CanonicalizationRiskPreview? preview,
        List<ProductLedgerPathBlocker> blockers)
    {
        if (preview is null)
        {
            blockers.Add(ProductLedgerPathBlocker.MissingCanonicalizationRiskPreview);
            return;
        }

        AddPathBlockers(preview, blockers);

        if (!preview.HasCanonicalPathEvidence)
        {
            blockers.Add(ProductLedgerPathBlocker.CanonicalPathEvidenceMissing);
        }

        if (!preview.HasJailBoundaryEvidence)
        {
            blockers.Add(ProductLedgerPathBlocker.JailBoundaryEvidenceMissing);
        }

        if (!preview.CanonicalPathInsideJail)
        {
            blockers.Add(ProductLedgerPathBlocker.PathAppearsInsideButCanonicalOutside);
        }

        if (!preview.HasTocTouMitigationEvidence)
        {
            blockers.Add(ProductLedgerPathBlocker.TocTouMitigationMissing);
        }

        if (preview.CasingNormalizationMismatch)
        {
            blockers.Add(ProductLedgerPathBlocker.CasingNormalizationMismatch);
        }

        if (preview.UnicodeNormalizationMismatch)
        {
            blockers.Add(ProductLedgerPathBlocker.UnicodeNormalizationMismatch);
        }

        if (preview.ClaimsLocalTempAsProductLedgerPath)
        {
            blockers.Add(ProductLedgerPathBlocker.LocalTempClaimedAsProductLedgerPath);
        }

        if (preview.ClaimsProductLedgerReadyWithoutProductPolicy)
        {
            blockers.Add(ProductLedgerPathBlocker.ProductLedgerReadyClaimWithoutProductPolicy);
        }
    }

    private static void AddPathBlockers(
        CanonicalizationRiskPreview preview,
        List<ProductLedgerPathBlocker> blockers)
    {
        var path = preview.CandidatePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            blockers.Add(ProductLedgerPathBlocker.EmptyPath);
            return;
        }

        if (!Path.IsPathRooted(path) && !preview.RelativePathExplicitlyHandled)
        {
            blockers.Add(ProductLedgerPathBlocker.RelativePathWithoutExplicitHandling);
        }

        if (path.Contains("..", StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerPathBlocker.PathTraversalRisk);
        }

        if (path.Contains('\\', StringComparison.Ordinal) && path.Contains('/', StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerPathBlocker.MixedSeparatorRisk);
        }

        if (path.StartsWith(@"\\", StringComparison.Ordinal)
            || path.StartsWith("//", StringComparison.Ordinal)
            || path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            blockers.Add(ProductLedgerPathBlocker.UncNetworkPathRisk);
        }

        if (ReservedWindowsDevicePathPattern.IsMatch(path))
        {
            blockers.Add(ProductLedgerPathBlocker.WindowsReservedDeviceNameRisk);
        }

        if (EnvironmentVariablePathPattern.IsMatch(path))
        {
            blockers.Add(ProductLedgerPathBlocker.EnvironmentVariableExpansionRisk);
        }

        if (DriveRelativePathPattern.IsMatch(path))
        {
            blockers.Add(ProductLedgerPathBlocker.DriveRelativePathRisk);
        }

        if (path.StartsWith(@"\\?\", StringComparison.Ordinal)
            || path.StartsWith(@"\\.\", StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerPathBlocker.LongPathPrefixAmbiguity);
        }

        if (ContainsTrailingDotOrSpace(path))
        {
            blockers.Add(ProductLedgerPathBlocker.TrailingDotOrSpaceRisk);
        }

        if (ContainsAlternateDataStreamSyntax(path))
        {
            blockers.Add(ProductLedgerPathBlocker.AlternateDataStreamRisk);
        }
    }

    private static void AddReparseBlockers(
        ReparsePointRiskPreview? preview,
        List<ProductLedgerPathBlocker> blockers)
    {
        if (preview is null)
        {
            blockers.Add(ProductLedgerPathBlocker.MissingReparsePointRiskPreview);
            return;
        }

        if (!preview.HasSymlinkJunctionReparseEvidence)
        {
            blockers.Add(ProductLedgerPathBlocker.SymlinkRiskUnresolved);
            blockers.Add(ProductLedgerPathBlocker.JunctionRiskUnresolved);
            blockers.Add(ProductLedgerPathBlocker.ReparsePointRiskUnresolved);
        }

        if (preview.SymlinkRiskUnresolved)
        {
            blockers.Add(ProductLedgerPathBlocker.SymlinkRiskUnresolved);
        }

        if (preview.JunctionRiskUnresolved)
        {
            blockers.Add(ProductLedgerPathBlocker.JunctionRiskUnresolved);
        }

        if (preview.ReparsePointRiskUnresolved)
        {
            blockers.Add(ProductLedgerPathBlocker.ReparsePointRiskUnresolved);
        }

        if (preview.HardlinkOrMountAliasRiskUnresolved)
        {
            blockers.Add(ProductLedgerPathBlocker.HardlinkOrMountAliasRiskUnresolved);
        }
    }

    private static void AddPolicyEvidenceBlockers(
        ProductLedgerPathReadinessRequest request,
        List<ProductLedgerPathBlocker> blockers)
    {
        if (!request.HasRedactionPolicyEvidence)
        {
            blockers.Add(ProductLedgerPathBlocker.MissingRedactionPolicyEvidence);
        }

        if (!request.HasRetentionPolicyEvidence)
        {
            blockers.Add(ProductLedgerPathBlocker.MissingRetentionPolicyEvidence);
        }

        if (!request.HasReplayFailureEvidence)
        {
            blockers.Add(ProductLedgerPathBlocker.MissingReplayFailureEvidence);
        }

        if (!request.HasRollbackNonRollbackClassification)
        {
            blockers.Add(ProductLedgerPathBlocker.MissingRollbackClassification);
        }
    }

    private static void AddAuthorityBlockers(
        AuthorityReadinessPreview? preview,
        List<ProductLedgerPathBlocker> blockers)
    {
        if (preview is null)
        {
            blockers.Add(ProductLedgerPathBlocker.MissingAuthorityReadinessPreview);
            return;
        }

        if (!preview.HasHumanApprovalEvidence)
        {
            blockers.Add(ProductLedgerPathBlocker.MissingHumanApprovalEvidence);
        }

        if (preview.TreatsHumanGoAsProductAuthority)
        {
            blockers.Add(ProductLedgerPathBlocker.HumanGoTreatedAsProductAuthority);
        }

        if (string.IsNullOrWhiteSpace(preview.OperatorIdentityEvidence))
        {
            blockers.Add(ProductLedgerPathBlocker.MissingOperatorIdentityEvidence);
        }

        if (string.IsNullOrWhiteSpace(preview.LocalOperatorSessionEvidence))
        {
            blockers.Add(ProductLedgerPathBlocker.MissingLocalOperatorSessionEvidence);
        }

        if (preview.ApprovalIsStale)
        {
            blockers.Add(ProductLedgerPathBlocker.StaleApprovalEvidence);
        }

        if (preview.ApprovalForDifferentScope)
        {
            blockers.Add(ProductLedgerPathBlocker.ApprovalForDifferentScope);
        }

        if (preview.ApprovalForDifferentLedgerPath)
        {
            blockers.Add(ProductLedgerPathBlocker.ApprovalForDifferentLedgerPath);
        }

        if (preview.ApprovalForDifferentRuntimeFlag)
        {
            blockers.Add(ProductLedgerPathBlocker.ApprovalForDifferentRuntimeFlag);
        }

        if (preview.ApprovalReplayOrTamperRisk)
        {
            blockers.Add(ProductLedgerPathBlocker.ApprovalReplayOrTamperRisk);
        }

        if (preview.ApprovalAfterRiskChanges)
        {
            blockers.Add(ProductLedgerPathBlocker.ApprovalAfterRiskChanges);
        }

        if (preview.EvidenceReferences is null
            || preview.EvidenceReferences.Count == 0
            || preview.EvidenceReferences.Any(string.IsNullOrWhiteSpace))
        {
            blockers.Add(ProductLedgerPathBlocker.ApprovalMissingEvidenceRefs);
        }

        if (preview.ApprovalAttemptsProviderCloudKmsWormExternalTrust)
        {
            blockers.Add(ProductLedgerPathBlocker.ApprovalAttemptsProviderCloudKmsWormExternalTrust);
        }

        if (preview.ApprovalAttemptsLiveAutomation)
        {
            blockers.Add(ProductLedgerPathBlocker.ApprovalAttemptsLiveAutomation);
        }

        if (preview.ApprovalAttemptsReleaseCommercial)
        {
            blockers.Add(ProductLedgerPathBlocker.ApprovalAttemptsReleaseCommercial);
        }
    }

    private static ProductLedgerPathReadinessResult Result(IReadOnlyList<ProductLedgerPathBlocker> blockers)
    {
        var distinct = blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
        var previewAllowed = distinct.Length == 0;
        return new ProductLedgerPathReadinessResult(
            Decision: previewAllowed
                ? ProductLedgerPathReadinessDecision.ReadinessPreviewAllowed
                : ProductLedgerPathReadinessDecision.Rejected,
            Blockers: distinct,
            ReadinessPreviewAllowed: previewAllowed,
            ProductLedgerPathActive: false,
            ProductLedgerWriteAllowed: false,
            ProductRuntimeEnabled: false,
            ProductServiceRegistrationAllowed: false,
            ProductCommandHandlersAllowed: false,
            UiProductActionsAllowed: false,
            DbProviderCloudNetworkAllowed: false,
            KmsWormExternalTrustAllowed: false,
            LiveAutomationAllowed: false,
            ReleaseCommercialReady: false,
            StatusText: ReadinessPreviewOnlyStatus);
    }

    private static bool ContainsTrailingDotOrSpace(string path) =>
        path.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries)
            .Any(segment => segment.EndsWith(".", StringComparison.Ordinal) || segment.EndsWith(" ", StringComparison.Ordinal));

    private static bool ContainsAlternateDataStreamSyntax(string path)
    {
        if (path.Length >= 2 && char.IsLetter(path[0]) && path[1] == ':')
        {
            path = path[2..];
        }

        return path.Contains(':', StringComparison.Ordinal);
    }
}
