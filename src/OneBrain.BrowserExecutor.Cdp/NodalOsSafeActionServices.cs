using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NodalOsSafeActionEvaluator
{
    private readonly NodalOsIdentityFingerprintEvaluator identityEvaluator = new();
    private readonly NodalOsWindowLivenessMonitor perceptionMonitor = new();

    public NodalOsSafeActionRunRecord Evaluate(NodalOsSafeActionFixture fixture)
    {
        var identity = identityEvaluator.Evaluate(fixture.IdentityFixture);
        var perception = perceptionMonitor.Evaluate(fixture.PerceptionFixture);
        var denied = new List<NodalOsActionDeniedReason>();

        if (fixture.Action.ApprovalRequirement == NodalOsActionApprovalRequirement.AlwaysBlocked)
            denied.Add(AlwaysBlockedReason(fixture.Action.Category));
        if (fixture.Action.Boundary.CoreAuthorityRequired && !fixture.Action.Boundary.CoreApproved &&
            fixture.Action.ApprovalRequirement != NodalOsActionApprovalRequirement.NoApprovalNeededForObserveOnly)
            denied.Add(NodalOsActionDeniedReason.MissingCoreApproval);
        if (identity.Fingerprint.Confidence is NodalOsIdentityConfidence.Unknown or NodalOsIdentityConfidence.Low)
            denied.Add(NodalOsActionDeniedReason.IdentityNotVerified);
        if (perception.Readiness is not (NodalOsPerceptionReadiness.UsableForReadOnlyContext or NodalOsPerceptionReadiness.WarningRequiresCoreReview))
            denied.Add(NodalOsActionDeniedReason.PerceptionNotUsable);
        if (fixture.OverlayBlocked)
            denied.Add(NodalOsActionDeniedReason.OverlayBlocked);
        if (!fixture.EvidenceRedacted &&
            fixture.Action.Category is NodalOsActionCategory.LocalEvidenceReview or NodalOsActionCategory.LocalCopyToClipboardIfRedacted)
            denied.Add(NodalOsActionDeniedReason.EvidenceNotRedacted);
        if (fixture.Action.Category is NodalOsActionCategory.BlockedExternalGeneral)
            denied.Add(NodalOsActionDeniedReason.ExternalGeneralBlocked);
        if (fixture.Action.Category is NodalOsActionCategory.BlockedProduction)
            denied.Add(NodalOsActionDeniedReason.ProductionBlocked);

        var distinctDenied = denied.Where(r => r != NodalOsActionDeniedReason.None).Distinct().ToArray();
        var decision = ResolveDecision(fixture.Action, distinctDenied);
        var evidenceRefs = fixture.Action.Preconditions.Select(p => p.EvidenceRef)
            .Concat(identity.Evidence.Select(e => e.EvidenceRef))
            .Concat(perception.EvidenceRefs)
            .Append($"safe-action:{fixture.Action.ActionId}:redacted")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var evidence = new NodalOsSafeActionEvidence(
            $"safe-action-evidence:{fixture.Action.ActionId}:redacted",
            fixture.Action.ActionId,
            fixture.Action.Category,
            fixture.Action.RiskLevel,
            decision,
            distinctDenied,
            evidenceRefs,
            "redacted action metadata only; no credentials, cookies, tokens, bodies, unredacted logs, or sensitive payloads persisted",
            Redacted: true);

        return new NodalOsSafeActionRunRecord(
            $"safe-action-run-{fixture.FixtureId}",
            DateTimeOffset.UtcNow,
            fixture.Action,
            identity.Fingerprint.Confidence,
            perception.Readiness,
            decision,
            distinctDenied,
            evidence,
            BrowserCredentialRedactor.Redact(fixture.OperatorExplanation),
            ActionExecuted: decision is NodalOsActionDecision.AllowedObserveOnly or NodalOsActionDecision.AllowedLocalReadOnlyWithCoreApproval or NodalOsActionDecision.AllowedLocalDraftOnlyWithCoreApproval,
            SensitiveActionAuthorized: false);
    }

    private static NodalOsActionDeniedReason AlwaysBlockedReason(NodalOsActionCategory category) =>
        category switch
        {
            NodalOsActionCategory.BlockedCredentialEntry => NodalOsActionDeniedReason.CredentialEntryBlocked,
            NodalOsActionCategory.BlockedSubmit => NodalOsActionDeniedReason.SubmitBlocked,
            NodalOsActionCategory.BlockedPayment => NodalOsActionDeniedReason.PaymentBlocked,
            NodalOsActionCategory.BlockedDelete => NodalOsActionDeniedReason.DeleteBlocked,
            NodalOsActionCategory.BlockedSign => NodalOsActionDeniedReason.SignBlocked,
            NodalOsActionCategory.BlockedSensitiveSurface => NodalOsActionDeniedReason.SensitiveSurfaceBlocked,
            NodalOsActionCategory.BlockedExternalGeneral => NodalOsActionDeniedReason.ExternalGeneralBlocked,
            NodalOsActionCategory.BlockedProduction => NodalOsActionDeniedReason.ProductionBlocked,
            _ => NodalOsActionDeniedReason.UnsafeActionCategory
        };

    private static NodalOsActionDecision ResolveDecision(
        NodalOsSafeAction action,
        IReadOnlyCollection<NodalOsActionDeniedReason> denied)
    {
        if (action.ApprovalRequirement == NodalOsActionApprovalRequirement.AlwaysBlocked)
            return NodalOsActionDecision.BlockedAlways;
        if (denied.Count > 0)
            return NodalOsActionDecision.Denied;
        return action.Category switch
        {
            NodalOsActionCategory.ObserveOnly => NodalOsActionDecision.AllowedObserveOnly,
            NodalOsActionCategory.LocalDraftOnly => NodalOsActionDecision.AllowedLocalDraftOnlyWithCoreApproval,
            _ => NodalOsActionDecision.AllowedLocalReadOnlyWithCoreApproval
        };
    }
}

