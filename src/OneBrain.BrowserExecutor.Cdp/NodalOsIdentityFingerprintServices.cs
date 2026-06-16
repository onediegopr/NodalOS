using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NodalOsIdentityFingerprintEvaluator
{
    private const string TargetOwnedHost = "lab.nodalos.com.ar";

    public NodalOsIdentityFingerprintV2 Evaluate(NodalOsIdentityFixture fixture)
    {
        var reasons = new List<NodalOsIdentityMismatchReason>();
        var signalsUsed = new List<string>();
        var signalsMissing = new List<string>();

        AddSignal(fixture.Observed.RuntimeProvider, "runtime provider", signalsUsed, signalsMissing, reasons);
        AddSignal(fixture.Observed.WindowTitleNormalized, "window title normalized", signalsUsed, signalsMissing, reasons);
        AddSignal(fixture.Observed.ProcessAppIdentity, "process/app identity", signalsUsed, signalsMissing, reasons);
        AddSignal(fixture.Observed.SafetyProfile, "safety profile", signalsUsed, signalsMissing, reasons);
        AddSignal(fixture.Observed.TargetOwnershipScope, "target ownership/scope", signalsUsed, signalsMissing, reasons);

        if (!string.IsNullOrWhiteSpace(fixture.Observed.Host))
            signalsUsed.Add("URL/host allowlisted");
        if (!string.IsNullOrWhiteSpace(fixture.Observed.RoutePath))
            signalsUsed.Add("expected route/path");
        if (!string.IsNullOrWhiteSpace(fixture.Observed.DomPageMetadataRedacted))
            signalsUsed.Add("DOM/page metadata redacted");
        if (!string.IsNullOrWhiteSpace(fixture.Observed.UiaMetadataRedacted))
            signalsUsed.Add("UIA metadata redacted");
        if (fixture.Observed.EvidenceRefs.Count > 0)
            signalsUsed.Add("evidence refs");

        if (!string.Equals(fixture.Expected.RuntimeProvider, fixture.Observed.RuntimeProvider, StringComparison.OrdinalIgnoreCase))
            reasons.Add(NodalOsIdentityMismatchReason.RuntimeProviderMismatch);
        if (!string.Equals(fixture.Expected.WindowTitleNormalized, fixture.Observed.WindowTitleNormalized, StringComparison.OrdinalIgnoreCase))
            reasons.Add(NodalOsIdentityMismatchReason.WindowTitleMismatch);
        if (!HostsMatch(fixture.Expected.Host, fixture.Observed.Host))
            reasons.Add(NodalOsIdentityMismatchReason.HostMismatch);
        if (fixture.Observed.IsSensitiveSurface)
            reasons.Add(NodalOsIdentityMismatchReason.SensitiveSurface);
        if (fixture.Observed.SafetyProfile.Contains("unsafe", StringComparison.OrdinalIgnoreCase))
            reasons.Add(NodalOsIdentityMismatchReason.UnsafeSurface);
        if (fixture.ForceStale)
            reasons.Add(NodalOsIdentityMismatchReason.StaleFingerprint);
        if (fixture.ForceAmbiguous)
            reasons.Add(NodalOsIdentityMismatchReason.AmbiguousIdentity);
        if (signalsMissing.Count > 0)
            reasons.Add(NodalOsIdentityMismatchReason.InsufficientEvidence);

        var confidence = ResolveConfidence(fixture, reasons);
        var decision = ResolveDecision(confidence, reasons);
        var evidenceRefs = fixture.Observed.EvidenceRefs.Count == 0
            ? [$"identity:{fixture.FixtureId}:redacted"]
            : fixture.Observed.EvidenceRefs;
        var fingerprintId = $"fp-v2-{fixture.FixtureId}";

        var fingerprint = new NodalOsRuntimeSurfaceFingerprint(
            fingerprintId,
            fixture.Observed,
            signalsUsed.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            signalsMissing.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            confidence,
            reasons.Distinct().ToArray(),
            "metadata-only identity evidence; no credentials, cookies, tokens, bodies, full DOM, or sensitive raw UIA persisted",
            CoreAuthorityRequired: true,
            GrantsActionAuthority: false);

        return new NodalOsIdentityFingerprintV2(
            $"identity-proof-{fixture.FixtureId}",
            fingerprint,
            evidenceRefs.Select(e => new NodalOsIdentityEvidence(e, "local-fixture-harness", fixture.ObservedAtUtc, Redacted: true)).ToArray(),
            decision,
            ActionAuthorityGranted: false,
            Redacted: true);
    }

    private static void AddSignal(
        string value,
        string signalName,
        ICollection<string> signalsUsed,
        ICollection<string> signalsMissing,
        ICollection<NodalOsIdentityMismatchReason> reasons)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            signalsMissing.Add(signalName);
            reasons.Add(NodalOsIdentityMismatchReason.MissingRequiredSignal);
            return;
        }

        signalsUsed.Add(signalName);
    }

    private static bool HostsMatch(string? expected, string? observed)
    {
        if (string.IsNullOrWhiteSpace(expected) && string.IsNullOrWhiteSpace(observed))
            return true;
        return string.Equals(expected, observed, StringComparison.OrdinalIgnoreCase);
    }

    private static NodalOsIdentityConfidence ResolveConfidence(
        NodalOsIdentityFixture fixture,
        IReadOnlyCollection<NodalOsIdentityMismatchReason> reasons)
    {
        if (reasons.Count > 0)
            return reasons.Contains(NodalOsIdentityMismatchReason.MissingRequiredSignal) ||
                reasons.Contains(NodalOsIdentityMismatchReason.InsufficientEvidence)
                ? NodalOsIdentityConfidence.Low
                : NodalOsIdentityConfidence.Unknown;

        if (fixture.Observed.IsTargetOwned && string.Equals(fixture.Observed.Host, TargetOwnedHost, StringComparison.OrdinalIgnoreCase))
            return NodalOsIdentityConfidence.VerifiedTargetOwned;
        if (fixture.Observed.IsFixture)
            return NodalOsIdentityConfidence.VerifiedFixture;
        return NodalOsIdentityConfidence.High;
    }

    private static NodalOsIdentityRecommendedDecision ResolveDecision(
        NodalOsIdentityConfidence confidence,
        IReadOnlyCollection<NodalOsIdentityMismatchReason> reasons)
    {
        if (reasons.Contains(NodalOsIdentityMismatchReason.SensitiveSurface) ||
            reasons.Contains(NodalOsIdentityMismatchReason.UnsafeSurface))
            return NodalOsIdentityRecommendedDecision.BlockSensitiveSurface;

        if (confidence is NodalOsIdentityConfidence.VerifiedFixture or NodalOsIdentityConfidence.VerifiedTargetOwned)
            return NodalOsIdentityRecommendedDecision.AllowReadOnlyLocalObservation;

        if (confidence is NodalOsIdentityConfidence.Low or NodalOsIdentityConfidence.Unknown)
            return NodalOsIdentityRecommendedDecision.BlockUntilIdentityVerified;

        return NodalOsIdentityRecommendedDecision.WarnAndRequireCoreReview;
    }
}

