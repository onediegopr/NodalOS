using System.Security.Cryptography;
using OneBrain.BrowserExecutor.Contracts;

namespace OneBrain.BrowserExecutor.Cdp;

// M206 — Synthetic OCR text fixture generator.
// Renders simple in-memory pixel-font text images for safe ONNX OCR testing.
// No real documents, no sensitive data, no raw persistence.
public sealed class NodalOsSyntheticOcrTextFixtureGenerator
{
    public const int BaseCharWidth = 5;
    public const int BaseCharHeight = 7;
    public const int FontScale = 8;
    public const int CharWidth = BaseCharWidth * FontScale;
    public const int CharHeight = BaseCharHeight * FontScale;
    public const int DefaultImageWidth = 640;
    public const int DefaultImageHeight = 640;
    public const int MaxImageWidth = 1600;
    public const int MaxImageHeight = 1200;

    private static readonly HashSet<string> SensitiveKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "secret", "token", "jwt", "apikey", "api_key", "credential", "ssn", "social",
        "credit", "card", "cvv", "pin", "passport", "license", "dob", "birth", "address", "phone",
        "email", "user", "login", "admin", "root", "private", "confidential"
    };

    private readonly NodalOsPixelImageRedactor _redactor = new();

    public NodalOsSyntheticOcrTextFixtureOptions DefaultOptions() =>
        new(
            DefaultImageWidth,
            DefaultImageHeight,
            NodalOsSyntheticOcrTextColorScheme.BlackOnWhite,
            NodalOsSyntheticOcrTextRenderMode.AntiAliasedPixelFont,
            HorizontalPadding: 32,
            VerticalPadding: 280,
            CharacterSpacing: 16,
            AllowRawPersistence: false,
            AllowFullScreen: false);

    public NodalOsSyntheticOcrTextFixture Generate(
        string text,
        NodalOsSyntheticOcrTextFixtureOptions? options = null)
    {
        options ??= DefaultOptions();
        var fixtureId = $"fixture-{Guid.NewGuid():N}";

        if (options.AllowRawPersistence)
            return Blocked(fixtureId, text, options, NodalOsSyntheticOcrTextFixtureStatus.BlockedByRawPersistence, "raw persistence not allowed");

        if (options.AllowFullScreen || options.Width > MaxImageWidth || options.Height > MaxImageHeight)
            return Blocked(fixtureId, text, options, NodalOsSyntheticOcrTextFixtureStatus.BlockedByFullScreen, "full-screen or oversized image blocked");

        if (options.Width <= 0 || options.Height <= 0)
            return Blocked(fixtureId, text, options, NodalOsSyntheticOcrTextFixtureStatus.BlockedByUnsupportedDimensions, "invalid image dimensions");

        var requiredWidth = options.HorizontalPadding * 2 + text.Length * CharWidth + Math.Max(0, text.Length - 1) * options.CharacterSpacing;
        if (requiredWidth > options.Width)
            return Blocked(fixtureId, text, options, NodalOsSyntheticOcrTextFixtureStatus.BlockedByUnsupportedDimensions, $"text too long for image width: {requiredWidth} > {options.Width}");

        if (ContainsSensitiveText(text))
            return Blocked(fixtureId, text, options, NodalOsSyntheticOcrTextFixtureStatus.BlockedBySensitiveText, "input text contains sensitive keywords");

        try
        {
            var imageBytes = RenderText(text, options);
            var redactionResult = Redact(imageBytes, options);

            if (!redactionResult.SafeForOcr || redactionResult.OriginalRawPersisted)
                return Blocked(fixtureId, text, options, NodalOsSyntheticOcrTextFixtureStatus.GenerationFailed, "redaction did not allow OCR");

            return new NodalOsSyntheticOcrTextFixture(
                fixtureId,
                text,
                redactionResult.RedactedImageBytesForOcrHandoff ?? imageBytes,
                options.Width,
                options.Height,
                options.ColorScheme,
                redactionResult,
                NodalOsSyntheticOcrTextFixtureStatus.Ready,
                SafeForOcr: true,
                OriginalRawPersisted: false,
                FullScreen: false,
                Sensitive: false,
                NoAuthority: true,
                "synthetic text fixture generated and redacted");
        }
        catch (Exception ex)
        {
            return Blocked(fixtureId, text, options, NodalOsSyntheticOcrTextFixtureStatus.GenerationFailed, $"generation failed: {ex.Message}");
        }
    }

    public NodalOsSyntheticOcrTextFixtureCatalog GenerateCatalog(
        IReadOnlyList<string>? texts = null,
        NodalOsSyntheticOcrTextFixtureOptions? options = null)
    {
        texts ??= DefaultTexts();
        options ??= DefaultOptions();

        var fixtures = texts.Select(t => Generate(t, options)).ToList();
        var ready = fixtures.Count(f => f.Status == NodalOsSyntheticOcrTextFixtureStatus.Ready);

        return new NodalOsSyntheticOcrTextFixtureCatalog(
            $"catalog-{Guid.NewGuid():N}",
            fixtures,
            ready,
            fixtures.Count - ready,
            ready == fixtures.Count,
            NoRawPersistence: fixtures.All(f => !f.OriginalRawPersisted),
            NoFullScreen: fixtures.All(f => !f.FullScreen),
            NoSensitive: fixtures.All(f => !f.Sensitive),
            NoAuthority: fixtures.All(f => f.NoAuthority));
    }

    private static byte[] RenderText(string text, NodalOsSyntheticOcrTextFixtureOptions options)
    {
        if (options.RenderMode == NodalOsSyntheticOcrTextRenderMode.AntiAliasedPixelFont)
            return RenderTextAntiAliased(text, options);

        return RenderTextPixelFont(text, options);
    }

    private static byte[] RenderTextPixelFont(string text, NodalOsSyntheticOcrTextFixtureOptions options)
    {
        var bytes = new byte[options.Width * options.Height * 4];
        var (bgR, bgG, bgB) = BackgroundColor(options.ColorScheme);
        var (fgR, fgG, fgB) = ForegroundColor(options.ColorScheme);

        FillBackground(bytes, bgR, bgG, bgB);

        var totalTextWidth = text.Length * CharWidth + Math.Max(0, text.Length - 1) * options.CharacterSpacing;
        var startX = options.HorizontalPadding + (options.Width - options.HorizontalPadding * 2 - totalTextWidth) / 2;
        var startY = options.VerticalPadding + (options.Height - options.VerticalPadding * 2 - CharHeight) / 2;

        var x = startX;
        foreach (var ch in text)
        {
            var bitmap = GetCharacterBitmap(ch);
            for (var row = 0; row < BaseCharHeight; row++)
            {
                for (var col = 0; col < BaseCharWidth; col++)
                {
                    if (!bitmap[row, col])
                        continue;

                    for (var sy = 0; sy < FontScale; sy++)
                    {
                        for (var sx = 0; sx < FontScale; sx++)
                        {
                            var px = x + col * FontScale + sx;
                            var py = startY + row * FontScale + sy;
                            if (px < 0 || px >= options.Width || py < 0 || py >= options.Height)
                                continue;

                            var idx = (py * options.Width + px) * 4;
                            bytes[idx + 0] = fgR;
                            bytes[idx + 1] = fgG;
                            bytes[idx + 2] = fgB;
                            bytes[idx + 3] = 255;
                        }
                    }
                }
            }
            x += CharWidth + options.CharacterSpacing;
        }

        return bytes;
    }

    private static byte[] RenderTextAntiAliased(string text, NodalOsSyntheticOcrTextFixtureOptions options)
    {
        const int supersample = 3;
        var (bgR, bgG, bgB) = BackgroundColor(options.ColorScheme);
        var (fgR, fgG, fgB) = ForegroundColor(options.ColorScheme);

        var ssWidth = options.Width * supersample;
        var ssHeight = options.Height * supersample;
        var ssBytes = new byte[ssWidth * ssHeight * 4];
        FillBackground(ssBytes, bgR, bgG, bgB);

        var ssCharWidth = BaseCharWidth * FontScale * supersample;
        var ssCharHeight = BaseCharHeight * FontScale * supersample;
        var ssSpacing = options.CharacterSpacing * supersample;
        var ssHPad = options.HorizontalPadding * supersample;
        var ssVPad = options.VerticalPadding * supersample;

        var totalTextWidth = text.Length * ssCharWidth + Math.Max(0, text.Length - 1) * ssSpacing;
        var startX = ssHPad + (ssWidth - ssHPad * 2 - totalTextWidth) / 2;
        var startY = ssVPad + (ssHeight - ssVPad * 2 - ssCharHeight) / 2;

        var x = startX;
        foreach (var ch in text)
        {
            var bitmap = GetCharacterBitmap(ch);
            for (var row = 0; row < BaseCharHeight; row++)
            {
                for (var col = 0; col < BaseCharWidth; col++)
                {
                    if (!bitmap[row, col])
                        continue;

                    for (var sy = 0; sy < FontScale * supersample; sy++)
                    {
                        for (var sx = 0; sx < FontScale * supersample; sx++)
                        {
                            var px = x + col * FontScale * supersample + sx;
                            var py = startY + row * FontScale * supersample + sy;
                            if (px < 0 || px >= ssWidth || py < 0 || py >= ssHeight)
                                continue;

                            var idx = (py * ssWidth + px) * 4;
                            ssBytes[idx + 0] = fgR;
                            ssBytes[idx + 1] = fgG;
                            ssBytes[idx + 2] = fgB;
                            ssBytes[idx + 3] = 255;
                        }
                    }
                }
            }
            x += ssCharWidth + ssSpacing;
        }

        return Downsample(ssBytes, ssWidth, ssHeight, options.Width, options.Height);
    }

    private static void FillBackground(byte[] bytes, byte r, byte g, byte b)
    {
        for (var i = 0; i < bytes.Length; i += 4)
        {
            bytes[i + 0] = r;
            bytes[i + 1] = g;
            bytes[i + 2] = b;
            bytes[i + 3] = 255;
        }
    }

    private static byte[] Downsample(byte[] source, int sourceWidth, int sourceHeight, int targetWidth, int targetHeight)
    {
        var scale = sourceWidth / targetWidth;
        var target = new byte[targetWidth * targetHeight * 4];

        for (var y = 0; y < targetHeight; y++)
        {
            for (var x = 0; x < targetWidth; x++)
            {
                var r = 0;
                var g = 0;
                var b = 0;
                var count = 0;
                for (var sy = 0; sy < scale; sy++)
                {
                    for (var sx = 0; sx < scale; sx++)
                    {
                        var srcIdx = ((y * scale + sy) * sourceWidth + (x * scale + sx)) * 4;
                        r += source[srcIdx + 0];
                        g += source[srcIdx + 1];
                        b += source[srcIdx + 2];
                        count++;
                    }
                }

                var idx = (y * targetWidth + x) * 4;
                target[idx + 0] = (byte)(r / count);
                target[idx + 1] = (byte)(g / count);
                target[idx + 2] = (byte)(b / count);
                target[idx + 3] = 255;
            }
        }

        return target;
    }

    private NodalOsPixelRedactionResult Redact(byte[] imageBytes, NodalOsSyntheticOcrTextFixtureOptions options)
    {
        var request = new NodalOsPixelRedactionRequest(
            $"redact-{Guid.NewGuid():N}",
            imageBytes,
            NodalOsPixelRedactionImageFormat.RawRgba32,
            options.Width,
            options.Height,
            new NodalOsOcrBoundingBox(0, 0, options.Width, options.Height),
            NodalOsOcrPurpose.EvidenceDebug,
            NodalOsOcrVisionSensitivity.Low,
            CandidateSensitiveRegions: Array.Empty<NodalOsPixelRedactionRegion>(),
            AllowRawPersistence: options.AllowRawPersistence,
            AllowFullScreen: options.AllowFullScreen);

        return _redactor.Redact(request);
    }

    private static NodalOsSyntheticOcrTextFixture Blocked(
        string fixtureId,
        string text,
        NodalOsSyntheticOcrTextFixtureOptions options,
        NodalOsSyntheticOcrTextFixtureStatus status,
        string reason)
    {
        return new NodalOsSyntheticOcrTextFixture(
            fixtureId,
            text,
            Array.Empty<byte>(),
            options.Width,
            options.Height,
            options.ColorScheme,
            RedactionResult: null!,
            status,
            SafeForOcr: false,
            OriginalRawPersisted: false,
            FullScreen: status == NodalOsSyntheticOcrTextFixtureStatus.BlockedByFullScreen,
            Sensitive: status == NodalOsSyntheticOcrTextFixtureStatus.BlockedBySensitiveText,
            NoAuthority: true,
            BrowserCredentialRedactor.Redact(reason));
    }

    private static bool ContainsSensitiveText(string text)
    {
        foreach (var keyword in SensitiveKeywords)
        {
            if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static (byte R, byte G, byte B) BackgroundColor(NodalOsSyntheticOcrTextColorScheme scheme)
    {
        return scheme switch
        {
            NodalOsSyntheticOcrTextColorScheme.WhiteOnBlack => (0, 0, 0),
            NodalOsSyntheticOcrTextColorScheme.GrayOnWhite => (255, 255, 255),
            _ => (255, 255, 255)
        };
    }

    private static (byte R, byte G, byte B) ForegroundColor(NodalOsSyntheticOcrTextColorScheme scheme)
    {
        return scheme switch
        {
            NodalOsSyntheticOcrTextColorScheme.WhiteOnBlack => (255, 255, 255),
            NodalOsSyntheticOcrTextColorScheme.GrayOnWhite => (128, 128, 128),
            _ => (0, 0, 0)
        };
    }

    private static bool[,] GetCharacterBitmap(char ch)
    {
        var upper = char.ToUpperInvariant(ch);
        if (Font.TryGetValue(upper, out var bitmap))
            return bitmap;
        return Font[' '];
    }

    private static IReadOnlyList<string> DefaultTexts() => new[]
    {
        "TEST",
        "NODAL",
        "HELLO",
        "SAFE",
        "ABC123",
        "12345"
    };

    private static readonly IReadOnlyDictionary<char, bool[,]> Font = new Dictionary<char, bool[,]>
    {
        [' '] = Parse(
            "     " +
            "     " +
            "     " +
            "     " +
            "     " +
            "     " +
            "     "),
        ['0'] = Parse(
            " ### " +
            "#   #" +
            "#  ##" +
            "# # #" +
            "##  #" +
            "#   #" +
            " ### "),
        ['1'] = Parse(
            "  #  " +
            " ##  " +
            "# #  " +
            "  #  " +
            "  #  " +
            "  #  " +
            "#####"),
        ['2'] = Parse(
            " ### " +
            "#   #" +
            "    #" +
            "   # " +
            "  #  " +
            " #   " +
            "#####"),
        ['3'] = Parse(
            "#####" +
            "    #" +
            "   # " +
            "  ## " +
            "    #" +
            "#   #" +
            " ### "),
        ['4'] = Parse(
            "   # " +
            "  ## " +
            " # # " +
            "#  # " +
            "#####" +
            "   # " +
            "  ###"),
        ['5'] = Parse(
            "#####" +
            "#    " +
            "#### " +
            "    #" +
            "    #" +
            "#   #" +
            " ### "),
        ['6'] = Parse(
            " ### " +
            "#   #" +
            "#    " +
            "#### " +
            "#   #" +
            "#   #" +
            " ### "),
        ['7'] = Parse(
            "#####" +
            "    #" +
            "   # " +
            "  #  " +
            " #   " +
            " #   " +
            "#    "),
        ['8'] = Parse(
            " ### " +
            "#   #" +
            "#   #" +
            " ### " +
            "#   #" +
            "#   #" +
            " ### "),
        ['9'] = Parse(
            " ### " +
            "#   #" +
            "#   #" +
            " ####" +
            "    #" +
            "#   #" +
            " ### "),
        ['A'] = Parse(
            " ### " +
            "#   #" +
            "#   #" +
            "#####" +
            "#   #" +
            "#   #" +
            "#   #"),
        ['B'] = Parse(
            "#### " +
            "#   #" +
            "#   #" +
            "#### " +
            "#   #" +
            "#   #" +
            "#### "),
        ['C'] = Parse(
            " ### " +
            "#   #" +
            "#    " +
            "#    " +
            "#    " +
            "#   #" +
            " ### "),
        ['D'] = Parse(
            "#### " +
            "#   #" +
            "#   #" +
            "#   #" +
            "#   #" +
            "#   #" +
            "#### "),
        ['E'] = Parse(
            "#####" +
            "#    " +
            "#    " +
            "#### " +
            "#    " +
            "#    " +
            "#####"),
        ['F'] = Parse(
            "#####" +
            "#    " +
            "#    " +
            "#### " +
            "#    " +
            "#    " +
            "#    "),
        ['G'] = Parse(
            " ### " +
            "#   #" +
            "#    " +
            "#  ##" +
            "#   #" +
            "#   #" +
            " ### "),
        ['H'] = Parse(
            "#   #" +
            "#   #" +
            "#   #" +
            "#####" +
            "#   #" +
            "#   #" +
            "#   #"),
        ['I'] = Parse(
            "#####" +
            "  #  " +
            "  #  " +
            "  #  " +
            "  #  " +
            "  #  " +
            "#####"),
        ['J'] = Parse(
            "#####" +
            "   # " +
            "   # " +
            "   # " +
            "#  # " +
            "#  # " +
            " ##  "),
        ['K'] = Parse(
            "#   #" +
            "#  # " +
            "# #  " +
            "##   " +
            "# #  " +
            "#  # " +
            "#   #"),
        ['L'] = Parse(
            "#    " +
            "#    " +
            "#    " +
            "#    " +
            "#    " +
            "#    " +
            "#####"),
        ['M'] = Parse(
            "#   #" +
            "## ##" +
            "# # #" +
            "#   #" +
            "#   #" +
            "#   #" +
            "#   #"),
        ['N'] = Parse(
            "#   #" +
            "##  #" +
            "# # #" +
            "#  ##" +
            "#   #" +
            "#   #" +
            "#   #"),
        ['O'] = Parse(
            " ### " +
            "#   #" +
            "#   #" +
            "#   #" +
            "#   #" +
            "#   #" +
            " ### "),
        ['P'] = Parse(
            "#### " +
            "#   #" +
            "#   #" +
            "#### " +
            "#    " +
            "#    " +
            "#    "),
        ['Q'] = Parse(
            " ### " +
            "#   #" +
            "#   #" +
            "#   #" +
            "# # #" +
            "#  # " +
            " ## #"),
        ['R'] = Parse(
            "#### " +
            "#   #" +
            "#   #" +
            "#### " +
            "# #  " +
            "#  # " +
            "#   #"),
        ['S'] = Parse(
            " ### " +
            "#   #" +
            "#    " +
            " ### " +
            "    #" +
            "#   #" +
            " ### "),
        ['T'] = Parse(
            "#####" +
            "  #  " +
            "  #  " +
            "  #  " +
            "  #  " +
            "  #  " +
            "  #  "),
        ['U'] = Parse(
            "#   #" +
            "#   #" +
            "#   #" +
            "#   #" +
            "#   #" +
            "#   #" +
            " ### "),
        ['V'] = Parse(
            "#   #" +
            "#   #" +
            "#   #" +
            "#   #" +
            " # # " +
            " # # " +
            "  #  "),
        ['W'] = Parse(
            "#   #" +
            "#   #" +
            "#   #" +
            "# # #" +
            "# # #" +
            "# # #" +
            " # # "),
        ['X'] = Parse(
            "#   #" +
            " # # " +
            "  #  " +
            "  #  " +
            "  #  " +
            " # # " +
            "#   #"),
        ['Y'] = Parse(
            "#   #" +
            " # # " +
            "  #  " +
            "  #  " +
            "  #  " +
            "  #  " +
            "  #  "),
        ['Z'] = Parse(
            "#####" +
            "    #" +
            "   # " +
            "  #  " +
            " #   " +
            "#    " +
            "#####")
    };

    private static bool[,] Parse(string pattern)
    {
        var bitmap = new bool[BaseCharHeight, BaseCharWidth];
        for (var row = 0; row < BaseCharHeight; row++)
        {
            for (var col = 0; col < BaseCharWidth; col++)
            {
                bitmap[row, col] = pattern[row * BaseCharWidth + col] == '#';
            }
        }
        return bitmap;
    }
}
