# NODAL OS — Stealth Engine: Diseño Técnico Bimodal

> **Versión:** 1.0  
> **Fecha:** 2026-06-25  
> **Arquitecto:** Diego (NODAL OS Team)  
> **Principio rector:** El modo companion existente (extensión Chrome MV3 + bridge .NET) permanece **INTOCADO**. El nuevo modo sigiloso es un **añadido paralelo** activado por `Mode = "stealth"` en `StartRunRequest`.

---

## 1. Análisis de lo existente

### 1.1 Componentes reutilizados SIN cambios

| Componente | Archivos | Estado |
|---|---|---|
| **Extensión Chrome MV3 completa** | `browser-extension/onebrain-chrome-lab/` (manifest.json, service_worker.js, content_script.js, sidepanel, recipe_core.js) | **INTOCCABLE**. Todo companion intacto. |
| **Contratos `OneBrain.BrowserExecutor.Contracts`** | 97 archivos .cs | Referencia, sin modificar. |
| **Tipos de dominio del Bridge** | `StartRunRequest`, `ToolRequest`, `AgentToolDecision`, `ChromeLabRunManager`, `ChromeLabClientRegistry`, `ProtocolEventBuffer`, `PendingToolRequestRegistry` | Intactas. |
| **Configuración** | `ChromeLabOptions` — lógica de carga, CLI, env vars | Intacta. Se añaden propiedades opcionales. |
| **Seguridad** | `ChromeLabSecurity` (tool validation, URLs) | Intacta. |
| **Cliente OpenAI** | `OpenAiAgentClient` | Uso en modo companion intacto. |

### 1.2 Puntos de inserción de ganchos (no intrusivos)

| Punto | Archivo:Línea | Cambio |
|---|---|---|
| **A** | `Program.cs` ~L117 (`POST /api/runs`) | Añadir `if (request.Mode == "stealth")` → `HandleStealthRunStart()`. Flujo companion intacto. |
| **B** | `Program.cs` ~L375 (`tool.result`) | Sin cambios. Resultados stealth via `/ws/stealth`. |
| **C** | `Program.cs` (nuevo) | Endpoint `/ws/stealth`, paralelo a `/ws/extension`. |
| **D** | `StartRunRequest` | YA tiene campo `Mode = "lab"`. Sin modificar. |
| **E** | Extensión Chrome | TOBA. El panel stealth es UI separada. |

### 1.3 Lo que NO se toca

- content_script.js — solo companion
- service_worker.js — solo companion
- Inyección de scripts en la extensión
- Handoff humano actual (sidepanel banner, pause/resume via extensión)
- Protocolo extensión↔bridge
- Recetas, aprendizaje, runner legacy

---

## 2. Arquitectura general bimodal

```
┌──────────────────────────────────────────────────────────────────┐
│                      NODAL OS — BIMODAL                          │
│                                                                  │
│ ┌── COMPANION MODE (existente, intocado) ──────────────────────┐ │
│ │  Chrome MV3 Extension                                         │ │
│ │  ┌──────────┐ ┌──────────┐ ┌───────────┐                    │ │
│ │  │ SW (2792)│ │ CS (1865)│ │ SidePanel │                    │ │
│ │  └────┬─────┘ └────┬─────┘ └─────┬─────┘                    │ │
│ │       └──────────────┼──────────────┘                         │ │
│ │                 WS /ws/extension (8787)                        │ │
│ └──────────────────────┼────────────────────────────────────────┘ │
│                         │                                         │
│       ╔═════════════════╧═════════════════╗                       │
│       ║ OneBrain.ChromeLab.Bridge (.NET) ║                       │
│       ║                                 ║                       │
│       ║ POST /api/runs                  ║                       │
│       ║  Mode = "lab"     → Companion   ║                       │
│       ║  Mode = "stealth" → Stealth     ║                       │
│       ║                                 ║                       │
│       ║ /ws/extension  (existente)      ║                       │
│       ║ /ws/stealth    (NUEVO)          ║                       │
│       ╚═════════════════╤═══════════════╝                       │
│                         │                                        │
│ ┌── STEALTH MODE (NUEVO) ─────────────────────────────────────┐ │
│ │                                                              │ │
│ │  ┌──────────────────────────────────┐                        │ │
│ │  │ StealthRunner (Node.js)          │  Playwright + Chromium │ │
│ │  │  • StealthBrowserManager (pool)  │                        │ │
│ │  │  • CaptchaSolver (2captcha/etc)  │                        │ │
│ │  │  • HumanBehaviorSim (mouse/kb)   │                        │ │
│ │  │  • RemoteHandoffServer (CDP)     │                        │ │
│ │  └────────────┬─────────────────────┘                        │ │
│ │               │ WS /ws/stealth                               │ │
│ └───────────────┼──────────────────────────────────────────────┘ │
│                 │                                                 │
│       ┌─────────┴──────────┐                                      │
│       │  Web Panel Stealth  │  Nueva UI (servida por el bridge)   │
│       └────────────────────┘                                      │
└──────────────────────────────────────────────────────────────────┘
```

### 2.1 Protocolo de comunicación Stealth Bridge ↔ Runner

WebSocket en `ws://{host}:{port}/ws/stealth`.

**Handshake:**
```json
// Runner → Bridge
{ "type": "stealth.hello", "protocolVersion": "stealth-v1", "runnerId": "runner-<guid>",
  "capabilities": ["stealthBrowser", "captchaSolving", "proxyRotation", "remoteHandoff"] }

// Bridge → Runner
{ "type": "stealth.ack", "runnerId": "runner-<guid>", "serverTime": "..." }
```

**Task (Bridge → Runner):**
```json
{ "type": "stealth.task", "taskId": "<guid>", "instruction": "...",
  "profile": { "preset": "desktop-win-chrome" },
  "proxy": { "url": "http://user:pass@proxy:8080", "type": "residential", "country": "US" },
  "captchaConfig": { "provider": "2captcha", "apiKey": "..." },
  "maxRetries": 3, "sessionPersistence": true }
```

**Result (Runner → Bridge):**
```json
{ "type": "stealth.result", "taskId": "<guid>", "stepId": "<guid>",
  "tool": "observePage", "success": true,
  "result": { "url": "...", "title": "...", "hasCaptchaLike": true, ... },
  "captchaDetected": { "type": "recaptcha_v2", "sitekey": "...", "solved": true, "solver": "2captcha" },
  "blockDetection": { "blocked": false } }
```

**Handoff Request (Runner → Bridge):**
```json
{ "type": "stealth.handoff.request", "taskId": "<guid>",
  "reason": "captcha_unsolved", "captchaType": "recaptcha_v2_enterprise",
  "attempts": 4, "screenshotBase64": "...", "pageUrl": "https://example.com/login" }
```

**Pause/Resume/Stop (Bridge → Runner):**
```json
{ "type": "stealth.pause", "taskId": "<guid>", "reason": "humanTakeoverRequired" }
{ "type": "stealth.resume", "taskId": "<guid>" }
{ "type": "stealth.stop", "taskId": "<guid>", "reason": "userStop" }
```

### 2.2 Flujo de ejecución — modo sigiloso

```
1. POST /api/runs { instruction: "...", mode: "stealth" }
2. Bridge detecta mode == "stealth"
3. Bridge: selecciona perfil de huella (StealthProfileService)
4. Bridge: asigna proxy del pool (ProxyManager)
5. Bridge: envía "stealth.task" al StealthRunner vía /ws/stealth
6. StealthRunner:
   a. Inicializa Chromium con launch() + perfil + proxy
   b. Inyecta scripts de camuflaje (addInitScript ×7)
   c. Navega a la URL extraída de la instrucción
   d. Observa página → detecta CAPTCHA → intenta resolver
   e. Envía "stealth.result" al bridge
7. Bridge recibe resultado:
   a. CAPTCHA resuelto → loop: OpenAI → decisión → próxima tool
   b. CAPTCHA sin resolver → handoff o rotación
   c. Bloqueo detectado → recuperación (rotar proxy+perfil, reintentar)
   d. Todo OK → loop continúa hasta completar/error/pausa
8. El loop del companion NO se modifica en absoluto.
```

