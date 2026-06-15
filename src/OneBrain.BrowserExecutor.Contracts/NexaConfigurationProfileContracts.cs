namespace OneBrain.BrowserExecutor.Contracts;

public enum NexaConfigurationProfileKind
{
    Development,
    Test,
    LocalSandbox,
    InternalPreview,
    PrivateBeta,
    ProductionLocked,
    EnterpriseControlled,
    Unknown
}

public enum NexaConfigurationProfileRiskPosture
{
    FixtureOnly,
    SandboxOnly,
    InternalControlled,
    FailClosed,
    EnterprisePolicyControlled
}

public enum NexaProfileVaultMode
{
    Disabled,
    SandboxSynthetic,
    OsBackedSynthetic,
    EnterpriseControlled,
    ProductiveExternal
}

public enum NexaProfileBrowserRuntimeMode
{
    FixturesOnly,
    LocalCdpReadOnly,
    ControlledRuntime,
    ProductionLocked
}

public enum NexaProfileRecorderReplayMode
{
    Disabled,
    ReadOnlySafeMode,
    Productive
}

public enum NexaProfileSensitiveSiteMode
{
    Disabled,
    SimulationOnly,
    EnterpriseControlled,
    RealPilot
}

public enum NexaProfileExternalMode
{
    Disabled,
    MockOnly,
    FutureReal
}

public sealed record NexaConfigurationProfileFeatureSet(
    IReadOnlySet<NexaFeatureFlag> EnabledFeatures,
    IReadOnlySet<NexaFeatureFlag> DisabledFeatures);

public sealed record NexaConfigurationProfileLimitSet(
    IReadOnlyList<NexaUsageLimit> Limits);

public sealed record NexaConfigurationProfilePolicy(
    bool RequiresAdminApproval,
    bool RequiresComplianceDecision,
    bool AllowRealBilling,
    bool AllowRealEmail,
    bool AllowPublicSaasActivation,
    bool SupportMetadataOnly,
    bool DiagnosticsRedacted,
    bool ExposesProfileRaw);

