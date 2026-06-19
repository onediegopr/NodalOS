using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

public sealed class NodalOsStepLibrary
{
    private readonly IReadOnlyDictionary<NodalOsStepKind, NodalOsStepDefinition> definitions;

    public NodalOsStepLibrary()
    {
        definitions = CreateDefinitions().ToDictionary(d => d.StepKind);
    }

    public NodalOsStepDefinition GetDefinition(NodalOsStepKind kind) => definitions[kind];

    public IReadOnlyList<NodalOsStepDefinition> GetAllDefinitions() =>
        definitions.Values.OrderBy(d => d.StepKind).ToArray();

    public bool TryMapFromRecipeActionKind(NodalOsRecipeActionKind actionKind, out NodalOsStepKind stepKind) =>
        Enum.TryParse(actionKind.ToString(), ignoreCase: false, out stepKind);

    public string MapToRunStepActionKind(NodalOsStepKind stepKind) => stepKind switch
    {
        NodalOsStepKind.DownloadRequest => "download-request",
        NodalOsStepKind.UploadRequest => "upload-request",
        NodalOsStepKind.AskHuman => "ask-human",
        _ => ToKebab(stepKind.ToString())
    };

    private static IReadOnlyList<NodalOsStepDefinition> CreateDefinitions() =>
    [
        Definition(
            NodalOsStepKind.Navigate,
            "Navigate",
            "Navigate to an allowed URL or route under policy.",
            NodalOsStepRiskLevel.Low,
            [NodalOsStepCapabilityKind.Navigation],
            false,
            false,
            false,
            true,
            [NexaFailureKind.NavigationTimeout, NexaFailureKind.PageLoadFailed, NexaFailureKind.NetworkUnavailable, NexaFailureKind.PolicyBlocked],
            ["allowed-domain", "navigation-observation"]),
        Definition(
            NodalOsStepKind.Read,
            "Read",
            "Read page or surface state without mutation.",
            NodalOsStepRiskLevel.Low,
            [NodalOsStepCapabilityKind.ReadOnly],
            true,
            false,
            false,
            true,
            [NexaFailureKind.SelectorNotFound, NexaFailureKind.SelectorAmbiguous, NexaFailureKind.ContentScriptUnreachable, NexaFailureKind.RuntimeDisconnected],
            ["read-observation", "evidence-ref"]),
        Definition(
            NodalOsStepKind.Click,
            "Click",
            "Interact with a non-sensitive target when policy allows it.",
            NodalOsStepRiskLevel.Medium,
            [NodalOsStepCapabilityKind.Interaction],
            false,
            true,
            false,
            true,
            [NexaFailureKind.SelectorNotFound, NexaFailureKind.SelectorAmbiguous, NexaFailureKind.PolicyBlocked, NexaFailureKind.ApprovalRequired, NexaFailureKind.NoProgressDetected],
            ["target-identity", "policy-decision"]),
        Definition(
            NodalOsStepKind.Type,
            "Type",
            "Enter text into a field; sensitive fields require approval or are blocked.",
            NodalOsStepRiskLevel.High,
            [NodalOsStepCapabilityKind.DataEntry],
            false,
            true,
            true,
            true,
            [NexaFailureKind.SensitiveDataRisk, NexaFailureKind.ApprovalRequired, NexaFailureKind.PolicyBlocked],
            ["target-identity", "policy-decision", "approval-if-sensitive"]),
        Definition(
            NodalOsStepKind.Extract,
            "Extract",
            "Extract non-sensitive data from an observed surface.",
            NodalOsStepRiskLevel.Low,
            [NodalOsStepCapabilityKind.ReadOnly, NodalOsStepCapabilityKind.Extraction],
            true,
            false,
            false,
            true,
            [NexaFailureKind.SelectorNotFound, NexaFailureKind.SelectorAmbiguous, NexaFailureKind.SensitiveDataRisk],
            ["extraction-observation", "redaction-status"]),
        Definition(
            NodalOsStepKind.Wait,
            "Wait",
            "Wait for bounded time or state transition.",
            NodalOsStepRiskLevel.None,
            [NodalOsStepCapabilityKind.ControlFlow],
            true,
            false,
            false,
            true,
            [NexaFailureKind.NavigationTimeout, NexaFailureKind.NoProgressDetected],
            ["timeout-bound"]),
        Definition(
            NodalOsStepKind.AskHuman,
            "Ask Human",
            "Request explicit human input or decision.",
            NodalOsStepRiskLevel.Low,
            [NodalOsStepCapabilityKind.HumanInput],
            true,
            false,
            false,
            true,
            [NexaFailureKind.HumanInputRequired],
            ["human-decision-request"]),
        Definition(
            NodalOsStepKind.Stop,
            "Stop",
            "Stop the run or recipe safely.",
            NodalOsStepRiskLevel.None,
            [NodalOsStepCapabilityKind.ControlFlow],
            true,
            false,
            false,
            true,
            [NexaFailureKind.PolicyBlocked],
            ["stop-reason"]),
        Definition(
            NodalOsStepKind.DownloadRequest,
            "Download Request",
            "Request a governed download; V1 never auto-downloads without approval.",
            NodalOsStepRiskLevel.High,
            [NodalOsStepCapabilityKind.FileTransfer],
            false,
            true,
            true,
            true,
            [NexaFailureKind.DownloadBlocked, NexaFailureKind.ApprovalRequired, NexaFailureKind.PolicyBlocked, NexaFailureKind.SensitiveDataRisk],
            ["safe-download-policy", "human-approval"]),
        Definition(
            NodalOsStepKind.UploadRequest,
            "Upload Request",
            "Request a governed upload; V1 never auto-uploads without approval.",
            NodalOsStepRiskLevel.High,
            [NodalOsStepCapabilityKind.FileTransfer],
            false,
            true,
            true,
            true,
            [NexaFailureKind.UploadBlocked, NexaFailureKind.ApprovalRequired, NexaFailureKind.PolicyBlocked, NexaFailureKind.SensitiveDataRisk],
            ["safe-upload-policy", "human-approval"])
    ];

