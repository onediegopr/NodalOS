using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("RecipeRuntimeFinalAuditHardening")]
public sealed class RecipeRuntimeFinalAuditHardeningTests
{
    [TestMethod]
    [TestCategory("RecipeRuntimeFinalAuditHardening")]
    public void RecipeRuntimeCoreScopeContainsNoExecutorLikePrimitives()
    {
        var recipeSourceRoot = Path.Combine(RepoRoot(), "src", "OneBrain.Core", "Recipes");
        Assert.IsTrue(Directory.Exists(recipeSourceRoot), recipeSourceRoot);

        var forbidden = new Dictionary<string, Regex>
        {
            ["DllImport"] = new(@"\[DllImport", RegexOptions.Compiled),
            ["SendInput"] = new(@"\bSendInput\s*\(", RegexOptions.Compiled),
            ["SetCursorPos"] = new(@"\bSetCursorPos\s*\(", RegexOptions.Compiled),
            ["SetForegroundWindow"] = new(@"\bSetForegroundWindow\s*\(", RegexOptions.Compiled),
            ["PostMessage"] = new(@"\bPostMessage\s*\(", RegexOptions.Compiled),
            ["SendMessage"] = new(@"\bSendMessage\s*\(", RegexOptions.Compiled),
            ["ClipboardApi"] = new(@"\bClipboard\.", RegexOptions.Compiled),
            ["FlaUI"] = new(@"\bFlaUI\b", RegexOptions.Compiled),
            ["AutomationEventHandler"] = new(@"\bAddAutomationEventHandler\s*\(", RegexOptions.Compiled),
            ["CopyFromScreen"] = new(@"\bGraphics\.CopyFromScreen\s*\(", RegexOptions.Compiled),
            ["ProcessStart"] = new(@"\bProcess\.Start\s*\(", RegexOptions.Compiled),
            ["ProcessStartInfo"] = new(@"\bProcessStartInfo\b", RegexOptions.Compiled),
            ["FileSystemWatcher"] = new(@"\bFileSystemWatcher\b", RegexOptions.Compiled),
            ["RegisterHotKey"] = new(@"\bRegisterHotKey\s*\(", RegexOptions.Compiled),
            ["SetWindowsHookEx"] = new(@"\bSetWindowsHookEx\s*\(", RegexOptions.Compiled),
            ["ChromeDebugger"] = new(@"chrome\.debugger", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            ["Playwright"] = new(@"\bPlaywright\b", RegexOptions.Compiled),
            ["Selenium"] = new(@"\bSelenium\b", RegexOptions.Compiled),
            ["Puppeteer"] = new(@"\bPuppeteer\b", RegexOptions.Compiled),
            ["HttpClient"] = new(@"\bHttpClient\b", RegexOptions.Compiled),
            ["WebRequest"] = new(@"\bWebRequest\b", RegexOptions.Compiled),
            ["TcpListener"] = new(@"\bTcpListener\b", RegexOptions.Compiled),
            ["UdpClient"] = new(@"\bUdpClient\b", RegexOptions.Compiled),
            ["Socket"] = new(@"\bSocket\b", RegexOptions.Compiled),
            ["WebSocket"] = new(@"\bWebSocket\b", RegexOptions.Compiled),
            ["EnvironmentSecretRead"] = new(@"\bEnvironment\.GetEnvironmentVariable\s*\(", RegexOptions.Compiled)
        };

        var hits = new List<string>();
        foreach (var file in Directory.EnumerateFiles(recipeSourceRoot, "*.cs", SearchOption.TopDirectoryOnly))
        {
            var text = File.ReadAllText(file);
            foreach (var (name, pattern) in forbidden)
            {
                if (pattern.IsMatch(text))
                    hits.Add($"{name}: {Path.GetFileName(file)}");
            }
        }

        Assert.AreEqual(0, hits.Count, "Recipe Runtime contracts must remain executor-free: " + string.Join(", ", hits));
    }

    [TestMethod]
    [TestCategory("RecipeRuntimeFinalAuditHardening")]
    public void CompositeTemplateReadinessAndComposesAllFinalAuditGates()
    {
        var excel = Find("excel.extract_rows_to_workitems");
        var browser = Find("browser.table_extract_preview");
        var sap = Find("sap.purchase_order_status_check");

        var missingLimits = RecipeTemplateReadinessEvaluator.Evaluate(
            excel with { RecipeDefinition = excel.RecipeDefinition with { RunLimits = null } },
            ReadyContext());
        var missingTool = RecipeTemplateReadinessEvaluator.Evaluate(sap, ReadyContext(registry: new RecipeToolTrustRegistry([])));
        var missingSecret = RecipeTemplateReadinessEvaluator.Evaluate(sap, ReadyContext(secrets: []));
        var rawSecret = RecipeTemplateReadinessEvaluator.Evaluate(sap, ReadyContext(rawSecretDetected: true));
        var liveBlockedTool = RecipeTemplateReadinessEvaluator.Evaluate(browser, ReadyContext());
        var connectorBlocked = RecipeTemplateReadinessEvaluator.Evaluate(sap, ReadyContext());
        var triggerAutorun = RecipeTemplateReadinessEvaluator.Evaluate(excel, ReadyContext(triggerBindings: [AutoRunTriggerBinding()]));
        var missingEvidence = RecipeTemplateReadinessEvaluator.Evaluate(excel, ReadyContext(includeEvidencePack: false));
        var failedValidation = RecipeTemplateReadinessEvaluator.Evaluate(excel, ReadyContext(validationEvidence: [Validation(RecipeValidationEvidenceStatus.Failed, RecipeValidationSeverity.Blocking)]));
        var missingApproval = RecipeTemplateReadinessEvaluator.Evaluate(
            Find("excel.bank_statement_reconciliation_preview"),
            ReadyContext(includeApprovalNarrative: false));
        var labSafe = RecipeTemplateReadinessEvaluator.Evaluate(excel, ReadyContext(labSnapshot: LabSnapshot()));

        AssertBlockedBy(missingLimits, "missing-limits");
        AssertBlockedBy(missingTool, "template-missing-tool-trust");
        AssertBlockedBy(missingSecret, "template-missing-secret-ref");
        AssertBlockedBy(rawSecret, "template-context-raw-secret-detected");
        AssertBlockedBy(liveBlockedTool, "template-tool-live-blocked");
        Assert.IsTrue(connectorBlocked.BlockingIssues.Any(i => i.Status == RecipeReadinessStatus.BlockedLiveRuntimeDisabled));
        AssertBlockedBy(triggerAutorun, "trigger-autorun-blocked");
        AssertBlockedBy(missingEvidence, "template-missing-evidence-pack");
        AssertBlockedBy(failedValidation, "template-blocking-validation-failed");
        AssertBlockedBy(missingApproval, "template-missing-approval-narrative");
        Assert.IsTrue(labSafe.IsReady);
        Assert.IsFalse(labSafe.LiveRuntimeEnabled);
        Assert.IsFalse(labSafe.ActionAuthorityGranted);
    }

    [TestMethod]
    [TestCategory("RecipeRuntimeFinalAuditHardening")]
    public void CaptureTemplateMappingCannotBypassCompositeReadiness()
    {
        var catalog = Catalog();
        var session = new RecipeCaptureSession(
            "capture.browser.fixture",
            "Browser portal fixture capture",
            "Manual description only",
            RecipeTemplateCategory.GenericBrowserPortal,
            RecipeTemplateSystem.GenericBrowserPortal,
            RecipeTemplateRegion.Global,
            [RecipeTemplateCountry.Global],
            RecipeCaptureMode.ManualDescriptionOnly,
            RecipeCaptureSafetyStatus.SafeForDraft,
            new RecipeCaptureReadiness(true, false, RecipeCaptureSessionStatus.Draft, [], [], "Draft only"),
            "Map observed portal steps to a blocked template candidate.",
            [new RecipeCapturedStepRef("step.fixture")],
            [new RecipeCapturedEvidenceRef("evidence.fixture", RecipeEvidenceSourceKind.ExtractedDataRef)],
            [],
            [],
            null,
            [],
            "redaction.summary",
            ["timeline.fixture"],
            []);

        var mapping = RecipeCaptureTemplateMapper.MapToTemplate(session, catalog, ReadyContext());
        var draft = new RecipeDraft(
            "draft.browser.fixture",
            "Browser portal fixture draft",
            session.CaptureSessionId,
            [],
            [],
            [new RecipeDraftValidationSuggestion("validation.fixture", RecipeValidationKind.EvidenceRefExists, RecipeValidationSeverity.Blocking, "step.fixture")],
            [new RecipeDraftEvidenceSuggestion("evidence.suggestion", RecipeEvidenceSourceKind.ExtractedDataRef, "step.fixture")],
            [],
            [mapping],
            RecipeRiskLevel.High,
            [],
            [],
            new RecipeSafeNextAction(RecipeSafeNextActionKind.KeepBlocked, "Keep draft blocked until composite readiness passes."));

        var readiness = RecipeCaptureSafetyPolicy.EvaluateDraft(draft);

        Assert.IsNotNull(mapping.TemplateReadiness);
        Assert.IsFalse(mapping.TemplateReadiness!.IsReady);
        Assert.IsFalse(mapping.CanOverrideCompositeReadiness);
        Assert.IsFalse(mapping.LiveRuntimeEnabled);
        Assert.IsFalse(readiness.IsRunReady);
        Assert.IsTrue(readiness.BlockingIssues.Any(i => i.IssueId == "capture-template-composite-readiness-blocked"));
        Assert.IsFalse(readiness.LiveRuntimeEnabled);
        Assert.IsFalse(readiness.ActionAuthorityGranted);
    }

    private static void AssertBlockedBy(RecipeTemplateReadiness readiness, string issueId)
    {
        Assert.IsFalse(readiness.IsReady, issueId);
        Assert.IsTrue(readiness.BlockingIssues.Any(i => i.IssueId == issueId), issueId);
        Assert.IsFalse(readiness.LiveRuntimeEnabled, issueId);
        Assert.IsFalse(readiness.ActionAuthorityGranted, issueId);
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
        RecipeApprovalNarrative? approvalNarrative = null,
        RecipeLabSnapshot? labSnapshot = null,
        bool includeEvidencePack = true,
        bool includeApprovalNarrative = true,
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
            includeApprovalNarrative
                ? approvalNarrative ?? RecipeApprovalNarrativeFactory.Create("narrative.template", "recipe.template", "final", "run.template", RecipeHumanInterventionKind.PaymentConfirmationRequired)
                : null,
            labSnapshot,
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
            new RecipeConnectorTrustRequirement(
                template.RequiredToolTrustRefs.First(),
                template.RequiredSecretRefs,
                ApprovalRequired: RecipeToolTrustSecretsPolicy.RequiresApproval(action) || template.SafetyProfile.RequiresHumanApproval,
                EvidencePolicyRequired: true),
            ApprovalPolicyPresent: true,
            EvidencePolicyPresent: true);
    }

