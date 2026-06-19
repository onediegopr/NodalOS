using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NodalOsWorkerBoundaryValidator
{
    private readonly NodalOsRedactionService redaction;
    private readonly NodalOsEvidenceRefBridge evidenceBridge;

    public NodalOsWorkerBoundaryValidator()
        : this(new NodalOsRedactionService())
    {
    }

    public NodalOsWorkerBoundaryValidator(NodalOsRedactionService redaction)
        : this(redaction, new NodalOsEvidenceRefBridge(redaction))
    {
    }

    public NodalOsWorkerBoundaryValidator(
        NodalOsRedactionService redaction,
        NodalOsEvidenceRefBridge evidenceBridge)
    {
        this.redaction = redaction;
        this.evidenceBridge = evidenceBridge;
    }

    public NodalOsWorkerBoundaryValidationResult ValidateManifest(NodalOsWorkerBoundaryManifest manifest)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, manifest.WorkerId, "WorkerId is required.");
        AddRequired(errors, manifest.Name, "Name is required.");
        AddRequired(errors, manifest.Version, "Version is required.");
        AddRequired(errors, manifest.Provenance, "Provenance is required.");

        if (manifest.BoundaryKind == NodalOsWorkerBoundaryKind.Unknown)
            errors.Add("BoundaryKind must be explicit.");

        if (!manifest.InternalOnly)
            errors.Add("Worker boundary manifests must be InternalOnly in V1.");

        ValidateRuntimeFlags(
            manifest.RuntimeExecutionAllowed,
            manifest.RuntimeExecutionDeferred,
            manifest.RequiresGlobalPolicyEvaluation,
            errors);

        if (manifest.CanAuthorizeActions)
            errors.Add("Worker boundary manifests cannot authorize actions.");

        if (manifest.Status == NodalOsWorkerStatus.Registered && manifest.Capabilities.Count == 0)
            errors.Add("Registered workers require declared capabilities.");

        ValidateStatus(manifest.Status, errors, warnings);

        if (ContainsSensitiveContent(ManifestValues(manifest)))
            errors.Add("Worker boundary manifest contains sensitive or secret-like content.");

        var canPassBoundaryPolicy = errors.Count == 0 && manifest.Status == NodalOsWorkerStatus.Registered;

        return Result(errors, warnings, canPassBoundaryPolicy);
    }

    public NodalOsWorkerBoundaryValidationResult ValidateHealthReport(NodalOsWorkerHealthReport report)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, report.WorkerId, "WorkerId is required.");

        if (report.HealthStatus == NodalOsWorkerHealthStatus.Unknown)
            warnings.Add("Unknown worker health does not grant runtime permission.");

        if (report.HealthStatus == NodalOsWorkerHealthStatus.Healthy)
            warnings.Add("Healthy worker status is diagnostic only and does not grant runtime permission.");

        if (ContainsSensitiveContent(HealthValues(report)))
            errors.Add("Worker health report contains sensitive or secret-like content.");

        return Result(errors, warnings, canPassBoundaryPolicy: false);
    }

    public NodalOsWorkerBoundaryValidationResult ValidateRequestEnvelope(NodalOsWorkerRequestEnvelope request)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, request.RequestId, "RequestId is required.");
        AddRequired(errors, request.WorkerId, "WorkerId is required.");

        if (!request.ExecutionDeferred)
            errors.Add("Worker request envelopes must defer execution in V1.");

        if (!request.RequiresGlobalPolicyEvaluation)
            errors.Add("Worker request envelopes must require global policy evaluation.");

        if (ContainsSensitiveContent(RequestValues(request)))
            errors.Add("Worker request envelope contains sensitive or secret-like content.");

        warnings.Add("Worker request envelope is contract-only and does not grant runtime permission.");
        return Result(errors, warnings, canPassBoundaryPolicy: errors.Count == 0);
    }

    public NodalOsWorkerBoundaryValidationResult ValidateResponseEnvelope(NodalOsWorkerResponseEnvelope response)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, response.ResponseId, "ResponseId is required.");
        AddRequired(errors, response.RequestId, "RequestId is required.");
        AddRequired(errors, response.WorkerId, "WorkerId is required.");

        if (response.Executed)
            errors.Add("Worker response envelopes must report Executed=false in V1.");

        if (!response.RuntimeExecutionDeferred)
            errors.Add("Worker response envelopes must defer runtime execution in V1.");

        ValidateResponseEvidenceRefs(response, errors, warnings);

        if (ContainsSensitiveContent(ResponseValues(response)))
            errors.Add("Worker response envelope contains sensitive or secret-like content.");

        warnings.Add("Worker response envelope cannot authorize actions.");
        return Result(errors, warnings, canPassBoundaryPolicy: errors.Count == 0);
    }

    private void ValidateResponseEvidenceRefs(
        NodalOsWorkerResponseEnvelope response,
        List<string> errors,
        List<string> warnings)
    {
        foreach (var evidenceRef in response.EvidenceRefs)
        {
            var result = evidenceBridge.ValidateBridgeRef(evidenceRef);

            errors.AddRange(result.Errors.Select(error => $"Worker response evidence ref {SafeEvidenceId(evidenceRef)}: {error}"));
            warnings.AddRange(result.Warnings.Select(warning => $"Worker response evidence ref {SafeEvidenceId(evidenceRef)}: {warning}"));

            if (evidenceRef.Authority != NodalOsEvidenceBridgeAuthority.NoAuthority &&
                evidenceRef.Authority != NodalOsEvidenceBridgeAuthority.DiagnosticOnly)
            {
                errors.Add($"Worker response evidence ref {SafeEvidenceId(evidenceRef)} cannot carry verification or action authority.");
            }
        }
    }

    private static void ValidateRuntimeFlags(
        bool runtimeExecutionAllowed,
        bool runtimeExecutionDeferred,
        bool requiresGlobalPolicyEvaluation,
        List<string> errors)
    {
        if (runtimeExecutionAllowed)
            errors.Add("Worker boundary cannot grant runtime execution.");

        if (!runtimeExecutionDeferred)
            errors.Add("Worker boundary must defer runtime execution in V1.");

        if (!requiresGlobalPolicyEvaluation)
            errors.Add("Worker boundary must require global policy evaluation.");
    }

    private static void ValidateStatus(
        NodalOsWorkerStatus status,
        List<string> errors,
        List<string> warnings)
    {
        switch (status)
        {
            case NodalOsWorkerStatus.Draft:
                warnings.Add("Draft worker boundary is design metadata only.");
                break;
            case NodalOsWorkerStatus.Registered:
                warnings.Add("Registered worker boundary can pass boundary policy only; runtime remains deferred.");
                break;
            case NodalOsWorkerStatus.Disabled:
                errors.Add("Disabled worker boundary cannot pass boundary policy.");
                break;
            case NodalOsWorkerStatus.Deprecated:
                errors.Add("Deprecated worker boundary cannot pass boundary policy.");
                break;
            case NodalOsWorkerStatus.Blocked:
                errors.Add("Blocked worker boundary cannot pass boundary policy.");
                break;
        }
    }

    private bool ContainsSensitiveContent(IEnumerable<string?> values) =>
        values.Any(value => !string.IsNullOrWhiteSpace(value) && redaction.ContainsSensitiveContent(value));

    private static IEnumerable<string?> ManifestValues(NodalOsWorkerBoundaryManifest manifest)
    {
        yield return manifest.WorkerId;
        yield return manifest.Name;
        yield return manifest.Description;
        yield return manifest.Version;
        yield return manifest.Provenance;

        foreach (var value in manifest.SupportedPackageIds)
            yield return value;

        foreach (var value in manifest.SupportedSkillIds)
            yield return value;

        foreach (var value in manifest.EvidenceRequirements)
            yield return value;
    }

    private static IEnumerable<string?> HealthValues(NodalOsWorkerHealthReport report)
    {
        yield return report.WorkerId;
        yield return report.Summary;

        foreach (var value in report.Warnings)
            yield return value;
    }

    private static IEnumerable<string?> RequestValues(NodalOsWorkerRequestEnvelope request)
    {
        yield return request.RequestId;
        yield return request.WorkerId;
        yield return request.PackageId;
        yield return request.SkillId;

        foreach (var value in request.EvidenceRequirements)
            yield return value;
    }

    private static IEnumerable<string?> ResponseValues(NodalOsWorkerResponseEnvelope response)
    {
        yield return response.ResponseId;
        yield return response.RequestId;
        yield return response.WorkerId;
        yield return response.Summary;

        foreach (var evidenceRef in response.EvidenceRefs)
        {
            yield return evidenceRef.EvidenceId;
            yield return evidenceRef.Kind;
            yield return evidenceRef.Ref;
            yield return evidenceRef.Hash;
            yield return evidenceRef.LedgerRef;
            yield return evidenceRef.Provenance;
        }
    }

    private static NodalOsWorkerBoundaryValidationResult Result(
        IReadOnlyList<string> errors,
        IReadOnlyList<string> warnings,
        bool canPassBoundaryPolicy) =>
        new()
        {
            IsValid = errors.Count == 0,
            CanPassBoundaryPolicy = canPassBoundaryPolicy,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            Errors = errors,
            Warnings = warnings
        };

    private static void AddRequired(List<string> errors, string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add(message);
    }

    private static string SafeEvidenceId(NodalOsEvidenceBridgeRef evidenceRef) =>
        string.IsNullOrWhiteSpace(evidenceRef.EvidenceId) ? "<missing>" : evidenceRef.EvidenceId;
}

