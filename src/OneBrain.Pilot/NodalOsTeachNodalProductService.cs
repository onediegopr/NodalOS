using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OneBrain.Core.History;
using OneBrain.Core.Perception;
using OneBrain.Core.Recipes;
using OneBrain.Core.Runtime;
using OneBrain.Core.Skills;
using OneBrain.Core.Verification;
using OneBrain.Observation.Teaching;
using OneBrain.Observation.Windows;

namespace OneBrain.Pilot;

public sealed class NodalOsTeachNodalProductService
{
    public static readonly TimeSpan SwitchDelay = TimeSpan.FromSeconds(2);
    public static readonly TimeSpan DemonstrationWindow = TimeSpan.FromSeconds(8);
    public static readonly TimeSpan ConsentLifetime = TimeSpan.FromMinutes(30);

    private const string CapabilityId = "desktop.uia.action";
    private const int MaximumSteps = 12;
    private const string EditedCompilationCode = "TEACH_NODAL_REVIEW_EDIT_REQUIRES_REVERIFICATION";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private readonly object _stateGate = new();
    private readonly TeachNodalWindowsObservationAdapterV1 _adapter;
    private readonly Func<IntPtr> _foregroundWindowProvider;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;
    private readonly string _draftRoot;
    private ActiveCapture? _active;
    private NodalOsTeachNodalProductProposal? _proposal;
    private NodalOsTeachNodalProductState _state = NodalOsTeachNodalProductState.Empty;
    private string[] _findings = [];

    public NodalOsTeachNodalProductService(
        string draftRoot,
        TeachNodalHwndSnapshotReader? snapshotReader = null,
        Func<IntPtr>? foregroundWindowProvider = null,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(draftRoot);
        _draftRoot = Path.GetFullPath(draftRoot);
        _adapter = new TeachNodalWindowsObservationAdapterV1(snapshotReader);
        _foregroundWindowProvider = foregroundWindowProvider ?? ForegroundWindowReader.GetForegroundWindow;
        _delay = delay ?? Task.Delay;
    }

    public NodalOsTeachNodalProductSnapshot GetSnapshot()
    {
        lock (_stateGate)
            return SnapshotUnsafe();
    }

