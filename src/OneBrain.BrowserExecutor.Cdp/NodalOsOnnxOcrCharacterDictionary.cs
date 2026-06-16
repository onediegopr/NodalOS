namespace OneBrain.BrowserExecutor.Cdp;

// M198 — ONNX OCR character dictionary / charset loader.
// Provides vocabulary for CTC decoding. Defaults to a small ASCII subset for offline fixtures.
public sealed class NodalOsOnnxOcrCharacterDictionary
{
    public static readonly IReadOnlyList<string> EnglishAscii =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!?.-,:;\"'()[]{}/\\@#$%&* "
        .Select(c => c.ToString())
        .ToList();

    public static readonly IReadOnlyList<string> DigitsOnly =
        "0123456789".Select(c => c.ToString()).ToList();

    public string DictionaryId { get; private init; } = string.Empty;
    public string Language { get; private init; } = string.Empty;
    public IReadOnlyList<string> Characters { get; private init; } = Array.Empty<string>();
    public int BlankIndex { get; private init; }
    public bool Unknown { get; private init; }
    public bool NoAuthority { get; private init; }

    public NodalOsOnnxOcrCharacterDictionary Load(string dictionaryId, string language, IReadOnlyList<string>? characters = null)
    {
        var chars = characters ?? (language == "digits" ? DigitsOnly : EnglishAscii);
        return new NodalOsOnnxOcrCharacterDictionary
        {
            DictionaryId = dictionaryId,
            Language = language,
            Characters = chars,
            BlankIndex = chars.Count,
            Unknown = false,
            NoAuthority = true
        };
    }

    public string DecodeCtc(IReadOnlyList<int> sequence, IReadOnlyList<string>? characters = null, int? blankIndex = null)
    {
        var chars = characters ?? Characters;
        var blank = blankIndex ?? BlankIndex;
        var result = new List<string>();
        int? previous = null;
        foreach (var index in sequence)
        {
            if (index == blank)
            {
                previous = blank;
                continue;
            }

            if (previous == index)
                continue;

            previous = index;
            if (index >= 0 && index < chars.Count)
                result.Add(chars[index]);
        }

        return string.Concat(result);
    }
}
