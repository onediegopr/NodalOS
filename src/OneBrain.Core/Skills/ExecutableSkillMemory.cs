using System.Security.Cryptography;
using System.Text;
using OneBrain.Core.Contracts;
using OneBrain.Core.History;
using OneBrain.Core.Perception;
using OneBrain.Core.Runtime;
using OneBrain.Core.Verification;

namespace OneBrain.Core.Skills;

public sealed class ExecutableSkillMemory
{
    private const int FailureCountBeforeInvalidation = 3;

    private readonly object _gate = new();
    private readonly Dictionary<string, ExecutableSkill> _skills = new(StringComparer.Ordinal);

    public IReadOnlyList<ExecutableSkill> List()
    {
        lock (_gate)
        {
            return _skills.Values
                .OrderBy(skill => skill.SkillId, StringComparer.Ordinal)
                .ToArray();
        }
    }

    public ExecutableSkill? Get(string skillId)
    {
        var normalizedSkillId = Identifier(skillId, 120);
        lock (_gate)
        {
            return _skills.GetValueOrDefault(normalizedSkillId);
        }
    }

    public ExecutableSkill RegisterCandidate(SkillCandidateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalized = NormalizeCandidate(request);

        lock (_gate)
        {
            if (_skills.TryGetValue(normalized.SkillId, out var existing))
            {
                EnsureCandidateIdentityMatches(existing, normalized);
                return existing;
            }

            var skill = new ExecutableSkill(
                SkillId: normalized.SkillId,
                Version: 1,
                TitleRedacted: normalized.TitleRedacted,
                AppProfileId: normalized.AppProfileId,
                AppProfileVersion: normalized.AppProfileVersion,
                RecipeId: normalized.RecipeId,
                ProcessMemoryId: normalized.ProcessMemoryId,
                LastRunId: normalized.RunId,
                State: normalized.InitialState,
                RequiredCapabilities: normalized.RequiredCapabilities,
                RiskLevel: normalized.RiskLevel,
                Transitions: [],
                EvidenceRefs: normalized.EvidenceRefs,
                CreatedAtUtc: normalized.ObservedAtUtc,
                UpdatedAtUtc: normalized.ObservedAtUtc,
                SkillFingerprint: string.Empty);
            skill = RefreshFingerprint(skill);
            _skills.Add(skill.SkillId, skill);
            return skill;
        }
    }

    public ExecutableSkill MarkSupervised(
        string skillId,
        DateTimeOffset supervisedAtUtc,
        IReadOnlyList<string> evidenceRefs)
    {
        var normalizedSkillId = Identifier(skillId, 120);
        var normalizedEvidence = Evidence(evidenceRefs);
        if (normalizedEvidence.Count == 0)
            throw new ArgumentException("Supervision requires at least one evidence reference.", nameof(evidenceRefs));

        lock (_gate)
        {
            var existing = RequireSkill(normalizedSkillId);
            if (existing.State is not (ExecutableSkillState.Observed or ExecutableSkillState.Candidate or ExecutableSkillState.Supervised))
                throw new InvalidOperationException($"Skill state '{existing.State}' cannot be marked supervised.");

            if (existing.State == ExecutableSkillState.Supervised)
                return existing;

            var updated = existing with
            {
                Version = existing.Version + 1,
                State = ExecutableSkillState.Supervised,
                EvidenceRefs = MergeEvidence(existing.EvidenceRefs, normalizedEvidence),
                UpdatedAtUtc = supervisedAtUtc
            };
            updated = RefreshFingerprint(updated);
            _skills[updated.SkillId] = updated;
            return updated;
        }
    }

    public SkillPromotionResult PromoteVerifiedTransition(SkillPromotionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Candidate);
        ArgumentNullException.ThrowIfNull(request.Before);
        ArgumentNullException.ThrowIfNull(request.After);
        ArgumentNullException.ThrowIfNull(request.Action);
        ArgumentNullException.ThrowIfNull(request.VerificationPlan);
        ArgumentNullException.ThrowIfNull(request.VerificationReport);
        ArgumentNullException.ThrowIfNull(request.EvidenceRefs);

        var candidate = NormalizeCandidate(request.Candidate);
        var actionValidation = NormalizeAndValidateAction(request.Action);
        var verificationValidation = ValidatePromotionEvidence(
            request.Before,
            request.After,
            request.VerificationPlan,
            request.VerificationReport,
            request.EvidenceRefs);
        var evidenceRefs = Evidence(
            request.EvidenceRefs
                .Concat(request.VerificationReport.EvidenceRefs)
                .Concat(request.Before.EvidenceRefs)
                .Concat(request.After.EvidenceRefs));

        if (!actionValidation.Valid || !verificationValidation.Valid)
        {
            var reasons = actionValidation.Reasons.Concat(verificationValidation.Reasons).ToArray();
            return RejectedPromotion(
                reasons.Length == 0 ? "Verified transition promotion was rejected." : string.Join(" | ", reasons),
                evidenceRefs);
        }

        var action = actionValidation.Action!;
        var transitionId = BuildTransitionId(
            candidate.SkillId,
            request.Before.StateFingerprint,
            request.After.StateFingerprint,
            action,
            request.VerificationPlan.PlanId,
            candidate.AppProfileVersion);
        var transitionFingerprint = BuildTransitionFingerprint(
            request.Before.StateFingerprint,
            request.After.StateFingerprint,
            action,
            request.VerificationPlan.PlanId,
            candidate.AppProfileVersion);

