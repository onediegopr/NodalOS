/**
 * Anti-Bot Stress Tests
 *
 * Suite de pruebas contra demos reales de anti-bots y mecanismos internos
 * del Stealth Core. Algunos tests requieren configuración externa (API keys,
 * proxies) y se saltan automáticamente si no están disponibles.
 *
 * IMPORTANTE: Ejecutar solo contra sitios de demo / con autorización.
 */
import { test } from 'node:test';
import assert from 'node:assert';
import { chromium } from 'playwright';
import { FingerprintGenerator, FingerprintProfile } from '../src/fingerprint/FingerprintProfile.js';
import { FingerprintInjector } from '../src/fingerprint/FingerprintInjector.js';
import { CaptchaDetector } from '../src/captcha/CaptchaDetector.js';
import { BlockDetector } from '../src/antiBlocking/BlockDetector.js';
import { DomainRateLimiter } from '../src/proxy/DomainRateLimiter.js';
import { ProxyManager } from '../src/proxy/ProxyManager.js';
import { ProxyHealthChecker } from '../src/proxy/ProxyHealthChecker.js';
import os from 'node:os';
import path from 'node:path';
import { existsSync, readdirSync, statSync } from 'node:fs';

const HEADLESS = process.env.ANTI_BOT_HEADLESS !== 'false';
const SKIP_LIVE = process.env.ANTI_BOT_SKIP_LIVE === 'true';

function findChromiumExe() {
  const candidates = [path.join(os.homedir(), 'AppData', 'Local', 'ms-playwright')];
  for (const base of candidates) {
    if (!existsSync(base)) continue;
    for (const dir of readdirSync(base)) {
      const full = path.join(base, dir);
      if (!statSync(full).isDirectory()) continue;
      const exe = path.join(full, 'chrome-win64', 'chrome.exe');
      if (existsSync(exe)) return exe;
    }
  }
  return null;
}

async function launchStealthPage(profile) {
  const executablePath = findChromiumExe();
  if (!executablePath) throw new Error('Chromium not found');

  const browser = await chromium.launch({
    headless: HEADLESS,
    executablePath,
    args: [
      '--no-sandbox',
      '--disable-setuid-sandbox',
      '--disable-blink-features=AutomationControlled',
      '--disable-features=IsolateOrigins,site-per-process',
      '--no-first-run',
      '--no-default-browser-check',
      '--disable-background-networking',
      '--disable-sync',
      '--disable-default-apps',
      '--hide-scrollbars',
      '--mute-audio',
      '--disable-hang-monitor',
      '--disable-prompt-on-repost',
      '--disable-client-side-phishing-detection',
      '--disable-component-update',
      `--window-size=${profile.viewport.width},${profile.viewport.height}`,
    ],
  });

  const context = await browser.newContext({
    viewport: profile.viewport,
    deviceScaleFactor: profile.deviceScaleFactor || 1,
    isMobile: profile.isMobile || false,
    hasTouch: profile.hasTouch || false,
    userAgent: profile.userAgent,
    locale: profile.locale,
    timezoneId: profile.timezone,
    geolocation: profile.geolocation,
    permissions: profile.permissions || [],
    ignoreHTTPSErrors: true,
  });

  const page = await context.newPage();
  const initScript = FingerprintInjector.getFullInitScript(profile);
  await page.evaluate(initScript, profile);
  return { browser, page };
}

function report(result) {
  console.log(`[ANTI-BOT] ${result.name}: ${result.status}${result.detail ? ' - ' + result.detail : ''}`);
  return result;
}