---

## 3. Diseño del Stealth Engine (Node.js + Playwright)

### 3.1 Estructura del proyecto `stealth-engine/`

```
stealth-engine/
├── package.json
├── src/
│   ├── index.js                   # Entry point — WS client + dispatcher
│   ├── StealthBrowserManager.js   # Pool de navegadores, ciclo de vida
│   ├── StealthSession.js          # Sesión: browser context + page + proxy
│   ├── fingerprint/
│   │   ├── FingerprintProfile.js      # Modelo de perfil
│   │   ├── FingerprintGenerator.js    # Generador sintético coherente
│   │   ├── FingerprintInjector.js     # addInitScripts completos
│   │   └── profiles/                  # JSON predefinidos
│   ├── captcha/
│   │   ├── CaptchaDetector.js         # Detección (DOM + frames + text + network)
│   │   ├── CaptchaSolver.js           # Fachada multi-proveedor
│   │   ├── TokenInjector.js           # Inyección de tokens en DOM
│   │   └── solvers/ (2captcha, anticaptcha, capsolver)
│   ├── behavior/
│   │   ├── HumanMouse.js              # Bézier + jitter + overshoot
│   │   ├── HumanKeyboard.js           # Velocidad variable + errores
│   │   ├── HumanScroll.js             # Scroll suave + pauses
│   │   └── HumanNavigation.js         # Tiempos de lectura
│   ├── proxy/
│   │   ├── ProxyManager.js            # Pool + rotación + sticky
│   │   ├── ProxyHealthChecker.js      # Verificación de proxies
│   │   └── ProxyCoherence.js          # Coherencia proxy ↔ perfil
│   ├── antiBlocking/
│   │   ├── BlockDetector.js           # 403, 429, Cloudflare, DataDome, etc.
│   │   ├── RecoveryStrategy.js        # Rotación + backoff + reintentos
│   │   └── DomainBlacklist.js         # Dominios problemáticos
│   ├── handoff/
│   │   ├── RemoteHandoffServer.js     # Streaming viewport + input relay
│   │   └── ScreenshotCapture.js
│   └── tools/
│       ├── tools.js                   # Implementaciones (observePage, click, etc.)
│       └── toolExecutor.js            # Dispatcher
├── tests/
│   └── stealth-suite.test.js          # Suite de validación de sigilo
└── config/
    └── stealth.default.json
```

### 3.2 package.json

```json
{
  "name": "nodalos-stealth-engine",
  "version": "0.1.0",
  "type": "module",
  "private": true,
  "dependencies": {
    "playwright": "^1.52.0",
    "playwright-extra": "^4.3.6",
    "puppeteer-extra-plugin-stealth": "^2.11.2",
    "ws": "^8.18.0",
    "2captcha": "^3.0.1",
    "capsolver-npm": "^1.1.1",
    "p-limit": "^6.1.0"
  },
  "scripts": {
    "start": "node src/index.js",
    "test": "node --test tests/*.test.js",
    "install-browsers": "npx playwright install chromium"
  }
}
```

### 3.3 Inicialización del navegador headless con sigilo completo

```javascript
// src/StealthSession.js
import { chromium } from 'playwright';
import { FingerprintInjector } from './fingerprint/FingerprintInjector.js';

export class StealthSession {
  constructor(config) {
    this.taskId = config.taskId;
    this.instruction = config.instruction;
    this.profile = config.profile;
    this.proxy = config.proxy;
    this.browser = null;
    this.context = null;
    this.page = null;
  }

  async initialize() {
    this.browser = await chromium.launch({
      headless: false,  // Headful for anti-bot evasion
      channel: 'chromium',
      args: this.buildLaunchArgs(),
      ignoreDefaultArgs: [
        '--enable-automation',
        '--disable-component-extensions-with-background-pages'
      ]
    });

    const ctxOpts = {
      viewport: this.profile.viewport,
      deviceScaleFactor: this.profile.deviceScaleFactor || 1,
      isMobile: this.profile.isMobile || false,
      hasTouch: this.profile.hasTouch || false,
      userAgent: this.profile.userAgent,
      locale: this.profile.locale,
      timezoneId: this.profile.timezone,
      geolocation: this.profile.geolocation,
      permissions: this.profile.permissions || [],
      bypassCSP: true,
      ignoreHTTPSErrors: true
    };

    if (this.proxy) {
      ctxOpts.proxy = {
        server: this.proxy.server,
        username: this.proxy.username || undefined,
        password: this.proxy.password || undefined
      };
    }

    this.context = await this.browser.newContext(ctxOpts);

    // —— INJECT CAMOUFLAGE BEFORE ANY PAGE LOAD ——
    const injector = new FingerprintInjector(this.profile);
    await this.context.addInitScript(injector.getInitScript());
    await this.context.addInitScript(FingerprintInjector.getCdpBypassScript());
    await this.context.addInitScript(FingerprintInjector.getWebRtcBlockerScript());
    await this.context.addInitScript(FingerprintInjector.getCanvasNoiseScript());
    await this.context.addInitScript(FingerprintInjector.getAudioNoiseScript());
    await this.context.addInitScript(FingerprintInjector.getPermissionsSpoofer());

    this.page = await this.context.newPage();
    await this.applyCdpPatches(this.page);

    return this;
  }

  buildLaunchArgs() {
    const args = [
      '--no-sandbox', '--disable-setuid-sandbox',
      '--disable-infobars',
      '--disable-blink-features=AutomationControlled',
      '--disable-features=IsolateOrigins,site-per-process',
      '--no-first-run', '--no-default-browser-check',
      '--disable-background-networking', '--disable-sync',
      '--disable-default-apps', '--hide-scrollbars',
      '--mute-audio', '--disable-hang-monitor',
      '--disable-prompt-on-repost',
      '--disable-client-side-phishing-detection',
      '--disable-component-update',
      `--window-size=${this.profile.viewport.width},${this.profile.viewport.height}`
    ];
    if (this.proxy?.server) args.push(`--proxy-server=${this.proxy.server}`);
    return args;
  }

  async applyCdpPatches(page) {
    const cdp = await page.context().newCDPSession(page);
    await cdp.send('Page.addScriptToEvaluateOnNewDocument', { source: `
      Object.defineProperty(navigator, 'webdriver', {
        get: () => undefined, configurable: true, enumerable: true });
      window.chrome = {
        runtime: {
          connect: () => ({ onDisconnect: { addListener: ()=>{}, disconnect: ()=>{} } }),
          sendMessage: () => {}, getManifest: () => ({}),
          getURL: p => 'chrome-extension://' + p, id: undefined
        },
        loadTimes: () => ({}), csi: () => ({}),
        app: { isInstalled: false,
          InstallState: { DISABLED:'disabled', INSTALLED:'installed', NOT_INSTALLED:'not_installed' },
          RunningState: { CANNOT_RUN:'cannot_run', READY_TO_RUN:'ready_to_run', RUNNING:'running' } }
      };
      delete window.__playwright__binding__;
      delete window.__pw_manual__;
    `});
    await cdp.detach();
  }

  async navigate(url) {
    return this.page.goto(url, { waitUntil: 'domcontentloaded', timeout: 45000 });
  }

  async dispose() {
    if (this.context) await this.context.close();
    if (this.browser) await this.browser.close();
  }
}
```

### 3.4 Perfiles de huella

