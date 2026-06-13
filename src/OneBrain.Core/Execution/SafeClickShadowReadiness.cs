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
    int DesktopMissingIdentity,
    int RuntimeStabilityChecked = 0,
    int RuntimeStable = 0,
    int RuntimeChanged = 0,
    int RuntimeMissing = 0,
    int ReobserveAttempted = 0,
    int ReobserveSucceeded = 0,
    int ReobserveChanged = 0,
    int DefaultDispatchBlockedByStaleIdentity = 0,
    int DefaultDispatchBlockedByMissingIdentity = 0,
    int DesktopEligibleForFsm = 0,
    int DesktopNotEligibleForFsm = 0,
    int DesktopRuntimeStable = 0,
    int DesktopRuntimeChanged = 0,
    int DesktopInvokePatternAvailable = 0,
    int DesktopRoleAllowed = 0,
    int DesktopRootAvailable = 0,
    int DesktopOptInRouted = 0,
    int DesktopOptInBlocked = 0,
    int DesktopDefaultFsmEnabled = 0,
    int DesktopDefaultFsmRouted = 0,
    int DesktopDefaultEligibleButNotEnabled = 0,
    int DesktopDefaultBlocked = 0,
    int DesktopDefaultBlockedByStaleIdentity = 0,
    int AllEligibleModeEnabled = 0,
    int DefaultFsmScopeWeb = 0,
    int DefaultFsmScopeDesktop = 0);

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
    IReadOnlyList<string> Reasons,
    bool InvokePatternAvailable = false,
    bool RoleAllowedForSafeExecutor = false,
    bool IsWebUia = false,
    bool DesktopEligibleForFsm = false,
    bool DesktopRootAvailable = false)
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
        bool usesUiaActionExecutor = false,
        bool desktopRootAvailable = false)
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
        var isDesktopSource = string.Equals(identitySource, "uia", StringComparison.OrdinalIgnoreCase);
        var isWebSource = string.Equals(identitySource, "web-uia", StringComparison.OrdinalIgnoreCase);

        // HITO-147: hardened eligibility. The executor dispatch is only safe when the resolved
        // target exposes InvokePattern on an allowlisted role, and only web-uia is in scope for
        // default routing. Desktop never becomes default-eligible in this hito.
        var invokePatternStrict = invokePatternAvailable == true;
        var roleAllowed = observedIdentity != null &&
                          ExecutorSurfacePolicy.IsRoleAllowed(observedIdentity.EffectiveControlType);

        var eligibleForFsm = hasApprovalV3 &&
                             hasTargetObserve &&
                             plan.IdentityStrength == IdentityStrength.Strong &&
                             hasRuntimeId &&
                             runtimeIdentityMatch == RuntimeIdentityMatch.Same &&
                             plan.ProjectedState == StepState.Bound &&
                             plan.ContractValid &&
                             !plan.WouldUseUnsafeFallback &&
                             invokePatternStrict &&
                             roleAllowed &&
                             isWebSource;
        var desktopEligibleForFsm = hasApprovalV3 &&
                                    hasTargetObserve &&
                                    plan.IdentityStrength == IdentityStrength.Strong &&
                                    hasRuntimeId &&
                                    runtimeIdentityMatch == RuntimeIdentityMatch.Same &&
                                    plan.ProjectedState == StepState.Bound &&
                                    plan.ContractValid &&
                                    !plan.WouldUseUnsafeFallback &&
                                    invokePatternStrict &&
                                    roleAllowed &&
                                    isDesktopSource &&
                                    desktopRootAvailable;
        var reason = ResolveReason(
            manifest,
            plan,
            hasTargetObserve,
            hasApprovalV3,
            hasRuntimeId,
            runtimeIdentityMatch,
            invokePatternStrict,
            roleAllowed,
            isWebSource,
            eligibleForFsm);
        var readinessReason = ParseReason(reason);
        var reasons = BuildReasons(reason, plan.Reasons);
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
            DesktopMissingIdentity: isDesktopSource && plan.IdentityStrength == IdentityStrength.None ? 1 : 0,
            DesktopEligibleForFsm: desktopEligibleForFsm ? 1 : 0,
            DesktopNotEligibleForFsm: isDesktopSource && !desktopEligibleForFsm ? 1 : 0,
            DesktopRuntimeStable: isDesktopSource && runtimeIdentityMatch == RuntimeIdentityMatch.Same ? 1 : 0,
            DesktopRuntimeChanged: isDesktopSource && runtimeIdentityMatch == RuntimeIdentityMatch.Different ? 1 : 0,
            DesktopInvokePatternAvailable: isDesktopSource && invokePatternAvailable == true ? 1 : 0,
            DesktopRoleAllowed: isDesktopSource && roleAllowed ? 1 : 0,
            DesktopRootAvailable: isDesktopSource && desktopRootAvailable ? 1 : 0);

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
            Reasons: reasons,
            InvokePatternAvailable: invokePatternStrict,
            RoleAllowedForSafeExecutor: roleAllowed,
            IsWebUia: isWebSource,
            DesktopEligibleForFsm: desktopEligibleForFsm,
            DesktopRootAvailable: desktopRootAvailable);
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
        bool invokePatternStrict,
        bool roleAllowed,
        bool isWebSource,
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

        if (!invokePatternStrict)
            return "InvokePatternUnavailable";

        if (!roleAllowed)
            return "RoleNotAllowed";

        if (!isWebSource)
            return "NotWebUia";

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
            "InvokePatternUnavailable" => SafeClickMigrationReadinessReason.InvokePatternUnavailable,
            "RoleNotAllowed" => SafeClickMigrationReadinessReason.RoleNotAllowed,
            "NotWebUia" => SafeClickMigrationReadinessReason.NotWebUia,
            "Ambiguous" => SafeClickMigrationReadinessReason.Ambiguous,
            "NotFound" => SafeClickMigrationReadinessReason.NotFound,
            "MissingManifest" => SafeClickMigrationReadinessReason.MissingManifest,
            "PolicyDenied" => SafeClickMigrationReadinessReason.PolicyDenied,
            "Blocked" => SafeClickMigrationReadinessReason.Blocked,
            _ => SafeClickMigrationReadinessReason.Unknown
        };
    }
}
