using System.Text.RegularExpressions;

namespace OneBrain.Core.Recipes;

public enum RecipeCatalogSafetyBadgeKind
{
    Preview,
    FixtureSafe,
    ReadOnly,
    HumanReviewRequired,
    LiveBlocked,
    FutureGated,
    ReferenceOnly,
    SecretsByReference,
    EvidenceByReference,
    ObserveOnlyTrigger
}

public enum RecipeCatalogReadinessBadgeKind
{
    CatalogPreview,
    FixtureReady,
    MissingToolTrust,
    MissingSecretRefs,
    MissingValidation,
    MissingEvidence,
    MissingApprovalPath,
    FutureGated,
    LiveBlocked,
    BlockedByPolicy
}

public enum RecipeTemplateBlockingReasonCategory
{
    MissingLimits,
    MissingValidation,
    MissingEvidence,
    MissingApprovalPath,
    MissingHumanInterventionPath,
    MissingToolTrust,
    MissingSecretReference,
    RawSecretDetected,
    ToolLiveBlocked,
    ConnectorExecutionBlocked,
    BrowserRuntimeBlocked,
    DesktopRuntimeBlocked,
    TriggerAutorunBlocked,
    RecorderReplayBlocked,
    CaptureBlocked,
    LocatorRepairApplyBlocked,
    SensitiveActionRequiresReview,
    FiscalSubmissionBlocked,
    PaymentExecutionBlocked,
    MarketplaceMutationBlocked,
    MessageSendBlocked,
    DeleteWriteBlocked,
    UnknownSystemBlocked,
    UnknownUnsafe
}

public enum RecipeOperatorPreviewSectionKind
{
    Overview,
    Readiness,
    BlockingReasons,
    MissingRequirements,
    ApprovalPath,
    EvidenceValidation,
    ToolTrustSecrets,
    TriggerObserveOnly,
    LocatorCapturePreview,
    SafeNextAction,
    NotAutomated
}

public enum RecipeHandoffExportAvailability
{
    PreviewOnly,
    Disabled,
    NoRealFileWritten
}

public enum RecipeProductSurfaceDemoReadinessStatus
{
    ReadOnlyDemoReady,
    MissingSurface,
    BlockedByOverclaim,
    BlockedLiveRuntimeLeak,
    BlockedByPolicy
}

public enum RecipeProductSurfaceDemoStepKind
{
    BrowseCatalog,
    OpenRecipeLabSummary,
    OpenTemplateDetail,
    ReviewReadinessExplanation,
    ReviewOperatorPreview,
    ReviewHandoffExportPreview,
    UnderstandBlockedLiveRuntime,
    ReadSafeHandoffSummary
}

public enum RecipeProductSurfaceNavigationEntryKind
{
    RecipeCatalog,
    RecipeLab,
    TemplateDetail,
    ReadinessExplanation,
    OperatorPreview,
    HandoffExportPreview,
    SafeDemo
}

public enum RecipeProductSurfaceCapabilityBadgeKind
{
    ReadOnly,
    PreviewSafe,
    FixtureSafe,
    DemoSafe,
    LiveRuntimeBlocked,
    ConnectorExecutionDisabled,
    SecretsByReferenceOnly,
    ExportPreviewOnly,
    HumanApprovalPathRequired,
    NotAutomated
}

public enum RecipeProductSurfaceDisabledActionKind
{
    RecipeExecution,
    WorkitemProcessing,
    ConnectorApi,
    VaultSecrets,
    BrowserAutomation,
    DesktopAutomation,
    RecorderReplayCapture,
    ExportFileGeneration,
    FiscalPaymentMarketplaceMessageDeleteWrite
}

public enum RecipeProductSurfaceDemoFlowStepKind
{
    CatalogOverview,
    LabOverview,
    TemplateDetail,
    ReadinessExplanation,
    OperatorPreview,
    HandoffExportPreview,
    BlockedLiveRuntimeExplanation,
    SafeClosingSummary
}

public sealed record RecipeCatalogSafetyBadge(
    RecipeCatalogSafetyBadgeKind Kind,
    string Label,
    string RedactedSummary);

public sealed record RecipeCatalogReadinessBadge(
    RecipeCatalogReadinessBadgeKind Kind,
    string Label,
    string RedactedSummary);

public sealed record RecipeCatalogFilterState(
    IReadOnlySet<RecipeTemplateCategory> Categories,
    IReadOnlySet<RecipeTemplateRegion> Regions,
    IReadOnlySet<RecipeTemplateStatus> Statuses,
    bool ShowPreviewOnly = true,
    bool ShowLiveBlocked = true,
    bool ShowHumanReviewRequired = true);

public sealed record RecipeTemplateCardViewModel(
    string TemplateId,
    string DisplayName,
    string Description,
    string PackName,
    RecipeTemplateSystem SystemFamily,
    RecipeTemplateRegion Region,
    IReadOnlyList<RecipeTemplateCountry> Countries,
    RecipeTemplateCategory Category,
    RecipeTemplateRuntimeEligibility RuntimeEligibility,
    RecipeTemplateStatus TemplateStatus,
    RecipeCatalogReadinessBadge ReadinessBadge,
    RecipeRiskLevel RiskLevel,
    bool RequiresHumanReview,
    string ToolTrustSummary,
    string SecretRefSummary,
    string TriggerStatusSummary,
    string LiveRuntimeStatus,
    string SafeNextActionSummary,
    string NotIncludedSummary,
    IReadOnlyList<RecipeCatalogSafetyBadge> SafetyBadges,
    IReadOnlyList<string> BlockingSummaries)
{
    public bool ReadOnly => true;
    public bool PreviewSafe => true;
    public bool CanStartRecipeRun => false;
    public bool CanProcessWorkitem => false;
    public bool CanOpenConnector => false;
    public bool CanRequestRawSecret => false;
    public bool CanReadRawSecret => false;
    public bool CanEnableBrowserRuntime => false;
    public bool CanEnableDesktopRuntime => false;
    public bool CanEnableConnectorExecution => false;
    public bool CanAuthorizeLiveRuntime => false;
    public bool LiveRuntimeEnabled => false;
    public bool ConnectorExecutionEnabled => false;
    public bool BrowserAutomationEnabled => false;
    public bool DesktopAutomationEnabled => false;
}

public sealed record RecipeCatalogPackViewModel(
    string PackId,
    string PackName,
    RecipeTemplateCategory Category,
    RecipeTemplateRegion Region,
    int TotalTemplates,
    int FixtureReadyCount,
    int LiveBlockedOrFutureGatedCount,
    string SafetySummary,
    IReadOnlyList<RecipeTemplateCardViewModel> Templates)
{
    public bool ReadOnly => true;
    public bool PreviewSafe => true;
    public bool CanStartRecipeRun => false;
    public bool CanProcessWorkitem => false;
    public bool LiveRuntimeEnabled => false;
}

public sealed record RecipeCatalogViewModel(
    string CatalogId,
    string Version,
    int TotalTemplates,
    IReadOnlyList<RecipeCatalogPackViewModel> Packs,
    RecipeCatalogFilterState FilterState,
    string ProductSurfaceSummary,
    IReadOnlyList<RecipeCatalogSafetyBadge> GlobalSafetyBadges)
{
    public bool ReadOnly => true;
    public bool PreviewSafe => true;
    public bool FixtureSafeOnly => true;
    public bool CanStartRecipeRun => false;
    public bool CanProcessWorkitem => false;
    public bool CanRequestSecrets => false;
    public bool CanEnableConnectorExecution => false;
    public bool CanEnableBrowserRuntime => false;
    public bool CanEnableDesktopRuntime => false;
    public bool LiveRuntimeEnabled => false;
}

public sealed record RecipeCatalogSurface(
    string SurfaceId,
    RecipeCatalogViewModel ViewModel,
    IReadOnlyList<string> CategoryLabels,
    IReadOnlyList<string> SafetyCopy,
    bool ReadOnly = true,
    bool PreviewSafe = true)
{
    public bool CanStartRecipeRun => false;
    public bool CanProcessWorkitem => false;
    public bool CanEnableLiveRuntime => false;
    public bool CanOpenConnector => false;
    public bool CanRequestSecrets => false;
}

public sealed record RecipeTemplateDetailHeader(
    string TemplateId,
    string DisplayName,
    string Description,
    string PackName,
    RecipeTemplateSystem SystemFamily,
    RecipeTemplateCategory Category,
    RecipeTemplateRegion Region,
    IReadOnlyList<RecipeTemplateCountry> Countries,
    string BusinessUseCaseSummary);

public sealed record RecipeTemplateDetailPackSummary(
    string PackId,
    string PackName,
    RecipeTemplateCategory Category,
    RecipeTemplateRegion Region);

public sealed record RecipeTemplateDetailSystemSummary(
    RecipeTemplateSystem SystemFamily,
    string RedactedSummary,
    string ConnectorBoundarySummary,
    string RuntimeBoundarySummary,
    string HumanReviewSummary,
    IReadOnlyList<string> ApplicableSystemMetadata);

public sealed record RecipeTemplateDetailRequirementSummary(
    IReadOnlyList<string> RequiredCapabilities,
    IReadOnlyList<string> RequiredToolTrustRefs,
    IReadOnlyList<string> RequiredSecretRefs,
    IReadOnlyList<string> ConnectorEligibilityRefs,
    IReadOnlyList<string> TriggerRefs,
    IReadOnlyList<string> EvidenceRequirementRefs,
    IReadOnlyList<string> ValidationRequirementRefs,
    IReadOnlyList<string> ApprovalHumanInterventionRequirementRefs)
{
    public bool SecretValuesShown => false;
    public bool RawPayloadShown => false;
}

public sealed record RecipeTemplateDetailSafetySummary(
    RecipeRiskLevel RiskLevel,
    IReadOnlyList<SensitiveActionCategory> SensitiveActionCategories,
    bool RequiresHumanReview,
    string LiveBlockedExplanation,
    string NotIncludedSummary,
    IReadOnlyList<RecipeCatalogSafetyBadge> SafetyBadges);

public sealed record RecipeTemplateMissingRequirement(
    string RequirementId,
    RecipeTemplateBlockingReasonCategory Category,
    string RedactedSummary,
    RecipeReadinessIssueSeverity Severity);

public sealed record RecipeTemplateBlockingReason(
    string ReasonId,
    RecipeTemplateBlockingReasonCategory Category,
    RecipeReadinessStatus Status,
    string RedactedSummary,
    RecipeReadinessIssueSeverity Severity);

public sealed record RecipeTemplateWarning(
    string WarningId,
    RecipeTemplateBlockingReasonCategory Category,
    string RedactedSummary,
    RecipeReadinessIssueSeverity Severity);

public sealed record RecipeTemplateReadinessReason(
    string ReasonId,
    string RedactedSummary,
    bool IsBlocking,
    RecipeReadinessIssueSeverity Severity);

public sealed record RecipeTemplateSafeNextActionExplanation(
    RecipeSafeNextActionKind Kind,
    string RedactedSummary,
    bool AllowsLiveRuntime = false,
    bool AllowsExternalMutation = false)
{
    public bool ActionAuthorityGranted => false;
}

public sealed record RecipeTemplateFutureEnablementNote(
    string NoteId,
    RecipeTemplateBlockingReasonCategory Category,
    string RedactedSummary,
    bool EnabledNow = false);

public sealed record RecipeTemplateReadinessExplanation(
    bool IsPreviewable,
    bool IsFixtureReady,
    RecipeTemplateStatus ReadinessStatus,
    RecipeReadinessStatus CanonicalReadinessStatus,
    string OperatorVisibleSummary,
    IReadOnlyList<RecipeTemplateReadinessReason> Reasons,
    IReadOnlyList<RecipeTemplateBlockingReason> BlockingReasons,
    IReadOnlyList<RecipeTemplateMissingRequirement> MissingRequirements,
    IReadOnlyList<RecipeTemplateWarning> Warnings,
    IReadOnlyList<RecipeTemplateFutureEnablementNote> FutureEnablementNotes,
    RecipeTemplateSafeNextActionExplanation SafeNextAction,
    IReadOnlyList<RecipeRunMode> BlockedRunModes,
    string ExplicitlyNotIncludedSummary)
{
    public bool LiveRuntimeEnabled => false;
    public bool ConnectorExecutionEnabled => false;
    public bool StartsRecipeRun => false;
    public bool ProcessesWorkitems => false;
}

public sealed record RecipeTemplateDetailSection(
    string SectionId,
    string Label,
    RecipeLabSectionStatus Status,
    string RedactedSummary,
    IReadOnlyList<string> SourceRefs)
{
    public bool ReadOnly => true;
    public bool CanExecute => false;
    public bool CanApply => false;
}