        lock (_gate)
        {
            var createdSkill = false;
            if (!_skills.TryGetValue(candidate.SkillId, out var skill))
            {
                skill = new ExecutableSkill(
                    SkillId: candidate.SkillId,
                    Version: 1,
                    TitleRedacted: candidate.TitleRedacted,
                    AppProfileId: candidate.AppProfileId,
                    AppProfileVersion: candidate.AppProfileVersion,
                    RecipeId: candidate.RecipeId,
                    ProcessMemoryId: candidate.ProcessMemoryId,
                    LastRunId: candidate.RunId,
                    State: candidate.InitialState,
                    RequiredCapabilities: candidate.RequiredCapabilities,
                    RiskLevel: candidate.RiskLevel,
                    Transitions: [],
                    EvidenceRefs: candidate.EvidenceRefs,
                    CreatedAtUtc: candidate.ObservedAtUtc,
                    UpdatedAtUtc: candidate.ObservedAtUtc,
                    SkillFingerprint: string.Empty);
                skill = RefreshFingerprint(skill);
                createdSkill = true;
            }
            else
            {
                EnsureCandidateIdentityMatches(skill, candidate);
            }

            var existingTransition = skill.Transitions.FirstOrDefault(transition =>
                string.Equals(transition.TransitionId, transitionId, StringComparison.Ordinal));
            if (existingTransition is not null)
            {
                var reverifiedTransition = existingTransition with
                {
                    State = VerifiedSkillTransitionState.Verified,
                    SuccessfulRuns = checked(existingTransition.SuccessfulRuns + 1),
                    LastFailureClass = null,
                    LastFailureKind = null,
                    LastVerifiedAtUtc = request.VerifiedAtUtc,
                    EvidenceRefs = MergeEvidence(existingTransition.EvidenceRefs, evidenceRefs),
                    SupersededByTransitionId = null
                };
                var transitions = ReplaceTransition(skill.Transitions, reverifiedTransition);
                var updated = skill with
                {
                    State = DeriveSkillState(skill.State, transitions),
                    LastRunId = candidate.RunId ?? skill.LastRunId,
                    Transitions = transitions,
                    EvidenceRefs = MergeEvidence(skill.EvidenceRefs, evidenceRefs),
                    UpdatedAtUtc = request.VerifiedAtUtc
                };
                updated = RefreshFingerprint(updated);
                _skills[updated.SkillId] = updated;
                return new SkillPromotionResult(
                    Decision: SkillPromotionDecision.ReverifiedExistingTransition,
                    Code: "SKILL_TRANSITION_REVERIFIED",
                    Reason: "The existing transition was verified again; no duplicate transition was created.",
                    Skill: updated,
                    Transition: reverifiedTransition,
                    EvidenceRefs: evidenceRefs);
            }

            var transition = new VerifiedSkillTransition(
                TransitionId: transitionId,
                FromStateFingerprint: request.Before.StateFingerprint,
                ToStateFingerprint: request.After.StateFingerprint,
                Action: action,
                VerificationPlanId: Identifier(request.VerificationPlan.PlanId, 160),
                AppProfileVersion: candidate.AppProfileVersion,
                State: VerifiedSkillTransitionState.Verified,
                SuccessfulRuns: 1,
                FailedRuns: 0,
                LastFailureClass: null,
                LastFailureKind: null,
                LastVerifiedAtUtc: request.VerifiedAtUtc,
                LastFailureAtUtc: null,
                EvidenceRefs: evidenceRefs,
                SupersededByTransitionId: null,
                TransitionFingerprint: transitionFingerprint);
            var addedTransitions = skill.Transitions
                .Append(transition)
                .OrderBy(value => value.FromStateFingerprint, StringComparer.Ordinal)
                .ThenBy(value => value.TransitionId, StringComparer.Ordinal)
                .ToArray();
            var promoted = skill with
            {
                Version = skill.Version + 1,
                State = DeriveSkillState(ExecutableSkillState.Verified, addedTransitions),
                LastRunId = candidate.RunId ?? skill.LastRunId,
                Transitions = addedTransitions,
                EvidenceRefs = MergeEvidence(skill.EvidenceRefs, evidenceRefs),
                UpdatedAtUtc = request.VerifiedAtUtc
            };
            promoted = RefreshFingerprint(promoted);
            _skills[promoted.SkillId] = promoted;

            return new SkillPromotionResult(
                Decision: createdSkill
                    ? SkillPromotionDecision.CreatedVerifiedSkill
                    : SkillPromotionDecision.AddedVerifiedTransition,
                Code: createdSkill ? "VERIFIED_SKILL_CREATED" : "SKILL_TRANSITION_ADDED",
                Reason: "A transition entered executable memory only after semantic verification and complete evidence.",
                Skill: promoted,
                Transition: transition,
                EvidenceRefs: evidenceRefs);
        }
    }

    public SkillFailureResult RecordTransitionFailure(SkillTransitionFailureObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        ArgumentNullException.ThrowIfNull(observation.EvidenceRefs);
        var skillId = Identifier(observation.SkillId, 120);
        var transitionId = Identifier(observation.TransitionId, 180);
        var evidenceRefs = Evidence(observation.EvidenceRefs);
        var reason = SafeRuntimeText.Sanitize(observation.ReasonRedacted, 500);

        if (observation.FailureClass == SemanticVerificationFailureClass.None)
        {
            return new SkillFailureResult(
                SkillFailureDecision.Rejected,
                "SKILL_FAILURE_CLASS_REQUIRED",
                "A successful verification cannot be recorded as a transition failure.",
                null,
                null,
                evidenceRefs);
        }

        lock (_gate)
        {
            if (!_skills.TryGetValue(skillId, out var skill))
            {
                return new SkillFailureResult(
                    SkillFailureDecision.Rejected,
                    "SKILL_NOT_FOUND",
                    "The skill was not found.",
                    null,
                    null,
                    evidenceRefs);
            }

            var transition = skill.Transitions.FirstOrDefault(value =>
                string.Equals(value.TransitionId, transitionId, StringComparison.Ordinal));
            if (transition is null)
            {
                return new SkillFailureResult(
                    SkillFailureDecision.Rejected,
                    "SKILL_TRANSITION_NOT_FOUND",
                    "The transition was not found.",
                    skill,
                    null,
                    evidenceRefs);
            }

            if (observation.FailureClass is SemanticVerificationFailureClass.UserInterrupted or
                SemanticVerificationFailureClass.ActionRejected)
            {
                return new SkillFailureResult(
                    SkillFailureDecision.IgnoredExternalInterruption,
                    "SKILL_FAILURE_NOT_ATTRIBUTED_TO_TRANSITION",
                    "User interruption or policy rejection does not degrade learned transition quality.",
                    skill,
                    transition,
                    evidenceRefs);
            }

            var failureCount = checked(transition.FailedRuns + 1);
            var severe = observation.FailureClass is
                SemanticVerificationFailureClass.UnexpectedSideEffect or
                SemanticVerificationFailureClass.ApplicationCrashed or
                SemanticVerificationFailureClass.EnvironmentChanged;
            var nextState = severe || failureCount >= FailureCountBeforeInvalidation
                ? VerifiedSkillTransitionState.Invalidated
                : VerifiedSkillTransitionState.Degraded;
            var updatedTransition = transition with
            {
                State = nextState,
                FailedRuns = failureCount,
                LastFailureClass = observation.FailureClass,
                LastFailureKind = observation.FailureKind,
                LastFailureAtUtc = observation.FailedAtUtc,
                EvidenceRefs = MergeEvidence(transition.EvidenceRefs, evidenceRefs)
            };
            var transitions = ReplaceTransition(skill.Transitions, updatedTransition);
            var updatedSkillState = DeriveSkillState(skill.State, transitions);
            var updatedSkill = skill with
            {
                Version = skill.Version + 1,
                State = updatedSkillState,
                Transitions = transitions,
                EvidenceRefs = MergeEvidence(skill.EvidenceRefs, evidenceRefs),
                UpdatedAtUtc = observation.FailedAtUtc
            };
            updatedSkill = RefreshFingerprint(updatedSkill);
            _skills[updatedSkill.SkillId] = updatedSkill;

            var decision = updatedSkillState == ExecutableSkillState.Invalidated
                ? SkillFailureDecision.SkillInvalidated
                : nextState == VerifiedSkillTransitionState.Invalidated
                    ? SkillFailureDecision.TransitionInvalidated
                    : SkillFailureDecision.TransitionDegraded;
            return new SkillFailureResult(
                Decision: decision,
                Code: decision switch
                {
                    SkillFailureDecision.SkillInvalidated => "SKILL_INVALIDATED",
                    SkillFailureDecision.TransitionInvalidated => "SKILL_TRANSITION_INVALIDATED",
                    _ => "SKILL_TRANSITION_DEGRADED"
                },
                Reason: reason.Length == 0
                    ? "Verified transition quality changed after a controlled failure observation."
                    : reason,
                Skill: updatedSkill,
                Transition: updatedTransition,
                EvidenceRefs: evidenceRefs);
        }
    }

    public SkillRepairResult RepairTransition(SkillRepairRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Before);
        ArgumentNullException.ThrowIfNull(request.After);
        ArgumentNullException.ThrowIfNull(request.RepairedAction);
        ArgumentNullException.ThrowIfNull(request.VerificationPlan);
        ArgumentNullException.ThrowIfNull(request.VerificationReport);
        ArgumentNullException.ThrowIfNull(request.EvidenceRefs);

        var skillId = Identifier(request.SkillId, 120);
        var transitionId = Identifier(request.TransitionId, 180);
        var actionValidation = NormalizeAndValidateAction(request.RepairedAction);
        var verificationValidation = ValidatePromotionEvidence(
            request.Before,
            request.After,
            request.VerificationPlan,
            request.VerificationReport,
            request.EvidenceRefs);
        var evidenceRefs = Evidence(
            request.EvidenceRefs
                .Concat(request.VerificationReport.EvidenceRefs)
                .Concat(request.Before.EvidenceRefs)
                .Concat(request.After.EvidenceRefs));

        if (!actionValidation.Valid || !verificationValidation.Valid)
        {
            return RejectedRepair(
                string.Join(" | ", actionValidation.Reasons.Concat(verificationValidation.Reasons)),
                evidenceRefs);
        }

        lock (_gate)
        {
            if (!_skills.TryGetValue(skillId, out var skill))
                return RejectedRepair("The skill was not found.", evidenceRefs);

            var original = skill.Transitions.FirstOrDefault(value =>
                string.Equals(value.TransitionId, transitionId, StringComparison.Ordinal));
            if (original is null)
                return RejectedRepair("The transition was not found.", evidenceRefs, skill);
            if (original.State == VerifiedSkillTransitionState.Verified)
                return RejectedRepair("Only a degraded or invalidated transition may be repaired.", evidenceRefs, skill, original);
            if (!string.IsNullOrWhiteSpace(original.SupersededByTransitionId))
                return RejectedRepair("The transition was already superseded by a localized repair.", evidenceRefs, skill, original);
            if (!string.Equals(original.FromStateFingerprint, request.Before.StateFingerprint, StringComparison.Ordinal) ||
                !string.Equals(original.ToStateFingerprint, request.After.StateFingerprint, StringComparison.Ordinal))
            {
                return RejectedRepair(
                    "Localized repair must preserve the verified semantic from-state and to-state.",
                    evidenceRefs,
                    skill,
                    original);
            }

            var repairedAction = actionValidation.Action!;
            if (!string.Equals(original.Action.CapabilityId, repairedAction.CapabilityId, StringComparison.Ordinal) ||
                !string.Equals(original.Action.Operation, repairedAction.Operation, StringComparison.Ordinal))
            {
                return RejectedRepair(
                    "Localized repair cannot change the transition capability or operation.",
                    evidenceRefs,
                    skill,
                    original);
            }

            var repairedTransitionId = BuildTransitionId(
                skill.SkillId,
                original.FromStateFingerprint,
                original.ToStateFingerprint,
                repairedAction,
                request.VerificationPlan.PlanId,
                skill.AppProfileVersion);
            if (string.Equals(repairedTransitionId, original.TransitionId, StringComparison.Ordinal))
            {
                return RejectedRepair(
                    "The proposed repair does not change the transition template.",
                    evidenceRefs,
                    skill,
                    original);
            }

            var repairedFingerprint = BuildTransitionFingerprint(
                original.FromStateFingerprint,
                original.ToStateFingerprint,
                repairedAction,
                request.VerificationPlan.PlanId,
                skill.AppProfileVersion);
            var superseded = original with
            {
                State = VerifiedSkillTransitionState.Invalidated,
                SupersededByTransitionId = repairedTransitionId,
                EvidenceRefs = MergeEvidence(original.EvidenceRefs, evidenceRefs)
            };
            var repaired = new VerifiedSkillTransition(
                TransitionId: repairedTransitionId,
                FromStateFingerprint: original.FromStateFingerprint,
                ToStateFingerprint: original.ToStateFingerprint,
                Action: repairedAction,
                VerificationPlanId: Identifier(request.VerificationPlan.PlanId, 160),
                AppProfileVersion: skill.AppProfileVersion,
                State: VerifiedSkillTransitionState.Verified,
                SuccessfulRuns: 1,
                FailedRuns: 0,
                LastFailureClass: null,
                LastFailureKind: null,
                LastVerifiedAtUtc: request.RepairedAtUtc,
                LastFailureAtUtc: null,
                EvidenceRefs: evidenceRefs,
                SupersededByTransitionId: null,
                TransitionFingerprint: repairedFingerprint);
            var transitions = skill.Transitions
                .Select(value => string.Equals(value.TransitionId, original.TransitionId, StringComparison.Ordinal)
                    ? superseded
                    : value)
                .Append(repaired)
                .OrderBy(value => value.FromStateFingerprint, StringComparer.Ordinal)
                .ThenBy(value => value.TransitionId, StringComparer.Ordinal)
                .ToArray();
            var updated = skill with
            {
                Version = skill.Version + 1,
                State = DeriveSkillState(skill.State, transitions),
                Transitions = transitions,
                EvidenceRefs = MergeEvidence(skill.EvidenceRefs, evidenceRefs),
                UpdatedAtUtc = request.RepairedAtUtc
            };
            updated = RefreshFingerprint(updated);
            _skills[updated.SkillId] = updated;

            return new SkillRepairResult(
                Decision: SkillRepairDecision.Repaired,
                Code: "SKILL_TRANSITION_LOCALLY_REPAIRED",
                Reason: "Only the failed transition template was replaced after semantic re-verification; other transitions were preserved.",
                Skill: updated,
                SupersededTransition: superseded,
                RepairedTransition: repaired,
                EvidenceRefs: evidenceRefs);
        }
    }

    public SkillReplaySelection FindReplay(SkillReplayRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.AuthorizedCapabilities);
        var skillId = Identifier(request.SkillId, 120);
        var appProfileId = Identifier(request.AppProfileId, 160);
        var currentFingerprint = Fingerprint(request.CurrentStateFingerprint, nameof(request.CurrentStateFingerprint));
        var authorizedCapabilities = request.AuthorizedCapabilities
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => Identifier(value, 160))
            .ToHashSet(StringComparer.Ordinal);

        lock (_gate)
        {
            if (!_skills.TryGetValue(skillId, out var skill))
                return Replay(SkillReplayDecision.SkillNotFound, "SKILL_NOT_FOUND", "The skill was not found.");
            if (skill.State == ExecutableSkillState.Archived)
                return Replay(SkillReplayDecision.Archived, "SKILL_ARCHIVED", "Archived skills are not replay candidates.", skill);
            if (skill.State == ExecutableSkillState.Invalidated)
                return Replay(SkillReplayDecision.Invalidated, "SKILL_INVALIDATED", "The skill has no verified active transition.", skill);
            if (skill.State is ExecutableSkillState.Observed or ExecutableSkillState.Candidate or ExecutableSkillState.Supervised)
                return Replay(SkillReplayDecision.SkillNotVerified, "SKILL_NOT_VERIFIED", "Observed, candidate and supervised skills cannot replay before verification.", skill);
            if (!string.Equals(skill.AppProfileId, appProfileId, StringComparison.Ordinal))
                return Replay(SkillReplayDecision.ProfileMismatch, "SKILL_APP_PROFILE_MISMATCH", "The current application profile does not match the skill.", skill);
            if (skill.AppProfileVersion != request.AppProfileVersion)
                return Replay(SkillReplayDecision.ProfileVersionMismatch, "SKILL_APP_PROFILE_VERSION_MISMATCH", "The application profile version changed; re-verification is required.", skill);

            var matching = skill.Transitions
                .Where(transition =>
                    string.Equals(transition.FromStateFingerprint, currentFingerprint, StringComparison.Ordinal) &&
                    string.IsNullOrWhiteSpace(transition.SupersededByTransitionId))
                .ToArray();
            if (matching.Length == 0)
                return Replay(SkillReplayDecision.CurrentStateNotFound, "SKILL_CURRENT_STATE_NOT_FOUND", "No transition starts from the current semantic state.", skill);

            var verified = matching.Where(transition => transition.State == VerifiedSkillTransitionState.Verified).ToArray();
            if (verified.Length == 0)
                return Replay(SkillReplayDecision.TransitionDegraded, "SKILL_TRANSITION_NOT_VERIFIED", "Matching transitions are degraded or invalidated and require localized repair.", skill, evidenceRefs: matching.SelectMany(value => value.EvidenceRefs));

            var authorized = verified.Where(transition =>
                    authorizedCapabilities.Contains(transition.Action.CapabilityId) &&
                    skill.RequiredCapabilities.All(authorizedCapabilities.Contains))
                .ToArray();
            if (authorized.Length == 0)
                return Replay(SkillReplayDecision.CapabilityNotAuthorized, "SKILL_CAPABILITY_NOT_AUTHORIZED", "Replay selection cannot expand the current mission capability scope.", skill, evidenceRefs: verified.SelectMany(value => value.EvidenceRefs));
            if (authorized.Length > 1)
                return Replay(SkillReplayDecision.Ambiguous, "SKILL_REPLAY_AMBIGUOUS", "Multiple verified transitions match the same semantic state; operator or planner disambiguation is required.", skill, evidenceRefs: authorized.SelectMany(value => value.EvidenceRefs));

            var transition = authorized[0];
            return Replay(
                SkillReplayDecision.Ready,
                "SKILL_REPLAY_TEMPLATE_READY",
                "A verified transition template was selected; existing mission authorization and policy still govern execution.",
                skill,
                transition,
                transition.EvidenceRefs);
        }
    }

    public ExecutableSkill Archive(string skillId, DateTimeOffset archivedAtUtc, IReadOnlyList<string> evidenceRefs)
    {
        var normalizedSkillId = Identifier(skillId, 120);
        var normalizedEvidence = Evidence(evidenceRefs);
        lock (_gate)
        {
            var skill = RequireSkill(normalizedSkillId);
            if (skill.State == ExecutableSkillState.Archived)
                return skill;

            var archived = skill with
            {
                Version = skill.Version + 1,
                State = ExecutableSkillState.Archived,
                EvidenceRefs = MergeEvidence(skill.EvidenceRefs, normalizedEvidence),
                UpdatedAtUtc = archivedAtUtc
            };
            archived = RefreshFingerprint(archived);
            _skills[archived.SkillId] = archived;
            return archived;
        }
    }

    private ExecutableSkill RequireSkill(string skillId) =>
        _skills.TryGetValue(skillId, out var skill)
            ? skill
            : throw new KeyNotFoundException($"Skill '{skillId}' was not found.");

    private static SkillCandidateRequest NormalizeCandidate(SkillCandidateRequest request)
    {
        if (request.InitialState is not (ExecutableSkillState.Observed or ExecutableSkillState.Candidate))
            throw new ArgumentException("A candidate may start only as Observed or Candidate.", nameof(request));
        if (request.AppProfileVersion < 1)
            throw new ArgumentOutOfRangeException(nameof(request), "Application profile version must be positive.");
        ArgumentNullException.ThrowIfNull(request.RequiredCapabilities);
        ArgumentNullException.ThrowIfNull(request.EvidenceRefs);

        var title = SafeText(request.TitleRedacted, 240, "skill title");
        var capabilities = request.RequiredCapabilities
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => Identifier(value, 160))
            .ToHashSet(StringComparer.Ordinal);
        if (capabilities.Count == 0)
            throw new ArgumentException("A skill candidate requires at least one capability.", nameof(request));

        return request with
        {
            SkillId = Identifier(request.SkillId, 120),
            TitleRedacted = title,
            AppProfileId = Identifier(request.AppProfileId, 160),
            RecipeId = NullableIdentifier(request.RecipeId, 160),
            ProcessMemoryId = NullableIdentifier(request.ProcessMemoryId, 160),
            RunId = NullableIdentifier(request.RunId, 160),
            RequiredCapabilities = capabilities,
            RiskLevel = SafeText(request.RiskLevel, 40, "risk level"),
            EvidenceRefs = Evidence(request.EvidenceRefs)
        };
    }

    private static void EnsureCandidateIdentityMatches(ExecutableSkill skill, SkillCandidateRequest candidate)
    {
        if (!string.Equals(skill.AppProfileId, candidate.AppProfileId, StringComparison.Ordinal) ||
            skill.AppProfileVersion != candidate.AppProfileVersion ||
            !string.Equals(skill.RecipeId ?? string.Empty, candidate.RecipeId ?? string.Empty, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Skill candidate identity conflicts with the existing skill.");
        }

        if (!skill.RequiredCapabilities.SetEquals(candidate.RequiredCapabilities))
            throw new InvalidOperationException("Skill candidate capabilities conflict with the existing skill.");
    }

    private static ActionValidation NormalizeAndValidateAction(SkillActionTemplate raw)
    {
        var reasons = new List<string>();
        try
        {
            ArgumentNullException.ThrowIfNull(raw.Parameters);
            ArgumentNullException.ThrowIfNull(raw.SelectorAliasRefs);
            ArgumentNullException.ThrowIfNull(raw.RecoveryAlternatives);

            var parameters = raw.Parameters.Select(parameter =>
            {
                var name = Identifier(parameter.Name, 80);
                var valueRef = SafeText(parameter.ValueRef, 240, $"parameter ref '{name}'");
                if (parameter.RawValuePresent)
                    reasons.Add($"Parameter '{name}' contains a raw value and cannot enter skill memory.");
                if (parameter.SecretByReference && !IsOpaqueSecretReference(valueRef))
                    reasons.Add($"Secret parameter '{name}' must use an opaque secret reference.");
                if (!parameter.SecretByReference && HistorySanitizer.ContainsSecretLikeContent(parameter.ValueRef))
                    reasons.Add($"Parameter '{name}' contains secret-like content.");
                if (ContainsLocalUserPath(parameter.ValueRef))
                    reasons.Add($"Parameter '{name}' contains a local user path instead of a reference.");
                return parameter with { Name = name, ValueRef = valueRef };
            }).OrderBy(parameter => parameter.Name, StringComparer.Ordinal).ToArray();

            var selectorAliasRefs = raw.SelectorAliasRefs
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => Identifier(value, 160))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var recoveryAlternatives = raw.RecoveryAlternatives.Select(recovery =>
            {
                ArgumentNullException.ThrowIfNull(recovery.EvidenceRefs);
                var summary = SafeText(recovery.SummaryRedacted, 300, "recovery summary");
                if (HistorySanitizer.ContainsSecretLikeContent(recovery.SummaryRedacted) || ContainsLocalUserPath(recovery.SummaryRedacted))
                    reasons.Add($"Recovery alternative '{recovery.RecoveryId}' contains unsafe raw content.");
                return recovery with
                {
                    RecoveryId = Identifier(recovery.RecoveryId, 120),
                    SummaryRedacted = summary,
                    SelectorAliasRef = NullableIdentifier(recovery.SelectorAliasRef, 160),
                    EvidenceRefs = Evidence(recovery.EvidenceRefs)
                };
            }).OrderBy(recovery => recovery.RecoveryId, StringComparer.Ordinal).ToArray();

            if (!raw.RequiresExistingMissionAuthorization)
                reasons.Add("Skill action templates must remain bound to existing mission authorization.");
            if (HistorySanitizer.ContainsSecretLikeContent(raw.SemanticTargetRef) || ContainsLocalUserPath(raw.SemanticTargetRef))
                reasons.Add("Semantic target contains unsafe raw content.");

            var action = raw with
            {
                TemplateId = Identifier(raw.TemplateId, 120),
                CapabilityId = Identifier(raw.CapabilityId, 160),
                Operation = Identifier(raw.Operation, 120),
                SemanticTargetRef = SafeText(raw.SemanticTargetRef, 200, "semantic target"),
                Parameters = parameters,
                SelectorAliasRefs = selectorAliasRefs,
                RecoveryAlternatives = recoveryAlternatives,
                RiskLevel = SafeText(raw.RiskLevel, 40, "risk level"),
                RequiresExistingMissionAuthorization = true
            };
            return new ActionValidation(reasons.Count == 0, action, reasons);
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentNullException)
        {
            reasons.Add(SafeRuntimeText.Sanitize(exception.Message, 300));
            return new ActionValidation(false, null, reasons);
        }
    }

    private static VerificationValidation ValidatePromotionEvidence(
        CognitiveSnapshotV2 before,
        CognitiveSnapshotV2 after,
        SemanticVerificationPlan plan,
        SemanticVerificationReport report,
        IEnumerable<string> requestEvidenceRefs)
    {
        var reasons = new List<string>();
        if (!before.SecretsExcluded || !after.SecretsExcluded)
            reasons.Add("Snapshots must exclude secrets before entering skill memory.");
        if (before.ContainsRawScreenshot || after.ContainsRawScreenshot)
            reasons.Add("Raw screenshots cannot enter executable skill memory.");
        if (before.ContainsRawDom || after.ContainsRawDom)
            reasons.Add("Raw DOM cannot enter executable skill memory.");
        if (before.ObservedContentCanChangeMissionGoal || after.ObservedContentCanChangeMissionGoal)
            reasons.Add("Observed content cannot carry mission control authority.");
        if (before.HasBlockingConflicts || after.HasBlockingConflicts)
            reasons.Add("Blocking perception conflicts must be resolved before learning a transition.");
        if (before.StateFingerprint.Length != 64 || after.StateFingerprint.Length != 64)
            reasons.Add("Semantic state fingerprints are invalid.");
        if (string.Equals(before.StateFingerprint, after.StateFingerprint, StringComparison.Ordinal))
            reasons.Add("A learned transition requires a semantic state change.");
        if (!report.Success ||
            !report.ActionExecuted ||
            !report.ProcessVerified ||
            !report.StateTransitionVerified ||
            !report.OutcomeVerified ||
            !report.SideEffectsChecked ||
            !report.EvidenceComplete ||
            report.FailureClass != SemanticVerificationFailureClass.None ||
            report.MappedFailureKind is not null)
        {
            reasons.Add("Semantic verification did not prove the complete transition and outcome.");
        }

        var allEvidence = Evidence(
            requestEvidenceRefs
                .Concat(report.EvidenceRefs)
                .Concat(before.EvidenceRefs)
                .Concat(after.EvidenceRefs));
        if (allEvidence.Count == 0)
            reasons.Add("Verified skill memory requires reference-only evidence.");
        var missingRequired = plan.RequiredEvidenceRefs
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => Identifier(value, 160))
            .Where(value => !allEvidence.Contains(value, StringComparer.Ordinal))
            .ToArray();
        if (missingRequired.Length > 0)
            reasons.Add("Verification plan evidence is incomplete: " + string.Join(", ", missingRequired));

        return new VerificationValidation(reasons.Count == 0, reasons);
    }

    private static ExecutableSkillState DeriveSkillState(
        ExecutableSkillState currentState,
        IReadOnlyList<VerifiedSkillTransition> transitions)
    {
        if (currentState == ExecutableSkillState.Archived)
            return currentState;

        var active = transitions
            .Where(transition => string.IsNullOrWhiteSpace(transition.SupersededByTransitionId))
            .ToArray();
        if (active.Length == 0)
            return currentState is ExecutableSkillState.Observed or ExecutableSkillState.Supervised
                ? currentState
                : ExecutableSkillState.Candidate;
        if (active.All(transition => transition.State == VerifiedSkillTransitionState.Invalidated))
            return ExecutableSkillState.Invalidated;
        if (active.Any(transition => transition.State != VerifiedSkillTransitionState.Verified))
            return ExecutableSkillState.Degraded;
        return ExecutableSkillState.Verified;
    }

    private static ExecutableSkill RefreshFingerprint(ExecutableSkill skill)
    {
        var builder = new StringBuilder();
        builder.Append("schema=executable-skill/v1\n");
        builder.Append("skill=").Append(skill.SkillId).Append('\n');
        builder.Append("version=").Append(skill.Version).Append('\n');
        builder.Append("app=").Append(skill.AppProfileId).Append('@').Append(skill.AppProfileVersion).Append('\n');
        builder.Append("recipe=").Append(skill.RecipeId ?? string.Empty).Append('\n');
        builder.Append("state=").Append(skill.State).Append('\n');
        foreach (var capability in skill.RequiredCapabilities.OrderBy(value => value, StringComparer.Ordinal))
            builder.Append("capability=").Append(capability).Append('\n');
        foreach (var transition in skill.Transitions.OrderBy(value => value.TransitionId, StringComparer.Ordinal))
        {
            builder.Append("transition=").Append(transition.TransitionId).Append('|')
                .Append(transition.State).Append('|')
                .Append(transition.SupersededByTransitionId ?? string.Empty).Append('|')
                .Append(transition.TransitionFingerprint).Append('\n');
        }

        return skill with { SkillFingerprint = Sha256(builder.ToString()) };
    }

    private static string BuildTransitionId(
        string skillId,
        string fromFingerprint,
        string toFingerprint,
        SkillActionTemplate action,
        string verificationPlanId,
        int appProfileVersion) =>
        "skill-transition-" + BuildTransitionFingerprint(
            fromFingerprint,
            toFingerprint,
            action,
            $"{skillId}|{verificationPlanId}",
            appProfileVersion)[..24];

    private static string BuildTransitionFingerprint(
        string fromFingerprint,
        string toFingerprint,
        SkillActionTemplate action,
        string verificationPlanId,
        int appProfileVersion)
    {
        var builder = new StringBuilder();
        builder.Append("from=").Append(Fingerprint(fromFingerprint, nameof(fromFingerprint))).Append('\n');
        builder.Append("to=").Append(Fingerprint(toFingerprint, nameof(toFingerprint))).Append('\n');
        builder.Append("template=").Append(action.TemplateId).Append('\n');
        builder.Append("capability=").Append(action.CapabilityId).Append('\n');
        builder.Append("operation=").Append(action.Operation).Append('\n');
        builder.Append("target=").Append(action.SemanticTargetRef).Append('\n');
        builder.Append("risk=").Append(action.RiskLevel).Append('\n');
        builder.Append("profileVersion=").Append(appProfileVersion).Append('\n');
        builder.Append("verification=").Append(Identifier(verificationPlanId, 240)).Append('\n');
        foreach (var parameter in action.Parameters.OrderBy(value => value.Name, StringComparer.Ordinal))
        {
            builder.Append("parameter=").Append(parameter.Name).Append('|')
                .Append(parameter.ValueRef).Append('|')
                .Append(parameter.SecretByReference).Append('\n');
        }
        foreach (var selector in action.SelectorAliasRefs.OrderBy(value => value, StringComparer.Ordinal))
            builder.Append("selector=").Append(selector).Append('\n');
        foreach (var recovery in action.RecoveryAlternatives.OrderBy(value => value.RecoveryId, StringComparer.Ordinal))
        {
            builder.Append("recovery=").Append(recovery.RecoveryId).Append('|')
                .Append(recovery.Kind).Append('|')
                .Append(recovery.SelectorAliasRef ?? string.Empty).Append('|')
                .Append(recovery.RequiresOperatorDecision).Append('\n');
        }
        return Sha256(builder.ToString());
    }

    private static IReadOnlyList<VerifiedSkillTransition> ReplaceTransition(
        IReadOnlyList<VerifiedSkillTransition> transitions,
        VerifiedSkillTransition replacement) =>
        transitions
            .Select(transition => string.Equals(
                    transition.TransitionId,
                    replacement.TransitionId,
                    StringComparison.Ordinal)
                ? replacement
                : transition)
            .OrderBy(transition => transition.FromStateFingerprint, StringComparer.Ordinal)
            .ThenBy(transition => transition.TransitionId, StringComparer.Ordinal)
            .ToArray();

    private static SkillPromotionResult RejectedPromotion(string reason, IReadOnlyList<string> evidenceRefs) =>
        new(
            SkillPromotionDecision.Rejected,
            "SKILL_PROMOTION_REJECTED",
            SafeRuntimeText.Sanitize(reason, 1000),
            null,
            null,
            evidenceRefs);

    private static SkillRepairResult RejectedRepair(
        string reason,
        IReadOnlyList<string> evidenceRefs,
        ExecutableSkill? skill = null,
        VerifiedSkillTransition? transition = null) =>
        new(
            SkillRepairDecision.Rejected,
            "SKILL_REPAIR_REJECTED",
            SafeRuntimeText.Sanitize(reason, 1000),
            skill,
            transition,
            null,
            evidenceRefs);

    private static SkillReplaySelection Replay(
        SkillReplayDecision decision,
        string code,
        string reason,
        ExecutableSkill? skill = null,
        VerifiedSkillTransition? transition = null,
        IEnumerable<string>? evidenceRefs = null) =>
        new(
            decision,
            code,
            reason,
            skill,
            transition,
            Evidence(evidenceRefs ?? Array.Empty<string>()));

    private static IReadOnlyList<string> MergeEvidence(
        IEnumerable<string> left,
        IEnumerable<string> right) =>
        Evidence(left.Concat(right));

    private static IReadOnlyList<string> Evidence(IEnumerable<string> values) =>
        values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => Identifier(value, 160))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .Take(512)
            .ToArray();

    private static string Identifier(string? value, int maximumLength)
    {
        var normalized = SafeRuntimeText.Sanitize(value, maximumLength);
        if (normalized.Length == 0)
            throw new ArgumentException("A required identifier is missing.");
        if (HistorySanitizer.ContainsSecretLikeContent(value) || ContainsLocalUserPath(value))
            throw new ArgumentException("An identifier contains secret-like or local-path content.");
        return normalized;
    }

    private static string? NullableIdentifier(string? value, int maximumLength) =>
        string.IsNullOrWhiteSpace(value) ? null : Identifier(value, maximumLength);

    private static string SafeText(string? value, int maximumLength, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{field} is required.");
        if (HistorySanitizer.ContainsSecretLikeContent(value) || ContainsLocalUserPath(value))
            throw new ArgumentException($"{field} contains secret-like or local-path content.");
        return SafeRuntimeText.Sanitize(value, maximumLength);
    }

    private static string Fingerprint(string? value, string field)
    {
        var normalized = Identifier(value, 80);
        if (normalized.Length != 64 || normalized.Any(character => !Uri.IsHexDigit(character)))
            throw new ArgumentException($"{field} must be a SHA-256 fingerprint.");
        return normalized.ToLowerInvariant();
    }

    private static bool IsOpaqueSecretReference(string value) =>
        value.StartsWith("secret-ref:", StringComparison.Ordinal) ||
        value.StartsWith("secret://", StringComparison.Ordinal);

    private static bool ContainsLocalUserPath(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        HistorySanitizer.SanitizeText(value).Contains("[LOCAL_PATH]", StringComparison.Ordinal);

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed record ActionValidation(
        bool Valid,
        SkillActionTemplate? Action,
        IReadOnlyList<string> Reasons);

    private sealed record VerificationValidation(
        bool Valid,
        IReadOnlyList<string> Reasons);
}

internal static class ReadOnlySetExtensions
{
    public static bool SetEquals<T>(this IReadOnlySet<T> left, IReadOnlySet<T> right) =>
        left.Count == right.Count && left.All(right.Contains);
}
