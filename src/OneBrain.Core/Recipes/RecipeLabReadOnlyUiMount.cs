namespace OneBrain.Core.Recipes;

public sealed record RecipeLabReadOnlyUiMountViewModel(
    string MountId,
    string Route,
    string NavigationLabel,
    RecipeCatalogSurface CatalogSurface,
    RecipeLabSurface LabSurface,
    RecipeTemplateDetailSurface TemplateDetailSurface,
    RecipeOperatorPreviewHandoffExportSurface OperatorSurface,
    ReliableRecipeLabAuditSurfaceViewModel AuditSurface,
    IReadOnlyList<string> StatusBadges,
    IReadOnlyList<string> SafetyNotices,
    IReadOnlyList<string> VisibleSections,
    IReadOnlyList<string> AllowedUiActions,
    IReadOnlyList<string> ForbiddenUiActions,
    bool RouteVisible,
    bool UsesReadOnlyProductSurface,
    bool UsesDeterministicFixture,
    bool ReadOnly,
    bool FixtureSafe,
    bool PreviewSafe,
    bool RuntimeEnabled,
    bool RecipeExecutionEnabled,
    bool BrowserCdpAutomationEnabled,
    bool WcuLiveEnabled,
    bool OcrLiveEnabled,
    bool ProviderCloudEnabled,
    bool DurablePersistenceEnabled,
    bool FilesystemWritesEnabled,
    bool HandoffExportWritesFile);

public static class RecipeLabReadOnlyUiMount
{
    public const string MountId = "recipe-lab.ui.read-only.mount.v1";
    public const string Route = "#recipeLabSurface";
    public const string NavigationLabel = "Recipe Lab";
    public const string SelectedTemplateId = "excel.extract_rows_to_workitems";

    public static RecipeLabReadOnlyUiMountViewModel CreateFixture()
    {
        var catalog = RecipeTemplateCatalogFactory.CreateGlobalLatamV1();
        var context = CreateReadinessContext(catalog);
        var labSnapshot = CreateLabSnapshot();
        var catalogSurface = RecipeProductSurfaceFactory.CreateCatalogSurface(catalog, context);
        var labSurface = RecipeProductSurfaceFactory.CreateLabSurface(labSnapshot);
        var detailSurface = RecipeProductSurfaceFactory.CreateTemplateDetailSurface(catalog, SelectedTemplateId, context);
        var operatorSurface = RecipeProductSurfaceFactory.CreateOperatorPreviewHandoffExportSurface(
            catalog,
            SelectedTemplateId,
            context,
            ["Operator note ref only."]);
        var auditSurface = ReliableRecipeLabAuditSurfacePresenter.CreateDefault();

        return new(
            MountId,
            Route,
            NavigationLabel,
            catalogSurface,
            labSurface,
            detailSurface,
            operatorSurface,
            auditSurface,
            ["READ_ONLY", "FIXTURE_SAFE", "NO_RUNTIME", "NO_LIVE_AUTOMATION"],
            [
                "Read-only Recipe Lab UI mount.",
                "Fixture-safe local catalog and previews only.",
                "No recipe execution.",
                "No runtime actions.",
                "No browser/CDP automation.",
                "No WCU live.",
                "No OCR live.",
                "No provider/cloud calls.",
                "No filesystem writes.",
                "No durable recipe persistence.",
                "Human approval required for any real action."
            ],
            [
                "Recipe Catalog Summary",
                "Recipe Templates",
                "Recipe Detail Preview",
                "Readiness Matrix",
                "Blocked Reasons",
                "Required Human Actions",
                "Operator Preview",
                "Handoff Export Preview",
                "No-Runtime Notices",
                "No-Live Notices"
            ],
            [
                "View recipe",
                "View readiness",
                "Copy preview",
                "Copy handoff preview",
                "Open read-only summary"
            ],
            [
                "recipe-execution-affordance",
                "browser-launch-affordance",
                "live-automation-affordance",
                "file-write-affordance",
                "provider-call-affordance",
                "connector-call-affordance"
            ],
            RouteVisible: true,
            UsesReadOnlyProductSurface: true,
            UsesDeterministicFixture: true,
            ReadOnly: true,
            FixtureSafe: true,
            PreviewSafe: true,
            RuntimeEnabled: false,
            RecipeExecutionEnabled: false,
            BrowserCdpAutomationEnabled: false,
            WcuLiveEnabled: false,
            OcrLiveEnabled: false,
            ProviderCloudEnabled: false,
            DurablePersistenceEnabled: false,
            FilesystemWritesEnabled: false,
            HandoffExportWritesFile: false);
    }