public sealed class NodalOsSafeActionFixtureHarness
{
    private readonly NodalOsSafeActionEvaluator evaluator = new();

    public IReadOnlyList<NodalOsSafeActionRunRecord> RunDefaultFixtures() =>
        CreateDefaultFixtures().Select(evaluator.Evaluate).ToArray();

    public NodalOsActionDecisionSummary BuildSummary()
    {
        var records = RunDefaultFixtures();
        return new NodalOsActionDecisionSummary(
            "safe-action-summary-m139-m141",
            records,
            records.Where(r => r.Decision is NodalOsActionDecision.AllowedObserveOnly or NodalOsActionDecision.AllowedLocalReadOnlyWithCoreApproval or NodalOsActionDecision.AllowedLocalDraftOnlyWithCoreApproval)
                .Select(r => r.Action.Category)
                .Distinct()
                .ToArray(),
            records.Where(r => r.Decision is NodalOsActionDecision.Denied or NodalOsActionDecision.BlockedAlways)
                .Select(r => r.Action.Category)
                .Distinct()
                .ToArray(),
            CoreAuthorityRequired: true,
            UiAdminCompanionAuthorityBlocked: true,
            SensitiveActionsAuthorized: false,
            Redacted: true);
    }

    public NodalOsActionBoundaryEvidence BuildBoundaryEvidence() =>
        new(
            "safe-action-boundary:m139-m141:redacted",
            CoreApprovalBoundaryEnforced: true,
            UiAdminCompanionAuthorityBlocked: true,
            IdentityPerceptionNonAuthoritative: true,
            DangerousSurfacesBlocked: true,
            [
                "credentials",
                "submit/pay/sign/delete",
                "sensitive surfaces",
                "production",
                "external CDP general-ready",
                "public SaaS"
            ],
            Redacted: true);

