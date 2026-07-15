using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Contracts;
using OneBrain.Core.Models;
using OneBrain.Core.Perception;
using OneBrain.Core.Verification;

namespace OneBrain.Runtime.Tests;

[TestClass]
[TestCategory("LivingSkills")]
[TestCategory("SemanticVerifierV2")]
public sealed class SemanticVerifierV2Tests
{
    [TestMethod]
    public void VerifiedTransitionRequiresOutcomeSideEffectChecksAndEvidence()
    {
        var before = Snapshot(
            processId: 100,
            Element("editor", ("value", "draft")),
            Element("neighbor", ("value", "unchanged")));
        var after = Snapshot(
            processId: 100,
            Element("editor", ("value", "saved")),
            Element("neighbor", ("value", "unchanged")),
            Element("success", ("name", "Saved")));
        var plan = Plan(
            preconditions:
            [
                Rule("editor-present", SemanticVerificationRuleKind.ElementPresent, "editor")
            ],
            transition:
            [
                Rule("editor-changed", SemanticVerificationRuleKind.PropertyChanged, "editor", "value"),
                Rule("success-added", SemanticVerificationRuleKind.ElementAdded, "success"),
                Rule("state-changed", SemanticVerificationRuleKind.StateFingerprintChanged)
            ],
            outcome:
            [
                Rule("editor-saved", SemanticVerificationRuleKind.PropertyEquals, "editor", "value", "saved"),
                Rule("success-visible", SemanticVerificationRuleKind.ElementPresent, "success"),
                Rule("no-conflict", SemanticVerificationRuleKind.NoBlockingConflicts)
            ],
            forbidden:
            [
                Rule("neighbor-must-not-change", SemanticVerificationRuleKind.PropertyChanged, "neighbor", "value")
            ],
            evidence: ["evidence:verification"]);

        var report = new SemanticVerifierV2().Verify(
            plan,
            Context(before, after, evidence: ["evidence:verification"]));

        Assert.IsTrue(report.Success);
        Assert.IsTrue(report.ActionExecuted);
        Assert.IsTrue(report.ProcessVerified);
        Assert.IsTrue(report.StateTransitionVerified);
        Assert.IsTrue(report.OutcomeVerified);
        Assert.IsTrue(report.SideEffectsChecked);
        Assert.IsTrue(report.EvidenceComplete);
        Assert.AreEqual(SemanticVerificationFailureClass.None, report.FailureClass);
        Assert.IsNull(report.MappedFailureKind);
        Assert.IsTrue(report.FactsObserved.Count >= 8);
    }

    [TestMethod]
    public void UnchangedStateFailsExpectedTransition()
    {
        var before = Snapshot(100, Element("editor", ("value", "draft")));
        var after = Snapshot(100, Element("editor", ("value", "draft")));
        var plan = Plan(
            transition:
            [
                Rule("editor-changed", SemanticVerificationRuleKind.PropertyChanged, "editor", "value")
            ]);

        var report = new SemanticVerifierV2().Verify(plan, Context(before, after));

        Assert.IsFalse(report.Success);
        Assert.AreEqual(SemanticVerificationFailureClass.ExpectedStateNotReached, report.FailureClass);
        Assert.AreEqual(FailureKind.Unverified, report.MappedFailureKind);
        Assert.IsFalse(report.StateTransitionVerified);
    }

    [TestMethod]
    public void ChangedForbiddenNeighborFailsAsUnexpectedSideEffect()
    {
        var before = Snapshot(
            100,
            Element("editor", ("value", "draft")),
            Element("neighbor", ("value", "stable")));
        var after = Snapshot(
            100,
            Element("editor", ("value", "saved")),
            Element("neighbor", ("value", "modified")));
        var plan = Plan(
            transition:
            [
                Rule("editor-changed", SemanticVerificationRuleKind.PropertyChanged, "editor", "value")
            ],
            outcome:
            [
                Rule("editor-saved", SemanticVerificationRuleKind.PropertyEquals, "editor", "value", "saved")
            ],
            forbidden:
            [
                Rule("neighbor-changed", SemanticVerificationRuleKind.PropertyChanged, "neighbor", "value")
            ]);

        var report = new SemanticVerifierV2().Verify(plan, Context(before, after));

        Assert.IsFalse(report.Success);
        Assert.AreEqual(SemanticVerificationFailureClass.UnexpectedSideEffect, report.FailureClass);
        Assert.AreEqual(FailureKind.PolicyDenied, report.MappedFailureKind);
        Assert.IsFalse(report.SideEffectsChecked);
    }

