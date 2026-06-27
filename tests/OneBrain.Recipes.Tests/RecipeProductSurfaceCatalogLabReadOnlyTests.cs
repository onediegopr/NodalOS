using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("RecipeProductSurfaceCatalogLabReadOnly")]
public sealed class RecipeProductSurfaceCatalogLabReadOnlyTests
{
    [TestMethod]
    [TestCategory("RecipeProductSurfaceCatalogLabReadOnly")]
    public void CatalogSurfaceSummarizesAllPackCategories()
    {
        var surface = CatalogSurface();
        var categories = surface.ViewModel.Packs.Select(p => p.Category).ToHashSet();

        CollectionAssert.AreEquivalent(
            new[]
            {
                RecipeTemplateCategory.ExcelMicrosoft365,
                RecipeTemplateCategory.GoogleWorkspace,
                RecipeTemplateCategory.SAP,
                RecipeTemplateCategory.MercadoLibreMercadoPago,
                RecipeTemplateCategory.ARCAFiscal,
                RecipeTemplateCategory.ERPLocalLATAM,
                RecipeTemplateCategory.GenericBrowserPortal,
                RecipeTemplateCategory.ComputerUseLegacy
            },
            categories.ToArray());
        Assert.AreEqual(41, surface.ViewModel.TotalTemplates);
        Assert.IsTrue(surface.CategoryLabels.Contains("Mercado Libre / Mercado Pago"));
        Assert.IsTrue(surface.CategoryLabels.Contains("ARCA / Fiscal Argentina"));
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceCatalogLabReadOnly")]
    public void TemplateCardsShowPreviewFixtureStatus()
    {
        var card = FindCard("excel.extract_rows_to_workitems");

        Assert.AreEqual(RecipeTemplateRuntimeEligibility.FixtureOnly, card.RuntimeEligibility);
        Assert.AreEqual(RecipeCatalogReadinessBadgeKind.FixtureReady, card.ReadinessBadge.Kind);
        Assert.IsTrue(card.SafetyBadges.Any(b => b.Kind == RecipeCatalogSafetyBadgeKind.Preview));
        Assert.IsTrue(card.SafetyBadges.Any(b => b.Kind == RecipeCatalogSafetyBadgeKind.FixtureSafe));
        Assert.IsTrue(card.ReadOnly);
        Assert.IsTrue(card.PreviewSafe);
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceCatalogLabReadOnly")]
    public void BrowserPortalTemplateCardExposesLiveBlockedStatus()
    {
        var card = FindCard("browser.table_extract_preview");

        Assert.AreEqual(RecipeTemplateRuntimeEligibility.LiveBlocked, card.RuntimeEligibility);
        Assert.AreEqual(RecipeCatalogReadinessBadgeKind.LiveBlocked, card.ReadinessBadge.Kind);
        Assert.IsTrue(card.SafetyBadges.Any(b => b.Kind == RecipeCatalogSafetyBadgeKind.LiveBlocked));
        Assert.IsFalse(card.CanEnableBrowserRuntime);
        Assert.IsFalse(card.BrowserAutomationEnabled);
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceCatalogLabReadOnly")]
    public void ComputerUseTemplateCardExposesLiveBlockedStatus()
    {
        var card = FindCard("desktop.legacy_app_export_report_preview");

        Assert.AreEqual(RecipeTemplateRuntimeEligibility.LiveBlocked, card.RuntimeEligibility);
        Assert.AreEqual(RecipeCatalogReadinessBadgeKind.LiveBlocked, card.ReadinessBadge.Kind);
        Assert.IsTrue(card.SafetyBadges.Any(b => b.Kind == RecipeCatalogSafetyBadgeKind.LiveBlocked));
        Assert.IsFalse(card.CanEnableDesktopRuntime);
        Assert.IsFalse(card.DesktopAutomationEnabled);
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceCatalogLabReadOnly")]
    public void FiscalPaymentMarketplaceTemplatesShowHumanApprovalAndBlockedState()
    {
        var ids = new[]
        {
            "meli.sync_stock_from_erp_draft",
            "meli.reconcile_orders_with_mercadopago_preview",
            "arca.fiscal_submission_human_review"
        };

        foreach (var card in ids.Select(FindCard))
        {
            Assert.IsTrue(card.RequiresHumanReview, card.TemplateId);
            Assert.IsTrue(card.SafetyBadges.Any(b => b.Kind == RecipeCatalogSafetyBadgeKind.HumanReviewRequired), card.TemplateId);
            Assert.IsTrue(card.RuntimeEligibility is RecipeTemplateRuntimeEligibility.FutureGated or RecipeTemplateRuntimeEligibility.LiveBlocked, card.TemplateId);
            Assert.IsFalse(card.LiveRuntimeEnabled, card.TemplateId);
            Assert.IsFalse(card.ConnectorExecutionEnabled, card.TemplateId);
        }
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceCatalogLabReadOnly")]
    public void CatalogCardsCannotStartRunsProcessWorkitemsReadSecretsOrEnableConnectors()
    {
        foreach (var card in CatalogSurface().ViewModel.Packs.SelectMany(p => p.Templates))
        {
            Assert.IsFalse(card.CanStartRecipeRun, card.TemplateId);
            Assert.IsFalse(card.CanProcessWorkitem, card.TemplateId);
            Assert.IsFalse(card.CanRequestRawSecret, card.TemplateId);
            Assert.IsFalse(card.CanReadRawSecret, card.TemplateId);
            Assert.IsFalse(card.CanOpenConnector, card.TemplateId);
            Assert.IsFalse(card.CanEnableConnectorExecution, card.TemplateId);
            Assert.IsFalse(card.CanAuthorizeLiveRuntime, card.TemplateId);
        }
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceCatalogLabReadOnly")]
    public void RecipeLabSummaryIsReadOnlyAndCannotExposeActions()
    {
        var surface = LabSurface();
        var view = surface.ViewModel;

        Assert.IsTrue(surface.ReadOnly);
        Assert.IsTrue(view.ReadOnly);
        Assert.IsFalse(surface.CanStartRecipeRun);
        Assert.IsFalse(surface.CanProcessWorkitem);
        Assert.IsFalse(surface.CanEnableLiveRuntime);
        Assert.IsFalse(view.CanStartRecipeRun);
        Assert.IsFalse(view.CanExecuteAction);
        Assert.IsFalse(view.CanEditRecipeContracts);
        Assert.IsFalse(view.CanApproveLiveRuntime);
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceCatalogLabReadOnly")]
    public void RecipeLabShowsCanonicalReadinessEvidenceTimelineRefsAndSecretAliasesOnly()
    {
        var view = LabSurface().ViewModel;

        Assert.AreEqual("RecipePolicyPreflightEvaluator", view.ReadinessSummary.CanonicalEvaluatorName);
        Assert.IsFalse(view.ReadinessSummary.FoundationOnlyReadinessUsedAsCanonical);
        StringAssert.Contains(view.EvidenceTimelineSummary, "reference");
        StringAssert.Contains(view.ToolTrustSecretSummary, "secret.ref");
        Assert.IsFalse(view.RawSecretValuesShown);
        Assert.IsFalse(view.RawPayloadShown);
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceCatalogLabReadOnly")]
    public void RecipeLabShowsTriggerObserveOnlyLocatorPreviewAndCaptureDraftOnly()
    {
        var view = LabSurface().ViewModel;

        StringAssert.Contains(view.TriggerObserveOnlySummary, "Observe-only");
        StringAssert.Contains(view.LocatorRepairPreviewSummary, "preview");
        StringAssert.Contains(view.CaptureDraftSummary, "Draft");
        Assert.IsFalse(view.CanApplyLocatorRepair);
        Assert.IsFalse(view.CanReplayLocator);
        Assert.IsFalse(view.CanRecordCapture);
        Assert.IsFalse(view.LiveRuntimeEnabled);
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceCatalogLabReadOnly")]
    public void LabCellsAreInspectionOnlyAndNoApplyOrReplayIsExposed()
    {
        foreach (var cell in LabSurface().ViewModel.Cells)
        {
            Assert.IsTrue(cell.InspectionOnly, cell.CellId);
            Assert.IsFalse(cell.CanExecute, cell.CellId);
            Assert.IsFalse(cell.CanApplyRepair, cell.CellId);
            Assert.IsFalse(cell.CanStartRecipeRun, cell.CellId);
            Assert.IsFalse(cell.CanSubmit, cell.CellId);
            Assert.IsFalse(cell.RawSecretValuesShown, cell.CellId);
        }
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceCatalogLabReadOnly")]
    public void CaptureDraftSummaryRemainsDraftOnlyAndCannotBecomeRunReady()
    {
        var catalog = RecipeTemplateCatalogFactory.CreateGlobalLatamV1();
        var mapping = RecipeCaptureTemplateMapper.MapToTemplate(CaptureSession(), catalog, ReadyContext());
        var lab = RecipeProductSurfaceFactory.CreateLabSurface(LabSnapshot(), null, mapping);

        Assert.IsFalse(mapping.LiveRuntimeEnabled);
        Assert.IsFalse(mapping.CanOverrideCompositeReadiness);
        StringAssert.Contains(lab.ViewModel.CaptureDraftSummary, "Draft");
        Assert.IsFalse(lab.ViewModel.CanStartRecipeRun);
        Assert.IsFalse(lab.ViewModel.CanRecordCapture);
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceCatalogLabReadOnly")]
    public void ProductCopyScanCatchesForbiddenWordingButNewSurfacesAreClean()
    {
        var forbidden = RecipeProductSurfaceCopyPolicy.FindForbiddenCopy(["Run recipe", "Live automation ready"]);
        Assert.AreEqual(2, forbidden.Count);

        var copy = SurfaceCopy(CatalogSurface(), LabSurface());
        var hits = RecipeProductSurfaceCopyPolicy.FindForbiddenCopy(copy);

        Assert.AreEqual(0, hits.Count, string.Join(Environment.NewLine, hits));
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceCatalogLabReadOnly")]
    public void NoRealRuntimeCapabilitiesAreIntroducedByProductSurface()
    {
        var catalog = CatalogSurface();
        var lab = LabSurface();

        Assert.IsFalse(catalog.CanStartRecipeRun);
        Assert.IsFalse(catalog.CanProcessWorkitem);
        Assert.IsFalse(catalog.CanEnableLiveRuntime);
        Assert.IsFalse(catalog.CanOpenConnector);
        Assert.IsFalse(catalog.CanRequestSecrets);
        Assert.IsFalse(lab.CanStartRecipeRun);
        Assert.IsFalse(lab.CanProcessWorkitem);
        Assert.IsFalse(lab.CanEnableLiveRuntime);
        Assert.IsFalse(lab.CanApplyLocatorRepair);
        Assert.IsFalse(lab.CanRecordCapture);
    }

    private static RecipeCatalogSurface CatalogSurface() =>
        RecipeProductSurfaceFactory.CreateCatalogSurface(RecipeTemplateCatalogFactory.CreateGlobalLatamV1(), ReadyContext());

    private static RecipeLabSurface LabSurface() =>
        RecipeProductSurfaceFactory.CreateLabSurface(LabSnapshot());

    private static RecipeTemplateCardViewModel FindCard(string templateId) =>
        CatalogSurface().ViewModel.Packs.SelectMany(p => p.Templates).Single(t => t.TemplateId == templateId);

    private static IReadOnlyList<string> SurfaceCopy(RecipeCatalogSurface catalog, RecipeLabSurface lab)
    {
        var copy = new List<string>
        {
            catalog.ViewModel.ProductSurfaceSummary,
            lab.ViewModel.SafetyBoundarySummary,
            lab.ViewModel.EvidenceTimelineSummary,
            lab.ViewModel.ApprovalHumanSummary,
            lab.ViewModel.ToolTrustSecretSummary,
            lab.ViewModel.TriggerObserveOnlySummary,
            lab.ViewModel.LocatorRepairPreviewSummary,
            lab.ViewModel.CaptureDraftSummary
        };

        copy.AddRange(catalog.SafetyCopy);
        copy.AddRange(lab.SafetyCopy);
        copy.AddRange(catalog.ViewModel.GlobalSafetyBadges.SelectMany(b => new[] { b.Label, b.RedactedSummary }));
        copy.AddRange(catalog.ViewModel.Packs.SelectMany(p => p.Templates).SelectMany(t => new[]
        {
            t.DisplayName,
            t.Description,
            t.ReadinessBadge.Label,
            t.ReadinessBadge.RedactedSummary,
            t.ToolTrustSummary,
            t.SecretRefSummary,
            t.TriggerStatusSummary,
            t.LiveRuntimeStatus,
            t.SafeNextActionSummary,
            t.NotIncludedSummary
        }));
        copy.AddRange(lab.ViewModel.Sections.SelectMany(s => new[] { s.Label, s.RedactedSummary }));
        copy.AddRange(lab.ViewModel.Cells.SelectMany(c => new[] { c.Label, c.RedactedSummary }));

        return copy;
    }

    private static RecipeTemplateReadinessContext ReadyContext()
    {
        var catalog = RecipeTemplateCatalogFactory.CreateGlobalLatamV1();
        return new(
            Registry(),
            Secrets(),
            ConnectorEligibilities(catalog),
            TriggerBindings: [],
            EvidencePack(),
            [new RecipeStepEvidenceResult(true, RecipeStepEvidenceStatus.Satisfied, [], [])],
            [new RecipeValidationEvidence("validation.evidence", RecipeValidationKind.EvidenceRefExists, "expected", "redacted actual", ["evidence.ref"], RecipeValidationEvidenceStatus.Passed, RecipeValidationSeverity.Blocking, RecipeEvidenceRedactionStatus.Applied)],
            RecipeApprovalNarrativeFactory.Create("narrative.template", "recipe.template", "surface", "run.template", RecipeHumanInterventionKind.PaymentConfirmationRequired),
            LabSnapshot: null,
            RawSecretDetected: false);
    }

    private static RecipeToolTrustRegistry Registry() =>
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

    private static IReadOnlyList<RecipeSecretRequirement> Secrets() =>
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

    private static IReadOnlyList<RecipeConnectorEligibility> ConnectorEligibilities(RecipeTemplateCatalog catalog) =>
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
            new RecipeConnectorTrustRequirement(template.RequiredToolTrustRefs.First(), template.RequiredSecretRefs, ApprovalRequired: RecipeToolTrustSecretsPolicy.RequiresApproval(action) || template.SafetyProfile.RequiresHumanApproval, EvidencePolicyRequired: true),
            ApprovalPolicyPresent: true,
            EvidencePolicyPresent: true);
    }

    private static RecipeEvidencePack EvidencePack() =>
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

    private static RecipeLabSnapshot LabSnapshot()
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

    private static RecipeCaptureSession CaptureSession() =>
        new(
            "capture.surface",
            "Capture draft summary",
            "Manual description only",
            RecipeTemplateCategory.ExcelMicrosoft365,
            RecipeTemplateSystem.Excel,
            RecipeTemplateRegion.Global,
            [RecipeTemplateCountry.Global],
            RecipeCaptureMode.ManualDescriptionOnly,
            RecipeCaptureSafetyStatus.SafeForDraft,
            new RecipeCaptureReadiness(true, false, RecipeCaptureSessionStatus.Draft, [], [], "Draft only"),
            "Describe observed workflow without recording.",
            [new RecipeCapturedStepRef("step.fixture")],
            [new RecipeCapturedEvidenceRef("evidence.fixture", RecipeEvidenceSourceKind.ExtractedDataRef)],
            [],
            [],
            null,
            [],
            "redaction.summary",
            ["timeline.fixture"],
            []);
}
