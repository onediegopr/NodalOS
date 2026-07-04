using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace OneBrain.Core.Approval;

public enum ProductLedgerLocalReportExportDecision
{
    Rejected,
    Blocked,
    ExportedBoundedLocal
}

public enum ProductLedgerLocalReportExportBlocker
{
    MissingRequest,
    MissingExplicitInternalLocalOnlyBoundedExportScope,
    EmptyAllowedRootPath,
    EmptyReportFilePath,
    EmptyReportContent,
    RelativeAllowedRootPath,
    RelativeReportFilePath,
    PathTraversalRisk,
    MixedSeparatorRisk,
    UncNetworkPathRisk,
    EnvironmentVariableExpansionRisk,
    WindowsReservedDeviceNameRisk,
    DriveRelativePathRisk,
    LongPathPrefixAmbiguity,
    TrailingDotOrSpaceRisk,
    AlternateDataStreamRisk,
    AllowedRootCanonicalizationFailed,
    ReportFileCanonicalizationFailed,
    AllowedRootMissing,
    CanonicalReportPathOutsideAllowedBoundary,
    DisplayedPathInsideButCanonicalOutside,
    ReparsePointEvidenceMissing,
    SymlinkJunctionReparsePointRiskUnresolved,
    TocTouMitigationMissing,
    HardlinkOrMountAliasRiskUnresolved,
    UnsafeReportFileName,
    NonReportExtension,
    ExistingFileRequiresExplicitSafeOverwrite,
    MissingOperatorInternalEvidence,
    MissingRedactionBeforePersistenceEvidence,
    MissingSafeContentEvidence,
    UnsafeReportContent,
    UnsafeEvidenceMetadata,
    PublicUiActionRequested,
    DestructiveActionRequested,
    ProductCommandHandlerRequested,
    ProductiveServiceRegistrationRequested,
    ProviderCloudNetworkClaimed,
    DbMigrationClaimed,
    KmsWormExternalTrustClaimed,
    BrowserCdpWcuOcrRecipesLiveClaimed,
    ReleaseCommercialClaimed,
    ExternalTelemetryOrSyncClaimed,
    BillingLicensingCloudClaimed,
    ExternalExportClaimed,
    UnboundedPhysicalExportClaimed,
    PostWriteHashVerificationFailed
}

public sealed record ProductLedgerLocalReportExportRequest(
    string? AllowedRootPath,
    string? ReportFilePath,
    string? ReportContent,
    IReadOnlyDictionary<string, string>? EvidenceMetadata,
    bool ExplicitInternalLocalOnlyBoundedExportScope,
    bool HasOperatorInternalEvidence,
    bool HasRedactionBeforePersistenceEvidence,
    bool HasSafeContentEvidence,
    bool HasResolvedReparsePointEvidence,
    bool HasTocTouMitigationEvidence,
    bool HardlinkOrMountAliasRiskUnresolved,
    bool AllowOverwriteExisting,
    bool RequestsPublicUiAction,
    bool RequestsDestructiveAction,
    bool RequestsProductCommandHandler,
    bool RequestsProductiveServiceRegistration,
    bool ClaimsProviderCloudNetwork,
    bool ClaimsDbMigration,
    bool ClaimsKmsWormExternalTrust,
    bool ClaimsBrowserCdpWcuOcrRecipesLive,
    bool ClaimsReleaseCommercial,
    bool ClaimsExternalTelemetryOrSync,
    bool ClaimsBillingLicensingCloud,
    bool ClaimsExternalExport,
    bool ClaimsUnboundedPhysicalExport);

public sealed record ProductLedgerLocalReportExportEvidence(
    string CanonicalAllowedRootPath,
    string CanonicalReportFilePath,
    string ReportHash,
    long ByteCount,
    IReadOnlyDictionary<string, string> EvidenceMetadata,
    string StatusText);

