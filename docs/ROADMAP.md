# NODAL OS — Roadmap

## Implemented (Fases 0-4)

| Phase | Feature | Status |
|-------|---------|--------|
| 0 | Unified friction detection layer (.NET) | Done |
| 0 | UnifiedFrictionPolicyEngine | Done |
| 0 | FrictionSignalRouter (companion + stealth) | Done |
| 0 | StealthHandoffGateway | Done |
| 1 | Stealth Engine MVP (Playwright + Chromium) | Done |
| 1 | Basic fingerprinting (webdriver, plugins, chrome) | Done |
| 1 | CAPTCHA detection + 2captcha solver | Done |
| 1 | Remote handoff server | Done |
| 2 | Advanced fingerprinting (WebGL, Canvas, Audio, WebRTC) | Done |
| 2 | Proxy manager with rotation | Done |
| 2 | Anti-blocking recovery strategy | Done |
| 2 | Human behavior simulation (Bézier mouse, typing, scroll) | Done |
| 3 | Visual CAPTCHA solver (OCR + AI vision) | Done |
| 3 | Adaptive behavior engine | Done |
| 3 | Fingerprint evolution pipeline | Done |
| 4 | Session pool with concurrency limits | Done |
| 4 | Docker Compose (3 services) | Done |
| 4 | Health checks + Prometheus metrics | Done |
| 4 | Stealth panel (viewport + relay) | Done |
| 4 | Documentation (5 docs) | Done |
| 4 | Deployment scripts | Done |

## Future Enhancements

### DeepBehaviorEngine
- Markov chain-based navigation patterns
- GAN-generated mouse trajectories
- Real-world browsing session replay

### Auto-Fingerprint Evolution
- Automated fingerprint testing against real browsers
- Self-healing injector scripts
- Continuous scoring vs browserleaks/fingerprint.com

### External Stealth Browser Providers
- Integration with Bright Data Scraping Browser
- Integration with Oxylabs Web Unblocker
- Integration with browserless.io

### Multi-Runner Scaling
- Kubernetes deployment
- Horizontal autoscaling based on queue depth
- Distributed proxy pool (Redis-backed)

### Known Limitations
- Playwright `headless: false` requires a display in Docker (uses Xvfb)
- Fingerprint evolution is manual (requires operator review)
- CAPTCHA AI vision requires paid API keys
- Proxy health checks rely on external httpbin.org
