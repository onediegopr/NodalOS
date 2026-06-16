using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class M65DedicatedEvidencePlanService
{
    public M65DedicatedEvidencePlan CreateDefaultPlan() =>
        new(
            "m65-dedicated-evidence-plan-m100-m102",
            "https://lab.nodalos.com.ar",
            [
                Requirement(M65EvidenceScope.HttpReadOnlyExpansion, M65EvidenceRequirementStatus.NotSufficientAlone, NexaExternalProofProbeKind.RealHttpClient, "HttpReadOnlyExternal", "HTTP read-only expansion is useful evidence but cannot close M65 alone."),
                Requirement(M65EvidenceScope.ChromeCdpDomReadOnly, M65EvidenceRequirementStatus.Required, NexaExternalProofProbeKind.RealChromeCdp, "ChromeCdpExternalReadOnly", "M65 requires dedicated browser/CDP/DOM read-only proof or an explicit later scope reduction."),
                Requirement(M65EvidenceScope.PolicyBlockedUnsafeRoutes, M65EvidenceRequirementStatus.Required, NexaExternalProofProbeKind.RealHttpClient, "CorePolicyGate", "Unsafe routes must be blocked before mutation, credential, payment, or destructive action."),
                Requirement(M65EvidenceScope.DedicatedLowRiskWorkflow, M65EvidenceRequirementStatus.Required, NexaExternalProofProbeKind.RealChromeCdp, "ChromeCdpExternalReadOnly", "A dedicated low-risk external scenario workflow must be evidenced separately from M51.")
            ],
            M65ClosureReadinessStatus.ScopeDefined,
            M51EvidenceSufficient: false,
            RequiresChromeCdpDomProof: true,
            PublicSaasStillDisabled: true,
            RealBillingStillDisabled: true,
            RealEmailStillDisabled: true,
            RealCredentialsStillBlocked: true,
            SensitiveSurfacesStillBlocked: true,
            Redacted: true);

    public M65ClosureReadiness Assess(M65DedicatedEvidencePlan plan, bool scenarioPlanReady, bool chromeCdpDomEvidenceCollected)
    {
        if (!scenarioPlanReady)
            return new M65ClosureReadiness(M65ClosureReadinessStatus.ScopeDefined, "M65 scope is defined; dedicated low-risk scenarios are still required.", ["M65 scenario plan"], CanClose: false, CandidateOnly: false, Redacted: true);
        if (plan.RequiresChromeCdpDomProof && !chromeCdpDomEvidenceCollected)
            return new M65ClosureReadiness(M65ClosureReadinessStatus.RequiresChromeCdpDomProof, "M65 scenario plan is ready but Chrome/CDP/DOM proof remains missing.", ["external Chrome/CDP/DOM read-only proof"], CanClose: false, CandidateOnly: false, Redacted: true);
        return new M65ClosureReadiness(M65ClosureReadinessStatus.EvidenceCollected, "M65 dedicated evidence collected; candidate review is required before closure.", [], CanClose: false, CandidateOnly: true, Redacted: true);
    }

    private static M65EvidenceRequirement Requirement(
        M65EvidenceScope scope,
        M65EvidenceRequirementStatus status,
        NexaExternalProofProbeKind probeKind,
        string tooling,
        string reason) =>
        new(scope, status, probeKind, tooling, RequiresLedger: true, RequiresTargetVerified: true, RequiresNoSecretsCookiesTokens: true, RequiresNoSubmitMutationPaymentLogin: true, BrowserCredentialRedactor.Redact(reason));
}