**Modelo:**
```javascript
// src/fingerprint/FingerprintProfile.js
export class FingerprintProfile {
  constructor(data) {
    this.profileId = data.profileId || crypto.randomUUID();
    this.deviceType = data.deviceType;    // 'desktop'|'mobile'
    this.os = data.os;                     // 'Windows'|'macOS'|'Android'|'iOS'
    this.viewport = data.viewport;         // {width,height}
    this.userAgent = data.userAgent;
    this.platform = data.platform;
    this.languages = data.languages;       // ['en-US','en']
    this.hardwareConcurrency = data.hardwareConcurrency;
    this.deviceMemory = data.deviceMemory;
    this.plugins = data.plugins;
    this.mimeTypes = data.mimeTypes;
    this.webglVendor = data.webglVendor;
    this.webglRenderer = data.webglRenderer;
    this.timezone = data.timezone;
    this.locale = data.locale;
    this.geolocation = data.geolocation;
    this.country = data.country;
  }

  static COUNTRY_MAPPING = {
    'US': { timezone:'America/New_York', locale:'en-US', languages:['en-US','en'], geolocation:{latitude:40.71,longitude:-74.0} },
    'AR': { timezone:'America/Argentina/Buenos_Aires', locale:'es-AR', languages:['es-AR','es'], geolocation:{latitude:-34.6,longitude:-58.38} },
    'GB': { timezone:'Europe/London', locale:'en-GB', languages:['en-GB','en'], geolocation:{latitude:51.5,longitude:-0.12} },
    'DE': { timezone:'Europe/Berlin', locale:'de-DE', languages:['de-DE','de','en'], geolocation:{latitude:52.52,longitude:13.4} },
    'BR': { timezone:'America/Sao_Paulo', locale:'pt-BR', languages:['pt-BR','pt'], geolocation:{latitude:-23.5,longitude:-46.6} },
    'ES': { timezone:'Europe/Madrid', locale:'es-ES', languages:['es-ES','es'], geolocation:{latitude:40.4,longitude:-3.7} },
    'MX': { timezone:'America/Mexico_City', locale:'es-MX', languages:['es-MX','es'], geolocation:{latitude:19.4,longitude:-99.1} },
  };

  static ensureCoherence(profile, proxyCountry) {
    const m = FingerprintProfile.COUNTRY_MAPPING[proxyCountry];
    if (!m) return profile;
    return new FingerprintProfile({...profile, ...m, country:proxyCountry});
  }
}
```

**Generador sintético:**
```javascript
// src/fingerprint/FingerprintGenerator.js
import { FingerprintProfile } from './FingerprintProfile.js';

const UA = {
  'desktop-win-chrome': [
    'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36',
    'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36',
  ],
  'desktop-mac-chrome': [
    'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36',
  ]
};

const WEBGL = [
  { vendor:'Google Inc. (Intel)', renderer:'ANGLE (Intel, Intel(R) UHD Graphics 620 Direct3D11 vs_5_0 ps_5_0, D3D11)' },
  { vendor:'Google Inc. (NVIDIA)', renderer:'ANGLE (NVIDIA, NVIDIA GeForce RTX 3060 Direct3D11 vs_5_0 ps_5_0, D3D11)' },
];

const PLUGINS = [
  { name:'Chrome PDF Plugin', filename:'internal-pdf-viewer', description:'Portable Document Format' },
  { name:'Chrome PDF Viewer', filename:'mhjfbmdgcfjbbpaeojofohoefgiehjai', description:'' },
  { name:'Native Client', filename:'internal-nacl-plugin', description:'' },
];

export class FingerprintGenerator {
  static generate(options = {}) {
    const dt = options.deviceType || 'desktop';
    const os = options.os || 'Windows';
    const key = `${dt}-${os.toLowerCase()}-chrome`;
    const uaList = UA[key] || UA['desktop-win-chrome'];
    const vm = options.viewport || { width:1920, height:1040 };
    const webgl = WEBGL[Math.floor(Math.random()*WEBGL.length)];

    return new FingerprintProfile({
      profileId: crypto.randomUUID(),
      deviceType: dt, os, browser:'Chrome', viewport: vm,
      screen: { width:vm.width, height:vm.height+100, availWidth:vm.width, availHeight:vm.height, colorDepth:24, pixelDepth:24 },
      deviceScaleFactor: 1, isMobile: dt==='mobile', hasTouch: dt==='mobile',
      userAgent: uaList[Math.floor(Math.random()*uaList.length)],
      platform: os==='Windows'?'Win32':os==='macOS'?'MacIntel':'Linux x86_64',
      languages: options.languages || ['en-US','en'],
      hardwareConcurrency: [4,8,12,16][Math.floor(Math.random()*4)],
      deviceMemory: [4,8,16,32][Math.floor(Math.random()*4)],
      maxTouchPoints: dt==='mobile'?5:0,
      plugins: PLUGINS,
      mimeTypes: [{type:'application/pdf',suffixes:'pdf'},{type:'text/pdf',suffixes:'pdf'}],
      webglVendor: webgl.vendor, webglRenderer: webgl.renderer,
      webglExtensions: ['ANGLE_instanced_arrays','EXT_blend_minmax','OES_texture_float','WEBGL_depth_texture'],
      audioNoiseFactor: Math.random()*0.0003+0.00005,
      canvasNoiseFactor: Math.random()*0.0002+0.00003,
      timezone: options.timezone||'America/New_York',
      locale: options.locale||'en-US',
      geolocation: options.geolocation||{latitude:40.71,longitude:-74.0},
      country: options.country||'US',
      permissions: ['geolocation','notifications']
    });
  }
}
```

### 3.5 Inyector de huella — `addInitScript` completo

Este es el **corazón del camuflaje**. Se inyecta con `context.addInitScript()` antes de cualquier navegación.

