# NODAL OS — Unified Friction Detection & Handoff Integration

> **Version:** 1.0  
> **Date:** 2026-06-25  
> **Integrates:** M17-M18 (CDP detection/decision/evidence) + M1346-M1352 (FrictionSignal/handoff UI) + Stealth Engine Design  
> **Principle:** Single policy gate for both modes. Stealth Engine reports, never decides.

---

## Table of Contents

1. [Problem Statement](#1-problem-statement)
2. [Unified Architecture](#2-unified-architecture)
3. [Contract Extensions](#3-contract-extensions)
4. [End-to-End Flow: Stealth + CAPTCHA](#4-end-to-end-flow-stealth--captcha)
5. [Integration Points in Existing Code](#5-integration-points-in-existing-code)
6. [Stealth Engine Refactoring](#6-stealth-engine-refactoring)
7. [Updated Phased Plan](#7-updated-phased-plan)
8. [Files to Modify / Create](#8-files-to-modify--create)

---

## 1. Problem Statement

Three design documents describe overlapping capabilities:

| Doc | Scope | Key Contracts |
|---|---|---|
| **M17-M18** | Interaction state detection, safety policy, evidence ledger, handoff verification | `BrowserCredentialBoundaryDetector`, `BrowserCredentialBoundaryDecision`, `EvidenceLedger`, `BrowserHumanHandoffCoordinator`, `NodalOsAssistedVerificationPolicy` |
| **M1346-M1352** | Friction detection taxonomy, policy gate, handoff UI in sidepanel, test harness | `AccessFrictionEvent`, `BrowserAccessFrictionType`, `BrowserHumanHandoffCompanionAdapter`, `BrowserHumanHandoffPresentation` |
| **Stealth Engine** | Playwright executor, fingerprinting, CAPTCHA solver, proxies, remote handoff | `CaptchaDetector.js`, `CaptchaSolver.js`, `BlockDetector.js`, `RemoteHandoffServer.js` |

**The Redundancy:** The Stealth Engine design embeds its own detection logic (`CaptchaDetector`, `BlockDetector`) and its own decision logic (implicit: if detected → solve/retry/handoff). This duplicates work already done in M17-M18 (`BrowserCredentialBoundaryDetector`) and M1346-M1352 (policy gate).

**The Goal:** Extract the detection and decision responsibilities from the Stealth Engine into the unified .NET layer, keeping the Stealth Engine as a **reporting executor** that:

1. **Detects** frictions at the browser level → emits structured `FrictionSignal` messages
2. **Executes** decisions received from the unified policy gate
3. **Provides** the remote handoff channel when the gate decides `RequiresHuman` for stealth mode

---

## 2. Unified Architecture

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                         NODAL OS — UNIFIED FRICTION LAYER                     │
│                                                                               │
│  ┌── DETECTION LAYER (produce FrictionSignals) ────────────────────────────┐ │
│  │                                                                          │ │
│  │  COMPANION MODE                          STEALTH MODE                    │ │
│  │  ┌──────────────────────┐               ┌─────────────────────────┐     │ │
│  │  │ content_script.js     │               │ CaptchaDetector.js       │     │ │
│  │  │  observePage()        │               │  detectByDom()           │     │ │
│  │  │  detectSensitiveHints │               │  detectByFrames()        │     │ │
│  │  │  hasCaptchaLike       │               │  detectByText()          │     │ │
│  │  │  hasTwoFactorLike     │               │                          │     │ │
│  │  │  hasPasswordField     │               │ BlockDetector.js         │     │ │
│  │  │  → tool.result        │               │  detect 403/429/503      │     │ │
│  │  └──────────┬───────────┘               │  detect Cloudflare/DD    │     │ │
│  │             │                            │  → stealth.friction.signal│    │ │
│  │             │ WS /ws/extension           └──────────┬──────────────┘     │ │
│  │             │                                       │ WS /ws/stealth     │ │
│  └─────────────┼───────────────────────────────────────┼────────────────────┘ │
│                │                                       │                      │
│                ▼                                       ▼                      │
│  ┌── UNIFIED POLICY GATE (.NET) ───────────────────────────────────────────┐ │
│  │                                                                          │ │
│  │  ┌─────────────────────────────────────────────────────────────────┐    │ │
│  │  │            UnifiedFrictionPolicyEngine                          │    │ │
│  │  │                                                                  │    │ │
│  │  │  ┌─────────────────────┐   ┌───────────────────────────────┐   │    │ │
│  │  │  │ FrictionSignalRouter │──▶│ BrowserCredentialBoundaryDetector │   │    │ │
│  │  │  │ (normalize signals   │   │ (existing, unchanged)            │   │    │ │
│  │  │  │  from both modes)   │   │  EvaluateObservation()           │   │    │ │
│  │  │  └─────────────────────┘   │  EvaluateAction()                │   │    │ │
│  │  │                            │  Decide()                        │   │    │ │
│  │  │                            └───────────────┬───────────────────┘   │    │ │
│  │  │                                            │                       │    │ │
│  │  │                          BrowserCredentialBoundaryDecision         │    │ │
│  │  │                          │ AllowReadOnly / RequiresHuman /         │    │ │
│  │  │                          │ BlockAndRetry / FailClosed             │    │ │
│  │  │                          ▼                                         │    │ │
│  │  │            ┌─────────────────────────────┐                        │    │ │
│  │  │            │  ModeRouter                 │                        │    │ │
│  │  │            │  Companion → sidepanel      │                        │    │ │
│  │  │            │  Stealth   → RemoteHandoff  │                        │    │ │
│  │  │            └─────────────────────────────┘                        │    │ │
│  │  └─────────────────────────────────────────────────────────────────┘    │ │
│  └──────────────────────────────────────────────────────────────────────────┘ │
│                                                                               │
│  ┌── HANDOFF CHANNELS ─────────────────────────────────────────────────────┐ │
│  │                                                                          │ │
│  │  COMPANION                                STEALTH                        │ │
│  │  BrowserHumanHandoffCompanionAdapter      StealthHandoffGateway          │ │
│  │    → service_worker.js                          ↓                        │ │
│  │    → sidepanel.html                   RemoteHandoffServer (Node.js)      │ │
│  │    → "Ya lo hice, continuar"               → viewport streaming          │ │
│  │    → POST /api/runs/{id}/resume             → mouse/keyboard relay       │ │
│  │                                              → "Done" → resume            │ │
│  └──────────────────────────────────────────────────────────────────────────┘ │
│                                                                               │
│  ┌── EVIDENCE LEDGER + VERIFICATION (M17-M18) ─────────────────────────────┐ │
│  │                                                                          │ │
│  │  EvidenceLedger                            NodalOsAssistedVerification   │ │
│  │    .Append(                                 Policy                        │ │
│  │      friction.signal,                         .CanResume()                │ │
│  │      policy.decision,                         .VerifyChallengeCleared()   │ │
│  │      handoff.requested,                                                     │ │
│  │      handoff.completed,                                                    │ │
│  │      verification.result                                                    │ │
│  │    )                                                                        │ │
│  └──────────────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────────────┘
```

### 2.1 Component Responsibilities (Single Responsibility)

| Component | Owns | Mode |
|---|---|---|
| `content_script.js` | Produces raw observation with `hasCaptchaLike`, `hasTwoFactorLike`, `hasPasswordField` | Companion |
| `CaptchaDetector.js` | Produces `FrictionSignal` for CAPTCHAs (DOM, frames, text). Reports via WS. Never decides. | Stealth |
| `BlockDetector.js` | Produces `FrictionSignal` for blocks (403, 429, 503, Cloudflare, DataDome). Reports via WS. Never decides. | Stealth |
| `BrowserCredentialBoundaryDetector` | Processes `FrictionSignal` list → `BrowserCredentialBoundary` → `BrowserCredentialBoundaryDecision` | **Both (unified)** |
| `UnifiedFrictionPolicyEngine` | Normalizes signals from both modes, routes to `BrowserCredentialBoundaryDetector`, applies stealth-specific overrides (e.g., `BlockAndRetry` for 429) | **Both (unified)** |
| `BrowserHumanHandoffCoordinator` | Creates `BrowserHumanHandoffRequest` when `RequiresHuman` | **Both (unified)** |
| `BrowserHumanHandoffCompanionAdapter` | Formats handoff for extension sidepanel (companion) | Companion |
| `StealthHandoffGateway` | Routes handoff to `RemoteHandoffServer` (stealth) | Stealth |
| `EvidenceLedger` | Records ALL events (signals, decisions, handoffs, verifications) | **Both (unified)** |
| `RemoteHandoffServer` | Streaming viewport + input relay (stealth) | Stealth |
| `CaptchaSolver.js` | Executes CAPTCHA solving when policy says `SolveAndRetry`. Reports result back. | Stealth |
| `RecoveryStrategy` | Executes proxy/profile rotation when policy says `BlockAndRetry` | Stealth |

---

## 3. Contract Extensions

### 3.1 New: `UnifiedFrictionSignalContracts.cs`

```csharp
// src/OneBrain.ChromeLab.Bridge/Stealth/UnifiedFrictionSignalContracts.cs
namespace OneBrain.ChromeLab.Bridge.Stealth;

/// <summary>
/// The unified friction signal emitted by both companion and stealth detectors.
/// This replaces the ad-hoc observation flags (hasCaptchaLike, hasTwoFactorLike, etc.)
/// and the stealth-only CaptchaDetector/BlockDetector internal results.
/// </summary>
public sealed record FrictionSignal(
    string SignalId,
    string TaskId,        // Universal ID: companion runId or stealth taskId
    FrictionSignalKind Kind,
    FrictionSignalSeverity Severity,
    string Source,        // "companion-observation" | "stealth-captcha-detector" | "stealth-block-detector"
    string FrameId,
    string? ElementId,
    string? Sitekey,      // For CAPTCHA: the sitekey if available
    string? BlockHttpCode, // "403", "429", "503"
    string? BlockPattern,  // "cloudflare_challenge", "datadome", "access_denied"
    string RedactedEvidence,
    string Reason,
    DateTimeOffset DetectedAtUtc,
    bool AutoSolvable,     // Can this friction be auto-resolved? (e.g., reCAPTCHA v2: yes; v3/Enterprise: maybe; DataDome: no)
    string? SolverRecommendation, // "2captcha" | "capsolver" | "none"
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> ProofRefs)
{
    /// <summary>
    /// Converts this FrictionSignal into the existing BrowserCredentialSignal
    /// so the unified policy gate (BrowserCredentialBoundaryDetector) can process it.
    /// </summary>
    public BrowserCredentialSignal ToBrowserCredentialSignal() => Kind switch
    {
        FrictionSignalKind.CaptchaDetected => new(
            BrowserCredentialSignalKind.CaptchaDetected,
            BrowserCredentialRisk.Critical,
            FrameId, ElementId ?? "captcha-frame",
            RedactedEvidence, Reason),

        FrictionSignalKind.TwoFactorDetected => new(
            BrowserCredentialSignalKind.TwoFactorPromptDetected,
            BrowserCredentialRisk.Critical,
            FrameId, ElementId ?? "2fa-frame",
            RedactedEvidence, Reason),

        FrictionSignalKind.PasswordFieldDetected => new(
            BrowserCredentialSignalKind.PasswordFieldDetected,
            BrowserCredentialRisk.Critical,
            FrameId, ElementId ?? "password-field",
            RedactedEvidence, Reason),

        FrictionSignalKind.LoginFormDetected => new(
            BrowserCredentialSignalKind.LoginFormDetected,
            BrowserCredentialRisk.High,
            FrameId, ElementId ?? "login-form",
            RedactedEvidence, Reason),

        FrictionSignalKind.BotBlockDetected => new(
            BrowserCredentialSignalKind.UnknownSensitivePrompt,
            BrowserCredentialRisk.Critical,
            FrameId, ElementId ?? "bot-block",
            RedactedEvidence, $"bot/anti-automation block: {Reason}"),

        FrictionSignalKind.RateLimitDetected => new(
            BrowserCredentialSignalKind.UnknownSensitivePrompt,
            BrowserCredentialRisk.High,
            FrameId, ElementId ?? "rate-limit",
            RedactedEvidence, $"rate limit: {Reason}"),

        _ => new(
            BrowserCredentialSignalKind.UnknownSensitivePrompt,
            BrowserCredentialRisk.Medium,
            FrameId, ElementId ?? "unknown",
            RedactedEvidence, Reason)
    };
}

public enum FrictionSignalKind
{
    CaptchaDetected,
    TwoFactorDetected,
    PasswordFieldDetected,
    LoginFormDetected,
    BotBlockDetected,      // Cloudflare, DataDome, Kasada, Imperva, Akamai
    RateLimitDetected,     // HTTP 429
    AccessDeniedDetected,  // HTTP 403
    ServiceUnavailable,    // HTTP 503
    SuspiciousRedirect,    // Redirected to /blocked, /access-denied, etc.
    UnknownFriction
}

public enum FrictionSignalSeverity { Info, Warning, Critical, Fatal }
```

### 3.2 New: `UnifiedFrictionPolicyDecision.cs`

```csharp
// src/OneBrain.ChromeLab.Bridge/Stealth/UnifiedFrictionPolicyDecision.cs

/// <summary>
/// The unified decision from the policy gate, extending
/// BrowserCredentialBoundaryDecisionKind with stealth-specific actions.
/// </summary>
public enum UnifiedFrictionDecisionKind
{
    // From existing BrowserCredentialBoundaryDecisionKind:
    AllowReadOnly,        // Continue, just observe
    RequiresHuman,        // Pause and activate handoff channel
    Block,                // Stop the task
    FailClosed,           // Stop with error (unknown sensitive)

    // Stealth-specific extensions:
    SolveAndRetry,        // Auto-solve CAPTCHA via external service
    RotateAndRetry,       // Rotate proxy+profile and retry navigation
    SolveThenRotate,      // Try solving first; if fails, rotate
    CooldownAndRetry      // Wait with exponential backoff, then retry
}

public sealed record UnifiedFrictionPolicyDecision(
    UnifiedFrictionDecisionKind Decision,
    BrowserCredentialRisk Risk,
    BrowserHumanHandoffReason? HandoffReason,
    string Message,
    FrictionSignal TriggerSignal,
    BrowserCredentialBoundary Boundary,
    // Stealth-specific parameters:
    string? SolverProvider,      // "2captcha" | "capsolver" -- for SolveAndRetry
    int? RetryAttempt,           // Current retry count
    int? MaxRetries,             // Max retries allowed
    int? CooldownMs,             // Backoff duration for CooldownAndRetry
    bool RotateProxy,            // Whether to rotate proxy
    bool RotateProfile,          // Whether to rotate fingerprint
    DateTimeOffset DecidedAtUtc,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<string> ProofRefs)
{
    public bool BlocksAutomation => Decision is UnifiedFrictionDecisionKind.Block or UnifiedFrictionDecisionKind.FailClosed;
    public bool RequiresHuman => Decision == UnifiedFrictionDecisionKind.RequiresHuman;
    public bool RequiresStealthAction => Decision is UnifiedFrictionDecisionKind.SolveAndRetry or UnifiedFrictionDecisionKind.RotateAndRetry or UnifiedFrictionDecisionKind.SolveThenRotate or UnifiedFrictionDecisionKind.CooldownAndRetry;
}
```

### 3.3 Unified Policy Engine Interface

```csharp
// src/OneBrain.ChromeLab.Bridge/Stealth/IUnifiedFrictionPolicyEngine.cs

public interface IUnifiedFrictionPolicyEngine
{
    /// <summary>
    /// Evaluates a FrictionSignal and returns a unified decision.
    /// This is the SINGLE entry point for all friction signals from both modes.
    /// </summary>
    Task<UnifiedFrictionPolicyDecision> EvaluateAsync(
        FrictionSignal signal,
        string mode,           // "lab" | "stealth"
        int currentRetryCount,
        CancellationToken ct);
}
```

### 3.4 Extend `BrowserCredentialBoundaryDecisionKind`

Add one stealth-specific value to the existing enum (in `BrowserCredentialBoundaryContracts.cs`):

```csharp
public enum BrowserCredentialBoundaryDecisionKind
{
    AllowReadOnly,
    Block,
    RequiresHuman,
    RequiresApproval,
    RedactAndContinue,
    FailClosed,
    RetryWithBackoff  // NEW: for stealth anti-blocking recovery
}
```

### 3.5 New WS Message Types (Stealth Protocol Extension)

Added to `StealthProtocol.cs`:

```json
// Stealth Engine → Bridge: friction detected
{
  "type": "stealth.friction.signal",
  "taskId": "<guid>",
  "signal": {
    "signalId": "<guid>",
    "kind": "CaptchaDetected",
    "severity": "Critical",
    "source": "stealth-captcha-detector",
    "sitekey": "6LeIxAcTAAAAAJcZVRqyHh71UMIEGNQ_MXjiZKhI",
    "autoSolvable": true,
    "solverRecommendation": "2captcha",
    "redactedEvidence": "CAPTCHA found in DOM: .g-recaptcha",
    "reason": "reCAPTCHA v2 detected on page",
    "detectedAtUtc": "2026-06-25T00:00:00Z"
  }
}

// Bridge → Stealth Engine: policy decision
{
  "type": "stealth.friction.decision",
  "taskId": "<guid>",
  "signalId": "<guid>",
  "decision": "SolveAndRetry",
  "solverProvider": "2captcha",
  "sitekey": "6LeIxAcTAAAAAJcZVRqyHh71UMIEGNQ_MXjiZKhI",
  "retryAttempt": 1,
  "maxRetries": 4,
  "message": "CAPTCHA detected. Attempting auto-solve via 2captcha."
}

// Stealth Engine → Bridge: solver result
{
  "type": "stealth.friction.solved",
  "taskId": "<guid>",
  "signalId": "<guid>",
  "success": true,
  "token": "03AGdBq24...",
  "provider": "2captcha",
  "durationMs": 4500,
  "cost": 0.002
}

// Bridge → Stealth Engine: activate handoff channel
{
  "type": "stealth.handoff.activate",
  "taskId": "<guid>",
  "handoffId": "<guid>",
  "reason": "CaptchaRequired",
  "message": "CAPTCHA could not be solved automatically. Human intervention required."
}
```

---

## 4. End-to-End Flow: Stealth + CAPTCHA

This is the complete flow when a stealth session encounters a CAPTCHA:

```
STEP  ACTOR              ACTION                                         WS MESSAGE TYPE
────  ─────────────────  ─────────────────────────────────────────────  ────────────────────
 1    User/API           POST /api/runs {mode:"stealth", instruction:"..."}
 2    Bridge             StealthTaskManager.Start()
 3    Bridge             StealthProfileService.Assign()
 4    Bridge             ProxyManager.Acquire()
 5    Bridge  ──────────▶ StealthEngine                                stealth.task
 6    StealthEngine      StealthSession.initialize()
                         → launch Chromium + inject camouflage
                         → navigate to URL
 7    StealthEngine      page.goto(url)
 8    StealthEngine      CaptchaDetector.detect(page)
                         → Found: .g-recaptcha, sitekey="6LeIx..."
 9    StealthEngine ────▶ Bridge                                        stealth.friction.signal
                         {kind:"CaptchaDetected", sitekey:"6LeIx...",
                          autoSolvable:true, solverRecommendation:"2captcha"}

     ─── UNIFIED POLICY GATE (in Bridge) ───

10    Bridge             UnifiedFrictionPolicyEngine.EvaluateAsync()
                         → FrictionSignalRouter.normalize()
                         → BrowserCredentialBoundaryDetector.Decide()
                         Signal: CaptchaDetected, Risk: Critical
                         Mode: stealth, CurrentRetry: 0
                         → UnifiedFrictionDecisionKind: SolveAndRetry

11    Bridge             EvidenceLedger.Append(
                           from: "running", to: "friction_detected",
                           event: "captcha.detected",
                           reasons: ["reCAPTCHA v2 found on example.com"])

12    Bridge  ──────────▶ StealthEngine                                stealth.friction.decision
                         {decision:"SolveAndRetry", provider:"2captcha",
                          sitekey:"6LeIx...", retryAttempt:1, maxRetries:4}

     ─── STEALTH ENGINE EXECUTES DECISION ───

13    StealthEngine      CaptchaSolver.solve(page, detection, taskId)
14    StealthEngine      → 2captcha API call: recaptcha(sitekey, url)
                         → waits ~4.5s
                         → receives token "03AGdBq24..."
15    StealthEngine      TokenInjector.inject(page, "recaptcha_v2", token)
                         → fills #g-recaptcha-response
                         → triggers ___grecaptcha_cfg callback
16    StealthEngine      page.waitForTimeout(1000)
17    StealthEngine      Verify: CaptchaDetector.detect(page) → null (gone)
18    StealthEngine ────▶ Bridge                                        stealth.friction.solved
                         {success:true, token:"...", provider:"2captcha",
                          durationMs:4500, cost:0.002}

19    Bridge             EvidenceLedger.Append(
                           from: "friction_detected", to: "running",
                           event: "captcha.solved",
                           reasons: ["Solved by 2captcha in 4500ms"])

20    Bridge             → Resume normal flow:
                         → Send observation to OpenAI
                         → Receive AgentToolDecision
                         → Send next tool.request to StealthEngine

     ─── ALTERNATIVE: SOLVE FAILS → HANDOFF ───

     (If step 14 fails after 4 retries with all providers)

14a   StealthEngine ────▶ Bridge                                        stealth.friction.solved
                         {success:false, error:"all providers exhausted"}

15a   Bridge             UnifiedFrictionPolicyEngine.EvaluateAsync()
                         → RetryCount=4, MaxRetries=4
                         → UnifiedFrictionDecisionKind: RequiresHuman

16a   Bridge             BrowserHumanHandoffCoordinator.CreateRequest()
                         → BrowserHumanHandoffRequest
                         → Reason: CaptchaRequired

17a   Bridge             ModeRouter: mode=stealth
                         → StealthHandoffGateway.Activate()

18a   Bridge  ──────────▶ StealthEngine                                stealth.handoff.activate
                         {handoffId:"...", reason:"CaptchaRequired"}

19a   StealthEngine      RemoteHandoffServer.startHandoff(taskId, page, ws)
                         → Screenshot streaming starts (500ms interval)
                         → Input relay activated (mouse, keyboard, scroll)
                         → Web panel shows viewport to operator

20a   StealthEngine ────▶ Bridge                                        stealth.handoff.status
                         {status:"active", handoffId:"..."}

21a   Bridge             EvidenceLedger.Append(
                           from: "friction_detected", to: "handoff_active",
                           event: "handoff.requested",
                           reasons: ["CAPTCHA unresolved after 4 solver attempts"])

     ─── HUMAN OPERATOR RESOLVES ───

22    Operator           Sees viewport in web panel
23    Operator           Clicks the CAPTCHA checkbox via relay
24    Operator           Clicks "Done" in web panel
25    StealthEngine ────▶ Bridge                                        stealth.handoff.completed
                         {handoffId:"...", taskId:"<guid>"}

26    Bridge             NodalOsAssistedVerificationPolicy.Verify()
                         → Re-observe page via StealthEngine
                         → Check: CAPTCHA no longer present
                         → Check: Page URL changed (navigated past challenge)
                         → VerificationDecision: VerifiedLowRisk

27    Bridge             EvidenceLedger.Append(
                           from: "handoff_active", to: "running",
                           event: "handoff.verified",
                           reasons: ["CAPTCHA challenge cleared by human"])

28    Bridge             → Resume normal flow (step 20)
```

---

## 5. Integration Points in Existing Code

### 5.1 Files That DO NOT Change

| File | Reason |
|---|---|
| `BrowserCredentialBoundaryContracts.cs` | Extended (add 1 enum value), not rewritten |
| `BrowserCredentialBoundaryService.cs` | `BrowserCredentialBoundaryDetector` used as-is by the unified engine |
| `BrowserHumanHandoffCompanionAdapter.cs` | Companion handoff unchanged |
| `BrowserHumanHandoffPresentationContracts.cs` | Unchanged |
| `EvidenceLedger.cs` | Used as-is, referenced from unified engine |
| `NodalOsAssistedVerificationContracts.cs` | Used for post-handoff verification |
| `NodalOsTimelineContracts.cs` | `NodalOsRuntimeStagnationDetector` extended to receive unified signals |
| `content_script.js` | Companion detection unchanged |
| `service_worker.js` | Companion handoff unchanged |
| `OpenAiAgentClient.cs` | Unchanged |

### 5.2 Files That ARE Modified (Minimal)

| File | Change | Lines Affected |
|---|---|---|
| `BrowserCredentialBoundaryContracts.cs` | Add `RetryWithBackoff` to `BrowserCredentialBoundaryDecisionKind` | +1 line in enum |
| `Program.cs` | In `HandleStealthMessage()`, route `stealth.friction.signal` to `UnifiedFrictionPolicyEngine` | ~15 new lines |
| `Program.cs` | In companion `HandleExtensionMessage()`, route `hasCaptchaLike/hasTwoFactorLike` flags → `FrictionSignal` → `UnifiedFrictionPolicyEngine` (alongside existing `ShouldPauseForCredentialEntry` — dual path during migration) | ~20 new lines |
| `Program.cs` | After `UnifiedFrictionPolicyDecision`, route to appropriate channel (companion sidepanel vs stealth handoff) | ~30 new lines |
| `config/chrome-lab.local.json` | Add `friction.unified.enabled` flag (default: `false` during migration, `true` after) | +3 lines |

### 5.3 New Files to Create

| File | Purpose |
|---|---|
| `src/OneBrain.ChromeLab.Bridge/Stealth/UnifiedFrictionSignalContracts.cs` | `FrictionSignal`, `FrictionSignalKind`, `FrictionSignalSeverity` |
| `src/OneBrain.ChromeLab.Bridge/Stealth/UnifiedFrictionPolicyDecision.cs` | `UnifiedFrictionDecisionKind`, `UnifiedFrictionPolicyDecision` |
| `src/OneBrain.ChromeLab.Bridge/Stealth/IUnifiedFrictionPolicyEngine.cs` | Interface |
| `src/OneBrain.ChromeLab.Bridge/Stealth/UnifiedFrictionPolicyEngine.cs` | Implementation: normalizes signals → `BrowserCredentialBoundaryDetector.Decide()` → applies stealth overrides |
| `src/OneBrain.ChromeLab.Bridge/Stealth/FrictionSignalRouter.cs` | Converts companion observation flags + stealth WS signals → `FrictionSignal` list |
| `src/OneBrain.ChromeLab.Bridge/Stealth/StealthHandoffGateway.cs` | Stealth-mode handoff: activates `RemoteHandoffServer`, relays completion |
| `src/OneBrain.ChromeLab.Bridge/Stealth/HandoffVerificationService.cs` | Post-handoff verification using `NodalOsAssistedVerificationPolicy` |

### 5.4 Stealth Engine (Node.js) Files to Refactor

| File | Change |
|---|---|
| `src/captcha/CaptchaDetector.js` | **REMOVE** internal decision logic. Keep detection only. Output: `stealth.friction.signal` message. Remove direct calls to `CaptchaSolver`. |
| `src/antiBlocking/BlockDetector.js` | Same: emit `stealth.friction.signal`, never act on own. |
| `src/captcha/CaptchaSolver.js` | **KEEP** solver logic. Receives `stealth.friction.decision` from bridge, executes `solve()`, reports `stealth.friction.solved`. |
| `src/handoff/RemoteHandoffServer.js` | **KEEP** streaming + relay. **ADD**: activation by `stealth.handoff.activate` from bridge instead of internal trigger. |
| `src/index.js` | Add message routing for `stealth.friction.decision` and `stealth.handoff.activate`. |
| `src/tools/toolExecutor.js` | **ADD**: after each `observePage`/`navigate`, always call `CaptchaDetector.detect()` + `BlockDetector.detect()` and report if found. |

---

## 6. Stealth Engine Refactoring

### 6.1 Before (Current Design) vs After (Unified)

**BEFORE — Decision embedded in Stealth Engine:**
```javascript
// OLD: Stealth Engine decides what to do
const captcha = await CaptchaDetector.detect(page);
if (captcha) {
  const result = await captchaSolver.solve(page, captcha, taskId);
  if (result.success) {
    await TokenInjector.inject(page, captcha.type, result.token);
    // continue...
  } else {
    // trigger handoff internally
    await remoteHandoffServer.startHandoff(taskId, page, someWs);
  }
}
```

**AFTER — Stealth Engine reports, Bridge decides:**
```javascript
// NEW: Stealth Engine only reports frictions
const friction = await CaptchaDetector.detect(page);
if (friction) {
  // Report to bridge, WAIT for decision
  sendToBridge({
    type: 'stealth.friction.signal',
    taskId,
    signal: {
      signalId: crypto.randomUUID(),
      kind: 'CaptchaDetected',
      severity: 'Critical',
      source: 'stealth-captcha-detector',
      sitekey: friction.sitekey,
      autoSolvable: friction.type === 'recaptcha_v2' || friction.type === 'hcaptcha',
      solverRecommendation: '2captcha',
      reason: `${friction.type} detected by ${friction.detectedBy}`,
      detectedAtUtc: new Date().toISOString()
    }
  });

  // Bridge will respond with:
  // - stealth.friction.decision { decision: "SolveAndRetry", ... }
  // - stealth.handoff.activate { handoffId: "...", ... }
  // - Or the next tool request (if friction is AllowReadOnly / resolved)
  // Stealth Engine waits for the next message from bridge.
  return; // Yield control back to bridge loop
}
```

### 6.2 Updated `CaptchaDetector.js`

```javascript
// src/captcha/CaptchaDetector.js — REFACTORED
// Now only produces FrictionSignal objects, NEVER makes decisions.

export class CaptchaDetector {
  /**
   * Detects CAPTCHAs and returns a FrictionSignal or null.
   * Does NOT solve, does NOT decide.
   */
  static async detect(page) {
    const results = await Promise.all([
      this.detectByDom(page),
      this.detectByFrames(page),
      this.detectByText(page)
    ]);
    const detection = results.find(r => r !== null);
    if (!detection) return null;

    return {
      kind: this.mapToFrictionKind(detection.type),
      severity: 'Critical',
      source: 'stealth-captcha-detector',
      sitekey: detection.sitekey,
      autoSolvable: this.isAutoSolvable(detection.type),
      solverRecommendation: this.recommendSolver(detection.type),
      redactedEvidence: `${detection.type} detected by ${detection.detectedBy}`,
      reason: `CAPTCHA challenge: ${detection.type} on page`,
      frameId: detection.frameId || 'main',
      elementId: detection.selector || null
    };
  }

  static isAutoSolvable(type) {
    return ['recaptcha_v2', 'hcaptcha', 'cloudflare_turnstile'].some(t => type.startsWith(t));
  }

  static recommendSolver(type) {
    if (type.startsWith('recaptcha')) return '2captcha';
    if (type === 'hcaptcha') return '2captcha';
    if (type === 'cloudflare_turnstile') return 'capsolver';
    return 'none';
  }

  static mapToFrictionKind(type) {
    if (type.startsWith('recaptcha')) return 'CaptchaDetected';
    if (type === 'hcaptcha') return 'CaptchaDetected';
    if (type === 'cloudflare_turnstile') return 'CaptchaDetected';
    if (type === 'datadome') return 'BotBlockDetected';
    return 'UnknownFriction';
  }

  // ... detectByDom, detectByFrames, detectByText remain identical ...
}
```

### 6.3 Updated Main Loop in `index.js`

```javascript
// src/index.js — REFACTORED main loop

async function handleBridgeMessage(msg) {
  switch (msg.type) {
    case 'stealth.task':
      await handleTaskStart(msg);
      break;

    case 'stealth.friction.decision':
      await handleFrictionDecision(msg);
      break;

    case 'stealth.handoff.activate':
      await handleHandoffActivate(msg);
      break;

    case 'stealth.pause':
    case 'stealth.resume':
    case 'stealth.stop':
      await handleLifecycle(msg);
      break;

    case 'tool.request':
      await executeToolAndReport(msg);
      break;
  }
}

async function executeToolAndReport(msg) {
  const session = manager.getSession(msg.taskId);
  const result = await executeTool(session.page, msg.tool, msg.args);

  // —— ALWAYS check for friction after navigation/observation ——
  if (msg.tool === 'navigate' || msg.tool === 'observePage') {
    const captcha = await CaptchaDetector.detect(session.page);
    if (captcha) {
      sendToBridge({ type: 'stealth.friction.signal', taskId: msg.taskId, signal: captcha });
      return; // Yield control — bridge will send decision
    }

    const block = await BlockDetector.detect(session.page, null);
    if (block) {
      sendToBridge({ type: 'stealth.friction.signal', taskId: msg.taskId, signal: block });
      return;
    }
  }

  // No friction, report result normally
  sendToBridge({ type: 'stealth.result', taskId: msg.taskId, stepId: msg.requestId,
    tool: msg.tool, success: true, result, timestamp: new Date().toISOString() });
}

async function handleFrictionDecision(msg) {
  const session = manager.getSession(msg.taskId);
  const { decision, solverProvider, sitekey, handoffId } = msg;

  switch (decision) {
    case 'SolveAndRetry': {
      const result = await captchaSolver.solve(session.page, { type: 'recaptcha_v2', sitekey }, msg.taskId);
      if (result.success) {
        await TokenInjector.inject(session.page, 'recaptcha_v2', result.token);
        sendToBridge({ type: 'stealth.friction.solved', taskId: msg.taskId,
          signalId: msg.signalId, success: true, token: result.token,
          provider: result.provider, durationMs: result.durationMs, cost: result.cost });
      } else {
        sendToBridge({ type: 'stealth.friction.solved', taskId: msg.taskId,
          signalId: msg.signalId, success: false, error: result.error });
      }
      break;
    }

    case 'RotateAndRetry': {
      await recoveryStrategy.recover(msg.taskId, session);
      // After recovery, bridge will send a new stealth.task or stealth.friction.decision
      break;
    }

    case 'CooldownAndRetry': {
      await new Promise(r => setTimeout(r, msg.cooldownMs || 5000));
      sendToBridge({ type: 'stealth.cooldown.complete', taskId: msg.taskId });
      break;
    }

    case 'RequiresHuman': {
      // Handoff is activated separately via stealth.handoff.activate
      break;
    }

    case 'AllowReadOnly':
    case 'Block':
    case 'FailClosed':
      // Bridge handles these states; Stealth Engine just waits
      break;
  }
}

async function handleHandoffActivate(msg) {
  const session = manager.getSession(msg.taskId);
  // Open a new WS connection to the bridge for the handoff channel
  const handoffWs = new WebSocket(`ws://${bridgeHost}:8788/handoff/${msg.handoffId}`);
  await handoffServer.startHandoff(msg.taskId, session.page, handoffWs);
}
```

---

## 7. Updated Phased Plan

### Phase 0: Unification Foundation (2 weeks) — **NEW, inserted before all other phases**

| Week | Deliverables |
|---|---|
| 0.1 | Create `UnifiedFrictionSignalContracts.cs`, `UnifiedFrictionPolicyDecision.cs`, `IUnifiedFrictionPolicyEngine.cs` |
| 0.2 | Implement `UnifiedFrictionPolicyEngine`: wraps `BrowserCredentialBoundaryDetector.Decide()` + stealth overrides (`SolveAndRetry`, `RotateAndRetry`, etc.) |
| 0.2 | Implement `FrictionSignalRouter`: normalizes companion `hasCaptchaLike/hasTwoFactorLike/hasPasswordField` → `FrictionSignal`; normalizes stealth WS signals → `FrictionSignal` |
| 0.2 | Add `RetryWithBackoff` to `BrowserCredentialBoundaryDecisionKind` enum |
| 0.2 | Wire up `EvidenceLedger` in unified engine |

### Phase 1: Stealth Engine Refactoring + MVP (4 weeks)

| Week | Deliverables |
|---|---|
| 1.1 | Refactor `CaptchaDetector.js` → emit `stealth.friction.signal`, never decide |
| 1.1 | Refactor `BlockDetector.js` → same |
| 1.2 | Refactor `index.js` → handle `stealth.friction.decision` and `stealth.handoff.activate` |
| 1.2 | Implement `StealthHandoffGateway` in .NET |
| 1.3 | Bridge infrastructure: `StealthProtocol.cs`, `StealthTaskManager`, `/ws/stealth`, dispatch in `POST /api/runs` |
| 1.3 | `CaptchaSolver.js` → executed on `SolveAndRetry` decision, reports `stealth.friction.solved` |
| 1.4 | `StealthSession.js` (launch + initScript partial), basic tools | 

### Phase 2: Full Fingerprinting + Companion Unification (3 weeks)

| Week | Deliverables |
|---|---|
| 2.1 | Complete fingerprinting (WebGL, Canvas, Audio, CDP bypass) |
| 2.2 | Companion side: dual-path friction reporting (existing `ShouldPauseForCredentialEntry` + new `FrictionSignal` path via unified engine) |
| 2.3 | `StealthHandoffGateway` → activate `RemoteHandoffServer`, verification via `NodalOsAssistedVerificationPolicy` |

### Phase 3: Proxy Rotation + Anti-blocking (3 weeks)

| Week | Deliverables |
|---|---|
| 3.1 | `ProxyManager.js`, `RecoveryStrategy` → executed on `RotateAndRetry` decision |
| 3.2 | `UnifiedFrictionPolicyEngine` → implement stealth overrides (429 → `CooldownAndRetry`, 503 → `RotateAndRetry`, DataDome → `RotateAndRetry`) |
| 3.3 | `DomainBlacklist.js` — learning from repeated friction signals |

### Phase 4: Human Behavior + Scalability (3 weeks) — unchanged from Stealth Engine Design

---

## 8. Files to Modify / Create

### .NET Bridge (`src/OneBrain.ChromeLab.Bridge/`)

| Action | File | Purpose |
|---|---|---|
| **MODIFY** | `Program.cs` | Route friction signals + decisions, dual-path companion handoff, stealth handoff activation |
| **EXTEND** | `src/OneBrain.BrowserExecutor.Contracts/BrowserCredentialBoundaryContracts.cs` | Add `RetryWithBackoff` to enum |
| **CREATE** | `Stealth/UnifiedFrictionSignalContracts.cs` | `FrictionSignal`, `FrictionSignalKind`, `FrictionSignalSeverity` |
| **CREATE** | `Stealth/UnifiedFrictionPolicyDecision.cs` | `UnifiedFrictionDecisionKind`, `UnifiedFrictionPolicyDecision` |
| **CREATE** | `Stealth/IUnifiedFrictionPolicyEngine.cs` | Interface |
| **CREATE** | `Stealth/UnifiedFrictionPolicyEngine.cs` | Implementation using existing `BrowserCredentialBoundaryDetector` |
| **CREATE** | `Stealth/FrictionSignalRouter.cs` | Signal normalization from both modes |
| **CREATE** | `Stealth/StealthHandoffGateway.cs` | Stealth handoff channel adapter |
| **CREATE** | `Stealth/HandoffVerificationService.cs` | Post-handoff verification using `NodalOsAssistedVerificationPolicy` |
| **CREATE** | `Stealth/StealthProtocol.cs` | Extended with `stealth.friction.*` and `stealth.handoff.*` message types |
| **CREATE** | `Stealth/StealthTaskManager.cs` | Task lifecycle |
| **CREATE** | `Stealth/StealthRunnerRegistry.cs` | Runner connection management |

### Stealth Engine (`stealth-engine/src/`)

| Action | File | Purpose |
|---|---|---|
| **REFACTOR** | `captcha/CaptchaDetector.js` | Remove decisions, emit structured friction signal |
| **REFACTOR** | `antiBlocking/BlockDetector.js` | Remove decisions, emit structured friction signal |
| **REFACTOR** | `captcha/CaptchaSolver.js` | Receive decisions, report results |
| **REFACTOR** | `handoff/RemoteHandoffServer.js` | Activate on bridge command, report completion |
| **REFACTOR** | `index.js` | Full message routing for unified protocol |
| **MODIFY** | `tools/toolExecutor.js` | Always run detectors after observe/navigate |
| **NO CHANGE** | `fingerprint/*` | Fingerprinting is pure execution |
| **NO CHANGE** | `behavior/*` | Human simulation is pure execution |
| **NO CHANGE** | `proxy/*` | Proxy management is execution |
| **NO CHANGE** | `StealthSession.js` | Browser lifecycles unchanged |
| **NO CHANGE** | `StealthBrowserManager.js` | Pool management unchanged |