    [TestMethod]
    public void MissingAfterSnapshotIsClassifiedAsApplicationCrash()
    {
        var before = Snapshot(100, Element("editor", ("value", "draft")));

        var report = new SemanticVerifierV2().Verify(
            Plan(),
            Context(before, after: null));

        Assert.IsFalse(report.Success);
        Assert.AreEqual(SemanticVerificationFailureClass.ApplicationCrashed, report.FailureClass);
        Assert.AreEqual(FailureKind.Stale, report.MappedFailureKind);
    }

    [TestMethod]
    public void MissingEvidenceCannotPromoteCompletion()
    {
        var before = Snapshot(100, Element("editor", ("value", "draft")));
        var after = Snapshot(100, Element("editor", ("value", "saved")));
        var plan = Plan(
            transition:
            [
                Rule("editor-changed", SemanticVerificationRuleKind.PropertyChanged, "editor", "value")
            ],
            evidence: ["evidence:required"]);

        var report = new SemanticVerifierV2().Verify(plan, Context(before, after));

        Assert.IsFalse(report.Success);
        Assert.IsFalse(report.EvidenceComplete);
        Assert.AreEqual(SemanticVerificationFailureClass.VerifierInconclusive, report.FailureClass);
        Assert.AreEqual(FailureKind.Unverified, report.MappedFailureKind);
    }