    public static IReadOnlyList<NodalOsSafeActionFixture> CreateDefaultFixtures()
    {
        var identities = NodalOsIdentityFixtureHarness.CreateDefaultFixtures().ToDictionary(f => f.FixtureId, StringComparer.Ordinal);
        var perceptions = NodalOsRobustPerceptionFixtureHarness.CreateDefaultFixtures().ToDictionary(f => f.FixtureId, StringComparer.Ordinal);
        var safeIdentity = identities["local-readiness-dashboard"];
        var ambiguousIdentity = identities["ambiguous-window"];
        var sensitiveIdentity = identities["sensitive-blocked-surface"];
        var safePerception = perceptions["alive-stable"];
        var overlayPerception = perceptions["system-overlay"];
        var ambiguousPerception = perceptions["ambiguous-interactive"];
        var sensitivePerception = perceptions["sensitive-surface"];

        return
        [
            Fixture("observe-only-local", Action("observe-only-local", NodalOsActionCategory.ObserveOnly, NodalOsActionRiskLevel.None, NodalOsActionApprovalRequirement.NoApprovalNeededForObserveOnly, coreApproved: false), safeIdentity, safePerception, redacted: true, overlay: false, "Observe local fixture state only."),
            Fixture("open-local-readiness-panel", Action("open-local-readiness-panel", NodalOsActionCategory.LocalPanelOpen, NodalOsActionRiskLevel.Low, NodalOsActionApprovalRequirement.CoreApprovalRequired, coreApproved: true), safeIdentity, safePerception, redacted: true, overlay: false, "Open local readiness panel under Core approval."),
            Fixture("open-local-diagnostics-panel", Action("open-local-diagnostics-panel", NodalOsActionCategory.LocalDiagnosticsOpen, NodalOsActionRiskLevel.Low, NodalOsActionApprovalRequirement.CoreApprovalRequired, coreApproved: true), safeIdentity, safePerception, redacted: true, overlay: false, "Open local diagnostics panel under Core approval."),
            Fixture("review-redacted-evidence", Action("review-redacted-evidence", NodalOsActionCategory.LocalEvidenceReview, NodalOsActionRiskLevel.Low, NodalOsActionApprovalRequirement.CoreApprovalRequired, coreApproved: true), safeIdentity, safePerception, redacted: true, overlay: false, "Review redacted evidence only."),
            Fixture("copy-redacted-log-summary", Action("copy-redacted-log-summary", NodalOsActionCategory.LocalCopyToClipboardIfRedacted, NodalOsActionRiskLevel.Medium, NodalOsActionApprovalRequirement.CoreApprovalRequired, coreApproved: true), safeIdentity, safePerception, redacted: true, overlay: false, "Copy redacted local log summary only."),
            Fixture("local-draft-only-note", Action("local-draft-only-note", NodalOsActionCategory.LocalDraftOnly, NodalOsActionRiskLevel.Low, NodalOsActionApprovalRequirement.CoreApprovalRequired, coreApproved: true), safeIdentity, safePerception, redacted: true, overlay: false, "Create local draft-only note; no submit."),
            Fixture("blocked-credential-entry", Action("blocked-credential-entry", NodalOsActionCategory.BlockedCredentialEntry, NodalOsActionRiskLevel.Prohibited, NodalOsActionApprovalRequirement.AlwaysBlocked, coreApproved: false), safeIdentity, safePerception, redacted: true, overlay: false, "Credential entry is always blocked."),
            Fixture("blocked-submit", Action("blocked-submit", NodalOsActionCategory.BlockedSubmit, NodalOsActionRiskLevel.Prohibited, NodalOsActionApprovalRequirement.AlwaysBlocked, coreApproved: false), safeIdentity, safePerception, redacted: true, overlay: false, "Submit is always blocked."),
            Fixture("blocked-payment", Action("blocked-payment", NodalOsActionCategory.BlockedPayment, NodalOsActionRiskLevel.Prohibited, NodalOsActionApprovalRequirement.AlwaysBlocked, coreApproved: false), safeIdentity, safePerception, redacted: true, overlay: false, "Payment is always blocked."),
            Fixture("blocked-delete", Action("blocked-delete", NodalOsActionCategory.BlockedDelete, NodalOsActionRiskLevel.Prohibited, NodalOsActionApprovalRequirement.AlwaysBlocked, coreApproved: false), safeIdentity, safePerception, redacted: true, overlay: false, "Delete is always blocked."),
            Fixture("blocked-sign", Action("blocked-sign", NodalOsActionCategory.BlockedSign, NodalOsActionRiskLevel.Prohibited, NodalOsActionApprovalRequirement.AlwaysBlocked, coreApproved: false), safeIdentity, safePerception, redacted: true, overlay: false, "Sign is always blocked."),
            Fixture("blocked-sensitive-surface", Action("blocked-sensitive-surface", NodalOsActionCategory.BlockedSensitiveSurface, NodalOsActionRiskLevel.Prohibited, NodalOsActionApprovalRequirement.AlwaysBlocked, coreApproved: false), sensitiveIdentity, sensitivePerception, redacted: true, overlay: false, "Sensitive surface is always blocked."),
            Fixture("blocked-external-general", Action("blocked-external-general", NodalOsActionCategory.BlockedExternalGeneral, NodalOsActionRiskLevel.Prohibited, NodalOsActionApprovalRequirement.AlwaysBlocked, coreApproved: false), safeIdentity, safePerception, redacted: true, overlay: false, "External general action is blocked."),
            Fixture("overlay-blocked-action", Action("overlay-blocked-action", NodalOsActionCategory.LocalPanelOpen, NodalOsActionRiskLevel.Low, NodalOsActionApprovalRequirement.CoreApprovalRequired, coreApproved: true), safeIdentity, overlayPerception, redacted: true, overlay: true, "Overlay prevents safe local action."),
            Fixture("ambiguous-identity-action", Action("ambiguous-identity-action", NodalOsActionCategory.LocalPanelOpen, NodalOsActionRiskLevel.Low, NodalOsActionApprovalRequirement.CoreApprovalRequired, coreApproved: true), ambiguousIdentity, ambiguousPerception, redacted: true, overlay: false, "Ambiguous identity prevents action."),
            Fixture("high-confidence-without-core", Action("high-confidence-without-core", NodalOsActionCategory.LocalPanelOpen, NodalOsActionRiskLevel.Low, NodalOsActionApprovalRequirement.CoreApprovalRequired, coreApproved: false), safeIdentity, safePerception, redacted: true, overlay: false, "Identity/perception confidence without Core approval is denied."),
            Fixture("unredacted-evidence-review", Action("unredacted-evidence-review", NodalOsActionCategory.LocalEvidenceReview, NodalOsActionRiskLevel.Medium, NodalOsActionApprovalRequirement.CoreApprovalRequired, coreApproved: true), safeIdentity, safePerception, redacted: false, overlay: false, "Unredacted evidence cannot be reviewed.")
        ];
    }

