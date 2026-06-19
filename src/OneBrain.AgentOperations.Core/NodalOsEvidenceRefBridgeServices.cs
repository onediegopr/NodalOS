using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NodalOsEvidenceRefBridge
{
    private const string RedactedPlaceholder = "[REDACTED]";
    private readonly NodalOsRedactionService redaction;

    public NodalOsEvidenceRefBridge()
        : this(new NodalOsRedactionService())
    {
    }

    public NodalOsEvidenceRefBridge(NodalOsRedactionService redaction) =>
        this.redaction = redaction;

    public NodalOsEvidenceBridgeResult BridgeFromEvidenceRef(
        NexaEvidenceRef evidenceRef,
        NodalOsEvidenceBridgeSourceKind sourceKind,
        NodalOsEvidenceBridgeUseKind useKind,
        NodalOsEvidenceBridgeOptions? options = null)
    {
        var effectiveOptions = options ?? new NodalOsEvidenceBridgeOptions();
        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(evidenceRef.EvidenceId))
            errors.Add("EvidenceId is required.");
        if (string.IsNullOrWhiteSpace(evidenceRef.Kind))
            errors.Add("Kind is required.");
        if (sourceKind == NodalOsEvidenceBridgeSourceKind.Unknown)
            warnings.Add("Evidence source kind is Unknown; bridge result is diagnostic-quality until provenance is clarified.");

        var redactedEvidenceId = RedactIfNeeded(evidenceRef.EvidenceId);
        var redactedKind = RedactIfNeeded(evidenceRef.Kind);
        var redactedRef = evidenceRef.Ref is null ? null : RedactIfNeeded(evidenceRef.Ref);
        var redactedHash = evidenceRef.Hash is null ? null : RedactIfNeeded(evidenceRef.Hash);

        var sensitive =
            redaction.ContainsSensitiveContent(evidenceRef.EvidenceId) ||
            redaction.ContainsSensitiveContent(evidenceRef.Kind) ||
            redaction.ContainsSensitiveContent(evidenceRef.Ref) ||
            redaction.ContainsSensitiveContent(evidenceRef.Hash);
        var alreadyRedacted = IsRedacted(evidenceRef.EvidenceId) ||
                              IsRedacted(evidenceRef.Kind) ||
                              IsRedacted(evidenceRef.Ref) ||
                              IsRedacted(evidenceRef.Hash);

        var sensitivity = ResolveSensitivity(sensitive, alreadyRedacted);
        var redactionState = ResolveRedactionState(sensitive, alreadyRedacted, effectiveOptions);

        if (sensitive && !alreadyRedacted && effectiveOptions.RequireRedactionForPotentiallySensitive)
        {
            if (effectiveOptions.RejectSensitiveWithoutRedaction)
                errors.Add("EvidenceRef contains sensitive content requiring redaction.");
            else
                warnings.Add("EvidenceRef contains sensitive content and must be redacted before persistence.");
        }

        var ledgerRef = ResolveLedgerRef(evidenceRef.Ref, redactedRef);
        if (!effectiveOptions.AllowLedgerRefOptional && string.IsNullOrWhiteSpace(ledgerRef))
            errors.Add("LedgerRef is required by bridge options.");

        var bridgeRef = new NodalOsEvidenceBridgeRef
        {
            EvidenceId = redactedEvidenceId,
            Kind = redactedKind,
            Ref = redactedRef,
            Hash = redactedHash,
            SourceKind = sourceKind,
            UseKind = useKind,
            Authority = ResolveAuthority(useKind),
            Sensitivity = sensitivity,
            RedactionState = redactionState,
            LedgerRef = ledgerRef,
            Provenance = BuildProvenance(sourceKind, useKind, redactedEvidenceId),
            CreatedAt = evidenceRef.CreatedAt
        };

        var validation = ValidateBridgeRef(bridgeRef, effectiveOptions);
        errors.AddRange(validation.Errors);
        warnings.AddRange(validation.Warnings);

        return new NodalOsEvidenceBridgeResult
        {
            Accepted = errors.Count == 0,
            Evidence = bridgeRef,
            Errors = errors.Distinct(StringComparer.Ordinal).ToArray(),
            Warnings = warnings.Distinct(StringComparer.Ordinal).ToArray()
        };
    }

    public IReadOnlyList<NodalOsEvidenceBridgeResult> BridgeFromRunReport(
        NexaRunReport report,
        NodalOsEvidenceBridgeUseKind useKind = NodalOsEvidenceBridgeUseKind.AuditTrail,
        NodalOsEvidenceBridgeOptions? options = null)
    {
        var refs = new List<NexaEvidenceRef>();
        refs.AddRange(report.EvidenceRefs);
        refs.AddRange(report.Steps.SelectMany(step => step.EvidenceRefs));
        refs.AddRange(report.Failures.SelectMany(failure => failure.EvidenceRefs));

        return refs
            .Select(evidenceRef => BridgeFromEvidenceRef(
                evidenceRef,
                NodalOsEvidenceBridgeSourceKind.RunReport,
                useKind,
                options))
            .ToArray();
    }

    public IReadOnlyList<NodalOsEvidenceBridgeResult> BridgeFromProgressReport(
        NodalOsAgentProgressReport report,
        NodalOsEvidenceBridgeUseKind useKind = NodalOsEvidenceBridgeUseKind.AuditTrail,
        NodalOsEvidenceBridgeOptions? options = null)
    {
        var refs = new List<NexaEvidenceRef>();
        refs.AddRange(report.EvidenceRefs);
        refs.AddRange(report.ProgressNotes.SelectMany(note => note.EvidenceRefs));
        refs.AddRange(report.Blockers.SelectMany(blocker => blocker.EvidenceRefs));
        refs.AddRange(report.HumanDecisionRequests.SelectMany(request => request.EvidenceRefs));
        refs.AddRange(report.VerificationSummaries.SelectMany(summary => summary.EvidenceRefs));

        return refs
            .Select(evidenceRef => BridgeFromEvidenceRef(
                evidenceRef,
                NodalOsEvidenceBridgeSourceKind.ProgressReport,
                useKind,
                options))
            .ToArray();
    }

    public IReadOnlyList<NodalOsEvidenceBridgeResult> BridgeFromVerificationResult(
        NodalOsVerificationBeforeDoneResult result,
        NodalOsEvidenceBridgeOptions? options = null) =>
        result.EvidenceRefs
            .Select(evidenceRef => BridgeFromEvidenceRef(
                evidenceRef,
                NodalOsEvidenceBridgeSourceKind.VerificationGate,
                NodalOsEvidenceBridgeUseKind.VerificationSupport,
                options))
            .ToArray();

    public NodalOsEvidenceBridgeResult ValidateBridgeRef(
        NodalOsEvidenceBridgeRef bridgeRef,
        NodalOsEvidenceBridgeOptions? options = null)
    {
        var effectiveOptions = options ?? new NodalOsEvidenceBridgeOptions();
        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(bridgeRef.EvidenceId))
            errors.Add("EvidenceId is required.");
        if (string.IsNullOrWhiteSpace(bridgeRef.Kind))
            errors.Add("Kind is required.");
        if (bridgeRef.SourceKind == NodalOsEvidenceBridgeSourceKind.Unknown)
            warnings.Add("Evidence source kind is Unknown; bridge result is diagnostic-quality until provenance is clarified.");
        if (bridgeRef.Authority is not NodalOsEvidenceBridgeAuthority.NoAuthority and
            not NodalOsEvidenceBridgeAuthority.SupportsVerificationOnly and
            not NodalOsEvidenceBridgeAuthority.DiagnosticOnly)
        {
            errors.Add("Evidence bridge authority cannot authorize actions.");
        }

        if (bridgeRef.Sensitivity == NodalOsEvidenceSensitivity.SecretRedacted &&
            bridgeRef.RedactionState != NodalOsEvidenceRedactionState.Redacted)
        {
            errors.Add("SecretRedacted evidence requires Redacted redaction state.");
        }

        if (bridgeRef.RedactionState == NodalOsEvidenceRedactionState.RedactionRequired &&
            effectiveOptions.RejectSensitiveWithoutRedaction)
        {
            errors.Add("Evidence requires redaction before bridge acceptance.");
        }

        if (bridgeRef.RedactionState == NodalOsEvidenceRedactionState.RejectedSensitive)
            errors.Add("Rejected sensitive evidence cannot cross the evidence bridge boundary.");

        if (bridgeRef.Sensitivity == NodalOsEvidenceSensitivity.Sensitive &&
            bridgeRef.RedactionState != NodalOsEvidenceRedactionState.Redacted &&
            effectiveOptions.RejectSensitiveWithoutRedaction)
        {
            errors.Add("Sensitive evidence must be redacted before bridge acceptance.");
        }

        if (ContainsSensitiveBridgeContent(bridgeRef) &&
            bridgeRef.RedactionState != NodalOsEvidenceRedactionState.Redacted &&
            effectiveOptions.RejectSensitiveWithoutRedaction)
        {
            errors.Add("Evidence bridge ref contains sensitive content requiring redaction.");
        }

        if (!effectiveOptions.AllowLedgerRefOptional && string.IsNullOrWhiteSpace(bridgeRef.LedgerRef))
            errors.Add("LedgerRef is required by bridge options.");

        return new NodalOsEvidenceBridgeResult
        {
            Accepted = errors.Count == 0,
            Evidence = bridgeRef,
            Errors = errors.Distinct(StringComparer.Ordinal).ToArray(),
            Warnings = warnings.Distinct(StringComparer.Ordinal).ToArray()
        };
    }

    public NodalOsEvidenceBridgeResult CreateDiagnosticRejected(
        NexaEvidenceRef evidenceRef,
        string reason,
        NodalOsEvidenceBridgeSourceKind sourceKind = NodalOsEvidenceBridgeSourceKind.Unknown)
    {
        var result = BridgeFromEvidenceRef(
            evidenceRef,
            sourceKind,
            NodalOsEvidenceBridgeUseKind.DiagnosticOnly,
            new NodalOsEvidenceBridgeOptions { RejectSensitiveWithoutRedaction = false });

        return result with
        {
            Accepted = false,
            Evidence = result.Evidence with
            {
                Authority = NodalOsEvidenceBridgeAuthority.DiagnosticOnly,
                UseKind = NodalOsEvidenceBridgeUseKind.DiagnosticOnly,
                RedactionState = result.Evidence.RedactionState == NodalOsEvidenceRedactionState.NotRequired
                    ? NodalOsEvidenceRedactionState.RejectedSensitive
                    : result.Evidence.RedactionState
            },
            Errors = ["Evidence rejected diagnostically."],
            Warnings = [RedactIfNeeded(reason)]
        };
    }

    private string RedactIfNeeded(string value) =>
        redaction.RedactValue(value).Value;

    private bool ContainsSensitiveBridgeContent(NodalOsEvidenceBridgeRef bridgeRef) =>
        redaction.ContainsSensitiveContent(bridgeRef.EvidenceId) ||
        redaction.ContainsSensitiveContent(bridgeRef.Kind) ||
        redaction.ContainsSensitiveContent(bridgeRef.Ref) ||
        redaction.ContainsSensitiveContent(bridgeRef.Hash) ||
        redaction.ContainsSensitiveContent(bridgeRef.LedgerRef) ||
        redaction.ContainsSensitiveContent(bridgeRef.Provenance);

    private static bool IsRedacted(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Contains(RedactedPlaceholder, StringComparison.OrdinalIgnoreCase);

    private static NodalOsEvidenceSensitivity ResolveSensitivity(bool sensitive, bool alreadyRedacted)
    {
        if (alreadyRedacted)
            return NodalOsEvidenceSensitivity.SecretRedacted;
        return sensitive ? NodalOsEvidenceSensitivity.Sensitive : NodalOsEvidenceSensitivity.NonSensitive;
    }

    private static NodalOsEvidenceRedactionState ResolveRedactionState(
        bool sensitive,
        bool alreadyRedacted,
        NodalOsEvidenceBridgeOptions options)
    {
        if (alreadyRedacted)
            return NodalOsEvidenceRedactionState.Redacted;
        if (!sensitive)
            return NodalOsEvidenceRedactionState.NotRequired;
        return options.RejectSensitiveWithoutRedaction
            ? NodalOsEvidenceRedactionState.RedactionRequired
            : NodalOsEvidenceRedactionState.Redacted;
    }

    private static NodalOsEvidenceBridgeAuthority ResolveAuthority(NodalOsEvidenceBridgeUseKind useKind) =>
        useKind switch
        {
            NodalOsEvidenceBridgeUseKind.DiagnosticOnly => NodalOsEvidenceBridgeAuthority.DiagnosticOnly,
            NodalOsEvidenceBridgeUseKind.VerificationSupport => NodalOsEvidenceBridgeAuthority.SupportsVerificationOnly,
            _ => NodalOsEvidenceBridgeAuthority.NoAuthority
        };

    private static string? ResolveLedgerRef(string? originalRef, string? redactedRef)
    {
        if (string.IsNullOrWhiteSpace(originalRef) || !string.Equals(originalRef, redactedRef, StringComparison.Ordinal))
            return null;

        return originalRef.StartsWith("audit:", StringComparison.OrdinalIgnoreCase) ||
               originalRef.StartsWith("ledger:", StringComparison.OrdinalIgnoreCase)
            ? redactedRef
            : null;
    }

    private static string BuildProvenance(
        NodalOsEvidenceBridgeSourceKind sourceKind,
        NodalOsEvidenceBridgeUseKind useKind,
        string evidenceId) =>
        $"{sourceKind}:{useKind}:{evidenceId}";
}
