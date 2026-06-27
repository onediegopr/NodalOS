namespace OneBrain.Core.Recipes;

public enum RecipeToolCategory
{
    BrowserRuntime,
    DesktopRuntime,
    Connector,
    LocalFile,
    CloudApi,
    LocalApi,
    Microsoft365,
    SAP,
    Marketplace,
    Payment,
    Fiscal,
    ERP,
    Email,
    Messaging,
    Storage,
    OCR,
    Vision,
    LLM,
    Unknown
}

public enum RecipeToolTrustLevel
{
    Untrusted,
    Candidate,
    Reviewed,
    ApprovedForPreview,
    ApprovedForDryRun,
    ApprovedForFixture,
    ApprovedForManualAssist,
    LiveBlocked,
    Disabled,
    Deprecated
}

public enum RecipeToolRuntimeStatus
{
    NotImplemented,
    FixtureOnly,
    ReferenceOnly,
    PreviewOnly,
    DryRunOnly,
    ManualAssistOnly,
    LiveBlocked,
    FutureGated,
    Disabled
}

public enum RecipeToolPermission
{
    ReadMetadata,
    ReadData,
    WriteDraft,
    MutateData,
    Submit,
    ExecutePayment,
    SendMessage,
    Publish,
    Delete,
    Unknown
}

public enum RecipeToolPermissionScope
{
    Workspace,
    Mission,
    Recipe,
    Tool,
    Organization,
    ExternalSystemRef,
    Unknown
}

public sealed record RecipeToolTrustRef(string ToolTrustId);

public sealed record RecipeToolCapabilityRef(string CapabilityId);

public sealed record RecipeToolCapability(
    string CapabilityId,
    string DisplayName,
    RecipeToolPermission Permission,
    bool FixtureOnly = true)
{
    public bool LiveRuntimeEnabled => false;
}

public sealed record RecipeToolOwner(string OwnerRef, string ContactRef);

public sealed record RecipeToolRiskProfile(
    RecipeRiskLevel RiskLevel,
    IReadOnlySet<SensitiveActionCategory> SensitiveCategories,
    bool RequiresHumanReview);

public sealed record RecipeToolEvidencePolicy(IReadOnlyList<string> EvidencePolicyRefs);

public sealed record RecipeToolApprovalPolicy(IReadOnlyList<string> ApprovalPolicyRefs, bool ApprovalRequired);

public sealed record RecipeToolSecretPolicy(IReadOnlyList<string> RequiredSecretRefs, bool SecretValuesAllowed = false);

public sealed record RecipeToolTrustEntry(
    string ToolId,
    string DisplayName,
    string ProviderSystemFamily,
    RecipeToolCategory Category,
    IReadOnlyList<RecipeToolCapability> SupportedCapabilities,
    RecipeToolTrustLevel TrustLevel,
    RecipeToolRuntimeStatus RuntimeStatus,
    IReadOnlyList<RecipeRunMode> AllowedRunModes,
    IReadOnlySet<RecipeActionCategory> AllowedActionCategories,
    IReadOnlySet<RecipeActionCategory> BlockedActionCategories,
    IReadOnlyList<RecipeToolPermissionScope> PermissionScopes,
    IReadOnlyList<string> RequiredSecretRefs,
    IReadOnlyList<string> RequiredApprovalPolicyRefs,
    IReadOnlyList<string> EvidencePolicyRefs,
    IReadOnlyList<string> RedactionPolicyRefs,
    RecipeToolOwner Owner,
    string Version,
    DateTimeOffset? UpdatedAt,
    bool Deprecated,
    bool Disabled,
    string NotesRef)
{
    public bool LiveRuntimeEnabled => false;
    public bool ConnectorExecutionEnabled => false;
    public bool IsTrustedForFixture =>
        !Disabled &&
        !Deprecated &&
        TrustLevel is RecipeToolTrustLevel.ApprovedForFixture or RecipeToolTrustLevel.ApprovedForManualAssist;

    public bool IsLiveBlocked =>
        RuntimeStatus is RecipeToolRuntimeStatus.LiveBlocked or RecipeToolRuntimeStatus.FutureGated or RecipeToolRuntimeStatus.Disabled ||
        TrustLevel is RecipeToolTrustLevel.LiveBlocked or RecipeToolTrustLevel.Disabled or RecipeToolTrustLevel.Deprecated ||
        Category is RecipeToolCategory.BrowserRuntime or RecipeToolCategory.DesktopRuntime;

    public static RecipeToolTrustEntry CandidateConnector(string toolId, string displayName) =>
        new(
            toolId,
            displayName,
            "fixture-provider",
            RecipeToolCategory.Connector,
            [],
            RecipeToolTrustLevel.Candidate,
            RecipeToolRuntimeStatus.ReferenceOnly,
            [RecipeRunMode.CatalogPreview],
            new HashSet<RecipeActionCategory> { RecipeActionCategory.ReadOnlyObservation },
            new HashSet<RecipeActionCategory> { RecipeActionCategory.ExternalSystemCall },
            [RecipeToolPermissionScope.Tool],
            [],
            [],
            [],
            [],
            new RecipeToolOwner("owner.fixture", "contact.fixture"),
            "1.0.0",
            DateTimeOffset.Parse("2026-06-27T00:00:00Z"),
            Deprecated: false,
            Disabled: false,
            NotesRef: "tool.notes.ref");
}

