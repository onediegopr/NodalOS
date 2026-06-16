using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.BrowserExecutor.Cdp;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.Safety.Tests;

[TestClass]
public sealed class M65DedicatedEvidenceM100M102Tests
{
    [TestMethod]
    public void M65DedicatedEvidencePlanDefinesScopeWithoutClosingM65()
    {
        var plan = new M65DedicatedEvidencePlanService().CreateDefaultPlan();

        Assert.AreEqual(M65ClosureReadinessStatus.ScopeDefined, plan.Status);
        Assert.IsFalse(plan.M51EvidenceSufficient);
        Assert.IsTrue(plan.RequiresChromeCdpDomProof);
        Assert.IsTrue(plan.Requirements.Any(r => r.Scope == M65EvidenceScope.ChromeCdpDomReadOnly && r.Status == M65EvidenceRequirementStatus.Required));
        Assert.IsTrue(plan.PublicSaasStillDisabled);
        Assert.IsTrue(plan.RealBillingStillDisabled);
        Assert.IsTrue(plan.RealEmailStillDisabled);
        Assert.IsTrue(plan.RealCredentialsStillBlocked);
        Assert.IsTrue(plan.SensitiveSurfacesStillBlocked);
    }

    [TestMethod]
    public void M65DedicatedEvidencePlanAdrDocumentsScopeAndDeferredStatus()
    {
        var doc = File.ReadAllText(SourcePath("docs", "adr", "m65-dedicated-evidence-plan-m100-m102.md"));

        StringAssert.Contains(doc, "M51 is closed with strict scope");
        StringAssert.Contains(doc, "M65 remains `DeferredNeedsDedicatedEvidence`");
        StringAssert.Contains(doc, "M51 evidence alone");
        StringAssert.Contains(doc, "external browser/CDP/DOM read-only proof");
        StringAssert.Contains(doc, "SaaS, public API, billing real, email real, real credentials");
    }

    [TestMethod]
    public void M65DedicatedEvidencePlanCanRemainScopeDefinedWithoutClosure()
    {
        var plan = new M65DedicatedEvidencePlanService().CreateDefaultPlan();
        var readiness = new M65DedicatedEvidencePlanService().Assess(plan, scenarioPlanReady: false, chromeCdpDomEvidenceCollected: false);

        Assert.AreEqual(M65ClosureReadinessStatus.ScopeDefined, readiness.Status);
        Assert.IsFalse(readiness.CanClose);
        CollectionAssert.Contains(readiness.MissingEvidence.ToArray(), "M65 scenario plan");
    }