public sealed record ProductLedgerLocalReportExportResult(
    ProductLedgerLocalReportExportDecision Decision,
    IReadOnlyList<ProductLedgerLocalReportExportBlocker> Blockers,
    ProductLedgerLocalReportExportEvidence? Evidence,
    bool InternalOnly,
    bool LocalOnly,
    bool Bounded,
    bool NonDestructive,
    bool FailClosed,
    bool PhysicalExportCreated,
    bool FileWritePerformed,
    bool PostWriteHashVerified,
    bool PublicUiActionAvailable,
    bool DestructiveActionAvailable,
    bool ProductCommandHandlerAvailable,
    bool ProductiveServiceRegistrationAvailable,
    bool ProviderCloudNetworkAvailable,
    bool DbMigrationAvailable,
    bool KmsWormExternalTrustAvailable,
    bool BrowserCdpWcuOcrRecipesLiveAvailable,
    bool ExternalTelemetryOrSyncAvailable,
    bool BillingLicensingCloudAvailable,
    bool ReleaseCommercialReady,
    string StatusText);

public sealed class ProductLedgerLocalReportExportService
{
    public const string ReadyStatus =
        "PRODUCT_LEDGER_LOCAL_REPORT_EXPORT_BOUNDED_INTERNAL_READY INTERNAL_ONLY LOCAL_ONLY BOUNDED DIAGNOSTIC_REPORT_EXPORT HASH_VERIFIED NO_PUBLIC_UI_ACTION NO_DESTRUCTIVE_ACTION NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    public const string RejectedStatus =
        "PRODUCT_LEDGER_LOCAL_REPORT_EXPORT_BOUNDED_INTERNAL_REJECTED FAIL_CLOSED NO_PUBLIC_UI_ACTION NO_DESTRUCTIVE_ACTION NO_PROVIDER_CLOUD_NETWORK NO_DB_MIGRATION NO_KMS_WORM_EXTERNAL_TRUST NO_LIVE_AUTOMATION NO_RELEASE_COMMERCIAL";

    private static readonly Regex EnvironmentVariablePathPattern = new(
        @"%[A-Za-z_][A-Za-z0-9_]*%|\$[A-Za-z_][A-Za-z0-9_]*|\$\{[A-Za-z_][A-Za-z0-9_]*\}",
        RegexOptions.Compiled);

    private static readonly Regex ReservedWindowsDevicePathPattern = new(
        @"(^|[\\/])(con|prn|aux|nul|com[1-9]|lpt[1-9])(\.|[\\/]|$)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ReportFileNamePattern = new(
        @"^product-ledger-[a-z0-9._-]{3,80}\.(json|md|txt)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex MetadataKeyPattern = new(
        @"^[a-z0-9][a-z0-9._-]{1,63}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly string[] UnsafeTextMarkers =
    [
        "raw",
        "raw payload",
        "raw_payload",
        "raw-payload",
        "unredacted",
        "secret",
        "password",
        "api_key",
        "apikey",
        "token",
        "bearer",
        "client_secret",
        "private_key",
        "external trust",
        "worm",
        "kms",
        "cloud",
        "provider://",
        "http://",
        "https://",
        "product" + "-ready",
        "public" + "-ready",
        "release-ready",
        "commercial-ready"
    ];

    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    public ProductLedgerLocalReportExportResult Export(ProductLedgerLocalReportExportRequest? request)
    {
        var blockers = new List<ProductLedgerLocalReportExportBlocker>();
        if (request is null)
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.MissingRequest);
            return Result(blockers, null);
        }

        AddScopeAndEvidenceBlockers(request, blockers);
        AddPathStringBlockers(request.AllowedRootPath, isAllowedRoot: true, blockers);
        AddPathStringBlockers(request.ReportFilePath, isAllowedRoot: false, blockers);
        AddContentBlockers(request, blockers);
        AddExternalSurfaceBlockers(request, blockers);

