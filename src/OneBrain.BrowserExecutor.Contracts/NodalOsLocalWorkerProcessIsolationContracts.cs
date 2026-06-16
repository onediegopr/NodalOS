namespace OneBrain.BrowserExecutor.Contracts;

// M189 — Real OS worker isolation boundary + hardened IPC pre-PaddleOCR.
// This milestone exercises process lifecycle, timeout, auth, size and version controls
// using an INNOCENT echo process (no OCR, no Python, no Paddle/Tesseract).
// The boundary is honest about what the OS actually enforces vs what is modeled/observed.

public enum NodalOsLocalWorkerIsolationEnforcementLevel
{
    Modeled,      // Policy exists but OS-level enforcement was not verified on this run.
    Observed,     // Runtime observation did not see a violation, but no hard sandbox.
    Enforced,     // OS-level sandbox/job object/container enforced the restriction.
    NotEnforced   // Known limitation: restriction cannot be enforced in this environment.
}

public enum NodalOsLocalWorkerProcessDecision
{
    Launched,
    TimedOut,
    Killed,
    RejectedByPolicy,
    RejectedByIpcSecurity,
    UnexpectedOutput,
    LaunchFailed,
    SimulatedOnly
}

public enum NodalOsLocalWorkerIpcAuthDecision
{
    Authenticated,
    MissingToken,
    InvalidToken,
    ExpiredToken
}

public sealed record NodalOsLocalWorkerProcessSandboxPolicy(
    bool AllowExternalProcess,
    bool AllowNetwork,
    bool AllowRawPersistence,
    bool AllowPython,
    bool AllowRealOcr,
    bool NoAuthority,
    int MaxProcessLifetimeMs,
    int MaxStdoutBytes,
    int MaxStderrBytes,
    NodalOsLocalWorkerIsolationEnforcementLevel NetworkEnforcement,
    NodalOsLocalWorkerIsolationEnforcementLevel FilesystemEnforcement,
    NodalOsLocalWorkerIsolationEnforcementLevel ProcessEnforcement);

public sealed record NodalOsLocalWorkerProcessLaunchSpec(
    string WorkerId,
    string ContractVersion,
    string ExecutablePath,
    string[] Arguments,
    int TimeoutMs,
    bool InnocentEchoOnly,
    bool NoAuthority,
    bool Redacted);

public sealed record NodalOsLocalWorkerIpcSecurityPolicy(
    string ContractVersion,
    string AuthToken,
    int MaxMessageBytes,
    int MaxLifetimeMs,
    bool RequireAuthToken,
    bool ValidateContractVersion,
    bool NoAuthority);

public sealed record NodalOsLocalWorkerProcessMessage(
    string MessageId,
    string ContractVersion,
    string AuthToken,
    string PayloadJson,
    DateTimeOffset SentAtUtc,
    bool NoAuthority);

public sealed record NodalOsLocalWorkerProcessResult(
    string ResultId,
    string WorkerId,
    NodalOsLocalWorkerProcessDecision Decision,
    int ExitCode,
    string StdoutRedacted,
    string StderrRedacted,
    TimeSpan Duration,
    bool NoAuthority,
    bool RawPersisted,
    bool NetworkObserved,
    bool FilesystemWriteObserved,
    bool Killed,
    IReadOnlyList<string> Warnings,
    DateTimeOffset CompletedAtUtc,
    bool Redacted);

public sealed record NodalOsLocalWorkerProcessHealth(
    string HealthId,
    bool IsResponsive,
    bool ContractVersionMatch,
    bool AuthTokenValid,
    bool WithinSizeLimits,
    bool WithinTimeoutLimits,
    bool NoAuthority,
    DateTimeOffset CheckedAtUtc);

public sealed record NodalOsLocalWorkerProcessIsolationEvidence(
    string EvidenceId,
    NodalOsLocalWorkerIsolationEnforcementLevel NetworkIsolation,
    NodalOsLocalWorkerIsolationEnforcementLevel FilesystemIsolation,
    NodalOsLocalWorkerIsolationEnforcementLevel ProcessIsolation,
    bool NoRawPersistence,
    bool NoNetworkIntentObserved,
    bool NoFilesystemWriteObserved,
    bool NoAuthority,
    string Notes,
    bool Redacted);

public sealed record NodalOsLocalWorkerProcessKillPolicy(
    bool KillOnTimeout,
    bool KillOnUnexpectedOutput,
    bool KillOnAuthFailure,
    bool KillOnOversizeMessage,
    int KillTimeoutMs,
    bool NoAuthority);