public static class NodalOsWorkerSkillCapabilityMapper
{
    public static NodalOsWorkerCapabilityKind Map(NodalOsSkillCapabilityKind capability) =>
        capability switch
        {
            NodalOsSkillCapabilityKind.ReadOnly => NodalOsWorkerCapabilityKind.ReadOnly,
            NodalOsSkillCapabilityKind.Navigation => NodalOsWorkerCapabilityKind.Navigation,
            NodalOsSkillCapabilityKind.Extraction => NodalOsWorkerCapabilityKind.Extraction,
            NodalOsSkillCapabilityKind.Interaction => NodalOsWorkerCapabilityKind.Interaction,
            NodalOsSkillCapabilityKind.DataEntry => NodalOsWorkerCapabilityKind.DataEntry,
            NodalOsSkillCapabilityKind.FileTransfer => NodalOsWorkerCapabilityKind.FileTransfer,
            NodalOsSkillCapabilityKind.HumanInput => NodalOsWorkerCapabilityKind.HumanInput,
            NodalOsSkillCapabilityKind.ControlFlow => NodalOsWorkerCapabilityKind.ControlFlow,
            NodalOsSkillCapabilityKind.EvidenceProcessing => NodalOsWorkerCapabilityKind.EvidenceProcessing,
            NodalOsSkillCapabilityKind.Reporting => NodalOsWorkerCapabilityKind.Reporting,
            NodalOsSkillCapabilityKind.Unknown => NodalOsWorkerCapabilityKind.Unknown,
            _ => NodalOsWorkerCapabilityKind.Unknown
        };