public sealed class NodalOsIdentityFixtureHarness
{
    private readonly NodalOsIdentityFingerprintEvaluator evaluator = new();

    public NodalOsIdentityFixtureHarnessReport RunDefaultFixtures()
    {
        var proofs = CreateDefaultFixtures()
            .Select(evaluator.Evaluate)
            .ToArray();
        var blocked = proofs.Any(p => p.RecommendedDecision is
            NodalOsIdentityRecommendedDecision.BlockSensitiveSurface or
            NodalOsIdentityRecommendedDecision.BlockUntilIdentityVerified);

        return new NodalOsIdentityFixtureHarnessReport(
            "identity-fixture-harness-m133-m135",
            proofs,
            IdentityFixtureReadiness: proofs.Any(p => p.Fingerprint.Confidence == NodalOsIdentityConfidence.VerifiedFixture) &&
                proofs.Any(p => p.Fingerprint.Confidence == NodalOsIdentityConfidence.VerifiedTargetOwned),
            IdentityBlocked: blocked,
            IdentityWarning: proofs.Any(p => p.RecommendedDecision == NodalOsIdentityRecommendedDecision.WarnAndRequireCoreReview),
            CoreAuthorityRequired: true,
            ActionAuthorityGranted: false,
            proofs.SelectMany(p => p.Evidence.Select(e => e.EvidenceRef)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            Redacted: true);
    }

    public static IReadOnlyList<NodalOsIdentityFixture> CreateDefaultFixtures()
    {
        var now = DateTimeOffset.UtcNow;
        var admin = Surface(
            "local-admin",
            "local-fixture",
            "nodal os admin",
            "nodal-os-local-admin",
            host: null,
            path: "/admin",
            isFixture: true,
            isTargetOwned: false,
            sensitive: false,
            evidenceRef: "identity:local-admin:redacted");
        var diagnostics = Surface(
            "local-diagnostics",
            "local-fixture",
            "nodal os diagnostics",
            "nodal-os-local-diagnostics",
            host: null,
            path: "/diagnostics",
            isFixture: true,
            isTargetOwned: false,
            sensitive: false,
            evidenceRef: "identity:local-diagnostics:redacted");
        var readiness = Surface(
            "local-readiness",
            "local-fixture",
            "nodal os readiness",
            "nodal-os-local-readiness",
            host: null,
            path: "/readiness",
            isFixture: true,
            isTargetOwned: false,
            sensitive: false,
            evidenceRef: "identity:local-readiness:redacted");
        var targetOwned = Surface(
            "target-owned-lab",
            "chrome-cdp-readonly",
            "nodal os lab",
            "vercel-static-lab",
            host: "lab.nodalos.com.ar",
            path: "/",
            isFixture: false,
            isTargetOwned: true,
            sensitive: false,
            evidenceRef: "identity:target-owned-lab:redacted");

        return
        [
            new("local-admin-surface", admin, admin, now, ForceStale: false, ForceAmbiguous: false),
            new("local-diagnostics-surface", diagnostics, diagnostics, now, ForceStale: false, ForceAmbiguous: false),
            new("local-readiness-dashboard", readiness, readiness, now, ForceStale: false, ForceAmbiguous: false),
            new("target-owned-lab-metadata", targetOwned, targetOwned, now, ForceStale: false, ForceAmbiguous: false),
            new("mismatched-host", targetOwned, targetOwned with { Host = "example.com", EvidenceRefs = ["identity:mismatched-host:redacted"] }, now, ForceStale: false, ForceAmbiguous: false),
            new("stale-fingerprint", admin, admin with { EvidenceRefs = ["identity:stale:redacted"] }, now.AddMinutes(-30), ForceStale: true, ForceAmbiguous: false),
            new("ambiguous-window", admin, admin with { WindowTitleNormalized = "nodal os", EvidenceRefs = ["identity:ambiguous:redacted"] }, now, ForceStale: false, ForceAmbiguous: true),
            new("sensitive-blocked-surface", admin, admin with { IsSensitiveSurface = true, SafetyProfile = "unsafe-sensitive-blocked", EvidenceRefs = ["identity:sensitive-blocked:redacted"] }, now, ForceStale: false, ForceAmbiguous: false),
            new("missing-evidence", admin, admin with { RuntimeProvider = "", EvidenceRefs = [] }, now, ForceStale: false, ForceAmbiguous: false)
        ];
    }

    private static NodalOsSurfaceIdentity Surface(
        string id,
        string runtimeProvider,
        string title,
        string appIdentity,
        string? host,
        string path,
        bool isFixture,
        bool isTargetOwned,
        bool sensitive,
        string evidenceRef) =>
        new(
            id,
            runtimeProvider,
            title,
            appIdentity,
            host,
            path,
            DomPageMetadataRedacted: "metadata:redacted",
            UiaMetadataRedacted: "uia:redacted",
            SafetyProfile: sensitive ? "unsafe-sensitive-blocked" : "local-private-preview-safe",
            TargetOwnershipScope: isTargetOwned ? "target-owned" : "local-fixture",
            isFixture,
            isTargetOwned,
            sensitive,
            [evidenceRef]);
}

public sealed class NodalOsIdentityPrivatePreviewIntegrationService
{
    public NodalOsIdentityEvidenceSummary BuildEvidenceSummary(NodalOsIdentityFingerprintV2 proof) =>
        new(
            proof.IdentityProofId,
            proof.Fingerprint.FingerprintId,
            proof.Fingerprint.Confidence,
            proof.Fingerprint.SignalsUsed,
            proof.Fingerprint.SignalsMissing,
            proof.Fingerprint.MismatchReasons,
            proof.Fingerprint.RedactionSummary,
            "Identity/Fingerprint v2 local fixture-first",
            proof.Fingerprint.Surface.IsTargetOwned ? "target-owned" : "local-fixture",
            proof.RecommendedDecision,
            CoreAuthorityRequired: true,
            ActionAuthorityGranted: false,
            Redacted: true);
}
