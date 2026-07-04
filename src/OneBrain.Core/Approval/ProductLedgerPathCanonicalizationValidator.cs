using System.Text;
using System.Text.RegularExpressions;

namespace OneBrain.Core.Approval;

public enum ProductLedgerPathCanonicalizationDecision
{
    Rejected,
    CandidateReadinessAllowed
}

public enum ProductLedgerPathCanonicalizationBlocker
{
    MissingRequest,
    MissingExplicitLocalOnlyMode,
    ProductLedgerWriteRequested,
    RuntimeEnablementRequested,
    ProductLedgerActiveClaimed,
    ProductReadyClaimed,
    ReleaseCommercialReadinessClaimed,
    ExternalTrustClaimed,
    WormKmsCloudClaimed,
    EmptyAllowedRootPath,
    EmptyCandidatePath,
    RelativeAllowedRootPath,
    RelativeCandidatePath,
    PathTraversalRisk,
    MixedSeparatorRisk,
    UncNetworkPathRisk,
    WindowsReservedDeviceNameRisk,
    EnvironmentVariableExpansionRisk,
    DriveRelativePathRisk,
    LongPathPrefixAmbiguity,
    TrailingDotOrSpaceRisk,
    AlternateDataStreamRisk,
    UnicodeNormalizationOrConfusableRisk,
    AllowedRootCanonicalizationFailed,
    CandidateCanonicalizationFailed,
    AllowedRootMissing,
    CandidateCanonicalPathMissing,
    CanonicalPathOutsideAllowedBoundary,
    DisplayedPathInsideButCanonicalOutside,
    ReparsePointEvidenceMissing,
    SymlinkJunctionReparsePointRiskUnresolved,
    TocTouMitigationMissing,
    LocalTempClaimedAsProductLedgerPath,
    HardlinkOrMountAliasRiskUnresolved
}

public sealed record ProductLedgerPathCanonicalizationRequest(
    string? CandidatePath,
    string? AllowedRootPath,
    bool ExplicitLocalOnlyMode,
    bool NoProductLedgerWriteAssertion,
    bool NoRuntimeEnablementAssertion,
    bool NoReleaseCommercialAssertion,
    bool ClaimsProductLedgerActive,
    bool ClaimsProductReady,
    bool ClaimsExternalTrust,
    bool ClaimsWormKmsCloud,
    bool ClaimsLocalTempAsProductLedgerPath,
    bool HasResolvedReparsePointEvidence,
    bool HasTocTouMitigationEvidence,
    bool HardlinkOrMountAliasRiskUnresolved);

public sealed record ProductLedgerPathCanonicalizationResult(
    ProductLedgerPathCanonicalizationDecision Decision,
    IReadOnlyList<ProductLedgerPathCanonicalizationBlocker> Blockers,
    bool CandidateReadinessAllowed,
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
    string? CanonicalCandidatePath,
    string? CanonicalAllowedRootPath,
    string StatusText);

public sealed class ProductLedgerPathCanonicalizationValidator
{
    public const string CandidateReadinessOnlyStatus =
        "CANDIDATE_READINESS_ONLY REAL_LOCAL_CANONICALIZATION_VALIDATOR NO_PRODUCT_LEDGER_WRITE NO_PRODUCT_RUNTIME_ENABLEMENT NO_ACTIVE_PRODUCT_LEDGER_PATH NO_RELEASE_COMMERCIAL NO_WORM_KMS_CLOUD NO_EXTERNAL_TRUST";

    private static readonly Regex EnvironmentVariablePathPattern = new(
        @"%[A-Za-z_][A-Za-z0-9_]*%|\$[A-Za-z_][A-Za-z0-9_]*|\$\{[A-Za-z_][A-Za-z0-9_]*\}",
        RegexOptions.Compiled);

    private static readonly Regex ReservedWindowsDevicePathPattern = new(
        @"(^|[\\/])(con|prn|aux|nul|com[1-9]|lpt[1-9])(\.|[\\/]|$)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex DriveRelativePathPattern = new(
        @"^[A-Za-z]:(?![\\/])",
        RegexOptions.Compiled);

