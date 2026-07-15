using System.Security.Cryptography;
using System.Text;
using OneBrain.Core.Contracts;
using OneBrain.Core.Models;
using OneBrain.Core.Runtime;

namespace OneBrain.Core.Perception;

public enum PerceptionClaimTrust
{
    TrustedApplicationState,
    ObservedData,
    ExternalContent,
    AgentMemory
}

public enum PerceptionAgreementLevel
{
    SingleSource,
    Agreed,
    Conflicting
}

public enum PerceptionConflictSeverity
{
    Informational,
    Material,
    Blocking
}

public sealed record PerceptionClaim(
    string SubjectRef,
    string Property,
    string ValueRedacted,
    Provenance Source,
    double Confidence,
    DateTimeOffset CapturedAtUtc,
    string EvidenceRef,
    PerceptionClaimTrust Trust = PerceptionClaimTrust.ObservedData)
{
    public bool CanInfluenceMissionGoal => false;

    public bool IsUntrustedContent => Trust is PerceptionClaimTrust.ObservedData
        or PerceptionClaimTrust.ExternalContent
        or PerceptionClaimTrust.AgentMemory;

    public PerceptionClaim Sanitize(string? forcedSubjectRef = null) => this with
    {
        SubjectRef = SafeRuntimeText.Sanitize(forcedSubjectRef ?? SubjectRef, 160),
        Property = SafeRuntimeText.Sanitize(Property, 80),
        ValueRedacted = SafeRuntimeText.Sanitize(ValueRedacted, 500),
        Confidence = Math.Clamp(Confidence, 0d, 1d),
        EvidenceRef = SafeRuntimeText.Sanitize(EvidenceRef, 160)
    };
}

public sealed record CrossChannelConflict(
    string SubjectRef,
    string Property,
    IReadOnlyList<string> ConflictingValuesRedacted,
    IReadOnlyList<Provenance> Sources,
    PerceptionConflictSeverity Severity,
    string Reason,
    IReadOnlyList<string> EvidenceRefs);

public sealed record CognitiveApplicationIdentity(
    string ApplicationRef,
    string ProcessNameRedacted,
    int ProcessId,
    string WindowTitleRedacted,
    string WindowClassRedacted = "");

public sealed record CognitiveSnapshotV2ElementInput(
    string SemanticRef,
    ElementIdentity Identity,
    IReadOnlyList<PerceptionClaim> Claims);

public sealed record CognitiveSnapshotV2Input(
    string SnapshotId,
    DateTimeOffset CapturedAtUtc,
    CognitiveApplicationIdentity Application,
    WindowBounds WindowBounds,
    bool IsForeground,
    IReadOnlyList<PerceptionClaim> WindowClaims,
    IReadOnlyList<CognitiveSnapshotV2ElementInput> Elements,
    IReadOnlyList<string> EvidenceRefs,
    bool ContainsRawScreenshot = false,
    bool ContainsRawDom = false);

public sealed record UnifiedElementSnapshot(
    string SemanticRef,
    ElementIdentity Identity,
    IReadOnlyDictionary<string, string> CanonicalProperties,
    IReadOnlyList<PerceptionClaim> Claims,
    IReadOnlyList<CrossChannelConflict> Conflicts,
    PerceptionAgreementLevel AgreementLevel,
    string SelectionReason,
    bool ActionEligible,
    IReadOnlyList<string> EvidenceRefs);

public sealed record CognitiveSnapshotV2(
    string SnapshotId,
    DateTimeOffset CapturedAtUtc,
    CognitiveApplicationIdentity Application,
    WindowBounds WindowBounds,
    bool IsForeground,
    IReadOnlyList<PerceptionClaim> WindowClaims,
    IReadOnlyList<UnifiedElementSnapshot> Elements,
    IReadOnlyList<CrossChannelConflict> Conflicts,
    string StateFingerprint,
    IReadOnlyList<string> EvidenceRefs,
    bool ContainsRawScreenshot,
    bool ContainsRawDom,
    bool SecretsExcluded,
    bool ObservedContentCanChangeMissionGoal)
{
    public bool HasBlockingConflicts => Conflicts.Any(conflict =>
        conflict.Severity == PerceptionConflictSeverity.Blocking);

    public bool ActionEligible => !HasBlockingConflicts;
}

