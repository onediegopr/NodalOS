using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NexaCanonicalWorkspaceGuardService
{
    public NexaCanonicalWorkspaceGuardResult Evaluate(NexaCanonicalWorkspaceGuardConfig config, NexaCanonicalWorkspaceSnapshot snapshot)
    {
        var reasons = new List<string>();
        var legacy = config.LegacyWorkspacePaths.Any(path => SamePath(path, snapshot.WorkspacePath));
        var expectedPath = SamePath(config.ExpectedWorkspacePath, snapshot.WorkspacePath);
        var matchesRemote = string.Equals(snapshot.Head, snapshot.ExpectedRemoteHead, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(snapshot.Head, config.ExpectedRemoteHead, StringComparison.OrdinalIgnoreCase);
        var detachedAccepted = snapshot.IsDetachedHead && matchesRemote && expectedPath && !snapshot.IsDirty && !legacy;

        if (legacy)
            reasons.Add("legacy workspace path is not canonical");
        if (!expectedPath)
            reasons.Add("workspace path does not match canonical path");
        if (snapshot.IsDirty)
            reasons.Add("workspace has uncommitted changes");
        if (!matchesRemote)
            reasons.Add("HEAD does not match expected canonical remote head");
        if (!snapshot.IsDetachedHead && !string.Equals(snapshot.CurrentBranch, config.ExpectedRemoteBranch, StringComparison.Ordinal))
            reasons.Add("current branch is not the expected canonical branch");

        var allowed = reasons.Count == 0 || detachedAccepted;
        if (detachedAccepted)
            reasons.Clear();

        var message = allowed
            ? "Canonical workspace guard allowed: clean canonical workspace matches expected remote head."
            : $"Canonical workspace guard blocked: {string.Join("; ", reasons)}. Use the canonical Codigo-m12-audit worktree; do not clean or modify legacy Codigo automatically.";

        return new NexaCanonicalWorkspaceGuardResult(
            allowed ? NexaCanonicalWorkspaceGuardDecisionKind.Allowed : NexaCanonicalWorkspaceGuardDecisionKind.Blocked,
            snapshot.WorkspacePath,
            config.ExpectedWorkspacePath,
            snapshot.Head,
            snapshot.ExpectedRemoteHead,
            config.ExpectedRemoteBranch,
            snapshot.IsDirty,
            legacy,
            matchesRemote,
            detachedAccepted,
            reasons.Select(BrowserCredentialRedactor.Redact).ToArray(),
            BrowserCredentialRedactor.Redact(message),
            ModifiedWorkspace: false);
    }

    private static bool SamePath(string left, string right) =>
        string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string path) =>
        Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}

public sealed class NexaPrivatePreviewReadinessDashboardService
{
    public NexaPrivatePreviewReadinessDashboard Build(NexaSkippedTestsAuditReport skippedReport, NexaPrivatePreviewGoNoGoReport goNoGo, NexaCanonicalWorkspaceGuardResult workspaceGuard)
        => Build(skippedReport, goNoGo, workspaceGuard, externalEvidencePack: null);

    public NexaPrivatePreviewReadinessDashboard Build(
        NexaSkippedTestsAuditReport skippedReport,
        NexaPrivatePreviewGoNoGoReport goNoGo,
        NexaCanonicalWorkspaceGuardResult workspaceGuard,
        NexaExternalReadOnlyEvidencePack? externalEvidencePack)
        => Build(skippedReport, goNoGo, workspaceGuard, externalEvidencePack, liveProofSafetyGate: null);