    public static IReadOnlyList<NodalOsWorkerCapabilityKind> MapMany(IEnumerable<NodalOsSkillCapabilityKind> capabilities) =>
        capabilities.Select(Map).Distinct().ToArray();
}

public static class NodalOsWorkerBoundaryJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string SerializeManifest(NodalOsWorkerBoundaryManifest manifest) =>
        JsonSerializer.Serialize(manifest, Options);

    public static NodalOsWorkerBoundaryManifest DeserializeManifest(string json) =>
        JsonSerializer.Deserialize<NodalOsWorkerBoundaryManifest>(json, Options) ??
            throw new InvalidOperationException("Worker boundary manifest JSON did not deserialize.");

    public static string SerializeHealthReport(NodalOsWorkerHealthReport report) =>
        JsonSerializer.Serialize(report, Options);

    public static NodalOsWorkerHealthReport DeserializeHealthReport(string json) =>
        JsonSerializer.Deserialize<NodalOsWorkerHealthReport>(json, Options) ??
            throw new InvalidOperationException("Worker health report JSON did not deserialize.");

    public static string SerializeRequestEnvelope(NodalOsWorkerRequestEnvelope request) =>
        JsonSerializer.Serialize(request, Options);

    public static NodalOsWorkerRequestEnvelope DeserializeRequestEnvelope(string json) =>
        JsonSerializer.Deserialize<NodalOsWorkerRequestEnvelope>(json, Options) ??
            throw new InvalidOperationException("Worker request envelope JSON did not deserialize.");

    public static string SerializeResponseEnvelope(NodalOsWorkerResponseEnvelope response) =>
        JsonSerializer.Serialize(response, Options);

    public static NodalOsWorkerResponseEnvelope DeserializeResponseEnvelope(string json) =>
        JsonSerializer.Deserialize<NodalOsWorkerResponseEnvelope>(json, Options) ??
            throw new InvalidOperationException("Worker response envelope JSON did not deserialize.");
}