    public async Task<NodalOsTeachNodalProductSnapshot> BindAsync(
        NodalOsTeachNodalBindRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            lock (_stateGate)
                RequireStateUnsafe(NodalOsTeachNodalProductState.Empty, "Discard the current Teach NODAL session before binding another application.");

            var title = Text(request.WorkflowTitle, 180, "workflow title");
            var profileId = Slug(request.AppProfileName, 80, "application profile");
            await _delay(SwitchDelay, cancellationToken).ConfigureAwait(false);
            var handle = _foregroundWindowProvider();
            if (handle == IntPtr.Zero)
                throw new InvalidOperationException("No foreground application was available after the switch countdown.");

            TryReleaseBinding();
            var now = DateTimeOffset.UtcNow;
            var bindingId = "teach-product-binding-" + Hash($"{handle.ToInt64()}|{profileId}|{now.UtcDateTime.Ticks}", 20);
            var binding = _adapter.Bind(
                handle,
                bindingId,
                profileId,
                1,
                "evidence:teach-product-binding:" + Hash(bindingId, 16),
                now);
            var session = _adapter.CreateCaptureSession(
                binding,
                "teach-product-" + Hash($"{binding.ApplicationRef}|{title}|{now.UtcDateTime.Ticks}", 24),
                title,
                "workspace.product",
                TeachNodalSurface.DesktopFixture,
                new HashSet<string>(StringComparer.Ordinal) { CapabilityId },
                ReliableRecipeRiskProfile.ReadOnly,
                MaximumSteps);
            session.Arm(
                new TeachNodalCaptureConsent(
                    ExplicitOptIn: true,
                    GrantedAtUtc: now,
                    ExpiresAtUtc: now.Add(ConsentLifetime),
                    AllowedSources: new HashSet<TeachNodalCaptureSource> { TeachNodalCaptureSource.Uia },
                    ConsentEvidenceRef: "evidence:teach-product-consent:" + Hash(bindingId, 16)),
                now);
            session.Start(now);

            lock (_stateGate)
            {
                _active = new ActiveCapture(binding, session, title, profileId, now, []);
                _proposal = null;
                _findings = [];
                _state = NodalOsTeachNodalProductState.Bound;
                return SnapshotUnsafe();
            }
        }
        catch (OperationCanceledException)
        {
            FailClosed("Teach NODAL application binding was cancelled.");
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return FailClosed(exception.Message);
        }
        finally
        {
            _operationGate.Release();
        }
    }

    public async Task<NodalOsTeachNodalProductSnapshot> CaptureStepAsync(
        NodalOsTeachNodalCaptureStepRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await _operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        TeachNodalWindowsStepToken? token = null;
        try
        {
            ActiveCapture active;
            lock (_stateGate)
            {
                RequireStateUnsafe(NodalOsTeachNodalProductState.Bound, "Teach NODAL is not ready to capture a step.");
                active = _active ?? throw new InvalidOperationException("Bind one foreground application before recording a step.");
                if (_proposal is not null)
                    throw new InvalidOperationException("Save or discard the current proposal before recording more steps.");
                if (active.Steps.Count >= MaximumSteps)
                    throw new InvalidOperationException($"Teach NODAL supports at most {MaximumSteps} steps in this product slice.");
                _state = NodalOsTeachNodalProductState.Capturing;
            }

            var intent = Text(request.Intent, 300, "step intent");
            var requestedTargetLabel = Text(request.TargetLabel, 240, "target label");
            var requestedTargetRole = Text(request.TargetRole, 120, "target role");
            var sequence = active.Steps.Count + 1;
            var stepId = $"step-{sequence:D2}-{Slug(request.Kind.ToString(), 30, "action kind")}";

            await _delay(SwitchDelay, cancellationToken).ConfigureAwait(false);
            token = _adapter.BeginStep(
                active.Binding,
                stepId,
                $"evidence:teach-product-before:{stepId}",
                DateTimeOffset.UtcNow,
                DemonstrationWindow.Add(TimeSpan.FromSeconds(5)));

            var target = FindTarget(token.Before, requestedTargetLabel, requestedTargetRole);
            var targetLabel = Property(target, "name", requestedTargetLabel);
            var targetRole = Property(target, "role", requestedTargetRole);
            var action = BuildAction(active, request, stepId, intent, targetLabel, targetRole, target);
            var verificationPlan = BuildVerificationPlan(active.ProfileId, stepId, target.SemanticRef);

            await _delay(DemonstrationWindow, cancellationToken).ConfigureAwait(false);
            var observation = _adapter.CompleteStepAndObserve(
                active.Session,
                active.Binding,
                token,
                sequence,
                TeachNodalCaptureSource.Uia,
                TrustedControlSource.UserInstruction,
                action,
                verificationPlan,
                actionExecuted: true,
                actionRejected: false,
                userInterrupted: false,
                evidenceRefs: [$"evidence:teach-product-observation:{stepId}"],
                completedAtUtc: DateTimeOffset.UtcNow);
            var verificationReport = new SemanticVerifierV2().Verify(
                verificationPlan,
                new SemanticVerificationContext(
                    observation.Before,
                    observation.After,
                    observation.ActionExecuted,
                    observation.ActionRejected,
                    observation.UserInterrupted,
                    observation.VerificationElapsed,
                    observation.EvidenceRefs));
            var changed = !string.Equals(
                observation.Before.StateFingerprint,
                observation.After.StateFingerprint,
                StringComparison.Ordinal);
            var step = new NodalOsTeachNodalProductStepSnapshot(
                stepId,
                request.Kind.ToString(),
                intent,
                targetLabel,
                targetRole,
                action.Parameters.Select(value => $"{value.Placeholder} → {value.ValueRef}").ToArray(),
                observation.Before.StateFingerprint,
                observation.After.StateFingerprint,
                changed,
                verificationReport.Success,
                $"evidence:teach-product-observation:{stepId}");

            lock (_stateGate)
            {
                _active = active with { Steps = active.Steps.Append(step).ToArray() };
                _state = NodalOsTeachNodalProductState.Bound;
                _findings = step.Verified
                    ? []
                    : [$"Step '{stepId}' did not pass semantic verification and remains review-only."];
                return SnapshotUnsafe();
            }
        }
        catch (OperationCanceledException)
        {
            CancelPending(token);
            FailClosed("Teach NODAL step capture was cancelled and the session was closed.");
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            CancelPending(token);
            return FailClosed(exception.Message);
        }
        finally
        {
            _operationGate.Release();
        }
    }

    public async Task<NodalOsTeachNodalProductSnapshot> FinishAsync(CancellationToken cancellationToken)
    {
        await _operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ActiveCapture active;
            lock (_stateGate)
            {
                RequireStateUnsafe(NodalOsTeachNodalProductState.Bound, "Teach NODAL is not ready to finish this demonstration.");
                active = _active ?? throw new InvalidOperationException("No active Teach NODAL demonstration exists.");
                if (active.Steps.Count == 0)
                    throw new InvalidOperationException("Record at least one semantic step before review.");
            }

            var review = active.Session.Complete(DateTimeOffset.UtcNow);
            var compilation = new TeachNodalCompilerV1().Compile(review.Demonstration);
            var saveAllowed = compilation.Decision != TeachNodalCompilationDecision.RejectedUnsafeDemonstration;
            var overlap = FindOverlap(active.ProfileId, active.Title);
            var now = DateTimeOffset.UtcNow;
            var findings = review.Findings
                .Concat(compilation.Findings)
                .Concat(active.Steps.Where(step => !step.Verified)
                    .Select(step => $"Step '{step.StepId}' requires review because verification did not pass."))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var proposal = new NodalOsTeachNodalProductProposal(
                overlap?.DraftId ?? "teach-draft-" + Hash($"{active.Binding.ApplicationRef}|{active.Title}|{active.StartedAtUtc.UtcDateTime.Ticks}", 24),
                overlap?.Version ?? 0,
                overlap is null ? NodalOsTeachNodalProposalKind.NewSkill : NodalOsTeachNodalProposalKind.UpdateCandidate,
                active.Title,
                active.Steps.Count == 1
                    ? $"Una acción observada en {active.Binding.ProcessNameRedacted}; revisar antes de guardar."
                    : $"{active.Steps.Count} acciones observadas en {active.Binding.ProcessNameRedacted}; revisar antes de guardar.",
                active.ProfileId,
                active.Binding.ApplicationRef,
                active.Binding.ProcessNameRedacted,
                compilation.Decision.ToString(),
                compilation.Code,
                compilation.Skill?.SkillFingerprint ?? string.Empty,
                active.Steps,
                findings,
                overlap?.CreatedAtUtc ?? now,
                now,
                ReviewRequired: true,
                SaveAllowed: saveAllowed,
                ScriptsIncluded: false,
                RawInputStored: false,
                RawScreenshotStored: false,
                RawDomStored: false,
                GlobalHooksUsed: false,
                ExecutionAuthorityGranted: false,
                ProductAuthorityGranted: false);

            lock (_stateGate)
            {
                _proposal = proposal;
                _findings = findings;
                _state = saveAllowed
                    ? NodalOsTeachNodalProductState.ReviewReady
                    : NodalOsTeachNodalProductState.FailedClosed;
                if (!saveAllowed)
                {
                    _active = null;
                    _proposal = null;
                    TryReleaseBinding();
                }
                return SnapshotUnsafe();
            }
        }
        catch (OperationCanceledException)
        {
            FailClosed("Teach NODAL proposal compilation was cancelled.");
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return FailClosed(exception.Message);
        }
        finally
        {
            _operationGate.Release();
        }
    }

    public async Task<NodalOsTeachNodalProductSnapshot> UpdateProposalAsync(
        NodalOsTeachNodalProposalEditRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.StepIntents);
        ArgumentNullException.ThrowIfNull(request.StepTargets);
        await _operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            lock (_stateGate)
            {
                RequireStateUnsafe(NodalOsTeachNodalProductState.ReviewReady, "Teach NODAL has no editable review-ready proposal.");
                var proposal = _proposal ?? throw new InvalidOperationException("No Teach NODAL proposal is available for review.");
                var title = Text(request.Title, 180, "proposal title");
                var summary = Text(request.Summary, 500, "proposal summary");
                var changed = !string.Equals(title, proposal.Title, StringComparison.Ordinal) ||
                    !string.Equals(summary, proposal.Summary, StringComparison.Ordinal);
                var steps = proposal.Steps.Select(step =>
                {
                    var intent = request.StepIntents.TryGetValue(step.StepId, out var requestedIntent)
                        ? Text(requestedIntent, 300, $"intent for {step.StepId}")
                        : step.Intent;
                    var target = request.StepTargets.TryGetValue(step.StepId, out var requestedTarget)
                        ? Text(requestedTarget, 240, $"target for {step.StepId}")
                        : step.TargetLabel;
                    var stepChanged = !string.Equals(intent, step.Intent, StringComparison.Ordinal) ||
                        !string.Equals(target, step.TargetLabel, StringComparison.Ordinal);
                    changed |= stepChanged;
                    return step with
                    {
                        Intent = intent,
                        TargetLabel = target,
                        Verified = stepChanged ? false : step.Verified
                    };
                }).ToArray();

                if (!changed)
                    return SnapshotUnsafe();

                var findings = proposal.Findings
                    .Append("Review edits changed the compiled proposal. Prior verification metadata is stale and re-verification is required before any future promotion or replay.")
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray();
                _proposal = proposal with
                {
                    Title = title,
                    Summary = summary,
                    Steps = steps,
                    CompilationDecision = TeachNodalCompilationDecision.DraftNeedsReview.ToString(),
                    CompilationCode = EditedCompilationCode,
                    SkillFingerprint = string.Empty,
                    Findings = findings,
                    UpdatedAtUtc = DateTimeOffset.UtcNow,
                    ReviewRequired = true,
                    SaveAllowed = true
                };
                _findings = findings;
                _state = NodalOsTeachNodalProductState.ReviewReady;
                return SnapshotUnsafe();
            }
        }
        catch (OperationCanceledException)
        {
            FailClosed("Teach NODAL proposal review was cancelled.");
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return FailClosed(exception.Message);
        }
        finally
        {
            _operationGate.Release();
        }
    }

    public async Task<NodalOsTeachNodalProductSnapshot> SaveAsync(CancellationToken cancellationToken)
    {
        await _operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            NodalOsTeachNodalProductProposal proposal;
            lock (_stateGate)
            {
                RequireStateUnsafe(NodalOsTeachNodalProductState.ReviewReady, "Only a review-ready Teach NODAL proposal can be saved once.");
                proposal = _proposal ?? throw new InvalidOperationException("No Teach NODAL proposal is available to save.");
                if (!proposal.SaveAllowed)
                    throw new InvalidOperationException("The proposal failed closed and cannot be saved.");
            }

            Directory.CreateDirectory(_draftRoot);
            var existing = ReadProposal(DraftPath(proposal.DraftId));
            var persisted = proposal with
            {
                Version = (existing?.Version ?? proposal.Version) + 1,
                Kind = existing is null ? proposal.Kind : NodalOsTeachNodalProposalKind.UpdateCandidate,
                CreatedAtUtc = existing?.CreatedAtUtc ?? proposal.CreatedAtUtc,
                UpdatedAtUtc = DateTimeOffset.UtcNow,
                ReviewRequired = true,
                SaveAllowed = true,
                ScriptsIncluded = false,
                RawInputStored = false,
                RawScreenshotStored = false,
                RawDomStored = false,
                GlobalHooksUsed = false,
                ExecutionAuthorityGranted = false,
                ProductAuthorityGranted = false
            };
            var target = DraftPath(persisted.DraftId);
            var temporary = target + ".tmp";
            await File.WriteAllTextAsync(
                temporary,
                JsonSerializer.Serialize(persisted, JsonOptions),
                new UTF8Encoding(false),
                cancellationToken).ConfigureAwait(false);
            File.Move(temporary, target, overwrite: true);

            lock (_stateGate)
            {
                _proposal = persisted;
                _state = NodalOsTeachNodalProductState.Saved;
                _findings = persisted.Findings
                    .Append("Draft saved locally as review-only. Replay and execution authority remain disabled.")
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
                return SnapshotUnsafe();
            }
        }
        catch (OperationCanceledException)
        {
            FailClosed("Teach NODAL draft persistence was cancelled.");
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            return FailClosed(exception.Message);
        }
        finally
        {
            _operationGate.Release();
        }
    }

    public async Task<NodalOsTeachNodalProductSnapshot> DiscardAsync(CancellationToken cancellationToken)
    {
        await _operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            TryReleaseBinding();
            lock (_stateGate)
            {
                _active = null;
                _proposal = null;
                _findings = [];
                _state = NodalOsTeachNodalProductState.Empty;
                return SnapshotUnsafe();
            }
        }
        finally
        {
            _operationGate.Release();
        }
    }

    private NodalOsTeachNodalProductSnapshot SnapshotUnsafe()
    {
        var active = _active;
        return new NodalOsTeachNodalProductSnapshot(
            _state switch
            {
                NodalOsTeachNodalProductState.Empty => "TEACH_NODAL_READY_TO_BIND",
                NodalOsTeachNodalProductState.Bound => "TEACH_NODAL_APPLICATION_BOUND",
                NodalOsTeachNodalProductState.Capturing => "TEACH_NODAL_CAPTURE_WINDOW_ACTIVE",
                NodalOsTeachNodalProductState.ReviewReady => "TEACH_NODAL_REVIEW_ONLY_PROPOSAL_READY",
                NodalOsTeachNodalProductState.Saved => "TEACH_NODAL_REVIEW_ONLY_DRAFT_SAVED",
                _ => "TEACH_NODAL_FAILED_CLOSED"
            },
            _state,
            active is not null,
            active?.Binding.WindowTitleRedacted ?? "not bound",
            active?.Binding.ProcessNameRedacted ?? "not bound",
            active?.ProfileId ?? _proposal?.AppProfileId ?? "not bound",
            active?.Steps.Count ?? _proposal?.Steps.Count ?? 0,
            _proposal,
            ListSavedDrafts(),
            _findings,
            ExplicitOptInRecorded: active is not null,
            ApplicationScopeBound: active is not null,
            VoiceEngineAdded: false,
            VideoStored: false,
            AudioStored: false,
            RawInputStored: false,
            GlobalHooksUsed: false,
            ReplayEnabled: false,
            ExecutionAuthorityGranted: false,
            ProductAuthorityGranted: false);
    }

    private NodalOsTeachNodalProductSnapshot FailClosed(string message)
    {
        TryReleaseBinding();
        lock (_stateGate)
        {
            _active = null;
            _proposal = null;
            _state = NodalOsTeachNodalProductState.FailedClosed;
            _findings = [SafeRuntimeText.Sanitize(message, 500)];
            return SnapshotUnsafe();
        }
    }

    private void CancelPending(TeachNodalWindowsStepToken? token)
    {
        if (token is null)
            return;
        try
        {
            ActiveCapture? active;
            lock (_stateGate)
                active = _active;
            if (active is not null)
                _adapter.CancelStep(active.Binding, token.TokenId);
        }
        catch (InvalidOperationException)
        {
        }
    }

    private void TryReleaseBinding()
    {
        try
        {
            var binding = _adapter.Binding;
            if (binding is not null && _adapter.PendingStepCount == 0)
                _adapter.ReleaseBinding(binding);
        }
        catch (InvalidOperationException)
        {
        }
    }

    private void RequireStateUnsafe(NodalOsTeachNodalProductState required, string reason)
    {
        if (_state == NodalOsTeachNodalProductState.FailedClosed)
            throw new InvalidOperationException("Teach NODAL failed closed. Discard the session before starting again.");
        if (_state != required)
            throw new InvalidOperationException(reason);
    }

    private static TeachNodalObservedAction BuildAction(
        ActiveCapture active,
        NodalOsTeachNodalCaptureStepRequest request,
        string stepId,
        string intent,
        string targetLabel,
        string targetRole,
        UnifiedElementSnapshot target)
    {
        IReadOnlyList<TeachNodalParameterObservation> parameters = [];
        if (request.Kind == TeachNodalActionKind.Type)
        {
            var name = Slug(request.ParameterName ?? "VALUE", 40, "parameter name")
                .Replace('-', '_')
                .ToUpperInvariant();
            var reference = ParameterReference(
                request.ParameterReference ?? $"variable-ref:{name}",
                request.SecretByReference);
            parameters =
            [
                new TeachNodalParameterObservation(
                    name,
                    $"{{{name}}}",
                    reference,
                    request.SecretByReference
                        ? TrustedControlSource.OperatorDecision
                        : TrustedControlSource.UserInstruction,
                    request.SecretByReference)
            ];
        }

        var operation = request.Kind switch
        {
            TeachNodalActionKind.Click => "invoke",
            TeachNodalActionKind.Type => "set-value",
            TeachNodalActionKind.Select => "select",
            TeachNodalActionKind.Navigate => "navigate",
            TeachNodalActionKind.Wait => "wait",
            _ => throw new ArgumentOutOfRangeException(nameof(request.Kind))
        };
        return new TeachNodalObservedAction(
            stepId,
            request.Kind,
            intent,
            CapabilityId,
            operation,
            target.SemanticRef,
            targetLabel,
            targetRole,
            TrustedControlSource.UiaObservation,
            parameters,
            [$"app-profile:{active.ProfileId}:{Hash(target.SemanticRef, 20)}"],
            0.92d);
    }

    private static SemanticVerificationPlan BuildVerificationPlan(
        string profileId,
        string stepId,
        string targetRef) =>
        new(
            "verify-" + stepId,
            [Rule(stepId + "-target", SemanticVerificationRuleKind.ElementPresent, targetRef)],
            [
                Rule(stepId + "-fingerprint", SemanticVerificationRuleKind.StateFingerprintChanged),
                Rule(stepId + "-conflicts", SemanticVerificationRuleKind.NoBlockingConflicts)
            ],
            [],
            [],
            [$"evidence:teach-product-observation:{stepId}"],
            DemonstrationWindow.Add(TimeSpan.FromSeconds(5)),
            profileId,
            RequireActionExecuted: true,
            AllowProcessChange: false,
            FailOnBlockingConflicts: true);

    private static SemanticVerificationRule Rule(
        string id,
        SemanticVerificationRuleKind kind,
        string? subject = null) =>
        new(id, kind, subject, null, null, Required: true);

    private static UnifiedElementSnapshot FindTarget(
        CognitiveSnapshotV2 snapshot,
        string targetLabel,
        string targetRole)
    {
        var candidates = snapshot.Elements
            .Where(element => element.ActionEligible)
            .Select(element => new { Element = element, Score = Score(element, targetLabel, targetRole) })
            .Where(value => value.Score > 0)
            .OrderByDescending(value => value.Score)
            .ThenBy(value => value.Element.SemanticRef, StringComparer.Ordinal)
            .ToArray();
        if (candidates.Length == 0)
            throw new InvalidOperationException("The named target was not found in the selected application's semantic UI tree.");
        if (candidates.Length > 1 && candidates[0].Score == candidates[1].Score)
            throw new InvalidOperationException("The named target is ambiguous. Use a more specific visible label.");
        return candidates[0].Element;
    }

    private static int Score(UnifiedElementSnapshot element, string label, string role)
    {
        var labelScore = 0;
        foreach (var property in new[] { "name", "text", "automationId", "value" })
        {
            if (!element.CanonicalProperties.TryGetValue(property, out var value))
                continue;
            if (string.Equals(value, label, StringComparison.OrdinalIgnoreCase))
                labelScore += 10;
            else if (value.Contains(label, StringComparison.OrdinalIgnoreCase))
                labelScore += 5;
        }
        if (labelScore == 0)
            return 0;

        var roleBonus = element.CanonicalProperties.TryGetValue("role", out var elementRole) &&
            string.Equals(elementRole, role, StringComparison.OrdinalIgnoreCase)
            ? 3
            : 0;
        return labelScore + roleBonus;
    }

    private static string Property(UnifiedElementSnapshot element, string key, string fallback) =>
        element.CanonicalProperties.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? SafeRuntimeText.Sanitize(value, key == "role" ? 120 : 240)
            : fallback;

    private NodalOsTeachNodalProductProposal? FindOverlap(string appProfileId, string title) =>
        ReadAllProposals()
            .Where(value => string.Equals(value.AppProfileId, appProfileId, StringComparison.Ordinal))
            .OrderByDescending(value => value.UpdatedAtUtc)
            .FirstOrDefault(value => string.Equals(
                Comparable(value.Title),
                Comparable(title),
                StringComparison.Ordinal));

    private IReadOnlyList<NodalOsTeachNodalSavedDraftSummary> ListSavedDrafts() =>
        ReadAllProposals()
            .OrderByDescending(value => value.UpdatedAtUtc)
            .Take(20)
            .Select(value => new NodalOsTeachNodalSavedDraftSummary(
                value.DraftId,
                value.Version,
                value.Title,
                value.AppProfileId,
                value.Steps.Count,
                value.UpdatedAtUtc))
            .ToArray();

    private IReadOnlyList<NodalOsTeachNodalProductProposal> ReadAllProposals()
    {
        if (!Directory.Exists(_draftRoot))
            return [];
        return Directory.EnumerateFiles(_draftRoot, "*.json", SearchOption.TopDirectoryOnly)
            .Select(ReadProposal)
            .Where(value => value is not null)
            .Cast<NodalOsTeachNodalProductProposal>()
            .ToArray();
    }

    private static NodalOsTeachNodalProductProposal? ReadProposal(string path)
    {
        if (!File.Exists(path))
            return null;
        try
        {
            var proposal = JsonSerializer.Deserialize<NodalOsTeachNodalProductProposal>(
                File.ReadAllText(path, Encoding.UTF8),
                JsonOptions);
            return IsValidPersistedProposal(proposal) ? proposal : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static bool IsValidPersistedProposal(NodalOsTeachNodalProductProposal? proposal)
    {
        if (proposal is null ||
            proposal.Version < 1 ||
            string.IsNullOrWhiteSpace(proposal.DraftId) ||
            string.IsNullOrWhiteSpace(proposal.Title) ||
            string.IsNullOrWhiteSpace(proposal.Summary) ||
            string.IsNullOrWhiteSpace(proposal.AppProfileId) ||
            string.IsNullOrWhiteSpace(proposal.ApplicationRef) ||
            string.IsNullOrWhiteSpace(proposal.ProcessNameRedacted) ||
            string.IsNullOrWhiteSpace(proposal.CompilationDecision) ||
            string.IsNullOrWhiteSpace(proposal.CompilationCode) ||
            proposal.Steps is null || proposal.Steps.Count is < 1 or > MaximumSteps ||
            proposal.Findings is null ||
            !proposal.ReviewRequired || !proposal.SaveAllowed ||
            proposal.ScriptsIncluded || proposal.RawInputStored || proposal.RawScreenshotStored ||
            proposal.RawDomStored || proposal.GlobalHooksUsed || proposal.ExecutionAuthorityGranted ||
            proposal.ProductAuthorityGranted || proposal.UpdatedAtUtc < proposal.CreatedAtUtc)
        {
            return false;
        }

        return proposal.Steps.All(step =>
            step is not null &&
            !string.IsNullOrWhiteSpace(step.StepId) &&
            !string.IsNullOrWhiteSpace(step.Kind) &&
            !string.IsNullOrWhiteSpace(step.Intent) &&
            !string.IsNullOrWhiteSpace(step.TargetLabel) &&
            !string.IsNullOrWhiteSpace(step.TargetRole) &&
            step.ParameterRefs is not null &&
            !string.IsNullOrWhiteSpace(step.BeforeFingerprint) &&
            !string.IsNullOrWhiteSpace(step.AfterFingerprint) &&
            !string.IsNullOrWhiteSpace(step.EvidenceRef));
    }

    private string DraftPath(string draftId) =>
        Path.Combine(_draftRoot, Slug(draftId, 120, "draft id") + ".json");

    private static string ParameterReference(string? value, bool secretByReference)
    {
        var requiredPrefix = secretByReference ? "secret-ref:" : null;
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("parameter reference is required.");
        var normalized = SafeRuntimeText.Sanitize(value, 180);
        var validPrefix = secretByReference
            ? normalized.StartsWith(requiredPrefix!, StringComparison.Ordinal)
            : normalized.StartsWith("variable-ref:", StringComparison.Ordinal) ||
              normalized.StartsWith("literal-ref:", StringComparison.Ordinal);
        if (!validPrefix)
            throw new ArgumentException(secretByReference
                ? "Sensitive input requires an opaque secret-ref: reference."
                : "Typed input requires variable-ref: or literal-ref:.");
        var separator = normalized.IndexOf(':');
        if (separator < 0 || separator == normalized.Length - 1)
            throw new ArgumentException("parameter reference requires a non-empty opaque identifier.");
        var identifier = normalized[(separator + 1)..];
        if (HistorySanitizer.ContainsSecretLikeContent(identifier))
            throw new ArgumentException("parameter reference contains raw-looking secret material.");
        if (HistorySanitizer.SanitizeText(normalized).Contains("[LOCAL_PATH]", StringComparison.Ordinal))
            throw new ArgumentException("parameter reference contains local-path content.");
        if (!secretByReference && HistorySanitizer.ContainsSecretLikeContent(normalized))
            throw new ArgumentException("non-secret parameter reference contains secret-like content.");
        return normalized;
    }

    private static string Text(string? value, int maximumLength, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{field} is required.");
        if (HistorySanitizer.ContainsSecretLikeContent(value))
            throw new ArgumentException($"{field} contains secret-like content.");
        if (HistorySanitizer.SanitizeText(value).Contains("[LOCAL_PATH]", StringComparison.Ordinal))
            throw new ArgumentException($"{field} contains local-path content.");
        var normalized = SafeRuntimeText.Sanitize(value, maximumLength);
        if (normalized.Length == 0)
            throw new ArgumentException($"{field} is required.");
        return normalized;
    }

    private static string Slug(string? value, int maximumLength, string field)
    {
        var text = Text(value, maximumLength, field).ToLowerInvariant();
        var builder = new StringBuilder(text.Length);
        var separator = false;
        foreach (var character in text)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                separator = false;
            }
            else if (!separator && builder.Length > 0)
            {
                builder.Append('-');
                separator = true;
            }
        }
        var result = builder.ToString().Trim('-');
        if (result.Length == 0)
            throw new ArgumentException($"{field} did not contain a usable identifier.");
        return result[..Math.Min(result.Length, maximumLength)];
    }

    private static string Comparable(string value) =>
        string.Concat(value.Where(char.IsLetterOrDigit)).ToLowerInvariant();

    private static string Hash(string value, int length)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
        return hash[..Math.Min(length, hash.Length)];
    }

    private sealed record ActiveCapture(
        TeachNodalWindowsApplicationBinding Binding,
        TeachNodalCaptureSessionV1 Session,
        string Title,
        string ProfileId,
        DateTimeOffset StartedAtUtc,
        IReadOnlyList<NodalOsTeachNodalProductStepSnapshot> Steps);
}

public static class NodalOsTeachNodalProductRuntime
{
    public static NodalOsTeachNodalProductService CreateDefault()
    {
        var root = NodalOsDesktopLaunchRuntime.ResolveProductDataRoot();
        return new NodalOsTeachNodalProductService(Path.Combine(root, "TeachNodal", "Drafts"));
    }
}
