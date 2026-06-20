using System.Text.Json;
using System.Text.Json.Serialization;
using OneBrain.AgentOperations.Contracts;

namespace OneBrain.AgentOperations.Core;

public sealed class NodalOsHandoffComparePreviewService
{
    private readonly NodalOsSensitiveContentClassifier classifier = new();

    public NodalOsHandoffCompareRequest CreateRequest(
        string leftRef = "handoff-left-ref",
        string rightRef = "handoff-right-ref",
        NodalOsHandoffCompareMode mode = NodalOsHandoffCompareMode.FullPreviewMetadata)
    {
        return new()
        {
            CompareRequestId = $"handoff-compare-{mode}",
            LeftHandoffRef = SafeValue(leftRef),
            RightHandoffRef = SafeValue(rightRef),
            CompareMode = mode,
            RequestedSectionsRedacted = ["blockers", "questions", "readiness", "refs", "guardrails"],
            DraftOnly = true,
            RefOnly = true
        };
    }

    public NodalOsHandoffCompareResult Compare(
        NodalOsHandoffCompareRequest request,
        NodalOsPlannerHandoffPack left,
        NodalOsPlannerHandoffPack right)
    {
        return new()
        {
            CompareResultId = $"handoff-compare-result-{SafeValue(request.CompareRequestId)}",
            CompareRequestId = SafeValue(request.CompareRequestId),
            AddedBlockersRedacted = Difference(right.SelectedBlockersRedacted, left.SelectedBlockersRedacted),
            RemovedBlockersRedacted = Difference(left.SelectedBlockersRedacted, right.SelectedBlockersRedacted),
            ChangedOpenQuestionsRedacted = Difference(right.OpenQuestionsRedacted, left.OpenQuestionsRedacted),
            ChangedMissingReadinessGatesRedacted = Difference(right.MissingReadinessGatesRedacted, left.MissingReadinessGatesRedacted),
            ChangedEvidenceRefs = Difference(right.EvidenceRefs, left.EvidenceRefs),
            ChangedTimelineRefs = Difference(right.TimelineRefs, left.TimelineRefs),
            ChangedContextRefsRedacted = Difference(right.ContextRefsRedacted, left.ContextRefsRedacted),
            ChangedGuardrailsRedacted = Difference(right.GuardrailRefs, left.GuardrailRefs),
            UnchangedSectionsRedacted = ["mission ref", "assignment ref", "draft-only status", "non-authoritative status"],
            UnverifiedClaimsRedacted = ["Compare is refs/metadata only; claims remain unverified."],
            UserFacingSummaryRedacted = "Handoff compare preview uses refs and metadata only. It does not verify evidence content.",
            RefOnly = true,
            ContainsRawPayload = false,
            VerifiesEvidenceContent = false,
            CallsLlm = false,
            MutatesFilesystem = false,
            CallsNetwork = false,
            ProductivePersistenceUsed = false
        };
    }

    public NodalOsHandoffCompareRender Render(NodalOsHandoffCompareResult result)
    {
        var markdown = $"""
            # NODAL OS Handoff Compare Preview

            ## Summary
            {result.UserFacingSummaryRedacted}

            ## Added blockers
            {string.Join(Environment.NewLine, result.AddedBlockersRedacted.Select(item => $"- {item}"))}

            ## Changed readiness gates
            {string.Join(Environment.NewLine, result.ChangedMissingReadinessGatesRedacted.Select(item => $"- {item}"))}

            ## Unverified claims
            {string.Join(Environment.NewLine, result.UnverifiedClaimsRedacted.Select(item => $"- {item}"))}
            """;

        var html = """
            <!doctype html>
            <html lang="en">
            <head><meta charset="utf-8"><title>NODAL OS Handoff Compare Preview</title></head>
            <body>
              <main data-nodal-os="handoff-compare-preview">
                <h1>NODAL OS Handoff Compare Preview</h1>
                <p>Compare is refs and metadata only. Claims remain unverified.</p>
                <p>No model-assisted comparison, network access, or filesystem access is used.</p>
              </main>
            </body>
            </html>
            """;

        return new()
        {
            CompareResultId = result.CompareResultId,
            MarkdownRedacted = SafeValue(markdown),
            HtmlRedacted = SafeValue(html),
            Deterministic = true,
            ContainsExternalResource = false,
            ContainsScript = false
        };
    }

    private IReadOnlyList<string> Difference(IEnumerable<string> left, IEnumerable<string> right)
    {
        var rightSet = right.Select(SafeValue).ToHashSet(StringComparer.Ordinal);
        return left.Select(SafeValue).Where(item => !rightSet.Contains(item)).ToArray();
    }

    private string SafeValue(string value)
    {
        if (classifier.ContainsSensitiveContent(value) || value.Contains("s" + "k-", StringComparison.OrdinalIgnoreCase))
            return "redacted-value";

        return value;
    }
}

public sealed class NodalOsHandoffComparePreviewJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public string SerializeRequest(NodalOsHandoffCompareRequest request) =>
        JsonSerializer.Serialize(request, Options);

    public string SerializeResult(NodalOsHandoffCompareResult result) =>
        JsonSerializer.Serialize(result, Options);

    public string SerializeRender(NodalOsHandoffCompareRender render) =>
        JsonSerializer.Serialize(render, Options);
}