public sealed record RecipeToolTrustRegistry(IReadOnlyList<RecipeToolTrustEntry> Entries)
{
    public bool LiveRuntimeEnabled => false;
    public RecipeToolTrustEntry? Find(string toolId) => Entries.FirstOrDefault(e => e.ToolId == toolId);
}

public enum RecipeSecretKind
{
    ApiKey,
    OAuthToken,
    RefreshToken,
    Username,
    Password,
    Certificate,
    PrivateKey,
    SessionCookie,
    ClientSecret,
    WebhookSecret,
    FiscalCertificate,
    PaymentCredential,
    MarketplaceCredential,
    ERPConnectionSecret,
    DatabaseConnectionSecret,
    UnknownSecret
}

public enum RecipeSecretScope
{
    User,
    Workspace,
    Mission,
    Recipe,
    Tool,
    Organization,
    EnvironmentRef,
    ExternalVaultRef,
    Unknown
}

public enum RecipeSecretPresenceStatus
{
    Unknown,
    RequiredMissing,
    ReferenceDeclared,
    PresentByReference,
    ExpiredByReference,
    RevokedByReference,
    InvalidByReference,
    BlockedRawValueDetected,
    BlockedScopeMismatch,
    BlockedByPolicy
}

public sealed record RecipeSecretRef(
    string SecretRefId,
    string DisplayAlias,
    RecipeSecretKind SecretKind,
    RecipeSecretScope Scope,
    string OwningToolOrSystemRef,
    RecipeSecretPresenceStatus PresenceStatus,
    string? RotationMetadataRef,
    string? LastVerifiedSummaryRef,
    string RedactionPolicyRef,
    IReadOnlyList<RecipeRunMode> AllowedUsageModes,
    IReadOnlyList<RecipeRunMode> BlockedUsageModes,
    string OperatorVisibleSummary,
    bool RawValuePresent = false)
{
    public bool StoresRawSecretValue => RawValuePresent;
    public bool LiveRuntimeEnabled => false;
}

public sealed record RecipeSecretRequirement(
    string RequirementId,
    string SecretRefId,
    RecipeSecretKind SecretKind,
    RecipeSecretScope RequiredScope,
    string OwningToolOrSystemRef,
    bool Required,
    RecipeSecretPresenceStatus PresenceStatus,
    bool RawValuePresent,
    string RedactionPolicyRef);

public sealed record RecipeSecretHandlingPolicy(
    IReadOnlyList<RecipeSecretRequirement> Requirements,
    bool RawSecretValuesAllowed = false);

public sealed record RecipeSecretRedactionPolicy(
    string RedactionPolicyRef,
    IReadOnlySet<RecipeSensitiveFieldCategory> SensitiveCategories,
    bool OperatorSummaryMayContainValues = false);

public sealed record RecipeSecretReadiness(
    bool IsReady,
    RecipeReadinessStatus Status,
    IReadOnlyList<RecipeReadinessIssue> BlockingIssues,
    bool RawSecretDetected,
    bool LiveRuntimeEnabled = false);