public sealed record RecipeTemplateDetailViewModel(
    string ViewModelId,
    RecipeTemplateDetailHeader Header,
    RecipeTemplateDetailPackSummary PackSummary,
    RecipeTemplateDetailSystemSummary SystemSummary,
    RecipeTemplateDetailSafetySummary SafetySummary,
    RecipeTemplateDetailRequirementSummary Requirements,
    RecipeTemplateReadinessExplanation ReadinessExplanation,
    IReadOnlyList<RecipeTemplateDetailSection> Sections,
    string TriggerObserveOnlySummary,
    string EvidenceValidationSummary,
    string LocatorCaptureImplicationsSummary,
    string OperatorVisibleSummary)
{
    public bool ReadOnly => true;
    public bool PreviewSafe => true;
    public bool FixtureSafeOnly => true;
    public bool CanStartRecipeRun => false;
    public bool CanProcessWorkitem => false;
    public bool CanOpenConnector => false;
    public bool CanRequestRawSecret => false;
    public bool CanReadRawSecret => false;
    public bool CanEnableConnectorExecution => false;
    public bool CanEnableBrowserRuntime => false;
    public bool CanEnableDesktopRuntime => false;
    public bool CanCreateRecorder => false;
    public bool CanCreateReplay => false;
    public bool CanCreateCapture => false;
    public bool CanApplyLocatorRepair => false;
    public bool RawSecretValuesShown => false;
    public bool RawPayloadShown => false;
    public bool LiveRuntimeEnabled => false;
}

public sealed record RecipeTemplateDetailSurface(
    string SurfaceId,
    RecipeTemplateDetailViewModel ViewModel,
    IReadOnlyList<string> SafetyCopy,
    bool ReadOnly = true,
    bool PreviewSafe = true)
{
    public bool CanStartRecipeRun => false;
    public bool CanProcessWorkitem => false;
    public bool CanEnableLiveRuntime => false;
    public bool CanOpenConnector => false;
    public bool CanRequestSecrets => false;
    public bool CanCreateRecorder => false;
    public bool CanCreateReplay => false;
    public bool CanCreateCapture => false;
}

public sealed record RecipeOperatorPreviewReviewSection(
    RecipeOperatorPreviewSectionKind Kind,
    string Label,
    RecipeLabSectionStatus Status,
    string RedactedSummary,
    IReadOnlyList<string> SourceRefs)
{
    public bool ReadOnly => true;
}

public sealed record RecipeOperatorPreviewDisabledActionState(
    string ActionId,
    string Label,
    string DisabledReason,
    bool Available = false)
{
    public bool CanInvoke => false;
    public bool GrantsLiveRuntime => false;
}

public sealed record RecipeOperatorPreviewViewModel(
    string ViewModelId,
    RecipeTemplateDetailHeader Template,
    RecipeTemplateStatus PreviewStatus,
    RecipeReadinessStatus ReadinessStatus,
    string OperatorReviewSummary,
    IReadOnlyList<RecipeOperatorPreviewReviewSection> RequiredReviewSections,
    string RequiredApprovalsSummary,
    string RequiredEvidenceSummary,
    IReadOnlyList<string> ExpectedHumanInterventionPoints,
    string BlockedLiveRuntimeExplanation,
    IReadOnlyList<RecipeOperatorPreviewDisabledActionState> DisabledActions,
    RecipeTemplateSafeNextActionExplanation SafeNextAction,
    string NotAutomatedSummary,
    string SystemSpecificPreviewSummary)
{
    public bool ReadOnly => true;
    public bool PreviewSafe => true;
    public bool CanStartRecipeRun => false;
    public bool CanProcessWorkitem => false;
    public bool CanOpenConnector => false;
    public bool CanRequestSecrets => false;
    public bool CanEnableLiveRuntime => false;
    public bool CanCreateScheduler => false;
    public bool CanCreateWatcherHookOrListener => false;
    public bool CanCreateRecorderReplayOrCapture => false;
    public bool CanApplyLocatorRepair => false;
    public bool LiveRuntimeEnabled => false;
}

public sealed record RecipeHandoffExportPreviewViewModel(
    string ViewModelId,
    string HandoffTitle,
    string TemplateSummary,
    string ReadinessSnapshot,
    IReadOnlyList<RecipeTemplateBlockingReason> BlockingReasons,
    IReadOnlyList<RecipeTemplateMissingRequirement> MissingRequirements,
    string ApprovalPathSummary,
    string ToolTrustSummary,
    string SecretReferencesSummary,
    IReadOnlyList<string> EvidenceRequirements,
    IReadOnlyList<string> ValidationRequirements,
    string LocatorCaptureImplications,
    string TriggerObserveOnlySummary,
    IReadOnlyList<string> OperatorNotes,
    IReadOnlyList<string> NotIncludedNotAutomated,
    RecipeHandoffExportAvailability ExportAvailability,
    string ProductSafeCopy)
{
    public bool ReadOnly => true;
    public bool PreviewOnly => true;
    public bool WritesRealFile => false;
    public bool GeneratesPdfOrDocx => false;
    public bool OpensSaveDialog => false;
    public bool CallsNetwork => false;
    public bool OpensConnector => false;
    public bool ReadsSecrets => false;
    public bool RawSecretValuesShown => false;
    public bool RawPayloadShown => false;
    public bool CanStartRecipeRun => false;
    public bool CanProcessWorkitem => false;
    public bool LiveRuntimeEnabled => false;
}

public sealed record RecipeOperatorPreviewHandoffExportSurface(
    string SurfaceId,
    RecipeOperatorPreviewViewModel OperatorPreview,
    RecipeHandoffExportPreviewViewModel HandoffExportPreview,
    IReadOnlyList<string> SafetyCopy,
    bool ReadOnly = true,
    bool PreviewSafe = true)
{
    public bool CanStartRecipeRun => false;
    public bool CanProcessWorkitem => false;
    public bool CanEnableLiveRuntime => false;
    public bool CanOpenConnector => false;
    public bool CanRequestSecrets => false;
    public bool CanWriteExportFile => false;
}

public sealed record RecipeProductSurfaceCapabilityMatrix(
    IReadOnlyList<string> AllowedCapabilities,
    IReadOnlyList<string> BlockedCapabilities)
{
    public bool LiveRuntimeAllowed => false;
    public bool RealExportAllowed => false;
    public bool ExternalMutationAllowed => false;
}

public sealed record RecipeProductSurfaceFinalCompositionViewModel(
    string ViewModelId,
    RecipeProductSurfaceDemoReadinessStatus DemoReadinessStatus,
    string CatalogReadinessSummary,
    string LabReadinessSummary,
    string TemplateDetailReadinessSummary,
    string OperatorPreviewReadinessSummary,
    string HandoffExportPreviewReadinessSummary,
    string BlockedLiveRuntimeState,
    string DisabledActionStateSummary,
    string SafeNextActionSummary,
    string NotAutomatedSummary,
    string InternalAllowedClaimSummary,
    string BlockedForbiddenClaimSummary,
    RecipeProductSurfaceCapabilityMatrix CapabilityMatrix)
{
    public bool ReadOnly => true;
    public bool PreviewSafe => true;
    public bool FixtureSafeOnly => true;
    public bool CanStartRecipeRun => false;
    public bool CanProcessWorkitem => false;
    public bool CanEnableLiveRuntime => false;
    public bool CanOpenConnector => false;
    public bool CanRequestSecrets => false;
    public bool CanWriteExportFile => false;
    public bool CanCreateRecorderReplayOrCapture => false;
    public bool CanCreateSchedulerWatcherHookOrListener => false;
    public bool CanApplyLocatorRepair => false;
    public bool LiveRuntimeEnabled => false;
}

public sealed record RecipeProductSurfaceSafeDemoStep(
    RecipeProductSurfaceDemoStepKind Kind,
    string Label,
    string RedactedSummary,
    bool PreviewOnly = true)
{
    public bool StartsRecipeRun => false;
    public bool ProcessesWorkitem => false;
    public bool WritesFile => false;
    public bool CallsNetwork => false;
    public bool ReadsSecrets => false;
    public bool EnablesAutomation => false;
}

public sealed record RecipeProductSurfaceSafeDemoScenario(
    string ScenarioId,
    string DisplayName,
    IReadOnlyList<RecipeProductSurfaceSafeDemoStep> Steps,
    string CorrectProductClaim,
    string BlockedClaimSummary,
    string SafeDemoReadinessSummary)
{
    public bool ReadOnly => true;
    public bool PreviewOnly => true;
    public bool CanStartRecipeRun => false;
    public bool CanWriteExportFile => false;
    public bool CanCallConnectorOrNetwork => false;
    public bool CanReadSecrets => false;
    public bool CanRecordReplayOrCapture => false;
    public bool CanProcessWorkitem => false;
    public bool LiveRuntimeEnabled => false;
}

public sealed record RecipeProductSurfaceSafeDemoReadinessSurface(
    string SurfaceId,
    RecipeProductSurfaceFinalCompositionViewModel FinalComposition,
    RecipeProductSurfaceSafeDemoScenario DemoScenario,
    IReadOnlyList<string> SafeUxCopy,
    bool ReadOnly = true,
    bool PreviewSafe = true)
{
    public bool CanStartRecipeRun => false;
    public bool CanProcessWorkitem => false;
    public bool CanEnableLiveRuntime => false;
    public bool CanOpenConnector => false;
    public bool CanRequestSecrets => false;
    public bool CanWriteExportFile => false;
    public bool CanRecordReplayOrCapture => false;
}

public sealed record RecipeProductSurfaceNavigationLabel(
    RecipeProductSurfaceNavigationEntryKind Kind,
    string Label,
    string OperatorSummary,
    string RouteHint,
    RecipeProductSurfaceCapabilityBadgeKind PrimaryBadge)
{
    public bool ReadOnly => true;
    public bool PreviewSafe => true;
    public bool StartsRecipeRun => false;
    public bool OpensConnector => false;
    public bool RequestsSecrets => false;
    public bool EnablesAutomation => false;
    public bool CreatesCaptureReplay => false;
    public bool LiveRuntimeEnabled => false;
}

public sealed record RecipeProductSurfaceCapabilityStatusBadge(
    RecipeProductSurfaceCapabilityBadgeKind Kind,
    string Label,
    string RedactedSummary)
{
    public bool GrantsCapability => false;
    public bool GrantsLiveRuntime => false;
    public bool GrantsConnectorExecution => false;
    public bool GrantsSecretAccess => false;
    public bool GrantsExternalMutation => false;
}

public sealed record RecipeProductSurfaceDisabledActionMessage(
    RecipeProductSurfaceDisabledActionKind Kind,
    string Label,
    string BlockedReason,
    string SafeNextAction)
{
    public bool Available => false;
    public bool CanInvoke => false;
    public bool GrantsLiveRuntime => false;
    public bool CallsConnectorOrNetwork => false;
    public bool ReadsSecrets => false;
    public bool WritesExternalSystem => false;
    public bool WritesFile => false;
}

public sealed record RecipeProductSurfaceNavigationMessagingTaxonomy(
    string TaxonomyId,
    string LineId,
    string ClosedProductSurfaceStatus,
    string FinalCloseCommit,
    IReadOnlyList<RecipeProductSurfaceNavigationLabel> NavigationLabels,
    IReadOnlyList<RecipeProductSurfaceCapabilityStatusBadge> CapabilityBadges,
    IReadOnlyList<RecipeProductSurfaceDisabledActionMessage> DisabledActionMessages,
    string AllowedFinalClaim,
    string ForbiddenFinalClaim,
    string NoLiveNoAutomationCopyPolicy)
{
    public bool ReadOnly => true;
    public bool PreviewSafe => true;
    public bool FixtureSafeOnly => true;
    public bool CanStartRecipeRun => false;
    public bool CanProcessWorkitem => false;
    public bool CanEnableLiveRuntime => false;
    public bool CanOpenConnector => false;
    public bool CanRequestSecrets => false;
    public bool CanCallNetwork => false;
    public bool CanCreateSchedulerWatcherHookOrListener => false;
    public bool CanCreateRecorderReplayOrCapture => false;
    public bool CanWriteExportFile => false;
    public bool CanApplyLocatorRepair => false;
    public bool LiveRuntimeEnabled => false;
}

public sealed record RecipeProductSurfaceDemoFlowStepCopy(
    RecipeProductSurfaceDemoFlowStepKind Kind,
    string StepId,
    string Title,
    string Subtitle,
    string OperatorDescription,
    IReadOnlyList<RecipeProductSurfaceCapabilityBadgeKind> SafetyBadges,
    string BlockedActionNote,
    string SafeNextAction,
    IReadOnlyList<string> UnavailableActionLabels,
    string ClaimGuardrailReminder)
{
    public bool ReadOnly => true;
    public bool PreviewSafe => true;
    public bool StartsRecipeRun => false;
    public bool ProcessesWorkitem => false;
    public bool OpensConnector => false;
    public bool RequestsSecrets => false;
    public bool CallsNetwork => false;
    public bool WritesFile => false;
    public bool EnablesBrowserAutomation => false;
    public bool EnablesDesktopAutomation => false;
    public bool CreatesRecorderReplayOrCapture => false;
    public bool EnablesExternalMutation => false;
    public bool LiveRuntimeEnabled => false;
}