    private static RecipeTemplateReadinessContext CreateReadinessContext(RecipeTemplateCatalog catalog) =>
        new(
            CreateToolTrustRegistry(),
            CreateSecretRequirements(),
            CreateConnectorEligibilities(catalog),
            TriggerBindings: [],
            CreateEvidencePack(),
            [new RecipeStepEvidenceResult(true, RecipeStepEvidenceStatus.Satisfied, [], [])],
            [new RecipeValidationEvidence("validation.evidence", RecipeValidationKind.EvidenceRefExists, "expected", "redacted actual", ["evidence.ref"], RecipeValidationEvidenceStatus.Passed, RecipeValidationSeverity.Blocking, RecipeEvidenceRedactionStatus.Applied)],
            RecipeApprovalNarrativeFactory.Create("narrative.template", "recipe.template", "surface", "run.template", RecipeHumanInterventionKind.PaymentConfirmationRequired),
            LabSnapshot: null,
            RawSecretDetected: false);

    private static RecipeToolTrustRegistry CreateToolTrustRegistry() =>
        new([
            TrustedTool("tool.excel.fixture", RecipeToolCategory.Microsoft365),
            TrustedTool("tool.google.fixture", RecipeToolCategory.Microsoft365),
            TrustedTool("tool.sap.future", RecipeToolCategory.SAP) with { RuntimeStatus = RecipeToolRuntimeStatus.FutureGated },
            TrustedTool("tool.meli.future", RecipeToolCategory.Marketplace) with { RuntimeStatus = RecipeToolRuntimeStatus.FutureGated },
            TrustedTool("tool.mercadopago.future", RecipeToolCategory.Payment) with { RuntimeStatus = RecipeToolRuntimeStatus.FutureGated },
            TrustedTool("tool.arca.future", RecipeToolCategory.Fiscal) with { RuntimeStatus = RecipeToolRuntimeStatus.FutureGated },
            TrustedTool("tool.erp.future", RecipeToolCategory.ERP) with { RuntimeStatus = RecipeToolRuntimeStatus.FutureGated },
            TrustedTool("tool.browser.runtime", RecipeToolCategory.BrowserRuntime) with { RuntimeStatus = RecipeToolRuntimeStatus.LiveBlocked },
            TrustedTool("tool.desktop.runtime", RecipeToolCategory.DesktopRuntime) with { RuntimeStatus = RecipeToolRuntimeStatus.LiveBlocked }
        ]);

    private static RecipeToolTrustEntry TrustedTool(string toolId, RecipeToolCategory category) =>
        RecipeToolTrustEntry.CandidateConnector(toolId, $"{toolId} fixture") with
        {
            Category = category,
            TrustLevel = RecipeToolTrustLevel.ApprovedForFixture,
            RuntimeStatus = RecipeToolRuntimeStatus.FixtureOnly,
            RequiredSecretRefs = [],
            RequiredApprovalPolicyRefs = ["approval.fixture"],
            EvidencePolicyRefs = ["evidence.fixture"],
            RedactionPolicyRefs = ["redaction.fixture"]
        };

    private static IReadOnlyList<RecipeSecretRequirement> CreateSecretRequirements() =>
    [
        Secret("secret.sap.ref", "tool.sap.future"),
        Secret("secret.meli.ref", "tool.meli.future"),
        Secret("secret.mp.ref", "tool.mercadopago.future"),
        Secret("secret.arca.ref", "tool.arca.future", RecipeSecretKind.FiscalCertificate),
        Secret("secret.erp.ref", "tool.erp.future"),
        Secret("secret.browser.ref", "tool.browser.runtime")
    ];

    private static RecipeSecretRequirement Secret(string id, string toolRef, RecipeSecretKind kind = RecipeSecretKind.ApiKey) =>
        new($"requirement:{id}", id, kind, RecipeSecretScope.Tool, toolRef, Required: true, RecipeSecretPresenceStatus.PresentByReference, RawValuePresent: false, "redaction.fixture");

    private static IReadOnlyList<RecipeConnectorEligibility> CreateConnectorEligibilities(RecipeTemplateCatalog catalog) =>
        catalog.Templates
            .Where(t => t.ConnectorEligibilityRefs.Count > 0)
            .SelectMany(t => t.ConnectorEligibilityRefs.Select(r => ConnectorEligibility(t, r)))
            .ToArray();

