const PROTOCOL_VERSION = 'chrome-lab-v1';
importScripts('recipe_core.js');

const DEFAULT_CONFIG = { host: '127.0.0.1', port: '8787', token: '' };
const RECIPES_KEY = 'nexaRecipes';
const LEARNING_DRAFT_KEY = 'nexaLearningDraft';
const SESSION_STATE_KEY = 'nexaRuntimeState';
const OUTGOING_QUEUE_LIMIT = 40;
const HEARTBEAT_MS = 15000;
const BACKOFF_STEPS = [250, 500, 1000, 2000, 5000];

let socket = null;
let connected = false;
let connectingPromise = null;
let reconnectTimer = null;
let heartbeatTimer = null;
let reconnectAttempt = 0;
let pingSeq = 0;
let lastPingSeq = 0;
let missedPongs = 0;
let connectionState = 'disconnected';
let clientId = '';
let lastConnectedAt = '';
let lastSeenAt = '';
let lastConnectionError = '';
let outgoingQueue = [];
let runtimeDebug = null;
let stopped = false;
let currentRunId = '';
let targetTabId = null;
let currentRequestId = '';
let currentTool = '';
let lastToolRequest = null;
let lastToolResult = null;
let lastRunStatus = null;
let lastHealth = null;
let lastAiError = '';
let learningSession = null;
let recipeRunner = null;
const sidePorts = new Set();

chrome.runtime.onInstalled.addListener(() => {
  chrome.sidePanel.setPanelBehavior({ openPanelOnActionClick: true });
  initializeRuntimeLifecycle();
});

chrome.runtime.onStartup.addListener(() => {
  initializeRuntimeLifecycle();
});

chrome.alarms.onAlarm.addListener((alarm) => {
  if (alarm.name !== 'nexa.keepalive') {
    return;
  }
  keepAliveTick();
});

initializeRuntimeLifecycle();

chrome.runtime.onConnect.addListener((port) => {
  if (port.name !== 'onebrain-sidepanel') {
    return;
  }

  sidePorts.add(port);
  port.onDisconnect.addListener(() => sidePorts.delete(port));
  port.onMessage.addListener((message) => handlePanelMessage(message));
  publishState('local', 'Side panel connected');
});

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  if (message && message.type === 'learning.event') {
    handleLearningEvent(message.event, sender).then(() => sendResponse({ ok: true }));
    return true;
  }
  return false;
});

chrome.tabs.onActivated.addListener(() => publishPageSnapshot());
chrome.tabs.onUpdated.addListener((_tabId, changeInfo) => {
  if (changeInfo.status === 'complete') {
    publishPageSnapshot();
  }
});

async function initializeRuntimeLifecycle() {
  await hydrateRuntimeState();
  chrome.alarms.create('nexa.keepalive', { periodInMinutes: 0.4 });
  const config = await chrome.storage.local.get(DEFAULT_CONFIG);
  if (config.host && config.port) {
    connectWebSocket(config).catch(() => {});
  }
}

async function hydrateRuntimeState() {
  const session = await chrome.storage.session.get({ [SESSION_STATE_KEY]: null });
  const saved = session[SESSION_STATE_KEY] || {};
  clientId = saved.clientId || crypto.randomUUID();
  currentRunId = saved.currentRunId || currentRunId || '';
  currentRequestId = saved.currentRequestId || currentRequestId || '';
  connectionState = saved.connectionState || connectionState;
  lastConnectedAt = saved.lastConnectedAt || '';
  lastSeenAt = saved.lastSeenAt || '';
  recipeRunner = saved.recipeRunner || recipeRunner;
  outgoingQueue = Array.isArray(saved.pendingOutgoingMessages) ? saved.pendingOutgoingMessages.slice(-OUTGOING_QUEUE_LIMIT) : [];
  await persistRuntimeState();
}

async function persistRuntimeState() {
  await chrome.storage.session.set({
    [SESSION_STATE_KEY]: {
      clientId,
      currentRunId,
      currentRequestId,
      connectionState,
      lastConnectedAt,
      lastSeenAt,
      recipeRunner,
      pendingOutgoingMessages: outgoingQueue.slice(-OUTGOING_QUEUE_LIMIT)
    }
  }).catch(() => {});
}

async function keepAliveTick() {
  const config = await chrome.storage.local.get(DEFAULT_CONFIG);
  if (!socket || socket.readyState !== WebSocket.OPEN) {
    scheduleReconnect();
  } else {
    sendPing();
  }
  await refreshDebug(config, { quiet: true });
  await persistRuntimeState();
}

async function handlePanelMessage(message) {
  switch (message.type) {
    case 'connect':
      await saveConfig(message.config);
      try {
        await connect(message.config);
      } catch (error) {
        publishState('error', String(error && error.message ? error.message : error));
      }
      break;
    case 'disconnect':
      disconnect('userDisconnect');
      break;
    case 'testHealth':
      testHealth(message.config);
      break;
    case 'refreshDebug':
      refreshDebug(message.config || await chrome.storage.local.get(DEFAULT_CONFIG));
      break;
    case 'startRun':
      startRun(message.instruction);
      break;
    case 'pause':
      runCommand('pause');
      break;
    case 'resume':
      stopped = false;
      runCommand('resume');
      break;
    case 'stop':
      stopAll('userStop');
      break;
    case 'resumeHuman':
      stopped = false;
      runCommand('resume');
      break;
    case 'loadConfig':
      publishSavedConfig();
      publishRuntimeSnapshot();
      publishRecipes();
      publishLearningState();
      break;
    case 'learningStart':
      startLearning(message.payload || {});
      break;
    case 'learningStop':
      stopLearning();
      break;
    case 'learningSaveRecipe':
      saveLearningRecipe(message.payload || {});
      break;
    case 'learningClearDraft':
      clearLearningDraft();
      break;
    case 'recipesList':
      publishRecipes();
      break;
    case 'recipeSave':
      saveRecipe(message.recipe);
      break;
    case 'recipeDelete':
      deleteRecipe(message.recipeId);
      break;
    case 'recipeDuplicate':
      duplicateRecipe(message.recipeId);
      break;
    case 'recipeRun':
      startRecipeRun(message.recipeId, message.parameters || {});
      break;
    case 'recipePause':
      pauseRecipeRun('pausedByUser');
      break;
    case 'recipeResume':
      resumeRecipeRun();
      break;
    case 'recipeRetryStep':
      retryRecipeStep();
      break;
    case 'recipeSkipStep':
      skipRecipeStep();
      break;
    case 'recipeAbort':
      abortRecipeRun('abortedByUser');
      break;
    default:
      publishState('error', `Unknown panel message: ${message.type}`);
      break;
  }
}

