using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.AgentOperations.Contracts;
using OneBrain.AgentOperations.Core;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("CoreRuntimeRegistryEventBusRedaction")]
[TestCategory("CoreRuntimeRegistry")]
[TestCategory("EventBus")]
[TestCategory("RedactionFoundation")]
public sealed class NodalOsCoreRuntimeRegistryEventBusRedactionM468M470Tests
{
    private readonly NodalOsCoreRuntimeValidator validator = new();
    private readonly NodalOsCoreRuntimeJsonSerializer serializer = new();

    [TestMethod]
    public void ExecutionRegistry_RegistersRequestWithValidInitialState()
    {
        var registry = new NodalOsExecutionRegistry();
        var request = NodalOsCoreRuntimeFixtures.ExecutionRequest();

        var entry = registry.Register(request);

        Assert.AreEqual(NodalOsExecutionRegistryState.Registered, entry.State);
        Assert.AreEqual(request.RequestId, entry.RequestId);
        Assert.IsFalse(entry.RuntimeExecutionAllowed);
        Assert.IsTrue(entry.RuntimeExecutionDeferred);
        Assert.IsTrue(validator.ValidateEntry(entry).IsValid);
    }

    [TestMethod]
    public void ExecutionRegistry_RejectsInvalidTransition()
    {
        var registry = new NodalOsExecutionRegistry();
        var entry = registry.Register(NodalOsCoreRuntimeFixtures.ExecutionRequest());

        try
        {
            registry.Transition(entry.RegistryEntryId, NodalOsExecutionRegistryState.Completed, "operator");
            Assert.Fail("Invalid transition should throw.");
        }
        catch (InvalidOperationException)
        {
        }
    }

    [TestMethod]
    public void ExecutionRegistry_AllowsExpectedTransition()
    {
        var registry = new NodalOsExecutionRegistry();
        var entry = registry.Register(NodalOsCoreRuntimeFixtures.ExecutionRequest());

        var transitioned = registry.Transition(
            entry.RegistryEntryId,
            NodalOsExecutionRegistryState.PolicyEvaluated,
            "policy-gate",
            policyDecisionRef: "policy:allow-dry-run");

        Assert.AreEqual(NodalOsExecutionRegistryState.PolicyEvaluated, transitioned.State);
        Assert.AreEqual("policy:allow-dry-run", transitioned.PolicyDecisionRef);
        Assert.AreEqual(2, transitioned.Transitions.Count);
    }

    [TestMethod]
    public void ExecutionRegistry_ConservesEvidenceRefs()
    {
        var registry = new NodalOsExecutionRegistry();
        var request = NodalOsCoreRuntimeFixtures.ExecutionRequest();

        var entry = registry.Register(request);

        Assert.AreEqual(request.EvidenceRefs.Count, entry.EvidenceRefs.Count);
        Assert.AreEqual(request.EvidenceRefs[0].EvidenceId, entry.EvidenceRefs[0].EvidenceId);
    }

    [TestMethod]
    public void ExecutionRegistry_ConservesVerificationRefsIfProvided()
    {
        var registry = new NodalOsExecutionRegistry();
        var entry = registry.Register(NodalOsCoreRuntimeFixtures.ExecutionRequest());
        var policyEvaluated = registry.Transition(entry.RegistryEntryId, NodalOsExecutionRegistryState.PolicyEvaluated, "policy-gate");
        var dryRun = registry.Transition(
            policyEvaluated.RegistryEntryId,
            NodalOsExecutionRegistryState.DryRunPlanned,
            "dry-run-planner",
            dryRunRef: "dry-run:preview-1");

        var skipped = registry.Transition(
            dryRun.RegistryEntryId,
            NodalOsExecutionRegistryState.ExecutionSkipped,
            "verification-gate",
            verificationReportRef: "verification:report-1");

        Assert.AreEqual("dry-run:preview-1", skipped.DryRunRef);
        Assert.AreEqual("verification:report-1", skipped.VerificationReportRef);
    }

