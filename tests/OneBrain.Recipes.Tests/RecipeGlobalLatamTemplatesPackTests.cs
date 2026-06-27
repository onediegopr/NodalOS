using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("RecipeGlobalLatamTemplatesPack")]
public sealed class RecipeGlobalLatamTemplatesPackTests
{
    [TestMethod]
    public void TemplateCatalogContainsGlobalAndLatamPacks()
    {
        var catalog = Catalog();

        Assert.AreEqual(8, catalog.Packs.Count);
        Assert.AreEqual(41, catalog.Templates.Count);
        Assert.IsTrue(catalog.Packs.Any(p => p.Region == RecipeTemplateRegion.Global));
        Assert.IsTrue(catalog.Packs.Any(p => p.Region == RecipeTemplateRegion.LATAM));
        Assert.IsTrue(catalog.Packs.Any(p => p.Region == RecipeTemplateRegion.Argentina));
        CollectionAssert.Contains(catalog.Packs.Select(p => p.PackId).ToArray(), "pack.mercado");
        CollectionAssert.Contains(catalog.Packs.Select(p => p.PackId).ToArray(), "pack.arca.fiscal");
        Assert.IsFalse(catalog.LiveRuntimeEnabled);
        Assert.IsFalse(catalog.ConnectorExecutionEnabled);
    }

    [TestMethod]
    public void AllTemplatesDefaultToPreviewFixtureOrReferenceOnlyNeverLive()
    {
        foreach (var template in Catalog().Templates)
        {
            Assert.IsFalse(template.LiveRuntimeEnabled, template.TemplateId);
            Assert.IsFalse(template.ConnectorExecutionEnabled, template.TemplateId);
            Assert.IsFalse(template.BrowserAutomationEnabled, template.TemplateId);
            Assert.IsFalse(template.DesktopAutomationEnabled, template.TemplateId);
            Assert.IsFalse(template.AutomaticRunEnabled, template.TemplateId);
            CollectionAssert.DoesNotContain(template.AllowedRunModes.ToArray(), RecipeRunMode.LiveRunBlocked, template.TemplateId);
            CollectionAssert.Contains(template.BlockedRunModes.ToArray(), RecipeRunMode.LiveRunBlocked, template.TemplateId);
        }
    }

    [TestMethod]
    public void CompositeTemplateReadinessInvokesBasePolicyPreflight()
    {
        var template = Find("excel.extract_rows_to_workitems");
        var broken = template with { RecipeDefinition = template.RecipeDefinition with { RunLimits = null } };
        var readiness = RecipeTemplateReadinessEvaluator.Evaluate(broken, ReadyContext());

        Assert.IsFalse(readiness.IsReady);
        Assert.IsTrue(readiness.BlockingIssues.Any(i => i.IssueId == "missing-limits"));
        Assert.AreEqual(RecipeTemplateStatus.BlockedByPolicy, readiness.Status);
        Assert.IsFalse(readiness.LiveRuntimeEnabled);
    }

    [TestMethod]
    public void CompositeReadinessBlocksMissingValidationAndEvidenceWhenRequired()
    {
        var template = Find("browser.download_file_evidence_preview");
        var missingValidationRecipe = template.RecipeDefinition with
        {
            Blocks =
            [
                template.RecipeDefinition.Blocks.Single() with
                {
                    BlockType = RecipeBlockType.FileDownloadEvidence
                }
            ],
            ValidationPolicy = new RecipeValidationPolicy([])
        };
        var missingValidation = template with { RecipeDefinition = missingValidationRecipe };
        var missingEvidence = RecipeTemplateReadinessEvaluator.Evaluate(template, ReadyContext(includeEvidencePack: false));

        var validationReadiness = RecipeTemplateReadinessEvaluator.Evaluate(missingValidation, ReadyContext());

        Assert.IsFalse(validationReadiness.IsReady);
        Assert.IsTrue(validationReadiness.BlockingIssues.Any(i => i.Status == RecipeReadinessStatus.BlockedMissingValidation));
        Assert.IsFalse(missingEvidence.IsReady);
        Assert.IsTrue(missingEvidence.BlockingIssues.Any(i => i.IssueId == "template-missing-evidence-pack"));
    }