async function publishSavedConfig() {
  const config = await chrome.storage.local.get(DEFAULT_CONFIG);
  publish({ type: 'config', config });
}

async function saveConfig(config) {
  await chrome.storage.local.set({
    host: config.host || DEFAULT_CONFIG.host,
    port: config.port || DEFAULT_CONFIG.port,
    token: config.token || DEFAULT_CONFIG.token
  });
}

function connect(config) {
  return connectWebSocket(config);
}

async function connectWebSocket(config) {
  config = config || await chrome.storage.local.get(DEFAULT_CONFIG);
  if (!clientId) {
    await hydrateRuntimeState();
  }
  if (socket && socket.readyState === WebSocket.OPEN && connected) {
    return Promise.resolve();
  }

  if (connectingPromise) {
    return connectingPromise;
  }

  clearReconnectTimer();
  closeSocketOnly('reconnect');
  stopped = false;
  connectionState = 'connecting';
  const url = `ws://${config.host || DEFAULT_CONFIG.host}:${config.port || DEFAULT_CONFIG.port}/ws/extension`;

  connectingPromise = new Promise((resolve, reject) => {
    socket = new WebSocket(url);
    socket.addEventListener('open', () => {
      connected = true;
      connectionState = 'connected';
      connectingPromise = null;
      reconnectAttempt = 0;
      missedPongs = 0;
      lastConnectedAt = new Date().toISOString();
      lastSeenAt = lastConnectedAt;
      publishState('connected', `Connected to ${url}`);
      sendToEngine({
        type: 'extension.hello',
        protocolVersion: PROTOCOL_VERSION,
        clientId,
        extensionVersion: chrome.runtime.getManifest().version,
        browser: 'Chrome',
        token: config.token || '',
        resumeRunId: currentRunId || null
      });
      flushOutgoingQueue();
      startHeartbeat();
      persistRuntimeState();
      resolve();
    });
    socket.addEventListener('message', (event) => handleEngineMessage(event.data));
    socket.addEventListener('close', () => {
      connected = false;
      connectingPromise = null;
      connectionState = stopped ? 'stopped' : 'reconnecting';
      stopHeartbeat();
      publishState(connectionState, 'Bridge connection closed');
      persistRuntimeState();
      scheduleReconnect();
      resolve();
    });
    socket.addEventListener('error', () => {
      connected = false;
      connectingPromise = null;
      connectionState = 'error';
      lastConnectionError = 'Bridge WebSocket error';
      stopHeartbeat();
      publishState('error', 'Bridge WebSocket error');
      persistRuntimeState();
      scheduleReconnect();
      reject(new Error('Bridge WebSocket error'));
    });
  });

  return connectingPromise;
}

function disconnect(reason) {
  clearReconnectTimer();
  closeSocketOnly(reason);
  socket = null;
  connected = false;
  connectingPromise = null;
  connectionState = 'disconnected';
  stopHeartbeat();
  publishState('disconnected', reason);
  persistRuntimeState();
}

function closeSocketOnly(reason) {
  if (socket) {
    try {
      socket.close(1000, reason);
    } catch {
      // Best effort close; reconnect logic owns recovery.
    }
  }
}

function scheduleReconnect() {
  if (stopped || reconnectTimer) {
    return;
  }
  reconnectAttempt += 1;
  const baseDelay = BACKOFF_STEPS[Math.min(reconnectAttempt - 1, BACKOFF_STEPS.length - 1)];
  const jitter = Math.floor(Math.random() * 120);
  reconnectTimer = setTimeout(async () => {
    reconnectTimer = null;
    const config = await chrome.storage.local.get(DEFAULT_CONFIG);
    connectWebSocket(config).catch(() => {});
  }, baseDelay + jitter);
}

function clearReconnectTimer() {
  if (reconnectTimer) {
    clearTimeout(reconnectTimer);
    reconnectTimer = null;
  }
}

async function testHealth(config) {
  const baseUrl = `http://${config.host || DEFAULT_CONFIG.host}:${config.port || DEFAULT_CONFIG.port}`;
  const url = `${baseUrl}/health`;
  try {
    const response = await fetch(url);
    const body = await response.json();
    let publicConfig = null;
    try {
      const configResponse = await fetch(`${baseUrl}/config/public`, { cache: 'no-store' });
      publicConfig = await configResponse.json();
    } catch {
      publicConfig = null;
    }
    await refreshDebug(config, { quiet: true });
    lastHealth = { ok: response.ok && body.ok, body: { ...body, publicConfig }, checkedAt: new Date().toISOString() };
    publish({ type: 'health', ok: response.ok && body.ok, body });
    publishRuntimeSnapshot();
  } catch (error) {
    lastHealth = { ok: false, error: String(error && error.message ? error.message : error), checkedAt: new Date().toISOString() };
    publish({ type: 'health', ok: false, error: String(error && error.message ? error.message : error) });
    publishRuntimeSnapshot();
  }
}

