using System.Collections.Concurrent;
using System.Diagnostics;
using OneBrain.Core.Runtime;

namespace OneBrain.Core.Models;

public enum ModelAttemptFailureKind
{
    None,
    Timeout,
    Cancelled,
    Network,
    Authentication,
    PaymentRequired,
    Forbidden,
    RateLimited,
    ProviderUnavailable,
    ModelUnavailable,
    ContextTooLarge,
    CapabilityUnsupported,
    BudgetExceeded,
    PolicyIncompatible,
    ContentRejected,
    NonRecoverable
}

public sealed record ModelRouteRequest(
    string LogicalModel,
    ModelCapabilities RequiredCapabilities,
    int RequiredContextWindow,
    bool LocalOnly,
    bool CloudAllowed,
    ModelPrivacyClass MaximumPrivacyClass,
    decimal MaximumInputCostPerMillion,
    decimal MaximumOutputCostPerMillion,
    decimal RemainingBudget,
    IReadOnlyCollection<string> AllowedProviderIds,
    bool PreferSpeed,
    bool PreferQuality);

public sealed record ModelRouteCandidate(
    ModelProviderDefinition Provider,
    ModelDefinition Model,
    int Score,
    int FallbackDepth);

public sealed record ModelRoutePlan(
    string LogicalModel,
    IReadOnlyList<ModelRouteCandidate> Candidates,
    string? BlockedReason)
{
    public bool IsRoutable => Candidates.Count > 0;
}

public sealed record ModelExecutionRequest(
    string CorrelationId,
    string? MissionId,
    string? StepId,
    object? Payload = null);

public sealed record ModelAttemptContext(
    ModelRouteCandidate Candidate,
    SecretReference? CredentialReference,
    ReadOnlyMemory<byte> CredentialBytes,
    ModelExecutionRequest Request,
    int AttemptIndex);

public sealed record ModelAttemptResult(
    bool Success,
    ModelAttemptFailureKind FailureKind,
    int? HttpStatus,
    object? Response,
    long InputTokens,
    long OutputTokens,
    decimal EstimatedCost,
    string SafeMessage)
{
    public static ModelAttemptResult Succeeded(
        object? response = null,
        long inputTokens = 0,
        long outputTokens = 0,
        decimal estimatedCost = 0,
        string safeMessage = "ok") =>
        new(true, ModelAttemptFailureKind.None, null, response, inputTokens, outputTokens, estimatedCost, safeMessage);

    public static ModelAttemptResult Failed(
        ModelAttemptFailureKind kind,
        string safeMessage,
        int? httpStatus = null,
        decimal estimatedCost = 0) =>
        new(false, kind, httpStatus, null, 0, 0, estimatedCost, safeMessage);

    public static ModelAttemptResult FromHttpStatus(int statusCode, string safeMessage) =>
        Failed(MapHttpStatus(statusCode), safeMessage, statusCode);

    public static ModelAttemptFailureKind MapHttpStatus(int statusCode) => statusCode switch
    {
        401 => ModelAttemptFailureKind.Authentication,
        402 => ModelAttemptFailureKind.PaymentRequired,
        403 => ModelAttemptFailureKind.Forbidden,
        408 => ModelAttemptFailureKind.Timeout,
        409 => ModelAttemptFailureKind.ProviderUnavailable,
        413 => ModelAttemptFailureKind.ContextTooLarge,
        429 => ModelAttemptFailureKind.RateLimited,
        500 or 502 or 503 or 504 => ModelAttemptFailureKind.ProviderUnavailable,
        _ when statusCode >= 400 && statusCode < 500 => ModelAttemptFailureKind.ContentRejected,
        _ => ModelAttemptFailureKind.NonRecoverable
    };
}

public interface IModelAttemptExecutor
{
    ValueTask<ModelAttemptResult> ExecuteAsync(
        ModelAttemptContext context,
        CancellationToken cancellationToken);
}