    [TestMethod]
    public void CompositeReadinessBlocksMissingToolTrustSecretAndRawSecret()
    {
        var sap = Find("sap.purchase_order_status_check");
        var missingTool = RecipeTemplateReadinessEvaluator.Evaluate(sap, ReadyContext(registry: new RecipeToolTrustRegistry([])));
        var missingSecret = RecipeTemplateReadinessEvaluator.Evaluate(sap, ReadyContext(secrets: []));
        var rawSecret = RecipeTemplateReadinessEvaluator.Evaluate(sap, ReadyContext(rawSecretDetected: true));

        Assert.IsTrue(missingTool.BlockingIssues.Any(i => i.IssueId == "template-missing-tool-trust"));
        Assert.IsTrue(missingSecret.BlockingIssues.Any(i => i.IssueId == "template-missing-secret-ref"));
        Assert.IsTrue(rawSecret.BlockingIssues.Any(i => i.IssueId == "template-context-raw-secret-detected"));
        Assert.IsFalse(missingTool.LiveRuntimeEnabled);
    }

    [TestMethod]
    public void CompositeReadinessBlocksLiveBlockedBrowserAndDesktopRuntimeTools()
    {
        var browser = RecipeTemplateReadinessEvaluator.Evaluate(Find("browser.table_extract_preview"), ReadyContext());
        var desktop = RecipeTemplateReadinessEvaluator.Evaluate(Find("desktop.legacy_app_export_report_preview"), ReadyContext());

        Assert.IsFalse(browser.IsReady);
        Assert.AreEqual(RecipeTemplateStatus.LiveBlocked, browser.Status);
        Assert.IsTrue(browser.BlockingIssues.Any(i => i.IssueId is "template-live-runtime-blocked" or "template-tool-live-blocked"));
        Assert.IsFalse(desktop.IsReady);
        Assert.AreEqual(RecipeTemplateStatus.LiveBlocked, desktop.Status);
        Assert.IsTrue(desktop.BlockingIssues.Any(i => i.IssueId is "template-live-runtime-blocked" or "template-tool-live-blocked" or "desktop-draft-blocked"));
    }

    [TestMethod]
    public void CompositeReadinessBlocksConnectorExecutionAndFutureGatedConnectors()
    {
        var sap = RecipeTemplateReadinessEvaluator.Evaluate(Find("sap.export_report_and_verify"), ReadyContext());
        var payment = RecipeTemplateReadinessEvaluator.Evaluate(Find("meli.reconcile_orders_with_mercadopago_preview"), ReadyContext());

        Assert.IsFalse(sap.IsReady);
        Assert.AreEqual(RecipeTemplateStatus.FutureGated, sap.Status);
        Assert.IsTrue(sap.BlockingIssues.Any(i => i.Status == RecipeReadinessStatus.BlockedLiveRuntimeDisabled));
        Assert.IsFalse(payment.IsReady);
        Assert.IsFalse(payment.ConnectorExecutionEnabled);
        Assert.IsFalse(payment.ActionAuthorityGranted);
    }

    [TestMethod]
    public void CompositeReadinessBlocksTriggerAutorun()
    {
        var trigger = new RecipeTriggerDefinition(
            "trigger.autorun",
            RecipeTriggerKind.ScheduleFuture,
            new RecipeDetectorRef("detector.autorun"),
            "recipe.fixture",
            "8.0.0",
            null,
            null,
            RecipeTriggerSource.FutureSchedule,
            RecipeTriggerScope.Recipe,
            RecipeTriggerSafetyMode.FutureGated,
            RecipeTriggerRunMode.FutureAutoRunBlocked,
            RecipeTriggerStatus.FutureGated,
            "future autorun request",
            "schema.trigger",
            [],
            [],
            ["evidence.trigger"],
            ["timeline.trigger"],
            [],
            [],
            [],
            [RecipeTriggerRunMode.FutureAutoRunBlocked]);
        var detector = new RecipeDetectorDefinition("detector.autorun", RecipeDetectorKind.FutureScheduleObserver, RecipeTriggerSource.FutureSchedule, RecipeTriggerScope.Recipe, RecipeTriggerSafetyMode.FutureGated, "future schedule", "schema.trigger", [], [], ["evidence.trigger"]);
        var policy = new RecipeTriggerPolicy([], [], AutoRunAllowed: true);

        var readiness = RecipeTemplateReadinessEvaluator.Evaluate(Find("excel.extract_rows_to_workitems"), ReadyContext(triggerBindings: [new RecipeTemplateTriggerBinding(trigger, detector, policy)]));

        Assert.IsFalse(readiness.IsReady);
        Assert.IsTrue(readiness.BlockingIssues.Any(i => i.IssueId == "trigger-autorun-blocked"));
        Assert.IsFalse(readiness.StartsRecipeRun);
        Assert.IsFalse(readiness.ProcessesWorkitems);
    }