```javascript
// src/fingerprint/FingerprintInjector.js
export class FingerprintInjector {
  constructor(profile) { this.profile = profile; }

  getInitScript() {
    const nav = this.profile;
    return () => {
      // —— Navigator ——
      Object.defineProperty(Navigator.prototype, 'webdriver',
        { get: () => undefined, configurable: true, enumerable: true });

      // Plugins array
      const plugins = []; /* populated below */;

      // —— Screen ——
      ['width','height','availWidth','availHeight','colorDepth','pixelDepth'].forEach(k => {
        Object.defineProperty(Screen.prototype, k, { get: () => nav.screen[k], configurable: true });
      });

      // —— Languages ——
      Object.defineProperty(Navigator.prototype, 'languages',
        { get: () => Object.freeze(nav.languages), configurable: true });
      Object.defineProperty(Navigator.prototype, 'language',
        { get: () => nav.languages[0], configurable: true });

      // —— Hardware ——
      Object.defineProperty(Navigator.prototype, 'hardwareConcurrency',
        { get: () => nav.hardwareConcurrency, configurable: true });
      Object.defineProperty(Navigator.prototype, 'deviceMemory',
        { get: () => nav.deviceMemory, configurable: true });
      Object.defineProperty(Navigator.prototype, 'platform',
        { get: () => nav.platform, configurable: true });
      Object.defineProperty(Navigator.prototype, 'vendor',
        { get: () => 'Google Inc.', configurable: true });

      // —— Cleanup automation props ——
      ['__playwright','__pw_manual__','__PW_inspect__','__nightmare',
       '_phantom','callPhantom','_selenium','webdriver','domAutomation'].forEach(k => {
        try { delete window[k]; } catch(e) {}
      });

      // —— Chrome runtime ——
      if (!window.chrome) {
        window.chrome = {
          runtime: {
            connect: () => ({ onDisconnect:{addListener:()=>{},disconnect:()=>{}} }),
            sendMessage: () => {}, getManifest: () => ({}),
            getURL: p => 'chrome-extension://' + p, id: undefined
          },
          loadTimes: () => ({}), csi: () => ({}),
          app: { isInstalled: false }
        };
      }

      // —— Notification permission ——
      if (Notification) {
        Object.defineProperty(Notification, 'permission',
          { get: () => 'default', configurable: true });
      }
    };
  }

  /** CDP bypass — prevents Runtime.runIfWaitingForDebugger detection */
  static getCdpBypassScript() {
    return () => {
      let protoAccess = 0;
      const orig = Object.getOwnPropertyDescriptor(Object.prototype, '__proto__');
      if (orig) {
        Object.defineProperty(Object.prototype, '__proto__', {
          get() { protoAccess++; if (protoAccess>1000) return {}; return orig.get.call(this); },
          set(v) { return orig.set.call(this, v); },
          configurable: true
        });
      }
    };
  }

  /** Blocks WebRTC IP leaks */
  static getWebRtcBlockerScript() {
    return () => {
      if (window.RTCPeerConnection) {
        const orig = window.RTCPeerConnection;
        window.RTCPeerConnection = function(cfg) {
          if (cfg?.iceServers) cfg.iceServers = cfg.iceServers.map(s=>({...s,urls:[]}));
          return new orig(cfg);
        };
        window.RTCPeerConnection.prototype = orig.prototype;
      }
      if (navigator.mediaDevices?.getUserMedia) {
        navigator.mediaDevices.getUserMedia = () =>
          Promise.reject(new Error('NotAllowedError'));
      }
    };
  }

  /** Adds noise to Canvas toDataURL and getImageData */
  static getCanvasNoiseScript() {
    return () => {
      const noise = canvas => {
        const getCtx = canvas.getContext;
        canvas.getContext = function(type,...args) {
          const ctx = getCtx.call(this,type,...args);
          if (type==='2d' && ctx.getImageData) {
            const orig = ctx.getImageData;
            ctx.getImageData = function(x,y,w,h) {
              const d = orig.call(this,x,y,w,h);
              if (d?.data?.length>4) d.data[~~(Math.random()*(d.data.length-4))] ^= 1;
              return d;
            };
          }
          return ctx;
        };
      };
      try {
        const origCE = document.createElement;
        document.createElement = function(tag,...args) {
          const el = origCE.call(this,tag,...args);
          if (tag.toLowerCase()==='canvas') noise(el);
          return el;
        };
        document.querySelectorAll('canvas').forEach(noise);
      } catch(e){}
    };
  }

  /** AudioContext noise */
  static getAudioNoiseScript() {
    return () => {
      const Ctx = window.AudioContext||window.webkitAudioContext;
      if (!Ctx) return;
      const origOsc = Ctx.prototype.createOscillator;
      Ctx.prototype.createOscillator = function() {
        const osc = origOsc.call(this);
        const origCn = osc.connect;
        osc.connect = function(dest) {
          try { const g = this.context.createGain(); g.gain.value=1+(Math.random()-0.5)*0.0001; origCn.call(this,g); g.connect(dest); return g; } catch(e){}
          return origCn.call(this,dest);
        };
        return osc;
      };
    };
  }

  /** Permission spoofing */
  static getPermissionsSpoofer() {
    return () => {
      if (!navigator.permissions) return;
      const orig = navigator.permissions.query;
      navigator.permissions.query = function(desc) {
        if (desc?.name==='notifications') return Promise.resolve({state:'prompt',onchange:null});
        return orig.call(this,desc);
      };
    };
  }
}
```

### 3.6 Gestión de sesiones y pool

```javascript
// src/StealthBrowserManager.js
import pLimit from 'p-limit';
import { StealthSession } from './StealthSession.js';
import { FingerprintGenerator, FingerprintProfile } from './fingerprint/index.js';
import { ProxyManager } from './proxy/ProxyManager.js';

export class StealthBrowserManager {
  constructor({ maxConcurrent = 5, proxies = [] } = {}) {
    this.maxConcurrent = maxConcurrent;
    this.activeSessions = new Map();
    this.proxyManager = new ProxyManager(proxies);
    this.limiter = pLimit(maxConcurrent);
  }

  async createSession(taskId, instruction, options = {}) {
    const proxy = this.proxyManager.acquire(taskId, options);

    let profile;
    if (options.fingerprintProfile) {
      profile = FingerprintProfile.ensureCoherence(options.fingerprintProfile, proxy?.country);
    } else {
      profile = FingerprintGenerator.generate({
        deviceType: options.deviceType||'desktop',
        os: options.os||'Windows',
        country: proxy?.country,
        languages: proxy?.country ?
          FingerprintProfile.COUNTRY_MAPPING[proxy.country]?.languages : ['en-US','en'],
        timezone: proxy?.country ?
          FingerprintProfile.COUNTRY_MAPPING[proxy.country]?.timezone : 'America/New_York',
        locale: proxy?.country ?
          FingerprintProfile.COUNTRY_MAPPING[proxy.country]?.locale : 'en-US',
        geolocation: proxy?.country ?
          FingerprintProfile.COUNTRY_MAPPING[proxy.country]?.geolocation : null
      });
    }

    const session = new StealthSession({
      taskId, instruction, profile,
      proxy: proxy?.url ? { server: proxy.url, username: proxy.username, password: proxy.password } : null
    });

    await session.initialize();
    this.activeSessions.set(taskId, session);
    return session;
  }

  async destroySession(taskId) {
    const s = this.activeSessions.get(taskId);
    if (s) {
      this.activeSessions.delete(taskId);
      await s.dispose();
      this.proxyManager.release(taskId);
    }
  }

  getSession(taskId) { return this.activeSessions.get(taskId); }

  getStats() {
    return {
      active: this.activeSessions.size,
      maxConcurrent: this.maxConcurrent,
      proxyPool: this.proxyManager.getStats()
    };
  }

  async shutdown() {
    for (const [id, s] of this.activeSessions) { try { await s.dispose(); } catch(e){} }
    this.activeSessions.clear();
  }
}
```

---

## 4. Módulo de CAPTCHA

### 4.1 Detección multidimensional

```javascript
// src/captcha/CaptchaDetector.js
export class CaptchaDetector {
  static async detect(page) {
    const results = await Promise.all([
      this.detectByDom(page),
      this.detectByFrames(page),
      this.detectByText(page)
    ]);
    return results.find(r => r) || null;
  }

  static async detectByDom(page) {
    const map = {
      'recaptcha_v2': ['.g-recaptcha', 'iframe[src*="recaptcha/api2/anchor"]', 'div[data-sitekey]'],
      'recaptcha_v3': ['.grecaptcha-badge', 'script[src*="recaptcha/api.js?render="]'],
      'hcaptcha': ['.h-captcha', 'iframe[src*="hcaptcha.com/captcha"]'],
      'cloudflare_turnstile': ['.cf-turnstile', 'iframe[src*="challenges.cloudflare.com"]', '#challenge-stage'],
      'datadome': ['iframe[src*="datadome.co"]', '#datadome-captcha'],
    };
    for (const [type, sels] of Object.entries(map)) {
      for (const sel of sels) {
        try {
          const el = await page.$(sel);
          if (el) {
            const sitekey = await this.extractSitekey(page, type);
            return { type, sitekey, selector: sel, detectedBy: 'dom', timestamp: Date.now() };
          }
        } catch(e) {}
      }
    }
    return null;
  }

  static async detectByFrames(page) {
    for (const frame of page.frames()) {
      const url = frame.url();
      if (url.includes('recaptcha/api2/anchor'))
        return { type:'recaptcha_v2', sitekey:extractFromUrl(url), detectedBy:'frame' };
      if (url.includes('hcaptcha.com/captcha'))
        return { type:'hcaptcha', sitekey:extractFromUrl(url), detectedBy:'frame' };
      if (url.includes('challenges.cloudflare.com'))
        return { type:'cloudflare_turnstile', sitekey:extractFromUrl(url), detectedBy:'frame' };
    }
    return null;
  }

  static async detectByText(page) {
    try {
      const txt = (await page.evaluate(()=>document.body?.innerText||'')).toLowerCase();
      if (txt.includes("i'm not a robot")) return { type:'recaptcha_v2', detectedBy:'text' };
      if (txt.includes('hcaptcha')||txt.includes('i am human')) return { type:'hcaptcha', detectedBy:'text' };
      if (txt.includes('are you a human')||txt.includes('datadome')) return { type:'datadome', detectedBy:'text' };
    } catch(e) {}
    return null;
  }

  static async extractSitekey(page, type) {
    return page.evaluate(() => {
      const el = document.querySelector('[data-sitekey]');
      if (el) return el.getAttribute('data-sitekey');
      for (const s of document.querySelectorAll('script[src*="recaptcha"]')) {
        const m = s.src.match(/[?&]k=([^&]+)/);
        if (m) return m[1];
      }
      return null;
    });
  }
}

function extractFromUrl(url) {
  try { return new URL(url).searchParams.get('k') || new URL(url).searchParams.get('sitekey'); }
  catch { return null; }
}
```

