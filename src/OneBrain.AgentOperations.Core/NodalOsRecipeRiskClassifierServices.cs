using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsRecipeRiskClassifier
{
    private static readonly NodalOsRecipeDslDecision[] RequiredDslDecisions =
    [
        NodalOsRecipeDslDecision.RepresentationOnly,
        NodalOsRecipeDslDecision.JsonCanonicalModelRequired,
        NodalOsRecipeDslDecision.ParserDeferred,
        NodalOsRecipeDslDecision.RuntimeDeferred,
        NodalOsRecipeDslDecision.ImportRequiresValidation,
        NodalOsRecipeDslDecision.DirectExecutionForbidden
    ];

    private readonly NodalOsRedactionService redaction;
    private readonly NodalOsEvidenceRefBridge evidenceBridge;

    public NodalOsRecipeRiskClassifier()
        : this(new NodalOsRedactionService())
    {
    }

    public NodalOsRecipeRiskClassifier(NodalOsRedactionService redaction)
        : this(redaction, new NodalOsEvidenceRefBridge(redaction))
    {
    }

    public NodalOsRecipeRiskClassifier(
        NodalOsRedactionService redaction,
        NodalOsEvidenceRefBridge evidenceBridge)
    {
        this.redaction = redaction;
        this.evidenceBridge = evidenceBridge;
    }

    public NodalOsRecipeStepRiskClassification ClassifyStep(NodalOsRecipeStepRiskInput input)
    {
        var categories = DetectCategories(input);
        var riskLevel = ResolveRiskLevel(categories);
        var approval = ResolveApprovalRequirement(categories, riskLevel);
        var requiresHandoff = categories.Contains(NodalOsRecipeStepRiskCategory.CredentialOrLogin) ||
                              categories.Contains(NodalOsRecipeStepRiskCategory.CaptchaOrTwoFactor) ||
                              categories.Contains(NodalOsRecipeStepRiskCategory.HumanDecisionRequired);
        var reasons = BuildReasons(categories, riskLevel, approval, requiresHandoff);
        var warnings = new List<string>();

        if (categories.Contains(NodalOsRecipeStepRiskCategory.BrowserAutomationFuture))
            warnings.Add("Browser automation is future-only and runtime remains deferred.");
        if (categories.Contains(NodalOsRecipeStepRiskCategory.Unsupported))
            warnings.Add("Unsupported step must remain blocked until explicitly modeled.");

        return new NodalOsRecipeStepRiskClassification
        {
            ClassificationId = $"recipe-step-risk-{Guid.NewGuid():N}",
            StepId = input.StepId,
            RiskLevel = riskLevel,
            Categories = categories,
            ApprovalRequirement = approval,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            RequiresEvidenceRedaction = true,
            RequiresHumanHandoff = requiresHandoff,
            CanAuthorizeAction = false,
            Reasons = reasons.Select(Sanitize).ToArray(),
            Warnings = warnings.Select(Sanitize).ToArray(),
            EvidenceRefs = input.EvidenceRefs,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public NodalOsRecipeRiskProfile BuildRiskProfile(
        string recipeId,
        IReadOnlyList<NodalOsRecipeStepRiskClassification> classifications)
    {
        var overallRisk = classifications.Count == 0
            ? NodalOsRecipeRiskLevel.Low
            : classifications.Max(classification => classification.RiskLevel);
        var requiresApproval = classifications.Any(RequiresApproval);
        var requiresHandoff = classifications.Any(classification => classification.RequiresHumanHandoff);
        var evidenceRefs = classifications.SelectMany(classification => classification.EvidenceRefs).ToArray();
        var warnings = classifications
            .SelectMany(classification => classification.Warnings)
            .Concat(classifications.Where(classification => classification.RiskLevel >= NodalOsRecipeRiskLevel.High)
                .Select(classification => $"Step {classification.StepId} requires elevated review."))
            .Select(Sanitize)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new NodalOsRecipeRiskProfile
        {
            ProfileId = $"recipe-risk-profile-{Guid.NewGuid():N}",
            RecipeId = recipeId,
            OverallRiskLevel = overallRisk,
            StepClassifications = classifications,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            RequiresEvidenceRedaction = true,
            RequiresHumanApproval = requiresApproval,
            RequiresHumanHandoff = requiresHandoff,
            CanAuthorizeAction = false,
            SummaryWarnings = warnings,
            EvidenceRefs = evidenceRefs,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public NodalOsRecipeRiskClassifierValidationResult ValidateClassification(
        NodalOsRecipeStepRiskClassification classification)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, classification.ClassificationId, "ClassificationId is required.");
        AddRequired(errors, classification.StepId, "StepId is required.");
        if (classification.CreatedAt == default)
            errors.Add("CreatedAt is required.");
        ValidateNoRuntimeAuthority(
            classification.RuntimeExecutionAllowed,
            classification.RuntimeExecutionDeferred,
            classification.RequiresGlobalPolicyEvaluation,
            classification.CanAuthorizeAction,
            errors);
        if (!classification.RequiresEvidenceRedaction)
            errors.Add("Recipe risk classification must require evidence redaction.");
        if (classification.Categories.Count == 0)
            errors.Add("At least one risk category is required.");
        if (classification.RiskLevel >= NodalOsRecipeRiskLevel.High &&
            !RequiresApproval(classification) &&
            !classification.RequiresHumanHandoff)
        {
            errors.Add("High or Critical recipe step risk requires approval or human handoff.");
        }
        foreach (var reason in classification.Reasons)
            ValidateSafeText(errors, "Reasons", reason);
        foreach (var warning in classification.Warnings)
            ValidateSafeText(errors, "Warnings", warning);
        ValidateEvidenceRefs(classification.EvidenceRefs, errors, warnings);

        return Result(errors, warnings);
    }

    public NodalOsRecipeRiskClassifierValidationResult ValidateRiskProfile(NodalOsRecipeRiskProfile profile)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, profile.ProfileId, "ProfileId is required.");
        AddRequired(errors, profile.RecipeId, "RecipeId is required.");
        if (profile.CreatedAt == default)
            errors.Add("CreatedAt is required.");
        ValidateNoRuntimeAuthority(
            profile.RuntimeExecutionAllowed,
            profile.RuntimeExecutionDeferred,
            profile.RequiresGlobalPolicyEvaluation,
            profile.CanAuthorizeAction,
            errors);
        if (!profile.RequiresEvidenceRedaction)
            errors.Add("Recipe risk profile must require evidence redaction.");
        if (profile.StepClassifications.Count > 0 &&
            profile.OverallRiskLevel != profile.StepClassifications.Max(classification => classification.RiskLevel))
        {
            errors.Add("Overall risk must equal the maximum step risk.");
        }
        if (profile.StepClassifications.Any(RequiresApproval) && !profile.RequiresHumanApproval)
            errors.Add("Risk profile must require human approval when any step requires approval.");
        if (profile.StepClassifications.Any(classification => classification.RequiresHumanHandoff) && !profile.RequiresHumanHandoff)
            errors.Add("Risk profile must require human handoff when any step requires handoff.");
        foreach (var warning in profile.SummaryWarnings)
            ValidateSafeText(errors, "SummaryWarnings", warning);
        ValidateEvidenceRefs(profile.EvidenceRefs, errors, warnings);

        return Result(errors, warnings);
    }

    public NodalOsRecipeRiskClassifierValidationResult ValidateDslDecision(NodalOsRecipeDslDecisionRecord decision)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        AddRequired(errors, decision.DecisionId, "DecisionId is required.");
        if (decision.CreatedAt == default)
            errors.Add("CreatedAt is required.");
        if (decision.DslIsRuntime)
            errors.Add("DSL is representation, not runtime.");
        if (decision.ParserImplemented)
            errors.Add("DSL parser is deferred in V1.");
        if (decision.DirectExecutionAllowed)
            errors.Add("Direct DSL execution is forbidden.");
        if (!decision.ImportRequiresValidation)
            errors.Add("DSL import must require validation.");
        if (!decision.JsonCanonicalModelRequired)
            errors.Add("DSL must compile to a future JSON canonical model before use.");
        foreach (var requiredDecision in RequiredDslDecisions)
        {
            if (!decision.Decisions.Contains(requiredDecision))
                errors.Add($"DSL decision is missing: {requiredDecision}.");
        }

        return Result(errors, warnings);
    }

    public void ValidateEvidenceRefs(
        IReadOnlyList<NodalOsEvidenceBridgeRef> evidenceRefs,
        List<string> errors,
        List<string> warnings)
    {
        foreach (var evidenceRef in evidenceRefs)
        {
            var result = evidenceBridge.ValidateBridgeRef(evidenceRef);
            errors.AddRange(result.Errors.Select(Sanitize));
            warnings.AddRange(result.Warnings.Select(Sanitize));
        }
    }

    public void ValidateNoRuntimeAuthority(
        bool runtimeExecutionAllowed,
        bool runtimeExecutionDeferred,
        bool requiresGlobalPolicyEvaluation,
        bool canAuthorizeAction,
        List<string> errors)
    {
        if (runtimeExecutionAllowed)
            errors.Add("Recipe risk classifier cannot allow runtime execution.");
        if (!runtimeExecutionDeferred)
            errors.Add("Recipe risk classifier must keep runtime execution deferred.");
        if (!requiresGlobalPolicyEvaluation)
            errors.Add("Recipe risk classifier must require global policy evaluation.");
        if (canAuthorizeAction)
            errors.Add("Recipe risk classifier cannot authorize actions.");
    }

    private IReadOnlyList<NodalOsRecipeStepRiskCategory> DetectCategories(NodalOsRecipeStepRiskInput input)
    {
        var text = string.Join(
            " ",
            new[]
                {
                    input.StepKind,
                    input.HumanReadableStepRedacted ?? string.Empty
                }
                .Concat(input.TargetKinds)
                .Concat(input.DeclaredOperations));
        var categories = new HashSet<NodalOsRecipeStepRiskCategory>();

        if (ContainsAny(text, "read", "observe", "status", "view"))
            categories.Add(NodalOsRecipeStepRiskCategory.ReadOnlyObservation);
        if (ContainsAny(text, "extract", "scrape", "parse"))
            categories.Add(NodalOsRecipeStepRiskCategory.Extraction);
        if (ContainsAny(text, "form", "fill", "type", "input"))
            categories.Add(NodalOsRecipeStepRiskCategory.FormFill);
        if (ContainsAny(text, "submit", "confirm"))
            categories.Add(NodalOsRecipeStepRiskCategory.Submit);
        if (ContainsAny(text, "purchase", "payment", "pay", "checkout"))
            categories.Add(NodalOsRecipeStepRiskCategory.PurchaseOrPayment);
        if (ContainsAny(text, "delete", "remove", "destroy", "destructive"))
            categories.Add(NodalOsRecipeStepRiskCategory.DeleteOrDestructive);
        if (ContainsAny(text, "publish", "send", "email", "post", "external"))
            categories.Add(NodalOsRecipeStepRiskCategory.ExternalPublishOrSend);
        if (ContainsAny(text, "credential", "login", "password", "sign in"))
            categories.Add(NodalOsRecipeStepRiskCategory.CredentialOrLogin);
        if (ContainsAny(text, "captcha", "2fa", "twofactor", "two-factor", "otp"))
            categories.Add(NodalOsRecipeStepRiskCategory.CaptchaOrTwoFactor);
        if (ContainsAny(text, "file system", "filesystem", "write file", "rename file", "move file", "local file"))
            categories.Add(NodalOsRecipeStepRiskCategory.FileSystemMutation);
        if (ContainsAny(text, "download", "export", "data export"))
            categories.Add(NodalOsRecipeStepRiskCategory.DataExport);
        if (ContainsAny(text, "network", "webhook", "external service", "api call"))
            categories.Add(NodalOsRecipeStepRiskCategory.NetworkOrExternalService);
        if (ContainsAny(text, "browser automation", "click", "selector", "dom", "cdp"))
            categories.Add(NodalOsRecipeStepRiskCategory.BrowserAutomationFuture);
        if (ContainsAny(text, "ask human", "human decision", "handoff"))
            categories.Add(NodalOsRecipeStepRiskCategory.HumanDecisionRequired);
        if (ContainsAny(text, "unsupported", "unknown"))
            categories.Add(NodalOsRecipeStepRiskCategory.Unsupported);

        if (categories.Count == 0)
            categories.Add(NodalOsRecipeStepRiskCategory.Unsupported);

        return categories.ToArray();
    }

    private static NodalOsRecipeRiskLevel ResolveRiskLevel(IReadOnlyCollection<NodalOsRecipeStepRiskCategory> categories)
    {
        if (categories.Contains(NodalOsRecipeStepRiskCategory.PurchaseOrPayment) ||
            categories.Contains(NodalOsRecipeStepRiskCategory.DeleteOrDestructive))
        {
            return NodalOsRecipeRiskLevel.Critical;
        }

        if (categories.Contains(NodalOsRecipeStepRiskCategory.Submit) ||
            categories.Contains(NodalOsRecipeStepRiskCategory.ExternalPublishOrSend) ||
            categories.Contains(NodalOsRecipeStepRiskCategory.CredentialOrLogin) ||
            categories.Contains(NodalOsRecipeStepRiskCategory.CaptchaOrTwoFactor) ||
            categories.Contains(NodalOsRecipeStepRiskCategory.FileSystemMutation) ||
            categories.Contains(NodalOsRecipeStepRiskCategory.Unsupported))
        {
            return NodalOsRecipeRiskLevel.High;
        }

        if (categories.Contains(NodalOsRecipeStepRiskCategory.FormFill) ||
            categories.Contains(NodalOsRecipeStepRiskCategory.DataExport) ||
            categories.Contains(NodalOsRecipeStepRiskCategory.NetworkOrExternalService) ||
            categories.Contains(NodalOsRecipeStepRiskCategory.BrowserAutomationFuture) ||
            categories.Contains(NodalOsRecipeStepRiskCategory.HumanDecisionRequired))
        {
            return NodalOsRecipeRiskLevel.Medium;
        }

        return NodalOsRecipeRiskLevel.Low;
    }

    private static NodalOsRecipeApprovalRequirement ResolveApprovalRequirement(
        IReadOnlyCollection<NodalOsRecipeStepRiskCategory> categories,
        NodalOsRecipeRiskLevel riskLevel)
    {
        if (categories.Contains(NodalOsRecipeStepRiskCategory.PurchaseOrPayment) ||
            categories.Contains(NodalOsRecipeStepRiskCategory.ExternalPublishOrSend))
        {
            return NodalOsRecipeApprovalRequirement.RequiredBeforePublishSendPayment;
        }

        if (categories.Contains(NodalOsRecipeStepRiskCategory.DeleteOrDestructive) ||
            categories.Contains(NodalOsRecipeStepRiskCategory.FileSystemMutation))
        {
            return NodalOsRecipeApprovalRequirement.RequiredBeforeDestructiveAction;
        }

        if (categories.Contains(NodalOsRecipeStepRiskCategory.CredentialOrLogin) ||
            categories.Contains(NodalOsRecipeStepRiskCategory.CaptchaOrTwoFactor))
        {
            return NodalOsRecipeApprovalRequirement.RequiredBeforeCredentialUse;
        }

        if (categories.Contains(NodalOsRecipeStepRiskCategory.Submit) ||
            categories.Contains(NodalOsRecipeStepRiskCategory.FormFill) ||
            riskLevel >= NodalOsRecipeRiskLevel.High)
        {
            return NodalOsRecipeApprovalRequirement.RequiredBeforeExecution;
        }

        return NodalOsRecipeApprovalRequirement.NotRequiredForObservation;
    }

    private static bool RequiresApproval(NodalOsRecipeStepRiskClassification classification) =>
        classification.ApprovalRequirement != NodalOsRecipeApprovalRequirement.NotRequiredForObservation;

    private static IReadOnlyList<string> BuildReasons(
        IReadOnlyList<NodalOsRecipeStepRiskCategory> categories,
        NodalOsRecipeRiskLevel riskLevel,
        NodalOsRecipeApprovalRequirement approval,
        bool requiresHandoff)
    {
        var reasons = new List<string>
        {
            $"Risk level classified as {riskLevel}.",
            $"Approval requirement: {approval}."
        };
        reasons.AddRange(categories.Select(category => $"Detected category: {category}."));
        if (requiresHandoff)
            reasons.Add("Human handoff is required for credential, captcha, two-factor, or human-decision category.");
        reasons.Add("Classification is contract-only and cannot authorize runtime action.");
        return reasons;
    }

    private void ValidateSafeText(List<string> errors, string fieldName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        if (redaction.ContainsSensitiveField(fieldName, value) || redaction.ContainsSensitiveContent(value))
            errors.Add($"{fieldName} contains sensitive content and must be redacted before recipe risk persistence.");
    }

    private string Sanitize(string value) =>
        redaction.RedactValue(value).Value;

    private static bool ContainsAny(string value, params string[] needles) =>
        needles.Any(needle => value.Contains(needle, StringComparison.OrdinalIgnoreCase));

    private static void AddRequired(List<string> errors, string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add(message);
    }

    private static NodalOsRecipeRiskClassifierValidationResult Result(
        List<string> errors,
        List<string> warnings) =>
        new()
        {
            IsValid = errors.Count == 0,
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            CanAuthorizeAction = false,
            Errors = errors.Distinct(StringComparer.Ordinal).ToArray(),
            Warnings = warnings.Distinct(StringComparer.Ordinal).ToArray()
        };
}