    [TestMethod]
    public void ExecutionRegistry_FailureReasonIsRedacted()
    {
        var registry = new NodalOsExecutionRegistry();
        var entry = registry.Register(NodalOsCoreRuntimeFixtures.ExecutionRequest());

        var failed = registry.Transition(
            entry.RegistryEntryId,
            NodalOsExecutionRegistryState.Failed,
            "operator",
            "Authorization: Bearer abcdefghijklmnopqrstuvwxyz");

        Assert.AreEqual("[REDACTED]", failed.FailureReasonRedacted);
        AssertDoesNotContain(serializer.SerializeEntry(failed), "abcdefghijklmnopqrstuvwxyz");
    }

    [TestMethod]
    public void ExecutionRegistry_SerializesWithoutSecrets()
    {
        var json = serializer.SerializeRequest(NodalOsCoreRuntimeFixtures.SecretExecutionRequest());

        AssertContains(json, "[REDACTED]");
        AssertDoesNotContain(json, "super-secret");
        AssertDoesNotContain(json, "abcdefghijklmnopqrstuvwxyz");
    }

    [TestMethod]
    public void ExecutionRegistry_DoesNotAuthorizeExecutionByItself()
    {
        var registry = new NodalOsExecutionRegistry();
        var entry = registry.Register(NodalOsCoreRuntimeFixtures.ExecutionRequest());

        Assert.IsFalse(entry.RuntimeExecutionAllowed);
        Assert.IsTrue(entry.RuntimeExecutionDeferred);
        Assert.IsTrue(entry.RequiresGlobalPolicyEvaluation);
    }

    [TestMethod]
    public void EventBus_PublishesValidCanonicalEvent()
    {
        var eventBus = new NodalOsCoreEventBus();
        var result = eventBus.Publish(NodalOsCoreRuntimeFixtures.CoreEvent());

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(1, eventBus.Snapshot().Count);
    }

    [TestMethod]
    public void EventBus_RejectsInvalidEvent()
    {
        var eventBus = new NodalOsCoreEventBus();
        var invalid = NodalOsCoreRuntimeFixtures.CoreEvent() with { EventId = string.Empty };

        var result = eventBus.Publish(invalid);

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual(0, eventBus.Snapshot().Count);
    }

