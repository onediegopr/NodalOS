using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OneBrain.Core.Recipes;

namespace OneBrain.Recipes.Tests;

[TestClass]
[TestCategory("RecipeToolTrustSecretsByReference")]
public sealed class RecipeToolTrustSecretsByReferenceTests
{
    [TestMethod]
    public void ToolTrustEntryDefaultsToCandidateAndDoesNotImplyLiveCapability()
    {
        var entry = RecipeToolTrustEntry.CandidateConnector("tool.connector", "Fixture connector");

        Assert.AreEqual(RecipeToolTrustLevel.Candidate, entry.TrustLevel);
        Assert.AreEqual(RecipeToolRuntimeStatus.ReferenceOnly, entry.RuntimeStatus);
        Assert.IsFalse(entry.LiveRuntimeEnabled);
        Assert.IsFalse(entry.ConnectorExecutionEnabled);
        Assert.IsFalse(entry.IsTrustedForFixture);
    }

    [TestMethod]
    public void BrowserAndDesktopRuntimeToolsRemainLiveBlocked()
    {
        foreach (var category in new[] { RecipeToolCategory.BrowserRuntime, RecipeToolCategory.DesktopRuntime })
        {
            var entry = TrustedTool(category) with { RuntimeStatus = RecipeToolRuntimeStatus.LiveBlocked };

            Assert.IsTrue(entry.IsLiveBlocked);
            Assert.IsFalse(entry.LiveRuntimeEnabled);
            Assert.IsFalse(entry.ConnectorExecutionEnabled);
        }
    }

    [TestMethod]
    public void ConnectorToolCanBeReferenceOrFixtureOrFutureGatedButNeverLive()
    {
        foreach (var status in new[] { RecipeToolRuntimeStatus.ReferenceOnly, RecipeToolRuntimeStatus.FixtureOnly, RecipeToolRuntimeStatus.FutureGated })
        {
            var entry = TrustedTool(RecipeToolCategory.Connector) with { RuntimeStatus = status };

            Assert.AreEqual(RecipeToolCategory.Connector, entry.Category);
            Assert.IsFalse(entry.LiveRuntimeEnabled);
            Assert.IsFalse(entry.ConnectorExecutionEnabled);
        }
    }

    [TestMethod]
    public void UnknownToolIsBlocked()
    {
        var registry = new RecipeToolTrustRegistry([TrustedTool(RecipeToolCategory.Unknown)]);
        var readiness = RecipeToolTrustSecretsPolicy.EvaluateCredentialedAction(ActionRequirement() with { ToolTrustRef = "tool.Unknown" }, registry, [PresentSecret()]);

        Assert.IsFalse(readiness.IsReady);
        Assert.AreEqual(RecipeCredentialedActionDecisionStatus.BlockedUntrustedTool, readiness.Decision.Status);
        Assert.IsFalse(readiness.Decision.AllowsLiveRuntime);
    }

    [TestMethod]
    public void ToolRequiringSecretWithoutSecretRefBlocksReadiness()
    {
        var readiness = RecipeToolTrustSecretsPolicy.EvaluateCredentialedAction(ActionRequirement(), TrustedRegistry(), []);

        AssertBlocked(readiness, RecipeCredentialedActionDecisionStatus.BlockedMissingSecretReference);
    }

