using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("RecipeProductSurfaceTemplateDetailReadinessUx")]
public sealed class RecipeProductSurfaceTemplateDetailReadinessUxTests
{
    [TestMethod]
    [TestCategory("RecipeProductSurfaceTemplateDetailReadinessUx")]
    public void TemplateDetailSurfaceIsReadOnlyAndCannotExposeRuntimeActions()
    {
        var surface = Detail("excel.extract_rows_to_workitems");
        var view = surface.ViewModel;

        Assert.IsTrue(surface.ReadOnly);
        Assert.IsTrue(surface.PreviewSafe);
        Assert.IsFalse(surface.CanStartRecipeRun);
        Assert.IsFalse(surface.CanProcessWorkitem);
        Assert.IsFalse(surface.CanEnableLiveRuntime);
        Assert.IsFalse(surface.CanOpenConnector);
        Assert.IsFalse(surface.CanRequestSecrets);
        Assert.IsFalse(surface.CanCreateRecorder);
        Assert.IsFalse(surface.CanCreateReplay);
        Assert.IsFalse(surface.CanCreateCapture);
        Assert.IsFalse(view.CanStartRecipeRun);
        Assert.IsFalse(view.CanProcessWorkitem);
        Assert.IsFalse(view.CanRequestRawSecret);
        Assert.IsFalse(view.CanReadRawSecret);
        Assert.IsFalse(view.CanEnableConnectorExecution);
        Assert.IsFalse(view.CanEnableBrowserRuntime);
        Assert.IsFalse(view.CanEnableDesktopRuntime);
        Assert.IsFalse(view.CanCreateRecorder);
        Assert.IsFalse(view.CanCreateReplay);
        Assert.IsFalse(view.CanCreateCapture);
        Assert.IsFalse(view.CanApplyLocatorRepair);
        Assert.IsFalse(view.LiveRuntimeEnabled);
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceTemplateDetailReadinessUx")]
    public void TemplateDetailExposesReadinessBlockingMissingSafeNextAndNotIncludedSummaries()
    {
        var view = Detail("browser.portal_login_readiness_check").ViewModel;

        Assert.IsTrue(view.ReadinessExplanation.IsPreviewable);
        Assert.IsFalse(view.ReadinessExplanation.LiveRuntimeEnabled);
        Assert.IsTrue(view.ReadinessExplanation.BlockingReasons.Count > 0);
        Assert.IsTrue(view.ReadinessExplanation.MissingRequirements.Count > 0);
        Assert.IsFalse(string.IsNullOrWhiteSpace(view.ReadinessExplanation.SafeNextAction.RedactedSummary));
        Assert.IsFalse(string.IsNullOrWhiteSpace(view.ReadinessExplanation.ExplicitlyNotIncludedSummary));
        Assert.IsTrue(view.Sections.Any(s => s.SectionId == "blocking"));
        Assert.IsTrue(view.Sections.Any(s => s.SectionId == "safe-next"));
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceTemplateDetailReadinessUx")]
    public void ExcelM365DetailStatesNoLiveConnectorOrSync()
    {
        var view = Detail("excel.generate_report_with_validation").ViewModel;

        StringAssert.Contains(view.SystemSummary.RedactedSummary, "Excel / Microsoft 365");
        StringAssert.Contains(view.SystemSummary.RedactedSummary, "without a live M365 connector");
        StringAssert.Contains(view.SystemSummary.RuntimeBoundarySummary, "file sync");
        Assert.AreEqual(RecipeTemplateCategory.ExcelMicrosoft365, view.Header.Category);
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceTemplateDetailReadinessUx")]
    public void GoogleWorkspaceDetailStatesNoApiOrGmailDelivery()
    {
        var view = Detail("google.gmail_attachment_to_review_queue").ViewModel;

        StringAssert.Contains(view.SystemSummary.RedactedSummary, "Google API calls");
        StringAssert.Contains(view.SystemSummary.RedactedSummary, "Gmail delivery");
        Assert.IsFalse(view.CanOpenConnector);
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceTemplateDetailReadinessUx")]
    public void SapDetailStatesNoGuiRfcBapiOrODataLiveCall()
    {
        var view = Detail("sap.vendor_invoice_validation_draft").ViewModel;

        StringAssert.Contains(view.SystemSummary.RedactedSummary, "SAP GUI automation");
        StringAssert.Contains(view.SystemSummary.RedactedSummary, "RFC");
        StringAssert.Contains(view.SystemSummary.RedactedSummary, "BAPI");
        StringAssert.Contains(view.SystemSummary.RedactedSummary, "OData");
        Assert.AreEqual(RecipeTemplateRuntimeEligibility.FutureGated, view.Header.TemplateId == "sap.vendor_invoice_validation_draft" ? RecipeTemplateRuntimeEligibility.FutureGated : RecipeTemplateRuntimeEligibility.Disabled);
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceTemplateDetailReadinessUx")]
    public void MercadoLibrePagoDetailBlocksMarketplaceAndPaymentMutation()
    {
        var stock = Detail("meli.sync_stock_from_erp_draft").ViewModel;
        var payment = Detail("meli.reconcile_orders_with_mercadopago_preview").ViewModel;

        StringAssert.Contains(stock.SystemSummary.RedactedSummary, "stock");
        StringAssert.Contains(stock.SystemSummary.RedactedSummary, "price");
        StringAssert.Contains(stock.SystemSummary.RedactedSummary, "listing");
        StringAssert.Contains(stock.SystemSummary.RedactedSummary, "message");
        StringAssert.Contains(payment.SystemSummary.RedactedSummary, "payment");
        Assert.IsTrue(stock.SafetySummary.RequiresHumanReview);
        Assert.IsTrue(payment.SafetySummary.RequiresHumanReview);
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceTemplateDetailReadinessUx")]
    public void ArcaDetailStatesNoFiscalSubmissionCertificatePrivateKeyOrWebService()
    {
        var view = Detail("arca.fiscal_submission_human_review").ViewModel;

        StringAssert.Contains(view.SystemSummary.RedactedSummary, "Fiscal submission");
        StringAssert.Contains(view.SystemSummary.RedactedSummary, "certificate/private-key");
        StringAssert.Contains(view.SystemSummary.RedactedSummary, "web service");
        Assert.IsTrue(view.SafetySummary.RequiresHumanReview);
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceTemplateDetailReadinessUx")]
    public void ErpLocalLatamDetailStatesNoApiDesktopMutationAndListsMetadataFamilies()
    {
        var view = Detail("erp.create_invoice_from_order_draft").ViewModel;

        StringAssert.Contains(view.SystemSummary.RedactedSummary, "ERP API calls");
        StringAssert.Contains(view.SystemSummary.RedactedSummary, "desktop automation");
        StringAssert.Contains(view.SystemSummary.RedactedSummary, "real ERP mutation");
        CollectionAssert.IsSubsetOf(
            new[] { "Tango", "Bejerman", "Contabilium", "Alegra", "Siigo", "Odoo", "TOTVS", "CONTPAQi", "Aspel" },
            view.SystemSummary.ApplicableSystemMetadata.ToArray());
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceTemplateDetailReadinessUx")]
    public void BrowserAndComputerUseDetailsKeepLiveAutomationBlocked()
    {
        var browser = Detail("browser.portal_login_readiness_check").ViewModel;
        var desktop = Detail("desktop.hotkey_lookup_playbook").ViewModel;

        StringAssert.Contains(browser.SystemSummary.RedactedSummary, "Browser automation");
        StringAssert.Contains(browser.SystemSummary.RedactedSummary, "real login");
        StringAssert.Contains(browser.SystemSummary.RedactedSummary, "challenge bypass");
        StringAssert.Contains(desktop.SystemSummary.RedactedSummary, "Desktop automation");
        StringAssert.Contains(desktop.SystemSummary.RedactedSummary, "UIA/vision");
        StringAssert.Contains(desktop.SystemSummary.RedactedSummary, "hotkey hooks");
        Assert.IsFalse(browser.CanEnableBrowserRuntime);
        Assert.IsFalse(desktop.CanEnableDesktopRuntime);
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceTemplateDetailReadinessUx")]
    public void SensitiveTemplatesShowHumanApprovalRequiredAndSecretRefsOnly()
    {
        var ids = new[]
        {
            "arca.fiscal_submission_human_review",
            "meli.sync_prices_from_excel_draft",
            "google.gmail_attachment_to_review_queue",
            "erp.price_list_update_review"
        };

        foreach (var view in ids.Select(id => Detail(id).ViewModel))
        {
            Assert.IsTrue(view.SafetySummary.RequiresHumanReview, view.Header.TemplateId);
            Assert.IsTrue(view.SafetySummary.SafetyBadges.Any(b => b.Kind == RecipeCatalogSafetyBadgeKind.HumanReviewRequired), view.Header.TemplateId);
            Assert.IsFalse(view.Requirements.SecretValuesShown, view.Header.TemplateId);
            Assert.IsFalse(view.RawSecretValuesShown, view.Header.TemplateId);
            Assert.IsTrue(view.Requirements.RequiredSecretRefs.All(r => r.Contains(".ref", StringComparison.OrdinalIgnoreCase)), view.Header.TemplateId);
        }
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceTemplateDetailReadinessUx")]
    public void ProductCopyScanCatchesForbiddenWordingButTemplateDetailsAreClean()
    {
        var forbidden = RecipeProductSurfaceCopyPolicy.FindForbiddenCopy(["Run recipe", "Capture now", "Live automation ready"]);
        Assert.AreEqual(3, forbidden.Count);

        var catalog = RecipeTemplateCatalogFactory.CreateGlobalLatamV1();
        var surfaces = catalog.Templates.Select(t => Detail(t.TemplateId)).ToArray();
        var hits = RecipeProductSurfaceCopyPolicy.FindForbiddenCopy(surfaces.SelectMany(SurfaceCopy));

        Assert.AreEqual(0, hits.Count, string.Join(Environment.NewLine, hits));
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceTemplateDetailReadinessUx")]
    public void TemplateDetailDoesNotIntroduceRealRuntimeCapabilities()
    {
        var view = Detail("sap.export_report_and_verify").ViewModel;

        Assert.IsFalse(view.CanOpenConnector);
        Assert.IsFalse(view.CanEnableConnectorExecution);
        Assert.IsFalse(view.CanCreateRecorder);
        Assert.IsFalse(view.CanCreateReplay);
        Assert.IsFalse(view.CanCreateCapture);
        Assert.IsFalse(view.LiveRuntimeEnabled);
        Assert.IsFalse(view.ReadinessExplanation.StartsRecipeRun);
        Assert.IsFalse(view.ReadinessExplanation.ProcessesWorkitems);
        Assert.IsFalse(view.ReadinessExplanation.ConnectorExecutionEnabled);
    }

    private static RecipeTemplateDetailSurface Detail(string templateId) =>
        RecipeProductSurfaceFactory.CreateTemplateDetailSurface(
            RecipeTemplateCatalogFactory.CreateGlobalLatamV1(),
            templateId,
            ReadyContext());

    private static IEnumerable<string> SurfaceCopy(RecipeTemplateDetailSurface surface)
    {
        var view = surface.ViewModel;
        yield return view.Header.DisplayName;
        yield return view.Header.Description;
        yield return view.Header.BusinessUseCaseSummary;
        yield return view.SystemSummary.RedactedSummary;
        yield return view.SystemSummary.ConnectorBoundarySummary;
        yield return view.SystemSummary.RuntimeBoundarySummary;
        yield return view.SystemSummary.HumanReviewSummary;
        yield return view.SafetySummary.LiveBlockedExplanation;
        yield return view.SafetySummary.NotIncludedSummary;
        yield return view.ReadinessExplanation.OperatorVisibleSummary;
        yield return view.ReadinessExplanation.ExplicitlyNotIncludedSummary;
        yield return view.ReadinessExplanation.SafeNextAction.RedactedSummary;
        yield return view.TriggerObserveOnlySummary;
        yield return view.EvidenceValidationSummary;
        yield return view.LocatorCaptureImplicationsSummary;
        yield return view.OperatorVisibleSummary;
        foreach (var text in surface.SafetyCopy)
            yield return text;
        foreach (var section in view.Sections)
        {
            yield return section.Label;
            yield return section.RedactedSummary;
        }
        foreach (var badge in view.SafetySummary.SafetyBadges)
        {
            yield return badge.Label;
            yield return badge.RedactedSummary;
        }
        foreach (var reason in view.ReadinessExplanation.Reasons)
            yield return reason.RedactedSummary;
        foreach (var reason in view.ReadinessExplanation.BlockingReasons)
            yield return reason.RedactedSummary;
        foreach (var missing in view.ReadinessExplanation.MissingRequirements)
            yield return missing.RedactedSummary;
        foreach (var warning in view.ReadinessExplanation.Warnings)
            yield return warning.RedactedSummary;
        foreach (var note in view.ReadinessExplanation.FutureEnablementNotes)
            yield return note.RedactedSummary;
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
}