public enum RecipeCredentialedActionDecisionStatus
{
    ReadyForPreview,
    ReadyForFixture,
    BlockedMissingToolTrust,
    BlockedMissingSecretReference,
    BlockedRawSecretDetected,
    BlockedScopeMismatch,
    BlockedMissingApprovalPolicy,
    BlockedUntrustedTool,
    BlockedLiveRuntimeDisabled,
    BlockedConnectorExecution,
    BlockedByPolicy
}

public sealed record RecipeCredentialedActionRequirement(
    string RequirementId,
    string ToolTrustRef,
    IReadOnlyList<string> RequiredSecretRefs,
    IReadOnlyList<RecipeSecretScope> AllowedSecretScopes,
    bool ApprovalNarrativeRequired,
    bool ApprovalNarrativePresent,
    RecipeConnectorActionCategory? ConnectorActionCategory = null);

public sealed record RecipeCredentialedActionGate(
    string GateId,
    IReadOnlyList<RecipeCredentialedActionRequirement> Requirements,
    string EvidencePolicyRef,
    string RedactionPolicyRef);

public sealed record RecipeCredentialedActionDecision(
    RecipeCredentialedActionDecisionStatus Status,
    string Summary,
    bool AllowsLiveRuntime = false,
    bool AllowsConnectorExecution = false,
    bool ActionAuthorityGranted = false);

public sealed record RecipeCredentialedActionReadiness(
    bool IsReady,
    RecipeCredentialedActionDecision Decision,
    IReadOnlyList<RecipeReadinessIssue> BlockingIssues);

public enum RecipeConnectorRuntimeMode
{
    ReferenceOnly,
    FixtureOnly,
    PreviewOnly,
    DryRunOnly,
    ManualAssistOnly,
    LiveBlocked,
    FutureGated,
    Disabled
}

public enum RecipeConnectorActionCategory
{
    ReadMetadata,
    ReadData,
    WriteDraft,
    MutateData,
    SubmitFiscal,
    ExecutePayment,
    SendMessage,
    PublishListing,
    UpdatePrice,
    UpdateStock,
    DeleteData,
    Unknown
}

public sealed record RecipeConnectorTrustRequirement(
    string ToolTrustRef,
    IReadOnlyList<string> RequiredSecretRefs,
    bool ApprovalRequired,
    bool EvidencePolicyRequired);

public sealed record RecipeConnectorEligibility(
    string EligibilityId,
    string ConnectorToolRef,
    RecipeConnectorRuntimeMode RuntimeMode,
    RecipeConnectorActionCategory ActionCategory,
    RecipeConnectorTrustRequirement TrustRequirement,
    bool ApprovalPolicyPresent,
    bool EvidencePolicyPresent)
{
    public bool LiveRuntimeEnabled => false;
    public bool ConnectorExecutionEnabled => false;
}

public sealed record RecipeConnectorEligibilityDecision(
    bool EligibleForPreview,
    bool EligibleForFixture,
    bool LiveBlocked,
    bool RequiresApproval,
    RecipeReadinessStatus Status,
    IReadOnlyList<string> Reasons)
{
    public bool LiveRuntimeEnabled => false;
    public bool ConnectorExecutionEnabled => false;
}

public static class RecipeToolTrustSecretsPolicy
{
    private static readonly RecipeSecretKind[] HighRiskSecretKinds =
    [
        RecipeSecretKind.Password,
        RecipeSecretKind.PrivateKey,
        RecipeSecretKind.SessionCookie,
        RecipeSecretKind.PaymentCredential,
        RecipeSecretKind.FiscalCertificate
    ];