public sealed class M65LowRiskExternalScenarioCatalogService
{
    public M65LowRiskExternalScenarioCatalog CreateDefaultCatalog() =>
        new(
            "https://lab.nodalos.com.ar",
            [
                Scenario("m65-landing-readonly", "/", M65LowRiskScenarioKind.LandingReadOnlyVerification, ["GET", "read metadata"], [], NexaExternalProofProbeKind.RealHttpClient, "HttpReadOnlyExternal", "redacted ledger evidence", "low", "NODAL OS may verify the landing page read-only; no submit or mutation.", true, true, M65ScenarioEvidenceMode.HttpReadOnlyScenario),
                Scenario("m65-document-readonly", "/document/", M65LowRiskScenarioKind.DocumentReadOnlyVerification, ["GET", "read synthetic document metadata"], [], NexaExternalProofProbeKind.RealChromeCdp, "ChromeCdpExternalReadOnly", "DOM read-only evidence plus ledger ref", "low", "NODAL OS must prove browser/DOM read-only behavior for M65 closure.", true, true, M65ScenarioEvidenceMode.BrowserCdpDomScenarioPending),
                Scenario("m65-report-readonly", "/report/", M65LowRiskScenarioKind.StructuredTableReportReadOnly, ["GET", "read synthetic table"], [], NexaExternalProofProbeKind.RealChromeCdp, "ChromeCdpExternalReadOnly", "DOM table evidence plus ledger ref", "low", "NODAL OS must prove structured report read-only behavior without body persistence.", true, true, M65ScenarioEvidenceMode.BrowserCdpDomScenarioPending),
                Scenario("m65-disabled-form-blocked", "/disabled-form/", M65LowRiskScenarioKind.DisabledFormPolicyVerification, ["GET policy preflight"], ["submit"], NexaExternalProofProbeKind.RealHttpClient, "CorePolicyGate", "policy blocked before mutation", "low", "NODAL OS blocks disabled form submit before any mutating action.", true, true, M65ScenarioEvidenceMode.PolicyOnlyScenario),
                Scenario("m65-login-blocked", "/blocked-login/", M65LowRiskScenarioKind.BlockedLoginPolicyVerification, ["GET policy preflight"], ["credentials", "login"], NexaExternalProofProbeKind.RealHttpClient, "CorePolicyGate", "credential flow blocked", "blocked", "NODAL OS blocks real login and credential entry.", true, true, M65ScenarioEvidenceMode.PolicyOnlyScenario),
                Scenario("m65-checkout-blocked", "/blocked-checkout/", M65LowRiskScenarioKind.BlockedCheckoutPaymentPolicyVerification, ["GET policy preflight"], ["checkout", "payment", "submit"], NexaExternalProofProbeKind.RealHttpClient, "CorePolicyGate", "payment flow blocked", "blocked", "NODAL OS blocks checkout/payment surfaces.", true, true, M65ScenarioEvidenceMode.PolicyOnlyScenario),
                Scenario("m65-destructive-blocked", "/blocked-destructive-action/", M65LowRiskScenarioKind.BlockedDestructiveActionPolicyVerification, ["GET policy preflight"], ["delete", "sign", "mutate"], NexaExternalProofProbeKind.RealHttpClient, "CorePolicyGate", "destructive action blocked", "blocked", "NODAL OS blocks delete/sign/mutate actions.", true, true, M65ScenarioEvidenceMode.PolicyOnlyScenario),
                Scenario("m65-multipage-readonly", "/products/ -> /document/ -> /report/", M65LowRiskScenarioKind.SyntheticMultiPageReadOnlyWorkflow, ["GET", "read DOM", "navigate read-only"], [], NexaExternalProofProbeKind.RealChromeCdp, "ChromeCdpExternalReadOnly", "multi-page DOM read-only ledger evidence", "low", "NODAL OS requires a synthetic multi-page browser workflow before M65 candidate closure.", true, true, M65ScenarioEvidenceMode.BrowserCdpDomScenarioPending),
                Scenario("m65-safe-filter-readonly", "/products/?filter=synthetic", M65LowRiskScenarioKind.SafeSearchFilterReadOnly, ["GET", "read filtered synthetic list"], ["POST search"], NexaExternalProofProbeKind.RealChromeCdp, "ChromeCdpExternalReadOnly", "read-only filter evidence", "low", "NODAL OS may only use safe static filters without server mutation.", true, false, M65ScenarioEvidenceMode.BrowserCdpDomScenarioPending),
                Scenario("m65-download-metadata-only", "/document/", M65LowRiskScenarioKind.SyntheticDownloadMetadataOnly, ["metadata inspection"], ["file download"], NexaExternalProofProbeKind.RealHttpClient, "HttpReadOnlyExternal", "metadata-only, no file download", "low", "NODAL OS may model download metadata but cannot treat it as closure evidence.", true, false, M65ScenarioEvidenceMode.NotEnoughForClosure)
            ],
            UsesRealCustomerData: false,
            UsesCredentials: false,
            AllowsSubmitMutationPaymentLogin: false,
            PlanReady: true,
            Redacted: true);