public sealed class NodalOsRecipeRiskClassifierJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializeStepInput(NodalOsRecipeStepRiskInput input) =>
        JsonSerializer.Serialize(input, Options);

    public NodalOsRecipeStepRiskInput? DeserializeStepInput(string json) =>
        JsonSerializer.Deserialize<NodalOsRecipeStepRiskInput>(json, Options);

    public string SerializeClassification(NodalOsRecipeStepRiskClassification classification) =>
        JsonSerializer.Serialize(classification, Options);

    public NodalOsRecipeStepRiskClassification? DeserializeClassification(string json) =>
        JsonSerializer.Deserialize<NodalOsRecipeStepRiskClassification>(json, Options);

    public string SerializeProfile(NodalOsRecipeRiskProfile profile) =>
        JsonSerializer.Serialize(profile, Options);

    public NodalOsRecipeRiskProfile? DeserializeProfile(string json) =>
        JsonSerializer.Deserialize<NodalOsRecipeRiskProfile>(json, Options);

    public string SerializeDslDecision(NodalOsRecipeDslDecisionRecord decision) =>
        JsonSerializer.Serialize(decision, Options);

    public NodalOsRecipeDslDecisionRecord? DeserializeDslDecision(string json) =>
        JsonSerializer.Deserialize<NodalOsRecipeDslDecisionRecord>(json, Options);
}