    [TestMethod]
    public void CompositeReadinessBlocksFailedBlockingValidationCompleteness()
    {
        var failed = new RecipeValidationEvidence("validation.failed", RecipeValidationKind.EvidenceRefExists, "expected", "redacted actual", ["evidence.ref"], RecipeValidationEvidenceStatus.Failed, RecipeValidationSeverity.Blocking, RecipeEvidenceRedactionStatus.Applied, "failed fixture validation");

        var readiness = RecipeTemplateReadinessEvaluator.Evaluate(Find("excel.extract_rows_to_workitems"), ReadyContext(validationEvidence: [failed]));

        Assert.IsFalse(readiness.IsReady);
        Assert.IsTrue(readiness.BlockingIssues.Any(i => i.IssueId == "template-blocking-validation-failed"));
    }

    [TestMethod]
    public void ExcelExtractRowsTemplateIsPreviewFixtureSafe()
    {
        var readiness = RecipeTemplateReadinessEvaluator.Evaluate(Find("excel.extract_rows_to_workitems"), ReadyContext());

        Assert.IsTrue(readiness.IsReady);
        Assert.AreEqual(RecipeTemplateStatus.FixtureReady, readiness.Status);
        Assert.IsFalse(readiness.LiveRuntimeEnabled);
        Assert.IsFalse(readiness.ActionAuthorityGranted);
    }

    [TestMethod]
    public void GoogleGmailAttachmentTemplateCreatesReviewQueueDraftOnlyNoEmailSend()
    {
        var template = Find("google.gmail_attachment_to_review_queue");

        Assert.AreEqual(RecipeTemplateRuntimeEligibility.FixtureOnly, template.RuntimeEligibility);
        Assert.IsFalse(template.SafetyProfile.SensitiveCategories.Contains(SensitiveActionCategory.EmailOrMessageSend));
        StringAssert.Contains(template.OperatorVisibleSummary, "no email sending");
        Assert.IsFalse(template.LiveRuntimeEnabled);
    }

    [TestMethod]
    public void SapMutationDraftTemplatesRequireHumanApprovalAndRemainLiveBlocked()
    {
        foreach (var template in Catalog().Templates.Where(t => t.Category == RecipeTemplateCategory.SAP && t.TemplateId.Contains("draft", StringComparison.OrdinalIgnoreCase)))
        {
            var readiness = RecipeTemplateReadinessEvaluator.Evaluate(template, ReadyContext());

            Assert.IsTrue(template.SafetyProfile.RequiresHumanApproval, template.TemplateId);
            Assert.IsFalse(readiness.IsReady, template.TemplateId);
            Assert.AreEqual(RecipeTemplateStatus.FutureGated, readiness.Status, template.TemplateId);
            Assert.IsFalse(readiness.LiveRuntimeEnabled, template.TemplateId);
        }
    }

    [TestMethod]
    public void MercadoLibreStockPriceListingTemplatesRequireApprovalAndRemainLiveBlocked()
    {
        var ids = new[] { "meli.sync_stock_from_erp_draft", "meli.sync_prices_from_excel_draft", "meli.publish_listing_draft_review" };

        foreach (var template in ids.Select(Find))
        {
            Assert.IsTrue(template.SafetyProfile.RequiresHumanApproval, template.TemplateId);
            Assert.IsTrue(template.SafetyProfile.SensitiveCategories.Any(c => c is SensitiveActionCategory.PriceOrStockChange or SensitiveActionCategory.PublicPosting or SensitiveActionCategory.MarketplaceListingChange));
            Assert.AreEqual(RecipeTemplateStatus.FutureGated, RecipeTemplateReadinessEvaluator.Evaluate(template, ReadyContext()).Status);
        }
    }

    [TestMethod]
    public void MercadoPagoReconciliationIsPreviewOnlyAndNoPaymentExecution()
    {
        var template = Find("meli.reconcile_orders_with_mercadopago_preview");

        Assert.IsTrue(template.SafetyProfile.SensitiveCategories.Contains(SensitiveActionCategory.Payment));
        StringAssert.Contains(template.OperatorVisibleSummary, "no payment execution");
        Assert.IsFalse(template.ConnectorExecutionEnabled);
        Assert.AreEqual(RecipeTemplateStatus.FutureGated, RecipeTemplateReadinessEvaluator.Evaluate(template, ReadyContext()).Status);
    }

    [TestMethod]
    public void ArcaFiscalTemplatesRequireHumanApprovalAndRemainLiveBlocked()
    {
        foreach (var template in Catalog().Templates.Where(t => t.Category == RecipeTemplateCategory.ARCAFiscal))
        {
            Assert.IsTrue(template.SafetyProfile.SensitiveCategories.Contains(SensitiveActionCategory.FiscalOrLegalSubmission), template.TemplateId);
            Assert.IsTrue(template.SafetyProfile.RequiresHumanApproval, template.TemplateId);
            Assert.IsFalse(RecipeTemplateReadinessEvaluator.Evaluate(template, ReadyContext()).LiveRuntimeEnabled);
        }
    }

