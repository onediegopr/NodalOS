using OneBrain.Core.Approval;
using OneBrain.Core.Contracts;
using OneBrain.Core.Identity;
using OneBrain.Core.Models;
using OneBrain.Core.Safety;

namespace OneBrain.Core.Execution;

public enum RuntimeIdentityMatch
{
    Unknown = 0,
    Same = 1,
    Different = 2,
    Missing = 3
}

public sealed record SafeClickMigrationMetrics(
    int TotalClicks,
    int EligibleForFsm,
    int NotEligibleForFsm,
    int ApprovalV3Strong,
    int ApprovalV2,
    int WeakIdentity,
    int TargetObservePresent,
    int TargetObserveMissing,
    int RuntimeIdPresent,
    int RuntimeIdMissing,
    int RuntimeIdStable,
    int RuntimeIdChanged,
    int RuntimeIdUnknown,
    int UsesElClick,
    int UsesUiaActionExecutor,
    int InvokePatternAvailable,
    int InvokePatternUnavailable,
    int InvokePatternUnknown,
    int WouldRequireLegacy,
    int WouldUseUnsafeFallback,
    int WebUiaEligible,
    int DesktopUiaObservable,
    int DesktopUiaStrong,
    int DesktopUiaWeak,
    int DesktopMissingIdentity);

public sealed record SafeClickShadowReadiness(
    bool Success,
    bool Blocked,
    string Reason,
    SafeClickMigrationReadinessReason ReadinessReason,
    StepState ProjectedState,
    IdentityStrength IdentityStrength,
    string IdentitySource,
    bool HasTargetObserve,
    bool HasApprovalV3,
    bool HasRuntimeId,
    RuntimeIdentityMatch RuntimeIdentityMatch,
    bool WouldUseUnsafeFallback,
    bool WouldRequireLegacy,
    bool EligibleForFsm,
    SafeClickMigrationMetrics Metrics,
    IReadOnlyList<string> Reasons)
{
    public string Summary =>
        EligibleForFsm
            ? "FSM READY: YES"
            : $"FSM READY: NO ({Reason})";
}