public sealed record RecipeProductSurfaceDemoFlowEmptyStateCopy(
    string StateId,
    string Label,
    string RedactedSummary,
    string SafeNextAction)
{
    public bool ReadOnly => true;
    public bool PreviewOnly => true;
    public bool StartsRecipeRun => false;
    public bool CallsNetwork => false;
    public bool ReadsSecrets => false;
    public bool WritesFile => false;
}

public sealed record RecipeProductSurfaceDemoFlowCopySet(
    string Intro,
    IReadOnlyList<string> StepTransitions,
    IReadOnlyList<RecipeProductSurfaceDemoFlowEmptyStateCopy> EmptyStates,
    IReadOnlyList<string> DisabledControlCopy,
    string BlockedLiveRuntimeCopy,
    string ExportPreviewOnlyCopy,
    string NoCredentialsReadCopy,
    string NoConnectorApiCallsCopy,
    string NoBrowserDesktopAutomationCopy,
    string NoRecordingPlaybackCaptureCopy,
    string NoAutomaticWorkitemProcessingCopy,
    string FinalSummary)
{
    public bool ProductFacing => true;
    public bool ReadOnly => true;
    public bool PreviewSafe => true;
}

public sealed record RecipeProductSurfaceDemoFlowCopySurface(
    string SurfaceId,
    RecipeProductSurfaceNavigationMessagingTaxonomy Taxonomy,
    IReadOnlyList<RecipeProductSurfaceDemoFlowStepCopy> Steps,
    RecipeProductSurfaceDemoFlowCopySet Microcopy,
    string AllowedFinalClaim,
    string ForbiddenFinalClaim,
    bool ReadOnly = true,
    bool PreviewSafe = true)
{
    public bool FixtureSafeOnly => true;
    public bool CanStartRecipeRun => false;
    public bool CanProcessWorkitem => false;
    public bool CanEnableLiveRuntime => false;
    public bool CanOpenConnector => false;
    public bool CanRequestSecrets => false;
    public bool CanCallNetwork => false;
    public bool CanCreateSchedulerWatcherHookOrListener => false;
    public bool CanCreateRecorderReplayOrCapture => false;
    public bool CanWriteExportFile => false;
    public bool CanApplyLocatorRepair => false;
    public bool LiveRuntimeEnabled => false;
}

public sealed record RecipeLabSectionViewModel(
    string SectionId,
    string Label,
    RecipeLabSectionStatus Status,
    string RedactedSummary,
    IReadOnlyList<string> SourceRefs)
{
    public bool ReadOnly => true;
    public bool CanExecute => false;
    public bool CanApply => false;
}

public sealed record RecipeLabNotebookCellViewModel(
    string CellId,
    RecipeLabCellKind Kind,
    RecipeLabCellStatus Status,
    string Label,
    string RedactedSummary,
    IReadOnlyList<string> SourceRefs)
{
    public bool InspectionOnly => true;
    public bool CanExecute => false;
    public bool CanApplyRepair => false;
    public bool CanStartRecipeRun => false;
    public bool CanSubmit => false;
    public bool RawSecretValuesShown => false;
}

public sealed record RecipeLabBlockedReasonViewModel(
    string ReasonId,
    RecipeReadinessStatus Status,
    string RedactedSummary,
    RecipeReadinessIssueSeverity Severity);

public sealed record RecipeLabSafeNextActionViewModel(
    RecipeSafeNextActionKind Kind,
    string Summary,
    bool AllowsLiveRuntime = false,
    bool AllowsExternalMutation = false)
{
    public bool ActionAuthorityGranted => false;
}

public sealed record RecipeLabReadOnlyViewModel(
    string ViewModelId,
    string RecipeId,
    string RecipeVersion,
    string DisplayName,
    RecipeLabReadinessSummary ReadinessSummary,
    IReadOnlyList<RecipeLabSectionViewModel> Sections,
    IReadOnlyList<RecipeLabNotebookCellViewModel> Cells,
    IReadOnlyList<RecipeLabBlockedReasonViewModel> BlockedReasons,
    RecipeLabSafeNextActionViewModel SafeNextAction,
    string EvidenceTimelineSummary,
    string ApprovalHumanSummary,
    string ToolTrustSecretSummary,
    string TriggerObserveOnlySummary,
    string LocatorRepairPreviewSummary,
    string CaptureDraftSummary,
    IReadOnlyList<RecipeRunMode> BlockedRunModes,
    string SafetyBoundarySummary)
{
    public bool ReadOnly => true;
    public bool PreviewSafe => true;
    public bool CanEditRecipeContracts => false;
    public bool CanStartRecipeRun => false;
    public bool CanProcessWorkitem => false;
    public bool CanExecuteAction => false;
    public bool CanApplyLocatorRepair => false;
    public bool CanReplayLocator => false;
    public bool CanRecordCapture => false;
    public bool CanApproveLiveRuntime => false;
    public bool RawSecretValuesShown => false;
    public bool RawPayloadShown => false;
    public bool LiveRuntimeEnabled => false;
}

public sealed record RecipeLabSurface(
    string SurfaceId,
    RecipeLabReadOnlyViewModel ViewModel,
    IReadOnlyList<string> SafetyCopy,
    bool ReadOnly = true,
    bool PreviewSafe = true)
{
    public bool CanStartRecipeRun => false;
    public bool CanProcessWorkitem => false;
    public bool CanEnableLiveRuntime => false;
    public bool CanApplyLocatorRepair => false;
    public bool CanRecordCapture => false;
}

public static class RecipeProductSurfaceCopyPolicy
{
    public static IReadOnlyList<string> AllowedCopy { get; } =
    [
        "Preview",
        "Fixture-safe",
        "Read-only",
        "Template",
        "Template detail",
        "Draft",
        "Requires human review",
        "Approval required",
        "Live runtime blocked",
        "Connector execution not enabled",
        "Browser automation not enabled",
        "Desktop automation not enabled",
        "Secrets by reference only",
        "Evidence by reference only",
        "Observe-only trigger",
        "Not included",
        "Future-gated"
    ];

    public static IReadOnlyList<string> ForbiddenCopy { get; } =
    [
        "Run recipe",
        "Run now",
        "Execute",
        "Automate now",
        "Autofill",
        "Apply",
        "Submit",
        "Sync live",
        "Pay",
        "Publish",
        "Send",
        "Invoice live",
        "Connect",
        "Connect now",
        "Use credentials",
        "Record",
        "Replay",
        "Capture now",
        "Execute now",
        "Start worker",
        "Trigger now",
        "Control browser",
        "Control desktop",
        "Live automation ready"
    ];

    public static IReadOnlyList<string> FindForbiddenCopy(IEnumerable<string> copy) =>
        copy
            .SelectMany(text => ForbiddenCopy
                .Where(term => ContainsForbiddenTerm(text, term))
                .Select(term => $"{term}: {text}"))
            .ToArray();

    private static bool ContainsForbiddenTerm(string text, string term)
    {
        var escaped = Regex.Escape(term);
        var pattern = char.IsLetterOrDigit(term[0]) && char.IsLetterOrDigit(term[^1])
            ? $@"\b{escaped}\b"
            : escaped;
        return Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}

public static class RecipeProductSurfaceFactory
{
    public static RecipeProductSurfaceDemoFlowCopySurface CreateDemoFlowCopySurface()
    {
        var taxonomy = CreateNavigationMessagingTaxonomy();
        var defaultBadges = new[]
        {
            RecipeProductSurfaceCapabilityBadgeKind.ReadOnly,
            RecipeProductSurfaceCapabilityBadgeKind.PreviewSafe,
            RecipeProductSurfaceCapabilityBadgeKind.FixtureSafe
        };

        var steps = new RecipeProductSurfaceDemoFlowStepCopy[]
        {
            DemoStep(
                RecipeProductSurfaceDemoFlowStepKind.CatalogOverview,
                "demo-flow.catalog-overview",
                "Browse Recipe Catalog",
                "Start with template categories and safety badges.",
                "The operator opens the Recipe Product Surface and browses fixture-safe catalog summaries.",
                defaultBadges,
                "Recipe execution is not enabled from this entrypoint.",
                "Review the catalog categories and live-blocked badges.",
                ["Recipe execution blocked", "Connector/API blocked", "Vault/secrets blocked"],
                taxonomy.AllowedFinalClaim),
            DemoStep(
                RecipeProductSurfaceDemoFlowStepKind.LabOverview,
                "demo-flow.lab-overview",
                "Review Recipe Lab",
                "Inspect readiness and referenced evidence.",
                "The operator reviews lab sections for readiness, evidence refs, approval refs, tool refs, trigger state, locator preview, and capture draft summary.",
                [.. defaultBadges, RecipeProductSurfaceCapabilityBadgeKind.LiveRuntimeBlocked],
                "Template cards cannot start recipes or process workitems.",
                "Open template detail preview for one selected template.",
                ["Workitem processing blocked", "Browser automation blocked", "Desktop automation blocked"],
                "The catalog supports inspection only; live automation claims stay blocked."),
            DemoStep(
                RecipeProductSurfaceDemoFlowStepKind.TemplateDetail,
                "demo-flow.template-detail",
                "Open Template Detail",
                "Explain system, region, category, requirements, and blocked modes.",
                "The operator reads what the selected template does, what requirements are missing, and what remains outside the current surface.",
                [.. defaultBadges, RecipeProductSurfaceCapabilityBadgeKind.SecretsByReferenceOnly],
                "The lab cannot change recipe contracts, open connectors, or reveal secret values.",
                "Read the readiness explanation before discussing handoff.",
                ["Vault/secrets blocked", "Recording/playback/capture-draft blocked"],
                "Recipe Lab is read-only and fixture-safe."),
            DemoStep(
                RecipeProductSurfaceDemoFlowStepKind.ReadinessExplanation,
                "demo-flow.readiness-explanation",
                "Read Readiness Explanation",
                "Understand why preview is allowed and live modes are blocked.",
                "The operator reviews missing requirements, blocking reasons, evidence expectations, approval path, and safe next action.",
                [.. defaultBadges, RecipeProductSurfaceCapabilityBadgeKind.HumanApprovalPathRequired],
                "Template detail cannot enable connector execution, browser automation, desktop automation, or external mutations.",
                "Continue to operator preview for the review sequence.",
                ["Connector/API blocked", "Fiscal/payment/marketplace/message/delete/write blocked"],
                "Template detail is product explanation, not runtime authority."),
            DemoStep(
                RecipeProductSurfaceDemoFlowStepKind.OperatorPreview,
                "demo-flow.operator-preview",
                "Review Operator Preview",
                "Walk through what a human would review.",
                "The operator sees required review sections, approvals, evidence, human intervention points, disabled actions, and not-automated summaries.",
                [.. defaultBadges, RecipeProductSurfaceCapabilityBadgeKind.NotAutomated],
                "Readiness explanation does not override policy or create a live path.",
                "Review handoff/export preview metadata next.",
                ["Recipe execution blocked", "Workitem processing blocked"],
                "Readiness copy must preserve the fixture-safe claim only."),
            DemoStep(
                RecipeProductSurfaceDemoFlowStepKind.HandoffExportPreview,
                "demo-flow.handoff-export-preview",
                "Review Handoff/Export Preview",
                "Inspect handoff metadata without file generation.",
                "The operator reviews readiness snapshot, blocked reasons, missing requirements, approval path, secret refs, evidence requirements, and not-included list.",
                [.. defaultBadges, RecipeProductSurfaceCapabilityBadgeKind.ExportPreviewOnly],
                "Operator preview cannot invoke disabled actions or grant live runtime.",
                "Confirm live runtime remains blocked.",
                ["Recipe execution blocked", "Export file generation blocked"],
                "Operator preview is a review aid only."),
            DemoStep(
                RecipeProductSurfaceDemoFlowStepKind.BlockedLiveRuntimeExplanation,
                "demo-flow.blocked-live-runtime",
                "Understand blocked live runtime",
                "Confirm what is not available.",
                "The operator verifies that live recipe execution, automation, connector/API, vault, recording/playback/capture-draft, workitem processing, external mutation, and real export remain unavailable.",
                [.. defaultBadges, RecipeProductSurfaceCapabilityBadgeKind.LiveRuntimeBlocked],
                "Handoff/export preview does not generate a real file and cannot call a filesystem, connector, or network path.",
                "Move to safe closing summary.",
                ["Export file generation blocked", "Connector/API blocked"],
                "Handoff/export is preview metadata only."),
            DemoStep(
                RecipeProductSurfaceDemoFlowStepKind.SafeClosingSummary,
                "demo-flow.safe-closing-summary",
                "Understand safe next action",
                "Close with the allowed product claim.",
                "The operator leaves with the fixture-safe product claim and the explicit understanding that live automation is not available.",
                [.. defaultBadges, RecipeProductSurfaceCapabilityBadgeKind.DemoSafe],
                "No step in the demo enables live runtime, automation, connector/API, vault, recording/playback/capture-draft, workitem processing, external mutation, or real export.",
                "Use the allowed claim and keep the forbidden claim out of product-facing copy.",
                taxonomy.DisabledActionMessages.Select(a => a.Label).ToArray(),
                taxonomy.AllowedFinalClaim)
        };

        var microcopy = new RecipeProductSurfaceDemoFlowCopySet(
            "Preview-only demo flow for the closed Recipe Product Surface. It explains inspection surfaces, blocked live runtime, and safe handoff metadata.",
            [
                "Next: inspect the read-only catalog.",
                "Next: review lab readiness without changing anything.",
                "Next: open template detail for system-specific explanation.",
                "Next: read why requirements are missing or blocked.",
                "Next: review operator-facing evidence and approval paths.",
                "Next: inspect handoff/export preview metadata only.",
                "Next: confirm live runtime remains blocked.",
                "Close: use the allowed fixture-safe product claim."
            ],
            [
                new("empty.no-live-runtime", "No live runtime available", "Live runtime is not enabled in this read-only product surface.", "Review readiness and blocked-state copy."),
                new("empty.no-connector", "No connector connected", "Connector/API calls are not enabled; connector refs remain preview metadata.", "Review connector eligibility refs."),
                new("empty.no-credentials", "No credentials requested", "No credentials are read or requested; secrets stay by reference only.", "Review secret aliases or refs by reference only."),
                new("empty.no-export-file", "No export file generated", "Export preview does not write a real file.", "Review the safe handoff summary."),
                new("empty.no-workitems", "No workitems processed", "Automatic workitem processing is not enabled.", "Review workitem metadata only."),
                new("empty.no-browser-desktop-automation", "No browser or desktop automation performed", "Browser and desktop automation are not enabled.", "Use preview-only explanations."),
                new("empty.preview-data-only", "Preview data only", "Fixture-safe summaries and refs are shown without live system access.", "Continue the read-only demo flow.")
            ],
            [
                "Disabled control: recipe execution is not enabled.",
                "Disabled control: connector/API calls are not enabled.",
                "Disabled control: secret reading is not enabled.",
                "Disabled control: browser and desktop automation are not enabled.",
                "Disabled control: recording/playback/capture-draft is not enabled.",
                "Disabled control: real export file generation is not enabled.",
                "Disabled control: automatic workitem processing is not enabled."
            ],
            "No live runtime available. Recipe execution and live automation are not enabled.",
            "Export preview only. No real export file is generated.",
            "No credentials are read. Secret refs are aliases only.",
            "No connector/API calls are made. Connector execution remains disabled.",
            "No browser or desktop automation is performed.",
            "No recording, playback, or real capture is performed.",
            "No automatic workitem processing is performed.",
            taxonomy.AllowedFinalClaim);

        return new(
            "recipe.product.surface.demo.flow.copy.v1",
            taxonomy,
            steps,
            microcopy,
            taxonomy.AllowedFinalClaim,
            taxonomy.ForbiddenFinalClaim);
    }