### 4.2 Solver multi-proveedor

```javascript
// src/captcha/CaptchaSolver.js
import Captcha from '2captcha';

const MAX_ATTEMPTS = 4;
const RETRY_MS = [2000, 5000, 10000, 30000];

export class CaptchaSolver {
  constructor(config = {}) {
    this.providers = [];
    if (config.twoCaptchaApiKey)
      this.providers.push({ name:'2captcha', solver:new Captcha.Solver(config.twoCaptchaApiKey) });
    if (config.capSolverApiKey)
      this.providers.push({ name:'capsolver', apiKey:config.capSolverApiKey });
  }

  async solve(page, detection, taskId) {
    if (!this.providers.length) return { success:false, token:null };

    let lastErr = null;
    for (let i = 0; i < MAX_ATTEMPTS; i++) {
      const p = this.providers[i % this.providers.length];
      try {
        const sitekey = detection.sitekey || await CaptchaDetector.extractSitekey(page, detection.type);
        const url = page.url();
        if (!sitekey) throw new Error('No sitekey');

        let result;
        if (detection.type.startsWith('recaptcha'))
          result = await p.solver.recaptcha({ googlekey:sitekey, pageurl:url });
        else if (detection.type === 'hcaptcha')
          result = await p.solver.hcaptcha({ sitekey, pageurl:url });
        else if (detection.type === 'cloudflare_turnstile')
          result = await p.solver.cloudflareTurnstile({ sitekey, pageurl:url });

        return { success:true, token:result.data, provider:p.name, durationMs:0, cost:0.002 };
      } catch (e) { lastErr = e.message; }
      if (i < MAX_ATTEMPTS-1) await new Promise(r=>setTimeout(r, RETRY_MS[i]||15000));
    }
    return { success:false, token:null, error:lastErr };
  }
}
```

### 4.3 Inyección de tokens

```javascript
// src/captcha/TokenInjector.js
export class TokenInjector {
  static async inject(page, type, token) {
    if (type.startsWith('recaptcha')) return this.injectRecaptcha(page, token);
    if (type==='hcaptcha') return this.injectHcaptcha(page, token);
    if (type==='cloudflare_turnstile') return this.injectTurnstile(page, token);
    return this.injectGeneric(page, token);
  }

  static async injectRecaptcha(page, token) {
    await page.evaluate(t => {
      const ta = document.querySelector('#g-recaptcha-response')
        || document.querySelector('textarea[name="g-recaptcha-response"]');
      if (ta) { ta.value = t; ta.dispatchEvent(new Event('input',{bubbles:true})); }

      const cfg = window.___grecaptcha_cfg;
      if (cfg?.clients) {
        for (const id of Object.keys(cfg.clients)) {
          const c = cfg.clients[id];
          if (c?.callback) {
            try {
              if (typeof c.callback==='function') c.callback(t);
              else if (typeof c.callback==='string' && window[c.callback]) window[c.callback](t);
            } catch(e) {}
          }
        }
      }
    }, token);
    await page.waitForTimeout(1000);
  }

  static async injectHcaptcha(page, token) {
    await page.evaluate(t => {
      const ta = document.querySelector('[name="h-captcha-response"]');
      if (ta) { ta.value = t; ta.dispatchEvent(new Event('input',{bubbles:true})); }
      if (window.hcaptcha?.setResponse) {
        try { window.hcaptcha.setResponse(window.hcaptcha.getRespKey?.()||0, t); } catch(e) {}
      }
    }, token);
    await page.waitForTimeout(1000);
  }

  static async injectTurnstile(page, token) {
    await page.evaluate(t => {
      const inp = document.querySelector('[name="cf-turnstile-response"]');
      if (inp) { inp.value = t; inp.dispatchEvent(new Event('input',{bubbles:true})); }
      Object.keys(window).filter(k=>k.startsWith('turnstile')).forEach(k => {
        try { if (window[k]?.callback) window[k].callback(t); } catch(e) {}
      });
    }, token);
    await page.waitForTimeout(1000);
  }

  static async injectGeneric(page, token) {
    await page.evaluate(t => {
      const sels = ['[name="captcha-response"]','#g-recaptcha-response','#h-captcha-response'];
      for (const sel of sels) {
        const el = document.querySelector(sel);
        if (el) { el.value = t; el.dispatchEvent(new Event('input',{bubbles:true})); break; }
      }
    }, token);
  }
}
```

---

## 5. Simulación de comportamiento humano

### 5.1 Ratón con curvas de Bézier

```javascript
// src/behavior/HumanMouse.js
export class HumanMouse {
  static async move(page, tx, ty, opts = {}) {
    const sx = opts.startX ?? ~~(Math.random()*300);
    const sy = opts.startY ?? ~~(Math.random()*300);
    const steps = opts.steps || 60 + ~~(Math.random()*40);

    // Control points
    const c1x = sx + (tx-sx)*0.25 + (Math.random()-0.5)*120;
    const c1y = sy + (ty-sy)*0.25 + (Math.random()-0.5)*80;
    const c2x = sx + (tx-sx)*0.75 + (Math.random()-0.5)*100;
    const c2y = sy + (ty-sy)*0.75 + (Math.random()-0.5)*60;

    // Overshoot?
    const over = opts.overshoot!==false && Math.random()>0.6;
    const ex = over ? tx+(tx-c2x)*0.3 : tx;
    const ey = over ? ty+(ty-c2y)*0.3 : ty;

    // Bezier points
    const pts = [];
    for (let i=0;i<=steps;i++) {
      const t = i/steps;
      pts.push({
        x: ~~((1-t)**3*sx + 3*(1-t)**2*t*c1x + 3*(1-t)*t**2*c2x + t**3*ex),
        y: ~~((1-t)**3*sy + 3*(1-t)**2*t*c1y + 3*(1-t)*t**2*c2y + t**3*ey)
      });
    }

    // Move with easing + jitter
    for (let i=0;i<pts.length;i++) {
      const t = i/pts.length;
      const easing = t<0.5 ? 2*t*t : -1+(4-2*t)*t;
      const delay = 2 + easing*8;
      const jx = (Math.random()-0.5)*3;
      const jy = (Math.random()-0.5)*3;
      await page.mouse.move(pts[i].x+jx, pts[i].y+jy);
      await new Promise(r=>setTimeout(r,delay));
    }

    // Return from overshoot
    if (over) {
      await new Promise(r=>setTimeout(r,30+Math.random()*50));
      for (let i=0;i<=8;i++) {
        const t = i/8;
        await page.mouse.move(ex+(tx-ex)*t+(Math.random()-0.5)*2, ey+(ty-ey)*t+(Math.random()-0.5)*2);
        await new Promise(r=>setTimeout(r,5+Math.random()*5));
      }
    }
  }

  static async click(page, x, y, opts = {}) {
    await HumanMouse.move(page, x, y, opts);
    await new Promise(r=>setTimeout(r,80+Math.random()*150));
    await page.mouse.click(x, y, opts);
    await new Promise(r=>setTimeout(r,50+Math.random()*100));
    if (Math.random()>0.7) {
      await page.mouse.move(x+(Math.random()-0.5)*3, y+(Math.random()-0.5)*3);
    }
  }
}
```

### 5.2 Escritura humana

