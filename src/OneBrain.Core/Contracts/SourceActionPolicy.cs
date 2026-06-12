namespace OneBrain.Core.Contracts;

public static class SourceActionPolicy
{
    public static ActionCeiling Resolve(Provenance provenance)
    {
        if (!Enum.IsDefined(provenance))
            return ActionCeiling.ReadOnly;

        return provenance switch
        {
            Provenance.Uia => ActionCeiling.FullActionWithPreflight,
            Provenance.Fixture => ActionCeiling.FullActionWithPreflight,
            Provenance.Api => ActionCeiling.FullActionWithPreflight,
            Provenance.Win32 => ActionCeiling.ReadOnly,
            Provenance.Msaa => ActionCeiling.ReadOnly,
            Provenance.Dom => ActionCeiling.ReadOnly,
            Provenance.Ocr => ActionCeiling.ReadOnly,
            Provenance.Vision => ActionCeiling.ReadOnly,
            Provenance.Inferred => ActionCeiling.ReadOnly,
            _ => ActionCeiling.ReadOnly
        };
    }
}