    public static RecipeProductSurfaceNavigationMessagingTaxonomy CreateNavigationMessagingTaxonomy()
    {
        var labels = new RecipeProductSurfaceNavigationLabel[]
        {
            new(
                RecipeProductSurfaceNavigationEntryKind.RecipeCatalog,
                "Recipe Catalog",
                "Browse fixture-safe template cards and status badges.",
                "recipes/catalog-preview",
                RecipeProductSurfaceCapabilityBadgeKind.ReadOnly),
            new(
                RecipeProductSurfaceNavigationEntryKind.RecipeLab,
                "Recipe Lab",
                "Inspect readiness, evidence refs, tool refs, trigger refs, and blocked live modes.",
                "recipes/lab-preview",
                RecipeProductSurfaceCapabilityBadgeKind.PreviewSafe),
            new(
                RecipeProductSurfaceNavigationEntryKind.TemplateDetail,
                "Template Detail",
                "Review one template, its system boundary, missing requirements, and safe next action.",
                "recipes/template-detail-preview",
                RecipeProductSurfaceCapabilityBadgeKind.FixtureSafe),
            new(
                RecipeProductSurfaceNavigationEntryKind.ReadinessExplanation,
                "Readiness Explanation",
                "Explain why a template is previewable, fixture-ready, blocked, or future-gated.",
                "recipes/readiness-explanation",
                RecipeProductSurfaceCapabilityBadgeKind.LiveRuntimeBlocked),
            new(
                RecipeProductSurfaceNavigationEntryKind.OperatorPreview,
                "Operator Preview",
                "Show what an operator would review before any future governed runtime design.",
                "recipes/operator-preview",
                RecipeProductSurfaceCapabilityBadgeKind.HumanApprovalPathRequired),
            new(
                RecipeProductSurfaceNavigationEntryKind.HandoffExportPreview,
                "Handoff/Export Preview",
                "Review handoff metadata only; no real file is generated.",
                "recipes/handoff-export-preview",
                RecipeProductSurfaceCapabilityBadgeKind.ExportPreviewOnly),
            new(
                RecipeProductSurfaceNavigationEntryKind.SafeDemo,
                "Safe Demo",
                "Walk through the read-only product surface and blocked live boundaries.",
                "recipes/safe-demo",
                RecipeProductSurfaceCapabilityBadgeKind.DemoSafe)
        };

        var badges = new RecipeProductSurfaceCapabilityStatusBadge[]
        {
            new(RecipeProductSurfaceCapabilityBadgeKind.ReadOnly, "Read-only", "Inspection surface only; no product action is exposed."),
            new(RecipeProductSurfaceCapabilityBadgeKind.PreviewSafe, "Preview-safe", "Product copy and view models remain preview-only."),
            new(RecipeProductSurfaceCapabilityBadgeKind.FixtureSafe, "Fixture-safe", "Uses contracts, fixtures, refs, and summaries only."),
            new(RecipeProductSurfaceCapabilityBadgeKind.DemoSafe, "Demo-safe", "Safe for explaining the closed surface without live behavior."),
            new(RecipeProductSurfaceCapabilityBadgeKind.LiveRuntimeBlocked, "Live runtime blocked", "No live runtime is available from navigation or messaging."),
            new(RecipeProductSurfaceCapabilityBadgeKind.ConnectorExecutionDisabled, "Connector execution disabled", "Connector/API/network activity is not enabled."),
            new(RecipeProductSurfaceCapabilityBadgeKind.SecretsByReferenceOnly, "Secrets by reference only", "Secret values are never requested or shown."),
            new(RecipeProductSurfaceCapabilityBadgeKind.ExportPreviewOnly, "Export preview only", "Handoff/export remains metadata preview; no real file is generated."),
            new(RecipeProductSurfaceCapabilityBadgeKind.HumanApprovalPathRequired, "Human approval path required", "Sensitive templates remain review-gated and blocked for live mutation."),
            new(RecipeProductSurfaceCapabilityBadgeKind.NotAutomated, "Not automated", "Browser, desktop, connector, vault, recorder, and external mutation paths are blocked.")
        };

        var disabled = new RecipeProductSurfaceDisabledActionMessage[]
        {
            Disabled(RecipeProductSurfaceDisabledActionKind.RecipeExecution, "Recipe execution blocked", "Recipe execution is not enabled in this read-only product surface.", "Review readiness and prepare requirements."),
            Disabled(RecipeProductSurfaceDisabledActionKind.WorkitemProcessing, "Workitem processing blocked", "Automatic workitem processing is not enabled in this closed product surface.", "Review queue metadata and handoff notes."),
            Disabled(RecipeProductSurfaceDisabledActionKind.ConnectorApi, "Connector/API blocked", "Connector/API/network calls are not enabled.", "Review connector eligibility and tool trust refs."),
            Disabled(RecipeProductSurfaceDisabledActionKind.VaultSecrets, "Vault/secrets blocked", "Vault access and secret reading are not enabled; secrets remain by reference only.", "Review required secret aliases or refs by reference only."),
            Disabled(RecipeProductSurfaceDisabledActionKind.BrowserAutomation, "Browser automation blocked", "Browser automation and CDP-driven runtime paths are not enabled.", "Use preview summaries and blocked runtime explanations."),
            Disabled(RecipeProductSurfaceDisabledActionKind.DesktopAutomation, "Desktop automation blocked", "Desktop/computer-use automation is not enabled.", "Use manual playbook and preview-only summaries."),
            Disabled(RecipeProductSurfaceDisabledActionKind.RecorderReplayCapture, "Recording/playback/capture-draft blocked", "Recording, playback, and real capture are not enabled.", "Review preview-only capture draft summaries."),
            Disabled(RecipeProductSurfaceDisabledActionKind.ExportFileGeneration, "Export file generation blocked", "Real export file generation is not enabled; handoff/export is preview metadata only.", "Review or copy the safe handoff summary text."),
            Disabled(RecipeProductSurfaceDisabledActionKind.FiscalPaymentMarketplaceMessageDeleteWrite, "Fiscal/payment/marketplace/message/delete/write blocked", "Live fiscal, payment, marketplace, message, delete, and write actions are not enabled.", "Request human review path and keep the item blocked for live action.")
        };

        return new(
            "recipe.product.surface.navigation.messaging.v1",
            "NODAL_RECIPE_PRODUCT_SURFACE_NAVIGATION_MESSAGING_READ_ONLY",
            "COMPLETE_READ_ONLY_PREVIEW_SAFE_FIXTURE_SAFE_PRODUCT_SURFACE_CLOSED",
            "df92f6fb4c86f246e1d956ede9fd4876e1d0080d",
            labels,
            badges,
            disabled,
            "NODAL OS has a fixture-safe Recipe Runtime product surface with read-only catalog, lab, templates, readiness explanations, operator previews and handoff/export preview summaries.",
            "NODAL OS can execute/live automate these recipes.",
            "Use read-only, preview-safe, fixture-safe copy only; live runtime, live automation, connector/API, vault, recording/playback/capture-draft, external mutation, and real export claims stay blocked.");
    }

    public static RecipeCatalogSurface CreateCatalogSurface(
        RecipeTemplateCatalog catalog,
        RecipeTemplateReadinessContext readinessContext,
        RecipeCatalogFilterState? filterState = null)
    {
        var packs = catalog.Packs
            .Select(pack => CreatePackViewModel(pack, readinessContext))
            .ToArray();

        var viewModel = new RecipeCatalogViewModel(
            catalog.CatalogId,
            catalog.Version,
            catalog.Templates.Count,
            packs,
            filterState ?? DefaultFilterState(),
            "Preview / Fixture-safe / No live execution.",
            GlobalSafetyBadges());

        return new(
            "recipe.catalog.surface.v1",
            viewModel,
            CategoryLabels(),
            [
                "Preview",
                "Fixture-safe",
                "Read-only",
                "Live runtime blocked",
                "Connector execution not enabled",
                "Browser automation not enabled",
                "Desktop automation not enabled",
                "Secrets by reference only",
                "Evidence by reference only"
            ]);
    }

