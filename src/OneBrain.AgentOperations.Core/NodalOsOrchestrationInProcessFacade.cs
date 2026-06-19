using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NodalOsOrchestrationInProcessFacade
{
    private readonly NodalOsOrchestrationCommandValidator validator;
    private readonly NodalOsEvidenceRefBridge evidenceBridge;
    private readonly NodalOsRedactionService redaction;

    public NodalOsOrchestrationInProcessFacade()
        : this(new NodalOsOrchestrationCommandValidator(), new NodalOsEvidenceRefBridge(), new NodalOsRedactionService())
    {
    }

    public NodalOsOrchestrationInProcessFacade(
        NodalOsOrchestrationCommandValidator validator,
        NodalOsEvidenceRefBridge evidenceBridge,
        NodalOsRedactionService redaction)
    {
        this.validator = validator;
        this.evidenceBridge = evidenceBridge;
        this.redaction = redaction;
    }

    public NodalOsOrchestrationCommandResult Dispatch(NodalOsOrchestrationCommandEnvelope command)
    {
        var commandValidation = validator.ValidateCommand(command);

        if (!commandValidation.IsValid)
        {
            var errors = commandValidation.Errors.ToList();
            var validationWarnings = commandValidation.Warnings.ToList();

            SanitizeList(errors);
            SanitizeList(validationWarnings);

            return InvalidResult(command, errors, validationWarnings);
        }

        var evidenceErrors = new List<string>();
        var evidenceWarnings = new List<string>();

        foreach (var evRef in command.EvidenceRefs)
        {
            var bridgeResult = evidenceBridge.ValidateBridgeRef(evRef);
            if (!bridgeResult.Accepted)
                evidenceErrors.AddRange(bridgeResult.Errors);
            evidenceWarnings.AddRange(bridgeResult.Warnings);
        }

        if (evidenceErrors.Count > 0)
        {
            SanitizeList(evidenceErrors);
            SanitizeList(evidenceWarnings);

            return InvalidResult(command, evidenceErrors, evidenceWarnings);
        }

        var state = DetermineContractState(command);
        var warnings = new List<string>(commandValidation.Warnings);
        warnings.AddRange(evidenceWarnings);

        if (!string.IsNullOrWhiteSpace(command.Summary) && redaction.ContainsSensitiveContent(command.Summary))
            warnings.Add("Command summary contains sensitive content and requires redaction before display.");

        if (command.Kind is NodalOsOrchestrationCommandKind.PauseRun or
                           NodalOsOrchestrationCommandKind.ResumeRun or
                           NodalOsOrchestrationCommandKind.CancelRun)
            warnings.Add("Pause, Resume, and Cancel are contract-only in V1; no runtime state transition engine exists.");

        if (command.Kind == NodalOsOrchestrationCommandKind.QuerySkillRegistry)
            warnings.Add("QuerySkillRegistry is catalog lookup only and cannot grant runtime permission.");

        if (command.Kind == NodalOsOrchestrationCommandKind.EvaluateVerificationBeforeDone)
            warnings.Add("EvaluateVerificationBeforeDone processes contract state only; it does not close or authorize completion automatically.");

        SanitizeList(warnings);

        return new NodalOsOrchestrationCommandResult
        {
            ResultId = $"facade-{Guid.NewGuid():N}",
            CommandId = command.CommandId,
            Kind = command.Kind,
            Accepted = true,
            Executed = false,
            RuntimeExecutionDeferred = true,
            State = state,
            EvidenceRefs = command.EvidenceRefs,
            FailureKinds = [],
            Errors = [],
            Warnings = warnings,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static NodalOsOrchestrationCommandResult InvalidResult(
        NodalOsOrchestrationCommandEnvelope command,
        IReadOnlyList<string> errors,
        IReadOnlyList<string> warnings) =>
        new()
        {
            ResultId = $"facade-{Guid.NewGuid():N}",
            CommandId = command.CommandId,
            Kind = command.Kind,
            Accepted = false,
            Executed = false,
            RuntimeExecutionDeferred = true,
            State = NodalOsOrchestrationState.Blocked,
            EvidenceRefs = [],
            FailureKinds = [],
            Errors = errors.ToArray(),
            Warnings = warnings.ToArray(),
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static NodalOsOrchestrationState DetermineContractState(NodalOsOrchestrationCommandEnvelope command) =>
        command.Kind switch
        {
            NodalOsOrchestrationCommandKind.CreateMission => NodalOsOrchestrationState.Prepared,
            NodalOsOrchestrationCommandKind.CreateTask => NodalOsOrchestrationState.Prepared,
            NodalOsOrchestrationCommandKind.PrepareRun => NodalOsOrchestrationState.Prepared,
            NodalOsOrchestrationCommandKind.ValidateRecipeManifest => NodalOsOrchestrationState.Completed,
            NodalOsOrchestrationCommandKind.ValidateSkill => NodalOsOrchestrationState.Completed,
            NodalOsOrchestrationCommandKind.RegisterPackageSnapshot => NodalOsOrchestrationState.Completed,
            NodalOsOrchestrationCommandKind.QuerySkillRegistry => NodalOsOrchestrationState.Completed,
            NodalOsOrchestrationCommandKind.PrepareWorkerRequest => NodalOsOrchestrationState.Prepared,
            NodalOsOrchestrationCommandKind.GetRunStatus => NodalOsOrchestrationState.Completed,
            NodalOsOrchestrationCommandKind.PauseRun => NodalOsOrchestrationState.Completed,
            NodalOsOrchestrationCommandKind.ResumeRun => NodalOsOrchestrationState.Completed,
            NodalOsOrchestrationCommandKind.CancelRun => NodalOsOrchestrationState.Completed,
            NodalOsOrchestrationCommandKind.RequestHumanDecision => NodalOsOrchestrationState.AwaitingApproval,
            NodalOsOrchestrationCommandKind.AttachEvidence => NodalOsOrchestrationState.Completed,
            NodalOsOrchestrationCommandKind.GetRunReport => NodalOsOrchestrationState.Completed,
            NodalOsOrchestrationCommandKind.GetProgressReport => NodalOsOrchestrationState.Completed,
            NodalOsOrchestrationCommandKind.EvaluateVerificationBeforeDone => NodalOsOrchestrationState.Completed,
            _ => NodalOsOrchestrationState.Blocked
        };

    private void SanitizeList(List<string> items)
    {
        for (var i = 0; i < items.Count; i++)
        {
            var redacted = redaction.RedactValue(items[i]);
            if (redacted.Value != items[i])
                items[i] = redacted.Value;
        }
    }
}