    private static NodalOsSafeAction Action(
        string actionId,
        NodalOsActionCategory category,
        NodalOsActionRiskLevel risk,
        NodalOsActionApprovalRequirement approval,
        bool coreApproved) =>
        new(
            actionId,
            category,
            risk,
            approval,
            [
                new("release gate ReadyWithRestrictions", true, "release-gate:ready-with-restrictions:redacted"),
                new("identity/fingerprint v2 signal present", true, "identity:fingerprint-v2:fixture-ready"),
                new("robust perception signal present", true, "perception:robust-fixture-ready")
            ],
            new NodalOsActionBoundary(
                CoreAuthorityRequired: approval != NodalOsActionApprovalRequirement.NoApprovalNeededForObserveOnly,
                CoreApproved: coreApproved,
                UiAdminCompanionAuthorityBlocked: true,
                IdentityGrantsAuthority: false,
                PerceptionGrantsAuthority: false,
                ProductionScopeBlocked: true,
                ExternalGeneralBlocked: true),
            RedactedPayloadOnly: true);

    private static NodalOsSafeActionFixture Fixture(
        string id,
        NodalOsSafeAction action,
        NodalOsIdentityFixture identity,
        NodalOsRobustPerceptionFixture perception,
        bool redacted,
        bool overlay,
        string explanation) =>
        new(
            id,
            action,
            identity,
            perception,
            ReleaseGateReadyWithRestrictions: true,
            EvidenceRedacted: redacted,
            OverlayBlocked: overlay,
            explanation);
}