    [TestMethod]
    public void BlockingCrossChannelConflictFailsAmbiguousBeforeOutcomePromotion()
    {
        var before = Snapshot(100, Element("action", ("name", "Continue")));
        var after = SnapshotWithClaims(
            processId: 100,
            new CognitiveSnapshotV2ElementInput(
                "action",
                Identity("action"),
                [
                    Claim("action", "name", "Continue", Provenance.Uia),
                    Claim("action", "name", "Delete", Provenance.Vision)
                ]));

        var report = new SemanticVerifierV2().Verify(
            Plan(),
            Context(before, after));

        Assert.IsFalse(report.Success);
        Assert.AreEqual(SemanticVerificationFailureClass.VerifierInconclusive, report.FailureClass);
        Assert.AreEqual(FailureKind.Ambiguous, report.MappedFailureKind);
        Assert.IsTrue(report.Reasons.Any(reason => reason.Contains("re-grounding", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void UserInterruptionStopsVerificationWithoutContinuing()
    {
        var before = Snapshot(100, Element("editor", ("value", "draft")));
        var after = Snapshot(100, Element("editor", ("value", "saved")));
        var context = Context(before, after) with { UserInterrupted = true };

        var report = new SemanticVerifierV2().Verify(Plan(), context);

        Assert.IsFalse(report.Success);
        Assert.AreEqual(SemanticVerificationFailureClass.UserInterrupted, report.FailureClass);
        Assert.AreEqual(FailureKind.HumanInterrupted, report.MappedFailureKind);
    }

    [TestMethod]
    public void ProcessReplacementFailsClosedAsEnvironmentChange()
    {
        var before = Snapshot(100, Element("editor", ("value", "draft")));
        var after = Snapshot(200, Element("editor", ("value", "saved")));

        var report = new SemanticVerifierV2().Verify(Plan(), Context(before, after));

        Assert.IsFalse(report.Success);
        Assert.IsFalse(report.ProcessVerified);
        Assert.AreEqual(SemanticVerificationFailureClass.EnvironmentChanged, report.FailureClass);
        Assert.AreEqual(FailureKind.Stale, report.MappedFailureKind);
    }

    [TestMethod]
    public void RejectedActionMapsToExistingPolicyFailureKind()
    {
        var before = Snapshot(100, Element("editor", ("value", "draft")));
        var context = Context(before, before) with
        {
            ActionExecuted = false,
            ActionRejected = true
        };

        var report = new SemanticVerifierV2().Verify(Plan(), context);

        Assert.IsFalse(report.Success);
        Assert.AreEqual(SemanticVerificationFailureClass.ActionRejected, report.FailureClass);
        Assert.AreEqual(FailureKind.PolicyDenied, report.MappedFailureKind);
    }

    [TestMethod]
    public void MissingRequiredTargetFailsAsNotFoundBeforeExecution()
    {
        var before = Snapshot(100, Element("other", ("value", "draft")));
        var plan = Plan(
            preconditions:
            [
                Rule("target-present", SemanticVerificationRuleKind.ElementPresent, "target")
            ]);

        var report = new SemanticVerifierV2().Verify(plan, Context(before, before));

        Assert.IsFalse(report.Success);
        Assert.AreEqual(SemanticVerificationFailureClass.TargetNotFound, report.FailureClass);
        Assert.AreEqual(FailureKind.NotFound, report.MappedFailureKind);
    }

    private static SemanticVerificationContext Context(
        CognitiveSnapshotV2 before,
        CognitiveSnapshotV2? after,
        IReadOnlyList<string>? evidence = null) =>
        new(
            Before: before,
            After: after,
            ActionExecuted: true,
            ActionRejected: false,
            UserInterrupted: false,
            Elapsed: TimeSpan.FromMilliseconds(50),
            EvidenceRefs: evidence ?? Array.Empty<string>());

    private static SemanticVerificationPlan Plan(
        IReadOnlyList<SemanticVerificationRule>? preconditions = null,
        IReadOnlyList<SemanticVerificationRule>? transition = null,
        IReadOnlyList<SemanticVerificationRule>? outcome = null,
        IReadOnlyList<SemanticVerificationRule>? forbidden = null,
        IReadOnlyList<string>? evidence = null) =>
        new(
            PlanId: "semantic-verification-fixture",
            Preconditions: preconditions ?? Array.Empty<SemanticVerificationRule>(),
            ExpectedTransition: transition ?? Array.Empty<SemanticVerificationRule>(),
            ExpectedOutcome: outcome ?? Array.Empty<SemanticVerificationRule>(),
            ForbiddenSideEffects: forbidden ?? Array.Empty<SemanticVerificationRule>(),
            RequiredEvidenceRefs: evidence ?? Array.Empty<string>(),
            Timeout: TimeSpan.FromSeconds(5),
            AppProfileRef: "fixture-editor",
            RequireActionExecuted: true,
            AllowProcessChange: false,
            FailOnBlockingConflicts: true);

    private static SemanticVerificationRule Rule(
        string id,
        SemanticVerificationRuleKind kind,
        string? subjectRef = null,
        string? property = null,
        string? expected = null) =>
        new(
            RuleId: id,
            Kind: kind,
            SubjectRef: subjectRef,
            Property: property,
            ExpectedValueRedacted: expected,
            Required: true);

    private static CognitiveSnapshotV2 Snapshot(
        int processId,
        params CognitiveSnapshotV2ElementInput[] elements) =>
        SnapshotWithClaims(processId, elements);

    private static CognitiveSnapshotV2 SnapshotWithClaims(
        int processId,
        params CognitiveSnapshotV2ElementInput[] elements) =>
        CognitiveSnapshotV2Factory.Create(new CognitiveSnapshotV2Input(
            SnapshotId: "semantic-fixture",
            CapturedAtUtc: DateTimeOffset.Parse("2026-07-15T00:00:00Z"),
            Application: new CognitiveApplicationIdentity(
                "app-fixture-editor",
                "fixture-editor",
                processId,
                "Fixture Editor"),
            WindowBounds: new WindowBounds(0, 0, 1280, 720),
            IsForeground: true,
            WindowClaims: [],
            Elements: elements,
            EvidenceRefs: ["evidence:snapshot"]));

    private static CognitiveSnapshotV2ElementInput Element(
        string semanticRef,
        params (string Property, string Value)[] properties) =>
        new(
            semanticRef,
            Identity(semanticRef),
            properties.Select(property =>
                Claim(semanticRef, property.Property, property.Value, Provenance.Fixture)).ToArray());

    private static ElementIdentity Identity(string semanticRef) =>
        new(
            runtimeId: semanticRef + "-runtime",
            role: "FixtureControl",
            name: semanticRef,
            automationId: semanticRef + "-automation")
        {
            ProcessName = "fixture-editor",
            WindowTitle = "Fixture Editor",
            Provenance = Provenance.Fixture
        };

    private static PerceptionClaim Claim(
        string subjectRef,
        string property,
        string value,
        Provenance source) =>
        new(
            SubjectRef: subjectRef,
            Property: property,
            ValueRedacted: value,
            Source: source,
            Confidence: 1d,
            CapturedAtUtc: DateTimeOffset.Parse("2026-07-15T00:00:00Z"),
            EvidenceRef: "evidence:claim");
}
