# NODAL OS — Configuration Guide

## Bridge Configuration (chrome-lab.local.json)

```json
{
  "OpenAiApiKey": "",
  "ExtensionToken": "nexa_...",
  "Host": "127.0.0.1",
  "Port": 8787,
  "AllowLan": false,
  "stealth": {
    "StealthEnabled": false,
    "StealthRunnerPort": 8788,
    "StealthMaxSessions": 3,
    "FingerprintProfile": "desktop-win-chrome",
    "CaptchaTwoCaptchaApiKey": "",
    "CaptchaAntiCaptchaApiKey": "",
    "CaptchaCapSolverApiKey": "",
    "CaptchaMaxAttempts": 4,
    "StealthMaxRetries": 4,
    "StealthCooldownMs": 5000
  }
}
```

| Key | Default | Description |
|-----|---------|-------------|
| `OpenAiApiKey` | "" | OpenAI API key for the AI agent |
| `ExtensionToken` | auto-generated | Token for Chrome extension auth |
| `Host` | 127.0.0.1 | Bind address |
| `Port` | 8787 | HTTP/WS port |
| `stealth.StealthEnabled` | false | Enable stealth mode |
| `stealth.StealthMaxSessions` | 3 | Max concurrent stealth sessions |
| `stealth.StealthMaxRetries` | 4 | Max recovery attempts on block |
| `stealth.StealthCooldownMs` | 5000 | Backoff base for cooldown |

## Stealth Engine Configuration (stealth.default.json)

| Section | Key | Default | Description |
|---------|-----|---------|-------------|
| `fingerprint` | `defaultPreset` | desktop-win-chrome | desktop-win-chrome, desktop-mac-chrome, mobile-android-chrome |
| `fingerprint` | `autoRotate` | false | Auto-rotate fingerprint on block |
| `fingerprint` | `canvasNoise` | true | Add noise to canvas fingerprint |
| `fingerprint` | `audioNoise` | true | Add noise to AudioContext |
| `fingerprint` | `webglSpoof` | true | Spoof WebGL vendor/renderer |
| `fingerprint` | `webrtcBlock` | true | Block WebRTC IP leaks |
| `proxy` | `enabled` | false | Enable proxy rotation |
| `proxy` | `rotationMode` | sticky | sticky or random |
| `proxy` | `staticProxies` | [] | Manual proxy list |
| `proxy` | `healthCheckIntervalMs` | 60000 | Proxy health check interval |
| `antiBlocking` | `maxRecoveryAttempts` | 5 | Max recovery retries |
| `antiBlocking` | `baseBackoffMs` | 5000 | Initial backoff |
| `antiBlocking` | `maxBackoffMs` | 120000 | Max backoff |
| `behavior` | `defaultProfile` | casual | casual, pro, elderly |
| `visualCaptcha` | `enabled` | false | Enable visual CAPTCHA solver |
| `visualCaptcha` | `ocrEngine` | tesseract | OCR backend |

## Environment Variables

| Variable | Config equivalent |
|----------|------------------|
| `OPENAI_API_KEY` | OpenAiApiKey |
| `NODAL_OS_CHROME_BRIDGE_TOKEN` | ExtensionToken |
| `BRIDGE_HOST` | Host (stealth engine) |
| `BRIDGE_PORT` | Port (stealth engine) |
| `STEALTH_MAX_SESSIONS` | StealthMaxSessions |
| `STEALTH_PROXY_ENABLED` | proxy.enabled |

## Typical Configurations

### Development (no proxy, no CAPTCHA solver)
```json
{ "stealth": { "StealthEnabled": true, "StealthMaxSessions": 1 } }
```

### Production with 2captcha
```json
{ "stealth": { "StealthEnabled": true, "CaptchaTwoCaptchaApiKey": "your_key", "StealthMaxSessions": 5 } }
```

### Full production with residential proxy
```json
{ "stealth": { "StealthEnabled": true, "CaptchaTwoCaptchaApiKey": "your_key", "StealthMaxSessions": 5 } }
```
+ stealth.default.json proxy section with enabled: true and staticProxies populated
