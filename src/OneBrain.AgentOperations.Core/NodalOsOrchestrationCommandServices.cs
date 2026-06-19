using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NodalOsOrchestrationCommandValidator
{
    private readonly NodalOsRedactionService redaction;
    private readonly NodalOsEvidenceRefBridge evidenceBridge;

    public NodalOsOrchestrationCommandValidator()
        : this(new NodalOsRedactionService())
    {
    }

    public NodalOsOrchestrationCommandValidator(NodalOsRedactionService redaction)
        : this(redaction, new NodalOsEvidenceRefBridge(redaction))
    {
    }

    public NodalOsOrchestrationCommandValidator(
        NodalOsRedactionService redaction,
        NodalOsEvidenceRefBridge evidenceBridge)
    {
        this.redaction = redaction;
        this.evidenceBridge = evidenceBridge;
    }

    public NodalOsOrchestrationCommandValidationResult ValidateCommand(
        NodalOsOrchestrationCommandEnvelope command)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, command.CommandId, "CommandId is required.");
        if (command.CreatedAt == default)
            errors.Add("CreatedAt is required.");

        ValidateNoRuntimeExecution(
            command.RuntimeExecutionAllowed,
            command.RuntimeExecutionDeferred,
            command.RequiresGlobalPolicyEvaluation,
            errors);

        if (command.RiskLevel is NodalOsOrchestrationCommandRiskLevel.High or NodalOsOrchestrationCommandRiskLevel.Critical &&
            !command.RequiresHumanApproval)
        {
            errors.Add("High and Critical orchestration commands require human approval.");
        }

        ValidateCommandShape(command, errors, warnings);
        ValidateEvidenceRefs(command.EvidenceRefs, errors, warnings);

        if (ContainsSensitiveContent(CommandValues(command)))
            errors.Add("Orchestration command contains sensitive or secret-like content.");

        warnings.Add("Orchestration command contracts are internal metadata only and do not grant runtime permission.");

        return Result(errors, warnings, canPassCommandPolicy: errors.Count == 0);
    }

    public NodalOsOrchestrationCommandValidationResult ValidateResult(
        NodalOsOrchestrationCommandResult result)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, result.ResultId, "ResultId is required.");
        AddRequired(errors, result.CommandId, "CommandId is required.");
        if (result.CreatedAt == default)
            errors.Add("CreatedAt is required.");

        if (result.Executed)
            errors.Add("Orchestration command results must report Executed=false in V1.");

        if (!result.RuntimeExecutionDeferred)
            errors.Add("Orchestration command results must defer runtime execution in V1.");

        if (result.Accepted)
            warnings.Add("Accepted means command contract accepted only; it does not mean executed.");

        if (result.State == NodalOsOrchestrationState.Completed)
            warnings.Add("Completed means contract handling completed only; it does not mean runtime execution completed.");

        if (result.State is NodalOsOrchestrationState.RunningFuture or NodalOsOrchestrationState.PausedFuture)
            warnings.Add("RunningFuture and PausedFuture are reserved conceptual states and do not represent V1 runtime.");

        ValidateEvidenceRefs(result.EvidenceRefs, errors, warnings);

        if (ContainsSensitiveContent(ResultValues(result)))
            errors.Add("Orchestration command result contains sensitive or secret-like content.");

        return Result(errors, warnings, canPassCommandPolicy: errors.Count == 0);
    }

    public NodalOsOrchestrationCommandValidationResult ValidateNoRuntimeExecution(
        NodalOsOrchestrationCommandEnvelope command)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        ValidateNoRuntimeExecution(
            command.RuntimeExecutionAllowed,
            command.RuntimeExecutionDeferred,
            command.RequiresGlobalPolicyEvaluation,
            errors);

        return Result(errors, warnings, canPassCommandPolicy: errors.Count == 0);
    }

    public NodalOsOrchestrationCommandValidationResult ValidateEvidenceRefs(
        IReadOnlyList<NodalOsEvidenceBridgeRef> evidenceRefs)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        ValidateEvidenceRefs(evidenceRefs, errors, warnings);

        return Result(errors, warnings, canPassCommandPolicy: errors.Count == 0);
    }

    private void ValidateCommandShape(
        NodalOsOrchestrationCommandEnvelope command,
        List<string> errors,
        List<string> warnings)
    {
        switch (command.Kind)
        {
            case NodalOsOrchestrationCommandKind.AttachEvidence:
                if (command.EvidenceRefs.Count == 0)
                    errors.Add("AttachEvidence requires evidence refs.");
                break;
            case NodalOsOrchestrationCommandKind.PrepareRun:
                if (string.IsNullOrWhiteSpace(command.MissionId) && string.IsNullOrWhiteSpace(command.TaskId))
                    errors.Add("PrepareRun requires MissionId or TaskId.");
                if (string.IsNullOrWhiteSpace(command.RecipeId) && string.IsNullOrWhiteSpace(command.SkillId))
                    errors.Add("PrepareRun requires RecipeId or SkillId.");
                break;
            case NodalOsOrchestrationCommandKind.PrepareWorkerRequest:
                if (string.IsNullOrWhiteSpace(command.WorkerId))
                    errors.Add("PrepareWorkerRequest requires WorkerId.");
                if (string.IsNullOrWhiteSpace(command.SkillId) && string.IsNullOrWhiteSpace(command.PackageId))
                    errors.Add("PrepareWorkerRequest requires SkillId or PackageId.");
                break;
            case NodalOsOrchestrationCommandKind.QuerySkillRegistry:
                warnings.Add("QuerySkillRegistry is catalog lookup only and cannot grant runtime permission.");
                break;
            case NodalOsOrchestrationCommandKind.PauseRun:
            case NodalOsOrchestrationCommandKind.ResumeRun:
            case NodalOsOrchestrationCommandKind.CancelRun:
                warnings.Add("Pause, Resume, and Cancel are contract-only in V1; no runtime state transition engine exists.");
                break;
        }
    }

    private void ValidateEvidenceRefs(
        IReadOnlyList<NodalOsEvidenceBridgeRef> evidenceRefs,
        List<string> errors,
        List<string> warnings)
    {
        foreach (var evidenceRef in evidenceRefs)
        {
            var result = evidenceBridge.ValidateBridgeRef(evidenceRef);
            errors.AddRange(result.Errors.Select(error => $"Orchestration evidence ref {SafeEvidenceId(evidenceRef)}: {error}"));
            warnings.AddRange(result.Warnings.Select(warning => $"Orchestration evidence ref {SafeEvidenceId(evidenceRef)}: {warning}"));

            if (redaction.ContainsSensitiveContent(evidenceRef.EvidenceId) ||
                redaction.ContainsSensitiveContent(evidenceRef.Kind) ||
                redaction.ContainsSensitiveContent(evidenceRef.Ref) ||
                redaction.ContainsSensitiveContent(evidenceRef.Hash) ||
                redaction.ContainsSensitiveContent(evidenceRef.LedgerRef) ||
                redaction.ContainsSensitiveContent(evidenceRef.Provenance))
            {
                errors.Add($"Orchestration evidence ref {SafeEvidenceId(evidenceRef)} contains sensitive or secret-like content.");
            }
        }
    }

    private static void ValidateNoRuntimeExecution(
        bool runtimeExecutionAllowed,
        bool runtimeExecutionDeferred,
        bool requiresGlobalPolicyEvaluation,
        List<string> errors)
    {
        if (runtimeExecutionAllowed)
            errors.Add("Orchestration command contracts cannot grant runtime execution.");

        if (!runtimeExecutionDeferred)
            errors.Add("Orchestration command contracts must defer runtime execution in V1.");

        if (!requiresGlobalPolicyEvaluation)
            errors.Add("Orchestration command contracts must require global policy evaluation.");
    }

    private bool ContainsSensitiveContent(IEnumerable<string?> values) =>
        values.Any(value => !string.IsNullOrWhiteSpace(value) && redaction.ContainsSensitiveContent(value));

    private static IEnumerable<string?> CommandValues(NodalOsOrchestrationCommandEnvelope command)
    {
        yield return command.CommandId;
        yield return command.MissionId;
        yield return command.TaskId;
        yield return command.RunId;
        yield return command.RecipeId;
        yield return command.PackageId;
        yield return command.SkillId;
        yield return command.WorkerId;
        yield return command.Summary;

        foreach (var value in command.EvidenceRequirements)
            yield return value;
    }

    private static IEnumerable<string?> ResultValues(NodalOsOrchestrationCommandResult result)
    {
        yield return result.ResultId;
        yield return result.CommandId;

        foreach (var value in result.Errors)
            yield return value;

        foreach (var value in result.Warnings)
            yield return value;
    }

    private static NodalOsOrchestrationCommandValidationResult Result(
        IReadOnlyList<string> errors,
        IReadOnlyList<string> warnings,
        bool canPassCommandPolicy) =>
        new()
        {
            IsValid = errors.Count == 0,
            CanPassCommandPolicy = canPassCommandPolicy,
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

public sealed class NodalOsOrchestrationCommandJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly NodalOsRedactionService redaction;

    public NodalOsOrchestrationCommandJsonSerializer()
        : this(new NodalOsRedactionService())
    {
    }

    public NodalOsOrchestrationCommandJsonSerializer(NodalOsRedactionService redaction) =>
        this.redaction = redaction;

    public string SerializeCommand(NodalOsOrchestrationCommandEnvelope command) =>
        JsonSerializer.Serialize(SanitizeCommand(command), Options);

    public NodalOsOrchestrationCommandEnvelope DeserializeCommand(string json) =>
        JsonSerializer.Deserialize<NodalOsOrchestrationCommandEnvelope>(json, Options) ??
            throw new InvalidOperationException("Orchestration command JSON did not deserialize.");

    public string SerializeResult(NodalOsOrchestrationCommandResult result) =>
        JsonSerializer.Serialize(SanitizeResult(result), Options);

    public NodalOsOrchestrationCommandResult DeserializeResult(string json) =>
        JsonSerializer.Deserialize<NodalOsOrchestrationCommandResult>(json, Options) ??
            throw new InvalidOperationException("Orchestration command result JSON did not deserialize.");

    private NodalOsOrchestrationCommandEnvelope SanitizeCommand(NodalOsOrchestrationCommandEnvelope command) =>
        command with
        {
            CommandId = Redact(command.CommandId),
            MissionId = RedactNullable(command.MissionId),
            TaskId = RedactNullable(command.TaskId),
            RunId = RedactNullable(command.RunId),
            RecipeId = RedactNullable(command.RecipeId),
            PackageId = RedactNullable(command.PackageId),
            SkillId = RedactNullable(command.SkillId),
            WorkerId = RedactNullable(command.WorkerId),
            Summary = RedactNullable(command.Summary),
            EvidenceRequirements = command.EvidenceRequirements.Select(Redact).ToArray(),
            EvidenceRefs = command.EvidenceRefs.Select(SanitizeEvidenceRef).ToArray()
        };

    private NodalOsOrchestrationCommandResult SanitizeResult(NodalOsOrchestrationCommandResult result) =>
        result with
        {
            ResultId = Redact(result.ResultId),
            CommandId = Redact(result.CommandId),
            EvidenceRefs = result.EvidenceRefs.Select(SanitizeEvidenceRef).ToArray(),
            Errors = result.Errors.Select(Redact).ToArray(),
            Warnings = result.Warnings.Select(Redact).ToArray()
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

public static class NodalOsOrchestrationCommandFixtures
{
    public static readonly DateTimeOffset FixedTimestamp = new(2026, 6, 19, 0, 0, 0, TimeSpan.Zero);

    public static NodalOsOrchestrationCommandEnvelope CreateMissionCommand() =>
        BaseCommand(NodalOsOrchestrationCommandKind.CreateMission) with
        {
            MissionId = "mission-internal-001",
            Summary = "Create mission contract only. No runtime action is implemented."
        };

    public static NodalOsOrchestrationCommandEnvelope PrepareRunCommand() =>
        BaseCommand(NodalOsOrchestrationCommandKind.PrepareRun) with
        {
            MissionId = "mission-internal-001",
            TaskId = "task-internal-001",
            RecipeId = "recipe-readonly-001",
            SkillId = "skill-readonly-001",
            EvidenceRequirements = ["policy-review", "verification-before-done"]
        };

    public static NodalOsOrchestrationCommandEnvelope ValidateRecipeManifestCommand() =>
        BaseCommand(NodalOsOrchestrationCommandKind.ValidateRecipeManifest) with
        {
            RecipeId = "recipe-readonly-001",
            Summary = "Validate manifest policy only."
        };

    public static NodalOsOrchestrationCommandEnvelope QuerySkillRegistryCommand() =>
        BaseCommand(NodalOsOrchestrationCommandKind.QuerySkillRegistry) with
        {
            PackageId = "pkg-internal-readonly-001",
            SkillId = "skill-readonly-001",
            Summary = "Catalog lookup only. Visible does not mean executable."
        };

    public static NodalOsOrchestrationCommandEnvelope PrepareWorkerRequestCommand() =>
        BaseCommand(NodalOsOrchestrationCommandKind.PrepareWorkerRequest) with
        {
            WorkerId = "worker-internal-readonly-001",
            PackageId = "pkg-internal-readonly-001",
            SkillId = "skill-readonly-001",
            EvidenceRequirements = ["worker-boundary-review"]
        };

    public static NodalOsOrchestrationCommandEnvelope AttachEvidenceCommand() =>
        BaseCommand(NodalOsOrchestrationCommandKind.AttachEvidence) with
        {
            EvidenceRefs = [ValidEvidenceRef()],
            EvidenceRequirements = ["evidence-ref-bridge"],
            Summary = "Attach no-authority evidence to orchestration metadata."
        };

    public static NodalOsOrchestrationCommandEnvelope RequestHumanDecisionCommand() =>
        BaseCommand(NodalOsOrchestrationCommandKind.RequestHumanDecision) with
        {
            RiskLevel = NodalOsOrchestrationCommandRiskLevel.High,
            RequiresHumanApproval = true,
            Summary = "Request human decision. UI may display, but policy remains authoritative."
        };

    public static NodalOsOrchestrationCommandEnvelope CancelRunCommand() =>
        BaseCommand(NodalOsOrchestrationCommandKind.CancelRun) with
        {
            RunId = "run-internal-001",
            Summary = "Cancel contract metadata only. No runtime state transition engine exists."
        };

    public static NodalOsOrchestrationCommandResult FutureRunningResultInvalidForExecution() =>
        BaseResult(NodalOsOrchestrationCommandKind.GetRunStatus) with
        {
            State = NodalOsOrchestrationState.RunningFuture,
            Executed = true
        };

    public static NodalOsOrchestrationCommandResult CompletedContractResult() =>
        BaseResult(NodalOsOrchestrationCommandKind.PrepareRun) with
        {
            Accepted = true,
            State = NodalOsOrchestrationState.Completed,
            EvidenceRefs = [ValidEvidenceRef()],
            Warnings = ["Completed means contract handling only."]
        };

    public static NodalOsEvidenceBridgeRef ValidEvidenceRef() =>
        new()
        {
            EvidenceId = "evidence-orchestration-command-001",
            Kind = "contract-fixture",
            Ref = "orchestration-command",
            Hash = "sha256:fixture",
            SourceKind = NodalOsEvidenceBridgeSourceKind.AgentOperation,
            UseKind = NodalOsEvidenceBridgeUseKind.AuditTrail,
            Authority = NodalOsEvidenceBridgeAuthority.NoAuthority,
            Sensitivity = NodalOsEvidenceSensitivity.NonSensitive,
            RedactionState = NodalOsEvidenceRedactionState.NotRequired,
            LedgerRef = null,
            Provenance = "internal-fixture",
            CreatedAt = FixedTimestamp
        };

    private static NodalOsOrchestrationCommandEnvelope BaseCommand(NodalOsOrchestrationCommandKind kind) =>
        new()
        {
            CommandId = $"orchestration-command-{kind.ToString().ToLowerInvariant()}-001",
            Kind = kind,
            RiskLevel = NodalOsOrchestrationCommandRiskLevel.Low,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            RequiresHumanApproval = false,
            EvidenceRefs = [],
            EvidenceRequirements = [],
            Summary = "Internal orchestration command contract fixture.",
            CreatedAt = FixedTimestamp
        };

    private static NodalOsOrchestrationCommandResult BaseResult(NodalOsOrchestrationCommandKind kind) =>
        new()
        {
            ResultId = $"orchestration-result-{kind.ToString().ToLowerInvariant()}-001",
            CommandId = $"orchestration-command-{kind.ToString().ToLowerInvariant()}-001",
            Kind = kind,
            Accepted = false,
            Executed = false,
            RuntimeExecutionDeferred = true,
            State = NodalOsOrchestrationState.Prepared,
            EvidenceRefs = [],
            FailureKinds = [],
            Errors = [],
            Warnings = [],
            CreatedAt = FixedTimestamp
        };
}
