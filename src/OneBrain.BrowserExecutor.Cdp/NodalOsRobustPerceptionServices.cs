using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NodalOsWindowLivenessMonitor
{
    private readonly NodalOsIdentityFingerprintEvaluator identityEvaluator = new();

    public NodalOsPerceptionEvaluation Evaluate(NodalOsRobustPerceptionFixture fixture)
    {
        var identity = identityEvaluator.Evaluate(fixture.IdentityFixture);
        var mismatch = new List<NodalOsPerceptionMismatchReason>();
        var signals = new List<NodalOsPerceptionSignal>
        {
            Signal("identity/fingerprint v2", identity.Fingerprint.Confidence.ToString(), "identity-fixture", Required: true, Present: true),
            Signal("window title normalized", fixture.IdentityFixture.Observed.WindowTitleNormalized, "local-fixture", Required: true, Present: !string.IsNullOrWhiteSpace(fixture.IdentityFixture.Observed.WindowTitleNormalized)),
            Signal("runtime provider", fixture.IdentityFixture.Observed.RuntimeProvider, "local-fixture", Required: true, Present: !string.IsNullOrWhiteSpace(fixture.IdentityFixture.Observed.RuntimeProvider)),
            Signal("UIA availability fixture", fixture.UiaAvailable ? "available" : "unavailable", "local-fixture", Required: false, Present: fixture.UiaAvailable),
            Signal("DOM/page metadata fixture", fixture.DomMetadataAvailable ? "available" : "unavailable", "local-fixture", Required: false, Present: fixture.DomMetadataAvailable),
            Signal("overlay/block signal", fixture.BlockedSurfaceReason.ToString(), "local-fixture", Required: false, Present: fixture.BlockedSurfaceReason != NodalOsBlockedSurfaceReason.None)
        };

        if (identity.Fingerprint.Confidence is NodalOsIdentityConfidence.Unknown or NodalOsIdentityConfidence.Low)
            mismatch.Add(NodalOsPerceptionMismatchReason.MissingIdentitySignal);
        if (fixture.LivenessState is NodalOsWindowLivenessState.NotFound or NodalOsWindowLivenessState.Crashed)
            mismatch.Add(NodalOsPerceptionMismatchReason.WindowNotAlive);
        if (fixture.LivenessState is NodalOsWindowLivenessState.Frozen or NodalOsWindowLivenessState.Minimized or NodalOsWindowLivenessState.Hidden or NodalOsWindowLivenessState.Occluded or NodalOsWindowLivenessState.Stale)
            mismatch.Add(NodalOsPerceptionMismatchReason.WindowNotResponding);
        if (fixture.StabilityState is not NodalOsSurfaceStabilityState.Stable)
            mismatch.Add(NodalOsPerceptionMismatchReason.SurfaceUnstable);
        if (fixture.BlockedSurfaceReason != NodalOsBlockedSurfaceReason.None)
            mismatch.Add(NodalOsPerceptionMismatchReason.OverlayBlocksPerception);
        if (fixture.UiaTreeEmpty)
            mismatch.Add(NodalOsPerceptionMismatchReason.UiaTreeEmpty);
        if (fixture.DomMetadataEmpty)
            mismatch.Add(NodalOsPerceptionMismatchReason.DomMetadataEmpty);
        if (fixture.Truncated)
            mismatch.Add(NodalOsPerceptionMismatchReason.SurfaceTruncated);
        if (fixture.AmbiguousInteractiveSurface)
            mismatch.Add(NodalOsPerceptionMismatchReason.AmbiguousSurface);
        if (fixture.SensitiveSurface)
            mismatch.Add(NodalOsPerceptionMismatchReason.SensitiveSurface);
        if (fixture.StabilityState == NodalOsSurfaceStabilityState.Unsafe || fixture.LivenessState == NodalOsWindowLivenessState.UnsafeSurface)
            mismatch.Add(NodalOsPerceptionMismatchReason.UnsafeSurface);

        var readiness = ResolveReadiness(fixture, mismatch);
        var evidenceRef = $"perception:{fixture.FixtureId}:redacted";
        var evidence = new NodalOsWindowLivenessEvidence(
            evidenceRef,
            DateTimeOffset.UtcNow,
            fixture.LivenessState,
            fixture.StabilityState,
            signals,
            mismatch.Distinct().ToArray(),
            CoreAuthorityRequired: true,
            ActionAuthorityGranted: false,
            Redacted: true);

        return new NodalOsPerceptionEvaluation(
            $"perception-proof-{fixture.FixtureId}",
            identity,
            evidence,
            readiness,
            identity.Evidence.Select(e => e.EvidenceRef).Append(evidenceRef).ToArray(),
            "redacted perception metadata only; no OCR/vision productive output, credentials, cookies, tokens, bodies, or full DOM persisted",
            BlocksSensitiveActions: readiness is NodalOsPerceptionReadiness.Blocked or NodalOsPerceptionReadiness.Unsafe,
            CoreAuthorityRequired: true,
            ActionAuthorityGranted: false);
    }

    private static NodalOsPerceptionSignal Signal(string name, string value, string source, bool Required, bool Present) =>
        new(name, BrowserCredentialRedactor.Redact(value), source, Required, Present);

    private static NodalOsPerceptionReadiness ResolveReadiness(
        NodalOsRobustPerceptionFixture fixture,
        IReadOnlyCollection<NodalOsPerceptionMismatchReason> mismatch)
    {
        if (mismatch.Contains(NodalOsPerceptionMismatchReason.UnsafeSurface) ||
            mismatch.Contains(NodalOsPerceptionMismatchReason.SensitiveSurface))
            return NodalOsPerceptionReadiness.Unsafe;

        if (fixture.LivenessState is NodalOsWindowLivenessState.Frozen or
            NodalOsWindowLivenessState.Crashed or
            NodalOsWindowLivenessState.Stale or
            NodalOsWindowLivenessState.BlockedByOverlay ||
            fixture.StabilityState is NodalOsSurfaceStabilityState.Blocked or
            NodalOsSurfaceStabilityState.Unsafe)
            return NodalOsPerceptionReadiness.Blocked;

        if (mismatch.Count > 0)
            return NodalOsPerceptionReadiness.WarningRequiresCoreReview;

        return NodalOsPerceptionReadiness.UsableForReadOnlyContext;
    }
}