    [TestMethod]
    public void ErpLocalMutationTemplatesRequireHumanApprovalAndRemainLiveBlocked()
    {
        var mutationTemplates = Catalog().Templates.Where(t => t.Category == RecipeTemplateCategory.ERPLocalLATAM && t.SafetyProfile.SensitiveCategories.Count > 0).ToArray();

        Assert.IsTrue(mutationTemplates.Length >= 5);
        foreach (var template in mutationTemplates)
        {
            Assert.IsTrue(template.SafetyProfile.RequiresHumanApproval, template.TemplateId);
            Assert.IsFalse(RecipeTemplateReadinessEvaluator.Evaluate(template, ReadyContext()).LiveRuntimeEnabled, template.TemplateId);
        }
    }

    [TestMethod]
    public void GenericBrowserPortalTemplatesRemainBrowserLiveBlocked()
    {
        foreach (var template in Catalog().Templates.Where(t => t.Category == RecipeTemplateCategory.GenericBrowserPortal))
        {
            var readiness = RecipeTemplateReadinessEvaluator.Evaluate(template, ReadyContext());
            Assert.AreEqual(RecipeTemplateRuntimeEligibility.LiveBlocked, template.RuntimeEligibility, template.TemplateId);
            Assert.AreEqual(RecipeTemplateStatus.LiveBlocked, readiness.Status, template.TemplateId);
            Assert.IsFalse(readiness.LiveRuntimeEnabled, template.TemplateId);
        }
    }

    [TestMethod]
    public void ComputerUseLegacyTemplatesRemainDesktopLiveBlocked()
    {
        foreach (var template in Catalog().Templates.Where(t => t.Category == RecipeTemplateCategory.ComputerUseLegacy))
        {
            var readiness = RecipeTemplateReadinessEvaluator.Evaluate(template, ReadyContext());
            Assert.AreEqual(RecipeTemplateRuntimeEligibility.LiveBlocked, template.RuntimeEligibility, template.TemplateId);
            Assert.AreEqual(RecipeTemplateStatus.LiveBlocked, readiness.Status, template.TemplateId);
            Assert.IsFalse(template.DesktopAutomationEnabled, template.TemplateId);
        }
    }

