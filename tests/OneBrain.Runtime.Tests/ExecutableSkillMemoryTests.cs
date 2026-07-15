using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Contracts;
using OneBrain.Core.Memory;
using OneBrain.Core.Models;
using OneBrain.Core.Perception;
using OneBrain.Core.Skills;
using OneBrain.Core.Verification;

namespace OneBrain.Runtime.Tests;

[TestClass]
[TestCategory("LivingSkills")]
[TestCategory("ExecutableSkillMemory")]
public sealed class ExecutableSkillMemoryTests
{
    [TestMethod]
    public void CandidateAndSupervisedSkillCannotReplayBeforeSemanticVerification()
    {
        var memory = new ExecutableSkillMemory();
        var fixture = VerifiedFixture("draft", "saved");
        var candidate = memory.RegisterCandidate(fixture.Request.Candidate);
        var candidateReplay = memory.FindReplay(ReplayRequest(candidate, fixture.Before.StateFingerprint));
        var supervised = memory.MarkSupervised(
            candidate.SkillId,
            fixture.VerifiedAtUtc.AddMinutes(1),
            ["evidence:supervision"]);
        var supervisedReplay = memory.FindReplay(ReplayRequest(supervised, fixture.Before.StateFingerprint));

        Assert.AreEqual(ExecutableSkillState.Candidate, candidate.State);
        Assert.AreEqual(SkillReplayDecision.SkillNotVerified, candidateReplay.Decision);
        Assert.AreEqual(ExecutableSkillState.Supervised, supervised.State);
        Assert.AreEqual(SkillReplayDecision.SkillNotVerified, supervisedReplay.Decision);
        Assert.IsFalse(candidateReplay.ActionAuthorityGranted);
        Assert.IsFalse(supervisedReplay.ActionAuthorityGranted);
    }

    [TestMethod]
    public void FailedOrIncompleteVerificationCannotEnterExecutableMemory()
    {
        var memory = new ExecutableSkillMemory();
        var fixture = VerifiedFixture("draft", "draft");

        var promotion = memory.PromoteVerifiedTransition(fixture.Request);

        Assert.AreEqual(SkillPromotionDecision.Rejected, promotion.Decision);
        Assert.AreEqual(0, memory.List().Count);
        Assert.IsNull(promotion.Skill);
        Assert.IsNull(promotion.Transition);
    }

    [TestMethod]
    public void SuccessfulSemanticVerificationPromotesTransitionAndExactStateReplayTemplate()
    {
        var memory = new ExecutableSkillMemory();
        var fixture = VerifiedFixture("draft", "saved");

        var promotion = memory.PromoteVerifiedTransition(fixture.Request);
        var replay = memory.FindReplay(ReplayRequest(promotion.Skill!, fixture.Before.StateFingerprint));

        Assert.AreEqual(SkillPromotionDecision.CreatedVerifiedSkill, promotion.Decision);
        Assert.AreEqual(ExecutableSkillState.Verified, promotion.Skill!.State);
        Assert.AreEqual(1, promotion.Skill.Transitions.Count);
        Assert.AreEqual(VerifiedSkillTransitionState.Verified, promotion.Transition!.State);
        Assert.AreEqual(1, promotion.Transition.SuccessfulRuns);
        Assert.AreEqual(0, promotion.Transition.FailedRuns);
        Assert.AreEqual(SkillReplayDecision.Ready, replay.Decision);
        Assert.IsTrue(replay.Ready);
        Assert.AreEqual(promotion.Transition.TransitionId, replay.Transition!.TransitionId);
        Assert.IsFalse(replay.ActionAuthorityGranted);
        Assert.IsTrue(replay.RequiresExistingMissionAuthorization);
        Assert.IsFalse(promotion.Skill.LiveExecutionAuthorityGranted);
    }