```javascript
// src/behavior/HumanKeyboard.js
export class HumanKeyboard {
  static async type(page, selector, text, opts = {}) {
    const el = await page.$(selector);
    if (!el) throw new Error('Element not found: '+selector);

    const box = await el.boundingBox();
    if (box) await page.mouse.click(box.x+box.width/2, box.y+box.height/2);

    const errRate = opts.errorRate || 0.03;
    for (let i=0;i<text.length;i++) {
      if (Math.random()<errRate) {
        const wrong = HumanKeyboard.nearbyKey(text[i]);
        await page.keyboard.type(wrong);
        await delay(60,150);
        await delay(100,300);
        await page.keyboard.press('Backspace');
        await delay(50,120);
      }
      await page.keyboard.type(text[i]);
      await delay(text.charCodeAt(i)===32 ? 100+Math.random()*200 : 50+Math.random()*130);
    }
    await delay(200,500);
  }

  static nearbyKey(c) {
    const kb = {
      'a':['q','w','s','z'], 'e':['w','s','d','r'],
      'i':['u','j','k','o'], 'o':['i','k','l','p'],
      's':['a','w','e','d','x'], 't':['r','f','g','y'],
    };
    const nb = kb[c.toLowerCase()];
    if (!nb) return c;
    const r = nb[~~(Math.random()*nb.length)];
    return c===c.toUpperCase() ? r.toUpperCase() : r;
  }
}

async function delay(min, max) {
  await new Promise(r=>setTimeout(r, min+Math.random()*(max-min)));
}
```

### 5.3 Scroll humano

```javascript
// src/behavior/HumanScroll.js
export class HumanScroll {
  static async scroll(page, opts = {}) {
    const max = opts.maxScrolls || 3+~~(Math.random()*4);
    for (let i=0;i<max;i++) {
      const delta = 200+Math.random()*400;
      const sub = 5+~~(Math.random()*10);
      const sd = ~~(delta/sub);
      for (let j=0;j<sub;j++) {
        await page.mouse.wheel(0, sd);
        await new Promise(r=>setTimeout(r,8+Math.random()*15));
      }
      await new Promise(r=>setTimeout(r,800+Math.random()*3000));
      if (Math.random()>0.5) await page.mouse.wheel(0,(Math.random()-0.5)*40);
    }
  }

  static async scrollToElement(page, sel) {
    const el = await page.$(sel);
    if (!el) throw new Error('Not found: '+sel);
    await el.scrollIntoViewIfNeeded();
    await new Promise(r=>setTimeout(r,300+Math.random()*500));
    const box = await el.boundingBox();
    if (box) {
      await page.evaluate(y=>window.scrollTo({top:y,behavior:'smooth'}), box.y-100+Math.random()*200);
      await new Promise(r=>setTimeout(r,400+Math.random()*600));
    }
  }
}
```

---

## 6. Proxy & Rotación de Identidad

### 6.1 Proxy Manager

```javascript
// src/proxy/ProxyManager.js
export class ProxyManager {
  constructor(proxies = []) {
    this.pool = proxies.map(p => ({
      id: crypto.randomUUID(), url:p.url, type:p.type, country:p.country,
      username:p.username||'', password:p.password||'',
      status:'available', assignedTo:null,
      usageCount:0, banCount:0, successRate:p.successRate||1.0
    }));
    this.lock = new Map(); // taskId → proxyId
  }

  acquire(taskId, opts = {}) {
    if (opts.sticky!==false && this.lock.has(taskId)) {
      const pid = this.lock.get(taskId);
      const p = this.pool.find(x=>x.id===pid);
      if (p && p.status!=='banned') { p.status='in_use'; p.assignedTo=taskId; return p; }
      this.lock.delete(taskId);
    }

    const avail = this.pool
      .filter(p=>p.status==='available')
      .filter(p=>!opts.country||p.country===opts.country)
      .sort((a,b)=>b.successRate-a.successRate);

    let p;
    if (avail.length) {
      p = avail[0];
    } else {
      const fb = this.pool.filter(p=>p.status!=='banned').sort((a,b)=>a.usageCount-b.usageCount);
      if (!fb.length) throw new Error('No proxies available');
      p = fb[0];
    }

    p.status='in_use'; p.assignedTo=taskId; p.usageCount++;
    this.lock.set(taskId, p.id);
    return p;
  }

  release(taskId) {
    const pid = this.lock.get(taskId);
    if (pid) { const p=this.pool.find(x=>x.id===pid); if(p){p.status='available';p.assignedTo=null;} this.lock.delete(taskId); }
  }

  markBanned(taskId) {
    const pid = this.lock.get(taskId);
    if (pid) { const p=this.pool.find(x=>x.id===pid); if(p){p.status='banned';p.banCount++;} this.lock.delete(taskId); }
  }

  rotate(taskId, opts={}) { this.release(taskId); return this.acquire(taskId,{...opts,sticky:false}); }

  getStats() {
    return {
      total:this.pool.length,
      available:this.pool.filter(p=>p.status==='available').length,
      inUse:this.pool.filter(p=>p.status==='in_use').length,
      banned:this.pool.filter(p=>p.status==='banned').length
    };
  }
}
```

---

## 7. Recuperación anti-bloqueo

### 7.1 Detector de bloqueos

```javascript
// src/antiBlocking/BlockDetector.js
export class BlockDetector {
  static async detect(page, response) {
    if (response && [403,429,503].includes(response.status())) {
      return { blocked:true, type:'http_'+response.status(), reason:'HTTP '+response.status() };
    }
    const patterns = [
      [/access denied/i,'access_denied'], [/blocked/i,'blocked'],
      [/too many requests/i,'rate_limited'], [/just a moment/i,'cloudflare'],
      [/checking your browser/i,'cloudflare'], [/are you a human/i,'datadome'],
      [/security check/i,'security_check']
    ];
    try {
      const title = (await page.title().catch(()=>'')) || '';
      const url = page.url() || '';
      for (const [rx,type] of patterns) {
        if (rx.test(title)||rx.test(url)) return { blocked:true, type, reason:title||url };
      }
      const body = await page.evaluate(()=>document.body?.innerText||'');
      for (const [rx,type] of patterns) {
        if (rx.test(body)) return { blocked:true, type, reason:body.slice(0,200) };
      }
    } catch(e) {}
    return null;
  }
}
```

### 7.2 Estrategia de recuperación

```javascript
// src/antiBlocking/RecoveryStrategy.js
const MAX = 5, BASE = 5000, MAX_MS = 120000;

export class RecoveryStrategy {
  constructor(manager) { this.manager = manager; this.log = []; }

  async recover(taskId, session, blockInfo) {
    const url = session.page.url();
    const domain = new URL(url).hostname;
    this.log.push({ domain, taskId, reason:blockInfo?.reason, ts:new Date().toISOString() });

    this.manager.proxyManager.markBanned(taskId);
    await this.manager.destroySession(taskId);

    for (let i=1;i<=MAX;i++) {
      const backoff = Math.min(BASE*2**(i-1)+Math.random()*3000, MAX_MS);
      await new Promise(r=>setTimeout(r,backoff));

      try {
        const s = await this.manager.createSession(taskId, session.instruction, {
          excludeTypes:['datacenter'],
          deviceType: i%2===0?'mobile':'desktop'
        });
        await s.navigate(url);
        const blocked = await BlockDetector.detect(s.page, null);
        if (!blocked) return { success:true, session:s, attempt:i };

        this.manager.proxyManager.markBanned(taskId);
        await this.manager.destroySession(taskId);
      } catch(e) {}
    }
    return { success:false, session:null, attempt:MAX, error:'All attempts failed' };
  }
}
```

---

## 8. Handoff humano en modo sigiloso