async function refreshDebug(config, options = {}) {
  const baseUrl = `http://${config.host || DEFAULT_CONFIG.host}:${config.port || DEFAULT_CONFIG.port}`;
  try {
    const [runtimeResponse, clientsResponse, eventsResponse, debugResponse] = await Promise.all([
      fetch(`${baseUrl}/runtime`, { cache: 'no-store' }),
      fetch(`${baseUrl}/clients`, { cache: 'no-store' }),
      fetch(`${baseUrl}/last-events`, { cache: 'no-store' }),
      fetch(`${baseUrl}/debug`, { cache: 'no-store' })
    ]);
    runtimeDebug = {
      runtime: await runtimeResponse.json(),
      clients: await clientsResponse.json(),
      events: await eventsResponse.json(),
      debug: await debugResponse.json(),
      checkedAt: new Date().toISOString()
    };
    publish({ type: 'debugSnapshot', debug: runtimeDebug });
    publishRuntimeSnapshot();
  } catch (error) {
    if (!options.quiet) {
      publish({ type: 'debugSnapshot', error: String(error && error.message ? error.message : error) });
    }
  }
}

async function startRun(instruction) {
  const config = await chrome.storage.local.get(DEFAULT_CONFIG);
  const url = `http://${config.host}:${config.port}/api/runs`;
  try {
    await connectWebSocket(config);
    const response = await fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ instruction, mode: 'lab' })
    });
    const body = await response.json();
    currentRunId = body.runId || '';
    publish({ type: 'runStarted', ok: response.ok, body });
    if (!response.ok) {
      publishState('error', body.message || body.error || 'Run rejected');
    }
  } catch (error) {
    publishState('error', String(error && error.message ? error.message : error));
  }
}

async function runCommand(command) {
  if (!currentRunId) {
    publishState('error', 'No active run');
    return;
  }
  const config = await chrome.storage.local.get(DEFAULT_CONFIG);
  const url = `http://${config.host}:${config.port}/api/runs/${currentRunId}/${command}`;
  try {
    const response = await fetch(url, { method: 'POST' });
    const body = await response.json();
    publish({ type: 'runCommand', command, ok: response.ok, body });
  } catch (error) {
    publishState('error', String(error && error.message ? error.message : error));
  }
}

function stopAll(reason) {
  stopped = true;
  abortRecipeRun(reason || 'userStop');
  notifyStopToTargetTab();
  if (currentRunId) {
    runCommand('stop');
  }
  sendToEngine({ type: 'run.stop', runId: currentRunId, reason });
  publishState('stopped', reason);
}

function handleEngineMessage(raw) {
  let message;
  try {
    message = JSON.parse(raw);
  } catch {
    publishState('error', 'Invalid JSON from engine');
    return;
  }

  publish({ type: 'engineMessage', message });
  lastSeenAt = new Date().toISOString();
  persistRuntimeState();
  if (message.type === 'engine.hello') {
    publishState('connected', 'Engine hello received');
    publishRuntimeSnapshot();
    return;
  }

  if (message.type === 'engine.pong') {
    if (Number(message.seq || 0) === lastPingSeq) {
      missedPongs = 0;
    }
    publishRuntimeSnapshot();
    return;
  }

  if (message.type === 'protocol.error') {
    lastConnectionError = message.message || message.error || 'Protocol error';
    connectionState = message.error === 'invalid_token' ? 'tokenError' : 'protocolError';
    publishState('error', lastConnectionError);
    publishRuntimeSnapshot();
    return;
  }

  if (message.type === 'run.pause') {
    publish({ type: 'humanIntervention', reason: message.reason, message: message.message });
    publishState('paused', message.message || message.reason);
    return;
  }

  if (message.type === 'run.resume') {
    publishState('running', 'Run resumed');
    return;
  }

  if (message.type === 'run.status') {
    lastRunStatus = message;
    if (message.status === 'error') {
      lastAiError = message.message || '';
    }
    publish({ type: 'runStatus', message });
    publishState(message.status || 'running', message.message || '');
    publishRuntimeSnapshot();
    return;
  }

  if (message.type === 'run.stop') {
    stopAll(message.reason || 'engineStop');
    return;
  }

  if (message.type === 'tool.request') {
    currentRequestId = message.requestId || '';
    currentTool = message.tool || '';
    lastToolRequest = message;
    publishRuntimeSnapshot();
    routeToolRequest(message);
  }
}

async function routeToolRequest(message) {
  if (stopped) {
    sendToolResult(message, false, null, 'Stopped');
    return;
  }

  const validation = validateToolMessage(message);
  if (!validation.ok) {
    sendToolResult(message, false, null, validation.error);
    return;
  }

  try {
    if (message.tool === 'navigate') {
      await navigate(message.args.url);
      sendToolResult(message, true, { navigated: true }, null);
      return;
    }

    const tab = await resolveTargetTab();
    if (!tab || !tab.id || restrictedUrl(tab.url || '')) {
      sendToolResult(message, false, null, 'Restricted or unavailable tab');
      return;
    }

    const response = await chrome.tabs.sendMessage(tab.id, {
      type: 'tool.execute',
      protocolVersion: PROTOCOL_VERSION,
      tool: message.tool,
      args: message.args || {}
    });

    if (response && response.success && response.result && response.result.hasCredentialLike) {
      publish({ type: 'humanIntervention', reason: 'credentialRequired', message: 'Credential/login/2FA/captcha detected.' });
    }
    sendToolResult(message, Boolean(response && response.success), response ? response.result : null, response ? response.error : 'No response');
  } catch (error) {
    sendToolResult(message, false, null, String(error && error.message ? error.message : error));
  }
}