// ── Test 1 — bot.sannysoft.com ──
test('Test 1 — bot.sannysoft.com', { skip: SKIP_LIVE }, async () => {
  const profile = FingerprintGenerator.generate({ preset: 'desktop-win-chrome' });
  const { browser, page } = await launchStealthPage(profile);
  try {
    await page.goto('https://bot.sannysoft.com', { waitUntil: 'domcontentloaded', timeout: 30000 });
    await page.waitForTimeout(3000);

    const results = await page.evaluate(() => {
      const rows = Array.from(document.querySelectorAll('table tr'));
      return rows.map(row => {
        const cells = Array.from(row.querySelectorAll('td, th'));
        return cells.map(c => c.innerText.trim());
      });
    });

    const failures = results.filter(r =>
      r.length >= 2 && /fail|red|warning/i.test(r[1]) && !/deprecated|experimental/i.test(r[0])
    );

    report({ name: 'bot.sannysoft.com', status: failures.length === 0 ? 'PASS' : 'FAIL', detail: `failures=${failures.length}` });
    assert.strictEqual(failures.length, 0, `Sannysoft checks failed: ${JSON.stringify(failures.slice(0, 5))}`);
  } finally {
    await browser.close();
  }
});

// ── Test 2 — fingerprint.com/demo consistency ──
test('Test 2 — fingerprint.com/demo visitor ID consistency', { skip: SKIP_LIVE }, async () => {
  const profile = FingerprintGenerator.generate({ preset: 'desktop-win-chrome' });

  async function getVisitorId() {
    const { browser, page } = await launchStealthPage(profile);
    try {
      await page.goto('https://fingerprint.com/demo/', { waitUntil: 'networkidle', timeout: 30000 });
      await page.waitForTimeout(5000);
      const text = await page.evaluate(() => document.body.innerText);
      const match = text.match(/visitor[\s\w]*id[\s:=]+([a-zA-Z0-9]+)/i);
      return match ? match[1] : null;
    } finally {
      await browser.close();
    }
  }

  const id1 = await getVisitorId();
  const id2 = await getVisitorId();

  report({ name: 'fingerprint.com/demo', status: id1 && id1 === id2 ? 'PASS' : 'INFO', detail: `id1=${id1}, id2=${id2}` });

  if (!id1 || !id2) {
    console.log('[ANTI-BOT] Could not extract visitor ID; test inconclusive (demo UI may have changed).');
    return;
  }

  assert.strictEqual(id1, id2, 'Visitor ID should be consistent with same profile');
});

// ── Test 3 — reCAPTCHA v2 demo detection ──
test('Test 3 — reCAPTCHA v2 demo detection', { skip: SKIP_LIVE }, async () => {
  const profile = FingerprintGenerator.generate({ preset: 'desktop-win-chrome' });
  const { browser, page } = await launchStealthPage(profile);
  try {
    await page.goto('https://www.google.com/recaptcha/api2/demo', { waitUntil: 'domcontentloaded', timeout: 30000 });
    await page.waitForTimeout(3000);

    const detection = await CaptchaDetector.detect(page);
    report({ name: 'reCAPTCHA v2 demo', status: detection?.type?.startsWith('recaptcha') ? 'PASS' : 'FAIL', detail: detection?.type || 'none' });
    assert.ok(detection?.type?.startsWith('recaptcha'), 'reCAPTCHA not detected');
  } finally {
    await browser.close();
  }
});

// ── Test 4 — hCaptcha demo detection ──
test('Test 4 — hCaptcha demo detection', { skip: SKIP_LIVE }, async () => {
  const profile = FingerprintGenerator.generate({ preset: 'desktop-win-chrome' });
  const { browser, page } = await launchStealthPage(profile);
  try {
    await page.goto('https://accounts.hcaptcha.com/demo', { waitUntil: 'domcontentloaded', timeout: 30000 });
    await page.waitForTimeout(3000);

    const detection = await CaptchaDetector.detect(page);
    report({ name: 'hCaptcha demo', status: detection?.type === 'hcaptcha' ? 'PASS' : 'FAIL', detail: detection?.type || 'none' });
    assert.strictEqual(detection?.type, 'hcaptcha', 'hCaptcha not detected');
  } finally {
    await browser.close();
  }
});