    public static RecipeSecretReadiness EvaluateSecretReadiness(
        RecipeSecretRequirement requirement,
        RecipeRiskProfile risk)
    {
        var blocking = new List<RecipeReadinessIssue>();

        if (requirement.RawValuePresent || requirement.PresenceStatus == RecipeSecretPresenceStatus.BlockedRawValueDetected)
            blocking.Add(Issue("raw-secret-detected", RecipeReadinessStatus.BlockedMissingSecretReference, "Secret values must remain by reference only."));

        if (requirement.Required && requirement.PresenceStatus is RecipeSecretPresenceStatus.Unknown or RecipeSecretPresenceStatus.RequiredMissing)
            blocking.Add(Issue("missing-secret-reference", RecipeReadinessStatus.BlockedMissingSecretReference, "Required secret reference is missing."));

        if (requirement.PresenceStatus == RecipeSecretPresenceStatus.BlockedScopeMismatch)
            blocking.Add(Issue("secret-scope-mismatch", RecipeReadinessStatus.BlockedMissingSecretReference, "Secret reference scope does not match requirement."));

        if (HighRiskSecretKinds.Contains(requirement.SecretKind) &&
            risk.OverallRisk is not (RecipeRiskLevel.High or RecipeRiskLevel.Critical or RecipeRiskLevel.Blocked) &&
            !risk.ApprovalPolicyPresent &&
            !risk.HumanInterventionPathPresent)
        {
            blocking.Add(Issue("high-risk-secret-requires-approval-or-human", RecipeReadinessStatus.BlockedMissingApprovalPolicy, "High-risk credential references require high/critical risk classification or human/approval path."));
        }

        return new(
            blocking.Count == 0,
            blocking.Count == 0 ? RecipeReadinessStatus.ReadyForFixtureRun : blocking[0].Status,
            blocking,
            RawSecretDetected: requirement.RawValuePresent,
            LiveRuntimeEnabled: false);
    }

    public static RecipeCredentialedActionReadiness EvaluateCredentialedAction(
        RecipeCredentialedActionRequirement requirement,
        RecipeToolTrustRegistry registry,
        IReadOnlyList<RecipeSecretRequirement> secrets,
        RecipeConnectorEligibility? connectorEligibility = null)
    {
        var blocking = new List<RecipeReadinessIssue>();
        var tool = registry.Find(requirement.ToolTrustRef);

        if (tool is null)
            blocking.Add(Issue("missing-tool-trust", RecipeReadinessStatus.BlockedMissingToolTrust, "Credentialed action requires a known tool trust ref."));
        else if (!tool.IsTrustedForFixture)
            blocking.Add(Issue("untrusted-tool", RecipeReadinessStatus.BlockedMissingToolTrust, "Tool is not trusted for fixture-safe use."));

        foreach (var secretRef in requirement.RequiredSecretRefs)
        {
            var secret = secrets.FirstOrDefault(s => s.SecretRefId == secretRef);
            if (secret is null || secret.PresenceStatus is RecipeSecretPresenceStatus.RequiredMissing or RecipeSecretPresenceStatus.Unknown)
            {
                blocking.Add(Issue("missing-secret-reference", RecipeReadinessStatus.BlockedMissingSecretReference, "Credentialed action requires declared secret refs."));
                continue;
            }

            if (secret.RawValuePresent || secret.PresenceStatus == RecipeSecretPresenceStatus.BlockedRawValueDetected)
                blocking.Add(Issue("raw-secret-detected", RecipeReadinessStatus.BlockedMissingSecretReference, "Raw secret value detected."));

            if (!requirement.AllowedSecretScopes.Contains(secret.RequiredScope) || secret.PresenceStatus == RecipeSecretPresenceStatus.BlockedScopeMismatch)
                blocking.Add(Issue("secret-scope-mismatch", RecipeReadinessStatus.BlockedMissingSecretReference, "Secret scope mismatch."));
        }

        if (requirement.ApprovalNarrativeRequired && !requirement.ApprovalNarrativePresent)
            blocking.Add(Issue("missing-approval-narrative", RecipeReadinessStatus.BlockedMissingApprovalPolicy, "Credentialed action requires approval narrative."));

        if (connectorEligibility is not null)
        {
            var connector = EvaluateConnectorEligibility(connectorEligibility);
            if (!connector.EligibleForPreview && !connector.EligibleForFixture)
                blocking.Add(Issue("connector-not-eligible", connector.Status, string.Join("; ", connector.Reasons)));
        }

        if (blocking.Count > 0)
        {
            var status = blocking[0].IssueId switch
            {
                "missing-tool-trust" => RecipeCredentialedActionDecisionStatus.BlockedMissingToolTrust,
                "untrusted-tool" => RecipeCredentialedActionDecisionStatus.BlockedUntrustedTool,
                "missing-secret-reference" => RecipeCredentialedActionDecisionStatus.BlockedMissingSecretReference,
                "raw-secret-detected" => RecipeCredentialedActionDecisionStatus.BlockedRawSecretDetected,
                "secret-scope-mismatch" => RecipeCredentialedActionDecisionStatus.BlockedScopeMismatch,
                "missing-approval-narrative" => RecipeCredentialedActionDecisionStatus.BlockedMissingApprovalPolicy,
                _ => RecipeCredentialedActionDecisionStatus.BlockedByPolicy
            };

            return new(false, new RecipeCredentialedActionDecision(status, blocking[0].Message), blocking);
        }

        return new(true, new RecipeCredentialedActionDecision(RecipeCredentialedActionDecisionStatus.ReadyForFixture, "Ready for fixture-safe credentialed preview."), []);
    }

