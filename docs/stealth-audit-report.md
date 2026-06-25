# NODAL OS Stealth Engine — Auditoría de Sigilo y Evasión

> **Fecha:** 2026-06-25  
> **Auditor:** Security Research (offensive bot-detection perspective)  
> **Alcance:** `stealth-engine/src/` — 5 módulos, ~3000 líneas  
> **Criterio:** Detección por sistemas anti-bot modernos (Cloudflare, DataDome, Akamai, Kasada, Imperva)

---

## MÓDULO 1 — CAPTCHA SOLVER (`captcha/`)

### 1.1 Proveedores no cubiertos

| Proveedor | Estado | Riesgo de FN |
|-----------|--------|-------------|
| reCAPTCHA v2 | Cubierto (DOM + frames + text) | Bajo |
| reCAPTCHA v3 | Cubierto parcial (solo `.grecaptcha-badge`) | **Alto** — v3 puede ser invisible sin badge |
| reCAPTCHA Enterprise | No distinguido de v2/v3 | Medio |
| hCaptcha | Cubierto (DOM + frames + text) | Bajo |
| Cloudflare Turnstile | Cubierto (DOM + frames + text) | Bajo |
| DataDome | Cubierto (DOM + text) | Medio — solo 2 selectores |
| **GeeTest** | **No cubierto** | **Alto** |
| **FunCaptcha (Arkose Labs)** | **No cubierto** | **Alto** |
| **Kasada** | **No cubierto** | **Alto** |
| **Imperva/Incapsula** | **No cubierto** | **Alto** |
| **Akamai Bot Manager** | **No cubierto** | **Crítico** |

**Recomendación:** Añadir selectores para GeeTest (`.geetest_captcha`, `iframe[src*="geeTest"]`), FunCaptcha (`iframe[src*="funcaptcha"]`, `div[data-funcaptcha]`), Kasada (`script[src*="kasada"]`, `script[src*="kpsdk"]`), Akamai (`script[src*="akamai"]`, `script[src*="bot-manager"]`).

### 1.2 Fragilidad de selectores DOM
```javascript
// Actual: solo busca en la página principal con page.$
'recaptcha_v3': ['.grecaptcha-badge', 'script[src*="recaptcha/api.js?render="]'],
```
**Problema:** reCAPTCHA v3 Enterprise usa `https://www.google.com/recaptcha/enterprise.js?render=SITEKEY`. El selector `script[src*="recaptcha/api.js?render="]` **no captura** `enterprise.js?render=`. Además, el badge puede estar oculto con CSS (`display:none` o `visibility:hidden;opacity:0`).

**Recomendación:** Usar `script[src*="recaptcha"]` en vez de `api.js` específico. Para v3 invisible, verificar `grecaptcha.execute()` en el scope global.

### 1.3 Inyección de token incompleta (CRÍTICA)
```javascript:line 15-26
// TokenInjector NO maneja data-callback attribute
const cfg = window.___grecaptcha_cfg;
if (cfg && cfg.clients) {
  for (const id of Object.keys(cfg.clients)) {
    const c = cfg.clients[id];
    if (c && c.callback) { ... }  // solo callback directo
  }
}
```
**Problema:** Muchos sitios usan `<div class="g-recaptcha" data-callback="onSubmit" data-sitekey="...">`. El token se inyecta en el textarea pero **nunca se dispara `onSubmit()`** porque el código no busca `data-callback` en el DOM. El formulario queda esperando el callback y nunca se envía.

**Recomendación:** Añadir búsqueda de `data-callback`:
```javascript
const widget = document.querySelector('.g-recaptcha');
const cbName = widget?.getAttribute('data-callback');
if (cbName && window[cbName]) window[cbName](t);
```

### 1.4 Clasificación por píxeles extremadamente frágil (ALTA)
```javascript:48-51
if (darkRatio > 0.1 && darkRatio < 0.5 && runDensity > 0.5) return 'text-distorted';
if (darkRatio > 0.3 && darkRatio < 0.8 && runDensity > 1.0) return 'image-selection';
if (darkRatio < 0.15 && runDensity < 0.3) return 'puzzle-slider';
```
**Problema:** Three magic thresholds based on pixel density and run count. A page with a dark-themed navbar, a product image grid, or even a heavily-styled login form could be misclassified. The `runDensity` metric is particularly brittle — it counts transitions between light→dark pixels, which varies wildly.