    public static RecipeLabSurface CreateLabSurface(
        RecipeLabSnapshot snapshot,
        RecipeTemplateReadiness? templateReadiness = null,
        RecipeDraftTemplateMapping? templateMapping = null)
    {
        var blockedReasons = snapshot.Readiness.BlockingIssues
            .Select(i => new RecipeLabBlockedReasonViewModel(i.IssueId, i.Status, i.RedactedSummary, i.Severity))
            .ToArray();

        var cells = snapshot.ViewModel.Sections
            .Select((section, index) => new RecipeLabNotebookCellViewModel(
                $"cell.{index + 1}.{section.SectionId}",
                ToCellKind(section.SectionId),
                ToCellStatus(section.Status),
                section.Label,
                section.RedactedSummary,
                section.SourceRefs))
            .ToArray();

        if (templateMapping is not null)
        {
            cells = cells
                .Append(new RecipeLabNotebookCellViewModel(
                    "cell.template.mapping",
                    RecipeLabCellKind.Overview,
                    templateMapping.TemplateReadiness?.IsReady == true ? RecipeLabCellStatus.FixtureOnly : RecipeLabCellStatus.LiveBlocked,
                    "Template mapping",
                    templateMapping.RedactedSummary,
                    templateMapping.TemplateId is null ? [] : [templateMapping.TemplateId]))
                .ToArray();
        }

        var viewModel = new RecipeLabReadOnlyViewModel(
            "recipe.lab.readonly.v1",
            snapshot.RecipeId,
            snapshot.RecipeVersion,
            snapshot.DisplayName,
            snapshot.Readiness,
            snapshot.ViewModel.Sections.Select(s => new RecipeLabSectionViewModel(s.SectionId, s.Label, s.Status, s.RedactedSummary, s.SourceRefs)).ToArray(),
            cells,
            blockedReasons,
            new RecipeLabSafeNextActionViewModel(snapshot.SafeNextAction.Kind, snapshot.SafeNextAction.Summary, snapshot.SafeNextAction.AllowsLiveRuntime, snapshot.SafeNextAction.AllowsExternalMutation),
            snapshot.EvidenceCompletenessSummary + " / " + snapshot.TimelineProjectionSummary,
            snapshot.ApprovalHumanInterventionSummary,
            BuildCapabilitySummary(snapshot.CapabilitySummary),
            snapshot.TriggerObserveOnlySummary,
            snapshot.LocatorRepairSummary,
            templateMapping is null ? "Draft-only capture summaries remain review-only." : "Draft-to-template mapping remains governed by composite readiness.",
            templateReadiness?.BlockedRunModes ?? [RecipeRunMode.LiveRunBlocked],
            snapshot.RedactionSafetySummary);

        return new(
            "recipe.lab.surface.v1",
            viewModel,
            [
                "Read-only",
                "Preview",
                "Fixture-safe",
                "Evidence by reference only",
                "Secrets by reference only",
                "Observe-only trigger",
                "Live runtime blocked"
            ]);
    }

    public static RecipeTemplateDetailSurface CreateTemplateDetailSurface(
        RecipeTemplateCatalog catalog,
        string templateId,
        RecipeTemplateReadinessContext readinessContext)
    {
        var pack = catalog.Packs.Single(p => p.Templates.Any(t => t.TemplateId == templateId));
        var template = pack.Templates.Single(t => t.TemplateId == templateId);
        var readiness = RecipeTemplateReadinessEvaluator.Evaluate(template, readinessContext);
        var systemSummary = SystemSummary(template);
        var explanation = ReadinessExplanation(template, readiness);

        var viewModel = new RecipeTemplateDetailViewModel(
            $"recipe.template.detail.{template.TemplateId}",
            new RecipeTemplateDetailHeader(
                template.TemplateId,
                SafeProductCopy(template.DisplayName),
                SafeProductCopy(template.Description),
                pack.DisplayName,
                template.System,
                template.Category,
                template.Region,
                template.Countries,
                SafeProductCopy(template.OperatorVisibleSummary)),
            new RecipeTemplateDetailPackSummary(pack.PackId, pack.DisplayName, pack.Category, pack.Region),
            systemSummary,
            new RecipeTemplateDetailSafetySummary(
                template.SafetyProfile.RiskLevel,
                template.SafetyProfile.SensitiveCategories,
                template.SafetyProfile.RequiresHumanApproval || template.ApprovalHumanInterventionRequirementRefs.Count > 0 || template.SafetyProfile.SensitiveCategories.Count > 0,
                LiveBlockedExplanation(template),
                SafeProductCopy(template.NotIncludedOrNotAutomatedSummary),
                DetailSafetyBadges(template)),
            new RecipeTemplateDetailRequirementSummary(
                template.RequiredCapabilities,
                template.RequiredToolTrustRefs,
                template.RequiredSecretRefs,
                template.ConnectorEligibilityRefs,
                template.TriggerRefs,
                template.EvidenceRequirementRefs,
                template.ValidationRequirementRefs,
                template.ApprovalHumanInterventionRequirementRefs),
            explanation,
            DetailSections(template, readiness, systemSummary, explanation),
            TriggerSummary(template),
            $"Evidence refs: {RefSummary(template.EvidenceRequirementRefs)}. Validation refs: {RefSummary(template.ValidationRequirementRefs)}.",
            LocatorCaptureSummary(template),
            SafeProductCopy(template.OperatorVisibleSummary));

        return new(
            $"recipe.template.detail.surface.{template.TemplateId}",
            viewModel,
            [
                "Preview",
                "Fixture-safe",
                "Read-only",
                "Template detail",
                "Live runtime blocked",
                "Connector execution not enabled",
                "Browser automation not enabled",
                "Desktop automation not enabled",
                "Secrets by reference only",
                "Evidence by reference only",
                "Observe-only trigger",
                "Not included",
                "Future-gated"
            ]);
    }

    public static RecipeOperatorPreviewHandoffExportSurface CreateOperatorPreviewHandoffExportSurface(
        RecipeTemplateCatalog catalog,
        string templateId,
        RecipeTemplateReadinessContext readinessContext,
        IReadOnlyList<string>? operatorNotes = null)
    {
        var detail = CreateTemplateDetailSurface(catalog, templateId, readinessContext);
        var view = detail.ViewModel;
        var notes = (operatorNotes ?? []).Select(SafeProductCopy).ToArray();

        var operatorPreview = new RecipeOperatorPreviewViewModel(
            $"recipe.operator.preview.{templateId}",
            view.Header,
            view.ReadinessExplanation.ReadinessStatus,
            view.ReadinessExplanation.CanonicalReadinessStatus,
            $"Operator preview for {view.Header.DisplayName}: review readiness, requirements, blocked states, and handoff metadata only.",
            OperatorReviewSections(view),
            view.SystemSummary.HumanReviewSummary,
            view.EvidenceValidationSummary,
            HumanInterventionPoints(view),
            view.SafetySummary.LiveBlockedExplanation,
            DisabledActionStates(view),
            view.ReadinessExplanation.SafeNextAction,
            view.SafetySummary.NotIncludedSummary,
            OperatorSystemPreviewSummary(view));

        var handoffPreview = new RecipeHandoffExportPreviewViewModel(
            $"recipe.handoff.export.preview.{templateId}",
            $"Handoff preview - {view.Header.DisplayName}",
            $"{view.Header.TemplateId} / {view.Header.SystemFamily} / {view.Header.Region} / {view.Header.Category}",
            $"{view.ReadinessExplanation.ReadinessStatus}; canonical {view.ReadinessExplanation.CanonicalReadinessStatus}; preview-only.",
            view.ReadinessExplanation.BlockingReasons,
            view.ReadinessExplanation.MissingRequirements,
            view.SystemSummary.HumanReviewSummary,
            ToolTrustSummary(view.Requirements.RequiredToolTrustRefs),
            SecretRefsSummary(view.Requirements.RequiredSecretRefs),
            view.Requirements.EvidenceRequirementRefs,
            view.Requirements.ValidationRequirementRefs,
            view.LocatorCaptureImplicationsSummary,
            view.TriggerObserveOnlySummary,
            notes,
            NotIncludedList(view),
            RecipeHandoffExportAvailability.PreviewOnly,
            "Export preview only. Handoff package not generated as a real file. No live execution.");

        return new(
            $"recipe.operator.preview.handoff.surface.{templateId}",
            operatorPreview,
            handoffPreview,
            [
                "Preview only",
                "No live execution",
                "Export preview only",
                "Handoff package not generated as a real file",
                "Operator review required",
                "Live runtime blocked",
                "Automation not enabled",
                "Safe next action: review readiness and prepare requirements"
            ]);
    }

    public static RecipeProductSurfaceSafeDemoReadinessSurface CreateSafeDemoReadinessSurface(
        RecipeTemplateCatalog catalog,
        RecipeTemplateReadinessContext readinessContext,
        RecipeLabSnapshot labSnapshot,
        string templateId = "excel.extract_rows_to_workitems")
    {
        var catalogSurface = CreateCatalogSurface(catalog, readinessContext);
        var labSurface = CreateLabSurface(labSnapshot);
        var detailSurface = CreateTemplateDetailSurface(catalog, templateId, readinessContext);
        var operatorSurface = CreateOperatorPreviewHandoffExportSurface(catalog, templateId, readinessContext);

        var capabilityMatrix = new RecipeProductSurfaceCapabilityMatrix(
            [
                "read-only catalog",
                "read-only lab",
                "template detail",
                "readiness explanation",
                "operator preview",
                "handoff/export preview metadata",
                "safe product/demo copy"
            ],
            [
                "live execution",
                "browser automation",
                "desktop automation",
                "live browser driver frameworks",
                "connector/API/network",
                "vault/secrets",
                "scheduler/watcher/hook/listener",
                "recorder/playback/capture",
                "automatic workitem processing",
                "fiscal/payment/marketplace/message/delete/write actions",
                "real export file generation"
            ]);

        var finalComposition = new RecipeProductSurfaceFinalCompositionViewModel(
            "recipe.product.surface.final.composition.v1",
            RecipeProductSurfaceDemoReadinessStatus.ReadOnlyDemoReady,
            $"{catalogSurface.ViewModel.TotalTemplates} templates are visible in read-only catalog preview.",
            $"Lab summary uses {labSurface.ViewModel.ReadinessSummary.CanonicalEvaluatorName} and remains read-only.",
            detailSurface.ViewModel.ReadinessExplanation.OperatorVisibleSummary,
            operatorSurface.OperatorPreview.OperatorReviewSummary,
            operatorSurface.HandoffExportPreview.ProductSafeCopy,
            "No live runtime. Recipe execution is not enabled.",
            "Unavailable actions are explicit and cannot be invoked.",
            "Safe next action: review readiness and prepare requirements.",
            "Not automated: live runtime, browser/desktop automation, connectors, vault access, recorder/playback/capture, real export, and external mutations.",
            "NODAL OS has a fixture-safe Recipe Runtime product surface with read-only catalog, lab, templates, readiness explanations, operator previews and handoff/export preview summaries.",
            "Blocked product claim: live recipe automation is not available from this surface.",
            capabilityMatrix);

        var demoScenario = new RecipeProductSurfaceSafeDemoScenario(
            "recipe.product.surface.safe.demo.v1",
            "Recipe Runtime Product Surface safe demo",
            SafeDemoSteps(detailSurface, operatorSurface),
            finalComposition.InternalAllowedClaimSummary,
            finalComposition.BlockedForbiddenClaimSummary,
            "Safe demo readiness is true only for read-only preview surfaces and does not authorize live automation.");

        return new(
            "recipe.product.surface.safe.demo.readiness.v1",
            finalComposition,
            demoScenario,
            [
                "Preview only",
                "Read-only product surface",
                "Fixture-safe",
                "No live runtime",
                "Recipe execution is not enabled",
                "Automation is not available in this build",
                "Handoff/export is preview-only",
                "No credentials are read",
                "No connector/API calls are made",
                "No browser or desktop automation is performed",
                "Safe next action: review readiness and prepare requirements"
            ]);
    }

    private static RecipeCatalogPackViewModel CreatePackViewModel(
        RecipeTemplatePack pack,
        RecipeTemplateReadinessContext readinessContext)
    {
        var cards = pack.Templates
            .Select(template =>
            {
                var readiness = RecipeTemplateReadinessEvaluator.Evaluate(template, readinessContext);
                return CreateTemplateCard(pack.DisplayName, template, readiness);
            })
            .ToArray();

        return new(
            pack.PackId,
            pack.DisplayName,
            pack.Category,
            pack.Region,
            pack.Templates.Count,
            cards.Count(c => c.TemplateStatus == RecipeTemplateStatus.FixtureReady || c.ReadinessBadge.Kind == RecipeCatalogReadinessBadgeKind.FixtureReady),
            cards.Count(c => c.RuntimeEligibility is RecipeTemplateRuntimeEligibility.LiveBlocked or RecipeTemplateRuntimeEligibility.FutureGated),
            pack.SafetySummary,
            cards);
    }