public static class NodalOsRecipeRiskClassifierFixtures
{
    public static NodalOsRecipeStepRiskInput ReadOnlyExtractionStep() =>
        Step("step-read-only-extract", "Read", "read extract observation", ["page"], ["read", "extract"]);

    public static NodalOsRecipeStepRiskInput FormFillStep() =>
        Step("step-form-fill", "Type", "form fill redacted field", ["form"], ["form fill", "type"]);

    public static NodalOsRecipeStepRiskInput SubmitStep() =>
        Step("step-submit", "Submit", "submit prepared form", ["form"], ["submit"]);

    public static NodalOsRecipeStepRiskInput PurchasePaymentStep() =>
        Step("step-payment", "Payment", "purchase payment checkout", ["checkout"], ["purchase", "payment"]);

    public static NodalOsRecipeStepRiskInput DeleteStep() =>
        Step("step-delete", "Delete", "delete destructive record", ["record"], ["delete", "destructive"]);

    public static NodalOsRecipeStepRiskInput PublishSendStep() =>
        Step("step-publish", "Publish", "external publish send", ["external"], ["publish", "send"]);

    public static NodalOsRecipeStepRiskInput CredentialLoginStep() =>
        Step("step-login", "Login", "login credential handoff", ["auth"], ["login", "credential"]);