public sealed class NodalOsSystemOverlayDetector
{
    public NodalOsOverlayDetectionResult Detect(NodalOsRobustPerceptionFixture fixture)
    {
        var reason = fixture.SensitiveSurface
            ? NodalOsBlockedSurfaceReason.SensitiveBlockedSurface
            : fixture.BlockedSurfaceReason;
        var overlayDetected = reason is
            NodalOsBlockedSurfaceReason.SystemModalOverlay or
            NodalOsBlockedSurfaceReason.PermissionOverlay or
            NodalOsBlockedSurfaceReason.SecurityWarningOverlay or
            NodalOsBlockedSurfaceReason.LoadingOverlay or
            NodalOsBlockedSurfaceReason.ConsentOverlayFixture or
            NodalOsBlockedSurfaceReason.BlockedLoginOverlayFixture or
            NodalOsBlockedSurfaceReason.SensitiveBlockedSurface;

        return new NodalOsOverlayDetectionResult(
            reason,
            overlayDetected,
            overlayDetected ? NodalOsIdentityConfidence.High : NodalOsIdentityConfidence.VerifiedFixture,
            [$"overlay:{fixture.FixtureId}:redacted"],
            BlocksPerception: reason != NodalOsBlockedSurfaceReason.None,
            ActionAuthorityGranted: false,
            Redacted: true);
    }
}

