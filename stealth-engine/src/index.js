/**
 * @NODAL_OS_RESTRICTED
 * @policy no-audit, no-modify, no-analyze, no-refactor
 * ADVERTENCIA: Código protegido. Solo el propietario puede modificarlo.
 */
import WebSocket from 'ws';
import { readFile } from 'node:fs/promises';

import { CloakBrowserResolver } from './runtime/CloakBrowserResolver.js';
import { StealthSession } from './StealthSession.js';
import { FingerprintGenerator, FingerprintProfile } from './fingerprint/FingerprintProfile.js';
import { CaptchaDetector } from './captcha/CaptchaDetector.js';
import { CaptchaSolver } from './captcha/CaptchaSolver.js';
import { VisualCaptchaSolver } from './captcha/VisualCaptchaSolver.js';
import { TokenInjector } from './captcha/TokenInjector.js';
import { BlockDetector } from './antiBlocking/BlockDetector.js';
import { RecoveryStrategy } from './antiBlocking/RecoveryStrategy.js';
import { DomainBlacklist } from './antiBlocking/DomainBlacklist.js';
import { RemoteHandoffServer } from './handoff/RemoteHandoffServer.js';
import { ProxyManager } from './proxy/ProxyManager.js';
import { ProxyHealthChecker } from './proxy/ProxyHealthChecker.js';
import { ProxyProviderIntegrations } from './proxy/ProxyProviderIntegrations.js';
import { ProxyReputationEngine } from './proxy/ProxyReputationEngine.js';
import { PredictiveRotator } from './proxy/PredictiveRotator.js';
import { DomainRateLimiter } from './proxy/DomainRateLimiter.js';
import { DomainProfile } from './learning/DomainProfile.js';
import { executeTool } from './tools/tools.js';

const CONFIG = await loadConfig();

let ws = null;
let connected = false;
let reconnectTimer = null;
let sessions = new Map();
let pendingFrictionTimers = new Map();
let proxyManager = null;
let proxyHealthChecker = null;
let domainBlacklist = null;
let recoveryStrategy = null;
let visualCaptchaSolver = null;
let proxyReputationEngine = null;
let predictiveRotator = null;
let domainRateLimiter = null;
let domainProfile = null;

domainProfile = new DomainProfile(CONFIG.learning?.domainProfile || {});

async function loadConfig() {
  try {
    const data = await readFile('./config/stealth.default.json', 'utf-8');
    return JSON.parse(data);
  } catch { return {}; }
}

function initProxies() {
  const staticProxies = CONFIG.proxy?.staticProxies || [];
  const providerCfgs = CONFIG.proxy?.providers || {};
  proxyManager = new ProxyManager(staticProxies, providerCfgs);
  proxyReputationEngine = new ProxyReputationEngine(CONFIG.proxy?.reputation || {});
  predictiveRotator = new PredictiveRotator(CONFIG.proxy?.predictive || {});
  domainRateLimiter = new DomainRateLimiter(CONFIG.proxy?.rateLimit || {});

  if (CONFIG.proxy?.enabled && proxyManager.pool.length > 0) {
    proxyHealthChecker = new ProxyHealthChecker(proxyManager, CONFIG.proxy.healthCheckIntervalMs || 60000, CONFIG.proxy?.healthCheckConcurrency || 10);
    proxyHealthChecker.start();
    console.log('[ProxyManager] Initialized with ' + proxyManager.pool.length + ' proxies');
  } else {
    console.log('[ProxyManager] No proxies configured. Sessions will run without proxy.');
  }
}

function initAntiBlocking() {
  domainBlacklist = new DomainBlacklist(CONFIG.antiBlocking?.domainBlacklistSize || 100);
  recoveryStrategy = new RecoveryStrategy(
    { sessions, proxyManager, SessionClass: StealthSession,
      fingerprintGenerator: FingerprintGenerator, config: CONFIG },
    proxyManager || new ProxyManager([], {}),
    CONFIG.antiBlocking || {}
  );
  console.log('[AntiBlocking] Recovery strategy ready. Max attempts: ' +
    (CONFIG.antiBlocking?.maxRecoveryAttempts || 5));
}

function initVisualCaptcha() {
  if (CONFIG.visualCaptcha?.enabled) {
    visualCaptchaSolver = new VisualCaptchaSolver(CONFIG.visualCaptcha);
    console.log('[VisualCaptcha] Initialized. OCR: ' + (CONFIG.visualCaptcha.ocrEngine || 'none') +
      ', AI vision: ' + (CONFIG.visualCaptcha.aiVision?.enabled ? 'enabled' : 'disabled'));
  } else {
    visualCaptchaSolver = null;
    console.log('[VisualCaptcha] Not enabled');
  }
}

