# NODAL OS — Architecture

## System Overview

NODAL OS is a bimodal browser automation system with two execution modes sharing a unified friction detection and policy layer.

```
┌─────────────────────────────────────────────┐
│                  NODAL OS                    │
│                                              │
│  ┌─ COMPANION MODE ──────────────────────┐  │
│  │  Chrome MV3 Extension                  │  │
│  │  (service_worker + content_script)     │  │
│  │          │ WS /ws/extension            │  │
│  └──────────┼─────────────────────────────┘  │
│             │                                 │
│  ┌──────────┴─────────────────────────────┐  │
│  │     OneBrain.ChromeLab.Bridge (.NET)   │  │
│  │                                         │  │
│  │  POST /api/runs  → Mode dispatch       │  │
│  │  /ws/extension   → Companion WS         │  │
│  │  /ws/stealth     → Stealth WS           │  │
│  │  /health         → Health status        │  │
│  │  /metrics        → Prometheus metrics   │  │
│  │                                         │  │
│  │  UnifiedFrictionPolicyEngine            │  │
│  │  StealthTaskManager                     │  │
│  │  StealthRunnerRegistry                  │  │
│  └──────────┬─────────────────────────────┘  │
│             │                                 │
│  ┌──────────┴─────────────────────────────┐  │
│  │  STEALTH MODE                           │  │
│  │  ┌───────────────────────────────────┐ │  │
│  │  │ Stealth Engine (Node.js)          │ │  │
│  │  │  Playwright + Chromium            │ │  │
│  │  │  Fingerprinting                   │ │  │
│  │  │  CAPTCHA Solver                   │ │  │
│  │  │  Human Behavior Simulation        │ │  │
│  │  │  Proxy Manager                    │ │  │
│  │  │  Anti-Blocking Recovery           │ │  │
│  │  │  Remote Handoff Server            │ │  │
│  │  └───────────────────────────────────┘ │  │
│  │                                         │  │
│  │  ┌───────────────────────────────────┐ │  │
│  │  │ Stealth Panel (HTML/JS)           │ │  │
│  │  │  Viewport streaming               │ │  │
│  │  │  Mouse/keyboard relay             │ │  │
│  │  └───────────────────────────────────┘ │  │
│  └─────────────────────────────────────────┘  │
└──────────────────────────────────────────────┘
```

## Components

### OneBrain.ChromeLab.Bridge (.NET 9 / net11.0)
Kestrel web server exposing REST + WebSocket endpoints. Orchestrates both modes.

### Chrome Extension (MV3)
Injects content scripts into the user's real Chrome browser. Executes tools (click, type, navigate) and reports observations. Pauses for human intervention on credentials/CAPTCHA/2FA.

### Stealth Engine (Node.js + Playwright)
Headful Chromium with full fingerprint camouflage. Reports frictions to the bridge; never decides autonomously.

### Stealth Panel
Web-based remote control for human takeover of stealth sessions. Receives viewport screenshots, relays mouse/keyboard events.

## Key Flows

### Companion Task
1. POST /api/runs { mode: "lab" }
2. Bridge sends tool.request to extension via /ws/extension
3. Extension executes in user's browser
4. Bridge evaluates observation with OpenAI, dispatches next tool
5. On credential/CAPTCHA → pause and show handoff in sidepanel

### Stealth Task  
1. POST /api/runs { mode: "stealth" }
2. Bridge selects profile, acquires proxy, sends stealth.task to runner
3. Runner launches Chromium, injects camouflage, navigates
4. On friction detection → stealth.friction.signal to bridge
5. Bridge evaluates via UnifiedFrictionPolicyEngine → stealth.friction.decision
6. Runner executes decision (solve, rotate, handoff)

### Human Handoff (Stealth)
1. UnifiedFrictionPolicyEngine returns RequiresHuman
2. StealthHandoffGateway activates RemoteHandoffServer
3. Operator connects via stealth-panel, sees viewport
4. Operator resolves CAPTCHA/2FA manually
5. Operator clicks "Done" → verification → resume

## WebSocket Protocol

### /ws/extension (companion)
- extension.hello → engine.hello
- extension.ping → engine.pong
- tool.result → tool.request

### /ws/stealth (stealth)
- stealth.hello → stealth.ack
- stealth.task → stealth.result
- stealth.friction.signal → stealth.friction.decision
- stealth.friction.solved
- stealth.handoff.activate → stealth.handoff.completed