    [TestMethod]
    public void ReverificationDeduplicatesTransitionAndDoesNotChangeStructuralSkillVersion()
    {
        var memory = new ExecutableSkillMemory();
        var fixture = VerifiedFixture("draft", "saved");
        var first = memory.PromoteVerifiedTransition(fixture.Request);
        var secondRequest = fixture.Request with
        {
            VerifiedAtUtc = fixture.Request.VerifiedAtUtc.AddMinutes(5),
            EvidenceRefs = ["evidence:semantic", "evidence:second-verification"]
        };

        var second = memory.PromoteVerifiedTransition(secondRequest);

        Assert.AreEqual(SkillPromotionDecision.ReverifiedExistingTransition, second.Decision);
        Assert.AreEqual(1, second.Skill!.Transitions.Count);
        Assert.AreEqual(2, second.Transition!.SuccessfulRuns);
        Assert.AreEqual(first.Skill!.Version, second.Skill.Version);
        Assert.AreEqual(first.Transition!.TransitionId, second.Transition.TransitionId);
        Assert.IsTrue(second.Transition.EvidenceRefs.Contains("evidence:second-verification", StringComparer.Ordinal));
    }

    [TestMethod]
    public void RawSecretParameterAndRawScreenshotAreRejectedWithoutMutation()
    {
        var memory = new ExecutableSkillMemory();
        var fixture = VerifiedFixture("draft", "saved");
        var unsafeAction = fixture.Request.Action with
        {
            Parameters =
            [
                new SkillParameterBinding(
                    "SECRET",
                    "s" + "k-raw-skill-secret-value-123456789",
                    SecretByReference: false,
                    RawValuePresent: true)
            ]
        };
        var unsafeRequest = fixture.Request with { Action = unsafeAction };
        var rawScreenshotRequest = fixture.Request with
        {
            Before = fixture.Before with { ContainsRawScreenshot = true }
        };

        var unsafeResult = memory.PromoteVerifiedTransition(unsafeRequest);
        var screenshotResult = memory.PromoteVerifiedTransition(rawScreenshotRequest);

        Assert.AreEqual(SkillPromotionDecision.Rejected, unsafeResult.Decision);
        Assert.AreEqual(SkillPromotionDecision.Rejected, screenshotResult.Decision);
        Assert.AreEqual(0, memory.List().Count);
    }

    [TestMethod]
    public void BlockingCrossChannelConflictCannotBeLearned()
    {
        var memory = new ExecutableSkillMemory();
        var fixture = VerifiedFixture("draft", "saved");
        var conflictingAfter = SnapshotWithClaims(
            "saved",
            processId: 101,
            [
                Claim("editor", "value", "saved", Provenance.Uia),
                Claim("editor", "value", "deleted", Provenance.Vision)
            ]);
        var request = fixture.Request with { After = conflictingAfter };

        var result = memory.PromoteVerifiedTransition(request);

        Assert.AreEqual(SkillPromotionDecision.Rejected, result.Decision);
        Assert.IsTrue(conflictingAfter.HasBlockingConflicts);
        Assert.AreEqual(0, memory.List().Count);
    }

    [TestMethod]
    public void ReplayFailsClosedOnProfileVersionOrCapabilityScopeMismatch()
    {
        var memory = new ExecutableSkillMemory();
        var fixture = VerifiedFixture("draft", "saved");
        var promotion = memory.PromoteVerifiedTransition(fixture.Request);
        var skill = promotion.Skill!;

        var profile = memory.FindReplay(ReplayRequest(skill, fixture.Before.StateFingerprint) with
        {
            AppProfileId = "other-profile"
        });
        var version = memory.FindReplay(ReplayRequest(skill, fixture.Before.StateFingerprint) with
        {
            AppProfileVersion = skill.AppProfileVersion + 1
        });
        var capability = memory.FindReplay(ReplayRequest(skill, fixture.Before.StateFingerprint) with
        {
            AuthorizedCapabilities = new HashSet<string>(StringComparer.Ordinal) { "filesystem.read" }
        });

        Assert.AreEqual(SkillReplayDecision.ProfileMismatch, profile.Decision);
        Assert.AreEqual(SkillReplayDecision.ProfileVersionMismatch, version.Decision);
        Assert.AreEqual(SkillReplayDecision.CapabilityNotAuthorized, capability.Decision);
    }