    public static NodalOsRecipeStepRiskInput CaptchaTwoFactorStep() =>
        Step("step-2fa", "TwoFactor", "captcha two-factor handoff", ["auth"], ["captcha", "two-factor"]);

    public static NodalOsRecipeStepRiskInput FileSystemMutationStep() =>
        Step("step-file-mutation", "FileSystem", "local file system mutation", ["local file"], ["write file", "file system"]);

    public static NodalOsRecipeStepRiskInput DataExportStep() =>
        Step("step-export", "Export", "data export request", ["data"], ["export", "download"]);

    public static NodalOsRecipeStepRiskInput BrowserAutomationFutureStep() =>
        Step("step-browser-future", "BrowserAutomationFuture", "browser automation future selector", ["browser"], ["browser automation", "selector"]);

    public static NodalOsRecipeStepRiskInput UnsupportedStep() =>
        Step("step-unsupported", "Unsupported", "unsupported unknown operation", ["unknown"], ["unsupported"]);

    public static NodalOsRecipeRiskProfile MixedRiskRecipeProfile()
    {
        var classifier = new NodalOsRecipeRiskClassifier();
        return classifier.BuildRiskProfile(
            "recipe-mixed-risk",
            [
                classifier.ClassifyStep(ReadOnlyExtractionStep()),
                classifier.ClassifyStep(SubmitStep()),
                classifier.ClassifyStep(CaptchaTwoFactorStep()),
                classifier.ClassifyStep(PurchasePaymentStep())
            ]);
    }

