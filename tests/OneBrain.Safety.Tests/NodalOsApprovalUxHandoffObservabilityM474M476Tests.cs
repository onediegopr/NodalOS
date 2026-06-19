using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalUxHandoffObservability")]
[TestCategory("ApprovalTimelineEvidence")]
[TestCategory("CoreRuntimeRegistryEventBusRedaction")]
[TestCategory("NewTopicsIntake")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsApprovalUxHandoffObservabilityM474M476Tests
{
    private readonly NodalOsApprovalUxHandoffObservabilityValidator validator = new();
    private readonly NodalOsApprovalUxHandoffObservabilityJsonSerializer serializer = new();

    [TestMethod]
    public void ApprovalUxPreview_CreatesValidPreviewFromApprovalTimelineEvidenceContext()
    {
        var (entry, card, timeline) = Context();
        var preview = new NodalOsApprovalUxPreviewService().CreatePreview([card], [timeline]);

        Assert.IsTrue(validator.ValidateApprovalUxPreview(preview).IsValid);
        Assert.AreEqual("NODAL OS", preview.ProjectOperationalName);
        Assert.AreEqual(card.ApprovalCardId, preview.Cards.Single().ApprovalCardId);
        Assert.AreEqual(entry.RegistryEntryId, preview.Cards.Single().ExecutionRegistryEntryId);
    }

    [TestMethod]
    public void ApprovalCardPreview_ContainsRequiredHumanFieldsRiskResourcesAndOptions()
    {
        var (_, card, timeline) = Context();
        var preview = new NodalOsApprovalUxPreviewService().CreateCardPreview(card, [timeline]);

        Assert.IsFalse(string.IsNullOrWhiteSpace(preview.TitleRedacted));
        Assert.IsFalse(string.IsNullOrWhiteSpace(preview.ShortSummaryRedacted));
        Assert.IsFalse(string.IsNullOrWhiteSpace(preview.FullExplanationRedacted));
        Assert.AreEqual(NodalOsApprovalSeverity.High, preview.Severity);
        Assert.IsTrue(preview.AffectedResourcesRedacted.Count > 0);
        Assert.IsTrue(preview.UserOptions.Count > 0);
    }

    [TestMethod]
    public void ApprovalCardPreview_RepresentsAllExpectedUserOptions()
    {
        var (_, card, timeline) = Context();
        card = card with
        {
            UserOptions =
            [
                NodalOsApprovalUserOptionKind.Approve,
                NodalOsApprovalUserOptionKind.Reject,
                NodalOsApprovalUserOptionKind.RequestChanges,
                NodalOsApprovalUserOptionKind.RequestExplanation,
                NodalOsApprovalUserOptionKind.Defer,
                NodalOsApprovalUserOptionKind.HumanHandoffRequired
            ]
        };

        var preview = new NodalOsApprovalUxPreviewService().CreateCardPreview(card, [timeline]);

        Assert.IsTrue(preview.UserOptions.Contains(NodalOsApprovalUserOptionKind.Approve));
        Assert.IsTrue(preview.UserOptions.Contains(NodalOsApprovalUserOptionKind.Reject));
        Assert.IsTrue(preview.UserOptions.Contains(NodalOsApprovalUserOptionKind.RequestChanges));
        Assert.IsTrue(preview.UserOptions.Contains(NodalOsApprovalUserOptionKind.RequestExplanation));
        Assert.IsTrue(preview.UserOptions.Contains(NodalOsApprovalUserOptionKind.Defer));
        Assert.IsTrue(preview.UserOptions.Contains(NodalOsApprovalUserOptionKind.HumanHandoffRequired));
    }

    [TestMethod]
    public void ApprovalCardPreview_PreservesRegistryEventTimelineEvidenceRefs()
    {
        var (entry, card, timeline) = Context();
        var preview = new NodalOsApprovalUxPreviewService().CreateCardPreview(card, [timeline]);

        Assert.AreEqual(entry.RegistryEntryId, preview.ExecutionRegistryEntryId);
        Assert.AreEqual(card.EventId, preview.EventId);
        AssertContains(preview.TimelineEntryIds, timeline.TimelineEntryId);
        Assert.IsTrue(preview.EvidenceRefs.Count >= card.EvidenceRefs.Count);
    }

    [TestMethod]
    public void ApprovalCardPreview_IncludesRollbackOrNoRollbackReason()
    {
        var (_, card, timeline) = Context();
        var withRollback = new NodalOsApprovalUxPreviewService().CreateCardPreview(card, [timeline]);
        var withoutRollback = new NodalOsApprovalUxPreviewService().CreateCardPreview(card with { RollbackPlanRedacted = null }, [timeline]);

        Assert.IsTrue(withRollback.RollbackAvailable);
        Assert.IsFalse(withoutRollback.RollbackAvailable);
        Assert.IsFalse(string.IsNullOrWhiteSpace(withoutRollback.NoRollbackReasonRedacted));
    }

    [TestMethod]
    public void ApprovalCardPreview_CannotAuthorizeExecutionAndDoesNotMutateRegistry()
    {
        var (entry, card, timeline) = Context();
        var beforeState = entry.State;
        var beforeEvidenceCount = entry.EvidenceRefs.Count;
        var preview = new NodalOsApprovalUxPreviewService().CreateCardPreview(card, [timeline]);

        Assert.IsFalse(preview.CanAuthorizeExecution);
        Assert.IsFalse(preview.RuntimeExecutionAllowed);
        Assert.IsTrue(preview.RuntimeExecutionDeferred);
        Assert.AreEqual(beforeState, entry.State);
        Assert.AreEqual(beforeEvidenceCount, entry.EvidenceRefs.Count);
    }

    [TestMethod]
    public void ApprovalUxPreview_SerializedOutputDoesNotContainSecrets()
    {
        var preview = NodalOsApprovalUxHandoffObservabilityFixtures.ApprovalUxPreview() with
        {
            Cards =
            [
                NodalOsApprovalUxHandoffObservabilityFixtures.ApprovalCardPreview() with
                {
                    FullExplanationRedacted = "Authorization: Bearer abcdefghijklmnopqrstuvwxyz",
                    PolicyGateReasonRedacted = "api_key=raw-fixture-key",
                    AffectedResourcesRedacted = ["Cookie: session=abc123"]
                }
            ]
        };

        var json = serializer.SerializeApprovalUxPreview(preview);

        AssertContains(json, "[REDACTED]");
        AssertDoesNotContain(json, "abcdefghijklmnopqrstuvwxyz");
        AssertDoesNotContain(json, "raw-fixture-key");
        AssertDoesNotContain(json, "session=abc123");
    }

    [TestMethod]
    public void HandoffDataPack_CreatesPackWithRegistryTimelineApprovalEvidenceAndGuardrails()
    {
        var pack = NodalOsApprovalUxHandoffObservabilityFixtures.HandoffDataPack();

        Assert.IsTrue(validator.ValidateHandoffDataPack(pack).IsValid);
        Assert.IsTrue(pack.RegistryEntries.Count > 0);
        Assert.IsTrue(pack.TimelineEntries.Count > 0);
        Assert.IsTrue(pack.ApprovalPreviews.Count > 0);
        Assert.IsTrue(pack.EvidenceRefs.Count > 0);
        AssertContains(pack.GuardrailsSummaryRedacted, "No runtime");
    }

    [TestMethod]
    public void HandoffDataPack_AnswersRequestDecisionPendingItems()
    {
        var pack = NodalOsApprovalUxHandoffObservabilityFixtures.HandoffDataPack();

        AssertContains(pack.RequestedSummaryRedacted, "User requested");
        AssertContains(pack.DecisionSummaryRedacted, "runtime remains deferred");
        Assert.IsTrue(pack.PendingItemsRedacted.Count > 0);
        Assert.IsTrue(pack.NextStepsRedacted.Count > 0);
    }

    [TestMethod]
    public void HandoffDataPack_JsonAndMarkdownAreRedacted()
    {
        var pack = NodalOsApprovalUxHandoffObservabilityFixtures.HandoffDataPack() with
        {
            RequestedSummaryRedacted = "password=super-secret",
            WarningsRedacted = ["Authorization: Bearer abcdefghijklmnopqrstuvwxyz"]
        };
        var json = serializer.SerializeHandoffDataPack(pack);
        var markdown = new NodalOsHandoffDataPackService().RenderMarkdown(pack);

        AssertContains(json, "[REDACTED]");
        AssertContains(markdown, "[REDACTED]");
        AssertDoesNotContain(json, "super-secret");
        AssertDoesNotContain(markdown, "abcdefghijklmnopqrstuvwxyz");
    }

    [TestMethod]
    public void HandoffDataPack_EvidenceIsRefOnlyAndRejectsUnsafePayloadMarkers()
    {
        var pack = NodalOsApprovalUxHandoffObservabilityFixtures.HandoffDataPack();
        var inlineScreenshot = pack with { EvidenceRefs = [Evidence("screenshot", "data:image/png;base64,abcdef")] };
        var rawNetwork = pack with { EvidenceRefs = [Evidence("network", "Authorization: Bearer abc body raw")] };
        var rawDom = pack with { EvidenceRefs = [Evidence("dom", "<html><body>raw</body></html>")] };

        Assert.IsTrue(validator.ValidateHandoffDataPack(pack).IsValid);
        Assert.IsFalse(validator.ValidateHandoffDataPack(inlineScreenshot).IsValid);
        Assert.IsFalse(validator.ValidateHandoffDataPack(rawNetwork).IsValid);
        Assert.IsFalse(validator.ValidateHandoffDataPack(rawDom).IsValid);
    }

    [TestMethod]
    public void HandoffDataPack_RejectsRawHeaderCookieBodySecretFields()
    {
        var pack = NodalOsApprovalUxHandoffObservabilityFixtures.HandoffDataPack() with
        {
            WarningsRedacted =
            [
                "Authorization: Bearer abc",
                "Cookie: session=abc",
                "request body raw",
                "secret=raw"
            ]
        };

        Assert.IsFalse(validator.ValidateHandoffDataPack(pack).IsValid);
    }

    [TestMethod]
    public void HandoffDataPack_DoesNotRequireCloudOrMutateRegistry()
    {
        var pack = NodalOsApprovalUxHandoffObservabilityFixtures.HandoffDataPack();
        var beforeState = pack.RegistryEntries.Single().State;

        Assert.IsFalse(pack.CloudRequired);
        Assert.IsFalse(pack.RuntimeExecutionAllowed);
        Assert.IsTrue(pack.RuntimeExecutionDeferred);
        _ = new NodalOsHandoffDataPackService().RenderMarkdown(pack);
        Assert.AreEqual(beforeState, pack.RegistryEntries.Single().State);
    }

    [TestMethod]
    public void RuntimeObservabilityReport_GeneratesAllSummariesAndCorrelationIds()
    {
        var report = NodalOsApprovalUxHandoffObservabilityFixtures.RuntimeObservabilityReport();

        Assert.IsTrue(validator.ValidateRuntimeObservabilityReport(report).IsValid);
        Assert.IsTrue(report.RegistryEntryIds.Count > 0);
        Assert.IsTrue(report.EventIds.Count > 0);
        Assert.IsTrue(report.TimelineEntryIds.Count > 0);
        Assert.IsTrue(report.ApprovalCardIds.Count > 0);
        Assert.IsTrue(report.EvidenceIds.Count > 0);
        AssertContains(report.RedactionAppliedSummaryRedacted, "Common redaction");
    }

    [TestMethod]
    public void RuntimeObservabilityReport_IncludesBlockedWarningsFailuresAndHandoff()
    {
        var report = NodalOsApprovalUxHandoffObservabilityFixtures.RuntimeObservabilityReport();

        Assert.IsTrue(report.BlockedActionsRedacted.Count > 0);
        Assert.IsTrue(report.WarningsRedacted.Count > 0);
        Assert.IsTrue(report.HumanHandoffRequirementsRedacted.Count > 0);
        Assert.IsNotNull(report.FailuresRedacted);
    }

    [TestMethod]
    public void RuntimeObservabilityReport_JsonAndTextAreRedacted()
    {
        var report = NodalOsApprovalUxHandoffObservabilityFixtures.RuntimeObservabilityReport() with
        {
            UserRequestRedacted = "refresh_token=raw-refresh-token",
            FailuresRedacted = ["Set-Cookie: session=abc123"]
        };
        var json = serializer.SerializeRuntimeObservabilityReport(report);
        var text = new NodalOsRuntimeObservabilityReportService().RenderText(report);

        AssertContains(json, "[REDACTED]");
        AssertContains(text, "[REDACTED]");
        AssertDoesNotContain(json, "raw-refresh-token");
        AssertDoesNotContain(text, "session=abc123");
    }

    [TestMethod]
    public void RuntimeObservabilityReport_DoesNotExecuteMutateOrRequireExternalSurfaces()
    {
        var report = NodalOsApprovalUxHandoffObservabilityFixtures.RuntimeObservabilityReport();

        Assert.IsFalse(report.RuntimeExecutionAllowed);
        Assert.IsTrue(report.RuntimeExecutionDeferred);
        Assert.IsFalse(report.UiRequired);
        Assert.IsFalse(report.CloudRequired);
        Assert.IsFalse(report.LlmProviderCallRequired);
    }

    [TestMethod]
    public void CrossLayer_PreviewHandoffObservabilityPreserveIdsWithoutDivergence()
    {
        var (entry, card, timeline) = Context();
        var preview = new NodalOsApprovalUxPreviewService().CreateCardPreview(card, [timeline]);
        var pack = new NodalOsHandoffDataPackService().CreateDataPack(
            [entry],
            [preview],
            [NodalOsApprovalTimelineEvidenceFixtures.ApprovalDecision()],
            [timeline],
            "Request",
            "Decision");
        var eventId = card.EventId ?? throw new InvalidOperationException("Expected event id.");
        var report = new NodalOsRuntimeObservabilityReportService().CreateReport(
            "Request",
            "Interpretation",
            [entry],
            [NodalOsCoreRuntimeFixtures.CoreEvent(NodalOsCoreEventKind.ApprovalRequired, entry.RegistryEntryId, entry.RequestId) with { EventId = eventId }],
            [timeline],
            [preview],
            pack.EvidenceRefs);

        AssertContains(pack.RegistryEntries.Select(e => e.RegistryEntryId), entry.RegistryEntryId);
        AssertContains(pack.ApprovalPreviews.Select(p => p.ApprovalCardId), card.ApprovalCardId);
        AssertContains(report.RegistryEntryIds, entry.RegistryEntryId);
        AssertContains(report.ApprovalCardIds, card.ApprovalCardId);
    }

    [TestMethod]
    public void CrossLayer_RedactionConsistentAndRefsNotDuplicatedConflictively()
    {
        var pack = NodalOsApprovalUxHandoffObservabilityFixtures.HandoffDataPack();
        var json = serializer.SerializeHandoffDataPack(pack with { RequestedSummaryRedacted = "api_key=raw-fixture-key" });

        AssertContains(json, "[REDACTED]");
        Assert.AreEqual(pack.EvidenceRefs.Select(e => e.EvidenceId).Distinct(StringComparer.Ordinal).Count(), pack.EvidenceRefs.Count);
    }

    [TestMethod]
    public void DependencyDirection_ContractsDoNotDependOnCore()
    {
        var contracts = File.ReadAllText(Path.Combine(RepoRoot(), "src", "OneBrain.AgentOperations.Contracts", "OneBrain.AgentOperations.Contracts.csproj"));
        var contractSources = string.Join(Environment.NewLine, Directory.GetFiles(
                Path.Combine(RepoRoot(), "src", "OneBrain.AgentOperations.Contracts"),
                "*.cs",
                SearchOption.AllDirectories)
            .Select(File.ReadAllText));

        AssertDoesNotContain(contracts, "OneBrain.AgentOperations.Core");
        AssertDoesNotContain(contractSources, "using OneBrain.AgentOperations.Core");
    }

    [TestMethod]
    public void NamingAndGuardrails_ArePreserved()
    {
        var artifact = File.ReadAllText(ArtifactPath());
        var serviceText = File.ReadAllText(Path.Combine(RepoRoot(), "src", "OneBrain.AgentOperations.Core", "NodalOsApprovalUxHandoffObservabilityServices.cs"));

        AssertContains(artifact, "\"projectOperationalName\": \"NODAL OS\"");
        AssertDoesNotContain(artifact, "\"projectOperationalName\": \"NEXA\"");
        AssertContains(artifact, "\"uiIntroduced\": false");
        AssertContains(artifact, "\"cloudIntroduced\": false");
        AssertContains(artifact, "\"llmProviderCallsIntroduced\": false");
        AssertContains(artifact, "\"schedulerOrWorkerIntroduced\": false");
        AssertContains(artifact, "\"browserAutomationIntroduced\": false");
        AssertContains(artifact, "\"recorderOrReplayIntroduced\": false");
        AssertContains(artifact, "\"queueIntroduced\": false");
        AssertContains(artifact, "\"dslParserRuntimeIntroduced\": false");
        AssertDoesNotContain(serviceText, "ProcessStartInfo");
        AssertDoesNotContain(serviceText, "Process.Start");
        AssertDoesNotContain(serviceText, "cmd.exe");
    }

    [TestMethod]
    public void RoadmapsReferenceM474M476AndAuditA()
    {
        var vnext = File.ReadAllText(Path.Combine(RepoRoot(), "docs", "roadmap", "nodal-os-roadmap-vnext.md"));
        var unified = File.ReadAllText(Path.Combine(RepoRoot(), "docs", "roadmap", "nodal-os-unified-roadmap-post-pause.md"));

        AssertContains(vnext, "M474-M476 Approval Center UX Contract Preview + Export/Handoff Data Pack + Runtime Observability Report");
        AssertContains(vnext, "AUDIT-A");
        AssertContains(unified, "M474-M476");
        AssertContains(unified, "AUDIT-A");
        AssertContains(unified, "before UI real");
    }

    private static (NodalOsExecutionRegistryEntry Entry, NodalOsApprovalCard Card, NodalOsTimelineEntry Timeline) Context()
    {
        var registry = new NodalOsExecutionRegistry();
        var request = NodalOsCoreRuntimeFixtures.ExecutionRequest();
        var entry = registry.Register(request);
        var coreEvent = NodalOsCoreRuntimeFixtures.CoreEvent(NodalOsCoreEventKind.ApprovalRequired, entry.RegistryEntryId, entry.RequestId);
        var card = new NodalOsApprovalCenterService().CreateApprovalCard(
            entry,
            coreEvent,
            NodalOsApprovalSeverity.High,
            NodalOsApprovalActionKind.SubmitFuture,
            "Human approval is required before any future submit action.",
            "Policy gate requires explicit approval for future mutable action.",
            ["resource://future-submit-target"]);
        var timeline = new NodalOsTimelineProjectionService().ProjectEvent(coreEvent) with
        {
            ApprovalCardId = card.ApprovalCardId
        };

        return (entry, card, timeline);
    }

    private static NodalOsEvidenceBridgeRef Evidence(string kind, string reference) =>
        new()
        {
            EvidenceId = $"evidence-{Guid.NewGuid():N}",
            Kind = kind,
            Ref = reference,
            Hash = "sha256:unsafe-fixture",
            SourceKind = NodalOsEvidenceBridgeSourceKind.AgentOperation,
            UseKind = NodalOsEvidenceBridgeUseKind.AuditTrail,
            Authority = NodalOsEvidenceBridgeAuthority.NoAuthority,
            Sensitivity = NodalOsEvidenceSensitivity.NonSensitive,
            RedactionState = NodalOsEvidenceRedactionState.NotRequired,
            Provenance = "NODAL OS:M474M476:Test",
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static string ArtifactPath() =>
        Path.Combine(RepoRoot(), "artifacts", "agent-operations", "m476", "approval-ux-handoff-observability-summary.json");

    private static void AssertContains(string text, string expected) =>
        Assert.IsTrue(text.Contains(expected, StringComparison.Ordinal), expected);

    private static void AssertContains(IEnumerable<string> values, string expected) =>
        Assert.IsTrue(values.Any(value => string.Equals(value, expected, StringComparison.Ordinal) || value.Contains(expected, StringComparison.Ordinal)), expected);

    private static void AssertDoesNotContain(string text, string unexpected) =>
        Assert.IsFalse(text.Contains(unexpected, StringComparison.Ordinal), unexpected);

    private static string RepoRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (File.Exists(Path.Combine(current, "OneBrain.slnx")))
                return current;

            current = Directory.GetParent(current)?.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
