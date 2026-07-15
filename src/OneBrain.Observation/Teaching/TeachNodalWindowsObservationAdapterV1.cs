using System.Security.Cryptography;
using System.Text;
using OneBrain.Core.History;
using OneBrain.Core.Models;
using OneBrain.Core.Perception;
using OneBrain.Core.Recipes;
using OneBrain.Core.Runtime;
using OneBrain.Core.Skills;
using OneBrain.Core.Verification;
using OneBrain.Observation.Windows;

namespace OneBrain.Observation.Teaching;

public delegate CognitiveSnapshot? TeachNodalHwndSnapshotReader(IntPtr windowHandle, string? expectedProcessName);

public sealed record TeachNodalWindowsApplicationBinding(
    string BindingId,
    long WindowHandle,
    int ProcessId,
    string ApplicationRef,
    string ProcessNameRedacted,
    string WindowTitleRedacted,
    string AppProfileId,
    int AppProfileVersion,
    string BaselineFingerprint,
    bool BaselineTreeTruncated,
    DateTimeOffset BoundAtUtc,
    string EvidenceRef)
{
    public bool ForegroundRequired => true;
    public bool GlobalHooksUsed => false;
    public bool RawInputCaptured => false;
    public bool RawScreenshotCaptured => false;
    public bool RawDomCaptured => false;
    public bool ExecutionAuthorityGranted => false;
}

public sealed record TeachNodalWindowsStepToken(
    string TokenId,
    string BindingId,
    string StepId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    CognitiveSnapshotV2 Before,
    string EvidenceRef)
{
    public bool GrantsExecutionAuthority => false;
    public bool CapturesInput => false;
}

public sealed class TeachNodalWindowsObservationAdapterV1
{
    public static readonly TimeSpan DefaultMaximumStepDuration = TimeSpan.FromMinutes(2);

    private static readonly HashSet<TeachNodalCaptureSource> AllowedSources =
    [
        TeachNodalCaptureSource.Uia,
        TeachNodalCaptureSource.ApplicationScopedPointer,
        TeachNodalCaptureSource.ApplicationScopedKeyboard,
        TeachNodalCaptureSource.OperatorAnnotation
    ];

    private readonly object _gate = new();
    private readonly TeachNodalHwndSnapshotReader _snapshotReader;
    private readonly Dictionary<string, PendingStep> _pending = new(StringComparer.Ordinal);
    private TeachNodalWindowsApplicationBinding? _binding;
    private long _tokenSequence;

    public TeachNodalWindowsObservationAdapterV1(TeachNodalHwndSnapshotReader? snapshotReader = null)
    {
        var reader = new CognitiveSnapshotReader();
        _snapshotReader = snapshotReader ?? reader.ReadFromHwnd;
    }

    public TeachNodalWindowsApplicationBinding? Binding
    {
        get
        {
            lock (_gate)
                return _binding;
        }
    }

    public int PendingStepCount
    {
        get
        {
            lock (_gate)
                return _pending.Count;
        }
    }

    public TeachNodalWindowsApplicationBinding BindCurrentForeground(
        string bindingId,
        string appProfileId,
        int appProfileVersion,
        string evidenceRef,
        DateTimeOffset boundAtUtc) =>
        Bind(
            ForegroundWindowReader.GetForegroundWindow(),
            bindingId,
            appProfileId,
            appProfileVersion,
            evidenceRef,
            boundAtUtc);

    public TeachNodalWindowsApplicationBinding Bind(
        IntPtr windowHandle,
        string bindingId,
        string appProfileId,
        int appProfileVersion,
        string evidenceRef,
        DateTimeOffset boundAtUtc)
    {
        if (windowHandle == IntPtr.Zero)
            throw new ArgumentException("Teach NODAL requires an explicitly selected non-zero window handle.", nameof(windowHandle));
        if (appProfileVersion < 1)
            throw new ArgumentOutOfRangeException(nameof(appProfileVersion), "Application profile version must be positive.");

        var normalizedBindingId = Identifier(bindingId, 120, "binding id");
        var normalizedProfileId = Identifier(appProfileId, 160, "application profile id");
        var normalizedEvidence = Identifier(evidenceRef, 160, "binding evidence reference");
        var legacy = _snapshotReader(windowHandle, expectedProcessName: null)
            ?? throw new InvalidOperationException("The selected Windows application could not be observed through UI Automation.");
        if (!legacy.Window.IsForeground)
            throw new InvalidOperationException("Teach NODAL can bind only the explicitly selected foreground application.");
        if (legacy.Window.ProcessId <= 0)
            throw new InvalidOperationException("The selected Windows application has no valid process identity.");

        var baseline = CognitiveSnapshotV2Factory.FromLegacy(
            legacy,
            normalizedEvidence,
            boundAtUtc,
            $"teach-windows-binding-{normalizedBindingId}");
        var binding = new TeachNodalWindowsApplicationBinding(
            BindingId: normalizedBindingId,
            WindowHandle: windowHandle.ToInt64(),
            ProcessId: legacy.Window.ProcessId,
            ApplicationRef: baseline.Application.ApplicationRef,
            ProcessNameRedacted: baseline.Application.ProcessNameRedacted,
            WindowTitleRedacted: baseline.Application.WindowTitleRedacted,
            AppProfileId: normalizedProfileId,
            AppProfileVersion: appProfileVersion,
            BaselineFingerprint: baseline.StateFingerprint,
            BaselineTreeTruncated: legacy.TreeTruncated,
            BoundAtUtc: boundAtUtc,
            EvidenceRef: normalizedEvidence);

        lock (_gate)
        {
            if (_pending.Count > 0)
                throw new InvalidOperationException("Cannot rebind Teach NODAL while application-scoped steps are pending.");
            _binding = binding;
            return binding;
        }
    }