    [TestMethod]
    public void OneDegradedTransitionDoesNotDisableUnrelatedVerifiedTransition()
    {
        var memory = new ExecutableSkillMemory();
        var firstFixture = VerifiedFixture("draft", "saved", skillId: "skill-multi-state");
        var secondFixture = VerifiedFixture("closed", "opened", skillId: "skill-multi-state");
        var first = memory.PromoteVerifiedTransition(firstFixture.Request);
        var second = memory.PromoteVerifiedTransition(secondFixture.Request);

        var failure = memory.RecordTransitionFailure(new SkillTransitionFailureObservation(
            SkillId: first.Skill!.SkillId,
            TransitionId: first.Transition!.TransitionId,
            FailureClass: SemanticVerificationFailureClass.TargetNotFound,
            FailureKind: FailureKind.NotFound,
            FailedAtUtc: firstFixture.VerifiedAtUtc.AddMinutes(1),
            EvidenceRefs: ["evidence:target-missing"],
            ReasonRedacted: "The target moved after an application update."));
        var degradedReplay = memory.FindReplay(ReplayRequest(failure.Skill!, firstFixture.Before.StateFingerprint));
        var unaffectedReplay = memory.FindReplay(ReplayRequest(failure.Skill!, secondFixture.Before.StateFingerprint));

        Assert.AreEqual(SkillFailureDecision.TransitionDegraded, failure.Decision);
        Assert.AreEqual(ExecutableSkillState.Degraded, failure.Skill!.State);
        Assert.AreEqual(VerifiedSkillTransitionState.Degraded, failure.Transition!.State);
        Assert.AreEqual(SkillReplayDecision.TransitionDegraded, degradedReplay.Decision);
        Assert.AreEqual(SkillReplayDecision.Ready, unaffectedReplay.Decision);
        Assert.AreEqual(second.Transition!.TransitionId, unaffectedReplay.Transition!.TransitionId);
        Assert.AreEqual(2, failure.Skill.Transitions.Count);
    }

    [TestMethod]
    public void SevereSideEffectInvalidatesSingleTransitionSkillImmediately()
    {
        var memory = new ExecutableSkillMemory();
        var fixture = VerifiedFixture("draft", "saved");
        var promoted = memory.PromoteVerifiedTransition(fixture.Request);

        var failure = memory.RecordTransitionFailure(new SkillTransitionFailureObservation(
            promoted.Skill!.SkillId,
            promoted.Transition!.TransitionId,
            SemanticVerificationFailureClass.UnexpectedSideEffect,
            FailureKind.PolicyDenied,
            fixture.VerifiedAtUtc.AddMinutes(1),
            ["evidence:unexpected-side-effect"],
            "A neighboring semantic object changed unexpectedly."));
        var replay = memory.FindReplay(ReplayRequest(failure.Skill!, fixture.Before.StateFingerprint));

        Assert.AreEqual(SkillFailureDecision.SkillInvalidated, failure.Decision);
        Assert.AreEqual(ExecutableSkillState.Invalidated, failure.Skill!.State);
        Assert.AreEqual(VerifiedSkillTransitionState.Invalidated, failure.Transition!.State);
        Assert.AreEqual(SkillReplayDecision.Invalidated, replay.Decision);
    }