    private static M65LowRiskExternalScenario Scenario(
        string id,
        string path,
        M65LowRiskScenarioKind kind,
        IReadOnlyList<string> allowed,
        IReadOnlyList<string> denied,
        NexaExternalProofProbeKind probeKind,
        string tooling,
        string evidence,
        string risk,
        string explanation,
        bool ledger,
        bool relevant,
        M65ScenarioEvidenceMode mode) =>
        new(id, path, kind, allowed, denied, probeKind, tooling, BrowserCredentialRedactor.Redact(evidence), risk, BrowserCredentialRedactor.Redact(explanation), ledger, relevant, mode, Redacted: true);
}

public sealed class M65DedicatedEvidenceReviewer
{
    public M65DedicatedEvidenceReview Review(M65DedicatedEvidenceReviewInput input)
    {
        var reasons = new List<string>();
        if (input.PublicSaasEnabled)
            reasons.Add("public SaaS remains blocked for M65");
        if (input.RealBillingEnabled || input.RealEmailEnabled)
            reasons.Add("real billing/email remains blocked for M65");
        if (input.RealCredentialsEnabled)
            reasons.Add("real credentials remain blocked for M65");
        if (input.SensitiveSurfaceEnabled)
            reasons.Add("sensitive surfaces remain blocked for M65");
        if (!input.ScenarioPlanReady)
            reasons.Add("M65 low-risk scenario plan required");
        if (!input.TargetVerified)
            reasons.Add("verified test-owned target required");
        if (!input.LedgerRefPresent)
            reasons.Add("persisted redacted ledger evidence required");
        if (!input.ReadOnlyProofPassed)
            reasons.Add("dedicated read-only proof required");
        if (input.ProofKind == NexaExternalProofProbeKind.ModeledFake)
            reasons.Add("modeled/fake proof cannot close M65");
        if (input.ProofKind == NexaExternalProofProbeKind.RealChromeCdp &&
            !string.Equals(input.Tooling, "ChromeCdpExternalReadOnly", StringComparison.Ordinal))
            reasons.Add("Chrome/CDP proof requires ChromeCdpExternalReadOnly tooling");
        if (input.ProofKind == NexaExternalProofProbeKind.RealHttpClient &&
            input.ScopeRequiresChromeCdpDomProof)
            reasons.Add("HTTP proof does not satisfy Chrome/CDP/DOM evidence requirement");
        if (input.SecretsCookiesTokensDetected)
            reasons.Add("secret/cookie/token leak blocks M65");
        if (input.SubmitMutationPaymentLoginDetected)
            reasons.Add("submit/mutation/payment/login blocks M65");
        if (input.PolicyViolationDetected)
            reasons.Add("policy violation blocks M65");
        if (input.M51Closed && input.ProofKind == NexaExternalProofProbeKind.RealHttpClient && input.ScopeRequiresChromeCdpDomProof && !input.ChromeCdpDomProofPassed)
            reasons.Add("M51 HTTP evidence alone is not sufficient for M65; Chrome/CDP/DOM proof required");

        var dangerous = input.PublicSaasEnabled || input.RealBillingEnabled || input.RealEmailEnabled || input.RealCredentialsEnabled || input.SensitiveSurfaceEnabled ||
            input.SecretsCookiesTokensDetected || input.SubmitMutationPaymentLoginDetected || input.PolicyViolationDetected;
        var status = dangerous
            ? M65ClosureReadinessStatus.BlockedByPolicy
            : input.ScopeRequiresChromeCdpDomProof && !input.ChromeCdpDomProofPassed
                ? M65ClosureReadinessStatus.RequiresChromeCdpDomProof
                : input.ProofKind == NexaExternalProofProbeKind.RealHttpClient && input.ScopeRequiresChromeCdpDomProof
                    ? M65ClosureReadinessStatus.RequiresChromeCdpDomProof
                : reasons.Count > 0
                    ? M65ClosureReadinessStatus.DeferredNeedsDedicatedEvidence
                    : M65ClosureReadinessStatus.CandidateCloseM65;

        var candidate = status == M65ClosureReadinessStatus.CandidateCloseM65;
        return new M65DedicatedEvidenceReview(
            status,
            reasons.Select(BrowserCredentialRedactor.Redact).ToArray(),
            candidate ? "CandidateCloseM65 requires explicit human review; no automatic closure." : "DoNotClose M65; dedicated evidence remains incomplete.",
            candidate,
            PublicSaasStillDisabled: !input.PublicSaasEnabled,
            RealBillingStillDisabled: !input.RealBillingEnabled,
            RealEmailStillDisabled: !input.RealEmailEnabled,
            RealCredentialsStillBlocked: !input.RealCredentialsEnabled,
            SensitiveSurfacesStillBlocked: !input.SensitiveSurfaceEnabled,
            Redacted: true);
    }
}