    private static RecipeTemplateCardViewModel CreateTemplateCard(
        string packName,
        RecipeTemplateDefinition template,
        RecipeTemplateReadiness readiness)
    {
        var badges = new List<RecipeCatalogSafetyBadge>
        {
            Badge(RecipeCatalogSafetyBadgeKind.Preview, "Preview", "Catalog inspection only."),
            Badge(RecipeCatalogSafetyBadgeKind.FixtureSafe, "Fixture-safe", "Uses fixture/reference contracts only."),
            Badge(RecipeCatalogSafetyBadgeKind.ReadOnly, "Read-only", "No product action is available from this card."),
            Badge(RecipeCatalogSafetyBadgeKind.SecretsByReference, "Secrets by reference only", SecretSummary(template)),
            Badge(RecipeCatalogSafetyBadgeKind.EvidenceByReference, "Evidence by reference only", "Evidence is represented by refs.")
        };

        if (template.RuntimeEligibility == RecipeTemplateRuntimeEligibility.LiveBlocked)
            badges.Add(Badge(RecipeCatalogSafetyBadgeKind.LiveBlocked, "Live runtime blocked", template.LiveRuntimeStatus));

        if (template.RuntimeEligibility == RecipeTemplateRuntimeEligibility.FutureGated)
            badges.Add(Badge(RecipeCatalogSafetyBadgeKind.FutureGated, "Future-gated", template.LiveRuntimeStatus));

        if (template.SafetyProfile.RequiresHumanApproval || template.ApprovalHumanInterventionRequirementRefs.Count > 0)
            badges.Add(Badge(RecipeCatalogSafetyBadgeKind.HumanReviewRequired, "Requires human review", "Sensitive or mutation-like template stays review-gated."));

        if (template.TriggerRefs.Count > 0)
            badges.Add(Badge(RecipeCatalogSafetyBadgeKind.ObserveOnlyTrigger, "Observe-only trigger", "Trigger refs cannot start recipes."));

        return new(
            template.TemplateId,
            SafeProductCopy(template.DisplayName),
            SafeProductCopy(template.Description),
            packName,
            template.System,
            template.Region,
            template.Countries,
            template.Category,
            template.RuntimeEligibility,
            readiness.Status,
            ReadinessBadge(readiness),
            template.SafetyProfile.RiskLevel,
            template.SafetyProfile.RequiresHumanApproval || template.SafetyProfile.SensitiveCategories.Count > 0,
            ToolTrustSummary(template),
            SecretSummary(template),
            TriggerSummary(template),
            SafeProductCopy(template.LiveRuntimeStatus),
            SafeProductCopy(template.SafeNextAction.Summary),
            SafeProductCopy(template.NotIncludedOrNotAutomatedSummary),
            badges,
            readiness.BlockingIssues.Select(i => SafeProductCopy(i.Message)).ToArray());
    }

    private static RecipeTemplateReadinessExplanation ReadinessExplanation(
        RecipeTemplateDefinition template,
        RecipeTemplateReadiness readiness)
    {
        var blocking = readiness.BlockingIssues.Select(i => new RecipeTemplateBlockingReason(
            i.IssueId,
            ToBlockingCategory(i, template),
            i.Status,
            SafeProductCopy(i.Message),
            i.Severity)).ToArray();

        var warnings = readiness.Warnings.Select(i => new RecipeTemplateWarning(
            i.IssueId,
            ToBlockingCategory(i, template),
            SafeProductCopy(i.Message),
            i.Severity)).ToArray();

        var missing = MissingRequirements(template, blocking);
        var reasons = blocking
            .Select(b => new RecipeTemplateReadinessReason(b.ReasonId, b.RedactedSummary, IsBlocking: true, b.Severity))
            .Concat(warnings.Select(w => new RecipeTemplateReadinessReason(w.WarningId, w.RedactedSummary, IsBlocking: false, w.Severity)))
            .DefaultIfEmpty(new RecipeTemplateReadinessReason("template-previewable", "Template is available for read-only preview.", IsBlocking: false, RecipeReadinessIssueSeverity.Info))
            .ToArray();

        return new(
            IsPreviewable: true,
            IsFixtureReady: readiness.IsReady && template.RuntimeEligibility == RecipeTemplateRuntimeEligibility.FixtureOnly,
            readiness.Status,
            readiness.CanonicalReadinessStatus,
            SafeProductCopy(readiness.OperatorSummary),
            reasons,
            blocking,
            missing,
            warnings,
            FutureEnablementNotes(template, blocking),
            new RecipeTemplateSafeNextActionExplanation(
                readiness.SafeNextAction.Kind,
                SafeProductCopy(readiness.SafeNextAction.Summary),
                readiness.SafeNextAction.AllowsLiveRuntime,
                readiness.SafeNextAction.AllowsExternalMutation),
            readiness.BlockedRunModes,
            SafeProductCopy(template.NotIncludedOrNotAutomatedSummary));
    }

    private static RecipeTemplateBlockingReasonCategory ToBlockingCategory(RecipeReadinessIssue issue, RecipeTemplateDefinition template)
    {
        var id = issue.IssueId.ToLowerInvariant();
        var message = issue.Message.ToLowerInvariant();

        if (id.Contains("limit"))
            return RecipeTemplateBlockingReasonCategory.MissingLimits;
        if (id.Contains("validation") && issue.Status == RecipeReadinessStatus.BlockedMissingValidation)
            return RecipeTemplateBlockingReasonCategory.MissingValidation;
        if (id.Contains("evidence"))
            return RecipeTemplateBlockingReasonCategory.MissingEvidence;
        if (id.Contains("approval"))
            return RecipeTemplateBlockingReasonCategory.MissingApprovalPath;
        if (id.Contains("tool") && message.Contains("live-blocked"))
            return RecipeTemplateBlockingReasonCategory.ToolLiveBlocked;
        if (id.Contains("tool"))
            return RecipeTemplateBlockingReasonCategory.MissingToolTrust;
        if (id.Contains("secret") && message.Contains("raw"))
            return RecipeTemplateBlockingReasonCategory.RawSecretDetected;
        if (id.Contains("secret"))
            return RecipeTemplateBlockingReasonCategory.MissingSecretReference;
        if (id.Contains("connector"))
            return RecipeTemplateBlockingReasonCategory.ConnectorExecutionBlocked;
        if (id.Contains("trigger"))
            return RecipeTemplateBlockingReasonCategory.TriggerAutorunBlocked;
        if (id.Contains("challenge"))
            return RecipeTemplateBlockingReasonCategory.SensitiveActionRequiresReview;
        if (template.Category == RecipeTemplateCategory.GenericBrowserPortal)
            return RecipeTemplateBlockingReasonCategory.BrowserRuntimeBlocked;
        if (template.Category == RecipeTemplateCategory.ComputerUseLegacy)
            return RecipeTemplateBlockingReasonCategory.DesktopRuntimeBlocked;
        if (template.System is RecipeTemplateSystem.ARCA or RecipeTemplateSystem.Fiscal)
            return RecipeTemplateBlockingReasonCategory.FiscalSubmissionBlocked;
        if (template.System == RecipeTemplateSystem.MercadoPago || template.SafetyProfile.SensitiveCategories.Contains(SensitiveActionCategory.Payment))
            return RecipeTemplateBlockingReasonCategory.PaymentExecutionBlocked;
        if (template.Category == RecipeTemplateCategory.MercadoLibreMercadoPago)
            return RecipeTemplateBlockingReasonCategory.MarketplaceMutationBlocked;
        if (issue.Status == RecipeReadinessStatus.BlockedByProtectedScope)
            return RecipeTemplateBlockingReasonCategory.UnknownSystemBlocked;

        return RecipeTemplateBlockingReasonCategory.UnknownUnsafe;
    }

    private static IReadOnlyList<RecipeTemplateMissingRequirement> MissingRequirements(
        RecipeTemplateDefinition template,
        IReadOnlyList<RecipeTemplateBlockingReason> blocking)
    {
        var missing = blocking
            .Where(b => b.Category is RecipeTemplateBlockingReasonCategory.MissingLimits
                or RecipeTemplateBlockingReasonCategory.MissingValidation
                or RecipeTemplateBlockingReasonCategory.MissingEvidence
                or RecipeTemplateBlockingReasonCategory.MissingApprovalPath
                or RecipeTemplateBlockingReasonCategory.MissingHumanInterventionPath
                or RecipeTemplateBlockingReasonCategory.MissingToolTrust
                or RecipeTemplateBlockingReasonCategory.MissingSecretReference)
            .Select(b => new RecipeTemplateMissingRequirement(b.ReasonId, b.Category, b.RedactedSummary, b.Severity))
            .ToList();

        if (template.RequiredToolTrustRefs.Count > 0 && blocking.Any(b => b.Category is RecipeTemplateBlockingReasonCategory.ToolLiveBlocked or RecipeTemplateBlockingReasonCategory.MissingToolTrust))
            missing.Add(new("template-tool-trust-summary", RecipeTemplateBlockingReasonCategory.MissingToolTrust, $"Tool trust refs remain unresolved or blocked: {string.Join(", ", template.RequiredToolTrustRefs)}.", RecipeReadinessIssueSeverity.Blocking));

        if (template.RequiredSecretRefs.Count > 0 && blocking.Any(b => b.Category is RecipeTemplateBlockingReasonCategory.MissingSecretReference or RecipeTemplateBlockingReasonCategory.RawSecretDetected))
            missing.Add(new("template-secret-ref-summary", RecipeTemplateBlockingReasonCategory.MissingSecretReference, $"Secret refs by alias/id only: {string.Join(", ", template.RequiredSecretRefs)}.", RecipeReadinessIssueSeverity.Blocking));

        return missing;
    }

    private static IReadOnlyList<RecipeTemplateFutureEnablementNote> FutureEnablementNotes(
        RecipeTemplateDefinition template,
        IReadOnlyList<RecipeTemplateBlockingReason> blocking)
    {
        var notes = new List<RecipeTemplateFutureEnablementNote>();

        if (template.RuntimeEligibility is RecipeTemplateRuntimeEligibility.FutureGated or RecipeTemplateRuntimeEligibility.LiveBlocked)
            notes.Add(new("future-runtime-not-enabled", RuntimeCategory(template), LiveBlockedExplanation(template)));

        if (template.ConnectorEligibilityRefs.Count > 0)
            notes.Add(new("future-connector-not-enabled", RecipeTemplateBlockingReasonCategory.ConnectorExecutionBlocked, "Connector eligibility is reference/fixture-only and does not enable connector execution."));

        if (template.TriggerRefs.Count > 0)
            notes.Add(new("future-trigger-not-enabled", RecipeTemplateBlockingReasonCategory.TriggerAutorunBlocked, "Trigger refs remain observe-only and cannot start a recipe."));

        if (blocking.Any(b => b.Category == RecipeTemplateBlockingReasonCategory.RawSecretDetected))
            notes.Add(new("raw-secret-blocks", RecipeTemplateBlockingReasonCategory.RawSecretDetected, "Raw secret markers block readiness; only secret refs are allowed."));

        return notes;
    }

    private static RecipeTemplateBlockingReasonCategory RuntimeCategory(RecipeTemplateDefinition template) =>
        template.Category switch
        {
            RecipeTemplateCategory.GenericBrowserPortal => RecipeTemplateBlockingReasonCategory.BrowserRuntimeBlocked,
            RecipeTemplateCategory.ComputerUseLegacy => RecipeTemplateBlockingReasonCategory.DesktopRuntimeBlocked,
            RecipeTemplateCategory.ARCAFiscal => RecipeTemplateBlockingReasonCategory.FiscalSubmissionBlocked,
            RecipeTemplateCategory.MercadoLibreMercadoPago when template.System == RecipeTemplateSystem.MercadoPago => RecipeTemplateBlockingReasonCategory.PaymentExecutionBlocked,
            RecipeTemplateCategory.MercadoLibreMercadoPago => RecipeTemplateBlockingReasonCategory.MarketplaceMutationBlocked,
            _ => RecipeTemplateBlockingReasonCategory.ConnectorExecutionBlocked
        };

    private static IReadOnlyList<RecipeTemplateDetailSection> DetailSections(
        RecipeTemplateDefinition template,
        RecipeTemplateReadiness readiness,
        RecipeTemplateDetailSystemSummary systemSummary,
        RecipeTemplateReadinessExplanation explanation) =>
    [
        new("overview", "Overview", RecipeLabSectionStatus.ReferenceOnly, SafeProductCopy(template.OperatorVisibleSummary), [template.TemplateId]),
        new("readiness", "Readiness", readiness.IsReady ? RecipeLabSectionStatus.FixtureOnly : RecipeLabSectionStatus.Blocked, explanation.OperatorVisibleSummary, [readiness.CanonicalReadinessStatus.ToString()]),
        new("blocking", "Blocking and missing requirements", explanation.BlockingReasons.Count > 0 ? RecipeLabSectionStatus.Blocked : RecipeLabSectionStatus.Ready, string.Join("; ", explanation.BlockingReasons.Select(b => b.RedactedSummary).DefaultIfEmpty("No blocking issue for preview.")), explanation.BlockingReasons.Select(b => b.ReasonId).ToArray()),
        new("requirements", "Requirements", RecipeLabSectionStatus.ReferenceOnly, $"Tools: {RefSummary(template.RequiredToolTrustRefs)}. Secrets: {RefSummary(template.RequiredSecretRefs)}. Evidence: {RefSummary(template.EvidenceRequirementRefs)}.", template.RequiredToolTrustRefs.Concat(template.RequiredSecretRefs).Concat(template.EvidenceRequirementRefs).ToArray()),
        new("human-review", "Human review", template.SafetyProfile.RequiresHumanApproval || template.SafetyProfile.SensitiveCategories.Count > 0 ? RecipeLabSectionStatus.NeedsHuman : RecipeLabSectionStatus.ReferenceOnly, HumanReviewSummary(template), template.ApprovalHumanInterventionRequirementRefs),
        new("system-boundary", "System boundary", template.RuntimeEligibility is RecipeTemplateRuntimeEligibility.LiveBlocked ? RecipeLabSectionStatus.LiveBlocked : RecipeLabSectionStatus.FutureGated, systemSummary.RuntimeBoundarySummary, template.ConnectorEligibilityRefs),
        new("safe-next", "Safe next action", RecipeLabSectionStatus.ReferenceOnly, explanation.SafeNextAction.RedactedSummary, [])
    ];

