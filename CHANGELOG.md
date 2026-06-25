# NODAL OS — Changelog

## v1.0.0-production (2026-06-25)

### Phase 0 — Unification
- UnifiedFrictionPolicyEngine: single policy gate for companion + stealth
- FrictionSignalRouter: normalizes friction signals from both modes
- StealthHandoffGateway: stealth-mode handoff activation
- Extended BrowserCredentialBoundaryDecisionKind with RetryWithBackoff

### Phase 1 — Stealth Engine MVP
- Playwright-based headful Chromium with fingerprint camouflage
- CAPTCHA detection (DOM + frames + text) and 2captcha solver
- Remote handoff server with viewport streaming
- Basic tools: observePage, navigate, click, setValue

### Phase 2 — Advanced Fingerprinting & Anti-Blocking
- Complete fingerprint injection: WebGL, Canvas, AudioContext, WebRTC, permissions
- Proxy manager with sticky sessions, rotation, health checks
- Anti-blocking recovery with exponential backoff
- Human behavior: Bézier mouse, keyboard errors, adaptive scroll

### Phase 3 — Visual CAPTCHA & Adaptive Behavior
- VisualCaptchaSolver: OCR (Tesseract) + AI vision (GPT-4V)
- AdaptiveBehaviorEngine: session-unique parameter variance
- Ballistic mouse trajectories for long distances
- FingerprintEvolutionPipeline: offline audit tool

### Phase 4 — Production Readiness
- StealthBrowserManager: concurrent session pool with queue
- Docker Compose: bridge + stealth-engine + stealth-panel
- Health checks and Prometheus metrics
- Complete documentation (ARCHITECTURE, CONFIGURATION, DEPLOYMENT, OPERATIONS, ROADMAP)
- Deployment scripts for Windows, Linux, macOS
