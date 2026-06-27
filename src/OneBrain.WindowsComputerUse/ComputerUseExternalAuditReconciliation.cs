namespace OneBrain.WindowsComputerUse;

public enum ComputerUseExternalAuditContainmentStatus
{
    NotEvaluated,
    ContainmentPass,
    ContainmentNoGo
}

public enum ComputerUseExternalAuditLiveAdvanceStatus
{
    NotEvaluated,
    LiveAdvanceNoGo,
    LiveAdvanceBlockedPendingHumanPolicyDecisionAndExternalGo
}

public sealed record ComputerUseExternalAuditReconciliationRecord(
    string BlockId,
    string Decision,
    string AuditedHead,
    ComputerUseExternalAuditContainmentStatus ContainmentStatus,
    ComputerUseExternalAuditLiveAdvanceStatus LiveAdvanceStatus,
    bool CurrentCodeDefectFound,
    bool AuditorRanBuild,
    bool AuditorRanTests,
    bool BehavioralLiveSafetyProven,
    bool LiveReadPermitted,
    bool ActionAuthorityGranted,
    bool ProductAutomationEnabled,
    IReadOnlyList<string> Caveats);

public static class ComputerUseExternalAuditReconciliation
{
    public const string Decision = "AUDIT_CONTAINMENT_PASS_BUT_LIVE_ADVANCE_NO_GO";
    public const string FinalDecision = "GO_WCU_EXTERNAL_AUDIT_NOGO_RECONCILIATION_CONTAINMENT_LOCK_READY";
    public const string AuditedHead = "c0ce467f5472dc65cafd9faeed6ee406930f7b6d";
    public const string BlockedLivePrototypeStatus = "BLOCKED_PENDING_HUMAN_POLICY_DECISION_AND_EXTERNAL_GO";

    public static ComputerUseExternalAuditReconciliationRecord Current() =>
        new(
            BlockId: "WCU-037A",
            Decision,
            AuditedHead,
            ComputerUseExternalAuditContainmentStatus.ContainmentPass,
            ComputerUseExternalAuditLiveAdvanceStatus.LiveAdvanceNoGo,
            CurrentCodeDefectFound: false,
            AuditorRanBuild: false,
            AuditorRanTests: false,
            BehavioralLiveSafetyProven: false,
            LiveReadPermitted: false,
            ActionAuthorityGranted: false,
            ProductAutomationEnabled: false,
            Caveats:
            [
                "External audit reviewed working HEAD c0ce467f and did not certify earlier HEAD 551f3b41 specifically.",
                "External audit did not run dotnet build or dotnet test.",
                "External audit confirmed containment/current-tree absence of live/action code, not behavioral live safety.",
                "External audit did not authorize WCU-037-044 live read-only prototype advancement."
            ]);
}
