using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OneBrain.WindowsComputerUse;

public enum ComputerUseHandoffReasonCode
{
    Unknown,
    LowConfidence,
    SensitiveSurface,
    BlockageDetected,
    VisualOnlyTarget,
    UacAdmin,
    LiveExecutionBlocked,
    RedactionRequired,
    ExternalAuditNoGo,
    ReplaySafety
}

public enum ComputerUseHandoffRedactionStatus
{
    None,
    Partial,
    Full,
    Required
}

public enum ComputerUseHandoffReplaySafety
{
    ReplayIsEvidenceOnly,
    ReplayBlockedNoAction,
    ReplayRejected
}

public sealed record ComputerUseHandoffId(string Value);

public sealed record ComputerUseBridgeTransferPolicy(
    bool EvidenceOnly,
    bool AllowNetworkTransfer,
    bool AllowProviderCall,
    bool AllowProcessExecution,
    bool AllowActionAuthority,
    bool AllowRawScreenshot,
    bool AllowClipboard,
    bool RequiresRedaction,
    bool RequiresHumanHandoff);

public sealed record ComputerUseBridgeObservation(
    string ObservationId,
    string SourceKind,
    string PayloadRedacted,
    ComputerUseRedactionStatus RedactionStatus,
    IReadOnlyList<string> SensitiveFieldsRedacted,
    bool EvidenceOnly,
    bool ActionAuthority,
    bool RawScreenshotPresent,
    bool ClipboardPresent,
    bool LiveProviderCalled);

public sealed record ComputerUseHandoffEnvelope(
    ComputerUseHandoffId HandoffId,
    string StableHandoffKey,
    string CorrelationId,
    IReadOnlyList<ComputerUseHandoffReasonCode> ReasonCodes,
    ComputerUseHandoffRedactionStatus RedactionStatus,
    ComputerUseHandoffReplaySafety ReplaySafety,
    ComputerUseBridgeTransferPolicy TransferPolicy,
    IReadOnlyList<ComputerUseBridgeObservation> BridgeObservations,
    IReadOnlyList<ComputerUseEvidenceRef> EvidenceRefs,
    string SummaryRedacted,
    IReadOnlyList<string> SensitiveFieldsRedacted,
    bool LiveReadPermitted,
    bool ActionAuthorityGranted,
    bool ProductAutomationEnabled,
    bool RawScreenshotPresent,
    bool ClipboardPresent,
    bool ReplayCanExecuteAction,
    DateTimeOffset CreatedAtUtc);

public sealed record ComputerUseBridgeIdempotencyResult(
    bool StableHandoffKeyMatched,
    bool DuplicatePrevented,
    bool RedactionPersisted,
    bool AuthorityRemainedFalse,
    bool ReplayAsActionBlocked,
    IReadOnlyList<string> Reasons);