    public static RecipeConnectorEligibilityDecision EvaluateConnectorEligibility(RecipeConnectorEligibility eligibility)
    {
        var reasons = new List<string>();
        var liveBlocked = eligibility.RuntimeMode is RecipeConnectorRuntimeMode.LiveBlocked or RecipeConnectorRuntimeMode.FutureGated or RecipeConnectorRuntimeMode.Disabled;

        if (eligibility.RuntimeMode is RecipeConnectorRuntimeMode.LiveBlocked or RecipeConnectorRuntimeMode.Disabled)
            reasons.Add("Connector live execution is blocked.");

        if (eligibility.RuntimeMode == RecipeConnectorRuntimeMode.FutureGated)
            reasons.Add("Connector runtime is future-gated until trust and secret policy allows reference/fixture use.");

        if (eligibility.ActionCategory is RecipeConnectorActionCategory.Unknown)
            reasons.Add("Unknown connector action is blocked.");

        if (RequiresApproval(eligibility.ActionCategory) && !eligibility.ApprovalPolicyPresent)
            reasons.Add($"{eligibility.ActionCategory} requires approval.");

        if (eligibility.TrustRequirement.EvidencePolicyRequired && !eligibility.EvidencePolicyPresent)
            reasons.Add("Connector eligibility requires evidence policy.");

        var previewOrFixtureMode = eligibility.RuntimeMode is RecipeConnectorRuntimeMode.ReferenceOnly or RecipeConnectorRuntimeMode.FixtureOnly or RecipeConnectorRuntimeMode.PreviewOnly or RecipeConnectorRuntimeMode.DryRunOnly or RecipeConnectorRuntimeMode.ManualAssistOnly;
        var eligible = reasons.Count == 0 && previewOrFixtureMode;

        return new(
            EligibleForPreview: eligible,
            EligibleForFixture: eligible && eligibility.RuntimeMode is RecipeConnectorRuntimeMode.FixtureOnly or RecipeConnectorRuntimeMode.PreviewOnly or RecipeConnectorRuntimeMode.DryRunOnly or RecipeConnectorRuntimeMode.ManualAssistOnly,
            LiveBlocked: true,
            RequiresApproval: RequiresApproval(eligibility.ActionCategory),
            Status: eligible ? RecipeReadinessStatus.ReadyForFixtureRun : RecipeReadinessStatus.BlockedRiskGate,
            Reasons: reasons.Count == 0 ? ["Connector is eligible for reference/fixture-only use; live execution remains blocked."] : reasons);
    }

    public static bool RequiresApproval(RecipeConnectorActionCategory category) =>
        category is RecipeConnectorActionCategory.SubmitFiscal or
            RecipeConnectorActionCategory.ExecutePayment or
            RecipeConnectorActionCategory.SendMessage or
            RecipeConnectorActionCategory.PublishListing or
            RecipeConnectorActionCategory.UpdatePrice or
            RecipeConnectorActionCategory.UpdateStock or
            RecipeConnectorActionCategory.DeleteData or
            RecipeConnectorActionCategory.MutateData;

    private static RecipeReadinessIssue Issue(string id, RecipeReadinessStatus status, string message) =>
        new(id, status, RecipeReadinessIssueSeverity.Blocking, message);
}