public static class SafeClickShadowReadinessEvaluator
{
    public static SafeClickShadowReadiness Evaluate(
        ApprovalManifest? manifest,
        SafeClickExecutionPlan plan,
        ElementIdentity? observedIdentity,
        bool? invokePatternAvailable = null,
        bool usesElClick = false,
        bool usesUiaActionExecutor = false)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var hasApprovalV3 = manifest != null &&
                            string.Equals(
                                manifest.IdentitySchemaVersion,
                                ApprovalManifestBuilder.IdentitySchemaVersion,
                                StringComparison.Ordinal);
        var identitySource = (manifest?.IdentitySource ?? "").Trim();
        var hasTargetObserve = HasTargetObserve(manifest);
        var hasRuntimeId = observedIdentity?.IsStrong == true;
        var runtimeIdentityMatch = ResolveRuntimeIdentityMatch(manifest, observedIdentity, hasApprovalV3);
        var eligibleForFsm = hasApprovalV3 &&
                             hasTargetObserve &&
                             plan.IdentityStrength == IdentityStrength.Strong &&
                             hasRuntimeId &&
                             runtimeIdentityMatch == RuntimeIdentityMatch.Same &&
                             plan.ProjectedState == StepState.Bound &&
                             plan.ContractValid &&
                             !plan.WouldUseUnsafeFallback;
        var reason = ResolveReason(
            manifest,
            plan,
            hasTargetObserve,
            hasApprovalV3,
            hasRuntimeId,
            runtimeIdentityMatch,
            eligibleForFsm);
        var readinessReason = ParseReason(reason);
        var reasons = BuildReasons(reason, plan.Reasons);
        var isDesktopSource = string.Equals(identitySource, "uia", StringComparison.OrdinalIgnoreCase);
        var isWebSource = string.Equals(identitySource, "web-uia", StringComparison.OrdinalIgnoreCase);
        var metrics = new SafeClickMigrationMetrics(
            TotalClicks: 1,
            EligibleForFsm: eligibleForFsm ? 1 : 0,
            NotEligibleForFsm: eligibleForFsm ? 0 : 1,
            ApprovalV3Strong: hasApprovalV3 && plan.IdentityStrength == IdentityStrength.Strong ? 1 : 0,
            ApprovalV2: hasApprovalV3 ? 0 : 1,
            WeakIdentity: plan.IdentityStrength == IdentityStrength.Weak ? 1 : 0,
            TargetObservePresent: hasTargetObserve ? 1 : 0,
            TargetObserveMissing: hasTargetObserve ? 0 : 1,
            RuntimeIdPresent: hasRuntimeId ? 1 : 0,
            RuntimeIdMissing: hasRuntimeId ? 0 : 1,
            RuntimeIdStable: runtimeIdentityMatch == RuntimeIdentityMatch.Same ? 1 : 0,
            RuntimeIdChanged: runtimeIdentityMatch == RuntimeIdentityMatch.Different ? 1 : 0,
            RuntimeIdUnknown: runtimeIdentityMatch == RuntimeIdentityMatch.Unknown ? 1 : 0,
            UsesElClick: usesElClick ? 1 : 0,
            UsesUiaActionExecutor: usesUiaActionExecutor ? 1 : 0,
            InvokePatternAvailable: invokePatternAvailable == true ? 1 : 0,
            InvokePatternUnavailable: invokePatternAvailable == false ? 1 : 0,
            InvokePatternUnknown: invokePatternAvailable.HasValue ? 0 : 1,
            WouldRequireLegacy: !eligibleForFsm ? 1 : 0,
            WouldUseUnsafeFallback: plan.WouldUseUnsafeFallback ? 1 : 0,
            WebUiaEligible: isWebSource && eligibleForFsm ? 1 : 0,
            DesktopUiaObservable: isDesktopSource ? 1 : 0,
            DesktopUiaStrong: isDesktopSource && plan.IdentityStrength == IdentityStrength.Strong ? 1 : 0,
            DesktopUiaWeak: isDesktopSource && plan.IdentityStrength == IdentityStrength.Weak ? 1 : 0,
            DesktopMissingIdentity: isDesktopSource && plan.IdentityStrength == IdentityStrength.None ? 1 : 0);

