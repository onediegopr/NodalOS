namespace OneBrain.BrowserPerception;

public enum LocatorStrategyKind
{
    Css,
    XPath,
    Accessibility,
    Text,
    Visual,
    Hybrid,
    FrameTargetRequired,
    ShadowPiercingRequired,
    HumanHandoff
}

public enum ElementLocatorType
{
    Css,
    XPath,
    Accessibility,
    Text,
    Visual,
    Frame,
    Shadow
}

public sealed record LocatorStrategy(
    LocatorStrategyKind Strategy,
    double Confidence,
    string Reason,
    IReadOnlyList<PerceptionSignalKind> RequiredSignals,
    bool HumanHandoffRequired)
{
    public static LocatorStrategy HumanHandoff(string reason) =>
        new(
            LocatorStrategyKind.HumanHandoff,
            0.2,
            reason,
            [PerceptionSignalKind.HIT_TEST, PerceptionSignalKind.FORM_STATE],
            HumanHandoffRequired: true);
}

public sealed record BrowserBoundingBoxMetadata(
    double X,
    double Y,
    double Width,
    double Height);

public sealed record InteractiveElementSnapshot(
    string ElementRef,
    string TagName,
    string? Role = null,
    string? AccessibleName = null,
    string? Text = null,
    string? Id = null,
    string? Name = null,
    IReadOnlyList<string>? CssClasses = null,
    string? Placeholder = null,
    string? FrameId = null,
    string? ShadowRootHint = null,
    bool IsVisible = true,
    bool IsEnabled = true,
    BrowserBoundingBoxMetadata? BoundingBox = null);

public sealed record ElementLocator(
    ElementLocatorType Type,
    string Value,
    double Confidence,
    string Reason,
    string ElementRef,
    bool CandidateOnly = true,
    bool ExecutesAction = false);

public sealed class LocatorEngine
{
    public LocatorStrategy SelectLocatorStrategy(PageTechnologyProfile profile, BrowserPerceptionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(snapshot);

        var blockages = new BlockageDetector().DetectBlockages(snapshot);
        if (blockages.Any(blockage => blockage.RequiresHumanHandoff))
        {
            return LocatorStrategy.HumanHandoff("Human handoff blockage prevents locator strategy selection.");
        }

        var complexSignals = CountTrue(profile.UsesFrames, profile.UsesShadowDom, profile.UsesCanvas, profile.LooksLikeSpa);
        if (complexSignals >= 3)
        {
            return LocatorStrategy.HumanHandoff("Contradictory or high-complexity page signals require manual review before locator routing.");
        }

        if (profile.UsesFrames)
        {
            return new LocatorStrategy(
                LocatorStrategyKind.FrameTargetRequired,
                0.84,
                "Relevant frame metadata requires frame-targeted locator candidates.",
                [PerceptionSignalKind.FRAME_TREE, PerceptionSignalKind.DOM],
                HumanHandoffRequired: false);
        }

        if (profile.UsesShadowDom)
        {
            return new LocatorStrategy(
                LocatorStrategyKind.ShadowPiercingRequired,
                0.84,
                "Shadow DOM marker requires shadow-aware locator candidates.",
                [PerceptionSignalKind.SHADOW_DOM, PerceptionSignalKind.DOM],
                HumanHandoffRequired: false);
        }

        if (profile.UsesCanvas)
        {
            return new LocatorStrategy(
                LocatorStrategyKind.Visual,
                0.82,
                "Canvas or visual-only surface requires visual locator candidates.",
                [PerceptionSignalKind.SCREENSHOT, PerceptionSignalKind.LAYOUT],
                HumanHandoffRequired: false);
        }

        if (profile.LooksLikeSpa || profile.HasSemanticAccessibility)
        {
            return new LocatorStrategy(
                LocatorStrategyKind.Accessibility,
                profile.HasSemanticAccessibility ? 0.82 : 0.68,
                "SPA or semantic controls favor accessibility-first locator candidates.",
                [PerceptionSignalKind.ACCESSIBILITY, PerceptionSignalKind.DOM],
                HumanHandoffRequired: false);
        }

        if (snapshot.Forms.FormsCount > 0 || profile.LooksLikeServerRendered)
        {
            return new LocatorStrategy(
                LocatorStrategyKind.Css,
                0.86,
                "Simple form or server-rendered markup supports CSS locator candidates.",
                [PerceptionSignalKind.DOM, PerceptionSignalKind.FORM_STATE],
                HumanHandoffRequired: false);
        }

        return LocatorStrategy.HumanHandoff("Low-confidence page shape does not allow locator routing.");
    }

