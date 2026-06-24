namespace OneBrain.Safety.Tests.SimulatedRuntime;

// F2 remediation: a REAL adversarial redactor. Unlike the previous declarative
// redaction (which prefixed "REDACTED:" and hardcoded ContainsSecrets:false),
// this scans an actual payload of forbidden-field values, builds output that
// never copies those values, and reports detection/redaction measured from the
// real payload. A disabled mode exists ONLY so tests can prove they can detect a
// redaction failure (i.e., the assertion is not vacuous).
//
// All sensitive values used by tests are explicitly FAKE_* placeholders; no real
// secret, credential, or session value is ever present.

public sealed record RawAuditPayload(
    string SafeSummary,
    string? Secret = null,
    string? Credential = null,
    string? Token = null,
    string? Cookie = null,
    string? RawUserData = null,
    string? RawLog = null,
    string? ProviderKey = null,
    string? PrivateKey = null,
    string? BrowserSessionData = null)
{
    public IReadOnlyList<(string Field, string Value)> PopulatedForbiddenFields =>
        new[]
        {
            ("secrets", Secret),
            ("credentials", Credential),
            ("tokens", Token),
            ("cookies", Cookie),
            ("rawUserData", RawUserData),
            ("rawLogs", RawLog),
            ("providerKeys", ProviderKey),
            ("privateKeys", PrivateKey),
            ("browserSessionData", BrowserSessionData)
        }
        .Where(static x => !string.IsNullOrEmpty(x.Item2))
        .Select(static x => (x.Item1, x.Item2!))
        .ToArray();

    public IReadOnlyList<string> SensitiveValues =>
        PopulatedForbiddenFields.Select(static x => x.Value).ToArray();
}

public sealed record AdversarialRedactionResult(
    string RedactedPayload,
    bool RawContainedSensitive,
    bool SecretDetected,
    bool Redacted,
    string RedactionMarker,
    RedactionProof RedactionProof,
    IReadOnlyList<string> DetectedFields,
    bool RedactionEnabled)
{
    public bool OutputContainsAnySensitiveValue(IEnumerable<string> sensitiveValues) =>
        sensitiveValues.Any(v => RedactedPayload.Contains(v, StringComparison.Ordinal));
}

public sealed class SimulatedRedactor
{
    public const string Marker = "[REDACTED]";

    public AdversarialRedactionResult Redact(RawAuditPayload payload, bool redactionEnabled = true)
    {
        var populated = payload.PopulatedForbiddenFields;
        var detected = populated.Count > 0;

        string redactedPayload;
        if (redactionEnabled)
        {
            // Output is built from ONLY the safe summary plus per-field markers.
            // Forbidden values are never copied into the output.
            var markers = string.Join("; ", populated.Select(x => $"{x.Field}={Marker}"));
            redactedPayload = detected ? $"{payload.SafeSummary} | {markers}" : payload.SafeSummary;
        }
        else
        {
            // UNSAFE mode: copies raw values through. Exists only to prove the
            // adversarial test can detect a redaction failure (non-vacuous).
            var raw = string.Join("; ", populated.Select(x => $"{x.Field}={x.Value}"));
            redactedPayload = detected ? $"{payload.SafeSummary} | {raw}" : payload.SafeSummary;
        }

        var leaked = populated.Where(x => redactedPayload.Contains(x.Value, StringComparison.Ordinal))
            .Select(static x => x.Field)
            .ToHashSet(StringComparer.Ordinal);

        return new AdversarialRedactionResult(
            RedactedPayload: redactedPayload,
            RawContainedSensitive: detected,
            SecretDetected: detected,
            Redacted: redactionEnabled && detected,
            RedactionMarker: Marker,
            RedactionProof: new RedactionProof(
                SecretsIncluded: leaked.Contains("secrets"),
                CredentialsIncluded: leaked.Contains("credentials"),
                TokensIncluded: leaked.Contains("tokens"),
                CookiesIncluded: leaked.Contains("cookies"),
                RawUserDataIncluded: leaked.Contains("rawUserData"),
                RawLogsIncluded: leaked.Contains("rawLogs"),
                ProviderKeysIncluded: leaked.Contains("providerKeys"),
                PrivateKeysIncluded: leaked.Contains("privateKeys"),
                BrowserSessionDataIncluded: leaked.Contains("browserSessionData")),
            DetectedFields: populated.Select(static x => x.Field).ToArray(),
            RedactionEnabled: redactionEnabled);
    }
}

public sealed record RedactedAuditExportPackage(
    string ExportId,
    string SerializedExport,
    AdversarialRedactionResult Redaction,
    NoExecutionProof NoExecutionProof,
    string EvidenceEnvelopeRef,
    string AuditExportPackageRef,
    string RedactionProofRef);

public sealed class SimulatedRedactingExporter
{
    // F2: export a payload through the real redactor. The serialized export string
    // is assembled from the redacted payload, so when redaction is enabled the
    // export cannot contain the original forbidden values.
    public RedactedAuditExportPackage ExportWithRawPayload(
        string exportContextId,
        RawAuditPayload payload,
        bool redactionEnabled = true) =>
        ExportWithRawPayload(exportContextId, payload, new RecordingSideEffectSink(), redactionEnabled);

    public RedactedAuditExportPackage ExportWithRawPayload(
        string exportContextId,
        RawAuditPayload payload,
        RecordingSideEffectSink sink,
        bool redactionEnabled = true)
    {
        var redaction = new SimulatedRedactor().Redact(payload, redactionEnabled);
        var exportId = $"export-{exportContextId}";
        var serialized =
            "{" +
            $"\"exportId\":\"{exportId}\"," +
            $"\"runtimeType\":\"{SimulatedDryRunOrchestrator.RuntimeType}\"," +
            $"\"fixtureType\":\"{SimulatedDryRunOrchestrator.RequiredFixtureType}\"," +
            $"\"redacted\":{redaction.Redacted.ToString().ToLowerInvariant()}," +
            $"\"secretDetected\":{redaction.SecretDetected.ToString().ToLowerInvariant()}," +
            $"\"redactedPayload\":\"{redaction.RedactedPayload}\"" +
            "}";

        return new RedactedAuditExportPackage(
            ExportId: exportId,
            SerializedExport: serialized,
            Redaction: redaction,
            NoExecutionProof: NoExecutionProof.FromSink(sink),
            EvidenceEnvelopeRef: $"evidence-{exportContextId}",
            AuditExportPackageRef: exportId,
            RedactionProofRef: $"redaction-{exportContextId}");
    }
}
