import WebSocket from 'ws';
import { readFile } from 'node:fs/promises';

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
import { executeTool } from './tools/tools.js';

const CONFIG = await loadConfig();

let ws = null;
let connected = false;
let reconnectTimer = null;
let sessions = new Map();
let proxyManager = null;
let proxyHealthChecker = null;
let domainBlacklist = null;
let recoveryStrategy = null;
let visualCaptchaSolver = null;

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

  if (CONFIG.proxy?.enabled && proxyManager.pool.length > 0) {
    proxyHealthChecker = new ProxyHealthChecker(proxyManager, CONFIG.proxy.healthCheckIntervalMs || 60000);
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
      capabilities: ['stealthBrowser', 'captchaSolving', 'proxyRotation', 'remoteHandoff'],
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

  const fingerprintProfile = msgProfile && msgProfile.preset
    ? FingerprintGenerator.generate({ preset: msgProfile.preset })
    : FingerprintGenerator.generate({ preset: CONFIG.fingerprint?.defaultPreset || 'desktop-win-chrome' });

  var proxy = null;
  if (CONFIG.proxy?.enabled) {
    proxy = msgProxy && msgProxy.server
      ? msgProxy
      : proxyManager?.acquire(taskId, { sticky: CONFIG.proxy.rotationMode !== 'random' });
  }

  if (proxy && proxy.country) {
    FingerprintProfile.ensureCoherence(fingerprintProfile, proxy.country);
  }

  const behaviorProfile = CONFIG.behavior?.defaultProfile || 'casual';

  try {
    const session = new StealthSession({
      taskId, instruction, profile: fingerprintProfile, proxy, behaviorProfile,
    });
    await session.initialize();
    sessions.set(taskId, session);

    const targetUrl = url || extractUrl(instruction) || 'about:blank';
    await session.navigate(targetUrl);

    const observation = await executeTool(session, 'observePage');
    send({ type: 'stealth.result', taskId, stepId: 'step-1', tool: 'observePage', success: true, result: observation, timestamp: new Date().toISOString() });
  } catch (error) {
    console.error('[StealthSession:' + taskId + '] Init error:', error.message);
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
        sendFrictionSignal(taskId, captcha, CaptchaDetector.mapToFrictionKind(captcha.type));
        return;
      }
      const block = await BlockDetector.detect(session.page, null);
      if (block) {
        sendFrictionSignal(taskId, block, block.kind);
        if (block.blockPattern) {
          const domain = new URL(session.page.url()).hostname;
          domainBlacklist?.record(domain, block.kind);
        }
        return;
      }
    }

    send({ type: 'stealth.result', taskId, stepId: requestId, tool, success: true, result, timestamp: new Date().toISOString() });
  } catch (error) {
    send({ type: 'stealth.result', taskId, stepId: requestId, tool, success: false, error: error.message, timestamp: new Date().toISOString() });
  }
}

function sendFrictionSignal(taskId, detection, kind) {
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
  console.log('[' + taskId + '] Friction decision: ' + decision);

  switch (decision) {
    case 'SolveAndRetry': {
      const solver = new CaptchaSolver({
        twoCaptchaApiKey: CONFIG.captcha?.twoCaptchaApiKey || '',
        visualSolver: visualCaptchaSolver,
      });
      const captchaType = session._lastCaptchaType || 'recaptcha_v2';
      const result = await solver.solve(session.page, { type: captchaType, sitekey }, taskId);
      if (result.success) {
        await TokenInjector.inject(session.page, captchaType, result.token);
        const stillThere = await CaptchaDetector.detect(session.page);
        if (!stillThere) {
          send({ type: 'stealth.friction.solved', taskId, signalId: msg.signalId, success: true, token: result.token, provider: result.provider, durationMs: result.durationMs, cost: result.cost });
          return;
        }
      }
      send({ type: 'stealth.friction.solved', taskId, signalId: msg.signalId, success: false, error: result.error || 'Not solved' });
      break;
    }

    case 'RotateAndRetry': {
      const recovery = await recoveryStrategy.recover(taskId, session, msg);
      if (recovery.success) {
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
  handoffWs.on('open', () => { const srv = new RemoteHandoffServer(); srv.startHandoff(taskId, session.page, handoffWs); });
  handoffWs.on('message', (data) => {
    try {
      const m = JSON.parse(data);
      if (m.type === 'handoff.done' || m.type === 'handoff.completed') {
        send({ type: 'stealth.handoff.completed', taskId, handoffId, success: true });
        handoffWs.close();
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
  if (proxyHealthChecker) proxyHealthChecker.stop();
  for (const [, s] of sessions) { await s.dispose().catch(() => {}); }
  if (ws) ws.close();
  process.exit(0);
}

console.log('NODAL OS Stealth Engine v0.2.0');
console.log('Bridge: ws://' + (CONFIG.bridgeHost || '127.0.0.1') + ':' + (CONFIG.bridgePort || 8787) + '/ws/stealth');
initProxies();
initAntiBlocking();
initVisualCaptcha();
connect();
