using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OneBrain.WindowsComputerUse;

public sealed record ComputerUseEvidenceConfidenceSummary(
    double TopLocatorConfidence,
    double AmbiguityScore,
    double StaleRiskScore,
    bool VisualFallbackRequired,
    bool RequiresHumanHandoff);

public sealed record ComputerUseUnifiedEvidencePack(
    string EvidenceId,
    string CorrelationId,
    IReadOnlyList<string> SourceSignals,
    ComputerUseRedactionStatus RedactionStatus,
    IReadOnlyList<string> SensitiveFieldsRedacted,
    bool RawScreenshotPresent,
    bool ClipboardPresent,
    bool ActionAuthorityGranted,
    bool RequiresHumanHandoff,
    ComputerUseEvidenceConfidenceSummary ConfidenceSummary,
    IReadOnlyList<ComputerUseEvidenceRef> EvidenceRefs,
    string SummaryRedacted,
    string TamperGuardHash,
    bool AuditLogBypassGuard,
    DateTimeOffset CreatedAtUtc);

public sealed class ComputerUseUnifiedEvidencePackBuilder
{
    private readonly ComputerUseEvidenceRedactor _redactor = new();

    public ComputerUseUnifiedEvidencePack Build(
        ComputerUseSnapshot snapshot,
        ComputerUseLocatorFusionResult locatorFusion,
        ComputerUsePolicyDecision? dryRunPlan = null,
        ComputerUsePerceptionFusionResult? perceptionFusion = null)
    {
        var sourceSignals = BuildSourceSignals(locatorFusion, perceptionFusion, dryRunPlan);
        var rawSummary = string.Join("; ", new[]
        {
            $"snapshot={snapshot.SnapshotId}",
            $"scenario={snapshot.Scenario}",
            $"top={locatorFusion.BestCandidate?.LabelRedacted ?? "none"}",
            $"selector={locatorFusion.BestCandidate?.SelectorKind ?? "none"}",
            $"handoff={locatorFusion.RequiresHumanHandoff}",
            $"reasons={string.Join(",", locatorFusion.HandoffReasons)}",
            $"dryRun={dryRunPlan?.Candidates.FirstOrDefault()?.ActionKind.ToString() ?? "none"}",
            $"policyExecute={dryRunPlan?.AllowedToExecuteLive.ToString() ?? "false"}"
        });
        var redaction = _redactor.Redact(rawSummary);
        var refs = BuildEvidenceRefs(snapshot, locatorFusion, dryRunPlan, perceptionFusion);
        var summary = new ComputerUseEvidenceConfidenceSummary(
            locatorFusion.BestCandidate?.ConfidenceBreakdown.FinalConfidence ?? 0,
            locatorFusion.Ambiguity.Score,
            locatorFusion.StaleElementRisk.Score,
            locatorFusion.VisualFallbackRequired,
            locatorFusion.RequiresHumanHandoff);
        var tamperInput = $"{snapshot.SnapshotId}|{redaction.Value}|{string.Join(",", refs.Select(r => r.RefId))}|{summary}";
        var sensitiveFields = CollectSensitiveFieldsRedacted(redaction, locatorFusion, refs, dryRunPlan, perceptionFusion);

        return new ComputerUseUnifiedEvidencePack(
            EvidenceId: $"wcu-unified-evidence-{Guid.NewGuid():N}",
            CorrelationId: snapshot.SnapshotId,
            SourceSignals: sourceSignals,
            RedactionStatus: redaction.Status,
            SensitiveFieldsRedacted: sensitiveFields,
            RawScreenshotPresent: false,
            ClipboardPresent: false,
            ActionAuthorityGranted: false,
            RequiresHumanHandoff: locatorFusion.RequiresHumanHandoff || dryRunPlan?.Candidates.Any(c => c.RequiresHumanHandoff) == true,
            summary,
            refs,
            redaction.Value,
            TamperGuardHash: Sha256(tamperInput),
            AuditLogBypassGuard: true,
            CreatedAtUtc: DateTimeOffset.UnixEpoch);
    }

    public string Serialize(ComputerUseUnifiedEvidencePack pack) =>
        JsonSerializer.Serialize(pack, new JsonSerializerOptions { WriteIndented = true });

    private IReadOnlyList<string> CollectSensitiveFieldsRedacted(
        ComputerUseRedactionResult summaryRedaction,
        ComputerUseLocatorFusionResult locatorFusion,
        IReadOnlyList<ComputerUseEvidenceRef> refs,
        ComputerUsePolicyDecision? dryRunPlan,
        ComputerUsePerceptionFusionResult? perceptionFusion)
    {
        var fields = new HashSet<string>(summaryRedaction.SensitiveFieldsRedacted, StringComparer.OrdinalIgnoreCase);

        void AddRange(IEnumerable<string> values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    fields.Add(value);
            }
        }

        foreach (var candidate in locatorFusion.LocatorCandidates)
        {
            AddRange(_redactor.Redact(candidate.LabelRedacted).SensitiveFieldsRedacted);
            foreach (var evidence in candidate.Evidence)
                AddRange(_redactor.Redact(evidence.DetailRedacted).SensitiveFieldsRedacted);
        }

        foreach (var visual in locatorFusion.VisualHintMatches)
            AddRange(_redactor.Redact(visual.TextRedacted).SensitiveFieldsRedacted);

        AddRange(_redactor.Redact(locatorFusion.Ambiguity.ReasonRedacted).SensitiveFieldsRedacted);
        foreach (var reason in locatorFusion.StaleElementRisk.Reasons)
            AddRange(_redactor.Redact(reason).SensitiveFieldsRedacted);

