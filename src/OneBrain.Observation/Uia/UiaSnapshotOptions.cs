using FlaUI.Core;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;

namespace OneBrain.Observation.Uia;

public sealed record UiaSnapshotOptions(
    bool UseCacheRequest,
    bool CacheLegacyProperties,
    bool CacheValueProperties,
    bool CachePatternAvailability)
{
    public static UiaSnapshotOptions Default { get; } = new(
        UseCacheRequest: true,
        CacheLegacyProperties: true,
        CacheValueProperties: true,
        CachePatternAvailability: true);

    public static UiaSnapshotOptions Disabled { get; } = Default with { UseCacheRequest = false };
}

public static class UiaSnapshotPropertySet
{
    public static readonly IReadOnlyList<string> DefaultPropertyNames =
    [
        "Name",
        "AutomationId",
        "ControlType",
        "ClassName",
        "RuntimeId",
        "BoundingRectangle",
        "IsEnabled",
        "IsOffscreen",
        "HasKeyboardFocus",
        "IsKeyboardFocusable",
        "HelpText"
    ];

    public static readonly IReadOnlyList<string> PatternAvailabilityNames =
    [
        "IsInvokePatternAvailable",
        "IsValuePatternAvailable",
        "IsTogglePatternAvailable",
        "IsSelectionItemPatternAvailable",
        "IsLegacyIAccessiblePatternAvailable",
        "IsTextPatternAvailable"
    ];

    public static readonly IReadOnlyList<string> LegacyPropertyNames =
    [
        "LegacyIAccessible.Name",
        "LegacyIAccessible.Value"
    ];

    public static readonly IReadOnlyList<string> ValuePropertyNames =
    [
        "Value.Value"
    ];
}

public static class UiaSnapshotCacheRequestFactory
{
    public static CacheRequest Create(UIA3Automation automation, UiaSnapshotOptions? options = null)
    {
        options ??= UiaSnapshotOptions.Default;

        var request = new CacheRequest
        {
            AutomationElementMode = AutomationElementMode.Full,
            TreeScope = TreeScope.Subtree
        };

        AddCoreElementProperties(request, automation);

        if (options.CachePatternAvailability)
            AddPatternAvailabilityProperties(request, automation);

        if (options.CacheLegacyProperties)
            AddLegacyProperties(request, automation);

        if (options.CacheValueProperties)
            AddValueProperties(request, automation);

        return request;
    }

    private static void AddCoreElementProperties(CacheRequest request, UIA3Automation automation)
    {
        var element = automation.PropertyLibrary.Element;
        request.Add(element.Name);
        request.Add(element.AutomationId);
        request.Add(element.ControlType);
        request.Add(element.ClassName);
        request.Add(element.RuntimeId);
        request.Add(element.BoundingRectangle);
        request.Add(element.IsEnabled);
        request.Add(element.IsOffscreen);
        request.Add(element.HasKeyboardFocus);
        request.Add(element.IsKeyboardFocusable);
        request.Add(element.HelpText);
    }

    private static void AddPatternAvailabilityProperties(CacheRequest request, UIA3Automation automation)
    {
        var availability = automation.PropertyLibrary.PatternAvailability;
        request.Add(availability.IsInvokePatternAvailable);
        request.Add(availability.IsValuePatternAvailable);
        request.Add(availability.IsTogglePatternAvailable);
        request.Add(availability.IsSelectionItemPatternAvailable);
        request.Add(availability.IsLegacyIAccessiblePatternAvailable);
        request.Add(availability.IsTextPatternAvailable);
    }

    private static void AddLegacyProperties(CacheRequest request, UIA3Automation automation)
    {
        var legacy = automation.PropertyLibrary.LegacyIAccessible;
        request.Add(legacy.Name);
        request.Add(legacy.Value);
    }

    private static void AddValueProperties(CacheRequest request, UIA3Automation automation)
    {
        var value = automation.PropertyLibrary.Value;
        request.Add(value.Value);
    }
}