        return new SafeClickShadowReadiness(
            Success: eligibleForFsm,
            Blocked: !eligibleForFsm,
            Reason: reason,
            ReadinessReason: readinessReason,
            ProjectedState: plan.ProjectedState,
            IdentityStrength: plan.IdentityStrength,
            IdentitySource: identitySource,
            HasTargetObserve: hasTargetObserve,
            HasApprovalV3: hasApprovalV3,
            HasRuntimeId: hasRuntimeId,
            RuntimeIdentityMatch: runtimeIdentityMatch,
            WouldUseUnsafeFallback: plan.WouldUseUnsafeFallback,
            WouldRequireLegacy: !eligibleForFsm,
            EligibleForFsm: eligibleForFsm,
            Metrics: metrics,
            Reasons: reasons);
    }

    private static bool HasTargetObserve(ApprovalManifest? manifest)
    {
        if (manifest == null)
            return false;

        return !string.IsNullOrWhiteSpace(manifest.IdentitySchemaVersion) ||
               !string.IsNullOrWhiteSpace(manifest.IdentitySource) ||
               !string.IsNullOrWhiteSpace(manifest.ApprovedIdentityDigest) ||
               manifest.ApprovedSelector != null;
    }

    private static RuntimeIdentityMatch ResolveRuntimeIdentityMatch(
        ApprovalManifest? manifest,
        ElementIdentity? observedIdentity,
        bool hasApprovalV3)
    {
        if (!hasApprovalV3 || manifest == null || string.IsNullOrWhiteSpace(manifest.ApprovedIdentityDigest))
            return RuntimeIdentityMatch.Unknown;

        if (observedIdentity == null || !observedIdentity.IsStrong)
            return RuntimeIdentityMatch.Missing;

        var approvedRuntimeId = manifest.ApprovedSelector?.ExpectedIdentity?.RuntimeId ?? "";
        if (!string.IsNullOrWhiteSpace(approvedRuntimeId))
        {
            return string.Equals(approvedRuntimeId, observedIdentity.RuntimeId, StringComparison.Ordinal)
                ? RuntimeIdentityMatch.Same
                : RuntimeIdentityMatch.Different;
        }

        var observedDigest = ElementFingerprintBuilder.Build(observedIdentity);
        if (string.IsNullOrWhiteSpace(observedDigest))
            return RuntimeIdentityMatch.Missing;

        return string.Equals(manifest.ApprovedIdentityDigest, observedDigest, StringComparison.Ordinal)
            ? RuntimeIdentityMatch.Same
            : RuntimeIdentityMatch.Different;
    }

    private static string ResolveReason(
        ApprovalManifest? manifest,
        SafeClickExecutionPlan plan,
        bool hasTargetObserve,
        bool hasApprovalV3,
        bool hasRuntimeId,
        RuntimeIdentityMatch runtimeIdentityMatch,
        bool eligibleForFsm)
    {
        if (eligibleForFsm)
            return "Ready";

        if (!hasApprovalV3)
            return "ApprovalV2";

        if (!hasTargetObserve)
            return "MissingTargetObserve";

        if (plan.IdentityStrength == IdentityStrength.None)
            return "MissingIdentity";

        if (plan.IdentityStrength == IdentityStrength.Weak)
            return "WeakIdentity";

        if (!hasRuntimeId || runtimeIdentityMatch == RuntimeIdentityMatch.Missing)
            return "RuntimeIdMissing";

        if (runtimeIdentityMatch == RuntimeIdentityMatch.Different)
            return "RuntimeIdChanged";

        if (plan.WouldUseUnsafeFallback)
            return "WouldUseLegacyFallback";

        if (plan.FailureKind == FailureKind.Ambiguous)
            return "Ambiguous";

        if (plan.FailureKind == FailureKind.NotFound)
            return "NotFound";

        if (!string.IsNullOrWhiteSpace(plan.BlockReason))
            return plan.BlockReason!;

        if (plan.FailureKind.HasValue)
            return plan.FailureKind.Value.ToString();

        if (manifest == null)
            return "MissingManifest";

        return "Blocked";
    }

    private static IReadOnlyList<string> BuildReasons(string reason, IReadOnlyList<string> plannerReasons)
    {
        var reasons = new List<string>();
        if (!string.IsNullOrWhiteSpace(reason) && !string.Equals(reason, "Ready", StringComparison.Ordinal))
            reasons.Add(reason);

        foreach (var plannerReason in plannerReasons)
        {
            if (string.IsNullOrWhiteSpace(plannerReason))
                continue;

            if (!reasons.Any(existing => string.Equals(existing, plannerReason, StringComparison.OrdinalIgnoreCase)))
                reasons.Add(plannerReason);
        }

        return reasons;
    }

    private static SafeClickMigrationReadinessReason ParseReason(string reason)
    {
        return reason switch
        {
            "Ready" => SafeClickMigrationReadinessReason.Ready,
            "ApprovalV2" => SafeClickMigrationReadinessReason.ApprovalV2,
            "MissingTargetObserve" => SafeClickMigrationReadinessReason.MissingTargetObserve,
            "MissingIdentity" => SafeClickMigrationReadinessReason.MissingIdentity,
            "WeakIdentity" => SafeClickMigrationReadinessReason.WeakIdentity,
            "RuntimeIdMissing" => SafeClickMigrationReadinessReason.RuntimeIdMissing,
            "RuntimeIdChanged" => SafeClickMigrationReadinessReason.RuntimeIdChanged,
            "WouldUseLegacyFallback" => SafeClickMigrationReadinessReason.WouldUseLegacyFallback,
            "Ambiguous" => SafeClickMigrationReadinessReason.Ambiguous,
            "NotFound" => SafeClickMigrationReadinessReason.NotFound,
            "MissingManifest" => SafeClickMigrationReadinessReason.MissingManifest,
            "PolicyDenied" => SafeClickMigrationReadinessReason.PolicyDenied,
            "Blocked" => SafeClickMigrationReadinessReason.Blocked,
            _ => SafeClickMigrationReadinessReason.Unknown
        };
    }
}