function validateToolMessage(message) {
  const allowed = new Set([
    'observePage',
    'getElementCatalog',
    'resolveTarget',
    'getCurrentTab',
    'navigate',
    'query',
    'read',
    'readElement',
    'click',
    'clickElement',
    'setValue',
    'setElementValue',
    'focusElement',
    'selectOption',
    'scrollIntoView',
    'scrollElementIntoView',
    'waitForSelector',
    'highlight',
    'highlightElement',
    'clearHighlight',
    'pauseForHuman',
    'stop'
  ]);
  if (!allowed.has(message.tool)) {
    return { ok: false, error: 'Tool not allowed' };
  }
  if (message.tool === 'navigate' && !allowedUrl(String((message.args || {}).url || ''))) {
    return { ok: false, error: 'URL rejected' };
  }
  if (message.tool === 'resolveTarget' && !String((message.args || {}).targetText || '').trim()) {
    return { ok: false, error: 'targetText required' };
  }
  return { ok: true };
}

async function navigate(url) {
  if (!allowedUrl(url)) {
    throw new Error('URL rejected');
  }

  const tab = await resolveTargetTab({ allowCreate: true });
  if (tab && tab.id) {
    targetTabId = tab.id;
    await chrome.tabs.update(tab.id, { url, active: true });
    await waitForTabComplete(tab.id, 15000);
    return;
  }

  const created = await chrome.tabs.create({ url, active: true });
  if (!created || !created.id) {
    throw new Error('Unable to create target tab');
  }

  targetTabId = created.id;
  await waitForTabComplete(created.id, 15000);
}

function waitForTabComplete(tabId, timeoutMs) {
  return new Promise((resolve) => {
    let done = false;
    const timer = setTimeout(() => finish(), timeoutMs);
    const listener = (updatedTabId, changeInfo) => {
      if (updatedTabId === tabId && changeInfo.status === 'complete') {
        finish();
      }
    };

    function finish() {
      if (done) {
        return;
      }
      done = true;
      clearTimeout(timer);
      chrome.tabs.onUpdated.removeListener(listener);
      resolve();
    }

    chrome.tabs.onUpdated.addListener(listener);
  });
}

function sendToolResult(request, success, result, error) {
  lastToolResult = { request, success, result, error, at: new Date().toISOString() };
  sendToEngine({
    type: 'tool.result',
    runId: request.runId,
    requestId: request.requestId,
    success,
    result,
    error
  });
  publish({ type: 'toolResult', request, success, result, error });
  publishRuntimeSnapshot();
}

function sendToEngine(message) {
  if (!socket || socket.readyState !== WebSocket.OPEN) {
    enqueueOutgoing(message);
    publishState('reconnecting', 'Not connected to engine; message queued');
    scheduleReconnect();
    return;
  }
  socket.send(JSON.stringify(message));
}

function enqueueOutgoing(message) {
  if (!isQueueSafe(message)) {
    return;
  }
  const key = message.requestId || `${message.type}-${Date.now()}`;
  outgoingQueue = outgoingQueue.filter((item) => item.key !== key);
  outgoingQueue.push({ key, message, queuedAt: new Date().toISOString() });
  if (outgoingQueue.length > OUTGOING_QUEUE_LIMIT) {
    outgoingQueue = outgoingQueue.slice(-OUTGOING_QUEUE_LIMIT);
  }
  persistRuntimeState();
}

function flushOutgoingQueue() {
  if (!socket || socket.readyState !== WebSocket.OPEN || outgoingQueue.length === 0) {
    return;
  }
  const queue = outgoingQueue;
  outgoingQueue = [];
  for (const item of queue) {
    socket.send(JSON.stringify(item.message));
  }
  persistRuntimeState();
}

function isQueueSafe(message) {
  const text = JSON.stringify(message || {});
  return !/(password|contrase|clave|token|otp|one-time-code)/i.test(text) || /extension\.hello|extension\.ping/.test(String(message && message.type || ''));
}

function startHeartbeat() {
  stopHeartbeat();
  heartbeatTimer = setInterval(sendPing, HEARTBEAT_MS);
  sendPing();
}

function stopHeartbeat() {
  if (heartbeatTimer) {
    clearInterval(heartbeatTimer);
    heartbeatTimer = null;
  }
}

function sendPing() {
  if (!socket || socket.readyState !== WebSocket.OPEN) {
    return;
  }
  if (lastPingSeq && missedPongs >= 2) {
    connectionState = 'degraded';
    publishState('reconnecting', 'Heartbeat degraded; reconnecting');
    closeSocketOnly('heartbeat missed');
    scheduleReconnect();
    return;
  }
  pingSeq += 1;
  lastPingSeq = pingSeq;
  missedPongs += 1;
  socket.send(JSON.stringify({ type: 'extension.ping', seq: pingSeq, clientId }));
  persistRuntimeState();
}

function publish(message) {
  for (const port of sidePorts) {
    port.postMessage(message);
  }
}

function publishState(status, message) {
  publish({ type: 'state', status, message, connected, stopped, currentRunId });
  publishRuntimeSnapshot();
}

async function publishPageSnapshot() {
  try {
    const tab = await resolveTargetTab();
    publish({
      type: 'page',
      page: {
        url: tab ? tab.url || '' : '',
        title: tab ? tab.title || '' : '',
        tabId: tab && tab.id ? String(tab.id) : '',
        restricted: tab ? restrictedUrl(tab.url || '') : true
      }
    });
  } catch {
    publish({ type: 'page', page: { restricted: true } });
  }
}

function allowedUrl(url) {
  try {
    const parsed = new URL(url);
    return parsed.protocol === 'http:' || parsed.protocol === 'https:';
  } catch {
    return false;
  }
}

function restrictedUrl(url) {
  return /^(chrome|edge|extension):\/\//i.test(url);
}

