using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M211 — ONNX native runtime crash isolation readiness review.
// Decides the safe next route after M209 + M210. Shadow mode stays blocked in this block.
public sealed class NodalOsOnnxNativeRuntimeCrashReadinessReview
{
    public sealed record Inputs(
        NodalOsOnnxNativeRuntimeCrashProbeMatrix Matrix,
        IReadOnlyList<NodalOsOnnxOutOfProcessGuardResult> GuardResults,
        // True only if a probe was allowed to crash the host in-process (must be false in the suite).
        bool InProcessCrashObserved,
        bool CleanupSucceeded,
        bool RawPersistenceDetected);

    public NodalOsOnnxNativeRuntimeCrashReadinessReport Evaluate(Inputs inputs)
    {
        if (inputs is null) throw new ArgumentNullException(nameof(inputs));

        var guardResults = inputs.GuardResults ?? Array.Empty<NodalOsOnnxOutOfProcessGuardResult>();
        var requirements = new List<NodalOsOnnxSyntheticOcrRequirement>();
        var warnings = new List<string>();

        var parentSurvived = guardResults.All(r => r.ParentSurvived);
        var noOrphans = guardResults.All(r => !r.OrphanProcessLeft);
        var tempCleaned = inputs.CleanupSucceeded && guardResults.All(r => r.TempFilesCleaned);
        var noRaw = !inputs.RawPersistenceDetected && guardResults.All(r => !r.RawPersisted);
        var noSaas = guardResults.All(r => !r.CallsSaas);
        var noAuthority = guardResults.All(r => r.NoAuthority);
        var guardContainsCrash = guardResults.Any(r =>
            r.ProbeResult.Status == NodalOsOnnxNativeRuntimeCrashProbeStatus.NativeRuntimeCrash &&
            r.ParentSurvived && !r.OrphanProcessLeft);

        requirements.Add(Req("in-process-crash-not-allowed", "Native crash is never run in-process", !inputs.InProcessCrashObserved, "no in-process crash observed", "in-process native crash observed", blocks: true));
        requirements.Add(Req("parent-survives", "Parent process survives child crash", parentSurvived, "parent survived all guard runs", "parent did not survive", blocks: true));
        requirements.Add(Req("no-orphans", "No orphan child processes", noOrphans, "no orphan processes left", "orphan child process left", blocks: true));
        requirements.Add(Req("temp-cleaned", "Temp/raw files cleaned", tempCleaned, "temp working dirs cleaned", "cleanup failed", blocks: true));
        requirements.Add(Req("no-raw", "No raw persistence", noRaw, "no raw image persisted", "raw persistence detected", blocks: true));
        requirements.Add(Req("no-saas", "No SaaS OCR", noSaas, "no SaaS call", "SaaS call detected", blocks: true));
        requirements.Add(Req("no-authority", "OCR is not authority", noAuthority, "no-authority preserved", "authority violation", blocks: true));
        requirements.Add(Req("matrix-modelled", "Crash matrix models safe + quarantined fixtures", inputs.Matrix.SafeFixtureCount > 0 && inputs.Matrix.QuarantinedFixtureCount > 0, "matrix includes safe and quarantined fixtures", "matrix incomplete", blocks: true));
        requirements.Add(Req("guard-contains-crash", "Out-of-process guard contains native crash", guardContainsCrash, "guard mapped a native crash with parent alive", "guard did not demonstrate containment", blocks: false));

        var decision = DetermineDecision(inputs, parentSurvived, noOrphans, tempCleaned, noRaw, noSaas, noAuthority, guardContainsCrash);

        if (inputs.InProcessCrashObserved)
            warnings.Add("an in-process native crash was observed; in-process risky probes must remain quarantined");
        if (!guardContainsCrash)
            warnings.Add("no guard run demonstrated native crash containment yet");
        if (!tempCleaned)
            warnings.Add("temp/raw cleanup did not fully succeed");

        return new NodalOsOnnxNativeRuntimeCrashReadinessReport(
            $"onnx-crash-readiness-{Guid.NewGuid():N}",
            decision,
            CanAttemptRedactedCropShadow: false, // shadow mode stays blocked in this block
            CanRunGuardedSyntheticText: decision is NodalOsOnnxNativeRuntimeCrashReadinessDecision.ReadyForGuardedSyntheticTextRun
                or NodalOsOnnxNativeRuntimeCrashReadinessDecision.ReadyForOutOfProcessOnly,
            CanContinueWithMoreFixtures: decision is NodalOsOnnxNativeRuntimeCrashReadinessDecision.ReadyForMoreSyntheticFixtures
                or NodalOsOnnxNativeRuntimeCrashReadinessDecision.ReadyForGuardedSyntheticTextRun
                or NodalOsOnnxNativeRuntimeCrashReadinessDecision.ReadyForOutOfProcessOnly,
            requirements,
            InProcessCrashContained: !inputs.InProcessCrashObserved,
            OutOfProcessGuardContainsCrash: guardContainsCrash,
            ParentProcessSurvived: parentSurvived,
            NoOrphanProcesses: noOrphans,
            TempFilesCleaned: tempCleaned,
            NoRawPersistence: noRaw,
            NoFullScreen: inputs.Matrix.NoFullScreen,
            NoSensitive: inputs.Matrix.NoSensitive,
            NoSaas: noSaas,
            NoAuthority: noAuthority,
            ShadowModeBlocked: true,
            ProductionPublicOcrBlocked: true,
            warnings,
            DateTimeOffset.UtcNow);
    }

    private static NodalOsOnnxNativeRuntimeCrashReadinessDecision DetermineDecision(
        Inputs inputs,
        bool parentSurvived,
        bool noOrphans,
        bool tempCleaned,
        bool noRaw,
        bool noSaas,
        bool noAuthority,
        bool guardContainsCrash)
    {
        // Any in-process native crash means the runtime cannot be trusted in-process: block.
        if (inputs.InProcessCrashObserved)
            return NodalOsOnnxNativeRuntimeCrashReadinessDecision.BlockedByModelRuntime;

        // Safety violations block before anything else.
        if (!noRaw || !noSaas || !noAuthority)
            return NodalOsOnnxNativeRuntimeCrashReadinessDecision.NotReady;

        if (!parentSurvived || !noOrphans || !tempCleaned)
            return NodalOsOnnxNativeRuntimeCrashReadinessDecision.BlockedByModelRuntime;

        // Out-of-process guard demonstrably contains the native crash and parent survives:
        // the only safe route is guarded / out-of-process execution. Shadow mode stays blocked.
        if (guardContainsCrash)
            return NodalOsOnnxNativeRuntimeCrashReadinessDecision.ReadyForOutOfProcessOnly;

        // Matrix modelled and safe diagnostics pass but no containment proof yet: keep gathering.
        return NodalOsOnnxNativeRuntimeCrashReadinessDecision.ReadyForMoreSyntheticFixtures;
    }

    private static NodalOsOnnxSyntheticOcrRequirement Req(
        string id, string name, bool satisfied, string evidence, string missingReason, bool blocks)
    {
        return new NodalOsOnnxSyntheticOcrRequirement(
            $"req-{id}-{Guid.NewGuid():N}",
            name,
            satisfied,
            BrowserCredentialRedactor.Redact(evidence),
            BrowserCredentialRedactor.Redact(missingReason),
            blocks);
    }
}