    public static NodalOsRecipeRiskProfile SafeReadOnlyRecipeProfile()
    {
        var classifier = new NodalOsRecipeRiskClassifier();
        return classifier.BuildRiskProfile(
            "recipe-read-only",
            [classifier.ClassifyStep(ReadOnlyExtractionStep())]);
    }

    public static NodalOsRecipeDslDecisionRecord DslRepresentationOnlyDecision() =>
        new()
        {
            DecisionId = "recipe-dsl-representation-only-v1",
            DslIsRuntime = false,
            ParserImplemented = false,
            DirectExecutionAllowed = false,
            ImportRequiresValidation = true,
            JsonCanonicalModelRequired = true,
            Decisions =
            [
                NodalOsRecipeDslDecision.RepresentationOnly,
                NodalOsRecipeDslDecision.JsonCanonicalModelRequired,
                NodalOsRecipeDslDecision.ParserDeferred,
                NodalOsRecipeDslDecision.RuntimeDeferred,
                NodalOsRecipeDslDecision.ImportRequiresValidation,
                NodalOsRecipeDslDecision.DirectExecutionForbidden
            ],
            CreatedAt = DateTimeOffset.UtcNow
        };

    public static NodalOsRecipeDslDecisionRecord InvalidDslDirectExecutionDecision() =>
        DslRepresentationOnlyDecision() with
        {
            DirectExecutionAllowed = true,
            DslIsRuntime = true
        };

