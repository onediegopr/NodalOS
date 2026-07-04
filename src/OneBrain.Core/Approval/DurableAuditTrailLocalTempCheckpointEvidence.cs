namespace OneBrain.Core.Approval;

public enum DurableAuditTrailLocalTempCheckpointDecision
{
    Captured,
    Matched,
    TailDeletionSuspected,
    HeadDiverged,
    Rejected
}

public enum DurableAuditTrailLocalTempCheckpointRejectReason
{
    LedgerOutsideLocalTempBoundary,
    LedgerVerificationFailed,
    MissingCheckpoint,
    MalformedCheckpoint,
    CheckpointLedgerPathMismatch,
    CheckpointTrustBoundaryMismatch,
    CheckpointClaimsExternalTrust,
    CheckpointClaimsWormKmsCloud,
    CheckpointClaimsReleaseCommercialReady
}

public sealed record DurableAuditTrailLocalTempCheckpoint(
    string LedgerFile,
    int EntryCount,
    long LastSequenceNumber,
    string LastHash,
    DateTimeOffset CapturedAtUtc,
    string TrustBoundary,
    bool ExternalTrust,
    bool WormOrKmsBacked,
    bool CloudBacked,
    bool ReleaseCommercialReady);

public sealed record DurableAuditTrailLocalTempCheckpointEvidenceResult(
    DurableAuditTrailLocalTempCheckpointDecision Decision,
    IReadOnlyList<DurableAuditTrailLocalTempCheckpointRejectReason> RejectReasons,
    DurableAuditTrailLocalTempCheckpoint? Checkpoint,
    DurableAuditTrailAppendOnlyMinimalVerification? CurrentVerification,
    bool TailDeletionEvidenceAvailable,
    bool ExternalTrustAvailable,
    bool ProductRuntimeEnabled,
    bool ReleaseCommercialReady);

public sealed class DurableAuditTrailLocalTempCheckpointEvidence
{
    private const string LocalTempTrustBoundary = "local-temp-test-only";

    public DurableAuditTrailLocalTempCheckpointEvidenceResult CaptureHeadCheckpoint(string ledgerFile)
    {
        if (!IsUnderTempPath(ledgerFile))
        {
            return Rejected([DurableAuditTrailLocalTempCheckpointRejectReason.LedgerOutsideLocalTempBoundary]);
        }

        var verification = new DurableAuditTrailAppendOnlyMinimal().VerifyFile(ledgerFile);
        if (!verification.Valid)
        {
            return Rejected(
                [DurableAuditTrailLocalTempCheckpointRejectReason.LedgerVerificationFailed],
                currentVerification: verification);
        }

        var checkpoint = new DurableAuditTrailLocalTempCheckpoint(
            LedgerFile: Path.GetFullPath(ledgerFile),
            EntryCount: verification.EntryCount,
            LastSequenceNumber: verification.LastSequenceNumber,
            LastHash: verification.LastHash,
            CapturedAtUtc: DateTimeOffset.UtcNow,
            TrustBoundary: LocalTempTrustBoundary,
            ExternalTrust: false,
            WormOrKmsBacked: false,
            CloudBacked: false,
            ReleaseCommercialReady: false);

        return new DurableAuditTrailLocalTempCheckpointEvidenceResult(
            Decision: DurableAuditTrailLocalTempCheckpointDecision.Captured,
            RejectReasons: [],
            Checkpoint: checkpoint,
            CurrentVerification: verification,
            TailDeletionEvidenceAvailable: false,
            ExternalTrustAvailable: false,
            ProductRuntimeEnabled: false,
            ReleaseCommercialReady: false);
    }

    public DurableAuditTrailLocalTempCheckpointEvidenceResult CompareHeadCheckpoint(
        string ledgerFile,
        DurableAuditTrailLocalTempCheckpoint? checkpoint)
    {
        if (!TryGetFullPath(ledgerFile, out var ledgerFullPath)
            || !IsUnderTempPath(ledgerFullPath))
        {
            return Rejected([DurableAuditTrailLocalTempCheckpointRejectReason.LedgerOutsideLocalTempBoundary]);
        }

        if (checkpoint is null)
        {
            return Rejected([DurableAuditTrailLocalTempCheckpointRejectReason.MissingCheckpoint]);
        }

        var checkpointRejections = ValidateCheckpoint(checkpoint);
        if (checkpointRejections.Count > 0)
        {
            return Rejected(checkpointRejections, checkpoint);
        }

        if (!TryGetFullPath(checkpoint.LedgerFile, out var checkpointLedgerFullPath)
            || !string.Equals(ledgerFullPath, checkpointLedgerFullPath, StringComparison.OrdinalIgnoreCase))
        {
            return Rejected([DurableAuditTrailLocalTempCheckpointRejectReason.CheckpointLedgerPathMismatch]);
        }

        var verification = new DurableAuditTrailAppendOnlyMinimal().VerifyFile(ledgerFile);
        if (!verification.Valid)
        {
            return Rejected(
                [DurableAuditTrailLocalTempCheckpointRejectReason.LedgerVerificationFailed],
                checkpoint,
                verification);
        }

        var decision = DurableAuditTrailLocalTempCheckpointDecision.Matched;
        var tailDeletionEvidenceAvailable = false;
        if (verification.EntryCount < checkpoint.EntryCount
            || verification.LastSequenceNumber < checkpoint.LastSequenceNumber)
        {
            decision = DurableAuditTrailLocalTempCheckpointDecision.TailDeletionSuspected;
            tailDeletionEvidenceAvailable = true;
        }
        else if (!string.Equals(verification.LastHash, checkpoint.LastHash, StringComparison.Ordinal))
        {
            decision = DurableAuditTrailLocalTempCheckpointDecision.HeadDiverged;
        }

        return new DurableAuditTrailLocalTempCheckpointEvidenceResult(
            Decision: decision,
            RejectReasons: [],
            Checkpoint: checkpoint,
            CurrentVerification: verification,
            TailDeletionEvidenceAvailable: tailDeletionEvidenceAvailable,
            ExternalTrustAvailable: false,
            ProductRuntimeEnabled: false,
            ReleaseCommercialReady: false);
    }