public static class CognitiveSnapshotV2Factory
{
    private static readonly HashSet<string> ActionCriticalProperties = new(
        [
            "name",
            "text",
            "value",
            "role",
            "controlType",
            "automationId",
            "isEnabled",
            "isVisible",
            "isOffscreen",
            "bounds",
            "target"
        ],
        StringComparer.OrdinalIgnoreCase);

    public static CognitiveSnapshotV2 Create(CognitiveSnapshotV2Input input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Application);
        ArgumentNullException.ThrowIfNull(input.WindowBounds);
        ArgumentNullException.ThrowIfNull(input.WindowClaims);
        ArgumentNullException.ThrowIfNull(input.Elements);
        ArgumentNullException.ThrowIfNull(input.EvidenceRefs);

        var snapshotId = SafeRuntimeText.Sanitize(input.SnapshotId, 160);
        if (snapshotId.Length == 0)
            throw new ArgumentException("Snapshot id is required.", nameof(input));

        var application = SanitizeApplication(input.Application);
        var windowClaims = input.WindowClaims
            .Select(claim => claim.Sanitize("window"))
            .Where(IsUsableClaim)
            .OrderBy(claim => claim.Property, StringComparer.Ordinal)
            .ThenBy(claim => claim.Source)
            .ThenBy(claim => claim.ValueRedacted, StringComparer.Ordinal)
            .ToArray();

        var elements = input.Elements
            .Select(FuseElement)
            .OrderBy(element => element.SemanticRef, StringComparer.Ordinal)
            .ToArray();

        var windowConflicts = DetectConflicts("window", windowClaims);
        var conflicts = windowConflicts
            .Concat(elements.SelectMany(element => element.Conflicts))
            .OrderBy(conflict => conflict.SubjectRef, StringComparer.Ordinal)
            .ThenBy(conflict => conflict.Property, StringComparer.Ordinal)
            .ToArray();