    private static RecipeConnectorEligibility ConnectorEligibility(RecipeTemplateDefinition template, string refId)
    {
        var action = Enum.Parse<RecipeConnectorActionCategory>(refId.Split(':').Last());
        var mode = template.RuntimeEligibility switch
        {
            RecipeTemplateRuntimeEligibility.LiveBlocked => RecipeConnectorRuntimeMode.LiveBlocked,
            RecipeTemplateRuntimeEligibility.FutureGated => RecipeConnectorRuntimeMode.FutureGated,
            RecipeTemplateRuntimeEligibility.FixtureOnly => RecipeConnectorRuntimeMode.FixtureOnly,
            _ => RecipeConnectorRuntimeMode.ReferenceOnly
        };

        return new(
            refId,
            template.RequiredToolTrustRefs.First(),
            mode,
            action,
            new RecipeConnectorTrustRequirement(
                template.RequiredToolTrustRefs.First(),
                template.RequiredSecretRefs,
                ApprovalRequired: RecipeToolTrustSecretsPolicy.RequiresApproval(action) || template.SafetyProfile.RequiresHumanApproval,
                EvidencePolicyRequired: true),
            ApprovalPolicyPresent: true,
            EvidencePolicyPresent: true);
    }

    private static RecipeEvidencePack CreateEvidencePack() =>
        new(
            "pack.template",
            "recipe.template",
            "surface",
            "run.template",
            MissionIdRef: null,
            WorkitemRefs: [],
            StepEvidenceRefs: ["step.evidence"],
            ValidationEvidenceRefs: ["validation.evidence"],
            ApprovalRefs: ["approval.ref"],
            TimelineEventRefs: ["timeline.ref"],
            ArtifactRefs: [],
            RedactionReportRef: "redaction.report",
            RecipeEvidenceSensitivity.Confidential,
            RecipeEvidenceCompleteness.Complete,
            RecipeEvidenceCaptureMode.ReferenceOnly,
            CreatedAt: null,
            RecipeRunMode.FixtureRun,
            FailureSummary: null,
            new RecipeEvidenceRedactionSummary(true, "redaction.policy", [], [], RecipeEvidenceSecretHandlingStatus.SecretRefsOnly));

    private static RecipeLabSnapshot CreateLabSnapshot()
    {
        var sections = new[]
        {
            new RecipeLabSection("overview", "Overview", RecipeLabSectionStatus.ReferenceOnly, "Read-only template overview.", ["recipe.template"]),
            new RecipeLabSection("readiness", "Readiness", RecipeLabSectionStatus.FixtureOnly, "Canonical readiness summary.", ["readiness.ref"]),
            new RecipeLabSection("evidence", "Evidence", RecipeLabSectionStatus.ReferenceOnly, "Evidence refs only.", ["evidence.ref"]),
            new RecipeLabSection("timeline", "Timeline", RecipeLabSectionStatus.ReferenceOnly, "Timeline refs only.", ["timeline.ref"]),
            new RecipeLabSection("approval", "Human review", RecipeLabSectionStatus.NeedsHuman, "Approval narrative summary.", ["approval.ref"]),
            new RecipeLabSection("tool-secret", "Tool trust and secret refs", RecipeLabSectionStatus.ReferenceOnly, "Tool trust refs and secret.ref aliases only.", ["tool.excel.fixture", "secret.ref"]),
            new RecipeLabSection("trigger", "Observe-only trigger", RecipeLabSectionStatus.ReferenceOnly, "Observe-only trigger summary.", ["trigger.ref"]),
            new RecipeLabSection("locator", "Locator repair preview", RecipeLabSectionStatus.ReferenceOnly, "Locator repair preview only.", ["locator.ref"])
        };

        var operatorSummary = new RecipeLabOperatorSummary("Read-only lab summary.", "fixture-safe", ["raw payloads", "secret values"]);

        return new(
            "lab.snapshot",
            "recipe.template",
            "surface",
            "Recipe Lab surface",
            "ExcelMicrosoft365",
            "Excel",
            "Global",
            RecipeRunMode.CatalogPreview,
            new RecipeLabReadinessSummary(RecipeReadinessStatus.ReadyForFixtureRun, true, [], [], "RecipePolicyPreflightEvaluator"),
            "Limits summary.",
            "Complete criteria summary.",
            "Terminate criteria summary.",
            "Validation summary.",
            "Risk summary.",
            "Deterministic target summary.",
            "Evidence by reference summary.",
            "Timeline reference summary.",
            "Human review summary.",
            new RecipeLabCapabilitySummary(["cap.fixture"], ["tool.excel.fixture"], ["secret.ref"], ["trigger.ref"], ["detector.ref"]),
            "Observe-only trigger summary.",
            "Workitem queue summary.",
            "Locator repair preview summary.",
            new RecipeLabSafeNextAction(RecipeSafeNextActionKind.KeepBlocked, "Keep preview blocked for live runtime."),
            ["Live runtime blocked", "Connector execution not enabled"],
            "Redacted, reference-only, no raw payload.",
            operatorSummary,
            new RecipeLabViewModel("view.lab", sections, operatorSummary));
    }
}