        var canonicalRoot = TryCanonicalize(request.AllowedRootPath, isAllowedRoot: true, blockers);
        var canonicalReport = TryCanonicalize(request.ReportFilePath, isAllowedRoot: false, blockers);
        AddBoundaryAndFilesystemBlockers(request, canonicalRoot, canonicalReport, blockers);

        ProductLedgerLocalReportExportEvidence? evidence = null;
        if (blockers.Count == 0)
        {
            evidence = WriteAndVerify(request, canonicalRoot!, canonicalReport!, blockers);
        }

        return Result(blockers, evidence);
    }

    private static void AddScopeAndEvidenceBlockers(
        ProductLedgerLocalReportExportRequest request,
        List<ProductLedgerLocalReportExportBlocker> blockers)
    {
        if (!request.ExplicitInternalLocalOnlyBoundedExportScope)
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.MissingExplicitInternalLocalOnlyBoundedExportScope);
        }

        if (!request.HasOperatorInternalEvidence)
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.MissingOperatorInternalEvidence);
        }

        if (!request.HasRedactionBeforePersistenceEvidence)
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.MissingRedactionBeforePersistenceEvidence);
        }

        if (!request.HasSafeContentEvidence)
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.MissingSafeContentEvidence);
        }

        if (!request.HasResolvedReparsePointEvidence)
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.ReparsePointEvidenceMissing);
            blockers.Add(ProductLedgerLocalReportExportBlocker.SymlinkJunctionReparsePointRiskUnresolved);
        }

        if (!request.HasTocTouMitigationEvidence)
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.TocTouMitigationMissing);
        }

        if (request.HardlinkOrMountAliasRiskUnresolved)
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.HardlinkOrMountAliasRiskUnresolved);
        }
    }

    private static void AddPathStringBlockers(
        string? path,
        bool isAllowedRoot,
        List<ProductLedgerLocalReportExportBlocker> blockers)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            blockers.Add(isAllowedRoot
                ? ProductLedgerLocalReportExportBlocker.EmptyAllowedRootPath
                : ProductLedgerLocalReportExportBlocker.EmptyReportFilePath);
            return;
        }

        if (!Path.IsPathRooted(path))
        {
            blockers.Add(isAllowedRoot
                ? ProductLedgerLocalReportExportBlocker.RelativeAllowedRootPath
                : ProductLedgerLocalReportExportBlocker.RelativeReportFilePath);
        }

        if (path.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries).Any(segment => segment == ".."))
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.PathTraversalRisk);
        }

        if (path.Contains('\\', StringComparison.Ordinal) && path.Contains('/', StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.MixedSeparatorRisk);
        }

        if (path.StartsWith(@"\\", StringComparison.Ordinal)
            || path.StartsWith("//", StringComparison.Ordinal)
            || path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.UncNetworkPathRisk);
        }

        if (EnvironmentVariablePathPattern.IsMatch(path))
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.EnvironmentVariableExpansionRisk);
        }

        if (ReservedWindowsDevicePathPattern.IsMatch(path))
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.WindowsReservedDeviceNameRisk);
        }

        if (path.Length >= 2 && char.IsLetter(path[0]) && path[1] == ':' && path.Length > 2 && path[2] is not ('\\' or '/'))
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.DriveRelativePathRisk);
        }

        if (path.StartsWith(@"\\?\", StringComparison.Ordinal)
            || path.StartsWith(@"\\.\", StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.LongPathPrefixAmbiguity);
        }

        if (path.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries)
            .Any(segment => segment.EndsWith(".", StringComparison.Ordinal) || segment.EndsWith(" ", StringComparison.Ordinal)))
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.TrailingDotOrSpaceRisk);
        }

        var pathWithoutDrive = path.Length >= 2 && char.IsLetter(path[0]) && path[1] == ':' ? path[2..] : path;
        if (pathWithoutDrive.Contains(':', StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.AlternateDataStreamRisk);
        }
    }

    private static void AddContentBlockers(
        ProductLedgerLocalReportExportRequest request,
        List<ProductLedgerLocalReportExportBlocker> blockers)
    {
        if (string.IsNullOrWhiteSpace(request.ReportContent))
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.EmptyReportContent);
        }
        else if (ContainsUnsafeText(request.ReportContent))
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.UnsafeReportContent);
        }

        if (!IsSafeMetadata(request.EvidenceMetadata))
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.UnsafeEvidenceMetadata);
        }
    }

    private static void AddExternalSurfaceBlockers(
        ProductLedgerLocalReportExportRequest request,
        List<ProductLedgerLocalReportExportBlocker> blockers)
    {
        if (request.RequestsPublicUiAction)
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.PublicUiActionRequested);
        }

        if (request.RequestsDestructiveAction)
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.DestructiveActionRequested);
        }

        if (request.RequestsProductCommandHandler)
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.ProductCommandHandlerRequested);
        }

        if (request.RequestsProductiveServiceRegistration)
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.ProductiveServiceRegistrationRequested);
        }

        if (request.ClaimsProviderCloudNetwork)
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.ProviderCloudNetworkClaimed);
        }

        if (request.ClaimsDbMigration)
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.DbMigrationClaimed);
        }

        if (request.ClaimsKmsWormExternalTrust)
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.KmsWormExternalTrustClaimed);
        }

        if (request.ClaimsBrowserCdpWcuOcrRecipesLive)
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.BrowserCdpWcuOcrRecipesLiveClaimed);
        }

        if (request.ClaimsReleaseCommercial)
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.ReleaseCommercialClaimed);
        }

        if (request.ClaimsExternalTelemetryOrSync)
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.ExternalTelemetryOrSyncClaimed);
        }

        if (request.ClaimsBillingLicensingCloud)
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.BillingLicensingCloudClaimed);
        }

        if (request.ClaimsExternalExport)
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.ExternalExportClaimed);
        }

        if (request.ClaimsUnboundedPhysicalExport)
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.UnboundedPhysicalExportClaimed);
        }
    }

    private static string? TryCanonicalize(
        string? path,
        bool isAllowedRoot,
        List<ProductLedgerLocalReportExportBlocker> blockers)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception)
        {
            blockers.Add(isAllowedRoot
                ? ProductLedgerLocalReportExportBlocker.AllowedRootCanonicalizationFailed
                : ProductLedgerLocalReportExportBlocker.ReportFileCanonicalizationFailed);
            return null;
        }
    }

    private static void AddBoundaryAndFilesystemBlockers(
        ProductLedgerLocalReportExportRequest request,
        string? canonicalRoot,
        string? canonicalReport,
        List<ProductLedgerLocalReportExportBlocker> blockers)
    {
        if (canonicalRoot is null || canonicalReport is null)
        {
            return;
        }

        if (!Directory.Exists(canonicalRoot))
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.AllowedRootMissing);
        }

        var root = EnsureTrailingSeparator(canonicalRoot);
        var insideBoundary = canonicalReport.StartsWith(root, StringComparison.OrdinalIgnoreCase);
        if (!insideBoundary)
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.CanonicalReportPathOutsideAllowedBoundary);
        }

        if (!insideBoundary
            && !string.IsNullOrWhiteSpace(request.ReportFilePath)
            && !string.IsNullOrWhiteSpace(request.AllowedRootPath)
            && request.ReportFilePath.StartsWith(EnsureTrailingSeparator(request.AllowedRootPath), StringComparison.OrdinalIgnoreCase))
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.DisplayedPathInsideButCanonicalOutside);
        }

        var fileName = Path.GetFileName(canonicalReport);
        if (!ReportFileNamePattern.IsMatch(fileName))
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.UnsafeReportFileName);
        }

        var extension = Path.GetExtension(canonicalReport);
        if (!extension.Equals(".json", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".md", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".txt", StringComparison.OrdinalIgnoreCase))
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.NonReportExtension);
        }

        if (File.Exists(canonicalReport) && !request.AllowOverwriteExisting)
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.ExistingFileRequiresExplicitSafeOverwrite);
        }

        if (HasReparsePoint(canonicalRoot)
            || (File.Exists(canonicalReport) && HasReparsePoint(canonicalReport))
            || (Directory.Exists(Path.GetDirectoryName(canonicalReport)) && HasReparsePoint(Path.GetDirectoryName(canonicalReport)!)))
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.SymlinkJunctionReparsePointRiskUnresolved);
        }
    }

    private static ProductLedgerLocalReportExportEvidence? WriteAndVerify(
        ProductLedgerLocalReportExportRequest request,
        string canonicalRoot,
        string canonicalReport,
        List<ProductLedgerLocalReportExportBlocker> blockers)
    {
        var directory = Path.GetDirectoryName(canonicalReport)!;
        Directory.CreateDirectory(directory);
        File.WriteAllText(canonicalReport, request.ReportContent!, Utf8NoBom);

        var bytes = File.ReadAllBytes(canonicalReport);
        var expectedHash = HashText(request.ReportContent!);
        var actualHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if (!string.Equals(expectedHash, actualHash, StringComparison.Ordinal))
        {
            blockers.Add(ProductLedgerLocalReportExportBlocker.PostWriteHashVerificationFailed);
            return null;
        }

        var metadata = request.EvidenceMetadata!
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        return new ProductLedgerLocalReportExportEvidence(
            CanonicalAllowedRootPath: canonicalRoot,
            CanonicalReportFilePath: canonicalReport,
            ReportHash: actualHash,
            ByteCount: bytes.LongLength,
            EvidenceMetadata: metadata,
            StatusText: ReadyStatus);
    }

    private static ProductLedgerLocalReportExportResult Result(
        IReadOnlyList<ProductLedgerLocalReportExportBlocker> blockers,
        ProductLedgerLocalReportExportEvidence? evidence)
    {
        var distinct = blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
        var exported = distinct.Length == 0 && evidence is not null;
        return new ProductLedgerLocalReportExportResult(
            Decision: exported
                ? ProductLedgerLocalReportExportDecision.ExportedBoundedLocal
                : (distinct.Contains(ProductLedgerLocalReportExportBlocker.MissingRequest)
                    ? ProductLedgerLocalReportExportDecision.Rejected
                    : ProductLedgerLocalReportExportDecision.Blocked),
            Blockers: distinct,
            Evidence: exported ? evidence : null,
            InternalOnly: true,
            LocalOnly: true,
            Bounded: true,
            NonDestructive: true,
            FailClosed: true,
            PhysicalExportCreated: exported,
            FileWritePerformed: exported,
            PostWriteHashVerified: exported,
            PublicUiActionAvailable: false,
            DestructiveActionAvailable: false,
            ProductCommandHandlerAvailable: false,
            ProductiveServiceRegistrationAvailable: false,
            ProviderCloudNetworkAvailable: false,
            DbMigrationAvailable: false,
            KmsWormExternalTrustAvailable: false,
            BrowserCdpWcuOcrRecipesLiveAvailable: false,
            ExternalTelemetryOrSyncAvailable: false,
            BillingLicensingCloudAvailable: false,
            ReleaseCommercialReady: false,
            StatusText: exported ? ReadyStatus : RejectedStatus);
    }

    private static bool IsSafeMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return false;
        }

        foreach (var pair in metadata)
        {
            if (!MetadataKeyPattern.IsMatch(pair.Key)
                || string.IsNullOrWhiteSpace(pair.Value)
                || pair.Value.Length > 128
                || pair.Value.Contains("..", StringComparison.Ordinal)
                || pair.Value.Contains('\\', StringComparison.Ordinal)
                || ContainsUnsafeText(pair.Key)
                || ContainsUnsafeText(pair.Value))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ContainsUnsafeText(string value) =>
        UnsafeTextMarkers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));

    private static string EnsureTrailingSeparator(string path) =>
        path.EndsWith(Path.DirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;

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

    private static string HashText(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }
}
