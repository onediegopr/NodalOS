using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Approval;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("ApprovalHumanReview")]
[TestCategory("PhaseEApprovalHumanReview")]
public sealed class ProductLedgerPathCanonicalizationValidatorTests
{
    [TestMethod]
    public void CanonicalizationValidator_ProducesNoWriteCandidateReadinessOnly()
    {
        var root = RepoRoot();
        var result = new ProductLedgerPathCanonicalizationValidator().Validate(
            new ProductLedgerPathCanonicalizationRequest(
                CandidatePath: Path.Combine(root, "src", "OneBrain.Core"),
                AllowedRootPath: root,
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
                HardlinkOrMountAliasRiskUnresolved: false));

        Assert.AreEqual(ProductLedgerPathCanonicalizationDecision.CandidateReadinessAllowed, result.Decision);
        Assert.AreEqual(0, result.Blockers.Count);
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
        StringAssert.Contains(result.StatusText, "NO_PRODUCT_LEDGER_WRITE");
        StringAssert.Contains(result.StatusText, "NO_PRODUCT_RUNTIME_ENABLEMENT");
        StringAssert.Contains(result.StatusText, "NO_ACTIVE_PRODUCT_LEDGER_PATH");
    }

    [TestMethod]
    public void CanonicalizationValidator_RejectsUnsafeRecipeCorpusWithoutProductEffects()
    {
        var root = RepoRoot();
        var result = new ProductLedgerPathCanonicalizationValidator().Validate(
            new ProductLedgerPathCanonicalizationRequest(
                CandidatePath: Path.Combine(root, "src", "..", ".."),
                AllowedRootPath: Path.Combine(root, "src"),
                ExplicitLocalOnlyMode: true,
                NoProductLedgerWriteAssertion: true,
                NoRuntimeEnablementAssertion: true,
                NoReleaseCommercialAssertion: true,
                ClaimsProductLedgerActive: true,
                ClaimsProductReady: true,
                ClaimsExternalTrust: true,
                ClaimsWormKmsCloud: true,
                ClaimsLocalTempAsProductLedgerPath: false,
                HasResolvedReparsePointEvidence: false,
                HasTocTouMitigationEvidence: false,
                HardlinkOrMountAliasRiskUnresolved: true));

        Assert.AreEqual(ProductLedgerPathCanonicalizationDecision.Rejected, result.Decision);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathCanonicalizationBlocker.DisplayedPathInsideButCanonicalOutside);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathCanonicalizationBlocker.ProductLedgerActiveClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathCanonicalizationBlocker.ProductReadyClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathCanonicalizationBlocker.ExternalTrustClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathCanonicalizationBlocker.WormKmsCloudClaimed);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathCanonicalizationBlocker.ReparsePointEvidenceMissing);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathCanonicalizationBlocker.TocTouMitigationMissing);
        CollectionAssert.Contains(result.Blockers.ToArray(), ProductLedgerPathCanonicalizationBlocker.HardlinkOrMountAliasRiskUnresolved);
        Assert.IsFalse(result.ProductLedgerPathActive);
        Assert.IsFalse(result.ProductLedgerWriteAllowed);
        Assert.IsFalse(result.ProductRuntimeEnabled);
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
