using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsScheduledReadOnlyRunValidator
{
    private static readonly string[] ForbiddenActionMarkers =
    [
        "click",
        "type",
        "submit",
        "upload",
        "download",
        "login",
        "captcha",
        "2fa",
        "payment",
        "pay",
        "send",
        "delete",
        "sign",
        "publish",
        "mutate",
        "write",
        "file system mutation"
    ];

    private readonly NodalOsRedactionService redaction;
    private readonly NodalOsEvidenceRefBridge evidenceBridge;

    public NodalOsScheduledReadOnlyRunValidator()
        : this(new NodalOsRedactionService())
    {
    }

    public NodalOsScheduledReadOnlyRunValidator(NodalOsRedactionService redaction)
        : this(redaction, new NodalOsEvidenceRefBridge(redaction))
    {
    }

    public NodalOsScheduledReadOnlyRunValidator(
        NodalOsRedactionService redaction,
        NodalOsEvidenceRefBridge evidenceBridge)
    {
        this.redaction = redaction;
        this.evidenceBridge = evidenceBridge;
    }

    public NodalOsScheduledReadOnlyValidationResult ValidateSchedule(
        NodalOsScheduledReadOnlySchedule schedule)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, schedule.ScheduleId, "ScheduleId is required.");
        AddRequired(errors, schedule.HumanOwner, "HumanOwner is required.");
        if (schedule.CreatedAt == default)
            errors.Add("CreatedAt is required.");

        ValidateReadOnlyInvariant(
            schedule.ReadOnly,
            schedule.RuntimeExecutionAllowed,
            schedule.RuntimeExecutionDeferred,
            schedule.RequiresGlobalPolicyEvaluation,
            errors);

        if (!schedule.RequiresEvidenceRedaction)
            errors.Add("Scheduled read-only schedules must require evidence redaction.");

        if (schedule.Status != NodalOsScheduledReadOnlyScheduleStatus.Draft &&
            string.IsNullOrWhiteSpace(schedule.MissionId) &&
            string.IsNullOrWhiteSpace(schedule.TaskId) &&
            string.IsNullOrWhiteSpace(schedule.RecipeId) &&
            string.IsNullOrWhiteSpace(schedule.SkillId) &&
            string.IsNullOrWhiteSpace(schedule.PackageId))
        {
            errors.Add("Non-draft schedules require at least one mission, task, recipe, skill, or package reference.");
        }

        if (schedule.Status is NodalOsScheduledReadOnlyScheduleStatus.Blocked or
            NodalOsScheduledReadOnlyScheduleStatus.Cancelled)
        {
            errors.Add("Blocked and Cancelled schedules cannot pass schedule policy.");
        }

        if (schedule.Status == NodalOsScheduledReadOnlyScheduleStatus.ScheduledReadOnlyFuture)
            warnings.Add("ScheduledReadOnlyFuture is a future contract state only; no scheduler is implemented.");

        if (schedule.FrequencyKind != NodalOsScheduledReadOnlyFrequencyKind.ManualOnly)
            warnings.Add("Future frequency kinds are contract metadata only and do not implement scheduler behavior.");

        ValidateScheduleForbiddenActions(schedule, errors);

        if (ContainsSensitiveContent(ScheduleValues(schedule)))
            errors.Add("Scheduled read-only schedule contains sensitive or secret-like content.");

        ValidateEvidenceRefs(schedule.EvidenceRefs, errors, warnings);

        warnings.Add("Schedule visible or valid does not mean executable; scheduled read-only contracts cannot grant runtime permission.");

        return Result(errors, warnings);
    }

    public NodalOsScheduledReadOnlyValidationResult ValidateRunRequest(
        NodalOsScheduledReadOnlyRunRequest request)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, request.RequestId, "RequestId is required.");
        AddRequired(errors, request.ScheduleId, "ScheduleId is required.");
        if (request.CreatedAt == default)
            errors.Add("CreatedAt is required.");

        if (!request.ManualTriggerRequired)
            errors.Add("Scheduled read-only run requests require ManualTriggerRequired=true in V1.");

        ValidateReadOnlyInvariant(
            request.ReadOnly,
            request.RuntimeExecutionAllowed,
            request.RuntimeExecutionDeferred,
            request.RequiresGlobalPolicyEvaluation,
            errors);

        ValidateEvidenceRefs(request.EvidenceRefs, errors, warnings);

        warnings.Add("Run request ready or healthy does not mean executable; V1 remains manual-trigger contract metadata only.");

        return Result(errors, warnings);
    }

    public NodalOsScheduledReadOnlyValidationResult ValidatePreview(
        NodalOsScheduledReadOnlyPreview preview)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, preview.PreviewId, "PreviewId is required.");
        AddRequired(errors, preview.ScheduleId, "ScheduleId is required.");
        if (preview.CreatedAt == default)
            errors.Add("CreatedAt is required.");

        if (!preview.DryRunOnly)
            errors.Add("Scheduled read-only previews must be dry-run only.");

        if (preview.Executed)
            errors.Add("Scheduled read-only previews must report Executed=false.");

        if (!preview.RuntimeExecutionDeferred)
            errors.Add("Scheduled read-only previews must defer runtime execution.");

        foreach (var operation in preview.PlannedReadOnlyOperations)
        {
            if (ContainsForbiddenAction(operation))
                errors.Add($"Scheduled read-only preview operation is forbidden: {SafeOperationLabel(operation)}.");
        }

        if (ContainsSensitiveContent(preview.Warnings))
            errors.Add("Scheduled read-only preview warnings contain sensitive or secret-like content.");

        ValidateEvidenceRefs(preview.EvidenceRefs, errors, warnings);
        warnings.Add("Dry-run preview is planning metadata only and cannot execute browser, desktop, worker, recipe, skill, or step actions.");

        return Result(errors, warnings);
    }

    public NodalOsScheduledReadOnlyValidationResult ValidateReadOnlyInvariant(
        NodalOsScheduledReadOnlySchedule schedule)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        ValidateReadOnlyInvariant(
            schedule.ReadOnly,
            schedule.RuntimeExecutionAllowed,
            schedule.RuntimeExecutionDeferred,
            schedule.RequiresGlobalPolicyEvaluation,
            errors);

        return Result(errors, warnings);
    }

    public NodalOsScheduledReadOnlyValidationResult ValidateEvidenceRefs(
        IReadOnlyList<NodalOsEvidenceBridgeRef> evidenceRefs)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        ValidateEvidenceRefs(evidenceRefs, errors, warnings);

        return Result(errors, warnings);
    }

    private void ValidateEvidenceRefs(
        IReadOnlyList<NodalOsEvidenceBridgeRef> evidenceRefs,
        List<string> errors,
        List<string> warnings)
    {
        foreach (var evidenceRef in evidenceRefs)
        {
            var result = evidenceBridge.ValidateBridgeRef(evidenceRef);
            errors.AddRange(result.Errors.Select(error => $"Scheduled read-only evidence ref {SafeEvidenceId(evidenceRef)}: {error}"));
            warnings.AddRange(result.Warnings.Select(warning => $"Scheduled read-only evidence ref {SafeEvidenceId(evidenceRef)}: {warning}"));

            if (redaction.ContainsSensitiveContent(evidenceRef.EvidenceId) ||
                redaction.ContainsSensitiveContent(evidenceRef.Kind) ||
                redaction.ContainsSensitiveContent(evidenceRef.Ref) ||
                redaction.ContainsSensitiveContent(evidenceRef.Hash) ||
                redaction.ContainsSensitiveContent(evidenceRef.LedgerRef) ||
                redaction.ContainsSensitiveContent(evidenceRef.Provenance))
            {
                errors.Add($"Scheduled read-only evidence ref {SafeEvidenceId(evidenceRef)} contains sensitive or secret-like content.");
            }
        }
    }

    private static void ValidateReadOnlyInvariant(
        bool readOnly,
        bool runtimeExecutionAllowed,
        bool runtimeExecutionDeferred,
        bool requiresGlobalPolicyEvaluation,
        List<string> errors)
    {
        if (!readOnly)
            errors.Add("Scheduled read-only contracts require ReadOnly=true.");

        if (runtimeExecutionAllowed)
            errors.Add("Scheduled read-only contracts cannot grant runtime execution.");

        if (!runtimeExecutionDeferred)
            errors.Add("Scheduled read-only contracts must defer runtime execution.");

        if (!requiresGlobalPolicyEvaluation)
            errors.Add("Scheduled read-only contracts must require global policy evaluation.");
    }

    private bool ContainsSensitiveContent(IEnumerable<string?> values) =>
        values.Any(value => !string.IsNullOrWhiteSpace(value) && redaction.ContainsSensitiveContent(value));

    private static void ValidateScheduleForbiddenActions(
        NodalOsScheduledReadOnlySchedule schedule,
        List<string> errors)
    {
        foreach (var target in schedule.AllowedTargets)
        {
            if (ContainsForbiddenAction(target))
                errors.Add("Scheduled read-only schedule allowed target contains a forbidden action marker.");
        }

        if (ContainsForbiddenAction(schedule.Summary))
            errors.Add("Scheduled read-only schedule summary contains a forbidden action marker.");
    }

    private static bool ContainsForbiddenAction(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return ForbiddenActionMarkers.Any(marker =>
            value.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string?> ScheduleValues(NodalOsScheduledReadOnlySchedule schedule)
    {
        yield return schedule.ScheduleId;
        yield return schedule.MissionId;
        yield return schedule.TaskId;
        yield return schedule.RecipeId;
        yield return schedule.SkillId;
        yield return schedule.PackageId;
        yield return schedule.HumanOwner;
        yield return schedule.Summary;

        foreach (var target in schedule.AllowedTargets)
            yield return target;

        foreach (var requirement in schedule.EvidenceRequirements)
            yield return requirement;
    }

    private static NodalOsScheduledReadOnlyValidationResult Result(
        IReadOnlyList<string> errors,
        IReadOnlyList<string> warnings) =>
        new()
        {
            IsValid = errors.Count == 0,
            CanPassSchedulePolicy = errors.Count == 0,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            Errors = errors.Distinct(StringComparer.Ordinal).ToArray(),
            Warnings = warnings.Distinct(StringComparer.Ordinal).ToArray()
        };

    private static void AddRequired(List<string> errors, string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add(message);
    }

    private static string SafeEvidenceId(NodalOsEvidenceBridgeRef evidenceRef) =>
        string.IsNullOrWhiteSpace(evidenceRef.EvidenceId) ? "<missing>" : evidenceRef.EvidenceId;

    private static string SafeOperationLabel(string? operation) =>
        string.IsNullOrWhiteSpace(operation) ? "<missing>" : operation.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "<operation>";
}

public sealed class NodalOsScheduledReadOnlyRunJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly NodalOsRedactionService redaction;

    public NodalOsScheduledReadOnlyRunJsonSerializer()
        : this(new NodalOsRedactionService())
    {
    }

    public NodalOsScheduledReadOnlyRunJsonSerializer(NodalOsRedactionService redaction) =>
        this.redaction = redaction;

    public string SerializeSchedule(NodalOsScheduledReadOnlySchedule schedule) =>
        JsonSerializer.Serialize(SanitizeSchedule(schedule), Options);

    public NodalOsScheduledReadOnlySchedule DeserializeSchedule(string json) =>
        JsonSerializer.Deserialize<NodalOsScheduledReadOnlySchedule>(json, Options) ??
            throw new InvalidOperationException("Scheduled read-only schedule JSON did not deserialize.");

    public string SerializeRunRequest(NodalOsScheduledReadOnlyRunRequest request) =>
        JsonSerializer.Serialize(SanitizeRunRequest(request), Options);

    public NodalOsScheduledReadOnlyRunRequest DeserializeRunRequest(string json) =>
        JsonSerializer.Deserialize<NodalOsScheduledReadOnlyRunRequest>(json, Options) ??
            throw new InvalidOperationException("Scheduled read-only run request JSON did not deserialize.");

    public string SerializePreview(NodalOsScheduledReadOnlyPreview preview) =>
        JsonSerializer.Serialize(SanitizePreview(preview), Options);

    public NodalOsScheduledReadOnlyPreview DeserializePreview(string json) =>
        JsonSerializer.Deserialize<NodalOsScheduledReadOnlyPreview>(json, Options) ??
            throw new InvalidOperationException("Scheduled read-only preview JSON did not deserialize.");

    private NodalOsScheduledReadOnlySchedule SanitizeSchedule(NodalOsScheduledReadOnlySchedule schedule) =>
        schedule with
        {
            ScheduleId = Redact(schedule.ScheduleId),
            MissionId = RedactNullable(schedule.MissionId),
            TaskId = RedactNullable(schedule.TaskId),
            RecipeId = RedactNullable(schedule.RecipeId),
            SkillId = RedactNullable(schedule.SkillId),
            PackageId = RedactNullable(schedule.PackageId),
            HumanOwner = Redact(schedule.HumanOwner),
            AllowedTargets = schedule.AllowedTargets.Select(Redact).ToArray(),
            EvidenceRequirements = schedule.EvidenceRequirements.Select(Redact).ToArray(),
            EvidenceRefs = schedule.EvidenceRefs.Select(SanitizeEvidenceRef).ToArray(),
            Summary = RedactNullable(schedule.Summary)
        };

    private NodalOsScheduledReadOnlyRunRequest SanitizeRunRequest(NodalOsScheduledReadOnlyRunRequest request) =>
        request with
        {
            RequestId = Redact(request.RequestId),
            ScheduleId = Redact(request.ScheduleId),
            EvidenceRefs = request.EvidenceRefs.Select(SanitizeEvidenceRef).ToArray()
        };

    private NodalOsScheduledReadOnlyPreview SanitizePreview(NodalOsScheduledReadOnlyPreview preview) =>
        preview with
        {
            PreviewId = Redact(preview.PreviewId),
            ScheduleId = Redact(preview.ScheduleId),
            PlannedReadOnlyOperations = preview.PlannedReadOnlyOperations.Select(Redact).ToArray(),
            EvidenceRefs = preview.EvidenceRefs.Select(SanitizeEvidenceRef).ToArray(),
            Warnings = preview.Warnings.Select(Redact).ToArray()
        };

    private NodalOsEvidenceBridgeRef SanitizeEvidenceRef(NodalOsEvidenceBridgeRef evidenceRef) =>
        evidenceRef with
        {
            EvidenceId = Redact(evidenceRef.EvidenceId),
            Kind = Redact(evidenceRef.Kind),
            Ref = RedactNullable(evidenceRef.Ref),
            Hash = RedactNullable(evidenceRef.Hash),
            LedgerRef = RedactNullable(evidenceRef.LedgerRef),
            Provenance = RedactNullable(evidenceRef.Provenance)
        };

    private string Redact(string value) => redaction.RedactValue(value).Value;

    private string? RedactNullable(string? value) => value is null ? null : Redact(value);
}

