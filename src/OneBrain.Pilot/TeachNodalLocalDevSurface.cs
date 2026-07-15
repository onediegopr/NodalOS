using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using OneBrain.Core.Memory;
using OneBrain.Core.Skills;

namespace OneBrain.Pilot;

public sealed record TeachNodalLocalDevStepSnapshot(
    string StepId,
    string Intent,
    string Action,
    string Capability,
    string Target,
    string ObservedLabel,
    string LabelSource,
    IReadOnlyList<string> ParameterRefs,
    IReadOnlyList<string> SelectorRefs,
    bool Verified,
    int VerificationFacts,
    string BeforeFingerprint,
    string AfterFingerprint,
    bool PromptInjectionObserved);

public sealed record TeachNodalLocalDevOperatorSnapshot(
    string Decision,
    bool Accepted,
    bool LocalDevOnly,
    bool ReadOnly,
    bool FixtureOnly,
    bool SecretsExcluded,
    string CaptureState,
    bool CaptureOptInRecorded,
    bool CaptureApplicationScopeBound,
    bool CaptureCanCompileVerifiedSkill,
    int CaptureObservationCount,
    int PerStepApprovalsRequested,
    bool CaptureRawInputStored,
    bool CaptureGlobalHooksUsed,
    bool CaptureExecutionAuthorityGranted,
    string DemonstrationId,
    string Title,
    string CompilationDecision,
    string CompilationCode,
    string RecipeId,
    string RecipeReviewState,
    bool RecipeLiveRuntimeEnabled,
    string SkillId,
    string SkillState,
    int TransitionCount,
    string SkillFingerprint,
    string ProcessMemoryId,
    string ProcessMemoryStatus,
    IReadOnlyList<TeachNodalLocalDevStepSnapshot> Steps,
    bool PromptInjectionObserved,
    bool PromptInjectionModifiedGoal,
    bool PromptInjectionExpandedScope,
    bool PromptInjectionPublishedExternally,
    IReadOnlyList<string> Findings,
    IReadOnlyList<string> EvidenceRefs,
    string DisabledActionId,
    string DisabledActionState,
    string RequiredOperatorSignal,
    string NextSafeStep,
    bool LiveRecorderUsed,
    bool MouseOrKeyboardHooksUsed,
    bool RawScreenshotStored,
    bool RawDomStored,
    bool NetworkUsed,
    bool ProductAuthorityGranted);

public static class TeachNodalLocalDevSurface
{
    public const string FeatureFlag = "NODAL_OS_TEACH_NODAL_SURFACE_ENABLED";
    public const string JsonRoute = "/api/runtime/teach-nodal";
    public const string HtmlRoute = "/runtime/teach-nodal";
    public const string DisabledActionId = "TEACH_NODAL_LIVE_CAPTURE_AUTHORITY";
    public const string RequiredOperatorSignal = "AUTHORIZE_NODAL_OS_TEACH_NODAL_APPLICATION_SCOPED_WINDOWS_OBSERVATION_ADAPTER_PREP";

    public static IEndpointRouteBuilder MapTeachNodalLocalDevSurface(
        this IEndpointRouteBuilder endpoints,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(environment);

        endpoints.MapGet(JsonRoute, (HttpContext context) =>
        {
            if (!IsRequestAllowed(environment, context.Connection.RemoteIpAddress))
                return Results.NotFound(new { decision = "TEACH_NODAL_SURFACE_LOCAL_DEV_ONLY" });
            ApplyReadOnlyHeaders(context.Response);
            return Results.Json(BuildSnapshot());
        });
        endpoints.MapGet(HtmlRoute, (HttpContext context) =>
        {
            if (!IsRequestAllowed(environment, context.Connection.RemoteIpAddress))
                return Results.NotFound();
            ApplyReadOnlyHeaders(context.Response);
            return Results.Content(TeachNodalLocalDevHtmlRenderer.Render(BuildSnapshot()), "text/html; charset=utf-8");
        });
        return endpoints;
    }

