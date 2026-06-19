using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("ApprovalTimelineEvidence")]
[TestCategory("CoreRuntimeRegistryEventBusRedaction")]
[TestCategory("NewTopicsIntake")]
[TestCategory("NamingCleanup")]
public sealed class NodalOsApprovalTimelineEvidenceM471M473Tests
{
    private readonly NodalOsApprovalTimelineEvidenceValidator validator = new();
    private readonly NodalOsApprovalTimelineEvidenceJsonSerializer serializer = new();

    [TestMethod]
    public void ApprovalCard_CreatedFromRegistryEventPolicyContext_IsValid()
    {
        var (entry, coreEvent) = RegistryAndEvent();
        var card = new NodalOsApprovalCenterService().CreateApprovalCard(
            entry,
            coreEvent,
            NodalOsApprovalSeverity.High,
            NodalOsApprovalActionKind.SubmitFuture,
            "User is asked to approve future submit action.",
            "Policy gate requires explicit approval.",
            ["resource://target-record"]);

        Assert.IsTrue(validator.ValidateApprovalCard(card).IsValid);
        Assert.AreEqual(entry.RegistryEntryId, card.ExecutionRegistryEntryId);
        Assert.AreEqual(coreEvent.EventId, card.EventId);
    }

    [TestMethod]
    public void ApprovalCard_RequiresHumanExplanation()
    {
        var result = validator.ValidateApprovalCard(NodalOsApprovalTimelineEvidenceFixtures.ApprovalCard() with
        {
            HumanExplanationRedacted = string.Empty
        });

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "Human explanation is required.");
    }

    [TestMethod]
    public void ApprovalCard_RequiresRiskSeverityAndAffectedResourcesOrExplicitNone()
    {
        var missingResources = NodalOsApprovalTimelineEvidenceFixtures.ApprovalCard() with
        {
            AffectedResourcesRedacted = [],
            NoAffectedResourcesReasonRedacted = null
        };
        var explicitNone = missingResources with
        {
            NoAffectedResourcesReasonRedacted = "No external resource is touched by this observation-only decision.",
            RequestedAction = NodalOsApprovalActionKind.Observation,
            Severity = NodalOsApprovalSeverity.Low
        };

        Assert.IsFalse(validator.ValidateApprovalCard(missingResources).IsValid);
        Assert.IsTrue(validator.ValidateApprovalCard(explicitNone).IsValid);
        Assert.AreEqual(NodalOsApprovalSeverity.Low, explicitNone.Severity);
    }

    [TestMethod]
    public void ApprovalCard_RequiresUserOptions()
    {
        var result = validator.ValidateApprovalCard(NodalOsApprovalTimelineEvidenceFixtures.ApprovalCard() with
        {
            UserOptions = []
        });

        Assert.IsFalse(result.IsValid);
        AssertContains(result.Errors, "Approval card requires user options.");
    }

    [TestMethod]
    public void ApprovalDecision_AllExpectedDecisionKindsAreModelable()
    {
        var kinds = new[]
        {
            NodalOsApprovalDecisionKind.Approve,
            NodalOsApprovalDecisionKind.Reject,
            NodalOsApprovalDecisionKind.RequestChanges,
            NodalOsApprovalDecisionKind.RequestExplanation,
            NodalOsApprovalDecisionKind.Defer,
            NodalOsApprovalDecisionKind.HumanHandoffRequired
        };

        foreach (var kind in kinds)
            Assert.IsTrue(validator.ValidateApprovalDecision(NodalOsApprovalTimelineEvidenceFixtures.ApprovalDecision(kind)).IsValid, kind.ToString());
    }

    [TestMethod]
    public void ApprovalDecision_DoesNotExecuteOrAuthorizeActions()
    {
        var decision = NodalOsApprovalTimelineEvidenceFixtures.ApprovalDecision();

        Assert.IsFalse(decision.RuntimeExecutionAllowed);
        Assert.IsFalse(decision.CanAuthorizeExecution);
        Assert.IsTrue(decision.RuntimeExecutionDeferred);
        Assert.IsTrue(validator.ValidateApprovalDecision(decision).IsValid);
    }

    [TestMethod]
    public void ApprovalCard_SerializedOutputDoesNotContainSecrets()
    {
        var card = NodalOsApprovalTimelineEvidenceFixtures.ApprovalCard() with
        {
            HumanExplanationRedacted = "Authorization: Bearer abcdefghijklmnopqrstuvwxyz",
            PolicyGateReasonRedacted = "api_key=raw-fixture-key",
            AffectedResourcesRedacted = ["Cookie: session=abc123"]
        };
        var json = serializer.SerializeApprovalCard(card);

        AssertContains(json, "[REDACTED]");
        AssertDoesNotContain(json, "abcdefghijklmnopqrstuvwxyz");
        AssertDoesNotContain(json, "raw-fixture-key");
        AssertDoesNotContain(json, "session=abc123");
    }

    [TestMethod]
    public void ApprovalCard_PolicyReasonSecretIsRejectedOrRedacted()
    {
        var invalid = NodalOsApprovalTimelineEvidenceFixtures.ApprovalCard() with
        {
            PolicyGateReasonRedacted = "password=super-secret"
        };
        var result = validator.ValidateApprovalCard(invalid);
        var json = serializer.SerializeApprovalCard(invalid);

        Assert.IsFalse(result.IsValid);
        AssertContains(json, "[REDACTED]");
        AssertDoesNotContain(json, "super-secret");
    }

    [TestMethod]
    public void ApprovalCard_EvidenceRefsAreConserved()
    {
        var card = NodalOsApprovalTimelineEvidenceFixtures.ApprovalCard();

        Assert.AreEqual(1, card.EvidenceRefs.Count);
        Assert.AreEqual(NodalOsEvidenceBridgeAuthority.NoAuthority, card.EvidenceRefs[0].Authority);
    }

    [TestMethod]
    public void ApprovalCard_RawSecretCookieHeaderBodyEvidenceRejected()
    {
        var badRefs = new[]
        {
            NodalOsApprovalTimelineEvidenceFixtures.EvidenceRef(reference: "Bearer abcdefghijklmnopqrstuvwxyz"),
            NodalOsApprovalTimelineEvidenceFixtures.EvidenceRef(reference: "Cookie: session=abc123"),
            NodalOsApprovalTimelineEvidenceFixtures.EvidenceRef(reference: "Authorization: Basic abcdefghijkl"),
            NodalOsApprovalTimelineEvidenceFixtures.EvidenceRef(kind: "network-metadata", reference: "body private payload")
        };

        foreach (var evidenceRef in badRefs)
        {
            var result = validator.ValidateApprovalCard(NodalOsApprovalTimelineEvidenceFixtures.ApprovalCard() with
            {
                EvidenceRefs = [evidenceRef]
            });

            Assert.IsFalse(result.IsValid, evidenceRef.Ref ?? evidenceRef.Kind);
        }
    }

    [TestMethod]
    public void Timeline_ProjectsAllRequiredEvents()
    {
        var service = new NodalOsTimelineProjectionService();
        var kinds = new[]
        {
            NodalOsCoreEventKind.ExecutionRequestRegistered,
            NodalOsCoreEventKind.PolicyGateEvaluated,
            NodalOsCoreEventKind.ApprovalRequired,
            NodalOsCoreEventKind.ApprovalGranted,
            NodalOsCoreEventKind.ApprovalRejected,
            NodalOsCoreEventKind.DryRunPlanCreated,
            NodalOsCoreEventKind.ExecutionCompleted,
            NodalOsCoreEventKind.ExecutionFailed,
            NodalOsCoreEventKind.EvidenceAttached,
            NodalOsCoreEventKind.WarningRaised,
            NodalOsCoreEventKind.HumanHandoffRequired,
            NodalOsCoreEventKind.RedactionApplied
        };

        foreach (var kind in kinds)
        {
            var entry = service.ProjectEvent(NodalOsCoreRuntimeFixtures.CoreEvent(kind));
            Assert.IsTrue(validator.ValidateTimelineEntry(entry).IsValid, kind.ToString());
            Assert.AreEqual(kind, entry.SourceEventKind);
        }
    }

    [TestMethod]
    public void Timeline_ConservesOrdering()
    {
        var service = new NodalOsTimelineProjectionService();
        var later = NodalOsCoreRuntimeFixtures.CoreEvent(NodalOsCoreEventKind.ExecutionCompleted) with
        {
            CreatedAt = DateTimeOffset.Parse("2026-01-01T00:00:02Z")
        };
        var earlier = NodalOsCoreRuntimeFixtures.CoreEvent(NodalOsCoreEventKind.ExecutionRequestRegistered) with
        {
            CreatedAt = DateTimeOffset.Parse("2026-01-01T00:00:01Z")
        };

        var entries = service.ProjectEvents([later, earlier]);

        Assert.AreEqual(earlier.EventId, entries[0].EventId);
        Assert.AreEqual(later.EventId, entries[1].EventId);
    }

    [TestMethod]
    public void Timeline_ConservesSafeEvidenceRefsAndRegistryApprovalLinks()
    {
        var approvalId = "approval-card-123";
        var decisionId = "approval-decision-123";
        var coreEvent = NodalOsCoreRuntimeFixtures.CoreEvent(NodalOsCoreEventKind.ApprovalGranted) with
        {
            MetadataRedacted = new Dictionary<string, string>
            {
                ["approvalCardId"] = approvalId,
                ["approvalDecisionId"] = decisionId
            }
        };

        var entry = new NodalOsTimelineProjectionService().ProjectEvent(coreEvent);

        Assert.AreEqual(coreEvent.ExecutionRegistryEntryId, entry.ExecutionRegistryEntryId);
        Assert.AreEqual(approvalId, entry.ApprovalCardId);
        Assert.AreEqual(decisionId, entry.ApprovalDecisionId);
        Assert.AreEqual(coreEvent.EvidenceRefs[0].EvidenceId, entry.EvidenceRefs[0].EvidenceId);
    }

    [TestMethod]
    public void Timeline_RequiresAttentionForApprovalWarningFailureAndHandoff()
    {
        var service = new NodalOsTimelineProjectionService();

        Assert.IsTrue(service.ProjectEvent(NodalOsCoreRuntimeFixtures.CoreEvent(NodalOsCoreEventKind.ApprovalRequired)).RequiresHumanAttention);
        Assert.IsTrue(service.ProjectEvent(NodalOsCoreRuntimeFixtures.CoreEvent(NodalOsCoreEventKind.WarningRaised)).RequiresHumanAttention);
        Assert.IsTrue(service.ProjectEvent(NodalOsCoreRuntimeFixtures.CoreEvent(NodalOsCoreEventKind.ExecutionFailed)).RequiresHumanAttention);
        Assert.IsTrue(service.ProjectEvent(NodalOsCoreRuntimeFixtures.CoreEvent(NodalOsCoreEventKind.HumanHandoffRequired)).RequiresHumanAttention);
    }

    [TestMethod]
    public void Timeline_SerializedOutputDoesNotContainSecrets()
    {
        var coreEvent = NodalOsCoreRuntimeFixtures.CoreEvent(NodalOsCoreEventKind.WarningRaised) with
        {
            HumanSummaryRedacted = "access_token=raw-fixture-token"
        };
        var entry = new NodalOsTimelineProjectionService().ProjectEvent(coreEvent);
        var json = serializer.SerializeTimelineEntry(entry);

        AssertContains(json, "[REDACTED]");
        AssertDoesNotContain(json, "raw-fixture-token");
    }

    [TestMethod]
    public void Timeline_DoesNotExecuteOrMutateRegistry()
    {
        var serviceText = File.ReadAllText(Path.Combine(RepoRoot(), "src", "OneBrain.AgentOperations.Core", "NodalOsApprovalTimelineEvidenceServices.cs"));

        AssertDoesNotContain(serviceText, "Process.Start");
        AssertDoesNotContain(serviceText, "HttpClient");
        AssertDoesNotContain(serviceText, "Task.Run");
        AssertDoesNotContain(serviceText, "Transition(");
    }

    [TestMethod]
    public void EvidenceRegistry_ValidatesCanonicalEvidenceRef()
    {
        var attachment = NodalOsApprovalTimelineEvidenceFixtures.EvidenceAttachment();

        Assert.IsTrue(validator.ValidateEvidenceAttachment(attachment).IsValid);
    }

    [TestMethod]
    public void EvidenceRegistry_AttachesEvidenceToRegistryEntryEventAndApprovalCard()
    {
        var integration = new NodalOsEvidenceRegistryIntegrationService();
        var (entry, coreEvent) = RegistryAndEvent();
        var card = NodalOsApprovalTimelineEvidenceFixtures.ApprovalCard();
        var evidenceRef = NodalOsApprovalTimelineEvidenceFixtures.EvidenceRef();

        var entryWithEvidence = integration.AttachToRegistryEntry(entry, evidenceRef);
        var eventWithEvidence = integration.AttachToEvent(coreEvent, evidenceRef);
        var cardWithEvidence = integration.AttachToApprovalCard(card, evidenceRef);

        Assert.IsTrue(entryWithEvidence.EvidenceRefs.Any(e => e.EvidenceId == evidenceRef.EvidenceId));
        Assert.IsTrue(eventWithEvidence.EvidenceRefs.Any(e => e.EvidenceId == evidenceRef.EvidenceId));
        Assert.IsTrue(cardWithEvidence.EvidenceRefs.Any(e => e.EvidenceId == evidenceRef.EvidenceId));
    }

    [TestMethod]
    public void EvidenceRegistry_TimelineEntryExposesSafeEvidenceRef()
    {
        var coreEvent = NodalOsCoreRuntimeFixtures.CoreEvent();
        var timeline = new NodalOsTimelineProjectionService().ProjectEvent(coreEvent);

        Assert.AreEqual(coreEvent.EvidenceRefs[0].EvidenceId, timeline.EvidenceRefs[0].EvidenceId);
        Assert.AreEqual(NodalOsEvidenceBridgeAuthority.NoAuthority, timeline.EvidenceRefs[0].Authority);
    }

    [TestMethod]
    public void EvidenceRegistry_RejectsRawSecretCookieHeaderBody()
    {
        var refs = new[]
        {
            NodalOsApprovalTimelineEvidenceFixtures.EvidenceRef(reference: "secret=raw-secret"),
            NodalOsApprovalTimelineEvidenceFixtures.EvidenceRef(reference: "Cookie: session=abc123"),
            NodalOsApprovalTimelineEvidenceFixtures.EvidenceRef(reference: "Authorization: Bearer abcdefghijklmnopqrstuvwxyz"),
            NodalOsApprovalTimelineEvidenceFixtures.EvidenceRef(kind: "network-metadata", reference: "body raw payload")
        };

        foreach (var evidenceRef in refs)
        {
            var attachment = NodalOsApprovalTimelineEvidenceFixtures.EvidenceAttachment() with
            {
                EvidenceRef = evidenceRef
            };

            Assert.IsFalse(validator.ValidateEvidenceAttachment(attachment).IsValid, evidenceRef.Ref ?? evidenceRef.Kind);
        }
    }

    [TestMethod]
    public void EvidenceRegistry_ScreenshotNetworkDomRestrictions()
    {
        var screenshotRaw = AttachmentWith(NodalOsApprovalTimelineEvidenceFixtures.EvidenceRef("screenshot-reference", "data:image/png;base64,abc"));
        var screenshotRef = AttachmentWith(NodalOsApprovalTimelineEvidenceFixtures.EvidenceRef("screenshot-reference", "ledger:screenshot-123"));
        var networkRaw = AttachmentWith(NodalOsApprovalTimelineEvidenceFixtures.EvidenceRef("network-metadata", "GET / Authorization: Bearer abcdefghijkl"));
        var networkSafe = AttachmentWith(NodalOsApprovalTimelineEvidenceFixtures.EvidenceRef("network-metadata", "ledger:network-metadata-redacted"));
        var domRaw = AttachmentWith(NodalOsApprovalTimelineEvidenceFixtures.EvidenceRef("dom-snapshot", "<input name='password' />"));
        var domSafe = AttachmentWith(NodalOsApprovalTimelineEvidenceFixtures.EvidenceRef("dom-snapshot", "ledger:dom-snapshot-redacted"));

        Assert.IsFalse(validator.ValidateEvidenceAttachment(screenshotRaw).IsValid);
        Assert.IsTrue(validator.ValidateEvidenceAttachment(screenshotRef).IsValid);
        Assert.IsFalse(validator.ValidateEvidenceAttachment(networkRaw).IsValid);
        Assert.IsTrue(validator.ValidateEvidenceAttachment(networkSafe).IsValid);
        Assert.IsFalse(validator.ValidateEvidenceAttachment(domRaw).IsValid);
        Assert.IsTrue(validator.ValidateEvidenceAttachment(domSafe).IsValid);
    }

    [TestMethod]
    public void EvidenceRegistry_SerializerDoesNotLeakSecrets()
    {
        var attachment = NodalOsApprovalTimelineEvidenceFixtures.EvidenceAttachment() with
        {
            MetadataRedacted = new Dictionary<string, string>
            {
                ["Authorization"] = "Bearer abcdefghijklmnopqrstuvwxyz",
                ["api_key"] = "raw-fixture-key"
            }
        };

        var json = serializer.SerializeEvidenceAttachment(attachment);

        AssertContains(json, "[REDACTED]");
        AssertDoesNotContain(json, "abcdefghijklmnopqrstuvwxyz");
        AssertDoesNotContain(json, "raw-fixture-key");
    }

    [TestMethod]
    public void CrossLayer_RegistryEventTimelineApprovalEvidence_NoDivergence()
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
            "Approval is required before any future submit.",
            "Policy gate marked the request as sensitive.",
            ["resource://target-redacted"]);
        var timeline = new NodalOsTimelineProjectionService().ProjectEvent(coreEvent with
        {
            MetadataRedacted = new Dictionary<string, string> { ["approvalCardId"] = card.ApprovalCardId }
        });

        Assert.AreEqual(entry.RegistryEntryId, coreEvent.ExecutionRegistryEntryId);
        Assert.AreEqual(entry.RegistryEntryId, card.ExecutionRegistryEntryId);
        Assert.AreEqual(entry.RegistryEntryId, timeline.ExecutionRegistryEntryId);
        Assert.AreEqual(card.ApprovalCardId, timeline.ApprovalCardId);
        Assert.AreEqual(entry.EvidenceRefs[0].EvidenceId, card.EvidenceRefs[0].EvidenceId);
        Assert.IsFalse(card.RuntimeExecutionAllowed);
        Assert.IsFalse(card.CanAuthorizeExecution);
        Assert.IsFalse(timeline.RuntimeExecutionAllowed);
    }

    [TestMethod]
    public void CrossLayer_RedactionAppliesConsistently()
    {
        var card = NodalOsApprovalTimelineEvidenceFixtures.ApprovalCard() with
        {
            HumanExplanationRedacted = "Bearer abcdefghijklmnopqrstuvwxyz"
        };
        var decision = NodalOsApprovalTimelineEvidenceFixtures.ApprovalDecision() with
        {
            DecisionReasonRedacted = "password=super-secret"
        };
        var timeline = NodalOsApprovalTimelineEvidenceFixtures.TimelineEntry() with
        {
            MessageRedacted = "api_key=raw-fixture-key"
        };

        AssertContains(serializer.SerializeApprovalCard(card), "[REDACTED]");
        AssertContains(serializer.SerializeApprovalDecision(decision), "[REDACTED]");
        AssertContains(serializer.SerializeTimelineEntry(timeline), "[REDACTED]");
    }

    [TestMethod]
    public void DependencyDirection_ContractsDoNotDependOnCore()
    {
        var contracts = File.ReadAllText(Path.Combine(RepoRoot(), "src", "OneBrain.AgentOperations.Contracts", "OneBrain.AgentOperations.Contracts.csproj"));
        var contractSources = string.Join(Environment.NewLine, Directory.GetFiles(Path.Combine(RepoRoot(), "src", "OneBrain.AgentOperations.Contracts"), "*.cs").Select(File.ReadAllText));

        AssertDoesNotContain(contracts, "OneBrain.AgentOperations.Core");
        AssertDoesNotContain(contractSources, "using OneBrain.AgentOperations.Core");
    }

    [TestMethod]
    public void NamingAndGuardrails_ArePreserved()
    {
        var artifact = File.ReadAllText(ArtifactPath());
        var serviceText = File.ReadAllText(Path.Combine(RepoRoot(), "src", "OneBrain.AgentOperations.Core", "NodalOsApprovalTimelineEvidenceServices.cs"));

        AssertContains(artifact, "\"projectOperationalName\": \"NODAL OS\"");
        AssertDoesNotContain(artifact, "\"projectOperationalName\": \"NEXA\"");
        AssertContains(artifact, "\"uiIntroduced\": false");
        AssertContains(artifact, "\"schedulerOrWorkerIntroduced\": false");
        AssertContains(artifact, "\"browserAutomationIntroduced\": false");
        AssertContains(artifact, "\"recorderOrReplayIntroduced\": false");
        AssertContains(artifact, "\"queueIntroduced\": false");
        AssertContains(artifact, "\"dslParserRuntimeIntroduced\": false");
        AssertDoesNotContain(serviceText, "ProcessStartInfo");
        AssertDoesNotContain(serviceText, "Process.Start");
        AssertDoesNotContain(serviceText, "cmd.exe");
        AssertDoesNotContain(serviceText, "powershell");
    }

    [TestMethod]
    public void RoadmapsReferenceM471M473()
    {
        var vnext = File.ReadAllText(Path.Combine(RepoRoot(), "docs", "roadmap", "nodal-os-roadmap-vnext.md"));
        var unified = File.ReadAllText(Path.Combine(RepoRoot(), "docs", "roadmap", "nodal-os-unified-roadmap-post-pause.md"));

        AssertContains(vnext, "M471-M473 Approval Center Data Model + Timeline Projection + Evidence Registry Integration");
        AssertContains(unified, "M471-M473");
        AssertContains(unified, "Approval Center Data Model + Timeline Projection + Evidence Registry Integration");
    }

    private static NodalOsEvidenceRegistryAttachment AttachmentWith(NodalOsEvidenceBridgeRef evidenceRef) =>
        NodalOsApprovalTimelineEvidenceFixtures.EvidenceAttachment() with
        {
            EvidenceRef = evidenceRef
        };

    private static (NodalOsExecutionRegistryEntry Entry, NodalOsCoreEvent Event) RegistryAndEvent()
    {
        var registry = new NodalOsExecutionRegistry();
        var request = NodalOsCoreRuntimeFixtures.ExecutionRequest();
        var entry = registry.Register(request);
        var coreEvent = NodalOsCoreRuntimeFixtures.CoreEvent(
            NodalOsCoreEventKind.ApprovalRequired,
            entry.RegistryEntryId,
            entry.RequestId);

        return (entry, coreEvent);
    }

    private static string ArtifactPath() =>
        Path.Combine(RepoRoot(), "artifacts", "agent-operations", "m473", "approval-timeline-evidence-integration-summary.json");

    private static void AssertContains(string text, string expected) =>
        Assert.IsTrue(text.Contains(expected, StringComparison.Ordinal), expected);

    private static void AssertContains(IEnumerable<string> values, string expected) =>
        Assert.IsTrue(values.Any(value => value.Contains(expected, StringComparison.Ordinal)), expected);

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
