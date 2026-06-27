using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("RecipeProductSurfaceSafeDemoReadiness")]
public sealed class RecipeProductSurfaceSafeDemoReadinessTests
{
    private const string CorrectClaim = "NODAL OS has a fixture-safe Recipe Runtime product surface with read-only catalog, lab, templates, readiness explanations, operator previews and handoff/export preview summaries.";
    private const string ForbiddenClaim = "NODAL OS can execute/live automate these recipes.";

    [TestMethod]
    [TestCategory("RecipeProductSurfaceSafeDemoReadiness")]
    public void FinalProductSurfaceSummaryIsReadOnlyAndCannotExposeRuntimePaths()
    {
        var surface = SafeDemoSurface();
        var composition = surface.FinalComposition;

        Assert.IsTrue(surface.ReadOnly);
        Assert.IsTrue(surface.PreviewSafe);
        Assert.IsTrue(composition.ReadOnly);
        Assert.IsTrue(composition.PreviewSafe);
        Assert.IsTrue(composition.FixtureSafeOnly);
        Assert.AreEqual(RecipeProductSurfaceDemoReadinessStatus.ReadOnlyDemoReady, composition.DemoReadinessStatus);
        Assert.IsFalse(surface.CanStartRecipeRun);
        Assert.IsFalse(surface.CanProcessWorkitem);
        Assert.IsFalse(surface.CanEnableLiveRuntime);
        Assert.IsFalse(surface.CanOpenConnector);
        Assert.IsFalse(surface.CanRequestSecrets);
        Assert.IsFalse(surface.CanWriteExportFile);
        Assert.IsFalse(surface.CanRecordReplayOrCapture);
        Assert.IsFalse(composition.CanStartRecipeRun);
        Assert.IsFalse(composition.CanProcessWorkitem);
        Assert.IsFalse(composition.CanEnableLiveRuntime);
        Assert.IsFalse(composition.CanOpenConnector);
        Assert.IsFalse(composition.CanRequestSecrets);
        Assert.IsFalse(composition.CanWriteExportFile);
        Assert.IsFalse(composition.CanCreateRecorderReplayOrCapture);
        Assert.IsFalse(composition.CanCreateSchedulerWatcherHookOrListener);
        Assert.IsFalse(composition.CanApplyLocatorRepair);
        Assert.IsFalse(composition.LiveRuntimeEnabled);
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceSafeDemoReadiness")]
    public void SafeDemoScenarioCoversFullReadOnlyJourneyAndNeverPerformsLiveWork()
    {
        var scenario = SafeDemoSurface().DemoScenario;

        CollectionAssert.AreEquivalent(
            Enum.GetValues<RecipeProductSurfaceDemoStepKind>(),
            scenario.Steps.Select(s => s.Kind).ToArray());

        foreach (var step in scenario.Steps)
        {
            Assert.IsTrue(step.PreviewOnly, step.Kind.ToString());
            Assert.IsFalse(step.StartsRecipeRun, step.Kind.ToString());
            Assert.IsFalse(step.ProcessesWorkitem, step.Kind.ToString());
            Assert.IsFalse(step.WritesFile, step.Kind.ToString());
            Assert.IsFalse(step.CallsNetwork, step.Kind.ToString());
            Assert.IsFalse(step.ReadsSecrets, step.Kind.ToString());
            Assert.IsFalse(step.EnablesAutomation, step.Kind.ToString());
        }

        Assert.IsTrue(scenario.ReadOnly);
        Assert.IsTrue(scenario.PreviewOnly);
        Assert.IsFalse(scenario.CanStartRecipeRun);
        Assert.IsFalse(scenario.CanWriteExportFile);
        Assert.IsFalse(scenario.CanCallConnectorOrNetwork);
        Assert.IsFalse(scenario.CanReadSecrets);
        Assert.IsFalse(scenario.CanRecordReplayOrCapture);
        Assert.IsFalse(scenario.CanProcessWorkitem);
        Assert.IsFalse(scenario.LiveRuntimeEnabled);
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceSafeDemoReadiness")]
    public void FinalCompositionRepresentsPhasesOneTwoAndThree()
    {
        var composition = SafeDemoSurface().FinalComposition;

        StringAssert.Contains(composition.CatalogReadinessSummary, "templates");
        StringAssert.Contains(composition.LabReadinessSummary, "RecipePolicyPreflightEvaluator");
        StringAssert.Contains(composition.TemplateDetailReadinessSummary, "template");
        StringAssert.Contains(composition.OperatorPreviewReadinessSummary, "Operator preview");
        StringAssert.Contains(composition.HandoffExportPreviewReadinessSummary, "Export preview only");
        StringAssert.Contains(composition.BlockedLiveRuntimeState, "No live runtime");
        StringAssert.Contains(composition.DisabledActionStateSummary, "cannot be invoked");
        StringAssert.Contains(composition.SafeNextActionSummary, "review readiness");
        StringAssert.Contains(composition.NotAutomatedSummary, "browser/desktop automation");
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceSafeDemoReadiness")]
    public void FinalCapabilityMatrixSeparatesAllowedPreviewSurfacesFromBlockedLiveCapabilities()
    {
        var matrix = SafeDemoSurface().FinalComposition.CapabilityMatrix;

        CollectionAssert.IsSubsetOf(
            new[]
            {
                "read-only catalog",
                "read-only lab",
                "template detail",
                "readiness explanation",
                "operator preview",
                "handoff/export preview metadata",
                "safe product/demo copy"
            },
            matrix.AllowedCapabilities.ToArray());

        CollectionAssert.IsSubsetOf(
            new[]
            {
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
            },
            matrix.BlockedCapabilities.ToArray());

        Assert.IsFalse(matrix.LiveRuntimeAllowed);
        Assert.IsFalse(matrix.RealExportAllowed);
        Assert.IsFalse(matrix.ExternalMutationAllowed);
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceSafeDemoReadiness")]
    public void SafeUxCopyStatesBoundaryAndAvoidsForbiddenProductCopy()
    {
        var surface = SafeDemoSurface();

        CollectionAssert.IsSubsetOf(
            new[]
            {
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
            },
            surface.SafeUxCopy.ToArray());

        var hits = RecipeProductSurfaceCopyPolicy.FindForbiddenCopy(SurfaceCopy(surface));
        Assert.AreEqual(0, hits.Count, string.Join(Environment.NewLine, hits));
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceSafeDemoReadiness")]
    public void CorrectClaimIsPresentAndForbiddenClaimIsAbsentFromProductSurfaceCopy()
    {
        var surface = SafeDemoSurface();
        var copy = SurfaceCopy(surface).ToArray();

        CollectionAssert.Contains(copy, CorrectClaim);
        CollectionAssert.DoesNotContain(copy, ForbiddenClaim);
        StringAssert.Contains(surface.FinalComposition.BlockedForbiddenClaimSummary, "Blocked product claim");
        StringAssert.Contains(surface.DemoScenario.BlockedClaimSummary, "not available");
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceSafeDemoReadiness")]
    public void HandoffExportRemainsPreviewOnlyAndNoFilesystemOrNetworkPathIsExposed()
    {
        var operatorSurface = OperatorSurface("sap.vendor_invoice_validation_draft");
        var export = operatorSurface.HandoffExportPreview;

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
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceSafeDemoReadiness")]
    public void SystemSpecificSummariesRemainSafeThroughFinalDemoReadiness()
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
        {
            var preview = OperatorSurface(id).OperatorPreview;
            StringAssert.Contains(preview.SystemSpecificPreviewSummary, expected, id);
            Assert.IsFalse(preview.LiveRuntimeEnabled, id);
            Assert.IsFalse(preview.CanEnableLiveRuntime, id);
            Assert.IsFalse(preview.CanCreateRecorderReplayOrCapture, id);
        }
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceSafeDemoReadiness")]
    public void ForbiddenCopyPolicyCatchesFinalPhaseForbiddenTerms()
    {
        var forbidden = RecipeProductSurfaceCopyPolicy.FindForbiddenCopy(
            ["Run now", "Execute now", "Apply", "Connect", "Start worker", "Trigger now", "Capture now"]);

        Assert.IsTrue(forbidden.Count >= 7, string.Join(Environment.NewLine, forbidden));
        Assert.IsTrue(forbidden.Any(h => h.StartsWith("Run now:", StringComparison.Ordinal)));
        Assert.IsTrue(forbidden.Any(h => h.StartsWith("Execute now:", StringComparison.Ordinal)));
        Assert.IsTrue(forbidden.Any(h => h.StartsWith("Start worker:", StringComparison.Ordinal)));
        Assert.IsTrue(forbidden.Any(h => h.StartsWith("Trigger now:", StringComparison.Ordinal)));
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceSafeDemoReadiness")]
    public void NoRealExportBrowserDesktopCdpConnectorVaultSchedulerRecorderCaptureOrWorkitemExecutionIsIntroduced()
    {
        var surface = SafeDemoSurface("browser.portal_login_readiness_check");
        var composition = surface.FinalComposition;
        var scenario = surface.DemoScenario;

        Assert.IsFalse(composition.CanOpenConnector);
        Assert.IsFalse(composition.CanRequestSecrets);
        Assert.IsFalse(composition.CanCreateSchedulerWatcherHookOrListener);
        Assert.IsFalse(composition.CanCreateRecorderReplayOrCapture);
        Assert.IsFalse(composition.CanWriteExportFile);
        Assert.IsFalse(composition.CanProcessWorkitem);
        Assert.IsFalse(scenario.CanCallConnectorOrNetwork);
        Assert.IsFalse(scenario.CanReadSecrets);
        Assert.IsFalse(scenario.CanRecordReplayOrCapture);
        Assert.IsFalse(scenario.CanProcessWorkitem);
        Assert.IsFalse(surface.CanWriteExportFile);
        Assert.IsFalse(surface.CanRecordReplayOrCapture);
    }

    private static RecipeProductSurfaceSafeDemoReadinessSurface SafeDemoSurface(string templateId = "excel.extract_rows_to_workitems") =>
        RecipeProductSurfaceFactory.CreateSafeDemoReadinessSurface(
            RecipeTemplateCatalogFactory.CreateGlobalLatamV1(),
            ReadyContext(),
            LabSnapshot(),
            templateId);

    private static RecipeOperatorPreviewHandoffExportSurface OperatorSurface(string templateId) =>
        RecipeProductSurfaceFactory.CreateOperatorPreviewHandoffExportSurface(
            RecipeTemplateCatalogFactory.CreateGlobalLatamV1(),
            templateId,
            ReadyContext());

    private static IEnumerable<string> SurfaceCopy(RecipeProductSurfaceSafeDemoReadinessSurface surface)
    {
        var composition = surface.FinalComposition;
        var scenario = surface.DemoScenario;

        yield return composition.CatalogReadinessSummary;
        yield return composition.LabReadinessSummary;
        yield return composition.TemplateDetailReadinessSummary;
        yield return composition.OperatorPreviewReadinessSummary;
        yield return composition.HandoffExportPreviewReadinessSummary;
        yield return composition.BlockedLiveRuntimeState;
        yield return composition.DisabledActionStateSummary;
        yield return composition.SafeNextActionSummary;
        yield return composition.NotAutomatedSummary;
        yield return composition.InternalAllowedClaimSummary;
        yield return composition.BlockedForbiddenClaimSummary;
        yield return scenario.CorrectProductClaim;
        yield return scenario.BlockedClaimSummary;
        yield return scenario.SafeDemoReadinessSummary;
        foreach (var copy in surface.SafeUxCopy)
            yield return copy;
        foreach (var allowed in composition.CapabilityMatrix.AllowedCapabilities)
            yield return allowed;
        foreach (var blocked in composition.CapabilityMatrix.BlockedCapabilities)
            yield return blocked;
        foreach (var step in scenario.Steps)
        {
            yield return step.Label;
            yield return step.RedactedSummary;
        }
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
}