public sealed record ModelFallbackPolicy(
    TimeSpan PerAttemptTimeout,
    TimeSpan TotalTimeout,
    int MaximumAttempts,
    int MaximumFallbackDepth,
    decimal MaximumTotalCost,
    int CircuitBreakerFailureThreshold,
    TimeSpan CircuitBreakerOpenDuration)
{
    public static ModelFallbackPolicy Default { get; } = new(
        PerAttemptTimeout: TimeSpan.FromSeconds(30),
        TotalTimeout: TimeSpan.FromMinutes(2),
        MaximumAttempts: 6,
        MaximumFallbackDepth: 5,
        MaximumTotalCost: 10m,
        CircuitBreakerFailureThreshold: 3,
        CircuitBreakerOpenDuration: TimeSpan.FromSeconds(30));
}

public sealed record ModelRoutingAttempt(
    string ProviderId,
    string ModelId,
    int AttemptIndex,
    int FallbackDepth,
    string CredentialSlot,
    bool Success,
    ModelAttemptFailureKind FailureKind,
    int? HttpStatus,
    TimeSpan Latency,
    decimal EstimatedCost,
    string SafeMessage);

public sealed record ModelRoutingResult(
    bool Success,
    bool Cancelled,
    bool RequiresOperatorIntervention,
    ModelRouteCandidate? SelectedCandidate,
    object? Response,
    IReadOnlyList<ModelRoutingAttempt> Attempts,
    decimal TotalEstimatedCost,
    string Decision,
    string SafeMessage);

public sealed class ModelCircuitBreaker
{
    private sealed record State(int Failures, DateTimeOffset? OpenUntil);

    private readonly ConcurrentDictionary<string, State> _state = new(StringComparer.OrdinalIgnoreCase);

    public bool IsOpen(string providerId, DateTimeOffset now) =>
        _state.TryGetValue(providerId, out var state) &&
        state.OpenUntil is { } openUntil &&
        openUntil > now;

    public void RecordSuccess(string providerId) => _state.TryRemove(providerId, out _);

    public void RecordFailure(string providerId, ModelFallbackPolicy policy, DateTimeOffset now)
    {
        _state.AddOrUpdate(
            providerId,
            _ => new State(1, null),
            (_, current) =>
            {
                var failures = current.Failures + 1;
                return failures >= policy.CircuitBreakerFailureThreshold
                    ? new State(failures, now.Add(policy.CircuitBreakerOpenDuration))
                    : new State(failures, current.OpenUntil);
            });
    }
}

public sealed class PolicyAwareModelRouter
{
    private readonly ModelCatalog _catalog;
    private readonly ISecretReferenceStore _secrets;
    private readonly IModelAttemptExecutor _executor;
    private readonly ModelCircuitBreaker _circuitBreaker;
    private readonly IRuntimeSignalObserver _observer;

    public PolicyAwareModelRouter(
        ModelCatalog catalog,
        ISecretReferenceStore secrets,
        IModelAttemptExecutor executor,
        ModelCircuitBreaker? circuitBreaker = null,
        IRuntimeSignalObserver? observer = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(secrets);
        ArgumentNullException.ThrowIfNull(executor);
        _catalog = catalog;
        _secrets = secrets;
        _executor = executor;
        _circuitBreaker = circuitBreaker ?? new ModelCircuitBreaker();
        _observer = observer ?? NullRuntimeSignalObserver.Instance;
    }

    public ModelRoutePlan Plan(ModelRouteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.LogicalModel);
        if (request.RequiredContextWindow < 0 || request.RemainingBudget < 0)
            throw new ArgumentOutOfRangeException(nameof(request));
        if (request.MaximumInputCostPerMillion < 0 || request.MaximumOutputCostPerMillion < 0)
            throw new ArgumentOutOfRangeException(nameof(request));

        var snapshot = _catalog.Snapshot();
        _catalog.TryGetAlias(request.LogicalModel, out var alias);
        var requiredCapabilities = request.RequiredCapabilities | (alias?.RequiredCapabilities ?? ModelCapabilities.None);
        HashSet<string>? allowedProviders = request.AllowedProviderIds.Count == 0
            ? null
            : new HashSet<string>(request.AllowedProviderIds, StringComparer.OrdinalIgnoreCase);
        var now = DateTimeOffset.UtcNow;
        var candidates = new List<ModelRouteCandidate>();