    [TestMethod]
    public void BrowserLoginSessionTemplatesDoNotBypassTwoFactorCaptchaOrChallenge()
    {
        var login = Find("browser.portal_login_readiness_check");
        var session = Find("browser.session_expired_detector_preview");

        Assert.IsTrue(login.SafetyProfile.SensitiveCategories.Contains(SensitiveActionCategory.TwoFactor));
        Assert.IsTrue(RecipeTemplateReadinessEvaluator.Evaluate(login, ReadyContext()).BlockingIssues.Any(i => i.IssueId == "template-challenge-human-required"));
        Assert.IsTrue(session.SafetyProfile.SensitiveCategories.Contains(SensitiveActionCategory.Login));
        Assert.IsFalse(session.OperatorVisibleSummary.Contains("bypass", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void TemplatePacksExposeSafeNextActionAndNotIncludedSummary()
    {
        foreach (var template in Catalog().Templates)
        {
            Assert.IsFalse(template.SafeNextAction.AllowsLiveRuntime, template.TemplateId);
            Assert.IsFalse(template.SafeNextAction.ActionAuthorityGranted, template.TemplateId);
            StringAssert.Contains(template.NotIncludedOrNotAutomatedSummary, "No live execution");
            StringAssert.Contains(template.NotIncludedOrNotAutomatedSummary, "no raw secrets");
        }
    }

    [TestMethod]
    public void TemplateLabSummaryExcludesRawSecretsAndPayloads()
    {
        var template = Find("excel.extract_rows_to_workitems");
        var context = ReadyContext();
        var preflight = RecipePolicyPreflightEvaluator.Evaluate(template.RecipeDefinition, RecipeRunMode.FixtureRun);
        var snapshot = RecipeLabSnapshotFactory.Create("snapshot.template", template.RecipeDefinition, RecipeRunMode.FixtureRun, preflight, EvidencePack(), toolTrustRegistry: context.ToolTrustRegistry);

        Assert.IsTrue(snapshot.ReadOnly);
        Assert.IsFalse(snapshot.RawSecretValuesExposed);
        Assert.IsFalse(snapshot.OperatorSummary.RawDataOmitted.Any(s => s.Contains("secret-value", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(snapshot.CanUnlockLiveRuntime);
    }

    [TestMethod]
    public void UnknownTemplateSystemDefaultsBlockedFutureGated()
    {
        var template = Find("excel.extract_rows_to_workitems") with
        {
            Category = RecipeTemplateCategory.Unknown,
            System = RecipeTemplateSystem.Unknown,
            RuntimeEligibility = RecipeTemplateRuntimeEligibility.FutureGated
        };

        var readiness = RecipeTemplateReadinessEvaluator.Evaluate(template, ReadyContext());

        Assert.IsFalse(readiness.IsReady);
        Assert.AreEqual(RecipeTemplateStatus.FutureGated, readiness.Status);
        Assert.IsTrue(readiness.BlockingIssues.Any(i => i.IssueId == "template-unknown-system"));
    }

    [TestMethod]
    public void AuditCleanupRuntimeBlockedToolGateStillPasses()
    {
        var tool = TrustedTool("tool.browser.runtime", RecipeToolCategory.BrowserRuntime) with
        {
            RuntimeStatus = RecipeToolRuntimeStatus.LiveBlocked,
            TrustLevel = RecipeToolTrustLevel.ApprovedForFixture
        };
        var readiness = RecipeToolTrustSecretsPolicy.EvaluateCredentialedAction(
            new RecipeCredentialedActionRequirement("gate.browser", tool.ToolId, ["secret.browser.ref"], [RecipeSecretScope.Tool], ApprovalNarrativeRequired: true, ApprovalNarrativePresent: true),
            new RecipeToolTrustRegistry([tool]),
            [Secret("secret.browser.ref", tool.ToolId)]);

        Assert.IsFalse(readiness.IsReady);
        Assert.AreEqual(RecipeCredentialedActionDecisionStatus.BlockedLiveRuntimeDisabled, readiness.Decision.Status);
    }

    [TestMethod]
    public void NoRealConnectorApiNetworkVaultBrowserDesktopSchedulerWatcherHookListenerRecorderReplayIsIntroduced()
    {
        var catalog = Catalog();
        var readiness = RecipeTemplateReadinessEvaluator.Evaluate(Find("excel.extract_rows_to_workitems"), ReadyContext());

        Assert.IsFalse(catalog.LiveRuntimeEnabled);
        Assert.IsFalse(catalog.ConnectorExecutionEnabled);
        Assert.IsFalse(readiness.LiveRuntimeEnabled);
        Assert.IsFalse(readiness.ConnectorExecutionEnabled);
        Assert.IsFalse(readiness.StartsRecipeRun);
        Assert.IsFalse(readiness.ProcessesWorkitems);
        Assert.IsFalse(catalog.Templates.Any(t => t.RawSecretValuesStored));
    }

    private static RecipeTemplateCatalog Catalog() => RecipeTemplateCatalogFactory.CreateGlobalLatamV1();

    private static RecipeTemplateDefinition Find(string id) =>
        Catalog().Templates.Single(t => t.TemplateId == id);

    private static RecipeTemplateReadinessContext ReadyContext(
        RecipeToolTrustRegistry? registry = null,
        IReadOnlyList<RecipeSecretRequirement>? secrets = null,
        IReadOnlyList<RecipeTemplateTriggerBinding>? triggerBindings = null,
        RecipeEvidencePack? evidencePack = null,
        IReadOnlyList<RecipeValidationEvidence>? validationEvidence = null,
        bool includeEvidencePack = true,
        bool rawSecretDetected = false)
    {
        var catalog = Catalog();
        return new(
            registry ?? Registry(),
            secrets ?? Secrets(),
            ConnectorEligibilities(catalog),
            triggerBindings ?? [],
            includeEvidencePack ? evidencePack ?? EvidencePack() : null,
            [new RecipeStepEvidenceResult(true, RecipeStepEvidenceStatus.Satisfied, [], [])],
            validationEvidence ?? [Validation(RecipeValidationEvidenceStatus.Passed, RecipeValidationSeverity.Blocking)],
            RecipeApprovalNarrativeFactory.Create("narrative.template", "recipe.template", "8.0.0", "run.template", RecipeHumanInterventionKind.PaymentConfirmationRequired),
            null,
            rawSecretDetected);
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
            "8.0.0",
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

    private static RecipeValidationEvidence Validation(RecipeValidationEvidenceStatus status, RecipeValidationSeverity severity) =>
        new("validation.evidence", RecipeValidationKind.EvidenceRefExists, "expected", "redacted actual", ["evidence.ref"], status, severity, RecipeEvidenceRedactionStatus.Applied);
}