    private static DurableAuditTrailLocalTempCheckpointEvidenceResult Rejected(
        IReadOnlyList<DurableAuditTrailLocalTempCheckpointRejectReason> reasons,
        DurableAuditTrailLocalTempCheckpoint? checkpoint = null,
        DurableAuditTrailAppendOnlyMinimalVerification? currentVerification = null) =>
        new(
            Decision: DurableAuditTrailLocalTempCheckpointDecision.Rejected,
            RejectReasons: reasons,
            Checkpoint: checkpoint,
            CurrentVerification: currentVerification,
            TailDeletionEvidenceAvailable: false,
            ExternalTrustAvailable: false,
            ProductRuntimeEnabled: false,
            ReleaseCommercialReady: false);

    private static IReadOnlyList<DurableAuditTrailLocalTempCheckpointRejectReason> ValidateCheckpoint(
        DurableAuditTrailLocalTempCheckpoint checkpoint)
    {
        var reasons = new List<DurableAuditTrailLocalTempCheckpointRejectReason>();
        if (!TryGetFullPath(checkpoint.LedgerFile, out var checkpointLedgerFullPath)
            || !IsUnderTempPath(checkpointLedgerFullPath)
            || checkpoint.EntryCount < 0
            || checkpoint.LastSequenceNumber < 0
            || string.IsNullOrWhiteSpace(checkpoint.LastHash)
            || checkpoint.CapturedAtUtc == default)
        {
            reasons.Add(DurableAuditTrailLocalTempCheckpointRejectReason.MalformedCheckpoint);
        }

        if (checkpoint.EntryCount == 0
            && (checkpoint.LastSequenceNumber != 0
                || !string.Equals(checkpoint.LastHash, "genesis", StringComparison.Ordinal)))
        {
            reasons.Add(DurableAuditTrailLocalTempCheckpointRejectReason.MalformedCheckpoint);
        }

        if (checkpoint.EntryCount > 0 && checkpoint.LastSequenceNumber <= 0)
        {
            reasons.Add(DurableAuditTrailLocalTempCheckpointRejectReason.MalformedCheckpoint);
        }

        if (!string.Equals(checkpoint.TrustBoundary, LocalTempTrustBoundary, StringComparison.Ordinal))
        {
            reasons.Add(DurableAuditTrailLocalTempCheckpointRejectReason.CheckpointTrustBoundaryMismatch);
        }

        if (checkpoint.ExternalTrust)
        {
            reasons.Add(DurableAuditTrailLocalTempCheckpointRejectReason.CheckpointClaimsExternalTrust);
        }

        if (checkpoint.WormOrKmsBacked || checkpoint.CloudBacked)
        {
            reasons.Add(DurableAuditTrailLocalTempCheckpointRejectReason.CheckpointClaimsWormKmsCloud);
        }

        if (checkpoint.ReleaseCommercialReady)
        {
            reasons.Add(DurableAuditTrailLocalTempCheckpointRejectReason.CheckpointClaimsReleaseCommercialReady);
        }

        return reasons.Distinct().ToArray();
    }

    private static bool IsUnderTempPath(string path)
    {
        if (!TryGetFullPath(path, out var fullPath))
        {
            return false;
        }

        var tempPath = EnsureTrailingDirectorySeparator(Path.GetFullPath(Path.GetTempPath()));
        return fullPath.StartsWith(tempPath, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetFullPath(string? path, out string fullPath)
    {
        fullPath = string.Empty;
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            fullPath = Path.GetFullPath(path);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
        catch (PathTooLongException)
        {
            return false;
        }
    }

    private static string EnsureTrailingDirectorySeparator(string path) =>
        path.EndsWith(Path.DirectorySeparatorChar)
            || path.EndsWith(Path.AltDirectorySeparatorChar)
                ? path
                : path + Path.DirectorySeparatorChar;
}