public sealed class NodalOsEmptySurfaceDetector
{
    public NodalOsEmptySurfaceDetectionResult Detect(NodalOsRobustPerceptionFixture fixture)
    {
        var reason = fixture.UiaTreeEmpty
            ? NodalOsBlockedSurfaceReason.EmptyUiaTree
            : fixture.DomMetadataEmpty
                ? NodalOsBlockedSurfaceReason.EmptyDomMetadata
                : fixture.Truncated
                    ? NodalOsBlockedSurfaceReason.TruncatedSurface
                    : fixture.AmbiguousInteractiveSurface
                        ? NodalOsBlockedSurfaceReason.AmbiguousInteractiveSurface
                        : NodalOsBlockedSurfaceReason.None;

        return new NodalOsEmptySurfaceDetectionResult(
            fixture.UiaTreeEmpty,
            fixture.DomMetadataEmpty,
            fixture.Truncated,
            fixture.AmbiguousInteractiveSurface,
            reason,
            [$"empty-surface:{fixture.FixtureId}:redacted"],
            BlocksPerception: reason != NodalOsBlockedSurfaceReason.None,
            Redacted: true);
    }
}

public sealed class NodalOsPerceptionBlockerExplanationService
{
    public NodalOsPerceptionBlockerExplanation Explain(
        NodalOsOverlayDetectionResult overlay,
        NodalOsEmptySurfaceDetectionResult empty,
        NodalOsPerceptionEvaluation evaluation)
    {
        var cause = overlay.BlocksPerception
            ? $"Perception blocked by {overlay.Reason}."
            : empty.BlocksPerception
                ? $"Perception insufficient: {empty.Reason}."
                : evaluation.Readiness == NodalOsPerceptionReadiness.Unsafe
                    ? "Perception blocked because the surface is sensitive or unsafe."
                    : "Perception is available for read-only context only.";

        var expected = overlay.Reason is NodalOsBlockedSurfaceReason.PermissionOverlay or NodalOsBlockedSurfaceReason.SystemModalOverlay
            ? "Operator must resolve the local fixture overlay or stop the run."
            : "Operator should keep the run local, review evidence, and let Core decide.";

        return new NodalOsPerceptionBlockerExplanation(
            cause,
            overlay.Confidence,
            expected,
            ["review local fixture evidence", "stop and file local issue if ambiguous", "continue read-only observation only when Core allows"],
            ["submit/pay/sign/delete", "real credentials", "sensitive sites", "public SaaS", "external CDP general-ready"],
            evaluation.Readiness is NodalOsPerceptionReadiness.Blocked or NodalOsPerceptionReadiness.Unsafe
                ? "Do not proceed until perception blocker is cleared or explicitly reviewed by Core."
                : "Use perception as read-only context; do not treat it as action authority.",
            overlay.EvidenceRefs.Concat(empty.EvidenceRefs).Concat(evaluation.EvidenceRefs).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            ActionAuthorityGranted: false,
            Redacted: true);
    }
}