    [TestMethod]
    public void UserInterruptionDoesNotPunishTransitionQuality()
    {
        var memory = new ExecutableSkillMemory();
        var fixture = VerifiedFixture("draft", "saved");
        var promoted = memory.PromoteVerifiedTransition(fixture.Request);

        var failure = memory.RecordTransitionFailure(new SkillTransitionFailureObservation(
            promoted.Skill!.SkillId,
            promoted.Transition!.TransitionId,
            SemanticVerificationFailureClass.UserInterrupted,
            FailureKind.HumanInterrupted,
            fixture.VerifiedAtUtc.AddMinutes(1),
            ["evidence:user-interruption"],
            "The operator paused the mission."));

        Assert.AreEqual(SkillFailureDecision.IgnoredExternalInterruption, failure.Decision);
        Assert.AreEqual(ExecutableSkillState.Verified, failure.Skill!.State);
        Assert.AreEqual(0, failure.Transition!.FailedRuns);
        Assert.AreEqual(VerifiedSkillTransitionState.Verified, failure.Transition.State);
    }

    [TestMethod]
    public void LocalizedRepairSupersedesOnlyFailedTransitionAndPreservesSkillGraph()
    {
        var memory = new ExecutableSkillMemory();
        var firstFixture = VerifiedFixture("draft", "saved", skillId: "skill-repair");
        var secondFixture = VerifiedFixture("closed", "opened", skillId: "skill-repair");
        var first = memory.PromoteVerifiedTransition(firstFixture.Request);
        var second = memory.PromoteVerifiedTransition(secondFixture.Request);
        var degraded = memory.RecordTransitionFailure(new SkillTransitionFailureObservation(
            first.Skill!.SkillId,
            first.Transition!.TransitionId,
            SemanticVerificationFailureClass.TargetNotFound,
            FailureKind.NotFound,
            firstFixture.VerifiedAtUtc.AddMinutes(1),
            ["evidence:locator-drift"],
            "The original selector alias no longer resolves."));
        var repairedAction = firstFixture.Request.Action with
        {
            TemplateId = "set-editor-value-repaired",
            SemanticTargetRef = "editor-semantic-role",
            SelectorAliasRefs = ["app-profile:editor:stable-alias-v2"]
        };
        var repair = memory.RepairTransition(new SkillRepairRequest(
            SkillId: degraded.Skill!.SkillId,
            TransitionId: degraded.Transition!.TransitionId,
            Before: firstFixture.Before,
            After: firstFixture.After,
            RepairedAction: repairedAction,
            VerificationPlan: firstFixture.Plan,
            VerificationReport: firstFixture.Report,
            RepairedAtUtc: firstFixture.VerifiedAtUtc.AddMinutes(2),
            EvidenceRefs: ["evidence:localized-repair"]));
        var repairedReplay = memory.FindReplay(ReplayRequest(repair.Skill!, firstFixture.Before.StateFingerprint));
        var unrelatedReplay = memory.FindReplay(ReplayRequest(repair.Skill!, secondFixture.Before.StateFingerprint));

        Assert.AreEqual(SkillRepairDecision.Repaired, repair.Decision);
        Assert.AreEqual(ExecutableSkillState.Verified, repair.Skill!.State);
        Assert.AreEqual(3, repair.Skill.Transitions.Count);
        Assert.AreEqual(VerifiedSkillTransitionState.Invalidated, repair.SupersededTransition!.State);
        Assert.AreEqual(repair.RepairedTransition!.TransitionId, repair.SupersededTransition.SupersededByTransitionId);
        Assert.AreEqual("app-profile:editor:stable-alias-v2", repair.RepairedTransition.Action.SelectorAliasRefs.Single());
        Assert.AreEqual(SkillReplayDecision.Ready, repairedReplay.Decision);
        Assert.AreEqual(repair.RepairedTransition.TransitionId, repairedReplay.Transition!.TransitionId);
        Assert.AreEqual(SkillReplayDecision.Ready, unrelatedReplay.Decision);
        Assert.AreEqual(second.Transition!.TransitionId, unrelatedReplay.Transition!.TransitionId);
    }