    [TestMethod]
    public void EventBus_ConservesOrderingAndTimestamp()
    {
        var eventBus = new NodalOsCoreEventBus();
        var first = NodalOsCoreRuntimeFixtures.CoreEvent() with { CreatedAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z") };
        var second = NodalOsCoreRuntimeFixtures.CoreEvent(NodalOsCoreEventKind.WarningRaised) with { CreatedAt = DateTimeOffset.Parse("2026-01-01T00:00:01Z") };

        eventBus.Publish(second);
        eventBus.Publish(first);
        var snapshot = eventBus.Snapshot();

        Assert.AreEqual(first.EventId, snapshot[0].EventId);
        Assert.AreEqual(second.EventId, snapshot[1].EventId);
    }

    [TestMethod]
    public void EventBus_LinksEventWithExecutionRegistryId()
    {
        var registry = new NodalOsExecutionRegistry();
        var entry = registry.Register(NodalOsCoreRuntimeFixtures.ExecutionRequest());
        var coreEvent = NodalOsCoreRuntimeFixtures.CoreEvent(registryEntryId: entry.RegistryEntryId, requestId: entry.RequestId);

        Assert.IsTrue(new NodalOsCoreEventBus().Publish(coreEvent).IsValid);
        Assert.AreEqual(entry.RegistryEntryId, coreEvent.ExecutionRegistryEntryId);
    }

    [TestMethod]
    public void EventBus_LinksEvidenceRefs()
    {
        var coreEvent = NodalOsCoreRuntimeFixtures.CoreEvent();
        var result = validator.ValidateEvent(coreEvent);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(1, coreEvent.EvidenceRefs.Count);
        Assert.AreEqual(NodalOsEvidenceBridgeAuthority.NoAuthority, coreEvent.EvidenceRefs[0].Authority);
    }

    [TestMethod]
    public void EventBus_TimelineProjectionDoesNotContainSecrets()
    {
        var eventBus = new NodalOsCoreEventBus();
        var coreEvent = NodalOsCoreRuntimeFixtures.CoreEvent() with
        {
            HumanSummaryRedacted = "password=super-secret"
        };

        var result = eventBus.Publish(coreEvent);
        var projection = eventBus.ToTimelineProjections().Single();

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual("[REDACTED]", projection.SummaryRedacted);
        AssertDoesNotContain(serializer.SerializeTimelineProjection(projection), "super-secret");
    }

    [TestMethod]
    public void EventBus_DoesNotExecuteSideEffects()
    {
        var serviceText = File.ReadAllText(Path.Combine(RepoRoot(), "src", "OneBrain.AgentOperations.Core", "NodalOsCoreRuntimeServices.cs"));

        AssertDoesNotContain(serviceText, "Process.Start");
        AssertDoesNotContain(serviceText, "Start-Process");
        AssertDoesNotContain(serviceText, "HttpClient");
        AssertDoesNotContain(serviceText, "Task.Run");
    }

    [TestMethod]
    public void Redaction_RedactsBearerToken()
    {
        var result = new NodalOsRedactionService().RedactValue("Authorization: Bearer abcdefghijklmnopqrstuvwxyz");

        Assert.IsTrue(result.WasRedacted);
        Assert.AreEqual("[REDACTED]", result.Value);
    }

    [TestMethod]
    public void Redaction_RedactsCookies()
    {
        var result = new NodalOsRedactionService().RedactValue("Cookie: session=abc123");

        Assert.IsTrue(result.WasRedacted);
        Assert.AreEqual("[REDACTED]", result.Value);
    }

    [TestMethod]
    public void Redaction_RedactsAuthorizationHeaders()
    {
        var result = new NodalOsRedactionService().RedactField("Authorization", "Basic abcdefghijkl");

        Assert.IsTrue(result.WasRedacted);
        Assert.AreEqual("[REDACTED]", result.Value);
    }

    [TestMethod]
    public void Redaction_RedactsPasswordSecretApiKey()
    {
        var redaction = new NodalOsRedactionService();

        Assert.AreEqual("[REDACTED]", redaction.RedactValue("password=super-secret").Value);
        Assert.AreEqual("[REDACTED]", redaction.RedactValue("secret=super-secret").Value);
        Assert.AreEqual("[REDACTED]", redaction.RedactValue("api_key=super-secret").Value);
    }

    [TestMethod]
    public void Redaction_RedactsAccessRefreshIdTokens()
    {
        var redaction = new NodalOsRedactionService();

        Assert.AreEqual("[REDACTED]", redaction.RedactValue("access_token=abc123456789").Value);
        Assert.AreEqual("[REDACTED]", redaction.RedactValue("refresh_token=abc123456789").Value);
        Assert.AreEqual("[REDACTED]", redaction.RedactValue("id_token=abc123456789").Value);
    }

    [TestMethod]
    public void Redaction_RedactsPrivateKeyConnectionStringAndEmail()
    {
        var redaction = new NodalOsRedactionService();

        Assert.AreEqual("[REDACTED]", redaction.RedactValue("-----BEGIN PRIVATE KEY----- abc -----END PRIVATE KEY-----").Value);
        Assert.AreEqual("[REDACTED]", redaction.RedactValue("Server=db;Database=nodal;User Id=sa;Password=secret;").Value);
        Assert.AreEqual("[REDACTED]", redaction.RedactValue("operator@example.test").Value);
    }

    [TestMethod]
    public void Redaction_IsIdempotent()
    {
        var redaction = new NodalOsRedactionService();
        var once = redaction.RedactValue("Bearer abcdefghijklmnopqrstuvwxyz").Value;
        var twice = redaction.RedactValue(once).Value;

        Assert.AreEqual(once, twice);
    }

    [TestMethod]
    public void RegistrySerializedOutputDoesNotContainSecrets()
    {
        var registry = new NodalOsExecutionRegistry();
        var entry = registry.Register(NodalOsCoreRuntimeFixtures.ExecutionRequest());
        var failed = registry.Transition(entry.RegistryEntryId, NodalOsExecutionRegistryState.Failed, "operator", "api_key=raw-fixture-key");
        var json = serializer.SerializeEntry(failed);

        AssertContains(json, "[REDACTED]");
        AssertDoesNotContain(json, "raw-fixture-key");
    }

    [TestMethod]
    public void EventSerializedOutputDoesNotContainSecrets()
    {
        var json = serializer.SerializeEvent(NodalOsCoreRuntimeFixtures.SecretCoreEvent());

        AssertContains(json, "[REDACTED]");
        AssertDoesNotContain(json, "raw-fixture-key");
        AssertDoesNotContain(json, "abcdefghijklmnopqrstuvwxyz");
        AssertDoesNotContain(json, "BEGIN PRIVATE KEY");
    }

    [TestMethod]
    public void ArtifactAndReportDoNotContainAdversarialSecrets()
    {
        var report = File.ReadAllText(ReportPath());
        var artifact = File.ReadAllText(ArtifactPath());

        AssertDoesNotContain(report, "raw-fixture-key");
        AssertDoesNotContain(artifact, "raw-fixture-key");
        AssertDoesNotContain(report, "abcdefghijklmnopqrstuvwxyz");
        AssertDoesNotContain(artifact, "abcdefghijklmnopqrstuvwxyz");
    }

    [TestMethod]
    public void Naming_NodalOsRemainsOperationalName()
    {
        var artifact = File.ReadAllText(ArtifactPath());

        AssertContains(artifact, "\"projectOperationalName\": \"NODAL OS\"");
        AssertDoesNotContain(artifact, "\"projectOperationalName\": \"NODRIX\"");
        AssertDoesNotContain(artifact, "\"projectOperationalName\": \"HOTEP\"");
    }

    [TestMethod]
    public void Naming_NoNexaOperationalReferenceInNewArtifacts()
    {
        var report = File.ReadAllText(ReportPath());
        var artifact = File.ReadAllText(ArtifactPath());

        AssertDoesNotContain(report, "NEXA is the operational project name");
        AssertDoesNotContain(artifact, "\"projectOperationalName\": \"NEXA\"");
    }

    [TestMethod]
    public void Guardrails_NoSchedulerWorkerBrowserAutomationRecorderReplayDslParserRuntime()
    {
        var artifact = File.ReadAllText(ArtifactPath());

        AssertContains(artifact, "\"browserAutomationIntroduced\": false");
        AssertContains(artifact, "\"schedulerOrWorkerIntroduced\": false");
        AssertContains(artifact, "\"recorderOrReplayIntroduced\": false");
        AssertContains(artifact, "\"dslParserRuntimeIntroduced\": false");
    }

    [TestMethod]
    public void Guardrails_NoShellOrSubprocessIntroduced()
    {
        var serviceText = File.ReadAllText(Path.Combine(RepoRoot(), "src", "OneBrain.AgentOperations.Core", "NodalOsCoreRuntimeServices.cs"));

        AssertDoesNotContain(serviceText, "ProcessStartInfo");
        AssertDoesNotContain(serviceText, "Process.Start");
        AssertDoesNotContain(serviceText, "CliWrap");
        AssertDoesNotContain(serviceText, "cmd.exe");
        AssertDoesNotContain(serviceText, "powershell");
    }

    [TestMethod]
    public void RoadmapReferencesM468M470()
    {
        var vnext = File.ReadAllText(Path.Combine(RepoRoot(), "docs", "roadmap", "nodal-os-roadmap-vnext.md"));
        var unified = File.ReadAllText(Path.Combine(RepoRoot(), "docs", "roadmap", "nodal-os-unified-roadmap-post-pause.md"));

        AssertContains(vnext, "M468-M470 Core Runtime Registry + EventBus + Redaction Foundation");
        AssertContains(unified, "M468-M470");
        AssertContains(unified, "Core Runtime Registry + EventBus + Redaction Foundation");
    }

    private static string ReportPath() =>
        Path.Combine(RepoRoot(), "docs", "reports", "core-runtime-registry-eventbus-redaction-m468-m470.md");

    private static string ArtifactPath() =>
        Path.Combine(RepoRoot(), "artifacts", "agent-operations", "m470", "core-runtime-registry-eventbus-redaction-summary.json");

    private static void AssertContains(string text, string expected) =>
        Assert.IsTrue(text.Contains(expected, StringComparison.Ordinal), expected);

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