public sealed class NodalOsSemanticAccessFallbackService
{
    public NodalOsSemanticAccessFallback Evaluate(NodalOsRobustPerceptionFixture fixture, NodalOsPerceptionEvaluation perception)
    {
        var evidence = new NodalOsSemanticFallbackEvidence(
            $"semantic-fallback:{fixture.FixtureId}:redacted",
            fixture.SemanticSignals,
            UsesOcrVisionProductive: false,
            UsesCredentials: false,
            Redacted: true);

        var decision = fixture.SensitiveSurface
            ? NodalOsSemanticFallbackDecision.BlockedSensitiveSurface
            : fixture.BlockedSurfaceReason != NodalOsBlockedSurfaceReason.None
                ? NodalOsSemanticFallbackDecision.BlockedByOverlay
                : fixture.RequiresFutureVisionOcr
                    ? NodalOsSemanticFallbackDecision.RequiresFutureVisionOcr
                    : fixture.SemanticSignals.Count == 0 || fixture.SemanticSignals.All(s => !s.Present)
                        ? NodalOsSemanticFallbackDecision.InsufficientSemanticEvidence
                        : perception.Readiness == NodalOsPerceptionReadiness.UsableForReadOnlyContext
                            ? NodalOsSemanticFallbackDecision.NotNeeded
                            : NodalOsSemanticFallbackDecision.AvailableForReadOnlyContext;

        if (decision == NodalOsSemanticFallbackDecision.InsufficientSemanticEvidence &&
            perception.Readiness == NodalOsPerceptionReadiness.WarningRequiresCoreReview)
            decision = NodalOsSemanticFallbackDecision.RequiresHumanReview;

        return new NodalOsSemanticAccessFallback(
            $"semantic-fallback-{fixture.FixtureId}",
            decision,
            evidence,
            "Semantic fallback uses redacted local fixture descriptors only and cannot grant action authority.",
            CoreAuthorityRequired: true,
            ActionAuthorityGranted: false);
    }
}

public sealed class NodalOsRobustPerceptionFixtureHarness
{
    private readonly NodalOsWindowLivenessMonitor liveness = new();
    private readonly NodalOsSystemOverlayDetector overlay = new();
    private readonly NodalOsEmptySurfaceDetector empty = new();
    private readonly NodalOsSemanticAccessFallbackService semantic = new();
    private readonly NodalOsPerceptionBlockerExplanationService explanation = new();

    public IReadOnlyList<(NodalOsRobustPerceptionFixture Fixture, NodalOsPerceptionEvaluation Evaluation, NodalOsOverlayDetectionResult Overlay, NodalOsEmptySurfaceDetectionResult Empty, NodalOsSemanticAccessFallback Semantic, NodalOsPerceptionBlockerExplanation Explanation)> RunDefaultFixtures() =>
        CreateDefaultFixtures()
            .Select(f =>
            {
                var eval = liveness.Evaluate(f);
                var overlayResult = overlay.Detect(f);
                var emptyResult = empty.Detect(f);
                var semanticResult = semantic.Evaluate(f, eval);
                var expl = explanation.Explain(overlayResult, emptyResult, eval);
                return (f, eval, overlayResult, emptyResult, semanticResult, expl);
            })
            .ToArray();