### 1.5 Fuga de información al solver externo (ALTA)
```javascript:48
body: JSON.stringify({ clientKey: this.twoCaptchaApiKey, task: { type: 'NoCaptchaTaskProxyless', websiteURL: url, websiteKey: sitekey } }),
```
**Problema:** Se envía `page.url()` completo a 2captcha **incluyendo query parameters de sesión, tokens OAuth, y potencialmente datos sensibles** en la URL. Por ejemplo: `https://example.com/checkout?sessionId=abc123&token=eyJhbGci...`. Esto es una fuga de datos a un tercero.

**Recomendación:** Enviar solo `origin` (protocolo + hostname):
```javascript
const safeUrl = new URL(url).origin;
```

### 1.6 Logging de sitekeys y tokens (MEDIA)
```javascript:18,28
console.log(`[${taskId}] Attempting CAPTCHA solve via 2captcha for ${detection.type}`);
// líneas 90, 160 — también expone sitekey parcial y tokens
console.log('[VisualCaptchaSolver] OCR extracted: ' + clean);
console.log('[VisualCaptchaSolver] AI answered: ' + answer);
```

### 1.7 Sin timeout por request HTTP individual (MEDIA)
```javascript:21-31
const response = await fetch('https://api.2captcha.com/createTask', { ... });
// No AbortController, no timeout por request
```

---

## MÓDULO 2 — FINGERPRINT INJECTOR (`fingerprint/`)

### 2.1 Propiedades NO falseadas (cobertura por capa)

| Capa | Propiedades falseadas | **Propiedades NO falseadas** |
|------|----------------------|---------------------------|
| Navigator | webdriver, vendor, platform, languages, hardwareConcurrency, deviceMemory, maxTouchPoints, plugins, mimeTypes | **productSub, appCodeName, appName, appVersion, buildID, oscpu, doNotTrack, cookieEnabled, onLine** |
| Screen | width, height, availWidth, availHeight, colorDepth, pixelDepth | **orientation, availLeft, availTop, isExtended** |
| WebGL | vendor, renderer (37445/37446), getSupportedExtensions | **readPixels no tiene ruido, shaderPrecisionFormat, MAX_TEXTURE_SIZE** |
| Canvas | 2D getImageData, toDataURL | **WebGL readPixels, canvas.toBlob, OffscreenCanvas** |
| Audio | OscillatorNode.connect, AnalyserNode.getByteFrequencyData | **AudioBuffer.getChannelData, AnalyserNode.getFloatTimeDomainData, BiquadFilterNode.getFrequencyResponse** |
| WebRTC | RTCPeerConnection iceServers vacío, getUserMedia rechazada | **RTCDataChannel, onicecandidate events, RTCIceCandidate.address** |
| Fonts | **NO FALSEADO** | El array `this.fonts` en el perfil existe pero **nunca se usa en el script de inyección** |
| Chrome | runtime, loadTimes, csi, app | **chrome.webstore, chrome.management** |
| CDP | __proto__ polling | **Runtime.runIfWaitingForDebugger, Debugger.scriptParsed, Page.frameNavigated** |
| Battery | **NO FALSEADO** | **navigator.getBattery()** no interceptado |
| Connection | **NO FALSEADO** | **navigator.connection.rtt, downlink, effectiveType** no falseados |
| Media | **NO FALSEADO** | **navigator.mediaCapabilities, MediaDevices.enumerateDevices** |
| Bluetooth | **NO FALSEADO** | **navigator.bluetooth** |
| USB | **NO FALSEADO** | **navigator.usb** |
| Gamepad | **NO FALSEADO** | **navigator.getGamepads** |

**Severidad global:** Crítica — hay al menos 25 propiedades comunes sin falsear.

### 2.2 Constante detectable: `vendor: 'Google Inc.'` (ALTA)
```javascript:9-10
Object.defineProperty(Navigator.prototype, 'vendor',
  { get: () => 'Google Inc.', configurable: true });
```
Es una firma constante. Un anti-bot puede comparar el vendor contra una base de datos de user-agents reales. Por ejemplo, Chrome 131 en macOS reporta `vendor: "Google Inc."` pero en algunos builds reporta `"Google Inc"`. La constante es detectable si todos los navegadores del pool reportan exactamente el mismo string.

### 2.3 Sin falseo de fuentes (CRÍTICA)
El array `this.fonts` en `FingerprintProfile` existe pero **nunca se usa en `getFullInitScript()`**. El fingerprinting de fuentes es una de las técnicas más usadas por anti-bots. La página solicita `document.fonts.check('12px Arial')` y recibe fuentes reales del sistema.