        var evidenceRefs = input.EvidenceRefs
            .Concat(windowClaims.Select(claim => claim.EvidenceRef))
            .Concat(elements.SelectMany(element => element.EvidenceRefs))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => SafeRuntimeText.Sanitize(value, 160))
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .Take(256)
            .ToArray();

        var fingerprint = ComputeStateFingerprint(
            application,
            input.WindowBounds,
            input.IsForeground,
            windowClaims,
            elements,
            conflicts);

        return new CognitiveSnapshotV2(
            SnapshotId: snapshotId,
            CapturedAtUtc: input.CapturedAtUtc,
            Application: application,
            WindowBounds: input.WindowBounds,
            IsForeground: input.IsForeground,
            WindowClaims: windowClaims,
            Elements: elements,
            Conflicts: conflicts,
            StateFingerprint: fingerprint,
            EvidenceRefs: evidenceRefs,
            ContainsRawScreenshot: input.ContainsRawScreenshot,
            ContainsRawDom: input.ContainsRawDom,
            SecretsExcluded: true,
            ObservedContentCanChangeMissionGoal: false);
    }

    public static CognitiveSnapshotV2 FromLegacy(
        CognitiveSnapshot snapshot,
        string evidenceRef,
        DateTimeOffset? capturedAtUtc = null,
        string? snapshotId = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (string.IsNullOrWhiteSpace(evidenceRef))
            throw new ArgumentException("Evidence reference is required.", nameof(evidenceRef));

        var capturedAt = capturedAtUtc ?? DateTimeOffset.UtcNow;
        var processName = SafeRuntimeText.Sanitize(snapshot.Window.ProcessName, 120);
        var windowTitle = SafeRuntimeText.Sanitize(snapshot.Window.Title, 240);
        var applicationRef = "app-" + ShortHash($"{processName}|{windowTitle}", 20);
        var id = string.IsNullOrWhiteSpace(snapshotId)
            ? "cognitive-v2-" + ShortHash($"{evidenceRef}|{processName}|{windowTitle}", 20)
            : snapshotId;

        var application = new CognitiveApplicationIdentity(
            ApplicationRef: applicationRef,
            ProcessNameRedacted: processName,
            ProcessId: snapshot.Window.ProcessId,
            WindowTitleRedacted: windowTitle);

        var windowClaims = new[]
        {
            Claim("window", "processName", processName, Provenance.Win32, 1d, capturedAt, evidenceRef,
                PerceptionClaimTrust.TrustedApplicationState),
            Claim("window", "windowTitle", windowTitle, Provenance.Win32, 0.95d, capturedAt, evidenceRef,
                PerceptionClaimTrust.TrustedApplicationState),
            Claim("window", "processId", snapshot.Window.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Provenance.Win32, 1d, capturedAt, evidenceRef, PerceptionClaimTrust.TrustedApplicationState),
            Claim("window", "isForeground", snapshot.Window.IsForeground.ToString(), Provenance.Win32, 1d, capturedAt,
                evidenceRef, PerceptionClaimTrust.TrustedApplicationState),
            Claim("window", "bounds", Bounds(snapshot.Window.Bounds), Provenance.Win32, 1d, capturedAt, evidenceRef,
                PerceptionClaimTrust.TrustedApplicationState)
        };

        var elements = snapshot.Elements.Select(element =>
        {
            var semanticRef = BuildLegacySemanticRef(element, processName);
            var identity = new ElementIdentity(
                element.RuntimeId,
                element.Role,
                element.Name,
                element.AutomationId)
            {
                Role = element.Role,
                ControlType = element.Role,
                ClassName = element.ClassName,
                ProcessName = processName,
                WindowTitle = windowTitle,
                BoundsHint = Bounds(element.Bounds),
                Provenance = Provenance.Uia
            };

            var claims = new[]
            {
                Claim(semanticRef, "role", element.Role, Provenance.Uia, 0.95d, capturedAt, evidenceRef),
                Claim(semanticRef, "name", element.Name, Provenance.Uia, 0.9d, capturedAt, evidenceRef),
                Claim(semanticRef, "automationId", element.AutomationId, Provenance.Uia, 0.98d, capturedAt, evidenceRef),
                Claim(semanticRef, "className", element.ClassName, Provenance.Uia, 0.9d, capturedAt, evidenceRef),
                Claim(semanticRef, "bounds", Bounds(element.Bounds), Provenance.Uia, 0.9d, capturedAt, evidenceRef),
                Claim(semanticRef, "isEnabled", element.IsEnabled.ToString(), Provenance.Uia, 0.98d, capturedAt, evidenceRef),
                Claim(semanticRef, "isOffscreen", element.IsOffscreen.ToString(), Provenance.Uia, 0.98d, capturedAt, evidenceRef),
                Claim(semanticRef, "isKeyboardFocusable", element.IsKeyboardFocusable.ToString(), Provenance.Uia, 0.95d,
                    capturedAt, evidenceRef),
                Claim(semanticRef, "patterns", string.Join(',', element.Patterns.OrderBy(value => value, StringComparer.Ordinal)),
                    Provenance.Uia, 0.9d, capturedAt, evidenceRef),
                Claim(semanticRef, "actions", string.Join(',', element.Actions.OrderBy(value => value, StringComparer.Ordinal)),
                    Provenance.Uia, 0.9d, capturedAt, evidenceRef)
            };

            return new CognitiveSnapshotV2ElementInput(semanticRef, identity, claims);
        }).ToArray();

        return Create(new CognitiveSnapshotV2Input(
            SnapshotId: id!,
            CapturedAtUtc: capturedAt,
            Application: application,
            WindowBounds: snapshot.Window.Bounds,
            IsForeground: snapshot.Window.IsForeground,
            WindowClaims: windowClaims,
            Elements: elements,
            EvidenceRefs: [evidenceRef],
            ContainsRawScreenshot: false,
            ContainsRawDom: false));
    }

    private static UnifiedElementSnapshot FuseElement(CognitiveSnapshotV2ElementInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Identity);
        ArgumentNullException.ThrowIfNull(input.Claims);

        var semanticRef = SafeRuntimeText.Sanitize(input.SemanticRef, 160);
        if (semanticRef.Length == 0)
            throw new ArgumentException("Element semantic ref is required.", nameof(input));

        var claims = input.Claims
            .Select(claim => claim.Sanitize(semanticRef))
            .Where(IsUsableClaim)
            .OrderBy(claim => claim.Property, StringComparer.Ordinal)
            .ThenBy(claim => claim.Source)
            .ThenBy(claim => claim.ValueRedacted, StringComparer.Ordinal)
            .ToArray();

        var canonical = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var selectionReasons = new List<string>();
        foreach (var group in claims.GroupBy(claim => claim.Property, StringComparer.OrdinalIgnoreCase))
        {
            var selection = SelectCanonical(group);
            canonical[group.Key] = selection.Value;
            selectionReasons.Add($"{group.Key}:{selection.Reason}");
        }

        var conflicts = DetectConflicts(semanticRef, claims);
        var distinctSources = claims.Select(claim => claim.Source).Distinct().Count();
        var agreement = conflicts.Count > 0
            ? PerceptionAgreementLevel.Conflicting
            : distinctSources > 1
                ? PerceptionAgreementLevel.Agreed
                : PerceptionAgreementLevel.SingleSource;
        var actionEligible = conflicts.All(conflict => conflict.Severity != PerceptionConflictSeverity.Blocking) &&
                             !CanonicalBooleanIs(canonical, "isEnabled", false) &&
                             !CanonicalBooleanIs(canonical, "isVisible", false) &&
                             !CanonicalBooleanIs(canonical, "isOffscreen", true);
        var evidenceRefs = claims
            .Select(claim => claim.EvidenceRef)
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        return new UnifiedElementSnapshot(
            SemanticRef: semanticRef,
            Identity: SanitizeIdentity(input.Identity),
            CanonicalProperties: canonical,
            Claims: claims,
            Conflicts: conflicts,
            AgreementLevel: agreement,
            SelectionReason: string.Join(" | ", selectionReasons),
            ActionEligible: actionEligible,
            EvidenceRefs: evidenceRefs);
    }

    private static IReadOnlyList<CrossChannelConflict> DetectConflicts(
        string subjectRef,
        IReadOnlyList<PerceptionClaim> claims)
    {
        return claims
            .GroupBy(claim => claim.Property, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var values = group
                    .Select(claim => claim.ValueRedacted)
                    .Where(value => value.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                if (values.Length <= 1)
                    return null;

                var sources = group.Select(claim => claim.Source).Distinct().OrderBy(source => source).ToArray();
                if (sources.Length <= 1)
                    return null;

                var severity = ActionCriticalProperties.Contains(group.Key)
                    ? PerceptionConflictSeverity.Blocking
                    : PerceptionConflictSeverity.Material;
                var evidenceRefs = group
                    .Select(claim => claim.EvidenceRef)
                    .Where(value => value.Length > 0)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray();

                return new CrossChannelConflict(
                    SubjectRef: subjectRef,
                    Property: group.Key,
                    ConflictingValuesRedacted: values,
                    Sources: sources,
                    Severity: severity,
                    Reason: severity == PerceptionConflictSeverity.Blocking
                        ? "Action-critical property disagrees across perception channels; re-ground before acting."
                        : "Perception channels disagree; preserve all claims and verify before relying on this property.",
                    EvidenceRefs: evidenceRefs);
            })
            .Where(conflict => conflict is not null)
            .Cast<CrossChannelConflict>()
            .OrderBy(conflict => conflict.Property, StringComparer.Ordinal)
            .ToArray();
    }

    private static (string Value, string Reason) SelectCanonical(IEnumerable<PerceptionClaim> claims)
    {
        var candidates = claims
            .Where(claim => claim.ValueRedacted.Length > 0)
            .GroupBy(claim => claim.ValueRedacted, StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                Value = group.OrderBy(claim => claim.ValueRedacted, StringComparer.Ordinal).First().ValueRedacted,
                Score = group.Sum(claim => claim.Confidence * SourceWeight(claim.Source)),
                Sources = group.Select(claim => claim.Source).Distinct().OrderBy(source => source).ToArray()
            })
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Value, StringComparer.Ordinal)
            .ToArray();

        if (candidates.Length == 0)
            return (string.Empty, "no non-empty claim");

        var selected = candidates[0];
        return (
            selected.Value,
            $"weighted agreement {selected.Score:0.###} from {string.Join(',', selected.Sources)}; conflicts remain explicit");
    }

    private static string ComputeStateFingerprint(
        CognitiveApplicationIdentity application,
        WindowBounds bounds,
        bool isForeground,
        IReadOnlyList<PerceptionClaim> windowClaims,
        IReadOnlyList<UnifiedElementSnapshot> elements,
        IReadOnlyList<CrossChannelConflict> conflicts)
    {
        var builder = new StringBuilder();
        builder.Append("schema=2\n");
        builder.Append("app=").Append(application.ApplicationRef).Append('\n');
        builder.Append("process=").Append(application.ProcessNameRedacted).Append('\n');
        builder.Append("title=").Append(application.WindowTitleRedacted).Append('\n');
        builder.Append("bounds=").Append(Bounds(bounds)).Append('\n');
        builder.Append("foreground=").Append(isForeground).Append('\n');

        foreach (var claim in windowClaims
                     .OrderBy(claim => claim.Property, StringComparer.Ordinal)
                     .ThenBy(claim => claim.Source)
                     .ThenBy(claim => claim.ValueRedacted, StringComparer.Ordinal))
        {
            if (string.Equals(claim.Property, "processId", StringComparison.OrdinalIgnoreCase))
                continue;
            builder.Append("window|")
                .Append(claim.Property).Append('|')
                .Append(claim.Source).Append('|')
                .Append(claim.ValueRedacted).Append('\n');
        }

        foreach (var element in elements.OrderBy(element => element.SemanticRef, StringComparer.Ordinal))
        {
            builder.Append("element|").Append(element.SemanticRef).Append('\n');
            foreach (var property in element.CanonicalProperties.OrderBy(pair => pair.Key, StringComparer.Ordinal))
                builder.Append("property|").Append(property.Key).Append('|').Append(property.Value).Append('\n');
        }

        foreach (var conflict in conflicts
                     .OrderBy(conflict => conflict.SubjectRef, StringComparer.Ordinal)
                     .ThenBy(conflict => conflict.Property, StringComparer.Ordinal))
        {
            builder.Append("conflict|")
                .Append(conflict.SubjectRef).Append('|')
                .Append(conflict.Property).Append('|')
                .Append(conflict.Severity).Append('|')
                .Append(string.Join('~', conflict.ConflictingValuesRedacted)).Append('\n');
        }

        return Sha256(builder.ToString());
    }

    private static CognitiveApplicationIdentity SanitizeApplication(CognitiveApplicationIdentity application)
    {
        var processName = SafeRuntimeText.Sanitize(application.ProcessNameRedacted, 120);
        var windowTitle = SafeRuntimeText.Sanitize(application.WindowTitleRedacted, 240);
        var windowClass = SafeRuntimeText.Sanitize(application.WindowClassRedacted, 120);
        var applicationRef = SafeRuntimeText.Sanitize(application.ApplicationRef, 160);
        if (applicationRef.Length == 0)
            applicationRef = "app-" + ShortHash($"{processName}|{windowTitle}|{windowClass}", 20);

        return application with
        {
            ApplicationRef = applicationRef,
            ProcessNameRedacted = processName,
            WindowTitleRedacted = windowTitle,
            WindowClassRedacted = windowClass
        };
    }

    private static ElementIdentity SanitizeIdentity(ElementIdentity identity) => identity with
    {
        RuntimeId = SafeRuntimeText.Sanitize(identity.RuntimeId, 160),
        AutomationId = SafeRuntimeText.Sanitize(identity.AutomationId, 160),
        Name = SafeRuntimeText.Sanitize(identity.Name, 240),
        HelpText = SafeRuntimeText.Sanitize(identity.HelpText, 240),
        LegacyName = SafeRuntimeText.Sanitize(identity.LegacyName, 240),
        LegacyValue = SafeRuntimeText.Sanitize(identity.LegacyValue, 240),
        LabeledByName = SafeRuntimeText.Sanitize(identity.LabeledByName, 240),
        Role = SafeRuntimeText.Sanitize(identity.Role, 120),
        ControlType = SafeRuntimeText.Sanitize(identity.ControlType, 120),
        ClassName = SafeRuntimeText.Sanitize(identity.ClassName, 160),
        FrameworkId = SafeRuntimeText.Sanitize(identity.FrameworkId, 120),
        ProcessName = SafeRuntimeText.Sanitize(identity.ProcessName, 120),
        WindowTitle = SafeRuntimeText.Sanitize(identity.WindowTitle, 240),
        AncestorPath = SafeRuntimeText.Sanitize(identity.AncestorPath, 500),
        ParentFingerprint = SafeRuntimeText.Sanitize(identity.ParentFingerprint, 160),
        BoundsHint = SafeRuntimeText.Sanitize(identity.BoundsHint, 120)
    };

    private static PerceptionClaim Claim(
        string subjectRef,
        string property,
        string value,
        Provenance source,
        double confidence,
        DateTimeOffset capturedAtUtc,
        string evidenceRef,
        PerceptionClaimTrust trust = PerceptionClaimTrust.ObservedData) =>
        new(subjectRef, property, value, source, confidence, capturedAtUtc, evidenceRef, trust);

    private static bool IsUsableClaim(PerceptionClaim claim) =>
        claim.SubjectRef.Length > 0 && claim.Property.Length > 0;

    private static bool CanonicalBooleanIs(
        IReadOnlyDictionary<string, string> properties,
        string key,
        bool expected) =>
        properties.TryGetValue(key, out var value) &&
        bool.TryParse(value, out var parsed) &&
        parsed == expected;

    private static string BuildLegacySemanticRef(UiElementSnapshot element, string processName)
    {
        var stable = string.IsNullOrWhiteSpace(element.RuntimeId)
            ? $"{processName}|{element.AutomationId}|{element.Role}|{element.Name}|{element.ClassName}|{Bounds(element.Bounds)}"
            : $"{processName}|runtime|{element.RuntimeId}";
        return "element-" + ShortHash(stable, 24);
    }

    private static double SourceWeight(Provenance source) => source switch
    {
        Provenance.Api => 1.10d,
        Provenance.Dom => 1.05d,
        Provenance.Uia => 1.00d,
        Provenance.Win32 => 0.98d,
        Provenance.Fixture => 0.95d,
        Provenance.Msaa => 0.85d,
        Provenance.Vision => 0.75d,
        Provenance.Ocr => 0.70d,
        Provenance.Inferred => 0.50d,
        _ => 0.50d
    };

    private static string Bounds(WindowBounds bounds) =>
        $"{bounds.Left},{bounds.Top},{bounds.Right},{bounds.Bottom}";

    private static string ShortHash(string value, int length) => Sha256(value)[..length];

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
