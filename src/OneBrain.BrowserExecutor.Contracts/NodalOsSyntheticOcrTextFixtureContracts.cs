namespace OneBrain.BrowserExecutor.Contracts;

// M206 — Synthetic OCR text fixture contracts.
// In-memory generated text images for safe ONNX OCR validation. No real documents/screens.

public enum NodalOsSyntheticOcrTextFixtureStatus
{
    Ready,
    BlockedBySensitiveText,
    BlockedByUnsupportedDimensions,
    BlockedByRawPersistence,
    BlockedByFullScreen,
    GenerationFailed
}

public enum NodalOsSyntheticOcrTextColorScheme
{
    BlackOnWhite,
    WhiteOnBlack,
    GrayOnWhite
}

public enum NodalOsSyntheticOcrTextRenderMode
{
    PixelFont,
    AntiAliasedPixelFont
}

public sealed record NodalOsSyntheticOcrTextFixtureOptions(
    int Width,
    int Height,
    NodalOsSyntheticOcrTextColorScheme ColorScheme,
    NodalOsSyntheticOcrTextRenderMode RenderMode,
    int HorizontalPadding,
    int VerticalPadding,
    int CharacterSpacing,
    bool AllowRawPersistence,
    bool AllowFullScreen);

public sealed record NodalOsSyntheticOcrTextFixture(
    string FixtureId,
    string ExpectedText,
    byte[] ImageBytes,
    int Width,
    int Height,
    NodalOsSyntheticOcrTextColorScheme ColorScheme,
    NodalOsPixelRedactionResult RedactionResult,
    NodalOsSyntheticOcrTextFixtureStatus Status,
    bool SafeForOcr,
    bool OriginalRawPersisted,
    bool FullScreen,
    bool Sensitive,
    bool NoAuthority,
    string Reason);

public sealed record NodalOsSyntheticOcrTextFixtureCatalog(
    string CatalogId,
    IReadOnlyList<NodalOsSyntheticOcrTextFixture> Fixtures,
    int ReadyCount,
    int BlockedCount,
    bool AllReady,
    bool NoRawPersistence,
    bool NoFullScreen,
    bool NoSensitive,
    bool NoAuthority);