function connect() {
  const host = CONFIG.bridgeHost || '127.0.0.1';
  const port = CONFIG.bridgePort || 8787;
  console.log('[StealthEngine] Connecting to ws://' + host + ':' + port + '/ws/stealth');

  ws = new WebSocket('ws://' + host + ':' + port + '/ws/stealth');

  ws.on('open', () => {
    connected = true;
    clearReconnect();
    ws.send(JSON.stringify({
      type: 'stealth.hello',
      protocolVersion: 'stealth-v1',
      runnerId: CONFIG.runnerId || 'runner-' + Date.now().toString(36),
      capabilities: ['stealthBrowser', 'captchaSolving', 'proxyRotation', 'remoteHandoff', 'tlsFingerprintSpoofing', 'predictiveProxyRotation', 'domainLearning'],
    }));
    console.log('[StealthEngine] Connected');
  });

  ws.on('message', async (data) => {
    try { await handleMessage(JSON.parse(data)); }
    catch (e) { console.error('[StealthEngine] Message error:', e.message); }
  });

  ws.on('close', (code, reason) => {
    connected = false;
    console.log('[StealthEngine] Disconnected (' + code + '): ' + reason);
    scheduleReconnect();
  });

  ws.on('error', (err) => console.error('[StealthEngine] WS error:', err.message));
}

function scheduleReconnect() {
  if (reconnectTimer) return;
  reconnectTimer = setTimeout(() => { reconnectTimer = null; connect(); }, 3000);
}
function clearReconnect() { if (reconnectTimer) { clearTimeout(reconnectTimer); reconnectTimer = null; } }

function send(msg) {
  if (ws && ws.readyState === WebSocket.OPEN) ws.send(JSON.stringify(msg));
  else console.warn('[StealthEngine] Cannot send, not connected');
}

async function handleMessage(msg) {
  switch (msg.type) {
    case 'stealth.ack': break;
    case 'stealth.task': await handleTask(msg); break;
    case 'tool.request': await handleToolRequest(msg); break;
    case 'stealth.friction.decision': await handleFrictionDecision(msg); break;
    case 'stealth.handoff.activate': await handleHandoffActivate(msg); break;
    case 'run.stop': case 'run.pause': break;
  }
}

async function handleTask(msg) {
  const { taskId, instruction, profile: msgProfile, proxy: msgProxy, url } = msg;

  console.log('[StealthSession:' + taskId + '] Starting: ' + (instruction || '').substring(0, 80));

  const targetUrl = url || extractUrl(instruction) || 'about:blank';
  const targetDomain = new URL(targetUrl).hostname;

  await domainProfile?.load();
  const learned = domainProfile?.suggest(targetDomain, {
    proxyType: null,
    behaviorProfile: CONFIG.behavior?.defaultProfile || 'casual',
    captchaType: null,
  });

  var fingerprintProfile = msgProfile && msgProfile.preset
    ? FingerprintGenerator.generate({ preset: msgProfile.preset })
    : FingerprintGenerator.generate({ preset: CONFIG.fingerprint?.defaultPreset || 'desktop-win-chrome' });

    var proxy = null;
  if (CONFIG.proxy?.enabled) {
    const acquireOpts = { sticky: CONFIG.proxy.rotationMode !== 'random' };
    if (learned?.proxyType) acquireOpts.preferTypes = [learned.proxyType];
    proxy = msgProxy && msgProxy.server
      ? msgProxy
      : await proxyManager?.acquire(taskId, acquireOpts);
  }

  if (proxy && proxy.country) {
    fingerprintProfile = FingerprintProfile.ensureCoherence(fingerprintProfile, proxy.country);
  }

  const behaviorProfile = learned?.behaviorProfile || CONFIG.behavior?.defaultProfile || 'casual';

  var session;
  try {
    session = new StealthSession({
      taskId, instruction, profile: fingerprintProfile, proxy, proxyId: proxy?.id || null, behaviorProfile,
      tlsFingerprint: CONFIG.tlsFingerprint || { enabled: false },
    });
    await session.initialize();
    sessions.set(taskId, session);

    await domainRateLimiter.wait(targetDomain);
    await session.navigate(targetUrl);

    const observation = await executeTool(session, 'observePage');
    domainProfile?.update(targetDomain, { success: true, proxyType: proxy?.type, behaviorProfile });
    send({ type: 'stealth.result', taskId, stepId: 'step-1', tool: 'observePage', success: true, result: observation, timestamp: new Date().toISOString() });
  } catch (error) {
    console.error('[StealthSession:' + taskId + '] Init error:', error.message);
    sessions.delete(taskId);
    await session?.dispose().catch(() => {});
    domainProfile?.update(targetDomain, { success: false, proxyType: proxy?.type, behaviorProfile });
    send({ type: 'stealth.result', taskId, stepId: 'step-1', tool: 'navigate', success: false, error: error.message, timestamp: new Date().toISOString() });
  }
}