public static class NodalOsScheduledReadOnlyRunFixtures
{
    public static readonly DateTimeOffset FixedTimestamp = new(2026, 6, 19, 0, 0, 0, TimeSpan.Zero);

    public static NodalOsScheduledReadOnlySchedule DraftSchedule() =>
        BaseSchedule() with
        {
            Status = NodalOsScheduledReadOnlyScheduleStatus.Draft,
            MissionId = null,
            TaskId = null,
            RecipeId = null,
            SkillId = null,
            PackageId = null,
            Summary = "Draft schedule contract only."
        };

    public static NodalOsScheduledReadOnlySchedule ValidManualOnlySchedule() =>
        BaseSchedule();

    public static NodalOsScheduledReadOnlyPreview ValidDryRunPreview() =>
        new()
        {
            PreviewId = "scheduled-readonly-preview-001",
            ScheduleId = "scheduled-readonly-001",
            DryRunOnly = true,
            Executed = false,
            RuntimeExecutionDeferred = true,
            PlannedReadOnlyOperations =
            [
                "validate manifest",
                "query registry",
                "prepare dry-run",
                "read observation",
                "collect evidence",
                "produce RunReport",
                "produce ProgressReport"
            ],
            EvidenceRefs = [ValidEvidenceRef()],
            Warnings = ["Preview is dry-run metadata only."],
            CreatedAt = FixedTimestamp
        };