async function resolveTargetTab(options = {}) {
  if (targetTabId) {
    try {
      const tab = await chrome.tabs.get(targetTabId);
      if (tab && tab.id && !restrictedUrl(tab.url || '')) {
        return tab;
      }
    } catch {
      targetTabId = null;
    }
  }

  const candidates = await chrome.tabs.query({});
  const preferred = candidates.find((tab) => tab.active && !restrictedUrl(tab.url || '') && tab.id);
  if (preferred) {
    targetTabId = preferred.id;
    return preferred;
  }

  const anyAllowed = candidates.find((tab) => !restrictedUrl(tab.url || '') && tab.id);
  if (anyAllowed) {
    targetTabId = anyAllowed.id;
    return anyAllowed;
  }

  if (options.allowCreate) {
    return null;
  }

  return null;
}

function notifyStopToTargetTab() {
  if (!targetTabId) {
    return;
  }

  chrome.tabs.sendMessage(targetTabId, {
    type: 'local.stop',
    protocolVersion: PROTOCOL_VERSION
  }).catch(() => {});
}

async function startLearning(payload) {
  const tab = await resolveTargetTab({ allowCreate: false });
  learningSession = {
    recipeId: `recipe-${Date.now().toString(36)}`,
    name: payload.name || 'Nueva receta',
    description: payload.description || '',
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    startUrl: tab ? tab.url || '' : '',
    steps: [],
    parameters: [],
    sensitiveFields: [],
    humanCheckpoints: [],
    recording: true
  };
  await chrome.storage.local.set({ [LEARNING_DRAFT_KEY]: learningSession });
  await sendLearningModeToTab(true);
  publishLearningState();
}

async function stopLearning() {
  if (learningSession) {
    learningSession = {
      ...learningSession,
      recording: false,
      updatedAt: new Date().toISOString()
    };
    await chrome.storage.local.set({ [LEARNING_DRAFT_KEY]: learningSession });
  }
  await sendLearningModeToTab(false);
  publishLearningState();
}

async function clearLearningDraft() {
  learningSession = null;
  await chrome.storage.local.remove(LEARNING_DRAFT_KEY);
  await sendLearningModeToTab(false);
  publishLearningState();
}

async function handleLearningEvent(event, sender) {
  if (!learningSession || !learningSession.recording || !event) {
    return;
  }

  const step = sanitizeLearningStep(event, sender);
  learningSession.steps.push(step);
  learningSession.updatedAt = new Date().toISOString();

  if (step.target && Array.isArray(step.target.riskFlags)) {
    for (const risk of step.target.riskFlags) {
      if ((risk === 'password' || risk === 'credentialLike') && !learningSession.sensitiveFields.includes(step.target.accessibleName || step.target.visibleText || risk)) {
        learningSession.sensitiveFields.push(step.target.accessibleName || step.target.visibleText || risk);
      }
    }
  }

  if (step.actionType === 'pause' || step.valueRedacted) {
    learningSession.humanCheckpoints.push({
      timestamp: step.timestamp,
      reason: step.valueRedacted ? 'sensitiveField' : 'humanCheckpoint',
      url: step.url
    });
  }

  await chrome.storage.local.set({ [LEARNING_DRAFT_KEY]: learningSession });
  publishLearningState();
}

function sanitizeLearningStep(event, sender) {
  const target = event.target ? {
    elementId: event.target.elementId || '',
    tagName: event.target.tagName || '',
    role: event.target.role || '',
    elementKind: event.target.elementKind || '',
    type: event.target.type || '',
    visibleText: event.target.visibleText || '',
    accessibleName: event.target.accessibleName || '',
    nearbyText: event.target.nearbyText || '',
    formContext: event.target.formContext || '',
    bounds: event.target.bounds || null,
    stableSelectors: Array.isArray(event.target.stableSelectors) ? event.target.stableSelectors : [],
    bestSelector: event.target.bestSelector || null,
    riskFlags: Array.isArray(event.target.riskFlags) ? event.target.riskFlags : [],
    isPassword: Boolean(event.target.isPassword),
    isCredentialLike: Boolean(event.target.isCredentialLike)
  } : null;

  return {
    timestamp: event.timestamp || new Date().toISOString(),
    actionType: event.actionType || 'unknown',
    url: event.url || (sender.tab && sender.tab.url) || '',
    title: event.title || (sender.tab && sender.tab.title) || '',
    previousUrl: event.previousUrl || '',
    target,
    value: event.valueRedacted ? '' : event.value || '',
    valueRedacted: Boolean(event.valueRedacted),
    eventMeta: event.eventMeta || {},
    verificationHint: event.verificationHint || {}
  };
}

async function saveLearningRecipe(payload) {
  if (!learningSession) {
    publish({ type: 'recipeError', message: 'No learning draft available.' });
    return;
  }

  const recipe = recipeFromLearningDraft(learningSession, payload);
  await upsertRecipe(recipe);
  learningSession = { ...recipe, recording: false };
  await chrome.storage.local.set({ [LEARNING_DRAFT_KEY]: learningSession });
  publishLearningState();
  publishRecipes();
}

async function saveRecipe(recipe) {
  if (!recipe || !recipe.recipeId) {
    publish({ type: 'recipeError', message: 'Invalid recipe payload.' });
    return;
  }
  const normalized = createRecipeV1({ ...recipe, updatedAt: new Date().toISOString() });
  const validation = validateRecipeV1(normalized);
  if (!validation.ok) {
    publish({ type: 'recipeError', message: `Recipe invalid: ${validation.errors.join(', ')}` });
    return;
  }
  await upsertRecipe(normalized);
  publishRecipes();
}

async function upsertRecipe(recipe) {
  const recipes = await readRecipes();
  const index = recipes.findIndex((item) => item.recipeId === recipe.recipeId);
  if (index >= 0) {
    recipes[index] = recipe;
  } else {
    recipes.unshift(recipe);
  }
  await chrome.storage.local.set({ [RECIPES_KEY]: recipes });
}

async function deleteRecipe(recipeId) {
  const recipes = await readRecipes();
  await chrome.storage.local.set({ [RECIPES_KEY]: recipes.filter((recipe) => recipe.recipeId !== recipeId) });
  publishRecipes();
}