### 2.4 WebGL readPixels sin ruido (MEDIA)
El script solo falsea `getImageData` y `toDataURL` del contexto 2D. Pero `WebGLRenderingContext.readPixels()` es otro método común de fingerprinting que no tiene ruido.

### 2.5 Race condition de inyección (MEDIA)
```javascript:71-73
this.context = await this.browser.newContext(ctxOpts);
await this.context.addInitScript(FingerprintInjector.getFullInitScript(this.profile));
this.page = await this.context.newPage();
```
**Problema:** `context.addInitScript()` inyecta en todas las páginas existentes y futuras del contexto. Pero `about:blank` se carga antes de que `addInitScript` termine. Al navegar a la URL real, el script ya está inyectado. Sin embargo, si una página usa `window.open()` o un `iframe` con `srcdoc`, el script podría no ejecutarse.

---

## MÓDULO 3 — PROXY Y ANTI-BLOQUEO

### 3.1 Sin coherencia geográfica automática (ALTA)
```javascript:60-62
// ProxyManager.acquire retorna proxy, pero NADIE actualiza el perfil
return { server: p.url, username: p.username, ... };
```
**Problema:** Si se asigna un proxy de Alemania (`country: 'DE'`), el `FingerprintProfile` sigue con `timezone: 'America/New_York'`, `locale: 'en-US'`. Un anti-bot cruza la IP con el timezone y detecta la incoherencia.

**Recomendación:** En `index.js handleTask()`, después de `proxyManager.acquire()`, aplicar:
```javascript
if (proxy?.country) {
  fingerprintProfile = FingerprintProfile.ensureCoherence(fingerprintProfile, proxy.country);
}
```

### 3.2 RecoveryStrategy rompe el taskId (CRÍTICA)
```javascript:54
taskId: crypto.randomUUID(),  // GENERA UN NUEVO ID
```
**Problema:** `RecoveryStrategy.recover()` crea una nueva sesión con `crypto.randomUUID()` como taskId. Pero el bridge sigue referenciando el taskId **original**. Cuando el runner envía `stealth.result`, el bridge no encuentra la tarea porque el ID cambió.

**Recomendación:** Pasar el taskId **original** como parámetro:
```javascript
taskId: taskId,  // no crypto.randomUUID()
```

### 3.3 BlockDetector no cubre Akamai/Imperva/Kasada (MEDIA)
Patrones faltantes en `BlockDetector`:
- Akamai: `Reference #`, `akamai`, `Edge IP`
- Imperva/Incapsula: `_Incapsula_Resource`, `incap_ses_`, `visid_incap_`, `Request unsuccessful`
- Kasada: `kpsdk-`, `_kasada`, `kasadav`
- Sucuri: `Access Denied - Sucuri`, `CloudProxy`, `Firewall`
- Distil Networks: `distil`, `Distil`, `BLC_TEST`

### 3.4 Fuga en health check (BAJA)
```javascript:38
const resp = await fetch('http://httpbin.org/ip', { signal: controller.signal });
```
Usar `httpbin.org/ip` revela a un tercero qué proxies estamos usando. En producción, usar un endpoint propio o al bridge.

---

## MÓDULO 4 — COMPORTAMIENTO HUMANO

### 4.1 Bézier siempre grado 3 (ALTA)
Trayectoria balística: `curveT = 1 - (1-t)^3` es siempre la misma función. Un detector de behavioral biometrics entrenado con ML puede identificar esta firma algorítmica. La curvatura, aceleración y jerk son constantes e iguales para todos los movimientos.

### 4.2 Parameter `dist` fuera de scope (CRÍTICA — bug)
```javascript:33
const noiseScale = dist > 500 ? 1.5 : 1;  // dist NO está definido aquí
```
`dist` se calcula en `move()` (línea 9) pero no se pasa a `_ballisticMove()`. Esto resulta en `dist` undefined, que evalúa a `NaN`, y `noiseScale` siempre es 1. **El jitter aumentado para distancias largas nunca se activa.**

### 4.3 Distribución de errores de tecleo poco realista (MEDIA)
El `HumanKeyboard` usa una distribución uniforme para el error rate (`Math.random() < errorRate`). Datos de keystroke dynamics reales muestran que los errores siguen patrones específicos:
- Sustitución de tecla adyacente: 60% de errores
- Transposición (dos teclas al revés): 20%
- Omisión: 15%
- Inserción extra: 5%