public sealed class M65FormalClosureReviewService
{
    public M65FormalClosureReview Review(M65FormalClosureReviewInput input)
    {
        var reasons = new List<string>();
        if (!input.CandidateReview.CandidateCloseM65)
            reasons.Add("M65 candidate review is required before formal closure");
        if (!string.Equals(input.TargetBaseUrl, "https://lab.nodalos.com.ar", StringComparison.OrdinalIgnoreCase))
            reasons.Add("M65 formal closure is limited to https://lab.nodalos.com.ar");
        if (input.ProofKind != NexaExternalProofProbeKind.RealChromeCdp)
            reasons.Add("RealChromeCdp proof required");
        if (!string.Equals(input.Tooling, "ChromeCdpExternalReadOnly", StringComparison.Ordinal))
            reasons.Add("ChromeCdpExternalReadOnly tooling required");
        if (!input.Capabilities.Contains("BrowserNavigationReadOnly") ||
            !input.Capabilities.Contains("DomSnapshotReadOnly") ||
            !input.Capabilities.Contains("PageMetadataReadOnly") ||
            !input.Capabilities.Contains("NetworkMetadataRedacted") ||
            !input.Capabilities.Contains("CoreGoverned"))
            reasons.Add("read-only Chrome/CDP capability set is incomplete");
        if (!input.LedgerPersisted || string.IsNullOrWhiteSpace(input.LedgerRef) || string.IsNullOrWhiteSpace(input.LedgerHash))
            reasons.Add("persisted redacted ledger ref and hash required");
        if (!input.IsolatedProfile)
            reasons.Add("isolated non-personal profile required");
        if (!input.NoSecretsCookiesTokens)
            reasons.Add("secret/cookie/token leak blocks M65 closure");
        if (!input.NoFullDomOrBodyPersisted)
            reasons.Add("full DOM/body persistence blocks M65 closure");
        if (!input.NoSubmitMutationPaymentLogin)
            reasons.Add("submit/mutation/payment/login blocks M65 closure");
        if (!input.BlockedRoutesPolicyVerified)
            reasons.Add("blocked route policy verification required");

        var inflation = input.GeneralExternalCdpRequested || input.PublicSaasEnabled || input.PublicApiEnabled ||
            input.RealBillingEnabled || input.RealEmailEnabled || input.RealCredentialsEnabled || input.SensitiveSitesEnabled;
        if (inflation)
            reasons.Add("scope inflation blocked: M65 does not unlock general external CDP, SaaS, public API, billing, email, credentials, or sensitive sites");

        var decision = inflation
            ? M65FormalClosureDecision.ScopeInflationBlocked
            : reasons.Count == 0
                ? M65FormalClosureDecision.FormallyClosedTargetOwnedReadOnlyCdp
                : input.CandidateReview.CandidateCloseM65
                    ? M65FormalClosureDecision.NeedsAdditionalEvidence
                    : M65FormalClosureDecision.DoNotClose;

        return new M65FormalClosureReview(
            decision,
            M65ClosureScope.TargetOwnedExternalLowRiskChromeCdpDomReadOnly,
            BrowserCredentialRedactor.Redact(input.TargetBaseUrl),
            decision == M65FormalClosureDecision.FormallyClosedTargetOwnedReadOnlyCdp
                ? "M65 formally closed only for target-owned low-risk Chrome/CDP/DOM read-only proof."
                : "M65 formal closure denied or still candidate-only.",
            reasons.Select(BrowserCredentialRedactor.Redact).ToArray(),
            BrowserCredentialRedactor.Redact(input.LedgerRef),
            BrowserCredentialRedactor.Redact(input.LedgerHash),
            ExternalCdpGeneralReady: false,
            PublicSaasStillDisabled: !input.PublicSaasEnabled,
            PublicApiStillDisabled: !input.PublicApiEnabled,
            RealBillingStillDisabled: !input.RealBillingEnabled,
            RealEmailStillDisabled: !input.RealEmailEnabled,
            RealCredentialsStillBlocked: !input.RealCredentialsEnabled,
            SensitiveSitesStillBlocked: !input.SensitiveSitesEnabled,
            Redacted: true);
    }
}