    private static RecipeTemplateTriggerBinding AutoRunTriggerBinding()
    {
        var trigger = new RecipeTriggerDefinition(
            "trigger.autorun",
            RecipeTriggerKind.ScheduleFuture,
            new RecipeDetectorRef("detector.autorun"),
            "recipe.fixture",
            "final",
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

        return new(trigger, detector, policy);
    }

    private static RecipeEvidencePack EvidencePack() =>
        new(
            "pack.template",
            "recipe.template",
            "final",
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

    private static RecipeLabSnapshot LabSnapshot() =>
        new(
            "lab.snapshot",
            "recipe.template",
            "final",
            "Template Lab",
            "ExcelMicrosoft365",
            "Excel",
            "Global",
            RecipeRunMode.FixtureRun,
            new RecipeLabReadinessSummary(RecipeReadinessStatus.ReadyForFixtureRun, true, [], [], "RecipePolicyPreflightEvaluator"),
            "limits",
            "complete",
            "terminate",
            "validation",
            "risk",
            "deterministic",
            "evidence",
            "timeline",
            "approval",
            new RecipeLabCapabilitySummary(["cap.fixture"], ["tool.excel.fixture"], [], [], []),
            "triggers",
            "workitems",
            "locator",
            new RecipeLabSafeNextAction(RecipeSafeNextActionKind.KeepBlocked, "Inspect only."),
            ["live runtime remains blocked"],
            "redacted and reference-only",
            new RecipeLabOperatorSummary("Read-only lab summary.", "fixture-safe", ["raw payloads", "secret values"]),
            new RecipeLabViewModel(
                "view.lab",
                [new RecipeLabSection("section.readiness", "Readiness", RecipeLabSectionStatus.Ready, "Ready for fixture inspection.", ["recipe.template"])],
                new RecipeLabOperatorSummary("Read-only lab summary.", "fixture-safe", ["raw payloads", "secret values"])));

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
            dir = dir.Parent;

        Assert.IsNotNull(dir);
        return dir.FullName;
    }
}
