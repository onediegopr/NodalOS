using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("RecipeProductSurfaceOperatorPreviewHandoffExport")]
public sealed class RecipeProductSurfaceOperatorPreviewHandoffExportTests
{
    [TestMethod]
    [TestCategory("RecipeProductSurfaceOperatorPreviewHandoffExport")]
    public void OperatorPreviewIsReadOnlyAndCannotStartRuntimePaths()
    {
        var surface = Surface("excel.extract_rows_to_workitems");
        var preview = surface.OperatorPreview;

        Assert.IsTrue(surface.ReadOnly);
        Assert.IsTrue(surface.PreviewSafe);
        Assert.IsFalse(surface.CanStartRecipeRun);
        Assert.IsFalse(surface.CanProcessWorkitem);
        Assert.IsFalse(surface.CanEnableLiveRuntime);
        Assert.IsFalse(surface.CanOpenConnector);
        Assert.IsFalse(surface.CanRequestSecrets);
        Assert.IsFalse(surface.CanWriteExportFile);
        Assert.IsFalse(preview.CanStartRecipeRun);
        Assert.IsFalse(preview.CanProcessWorkitem);
        Assert.IsFalse(preview.CanOpenConnector);
        Assert.IsFalse(preview.CanRequestSecrets);
        Assert.IsFalse(preview.CanEnableLiveRuntime);
        Assert.IsFalse(preview.CanCreateScheduler);
        Assert.IsFalse(preview.CanCreateWatcherHookOrListener);
        Assert.IsFalse(preview.CanCreateRecorderReplayOrCapture);
        Assert.IsFalse(preview.CanApplyLocatorRepair);
        Assert.IsFalse(preview.LiveRuntimeEnabled);
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceOperatorPreviewHandoffExport")]
    public void HandoffExportPreviewNeverWritesFilesOrGeneratesArtifacts()
    {
        var export = Surface("sap.vendor_invoice_validation_draft").HandoffExportPreview;

        Assert.AreEqual(RecipeHandoffExportAvailability.PreviewOnly, export.ExportAvailability);
        Assert.IsTrue(export.PreviewOnly);
        Assert.IsFalse(export.WritesRealFile);
        Assert.IsFalse(export.GeneratesPdfOrDocx);
        Assert.IsFalse(export.OpensSaveDialog);
        Assert.IsFalse(export.CallsNetwork);
        Assert.IsFalse(export.OpensConnector);
        Assert.IsFalse(export.ReadsSecrets);
        Assert.IsFalse(export.CanStartRecipeRun);
        Assert.IsFalse(export.CanProcessWorkitem);
        Assert.IsFalse(export.LiveRuntimeEnabled);
        StringAssert.Contains(export.ProductSafeCopy, "Export preview only");
        StringAssert.Contains(export.ProductSafeCopy, "not generated as a real file");
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceOperatorPreviewHandoffExport")]
    public void HandoffPreviewContainsReadinessApprovalsEvidenceAndNoRawSecrets()
    {
        var export = Surface("meli.sync_stock_from_erp_draft", ["Operator note ref only."]).HandoffExportPreview;

        Assert.IsTrue(export.BlockingReasons.Count > 0);
        Assert.IsTrue(export.MissingRequirements.Count > 0);
        StringAssert.Contains(export.ApprovalPathSummary, "Requires human review");
        StringAssert.Contains(export.ToolTrustSummary, "tool.meli.future");
        StringAssert.Contains(export.SecretReferencesSummary, "secret.meli.ref");
        Assert.IsFalse(export.RawSecretValuesShown);
        Assert.IsFalse(export.RawPayloadShown);
        Assert.IsTrue(export.EvidenceRequirements.All(r => r.Contains("evidence", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(export.ValidationRequirements.All(r => r.Contains("validation", StringComparison.OrdinalIgnoreCase)));
        CollectionAssert.Contains(export.OperatorNotes.ToArray(), "Operator note ref only.");
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceOperatorPreviewHandoffExport")]
    public void DisabledActionSemanticsAreExplicitAndUnavailable()
    {
        var disabled = Surface("browser.portal_login_readiness_check").OperatorPreview.DisabledActions;

        Assert.IsTrue(disabled.Count >= 8);
        foreach (var action in disabled)
        {
            Assert.IsFalse(action.Available, action.ActionId);
            Assert.IsFalse(action.CanInvoke, action.ActionId);
            Assert.IsFalse(action.GrantsLiveRuntime, action.ActionId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(action.DisabledReason), action.ActionId);
        }

        Assert.IsTrue(disabled.Any(a => a.ActionId == "browser-runtime"));
        Assert.IsTrue(disabled.Any(a => a.ActionId == "desktop-runtime"));
        Assert.IsTrue(disabled.Any(a => a.ActionId == "file-output"));
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceOperatorPreviewHandoffExport")]
    public void FiscalPaymentMarketplaceMessageDeleteWriteActionsRemainPreviewOnlyAndHumanGated()
    {
        var ids = new[]
        {
            "arca.fiscal_submission_human_review",
            "meli.reconcile_orders_with_mercadopago_preview",
            "meli.publish_listing_draft_review",
            "erp.create_invoice_from_order_draft"
        };

        foreach (var preview in ids.Select(id => Surface(id).OperatorPreview))
        {
            Assert.IsTrue(preview.ExpectedHumanInterventionPoints.Count > 0, preview.Template.TemplateId);
            Assert.IsFalse(preview.SafeNextAction.AllowsExternalMutation, preview.Template.TemplateId);
            Assert.IsFalse(preview.SafeNextAction.AllowsLiveRuntime, preview.Template.TemplateId);
            StringAssert.Contains(preview.BlockedLiveRuntimeExplanation, "not enabled");
            Assert.IsFalse(preview.LiveRuntimeEnabled);
        }
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceOperatorPreviewHandoffExport")]
    public void SystemSpecificOperatorSummariesRemainSafe()
    {
        var cases = new Dictionary<string, string>
        {
            ["excel.reconcile_two_files"] = "workbook refs",
            ["google.gmail_attachment_to_review_queue"] = "Gmail delivery",
            ["sap.export_report_and_verify"] = "SAP GUI",
            ["meli.sync_prices_from_excel_draft"] = "marketplace/payment",
            ["arca.prepare_invoice_draft_review"] = "fiscal review",
            ["erp.cash_register_close_review"] = "ERP draft",
            ["browser.portal_login_readiness_check"] = "browser automation",
            ["desktop.hotkey_lookup_playbook"] = "desktop automation"
        };

        foreach (var (id, expected) in cases)
            StringAssert.Contains(Surface(id).OperatorPreview.SystemSpecificPreviewSummary, expected, id);
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceOperatorPreviewHandoffExport")]
    public void ProductCopyDoesNotOverclaimExecution()
    {
        var forbidden = RecipeProductSurfaceCopyPolicy.FindForbiddenCopy(["Run now", "Execute", "Apply", "Connect", "Capture now"]);
        Assert.AreEqual(5, forbidden.Count);

        var catalog = RecipeTemplateCatalogFactory.CreateGlobalLatamV1();
        var copy = catalog.Templates.Select(t => Surface(t.TemplateId)).SelectMany(SurfaceCopy);
        var hits = RecipeProductSurfaceCopyPolicy.FindForbiddenCopy(copy);

        Assert.AreEqual(0, hits.Count, string.Join(Environment.NewLine, hits));
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceOperatorPreviewHandoffExport")]
    public void NoBrowserDesktopCdpConnectorVaultSchedulerWatcherRecorderCaptureOrWorkitemProcessingIsIntroduced()
    {
        var surface = Surface("desktop.legacy_app_export_report_preview");
        var preview = surface.OperatorPreview;
        var export = surface.HandoffExportPreview;

        Assert.IsFalse(preview.CanOpenConnector);
        Assert.IsFalse(preview.CanCreateScheduler);
        Assert.IsFalse(preview.CanCreateWatcherHookOrListener);
        Assert.IsFalse(preview.CanCreateRecorderReplayOrCapture);
        Assert.IsFalse(export.CallsNetwork);
        Assert.IsFalse(export.OpensConnector);
        Assert.IsFalse(export.ReadsSecrets);
        Assert.IsFalse(export.WritesRealFile);
        Assert.IsFalse(export.CanProcessWorkitem);
        Assert.IsFalse(surface.CanProcessWorkitem);
    }

    private static RecipeOperatorPreviewHandoffExportSurface Surface(string templateId, IReadOnlyList<string>? notes = null) =>
        RecipeProductSurfaceFactory.CreateOperatorPreviewHandoffExportSurface(
            RecipeTemplateCatalogFactory.CreateGlobalLatamV1(),
            templateId,
            ReadyContext(),
            notes);

    private static IEnumerable<string> SurfaceCopy(RecipeOperatorPreviewHandoffExportSurface surface)
    {
        var preview = surface.OperatorPreview;
        var export = surface.HandoffExportPreview;

        yield return preview.OperatorReviewSummary;
        yield return preview.RequiredApprovalsSummary;
        yield return preview.RequiredEvidenceSummary;
        yield return preview.BlockedLiveRuntimeExplanation;
        yield return preview.SafeNextAction.RedactedSummary;
        yield return preview.NotAutomatedSummary;
        yield return preview.SystemSpecificPreviewSummary;
        yield return export.HandoffTitle;
        yield return export.TemplateSummary;
        yield return export.ReadinessSnapshot;
        yield return export.ApprovalPathSummary;
        yield return export.ToolTrustSummary;
        yield return export.SecretReferencesSummary;
        yield return export.LocatorCaptureImplications;
        yield return export.TriggerObserveOnlySummary;
        yield return export.ProductSafeCopy;
        foreach (var text in surface.SafetyCopy)
            yield return text;
        foreach (var section in preview.RequiredReviewSections)
        {
            yield return section.Label;
            yield return section.RedactedSummary;
        }
        foreach (var action in preview.DisabledActions)
        {
            yield return action.Label;
            yield return action.DisabledReason;
        }
        foreach (var item in export.NotIncludedNotAutomated)
            yield return item;
        foreach (var note in export.OperatorNotes)
            yield return note;
        foreach (var reason in export.BlockingReasons)
            yield return reason.RedactedSummary;
        foreach (var missing in export.MissingRequirements)
            yield return missing.RedactedSummary;
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