public sealed class ExternalCdpScopeLockService
{
    public ExternalCdpScopeLockDecision Evaluate(ExternalCdpScopeLockRequest request)
    {
        var reasons = new List<string>();
        ExternalCdpScopeLockStatus status;

        if (request.GeneralExternalCdpRequested)
        {
            status = ExternalCdpScopeLockStatus.GeneralExternalCdpBlocked;
            reasons.Add("general external CDP remains blocked after M65");
        }
        else if (request.ProductionModeRequested)
        {
            status = ExternalCdpScopeLockStatus.ProductionExternalCdpBlocked;
            reasons.Add("production external CDP remains blocked");
        }
        else if (request.IsSensitiveTarget)
        {
            status = ExternalCdpScopeLockStatus.SensitiveExternalCdpBlocked;
            reasons.Add("sensitive external targets remain blocked");
        }
        else if (request.IsThirdPartyTarget)
        {
            status = ExternalCdpScopeLockStatus.ThirdPartyExternalCdpBlocked;
            reasons.Add("third-party external targets require separate evidence and are blocked now");
        }
        else if (request.CredentialsRequested || request.SubmitPaySignDeleteRequested)
        {
            status = ExternalCdpScopeLockStatus.RequiresDedicatedApproval;
            reasons.Add("credentials and submit/pay/sign/delete require dedicated approval and remain blocked");
        }
        else if (!request.HasDedicatedApproval || !request.HasNewEvidence)
        {
            status = ExternalCdpScopeLockStatus.RequiresNewEvidence;
            reasons.Add("new external CDP target or capability requires dedicated approval and new evidence");
        }
        else
        {
            status = ExternalCdpScopeLockStatus.TargetOwnedProofOnly;
        }

        var allowed = status == ExternalCdpScopeLockStatus.TargetOwnedProofOnly &&
            request.M65FormallyClosed &&
            request.IsTargetOwnedLabHost &&
            string.Equals(request.RequestedTargetHost, "lab.nodalos.com.ar", StringComparison.OrdinalIgnoreCase);

        if (!request.M65FormallyClosed)
            reasons.Add("M65 formal closure required for target-owned proof status");
        if (!request.IsTargetOwnedLabHost || !string.Equals(request.RequestedTargetHost, "lab.nodalos.com.ar", StringComparison.OrdinalIgnoreCase))
            reasons.Add("only lab.nodalos.com.ar target-owned proof status is recognized");

        return new ExternalCdpScopeLockDecision(
            allowed ? ExternalCdpScopeLockStatus.TargetOwnedProofOnly : status,
            allowed,
            BrowserRuntimeExternalGeneralReady: false,
            reasons.Select(BrowserCredentialRedactor.Redact).ToArray(),
            allowed
                ? "External CDP status is locked to target-owned lab.nodalos.com.ar read-only proof only."
                : "External CDP remains blocked outside the target-owned read-only proof scope.",
            Redacted: true);
    }
}