    private static readonly Regex ConfusableScriptPathPattern = new(
        @"[\p{IsCyrillic}\p{IsGreek}]",
        RegexOptions.Compiled);

    public ProductLedgerPathCanonicalizationResult Validate(ProductLedgerPathCanonicalizationRequest? request)
    {
        var blockers = new List<ProductLedgerPathCanonicalizationBlocker>();
        if (request is null)
        {
            blockers.Add(ProductLedgerPathCanonicalizationBlocker.MissingRequest);
            return Result(blockers, null, null);
        }

        AddTopLevelBlockers(request, blockers);
        AddPathStringBlockers(request.AllowedRootPath, true, blockers);
        AddPathStringBlockers(request.CandidatePath, false, blockers);

        var canonicalAllowedRoot = TryCanonicalize(request.AllowedRootPath, blockers, true);
        var canonicalCandidate = TryCanonicalize(request.CandidatePath, blockers, false);

        AddBoundaryBlockers(request, canonicalAllowedRoot, canonicalCandidate, blockers);
        AddFilesystemEvidenceBlockers(request, canonicalAllowedRoot, canonicalCandidate, blockers);

        return Result(blockers, canonicalCandidate, canonicalAllowedRoot);
    }

    private static void AddTopLevelBlockers(
        ProductLedgerPathCanonicalizationRequest request,
        List<ProductLedgerPathCanonicalizationBlocker> blockers)
    {
        if (!request.ExplicitLocalOnlyMode)
        {
            blockers.Add(ProductLedgerPathCanonicalizationBlocker.MissingExplicitLocalOnlyMode);
        }

        if (!request.NoProductLedgerWriteAssertion)
        {
            blockers.Add(ProductLedgerPathCanonicalizationBlocker.ProductLedgerWriteRequested);
        }

        if (!request.NoRuntimeEnablementAssertion)
        {
            blockers.Add(ProductLedgerPathCanonicalizationBlocker.RuntimeEnablementRequested);
        }

        if (!request.NoReleaseCommercialAssertion)
        {
            blockers.Add(ProductLedgerPathCanonicalizationBlocker.ReleaseCommercialReadinessClaimed);
        }

        if (request.ClaimsProductLedgerActive)
        {
            blockers.Add(ProductLedgerPathCanonicalizationBlocker.ProductLedgerActiveClaimed);
        }

        if (request.ClaimsProductReady)
        {
            blockers.Add(ProductLedgerPathCanonicalizationBlocker.ProductReadyClaimed);
        }

        if (request.ClaimsExternalTrust)
        {
            blockers.Add(ProductLedgerPathCanonicalizationBlocker.ExternalTrustClaimed);
        }

        if (request.ClaimsWormKmsCloud)
        {
            blockers.Add(ProductLedgerPathCanonicalizationBlocker.WormKmsCloudClaimed);
        }

        if (request.ClaimsLocalTempAsProductLedgerPath)
        {
            blockers.Add(ProductLedgerPathCanonicalizationBlocker.LocalTempClaimedAsProductLedgerPath);
        }

        if (!request.HasResolvedReparsePointEvidence)
        {
            blockers.Add(ProductLedgerPathCanonicalizationBlocker.ReparsePointEvidenceMissing);
            blockers.Add(ProductLedgerPathCanonicalizationBlocker.SymlinkJunctionReparsePointRiskUnresolved);
        }

        if (!request.HasTocTouMitigationEvidence)
        {
            blockers.Add(ProductLedgerPathCanonicalizationBlocker.TocTouMitigationMissing);
        }

        if (request.HardlinkOrMountAliasRiskUnresolved)
        {
            blockers.Add(ProductLedgerPathCanonicalizationBlocker.HardlinkOrMountAliasRiskUnresolved);
        }
    }