    private static NodalOsStepDefinition Definition(
        NodalOsStepKind kind,
        string name,
        string description,
        NodalOsStepRiskLevel risk,
        IReadOnlyList<NodalOsStepCapabilityKind> capabilities,
        bool readOnly,
        bool approval,
        bool sensitive,
        bool allowed,
        IReadOnlyList<NexaFailureKind> failures,
        IReadOnlyList<string> evidenceRequirements,
        string? blockedReason = null) =>
        new()
        {
            StepKind = kind,
            Name = name,
            Description = description,
            RiskLevel = risk,
            Capabilities = capabilities,
            IsReadOnlyCapable = readOnly,
            RequiresApprovalByDefault = approval,
            IsSensitiveByDefault = sensitive,
            IsAllowedInV1 = allowed,
            BlockedReason = blockedReason,
            PossibleFailureKinds = failures,
            EvidenceRequirements = evidenceRequirements
        };

    private static string ToKebab(string value) =>
        string.Concat(value.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? $"-{char.ToLowerInvariant(c)}" : char.ToLowerInvariant(c).ToString()));
}

public sealed class NodalOsStepLibraryValidator
{
    private readonly NodalOsStepLibrary library;

    public NodalOsStepLibraryValidator(NodalOsStepLibrary? library = null)
    {
        this.library = library ?? new NodalOsStepLibrary();
    }

