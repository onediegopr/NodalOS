namespace OneBrain.Verification.Reports;

public sealed record ActionVerificationReport(
    int ElementsBefore,
    int ElementsAfter,
    string TitleBefore,
    string TitleAfter,
    bool TargetExistsAfter,
    string SelectorUsed,
    bool SnapshotBefore,
    bool SnapshotAfter,
    bool SameProcess,
    bool ElementCountChanged,
    string Notes);