    public NexaPrivatePreviewReadinessDashboard Build(
        NexaSkippedTestsAuditReport skippedReport,
        NexaPrivatePreviewGoNoGoReport goNoGo,
        NexaCanonicalWorkspaceGuardResult workspaceGuard,
        NexaExternalReadOnlyEvidencePack? externalEvidencePack,
        NexaLiveProofSafetyGateDecision? liveProofSafetyGate)
    {
        var externalSkipped = skippedReport.Items
            .Where(item => item.Category == NexaSkippedTestCategory.ExternalTargetBlocked)
            .ToArray();
        var localBlockedBySkipped = skippedReport.Items.Any(item => item.BlocksLocalPrivatePreview);
        var externalBlocked = externalSkipped.Length > 0;
        var candidateProof = externalEvidencePack?.CandidateForM51M65Closure == true &&
            externalEvidencePack.ProbeKind is NexaExternalProofProbeKind.RealHttpClient or NexaExternalProofProbeKind.RealChromeCdp &&
            externalEvidencePack.PersistenceStatus == NexaExternalEvidencePersistenceStatus.PersistedRedactedLedger;
        var activeBlockers = new List<string>();

        if (workspaceGuard.Decision != NexaCanonicalWorkspaceGuardDecisionKind.Allowed)
            activeBlockers.Add("canonical workspace guard blocked local preview operations");
        if (candidateProof)
            activeBlockers.Add("external HTTP read-only candidate proof exists for M51 review; M65 remains deferred pending dedicated evidence");
        else if (liveProofSafetyGate?.ReadyForReadOnlyLiveProof == true)
            activeBlockers.Add("live proof safety gate is ready, but external proof has not executed; M51/M65 remain deferred");
        else if (externalBlocked)
            activeBlockers.Add("M51/M65 external target proof blocked: no test-owned external target configured");
        activeBlockers.Add("public SaaS remains blocked");
        activeBlockers.Add("real billing remains blocked");
        activeBlockers.Add("real email remains blocked");
        activeBlockers.Add("real client credentials remain blocked");
        activeBlockers.Add("submit/pay/sign/delete remains blocked");

        var localAllowed =
            workspaceGuard.Decision == NexaCanonicalWorkspaceGuardDecisionKind.Allowed &&
            !localBlockedBySkipped &&
            goNoGo.Decision == NexaPrivatePreviewGoNoGoDecisionKind.GoForNextLocalPreview &&
            goNoGo.PublicSaasStillDisabled &&
            goNoGo.RealBillingStillDisabled &&
            goNoGo.RealEmailStillDisabled &&
            goNoGo.M51DeferredExplicit;

        var decision = new NexaPrivatePreviewReadinessDecision(
            localAllowed,
            ExternalLiveAllowed: false,
            PublicSaasAllowed: false,
            RealBillingAllowed: false,
            RealEmailAllowed: false,
            RealClientCredentialsAllowed: false,
            SensitiveRealPilotAllowed: false,
            SubmitPaySignDeleteAllowed: false,
            localAllowed ? "GO local private preview only" : "NO-GO local preview until local blockers are fixed",
            candidateProof ? "NO-GO external/live until M51 closure review accepts candidate proof; M65 remains deferred" :
            liveProofSafetyGate?.ReadyForReadOnlyLiveProof == true ? "NO-GO external/live: live gate ready but proof not executed" :
            externalBlocked ? "NO-GO external/live: test-owned external target missing" : "NO-GO external/live until live proof is explicitly executed");

        return new NexaPrivatePreviewReadinessDashboard(
            "private-preview-readiness-dashboard-local",
            Metrics(),
            activeBlockers.Select(BrowserCredentialRedactor.Redact).ToArray(),
            skippedReport.Items,
            decision,
            M51Deferred: true,
            M65Blocked: true,
            Redacted: true);
    }

    private static IReadOnlyList<NexaPrivatePreviewReadinessMetric> Metrics() =>
    [
        new(NexaReadinessArea.Engineering, 97, Estimated: true, "NODAL OS engineering stable on canonical branch"),
        new(NexaReadinessArea.BrowserRuntimeLocal, 97, Estimated: true, "local/sandbox browser runtime remains governed"),
        new(NexaReadinessArea.PrivateLocalApi, 85, Estimated: true, "private local API ready for local preview only"),
        new(NexaReadinessArea.Vault, 86, Estimated: true, "vault readiness remains synthetic/local guarded"),
        new(NexaReadinessArea.SecurityLeak, 94, Estimated: true, "leak hardening is current for local surfaces"),
        new(NexaReadinessArea.Operational, 82, Estimated: true, "operator flow, triage and go/no-go available"),
        new(NexaReadinessArea.ExternalLive, 0, Estimated: true, "external/live remains deferred until test-owned target exists")
    ];
}