    private static IReadOnlyList<RecipeOperatorPreviewReviewSection> OperatorReviewSections(RecipeTemplateDetailViewModel view) =>
    [
        new(RecipeOperatorPreviewSectionKind.Overview, "Overview", RecipeLabSectionStatus.ReferenceOnly, view.OperatorVisibleSummary, [view.Header.TemplateId]),
        new(RecipeOperatorPreviewSectionKind.Readiness, "Readiness", view.ReadinessExplanation.IsFixtureReady ? RecipeLabSectionStatus.FixtureOnly : RecipeLabSectionStatus.Blocked, view.ReadinessExplanation.OperatorVisibleSummary, [view.ReadinessExplanation.CanonicalReadinessStatus.ToString()]),
        new(RecipeOperatorPreviewSectionKind.BlockingReasons, "Blocking reasons", view.ReadinessExplanation.BlockingReasons.Count > 0 ? RecipeLabSectionStatus.Blocked : RecipeLabSectionStatus.Ready, JoinOrDefault(view.ReadinessExplanation.BlockingReasons.Select(r => r.RedactedSummary), "No blocking reason for preview."), view.ReadinessExplanation.BlockingReasons.Select(r => r.ReasonId).ToArray()),
        new(RecipeOperatorPreviewSectionKind.MissingRequirements, "Missing requirements", view.ReadinessExplanation.MissingRequirements.Count > 0 ? RecipeLabSectionStatus.Warning : RecipeLabSectionStatus.Ready, JoinOrDefault(view.ReadinessExplanation.MissingRequirements.Select(r => r.RedactedSummary), "No missing requirement for preview."), view.ReadinessExplanation.MissingRequirements.Select(r => r.RequirementId).ToArray()),
        new(RecipeOperatorPreviewSectionKind.ApprovalPath, "Approval path", view.SafetySummary.RequiresHumanReview ? RecipeLabSectionStatus.NeedsHuman : RecipeLabSectionStatus.ReferenceOnly, view.SystemSummary.HumanReviewSummary, view.Requirements.ApprovalHumanInterventionRequirementRefs),
        new(RecipeOperatorPreviewSectionKind.EvidenceValidation, "Evidence and validation", RecipeLabSectionStatus.ReferenceOnly, view.EvidenceValidationSummary, view.Requirements.EvidenceRequirementRefs.Concat(view.Requirements.ValidationRequirementRefs).ToArray()),
        new(RecipeOperatorPreviewSectionKind.ToolTrustSecrets, "Tool trust and secret refs", RecipeLabSectionStatus.ReferenceOnly, $"{ToolTrustSummary(view.Requirements.RequiredToolTrustRefs)} {SecretRefsSummary(view.Requirements.RequiredSecretRefs)}", view.Requirements.RequiredToolTrustRefs.Concat(view.Requirements.RequiredSecretRefs).ToArray()),
        new(RecipeOperatorPreviewSectionKind.TriggerObserveOnly, "Observe-only trigger", RecipeLabSectionStatus.ReferenceOnly, view.TriggerObserveOnlySummary, view.Requirements.TriggerRefs),
        new(RecipeOperatorPreviewSectionKind.LocatorCapturePreview, "Locator and capture preview", RecipeLabSectionStatus.ReferenceOnly, view.LocatorCaptureImplicationsSummary, []),
        new(RecipeOperatorPreviewSectionKind.SafeNextAction, "Safe next action", RecipeLabSectionStatus.ReferenceOnly, view.ReadinessExplanation.SafeNextAction.RedactedSummary, []),
        new(RecipeOperatorPreviewSectionKind.NotAutomated, "Not automated", RecipeLabSectionStatus.LiveBlocked, view.SafetySummary.NotIncludedSummary, [])
    ];

    private static IReadOnlyList<RecipeProductSurfaceSafeDemoStep> SafeDemoSteps(
        RecipeTemplateDetailSurface detailSurface,
        RecipeOperatorPreviewHandoffExportSurface operatorSurface) =>
    [
        new(RecipeProductSurfaceDemoStepKind.BrowseCatalog, "Browse recipe catalog", "Browse read-only template categories and readiness badges."),
        new(RecipeProductSurfaceDemoStepKind.OpenRecipeLabSummary, "Open recipe lab summary", "Inspect lab readiness, evidence refs, tool refs, trigger refs, and blocked live modes."),
        new(RecipeProductSurfaceDemoStepKind.OpenTemplateDetail, "Open template detail", detailSurface.ViewModel.OperatorVisibleSummary),
        new(RecipeProductSurfaceDemoStepKind.ReviewReadinessExplanation, "Review readiness explanation", detailSurface.ViewModel.ReadinessExplanation.OperatorVisibleSummary),
        new(RecipeProductSurfaceDemoStepKind.ReviewOperatorPreview, "Review operator preview", operatorSurface.OperatorPreview.OperatorReviewSummary),
        new(RecipeProductSurfaceDemoStepKind.ReviewHandoffExportPreview, "Review handoff/export preview", operatorSurface.HandoffExportPreview.ProductSafeCopy),
        new(RecipeProductSurfaceDemoStepKind.UnderstandBlockedLiveRuntime, "Understand blocked live runtime", detailSurface.ViewModel.SafetySummary.LiveBlockedExplanation),
        new(RecipeProductSurfaceDemoStepKind.ReadSafeHandoffSummary, "Read safe handoff summary", "Handoff/export is preview-only metadata; no real file is written.")
    ];

    private static IReadOnlyList<RecipeOperatorPreviewDisabledActionState> DisabledActionStates(RecipeTemplateDetailViewModel view) =>
    [
        new("recipe-start", "Recipe start unavailable", "Preview surface cannot start recipes."),
        new("workitem-processing", "Workitem processing unavailable", "Preview surface cannot process workitems."),
        new("connector-activation", "Connector activation unavailable", "Connector execution not enabled."),
        new("secret-value-access", "Secret value access unavailable", "Secrets are shown by reference only."),
        new("browser-runtime", "Browser runtime unavailable", "Browser automation not enabled."),
        new("desktop-runtime", "Desktop runtime unavailable", "Desktop automation not enabled."),
        new("file-output", "Real file output unavailable", "Export preview does not write files."),
        new("live-mutation", "Live mutation unavailable", view.SafetySummary.LiveBlockedExplanation)
    ];

    private static IReadOnlyList<string> HumanInterventionPoints(RecipeTemplateDetailViewModel view)
    {
        var points = new List<string>();
        if (view.SafetySummary.RequiresHumanReview)
            points.Add(view.SystemSummary.HumanReviewSummary);
        if (view.ReadinessExplanation.BlockingReasons.Count > 0)
            points.Add("Operator must review blocked readiness before future runtime planning.");
        if (view.Requirements.RequiredSecretRefs.Count > 0)
            points.Add("Secret refs must remain alias/id only; no values are requested.");
        return points.Count == 0 ? ["Operator review confirms preview-only readiness."] : points.Select(SafeProductCopy).ToArray();
    }

    private static string OperatorSystemPreviewSummary(RecipeTemplateDetailViewModel view) =>
        view.Header.Category switch
        {
            RecipeTemplateCategory.ExcelMicrosoft365 => "Previewable: workbook refs, validations, evidence expectations. Blocked: live connector and file sync. Operator reviews evidence requirements.",
            RecipeTemplateCategory.GoogleWorkspace => "Previewable: Workspace refs and review queues. Blocked: Google API calls and Gmail delivery. Operator reviews draft-only handoff.",
            RecipeTemplateCategory.SAP => "Previewable: SAP template intent and requirements. Blocked: SAP GUI, RFC, BAPI, OData, and connector execution. Operator reviews human-gated future path.",
            RecipeTemplateCategory.MercadoLibreMercadoPago => "Previewable: marketplace/payment review metadata. Blocked: API calls, stock, price, listing, message, and payment mutations. Operator approval path required.",
            RecipeTemplateCategory.ARCAFiscal => "Previewable: fiscal review metadata. Blocked: fiscal submission, web service calls, certificate/private-key use. Human/legal review required.",
            RecipeTemplateCategory.ERPLocalLATAM => "Previewable: ERP draft/review metadata. Blocked: ERP API, desktop automation, and mutation. Operator reviews local system fit.",
            RecipeTemplateCategory.GenericBrowserPortal => "Previewable: portal readiness/check/playbook metadata. Blocked: browser automation, real login, and challenge bypass.",
            RecipeTemplateCategory.ComputerUseLegacy => "Previewable: manual playbook and draft metadata. Blocked: desktop automation, UIA/vision, hotkey hooks, and live continuation.",
            _ => "Previewable: template metadata only. Blocked: unknown live path."
        };

    private static IReadOnlyList<string> NotIncludedList(RecipeTemplateDetailViewModel view)
    {
        var items = new List<string>
        {
            view.SafetySummary.NotIncludedSummary,
            view.SafetySummary.LiveBlockedExplanation,
            "No real handoff file is written.",
            "No raw secret values are included.",
            "No automatic workitem processing is included."
        };

        return items.Select(SafeProductCopy).ToArray();
    }

    private static string JoinOrDefault(IEnumerable<string> values, string fallback)
    {
        var materialized = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToArray();
        return materialized.Length == 0 ? fallback : string.Join("; ", materialized);
    }

    private static IReadOnlyList<RecipeCatalogSafetyBadge> DetailSafetyBadges(RecipeTemplateDefinition template)
    {
        var badges = new List<RecipeCatalogSafetyBadge>
        {
            Badge(RecipeCatalogSafetyBadgeKind.Preview, "Preview", "Template detail inspection only."),
            Badge(RecipeCatalogSafetyBadgeKind.FixtureSafe, "Fixture-safe", "No real systems are called."),
            Badge(RecipeCatalogSafetyBadgeKind.ReadOnly, "Read-only", "No product action is exposed."),
            Badge(RecipeCatalogSafetyBadgeKind.SecretsByReference, "Secrets by reference only", SecretSummary(template)),
            Badge(RecipeCatalogSafetyBadgeKind.EvidenceByReference, "Evidence by reference only", "Evidence requirements are refs only.")
        };

        if (template.RuntimeEligibility == RecipeTemplateRuntimeEligibility.LiveBlocked)
            badges.Add(Badge(RecipeCatalogSafetyBadgeKind.LiveBlocked, "Live runtime blocked", LiveBlockedExplanation(template)));
        if (template.RuntimeEligibility == RecipeTemplateRuntimeEligibility.FutureGated)
            badges.Add(Badge(RecipeCatalogSafetyBadgeKind.FutureGated, "Future-gated", LiveBlockedExplanation(template)));
        if (template.SafetyProfile.RequiresHumanApproval || template.SafetyProfile.SensitiveCategories.Count > 0)
            badges.Add(Badge(RecipeCatalogSafetyBadgeKind.HumanReviewRequired, "Requires human review", HumanReviewSummary(template)));
        if (template.TriggerRefs.Count > 0)
            badges.Add(Badge(RecipeCatalogSafetyBadgeKind.ObserveOnlyTrigger, "Observe-only trigger", TriggerSummary(template)));

        return badges;
    }