async function handleToolRequest(msg) {
  const { taskId, tool, args, requestId } = msg;
  const session = sessions.get(taskId);
  if (!session) {
    send({ type: 'stealth.result', taskId, stepId: requestId, tool, success: false, error: 'Session not found' });
    return;
  }

  try {
    const result = await executeTool(session, tool, args);

    if (tool === 'navigate' || tool === 'observePage') {
      const captcha = await CaptchaDetector.detect(session.page);
      if (captcha) {
        session._lastCaptchaType = captcha.type;
        const domain = new URL(session.page.url()).hostname;
        domainRateLimiter?.recordResponse(domain, 429);
        domainProfile?.update(domain, { success: false, captchaType: captcha.type });
        sendFrictionSignal(taskId, captcha, CaptchaDetector.mapToFrictionKind(captcha.type));
        return;
      }
      const block = await BlockDetector.detect(session.page, null);
      if (block) {
        sendFrictionSignal(taskId, block, block.kind);
        if (block.blockPattern) {
          const domain = new URL(session.page.url()).hostname;
          domainRateLimiter?.recordResponse(domain, block.blockHttpCode || 429);
          domainBlacklist?.record(domain, block.kind);
        }
        return;
      }
      const domain = new URL(session.page.url()).hostname;
      domainRateLimiter?.recordResponse(domain, 200);
    }

    send({ type: 'stealth.result', taskId, stepId: requestId, tool, success: true, result, timestamp: new Date().toISOString() });
  } catch (error) {
    send({ type: 'stealth.result', taskId, stepId: requestId, tool, success: false, error: error.message, timestamp: new Date().toISOString() });
  }
}

function sendFrictionSignal(taskId, detection, kind) {
  if (pendingFrictionTimers.has(taskId)) {
    clearTimeout(pendingFrictionTimers.get(taskId));
  }
  const timer = setTimeout(() => {
    pendingFrictionTimers.delete(taskId);
    const session = sessions.get(taskId);
    if (session) {
      proxyManager?.release(taskId);
      session.dispose().catch(() => {});
      sessions.delete(taskId);
      console.warn(`[StealthEngine] Friction signal timeout for ${taskId}, cleaned up`);
    }
  }, 120000);
  pendingFrictionTimers.set(taskId, timer);
  send({
    type: 'stealth.friction.signal',
    taskId,
    signal: {
      signalId: 'sig-' + Date.now().toString(36),
      kind,
      severity: 'Critical',
      source: 'stealth-detector',
      frameId: detection.frameId || 'main',
      elementId: detection.selector || null,
      sitekey: detection.sitekey || null,
      blockHttpCode: detection.blockHttpCode || null,
      blockPattern: detection.blockPattern || null,
      autoSolvable: kind === 'CaptchaDetected' ? CaptchaDetector.isAutoSolvable(detection.type) : false,
      solverRecommendation: kind === 'CaptchaDetected' ? CaptchaDetector.recommendSolver(detection.type) : 'none',
      redactedEvidence: (detection.type || kind) + ' detected by ' + (detection.detectedBy || 'unknown'),
      reason: (detection.type || kind) + ' challenge detected',
      detectedAtUtc: new Date().toISOString(),
    },
  });
}