Nuestro código solo hace sustitución con `nearbyKey`. Los otros tipos de error no se simulan.

### 4.4 Scroll sin pausa antes del primer movimiento (MEDIA)
Cada scroll empieza inmediatamente. Un humano típicamente hace hover (mueve el mouse a la barra de scroll o al área de contenido) antes de scrollear.

---

## MÓDULO 5 — INTEGRACIÓN

### 5.1 Solver hardcodeado a recaptcha_v2 (CRÍTICA)
```javascript:235
const result = await solver.solve(session.page, { type: 'recaptcha_v2', sitekey }, taskId);
```
Esto funciona para reCAPTCHA v2 pero **nunca para hCaptcha ni Turnstile**, que el detector sí identifica. El solver siempre se llama con `type: 'recaptcha_v2'` independientemente de qué tipo detectó `CaptchaDetector`.

**Recomendación:** Pasar el tipo detectado real:
```javascript
const captchaType = session._lastDetectedCaptcha?.type || 'recaptcha_v2';
const result = await solver.solve(session.page, { type: captchaType, sitekey }, taskId);
```

### 5.2 Sin keepalive entre runner y bridge (MEDIA)
El runner no envía pings periódicos al bridge. Si el WebSocket está idle, firewalls intermedios pueden cerrar la conexión. El companion sí tiene `extension.ping` cada 15s.

### 5.3 Sin limpieza de sesión ante error de solver (BAJA)
```javascript:236-243
if (result.success) { ... return; }
// Si falla, el token se reporta pero la sesión sigue abierta
send({ type: 'stealth.friction.solved', taskId, signalId: msg.signalId, success: false, ... });
```

---

## RESUMEN — TOP 10 PROBLEMAS MÁS CRÍTICOS

| # | Severidad | Módulo | Problema | Impacto |
|---|-----------|--------|----------|---------|
| 1 | **Crítica** | Integración | Solver hardcodeado a `recaptcha_v2` (línea 235) | hCaptcha/Turnstile nunca se resuelven |
| 2 | **Crítica** | Anti-bloqueo | `RecoveryStrategy` genera nuevo taskId rompiendo tracking | Sesiones rotadas no pueden continuar |
| 3 | **Crítica** | Fingerprint | Fuentes (`fonts`) nunca se falsean en el script de inyección | Fingerprinting de fuentes es ubicuo en anti-bots |
| 4 | **Crítica** | Behavior | Variable `dist` fuera de scope en `_ballisticMove()` | Bug: jitter para distancias largas nunca funciona |
| 5 | **Alta** | Fingerprint | 25+ propiedades de navigator/screen/WebGL/API sin falsear | Superficie de ataque enorme |
| 6 | **Alta** | CAPTCHA | TokenInjector no maneja `data-callback` attribute | Token inyectado pero callback nunca se dispara |
| 7 | **Alta** | CAPTCHA | URL completa enviada a 2captcha con parámetros de sesión | Fuga de datos sensible a tercero |
| 8 | **Alta** | Proxy | Sin coherencia geográfica proxy↔perfil | IP Alemana + timezone New York = detectable |
| 9 | **Media** | CAPTCHA | 4 proveedores de CAPTCHA no cubiertos (GeeTest, FunCaptcha, Kasada, Akamai) | No se detectan, no se reportan, no se resuelven |
| 10 | **Media** | Behavior | Bézier siempre grado 3, curva balística siempre `1-(1-t)^3` | Firma algorítmica detectable por behavioral ML |

---

## PRIORIDAD DE CORRECCIÓN POR MÓDULO

| Prioridad | Módulo | Correcciones urgentes |
|-----------|--------|----------------------|
| **1** | Integración (`index.js`) | Fix solver hardcode, fix taskId en RecoveryStrategy |
| **2** | Fingerprint (`FingerprintInjector.js`) | Fonts, Battery, Connection, doNotTrack, appVersion |
| **3** | CAPTCHA (`TokenInjector.js`, `CaptchaDetector.js`, `CaptchaSolver.js`) | data-callback, safe URL, GeeTest/FunCaptcha selectors |
| **4** | Proxy (`ProxyManager.js`) | Coherencia geográfica |
| **5** | Behavior (`HumanMouse.js`) | Fix dist bug, variar grado de Bézier |