    public NodalOsStepValidationResult Validate(NodalOsStepValidationContext context)
    {
        var definition = library.GetDefinition(context.StepKind);
        var errors = new List<string>();
        var warnings = new List<string>();
        var failures = new List<NexaFailureKind>(definition.PossibleFailureKinds);
        var requiresApproval = definition.RequiresApprovalByDefault ||
                               context.RequiresFileUpload ||
                               context.RequiresFileDownload ||
                               (context.StepKind == NodalOsStepKind.Type && context.TargetsSensitiveField);

        if (!definition.IsAllowedInV1)
            errors.Add(definition.BlockedReason ?? $"{context.StepKind} is not allowed in Step Library V1.");

        if (context.IsSubmitLike)
            Block(errors, failures, "Submit-like actions are blocked in Step Library V1.", NexaFailureKind.PolicyBlocked, NexaFailureKind.ApprovalRequired);

        if (context.IsLoginRelated)
            Block(errors, failures, "Login-related automation is blocked in Step Library V1.", NexaFailureKind.LoginRequired, NexaFailureKind.HumanInputRequired);

        if (context.IsCaptchaOrTwoFactorRelated)
            Block(errors, failures, "Captcha or two-factor automation is blocked in Step Library V1.", NexaFailureKind.CaptchaDetected, NexaFailureKind.TwoFactorRequired, NexaFailureKind.HumanInputRequired);

        if (context.StepKind == NodalOsStepKind.Type && context.TargetsSensitiveField)
        {
            requiresApproval = true;
            AddDistinct(failures, NexaFailureKind.SensitiveDataRisk, NexaFailureKind.ApprovalRequired);

            if (context.GlobalSensitiveActionsBlocked)
                Block(errors, failures, "Sensitive Type step is blocked by global sensitive action policy.", NexaFailureKind.PolicyBlocked, NexaFailureKind.SensitiveDataRisk);
        }

        if (context.StepKind == NodalOsStepKind.DownloadRequest || context.RequiresFileDownload)
        {
            requiresApproval = true;
            AddDistinct(failures, NexaFailureKind.DownloadBlocked, NexaFailureKind.ApprovalRequired);
        }

        if (context.StepKind == NodalOsStepKind.UploadRequest || context.RequiresFileUpload)
        {
            requiresApproval = true;
            AddDistinct(failures, NexaFailureKind.UploadBlocked, NexaFailureKind.ApprovalRequired);
        }

        if (requiresApproval && !context.HumanApprovalAvailable)
            warnings.Add("Step requires human approval before any future execution path could proceed.");

        return new NodalOsStepValidationResult
        {
            IsValid = errors.Count == 0,
            IsAllowed = errors.Count == 0,
            RequiresApproval = requiresApproval,
            Errors = errors.Distinct(StringComparer.Ordinal).ToArray(),
            Warnings = warnings.Distinct(StringComparer.Ordinal).ToArray(),
            FailureKinds = failures.Distinct().OrderBy(f => f).ToArray()
        };
    }

    public bool ValidateDefinitionCoverage()
    {
        var definitions = library.GetAllDefinitions();
        return Enum.GetValues<NodalOsStepKind>().All(kind =>
            definitions.Any(definition =>
                definition.StepKind == kind &&
                !string.IsNullOrWhiteSpace(definition.Name) &&
                !string.IsNullOrWhiteSpace(definition.Description) &&
                definition.Capabilities.Count > 0 &&
                definition.PossibleFailureKinds.Count > 0 &&
                definition.EvidenceRequirements.Count > 0));
    }

    private static void Block(List<string> errors, List<NexaFailureKind> failures, string error, params NexaFailureKind[] failureKinds)
    {
        errors.Add(error);
        AddDistinct(failures, failureKinds);
    }

    private static void AddDistinct(List<NexaFailureKind> failures, params NexaFailureKind[] failureKinds)
    {
        foreach (var failureKind in failureKinds)
        {
            if (!failures.Contains(failureKind))
                failures.Add(failureKind);
        }
    }
}

public static class NodalOsStepLibrarySanitizer
{
    private static readonly NodalOsRedactionService Redaction = new();

    public static bool ContainsSecretLikeContent(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        Redaction.ContainsSensitiveContent(value);

    public static string SanitizeLabelOrDescription(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return Redaction.RedactValue(value).Value;
    }
}