    public static NodalOsEvidenceBridgeRef ValidEvidenceRef() =>
        new()
        {
            EvidenceId = $"evidence-{Guid.NewGuid():N}",
            Kind = "recipe-risk-contract",
            Ref = "ledger:recipe-risk-contract",
            Hash = "sha256:recipe-risk-contract",
            SourceKind = NodalOsEvidenceBridgeSourceKind.RecipeManifest,
            UseKind = NodalOsEvidenceBridgeUseKind.AuditTrail,
            Authority = NodalOsEvidenceBridgeAuthority.NoAuthority,
            Sensitivity = NodalOsEvidenceSensitivity.NonSensitive,
            RedactionState = NodalOsEvidenceRedactionState.NotRequired,
            LedgerRef = "ledger:recipe-risk-contract",
            Provenance = "NODRIX:RecipeRiskClassifier:ContractOnly",
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static NodalOsRecipeStepRiskInput Step(
        string stepId,
        string stepKind,
        string humanReadableStep,
        IReadOnlyList<string> targetKinds,
        IReadOnlyList<string> operations) =>
        new()
        {
            StepId = stepId,
            RecipeId = "recipe-risk-fixture",
            MissionId = "mission-risk-fixture",
            TaskId = "task-risk-fixture",
            StepKind = stepKind,
            HumanReadableStepRedacted = humanReadableStep,
            TargetKinds = targetKinds,
            DeclaredOperations = operations,
            EvidenceRefs = [ValidEvidenceRef()]
        };
}
