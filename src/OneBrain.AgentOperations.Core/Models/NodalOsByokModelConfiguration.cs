using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;
using OneBrain.Core.History;
using OneBrain.Core.Models;
using OneBrain.Core.Runtime;

namespace OneBrain.AgentOperations.Core.Models;

public enum NodalOsByokProviderType
{
    OpenAiCompatibleCloud,
    OpenAiCompatibleLocal
}

public enum NodalOsByokConfigurationState
{
    NotConfigured,
    ReadyForConnectionTest,
    Connected,
    ConnectionFailed,
    ConfigurationInvalid,
    CorruptMetadata,
    FailedClosed
}

public sealed record NodalOsByokProviderConfiguration(
    string SlotId,
    string ProviderId,
    string DisplayNameRedacted,
    NodalOsByokProviderType ProviderType,
    string Endpoint,
    string ModelId,
    bool RequiresCredential,
    SecretReference? CredentialReference,
    ModelPrivacyClass PrivacyClass,
    decimal InputCostPerMillion,
    decimal OutputCostPerMillion,
    int ContextWindow,
    ModelCapabilities Capabilities,
    int Priority);

public sealed record NodalOsByokModelConfigurationRequest(
    string PrimaryProviderId,
    string PrimaryDisplayName,
    NodalOsByokProviderType PrimaryProviderType,
    string PrimaryEndpoint,
    string PrimaryModelId,
    string? PrimaryApiKey,
    bool EnableFallback,
    string? FallbackProviderId,
    string? FallbackDisplayName,
    NodalOsByokProviderType? FallbackProviderType,
    string? FallbackEndpoint,
    string? FallbackModelId,
    string? FallbackApiKey,
    bool CloudAllowed,
    decimal MaximumTotalCostUsd,
    int PerAttemptTimeoutSeconds,
    decimal PrimaryInputCostPerMillion = 0,
    decimal PrimaryOutputCostPerMillion = 0,
    decimal FallbackInputCostPerMillion = 0,
    decimal FallbackOutputCostPerMillion = 0);

public sealed record NodalOsPersistedByokConnectionTest(
    bool Success,
    bool Cancelled,
    bool FallbackApplied,
    string Decision,
    string? SelectedProviderId,
    string? SelectedModelId,
    int AttemptCount,
    decimal TotalEstimatedCost,
    string? ResponseSha256,
    IReadOnlyList<string> AttemptSummaries,
    IReadOnlyList<NodalOsEvidenceBridgeRef> EvidenceRefs,
    IReadOnlyList<NodalOsCoreTimelineProjection> Timeline,
    DateTimeOffset TestedAt);