async function duplicateRecipe(recipeId) {
  const recipes = await readRecipes();
  const source = recipes.find((recipe) => recipe.recipeId === recipeId);
  if (!source) {
    publish({ type: 'recipeError', message: 'Recipe not found.' });
    return;
  }

  const copy = {
    ...createRecipeV1(source),
    recipeId: `recipe-${Date.now().toString(36)}`,
    name: `${source.name || 'Receta'} copia`,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString()
  };
  recipes.unshift(copy);
  await chrome.storage.local.set({ [RECIPES_KEY]: recipes });
  publishRecipes();
}

async function readRecipes() {
  const data = await chrome.storage.local.get({ [RECIPES_KEY]: [] });
  const recipes = Array.isArray(data[RECIPES_KEY]) ? data[RECIPES_KEY] : [];
  return recipes.map((recipe) => createRecipeV1(recipe));
}

async function publishRecipes() {
  publish({ type: 'recipes', recipes: await readRecipes() });
}

async function publishLearningState() {
  if (!learningSession) {
    const data = await chrome.storage.local.get({ [LEARNING_DRAFT_KEY]: null });
    learningSession = data[LEARNING_DRAFT_KEY] || null;
  }
  publish({ type: 'learningState', draft: learningSession });
}

function inferParameters(steps) {
  return recipeFromLearningDraft({ steps }, {}).parameters;
}

async function sendLearningModeToTab(enabled) {
  const tab = await resolveTargetTab({ allowCreate: false });
  if (!tab || !tab.id || restrictedUrl(tab.url || '')) {
    return;
  }

  await chrome.tabs.sendMessage(tab.id, {
    type: enabled ? 'learning.start' : 'learning.stop',
    protocolVersion: PROTOCOL_VERSION
  }).catch(() => {});
}

async function startRecipeRun(recipeId, parameters) {
  const recipes = await readRecipes();
  const recipe = recipes.find((item) => item.recipeId === recipeId);
  if (!recipe) {
    publish({ type: 'recipeError', message: 'Recipe not found.' });
    return;
  }

  const missing = validateRecipeParameters(recipe, parameters || {});
  if (missing.length > 0) {
    publish({ type: 'recipeRunParameterRequired', recipe, missing });
    return;
  }

  recipeRunner = {
    recipeRunId: `recipe-run-${Date.now().toString(36)}`,
    recipeId: recipe.recipeId,
    recipe,
    parameters: parameters || {},
    currentStepIndex: 0,
    currentStepId: '',
    currentStepLabel: '',
    status: 'running',
    startedAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    completedAt: '',
    lastError: '',
    lastSnapshot: null,
    lastCandidates: [],
    stepResults: recipe.steps.map((step) => ({
      stepId: step.stepId,
      type: step.type,
      status: 'pending',
      startedAt: '',
      completedAt: '',
      summary: '',
      toolRequest: null,
      toolResult: null,
      verificationResult: null,
      error: ''
    }))
  };

  publishRecipeRunState();
  await continueRecipeRun();
}

async function continueRecipeRun() {
  if (!recipeRunner || recipeRunner.status !== 'running') {
    return;
  }

  while (recipeRunner && recipeRunner.status === 'running' && recipeRunner.currentStepIndex < recipeRunner.recipe.steps.length) {
    const step = recipeRunner.recipe.steps[recipeRunner.currentStepIndex];
    recipeRunner.currentStepId = step.stepId;
    recipeRunner.currentStepLabel = step.label || step.type;
    const result = recipeRunner.stepResults[recipeRunner.currentStepIndex];
    result.status = 'running';
    result.startedAt = result.startedAt || new Date().toISOString();
    recipeRunner.updatedAt = new Date().toISOString();
    publishRecipeRunState();

    try {
      const execution = await executeRecipeStep(step);
      result.status = execution.paused ? 'paused' : 'passed';
      result.completedAt = new Date().toISOString();
      result.summary = execution.summary || `${step.type} passed`;
      result.toolRequest = execution.toolRequest || null;
      result.toolResult = execution.toolResult || null;
      result.verificationResult = execution.verificationResult || null;

      if (execution.paused) {
        recipeRunner.status = execution.waitingHuman ? 'waitingHuman' : 'paused';
        publish({ type: 'humanIntervention', reason: execution.reason || 'humanCheckpoint', message: execution.message || step.label });
        publishRecipeRunState();
        return;
      }

      recipeRunner.currentStepIndex += 1;
      recipeRunner.updatedAt = new Date().toISOString();
      publishRecipeRunState();
    } catch (error) {
      failRecipeRun(error, result);
      return;
    }
  }

  if (recipeRunner && recipeRunner.currentStepIndex >= recipeRunner.recipe.steps.length) {
    recipeRunner.status = 'completed';
    recipeRunner.completedAt = new Date().toISOString();
    recipeRunner.updatedAt = new Date().toISOString();
    publishRecipeRunState();
  }
}

