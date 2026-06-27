using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("RecipeProductSurfaceNavigationMessagingFinalPolish")]
public sealed class RecipeProductSurfaceNavigationMessagingFinalPolishTests
{
    private const string AllowedClaim = "NODAL OS has a fixture-safe Recipe Runtime product surface with read-only catalog, lab, templates, readiness explanations, operator previews and handoff/export preview summaries.";
    private const string ForbiddenClaim = "NODAL OS can execute/live automate these recipes.";

    [TestMethod]
    [TestCategory("RecipeProductSurfaceNavigationMessagingFinalPolish")]
    public void FinalNavigationMessagingLineSummaryIsReadOnlyAndAuditReady()
    {
        var surface = Surface();
        var composition = surface.FinalComposition;

        Assert.IsTrue(surface.ReadOnly);
        Assert.IsTrue(surface.PreviewSafe);
        Assert.IsTrue(surface.FixtureSafeOnly);
        Assert.AreEqual(RecipeProductSurfaceNavigationMessagingAuditReadinessStatus.ReadyForFinalAudit, composition.AuditReadinessStatus);
        Assert.AreEqual("COMPLETE_READ_ONLY_NAVIGATION_MESSAGING_CLOSED", composition.FinalLineStatus);
        Assert.IsTrue(composition.ReadOnly);
        Assert.IsTrue(composition.PreviewSafe);
        Assert.IsTrue(composition.FixtureSafeOnly);
        Assert.IsFalse(surface.CanStartRecipeRun);
        Assert.IsFalse(surface.CanProcessWorkitem);
        Assert.IsFalse(surface.CanEnableLiveRuntime);
        Assert.IsFalse(surface.CanOpenConnector);
        Assert.IsFalse(surface.CanRequestSecrets);
        Assert.IsFalse(surface.CanCallNetwork);
        Assert.IsFalse(surface.CanCreateSchedulerWatcherHookOrListener);
        Assert.IsFalse(surface.CanCreateRecorderReplayOrCapture);
        Assert.IsFalse(surface.CanWriteExportFile);
        Assert.IsFalse(surface.CanApplyLocatorRepair);
        Assert.IsFalse(surface.LiveRuntimeEnabled);
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceNavigationMessagingFinalPolish")]
    public void PhaseOneTaxonomyAndPhaseTwoDemoFlowAreRepresented()
    {
        var surface = Surface();

        Assert.AreEqual("NODAL_RECIPE_PRODUCT_SURFACE_NAVIGATION_MESSAGING_READ_ONLY", surface.Taxonomy.LineId);
        Assert.AreEqual("COMPLETE_READ_ONLY_PREVIEW_SAFE_FIXTURE_SAFE_PRODUCT_SURFACE_CLOSED", surface.Taxonomy.ClosedProductSurfaceStatus);
        Assert.AreEqual(Enum.GetValues<RecipeProductSurfaceNavigationEntryKind>().Length, surface.Taxonomy.NavigationLabels.Count);
        Assert.AreEqual(Enum.GetValues<RecipeProductSurfaceCapabilityBadgeKind>().Length, surface.Taxonomy.CapabilityBadges.Count);
        Assert.AreEqual(Enum.GetValues<RecipeProductSurfaceDisabledActionKind>().Length, surface.Taxonomy.DisabledActionMessages.Count);
        Assert.AreEqual(Enum.GetValues<RecipeProductSurfaceDemoFlowStepKind>().Length, surface.DemoFlow.Steps.Count);
        Assert.IsTrue(surface.DemoFlow.Microcopy.EmptyStates.Count >= 7);
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceNavigationMessagingFinalPolish")]
    public void FinalCopyConsistencySetContainsRequiredSafeLanguage()
    {
        var copy = Surface().FinalCopyConsistencySet;

        CollectionAssert.IsSubsetOf(
            new[]
            {
                "read-only",
                "preview-safe",
                "fixture-safe",
                "demo-safe",
                "live runtime blocked",
                "automation not enabled",
                "connector execution disabled",
                "secrets by reference only",
                "export preview only",
                "no real file generated",
                "no workitems processed",
                "safe next action: review readiness and prepare requirements"
            },
            copy.ToArray());
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceNavigationMessagingFinalPolish")]
    public void AuditReadinessMatrixCoversAllFinalAuditAreas()
    {
        var matrix = Surface().FinalComposition.AuditMatrix;

        CollectionAssert.AreEquivalent(
            Enum.GetValues<RecipeProductSurfaceNavigationMessagingAuditArea>(),
            matrix.Select(item => item.Area).ToArray());

        foreach (var item in matrix)
        {
            Assert.AreEqual(RecipeProductSurfaceNavigationMessagingAuditReadinessStatus.ReadyForFinalAudit, item.Status, item.Area.ToString());
            Assert.IsFalse(item.BlocksFinalAudit, item.Area.ToString());
            Assert.IsFalse(item.GrantsRuntimeCapability, item.Area.ToString());
            Assert.IsFalse(item.GrantsLiveRuntime, item.Area.ToString());
            Assert.IsFalse(string.IsNullOrWhiteSpace(item.RedactedSummary), item.Area.ToString());
            Assert.IsFalse(string.IsNullOrWhiteSpace(item.EvidenceSummary), item.Area.ToString());
        }
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceNavigationMessagingFinalPolish")]
    public void DisabledActionMessagingRemainsExplicitAndSafe()
    {
        foreach (var action in Surface().Taxonomy.DisabledActionMessages)
        {
            Assert.IsFalse(action.Available, action.Label);
            Assert.IsFalse(action.CanInvoke, action.Label);
            Assert.IsFalse(action.GrantsLiveRuntime, action.Label);
            Assert.IsFalse(action.CallsConnectorOrNetwork, action.Label);
            Assert.IsFalse(action.ReadsSecrets, action.Label);
            Assert.IsFalse(action.WritesExternalSystem, action.Label);
            Assert.IsFalse(action.WritesFile, action.Label);
            StringAssert.Contains(action.BlockedReason, "not enabled", action.Label);
            Assert.IsFalse(string.IsNullOrWhiteSpace(action.SafeNextAction), action.Label);
        }
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceNavigationMessagingFinalPolish")]
    public void AllowedClaimIsPresentAndForbiddenClaimIsNotProductFacing()
    {
        var surface = Surface();
        var copy = ProductFacingCopy(surface).ToArray();

        Assert.AreEqual(AllowedClaim, surface.FinalComposition.AllowedFinalClaim);
        Assert.AreEqual(ForbiddenClaim, surface.FinalComposition.ForbiddenFinalClaim);
        CollectionAssert.Contains(copy, AllowedClaim);
        CollectionAssert.DoesNotContain(copy, ForbiddenClaim);
        Assert.IsFalse(copy.Any(text => text.Contains("live automate", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceNavigationMessagingFinalPolish")]
    public void ProductFacingCopyAvoidsForbiddenLiveActionClaims()
    {
        var hits = RecipeProductSurfaceCopyPolicy.FindForbiddenCopy(ProductFacingCopy(Surface()));

        Assert.AreEqual(0, hits.Count, string.Join(Environment.NewLine, hits));
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceNavigationMessagingFinalPolish")]
    public void LiveActionTermsAppearOnlyInApprovedSafeCopyEntries()
    {
        var riskyTerms = new[]
        {
            "execution",
            "live runtime",
            "live automation",
            "automation",
            "connector",
            "API",
            "vault",
            "secret",
            "scheduler",
            "watcher",
            "hook",
            "listener",
            "recording",
            "playback",
            "capture",
            "workitem",
            "export",
            "mutation",
            "browser",
            "desktop"
        };

        foreach (var text in AllCopy(Surface()))
        {
            if (!riskyTerms.Any(term => text.Contains(term, StringComparison.OrdinalIgnoreCase)))
                continue;

            Assert.IsTrue(IsApprovedSafeRiskCopy(text), $"Risk term must be inside an approved safe copy entry: {text}");
        }
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceNavigationMessagingFinalPolish")]
    public void ForbiddenCopyPolicyScansAllGeneratedReadOnlyProductSurfaceCopy()
    {
        var copy = AllGeneratedProductSurfaceCopy().ToArray();

        Assert.IsTrue(copy.Any(text => text.Contains("Locator", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(copy.Any(text => text.Contains("capture", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(copy.Any(text => text.Contains("preview", StringComparison.OrdinalIgnoreCase)));

        var hits = RecipeProductSurfaceCopyPolicy.FindForbiddenCopy(copy);
        Assert.AreEqual(0, hits.Count, string.Join(Environment.NewLine, hits));
    }

    [TestMethod]
    [TestCategory("RecipeProductSurfaceNavigationMessagingFinalPolish")]
    public void NoConnectorApiVaultSchedulerRecorderExportWriteOrProtectedScopeCapabilitiesAreExposed()
    {
        var surface = Surface();
        var composition = surface.FinalComposition;

        Assert.IsFalse(composition.CanOpenConnector);
        Assert.IsFalse(composition.CanRequestSecrets);
        Assert.IsFalse(composition.CanCallNetwork);
        Assert.IsFalse(composition.CanCreateSchedulerWatcherHookOrListener);
        Assert.IsFalse(composition.CanCreateRecorderReplayOrCapture);
        Assert.IsFalse(composition.CanWriteExportFile);
        Assert.IsFalse(composition.CanApplyLocatorRepair);
        Assert.IsFalse(composition.LiveRuntimeEnabled);

        var productFacing = ProductFacingCopy(surface).ToArray();
        var enabledProtectedTerms = new[]
        {
            "CDP enabled",
            "Playwright enabled",
            "Selenium enabled",
            "Puppeteer enabled",
            "browser runtime enabled",
            "desktop runtime enabled",
            "connector enabled",
            "vault enabled",
            "real export enabled"
        };

        foreach (var marker in enabledProtectedTerms)
            Assert.IsFalse(productFacing.Any(text => text.Contains(marker, StringComparison.OrdinalIgnoreCase)), marker);
    }

    private static RecipeProductSurfaceNavigationMessagingFinalPolishSurface Surface() =>
        RecipeProductSurfaceFactory.CreateNavigationMessagingFinalPolishSurface();

    private static IEnumerable<string> ProductFacingCopy(RecipeProductSurfaceNavigationMessagingFinalPolishSurface surface)
    {
        yield return surface.FinalComposition.AllowedFinalClaim;
        yield return surface.FinalComposition.NavigationTaxonomyReadiness;
        yield return surface.FinalComposition.CapabilityBadgeReadiness;
        yield return surface.FinalComposition.DisabledActionMessagingReadiness;
        yield return surface.FinalComposition.DemoFlowCopyReadiness;
        yield return surface.FinalComposition.EmptyStateReadiness;
        yield return surface.FinalComposition.ProductClaimGuardrailReadiness;
        yield return surface.FinalComposition.AuditReadinessSummary;

        foreach (var item in surface.FinalComposition.AuditMatrix)
        {
            yield return item.RedactedSummary;
            yield return item.EvidenceSummary;
        }

        foreach (var copy in surface.FinalCopyConsistencySet)
            yield return copy;
    }

    private static IEnumerable<string> AllCopy(RecipeProductSurfaceNavigationMessagingFinalPolishSurface surface) =>
        ProductFacingCopy(surface)
            .Concat(surface.Taxonomy.NavigationLabels.SelectMany(label => new[] { label.Label, label.OperatorSummary, label.RouteHint }))
            .Concat(surface.Taxonomy.CapabilityBadges.SelectMany(badge => new[] { badge.Label, badge.RedactedSummary }))
            .Concat(surface.Taxonomy.DisabledActionMessages.SelectMany(action => new[] { action.Label, action.BlockedReason, action.SafeNextAction }))
            .Concat(surface.DemoFlow.Steps.SelectMany(step => new[] { step.Title, step.Subtitle, step.OperatorDescription, step.BlockedActionNote, step.SafeNextAction, step.ClaimGuardrailReminder }))
            .Concat(surface.DemoFlow.Microcopy.EmptyStates.SelectMany(state => new[] { state.Label, state.RedactedSummary, state.SafeNextAction }))
            .Concat(surface.DemoFlow.Microcopy.DisabledControlCopy)
            .Concat(surface.DemoFlow.Microcopy.StepTransitions);

    private static bool IsApprovedSafeRiskCopy(string text)
    {
        var approvedExactCopy = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Connector execution disabled",
            "Secrets by reference only",
            "Export preview only",
            "Handoff/Export Preview",
            "Review Handoff/Export Preview",
            "recipes/handoff-export-preview",
            "Live runtime blocked",
            "Not automated",
            "Recipe execution blocked",
            "Workitem processing blocked",
            "Connector/API blocked",
            "Vault/secrets blocked",
            "Browser automation blocked",
            "Desktop automation blocked",
            "Recording/playback/capture-draft blocked",
            "Export file generation blocked",
            "Fiscal/payment/marketplace/message/delete/write blocked",
            "Review connector eligibility and tool trust refs only.",
            "Review required secret aliases or refs by reference only.",
            "Review preview-only capture draft summaries.",
            "Review handoff/export preview metadata next.",
            "Review or copy the safe handoff summary text.",
            "Request human review path and keep the item blocked for live action.",
            "Secret values are never requested or shown.",
            "Browser, desktop, connector, vault, recorder, and external mutation paths are blocked.",
            "Review connector eligibility refs only.",
            "No live runtime available",
            "No connector connected",
            "No credentials requested",
            "No export file generated",
            "No workitems processed",
            "No browser or desktop automation performed",
            "Preview data only",
            "automation not enabled",
            "safe next action: review readiness and prepare requirements"
        };

        if (approvedExactCopy.Contains(text))
            return true;

        var approvedPhrasePatterns = new[]
        {
            "is not enabled",
            "are not enabled",
            "not available",
            "does not write",
            "remain unavailable",
            "remains unavailable",
            "remain not enabled",
            "remains disabled",
            "remains blocked",
            "remain blocked",
            "stays blocked",
            "stay blocked",
            "blocked live",
            "blocked runtime",
            "blocked for live mutation",
            "blocked-state",
            "blocked-state copy",
            "blocked claim",
            "blocked reason",
            "blocked reasons",
            "disabled control",
            "disabled actions",
            "unavailable action",
            "not automated",
            "no live",
            "no real",
            "no export",
            "no step",
            "no connector",
            "no credentials",
            "no browser",
            "no recording",
            "no automatic",
            "no workitems",
            "no protected",
            "read-only",
            "preview-only",
            "preview-safe",
            "fixture-safe",
            "by reference only",
            "metadata preview",
            "metadata only",
            "outside this closed messaging line",
            "outside the closed messaging line",
            "grant no capability",
            "return false",
            "flags return false",
            "excluded from product-facing copy",
            "cannot change",
            "cannot enable",
            "cannot call",
            "cannot start",
            "cannot process",
            "not runtime-ready",
            "without live behavior",
            "external final audit",
            "audit readiness",
            "copy policy tests",
            "Product copy and view models remain preview-only.",
            "Handoff/export remains metadata preview; no real file is generated."
        };

        return approvedPhrasePatterns.Any(pattern => text.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> AllGeneratedProductSurfaceCopy()
    {
        var catalog = RecipeTemplateCatalogFactory.CreateGlobalLatamV1();
        var context = ReadyContext(catalog);
        var labSnapshot = LabSnapshot();
        var catalogSurface = RecipeProductSurfaceFactory.CreateCatalogSurface(catalog, context);
        var labSurface = RecipeProductSurfaceFactory.CreateLabSurface(labSnapshot);
        var detailSurface = RecipeProductSurfaceFactory.CreateTemplateDetailSurface(catalog, "excel.extract_rows_to_workitems", context);
        var operatorSurface = RecipeProductSurfaceFactory.CreateOperatorPreviewHandoffExportSurface(catalog, "excel.extract_rows_to_workitems", context);
        var navigationSurface = Surface();

        foreach (var text in AllCopy(navigationSurface))
            yield return text;

        yield return catalogSurface.ViewModel.ProductSurfaceSummary;
        foreach (var text in catalogSurface.SafetyCopy)
            yield return text;
        foreach (var badge in catalogSurface.ViewModel.GlobalSafetyBadges)
        {
            yield return badge.Label;
            yield return badge.RedactedSummary;
        }
        foreach (var pack in catalogSurface.ViewModel.Packs)
        {
            yield return pack.PackName;
            yield return pack.SafetySummary;
            foreach (var card in pack.Templates)
            {
                yield return card.DisplayName;
                yield return card.Description;
                yield return card.ToolTrustSummary;
                yield return card.SecretRefSummary;
                yield return card.TriggerStatusSummary;
                yield return card.LiveRuntimeStatus;
                yield return card.SafeNextActionSummary;
                yield return card.NotIncludedSummary;
                foreach (var blocked in card.BlockingSummaries)
                    yield return blocked;
            }
        }

        yield return labSurface.ViewModel.SafetyBoundarySummary;
        yield return labSurface.ViewModel.EvidenceTimelineSummary;
        yield return labSurface.ViewModel.ApprovalHumanSummary;
        yield return labSurface.ViewModel.ToolTrustSecretSummary;
        yield return labSurface.ViewModel.TriggerObserveOnlySummary;
        yield return labSurface.ViewModel.LocatorRepairPreviewSummary;
        yield return labSurface.ViewModel.CaptureDraftSummary;
        foreach (var section in labSurface.ViewModel.Sections)
        {
            yield return section.Label;
            yield return section.RedactedSummary;
        }
        foreach (var cell in labSurface.ViewModel.Cells)
        {
            yield return cell.Label;
            yield return cell.RedactedSummary;
        }

        var detail = detailSurface.ViewModel;
        yield return detail.Header.DisplayName;
        yield return detail.Header.Description;
        yield return detail.Header.BusinessUseCaseSummary;
        yield return detail.SystemSummary.RedactedSummary;
        yield return detail.SystemSummary.ConnectorBoundarySummary;
        yield return detail.SystemSummary.RuntimeBoundarySummary;
        yield return detail.SystemSummary.HumanReviewSummary;
        yield return detail.SafetySummary.LiveBlockedExplanation;
        yield return detail.SafetySummary.NotIncludedSummary;
        yield return detail.ReadinessExplanation.OperatorVisibleSummary;
        yield return detail.ReadinessExplanation.SafeNextAction.RedactedSummary;
        yield return detail.ReadinessExplanation.ExplicitlyNotIncludedSummary;
        yield return detail.TriggerObserveOnlySummary;
        yield return detail.EvidenceValidationSummary;
        yield return detail.LocatorCaptureImplicationsSummary;
        yield return detail.OperatorVisibleSummary;
        foreach (var section in detail.Sections)
        {
            yield return section.Label;
            yield return section.RedactedSummary;
        }

        yield return operatorSurface.OperatorPreview.OperatorReviewSummary;
        yield return operatorSurface.OperatorPreview.RequiredApprovalsSummary;
        yield return operatorSurface.OperatorPreview.RequiredEvidenceSummary;
        yield return operatorSurface.OperatorPreview.BlockedLiveRuntimeExplanation;
        yield return operatorSurface.OperatorPreview.SafeNextAction.RedactedSummary;
        yield return operatorSurface.OperatorPreview.NotAutomatedSummary;
        yield return operatorSurface.OperatorPreview.SystemSpecificPreviewSummary;
        foreach (var section in operatorSurface.OperatorPreview.RequiredReviewSections)
        {
            yield return section.Label;
            yield return section.RedactedSummary;
        }
        foreach (var action in operatorSurface.OperatorPreview.DisabledActions)
        {
            yield return action.Label;
            yield return action.DisabledReason;
        }

        yield return operatorSurface.HandoffExportPreview.HandoffTitle;
        yield return operatorSurface.HandoffExportPreview.TemplateSummary;
        yield return operatorSurface.HandoffExportPreview.ReadinessSnapshot;
        yield return operatorSurface.HandoffExportPreview.ApprovalPathSummary;
        yield return operatorSurface.HandoffExportPreview.ToolTrustSummary;
        yield return operatorSurface.HandoffExportPreview.SecretReferencesSummary;
        yield return operatorSurface.HandoffExportPreview.LocatorCaptureImplications;
        yield return operatorSurface.HandoffExportPreview.TriggerObserveOnlySummary;
        yield return operatorSurface.HandoffExportPreview.ProductSafeCopy;
        foreach (var item in operatorSurface.HandoffExportPreview.NotIncludedNotAutomated)
            yield return item;
        foreach (var text in operatorSurface.SafetyCopy)
            yield return text;
    }

    private static RecipeTemplateReadinessContext ReadyContext(RecipeTemplateCatalog catalog) =>
        new(
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