        foreach (var model in snapshot.Models)
        {
            var provider = snapshot.Providers.FirstOrDefault(value =>
                string.Equals(value.ProviderId, model.ProviderId, StringComparison.OrdinalIgnoreCase));
            if (provider is null)
                continue;
            if (!model.Available || model.DeprecatesAt is { } deprecatesAt && deprecatesAt <= now)
                continue;
            if (provider.State == ModelProviderState.Unavailable || _circuitBreaker.IsOpen(provider.ProviderId, now))
                continue;
            if (allowedProviders is not null && !allowedProviders.Contains(provider.ProviderId))
                continue;
            if (request.LocalOnly && provider.Kind != ModelProviderKind.Local)
                continue;
            if (!request.CloudAllowed && provider.Kind == ModelProviderKind.Cloud)
                continue;
            if ((int)model.PrivacyClass > (int)request.MaximumPrivacyClass ||
                (int)provider.PrivacyClass > (int)request.MaximumPrivacyClass)
                continue;
            if ((model.Capabilities & requiredCapabilities) != requiredCapabilities)
                continue;
            if (model.ContextWindow < request.RequiredContextWindow)
                continue;
            if (model.InputCostPerMillion > request.MaximumInputCostPerMillion ||
                model.OutputCostPerMillion > request.MaximumOutputCostPerMillion)
                continue;
            if (alias is not null &&
                (model.SpeedScore < alias.MinimumSpeedScore || model.QualityScore < alias.MinimumQualityScore))
                continue;

            candidates.Add(new ModelRouteCandidate(provider, model, Score(provider, model, alias, request), 0));
        }

        var ordered = candidates
            .OrderByDescending(value => value.Score)
            .ThenBy(value => value.Provider.ProviderId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(value => value.Model.ModelId, StringComparer.OrdinalIgnoreCase)
            .Select((value, index) => value with { FallbackDepth = index })
            .ToArray();

        return ordered.Length == 0
            ? new ModelRoutePlan(request.LogicalModel, Array.Empty<ModelRouteCandidate>(), "NO_COMPATIBLE_MODEL_WITHIN_AUTHORIZED_POLICY")
            : new ModelRoutePlan(request.LogicalModel, ordered, null);
    }

    public async ValueTask<ModelRoutingResult> ExecuteAsync(
        ModelRouteRequest routeRequest,
        ModelExecutionRequest executionRequest,
        ModelFallbackPolicy? policy = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(routeRequest);
        ArgumentNullException.ThrowIfNull(executionRequest);
        ArgumentException.ThrowIfNullOrWhiteSpace(executionRequest.CorrelationId);
        policy ??= ModelFallbackPolicy.Default;
        ValidatePolicy(policy);

        if (cancellationToken.IsCancellationRequested)
            return Cancelled(Array.Empty<ModelRoutingAttempt>(), 0);

        var plan = Plan(routeRequest);
        if (!plan.IsRoutable)
        {
            Emit("routing", "model_route_blocked", executionRequest, new[]
            {
                Pair("logical_model", routeRequest.LogicalModel),
                Pair("reason", plan.BlockedReason)
            });
            return new ModelRoutingResult(
                Success: false,
                Cancelled: false,
                RequiresOperatorIntervention: true,
                SelectedCandidate: null,
                Response: null,
                Attempts: Array.Empty<ModelRoutingAttempt>(),
                TotalEstimatedCost: 0,
                Decision: "BLOCKED_MODEL_ROUTE_POLICY",
                SafeMessage: plan.BlockedReason ?? "No compatible model is available.");
        }

        using var totalTimeout = new CancellationTokenSource(policy.TotalTimeout);
        using var linkedTotal = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, totalTimeout.Token);
        var attempts = new List<ModelRoutingAttempt>();
        var totalCost = 0m;
        var attemptIndex = 0;
        var lastFailure = ModelAttemptFailureKind.None;
        var budgetLimit = Math.Min(policy.MaximumTotalCost, routeRequest.RemainingBudget);