    [TestMethod]
    public void SecretRefStoresAliasKindScopeOnlyNoRawValue()
    {
        var secret = SecretRef(RecipeSecretKind.ApiKey, RecipeSecretScope.Tool);
        var json = JsonSerializer.Serialize(secret, JsonOptions());

        Assert.AreEqual("fixture-api-key", secret.DisplayAlias);
        Assert.AreEqual(RecipeSecretPresenceStatus.PresentByReference, secret.PresenceStatus);
        Assert.IsFalse(secret.StoresRawSecretValue);
        Assert.IsFalse(json.Contains("secret-value", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains("sk-", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void RawSecretDetectedAndScopeMismatchBlockReadiness()
    {
        var raw = PresentSecret() with { RawValuePresent = true, PresenceStatus = RecipeSecretPresenceStatus.BlockedRawValueDetected };
        var mismatch = PresentSecret() with { RequiredScope = RecipeSecretScope.Organization, PresenceStatus = RecipeSecretPresenceStatus.BlockedScopeMismatch };

        AssertBlocked(RecipeToolTrustSecretsPolicy.EvaluateCredentialedAction(ActionRequirement(), TrustedRegistry(), [raw]), RecipeCredentialedActionDecisionStatus.BlockedRawSecretDetected);
        AssertBlocked(RecipeToolTrustSecretsPolicy.EvaluateCredentialedAction(ActionRequirement(), TrustedRegistry(), [mismatch]), RecipeCredentialedActionDecisionStatus.BlockedScopeMismatch);
    }

    [TestMethod]
    public void HighRiskSecretKindsRequireHighCriticalRiskOrHumanApprovalPath()
    {
        foreach (var kind in new[] { RecipeSecretKind.Password, RecipeSecretKind.SessionCookie, RecipeSecretKind.PrivateKey, RecipeSecretKind.PaymentCredential, RecipeSecretKind.FiscalCertificate })
        {
            var requirement = PresentSecret(kind);
            var lowNoReview = Risk(RecipeRiskLevel.Low, approval: false, human: false);
            var lowWithHuman = Risk(RecipeRiskLevel.Low, approval: false, human: true);

            var blocked = RecipeToolTrustSecretsPolicy.EvaluateSecretReadiness(requirement, lowNoReview);
            var ready = RecipeToolTrustSecretsPolicy.EvaluateSecretReadiness(requirement, lowWithHuman);

            Assert.IsFalse(blocked.IsReady, kind.ToString());
            Assert.IsTrue(ready.IsReady, kind.ToString());
            Assert.IsFalse(ready.LiveRuntimeEnabled);
        }
    }

    [TestMethod]
    public void CredentialedActionWithoutTrustedToolOrSecretOrApprovalBlocks()
    {
        AssertBlocked(
            RecipeToolTrustSecretsPolicy.EvaluateCredentialedAction(ActionRequirement(), new RecipeToolTrustRegistry([]), [PresentSecret()]),
            RecipeCredentialedActionDecisionStatus.BlockedMissingToolTrust);

        AssertBlocked(
            RecipeToolTrustSecretsPolicy.EvaluateCredentialedAction(ActionRequirement() with { ApprovalNarrativePresent = false }, TrustedRegistry(), [PresentSecret()]),
            RecipeCredentialedActionDecisionStatus.BlockedMissingApprovalPolicy);
    }

    [TestMethod]
    public void ConnectorDraftWithTrustSecretRefsIsPreviewFixtureOnlyNotLive()
    {
        var eligibility = ConnectorEligibility(RecipeConnectorRuntimeMode.FixtureOnly, RecipeConnectorActionCategory.ReadData);
        var readiness = RecipeToolTrustSecretsPolicy.EvaluateCredentialedAction(ActionRequirement(RecipeConnectorActionCategory.ReadData), TrustedRegistry(), [PresentSecret()], eligibility);
        var connector = RecipeToolTrustSecretsPolicy.EvaluateConnectorEligibility(eligibility);

        Assert.IsTrue(readiness.IsReady);
        Assert.AreEqual(RecipeCredentialedActionDecisionStatus.ReadyForFixture, readiness.Decision.Status);
        Assert.IsTrue(connector.EligibleForFixture);
        Assert.IsTrue(connector.LiveBlocked);
        Assert.IsFalse(connector.LiveRuntimeEnabled);
        Assert.IsFalse(connector.ConnectorExecutionEnabled);
    }

    [TestMethod]
    public void FutureConnectorRuntimeIsBlockedFutureGatedUnlessReferenceFixtureEligibilityExists()
    {
        var futurePack = EvidencePack(RecipeEvidenceCaptureMode.FutureConnectorRuntime);
        var completeness = RecipeEvidencePolicy.EvaluatePackCompleteness(futurePack, [new RecipeStepEvidenceResult(true, RecipeStepEvidenceStatus.Satisfied, [], [])], [Validation(RecipeValidationEvidenceStatus.Passed, RecipeValidationSeverity.Blocking)]);
        var futureEligibility = RecipeToolTrustSecretsPolicy.EvaluateConnectorEligibility(ConnectorEligibility(RecipeConnectorRuntimeMode.FutureGated, RecipeConnectorActionCategory.ReadData));

        Assert.AreEqual(RecipeEvidenceCompleteness.BlockedLiveRuntimeDisabled, completeness);
        Assert.IsFalse(futureEligibility.EligibleForPreview);
        Assert.IsTrue(futureEligibility.LiveBlocked);
    }

    [TestMethod]
    public void MutatingConnectorActionsRequireApprovalAndRemainLiveBlocked()
    {
        var actions = new[]
        {
            RecipeConnectorActionCategory.SubmitFiscal,
            RecipeConnectorActionCategory.ExecutePayment,
            RecipeConnectorActionCategory.SendMessage,
            RecipeConnectorActionCategory.PublishListing,
            RecipeConnectorActionCategory.UpdatePrice,
            RecipeConnectorActionCategory.UpdateStock,
            RecipeConnectorActionCategory.DeleteData
        };

        foreach (var action in actions)
        {
            var missingApproval = RecipeToolTrustSecretsPolicy.EvaluateConnectorEligibility(ConnectorEligibility(RecipeConnectorRuntimeMode.FixtureOnly, action) with { ApprovalPolicyPresent = false });
            var withApproval = RecipeToolTrustSecretsPolicy.EvaluateConnectorEligibility(ConnectorEligibility(RecipeConnectorRuntimeMode.ManualAssistOnly, action));

            Assert.IsFalse(missingApproval.EligibleForPreview, action.ToString());
            Assert.IsTrue(missingApproval.RequiresApproval, action.ToString());
            Assert.IsTrue(withApproval.LiveBlocked, action.ToString());
            Assert.IsFalse(withApproval.LiveRuntimeEnabled, action.ToString());
            Assert.IsFalse(withApproval.ConnectorExecutionEnabled, action.ToString());
        }
    }

    [TestMethod]
    public void UnknownConnectorActionIsBlocked()
    {
        var decision = RecipeToolTrustSecretsPolicy.EvaluateConnectorEligibility(ConnectorEligibility(RecipeConnectorRuntimeMode.FixtureOnly, RecipeConnectorActionCategory.Unknown));

        Assert.IsFalse(decision.EligibleForPreview);
        Assert.AreEqual(RecipeReadinessStatus.BlockedRiskGate, decision.Status);
    }

    [TestMethod]
    public void SensitiveCategoriesRequireHumanOrApprovalPath()
    {
        var categories = new[]
        {
            SensitiveActionCategory.Login,
            SensitiveActionCategory.CredentialUse,
            SensitiveActionCategory.DataDeletion,
            SensitiveActionCategory.DataMutation,
            SensitiveActionCategory.FileWrite,
            SensitiveActionCategory.ExternalSystemMutation,
            SensitiveActionCategory.MarketplaceListingChange,
            SensitiveActionCategory.PriceOrStockChange,
            SensitiveActionCategory.PersonalDataHandling,
            SensitiveActionCategory.SecretHandling
        };

        foreach (var category in categories)
        {
            var result = RecipePolicyPreflightEvaluator.Evaluate(SensitiveRecipe(category, approval: false, human: false), RecipeRunMode.FixtureRun);
            Assert.IsTrue(result.BlockingIssues.Any(i => i.IssueId == "sensitive-requires-human-or-approval"), category.ToString());
            Assert.IsFalse(result.LiveRuntimeEnabled);
        }
    }

    [TestMethod]
    public void ChallengeLiveAndUnknownSensitiveCategoriesRemainBlocked()
    {
        foreach (var category in new[] { SensitiveActionCategory.CaptchaOrChallenge, SensitiveActionCategory.TwoFactor })
            Assert.IsTrue(RecipePolicyPreflightEvaluator.Evaluate(SensitiveRecipe(category), RecipeRunMode.FixtureRun).BlockingIssues.Any(i => i.IssueId == "challenge-human-required"));

        foreach (var category in new[] { SensitiveActionCategory.BrowserLiveAction, SensitiveActionCategory.DesktopLiveAction })
            Assert.IsTrue(RecipePolicyPreflightEvaluator.Evaluate(SensitiveRecipe(category), RecipeRunMode.FixtureRun).BlockingIssues.Any(i => i.IssueId == "live-action-blocked"));

        Assert.IsTrue(RecipePolicyPreflightEvaluator.Evaluate(SensitiveRecipe(SensitiveActionCategory.UnknownSensitiveAction), RecipeRunMode.FixtureRun).BlockingIssues.Any(i => i.IssueId == "unknown-sensitive-blocked"));
    }

    [TestMethod]
    public void PersonalDataAndSecretHandlingRequireRedactionEvidenceExpectation()
    {
        foreach (var category in new[] { SensitiveActionCategory.PersonalDataHandling, SensitiveActionCategory.SecretHandling })
        {
            var recipe = SensitiveRecipe(category);
            var redaction = EvidencePack(RecipeEvidenceCaptureMode.ReferenceOnly).RedactionSummary;

            Assert.IsTrue(recipe.EvidenceExpectationRefs.Count > 0);
            Assert.IsTrue(redaction.RedactionApplied);
            Assert.IsTrue(redaction.EvidenceSafeForHandoff);
        }
    }

    [TestMethod]
    public void FailedBlockingValidationPreventsCompleteEvidenceStatusButWarningCanBeRepresented()
    {
        var step = new RecipeStepEvidenceResult(true, RecipeStepEvidenceStatus.Satisfied, [], []);

        var failedBlocking = RecipeEvidencePolicy.EvaluatePackCompleteness(EvidencePack(RecipeEvidenceCaptureMode.ReferenceOnly), [step], [Validation(RecipeValidationEvidenceStatus.Failed, RecipeValidationSeverity.Blocking)]);
        var failedWarning = RecipeEvidencePolicy.EvaluatePackCompleteness(EvidencePack(RecipeEvidenceCaptureMode.ReferenceOnly), [step], [Validation(RecipeValidationEvidenceStatus.Failed, RecipeValidationSeverity.Warning)]);

        Assert.AreEqual(RecipeEvidenceCompleteness.BlockedMissingRequiredEvidence, failedBlocking);
        Assert.AreEqual(RecipeEvidenceCompleteness.Complete, failedWarning);
    }

    [TestMethod]
    public void ApprovalDecisionRemainsNarrativeBoundAfterAuditCleanup()
    {
        var narrative = RecipeApprovalNarrativeFactory.Create("narrative.challenge", "recipe.1", "5.0.0", "run.1", RecipeHumanInterventionKind.CaptchaOrChallengeDetected);
        var decision = RecipeApprovalDecisionPolicy.Decide("decision.absent", narrative, RecipeApprovalDecisionOption.ApproveFixtureRunOnly);

        Assert.AreEqual(RecipeApprovalDecisionOption.KeepBlocked, decision.Option);
        Assert.AreEqual(RecipeApprovalDecisionStatus.KeptBlocked, decision.Status);
        Assert.IsFalse(decision.LiveRuntimeEnabled);
        Assert.IsFalse(decision.ActionAuthorityGranted);
    }

    [TestMethod]
    public void NoVaultApiNetworkConnectorExecutionOrLiveRuntimeIsIntroduced()
    {
        var readiness = RecipeToolTrustSecretsPolicy.EvaluateCredentialedAction(ActionRequirement(), TrustedRegistry(), [PresentSecret()]);
        var connector = RecipeToolTrustSecretsPolicy.EvaluateConnectorEligibility(ConnectorEligibility(RecipeConnectorRuntimeMode.FixtureOnly, RecipeConnectorActionCategory.ReadData));
        var secret = SecretRef(RecipeSecretKind.OAuthToken, RecipeSecretScope.ExternalVaultRef);

        Assert.IsTrue(readiness.IsReady);
        Assert.IsFalse(readiness.Decision.AllowsLiveRuntime);
        Assert.IsFalse(readiness.Decision.AllowsConnectorExecution);
        Assert.IsFalse(readiness.Decision.ActionAuthorityGranted);
        Assert.IsFalse(connector.LiveRuntimeEnabled);
        Assert.IsFalse(connector.ConnectorExecutionEnabled);
        Assert.IsFalse(secret.LiveRuntimeEnabled);
        Assert.IsFalse(secret.StoresRawSecretValue);
    }

    private static RecipeToolTrustRegistry TrustedRegistry() =>
        new([TrustedTool(RecipeToolCategory.Connector)]);

    private static RecipeToolTrustEntry TrustedTool(RecipeToolCategory category) =>
        RecipeToolTrustEntry.CandidateConnector($"tool.{category}", $"{category} fixture") with
        {
            Category = category,
            TrustLevel = category == RecipeToolCategory.Unknown ? RecipeToolTrustLevel.Untrusted : RecipeToolTrustLevel.ApprovedForFixture,
            RuntimeStatus = category is RecipeToolCategory.BrowserRuntime or RecipeToolCategory.DesktopRuntime
                ? RecipeToolRuntimeStatus.LiveBlocked
                : RecipeToolRuntimeStatus.FixtureOnly,
            RequiredSecretRefs = ["secret.fixture"],
            RequiredApprovalPolicyRefs = ["approval.policy"],
            EvidencePolicyRefs = ["evidence.policy"],
            RedactionPolicyRefs = ["redaction.policy"]
        };

    private static RecipeCredentialedActionRequirement ActionRequirement(RecipeConnectorActionCategory? action = null) =>
        new(
            "credentialed.action",
            "tool.Connector",
            ["secret.fixture"],
            [RecipeSecretScope.Tool, RecipeSecretScope.ExternalVaultRef],
            ApprovalNarrativeRequired: true,
            ApprovalNarrativePresent: true,
            action);

    private static RecipeSecretRequirement PresentSecret(RecipeSecretKind kind = RecipeSecretKind.ApiKey) =>
        new(
            "secret.requirement",
            "secret.fixture",
            kind,
            RecipeSecretScope.Tool,
            "tool.Connector",
            Required: true,
            RecipeSecretPresenceStatus.PresentByReference,
            RawValuePresent: false,
            RedactionPolicyRef: "redaction.policy");

    private static RecipeSecretRef SecretRef(RecipeSecretKind kind, RecipeSecretScope scope) =>
        new(
            "secret.fixture",
            "fixture-api-key",
            kind,
            scope,
            "tool.Connector",
            RecipeSecretPresenceStatus.PresentByReference,
            RotationMetadataRef: "rotation.ref",
            LastVerifiedSummaryRef: "verified.ref",
            RedactionPolicyRef: "redaction.policy",
            AllowedUsageModes: [RecipeRunMode.CatalogPreview, RecipeRunMode.FixtureRun],
            BlockedUsageModes: [RecipeRunMode.LiveRunBlocked, RecipeRunMode.LiveRunAllowedFuture],
            OperatorVisibleSummary: "Secret is present by reference; value omitted.",
            RawValuePresent: false);

    private static RecipeRiskProfile Risk(RecipeRiskLevel level, bool approval, bool human) =>
        new(
            "risk.secret",
            level,
            new HashSet<SensitiveActionCategory> { SensitiveActionCategory.SecretHandling },
            [],
            approval,
            human,
            ["secret.fixture"],
            SecretValuesExposed: false);

    private static RecipeConnectorEligibility ConnectorEligibility(RecipeConnectorRuntimeMode mode, RecipeConnectorActionCategory action) =>
        new(
            "connector.eligibility",
            "tool.Connector",
            mode,
            action,
            new RecipeConnectorTrustRequirement("tool.Connector", ["secret.fixture"], ApprovalRequired: RecipeToolTrustSecretsPolicy.RequiresApproval(action), EvidencePolicyRequired: true),
            ApprovalPolicyPresent: true,
            EvidencePolicyPresent: true);

    private static RecipeDefinition SensitiveRecipe(SensitiveActionCategory category, bool approval = true, bool human = true) =>
        new($"sensitive.{category}")
        {
            RecipeId = $"recipe.sensitive.{category}",
            RequiredToolTrustRefs = ["tool.Connector"],
            RequiredSecretRefs = ["secret.fixture"],
            OutputSchemaRef = "schema.output",
            Blocks =
            [
                new(
                    "submit",
                    RecipeBlockType.BrowserAction,
                    "submit",
                    "submit fixture",
                    TargetRef: "target.fixture",
                    InputBinding: "input.fixture",
                    OutputBinding: "output.fixture",
                    Preconditions: [],
                    Postconditions: [],
                    ValidationRefs: ["validation.fixture"],
                    RecipeRiskLevel.High,
                    RecipeApprovalRequirement.Required,
                    EvidenceExpectationRef: "evidence.fixture",
                    FailurePolicyRef: "failure.fixture",
                    NextBlockRefs: [])
            ],
            RunLimits = new(MaxSteps: 10, MaxRuntimeSeconds: 60, MaxRetries: 1, MaxLoopIterations: 2, MaxNestedLoops: 1),
            CompleteCriteria = new([new("complete", RecipeCompleteCriterionType.ExpectedOutputExists, "output.ref")]),
            TerminateCriteria = new([
                new("policy", RecipeTerminateCriterionType.PolicyBlocked, "policy.ref"),
                new("human", RecipeTerminateCriterionType.HumanInterventionRequired, "human.ref")
            ]),
            ValidationPolicy = new([new("validation", RecipeValidationKind.VisibleTextExists, RecipeValidationSeverity.Blocking, AppliesToBlockId: "submit", PostValidation: true)]),
            RuntimeRiskProfile = new(
                "risk.fixture",
                RecipeRiskLevel.High,
                new HashSet<SensitiveActionCategory> { category },
                [],
                approval,
                human,
                ["secret.fixture"],
                SecretValuesExposed: false),
            ActionResolutionPolicy = new([
                new ActionResolutionAttempt(1, ActionResolutionStrategy.KnownTarget, "target.fixture", "evidence.target")
            ]),
            EvidenceExpectationRefs = ["evidence.redaction.fixture"],
            ApprovalCheckpointRefs = ["approval.fixture"]
        };

    private static RecipeEvidencePack EvidencePack(RecipeEvidenceCaptureMode mode) =>
        new(
            "pack.1",
            "recipe.1",
            "5.0.0",
            "run.1",
            MissionIdRef: null,
            WorkitemRefs: [],
            StepEvidenceRefs: ["step.evidence"],
            ValidationEvidenceRefs: ["validation.evidence"],
            ApprovalRefs: ["approval.ref"],
            TimelineEventRefs: ["timeline.ref"],
            ArtifactRefs: [],
            RedactionReportRef: "redaction.report",
            RecipeEvidenceSensitivity.Confidential,
            RecipeEvidenceCompleteness.Partial,
            mode,
            CreatedAt: null,
            RecipeRunMode.FixtureRun,
            FailureSummary: null,
            new RecipeEvidenceRedactionSummary(
                RedactionApplied: true,
                RedactionPolicyRef: "redaction.policy",
                SensitiveFields: [new RecipeSensitiveFieldSummary(RecipeSensitiveFieldCategory.Secret, "[REDACTED]", "secret.fixture", RawValuePresent: false)],
                SecretRefs: ["secret.fixture"],
                RecipeEvidenceSecretHandlingStatus.SecretRefsOnly,
                RawPayloadExposed: false,
                EvidenceSafeForHandoff: true,
                EvidenceSafeForTimeline: true));

    private static RecipeValidationEvidence Validation(RecipeValidationEvidenceStatus status, RecipeValidationSeverity severity) =>
        new(
            $"validation.{status}.{severity}",
            RecipeValidationKind.EvidenceRefExists,
            "expected ref",
            "[REDACTED]",
            ["evidence.ref"],
            status,
            severity,
            RecipeEvidenceRedactionStatus.Applied,
            status == RecipeValidationEvidenceStatus.Passed ? null : "fixture validation failed");

    private static void AssertBlocked(RecipeCredentialedActionReadiness readiness, RecipeCredentialedActionDecisionStatus status)
    {
        Assert.IsFalse(readiness.IsReady);
        Assert.AreEqual(status, readiness.Decision.Status);
        Assert.IsFalse(readiness.Decision.AllowsLiveRuntime);
        Assert.IsFalse(readiness.Decision.AllowsConnectorExecution);
        Assert.IsFalse(readiness.Decision.ActionAuthorityGranted);
    }

    private static JsonSerializerOptions JsonOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