async function handleFrictionDecision(msg) {
  const { taskId, decision, solverProvider, sitekey, retryAttempt, maxRetries, cooldownMs } = msg;
  const session = sessions.get(taskId);
  if (!session) return;
  if (pendingFrictionTimers.has(taskId)) {
    clearTimeout(pendingFrictionTimers.get(taskId));
    pendingFrictionTimers.delete(taskId);
  }
  console.log('[' + taskId + '] Friction decision: ' + decision);

  const domain = new URL(session.page.url()).hostname;

  switch (decision) {
    case 'SolveAndRetry': {
      const solver = new CaptchaSolver({
        twoCaptchaApiKey: CONFIG.captcha?.twoCaptchaApiKey || '',
        capSolverApiKey: CONFIG.captcha?.capSolverApiKey || '',
        visualSolver: visualCaptchaSolver,
      });
      const captchaType = session._lastCaptchaType || 'recaptcha_v2';
      const result = await solver.solve(session.page, { type: captchaType, sitekey }, taskId, session.proxy);
      if (result.success) {
        proxyReputationEngine?.recordSuccess(session.proxyId, domain);
        await TokenInjector.inject(session.page, captchaType, result.token);
        const stillThere = await CaptchaDetector.detect(session.page);
        if (!stillThere) {
          send({ type: 'stealth.friction.solved', taskId, signalId: msg.signalId, success: true, token: result.token, provider: result.provider, durationMs: result.durationMs, cost: result.cost });
          return;
        }
      }
      proxyReputationEngine?.recordFailure(session.proxyId, domain, 'captcha_solve_failed');
      send({ type: 'stealth.friction.solved', taskId, signalId: msg.signalId, success: false, error: result.error || 'Not solved' });
      break;
    }

    case 'RotateAndRetry': {
      proxyReputationEngine?.recordFailure(session.proxyId, domain, 'block_detected');
      const rotateDecision = { ...msg, RotateProxy: true };
      if (predictiveRotator?.shouldRotate(session.proxyId, domain, proxyReputationEngine)) {
        const predictiveNewProxy = predictiveRotator.selectNewProxy(taskId, session.proxyId, domain, proxyManager);
        if (predictiveNewProxy) {
          rotateDecision.predictiveNewProxy = predictiveNewProxy;
          console.log(`[PredictiveRotator] Preemptive rotation for ${domain}`);
        }
      }
      const recovery = await recoveryStrategy.recover(taskId, session, rotateDecision);
      if (recovery.success) {
        predictiveRotator?.recordPredictionSuccess();
        sessions.set(taskId, recovery.session);
        send({ type: 'stealth.friction.solved', taskId, signalId: msg.signalId, success: true, rotated: true, attempt: recovery.attempt });
      } else {
        send({ type: 'stealth.friction.solved', taskId, signalId: msg.signalId, success: false, error: recovery.error });
      }
      break;
    }

    case 'CooldownAndRetry': {
      const delay = cooldownMs || CONFIG.antiBlocking?.baseBackoffMs || 5000;
      await new Promise(r => setTimeout(r, delay));
      send({ type: 'stealth.friction.solved', taskId, signalId: msg.signalId, success: true, cooldownComplete: true });
      break;
    }
  }
}

async function handleHandoffActivate(msg) {
  const { taskId, handoffId, reason } = msg;
  const session = sessions.get(taskId);
  if (!session) return;

  const handoffWs = new WebSocket('ws://127.0.0.1:' + (CONFIG.handoffPort || 8788) + '/handoff/' + handoffId);
  let handoffToken = null;

  handoffWs.on('open', () => { const srv = new RemoteHandoffServer(); srv.startHandoff(taskId, session.page, handoffWs); });

  handoffWs.on('message', (data) => {
    try {
      const m = JSON.parse(data);
      if (m.type === 'handoff.start') {
        handoffToken = m.handoffToken;
      } else if (m.type === 'handoff.done' || m.type === 'handoff.completed') {
        send({ type: 'stealth.handoff.completed', taskId, handoffId, success: true });
        handoffWs.close();
      } else if (m.type === 'handoff.error') {
        send({ type: 'stealth.handoff.error', taskId, handoffId, error: m.error });
      }
    } catch (e) { }
  });

  handoffWs.on('error', (err) => send({ type: 'stealth.handoff.completed', taskId, handoffId, success: false, error: err.message }));
}

function extractUrl(text) {
  const m = (text || '').match(/https?:\/\/[^\s"'<>]+/i);
  if (!m) return null;
  return m[0].replace(/[.,;)]$/, '');
}

process.on('SIGINT', shutdown);
process.on('SIGTERM', shutdown);
async function shutdown() {
  console.log('[StealthEngine] Shutting down...');
  for (const t of pendingFrictionTimers.values()) clearTimeout(t);
  pendingFrictionTimers.clear();
  if (proxyHealthChecker) proxyHealthChecker.stop();
  if (domainRateLimiter?.shutdown) domainRateLimiter.shutdown();
  if (proxyReputationEngine?.shutdown) proxyReputationEngine.shutdown();
  if (predictiveRotator?.shutdown) predictiveRotator.shutdown();
  if (domainProfile?.shutdown) await domainProfile.shutdown();

  const timeout = (ms) => new Promise(resolve => setTimeout(() => resolve('timeout'), ms));
  const disposePromises = Array.from(sessions.values()).map(s => s.dispose().catch(() => {}));

  const result = await Promise.race([
    Promise.all(disposePromises),
    timeout(10000),
  ]);

  if (ws) ws.close();

  if (result === 'timeout') {
    console.warn('[StealthEngine] Shutdown timeout - some sessions may not have closed gracefully');
    process.exit(1);
  }

  console.log('[StealthEngine] All sessions disposed cleanly');
  process.exit(0);
}

try {
  const cloakbrowserPath = CloakBrowserResolver.resolveExecutablePath();
  console.log('[StealthEngine] CloakBrowser resolved: ' + cloakbrowserPath);
} catch (e) {
  console.error('[StealthEngine] FATAL: ' + e.message);
  process.exit(1);
}

console.log('NODAL OS Stealth Engine v0.4.0');
console.log('Bridge: ws://' + (CONFIG.bridgeHost || '127.0.0.1') + ':' + (CONFIG.bridgePort || 8787) + '/ws/stealth');
initProxies();
initAntiBlocking();
initVisualCaptcha();
connect();