    public TeachNodalCaptureSessionV1 CreateCaptureSession(
        TeachNodalWindowsApplicationBinding binding,
        string captureId,
        string titleRedacted,
        string workspaceScope,
        TeachNodalSurface surface,
        IReadOnlySet<string> authorizedCapabilities,
        ReliableRecipeRiskProfile riskProfile,
        int maximumSteps = TeachNodalCompilerV1.MaximumSteps)
    {
        EnsureCurrentBinding(binding);
        ArgumentNullException.ThrowIfNull(authorizedCapabilities);
        return new TeachNodalCaptureSessionV1(new TeachNodalCaptureScope(
            CaptureId: captureId,
            TitleRedacted: titleRedacted,
            WorkspaceScope: workspaceScope,
            AppProfileId: binding.AppProfileId,
            AppProfileVersion: binding.AppProfileVersion,
            ApplicationRef: binding.ApplicationRef,
            Surface: surface,
            AuthorizedCapabilities: authorizedCapabilities,
            RiskProfile: riskProfile,
            MaximumSteps: maximumSteps));
    }

    public TeachNodalWindowsStepToken BeginStep(
        TeachNodalWindowsApplicationBinding binding,
        string stepId,
        string evidenceRef,
        DateTimeOffset startedAtUtc,
        TimeSpan? maximumDuration = null)
    {
        EnsureCurrentBinding(binding);
        var normalizedStepId = Identifier(stepId, 120, "step id");
        var normalizedEvidence = Identifier(evidenceRef, 160, "before-state evidence reference");
        var duration = maximumDuration ?? DefaultMaximumStepDuration;
        if (duration <= TimeSpan.Zero || duration > TimeSpan.FromMinutes(10))
            throw new ArgumentOutOfRangeException(nameof(maximumDuration), "Step observation duration must be between zero and ten minutes.");

        var before = ReadStableSnapshot(
            binding,
            normalizedEvidence,
            startedAtUtc,
            $"teach-windows-before-{normalizedStepId}");

        lock (_gate)
        {
            EnsureCurrentBindingUnsafe(binding);
            if (_pending.Values.Any(value => string.Equals(value.Token.StepId, normalizedStepId, StringComparison.Ordinal)))
                throw new InvalidOperationException($"Teach NODAL step '{normalizedStepId}' is already pending.");
            var sequence = checked(++_tokenSequence);
            var tokenId = "teach-windows-token-" + ShortHash(
                $"{binding.BindingId}|{normalizedStepId}|{startedAtUtc.UtcDateTime.Ticks}|{sequence}|{before.StateFingerprint}",
                24);
            var token = new TeachNodalWindowsStepToken(
                TokenId: tokenId,
                BindingId: binding.BindingId,
                StepId: normalizedStepId,
                StartedAtUtc: startedAtUtc,
                ExpiresAtUtc: startedAtUtc.Add(duration),
                Before: before,
                EvidenceRef: normalizedEvidence);
            _pending.Add(token.TokenId, new PendingStep(token));
            return token;
        }
    }