    public static IReadOnlyList<NodalOsRobustPerceptionFixture> CreateDefaultFixtures()
    {
        var identityFixtures = NodalOsIdentityFixtureHarness.CreateDefaultFixtures().ToDictionary(f => f.FixtureId, StringComparer.Ordinal);
        return
        [
            Fixture("alive-stable", identityFixtures["local-readiness-dashboard"], NodalOsWindowLivenessState.Responding, NodalOsSurfaceStabilityState.Stable, NodalOsBlockedSurfaceReason.None, semantic: [Semantic("readiness dashboard", present: true)]),
            Fixture("frozen-window", identityFixtures["local-admin-surface"], NodalOsWindowLivenessState.Frozen, NodalOsSurfaceStabilityState.Stable, NodalOsBlockedSurfaceReason.None, semantic: [Semantic("admin shell", present: true)]),
            Fixture("stale-window", identityFixtures["stale-fingerprint"], NodalOsWindowLivenessState.Stale, NodalOsSurfaceStabilityState.Stable, NodalOsBlockedSurfaceReason.None, semantic: [Semantic("stale admin shell", present: true)]),
            Fixture("minimized-window", identityFixtures["local-diagnostics-surface"], NodalOsWindowLivenessState.Minimized, NodalOsSurfaceStabilityState.Stable, NodalOsBlockedSurfaceReason.None, semantic: [Semantic("diagnostics", present: true)]),
            Fixture("hidden-window", identityFixtures["local-diagnostics-surface"], NodalOsWindowLivenessState.Hidden, NodalOsSurfaceStabilityState.Stable, NodalOsBlockedSurfaceReason.None, semantic: [Semantic("diagnostics", present: true)]),
            Fixture("occluded-window", identityFixtures["local-diagnostics-surface"], NodalOsWindowLivenessState.Occluded, NodalOsSurfaceStabilityState.Stable, NodalOsBlockedSurfaceReason.None, semantic: [Semantic("diagnostics", present: true)]),
            Fixture("system-overlay", identityFixtures["local-admin-surface"], NodalOsWindowLivenessState.BlockedByOverlay, NodalOsSurfaceStabilityState.Blocked, NodalOsBlockedSurfaceReason.SystemModalOverlay, semantic: [Semantic("admin shell", present: true)]),
            Fixture("permission-overlay", identityFixtures["local-admin-surface"], NodalOsWindowLivenessState.BlockedByOverlay, NodalOsSurfaceStabilityState.Blocked, NodalOsBlockedSurfaceReason.PermissionOverlay, semantic: [Semantic("permission fixture", present: true)]),
            Fixture("empty-uia", identityFixtures["local-admin-surface"], NodalOsWindowLivenessState.Responding, NodalOsSurfaceStabilityState.Empty, NodalOsBlockedSurfaceReason.None, uiaEmpty: true, semantic: []),
            Fixture("empty-dom", identityFixtures["target-owned-lab-metadata"], NodalOsWindowLivenessState.Responding, NodalOsSurfaceStabilityState.Empty, NodalOsBlockedSurfaceReason.None, domEmpty: true, semantic: []),
            Fixture("truncated-surface", identityFixtures["local-admin-surface"], NodalOsWindowLivenessState.Responding, NodalOsSurfaceStabilityState.Truncated, NodalOsBlockedSurfaceReason.None, truncated: true, semantic: [Semantic("partial admin", present: true)]),
            Fixture("ambiguous-interactive", identityFixtures["ambiguous-window"], NodalOsWindowLivenessState.Responding, NodalOsSurfaceStabilityState.Ambiguous, NodalOsBlockedSurfaceReason.None, ambiguous: true, semantic: []),
            Fixture("sensitive-surface", identityFixtures["sensitive-blocked-surface"], NodalOsWindowLivenessState.UnsafeSurface, NodalOsSurfaceStabilityState.Unsafe, NodalOsBlockedSurfaceReason.SensitiveBlockedSurface, sensitive: true, semantic: [Semantic("sensitive blocked", present: true)]),
            Fixture("future-vision-ocr-required", identityFixtures["local-admin-surface"], NodalOsWindowLivenessState.Responding, NodalOsSurfaceStabilityState.Ambiguous, NodalOsBlockedSurfaceReason.None, ambiguous: true, semantic: [], requiresVisionOcr: true)
        ];
    }

    private static NodalOsRobustPerceptionFixture Fixture(
        string id,
        NodalOsIdentityFixture identityFixture,
        NodalOsWindowLivenessState liveness,
        NodalOsSurfaceStabilityState stability,
        NodalOsBlockedSurfaceReason reason,
        bool uiaEmpty = false,
        bool domEmpty = false,
        bool truncated = false,
        bool ambiguous = false,
        bool sensitive = false,
        IReadOnlyList<NodalOsSemanticAccessSignal>? semantic = null,
        bool requiresVisionOcr = false) =>
        new(
            id,
            identityFixture,
            liveness,
            stability,
            reason,
            UiaAvailable: !uiaEmpty,
            DomMetadataAvailable: !domEmpty,
            UiaTreeEmpty: uiaEmpty,
            DomMetadataEmpty: domEmpty,
            truncated,
            ambiguous,
            sensitive,
            semantic ?? [],
            requiresVisionOcr);

    private static NodalOsSemanticAccessSignal Semantic(string descriptor, bool present) =>
        new("fixture-label", descriptor, "local-fixture-descriptor", present);
}