```javascript
// src/handoff/RemoteHandoffServer.js
import { WebSocketServer } from 'ws';

export class RemoteHandoffServer {
  constructor(port = 8788) {
    this.wss = new WebSocketServer({ port });
    this.sessions = new Map();
  }

  async startHandoff(taskId, page, ws) {
    this.sessions.set(taskId, { page, ws, streaming:true });
    ws.send(JSON.stringify({
      type:'handoff.start', taskId, url:page.url(),
      title: await page.title(), instruction:'CAPTCHA/2FA requires human intervention.'
    }));
    this.startStream(taskId, page, ws);

    ws.on('message', async data => {
      try {
        const msg = JSON.parse(data);
        switch(msg.type) {
          case 'handoff.mousemove': await page.mouse.move(msg.x,msg.y); break;
          case 'handoff.mouseclick': await page.mouse.click(msg.x,msg.y); break;
          case 'handoff.keydown': await page.keyboard.down(msg.key); break;
          case 'handoff.keyup': await page.keyboard.up(msg.key); break;
          case 'handoff.type': await page.keyboard.type(msg.text); break;
          case 'handoff.scroll': await page.mouse.wheel(msg.dx||0,msg.dy||0); break;
          case 'handoff.done': ws.send(JSON.stringify({type:'handoff.complete',taskId})); break;
        }
      } catch(e) {
        ws.send(JSON.stringify({type:'handoff.error',error:e.message}));
      }
    });
    ws.on('close', () => this.stopHandoff(taskId));
  }

  async startStream(taskId, page, ws, ms = 500) {
    const s = this.sessions.get(taskId);
    if (!s) return;
    while (s.streaming && ws.readyState===ws.OPEN) {
      try {
        const ss = await page.screenshot({type:'jpeg',quality:60});
        ws.send(JSON.stringify({type:'handoff.screenshot',taskId,data:ss.toString('base64')}));
      } catch(e) { if(ws.readyState!==ws.OPEN) break; }
      await new Promise(r=>setTimeout(r,ms));
    }
  }

  stopHandoff(taskId) {
    const s = this.sessions.get(taskId);
    if (s) { s.streaming=false; this.sessions.delete(taskId); }
  }
}
```

---

## 9. Plan de integración

### 9.1 Archivos existentes a modificar

| Archivo | Cambio | Justificación |
|---|---|---|
| `Program.cs` | Añadir `HandleStealthRunStart()` (~60 líneas) | Dispatch bimodal |
| `Program.cs` | Endpoint `/ws/stealth` (~80 líneas) | Nuevo WS paralelo |
| `Program.cs` | `HandleStealthMessage()` (~120 líneas) | Manejo de mensajes |
| `Program.cs` | DI: `AddSingleton<StealthTaskManager, StealthRunnerRegistry, ...>` | 4 líneas |
| `ChromeLabOptions.cs` | Propiedades opcionales: `CaptchaSolverApiKey`, `ProxyListPath` | Compatible hacia atrás |
| `config/chrome-lab.local.json` | Sección `"stealth"` opcional | Ignorada si no existe |

### 9.2 Nuevos archivos .NET (`src/.../Bridge/Stealth/`)

```
Stealth/
├── StealthProtocol.cs           # Tipos del protocolo (task, runner, captcha, block, handoff)
├── StealthTaskManager.cs        # Ciclo de vida de tareas sigilosas
├── StealthRunnerRegistry.cs     # Registro de runners conectados
├── StealthProfileService.cs     # Servicio de perfiles (generación, coherencia, pool)
└── StealthRecoveryOrchestrator.cs # Orquestación de recuperación anti-bloqueo
```

### 9.3 Contratos .NET

```csharp
// StealthProtocol.cs
public static class StealthProtocol { public const string Version = "stealth-v1"; }

public sealed record StealthTaskRequest(string Type, string TaskId, string Instruction,
    StealthProfileConfig Profile, StealthProxyConfig? Proxy, StealthCaptchaConfig Captcha,
    int MaxRetries, bool SessionPersistence);

public sealed record StealthProfileConfig(string? ProfileId, string? Preset,
    string DeviceType, string Os, string? Country, int? ViewportWidth, int? ViewportHeight);

public sealed record StealthProxyConfig(string Server, string? Username, string? Password,
    string Type, string Country);

public sealed record StealthCaptchaConfig(string Provider, string ApiKey,
    int MaxAttempts, int TimeoutSeconds);

public sealed record StealthTaskResult(string Type, string TaskId, string StepId,
    string Tool, bool Success, JsonElement? Result, StealthCaptchaInfo? CaptchaDetected,
    StealthBlockInfo? BlockDetection, string Timestamp);

public sealed record StealthCaptchaInfo(string Type, string? Sitekey, bool Solved,
    string? Solver, int DurationMs, double Cost);

public sealed record StealthBlockInfo(bool Blocked, string Type, string Reason);

public sealed record StealthHandoffRequest(string Type, string TaskId, string Reason,
    string CaptchaType, int Attempts, string ScreenshotBase64, string PageUrl);

// Runner registry (thread-safe, analogous to ChromeLabClientRegistry)
public sealed class StealthRunnerRegistry {
    private readonly ConcurrentDictionary<string, StealthRunnerConnection> _runners = new();
    public bool HasConnectedRunner => _runners.Values.Any(r => r.Connected);
    public string Add(WebSocket socket) { /* ... */ }
    public void MarkConnected(string runnerId, string[] capabilities) { /* ... */ }
    public async Task BroadcastAsync(object message, CancellationToken ct) { /* ... */ }
    public void Remove(string runnerId) { /* ... */ }
}

// Task manager (analogous to ChromeLabRunManager)
public sealed class StealthTaskManager {
    private readonly ConcurrentDictionary<string, StealthTask> _tasks = new();
    public StealthTask Start(string instruction) { /* ... */ }
    public StealthTask? Get(string taskId) { /* ... */ }
    public StealthTask Pause(string taskId, string reason) { /* ... */ }
    public StealthTask Resume(string taskId) { /* ... */ }
    public StealthTask Complete(string taskId, string message) { /* ... */ }
    public StealthTask Error(string taskId, string error) { /* ... */ }
}
```

### 9.4 Ganchos en Program.cs

```csharp
// Servicios (junto a los existentes):
builder.Services.AddSingleton<StealthTaskManager>();
builder.Services.AddSingleton<StealthRunnerRegistry>();
builder.Services.AddSingleton<StealthProfileService>();
builder.Services.AddSingleton<StealthRecoveryOrchestrator>();

// POST /api/runs — dispatch:
app.MapPost("/api/runs", async (StartRunRequest request, ...) => {
    // ... validaciones existentes intactas ...

    if (string.Equals(request.Mode, "stealth", StringComparison.OrdinalIgnoreCase))
        return await HandleStealthRunStart(request, stealthRuns, stealthRunners,
            stealthProfiles, agent, options, ct);

    // ... flujo companion intacto ...
});

// Nuevo endpoint:
app.Map("/ws/stealth", async (HttpContext context, StealthRunnerRegistry runners, ...) => {
    if (!context.WebSockets.IsWebSocketRequest) { context.Response.StatusCode = 400; return; }
    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    var runnerId = runners.Add(socket);
    try {
        while (socket.State == WebSocketState.Open)
            await HandleStealthMessage(await ReceiveTextMessageAsync(socket, ct),
                socket, runnerId, runners, tasks, agent, options, ct);
    } finally { runners.Remove(runnerId); }
});
```

### 9.5 NO se modifica

| Componente | Razón |
|---|---|
| Extensión Chrome completa | Companion intacto |
| `OpenAiAgentClient` | Misma API key, mismo patrón |
| `ChromeLabSecurity` | Validaciones aplican a ambos modos |
| `ChromeLabRunManager` | Companion tiene su ciclo de vida |
| `ChromeLabClientRegistry` | Solo extensiones Chrome |

---

## 10. Suite de pruebas de sigilo

