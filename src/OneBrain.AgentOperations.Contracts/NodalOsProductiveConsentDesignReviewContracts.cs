namespace OneBrain.AgentOperations.Contracts;

public enum NodalOsProductiveConsentReviewSection
{
    LedgerStorageBoundary,
    ConsentScopeModel,
    FreshnessPolicy,
    RevocationPolicy,
    PerCapabilityConsentRequirements,
    DisclosureRequirements,
    AuditTrailRequirements,
    EvidenceTimelineRequirements,
    FailClosedBehavior,
    MigrationRollbackDisableStrategy,
    LocalOnlyDefault,
    NoCloudByDefault,
    NoLlmImplication
}

public sealed record NodalOsProductiveConsentDesignReview
{
    public required string ReviewId { get; init; }
    public required string DesignDraftRef { get; init; }
    public required string AuditCheckpointRef { get; init; }
    public required string DisabledAccessRoadmapRef { get; init; }
    public required string WorkspaceRef { get; init; }
    public required string MissionRef { get; init; }
    public required bool IsReviewOnly { get; init; }
    public required bool IsNoOp { get; init; }
    public required bool CanApproveImplementation { get; init; }
    public required bool CanAuthorizeProductiveConsent { get; init; }
    public required bool CanPersistConsent { get; init; }
    public required bool CanEnforceConsent { get; init; }
    public required bool CanAuthorizeCapability { get; init; }
    public required bool CanAuthorizeFilesystemAccess { get; init; }
    public required bool CanAuthorizeLlmContext { get; init; }
    public required bool CanUseCloud { get; init; }
    public IReadOnlyList<NodalOsProductiveConsentReviewSection> ReviewSections { get; init; } = [];
    public required NodalOsProductiveConsentReviewFindings Findings { get; init; }
    public required NodalOsProductiveConsentReviewDecision Decision { get; init; }
}

public sealed record NodalOsProductiveConsentReviewFindings
{
    public IReadOnlyList<string> AcceptedDesignAssumptionsRedacted { get; init; } = [];
    public IReadOnlyList<string> OpenQuestionsRedacted { get; init; } = [];
    public IReadOnlyList<string> BlockersRedacted { get; init; } = [];
    public IReadOnlyList<string> RequiredAuditsRedacted { get; init; } = [];
    public IReadOnlyList<string> RequiredAdversarialTestsRedacted { get; init; } = [];
    public IReadOnlyList<string> ImplementationPrerequisitesRedacted { get; init; } = [];
    public IReadOnlyList<string> UserFacingRiskNotesRedacted { get; init; } = [];
}

public sealed record NodalOsProductiveConsentReviewDecision
{
    public required string DecisionId { get; init; }
    public required bool ReadyForDesignReviewCloseout { get; init; }
    public required bool ReadyForProductiveConsentImplementation { get; init; }
    public required bool ReadyForConsentPersistence { get; init; }
    public required bool ReadyForConsentEnforcement { get; init; }
    public required bool ReadyForCapabilityAuthorization { get; init; }
    public required bool ReadyForFilesystemAccess { get; init; }
    public required bool ReadyForLlmContext { get; init; }
    public IReadOnlyList<string> RequiredBeforeImplementationRedacted { get; init; } = [];
}