public sealed record NexaConfigurationProfile(
    NexaConfigurationProfileKind Kind,
    NexaConfigurationProfileRiskPosture RiskPosture,
    NexaConfigurationProfileFeatureSet Features,
    NexaConfigurationProfileLimitSet Limits,
    NexaConfigurationProfilePolicy Policy,
    NexaProfileVaultMode VaultMode,
    NexaProfileBrowserRuntimeMode BrowserRuntimeMode,
    NexaProfileRecorderReplayMode RecorderReplayMode,
    NexaProfileSensitiveSiteMode SensitiveSiteMode,
    NexaProfileExternalMode BillingMode,
    NexaProfileExternalMode EmailMode)
{
    public static NexaConfigurationProfile Development() =>
        Build(NexaConfigurationProfileKind.Development, NexaConfigurationProfileRiskPosture.FixtureOnly, NexaProfileVaultMode.OsBackedSynthetic, NexaProfileBrowserRuntimeMode.LocalCdpReadOnly, NexaProfileRecorderReplayMode.ReadOnlySafeMode, NexaProfileSensitiveSiteMode.SimulationOnly);

    public static NexaConfigurationProfile Test() =>
        Build(NexaConfigurationProfileKind.Test, NexaConfigurationProfileRiskPosture.SandboxOnly, NexaProfileVaultMode.OsBackedSynthetic, NexaProfileBrowserRuntimeMode.LocalCdpReadOnly, NexaProfileRecorderReplayMode.ReadOnlySafeMode, NexaProfileSensitiveSiteMode.SimulationOnly);

    public static NexaConfigurationProfile LocalSandbox() =>
        Build(NexaConfigurationProfileKind.LocalSandbox, NexaConfigurationProfileRiskPosture.SandboxOnly, NexaProfileVaultMode.SandboxSynthetic, NexaProfileBrowserRuntimeMode.LocalCdpReadOnly, NexaProfileRecorderReplayMode.ReadOnlySafeMode, NexaProfileSensitiveSiteMode.SimulationOnly);

    public static NexaConfigurationProfile InternalPreview() =>
        Build(NexaConfigurationProfileKind.InternalPreview, NexaConfigurationProfileRiskPosture.InternalControlled, NexaProfileVaultMode.OsBackedSynthetic, NexaProfileBrowserRuntimeMode.ControlledRuntime, NexaProfileRecorderReplayMode.ReadOnlySafeMode, NexaProfileSensitiveSiteMode.SimulationOnly);

    public static NexaConfigurationProfile PrivateBeta() =>
        Build(NexaConfigurationProfileKind.PrivateBeta, NexaConfigurationProfileRiskPosture.InternalControlled, NexaProfileVaultMode.OsBackedSynthetic, NexaProfileBrowserRuntimeMode.ControlledRuntime, NexaProfileRecorderReplayMode.ReadOnlySafeMode, NexaProfileSensitiveSiteMode.SimulationOnly);

    public static NexaConfigurationProfile ProductionLocked() =>
        Build(NexaConfigurationProfileKind.ProductionLocked, NexaConfigurationProfileRiskPosture.FailClosed, NexaProfileVaultMode.Disabled, NexaProfileBrowserRuntimeMode.ProductionLocked, NexaProfileRecorderReplayMode.Disabled, NexaProfileSensitiveSiteMode.Disabled);

    public static NexaConfigurationProfile EnterpriseControlled() =>
        Build(NexaConfigurationProfileKind.EnterpriseControlled, NexaConfigurationProfileRiskPosture.EnterprisePolicyControlled, NexaProfileVaultMode.EnterpriseControlled, NexaProfileBrowserRuntimeMode.ControlledRuntime, NexaProfileRecorderReplayMode.ReadOnlySafeMode, NexaProfileSensitiveSiteMode.EnterpriseControlled, requiresCompliance: true);

    private static NexaConfigurationProfile Build(
        NexaConfigurationProfileKind kind,
        NexaConfigurationProfileRiskPosture posture,
        NexaProfileVaultMode vault,
        NexaProfileBrowserRuntimeMode runtime,
        NexaProfileRecorderReplayMode recorder,
        NexaProfileSensitiveSiteMode sensitive,
        bool requiresCompliance = false)
    {
        var enabled = new HashSet<NexaFeatureFlag>
        {
            NexaFeatureFlag.BrowserRuntime,
            NexaFeatureFlag.CdpLiveReadOnly,
            NexaFeatureFlag.AdminConsole,
            NexaFeatureFlag.RecorderReadOnly,
            NexaFeatureFlag.ReplaySafeMode
        };
        if (sensitive is NexaProfileSensitiveSiteMode.SimulationOnly or NexaProfileSensitiveSiteMode.EnterpriseControlled)
            enabled.Add(NexaFeatureFlag.SensitiveSimulation);
        return new NexaConfigurationProfile(
            kind,
            posture,
            new NexaConfigurationProfileFeatureSet(enabled, DangerousFeatures()),
            new NexaConfigurationProfileLimitSet([new NexaUsageLimit($"limit-profile-{kind.ToString().ToLowerInvariant()}", 1000, TimeSpan.FromDays(30))]),
            new NexaConfigurationProfilePolicy(RequiresAdminApproval: kind is NexaConfigurationProfileKind.PrivateBeta or NexaConfigurationProfileKind.EnterpriseControlled, RequiresComplianceDecision: requiresCompliance, AllowRealBilling: false, AllowRealEmail: false, AllowPublicSaasActivation: false, SupportMetadataOnly: true, DiagnosticsRedacted: true, ExposesProfileRaw: false),
            vault,
            runtime,
            recorder,
            sensitive,
            NexaProfileExternalMode.MockOnly,
            NexaProfileExternalMode.MockOnly);
    }

    private static IReadOnlySet<NexaFeatureFlag> DangerousFeatures() =>
        new HashSet<NexaFeatureFlag>
        {
            NexaFeatureFlag.SensitiveRealPilot,
            NexaFeatureFlag.ProductiveVault,
            NexaFeatureFlag.RecorderProductive,
            NexaFeatureFlag.ReplayProductive
        };
}

public sealed record NexaConfigurationProfileRequest(
    NexaConfigurationProfile Profile,
    IReadOnlySet<NexaFeatureFlag> RequestedFeatures,
    bool ProductiveVaultEntitlement,
    bool ComplianceDecisionApproved,
    bool AdminOverride,
    bool RealBillingRequested,
    bool RealEmailRequested,
    bool PublicSaasActivationRequested,
    bool ProfileRawRequested);

public sealed record NexaConfigurationProfileDecision(
    bool Allowed,
    string Reason,
    IReadOnlyList<string> Violations,
    bool Redacted);

public sealed record NexaConfigurationProfileEvaluationResult(
    NexaConfigurationProfileDecision Decision,
    NexaConfigurationProfile Profile);
