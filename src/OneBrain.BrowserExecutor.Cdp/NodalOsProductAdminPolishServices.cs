using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NodalOsProductAdminPolishService
{
    public NodalOsProductAdminPolishSummary BuildSummary() =>
        new(
            "NODAL OS",
            "Private preview local stable under ReadyWithRestrictions",
            "M51 closed: HTTP read-only target-owned proof with persisted ledger; not browser/DOM general proof",
            "M65 closed: limited target-owned Chrome/CDP/DOM read-only proof; external general CDP remains false",
            "HITO-162 replacement stable local fixture-first: identity, perception, safe actions, process memory",
            [
                "Identity/Fingerprint v2 readiness: 85-90%",
                "Robust perception readiness: 75-85%",
                "Safe action expansion readiness: 70-80%",
                "Process memory/workflow learning readiness: 65-75%",
                "Cross-signal consistency: stable local fixture-first"
            ],
            [
                NodalOsProductAdminPolishState.ProductAdminPreviewStable,
                NodalOsProductAdminPolishState.Hito162ReplacementStable,
                NodalOsProductAdminPolishState.LocalFixtureSignalsReady,
                NodalOsProductAdminPolishState.ActionAuthorityCoreOnly,
                NodalOsProductAdminPolishState.ExternalGeneralStillBlocked,
                NodalOsProductAdminPolishState.ProductionStillBlocked
            ],
            ExternalGeneralCdpReady: false,
            ProductionReady: false,
            PublicSaasReady: false,
            RecorderReplayProductiveEnabled: false,
            CoreAuthorityRequired: true,
            [
                "production/SaaS public blocked",
                "public API real blocked",
                "billing/email real blocked",
                "credentials blocked",
                "sensitive sites blocked",
                "submit/pay/sign/delete blocked",
                "productive recorder/replay blocked",
                "external CDP general-ready blocked"
            ],
            "Continue internal local private preview iteration; use Product/Admin polish and operator UX guide.",
            [
                "m51:http-readonly-target-owned-ledger:redacted",
                "m65:target-owned-cdp-ledger:redacted",
                "hito-162:replacement-stable-local-fixture-first:redacted",
                "cross-signal:consistent:redacted"
            ],
            Redacted: true);
}

public sealed class NodalOsOperatorUxDecisionClarityService
{
    public NodalOsOperatorUxDecisionClaritySummary BuildSummary() =>
        new(
            "NODAL OS",
            NodalOsOperatorUxDecision.ReadyWithRestrictions,
            "Continue internal local private preview with Product/Admin, readiness, evidence review, and local issue triage.",
            "Do not use production, SaaS public, public API, real credentials, sensitive sites, submit/pay/sign/delete, or productive recorder/replay.",
            "Blocked surfaces exceed the approved local private preview scope and require dedicated evidence, Core gates, and explicit future approval.",
            [
                "operator-guide:m148-m150:redacted",
                "product-admin-polish:m148:redacted",
                "hito-162-replacement:stable:redacted",
                "release-gate:ready-with-restrictions:redacted"
            ],
            HumanInterventionRequired: false,
            "Create a local private preview issue report; stop immediately on credential/login/payment/delete/sensitive/scope-inflation signals.",
            [
                "credential/login prompt appears",
                "submit/pay/sign/delete requested",
                "sensitive site or real customer data appears",
                "external general CDP is requested",
                "unredacted evidence or token/cookie appears",
                "scope inflation warning appears"
            ],
            [
                "Identity mismatch: stop or request Core/human review.",
                "Perception blocker: do not proceed until resolved.",
                "Safe action denied: use denied reason, do not bypass.",
                "Process memory denied: keep memory local-only and redacted.",
                "Scope inflation: block and file issue."
            ],
            [
                "production/SaaS public blocked",
                "credentials blocked",
                "sensitive sites blocked",
                "submit/pay/sign/delete blocked",
                "productive recorder/replay blocked"
            ],
            Redacted: true);
}

public sealed class NodalOsInternalPreviewIterationService
{
    public NodalOsInternalPreviewIterationRunRecord RunModeledIteration(string commit)
    {
        var product = new NodalOsProductAdminPolishService().BuildSummary();
        var op = new NodalOsOperatorUxDecisionClarityService().BuildSummary();
        return new NodalOsInternalPreviewIterationRunRecord(
            "m148-m150-internal-preview-iteration-3",
            DateTimeOffset.UtcNow,
            commit,
            "internal local private preview only; no production, no SaaS public, no external general CDP",
            product,
            op,
            [
                "identity/fingerprint v2 stable fixture signal",
                "robust perception stable fixture signal",
                "safe action boundary stable fixture signal",
                "process memory local-only stable fixture signal",
                "cross-signal consistency stable"
            ],
            [
                "review Product/Admin summary",
                "review Operator UX next action guidance",
                "review HITO-162 replacement status",
                "review active blockers",
                "review redacted evidence/log refs",
                "review local issue capture"
            ],
            product.ActiveBlockers,
            product.EvidenceRefs.Concat(op.EvidenceRefs).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            [],
            NodalOsOperatorUxDecision.ContinueInternalPreviewStable,
            ScopeExpanded: false,
            ProofLiveExecuted: false,
            Redacted: true);
    }

    public NodalOsOperatorUxDecision DecidePostRun(IReadOnlyList<NodalOsPrivatePreviewIssue> issues, bool scopeExpanded)
    {
        if (scopeExpanded)
            return NodalOsOperatorUxDecision.BlockedByScopeInflation;
        if (issues.Any(i => i.Category == NodalOsPrivatePreviewIssueCategory.SecurityBlocker ||
            i.Severity is NodalOsPrivatePreviewIssueSeverity.Critical or NodalOsPrivatePreviewIssueSeverity.High))
            return NodalOsOperatorUxDecision.BlockedBySecurityIssue;
        if (issues.Any(i => i.Category == NodalOsPrivatePreviewIssueCategory.ProductAdminBug))
            return NodalOsOperatorUxDecision.NeedsProductAdminPolish;
        if (issues.Any(i => i.Category == NodalOsPrivatePreviewIssueCategory.BlockerExplanationWeak))
            return NodalOsOperatorUxDecision.NeedsOperatorUxFixes;
        if (issues.Any(i => i.Severity is NodalOsPrivatePreviewIssueSeverity.Low or NodalOsPrivatePreviewIssueSeverity.Medium))
            return NodalOsOperatorUxDecision.ContinueWithMinorFixes;
        return NodalOsOperatorUxDecision.ContinueInternalPreviewStable;
    }
}

