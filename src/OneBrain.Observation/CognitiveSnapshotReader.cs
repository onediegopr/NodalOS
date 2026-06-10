using OneBrain.Core.Models;
using OneBrain.Observation.Uia;
using OneBrain.Observation.Windows;

namespace OneBrain.Observation;

public sealed class CognitiveSnapshotReader
{
    private readonly ForegroundWindowReader _windowReader = new();
    private readonly UiaElementReader _elementReader = new();

    public CognitiveSnapshot? Read()
    {
        var window = _windowReader.Read();

        if (window is null)
        {
            return null;
        }

        var elements = _elementReader.ReadForegroundWindowElements();

        return new CognitiveSnapshot(window, elements);
    }
}