public sealed record NodalOsPersistedByokModelConfiguration(
    int SchemaVersion,
    string ConfigurationId,
    string LogicalModel,
    NodalOsByokProviderConfiguration Primary,
    NodalOsByokProviderConfiguration? Fallback,
    bool CloudAllowed,
    ModelPrivacyClass MaximumPrivacyClass,
    decimal MaximumTotalCostUsd,
    int PerAttemptTimeoutSeconds,
    NodalOsPersistedByokConnectionTest? LastConnectionTest,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record NodalOsByokProviderSnapshot(
    string SlotId,
    string ProviderId,
    string DisplayNameRedacted,
    string ProviderType,
    string EndpointRedacted,
    string ModelId,
    bool Local,
    bool CredentialConfigured,
    string? CredentialStoreId,
    string PrivacyClass,
    decimal InputCostPerMillion,
    decimal OutputCostPerMillion,
    int Priority);

public sealed record NodalOsByokModelConfigurationSnapshot(
    string Decision,
    bool Accepted,
    NodalOsByokConfigurationState State,
    bool Configured,
    bool Persisted,
    bool Rehydrated,
    string LogicalModel,
    NodalOsByokProviderSnapshot? Primary,
    NodalOsByokProviderSnapshot? Fallback,
    bool CloudAllowed,
    string MaximumPrivacyClass,
    decimal MaximumTotalCostUsd,
    int PerAttemptTimeoutSeconds,
    bool ConnectionTested,
    bool Connected,
    bool Cancelled,
    bool FallbackApplied,
    string? SelectedProviderId,
    string? SelectedModelId,
    int AttemptCount,
    decimal TotalEstimatedCost,
    string? ResponseSha256,
    IReadOnlyList<string> AttemptSummaries,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<NodalOsCoreTimelineProjection> Timeline,
    IReadOnlyList<string> Blockers,
    DateTimeOffset? TestedAt,
    bool RealProviderCallAttempted,
    bool NetworkUsed,
    bool SecretsExcluded,
    bool ProductAuthorityGranted);

public sealed record NodalOsOpenAiCompatibleProbe(string Prompt, int MaximumOutputTokens = 8);

public sealed record NodalOsOpenAiCompatibleResponseSummary(
    string ResponseSha256,
    int ContentLength,
    long InputTokens,
    long OutputTokens);

public sealed class NodalOsOpenAiCompatibleAttemptExecutor : IModelAttemptExecutor
{
    private const int MaximumResponseBytes = 1024 * 1024;
    private readonly HttpClient client;

    public NodalOsOpenAiCompatibleAttemptExecutor(HttpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        this.client = client;
    }

    public async ValueTask<ModelAttemptResult> ExecuteAsync(
        ModelAttemptContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Request.Payload is not NodalOsOpenAiCompatibleProbe probe || string.IsNullOrWhiteSpace(probe.Prompt))
        {
            return ModelAttemptResult.Failed(
                ModelAttemptFailureKind.NonRecoverable,
                "The OpenAI-compatible request payload is invalid.");
        }

        var endpoint = ChatCompletionsEndpoint(context.Candidate.Provider.Endpoint);
        if (endpoint is null)
        {
            return ModelAttemptResult.Failed(
                ModelAttemptFailureKind.PolicyIncompatible,
                "The configured provider endpoint is invalid.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!context.CredentialBytes.IsEmpty)
        {
            var credential = Encoding.UTF8.GetString(context.CredentialBytes.Span);
            if (string.IsNullOrWhiteSpace(credential) || credential.Any(char.IsControl))
            {
                return ModelAttemptResult.Failed(
                    ModelAttemptFailureKind.Authentication,
                    "The configured credential could not be opened safely.");
            }
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credential);
        }

        var payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            model = context.Candidate.Model.UpstreamModelId,
            messages = new[] { new { role = "user", content = SafeRuntimeText.Sanitize(probe.Prompt, 240) } },
            max_tokens = Math.Clamp(probe.MaximumOutputTokens, 1, 32),
            temperature = 0
        });
        try
        {
            request.Content = new ByteArrayContent(payload);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            using var response = await client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return ModelAttemptResult.FromHttpStatus(
                    (int)response.StatusCode,
                    SafeHttpFailure(response.StatusCode));
            }

            var bytes = await ReadBoundedAsync(response.Content, cancellationToken).ConfigureAwait(false);
            try
            {
                using var document = JsonDocument.Parse(bytes);
                var root = document.RootElement;
                var content = ReadContent(root);
                if (string.IsNullOrWhiteSpace(content))
                {
                    return ModelAttemptResult.Failed(
                        ModelAttemptFailureKind.NonRecoverable,
                        "The provider returned no compatible message content.");
                }

                var contentBytes = Encoding.UTF8.GetBytes(content);
                try
                {
                    var inputTokens = ReadUsage(root, "prompt_tokens");
                    var outputTokens = ReadUsage(root, "completion_tokens");
                    var estimatedCost = EstimateCost(context.Candidate.Model, inputTokens, outputTokens);
                    return ModelAttemptResult.Succeeded(
                        new NodalOsOpenAiCompatibleResponseSummary(
                            Sha256(contentBytes),
                            contentBytes.Length,
                            inputTokens,
                            outputTokens),
                        inputTokens,
                        outputTokens,
                        estimatedCost,
                        "The provider returned a compatible chat response.");
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(contentBytes);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(bytes);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            return ModelAttemptResult.Failed(
                ModelAttemptFailureKind.Network,
                "The provider connection failed without exposing transport details.");
        }
        catch (JsonException)
        {
            return ModelAttemptResult.Failed(
                ModelAttemptFailureKind.NonRecoverable,
                "The provider returned an incompatible JSON response.");
        }
        catch (InvalidDataException)
        {
            return ModelAttemptResult.Failed(
                ModelAttemptFailureKind.NonRecoverable,
                "The provider response exceeded the bounded connection-test limit.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(payload);
        }
    }

    private static Uri? ChatCompletionsEndpoint(Uri endpoint)
    {
        try
        {
            var value = endpoint.AbsoluteUri.TrimEnd('/') + "/chat/completions";
            return Uri.TryCreate(value, UriKind.Absolute, out var result) ? result : null;
        }
        catch
        {
            return null;
        }
    }

    private static async Task<byte[]> ReadBoundedAsync(HttpContent content, CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength is > MaximumResponseBytes)
            throw new InvalidDataException("Response exceeds bounded limit.");
        await using var source = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var destination = new MemoryStream();
        var buffer = new byte[16 * 1024];
        try
        {
            while (true)
            {
                var read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                    break;
                if (destination.Length + read > MaximumResponseBytes)
                    throw new InvalidDataException("Response exceeds bounded limit.");
                destination.Write(buffer, 0, read);
            }
            return destination.ToArray();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(buffer);
        }
    }

    private static string? ReadContent(JsonElement root)
    {
        if (!root.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array || choices.GetArrayLength() == 0)
            return null;
        var first = choices[0];
        if (!first.TryGetProperty("message", out var message) || !message.TryGetProperty("content", out var content))
            return null;
        return content.ValueKind == JsonValueKind.String ? content.GetString() : null;
    }

    private static long ReadUsage(JsonElement root, string name)
    {
        if (!root.TryGetProperty("usage", out var usage) || usage.ValueKind != JsonValueKind.Object ||
            !usage.TryGetProperty(name, out var value) || !value.TryGetInt64(out var parsed) || parsed < 0)
            return 0;
        return parsed;
    }

    private static decimal EstimateCost(ModelDefinition model, long inputTokens, long outputTokens) =>
        decimal.Round(
            (inputTokens / 1_000_000m * model.InputCostPerMillion) +
            (outputTokens / 1_000_000m * model.OutputCostPerMillion),
            8,
            MidpointRounding.AwayFromZero);

    private static string SafeHttpFailure(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.Unauthorized => "The provider rejected the configured credential.",
        HttpStatusCode.PaymentRequired => "The provider reported a billing or credit restriction.",
        HttpStatusCode.Forbidden => "The provider rejected the configured access scope.",
        HttpStatusCode.RequestTimeout => "The provider request timed out.",
        HttpStatusCode.TooManyRequests => "The provider rate limit was reached.",
        HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout =>
            "The provider is temporarily unavailable.",
        _ => "The provider rejected the bounded connection test."
    };

    private static string Sha256(byte[] value) =>
        Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant();
}