public sealed class ComputerUseBridgeHandoffBuilder
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
    private readonly ComputerUseEvidenceRedactor _redactor = new();

    public ComputerUseHandoffEnvelope Build(
        ComputerUseSnapshot snapshot,
        ComputerUseUnifiedEvidencePack evidence,
        ComputerUseLocatorFusionResult? locatorFusion = null,
        RobustPerceptionBridgeResult? bridgeResult = null,
        string handoffReason = "Containment-only handoff; live prototype remains blocked.")
    {
        var reasonRedaction = _redactor.Redact(handoffReason);
        var observations = BuildObservations(snapshot, evidence, locatorFusion, bridgeResult).ToArray();
        var refs = evidence.EvidenceRefs
            .Concat(locatorFusion?.EvidenceRefs.Select(r => new ComputerUseEvidenceRef(r, "locator.fusion.ref", "Redacted locator fusion evidence ref.", true)) ?? [])
            .DistinctBy(r => r.RefId, StringComparer.OrdinalIgnoreCase)
            .OrderBy(r => r.RefId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var sensitiveFields = evidence.SensitiveFieldsRedacted
            .Concat(reasonRedaction.SensitiveFieldsRedacted)
            .Concat(observations.SelectMany(o => o.SensitiveFieldsRedacted))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var reasonCodes = ResolveReasonCodes(evidence, locatorFusion).ToArray();
        var stableKeyInput = string.Join("|", new[]
        {
            snapshot.SnapshotId,
            snapshot.Scenario,
            evidence.CorrelationId,
            string.Join(",", reasonCodes.OrderBy(r => r.ToString())),
            string.Join(",", refs.Select(r => r.RefId)),
            reasonRedaction.Value,
            evidence.SummaryRedacted
        });
        var stableKey = Sha256(stableKeyInput);

        return new ComputerUseHandoffEnvelope(
            HandoffId: new ComputerUseHandoffId($"wcu-handoff-{stableKey[..24]}"),
            StableHandoffKey: stableKey,
            CorrelationId: evidence.CorrelationId,
            ReasonCodes: reasonCodes,
            RedactionStatus: ToHandoffRedactionStatus(evidence.RedactionStatus, reasonRedaction.Status, observations),
            ReplaySafety: ComputerUseHandoffReplaySafety.ReplayBlockedNoAction,
            TransferPolicy: ContainmentOnlyPolicy(),
            BridgeObservations: observations,
            EvidenceRefs: refs,
            SummaryRedacted: reasonRedaction.Value,
            SensitiveFieldsRedacted: sensitiveFields,
            LiveReadPermitted: false,
            ActionAuthorityGranted: false,
            ProductAutomationEnabled: false,
            RawScreenshotPresent: false,
            ClipboardPresent: false,
            ReplayCanExecuteAction: false,
            CreatedAtUtc: DateTimeOffset.UnixEpoch);
    }

    public ComputerUseBridgeIdempotencyResult Compare(ComputerUseHandoffEnvelope first, ComputerUseHandoffEnvelope second)
    {
        var stableKeyMatched = string.Equals(first.StableHandoffKey, second.StableHandoffKey, StringComparison.Ordinal);
        var duplicatePrevented = stableKeyMatched && string.Equals(first.HandoffId.Value, second.HandoffId.Value, StringComparison.Ordinal);
        var redactionPersisted = IsRedactionPersistent(first) && IsRedactionPersistent(second);
        var authorityFalse = NoAuthority(first) && NoAuthority(second);
        var replayBlocked = !first.ReplayCanExecuteAction && !second.ReplayCanExecuteAction &&
            first.ReplaySafety == ComputerUseHandoffReplaySafety.ReplayBlockedNoAction &&
            second.ReplaySafety == ComputerUseHandoffReplaySafety.ReplayBlockedNoAction;

        return new ComputerUseBridgeIdempotencyResult(
            stableKeyMatched,
            duplicatePrevented,
            redactionPersisted,
            authorityFalse,
            replayBlocked,
            BuildReasons(stableKeyMatched, duplicatePrevented, redactionPersisted, authorityFalse, replayBlocked));
    }

    public string Serialize(ComputerUseHandoffEnvelope envelope) =>
        JsonSerializer.Serialize(envelope, SerializerOptions);

    public ComputerUseHandoffEnvelope Deserialize(string json) =>
        JsonSerializer.Deserialize<ComputerUseHandoffEnvelope>(json, SerializerOptions)
            ?? throw new InvalidOperationException("Computer use handoff envelope could not be deserialized.");

    public bool CanReplayAsAction(ComputerUseHandoffEnvelope envelope) =>
        envelope.ReplayCanExecuteAction ||
        envelope.ActionAuthorityGranted ||
        envelope.TransferPolicy.AllowActionAuthority ||
        envelope.TransferPolicy.AllowProcessExecution ||
        envelope.TransferPolicy.AllowProviderCall;

    public static ComputerUseBridgeTransferPolicy ContainmentOnlyPolicy() =>
        new(
            EvidenceOnly: true,
            AllowNetworkTransfer: false,
            AllowProviderCall: false,
            AllowProcessExecution: false,
            AllowActionAuthority: false,
            AllowRawScreenshot: false,
            AllowClipboard: false,
            RequiresRedaction: true,
            RequiresHumanHandoff: true);

    private IEnumerable<ComputerUseBridgeObservation> BuildObservations(
        ComputerUseSnapshot snapshot,
        ComputerUseUnifiedEvidencePack evidence,
        ComputerUseLocatorFusionResult? locatorFusion,
        RobustPerceptionBridgeResult? bridgeResult)
    {
        yield return Observation(
            "unified-evidence",
            "evidence.unified",
            $"{evidence.EvidenceId}; {evidence.SummaryRedacted}; handoff={evidence.RequiresHumanHandoff}");

        yield return Observation(
            "snapshot",
            "uia.snapshot.fixture",
            $"{snapshot.SnapshotId}; {snapshot.Scenario}; windows={snapshot.Windows.Count}");

        if (locatorFusion is not null)
        {
            yield return Observation(
                "locator-fusion",
                "locator.fusion",
                $"{locatorFusion.BestCandidate?.CandidateId ?? "none"}; confidence={locatorFusion.BestCandidate?.ConfidenceBreakdown.FinalConfidence ?? 0:0.00}; handoff={locatorFusion.RequiresHumanHandoff}; reasons={string.Join(",", locatorFusion.HandoffReasons)}");
        }

        if (bridgeResult is not null)
        {
            foreach (var observation in bridgeResult.Observations)
            {
                var text = string.Join("; ", observation.Signals.SelectMany(s => s.TextObservations).Select(t => t.TextRedacted)
                    .Concat(observation.Signals.SelectMany(s => s.ElementObservations).Select(e => e.LabelRedacted)));
                yield return Observation(
                    observation.ObservationId,
                    bridgeResult.ProviderId,
                    string.IsNullOrWhiteSpace(text) ? "redacted visual observation metadata" : text,
                    observation.SensitiveFieldsRedacted);
            }
        }
    }

    private ComputerUseBridgeObservation Observation(
        string id,
        string source,
        string payload,
        IEnumerable<string>? extraSensitiveFields = null)
    {
        var redaction = _redactor.Redact(payload);
        var sensitiveFields = redaction.SensitiveFieldsRedacted
            .Concat(extraSensitiveFields ?? [])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new ComputerUseBridgeObservation(
            id,
            source,
            redaction.Value,
            redaction.Status,
            sensitiveFields,
            EvidenceOnly: true,
            ActionAuthority: false,
            RawScreenshotPresent: false,
            ClipboardPresent: false,
            LiveProviderCalled: false);
    }

    private static IReadOnlyList<ComputerUseHandoffReasonCode> ResolveReasonCodes(
        ComputerUseUnifiedEvidencePack evidence,
        ComputerUseLocatorFusionResult? locatorFusion)
    {
        var codes = new SortedSet<ComputerUseHandoffReasonCode>();
        codes.Add(ComputerUseHandoffReasonCode.LiveExecutionBlocked);
        codes.Add(ComputerUseHandoffReasonCode.ExternalAuditNoGo);
        codes.Add(ComputerUseHandoffReasonCode.ReplaySafety);
        if (evidence.RedactionStatus != ComputerUseRedactionStatus.None || evidence.SensitiveFieldsRedacted.Count > 0)
            codes.Add(ComputerUseHandoffReasonCode.RedactionRequired);
        if (evidence.RequiresHumanHandoff || locatorFusion?.RequiresHumanHandoff == true)
            codes.Add(ComputerUseHandoffReasonCode.BlockageDetected);
        if (locatorFusion?.VisualFallbackRequired == true)
            codes.Add(ComputerUseHandoffReasonCode.VisualOnlyTarget);
        if (locatorFusion?.HandoffReasons.Any(r => r is ComputerUseLocatorHandoffReason.LowConfidence) == true)
            codes.Add(ComputerUseHandoffReasonCode.LowConfidence);
        if (locatorFusion?.HandoffReasons.Any(r => r is ComputerUseLocatorHandoffReason.SensitiveSurface) == true)
            codes.Add(ComputerUseHandoffReasonCode.SensitiveSurface);
        if (locatorFusion?.HandoffReasons.Any(r => r is ComputerUseLocatorHandoffReason.UacAdmin) == true)
            codes.Add(ComputerUseHandoffReasonCode.UacAdmin);

        return codes.ToArray();
    }

    private static ComputerUseHandoffRedactionStatus ToHandoffRedactionStatus(
        ComputerUseRedactionStatus evidence,
        ComputerUseRedactionStatus reason,
        IReadOnlyList<ComputerUseBridgeObservation> observations)
    {
        if (evidence == ComputerUseRedactionStatus.Full || reason == ComputerUseRedactionStatus.Full)
            return ComputerUseHandoffRedactionStatus.Full;
        if (evidence == ComputerUseRedactionStatus.Partial ||
            reason == ComputerUseRedactionStatus.Partial ||
            observations.Any(o => o.RedactionStatus != ComputerUseRedactionStatus.None || o.SensitiveFieldsRedacted.Count > 0))
        {
            return ComputerUseHandoffRedactionStatus.Partial;
        }

        return ComputerUseHandoffRedactionStatus.Required;
    }

    private static bool IsRedactionPersistent(ComputerUseHandoffEnvelope envelope) =>
        envelope.RedactionStatus != ComputerUseHandoffRedactionStatus.None &&
        envelope.BridgeObservations.All(o => o.EvidenceOnly && !o.RawScreenshotPresent && !o.ClipboardPresent && !o.LiveProviderCalled);

    private static bool NoAuthority(ComputerUseHandoffEnvelope envelope) =>
        !envelope.LiveReadPermitted &&
        !envelope.ActionAuthorityGranted &&
        !envelope.ProductAutomationEnabled &&
        !envelope.RawScreenshotPresent &&
        !envelope.ClipboardPresent &&
        !envelope.TransferPolicy.AllowActionAuthority &&
        !envelope.TransferPolicy.AllowProcessExecution;

    private static IReadOnlyList<string> BuildReasons(
        bool stableKeyMatched,
        bool duplicatePrevented,
        bool redactionPersisted,
        bool authorityFalse,
        bool replayBlocked)
    {
        var reasons = new List<string>();
        Add(reasons, stableKeyMatched, "stable_handoff_key_matched");
        Add(reasons, duplicatePrevented, "duplicate_prevented");
        Add(reasons, redactionPersisted, "redaction_persisted");
        Add(reasons, authorityFalse, "authority_remained_false");
        Add(reasons, replayBlocked, "replay_as_action_blocked");
        return reasons;
    }

    private static void Add(ICollection<string> reasons, bool condition, string reason)
    {
        if (condition)
            reasons.Add(reason);
    }

    private static string Sha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