```javascript
// tests/stealth-suite.test.js
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { StealthSession } from '../src/StealthSession.js';
import { FingerprintGenerator } from '../src/fingerprint/FingerprintGenerator.js';
import { CaptchaDetector } from '../src/captcha/CaptchaDetector.js';

test('navigator.webdriver debe ser undefined', async () => {
  const s = new StealthSession({taskId:'t1', profile:FingerprintGenerator.generate(), proxy:null});
  await s.initialize(); await s.navigate('about:blank');
  const wd = await s.page.evaluate(()=>navigator.webdriver);
  assert.ok(wd===undefined||wd===false);
  await s.dispose();
});

test('plugins.length >= 2', async () => {
  const s = new StealthSession({taskId:'t2', profile:FingerprintGenerator.generate(), proxy:null});
  await s.initialize(); await s.navigate('about:blank');
  const len = await s.page.evaluate(()=>navigator.plugins.length);
  assert.ok(len>=2);
  await s.dispose();
});

test('languages match profile', async () => {
  const p = FingerprintGenerator.generate({languages:['es-AR','es']});
  const s = new StealthSession({taskId:'t3', profile:p, proxy:null});
  await s.initialize(); await s.navigate('about:blank');
  assert.deepEqual(await s.page.evaluate(()=>navigator.languages), ['es-AR','es']);
  await s.dispose();
});

test('window.chrome exists, no automation leaks', async () => {
  const s = new StealthSession({taskId:'t4', profile:FingerprintGenerator.generate(), proxy:null});
  await s.initialize(); await s.navigate('about:blank');
  assert.ok(await s.page.evaluate(()=>typeof window.chrome!=='undefined'));
  assert.ok(await s.page.evaluate(()=>
    ['webdriver','__playwright','__pw_manual__','_selenium'].every(k=>!(k in window))
  ));
  await s.dispose();
});

test('screen consistent with viewport', async () => {
  const p = FingerprintGenerator.generate({viewport:{width:1920,height:1080}});
  const s = new StealthSession({taskId:'t5', profile:p, proxy:null});
  await s.initialize(); await s.navigate('about:blank');
  const sc = await s.page.evaluate(()=>({w:screen.width,h:screen.height}));
  assert.ok(sc.w>=1920&&sc.h>=1080);
  await s.dispose();
});

test('WebGL vendor/renderer realistic', async () => {
  const s = new StealthSession({taskId:'t6', profile:FingerprintGenerator.generate(), proxy:null});
  await s.initialize(); await s.navigate('about:blank');
  const gl = await s.page.evaluate(()=>{
    const c=document.createElement('canvas');
    const g=c.getContext('webgl')||c.getContext('experimental-webgl');
    if(!g)return null;
    const d=g.getExtension('WEBGL_debug_renderer_info');
    return {v:d?g.getParameter(d.UNMASKED_VENDOR_WEBGL):null,r:d?g.getParameter(d.UNMASKED_RENDERER_WEBGL):null};
  });
  if (gl) { assert.ok(gl.v?.length>0); assert.ok(gl.r?.length>0); }
  await s.dispose();
});

test('bot.sannysoft.com pass', {timeout:30000}, async () => {
  const s = new StealthSession({taskId:'t7', profile:FingerprintGenerator.generate(), proxy:null});
  await s.initialize();
  await s.page.goto('https://bot.sannysoft.com',{waitUntil:'networkidle'});
  await s.page.waitForTimeout(3000);
  const r = await s.page.evaluate(()=>{
    const c={};document.querySelectorAll('table tr').forEach(r=>{
      const t=r.querySelectorAll('td');if(t.length>=2)c[t[0].textContent.trim()]=t[1].textContent.trim();
    });return c;
  });
  const wd = Object.entries(r).find(([k])=>k.includes('WebDriver'));
  if (wd) assert.ok(wd[1].includes('missing')||wd[1].includes('false'),'WebDriver failed: '+wd[1]);
  await s.dispose();
});

test('CAPTCHA detection on demo page', {timeout:20000}, async () => {
  const s = new StealthSession({taskId:'t9', profile:FingerprintGenerator.generate(), proxy:null});
  await s.initialize();
  await s.page.goto('https://www.google.com/recaptcha/api2/demo',{waitUntil:'domcontentloaded'});
  await s.page.waitForTimeout(3000);
  const d = await CaptchaDetector.detect(s.page);
  assert.ok(d!==null);
  await s.dispose();
});
```

---

## 11. Plan de implementación en fases

### Fase 1: MVP (4-6 semanas)

**Objetivo:** Modo sigiloso básico funcional con resolución reCAPTCHA v2 y handoff mínimo.

| Sem | Entregables |
|---|---|
| 1-2 | `StealthProtocol.cs`, `StealthTaskManager`, `StealthRunnerRegistry`, endpoint `/ws/stealth`, dispatch `POST /api/runs` |
| 2-3 | `StealthSession.js` (launch + initScript parcial), `FingerprintInjector.js` (webdriver, plugins, screen, chrome), tools básicas |
| 3-4 | `CaptchaDetector.js`, `CaptchaSolver.js` (2captcha), `TokenInjector.js` (reCAPTCHA v2 + hCaptcha) |
| 4-5 | `RemoteHandoffServer.js` (screenshots + mouse), panel HTML simple, pause/resume |
| 5-6 | `ProxyManager.js` (fijo, sin rotación automática), coherencia básica proxy-perfil |

### Fase 2: Fingerprinting avanzado + rotación (3-4 semanas)

| Sem | Entregables |
|---|---|
| 7-8 | WebGL, Canvas noise, AudioContext, Fonts, CDP bypass completo |
| 8-9 | Rotación automática de proxy + health check + sticky sessions |
| 9-10 | Perfiles predefinidos curados (desktop Win/Mac, mobile) |

### Fase 3: Comportamiento humano + anti-bloqueo (3-4 semanas)

| Sem | Entregables |
|---|---|
| 11-12 | `HumanMouse.js` (Bézier+jitter), `HumanKeyboard.js` (errores), `HumanScroll.js` |
| 12-13 | `BlockDetector.js`, `RecoveryStrategy.js`, `DomainBlacklist.js` |
| 13-14 | Streaming viewport mejorado, relay teclado completo |

### Fase 4: Escalabilidad (3-4 semanas)

| Sem | Entregables |
|---|---|
| 15-16 | Pool con concurrencia, resource management, timeouts |
| 16-17 | Docker Compose (bridge + runner + panel) |
| 17-18 | Métricas, monitoreo, dashboard operacional |

**Total estimado: 13-18 semanas para sistema completo.**

---

## Apéndice A: Configuración extendida

```json
{
  "OpenAiApiKey": "",
  "ExtensionToken": "nexa_...",
  "Host": "127.0.0.1",
  "Port": 8787,
  "stealth": {
    "enabled": true,
    "maxConcurrentSessions": 5,
    "proxyList": [
      { "url": "http://user:pass@res.proxy.com:8080", "type": "residential", "country": "US" }
    ],
    "captcha": {
      "twoCaptchaApiKey": "",
      "antiCaptchaApiKey": "",
      "capSolverApiKey": "",
      "defaultProvider": "2captcha",
      "maxAttempts": 4
    },
    "fingerprint": {
      "defaultPreset": "desktop-win-chrome",
      "autoRotate": true
    },
    "antiBlocking": {
      "maxRecoveryAttempts": 5,
      "baseBackoffMs": 5000,
      "maxBackoffMs": 120000
    }
  }
}
```

## Apéndice B: Docker Compose (Fase 4)

```yaml
version: '3.8'
services:
  bridge:
    build: { context: ./src/OneBrain.ChromeLab.Bridge, dockerfile: Dockerfile }
    ports: ["8787:8787"]
    environment:
      - OPENAI_API_KEY=${OPENAI_API_KEY}
      - BRIDGE_TOKEN=${NODAL_OS_CHROME_BRIDGE_TOKEN}
    volumes: ["./config:/app/config"]
    restart: unless-stopped

  stealth-engine:
    build: { context: ./stealth-engine, dockerfile: Dockerfile }
    ports: ["8788:8788"]
    environment:
      - BRIDGE_HOST=bridge
      - BRIDGE_PORT=8787
      - CAPTCHA_API_KEY=${CAPTCHA_API_KEY}
    volumes:
      - "./stealth-engine/profiles:/app/profiles"
      - "stealth-sessions:/app/sessions"
    depends_on: [bridge]
    restart: unless-stopped

  stealth-panel:
    build: { context: ./stealth-panel, dockerfile: Dockerfile }
    ports: ["8789:80"]
    depends_on: [bridge]
    restart: unless-stopped

volumes:
  stealth-sessions:
```