    public TeachNodalCaptureObservation CompleteStep(
        TeachNodalWindowsApplicationBinding binding,
        TeachNodalWindowsStepToken token,
        TeachNodalCaptureSource source,
        TrustedControlSource intentSource,
        TeachNodalObservedAction action,
        SemanticVerificationPlan verificationPlan,
        bool actionExecuted,
        bool actionRejected,
        bool userInterrupted,
        IReadOnlyList<string> evidenceRefs,
        DateTimeOffset completedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(token);
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(verificationPlan);
        ArgumentNullException.ThrowIfNull(evidenceRefs);
        EnsureCurrentBinding(binding);
        if (!AllowedSources.Contains(source))
            throw new InvalidOperationException("The Windows teaching adapter accepts only application-scoped UIA, pointer, keyboard or operator annotation sources.");
        if (intentSource is not (TrustedControlSource.UserInstruction or TrustedControlSource.OperatorDecision))
            throw new InvalidOperationException("Teach NODAL step intent must originate from trusted user or operator authority.");
        if (action.TargetLabelSource is TrustedControlSource.SystemPolicy or TrustedControlSource.UserInstruction or TrustedControlSource.OperatorDecision)
            throw new InvalidOperationException("Observed Windows target labels must remain data and cannot carry control authority.");
        if (!string.IsNullOrWhiteSpace(verificationPlan.AppProfileRef) &&
            !string.Equals(verificationPlan.AppProfileRef, binding.AppProfileId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Semantic verification plan does not belong to the bound Windows application profile.");
        }
        if (actionExecuted && actionRejected)
            throw new InvalidOperationException("A Windows teaching action cannot be both executed and rejected.");

        PendingStep pending;
        lock (_gate)
        {
            EnsureCurrentBindingUnsafe(binding);
            if (!_pending.TryGetValue(token.TokenId, out pending!))
                throw new InvalidOperationException("Teach NODAL step token is unknown, cancelled or already consumed.");
            if (pending.Token != token)
                throw new InvalidOperationException("Teach NODAL step token content does not match the pending observation.");
            if (!string.Equals(token.BindingId, binding.BindingId, StringComparison.Ordinal))
                throw new InvalidOperationException("Teach NODAL step token belongs to a different application binding.");
            if (completedAtUtc < token.StartedAtUtc || completedAtUtc > token.ExpiresAtUtc)
                throw new InvalidOperationException("Teach NODAL step completed outside its bounded observation window.");
        }

        var afterEvidence = Evidence(evidenceRefs.Append($"evidence:teach-windows-after:{token.StepId}"));
        var after = ReadStableSnapshot(
            binding,
            afterEvidence.First(),
            completedAtUtc,
            $"teach-windows-after-{token.StepId}");
        var combinedEvidence = Evidence(
            evidenceRefs
                .Append(binding.EvidenceRef)
                .Append(token.EvidenceRef)
                .Concat(token.Before.EvidenceRefs)
                .Concat(after.EvidenceRefs));
        if (combinedEvidence.Count == 0)
            throw new InvalidOperationException("Teach NODAL Windows observation requires reference-only evidence.");

        var observation = new TeachNodalCaptureObservation(
            Sequence: 0,
            CapturedAtUtc: completedAtUtc,
            Source: source,
            IntentSource: intentSource,
            StepId: token.StepId,
            Action: action,
            Before: token.Before,
            After: after,
            VerificationPlan: verificationPlan,
            ActionExecuted: actionExecuted,
            ActionRejected: actionRejected,
            UserInterrupted: userInterrupted,
            VerificationElapsed: completedAtUtc - token.StartedAtUtc,
            EvidenceRefs: combinedEvidence,
            TargetApplicationForeground: true,
            GlobalHookUsed: false,
            RawKeyboardPayloadPresent: false,
            RawPointerCoordinatesPresent: false,
            RawScreenshotPresent: false,
            RawDomPresent: false);

        lock (_gate)
        {
            if (!_pending.Remove(token.TokenId))
                throw new InvalidOperationException("Teach NODAL step token was consumed concurrently.");
        }
        return observation;
    }

    public TeachNodalCaptureObservation CompleteStepAndObserve(
        TeachNodalCaptureSessionV1 session,
        TeachNodalWindowsApplicationBinding binding,
        TeachNodalWindowsStepToken token,
        long sequence,
        TeachNodalCaptureSource source,
        TrustedControlSource intentSource,
        TeachNodalObservedAction action,
        SemanticVerificationPlan verificationPlan,
        bool actionExecuted,
        bool actionRejected,
        bool userInterrupted,
        IReadOnlyList<string> evidenceRefs,
        DateTimeOffset completedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(session);
        EnsureSessionMatchesBinding(session, binding);
        var observation = CompleteStep(
            binding,
            token,
            source,
            intentSource,
            action,
            verificationPlan,
            actionExecuted,
            actionRejected,
            userInterrupted,
            evidenceRefs,
            completedAtUtc) with
        {
            Sequence = sequence
        };
        session.Observe(observation);
        return observation;
    }

    public bool CancelStep(TeachNodalWindowsApplicationBinding binding, string tokenId)
    {
        EnsureCurrentBinding(binding);
        var normalizedTokenId = Identifier(tokenId, 160, "step token id");
        lock (_gate)
            return _pending.Remove(normalizedTokenId);
    }

    public void ReleaseBinding(TeachNodalWindowsApplicationBinding binding)
    {
        EnsureCurrentBinding(binding);
        lock (_gate)
        {
            if (_pending.Count > 0)
                throw new InvalidOperationException("Cannot release a Windows teaching binding while steps are pending.");
            _binding = null;
        }
    }

    private CognitiveSnapshotV2 ReadStableSnapshot(
        TeachNodalWindowsApplicationBinding binding,
        string evidenceRef,
        DateTimeOffset capturedAtUtc,
        string snapshotId)
    {
        var legacy = _snapshotReader(new IntPtr(binding.WindowHandle), binding.ProcessNameRedacted)
            ?? throw new InvalidOperationException("The bound Windows application is no longer observable through UI Automation.");
        if (!legacy.Window.IsForeground)
            throw new InvalidOperationException("The bound Windows application is no longer foreground; capture stopped fail-closed.");
        if (legacy.Window.ProcessId != binding.ProcessId ||
            !string.Equals(legacy.Window.ProcessName, binding.ProcessNameRedacted, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The selected window now belongs to a different process identity.");
        }

        var observed = CognitiveSnapshotV2Factory.FromLegacy(
            legacy,
            Identifier(evidenceRef, 160, "snapshot evidence reference"),
            capturedAtUtc,
            snapshotId);
        var stable = CognitiveSnapshotV2Factory.Create(new CognitiveSnapshotV2Input(
            SnapshotId: observed.SnapshotId,
            CapturedAtUtc: observed.CapturedAtUtc,
            Application: observed.Application with { ApplicationRef = binding.ApplicationRef },
            WindowBounds: observed.WindowBounds,
            IsForeground: observed.IsForeground,
            WindowClaims: observed.WindowClaims,
            Elements: observed.Elements.Select(element => new CognitiveSnapshotV2ElementInput(
                element.SemanticRef,
                element.Identity,
                element.Claims)).ToArray(),
            EvidenceRefs: observed.EvidenceRefs,
            ContainsRawScreenshot: false,
            ContainsRawDom: false));
        if (!stable.SecretsExcluded || stable.ContainsRawScreenshot || stable.ContainsRawDom ||
            stable.ObservedContentCanChangeMissionGoal)
        {
            throw new InvalidOperationException("Windows observation crossed a raw-data, secret or control-authority boundary.");
        }
        if (!string.Equals(stable.Application.ApplicationRef, binding.ApplicationRef, StringComparison.Ordinal))
            throw new InvalidOperationException("Windows observation lost its stable application binding.");
        return stable;
    }

    private void EnsureCurrentBinding(TeachNodalWindowsApplicationBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        lock (_gate)
            EnsureCurrentBindingUnsafe(binding);
    }

    private void EnsureCurrentBindingUnsafe(TeachNodalWindowsApplicationBinding binding)
    {
        if (_binding is null || _binding != binding)
            throw new InvalidOperationException("Teach NODAL Windows application binding is not current.");
    }

    private static void EnsureSessionMatchesBinding(
        TeachNodalCaptureSessionV1 session,
        TeachNodalWindowsApplicationBinding binding)
    {
        if (!string.Equals(session.Scope.ApplicationRef, binding.ApplicationRef, StringComparison.Ordinal) ||
            !string.Equals(session.Scope.AppProfileId, binding.AppProfileId, StringComparison.Ordinal) ||
            session.Scope.AppProfileVersion != binding.AppProfileVersion)
        {
            throw new InvalidOperationException("Teach NODAL capture session does not match the Windows application binding.");
        }
    }

    private static IReadOnlyList<string> Evidence(IEnumerable<string> values) => values
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => Identifier(value, 160, "evidence reference"))
        .Distinct(StringComparer.Ordinal)
        .OrderBy(value => value, StringComparer.Ordinal)
        .Take(512)
        .ToArray();

    private static string Identifier(string? value, int maximumLength, string field)
    {
        var normalized = SafeRuntimeText.Sanitize(value, maximumLength);
        if (normalized.Length == 0)
            throw new ArgumentException($"{field} is required.");
        if (HistorySanitizer.ContainsSecretLikeContent(value) || ContainsLocalPath(value))
            throw new ArgumentException($"{field} contains secret-like or local-path content.");
        return normalized;
    }

    private static bool ContainsLocalPath(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        HistorySanitizer.SanitizeText(value).Contains("[LOCAL_PATH]", StringComparison.Ordinal);

    private static string ShortHash(string value, int characters) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))
            .ToLowerInvariant()[..characters];

    private sealed record PendingStep(TeachNodalWindowsStepToken Token);
}