public sealed class NexaOperatorBlockerExplanationService
{
    public NexaOperatorBlockerExplanation Explain(NexaOperatorBlockerScenario scenario, IReadOnlyList<string>? evidenceRefs = null)
    {
        var evidence = evidenceRefs ?? [];
        return scenario switch
        {
            NexaOperatorBlockerScenario.MissingTestOwnedExternalTarget => Build(
                scenario,
                NexaOperatorBlockerCategory.ExternalTargetMissing,
                "M51/M65 external proof is deferred because no test-owned external target is configured.",
                "Provide a controlled external target URL and ownership proof, then run the opt-in live tests.",
                ["continue local private preview", "prepare target setup without live execution"],
                ["declare external/live validated", "use third-party or sensitive sites"],
                evidence,
                "Create a test-owned external target before retrying external/live validation."),
            NexaOperatorBlockerScenario.RealCredentialsBlocked => Build(
                scenario,
                NexaOperatorBlockerCategory.Credential,
                "Real client credentials are blocked by readiness policy.",
                "Use synthetic credentials only and complete external proof plus security audit before real credential planning.",
                ["synthetic credential tests", "vault policy review"],
                ["real customer credentials", "support viewing secrets"],
                evidence,
                "Keep credentials synthetic and address M58 blockers first."),
            NexaOperatorBlockerScenario.IrreversibleActionBlocked => Build(
                scenario,
                NexaOperatorBlockerCategory.Security,
                "Submit/pay/sign/delete is blocked for private preview.",
                "Use read-only or dry-run flows that produce evidence without mutation.",
                ["read-only navigation", "dry-run reports"],
                ["submit", "pay", "sign", "delete"],
                evidence,
                "Stay in read-only/dry-run mode."),
            NexaOperatorBlockerScenario.RealBillingBlocked => Build(
                scenario,
                NexaOperatorBlockerCategory.Billing,
                "Real billing providers are disabled; only sandbox/mock billing is allowed.",
                "Use billing sandbox ledger and invoice previews without charging money.",
                ["billing sandbox", "invoice preview"],
                ["real charge", "real payment provider", "payment card storage"],
                evidence,
                "Continue with sandbox billing only."),
            NexaOperatorBlockerScenario.RealEmailBlocked => Build(
                scenario,
                NexaOperatorBlockerCategory.Email,
                "Real email delivery is disabled; only sandbox/mock outbox is allowed.",
                "Use sandbox outbox drafts and do not send messages externally.",
                ["mock outbox", "sandbox template rendering"],
                ["SMTP/API real send", "external recipient delivery"],
                evidence,
                "Continue with sandbox email only."),
            NexaOperatorBlockerScenario.PublicSaasBlocked => Build(
                scenario,
                NexaOperatorBlockerCategory.PublicSaas,
                "Public SaaS activation and public API exposure are blocked.",
                "Use local private preview and private local API only.",
                ["local product shell", "in-process private local API"],
                ["public SaaS", "public network listener", "external users"],
                evidence,
            "Do not expose NODAL OS publicly."),
            NexaOperatorBlockerScenario.NonCanonicalWorktree => Build(
                scenario,
                NexaOperatorBlockerCategory.Worktree,
            "The current workspace is not the canonical clean NODAL OS worktree.",
                "Switch to Codigo-m12-audit at the canonical remote head before running preview operations.",
                ["inspect status", "use canonical worktree"],
                ["commit from dirty legacy Codigo", "merge legacy Browser-004.x changes"],
                evidence,
                "Use the canonical Codigo-m12-audit worktree."),
            NexaOperatorBlockerScenario.SkippedTestsBlockExternalLive => Build(
                scenario,
                NexaOperatorBlockerCategory.ExternalTargetMissing,
                "Skipped external/live tests block external validation but do not block local private preview.",
                "Run only local preview or configure the required opt-in external target tests.",
                ["local private preview", "configure opt-in live tests"],
                ["claim external proof", "run against non-owned sites"],
                evidence,
                "Keep external/live denied until skipped live tests are executed safely."),
            NexaOperatorBlockerScenario.CorePermissionMissing => Build(
                scenario,
                NexaOperatorBlockerCategory.CorePermission,
                "Core did not grant permission for the requested operation.",
                "Request a Core-governed decision with policy, tenant, license and gate evidence.",
                ["request safe Core decision", "show read-only status"],
                ["UI override", "Companion authority", "bypass policy"],
                evidence,
                "Let Core decide; UI/Companion must only explain and transport."),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "Unknown blocker scenario.")
        };
    }

    private static NexaOperatorBlockerExplanation Build(
        NexaOperatorBlockerScenario scenario,
        NexaOperatorBlockerCategory category,
        string cause,
        string userExpectedAction,
        IReadOnlyList<string> safeOptions,
        IReadOnlyList<string> blockedOptions,
        IReadOnlyList<string> evidenceRefs,
        string recommendedNextStep)
    {
        var message = $"{cause} Expected action: {userExpectedAction} Recommended next step: {recommendedNextStep}";
        return new NexaOperatorBlockerExplanation(
            scenario,
            category,
            StrongRedact(cause),
            StrongRedact(userExpectedAction),
            safeOptions.Select(StrongRedact).ToArray(),
            blockedOptions.Select(StrongRedact).ToArray(),
            evidenceRefs.Select(StrongRedact).ToArray(),
            StrongRedact(recommendedNextStep),
            StrongRedact(message),
            Redacted: true);
    }

    private static string StrongRedact(string value)
    {
        var redacted = BrowserCredentialRedactor.Redact(value);
        foreach (var secret in NexaLeakHardeningCorpus.Default().SecretValues)
            redacted = redacted.Replace(secret, BrowserCredentialRedactor.Redacted, StringComparison.Ordinal);
        return redacted;
    }
}