    [TestMethod]
    public void RepairCannotChangeCapabilityOperationOrSemanticEndpoints()
    {
        var memory = new ExecutableSkillMemory();
        var fixture = VerifiedFixture("draft", "saved");
        var promoted = memory.PromoteVerifiedTransition(fixture.Request);
        var degraded = memory.RecordTransitionFailure(new SkillTransitionFailureObservation(
            promoted.Skill!.SkillId,
            promoted.Transition!.TransitionId,
            SemanticVerificationFailureClass.TargetNotFound,
            FailureKind.NotFound,
            fixture.VerifiedAtUtc.AddMinutes(1),
            ["evidence:drift"],
            "Target drift."));
        var changedCapability = fixture.Request.Action with
        {
            TemplateId = "unsafe-capability-change",
            CapabilityId = "browser.action.execute"
        };
        var request = new SkillRepairRequest(
            degraded.Skill!.SkillId,
            degraded.Transition!.TransitionId,
            fixture.Before,
            fixture.After,
            changedCapability,
            fixture.Plan,
            fixture.Report,
            fixture.VerifiedAtUtc.AddMinutes(2),
            ["evidence:invalid-repair"]);
        var versionBefore = degraded.Skill.Version;

        var result = memory.RepairTransition(request);
        var stored = memory.Get(degraded.Skill.SkillId)!;

        Assert.AreEqual(SkillRepairDecision.Rejected, result.Decision);
        Assert.AreEqual(versionBefore, stored.Version);
        Assert.AreEqual(1, stored.Transitions.Count);
        Assert.AreEqual(VerifiedSkillTransitionState.Degraded, stored.Transitions.Single().State);
    }

    [TestMethod]
    public void ArchivedSkillCannotReturnReplayTemplate()
    {
        var memory = new ExecutableSkillMemory();
        var fixture = VerifiedFixture("draft", "saved");
        var promoted = memory.PromoteVerifiedTransition(fixture.Request);
        var archived = memory.Archive(
            promoted.Skill!.SkillId,
            fixture.VerifiedAtUtc.AddDays(1),
            ["evidence:archived"]);

        var replay = memory.FindReplay(ReplayRequest(archived, fixture.Before.StateFingerprint));

        Assert.AreEqual(ExecutableSkillState.Archived, archived.State);
        Assert.AreEqual(SkillReplayDecision.Archived, replay.Decision);
        Assert.IsFalse(replay.Ready);
    }