    private static void AddPathStringBlockers(
        string? path,
        bool isAllowedRoot,
        List<ProductLedgerPathCanonicalizationBlocker> blockers)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            blockers.Add(isAllowedRoot
                ? ProductLedgerPathCanonicalizationBlocker.EmptyAllowedRootPath
                : ProductLedgerPathCanonicalizationBlocker.EmptyCandidatePath);
            return;
        }

        if (!Path.IsPathRooted(path))
        {
            blockers.Add(isAllowedRoot
                ? ProductLedgerPathCanonicalizationBlocker.RelativeAllowedRootPath
                : ProductLedgerPathCanonicalizationBlocker.RelativeCandidatePath);
        }

        if (ContainsTraversalSegment(path))
        {
            blockers.Add(ProductLedgerPathCanonicalizationBlocker.PathTraversalRisk);
        }

        if (path.Contains('\\', StringComparison.Ordinal) && path.Contains('/', StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerPathCanonicalizationBlocker.MixedSeparatorRisk);
        }

        if (path.StartsWith(@"\\", StringComparison.Ordinal)
            || path.StartsWith("//", StringComparison.Ordinal)
            || path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            blockers.Add(ProductLedgerPathCanonicalizationBlocker.UncNetworkPathRisk);
        }

        if (EnvironmentVariablePathPattern.IsMatch(path))
        {
            blockers.Add(ProductLedgerPathCanonicalizationBlocker.EnvironmentVariableExpansionRisk);
        }

        if (ReservedWindowsDevicePathPattern.IsMatch(path))
        {
            blockers.Add(ProductLedgerPathCanonicalizationBlocker.WindowsReservedDeviceNameRisk);
        }

        if (DriveRelativePathPattern.IsMatch(path))
        {
            blockers.Add(ProductLedgerPathCanonicalizationBlocker.DriveRelativePathRisk);
        }

        if (path.StartsWith(@"\\?\", StringComparison.Ordinal)
            || path.StartsWith(@"\\.\", StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerPathCanonicalizationBlocker.LongPathPrefixAmbiguity);
        }

        if (ContainsTrailingDotOrSpace(path))
        {
            blockers.Add(ProductLedgerPathCanonicalizationBlocker.TrailingDotOrSpaceRisk);
        }

        if (ContainsAlternateDataStreamSyntax(path))
        {
            blockers.Add(ProductLedgerPathCanonicalizationBlocker.AlternateDataStreamRisk);
        }

        if (!path.Equals(path.Normalize(NormalizationForm.FormC), StringComparison.Ordinal)
            || ConfusableScriptPathPattern.IsMatch(path))
        {
            blockers.Add(ProductLedgerPathCanonicalizationBlocker.UnicodeNormalizationOrConfusableRisk);
        }
    }

    private static string? TryCanonicalize(
        string? path,
        List<ProductLedgerPathCanonicalizationBlocker> blockers,
        bool isAllowedRoot)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception) when (
            isAllowedRoot
                ? AddAndReturnTrue(blockers, ProductLedgerPathCanonicalizationBlocker.AllowedRootCanonicalizationFailed)
                : AddAndReturnTrue(blockers, ProductLedgerPathCanonicalizationBlocker.CandidateCanonicalizationFailed))
        {
            return null;
        }
    }

    private static void AddBoundaryBlockers(
        ProductLedgerPathCanonicalizationRequest request,
        string? canonicalAllowedRoot,
        string? canonicalCandidate,
        List<ProductLedgerPathCanonicalizationBlocker> blockers)
    {
        if (canonicalAllowedRoot is null || canonicalCandidate is null)
        {
            return;
        }

        var normalizedRoot = EnsureTrailingSeparator(canonicalAllowedRoot);
        var insideBoundary =
            canonicalCandidate.Equals(canonicalAllowedRoot, StringComparison.OrdinalIgnoreCase)
            || canonicalCandidate.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);

        if (!insideBoundary)
        {
            blockers.Add(ProductLedgerPathCanonicalizationBlocker.CanonicalPathOutsideAllowedBoundary);
        }

        if (!insideBoundary && DisplayedPathAppearsInsideBoundary(request.CandidatePath, request.AllowedRootPath))
        {
            blockers.Add(ProductLedgerPathCanonicalizationBlocker.DisplayedPathInsideButCanonicalOutside);
        }
    }

    private static void AddFilesystemEvidenceBlockers(
        ProductLedgerPathCanonicalizationRequest request,
        string? canonicalAllowedRoot,
        string? canonicalCandidate,
        List<ProductLedgerPathCanonicalizationBlocker> blockers)
    {
        if (canonicalAllowedRoot is not null && !Path.Exists(canonicalAllowedRoot))
        {
            blockers.Add(ProductLedgerPathCanonicalizationBlocker.AllowedRootMissing);
        }

        if (canonicalCandidate is not null && !Path.Exists(canonicalCandidate))
        {
            blockers.Add(ProductLedgerPathCanonicalizationBlocker.CandidateCanonicalPathMissing);
        }

        if (canonicalAllowedRoot is not null && HasReparsePoint(canonicalAllowedRoot))
        {
            blockers.Add(ProductLedgerPathCanonicalizationBlocker.SymlinkJunctionReparsePointRiskUnresolved);
        }

        if (canonicalCandidate is not null && HasReparsePoint(canonicalCandidate))
        {
            blockers.Add(ProductLedgerPathCanonicalizationBlocker.SymlinkJunctionReparsePointRiskUnresolved);
        }

        if (request.ClaimsLocalTempAsProductLedgerPath
            || (request.ClaimsProductLedgerActive && IsUnderLocalTemp(canonicalCandidate)))
        {
            blockers.Add(ProductLedgerPathCanonicalizationBlocker.LocalTempClaimedAsProductLedgerPath);
        }
    }

    private static ProductLedgerPathCanonicalizationResult Result(
        IReadOnlyList<ProductLedgerPathCanonicalizationBlocker> blockers,
        string? canonicalCandidate,
        string? canonicalAllowedRoot)
    {
        var distinct = blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
        var allowed = distinct.Length == 0;
        return new ProductLedgerPathCanonicalizationResult(
            Decision: allowed
                ? ProductLedgerPathCanonicalizationDecision.CandidateReadinessAllowed
                : ProductLedgerPathCanonicalizationDecision.Rejected,
            Blockers: distinct,
            CandidateReadinessAllowed: allowed,
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
            CanonicalCandidatePath: canonicalCandidate,
            CanonicalAllowedRootPath: canonicalAllowedRoot,
            StatusText: CandidateReadinessOnlyStatus);
    }

    private static bool AddAndReturnTrue(
        List<ProductLedgerPathCanonicalizationBlocker> blockers,
        ProductLedgerPathCanonicalizationBlocker blocker)
    {
        blockers.Add(blocker);
        return true;
    }

    private static bool ContainsTraversalSegment(string path) =>
        path.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries)
            .Any(segment => segment.Equals("..", StringComparison.Ordinal));

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

    private static string EnsureTrailingSeparator(string path) =>
        path.EndsWith(Path.DirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;

    private static bool DisplayedPathAppearsInsideBoundary(string? candidatePath, string? allowedRootPath) =>
        !string.IsNullOrWhiteSpace(candidatePath)
        && !string.IsNullOrWhiteSpace(allowedRootPath)
        && candidatePath.StartsWith(EnsureTrailingSeparator(allowedRootPath), StringComparison.OrdinalIgnoreCase);

    private static bool HasReparsePoint(string path)
    {
        try
        {
            return (File.GetAttributes(path) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
        }
        catch (Exception)
        {
            return true;
        }
    }

    private static bool IsUnderLocalTemp(string? canonicalCandidate)
    {
        if (canonicalCandidate is null)
        {
            return false;
        }

        var tempRoot = EnsureTrailingSeparator(Path.GetFullPath(Path.GetTempPath()));
        return canonicalCandidate.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase);
    }
}