        foreach (var candidate in plan.Candidates.Take(policy.MaximumFallbackDepth + 1))
        {
            if (attemptIndex >= policy.MaximumAttempts || candidate.FallbackDepth > policy.MaximumFallbackDepth)
                break;
            if (_circuitBreaker.IsOpen(candidate.Provider.ProviderId, DateTimeOffset.UtcNow))
                continue;

            IReadOnlyList<SecretReference?> credentialReferences = candidate.Provider.RequiresCredential
                ? candidate.Provider.CredentialReferences.Cast<SecretReference?>().ToArray()
                : new SecretReference?[] { null };

            foreach (var credentialReference in credentialReferences)
            {
                if (attemptIndex >= policy.MaximumAttempts)
                    break;
                if (cancellationToken.IsCancellationRequested)
                    return Cancelled(attempts, totalCost);
                if (totalTimeout.IsCancellationRequested)
                    return TimedOut(attempts, totalCost);

                SecretLease? lease = null;
                if (candidate.Provider.RequiresCredential)
                {
                    try
                    {
                        lease = await _secrets.OpenAsync(credentialReference!, linkedTotal.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        return Cancelled(attempts, totalCost);
                    }
                    catch (OperationCanceledException)
                    {
                        return TimedOut(attempts, totalCost);
                    }

                    if (lease is null)
                    {
                        attempts.Add(CreateAttempt(
                            candidate,
                            attemptIndex++,
                            credentialReference,
                            false,
                            ModelAttemptFailureKind.Authentication,
                            null,
                            TimeSpan.Zero,
                            0,
                            "Credential reference is unavailable."));
                        lastFailure = ModelAttemptFailureKind.Authentication;
                        continue;
                    }
                }

                using (lease)
                using (var attemptTimeout = new CancellationTokenSource(policy.PerAttemptTimeout))
                using (var attemptLinked = CancellationTokenSource.CreateLinkedTokenSource(
                           cancellationToken,
                           totalTimeout.Token,
                           attemptTimeout.Token))
                {
                    var stopwatch = Stopwatch.StartNew();
                    Emit("model", "model_attempt_started", executionRequest, new[]
                    {
                        Pair("provider", candidate.Provider.ProviderId),
                        Pair("model", candidate.Model.ModelId),
                        Pair("attempt", attemptIndex.ToString()),
                        Pair("fallback_depth", candidate.FallbackDepth.ToString())
                    });

                    ModelAttemptResult result;
                    try
                    {
                        result = await _executor.ExecuteAsync(
                            new ModelAttemptContext(
                                candidate,
                                credentialReference,
                                lease?.Bytes ?? ReadOnlyMemory<byte>.Empty,
                                executionRequest,
                                attemptIndex),
                            attemptLinked.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        return Cancelled(attempts, totalCost);
                    }
                    catch (OperationCanceledException)
                    {
                        result = ModelAttemptResult.Failed(
                            ModelAttemptFailureKind.Timeout,
                            totalTimeout.IsCancellationRequested
                                ? "The total model routing timeout was reached."
                                : "The model attempt timed out.");
                    }
                    catch
                    {
                        result = ModelAttemptResult.Failed(
                            ModelAttemptFailureKind.NonRecoverable,
                            "The model attempt failed with an internal error.");
                    }

                    stopwatch.Stop();
                    var currentAttempt = attemptIndex++;
                    totalCost += result.EstimatedCost;
                    lastFailure = result.FailureKind;
                    attempts.Add(CreateAttempt(
                        candidate,
                        currentAttempt,
                        credentialReference,
                        result.Success,
                        result.FailureKind,
                        result.HttpStatus,
                        stopwatch.Elapsed,
                        result.EstimatedCost,
                        result.SafeMessage));

                    if (totalCost > budgetLimit)
                    {
                        return Exhausted(
                            attempts,
                            totalCost,
                            ModelAttemptFailureKind.BudgetExceeded,
                            true,
                            "BLOCKED_MODEL_ROUTE_BUDGET",
                            "The authorized model budget was exhausted.");
                    }

                    if (result.Success)
                    {
                        _circuitBreaker.RecordSuccess(candidate.Provider.ProviderId);
                        Emit("model", "model_attempt_succeeded", executionRequest, new[]
                        {
                            Pair("provider", candidate.Provider.ProviderId),
                            Pair("model", candidate.Model.ModelId),
                            Pair("attempt", currentAttempt.ToString()),
                            Pair("fallback_depth", candidate.FallbackDepth.ToString()),
                            Pair("latency_ms", stopwatch.ElapsedMilliseconds.ToString()),
                            Pair("cost", result.EstimatedCost.ToString("0.######"))
                        }, stopwatch.Elapsed);
                        return new ModelRoutingResult(
                            Success: true,
                            Cancelled: false,
                            RequiresOperatorIntervention: false,
                            SelectedCandidate: candidate,
                            Response: result.Response,
                            Attempts: attempts,
                            TotalEstimatedCost: totalCost,
                            Decision: candidate.FallbackDepth == 0 && attempts.Count == 1
                                ? "MODEL_ROUTE_PRIMARY_SUCCEEDED"
                                : "MODEL_ROUTE_FALLBACK_SUCCEEDED",
                            SafeMessage: candidate.FallbackDepth == 0 && attempts.Count == 1
                                ? "The primary compatible model succeeded."
                                : "The mission continued using an authorized fallback model.");
                    }

                    if (IsCircuitBreakingFailure(result.FailureKind))
                        _circuitBreaker.RecordFailure(candidate.Provider.ProviderId, policy, DateTimeOffset.UtcNow);

                    Emit("model", "model_attempt_failed", executionRequest, new[]
                    {
                        Pair("provider", candidate.Provider.ProviderId),
                        Pair("model", candidate.Model.ModelId),
                        Pair("attempt", currentAttempt.ToString()),
                        Pair("fallback_depth", candidate.FallbackDepth.ToString()),
                        Pair("failure", result.FailureKind.ToString()),
                        Pair("http_status", result.HttpStatus?.ToString()),
                        Pair("latency_ms", stopwatch.ElapsedMilliseconds.ToString())
                    }, stopwatch.Elapsed);

                    if (!IsRecoverable(result.FailureKind))
                    {
                        return Exhausted(
                            attempts,
                            totalCost,
                            result.FailureKind,
                            result.FailureKind is ModelAttemptFailureKind.PolicyIncompatible or ModelAttemptFailureKind.BudgetExceeded,
                            "MODEL_ROUTE_NON_RECOVERABLE_FAILURE",
                            result.SafeMessage);
                    }

                    if (totalTimeout.IsCancellationRequested)
                        return TimedOut(attempts, totalCost);
                }
            }
        }

        Emit("routing", "model_route_exhausted", executionRequest, new[]
        {
            Pair("logical_model", routeRequest.LogicalModel),
            Pair("attempts", attempts.Count.ToString()),
            Pair("last_failure", lastFailure.ToString())
        });
        return Exhausted(
            attempts,
            totalCost,
            lastFailure,
            true,
            "MODEL_ROUTE_FALLBACK_EXHAUSTED",
            "All compatible authorized model routes were exhausted.");
    }

    private void Emit(
        string category,
        string name,
        ModelExecutionRequest request,
        IEnumerable<KeyValuePair<string, string?>> dimensions,
        TimeSpan? duration = null)
    {
        _observer.TryObserve(RuntimeSignal.Create(
            category,
            name,
            request.CorrelationId,
            request.MissionId,
            request.StepId,
            duration: duration,
            dimensions: dimensions));
    }

    private static int Score(
        ModelProviderDefinition provider,
        ModelDefinition model,
        LogicalModelAlias? alias,
        ModelRouteRequest request)
    {
        var score = provider.HealthScore * 2;
        score += request.PreferSpeed ? model.SpeedScore * 3 : model.SpeedScore;
        score += request.PreferQuality ? model.QualityScore * 3 : model.QualityScore;
        if (alias?.PreferLocal == true && provider.Kind == ModelProviderKind.Local)
            score += 100;
        if (provider.State == ModelProviderState.Degraded)
            score -= 75;
        score -= (int)Math.Min(100m, model.InputCostPerMillion + model.OutputCostPerMillion);
        return score;
    }

    private static ModelRoutingAttempt CreateAttempt(
        ModelRouteCandidate candidate,
        int attemptIndex,
        SecretReference? credentialReference,
        bool success,
        ModelAttemptFailureKind failureKind,
        int? httpStatus,
        TimeSpan latency,
        decimal cost,
        string message) =>
        new(
            ProviderId: candidate.Provider.ProviderId,
            ModelId: candidate.Model.ModelId,
            AttemptIndex: attemptIndex,
            FallbackDepth: candidate.FallbackDepth,
            CredentialSlot: credentialReference is null ? "none" : "configured",
            Success: success,
            FailureKind: failureKind,
            HttpStatus: httpStatus,
            Latency: latency,
            EstimatedCost: cost,
            SafeMessage: SafeRuntimeText.Sanitize(message));

    private static ModelRoutingResult Cancelled(IReadOnlyList<ModelRoutingAttempt> attempts, decimal totalCost) =>
        new(
            Success: false,
            Cancelled: true,
            RequiresOperatorIntervention: false,
            SelectedCandidate: null,
            Response: null,
            Attempts: attempts,
            TotalEstimatedCost: totalCost,
            Decision: "CANCELLED_BY_OPERATOR",
            SafeMessage: "The model request was cancelled.");

    private static ModelRoutingResult TimedOut(IReadOnlyList<ModelRoutingAttempt> attempts, decimal totalCost) =>
        Exhausted(
            attempts,
            totalCost,
            ModelAttemptFailureKind.Timeout,
            false,
            "MODEL_ROUTE_TOTAL_TIMEOUT",
            "The total model routing timeout was reached.");

    private static ModelRoutingResult Exhausted(
        IReadOnlyList<ModelRoutingAttempt> attempts,
        decimal totalCost,
        ModelAttemptFailureKind failure,
        bool requiresOperator,
        string decision,
        string message) =>
        new(
            Success: false,
            Cancelled: failure == ModelAttemptFailureKind.Cancelled,
            RequiresOperatorIntervention: requiresOperator,
            SelectedCandidate: null,
            Response: null,
            Attempts: attempts,
            TotalEstimatedCost: totalCost,
            Decision: decision,
            SafeMessage: SafeRuntimeText.Sanitize(message));

    private static bool IsRecoverable(ModelAttemptFailureKind failure) => failure is
        ModelAttemptFailureKind.Timeout or
        ModelAttemptFailureKind.Network or
        ModelAttemptFailureKind.Authentication or
        ModelAttemptFailureKind.PaymentRequired or
        ModelAttemptFailureKind.Forbidden or
        ModelAttemptFailureKind.RateLimited or
        ModelAttemptFailureKind.ProviderUnavailable or
        ModelAttemptFailureKind.ModelUnavailable or
        ModelAttemptFailureKind.ContextTooLarge or
        ModelAttemptFailureKind.CapabilityUnsupported;

    private static bool IsCircuitBreakingFailure(ModelAttemptFailureKind failure) => failure is
        ModelAttemptFailureKind.Timeout or
        ModelAttemptFailureKind.Network or
        ModelAttemptFailureKind.RateLimited or
        ModelAttemptFailureKind.ProviderUnavailable;

    private static void ValidatePolicy(ModelFallbackPolicy policy)
    {
        if (policy.PerAttemptTimeout <= TimeSpan.Zero || policy.TotalTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(policy));
        if (policy.MaximumAttempts < 1 || policy.MaximumFallbackDepth < 0)
            throw new ArgumentOutOfRangeException(nameof(policy));
        if (policy.MaximumTotalCost < 0 ||
            policy.CircuitBreakerFailureThreshold < 1 ||
            policy.CircuitBreakerOpenDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(policy));
    }

    private static KeyValuePair<string, string?> Pair(string key, string? value) => new(key, value);
}