public sealed class NodalOsByokModelConfigurationService
{
    public const int CurrentSchemaVersion = 1;
    public const string LogicalModelAlias = "standard_task";

    private const int MaximumMetadataBytes = 2 * 1024 * 1024;
    private const int MaximumApiKeyCharacters = 8192;
    private readonly string metadataFilePath;
    private readonly ISecretReferenceStore secretStore;
    private readonly HttpClient httpClient;
    private readonly NodalOsRedactionService redaction = new();
    private readonly NodalOsCoreRuntimeValidator coreValidator = new();

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public NodalOsByokModelConfigurationService(
        string metadataFilePath,
        ISecretReferenceStore secretStore,
        HttpClient? httpClient = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(metadataFilePath);
        ArgumentNullException.ThrowIfNull(secretStore);
        this.metadataFilePath = Path.GetFullPath(metadataFilePath);
        this.secretStore = secretStore;
        this.httpClient = httpClient ?? new HttpClient();
    }

    public string MetadataFilePath => metadataFilePath;

    public async ValueTask<NodalOsByokModelConfigurationSnapshot> ConfigureAsync(
        NodalOsByokModelConfigurationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var existing = await TryReadDocumentAsync(cancellationToken).ConfigureAwait(false);
        var blockers = new List<string>();
        var primary = await BuildProviderAsync(
                "primary",
                request.PrimaryProviderId,
                request.PrimaryDisplayName,
                request.PrimaryProviderType,
                request.PrimaryEndpoint,
                request.PrimaryModelId,
                request.PrimaryApiKey,
                request.PrimaryInputCostPerMillion,
                request.PrimaryOutputCostPerMillion,
                priority: 0,
                existing?.Primary,
                blockers,
                cancellationToken)
            .ConfigureAwait(false);

        NodalOsByokProviderConfiguration? fallback = null;
        if (request.EnableFallback)
        {
            if (request.FallbackProviderType is null)
            {
                blockers.Add("Fallback provider type is required when fallback is enabled.");
            }
            else
            {
                fallback = await BuildProviderAsync(
                        "fallback",
                        request.FallbackProviderId,
                        request.FallbackDisplayName,
                        request.FallbackProviderType.Value,
                        request.FallbackEndpoint,
                        request.FallbackModelId,
                        request.FallbackApiKey,
                        request.FallbackInputCostPerMillion,
                        request.FallbackOutputCostPerMillion,
                        priority: 1,
                        existing?.Fallback,
                        blockers,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        if (request.MaximumTotalCostUsd is < 0 or > 1000)
            blockers.Add("Maximum connection-test cost must be between 0 and 1000 USD.");
        if (request.PerAttemptTimeoutSeconds is < 1 or > 120)
            blockers.Add("Per-attempt timeout must be between 1 and 120 seconds.");
        if (primary is not null && primary.ProviderType == NodalOsByokProviderType.OpenAiCompatibleCloud && !request.CloudAllowed)
            blockers.Add("Cloud access must be explicitly allowed for a cloud primary provider.");
        if (fallback is not null && fallback.ProviderType == NodalOsByokProviderType.OpenAiCompatibleCloud && !request.CloudAllowed)
            blockers.Add("Cloud access must be explicitly allowed for a cloud fallback provider.");
        if (primary is not null && fallback is not null &&
            string.Equals(primary.ProviderId, fallback.ProviderId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(primary.ModelId, fallback.ModelId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(primary.Endpoint, fallback.Endpoint, StringComparison.OrdinalIgnoreCase))
            blockers.Add("Fallback must differ from the primary provider route.");

        if (blockers.Count > 0 || primary is null)
        {
            return Failure(
                "BLOCKED_BYOK_MODEL_CONFIGURATION_INVALID",
                NodalOsByokConfigurationState.ConfigurationInvalid,
                blockers.DefaultIfEmpty("Model configuration is invalid.").ToArray());
        }

        if (!request.EnableFallback && existing?.Fallback?.CredentialReference is { } retiredFallbackReference)
            await BestEffortDeleteAsync(retiredFallbackReference).ConfigureAwait(false);

        var now = DateTimeOffset.UtcNow;
        var document = new NodalOsPersistedByokModelConfiguration(
            CurrentSchemaVersion,
            ConfigurationId(primary, fallback),
            LogicalModelAlias,
            primary,
            fallback,
            request.CloudAllowed,
            request.CloudAllowed ? ModelPrivacyClass.AuthorizedCloud : ModelPrivacyClass.LocalOnly,
            request.MaximumTotalCostUsd,
            request.PerAttemptTimeoutSeconds,
            LastConnectionTest: null,
            existing?.CreatedAt ?? now,
            now);

        try
        {
            await WriteDocumentAsync(document, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return Failure(
                "BLOCKED_BYOK_MODEL_CONFIGURATION_PERSISTENCE_FAILED",
                NodalOsByokConfigurationState.FailedClosed,
                ["Model configuration persistence failed closed."]);
        }

        return Snapshot(
            document,
            "GO_BYOK_MODEL_CONFIGURATION_READY_FOR_TEST",
            NodalOsByokConfigurationState.ReadyForConnectionTest,
            accepted: true,
            rehydrated: false,
            blockers: []);
    }

    public async ValueTask<NodalOsByokModelConfigurationSnapshot> TestConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        var document = await TryReadDocumentAsync(cancellationToken).ConfigureAwait(false);
        if (document is null || !ValidateDocument(document))
        {
            return Failure(
                "BLOCKED_BYOK_MODEL_CONNECTION_CONFIGURATION_REQUIRED",
                NodalOsByokConfigurationState.NotConfigured,
                ["A valid persisted model configuration is required before testing a provider call."]);
        }

        var catalog = BuildCatalog(document);
        var router = new PolicyAwareModelRouter(
            catalog,
            secretStore,
            new NodalOsOpenAiCompatibleAttemptExecutor(httpClient));
        var route = new ModelRouteRequest(
            document.LogicalModel,
            ModelCapabilities.Chat,
            RequiredContextWindow: 1,
            LocalOnly: !document.CloudAllowed,
            CloudAllowed: document.CloudAllowed,
            document.MaximumPrivacyClass,
            MaximumInputCostPerMillion: Math.Max(document.Primary.InputCostPerMillion, document.Fallback?.InputCostPerMillion ?? 0),
            MaximumOutputCostPerMillion: Math.Max(document.Primary.OutputCostPerMillion, document.Fallback?.OutputCostPerMillion ?? 0),
            RemainingBudget: document.MaximumTotalCostUsd,
            AllowedProviderIds: Providers(document).Select(value => value.ProviderId).ToArray(),
            PreferSpeed: true,
            PreferQuality: false);
        var providerCount = Providers(document).Count;
        var fallbackPolicy = new ModelFallbackPolicy(
            TimeSpan.FromSeconds(document.PerAttemptTimeoutSeconds),
            TimeSpan.FromSeconds(document.PerAttemptTimeoutSeconds * providerCount + 5),
            MaximumAttempts: providerCount,
            MaximumFallbackDepth: Math.Max(0, providerCount - 1),
            MaximumTotalCost: document.MaximumTotalCostUsd,
            CircuitBreakerFailureThreshold: 2,
            CircuitBreakerOpenDuration: TimeSpan.FromSeconds(30));
        var correlationId = $"byok-connection-{Guid.NewGuid():N}";
        var stopwatch = Stopwatch.StartNew();
        var result = await router.ExecuteAsync(
                route,
                new ModelExecutionRequest(
                    correlationId,
                    MissionId: null,
                    StepId: "byok-model-connection-test",
                    new NodalOsOpenAiCompatibleProbe("Reply with a short acknowledgement for the NODAL OS connection test.")),
                fallbackPolicy,
                cancellationToken)
            .ConfigureAwait(false);
        stopwatch.Stop();

        var response = result.Response as NodalOsOpenAiCompatibleResponseSummary;
        var fallbackApplied = result.Success && result.SelectedCandidate?.FallbackDepth > 0;
        var attemptSummaries = result.Attempts.Select(attempt =>
                $"{attempt.AttemptIndex}:{attempt.ProviderId}:{attempt.ModelId}:{attempt.FailureKind}:{attempt.Latency.TotalMilliseconds:0}ms")
            .Select(value => SafeRuntimeText.Sanitize(value, 240))
            .ToArray();
        var evidence = result.Success && response is not null
            ? new[]
            {
                Evidence(
                    $"evidence:byok-model-connection:{document.ConfigurationId}:{response.ResponseSha256}",
                    "byok-model-connection-verification",
                    response.ResponseSha256,
                    $"byok-model-connection:{document.ConfigurationId}",
                    "A configured provider returned a bounded OpenAI-compatible response under the persisted privacy and cost policy.")
            }
            : Array.Empty<NodalOsEvidenceBridgeRef>();
        var timeline = BuildTimeline(
            document,
            result,
            fallbackApplied,
            evidence,
            stopwatch.Elapsed);
        var tested = new NodalOsPersistedByokConnectionTest(
            result.Success,
            result.Cancelled,
            fallbackApplied,
            result.Decision,
            result.SelectedCandidate?.Provider.ProviderId,
            result.SelectedCandidate?.Model.UpstreamModelId,
            result.Attempts.Count,
            result.TotalEstimatedCost,
            response?.ResponseSha256,
            attemptSummaries,
            evidence,
            timeline,
            DateTimeOffset.UtcNow);
        var updated = document with { LastConnectionTest = tested, UpdatedAt = DateTimeOffset.UtcNow };

        if (!result.Cancelled)
        {
            try
            {
                await WriteDocumentAsync(updated, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return Failure(
                    "BLOCKED_BYOK_MODEL_CONNECTION_RESULT_PERSISTENCE_FAILED",
                    NodalOsByokConfigurationState.FailedClosed,
                    ["The connection-test result could not be persisted safely."]);
            }
        }

        if (result.Cancelled)
        {
            return Snapshot(
                document,
                "CANCELLED_BY_OPERATOR",
                NodalOsByokConfigurationState.ReadyForConnectionTest,
                accepted: false,
                rehydrated: false,
                blockers: ["The operator cancelled the provider call; no fallback continued."],
                transientTest: tested);
        }

        return Snapshot(
            updated,
            result.Success ? "GO_BYOK_MODEL_CONNECTION_VERIFIED" : result.Decision,
            result.Success ? NodalOsByokConfigurationState.Connected : NodalOsByokConfigurationState.ConnectionFailed,
            accepted: result.Success,
            rehydrated: false,
            blockers: result.Success ? [] : [result.SafeMessage]);
    }

    public async ValueTask<NodalOsByokModelConfigurationSnapshot> GetCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(metadataFilePath))
            return Empty(appConfigurationMutated: false);
        NodalOsPersistedByokModelConfiguration? document;
        try
        {
            document = await ReadDocumentAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return Failure(
                "BLOCKED_BYOK_MODEL_CONFIGURATION_METADATA_CORRUPT",
                NodalOsByokConfigurationState.CorruptMetadata,
                ["Persisted model configuration is unavailable or invalid."],
                persisted: true);
        }

        if (document is null || !ValidateDocument(document))
        {
            return Failure(
                "BLOCKED_BYOK_MODEL_CONFIGURATION_METADATA_CORRUPT",
                NodalOsByokConfigurationState.CorruptMetadata,
                ["Persisted model configuration failed canonical validation."],
                persisted: true);
        }

        var state = document.LastConnectionTest is null
            ? NodalOsByokConfigurationState.ReadyForConnectionTest
            : document.LastConnectionTest.Success
                ? NodalOsByokConfigurationState.Connected
                : NodalOsByokConfigurationState.ConnectionFailed;
        var decision = document.LastConnectionTest is null
            ? "GO_BYOK_MODEL_CONFIGURATION_REHYDRATED"
            : document.LastConnectionTest.Success
                ? "GO_BYOK_MODEL_CONNECTION_REHYDRATED"
                : "BYOK_MODEL_CONNECTION_FAILURE_REHYDRATED";
        return Snapshot(document, decision, state, accepted: true, rehydrated: true, blockers: []);
    }

    public async ValueTask<NodalOsByokModelConfigurationSnapshot> ClearAsync(
        CancellationToken cancellationToken = default)
    {
        var existing = await TryReadDocumentAsync(cancellationToken).ConfigureAwait(false);
        foreach (var reference in Providers(existing).Select(value => value.CredentialReference).OfType<SecretReference>())
            await BestEffortDeleteAsync(reference).ConfigureAwait(false);
        try
        {
            if (File.Exists(metadataFilePath))
                File.Delete(metadataFilePath);
        }
        catch
        {
            return Failure(
                "BLOCKED_BYOK_MODEL_CONFIGURATION_CLEAR_FAILED",
                NodalOsByokConfigurationState.FailedClosed,
                ["Model configuration could not be cleared safely."],
                persisted: File.Exists(metadataFilePath));
        }
        return Empty(appConfigurationMutated: true);
    }

    private async ValueTask<NodalOsByokProviderConfiguration?> BuildProviderAsync(
        string slotId,
        string? providerId,
        string? displayName,
        NodalOsByokProviderType providerType,
        string? endpointText,
        string? modelId,
        string? apiKey,
        decimal inputCost,
        decimal outputCost,
        int priority,
        NodalOsByokProviderConfiguration? existing,
        List<string> blockers,
        CancellationToken cancellationToken)
    {
        var normalizedProviderId = NormalizeIdentifier(providerId, 80);
        if (normalizedProviderId is null)
            blockers.Add($"{slotId} provider id is invalid.");
        var normalizedDisplayName = NormalizeDisplayName(displayName, normalizedProviderId ?? slotId);
        var endpoint = NormalizeEndpoint(endpointText, providerType);
        if (endpoint is null)
            blockers.Add(providerType == NodalOsByokProviderType.OpenAiCompatibleCloud
                ? $"{slotId} cloud endpoint must be an HTTPS URL without credentials, query or fragment."
                : $"{slotId} local endpoint must be an HTTP(S) loopback URL without credentials, query or fragment.");
        var normalizedModelId = NormalizeModelId(modelId);
        if (normalizedModelId is null)
            blockers.Add($"{slotId} model id is invalid.");
        if (inputCost is < 0 or > 100000 || outputCost is < 0 or > 100000)
            blockers.Add($"{slotId} model costs must be non-negative and bounded.");
        if (!string.IsNullOrEmpty(apiKey) && apiKey.Length > MaximumApiKeyCharacters)
            blockers.Add($"{slotId} credential exceeds the bounded input limit.");
        if (normalizedProviderId is null || endpoint is null || normalizedModelId is null)
            return null;

        var cloud = providerType == NodalOsByokProviderType.OpenAiCompatibleCloud;
        var existingReference = existing is not null &&
            string.Equals(existing.SlotId, slotId, StringComparison.Ordinal) &&
            string.Equals(existing.ProviderId, normalizedProviderId, StringComparison.OrdinalIgnoreCase)
                ? existing.CredentialReference
                : null;
        SecretReference? credentialReference = existingReference;
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            var keyBytes = Encoding.UTF8.GetBytes(apiKey.Trim());
            try
            {
                credentialReference = await secretStore.StoreAsync(
                        $"nodal-os:byok:{slotId}:{normalizedProviderId}",
                        keyBytes,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(keyBytes);
            }
        }
        if (cloud && credentialReference is null)
            blockers.Add($"{slotId} cloud provider requires a credential.");
        if (existing?.CredentialReference is { } replaced && credentialReference is not null && replaced != credentialReference)
            await BestEffortDeleteAsync(replaced).ConfigureAwait(false);

        return new NodalOsByokProviderConfiguration(
            slotId,
            normalizedProviderId,
            normalizedDisplayName,
            providerType,
            endpoint.AbsoluteUri.TrimEnd('/'),
            normalizedModelId,
            RequiresCredential: credentialReference is not null,
            credentialReference,
            cloud ? ModelPrivacyClass.AuthorizedCloud : ModelPrivacyClass.LocalOnly,
            inputCost,
            outputCost,
            ContextWindow: 128_000,
            Capabilities: ModelCapabilities.Chat | ModelCapabilities.StructuredOutput | ModelCapabilities.Streaming,
            priority);
    }

    private static ModelCatalog BuildCatalog(NodalOsPersistedByokModelConfiguration document)
    {
        var providers = Providers(document).Select(provider => new ModelProviderDefinition(
            provider.ProviderId,
            provider.DisplayNameRedacted,
            provider.ProviderType == NodalOsByokProviderType.OpenAiCompatibleCloud ? ModelProviderKind.Cloud : ModelProviderKind.Local,
            new Uri(provider.Endpoint, UriKind.Absolute),
            provider.RequiresCredential,
            provider.CredentialReference is null ? [] : [provider.CredentialReference],
            ModelProviderState.Ready,
            HealthScore: Math.Max(1, 100 - provider.Priority * 10),
            provider.PrivacyClass,
            provider.ProviderType == NodalOsByokProviderType.OpenAiCompatibleCloud ? ["configured-cloud"] : ["local-loopback"])).ToArray();
        var models = Providers(document).Select(provider => new ModelDefinition(
            ModelId: provider.SlotId,
            ProviderId: provider.ProviderId,
            UpstreamModelId: provider.ModelId,
            provider.ContextWindow,
            provider.Capabilities,
            provider.PrivacyClass,
            provider.InputCostPerMillion,
            provider.OutputCostPerMillion,
            SpeedScore: Math.Max(1, 100 - provider.Priority * 10),
            QualityScore: Math.Max(1, 90 - provider.Priority * 5),
            Available: true)).ToArray();
        return new ModelCatalog(
            providers,
            models,
            [new LogicalModelAlias(document.LogicalModel, ModelCapabilities.Chat, PreferLocal: !document.CloudAllowed, 0, 0)]);
    }

    private IReadOnlyList<NodalOsCoreTimelineProjection> BuildTimeline(
        NodalOsPersistedByokModelConfiguration document,
        ModelRoutingResult result,
        bool fallbackApplied,
        IReadOnlyList<NodalOsEvidenceBridgeRef> evidence,
        TimeSpan elapsed)
    {
        var events = new List<NodalOsCoreTimelineProjection>
        {
            Timeline(
                NodalOsCoreEventKind.PolicyGateEvaluated,
                document,
                $"Model connection policy allowed {Providers(document).Count} configured route(s), cloud allowed {document.CloudAllowed}, maximum cost {document.MaximumTotalCostUsd:0.########} USD.",
                [])
        };
        if (fallbackApplied)
        {
            events.Add(Timeline(
                NodalOsCoreEventKind.WarningRaised,
                document,
                $"The primary model route failed and the pre-authorized fallback continued automatically after {result.Attempts.Count} bounded attempt(s).",
                []));
        }
        events.Add(Timeline(
            result.Success ? NodalOsCoreEventKind.ExecutionCompleted : NodalOsCoreEventKind.ExecutionFailed,
            document,
            result.Success
                ? $"A real provider connection completed in {elapsed.TotalMilliseconds:0} ms with {result.Attempts.Count} bounded attempt(s)."
                : $"The bounded provider connection did not complete after {result.Attempts.Count} attempt(s).",
            evidence));
        if (evidence.Count > 0)
        {
            events.Add(Timeline(
                NodalOsCoreEventKind.EvidenceAttached,
                document,
                "The provider response hash and redacted routing outcome were attached as verification evidence.",
                evidence));
        }
        return events;
    }

    private NodalOsCoreTimelineProjection Timeline(
        NodalOsCoreEventKind kind,
        NodalOsPersistedByokModelConfiguration document,
        string summary,
        IReadOnlyList<NodalOsEvidenceBridgeRef> evidence)
    {
        var coreEvent = new NodalOsCoreEvent
        {
            EventId = $"event-byok-{Guid.NewGuid():N}",
            Kind = kind,
            ExecutionRegistryEntryId = $"registry-byok-{document.ConfigurationId}",
            ExecutionRequestId = $"request-byok-{document.ConfigurationId}",
            MissionId = null,
            TaskId = "byok-model-connection-test",
            RuntimeExecutionAllowed = false,
            RuntimeExecutionDeferred = true,
            RequiresGlobalPolicyEvaluation = true,
            RequiresEvidenceRedaction = true,
            MetadataRedacted = new Dictionary<string, string>
            {
                ["logicalModel"] = document.LogicalModel,
                ["configurationId"] = document.ConfigurationId
            },
            EvidenceRefs = evidence,
            HumanSummaryRedacted = SafeRuntimeText.Sanitize(summary, 400),
            TechnicalSummaryRedacted = SafeRuntimeText.Sanitize(summary, 400),
            CreatedAt = DateTimeOffset.UtcNow
        };
        var validation = coreValidator.ValidateEvent(coreEvent);
        if (!validation.IsValid)
            throw new InvalidOperationException(string.Join(" | ", validation.Errors));
        return coreValidator.ToTimelineProjection(coreEvent);
    }

    private static NodalOsEvidenceBridgeRef Evidence(
        string id,
        string kind,
        string hash,
        string ledgerRef,
        string provenance) => new()
    {
        EvidenceId = id,
        Kind = kind,
        Ref = null,
        Hash = hash,
        SourceKind = NodalOsEvidenceBridgeSourceKind.VerificationGate,
        UseKind = NodalOsEvidenceBridgeUseKind.VerificationSupport,
        Authority = NodalOsEvidenceBridgeAuthority.SupportsVerificationOnly,
        Sensitivity = NodalOsEvidenceSensitivity.NonSensitive,
        RedactionState = NodalOsEvidenceRedactionState.NotRequired,
        LedgerRef = ledgerRef,
        Provenance = provenance,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private async ValueTask WriteDocumentAsync(
        NodalOsPersistedByokModelConfiguration document,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(document, JsonOptions);
        if (Encoding.UTF8.GetByteCount(json) > MaximumMetadataBytes || HistorySanitizer.ContainsSecretLikeContent(json))
            throw new InvalidDataException("Model configuration metadata crossed the secret boundary.");
        var directory = Path.GetDirectoryName(metadataFilePath)
            ?? throw new InvalidOperationException("Model configuration metadata directory is unavailable.");
        Directory.CreateDirectory(directory);
        var temp = $"{metadataFilePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await File.WriteAllTextAsync(temp, json, new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
            File.Move(temp, metadataFilePath, overwrite: true);
            TryHide(metadataFilePath);
        }
        finally
        {
            TryDelete(temp);
        }
    }

    private async ValueTask<NodalOsPersistedByokModelConfiguration?> ReadDocumentAsync(CancellationToken cancellationToken)
    {
        var info = new FileInfo(metadataFilePath);
        if (!info.Exists || info.Length is <= 0 or > MaximumMetadataBytes)
            throw new InvalidDataException("Model configuration metadata size is invalid.");
        var json = await File.ReadAllTextAsync(info.FullName, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<NodalOsPersistedByokModelConfiguration>(json, JsonOptions);
    }

    private async ValueTask<NodalOsPersistedByokModelConfiguration?> TryReadDocumentAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(metadataFilePath))
            return null;
        try
        {
            return await ReadDocumentAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private static bool ValidateDocument(NodalOsPersistedByokModelConfiguration document) =>
        document.SchemaVersion == CurrentSchemaVersion &&
        SafeIdentifier(document.ConfigurationId, 128) &&
        string.Equals(document.LogicalModel, LogicalModelAlias, StringComparison.Ordinal) &&
        ValidateProvider(document.Primary) &&
        (document.Fallback is null || ValidateProvider(document.Fallback)) &&
        document.MaximumTotalCostUsd is >= 0 and <= 1000 &&
        document.PerAttemptTimeoutSeconds is >= 1 and <= 120 &&
        document.CreatedAt != default && document.UpdatedAt != default;

    private static bool ValidateProvider(NodalOsByokProviderConfiguration provider) =>
        provider.SlotId is "primary" or "fallback" &&
        SafeIdentifier(provider.ProviderId, 80) &&
        !string.IsNullOrWhiteSpace(provider.DisplayNameRedacted) &&
        NormalizeEndpoint(provider.Endpoint, provider.ProviderType) is not null &&
        NormalizeModelId(provider.ModelId) is not null &&
        provider.InputCostPerMillion >= 0 && provider.OutputCostPerMillion >= 0 &&
        provider.ContextWindow > 0 &&
        (!provider.RequiresCredential || provider.CredentialReference is not null) &&
        (provider.CredentialReference is null || provider.CredentialReference.ToString().Contains("[REDACTED]", StringComparison.Ordinal));

    private NodalOsByokModelConfigurationSnapshot Snapshot(
        NodalOsPersistedByokModelConfiguration document,
        string decision,
        NodalOsByokConfigurationState state,
        bool accepted,
        bool rehydrated,
        IReadOnlyList<string> blockers,
        NodalOsPersistedByokConnectionTest? transientTest = null)
    {
        var test = transientTest ?? document.LastConnectionTest;
        return new NodalOsByokModelConfigurationSnapshot(
            decision,
            accepted,
            state,
            Configured: true,
            Persisted: true,
            Rehydrated: rehydrated,
            document.LogicalModel,
            ProviderSnapshot(document.Primary),
            document.Fallback is null ? null : ProviderSnapshot(document.Fallback),
            document.CloudAllowed,
            document.MaximumPrivacyClass.ToString(),
            document.MaximumTotalCostUsd,
            document.PerAttemptTimeoutSeconds,
            ConnectionTested: test is not null,
            Connected: test?.Success == true,
            Cancelled: test?.Cancelled == true,
            FallbackApplied: test?.FallbackApplied == true,
            test?.SelectedProviderId,
            test?.SelectedModelId,
            test?.AttemptCount ?? 0,
            test?.TotalEstimatedCost ?? 0,
            test?.ResponseSha256,
            test?.AttemptSummaries ?? [],
            test?.EvidenceRefs.Select(value => value.EvidenceId).Distinct(StringComparer.Ordinal).ToArray() ?? [],
            test?.Timeline ?? [],
            blockers.Select(value => SafeRuntimeText.Sanitize(value, 240)).ToArray(),
            test?.TestedAt,
            RealProviderCallAttempted: test is not null,
            NetworkUsed: test is not null,
            SecretsExcluded: true,
            ProductAuthorityGranted: false);
    }

    private static NodalOsByokProviderSnapshot ProviderSnapshot(NodalOsByokProviderConfiguration provider) => new(
        provider.SlotId,
        provider.ProviderId,
        provider.DisplayNameRedacted,
        provider.ProviderType.ToString(),
        EndpointDisplay(provider.Endpoint),
        provider.ModelId,
        Local: provider.ProviderType == NodalOsByokProviderType.OpenAiCompatibleLocal,
        CredentialConfigured: provider.CredentialReference is not null,
        CredentialStoreId: provider.CredentialReference?.StoreId,
        provider.PrivacyClass.ToString(),
        provider.InputCostPerMillion,
        provider.OutputCostPerMillion,
        provider.Priority);

    private static NodalOsByokModelConfigurationSnapshot Empty(bool appConfigurationMutated) => new(
        "BYOK_MODEL_CONFIGURATION_NOT_CONFIGURED",
        Accepted: true,
        NodalOsByokConfigurationState.NotConfigured,
        Configured: false,
        Persisted: false,
        Rehydrated: false,
        LogicalModelAlias,
        Primary: null,
        Fallback: null,
        CloudAllowed: false,
        MaximumPrivacyClass: ModelPrivacyClass.LocalOnly.ToString(),
        MaximumTotalCostUsd: 0,
        PerAttemptTimeoutSeconds: 30,
        ConnectionTested: false,
        Connected: false,
        Cancelled: false,
        FallbackApplied: false,
        SelectedProviderId: null,
        SelectedModelId: null,
        AttemptCount: 0,
        TotalEstimatedCost: 0,
        ResponseSha256: null,
        AttemptSummaries: [],
        EvidenceRefs: [],
        Timeline: [],
        Blockers: [],
        TestedAt: null,
        RealProviderCallAttempted: false,
        NetworkUsed: false,
        SecretsExcluded: true,
        ProductAuthorityGranted: false);

    private static NodalOsByokModelConfigurationSnapshot Failure(
        string decision,
        NodalOsByokConfigurationState state,
        IReadOnlyList<string> blockers,
        bool persisted = false) => new(
        decision,
        Accepted: false,
        state,
        Configured: persisted,
        Persisted: persisted,
        Rehydrated: false,
        LogicalModelAlias,
        Primary: null,
        Fallback: null,
        CloudAllowed: false,
        MaximumPrivacyClass: ModelPrivacyClass.LocalOnly.ToString(),
        MaximumTotalCostUsd: 0,
        PerAttemptTimeoutSeconds: 30,
        ConnectionTested: false,
        Connected: false,
        Cancelled: false,
        FallbackApplied: false,
        SelectedProviderId: null,
        SelectedModelId: null,
        AttemptCount: 0,
        TotalEstimatedCost: 0,
        ResponseSha256: null,
        AttemptSummaries: [],
        EvidenceRefs: [],
        Timeline: [],
        Blockers: blockers.Select(value => SafeRuntimeText.Sanitize(value, 240)).ToArray(),
        TestedAt: null,
        RealProviderCallAttempted: false,
        NetworkUsed: false,
        SecretsExcluded: true,
        ProductAuthorityGranted: false);

    private static IReadOnlyList<NodalOsByokProviderConfiguration> Providers(NodalOsPersistedByokModelConfiguration? document)
    {
        if (document is null)
            return [];
        return document.Fallback is null ? [document.Primary] : [document.Primary, document.Fallback];
    }

    private static string ConfigurationId(
        NodalOsByokProviderConfiguration primary,
        NodalOsByokProviderConfiguration? fallback)
    {
        var canonical = string.Join("|", primary.ProviderId, primary.Endpoint, primary.ModelId,
            fallback?.ProviderId ?? "none", fallback?.Endpoint ?? "none", fallback?.ModelId ?? "none");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant()[..24];
    }

    private static string? NormalizeIdentifier(string? value, int maximumLength)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return SafeIdentifier(normalized, maximumLength) ? normalized : null;
    }

    private string NormalizeDisplayName(string? value, string fallback)
    {
        var safe = redaction.RedactValue(SafeRuntimeText.Sanitize(value, 100)).Value.Trim();
        return string.IsNullOrWhiteSpace(safe) ? fallback : safe;
    }

    private static Uri? NormalizeEndpoint(string? value, NodalOsByokProviderType type)
    {
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var endpoint) ||
            !string.IsNullOrEmpty(endpoint.UserInfo) || !string.IsNullOrEmpty(endpoint.Query) ||
            !string.IsNullOrEmpty(endpoint.Fragment))
            return null;
        if (type == NodalOsByokProviderType.OpenAiCompatibleCloud)
            return endpoint.Scheme == Uri.UriSchemeHttps ? endpoint : null;
        if (endpoint.Scheme is not (Uri.UriSchemeHttp or Uri.UriSchemeHttps))
            return null;
        return IPAddress.TryParse(endpoint.Host, out var address) && IPAddress.IsLoopback(address) ||
               string.Equals(endpoint.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            ? endpoint
            : null;
    }

    private static string? NormalizeModelId(string? value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > 160 || normalized.Any(char.IsControl) || normalized.Any(char.IsWhiteSpace))
            return null;
        return normalized.All(character => char.IsLetterOrDigit(character) || character is '-' or '_' or '.' or '/' or ':')
            ? normalized
            : null;
    }

    private static bool SafeIdentifier(string? value, int maximumLength) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= maximumLength &&
        value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.');

    private static string EndpointDisplay(string endpoint)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            return "invalid";
        return uri.GetLeftPart(UriPartial.Authority) + uri.AbsolutePath.TrimEnd('/');
    }

    private async ValueTask BestEffortDeleteAsync(SecretReference reference)
    {
        try
        {
            await secretStore.DeleteAsync(reference, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
        }
    }

    private static void TryHide(string path)
    {
        try
        {
            File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Hidden);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
