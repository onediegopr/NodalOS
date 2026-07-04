using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerPathCanonicalizationValidatorTests
{
    [TestMethod]
    public void CanonicalizationValidator_FailsClosedByDefault()
    {
        var result = new ProductLedgerPathCanonicalizationValidator().Validate(null);

        Assert.AreEqual(ProductLedgerPathCanonicalizationDecision.Rejected, result.Decision);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathCanonicalizationBlocker.MissingRequest);
        AssertNoProductEnablement(result);
    }

    [TestMethod]
    public void CanonicalizationValidator_RejectsEmptyNullAndWhitespace()
    {
        var validator = new ProductLedgerPathCanonicalizationValidator();

        var cases = new[]
        {
            ReadyRequest() with { CandidatePath = null },
            ReadyRequest() with { CandidatePath = "" },
            ReadyRequest() with { CandidatePath = "   " },
            ReadyRequest() with { AllowedRootPath = null },
            ReadyRequest() with { AllowedRootPath = "" },
            ReadyRequest() with { AllowedRootPath = "   " }
        };

        foreach (var request in cases)
        {
            var result = validator.Validate(request);

            Assert.AreEqual(ProductLedgerPathCanonicalizationDecision.Rejected, result.Decision);
            Assert.IsTrue(
                result.Blockers.Contains(ProductLedgerPathCanonicalizationBlocker.EmptyCandidatePath)
                || result.Blockers.Contains(ProductLedgerPathCanonicalizationBlocker.EmptyAllowedRootPath));
            AssertNoProductEnablement(result);
        }
    }

    [TestMethod]
    public void CanonicalizationValidator_RejectsPathCorpusRisks()
    {
        var ready = ReadyRequest();
        var cases = new Dictionary<ProductLedgerPathCanonicalizationRequest, ProductLedgerPathCanonicalizationBlocker>
        {
            [ready with { CandidatePath = "relative\\ledger" }] = ProductLedgerPathCanonicalizationBlocker.RelativeCandidatePath,
            [ready with { CandidatePath = Path.Combine(RepoRoot(), "..", "outside") }] = ProductLedgerPathCanonicalizationBlocker.PathTraversalRisk,
            [ready with { CandidatePath = RepoRoot() + "/mixed" }] = ProductLedgerPathCanonicalizationBlocker.MixedSeparatorRisk,
            [ready with { CandidatePath = @"\\server\share\ledger" }] = ProductLedgerPathCanonicalizationBlocker.UncNetworkPathRisk,
            [ready with { CandidatePath = @"%TEMP%\ledger" }] = ProductLedgerPathCanonicalizationBlocker.EnvironmentVariableExpansionRisk,
            [ready with { CandidatePath = "C:ledger" }] = ProductLedgerPathCanonicalizationBlocker.DriveRelativePathRisk,
            [ready with { CandidatePath = @"\\?\C:\safe\ledger" }] = ProductLedgerPathCanonicalizationBlocker.LongPathPrefixAmbiguity,
            [ready with { CandidatePath = @"C:\safe\CON" }] = ProductLedgerPathCanonicalizationBlocker.WindowsReservedDeviceNameRisk,
            [ready with { CandidatePath = @"C:\safe\ledger:stream" }] = ProductLedgerPathCanonicalizationBlocker.AlternateDataStreamRisk,
            [ready with { CandidatePath = @"C:\safe\ledger. " }] = ProductLedgerPathCanonicalizationBlocker.TrailingDotOrSpaceRisk,
            [ready with { CandidatePath = "C:\\safe\\cafe\u0301" }] = ProductLedgerPathCanonicalizationBlocker.UnicodeNormalizationOrConfusableRisk,
            [ready with { CandidatePath = "C:\\safe\\ledg\u0435r" }] = ProductLedgerPathCanonicalizationBlocker.UnicodeNormalizationOrConfusableRisk
        };

        foreach (var testCase in cases)
        {
            var result = new ProductLedgerPathCanonicalizationValidator().Validate(testCase.Key);

            Assert.AreEqual(ProductLedgerPathCanonicalizationDecision.Rejected, result.Decision, testCase.Value.ToString());
            CollectionAssert.Contains(result.Blockers.ToArray(), testCase.Value, testCase.Value.ToString());
            AssertNoProductEnablement(result);
        }
    }

    [TestMethod]
    public void CanonicalizationValidator_RejectsDisplayedInsideCanonicalOutsideMismatch()
    {
        var root = RepoRoot();
        var displayedInsideButEscapes = Path.Combine(root, "src", "..", "..");

        var result = new ProductLedgerPathCanonicalizationValidator().Validate(
            ReadyRequest() with { CandidatePath = displayedInsideButEscapes, AllowedRootPath = Path.Combine(root, "src") });

        Assert.AreEqual(ProductLedgerPathCanonicalizationDecision.Rejected, result.Decision);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathCanonicalizationBlocker.DisplayedPathInsideButCanonicalOutside);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathCanonicalizationBlocker.CanonicalPathOutsideAllowedBoundary);
        AssertNoProductEnablement(result);
    }

    [TestMethod]
    public void CanonicalizationValidator_RejectsMissingCanonicalEvidenceAndUnresolvedReparse()
    {
        var missingCandidate = Path.Combine(RepoRoot(), "definitely-missing-ledger-candidate");
        var missingResult = new ProductLedgerPathCanonicalizationValidator().Validate(
            ReadyRequest() with { CandidatePath = missingCandidate });
        var unresolvedResult = new ProductLedgerPathCanonicalizationValidator().Validate(
            ReadyRequest() with { HasResolvedReparsePointEvidence = false });

        CollectionAssert.Contains(missingResult.Blockers.ToArray(), ProductLedgerPathCanonicalizationBlocker.CandidateCanonicalPathMissing);
        CollectionAssert.Contains(unresolvedResult.Blockers.ToArray(), ProductLedgerPathCanonicalizationBlocker.ReparsePointEvidenceMissing);
        CollectionAssert.Contains(unresolvedResult.Blockers.ToArray(), ProductLedgerPathCanonicalizationBlocker.SymlinkJunctionReparsePointRiskUnresolved);
        AssertNoProductEnablement(missingResult);
        AssertNoProductEnablement(unresolvedResult);
    }

    [TestMethod]
    public void CanonicalizationValidator_RejectsLocalTempProductLedgerAndProductReadyClaims()
    {
        var request = ReadyRequest() with
        {
            CandidatePath = Path.GetTempPath(),
            AllowedRootPath = Path.GetTempPath(),
            ClaimsLocalTempAsProductLedgerPath = true,
            ClaimsProductLedgerActive = true,
            ClaimsProductReady = true,
            NoProductLedgerWriteAssertion = false,
            NoRuntimeEnablementAssertion = false,
            NoReleaseCommercialAssertion = false,
            ClaimsExternalTrust = true,
            ClaimsWormKmsCloud = true
        };

        var result = new ProductLedgerPathCanonicalizationValidator().Validate(request);

        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathCanonicalizationBlocker.LocalTempClaimedAsProductLedgerPath);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathCanonicalizationBlocker.ProductLedgerActiveClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathCanonicalizationBlocker.ProductReadyClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathCanonicalizationBlocker.ProductLedgerWriteRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathCanonicalizationBlocker.RuntimeEnablementRequested);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathCanonicalizationBlocker.ReleaseCommercialReadinessClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathCanonicalizationBlocker.ExternalTrustClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathCanonicalizationBlocker.WormKmsCloudClaimed);
        AssertNoProductEnablement(result);
    }

    [TestMethod]
    public void CanonicalizationValidator_AllowsCandidateReadinessOnlyForSafeLocalExistingPath()
    {
        var root = RepoRoot();
        var request = ReadyRequest() with
        {
            AllowedRootPath = root,
            CandidatePath = Path.Combine(root, "src", "OneBrain.Core")
        };

        var result = new ProductLedgerPathCanonicalizationValidator().Validate(request);

        Assert.AreEqual(ProductLedgerPathCanonicalizationDecision.CandidateReadinessAllowed, result.Decision);
        Assert.IsTrue(result.CandidateReadinessAllowed);
        Assert.AreEqual(0, result.Blockers.Count);
        Assert.IsNotNull(result.CanonicalCandidatePath);
        Assert.IsNotNull(result.CanonicalAllowedRootPath);
        StringAssert.Contains(result.StatusText, "CANDIDATE_READINESS_ONLY");
        StringAssert.Contains(result.StatusText, "REAL_LOCAL_CANONICALIZATION_VALIDATOR");
        StringAssert.Contains(result.StatusText, "NO_PRODUCT_LEDGER_WRITE");
        StringAssert.Contains(result.StatusText, "NO_PRODUCT_RUNTIME_ENABLEMENT");
        AssertNoProductEnablement(result);
    }

    [TestMethod]
    public void CanonicalizationValidator_SourceContainsNoProductRegistrationHandlersWritersOrProviders()
    {
        var source = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src",
            "OneBrain.Core",
            "Approval",
            "ProductLedgerPathCanonicalizationValidator.cs"));
        var forbiddenFragments = new[]
        {
            "Add" + "Singleton",
            "Add" + "Scoped",
            "Add" + "Transient",
            "IHosted" + "Service",
            "Map" + "Post",
            "Map" + "Get",
            "Add" + "CommandHandler",
            "ICommand" + "Handler",
            "Run" + "ProductAction",
            "Product" + "ActionButton",
            "Http" + "Client",
            "Web" + "Socket",
            "Db" + "Context",
            "Migration" + "Builder",
            "Save" + "Changes",
            "File." + "AppendAllText",
            "File." + "WriteAllText",
            "Directory." + "CreateDirectory",
            "Release" + "Ready = true",
            "Commercial" + "Ready = true"
        };

        foreach (var fragment in forbiddenFragments)
        {
            Assert.IsFalse(source.Contains(fragment, StringComparison.Ordinal), fragment);
        }
    }

    private static ProductLedgerPathCanonicalizationRequest ReadyRequest() =>
        new(
            CandidatePath: Path.Combine(RepoRoot(), "src", "OneBrain.Core"),
            AllowedRootPath: RepoRoot(),
            ExplicitLocalOnlyMode: true,
            NoProductLedgerWriteAssertion: true,
            NoRuntimeEnablementAssertion: true,
            NoReleaseCommercialAssertion: true,
            ClaimsProductLedgerActive: false,
            ClaimsProductReady: false,
            ClaimsExternalTrust: false,
            ClaimsWormKmsCloud: false,
            ClaimsLocalTempAsProductLedgerPath: false,
            HasResolvedReparsePointEvidence: true,
            HasTocTouMitigationEvidence: true,
            HardlinkOrMountAliasRiskUnresolved: false);

    private static void AssertNoProductEnablement(ProductLedgerPathCanonicalizationResult result)
    {
        Assert.IsFalse(result.ProductLedgerPathActive);
        Assert.IsFalse(result.ProductLedgerWriteAllowed);
        Assert.IsFalse(result.ProductRuntimeEnabled);
        Assert.IsFalse(result.ProductServiceRegistrationAllowed);
        Assert.IsFalse(result.ProductCommandHandlersAllowed);
        Assert.IsFalse(result.UiProductActionsAllowed);
        Assert.IsFalse(result.DbProviderCloudNetworkAllowed);
        Assert.IsFalse(result.KmsWormExternalTrustAllowed);
        Assert.IsFalse(result.LiveAutomationAllowed);
        Assert.IsFalse(result.ReleaseCommercialReady);
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(
                directory.FullName,
                "src",
                "OneBrain.Core",
                "Approval",
                "ProductLedgerPathReadinessScaffold.cs")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail("repo root not found");
        return string.Empty;
    }
}
