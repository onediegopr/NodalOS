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
    public void PromotionRejectsFailedVerificationRawValuesRawScreenshotsAndBlockingConflicts()
    {
        var unchangedMemory = new ExecutableSkillMemory();
        var unchanged = VerifiedFixture("draft", "draft");
        Assert.AreEqual(
            SkillPromotionDecision.Rejected,
            unchangedMemory.PromoteVerifiedTransition(unchanged.Request).Decision);

        var unsafeMemory = new ExecutableSkillMemory();
        var verified = VerifiedFixture("draft", "saved");
        var unsafeAction = verified.Action with
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
        Assert.AreEqual(
            SkillPromotionDecision.Rejected,
            unsafeMemory.PromoteVerifiedTransition(verified.Request with { Action = unsafeAction }).Decision);
        Assert.AreEqual(
            SkillPromotionDecision.Rejected,
            unsafeMemory.PromoteVerifiedTransition(verified.Request with
            {
                Before = verified.Before with { ContainsRawScreenshot = true }
            }).Decision);

        var conflictMemory = new ExecutableSkillMemory();
        var conflictingAfter = SnapshotWithClaims(
            "saved",
            101,
            [
                Claim("editor", "value", "saved", Provenance.Uia),
                Claim("editor", "value", "deleted", Provenance.Vision)
            ]);
        Assert.IsTrue(conflictingAfter.HasBlockingConflicts);
        Assert.AreEqual(
            SkillPromotionDecision.Rejected,
            conflictMemory.PromoteVerifiedTransition(verified.Request with { After = conflictingAfter }).Decision);

        Assert.AreEqual(0, unchangedMemory.List().Count);
        Assert.AreEqual(0, unsafeMemory.List().Count);
        Assert.AreEqual(0, conflictMemory.List().Count);
    }

    [TestMethod]
    public void VerifiedPromotionReverificationAndReplayAreDeterministicAndNonAuthoritative()
    {
        var memory = new ExecutableSkillMemory();
        var fixture = VerifiedFixture("draft", "saved");
        var first = memory.PromoteVerifiedTransition(fixture.Request);
        var firstSkill = RequiredSkill(first);
        var firstTransition = RequiredTransition(first);
        var replay = memory.FindReplay(ReplayRequest(firstSkill, fixture.Before.StateFingerprint));
        var second = memory.PromoteVerifiedTransition(fixture.Request with
        {
            VerifiedAtUtc = fixture.VerifiedAtUtc.AddMinutes(5),
            EvidenceRefs = ["evidence:semantic", "evidence:second-verification"]
        });
        var secondSkill = RequiredSkill(second);
        var secondTransition = RequiredTransition(second);

        Assert.AreEqual(SkillPromotionDecision.CreatedVerifiedSkill, first.Decision);
        Assert.AreEqual(ExecutableSkillState.Verified, firstSkill.State);
        Assert.AreEqual(SkillReplayDecision.Ready, replay.Decision);
        Assert.AreEqual(firstTransition.TransitionId, replay.Transition?.TransitionId);
        Assert.IsFalse(replay.ActionAuthorityGranted);
        Assert.IsTrue(replay.RequiresExistingMissionAuthorization);
        Assert.IsFalse(firstSkill.LiveExecutionAuthorityGranted);

        Assert.AreEqual(SkillPromotionDecision.ReverifiedExistingTransition, second.Decision);
        Assert.AreEqual(1, secondSkill.Transitions.Count);
        Assert.AreEqual(2, secondTransition.SuccessfulRuns);
        Assert.AreEqual(firstSkill.Version, secondSkill.Version);
        Assert.AreEqual(firstTransition.TransitionId, secondTransition.TransitionId);
        Assert.IsTrue(secondTransition.EvidenceRefs.Contains("evidence:second-verification", StringComparer.Ordinal));
    }

    [TestMethod]
    public void ReplayFailsClosedOnProfileVersionCapabilityAndStateMismatch()
    {
        var memory = new ExecutableSkillMemory();
        var fixture = VerifiedFixture("draft", "saved");
        var promoted = memory.PromoteVerifiedTransition(fixture.Request);
        var skill = RequiredSkill(promoted);
        var unrelatedState = Snapshot("other", 101).StateFingerprint;

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
        var state = memory.FindReplay(ReplayRequest(skill, unrelatedState));

        Assert.AreEqual(SkillReplayDecision.ProfileMismatch, profile.Decision);
        Assert.AreEqual(SkillReplayDecision.ProfileVersionMismatch, version.Decision);
        Assert.AreEqual(SkillReplayDecision.CapabilityNotAuthorized, capability.Decision);
        Assert.AreEqual(SkillReplayDecision.CurrentStateNotFound, state.Decision);
    }

    [TestMethod]
    public void OneDegradedTransitionDoesNotDisableUnrelatedVerifiedTransition()
    {
        var memory = new ExecutableSkillMemory();
        var firstFixture = VerifiedFixture("draft", "saved", "skill-multi-state");
        var secondFixture = VerifiedFixture("closed", "opened", "skill-multi-state");
        var first = memory.PromoteVerifiedTransition(firstFixture.Request);
        var second = memory.PromoteVerifiedTransition(secondFixture.Request);
        var firstSkill = RequiredSkill(first);
        var firstTransition = RequiredTransition(first);
        var secondTransition = RequiredTransition(second);

        var failure = memory.RecordTransitionFailure(new SkillTransitionFailureObservation(
            firstSkill.SkillId,
            firstTransition.TransitionId,
            SemanticVerificationFailureClass.TargetNotFound,
            FailureKind.NotFound,
            firstFixture.VerifiedAtUtc.AddMinutes(1),
            ["evidence:target-missing"],
            "The target moved after an application update."));
        var failedSkill = failure.Skill ?? throw new AssertFailedException("Expected degraded skill.");
        var degradedReplay = memory.FindReplay(ReplayRequest(failedSkill, firstFixture.Before.StateFingerprint));
        var unaffectedReplay = memory.FindReplay(ReplayRequest(failedSkill, secondFixture.Before.StateFingerprint));

        Assert.AreEqual(SkillFailureDecision.TransitionDegraded, failure.Decision);
        Assert.AreEqual(ExecutableSkillState.Degraded, failedSkill.State);
        Assert.AreEqual(VerifiedSkillTransitionState.Degraded, failure.Transition?.State);
        Assert.AreEqual(SkillReplayDecision.TransitionDegraded, degradedReplay.Decision);
        Assert.AreEqual(SkillReplayDecision.Ready, unaffectedReplay.Decision);
        Assert.AreEqual(secondTransition.TransitionId, unaffectedReplay.Transition?.TransitionId);
        Assert.AreEqual(2, failedSkill.Transitions.Count);
    }

    [TestMethod]
    public void SevereSideEffectInvalidatesSkillButUserInterruptionDoesNotPunishQuality()
    {
        var interruptedMemory = new ExecutableSkillMemory();
        var fixture = VerifiedFixture("draft", "saved");
        var interruptedPromotion = interruptedMemory.PromoteVerifiedTransition(fixture.Request);
        var interruptedSkill = RequiredSkill(interruptedPromotion);
        var interruptedTransition = RequiredTransition(interruptedPromotion);
        var interruption = interruptedMemory.RecordTransitionFailure(new SkillTransitionFailureObservation(
            interruptedSkill.SkillId,
            interruptedTransition.TransitionId,
            SemanticVerificationFailureClass.UserInterrupted,
            FailureKind.HumanInterrupted,
            fixture.VerifiedAtUtc.AddMinutes(1),
            ["evidence:user-interruption"],
            "The operator paused the mission."));

        Assert.AreEqual(SkillFailureDecision.IgnoredExternalInterruption, interruption.Decision);
        Assert.AreEqual(ExecutableSkillState.Verified, interruption.Skill?.State);
        Assert.AreEqual(0, interruption.Transition?.FailedRuns);

        var severeMemory = new ExecutableSkillMemory();
        var severePromotion = severeMemory.PromoteVerifiedTransition(fixture.Request);
        var severeSkill = RequiredSkill(severePromotion);
        var severeTransition = RequiredTransition(severePromotion);
        var failure = severeMemory.RecordTransitionFailure(new SkillTransitionFailureObservation(
            severeSkill.SkillId,
            severeTransition.TransitionId,
            SemanticVerificationFailureClass.UnexpectedSideEffect,
            FailureKind.PolicyDenied,
            fixture.VerifiedAtUtc.AddMinutes(1),
            ["evidence:unexpected-side-effect"],
            "A neighboring semantic object changed unexpectedly."));
        var invalidatedSkill = failure.Skill ?? throw new AssertFailedException("Expected invalidated skill.");

        Assert.AreEqual(SkillFailureDecision.SkillInvalidated, failure.Decision);
        Assert.AreEqual(ExecutableSkillState.Invalidated, invalidatedSkill.State);
        Assert.AreEqual(VerifiedSkillTransitionState.Invalidated, failure.Transition?.State);
        Assert.AreEqual(
            SkillReplayDecision.Invalidated,
            severeMemory.FindReplay(ReplayRequest(invalidatedSkill, fixture.Before.StateFingerprint)).Decision);
    }

    [TestMethod]
    public void LocalizedRepairSupersedesOnlyFailedTransitionAndPreservesOtherStates()
    {
        var memory = new ExecutableSkillMemory();
        var firstFixture = VerifiedFixture("draft", "saved", "skill-repair");
        var secondFixture = VerifiedFixture("closed", "opened", "skill-repair");
        var first = memory.PromoteVerifiedTransition(firstFixture.Request);
        var second = memory.PromoteVerifiedTransition(secondFixture.Request);
        var firstSkill = RequiredSkill(first);
        var firstTransition = RequiredTransition(first);
        var secondTransition = RequiredTransition(second);
        var degraded = memory.RecordTransitionFailure(new SkillTransitionFailureObservation(
            firstSkill.SkillId,
            firstTransition.TransitionId,
            SemanticVerificationFailureClass.TargetNotFound,
            FailureKind.NotFound,
            firstFixture.VerifiedAtUtc.AddMinutes(1),
            ["evidence:locator-drift"],
            "The original selector alias no longer resolves."));
        var degradedSkill = degraded.Skill ?? throw new AssertFailedException("Expected degraded skill.");
        var degradedTransition = degraded.Transition ?? throw new AssertFailedException("Expected degraded transition.");
        var repairedAction = firstFixture.Action with
        {
            TemplateId = "set-editor-value-repaired",
            SelectorAliasRefs = ["app-profile:editor:stable-alias-v2"]
        };

        var repair = memory.RepairTransition(new SkillRepairRequest(
            degradedSkill.SkillId,
            degradedTransition.TransitionId,
            firstFixture.Before,
            firstFixture.After,
            repairedAction,
            firstFixture.Plan,
            firstFixture.Report,
            firstFixture.VerifiedAtUtc.AddMinutes(2),
            ["evidence:localized-repair"]));
        var repairedSkill = repair.Skill ?? throw new AssertFailedException("Expected repaired skill.");
        var repairedTransition = repair.RepairedTransition ?? throw new AssertFailedException("Expected repaired transition.");
        var superseded = repair.SupersededTransition ?? throw new AssertFailedException("Expected superseded transition.");

        Assert.AreEqual(SkillRepairDecision.Repaired, repair.Decision);
        Assert.AreEqual(ExecutableSkillState.Verified, repairedSkill.State);
        Assert.AreEqual(3, repairedSkill.Transitions.Count);
        Assert.AreEqual(VerifiedSkillTransitionState.Invalidated, superseded.State);
        Assert.AreEqual(repairedTransition.TransitionId, superseded.SupersededByTransitionId);
        Assert.AreEqual("app-profile:editor:stable-alias-v2", repairedTransition.Action.SelectorAliasRefs.Single());
        Assert.AreEqual(
            repairedTransition.TransitionId,
            memory.FindReplay(ReplayRequest(repairedSkill, firstFixture.Before.StateFingerprint)).Transition?.TransitionId);
        Assert.AreEqual(
            secondTransition.TransitionId,
            memory.FindReplay(ReplayRequest(repairedSkill, secondFixture.Before.StateFingerprint)).Transition?.TransitionId);
    }

    [TestMethod]
    public void LocalizedRepairCannotChangeCapabilityOrOperation()
    {
        var memory = new ExecutableSkillMemory();
        var fixture = VerifiedFixture("draft", "saved");
        var promoted = memory.PromoteVerifiedTransition(fixture.Request);
        var skill = RequiredSkill(promoted);
        var transition = RequiredTransition(promoted);
        var degraded = memory.RecordTransitionFailure(new SkillTransitionFailureObservation(
            skill.SkillId,
            transition.TransitionId,
            SemanticVerificationFailureClass.TargetNotFound,
            FailureKind.NotFound,
            fixture.VerifiedAtUtc.AddMinutes(1),
            ["evidence:drift"],
            "Target drift."));
        var degradedSkill = degraded.Skill ?? throw new AssertFailedException("Expected degraded skill.");
        var degradedTransition = degraded.Transition ?? throw new AssertFailedException("Expected degraded transition.");
        var changedCapability = fixture.Action with
        {
            TemplateId = "unsafe-capability-change",
            CapabilityId = "browser.action.execute"
        };
        var versionBefore = degradedSkill.Version;

        var result = memory.RepairTransition(new SkillRepairRequest(
            degradedSkill.SkillId,
            degradedTransition.TransitionId,
            fixture.Before,
            fixture.After,
            changedCapability,
            fixture.Plan,
            fixture.Report,
            fixture.VerifiedAtUtc.AddMinutes(2),
            ["evidence:invalid-repair"]));
        var stored = memory.Get(degradedSkill.SkillId) ?? throw new AssertFailedException("Expected stored skill.");

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
        var skill = RequiredSkill(promoted);
        var archived = memory.Archive(
            skill.SkillId,
            fixture.VerifiedAtUtc.AddDays(1),
            ["evidence:archived"]);

        var replay = memory.FindReplay(ReplayRequest(archived, fixture.Before.StateFingerprint));

        Assert.AreEqual(ExecutableSkillState.Archived, archived.State);
        Assert.AreEqual(SkillReplayDecision.Archived, replay.Decision);
        Assert.IsFalse(replay.Ready);
    }

    [TestMethod]
    public void ProcessMemoryProjectionUsesExistingStoreWithoutRawDataOrSecondLedger()
    {
        var memory = new ExecutableSkillMemory();
        var fixture = VerifiedFixture("draft", "saved", recipeId: "recipe-verified-editor");
        var promoted = memory.PromoteVerifiedTransition(fixture.Request);
        var skill = RequiredSkill(promoted);
        var projection = skill.ToProcessMemoryEntry();
        var root = Path.Combine(Path.GetTempPath(), "nodal-skill-memory-" + Guid.NewGuid().ToString("N"));
        try
        {
            var write = ProcessMemoryStore.Write(root, projection);
            var loaded = ProcessMemoryStore.ReadById(root, projection.Id)
                         ?? throw new AssertFailedException("Expected process memory projection to load.");
            var json = JsonSerializer.Serialize(projection);

            Assert.AreEqual(ProcessMemoryStatuses.Stable, projection.Status);
            Assert.AreEqual(ProcessMemorySources.Recipe, projection.Source);
            Assert.AreEqual("recipe-verified-editor", projection.Links.RecipeId);
            Assert.AreEqual(skill.SkillFingerprint, projection.Links.ConfidenceId);
            Assert.IsTrue(projection.ConfidenceScore >= 75);
            Assert.IsTrue(write.Success, write.Error);
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
    public void TransitionAndSkillIdentityIgnoreEvidenceAndVerificationTime()
    {
        var firstMemory = new ExecutableSkillMemory();
        var secondMemory = new ExecutableSkillMemory();
        var firstFixture = VerifiedFixture("draft", "saved");
        var secondFixture = VerifiedFixture("draft", "saved");
        var secondRequest = secondFixture.Request with
        {
            VerifiedAtUtc = DateTimeOffset.Parse("2026-07-16T00:00:00Z"),
            EvidenceRefs = ["evidence:different-run"]
        };

        var first = firstMemory.PromoteVerifiedTransition(firstFixture.Request);
        var second = secondMemory.PromoteVerifiedTransition(secondRequest);
        var firstSkill = RequiredSkill(first);
        var secondSkill = RequiredSkill(second);
        var firstTransition = RequiredTransition(first);
        var secondTransition = RequiredTransition(second);

        Assert.AreEqual(firstTransition.TransitionId, secondTransition.TransitionId);
        Assert.AreEqual(firstTransition.TransitionFingerprint, secondTransition.TransitionFingerprint);
        Assert.AreEqual(firstSkill.SkillFingerprint, secondSkill.SkillFingerprint);
    }

    private static ExecutableSkill RequiredSkill(SkillPromotionResult result) =>
        result.Skill ?? throw new AssertFailedException("Expected promoted skill.");

    private static VerifiedSkillTransition RequiredTransition(SkillPromotionResult result) =>
        result.Transition ?? throw new AssertFailedException("Expected promoted transition.");

    private static SkillReplayRequest ReplayRequest(ExecutableSkill skill, string currentStateFingerprint) =>
        new(
            skill.SkillId,
            skill.AppProfileId,
            skill.AppProfileVersion,
            currentStateFingerprint,
            new HashSet<string>(skill.RequiredCapabilities, StringComparer.Ordinal));

    private static VerifiedFixtureData VerifiedFixture(
        string beforeValue,
        string afterValue,
        string skillId = "skill-editor-save",
        string? recipeId = "recipe-editor-save")
    {
        var before = Snapshot(beforeValue, 101);
        var after = Snapshot(afterValue, 101);
        var plan = new SemanticVerificationPlan(
            PlanId: "verify-editor-transition",
            Preconditions: [Rule("editor-present", SemanticVerificationRuleKind.ElementPresent, "editor")],
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
                before,
                after,
                ActionExecuted: true,
                ActionRejected: false,
                UserInterrupted: false,
                Elapsed: TimeSpan.FromMilliseconds(50),
                EvidenceRefs: ["evidence:semantic"]));
        var verifiedAt = DateTimeOffset.Parse("2026-07-15T00:00:00Z");
        var candidate = new SkillCandidateRequest(
            skillId,
            "Set fixture editor semantic value",
            "fixture-editor",
            1,
            recipeId,
            null,
            "run-fixture-editor",
            new HashSet<string>(StringComparer.Ordinal) { "desktop.uia.action" },
            "low",
            verifiedAt.AddMinutes(-1),
            ["evidence:candidate"],
            ExecutableSkillState.Candidate);
        var action = Action();
        var request = new SkillPromotionRequest(
            candidate,
            before,
            after,
            action,
            plan,
            report,
            verifiedAt,
            ["evidence:semantic", "evidence:skill-promotion"]);
        return new VerifiedFixtureData(before, after, plan, report, action, request, verifiedAt);
    }

    private static SkillActionTemplate Action() =>
        new(
            TemplateId: "set-editor-value",
            CapabilityId: "desktop.uia.action",
            Operation: "set-value",
            SemanticTargetRef: "editor-semantic-role",
            Parameters: [new SkillParameterBinding("TEXT", "variable-ref:TEXT")],
            SelectorAliasRefs: ["app-profile:editor:stable-alias"],
            RecoveryAlternatives:
            [
                new SkillRecoveryAlternative(
                    "reobserve-editor",
                    SkillRecoveryKind.ReobserveApplication,
                    "Capture a fresh semantic snapshot of the fixture editor.",
                    null,
                    ["evidence:recovery-design"]),
                new SkillRecoveryAlternative(
                    "alternate-editor-alias",
                    SkillRecoveryKind.AlternateSelectorAlias,
                    "Try the validated secondary app-profile selector alias.",
                    "app-profile:editor:secondary-alias",
                    ["evidence:selector-alias"])
            ],
            RiskLevel: "low",
            RequiresExistingMissionAuthorization: true);

    private static CognitiveSnapshotV2 Snapshot(string value, int processId) =>
        SnapshotWithClaims(value, processId, [Claim("editor", "value", value, Provenance.Fixture)]);

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
            subjectRef,
            property,
            value,
            provenance,
            1d,
            DateTimeOffset.Parse("2026-07-15T00:00:00Z"),
            "evidence:claim:" + provenance);

    private static SemanticVerificationRule Rule(
        string ruleId,
        SemanticVerificationRuleKind kind,
        string? subjectRef = null,
        string? property = null,
        string? expected = null) =>
        new(ruleId, kind, subjectRef, property, expected, Required: true);

    private sealed record VerifiedFixtureData(
        CognitiveSnapshotV2 Before,
        CognitiveSnapshotV2 After,
        SemanticVerificationPlan Plan,
        SemanticVerificationReport Report,
        SkillActionTemplate Action,
        SkillPromotionRequest Request,
        DateTimeOffset VerifiedAtUtc);
}
