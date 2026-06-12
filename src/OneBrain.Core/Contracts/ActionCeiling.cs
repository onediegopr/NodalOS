namespace OneBrain.Core.Contracts;

public enum ActionCeiling
{
    None = 0,
    ReadOnly = 1,
    Diagnostic = 2,
    BenignAction = 3,
    FullActionWithPreflight = 4,
    Human = 5
}
