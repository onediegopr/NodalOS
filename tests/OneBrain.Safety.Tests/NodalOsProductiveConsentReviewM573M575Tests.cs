using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;
using TextStore = System.IO.File;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ProductiveConsentDesignReview")]
[TestCategory("DisabledConsentStorage")]
[TestCategory("ConsentAuditAcceptance")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsProductiveConsentReviewM573M575Tests
{
    private static readonly string[] SensitiveMarkers =
    [
        "Bear" + "er ",
        "Authorization:",
        "Cook" + "ie:",
        "password",
        "raw " + "secret",
        "api" + "_key",
        "access" + "_token",
        "refresh" + "_token",
        "private key",
        "s" + "k-"
    ];

    private readonly NodalOsPerCapabilityAccessGateService gateService = new();
    private readonly NodalOsProductiveConsentDesignReviewService reviewService = new();
    private readonly NodalOsProductiveConsentDesignReviewJsonSerializer reviewSerializer = new();
    private readonly NodalOsDisabledConsentStorageContractService storageService = new();
    private readonly NodalOsDisabledConsentStorageContractJsonSerializer storageSerializer = new();
    private readonly NodalOsConsentAuditAcceptanceService acceptanceService = new();
    private readonly NodalOsConsentAuditAcceptanceJsonSerializer acceptanceSerializer = new();

    [TestMethod]
    public void ProductiveConsentDesignReview_IsReviewOnlyNoOpAndCannotAuthorize()
    {
        var review = Review();

        Assert.IsTrue(review.IsReviewOnly);
        Assert.IsTrue(review.IsNoOp);
        Assert.IsFalse(review.CanApproveImplementation);
        Assert.IsFalse(review.CanAuthorizeProductiveConsent);
        Assert.IsFalse(review.CanPersistConsent);
        Assert.IsFalse(review.CanEnforceConsent);
        Assert.IsFalse(review.CanAuthorizeCapability);
        Assert.IsFalse(review.CanAuthorizeFilesystemAccess);
        Assert.IsFalse(review.CanAuthorizeLlmContext);
        Assert.IsFalse(review.CanUseCloud);
        AssertSafeOutput(reviewSerializer.Serialize(review));
    }

    [TestMethod]
    public void ProductiveConsentDesignReviewDecision_ClosesReviewOnly()
    {
        var decision = Review().Decision;

        Assert.IsTrue(decision.ReadyForDesignReviewCloseout);
        Assert.IsFalse(decision.ReadyForProductiveConsentImplementation);
        Assert.IsFalse(decision.ReadyForConsentPersistence);
        Assert.IsFalse(decision.ReadyForConsentEnforcement);
        Assert.IsFalse(decision.ReadyForCapabilityAuthorization);
        Assert.IsFalse(decision.ReadyForFilesystemAccess);
        Assert.IsFalse(decision.ReadyForLlmContext);
    }

    [TestMethod]
    public void DisabledConsentStorageContract_IsDisabledContractOnlyAndNonProductive()
    {
        var contract = Storage();

        Assert.IsTrue(contract.DisabledByDefault);
        Assert.IsTrue(contract.IsContractOnly);
        Assert.IsFalse(contract.UsesProductivePersistence);
        Assert.IsFalse(contract.PersistsConsent);
        Assert.IsFalse(contract.ReadsProductiveConsent);
        Assert.IsFalse(contract.WritesProductiveConsent);
        Assert.IsFalse(contract.DeletesProductiveConsent);
        Assert.IsFalse(contract.MigratesConsent);
        Assert.IsFalse(contract.SyncsConsentToCloud);
        Assert.IsFalse(contract.CanAuthorizeCapability);
        Assert.IsFalse(contract.CanAuthorizeFilesystemAccess);
        Assert.IsFalse(contract.CanAuthorizeLlmContext);
        AssertSafeOutput(storageSerializer.Serialize(contract));
    }

    [TestMethod]
    public void ConsentStorageRecordDrafts_AreDraftOnlyRedactedAndNonAuthorizing()
    {
        foreach (var record in Storage().RecordDrafts)
        {
            Assert.IsTrue(record.IsDraftOnly);
            Assert.IsFalse(record.IsAuthoritative);
            Assert.IsFalse(record.ContainsRawSecret);
            Assert.IsFalse(record.ContainsRawFileContent);
            Assert.IsFalse(record.ContainsUnredactedPath);
            Assert.IsFalse(record.CanAuthorizeRealUse);
        }
    }

    [TestMethod]
    public void StorageSafetyRules_BlockImplicitInheritanceAndRepresentationImplication()
    {
        var rules = Storage().SafetyRules.Select(rule => rule.Kind).ToHashSet();

        Assert.IsTrue(rules.Contains(NodalOsConsentStorageSafetyRuleKind.NoImplicitConsentInheritance));
        Assert.IsTrue(rules.Contains(NodalOsConsentStorageSafetyRuleKind.NoCrossCapabilityInheritance));
        Assert.IsTrue(rules.Contains(NodalOsConsentStorageSafetyRuleKind.ContentAccessDoesNotImplyRepresentationOrLlmContext));
        Assert.IsTrue(Storage().SafetyRules.All(rule => rule.BlocksProductiveUseIfMissing));
    }

    [TestMethod]
    public void StorageReadiness_BlocksProductivePersistenceAndEnforcement()
    {
        var readiness = Storage().Readiness;

        Assert.IsTrue(readiness.ReadyForStorageDesignReview);
        Assert.IsFalse(readiness.ReadyForProductivePersistence);
        Assert.IsFalse(readiness.ReadyForConsentEnforcement);
        Assert.IsFalse(readiness.ReadyForCapabilityAuthorization);
        Assert.IsFalse(readiness.ReadyForFilesystemAccess);
        Assert.IsFalse(readiness.ReadyForLlmContext);
    }

    [TestMethod]
    public void ConsentAuditAcceptance_IsAcceptanceOnlyAndCannotApprove()
    {
        var acceptance = Acceptance();

        Assert.IsTrue(acceptance.IsAcceptanceOnly);
        Assert.IsFalse(acceptance.CanApproveImplementation);
        Assert.IsFalse(acceptance.CanEnableProductiveConsent);
        Assert.IsFalse(acceptance.CanAuthorizeCapability);
        Assert.IsFalse(acceptance.CanAccessFilesystem);
        Assert.IsFalse(acceptance.CanBuildLlmContext);
        Assert.IsFalse(acceptance.CanUseCloud);
        AssertSafeOutput(acceptanceSerializer.Serialize(acceptance));
    }

    [TestMethod]
    public void ConsentAuditAcceptanceCriteria_IncludeFailClosedAuditEvidenceRollbackAndTests()
    {
        var kinds = Acceptance().AcceptanceCriteria.Select(item => item.Kind).ToHashSet();

        Assert.IsTrue(kinds.Contains(NodalOsConsentAuditCriterionKind.MissingStaleRevokedFailsClosed));
        Assert.IsTrue(kinds.Contains(NodalOsConsentAuditCriterionKind.AuditTrailRequirementsDefined));
        Assert.IsTrue(kinds.Contains(NodalOsConsentAuditCriterionKind.EvidenceTimelineRequirementsDefined));
        Assert.IsTrue(kinds.Contains(NodalOsConsentAuditCriterionKind.RollbackDisableStrategyDefined));
        Assert.IsTrue(kinds.Contains(NodalOsConsentAuditCriterionKind.AdversarialTestsRequired));
        Assert.IsTrue(Acceptance().AcceptanceCriteria.All(item => item.BlocksImplementationIfMissing));
    }

    [TestMethod]
    public void ConsentAuditAcceptanceDecision_AcceptsGovernanceBaselineOnly()
    {
        var decision = Acceptance().Decision;

        Assert.IsTrue(decision.ConsentDesignAcceptedAsGovernanceBaseline);
        Assert.IsFalse(decision.ReadyForProductiveConsentImplementation);
        Assert.IsFalse(decision.ReadyForProductiveConsentStorage);
        Assert.IsFalse(decision.ReadyForProductiveConsentEnforcement);
        Assert.IsFalse(decision.ReadyForCapabilityAuthorization);
        Assert.IsFalse(decision.ReadyForFilesystemAccess);
        Assert.IsFalse(decision.ReadyForLlmContext);
        Assert.IsFalse(decision.ReadyForCloud);
        Assert.IsFalse(decision.ReadyForRuntime);
        AssertContains(decision.RecommendedNextMilestoneRedacted, "ADR");
        AssertDoesNotContain(decision.RecommendedNextMilestoneRedacted, "direct");
    }

    [TestMethod]
    public void StaticPreviewArtifact_HasNoExternalScriptsOrNetworkResources()
    {
        var html = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m575", "productive-consent-design-review-preview.html"));

        AssertDoesNotContain(html, "<script");
        AssertDoesNotContain(html, "https://");
        AssertDoesNotContain(html, "http://");
        AssertDoesNotContain(html, "cdn");
        AssertSafeOutput(html);
    }

    [TestMethod]
    public void Artifacts_DeclareReviewStorageAndAcceptanceBlocked()
    {
        var review = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m575", "productive-consent-design-review.json"));
        var storage = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m575", "disabled-consent-storage-contract.json"));
        var acceptance = TextStore.ReadAllText(PathFor("artifacts", "agent-operations", "m575", "consent-audit-acceptance.json"));

        AssertContains(review, "\"isReviewOnly\": true");
        AssertContains(review, "\"canApproveImplementation\": false");
        AssertContains(storage, "\"disabledByDefault\": true");
        AssertContains(storage, "\"persistsConsent\": false");
        AssertContains(acceptance, "\"isAcceptanceOnly\": true");
        AssertContains(acceptance, "\"readyForProductiveConsentImplementation\": false");
        AssertSafeOutput(review + storage + acceptance);
    }

    [TestMethod]
    public void Boundary_NewProductiveConsentReviewFiles_DoNotReferenceForbiddenRuntimeOrIoPrimitives()
    {
        var source = NewSource();

        AssertDoesNotContain(source, "OneBrain." + "BrowserExecutor" + ".Cdp");
        AssertDoesNotContain(source, "Http" + "Client");
        AssertDoesNotContain(source, "Client" + "WebSocket");
        AssertDoesNotContain(source, "Process" + ".Start");
        AssertDoesNotContain(source, "System.Diagnostics." + "Process");
        AssertDoesNotContain(source, "Background" + "Service");
        AssertDoesNotContain(source, "Task.Run");
        AssertDoesNotContain(source, "File" + ".Read");
        AssertDoesNotContain(source, "File" + ".Write");
        AssertDoesNotContain(source, "File" + ".Delete");
        AssertDoesNotContain(source, "File" + ".Move");
        AssertDoesNotContain(source, "Directory" + ".");
        AssertDoesNotContain(source, "File" + "Info");
        AssertDoesNotContain(source, "Directory" + "Info");
    }

    private NodalOsProductiveConsentDesignReview Review() => reviewService.CreateReview();
    private NodalOsDisabledConsentStorageContract Storage() => storageService.CreateContract(Review(), gateService.CreateGates());
    private NodalOsConsentAuditAcceptancePack Acceptance() => acceptanceService.CreateAcceptance(Review(), Storage());

    private static void AssertContains(string value, string expected) => StringAssert.Contains(value, expected);

    private static void AssertSafeOutput(string value)
    {
        foreach (var marker in SensitiveMarkers)
            AssertDoesNotContain(value, marker);

        AssertDoesNotContain(value, "NEXA");
        AssertDoesNotContain(value, "NODRIX");
        AssertDoesNotContain(value, "HOTEP");
    }

    private static void AssertDoesNotContain(string value, string forbidden) =>
        Assert.IsFalse(value.Contains(forbidden, StringComparison.OrdinalIgnoreCase), $"Unexpected content: {forbidden}");

    private static string NewSource()
    {
        var root = FindRepoRoot();
        var files = new[]
        {
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsProductiveConsentDesignReviewContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsDisabledConsentStorageContractContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Contracts", "NodalOsConsentAuditAcceptanceContracts.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsProductiveConsentDesignReviewServices.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsDisabledConsentStorageContractServices.cs"),
            Path.Combine(root, "src", "OneBrain.AgentOperations.Core", "NodalOsConsentAuditAcceptanceServices.cs")
        };

        return string.Join(Environment.NewLine, files.Select(TextStore.ReadAllText));
    }

    private static string PathFor(params string[] segments) => Path.Combine([FindRepoRoot(), .. segments]);

    private static string FindRepoRoot()
    {
        var current = Path.GetFullPath(AppContext.BaseDirectory);
        while (!string.IsNullOrEmpty(current) && !TextStore.Exists(Path.Combine(current, "OneBrain.slnx")))
            current = Path.GetDirectoryName(current) ?? string.Empty;

        return string.IsNullOrEmpty(current) ? throw new InvalidOperationException("Repository root not found.") : current;
    }
}