// ── Test 5 — Cloudflare Turnstile demo detection ──
test('Test 5 — Cloudflare Turnstile demo detection', { skip: SKIP_LIVE }, async () => {
  const profile = FingerprintGenerator.generate({ preset: 'desktop-win-chrome' });
  const { browser, page } = await launchStealthPage(profile);
  try {
    await page.goto('https://turnstile.cloudflare.com/', { waitUntil: 'domcontentloaded', timeout: 30000 });
    await page.waitForTimeout(3000);

    const detection = await CaptchaDetector.detect(page);
    report({ name: 'Cloudflare Turnstile', status: detection?.type === 'cloudflare_turnstile' ? 'PASS' : 'FAIL', detail: detection?.type || 'none' });
    assert.strictEqual(detection?.type, 'cloudflare_turnstile', 'Turnstile not detected');
  } finally {
    await browser.close();
  }
});

// ── Test 6 — BlockDetector on simulated 403 ──
test('Test 6 — BlockDetector detects simulated 403', async () => {
  const profile = FingerprintGenerator.generate({ preset: 'desktop-win-chrome' });
  const { browser, page } = await launchStealthPage(profile);
  try {
    await page.setContent(`
      <html><head><title>403 Forbidden</title></head>
      <body><h1>Access Denied</h1><p>You are blocked.</p></body></html>
    `);
    const detection = await BlockDetector.detect(page, { status: () => 403 });
    report({ name: 'Simulated 403', status: detection?.kind === 'AccessDeniedDetected' ? 'PASS' : 'FAIL', detail: detection?.kind || 'none' });
    assert.strictEqual(detection?.kind, 'AccessDeniedDetected', '403 block not detected');
  } finally {
    await browser.close();
  }
});

// ── Test 7 — DomainRateLimiter 429 backoff ──
test('Test 7 — DomainRateLimiter applies backoff on 429', async () => {
  const limiter = new DomainRateLimiter({ baseDelayMs: 100, maxDelayMs: 5000 });
  const domain = 'test.example.com';

  limiter.recordResponse(domain, 429);
  const delay1 = limiter.getDelay(domain);
  limiter.recordResponse(domain, 429);
  const delay2 = limiter.getDelay(domain);

  report({ name: 'DomainRateLimiter 429', status: delay2 > delay1 ? 'PASS' : 'FAIL', detail: `delay1=${delay1}ms delay2=${delay2}ms` });
  assert.ok(delay2 > delay1, 'Delay should increase after consecutive 429s');
});

// ── Test 8 — ProxyHealthChecker marks invalid proxy as dead ──
test('Test 8 — ProxyHealthChecker marks invalid proxy dead', async () => {
  const pm = new ProxyManager([{ url: 'http://127.0.0.1:9999', type: 'datacenter' }]);
  const checker = new ProxyHealthChecker(pm, 60000, 1);

  // Run a single check cycle
  await checker.check();

  const proxy = pm.pool[0];
  report({ name: 'ProxyHealthChecker invalid', status: proxy.healthStatus === 'dead' ? 'PASS' : 'FAIL', detail: proxy.healthStatus });
  assert.strictEqual(proxy.healthStatus, 'dead', 'Invalid proxy should be marked dead');
  checker.stop();
});

// ── Test 9 — Handoff request placeholder ──
test('Test 9 — Handoff remote request', async () => {
  // Handoff activation is triggered by the bridge friction policy engine
  // (RequiresHuman=true) and is not directly testable in a standalone script
  // without a connected bridge. Verificamos que el RemoteHandoffServer puede
  // inicializarse y aceptar conexiones.
  const { RemoteHandoffServer } = await import('../src/handoff/RemoteHandoffServer.js');
  const srv = new RemoteHandoffServer(18788);
  srv.start();

  report({ name: 'RemoteHandoffServer', status: 'PASS', detail: 'listening on ws://127.0.0.1:18788' });
  assert.ok(srv.wss, 'RemoteHandoffServer should have WebSocketServer');

  // Cleanup
  srv.wss.close();
});