    public static TeachNodalLocalDevOperatorSnapshot BuildSnapshot()
    {
        var capture = TeachNodalLocalDevFixture.CreateCaptureReview();
        var demonstration = capture.Demonstration;
        var compilation = new TeachNodalCompilerV1().Compile(demonstration);
        var skill = compilation.Skill;
        var recipe = compilation.RecipeDraft;
        var memory = compilation.ProcessMemoryProjection;
        var injectionDecisions = compilation.ControlDecisions.Where(value => value.PossiblePromptInjection).ToArray();
        var steps = demonstration.Steps.Select(step => new TeachNodalLocalDevStepSnapshot(
            step.StepId,
            step.Action.IntentRedacted,
            $"{step.Action.Kind}/{step.Action.Operation}",
            step.Action.CapabilityId,
            step.Action.SemanticTargetRef,
            step.Action.TargetLabelRedacted,
            step.Action.TargetLabelSource.ToString(),
            step.Action.Parameters.Select(value => $"{value.Placeholder} → {value.ValueRef}").ToArray(),
            step.Action.SelectorAliasRefs,
            step.VerificationReport.Success,
            step.VerificationReport.FactsObserved.Count,
            step.Before.StateFingerprint,
            step.After.StateFingerprint,
            compilation.Findings.Any(value =>
                value.Contains("prompt injection", StringComparison.OrdinalIgnoreCase) &&
                value.Contains($"'{step.StepId}'", StringComparison.Ordinal)))).ToArray();
        var parametersAreReferences = demonstration.Steps.SelectMany(value => value.Action.Parameters).All(value =>
            value.SecretByReference
                ? value.ValueRef.StartsWith("secret-ref:", StringComparison.Ordinal) ||
                  value.ValueRef.StartsWith("secret://", StringComparison.Ordinal)
                : value.ValueRef.StartsWith("variable-ref:", StringComparison.Ordinal) ||
                  value.ValueRef.StartsWith("literal-ref:", StringComparison.Ordinal));
        var secretsExcluded = parametersAreReferences && demonstration.Steps.All(value =>
            value.Before.SecretsExcluded && value.After.SecretsExcluded);
        var compilationVerified = compilation.Decision is
            TeachNodalCompilationDecision.CompiledVerifiedSkill or
            TeachNodalCompilationDecision.CompiledVerifiedSkillRecipeNeedsReview;
        var captureReady = capture.State == TeachNodalCaptureSessionState.ReviewReady &&
                           capture.ExplicitOptInRecorded && capture.ApplicationScopeBound &&
                           capture.CanCompileVerifiedSkill && capture.ObservationCount == demonstration.Steps.Count &&
                           capture.PerStepApprovalsRequested == 0 && !capture.RawInputStored &&
                           !capture.GlobalHooksUsed && !capture.ExecutionAuthorityGranted;
        var accepted = captureReady && compilationVerified && compilation.FixtureOnly && secretsExcluded &&
                       skill is not null && skill.State == ExecutableSkillState.Verified &&
                       skill.Transitions.Count == demonstration.Steps.Count &&
                       recipe is not null && !recipe.LiveRuntimeEnabled &&
                       memory is not null && memory.Status == ProcessMemoryStatuses.Stable &&
                       steps.All(value => value.Verified && value.BeforeFingerprint != value.AfterFingerprint) &&
                       injectionDecisions.Length > 0 && injectionDecisions.All(value =>
                           !value.CanModifyMissionGoal && !value.CanExpandScope && !value.CanPublishExternally) &&
                       !compilation.LiveRecorderUsed && !compilation.MouseOrKeyboardHooksUsed &&
                       !compilation.RawScreenshotStored && !compilation.RawDomStored &&
                       !compilation.NetworkUsed && !compilation.ProductAuthorityGranted;
        var findings = capture.Findings.Concat(compilation.Findings)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        var evidenceRefs = capture.EvidenceRefs.Concat(compilation.EvidenceRefs)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        return new TeachNodalLocalDevOperatorSnapshot(
            Decision: accepted
                ? "GO_TEACH_NODAL_CAPTURE_PREP_LOCAL_DEV_SURFACE_READY"
                : "BLOCKED_TEACH_NODAL_CAPTURE_PREP_NOT_VERIFIED",
            Accepted: accepted,
            LocalDevOnly: true,
            ReadOnly: true,
            FixtureOnly: compilation.FixtureOnly,
            SecretsExcluded: secretsExcluded,
            CaptureState: capture.State.ToString(),
            CaptureOptInRecorded: capture.ExplicitOptInRecorded,
            CaptureApplicationScopeBound: capture.ApplicationScopeBound,
            CaptureCanCompileVerifiedSkill: capture.CanCompileVerifiedSkill,
            CaptureObservationCount: capture.ObservationCount,
            PerStepApprovalsRequested: capture.PerStepApprovalsRequested,
            CaptureRawInputStored: capture.RawInputStored,
            CaptureGlobalHooksUsed: capture.GlobalHooksUsed,
            CaptureExecutionAuthorityGranted: capture.ExecutionAuthorityGranted,
            DemonstrationId: demonstration.DemonstrationId,
            Title: demonstration.TitleRedacted,
            CompilationDecision: compilation.Decision.ToString(),
            CompilationCode: compilation.Code,
            RecipeId: recipe?.Recipe.Id ?? "not-created",
            RecipeReviewState: recipe?.ReviewState.ToString() ?? "Unavailable",
            RecipeLiveRuntimeEnabled: recipe?.LiveRuntimeEnabled ?? false,
            SkillId: skill?.SkillId ?? "not-created",
            SkillState: skill?.State.ToString() ?? "Unavailable",
            TransitionCount: skill?.Transitions.Count ?? 0,
            SkillFingerprint: skill?.SkillFingerprint ?? string.Empty,
            ProcessMemoryId: memory?.Id ?? "not-created",
            ProcessMemoryStatus: memory?.Status ?? "unavailable",
            Steps: steps,
            PromptInjectionObserved: injectionDecisions.Length > 0,
            PromptInjectionModifiedGoal: injectionDecisions.Any(value => value.CanModifyMissionGoal),
            PromptInjectionExpandedScope: injectionDecisions.Any(value => value.CanExpandScope),
            PromptInjectionPublishedExternally: injectionDecisions.Any(value => value.CanPublishExternally),
            Findings: findings,
            EvidenceRefs: evidenceRefs,
            DisabledActionId: DisabledActionId,
            DisabledActionState: "DISABLED_CAPTURE_ADAPTER_NOT_CONNECTED",
            RequiredOperatorSignal: RequiredOperatorSignal,
            NextSafeStep: "Connect an explicit application-scoped observation adapter. Global hooks and raw input remain closed.",
            LiveRecorderUsed: compilation.LiveRecorderUsed,
            MouseOrKeyboardHooksUsed: compilation.MouseOrKeyboardHooksUsed,
            RawScreenshotStored: compilation.RawScreenshotStored,
            RawDomStored: compilation.RawDomStored,
            NetworkUsed: compilation.NetworkUsed,
            ProductAuthorityGranted: compilation.ProductAuthorityGranted);
    }

    public static bool IsEnabled(IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        if (environment.IsDevelopment()) return true;
        var value = Environment.GetEnvironmentVariable(FeatureFlag);
        return value is not null && (value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                                    value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                                    value.Equals("yes", StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsRequestAllowed(IHostEnvironment environment, IPAddress? remoteAddress) =>
        IsEnabled(environment) && remoteAddress is not null && IPAddress.IsLoopback(remoteAddress);

    private static void ApplyReadOnlyHeaders(HttpResponse response)
    {
        response.Headers.CacheControl = "no-store";
        response.Headers.Pragma = "no-cache";
        response.Headers.XContentTypeOptions = "nosniff";
        response.Headers["Content-Security-Policy"] = "default-src 'none'; style-src 'unsafe-inline'; base-uri 'none'; form-action 'none'; frame-ancestors 'none'";
        response.Headers["Referrer-Policy"] = "no-referrer";
        response.Headers["X-Frame-Options"] = "DENY";
    }
}