    public static NodalOsScheduledReadOnlySchedule InvalidMutableActionSchedule() =>
        BaseSchedule() with
        {
            Summary = "Invalid schedule tries to click submit and mutate external state.",
            AllowedTargets = ["https://lab.nodalos.com.ar?action=submit"]
        };

    public static NodalOsScheduledReadOnlyRunRequest RequestFromSchedule(
        NodalOsScheduledReadOnlySchedule? schedule = null)
    {
        var effectiveSchedule = schedule ?? ValidManualOnlySchedule();
        return new NodalOsScheduledReadOnlyRunRequest
        {
            RequestId = "scheduled-readonly-request-001",
            ScheduleId = effectiveSchedule.ScheduleId,
            ManualTriggerRequired = true,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            ReadOnly = true,
            RequiresGlobalPolicyEvaluation = true,
            EvidenceRefs = effectiveSchedule.EvidenceRefs,
            CreatedAt = FixedTimestamp
        };
    }

    public static NodalOsEvidenceBridgeRef ValidEvidenceRef() =>
        new()
        {
            EvidenceId = "evidence-scheduled-readonly-001",
            Kind = "scheduled-readonly-contract-fixture",
            Ref = "scheduled-readonly-contract",
            Hash = "sha256:scheduled-readonly-fixture",
            SourceKind = NodalOsEvidenceBridgeSourceKind.AgentOperation,
            UseKind = NodalOsEvidenceBridgeUseKind.AuditTrail,
            Authority = NodalOsEvidenceBridgeAuthority.NoAuthority,
            Sensitivity = NodalOsEvidenceSensitivity.NonSensitive,
            RedactionState = NodalOsEvidenceRedactionState.NotRequired,
            LedgerRef = null,
            Provenance = "internal-fixture",
            CreatedAt = FixedTimestamp
        };

    private static NodalOsScheduledReadOnlySchedule BaseSchedule() =>
        new()
        {
            ScheduleId = "scheduled-readonly-001",
            MissionId = "mission-internal-001",
            TaskId = "task-internal-001",
            RecipeId = "recipe-readonly-001",
            SkillId = "skill-readonly-001",
            PackageId = "pkg-internal-readonly-001",
            HumanOwner = "internal-operator",
            Status = NodalOsScheduledReadOnlyScheduleStatus.AwaitingManualTrigger,
            FrequencyKind = NodalOsScheduledReadOnlyFrequencyKind.ManualOnly,
            ReadOnly = true,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            RequiresEvidenceRedaction = true,
            AllowedTargets = ["https://lab.nodalos.com.ar/read-only"],
            EvidenceRequirements = ["policy-review", "evidence-redaction", "RunReport", "ProgressReport"],
            EvidenceRefs = [ValidEvidenceRef()],
            Summary = "Scheduled read-only contract fixture. No scheduler or execution is implemented.",
            CreatedAt = FixedTimestamp
        };
}
