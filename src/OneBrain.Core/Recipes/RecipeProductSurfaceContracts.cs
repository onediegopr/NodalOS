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
        "Draft",
        "Requires human review",
        "Live runtime blocked",
        "Connector execution not enabled",
        "Browser automation not enabled",
        "Desktop automation not enabled",
        "Secrets by reference only",
        "Evidence by reference only",
        "Observe-only trigger"
    ];

    public static IReadOnlyList<string> ForbiddenCopy { get; } =
    [
        "Run recipe",
        "Execute",
        "Automate now",
        "Autofill",
        "Submit",
        "Sync live",
        "Pay",
        "Publish",
        "Send",
        "Invoice live",
        "Connect now",
        "Use credentials",
        "Record",
        "Replay",
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

    private static RecipeCatalogSafetyBadge Badge(RecipeCatalogSafetyBadgeKind kind, string label, string summary) =>
        new(kind, label, SafeProductCopy(summary));

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

    private static string SecretSummary(RecipeTemplateDefinition template) =>
        template.RequiredSecretRefs.Count == 0
            ? "No secret refs required."
            : $"Secret refs only: {string.Join(", ", template.RequiredSecretRefs)}.";

    private static string TriggerSummary(RecipeTemplateDefinition template) =>
        template.TriggerRefs.Count == 0
            ? "No trigger refs."
            : $"Observe-only trigger refs: {string.Join(", ", template.TriggerRefs)}.";

    private static string BuildCapabilitySummary(RecipeLabCapabilitySummary summary) =>
        $"Tools: {string.Join(", ", summary.RequiredToolTrustRefs)}; secrets: {string.Join(", ", summary.RequiredSecretAliasesOrRefs)}; triggers: {string.Join(", ", summary.TriggerRefs)}.";

    private static string SafeProductCopy(string value) =>
        value
            .Replace("Run recipe", "Inspect template", StringComparison.OrdinalIgnoreCase)
            .Replace("Execute", "Preview", StringComparison.OrdinalIgnoreCase)
            .Replace("Automate now", "Preview only", StringComparison.OrdinalIgnoreCase)
            .Replace("Autofill", "Draft fill", StringComparison.OrdinalIgnoreCase)
            .Replace("Submit", "Submission review", StringComparison.OrdinalIgnoreCase)
            .Replace("Sync live", "Sync draft", StringComparison.OrdinalIgnoreCase)
            .Replace("Pay", "Payment review", StringComparison.OrdinalIgnoreCase)
            .Replace("Publish", "Listing review", StringComparison.OrdinalIgnoreCase)
            .Replace("Send", "Message review", StringComparison.OrdinalIgnoreCase)
            .Replace("Invoice live", "Invoice draft review", StringComparison.OrdinalIgnoreCase)
            .Replace("Connect now", "Connector preview", StringComparison.OrdinalIgnoreCase)
            .Replace("Use credentials", "Use secret refs", StringComparison.OrdinalIgnoreCase)
            .Replace("Record", "Capture draft", StringComparison.OrdinalIgnoreCase)
            .Replace("Replay", "Playback review", StringComparison.OrdinalIgnoreCase)
            .Replace("Control browser", "Browser preview", StringComparison.OrdinalIgnoreCase)
            .Replace("Control desktop", "Desktop preview", StringComparison.OrdinalIgnoreCase)
            .Replace("Live automation ready", "Live runtime blocked", StringComparison.OrdinalIgnoreCase);

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