    [TestMethod]
    public void M65DedicatedEvidencePlanRequiresDedicatedEvidenceBeyondM51()
    {
        var review = new M65DedicatedEvidenceReviewer().Review(Input(
            proofKind: NexaExternalProofProbeKind.RealHttpClient,
            ledger: true,
            readOnlyProof: true,
            cdpDom: false));

        Assert.AreEqual(M65ClosureReadinessStatus.RequiresChromeCdpDomProof, review.Status);
        Assert.IsFalse(review.CandidateCloseM65);
        Assert.IsTrue(review.ReasonCodes.Any(r => r.Contains("M51 HTTP evidence alone", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void M65LowRiskScenarioExpansionCatalogExists()
    {
        var catalog = new M65LowRiskExternalScenarioCatalogService().CreateDefaultCatalog();

        Assert.IsTrue(catalog.PlanReady);
        Assert.AreEqual("https://lab.nodalos.com.ar", catalog.TargetBaseUrl);
        Assert.IsFalse(catalog.UsesRealCustomerData);
        Assert.IsFalse(catalog.UsesCredentials);
        Assert.IsFalse(catalog.AllowsSubmitMutationPaymentLogin);
        Assert.IsTrue(catalog.Scenarios.Count >= 8);
    }

    [TestMethod]
    public void M65LowRiskScenarioExpansionBlocksUnsafeSurfaces()
    {
        var catalog = new M65LowRiskExternalScenarioCatalogService().CreateDefaultCatalog();

        Assert.IsTrue(catalog.Scenarios.Any(s => s.Kind == M65LowRiskScenarioKind.BlockedLoginPolicyVerification && s.DeniedActions.Contains("credentials")));
        Assert.IsTrue(catalog.Scenarios.Any(s => s.Kind == M65LowRiskScenarioKind.BlockedCheckoutPaymentPolicyVerification && s.DeniedActions.Contains("payment")));
        Assert.IsTrue(catalog.Scenarios.Any(s => s.Kind == M65LowRiskScenarioKind.BlockedDestructiveActionPolicyVerification && s.DeniedActions.Contains("delete")));
        Assert.IsTrue(catalog.Scenarios.Any(s => s.Kind == M65LowRiskScenarioKind.DisabledFormPolicyVerification && s.DeniedActions.Contains("submit")));
        Assert.IsTrue(catalog.Scenarios.All(s => s.PolicyExplanation.Contains("NODAL OS", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void M65LowRiskScenarioExpansionMarksHttpOnlyAsInsufficientWhenCdpDomRequired()
    {
        var catalog = new M65LowRiskExternalScenarioCatalogService().CreateDefaultCatalog();

        Assert.IsTrue(catalog.Scenarios.Any(s => s.EvidenceMode == M65ScenarioEvidenceMode.HttpReadOnlyScenario));
        Assert.IsTrue(catalog.Scenarios.Any(s => s.EvidenceMode == M65ScenarioEvidenceMode.BrowserCdpDomScenarioPending && s.RequiredProbeKind == NexaExternalProofProbeKind.RealChromeCdp));
        Assert.IsTrue(catalog.Scenarios.Any(s => s.EvidenceMode == M65ScenarioEvidenceMode.NotEnoughForClosure));
    }

    [TestMethod]
    public void M65ClosureGateM51EvidenceAloneCannotCloseM65()
    {
        var review = new M65DedicatedEvidenceReviewer().Review(Input(
            proofKind: NexaExternalProofProbeKind.RealHttpClient,
            ledger: true,
            readOnlyProof: true,
            cdpDom: false));

        Assert.AreNotEqual(M65ClosureReadinessStatus.CandidateCloseM65, review.Status);
        Assert.IsFalse(review.CandidateCloseM65);
    }

    [TestMethod]
    public void M65ClosureGateScenarioPlanAloneCannotCloseM65()
    {
        var review = new M65DedicatedEvidenceReviewer().Review(Input(
            proofKind: NexaExternalProofProbeKind.ModeledFake,
            ledger: false,
            readOnlyProof: false,
            cdpDom: false));

        Assert.AreEqual(M65ClosureReadinessStatus.RequiresChromeCdpDomProof, review.Status);
        Assert.IsFalse(review.CandidateCloseM65);
        Assert.IsTrue(review.ReasonCodes.Any(r => r.Contains("modeled", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void M65ClosureGateMissingLedgerBlocks()
    {
        var review = new M65DedicatedEvidenceReviewer().Review(Input(
            proofKind: NexaExternalProofProbeKind.RealChromeCdp,
            ledger: false,
            readOnlyProof: true,
            cdpDom: true));

        Assert.AreEqual(M65ClosureReadinessStatus.DeferredNeedsDedicatedEvidence, review.Status);
        Assert.IsTrue(review.ReasonCodes.Any(r => r.Contains("ledger", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void M65ClosureGateSensitiveLeakBlocks()
    {
        var review = new M65DedicatedEvidenceReviewer().Review(Input(
            proofKind: NexaExternalProofProbeKind.RealChromeCdp,
            ledger: true,
            readOnlyProof: true,
            cdpDom: true,
            secrets: true));

        Assert.AreEqual(M65ClosureReadinessStatus.BlockedByPolicy, review.Status);
        Assert.IsFalse(review.CandidateCloseM65);
    }

    [TestMethod]
    public void M65ClosureGateMutationBlocks()
    {
        var review = new M65DedicatedEvidenceReviewer().Review(Input(
            proofKind: NexaExternalProofProbeKind.RealChromeCdp,
            ledger: true,
            readOnlyProof: true,
            cdpDom: true,
            mutation: true));

        Assert.AreEqual(M65ClosureReadinessStatus.BlockedByPolicy, review.Status);
        Assert.IsFalse(review.CandidateCloseM65);
    }

    [TestMethod]
    public void M65ClosureGateDedicatedEvidenceAllPresentYieldsCandidateOnly()
    {
        var review = new M65DedicatedEvidenceReviewer().Review(Input(
            proofKind: NexaExternalProofProbeKind.RealChromeCdp,
            ledger: true,
            readOnlyProof: true,
            cdpDom: true));

        Assert.AreEqual(M65ClosureReadinessStatus.CandidateCloseM65, review.Status);
        Assert.IsTrue(review.CandidateCloseM65);
        StringAssert.Contains(review.Recommendation, "explicit human review");
    }

    [TestMethod]
    public void M65ClosureGateCandidateDoesNotOpenSensitiveSurfaces()
    {
        var review = new M65DedicatedEvidenceReviewer().Review(Input(
            proofKind: NexaExternalProofProbeKind.RealChromeCdp,
            ledger: true,
            readOnlyProof: true,
            cdpDom: true));

        Assert.IsTrue(review.PublicSaasStillDisabled);
        Assert.IsTrue(review.RealBillingStillDisabled);
        Assert.IsTrue(review.RealEmailStillDisabled);
        Assert.IsTrue(review.RealCredentialsStillBlocked);
        Assert.IsTrue(review.SensitiveSurfacesStillBlocked);
    }

    [TestMethod]
    public void M65FormalClosureValidCandidateAndLedgerClosesTargetOwnedReadOnlyCdpOnly()
    {
        var review = new M65FormalClosureReviewService().Review(FormalInput());

        Assert.AreEqual(M65FormalClosureDecision.FormallyClosedTargetOwnedReadOnlyCdp, review.Decision);
        Assert.AreEqual(M65ClosureScope.TargetOwnedExternalLowRiskChromeCdpDomReadOnly, review.Scope);
        Assert.IsFalse(review.ExternalCdpGeneralReady);
        Assert.IsTrue(review.PublicSaasStillDisabled);
        Assert.IsTrue(review.PublicApiStillDisabled);
        Assert.IsTrue(review.RealBillingStillDisabled);
        Assert.IsTrue(review.RealEmailStillDisabled);
        Assert.IsTrue(review.RealCredentialsStillBlocked);
        Assert.IsTrue(review.SensitiveSitesStillBlocked);
    }

    [TestMethod]
    public void M65FormalClosureWithoutLedgerDoesNotClose()
    {
        var review = new M65FormalClosureReviewService().Review(FormalInput(ledger: false));

        Assert.AreEqual(M65FormalClosureDecision.NeedsAdditionalEvidence, review.Decision);
        Assert.IsTrue(review.ReasonCodes.Any(r => r.Contains("ledger", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void M65FormalClosureWithoutRealChromeCdpDoesNotClose()
    {
        var review = new M65FormalClosureReviewService().Review(FormalInput(proofKind: NexaExternalProofProbeKind.RealHttpClient, tooling: "HttpReadOnlyExternal"));

        Assert.AreEqual(M65FormalClosureDecision.NeedsAdditionalEvidence, review.Decision);
        Assert.IsTrue(review.ReasonCodes.Any(r => r.Contains("RealChromeCdp", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void M65FormalClosureWithLeaksDoesNotClose()
    {
        var review = new M65FormalClosureReviewService().Review(FormalInput(noSecrets: false));

        Assert.AreEqual(M65FormalClosureDecision.NeedsAdditionalEvidence, review.Decision);
        Assert.IsTrue(review.ReasonCodes.Any(r => r.Contains("leak", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void M65FormalClosureWithMutationDoesNotClose()
    {
        var review = new M65FormalClosureReviewService().Review(FormalInput(noMutation: false));

        Assert.AreEqual(M65FormalClosureDecision.NeedsAdditionalEvidence, review.Decision);
        Assert.IsTrue(review.ReasonCodes.Any(r => r.Contains("mutation", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void M65FormalClosureScopeInflationIsBlocked()
    {
        var review = new M65FormalClosureReviewService().Review(FormalInput(generalExternalCdp: true));

        Assert.AreEqual(M65FormalClosureDecision.ScopeInflationBlocked, review.Decision);
        Assert.IsFalse(review.ExternalCdpGeneralReady);
    }

    [TestMethod]
    public void ExternalCdpScopeLockKeepsM65ClosedTargetOwnedOnly()
    {
        var decision = new ExternalCdpScopeLockService().Evaluate(ScopeRequest());

        Assert.AreEqual(ExternalCdpScopeLockStatus.TargetOwnedProofOnly, decision.Status);
        Assert.IsTrue(decision.Allowed);
        Assert.IsFalse(decision.BrowserRuntimeExternalGeneralReady);
        StringAssert.Contains(decision.OperatorMessage, "target-owned");
    }

    [TestMethod]
    public void ExternalCdpScopeLockBlocksThirdPartyTargets()
    {
        var decision = new ExternalCdpScopeLockService().Evaluate(ScopeRequest(host: "example.com", isLab: false, thirdParty: true));

        Assert.AreEqual(ExternalCdpScopeLockStatus.ThirdPartyExternalCdpBlocked, decision.Status);
        Assert.IsFalse(decision.Allowed);
    }

    [TestMethod]
    public void ExternalCdpScopeLockBlocksSensitiveTargets()
    {
        var decision = new ExternalCdpScopeLockService().Evaluate(ScopeRequest(sensitive: true));

        Assert.AreEqual(ExternalCdpScopeLockStatus.SensitiveExternalCdpBlocked, decision.Status);
        Assert.IsFalse(decision.Allowed);
    }

    [TestMethod]
    public void ExternalCdpScopeLockBlocksCredentialsAndIrreversibleActions()
    {
        var credential = new ExternalCdpScopeLockService().Evaluate(ScopeRequest(credentials: true));
        var irreversible = new ExternalCdpScopeLockService().Evaluate(ScopeRequest(irreversible: true));

        Assert.AreEqual(ExternalCdpScopeLockStatus.RequiresDedicatedApproval, credential.Status);
        Assert.AreEqual(ExternalCdpScopeLockStatus.RequiresDedicatedApproval, irreversible.Status);
        Assert.IsFalse(credential.Allowed);
        Assert.IsFalse(irreversible.Allowed);
    }

    [TestMethod]
    public void ExternalCdpScopeLockBlocksGeneralExternalCdp()
    {
        var decision = new ExternalCdpScopeLockService().Evaluate(ScopeRequest(general: true));

        Assert.AreEqual(ExternalCdpScopeLockStatus.GeneralExternalCdpBlocked, decision.Status);
        Assert.IsFalse(decision.BrowserRuntimeExternalGeneralReady);
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateReflectsFormalClosureWithoutGeneralExternalReady()
    {
        var observed = new BrowserRuntimeObservedState(
            CompanionAuthoritative: false,
            LegacyRunnerEnabled: false,
            RealProfileActive: false,
            RealVaultActive: false,
            LoginRealActive: false,
            BrowserNetworkCaptureMode.MetadataOnly,
            RequestBodyCaptureSupported: false,
            ResponseBodyCaptureSupported: false,
            SensitiveHeaderValueCaptureSupported: false,
            ReplayExecutableEnabled: false,
            DownloadMode: "disabled",
            UploadMode: "disabled",
            TargetFrameManagerHealthy: true,
            AuditLedgerIntegrityProviderKind: "hmac",
            AuditLedgerHeadSealAvailable: true,
            AuditLedgerHeadSealValid: true,
            CdpLiveProofAvailable: false,
            Browser004xLegacyIsolated: true,
            Capabilities: [],
            M65DedicatedEvidencePlanDefined: true,
            M65LowRiskScenarioPlanReady: true,
            M65ClosureGateDefined: true,
            M65RequiresChromeCdpDomProof: true,
            M65FormalClosureReviewDefined: true,
            M65ClosedTargetOwnedReadOnlyCdp: true,
            ExternalCdpScopeLockDefined: true,
            ExternalCdpGeneralReady: false);

        Assert.IsTrue(observed.M65DedicatedEvidenceReadinessAllowed);
        Assert.IsTrue(observed.ExternalCdpScopeLockAllowed);
    }

    [TestMethod]
    public void BrowserRuntimeExternalScopeGuardKeepsGeneralExternalCdpDisabled()
    {
        var decision = new ExternalCdpScopeLockService().Evaluate(ScopeRequest());

        Assert.IsTrue(decision.Allowed);
        Assert.IsFalse(decision.BrowserRuntimeExternalGeneralReady);
        StringAssert.Contains(decision.OperatorMessage, "target-owned");
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateFailsIfM65ClosureInflatesExternalCdp()
    {
        var observed = new BrowserRuntimeObservedState(
            CompanionAuthoritative: false,
            LegacyRunnerEnabled: false,
            RealProfileActive: false,
            RealVaultActive: false,
            LoginRealActive: false,
            BrowserNetworkCaptureMode.MetadataOnly,
            RequestBodyCaptureSupported: false,
            ResponseBodyCaptureSupported: false,
            SensitiveHeaderValueCaptureSupported: false,
            ReplayExecutableEnabled: false,
            DownloadMode: "disabled",
            UploadMode: "disabled",
            TargetFrameManagerHealthy: true,
            AuditLedgerIntegrityProviderKind: "hmac",
            AuditLedgerHeadSealAvailable: true,
            AuditLedgerHeadSealValid: true,
            CdpLiveProofAvailable: false,
            Browser004xLegacyIsolated: true,
            Capabilities: [],
            M65FormalClosureReviewDefined: true,
            M65ClosedTargetOwnedReadOnlyCdp: true,
            ExternalCdpScopeLockDefined: true,
            ExternalCdpGeneralReady: true);

        Assert.IsFalse(observed.ExternalCdpScopeLockAllowed);
    }

    [TestMethod]
    public void BrowserRuntimePhaseGateReflectsM65PlanWithoutClosure()
    {
        var observed = new BrowserRuntimeObservedState(
            CompanionAuthoritative: false,
            LegacyRunnerEnabled: false,
            RealProfileActive: false,
            RealVaultActive: false,
            LoginRealActive: false,
            BrowserNetworkCaptureMode.MetadataOnly,
            RequestBodyCaptureSupported: false,
            ResponseBodyCaptureSupported: false,
            SensitiveHeaderValueCaptureSupported: false,
            ReplayExecutableEnabled: false,
            DownloadMode: "disabled",
            UploadMode: "disabled",
            TargetFrameManagerHealthy: true,
            AuditLedgerIntegrityProviderKind: "hmac",
            AuditLedgerHeadSealAvailable: true,
            AuditLedgerHeadSealValid: true,
            CdpLiveProofAvailable: false,
            Browser004xLegacyIsolated: true,
            Capabilities: [],
            M65DedicatedEvidencePlanDefined: true,
            M65LowRiskScenarioPlanReady: true,
            M65ClosureGateDefined: true,
            M65RequiresChromeCdpDomProof: true);

        Assert.IsTrue(observed.M65DedicatedEvidenceReadinessAllowed);
        Assert.IsFalse(observed.M65ClosesFromM51Only);
        Assert.IsFalse(observed.M65CandidateWithoutDedicatedEvidence);
    }

    private static M65DedicatedEvidenceReviewInput Input(
        NexaExternalProofProbeKind proofKind,
        bool ledger,
        bool readOnlyProof,
        bool cdpDom,
        bool secrets = false,
        bool mutation = false) =>
        new(
            M51Closed: true,
            ScenarioPlanReady: true,
            proofKind,
            proofKind == NexaExternalProofProbeKind.RealChromeCdp ? "ChromeCdpExternalReadOnly" : "HttpReadOnlyExternal",
            LedgerRefPresent: ledger,
            TargetVerified: true,
            ReadOnlyProofPassed: readOnlyProof,
            ChromeCdpDomProofPassed: cdpDom,
            SecretsCookiesTokensDetected: secrets,
            SubmitMutationPaymentLoginDetected: mutation,
            PolicyViolationDetected: false,
            ScopeRequiresChromeCdpDomProof: true,
            PublicSaasEnabled: false,
            RealBillingEnabled: false,
            RealEmailEnabled: false,
            RealCredentialsEnabled: false,
            SensitiveSurfaceEnabled: false);

    private static M65FormalClosureReviewInput FormalInput(
        NexaExternalProofProbeKind proofKind = NexaExternalProofProbeKind.RealChromeCdp,
        string tooling = "ChromeCdpExternalReadOnly",
        bool ledger = true,
        bool noSecrets = true,
        bool noMutation = true,
        bool generalExternalCdp = false) =>
        new(
            new M65DedicatedEvidenceReviewer().Review(Input(
                proofKind: NexaExternalProofProbeKind.RealChromeCdp,
                ledger: true,
                readOnlyProof: true,
                cdpDom: true)),
            "https://lab.nodalos.com.ar",
            proofKind,
            tooling,
            ["BrowserNavigationReadOnly", "DomSnapshotReadOnly", "PageMetadataReadOnly", "NetworkMetadataRedacted", "CoreGoverned"],
            LedgerPersisted: ledger,
            ledger ? "audit-ledger-edb3e2fbb0a0446788dae17a269c0058" : null,
            ledger ? "61f52af1eebf08d59a24e5fbb72e70acf0038e7a329bff6599a0ac00c757f03e" : null,
            IsolatedProfile: true,
            NoSecretsCookiesTokens: noSecrets,
            NoFullDomOrBodyPersisted: true,
            NoSubmitMutationPaymentLogin: noMutation,
            BlockedRoutesPolicyVerified: true,
            PublicSaasEnabled: false,
            PublicApiEnabled: false,
            RealBillingEnabled: false,
            RealEmailEnabled: false,
            RealCredentialsEnabled: false,
            SensitiveSitesEnabled: false,
            GeneralExternalCdpRequested: generalExternalCdp);

    private static ExternalCdpScopeLockRequest ScopeRequest(
        string host = "lab.nodalos.com.ar",
        bool isLab = true,
        bool thirdParty = false,
        bool sensitive = false,
        bool credentials = false,
        bool irreversible = false,
        bool general = false) =>
        new(
            M65FormallyClosed: true,
            host,
            isLab,
            thirdParty,
            sensitive,
            ProductionModeRequested: false,
            CredentialsRequested: credentials,
            SubmitPaySignDeleteRequested: irreversible,
            GeneralExternalCdpRequested: general,
            HasDedicatedApproval: true,
            HasNewEvidence: true);

    private static string SourcePath(params string[] relativePath)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "OneBrain.slnx")))
                return Path.Combine(new[] { current.FullName }.Concat(relativePath).ToArray());
            current = current.Parent;
        }

        Assert.Fail("Could not locate repository root.");
        return "";
    }
}