    private static RecipeTemplateDetailSystemSummary SystemSummary(RecipeTemplateDefinition template)
    {
        var summary = template.Category switch
        {
            RecipeTemplateCategory.ExcelMicrosoft365 => "Excel / Microsoft 365 preview workflow. Spreadsheet refs, validations, and evidence expectations are inspectable without a live M365 connector or file sync.",
            RecipeTemplateCategory.GoogleWorkspace => "Google Workspace preview workflow. Google API calls and Gmail delivery are not enabled; attachment and calendar flows stay draft/review only.",
            RecipeTemplateCategory.SAP => "SAP template is future API/connector-first. SAP GUI automation, RFC, BAPI, and OData calls are not enabled.",
            RecipeTemplateCategory.MercadoLibreMercadoPago => "Mercado Libre / Mercado Pago template is review-only. API calls, stock, price, listing, message, and payment mutations are not enabled.",
            RecipeTemplateCategory.ARCAFiscal => "ARCA / Fiscal template is legal/fiscal review-only. Fiscal submission, certificate/private-key usage, and web service calls are not enabled.",
            RecipeTemplateCategory.ERPLocalLATAM => "ERP Local LATAM template is draft/review only. ERP API calls, desktop automation, and real ERP mutation are not enabled.",
            RecipeTemplateCategory.GenericBrowserPortal => "Browser portal template is readiness/check/playbook only. Browser automation, real login, and challenge bypass are not enabled.",
            RecipeTemplateCategory.ComputerUseLegacy => "Computer Use legacy template is manual playbook/draft only. Desktop automation, UIA/vision execution, and hotkey hooks are not enabled.",
            _ => "Unknown template system remains blocked."
        };

        return new(
            template.System,
            summary,
            template.ConnectorEligibilityRefs.Count == 0 ? "Connector execution not enabled." : "Connector refs are future-gated/reference-only.",
            LiveBlockedExplanation(template),
            HumanReviewSummary(template),
            template.Category == RecipeTemplateCategory.ERPLocalLATAM
                ? ["Tango", "Bejerman", "Contabilium", "Alegra", "Siigo", "Odoo", "TOTVS", "CONTPAQi", "Aspel"]
                : []);
    }

    private static string LiveBlockedExplanation(RecipeTemplateDefinition template) =>
        template.Category switch
        {
            RecipeTemplateCategory.GenericBrowserPortal => "Browser automation not enabled; live runtime blocked.",
            RecipeTemplateCategory.ComputerUseLegacy => "Desktop automation not enabled; live runtime blocked.",
            RecipeTemplateCategory.ARCAFiscal => "Fiscal submission is not enabled; live runtime blocked.",
            RecipeTemplateCategory.MercadoLibreMercadoPago when template.System == RecipeTemplateSystem.MercadoPago => "Payment execution is not enabled; live runtime blocked.",
            RecipeTemplateCategory.MercadoLibreMercadoPago => "Marketplace mutation is not enabled; live runtime blocked.",
            RecipeTemplateCategory.SAP => "SAP connector execution and SAP GUI automation are not enabled.",
            RecipeTemplateCategory.ERPLocalLATAM => "ERP connector execution and desktop mutation are not enabled.",
            RecipeTemplateCategory.GoogleWorkspace => "Google API and Gmail delivery are not enabled.",
            RecipeTemplateCategory.ExcelMicrosoft365 => "Live connector and file sync are not enabled.",
            _ => "Live runtime blocked."
        };

    private static string HumanReviewSummary(RecipeTemplateDefinition template)
    {
        if (!template.SafetyProfile.RequiresHumanApproval && template.SafetyProfile.SensitiveCategories.Count == 0)
            return "No human approval path required for preview inspection.";

        return $"Requires human review for: {string.Join(", ", template.SafetyProfile.SensitiveCategories)}.";
    }

    private static string LocatorCaptureSummary(RecipeTemplateDefinition template)
    {
        if (template.Category == RecipeTemplateCategory.GenericBrowserPortal)
            return "Locator implications are preview-only; no browser selector test or live locator repair activation.";
        if (template.Category == RecipeTemplateCategory.ComputerUseLegacy)
            return "Locator and capture implications are manual/preview-only; no UIA, vision, hook, capture, or playback path.";
        return "Capture and locator summaries remain reference-only; no recorder, playback, or live repair activation.";
    }

    private static RecipeCatalogSafetyBadge Badge(RecipeCatalogSafetyBadgeKind kind, string label, string summary) =>
        new(kind, label, SafeProductCopy(summary));

    private static RecipeProductSurfaceDisabledActionMessage Disabled(
        RecipeProductSurfaceDisabledActionKind kind,
        string label,
        string reason,
        string safeNextAction) =>
        new(kind, SafeProductCopy(label), SafeProductCopy(reason), SafeProductCopy(safeNextAction));

    private static RecipeProductSurfaceDemoFlowStepCopy DemoStep(
        RecipeProductSurfaceDemoFlowStepKind kind,
        string stepId,
        string title,
        string subtitle,
        string description,
        IReadOnlyList<RecipeProductSurfaceCapabilityBadgeKind> badges,
        string blockedActionNote,
        string safeNextAction,
        IReadOnlyList<string> unavailableActionLabels,
        string claimGuardrailReminder) =>
        new(
            kind,
            stepId,
            SafeProductCopy(title),
            SafeProductCopy(subtitle),
            SafeProductCopy(description),
            badges,
            SafeProductCopy(blockedActionNote),
            SafeProductCopy(safeNextAction),
            unavailableActionLabels.Select(SafeProductCopy).ToArray(),
            SafeProductCopy(claimGuardrailReminder));

    private static RecipeCatalogReadinessBadge ReadinessBadge(RecipeTemplateReadiness readiness)
    {
        var kind = readiness.Status switch
        {
            RecipeTemplateStatus.FixtureReady => RecipeCatalogReadinessBadgeKind.FixtureReady,
            RecipeTemplateStatus.MissingToolTrust => RecipeCatalogReadinessBadgeKind.MissingToolTrust,
            RecipeTemplateStatus.MissingSecretRefs => RecipeCatalogReadinessBadgeKind.MissingSecretRefs,
            RecipeTemplateStatus.MissingValidation => RecipeCatalogReadinessBadgeKind.MissingValidation,
            RecipeTemplateStatus.MissingEvidence => RecipeCatalogReadinessBadgeKind.MissingEvidence,
            RecipeTemplateStatus.MissingApprovalPath => RecipeCatalogReadinessBadgeKind.MissingApprovalPath,
            RecipeTemplateStatus.FutureGated => RecipeCatalogReadinessBadgeKind.FutureGated,
            RecipeTemplateStatus.LiveBlocked => RecipeCatalogReadinessBadgeKind.LiveBlocked,
            RecipeTemplateStatus.BlockedByPolicy => RecipeCatalogReadinessBadgeKind.BlockedByPolicy,
            _ => RecipeCatalogReadinessBadgeKind.CatalogPreview
        };

        return new(kind, readiness.Status.ToString(), SafeProductCopy(readiness.OperatorSummary));
    }

    private static RecipeCatalogFilterState DefaultFilterState() =>
        new(
            Enum.GetValues<RecipeTemplateCategory>().Where(c => c != RecipeTemplateCategory.Unknown).ToHashSet(),
            Enum.GetValues<RecipeTemplateRegion>().Where(r => r != RecipeTemplateRegion.Unknown).ToHashSet(),
            Enum.GetValues<RecipeTemplateStatus>().ToHashSet());

    private static IReadOnlyList<RecipeCatalogSafetyBadge> GlobalSafetyBadges() =>
    [
        Badge(RecipeCatalogSafetyBadgeKind.Preview, "Preview", "Product surface is inspection-only."),
        Badge(RecipeCatalogSafetyBadgeKind.FixtureSafe, "Fixture-safe", "Template contracts use fixtures and refs."),
        Badge(RecipeCatalogSafetyBadgeKind.ReadOnly, "Read-only", "No action command is exposed."),
        Badge(RecipeCatalogSafetyBadgeKind.LiveBlocked, "Live runtime blocked", "Live runtime remains disabled."),
        Badge(RecipeCatalogSafetyBadgeKind.SecretsByReference, "Secrets by reference only", "Secret values are not shown.")
    ];

    private static IReadOnlyList<string> CategoryLabels() =>
    [
        "Excel / Microsoft 365",
        "Google Workspace",
        "SAP",
        "Mercado Libre / Mercado Pago",
        "ARCA / Fiscal Argentina",
        "ERP Local LATAM",
        "Generic Browser Portals",
        "Computer Use Legacy"
    ];

    private static string ToolTrustSummary(RecipeTemplateDefinition template) =>
        template.RequiredToolTrustRefs.Count == 0
            ? "No tool trust refs required."
            : $"Tool trust refs: {string.Join(", ", template.RequiredToolTrustRefs)}.";

    private static string ToolTrustSummary(IReadOnlyList<string> refs) =>
        refs.Count == 0 ? "No tool trust refs required." : $"Tool trust refs: {string.Join(", ", refs)}.";

    private static string SecretSummary(RecipeTemplateDefinition template) =>
        template.RequiredSecretRefs.Count == 0
            ? "No secret refs required."
            : $"Secret refs only: {string.Join(", ", template.RequiredSecretRefs)}.";

    private static string SecretRefsSummary(IReadOnlyList<string> refs) =>
        refs.Count == 0 ? "No secret refs required." : $"Secret refs only: {string.Join(", ", refs)}.";

    private static string TriggerSummary(RecipeTemplateDefinition template) =>
        template.TriggerRefs.Count == 0
            ? "No trigger refs."
            : $"Observe-only trigger refs: {string.Join(", ", template.TriggerRefs)}.";

    private static string BuildCapabilitySummary(RecipeLabCapabilitySummary summary) =>
        $"Tools: {string.Join(", ", summary.RequiredToolTrustRefs)}; secrets: {string.Join(", ", summary.RequiredSecretAliasesOrRefs)}; triggers: {string.Join(", ", summary.TriggerRefs)}.";

    private static string SafeProductCopy(string value) =>
        ReplaceForbiddenProductCopy(value);

    private static string ReplaceForbiddenProductCopy(string value)
    {
        var replacements = new (string Forbidden, string Safe)[]
        {
            ("Live automation ready", "Live runtime blocked"),
            ("Run recipe", "Inspect template"),
            ("Run now", "Preview only"),
            ("Automate now", "Preview only"),
            ("Sync live", "Sync draft"),
            ("Invoice live", "Invoice draft review"),
            ("Connect now", "Connector preview"),
            ("Connect", "Connector preview"),
            ("Use credentials", "Use secret refs"),
            ("Capture now", "Capture draft review"),
            ("Control browser", "Browser preview"),
            ("Control desktop", "Desktop preview"),
            ("Execute", "Preview"),
            ("Autofill", "Draft fill"),
            ("Apply", "Review"),
            ("Submit", "Submission review"),
            ("Publish", "Listing review"),
            ("Send", "Message review"),
            ("Record", "Capture draft"),
            ("Replay", "Playback review"),
            ("Pay", "Payment review")
        };

        var result = value;
        foreach (var (forbidden, safe) in replacements)
        {
            var escaped = Regex.Escape(forbidden);
            var pattern = char.IsLetterOrDigit(forbidden[0]) && char.IsLetterOrDigit(forbidden[^1])
                ? $@"\b{escaped}\b"
                : escaped;
            result = Regex.Replace(result, pattern, safe, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        return result;
    }

    private static string RefSummary(IReadOnlyList<string> refs) =>
        refs.Count == 0 ? "none" : string.Join(", ", refs);

    private static RecipeLabCellKind ToCellKind(string sectionId)
    {
        var id = sectionId.ToLowerInvariant();
        if (id.Contains("readiness") || id.Contains("preflight"))
            return RecipeLabCellKind.Preflight;
        if (id.Contains("evidence"))
            return RecipeLabCellKind.Evidence;
        if (id.Contains("timeline"))
            return RecipeLabCellKind.Timeline;
        if (id.Contains("approval"))
            return RecipeLabCellKind.ApprovalNarrative;
        if (id.Contains("trigger"))
            return RecipeLabCellKind.TriggerObservation;
        if (id.Contains("tool"))
            return RecipeLabCellKind.ToolTrust;
        if (id.Contains("secret"))
            return RecipeLabCellKind.SecretReference;
        if (id.Contains("locator"))
            return RecipeLabCellKind.LocatorRepair;
        return RecipeLabCellKind.Overview;
    }

    private static RecipeLabCellStatus ToCellStatus(RecipeLabSectionStatus status) =>
        status switch
        {
            RecipeLabSectionStatus.Ready => RecipeLabCellStatus.Ready,
            RecipeLabSectionStatus.Warning => RecipeLabCellStatus.Warning,
            RecipeLabSectionStatus.Blocked => RecipeLabCellStatus.Blocked,
            RecipeLabSectionStatus.NeedsHuman => RecipeLabCellStatus.NeedsHuman,
            RecipeLabSectionStatus.MissingEvidence => RecipeLabCellStatus.MissingEvidence,
            RecipeLabSectionStatus.Redacted => RecipeLabCellStatus.Redacted,
            RecipeLabSectionStatus.FutureGated => RecipeLabCellStatus.FutureGated,
            RecipeLabSectionStatus.LiveBlocked => RecipeLabCellStatus.LiveBlocked,
            RecipeLabSectionStatus.FixtureOnly => RecipeLabCellStatus.FixtureOnly,
            RecipeLabSectionStatus.ReferenceOnly => RecipeLabCellStatus.ReferenceOnly,
            _ => RecipeLabCellStatus.NotStarted
        };
}