public static class NodalOsWorkerBoundaryFixtures
{
    public static readonly DateTimeOffset FixedTimestamp = new(2026, 6, 19, 0, 0, 0, TimeSpan.Zero);

    public static NodalOsWorkerBoundaryManifest ValidDraftWorker() =>
        ValidRegisteredWorker() with
        {
            WorkerId = "worker-draft-001",
            Name = "Draft worker boundary",
            Status = NodalOsWorkerStatus.Draft,
            Capabilities = []
        };

    public static NodalOsWorkerBoundaryManifest ValidRegisteredWorker() =>
        new()
        {
            WorkerId = "worker-internal-readonly-001",
            Name = "Internal read-only worker boundary",
            Description = "Contract-only worker boundary fixture. No runtime worker is implemented.",
            Version = "1.0.0",
            Status = NodalOsWorkerStatus.Registered,
            BoundaryKind = NodalOsWorkerBoundaryKind.InProcessAdapter,
            Capabilities =
            [
                NodalOsWorkerCapabilityKind.ReadOnly,
                NodalOsWorkerCapabilityKind.Reporting,
                NodalOsWorkerCapabilityKind.EvidenceProcessing
            ],
            SupportedPackageIds = ["pkg-internal-readonly-001"],
            SupportedSkillIds = ["skill-readonly-001"],
            Provenance = "internal-fixture",
            InternalOnly = true,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            CanAuthorizeActions = false,
            EvidenceRequirements = ["worker-boundary-review", "evidence-ref-bridge"],
            CreatedAt = FixedTimestamp,
            UpdatedAt = FixedTimestamp
        };

    public static NodalOsWorkerBoundaryManifest BlockedWorker() =>
        ValidRegisteredWorker() with
        {
            WorkerId = "worker-blocked-001",
            Name = "Blocked worker boundary",
            Status = NodalOsWorkerStatus.Blocked
        };

    public static NodalOsWorkerBoundaryManifest RuntimeExecutionAttemptInvalidWorker() =>
        ValidRegisteredWorker() with
        {
            WorkerId = "worker-runtime-attempt-001",
            RuntimeExecutionAllowed = true,
            RuntimeExecutionDeferred = false,
            CanAuthorizeActions = true
        };

    public static NodalOsWorkerHealthReport WorkerHealthReport() =>
        new()
        {
            WorkerId = "worker-internal-readonly-001",
            HealthStatus = NodalOsWorkerHealthStatus.Healthy,
            Summary = "Healthy contract fixture. Health is diagnostic and not execution permission.",
            ReportedCapabilities =
            [
                NodalOsWorkerCapabilityKind.ReadOnly,
                NodalOsWorkerCapabilityKind.Reporting
            ],
            Warnings = ["No runtime worker implemented."],
            CreatedAt = FixedTimestamp
        };

    public static NodalOsWorkerRequestEnvelope RequestEnvelope() =>
        new()
        {
            RequestId = "worker-request-001",
            WorkerId = "worker-internal-readonly-001",
            PackageId = "pkg-internal-readonly-001",
            SkillId = "skill-readonly-001",
            ExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            EvidenceRequirements = ["worker-boundary-review"],
            CreatedAt = FixedTimestamp
        };

    public static NodalOsWorkerResponseEnvelope ResponseEnvelope() =>
        new()
        {
            ResponseId = "worker-response-001",
            RequestId = "worker-request-001",
            WorkerId = "worker-internal-readonly-001",
            Executed = false,
            RuntimeExecutionDeferred = true,
            Summary = "Contract-only response. No worker execution occurred.",
            EvidenceRefs =
            [
                new NodalOsEvidenceBridgeRef
                {
                    EvidenceId = "evidence-worker-boundary-001",
                    Kind = "contract-fixture",
                    Ref = "worker-boundary",
                    Hash = "sha256:fixture",
                    SourceKind = NodalOsEvidenceBridgeSourceKind.AgentOperation,
                    UseKind = NodalOsEvidenceBridgeUseKind.AuditTrail,
                    Authority = NodalOsEvidenceBridgeAuthority.NoAuthority,
                    Sensitivity = NodalOsEvidenceSensitivity.NonSensitive,
                    RedactionState = NodalOsEvidenceRedactionState.NotRequired,
                    LedgerRef = null,
                    Provenance = "internal-fixture",
                    CreatedAt = FixedTimestamp
                }
            ],
            FailureKinds = [NexaFailureKind.HumanInputRequired],
            CreatedAt = FixedTimestamp
        };
}