async function executeRecipeStep(step) {
  switch (step.type) {
    case 'navigate': {
      const url = resolveTemplate(step.url || step.value || recipeRunner.recipe.startUrl, recipeRunner.parameters);
      if (!allowedUrl(url)) {
        throw new Error(`URL rejected: ${url}`);
      }
      await navigate(url);
      return { summary: `Navigated to ${url}`, toolRequest: { tool: 'navigate', args: { url } }, toolResult: { navigated: true, url } };
    }
    case 'observe': {
      const result = await executeTabTool('observePage', {});
      recipeRunner.lastSnapshot = result;
      return { summary: 'Observed page', toolRequest: { tool: 'observePage', args: {} }, toolResult: result };
    }
    case 'resolveTarget': {
      const args = resolveTargetArgsFromStep(step);
      const result = await executeTabTool('resolveTarget', args);
      recipeRunner.lastCandidates = result && Array.isArray(result.candidates) ? result.candidates : [];
      const score = result && result.bestCandidate ? Number(result.bestCandidate.score || 0) : 0;
      const threshold = Number(step.target && step.target.minScore || step.minScore || 0.35);
      if (!result || !result.bestCandidate || score < threshold) {
        throw new Error(`Target resolution failed: score ${score.toFixed(2)} below ${threshold}`);
      }
      step.__resolvedTarget = result.bestCandidate;
      return { summary: `Resolved target with score ${score.toFixed(2)}`, toolRequest: { tool: 'resolveTarget', args }, toolResult: result };
    }
    case 'click': {
      const target = await resolveTargetForAction(step, 'click');
      const args = {
        elementId: target.elementId || '',
        stableSelectors: target.element && target.element.stableSelectors ? target.element.stableSelectors : target.stableSelectors || [],
        verify: step.verify || { expectDomChange: true }
      };
      const result = await executeTabTool('clickElement', args);
      if (result && result.verificationStatus === 'failed') {
        throw new Error(result.reason || 'Click verification failed');
      }
      return { summary: `Clicked ${step.label || target.elementId}`, toolRequest: { tool: 'clickElement', args }, toolResult: result, verificationResult: result };
    }
    case 'input': {
      const target = await resolveTargetForAction(step, 'input');
      const value = resolveTemplate(String(step.value || ''), recipeRunner.parameters);
      const args = {
        elementId: target.elementId || '',
        stableSelectors: target.element && target.element.stableSelectors ? target.element.stableSelectors : target.stableSelectors || [],
        value
      };
      const result = await executeTabTool('setElementValue', args);
      if (!result || result.success === false) {
        throw new Error(result && result.reason ? result.reason : 'Input step failed');
      }
      return { summary: `Input ${step.label || target.elementId}`, toolRequest: { tool: 'setElementValue', args: redactToolArgs(step, args) }, toolResult: result, verificationResult: result };
    }
    case 'select': {
      const target = await resolveTargetForAction(step, 'select');
      const value = resolveTemplate(String(step.value || ''), recipeRunner.parameters);
      const args = {
        elementId: target.elementId || '',
        stableSelectors: target.element && target.element.stableSelectors ? target.element.stableSelectors : target.stableSelectors || [],
        value
      };
      const result = await executeTabTool('selectOption', args);
      return { summary: `Selected ${step.label || target.elementId}`, toolRequest: { tool: 'selectOption', args: redactToolArgs(step, args) }, toolResult: result, verificationResult: result };
    }
    case 'wait':
      await waitForRecipeCondition(step);
      return { summary: step.label || 'Wait completed' };
    case 'verify': {
      const result = await verifyRecipeCondition(step);
      if (!result.passed) {
        throw new Error(result.reason || 'Verify failed');
      }
      return { summary: result.reason || 'Verify passed', verificationResult: result };
    }
    case 'humanCheckpoint':
      return {
        paused: true,
        waitingHuman: true,
        reason: 'humanCheckpoint',
        message: step.label || step.reason || 'Intervencion humana requerida',
        summary: step.label || 'Human checkpoint'
      };
    default:
      throw new Error(`Unsupported recipe step: ${step.type}`);
  }
}

async function resolveTargetForAction(step, intent) {
  if (step.__resolvedTarget) {
    return step.__resolvedTarget;
  }
  if (step.target && step.target.elementId) {
    return { elementId: step.target.elementId, stableSelectors: step.target.stableSelectors || [] };
  }
  const args = resolveTargetArgsFromStep(step, intent);
  const result = await executeTabTool('resolveTarget', args);
  recipeRunner.lastCandidates = result && Array.isArray(result.candidates) ? result.candidates : [];
  const score = result && result.bestCandidate ? Number(result.bestCandidate.score || 0) : 0;
  const threshold = Number(step.target && step.target.minScore || 0.35);
  if (!result || !result.bestCandidate || score < threshold) {
    throw new Error(`Target not found for ${step.label || step.type}`);
  }
  return result.bestCandidate;
}

function resolveTargetArgsFromStep(step, forcedIntent) {
  const target = step.target || {};
  return {
    intent: forcedIntent || target.intent || step.type,
    targetText: target.semantic || target.observedText || step.label || '',
    context: target.nearbyText || target.formContext || step.label || '',
    elementKinds: target.role || [],
    maxCandidates: 5
  };
}

async function executeTabTool(tool, args) {
  const tab = await resolveTargetTab();
  if (!tab || !tab.id || restrictedUrl(tab.url || '')) {
    throw new Error('Restricted or unavailable tab');
  }
  const response = await chrome.tabs.sendMessage(tab.id, { type: 'tool.execute', protocolVersion: PROTOCOL_VERSION, tool, args: args || {} });
  if (!response || !response.success) {
    throw new Error(response && response.error ? response.error : `${tool} failed`);
  }
  return response.result;
}

async function waitForRecipeCondition(step) {
  const timeoutMs = Number(step.timeoutMs || 3000);
  const started = Date.now();
  while (Date.now() - started <= timeoutMs) {
    const result = await verifyRecipeCondition(step);
    if (result.passed) {
      return;
    }
    await delay(200);
  }
  throw new Error(step.label || 'Wait timeout');
}

async function verifyRecipeCondition(step) {
  const verify = step.verify || {};
  const observation = await executeTabTool('observePage', {});
  recipeRunner.lastSnapshot = observation;
  if (verify.expectTextAppears) {
    const passed = String(observation.visibleTextSummary || '').toLowerCase().includes(String(verify.expectTextAppears).toLowerCase());
    return { passed, reason: passed ? 'Expected text found' : 'Expected text not found', observation };
  }
  if (verify.expectUrlContains) {
    const passed = String(observation.url || '').includes(String(verify.expectUrlContains));
    return { passed, reason: passed ? 'URL condition met' : 'URL condition not met', observation };
  }
  return { passed: true, reason: 'No verify condition required', observation };
}