    public IReadOnlyList<ElementLocator> GenerateElementLocators(
        IEnumerable<InteractiveElementSnapshot> elements,
        LocatorStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(elements);
        ArgumentNullException.ThrowIfNull(strategy);

        if (strategy.HumanHandoffRequired)
            return [];

        var locators = new List<ElementLocator>();
        foreach (var element in elements.Where(element => element.IsVisible && element.IsEnabled))
        {
            locators.AddRange(GenerateElementLocators(element, strategy));
        }

        return locators;
    }

    private static IEnumerable<ElementLocator> GenerateElementLocators(
        InteractiveElementSnapshot element,
        LocatorStrategy strategy)
    {
        var elementRef = string.IsNullOrWhiteSpace(element.ElementRef) ? "element:unknown" : element.ElementRef;

        if (strategy.Strategy == LocatorStrategyKind.FrameTargetRequired && !string.IsNullOrWhiteSpace(element.FrameId))
        {
            yield return new ElementLocator(
                ElementLocatorType.Frame,
                "frame:" + Redact(element.FrameId),
                0.78,
                "Frame-scoped candidate only; no page action is executed.",
                elementRef);
        }

        if (strategy.Strategy == LocatorStrategyKind.ShadowPiercingRequired && !string.IsNullOrWhiteSpace(element.ShadowRootHint))
        {
            yield return new ElementLocator(
                ElementLocatorType.Shadow,
                "shadow:" + Redact(element.ShadowRootHint),
                0.76,
                "Shadow-aware candidate only; no page action is executed.",
                elementRef);
        }

        if (strategy.Strategy == LocatorStrategyKind.Visual && element.BoundingBox is not null)
        {
            yield return new ElementLocator(
                ElementLocatorType.Visual,
                $"bbox:{element.BoundingBox.X:0},{element.BoundingBox.Y:0},{element.BoundingBox.Width:0},{element.BoundingBox.Height:0}",
                0.7,
                "Visual metadata candidate only; no click or OCR is executed.",
                elementRef);
        }

        if (!string.IsNullOrWhiteSpace(element.AccessibleName))
        {
            yield return new ElementLocator(
                ElementLocatorType.Accessibility,
                BuildAccessibilityValue(element),
                strategy.Strategy == LocatorStrategyKind.Accessibility ? 0.86 : 0.68,
                "Accessible role/name candidate only.",
                elementRef);
        }

        if (!string.IsNullOrWhiteSpace(element.Id) && IsSafeCssIdentifier(element.Id))
        {
            yield return new ElementLocator(
                ElementLocatorType.Css,
                "#" + element.Id,
                strategy.Strategy == LocatorStrategyKind.Css ? 0.84 : 0.62,
                "Stable id CSS candidate only.",
                elementRef);
        }

        if (!string.IsNullOrWhiteSpace(element.Name) && IsSafeCssIdentifier(element.Name))
        {
            yield return new ElementLocator(
                ElementLocatorType.Css,
                $"{NormalizeTag(element.TagName)}[name=\"{element.Name}\"]",
                0.72,
                "Name attribute CSS candidate only.",
                elementRef);
        }

        if (!string.IsNullOrWhiteSpace(element.Text))
        {
            yield return new ElementLocator(
                ElementLocatorType.Text,
                Redact(element.Text),
                strategy.Strategy == LocatorStrategyKind.Accessibility ? 0.66 : 0.58,
                "Redacted text candidate only.",
                elementRef);
        }
    }

    private static string BuildAccessibilityValue(InteractiveElementSnapshot element)
    {
        var role = string.IsNullOrWhiteSpace(element.Role) ? NormalizeTag(element.TagName) : Redact(element.Role);
        return $"{role}:{Redact(element.AccessibleName ?? "")}";
    }

    private static string NormalizeTag(string tagName) =>
        string.IsNullOrWhiteSpace(tagName) ? "element" : tagName.Trim().ToLowerInvariant();

    private static bool IsSafeCssIdentifier(string value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.All(character => char.IsLetterOrDigit(character) || character is '_' or '-');

    private static string Redact(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        var normalized = value.Trim();
        return normalized.Length <= 80 ? normalized : string.Concat(normalized.AsSpan(0, 80), "...");
    }

    private static int CountTrue(params bool[] values) =>
        values.Count(value => value);
}