        AddRange(_redactor.Redact(locatorFusion.Win32Anchor.WindowTitleRedacted).SensitiveFieldsRedacted);
        AddRange(_redactor.Redact(locatorFusion.Win32Anchor.ProcessNameRedacted).SensitiveFieldsRedacted);

        foreach (var blockage in locatorFusion.Blockages)
            AddRange(_redactor.Redact(blockage.Reason).SensitiveFieldsRedacted);

        foreach (var surface in locatorFusion.SensitiveSurfaces)
        {
            AddRange(_redactor.Redact(surface.SurfaceId).SensitiveFieldsRedacted);
            AddRange(_redactor.Redact(surface.Category).SensitiveFieldsRedacted);
            AddRange(_redactor.Redact(surface.Reason).SensitiveFieldsRedacted);
        }

        foreach (var r in refs)
            AddRange(_redactor.Redact(r.Description).SensitiveFieldsRedacted);

        if (dryRunPlan is not null)
        {
            foreach (var candidate in dryRunPlan.Candidates)
            {
                AddRange(_redactor.Redact(candidate.Objective).SensitiveFieldsRedacted);
                AddRange(_redactor.Redact(candidate.Reason).SensitiveFieldsRedacted);
                if (candidate.Target is not null)
                    AddRange(_redactor.Redact(candidate.Target.SelectorKind).SensitiveFieldsRedacted);
            }

            foreach (var blockage in dryRunPlan.Blockages)
                AddRange(_redactor.Redact(blockage.Reason).SensitiveFieldsRedacted);
            foreach (var surface in dryRunPlan.SensitiveSurfaces)
                AddRange(_redactor.Redact(surface.Reason).SensitiveFieldsRedacted);
        }

        if (perceptionFusion is not null)
        {
            foreach (var reason in perceptionFusion.Reasons)
                AddRange(_redactor.Redact(reason).SensitiveFieldsRedacted);
            foreach (var blockage in perceptionFusion.Blockages)
                AddRange(_redactor.Redact(blockage.Reason).SensitiveFieldsRedacted);
            foreach (var surface in perceptionFusion.SensitiveSurfaces)
                AddRange(_redactor.Redact(surface.Reason).SensitiveFieldsRedacted);
        }

        return fields.ToArray();
    }

    private static IReadOnlyList<string> BuildSourceSignals(
        ComputerUseLocatorFusionResult locatorFusion,
        ComputerUsePerceptionFusionResult? perceptionFusion,
        ComputerUsePolicyDecision? dryRunPlan)
    {
        var signals = new SortedSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "uia.snapshot",
            "uia.element.identity",
            "locator.fusion",
            "evidence.redaction"
        };

        if (locatorFusion.Win32Anchor.SignalId != "win32-anchor:none")
            signals.Add("win32.context");
        if (locatorFusion.EventContinuitySignals.Count > 0)
            signals.Add("uia.event.stream");
        if (locatorFusion.VisualHintMatches.Count > 0)
            signals.Add("ocr.visual.bridge");
        if (locatorFusion.SensitiveSurfaces.Count > 0)
            signals.Add("sensitive.surface.detector");
        if (locatorFusion.Blockages.Count > 0)
            signals.Add("blockage.detector");
        if (dryRunPlan is not null)
            signals.Add("safe.action.dry-run");
        if (perceptionFusion is not null)
            signals.Add("perception.fusion");

        return signals.ToArray();
    }

    private static IReadOnlyList<ComputerUseEvidenceRef> BuildEvidenceRefs(
        ComputerUseSnapshot snapshot,
        ComputerUseLocatorFusionResult locatorFusion,
        ComputerUsePolicyDecision? dryRunPlan,
        ComputerUsePerceptionFusionResult? perceptionFusion)
    {
        var refs = new List<ComputerUseEvidenceRef>
        {
            new(snapshot.SnapshotId, "uia.snapshot", "Redacted fixture UIA snapshot metadata.", true),
            new($"locator:{locatorFusion.BestCandidate?.CandidateId ?? "none"}", "locator.fusion", "Ranked selector candidates and confidence breakdown.", true),
            new($"ambiguity:{locatorFusion.Ambiguity.IsAmbiguous}", "locator.ambiguity", locatorFusion.Ambiguity.ReasonRedacted, true),
            new($"stale:{locatorFusion.StaleElementRisk.Score:0.00}", "stale.element.risk", string.Join("; ", locatorFusion.StaleElementRisk.Reasons), true),
            new(locatorFusion.Win32Anchor.SignalId, "win32.anchor", "Redacted Win32 active window anchor.", true)
        };

        refs.AddRange(locatorFusion.VisualHintMatches.Select(h => new ComputerUseEvidenceRef($"visual:{h.ObservationId}", "ocr.visual.hint", "Redacted visual/OCR hint metadata.", true)));
        refs.AddRange(locatorFusion.EventContinuitySignals.Select(e => new ComputerUseEvidenceRef(e.SignalId, "uia.event.continuity", "Redacted UIA event continuity metadata.", true)));
        if (dryRunPlan is not null)
        {
            refs.Add(new($"dry-run:{dryRunPlan.Candidates.FirstOrDefault()?.ActionKind.ToString() ?? "none"}", "safe.action.dry-run", "Dry-run plan; no live execution authority.", true));
        }
        if (perceptionFusion is not null)
        {
            refs.Add(new($"perception:{perceptionFusion.CapabilityClassification.TechnologyKind}", "perception.fusion", "Read-only perception fusion result.", true));
        }

        return refs.DistinctBy(r => r.RefId).ToArray();
    }

    private static string Sha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