    [TestMethod]
    public void ProcessMemoryProjectionUsesExistingStoreAndNeverCreatesSecondLedger()
    {
        var memory = new ExecutableSkillMemory();
        var fixture = VerifiedFixture("draft", "saved", recipeId: "recipe-verified-editor");
        var promoted = memory.PromoteVerifiedTransition(fixture.Request);
        var projection = promoted.Skill!.ToProcessMemoryEntry();
        var root = Path.Combine(Path.GetTempPath(), "nodal-skill-memory-" + Guid.NewGuid().ToString("N"));
        try
        {
            var write = ProcessMemoryStore.Write(root, projection);
            var loaded = ProcessMemoryStore.ReadById(root, projection.Id);
            var json = JsonSerializer.Serialize(projection);

            Assert.AreEqual(ProcessMemoryStatuses.Stable, projection.Status);
            Assert.AreEqual(ProcessMemorySources.Recipe, projection.Source);
            Assert.AreEqual("recipe-verified-editor", projection.Links.RecipeId);
            Assert.AreEqual(promoted.Skill.SkillFingerprint, projection.Links.ConfidenceId);
            Assert.IsTrue(projection.ConfidenceScore >= 75);
            Assert.IsTrue(write.Success, write.Error);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(projection.Id, loaded.Id);
            Assert.IsFalse(json.Contains(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(json.Contains("RawValuePresent\":true", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(projection.Notes.Any(note => note.Contains("existing Process Memory", StringComparison.Ordinal)));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public void TransitionIdentityIsDeterministicAcrossEvidenceAndVerificationTime()
    {
        var firstMemory = new ExecutableSkillMemory();
        var secondMemory = new ExecutableSkillMemory();
        var firstFixture = VerifiedFixture("draft", "saved");
        var secondFixture = VerifiedFixture("draft", "saved") with
        {
            Request = VerifiedFixture("draft", "saved").Request with
            {
                VerifiedAtUtc = DateTimeOffset.Parse("2026-07-16T00:00:00Z"),
                EvidenceRefs = ["evidence:different-run"]
            }
        };

        var first = firstMemory.PromoteVerifiedTransition(firstFixture.Request);
        var second = secondMemory.PromoteVerifiedTransition(secondFixture.Request);

        Assert.AreEqual(first.Transition!.TransitionId, second.Transition!.TransitionId);
        Assert.AreEqual(first.Transition.TransitionFingerprint, second.Transition.TransitionFingerprint);
        Assert.AreEqual(first.Skill!.SkillFingerprint, second.Skill!.SkillFingerprint);
    }

    private static SkillReplayRequest ReplayRequest(ExecutableSkill skill, string currentStateFingerprint) =>
        new(
            SkillId: skill.SkillId,
            AppProfileId: skill.AppProfileId,
            AppProfileVersion: skill.AppProfileVersion,
            CurrentStateFingerprint: currentStateFingerprint,
            AuthorizedCapabilities: new HashSet<string>(skill.RequiredCapabilities, StringComparer.Ordinal));

    private static VerifiedFixtureData VerifiedFixture(
        string beforeValue,
        string afterValue,
        string skillId = "skill-editor-save",
        string? recipeId = "recipe-editor-save")
    {
        var before = Snapshot(beforeValue, processId: 101);
        var after = Snapshot(afterValue, processId: 101);
        var plan = new SemanticVerificationPlan(
            PlanId: "verify-editor-transition",
            Preconditions:
            [
                Rule("editor-present", SemanticVerificationRuleKind.ElementPresent, "editor")
            ],
            ExpectedTransition:
            [
                Rule("editor-value-changed", SemanticVerificationRuleKind.PropertyChanged, "editor", "value"),
                Rule("state-fingerprint-changed", SemanticVerificationRuleKind.StateFingerprintChanged)
            ],
            ExpectedOutcome:
            [
                Rule("editor-value-outcome", SemanticVerificationRuleKind.PropertyEquals, "editor", "value", afterValue),
                Rule("no-blocking-conflict", SemanticVerificationRuleKind.NoBlockingConflicts)
            ],
            ForbiddenSideEffects: [],
            RequiredEvidenceRefs: ["evidence:semantic"],
            Timeout: TimeSpan.FromSeconds(5),
            AppProfileRef: "fixture-editor",
            RequireActionExecuted: true,
            AllowProcessChange: false,
            FailOnBlockingConflicts: true);
        var report = new SemanticVerifierV2().Verify(
            plan,
            new SemanticVerificationContext(
                Before: before,
                After: after,
                ActionExecuted: true,
                ActionRejected: false,
                UserInterrupted: false,
                Elapsed: TimeSpan.FromMilliseconds(50),
                EvidenceRefs: ["evidence:semantic"]));
        var verifiedAt = DateTimeOffset.Parse("2026-07-15T00:00:00Z");
        var candidate = new SkillCandidateRequest(
            SkillId: skillId,
            TitleRedacted: "Set fixture editor semantic value",
            AppProfileId: "fixture-editor",
            AppProfileVersion: 1,
            RecipeId: recipeId,
            ProcessMemoryId: null,
            RunId: "run-fixture-editor",
            RequiredCapabilities: new HashSet<string>(StringComparer.Ordinal) { "desktop.uia.action" },
            RiskLevel: "low",
            ObservedAtUtc: verifiedAt.AddMinutes(-1),
            EvidenceRefs: ["evidence:candidate"],
            InitialState: ExecutableSkillState.Candidate);
        var action = Action();
        var request = new SkillPromotionRequest(
            Candidate: candidate,
            Before: before,
            After: after,
            Action: action,
            VerificationPlan: plan,
            VerificationReport: report,
            VerifiedAtUtc: verifiedAt,
            EvidenceRefs: ["evidence:semantic", "evidence:skill-promotion"]);
        return new VerifiedFixtureData(before, after, plan, report, action, request, verifiedAt);
    }

    private static SkillActionTemplate Action() =>
        new(
            TemplateId: "set-editor-value",
            CapabilityId: "desktop.uia.action",
            Operation: "set-value",
            SemanticTargetRef: "editor-semantic-role",
            Parameters:
            [
                new SkillParameterBinding("TEXT", "variable-ref:TEXT")
            ],
            SelectorAliasRefs: ["app-profile:editor:stable-alias"],
            RecoveryAlternatives:
            [
                new SkillRecoveryAlternative(
                    RecoveryId: "reobserve-editor",
                    Kind: SkillRecoveryKind.ReobserveApplication,
                    SummaryRedacted: "Capture a fresh semantic snapshot of the fixture editor.",
                    SelectorAliasRef: null,
                    EvidenceRefs: ["evidence:recovery-design"]),
                new SkillRecoveryAlternative(
                    RecoveryId: "alternate-editor-alias",
                    Kind: SkillRecoveryKind.AlternateSelectorAlias,
                    SummaryRedacted: "Try the validated secondary app-profile selector alias.",
                    SelectorAliasRef: "app-profile:editor:secondary-alias",
                    EvidenceRefs: ["evidence:selector-alias"])
            ],
            RiskLevel: "low",
            RequiresExistingMissionAuthorization: true);

    private static CognitiveSnapshotV2 Snapshot(string value, int processId) =>
        SnapshotWithClaims(
            value,
            processId,
            [Claim("editor", "value", value, Provenance.Fixture)]);

    private static CognitiveSnapshotV2 SnapshotWithClaims(
        string value,
        int processId,
        IReadOnlyList<PerceptionClaim> claims) =>
        CognitiveSnapshotV2Factory.Create(new CognitiveSnapshotV2Input(
            SnapshotId: "skill-snapshot-" + value,
            CapturedAtUtc: DateTimeOffset.Parse("2026-07-15T00:00:00Z"),
            Application: new CognitiveApplicationIdentity(
                "app-fixture-editor",
                "fixture-editor",
                processId,
                "Fixture Editor"),
            WindowBounds: new WindowBounds(0, 0, 1280, 720),
            IsForeground: true,
            WindowClaims: [],
            Elements:
            [
                new CognitiveSnapshotV2ElementInput(
                    "editor",
                    new ElementIdentity(
                        runtimeId: "editor-runtime",
                        role: "Edit",
                        name: "Editor",
                        automationId: "editor-main")
                    {
                        ProcessName = "fixture-editor",
                        WindowTitle = "Fixture Editor",
                        Provenance = Provenance.Fixture
                    },
                    claims)
            ],
            EvidenceRefs: ["evidence:snapshot:" + value],
            ContainsRawScreenshot: false,
            ContainsRawDom: false));

    private static PerceptionClaim Claim(
        string subjectRef,
        string property,
        string value,
        Provenance provenance) =>
        new(
            SubjectRef: subjectRef,
            Property: property,
            ValueRedacted: value,
            Source: provenance,
            Confidence: 1d,
            CapturedAtUtc: DateTimeOffset.Parse("2026-07-15T00:00:00Z"),
            EvidenceRef: "evidence:claim:" + provenance);

    private static SemanticVerificationRule Rule(
        string ruleId,
        SemanticVerificationRuleKind kind,
        string? subjectRef = null,
        string? property = null,
        string? expected = null) =>
        new(
            RuleId: ruleId,
            Kind: kind,
            SubjectRef: subjectRef,
            Property: property,
            ExpectedValueRedacted: expected,
            Required: true);

    private sealed record VerifiedFixtureData(
        CognitiveSnapshotV2 Before,
        CognitiveSnapshotV2 After,
        SemanticVerificationPlan Plan,
        SemanticVerificationReport Report,
        SkillActionTemplate Action,
        SkillPromotionRequest Request,
        DateTimeOffset VerifiedAtUtc);
}