function validateRecipeParameters(recipe, parameters) {
  return (recipe.parameters || [])
    .filter((parameter) => parameter.required && (parameters[parameter.name] === undefined || parameters[parameter.name] === null || parameters[parameter.name] === ''))
    .map((parameter) => parameter.name);
}

function resolveTemplate(value, parameters) {
  return String(value || '').replace(/\{\{([a-zA-Z0-9_]+)\}\}/g, (_match, name) => parameters[name] ?? '');
}

function redactToolArgs(step, args) {
  const parameterName = String(step.value || '').match(/\{\{([a-zA-Z0-9_]+)\}\}/);
  const parameter = parameterName ? (recipeRunner.recipe.parameters || []).find((item) => item.name === parameterName[1]) : null;
  return { ...args, value: redactParameterValue(parameter, args.value) };
}

function failRecipeRun(error, stepResult) {
  if (!recipeRunner) {
    return;
  }
  const message = error && error.message ? error.message : String(error);
  stepResult.status = 'failed';
  stepResult.completedAt = new Date().toISOString();
  stepResult.error = message;
  stepResult.summary = message;
  recipeRunner.status = 'failed';
  recipeRunner.lastError = message;
  recipeRunner.updatedAt = new Date().toISOString();
  publishRecipeRunState();
}

function pauseRecipeRun(reason) {
  if (!recipeRunner) {
    return;
  }
  recipeRunner.status = 'paused';
  recipeRunner.updatedAt = new Date().toISOString();
  recipeRunner.lastError = reason || '';
  publishRecipeRunState();
}

async function resumeRecipeRun() {
  if (!recipeRunner) {
    return;
  }
  if (recipeRunner.status === 'waitingHuman') {
    recipeRunner.currentStepIndex += 1;
  }
  recipeRunner.status = 'running';
  recipeRunner.updatedAt = new Date().toISOString();
  publishRecipeRunState();
  await executeTabTool('observePage', {}).then((result) => {
    recipeRunner.lastSnapshot = result;
    publishRecipeRunState();
  }).catch(() => {});
  await continueRecipeRun();
}

async function retryRecipeStep() {
  if (!recipeRunner) {
    return;
  }
  const result = recipeRunner.stepResults[recipeRunner.currentStepIndex];
  if (result) {
    result.status = 'pending';
    result.error = '';
  }
  recipeRunner.status = 'running';
  publishRecipeRunState();
  await continueRecipeRun();
}

async function skipRecipeStep() {
  if (!recipeRunner) {
    return;
  }
  const step = recipeRunner.recipe.steps[recipeRunner.currentStepIndex];
  if (!step || !['observe', 'wait', 'verify'].includes(step.type)) {
    recipeRunner.lastError = 'Skip is only allowed for observe/wait/verify steps.';
    publishRecipeRunState();
    return;
  }
  const result = recipeRunner.stepResults[recipeRunner.currentStepIndex];
  if (result) {
    result.status = 'skipped';
    result.completedAt = new Date().toISOString();
    result.summary = 'Skipped by user';
  }
  recipeRunner.currentStepIndex += 1;
  recipeRunner.status = 'running';
  publishRecipeRunState();
  await continueRecipeRun();
}

function abortRecipeRun(reason) {
  if (!recipeRunner) {
    return;
  }
  recipeRunner.status = 'stopped';
  recipeRunner.completedAt = new Date().toISOString();
  recipeRunner.lastError = reason || '';
  publishRecipeRunState();
}

function publishRecipeRunState() {
  publish({ type: 'recipeRunState', run: recipeRunner });
  publishRuntimeSnapshot();
}

function delay(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

async function publishRuntimeSnapshot() {
  const config = await chrome.storage.local.get(DEFAULT_CONFIG);
  let tab = null;
  try {
    tab = await resolveTargetTab();
  } catch {
    tab = null;
  }
  publish({
    type: 'runtimeSnapshot',
    runtime: {
      connection: {
        connected,
        state: connectionState,
        stopped,
        clientId,
        lastConnectedAt,
        lastSeenAt,
        lastError: lastConnectionError,
        queuedMessages: outgoingQueue.length,
        host: config.host || DEFAULT_CONFIG.host,
        port: config.port || DEFAULT_CONFIG.port,
        health: lastHealth,
        debug: runtimeDebug
      },
      ai: {
        provider: lastHealth && lastHealth.body && lastHealth.body.publicConfig ? lastHealth.body.publicConfig.aiProvider || 'OpenAI' : 'OpenAI',
        model: lastHealth && lastHealth.body && lastHealth.body.publicConfig ? lastHealth.body.publicConfig.model || '-' : '-',
        hasApiKeyLocal: lastHealth && lastHealth.body && lastHealth.body.publicConfig ? Boolean(lastHealth.body.publicConfig.hasApiKey) : null,
        lastError: lastAiError
      },
      extension: {
        webSocketConnected: connected,
        protocolVersion: PROTOCOL_VERSION,
        clientId,
        lastSeenAt,
        tabId: tab && tab.id ? String(tab.id) : '',
        url: tab ? tab.url || '' : '',
        contentScriptActive: tab ? !restrictedUrl(tab.url || '') : false,
        permissions: ['activeTab', 'scripting', 'storage', 'tabs', 'sidePanel']
      },
      run: {
        runId: currentRunId,
        requestId: currentRequestId,
        status: stopped ? 'stopped' : connected ? 'connected' : 'idle',
        currentTool,
        lastToolResult,
        lastToolRequest,
        lastRunStatus,
        lastError: lastToolResult && !lastToolResult.success ? lastToolResult.error : '',
        recipeRun: recipeRunner
      }
    }
  });
}
