const PROTOCOL_VERSION = 'chrome-lab-v1';
importScripts('recipe_core.js');

const DEFAULT_CONFIG = { host: '127.0.0.1', port: '8787', token: '' };
const RECIPES_KEY = 'nexaRecipes';
const LEARNING_DRAFT_KEY = 'nexaLearningDraft';
const SESSION_STATE_KEY = 'nexaRuntimeState';
const OUTGOING_QUEUE_LIMIT = 40;
const HEARTBEAT_MS = 15000;
const BACKOFF_STEPS = [250, 500, 1000, 2000, 5000];
const EXTENSION_RUNTIME_MODE = 'core-governed-companion';
const CORE_GOVERNED_MODE = true;
const LEGACY_RUNNER_ENABLED = false;
const EXTENSION_CAPABILITIES = Object.freeze({
  companionMode: true,
  relayMode: true,
  coreGovernedMode: CORE_GOVERNED_MODE,
  legacyRunnerEnabled: LEGACY_RUNNER_ENABLED,
  serviceWorkerRunOwner: false,
  canVerifyFinalSuccess: false,
  contentScriptAuthoritative: false
});

let socket = null;
let connected = false;
let connectingPromise = null;
let reconnectTimer = null;
let heartbeatTimer = null;
let reconnectAttempt = 0;
let reconnectBlocked = false;
let reconnectBlockedReason = '';
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
let runState = 'idle';
let stopRequestedExplicitly = false;
let connectionStoppedByUser = false;
let currentRunId = '';
let targetTabId = null;
let targetTabPinnedExplicitly = false;
let lastTargetTabSnapshot = null;
let currentRequestId = '';
let currentTool = '';
let lastToolRequest = null;
let lastToolResult = null;
let lastRunStatus = null;
let lastHealth = null;
let lastAiError = '';
let lastProtocolError = '';
let lastWsCloseCode = 0;
let lastWsCloseReason = '';
let lastWsCloseWasClean = false;
let lastWsCloseAt = '';
let lastStateTransition = null;
let lastContentScriptStatus = 'unknown';
let lastContentScriptMessage = '';
let lastContentScriptInjectedAt = '';
let lastContentScriptLastSeenAt = '';
let lastContentScriptLastUrl = '';
let lastContentScriptLastError = '';
let lastSendMessageError = '';
let lastObserveRequestAt = '';
let lastObserveResultAt = '';
let lastObserveError = '';
let lastResolvedTabAllowed = false;
let lastObservedPage = null;
let lastHandoffFingerprint = '';
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
chrome.tabs.onUpdated.addListener((tabId, changeInfo, tab) => {
  if (changeInfo.status === 'complete') {
    publishPageSnapshot();
  }
  handleLearningTabUpdated(tabId, changeInfo, tab).catch(() => {});
});

function shouldPreferActiveTab() {
  const recipeStatus = recipeRunner && recipeRunner.status ? recipeRunner.status : '';
  const learningStatus = learningSession && learningSession.learningState ? learningSession.learningState : '';
  if (currentRunId) {
    return false;
  }
  if (recipeStatus && !['completed', 'stopped', 'failed'].includes(recipeStatus)) {
    return false;
  }
  if (learningStatus && ['recording', 'paused'].includes(learningStatus)) {
    return false;
  }
  return true;
}

async function initializeRuntimeLifecycle() {
  await hydrateRuntimeState();
  await hydrateLearningSession();
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
  runState = saved.runState || runState;
  reconnectBlocked = Boolean(saved.reconnectBlocked);
  reconnectBlockedReason = saved.reconnectBlockedReason || '';
  stopRequestedExplicitly = Boolean(saved.stopRequestedExplicitly);
  connectionStoppedByUser = Boolean(saved.connectionStoppedByUser);
  lastConnectedAt = saved.lastConnectedAt || '';
  lastSeenAt = saved.lastSeenAt || '';
  lastProtocolError = saved.lastProtocolError || '';
  lastWsCloseCode = Number(saved.lastWsCloseCode || 0);
  lastWsCloseReason = saved.lastWsCloseReason || '';
  lastWsCloseWasClean = Boolean(saved.lastWsCloseWasClean);
  lastWsCloseAt = saved.lastWsCloseAt || '';
  lastStateTransition = saved.lastStateTransition || null;
  lastContentScriptStatus = saved.lastContentScriptStatus || 'unknown';
  lastContentScriptMessage = saved.lastContentScriptMessage || '';
  lastContentScriptInjectedAt = saved.lastContentScriptInjectedAt || '';
  lastContentScriptLastSeenAt = saved.lastContentScriptLastSeenAt || '';
  lastContentScriptLastUrl = saved.lastContentScriptLastUrl || '';
  lastContentScriptLastError = saved.lastContentScriptLastError || '';
  lastSendMessageError = saved.lastSendMessageError || '';
  lastObserveRequestAt = saved.lastObserveRequestAt || '';
  lastObserveResultAt = saved.lastObserveResultAt || '';
  lastObserveError = saved.lastObserveError || '';
  lastResolvedTabAllowed = Boolean(saved.lastResolvedTabAllowed);
  recipeRunner = LEGACY_RUNNER_ENABLED ? (saved.recipeRunner || recipeRunner) : null;
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
      runState,
      reconnectBlocked,
      reconnectBlockedReason,
      stopRequestedExplicitly,
      connectionStoppedByUser,
      lastConnectedAt,
      lastSeenAt,
      lastProtocolError,
      lastWsCloseCode,
      lastWsCloseReason,
      lastWsCloseWasClean,
      lastWsCloseAt,
      lastStateTransition,
      lastContentScriptStatus,
      lastContentScriptMessage,
      lastContentScriptInjectedAt,
      lastContentScriptLastSeenAt,
      lastContentScriptLastUrl,
      lastContentScriptLastError,
      lastSendMessageError,
      lastObserveRequestAt,
      lastObserveResultAt,
      lastObserveError,
      lastResolvedTabAllowed,
      recipeRunner: LEGACY_RUNNER_ENABLED ? recipeRunner : null,
      pendingOutgoingMessages: outgoingQueue.slice(-OUTGOING_QUEUE_LIMIT)
    }
  }).catch(() => {});
}

async function hydrateLearningSession() {
  const data = await chrome.storage.local.get({ [LEARNING_DRAFT_KEY]: null });
  learningSession = data[LEARNING_DRAFT_KEY] || null;
  if (learningSession && learningSession.learningState !== 'recording' && learningSession.learningState !== 'paused') {
    learningSession = null;
    await chrome.storage.local.remove(LEARNING_DRAFT_KEY);
  }
}

async function keepAliveTick() {
  const config = await chrome.storage.local.get(DEFAULT_CONFIG);
  if (!socket || socket.readyState !== WebSocket.OPEN) {
    scheduleReconnect('keepalive');
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
        setConnectionState('error', String(error && error.message ? error.message : error), { source: 'service_worker', cause: 'connect.exception' });
      }
      break;
    case 'saveTokenAndConnect':
      await saveTokenAndConnect(message.config || {});
      break;
    case 'clearSavedToken':
      await clearSavedToken();
      break;
    case 'clearLocalRuntimeState':
      await clearLocalRuntimeState();
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
    case 'prepareCurrentTab':
      await prepareCurrentTab();
      break;
    case 'startRun':
      startRun(message.instruction);
      break;
    case 'pause':
      setRunState('paused', 'Pause requested', { source: 'side_panel', cause: 'pause.click' });
      runCommand('pause');
      break;
    case 'resume':
      stopRequestedExplicitly = false;
      setRunState('running', 'Resume requested', { source: 'side_panel', cause: 'resume.click' });
      runCommand('resume');
      break;
    case 'stop':
      stopAll('userStop');
      break;
    case 'resumeHuman':
      stopRequestedExplicitly = false;
      setRunState('running', 'Resume requested', { source: 'side_panel', cause: 'resumeHuman.click' });
      runCommand('resume');
      break;
    case 'handoffContinue':
      await handleHandoffContinue();
      break;
    case 'handoff.userCompleted':
    case 'handoff.cancelled':
      handleCompanionHandoffEvent(message);
      break;
    case 'vaultConsent.userApproved':
    case 'vaultConsent.userDenied':
    case 'vaultConsent.cancelled':
    case 'profileConsent.userApproved':
    case 'profileConsent.userDenied':
    case 'profileConsent.cancelled':
      handleCompanionConsentEvent(message);
      break;
    case 'observeCurrentPage':
      await observeCurrentPage('manualObserve');
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
    case 'learningPause':
      pauseLearning();
      break;
    case 'learningResume':
      resumeLearning();
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
      setConnectionState('error', `Unknown panel message: ${message.type}`, { source: 'side_panel', cause: 'message.unknown' });
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

async function saveTokenAndConnect(config) {
  const normalized = {
    host: config.host || DEFAULT_CONFIG.host,
    port: config.port || DEFAULT_CONFIG.port,
    token: String(config.token || '').trim()
  };
  await saveConfig(normalized);
  await connect(normalized);
  await refreshDebug(normalized, { quiet: true });
  publishSavedConfig();
}

async function clearSavedToken() {
  reconnectBlocked = true;
  reconnectBlockedReason = 'Saved token cleared';
  await chrome.storage.local.set({ token: '' });
  disconnect('tokenCleared');
  publishSavedConfig();
  publishRuntimeSnapshot();
}

async function clearLocalRuntimeState() {
  clearReconnectTimer();
  outgoingQueue = [];
  currentRunId = '';
  currentRequestId = '';
  currentTool = '';
  lastToolRequest = null;
  lastToolResult = null;
  lastRunStatus = null;
  lastAiError = '';
  lastConnectionError = '';
  lastProtocolError = '';
  lastWsCloseCode = 0;
  lastWsCloseReason = '';
  lastWsCloseWasClean = false;
  lastWsCloseAt = '';
  lastStateTransition = null;
  targetTabId = null;
  targetTabPinnedExplicitly = false;
  lastTargetTabSnapshot = null;
  lastObservedPage = null;
  lastHandoffFingerprint = '';
  stopRequestedExplicitly = false;
  connectionStoppedByUser = false;
  runState = 'idle';
  if (!connected) {
    connectionState = 'disconnected';
    reconnectBlocked = false;
    reconnectBlockedReason = '';
  }
  await persistRuntimeState();
  setConnectionState(connected ? 'connected' : 'disconnected', 'Local runtime state cleared', { source: 'side_panel', cause: 'runtime.clear' });
  publishRuntimeSnapshot();
}

function connect(config) {
  reconnectBlocked = false;
  reconnectBlockedReason = '';
  lastConnectionError = '';
  lastProtocolError = '';
  connectionStoppedByUser = false;
  return connectWebSocket(config, { manual: true });
}

async function connectWebSocket(config, options = {}) {
  config = config || await chrome.storage.local.get(DEFAULT_CONFIG);
  if (!clientId) {
    await hydrateRuntimeState();
  }
  if (options.manual) {
    reconnectBlocked = false;
    reconnectBlockedReason = '';
    lastConnectionError = '';
  }
  if (reconnectBlocked && !options.manual) {
    const blockedReason = reconnectBlockedReason || lastConnectionError || 'Reconnect blocked until configuration changes.';
    setConnectionState(connectionState === 'tokenError' ? 'tokenError' : 'error', blockedReason, { source: 'service_worker', cause: 'connect.blocked' });
    throw new Error(blockedReason);
  }
  if (socket && socket.readyState === WebSocket.OPEN && connected) {
    return Promise.resolve();
  }

  if (connectingPromise) {
    return connectingPromise;
  }

  clearReconnectTimer();
  closeSocketOnly('reconnect');
  connectionStoppedByUser = false;
  stopRequestedExplicitly = false;
  setConnectionState('connecting', 'Connecting to bridge', { source: 'service_worker', cause: options.reason || 'connect.requested' });
  await validateConnectionConfig(config);
  const url = `ws://${config.host || DEFAULT_CONFIG.host}:${config.port || DEFAULT_CONFIG.port}/ws/extension`;

  connectingPromise = new Promise((resolve, reject) => {
    socket = new WebSocket(url);
    socket.addEventListener('open', () => {
      connected = true;
      connectingPromise = null;
      reconnectAttempt = 0;
      missedPongs = 0;
      lastConnectedAt = new Date().toISOString();
      lastSeenAt = lastConnectedAt;
      setConnectionState('connected', `Connected to ${url}`, { source: 'service_worker', cause: 'socket.open' });
      sendToEngine({
        type: 'extension.hello',
        protocolVersion: PROTOCOL_VERSION,
        clientId,
        extensionVersion: chrome.runtime.getManifest().version,
        browser: 'Chrome',
        runtimeKind: EXTENSION_RUNTIME_MODE,
        capabilities: EXTENSION_CAPABILITIES,
        token: config.token || '',
        resumeRunId: currentRunId || null
      });
      flushOutgoingQueue();
      startHeartbeat();
      persistRuntimeState();
      resolve();
    });
    socket.addEventListener('message', (event) => handleEngineMessage(event.data));
    socket.addEventListener('close', (event) => {
      connected = false;
      connectingPromise = null;
      stopHeartbeat();
      lastWsCloseCode = Number(event.code || 0);
      lastWsCloseReason = String(event.reason || '');
      lastWsCloseWasClean = Boolean(event.wasClean);
      lastWsCloseAt = new Date().toISOString();
      if (reconnectBlocked) {
        const blockedState = connectionState === 'tokenError' || connectionState === 'protocolError' ? connectionState : 'error';
        setConnectionState(blockedState, reconnectBlockedReason || lastConnectionError || 'Bridge connection blocked', {
          source: 'service_worker',
          cause: 'socket.close.blocked'
        });
        persistRuntimeState();
        resolve();
        return;
      }
      const nextState = connectionStoppedByUser ? 'disconnected' : 'reconnecting';
      setConnectionState(nextState, connectionStoppedByUser ? 'Bridge disconnected by user' : 'Bridge connection closed', {
        source: 'service_worker',
        cause: connectionStoppedByUser ? 'socket.close.user' : 'socket.close.unexpected'
      });
      persistRuntimeState();
      if (!connectionStoppedByUser) {
        scheduleReconnect('socketClose');
      }
      resolve();
    });
    socket.addEventListener('error', () => {
      connected = false;
      connectingPromise = null;
      stopHeartbeat();
      if (reconnectBlocked) {
        setConnectionState(connectionState === 'tokenError' ? 'tokenError' : 'error', reconnectBlockedReason || lastConnectionError || 'Bridge connection blocked', {
          source: 'service_worker',
          cause: 'socket.error.blocked'
        });
        persistRuntimeState();
        reject(new Error(reconnectBlockedReason || lastConnectionError || 'Bridge connection blocked'));
        return;
      }
      lastConnectionError = 'Bridge WebSocket error';
      setConnectionState('error', 'Bridge WebSocket error', { source: 'service_worker', cause: 'socket.error' });
      persistRuntimeState();
      scheduleReconnect('socketError');
      reject(new Error('Bridge WebSocket error'));
    });
  });

  return connectingPromise;
}

function disconnect(reason) {
  connectionStoppedByUser = true;
  reconnectBlocked = true;
  reconnectBlockedReason = reason || 'Disconnected by user';
  clearReconnectTimer();
  closeSocketOnly(reason);
  socket = null;
  connected = false;
  connectingPromise = null;
  stopHeartbeat();
  setConnectionState('disconnected', reason || 'Disconnected by user', { source: 'side_panel', cause: reason || 'disconnect.click' });
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

function scheduleReconnect(reason) {
  if (connectionStoppedByUser || reconnectTimer || reconnectBlocked) {
    return;
  }
  reconnectAttempt += 1;
  const baseDelay = BACKOFF_STEPS[Math.min(reconnectAttempt - 1, BACKOFF_STEPS.length - 1)];
  const jitter = Math.floor(Math.random() * 120);
  reconnectTimer = setTimeout(async () => {
    reconnectTimer = null;
    const config = await chrome.storage.local.get(DEFAULT_CONFIG);
    connectWebSocket(config, { reason: reason || 'scheduledReconnect' }).catch(() => {});
  }, baseDelay + jitter);
}

async function validateConnectionConfig(config) {
  const host = config.host || DEFAULT_CONFIG.host;
  const port = config.port || DEFAULT_CONFIG.port;
  const token = String(config.token || '').trim();
  try {
    const response = await fetch(`http://${host}:${port}/config/public`, { cache: 'no-store' });
    if (!response.ok) {
      return;
    }
    const publicConfig = await response.json();
    if (publicConfig && publicConfig.requiresToken && !token) {
      const paired = await tryLocalPairing(config);
      if (paired) {
        return;
      }
      blockReconnect('tokenError', 'Bridge requires a connection token. Enter the token and press Reconnect.');
      throw new Error(lastConnectionError);
    }
  } catch (error) {
    if (reconnectBlocked) {
      throw error;
    }
  }
}

function blockReconnect(state, message) {
  reconnectBlocked = true;
  reconnectBlockedReason = message || 'Reconnect blocked';
  connected = false;
  connectingPromise = null;
  connectionState = state || 'error';
  lastConnectionError = reconnectBlockedReason;
  lastProtocolError = reconnectBlockedReason;
  clearReconnectTimer();
  stopHeartbeat();
  persistRuntimeState();
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
    const target = await getTargetTabForRun(instruction || '');
    if (!target || !target.id) {
      setRunState('error', 'no_target_tab_selected', { source: 'service_worker', cause: 'run.target.missing' });
      publish({
        type: 'runStarted',
        ok: false,
        body: {
          ok: false,
          error: 'no_target_tab_selected',
          message: 'Abrí o prepará una pestaña web objetivo para que NODAL OS pueda operar. No voy a usar ChatGPT como target implícito.'
        }
      });
      publishRuntimeSnapshot();
      return;
    }
    targetTabId = target.id;
    targetTabPinnedExplicitly = true;
    rememberTargetTab(target, 'run.start');
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
      setRunState('error', body.message || body.error || 'Run rejected', { source: 'bridge', cause: 'run.start.rejected' });
      return;
    }
    stopRequestedExplicitly = false;
    setRunState(body.status === 'error' ? 'error' : 'running', body.message || 'Run started', { source: 'bridge', cause: 'run.start.accepted' });
  } catch (error) {
    setRunState('error', String(error && error.message ? error.message : error), { source: 'service_worker', cause: 'run.start.exception' });
  }
}

async function runCommand(command) {
  if (!currentRunId) {
    setRunState('error', 'No active run', { source: 'service_worker', cause: `run.${command}.missing` });
    return;
  }
  const config = await chrome.storage.local.get(DEFAULT_CONFIG);
  const url = `http://${config.host}:${config.port}/api/runs/${currentRunId}/${command}`;
  try {
    const response = await fetch(url, { method: 'POST' });
    const body = await response.json();
    publish({ type: 'runCommand', command, ok: response.ok, body });
  } catch (error) {
    setRunState('error', String(error && error.message ? error.message : error), { source: 'service_worker', cause: `run.${command}.exception` });
  }
}

function stopAll(reason) {
  stopRequestedExplicitly = true;
  setRunState('stopped', reason || 'userStop', { source: 'side_panel', cause: reason || 'stop.click', stopExplicit: true });
  abortRecipeRun(reason || 'userStop');
  notifyStopToTargetTab();
  if (currentRunId) {
    runCommand('stop');
  }
  sendToEngine({ type: 'run.stop', runId: currentRunId, reason });
}

function applyRemoteRunStop(reason) {
  stopRequestedExplicitly = reason === 'userStop' || reason === 'agentStop' || reason === 'engineStop';
  setRunState('stopped', reason || 'engineStop', { source: 'bridge', cause: 'run.stop', stopExplicit: stopRequestedExplicitly });
  abortRecipeRun(reason || 'engineStop');
  notifyStopToTargetTab();
  currentRunId = '';
  currentRequestId = '';
  currentTool = '';
}

function handleEngineMessage(raw) {
  let message;
  try {
    message = JSON.parse(raw);
  } catch {
    setConnectionState(connectionState === 'connected' ? 'connected' : 'error', 'Invalid JSON from engine', {
      source: 'bridge',
      cause: 'protocol.invalidJson'
    });
    return;
  }

  publish({ type: 'engineMessage', message });
  lastSeenAt = new Date().toISOString();
  persistRuntimeState();
  if (message.type && /^handoff\./.test(message.type)) {
    publish(message);
    publishRuntimeSnapshot();
    return;
  }
  if (message.type === 'engine.hello') {
    setConnectionState('connected', 'Engine hello received', { source: 'bridge', cause: 'engine.hello' });
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
    lastProtocolError = lastConnectionError;
    connectionState = message.error === 'invalid_token' ? 'tokenError' : 'protocolError';
    if (message.error === 'invalid_token' || message.error === 'protocol_version_mismatch') {
      blockReconnect(connectionState, lastConnectionError);
      closeSocketOnly('protocol rejected');
      if (message.error === 'invalid_token') {
        repairLocalTokenAfterAuthError().catch(() => {});
      }
    }
    setConnectionState(connectionState, lastConnectionError, { source: 'bridge', cause: `protocol.${message.error || 'error'}` });
    publishRuntimeSnapshot();
    return;
  }

  if (message.type === 'run.pause') {
    publishHumanIntervention(createHumanHandoffPayload(message.reason, message.message, lastObservedPage));
    setRunState('paused', message.message || message.reason, { source: 'bridge', cause: 'run.pause' });
    return;
  }

  if (message.type === 'run.resume') {
    stopRequestedExplicitly = false;
    setRunState('running', 'Run resumed', { source: 'bridge', cause: 'run.resume' });
    return;
  }

  if (message.type === 'run.status') {
    const normalizedStatus = normalizeCoreRunStatus(message);
    lastRunStatus = normalizedStatus;
    if (normalizedStatus.aiError || normalizedStatus.provider === 'openai') {
      lastAiError = normalizedStatus.message || normalizedStatus.aiError || '';
    } else if (normalizedStatus.status === 'error') {
      lastAiError = normalizedStatus.message || '';
    }
    publish({ type: 'runStatus', message: normalizedStatus });
    setRunState(normalizedStatus.status || 'running', normalizedStatus.message || '', { source: 'bridge', cause: 'run.status' });
    if (normalizedStatus.status === 'completed') {
      currentRunId = '';
      currentRequestId = '';
      currentTool = '';
      stopRequestedExplicitly = false;
    }
    publishRuntimeSnapshot();
    return;
  }

  if (message.type === 'run.stop') {
    applyRemoteRunStop(message.reason || 'engineStop');
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

async function repairLocalTokenAfterAuthError() {
  const config = await chrome.storage.local.get(DEFAULT_CONFIG);
  const paired = await tryLocalPairing(config);
  if (!paired) {
    return;
  }
  reconnectBlocked = false;
  reconnectBlockedReason = '';
  lastConnectionError = '';
  lastProtocolError = '';
  await connectWebSocket({ ...config, token: paired }, { manual: true });
}

async function tryLocalPairing(config) {
  const host = config.host || DEFAULT_CONFIG.host;
  const port = config.port || DEFAULT_CONFIG.port;
  if (!isLoopbackHost(host)) {
    return '';
  }
  try {
    const response = await fetch(`http://${host}:${port}/pairing/local-token`, { cache: 'no-store' });
    if (!response.ok) {
      return '';
    }
    const body = await response.json();
    const token = String(body && body.token || '').trim();
    if (!token) {
      return '';
    }
    await chrome.storage.local.set({ host, port, token });
    stateTokenLoadedForRuntime(host, port, token);
    return token;
  } catch {
    return '';
  }
}

function isLoopbackHost(host) {
  return host === '127.0.0.1' || host === 'localhost' || host === '::1';
}

function stateTokenLoadedForRuntime(host, port, token) {
  publish({ type: 'config', config: { host, port, token } });
}

async function routeToolRequest(message) {
  if (runState === 'stopped') {
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
      const navigationResult = await navigate(message.args.url);
      sendToolResult(message, true, navigationResult || { navigated: true }, null);
      return;
    }

    const tab = await resolveTargetTab();
    if (!tab || !tab.id || restrictedUrl(tab.url || '')) {
      sendToolResult(message, false, null, 'Restricted or unavailable tab');
      return;
    }

    const response = await sendTabMessageWithRecovery(tab, {
      type: 'tool.execute',
      protocolVersion: PROTOCOL_VERSION,
      runId: message.runId || currentRunId || '',
      requestId: message.requestId || currentRequestId || '',
      tool: message.tool,
      args: message.args || {}
    });

    if (response && response.success && response.result) {
      response.result.targetTabId = tab.id;
      response.result.targetUrl = tab.url || response.result.url || '';
      response.result.targetTitle = tab.title || response.result.title || '';
    }

    if (message.tool === 'observePage' && response && response.success && response.result) {
      lastObservedPage = response.result;
    }

    if (response && response.success && response.result && hasStrongCredentialSignal(response.result)) {
      publishHumanIntervention(createHumanHandoffPayload('credentialRequired', 'NODAL OS llegó a una pantalla de acceso o validación humana.', response.result));
    }
    sendToolResult(message, Boolean(response && response.success), response ? response.result : null, response ? response.error : 'No response', response ? response.errorCode : '');
  } catch (error) {
    const errorText = String(error && error.message ? error.message : error);
    if (/Sensitive submit blocked|credential|captcha|2fa|password/i.test(errorText)) {
      publishHumanIntervention(createHumanHandoffPayload('credentialRequired', 'NODAL OS se detuvo antes de una acción sensible y necesita que la resuelvas manualmente.', lastObservedPage));
      setRunState('paused', 'Intervencion humana requerida', { source: 'service_worker', cause: 'tool.sensitive' });
    }
    sendToolResult(message, false, null, errorText, error && error.errorCode ? error.errorCode : '');
  }
}

function validateToolMessage(message) {
  const allowed = new Set([
    'observePage',
    'pingContentScript',
    'getElementCatalog',
    'resolveTarget',
    'getCurrentTab',
    'navigate',
    'query',
    'read',
    'readElement',
    'click',
    'clickElement',
    'submitElement',
    'setValue',
    'setElementValue',
    'focusElement',
    'selectOption',
    'scrollIntoView',
    'scrollElementIntoView',
    'waitForSelector',
    'highlight',
    'highlightElement',
    'sortResults',
    'openCheapestResult',
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

  const tab = await resolveTargetTab({ allowCreate: false, preferActive: false });
  if (tab && tab.id) {
    targetTabId = tab.id;
    rememberTargetTab(tab, 'navigate');
    await chrome.tabs.update(tab.id, { url, active: true });
    await waitForTabComplete(tab.id, 15000);
    const updated = await chrome.tabs.get(tab.id).catch(() => null);
    if (updated) {
      rememberTargetTab(updated, 'navigate.complete');
    }
    return {
      navigated: true,
      tabId: String(tab.id),
      url,
      targetUrl: updated && updated.url ? updated.url : url,
      openedNewTab: false
    };
  }

  throw new Error('no_target_tab_selected');
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

function sendToolResult(request, success, result, error, errorCode = '') {
  const relayResult = {
    type: 'tool.result',
    runId: request.runId,
    requestId: request.requestId,
    actionId: request.actionId || request.requestId || '',
    correlationId: request.correlationId || request.requestId || request.runId || '',
    runtimeKind: EXTENSION_RUNTIME_MODE,
    source: 'extension-relay',
    authoritative: false,
    verificationStatus: 'NotVerified',
    evidenceRefs: [],
    redacted: true,
    success,
    result,
    error,
    errorCode
  };
  lastToolResult = { request, ...relayResult, at: new Date().toISOString() };
  sendToEngine(relayResult);
  publish({ type: 'toolResult', request, ...relayResult });
  publishRuntimeSnapshot();
}

function normalizeCoreRunStatus(message) {
  const normalized = {
    ...message,
    runtimeKind: message.runtimeKind || 'core-browser-executor',
    source: message.source || 'core',
    coreGoverned: true,
    redacted: message.redacted !== false
  };
  if ((message.status || '').toLowerCase() !== 'completed') {
    return normalized;
  }
  if (isVerificationStronglyVerified(message)) {
    normalized.verificationStatus = message.verificationStatus || 'Verified';
    return normalized;
  }
  return {
    ...normalized,
    status: 'uncertain',
    verificationStatus: message.verificationStatus || 'Uncertain',
    errorCode: message.errorCode || 'verification_required',
    message: message.message || 'Core-governed mode requires Verified status before completion.'
  };
}

function isVerificationStronglyVerified(message) {
  const status = String(
    message.verificationStatus ||
    (message.verification && message.verification.status) ||
    (message.result && message.result.verificationStatus) ||
    ''
  ).toLowerCase();
  return status === 'verified';
}

function handleCompanionHandoffEvent(message) {
  const event = normalizeCompanionHandoffEvent(message);
  sendToEngine(event);
  publish(event);
  publishRuntimeSnapshot();
}

function handleCompanionConsentEvent(message) {
  const event = normalizeCompanionConsentEvent(message);
  sendToEngine(event);
  publish(event);
  publishRuntimeSnapshot();
}

function normalizeCompanionHandoffEvent(message) {
  const type = message && message.type === 'handoff.cancelled' ? 'handoff.cancelled' : 'handoff.userCompleted';
  return {
    type,
    handoffId: redactCompanionText(message && message.handoffId),
    runId: redactCompanionText(message && message.runId ? message.runId : currentRunId),
    actionId: redactCompanionText(message && message.actionId),
    correlationId: redactCompanionText(message && message.correlationId),
    runtimeKind: EXTENSION_RUNTIME_MODE,
    source: 'chrome-companion',
    authoritative: false,
    verificationStatus: 'NotVerified',
    evidenceRefs: [],
    proofRefs: [],
    redacted: true,
    diagnostics: redactCompanionText(message && message.diagnostics ? JSON.stringify(message.diagnostics) : '')
  };
}

function normalizeCompanionConsentEvent(message) {
  const rawType = String(message && message.type || '');
  const prefix = rawType.startsWith('profileConsent.') ? 'profileConsent' : 'vaultConsent';
  const suffix = rawType.endsWith('.userDenied') ? 'userDenied' : rawType.endsWith('.cancelled') ? 'cancelled' : 'userApproved';
  return {
    type: `${prefix}.${suffix}`,
    consentId: redactCompanionText(message && message.consentId),
    runId: redactCompanionText(message && message.runId ? message.runId : currentRunId),
    actionId: redactCompanionText(message && message.actionId),
    correlationId: redactCompanionText(message && message.correlationId),
    consentType: redactCompanionText(message && message.consentType),
    scope: redactCompanionText(message && message.scope),
    runtimeKind: EXTENSION_RUNTIME_MODE,
    source: 'chrome-companion',
    authoritative: false,
    verificationStatus: 'NotVerified',
    evidenceRefs: [],
    proofRefs: [],
    redacted: true,
    diagnostics: redactCompanionText(message && message.diagnostics ? JSON.stringify(message.diagnostics) : '')
  };
}

function redactCompanionText(value) {
  if (value === null || value === undefined) {
    return '';
  }
  return String(value)
    .replace(/s[k]-[A-Za-z0-9_-]{8,}/gi, '[redacted]')
    .replace(/authorization\s*[:=]\s*bearer\s+[A-Za-z0-9._-]+/gi, 'authorization=[redacted]')
    .replace(/bearer\s+[A-Za-z0-9._-]+/gi, 'bearer [redacted]')
    .replace(/(password|passwd|secret|token|access_token|refresh_token|id_token|api[_-]?key|cookie|set-cookie|authorization|otp|code|clave(?:\s+fiscal)?|sessionid|csrf|xsrf|jwt|client_secret)\s*[:=]\s*[^;\s,}]+/gi, '$1=[redacted]')
    .replace(/\b(CUIT|DNI)\s*[:=]\s*\d{7,11}\b/gi, '$1=[redacted]');
}

function sendToEngine(message) {
  if (!socket || socket.readyState !== WebSocket.OPEN) {
    if (reconnectBlocked) {
      setConnectionState(connectionState === 'tokenError' ? 'tokenError' : 'error', reconnectBlockedReason || lastConnectionError || 'Bridge connection blocked', {
        source: 'service_worker',
        cause: 'send.blocked'
      });
      return;
    }
    enqueueOutgoing(message);
    setConnectionState('reconnecting', 'Not connected to engine; message queued', { source: 'service_worker', cause: 'send.queued' });
    scheduleReconnect('sendToEngine');
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
    setConnectionState('reconnecting', 'Heartbeat degraded; reconnecting', { source: 'service_worker', cause: 'heartbeat.missed' });
    closeSocketOnly('heartbeat missed');
    scheduleReconnect('heartbeatMissed');
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

function publishState(status, message, details = {}) {
  lastStateTransition = {
    timestamp: new Date().toISOString(),
    source: details.source || 'service_worker',
    cause: details.cause || '',
    status,
    message,
    previousConnectionState: details.previousConnectionState ?? connectionState,
    newConnectionState: details.newConnectionState ?? connectionState,
    previousRunState: details.previousRunState ?? runState,
    newRunState: details.newRunState ?? runState,
    wsCloseCode: lastWsCloseCode,
    wsCloseReason: lastWsCloseReason,
    wsCloseWasClean: lastWsCloseWasClean,
    protocolError: lastProtocolError,
    stopExplicit: details.stopExplicit ?? stopRequestedExplicitly
  };
  publish({
    type: 'state',
    status,
    message,
    connected,
    currentRunId,
    currentRequestId,
    connectionStatus: connectionState,
    runStatus: runState,
    source: lastStateTransition.source,
    cause: lastStateTransition.cause,
    previousConnectionState: lastStateTransition.previousConnectionState,
    newConnectionState: lastStateTransition.newConnectionState,
    previousRunState: lastStateTransition.previousRunState,
    newRunState: lastStateTransition.newRunState,
    wsCloseCode: lastWsCloseCode,
    wsCloseReason: lastWsCloseReason,
    wsCloseWasClean: lastWsCloseWasClean,
    protocolError: lastProtocolError,
    stopExplicit: lastStateTransition.stopExplicit
  });
  publishRuntimeSnapshot();
}

function setConnectionState(nextState, message, details = {}) {
  const previousConnectionState = connectionState;
  const previousRunState = runState;
  connectionState = nextState;
  publishState(nextState, message, {
    ...details,
    previousConnectionState,
    newConnectionState: nextState,
    previousRunState,
    newRunState: runState
  });
}

function setRunState(nextState, message, details = {}) {
  const previousConnectionState = connectionState;
  const previousRunState = runState;
  runState = nextState;
  publishState(nextState, message, {
    ...details,
    previousConnectionState,
    newConnectionState: connectionState,
    previousRunState,
    newRunState: nextState
  });
}

async function publishPageSnapshot() {
  try {
    const tab = await resolveTargetTab({ preferActive: shouldPreferActiveTab() });
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

function buildContentScriptUnavailableError(tab) {
  return {
    ok: false,
    error: 'content_script_unavailable',
    message: 'No content script is available in the active tab. Reload this page or choose a normal web tab.',
    tabId: tab && tab.id ? tab.id : null,
    url: tab && tab.url ? tab.url : ''
  };
}

function isReceivingEndMissing(error) {
  const text = String(error && error.message ? error.message : error || '');
  return /Receiving end does not exist/i.test(text);
}

function isTabAllowed(tab) {
  return Boolean(tab && tab.id && !restrictedUrl(tab.url || ''));
}

function rememberContentScriptOk(tab, message = 'Content script active') {
  lastContentScriptStatus = 'active';
  lastContentScriptMessage = message;
  lastContentScriptLastSeenAt = new Date().toISOString();
  lastContentScriptLastUrl = tab && tab.url ? tab.url : '';
  lastContentScriptLastError = '';
  lastSendMessageError = '';
}

function rememberContentScriptError(errorCode, message, tab, sendMessageError = '') {
  lastContentScriptStatus = 'error';
  lastContentScriptMessage = message || errorCode || 'Content script error';
  lastContentScriptLastError = lastContentScriptMessage;
  lastContentScriptLastUrl = tab && tab.url ? tab.url : '';
  if (sendMessageError) {
    lastSendMessageError = sendMessageError;
  }
}

function makeChromeSendMessageError(error, tab) {
  const text = String(error && error.message ? error.message : error || chrome.runtime.lastError && chrome.runtime.lastError.message || 'chrome.tabs.sendMessage failed');
  rememberContentScriptError('chrome_send_message_failed', text, tab, text);
  return {
    success: false,
    errorCode: 'chrome_send_message_failed',
    error: text,
    result: {
      ok: false,
      errorCode: 'chrome_send_message_failed',
      error: text,
      lastSendMessageError: text,
      tabId: tab && tab.id ? tab.id : null,
      url: tab && tab.url ? tab.url : ''
    }
  };
}

async function ensureContentScript(tabId) {
  if (!tabId) {
    lastContentScriptStatus = 'error';
    lastContentScriptMessage = 'Active tab missing';
    return { ok: false, error: 'invalid_tab', message: 'No active tab available.', tabId: null, url: '' };
  }

  let tab;
  try {
    tab = await chrome.tabs.get(tabId);
  } catch {
    lastContentScriptStatus = 'error';
    lastContentScriptMessage = 'Active tab not found';
    return { ok: false, error: 'invalid_tab', message: 'No active tab available.', tabId, url: '' };
  }

  lastResolvedTabAllowed = isTabAllowed(tab);
  if (!lastResolvedTabAllowed) {
    lastContentScriptStatus = 'unavailable';
    lastContentScriptMessage = 'Restricted or unavailable tab';
    return {
      ok: false,
      error: 'restricted_tab',
      message: 'Open a normal web tab so NODAL OS can operate.',
      tabId,
      url: tab.url || ''
    };
  }

  try {
    const ping = await chrome.tabs.sendMessage(tabId, {
      type: 'nexa.content.ping',
      protocolVersion: PROTOCOL_VERSION
    });
    if (ping && ping.ok && Array.isArray(ping.capabilities) && ping.capabilities.includes('observePageLite')) {
      rememberContentScriptOk(tab, 'Content script active');
      return { ok: true, tabId, url: tab.url || '', injected: false, ping };
    }
    lastContentScriptMessage = 'Content script active but missing required capabilities; reinjecting';
  } catch (error) {
    if (!isReceivingEndMissing(error)) {
      lastContentScriptMessage = String(error && error.message ? error.message : error);
      rememberContentScriptError('content_script_error', lastContentScriptMessage, tab, lastContentScriptMessage);
      return {
        ok: false,
        error: 'content_script_error',
        message: lastContentScriptMessage,
        tabId,
        url: tab.url || ''
      };
    }
  }

  try {
    await chrome.scripting['executeScript']({
      target: { tabId },
      files: ['content_script.js']
    });
    await delay(150);
    const ping = await chrome.tabs.sendMessage(tabId, {
      type: 'nexa.content.ping',
      protocolVersion: PROTOCOL_VERSION
    });
    if (ping && ping.ok && Array.isArray(ping.capabilities) && ping.capabilities.includes('observePageLite')) {
      lastContentScriptStatus = 'injected';
      lastContentScriptMessage = 'Content script injected';
      lastContentScriptLastSeenAt = new Date().toISOString();
      lastContentScriptLastUrl = tab.url || '';
      lastContentScriptLastError = '';
      lastSendMessageError = '';
      lastContentScriptInjectedAt = new Date().toISOString();
      return { ok: true, tabId, url: tab.url || '', injected: true };
    }
  } catch (error) {
    lastContentScriptMessage = String(error && error.message ? error.message : error);
    rememberContentScriptError('chrome_send_message_failed', lastContentScriptMessage, tab, lastContentScriptMessage);
  }

  const unavailable = buildContentScriptUnavailableError(tab);
  lastContentScriptStatus = 'unavailable';
  lastContentScriptMessage = unavailable.message;
  return unavailable;
}

async function sendTabMessageWithRecovery(tab, message) {
  const ensured = await ensureContentScript(tab.id);
  if (!ensured.ok) {
    return {
      success: false,
      error: ensured.message || ensured.error || 'Content script unavailable',
      errorCode: ensured.error || 'content_script_unavailable',
      result: {
        ok: false,
        errorCode: ensured.error || 'content_script_unavailable',
        error: ensured.message || ensured.error || 'Content script unavailable',
        tabId: tab && tab.id ? tab.id : null,
        url: tab && tab.url ? tab.url : ''
      }
    };
  }

  try {
    if (message && message.tool === 'observePage') {
      lastObserveRequestAt = new Date().toISOString();
      lastObserveError = '';
    }
    const response = await chrome.tabs.sendMessage(tab.id, message);
    rememberContentScriptOk(tab, 'Content script responded');
    if (message && message.tool === 'observePage') {
      lastObserveResultAt = new Date().toISOString();
    }
    return response;
  } catch (error) {
    if (!isReceivingEndMissing(error)) {
      const failed = makeChromeSendMessageError(error, tab);
      if (message && message.tool === 'observePage') {
        lastObserveError = failed.error;
      }
      return failed;
    }

    const retried = await ensureContentScript(tab.id);
    if (!retried.ok) {
      const failed = makeChromeSendMessageError(retried.message || retried.error || 'Content script unavailable', tab);
      if (message && message.tool === 'observePage') {
        lastObserveError = failed.error;
      }
      return failed;
    }
    try {
      const response = await chrome.tabs.sendMessage(tab.id, message);
      rememberContentScriptOk(tab, 'Content script responded after injection');
      if (message && message.tool === 'observePage') {
        lastObserveResultAt = new Date().toISOString();
      }
      return response;
    } catch (retryError) {
      const failed = makeChromeSendMessageError(retryError, tab);
      if (message && message.tool === 'observePage') {
        lastObserveError = failed.error;
      }
      return failed;
    }
  }
}

async function resolveTargetTab(options = {}) {
  if (options.preferActive) {
    const activeTab = await resolveActiveWebTab({ includeChatGpt: Boolean(options.includeChatGpt) });
    if (activeTab) {
      if (options.pinTarget !== false) {
        targetTabId = activeTab.id;
        targetTabPinnedExplicitly = Boolean(options.explicit);
        rememberTargetTab(activeTab, options.explicit ? 'explicit.active' : 'active');
      }
      lastResolvedTabAllowed = true;
      return activeTab;
    }
  }

  if (targetTabId) {
    try {
      const tab = await chrome.tabs.get(targetTabId);
      if (tab && tab.id && !restrictedUrl(tab.url || '')) {
        rememberTargetTab(tab, 'pinned');
        return tab;
      }
    } catch {
      targetTabId = null;
    }
  }

  const candidates = await chrome.tabs.query({});
  const preferred = candidates.find((tab) => tab.active && isTabImplicitTargetCandidate(tab));
  if (preferred) {
    targetTabId = preferred.id;
    targetTabPinnedExplicitly = false;
    rememberTargetTab(preferred, 'active.implicit');
    lastResolvedTabAllowed = true;
    return preferred;
  }

  const anyAllowed = candidates.find((tab) => isTabImplicitTargetCandidate(tab));
  if (anyAllowed) {
    targetTabId = anyAllowed.id;
    targetTabPinnedExplicitly = false;
    rememberTargetTab(anyAllowed, 'fallback.implicit');
    lastResolvedTabAllowed = true;
    return anyAllowed;
  }

  if (options.allowCreate) {
    lastResolvedTabAllowed = false;
    return null;
  }

  lastResolvedTabAllowed = false;
  return null;
}

async function resolveActiveWebTab(options = {}) {
  const activeTabs = await chrome.tabs.query({ active: true, lastFocusedWindow: true });
  const allowed = (tab) => isTabAllowed(tab) && (options.includeChatGpt || !isChatGptTab(tab));
  const preferred = activeTabs.find((tab) => allowed(tab));
  if (preferred) {
    return preferred;
  }

  const allTabs = await chrome.tabs.query({});
  return allTabs.find((tab) => tab.active && allowed(tab)) ||
    allTabs.find((tab) => allowed(tab)) ||
    null;
}

async function getTargetTabForRun(instruction) {
  const allTabs = await chrome.tabs.query({});
  const desired = inferDesiredTargetSite(instruction);
  if (desired) {
    const matching = allTabs.find((tab) => isTabAllowed(tab) && desired.hosts.some((host) => String(tab.url || '').includes(host)));
    if (matching) {
      return matching;
    }
  }

  if (targetTabPinnedExplicitly && targetTabId) {
    try {
      const pinned = await chrome.tabs.get(targetTabId);
      if (isTabAllowed(pinned) && !isChatGptTab(pinned)) {
        return pinned;
      }
    } catch {
      targetTabId = null;
      targetTabPinnedExplicitly = false;
    }
  }

  const activeTabs = await chrome.tabs.query({ active: true, lastFocusedWindow: true });
  const activeAllowed = activeTabs.find((tab) => isTabImplicitTargetCandidate(tab));
  if (activeAllowed) {
    return activeAllowed;
  }

  const lastMatching = allTabs.find((tab) => isTabImplicitTargetCandidate(tab) && desired && desired.hosts.some((host) => String(tab.url || '').includes(host)));
  if (lastMatching) {
    return lastMatching;
  }

  return allTabs.find((tab) => isTabImplicitTargetCandidate(tab)) || null;
}

function inferDesiredTargetSite(instruction) {
  const text = normalizeText(instruction || '');
  if (/mercado\s*libre|mercadolibre|ml argentina/.test(text)) {
    return { label: 'Mercado Libre Argentina', hosts: ['mercadolibre.com.ar', 'listado.mercadolibre.com.ar'] };
  }
  if (/\b(?:chatgpt|openai)\b/.test(text)) {
    return { label: 'ChatGPT', hosts: ['chatgpt.com'] };
  }
  return null;
}

function normalizeText(value) {
  return String(value || '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/\s+/g, ' ')
    .trim();
}

function isTabImplicitTargetCandidate(tab) {
  return isTabAllowed(tab) && !isChatGptTab(tab);
}

function isChatGptTab(tab) {
  return /^https:\/\/chatgpt\.com\//i.test(String(tab && tab.url ? tab.url : ''));
}

function rememberTargetTab(tab, reason) {
  if (!tab || !tab.id) {
    return;
  }
  lastTargetTabSnapshot = {
    tabId: String(tab.id),
    url: tab.url || '',
    title: tab.title || '',
    active: Boolean(tab.active),
    allowed: isTabAllowed(tab),
    reason,
    updatedAt: new Date().toISOString()
  };
}

async function handleLearningTabUpdated(tabId, changeInfo, tab) {
  if (!learningSession || learningSession.learningState !== 'recording' || !learningSession.startTabId || tabId !== learningSession.startTabId) {
    return;
  }
  if (changeInfo.status !== 'complete' || !tab || restrictedUrl(tab.url || '')) {
    return;
  }

  await appendLearningNavigationIfNeeded(tab.url || '', tab.title || '', learningSession.lastKnownUrl || '');
  await sendLearningModeToTab(true, tabId);
  publishLearningState();
}

async function appendLearningNavigationIfNeeded(url, title, previousUrl) {
  if (!learningSession || !url || restrictedUrl(url)) {
    return;
  }
  if (learningSession.lastKnownUrl === url && Array.isArray(learningSession.steps) && learningSession.steps.length > 0) {
    return;
  }

  const step = {
    timestamp: new Date().toISOString(),
    actionType: 'navigate',
    url,
    title,
    previousUrl: previousUrl || learningSession.lastKnownUrl || '',
    target: null,
    value: '',
    valueRedacted: false,
    eventMeta: {},
    verificationHint: {
      beforeUrl: previousUrl || learningSession.lastKnownUrl || '',
      expectedUrlChange: true
    }
  };

  learningSession.steps.push(step);
  learningSession.updatedAt = step.timestamp;
  learningSession.lastKnownUrl = url;
  learningSession.activeTabUrl = url;
  learningSession.capturedEventCount = learningSession.steps.length;
  learningSession.lastEventSummary = summarizeLearningEvent(step);
  await chrome.storage.local.set({ [LEARNING_DRAFT_KEY]: learningSession });
}

function notifyStopToTargetTab() {
  if (!targetTabId) {
    return;
  }

  chrome.tabs.get(targetTabId).then((tab) => {
    if (!isTabAllowed(tab)) {
      return;
    }
    return sendTabMessageWithRecovery(tab, {
      type: 'local.stop',
      protocolVersion: PROTOCOL_VERSION,
      runId: currentRunId || ''
    }).catch(() => {});
  }).catch(() => {});
}

async function startLearning(payload) {
  const tab = await resolveActiveWebTab();
  if (!tab || !tab.id) {
    publish({ type: 'recipeError', message: 'Abri una pestaña web normal antes de comenzar aprendizaje.' });
    return;
  }
  targetTabId = tab.id;
  learningSession = {
    recipeId: `recipe-${Date.now().toString(36)}`,
    name: payload.name || 'Nueva receta',
    description: payload.description || '',
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    startUrl: tab ? tab.url || '' : '',
    startTabId: tab.id,
    lastKnownUrl: tab.url || '',
    activeTabUrl: tab.url || '',
    steps: [],
    parameters: [],
    sensitiveFields: [],
    humanCheckpoints: [],
    capturedEventCount: 0,
    lastEventSummary: '',
    recording: true,
    learningState: 'recording'
  };
  await chrome.storage.local.set({ [LEARNING_DRAFT_KEY]: learningSession });
  await sendLearningModeToTab(true, tab.id);
  await appendLearningNavigationIfNeeded(tab.url || '', tab.title || '', '');
  publishLearningState();
}

async function stopLearning() {
  if (learningSession) {
    if (!Array.isArray(learningSession.steps) || learningSession.steps.length === 0) {
      publish({ type: 'recipeError', message: 'No se capturaron acciones. La receta no puede guardarse todavía.' });
    }
    learningSession = {
      ...learningSession,
      recording: false,
      learningState: 'reviewing',
      updatedAt: new Date().toISOString()
    };
    await chrome.storage.local.set({ [LEARNING_DRAFT_KEY]: learningSession });
  }
  await sendLearningModeToTab(false, learningSession && learningSession.startTabId ? learningSession.startTabId : null);
  publishLearningState();
}

async function pauseLearning() {
  if (!learningSession) {
    publish({ type: 'recipeError', message: 'No active learning session.' });
    return;
  }
  learningSession = {
    ...learningSession,
    recording: false,
    learningState: 'paused',
    updatedAt: new Date().toISOString()
  };
  await chrome.storage.local.set({ [LEARNING_DRAFT_KEY]: learningSession });
  await sendLearningModeToTab(false, learningSession.startTabId || null);
  publish({ type: 'learningNotice', message: 'Aprendizaje pausado. Podés navegar o buscar sin que NODAL OS lo grabe.' });
  publishLearningState();
}

async function resumeLearning() {
  if (!learningSession) {
    publish({ type: 'recipeError', message: 'No learning draft available.' });
    return;
  }
  learningSession = {
    ...learningSession,
    recording: true,
    learningState: 'recording',
    updatedAt: new Date().toISOString()
  };
  await chrome.storage.local.set({ [LEARNING_DRAFT_KEY]: learningSession });
  await sendLearningModeToTab(true, learningSession.startTabId || null);
  publish({ type: 'learningNotice', message: 'Aprendizaje reanudado. NODAL OS vuelve a capturar tus acciones.' });
  publishLearningState();
}

async function clearLearningDraft() {
  const learningTabId = learningSession && learningSession.startTabId ? learningSession.startTabId : null;
  learningSession = null;
  await chrome.storage.local.remove(LEARNING_DRAFT_KEY);
  await sendLearningModeToTab(false, learningTabId);
  publishLearningState();
}

async function prepareCurrentTab() {
  const tab = await resolveActiveWebTab({ includeChatGpt: true });
  if (!tab || !tab.id) {
    publish({ type: 'prepareTabResult', ok: false, error: 'invalid_tab', message: 'Open a normal web tab so NODAL OS can operate.' });
    publishRuntimeSnapshot();
    return;
  }

  targetTabId = tab.id;
  targetTabPinnedExplicitly = true;
  rememberTargetTab(tab, 'prepareCurrentTab');
  const result = await ensureContentScript(tab.id);
  publish({
    type: 'prepareTabResult',
    ...result
  });
  publishRuntimeSnapshot();
}

async function handleLearningEvent(event, sender) {
  if (!learningSession || !learningSession.recording || learningSession.learningState === 'paused' || !event) {
    return;
  }
  if (learningSession.startTabId && sender.tab && sender.tab.id && sender.tab.id !== learningSession.startTabId) {
    return;
  }

  const step = sanitizeLearningStep(event, sender);
  learningSession.steps.push(step);
  learningSession.updatedAt = new Date().toISOString();
  learningSession.activeTabUrl = step.url || learningSession.activeTabUrl || '';
  learningSession.lastKnownUrl = step.url || learningSession.lastKnownUrl || '';
  learningSession.capturedEventCount = Array.isArray(learningSession.steps) ? learningSession.steps.length : 0;
  learningSession.lastEventSummary = summarizeLearningEvent(step);

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

function summarizeLearningEvent(step) {
  if (!step) {
    return '';
  }
  if (step.actionType === 'navigate') {
    return `navigate ${step.url || ''}`.trim();
  }
  const label = step.target && (step.target.accessibleName || step.target.visibleText)
    ? step.target.accessibleName || step.target.visibleText
    : step.url || '';
  return `${step.actionType || 'event'} ${label}`.trim();
}

async function saveLearningRecipe(payload) {
  if (!learningSession) {
    publish({ type: 'recipeError', message: 'No learning draft available.' });
    return;
  }

  const recipe = recipeFromLearningDraft(learningSession, payload);
  if (!Array.isArray(recipe.steps) || recipe.steps.length === 0) {
    publish({ type: 'recipeError', message: 'Esta receta no tiene pasos. Captura acciones reales antes de guardar.' });
    publishLearningState();
    return;
  }
  await upsertRecipe(recipe);
  learningSession = { ...recipe, recording: false, learningState: 'saved' };
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

async function sendLearningModeToTab(enabled, preferredTabId = null) {
  const tab = preferredTabId
    ? await chrome.tabs.get(preferredTabId).catch(() => null)
    : await resolveActiveWebTab();
  if (!tab || !tab.id || restrictedUrl(tab.url || '')) {
    return;
  }

  await sendTabMessageWithRecovery(tab, {
    type: enabled ? 'learning.start' : 'learning.stop',
    protocolVersion: PROTOCOL_VERSION
  }).catch(() => {});
}

async function startRecipeRun(recipeId, parameters) {
  if (!LEGACY_RUNNER_ENABLED) {
    recipeRunner = null;
    publish({
      type: 'recipeRunState',
      run: {
        recipeId,
        status: 'blocked',
        runtimeKind: EXTENSION_RUNTIME_MODE,
        coreGoverned: true,
        legacyRunnerEnabled: LEGACY_RUNNER_ENABLED,
        errorCode: 'legacy_runner_disabled',
        message: 'Recipe execution is core-governed. The extension companion cannot own or complete runs.'
      }
    });
    publishRuntimeSnapshot();
    return;
  }

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
  if (!LEGACY_RUNNER_ENABLED) {
    recipeRunner = null;
    publish({
      type: 'recipeRunState',
      run: {
        status: 'blocked',
        runtimeKind: EXTENSION_RUNTIME_MODE,
        coreGoverned: true,
        legacyRunnerEnabled: LEGACY_RUNNER_ENABLED,
        errorCode: 'legacy_runner_disabled',
        message: 'Legacy recipe runner is disabled in core-governed companion mode.'
      }
    });
    publishRuntimeSnapshot();
    return;
  }

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
  const response = await sendTabMessageWithRecovery(tab, { type: 'tool.execute', protocolVersion: PROTOCOL_VERSION, tool, args: args || {} });
  if (!response || !response.success) {
    throw new Error(response && response.error ? response.error : `${tool} failed`);
  }
  if (tool === 'observePage') {
    lastObservedPage = response.result || null;
  }
  return response.result;
}

async function observeCurrentPage(reason, options = {}) {
  try {
    const result = await executeTabTool('observePage', {});
    const handoff = options.includeHandoff === false ? null : createResumeHandoff(result, reason);
    publish({ type: 'observeCurrentPageResult', ok: true, result, handoff, reason });
    publishRuntimeSnapshot();
    return result;
  } catch (error) {
    const message = String(error && error.message ? error.message : error);
    publish({ type: 'observeCurrentPageResult', ok: false, error: 'observe_failed', message, reason });
    throw error;
  }
}

async function handleHandoffContinue() {
  const observation = await observeCurrentPage('handoffContinue');
  const fingerprint = buildObservationFingerprint(observation);
  if (fingerprint && fingerprint === lastHandoffFingerprint && looksLikeAccessScreen(observation)) {
    publishHumanIntervention({
      status: 'Estoy esperando que completes algo',
      whatHappened: 'Sigo viendo la misma pantalla de acceso.',
      whatDid: 'Reobserve la pagina antes de repetir cualquier accion.',
      whatSee: describeObservation(observation),
      whatNeed: 'Si todavia falta completar algo, hacelo manualmente. Si ya terminaste, verifica que la pagina haya cambiado o indicame el proximo paso.',
      bannerMessage: 'NODAL OS sigue viendo la misma pantalla de acceso. Completala manualmente y luego volvé a continuar.',
      timeline: 'Reobserve la pagina y sigo esperando intervencion humana',
      reason: 'credentialRequired'
    });
    setRunState('paused', 'Sigo viendo la misma pantalla de acceso', { source: 'service_worker', cause: 'handoff.sameScreen' });
    return;
  }

  if (currentRunId) {
    stopRequestedExplicitly = false;
    setRunState('running', 'Reobserve la pagina y continue desde el nuevo estado', { source: 'side_panel', cause: 'handoff.continue' });
    runCommand('resume');
  }
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

function publishHumanIntervention(handoff) {
  const payload = handoff && handoff.handoff ? handoff.handoff : handoff;
  lastHandoffFingerprint = payload && payload.fingerprint ? payload.fingerprint : lastHandoffFingerprint;
  publish({ type: 'humanIntervention', handoff: payload, reason: payload && payload.reason ? payload.reason : 'humanIntervention', message: payload && payload.whatHappened ? payload.whatHappened : '' });
}

function createHumanHandoffPayload(reason, message, observation) {
  const whatSee = observation ? describeObservation(observation) : 'Estoy viendo una pagina que requiere validacion humana.';
  const payload = {
    status: 'Estoy esperando que completes algo',
    whatHappened: message || 'NODAL OS llegó a un punto donde la página requiere una acción humana.',
    whatDid: 'Observe la pagina y me detuve antes de tocar credenciales, captcha, 2FA o una accion sensible.',
    whatSee,
    whatNeed: 'Completa manualmente lo que corresponda y luego presiona "Ya lo resolvi, continuar".',
    bannerMessage: 'Completa manualmente el acceso o validacion y luego presiona "Ya lo resolvi, continuar".',
    timeline: 'NODAL OS espera intervención humana',
    reason: reason || 'humanIntervention',
    fingerprint: observation ? buildObservationFingerprint(observation) : lastHandoffFingerprint
  };
  return payload;
}

function createResumeHandoff(observation, reason) {
  if (!looksLikeAccessScreen(observation)) {
    lastHandoffFingerprint = '';
    return null;
  }
  return {
    status: 'Estoy esperando que completes algo',
    whatHappened: reason === 'handoffContinue'
      ? 'NODAL OS reobservó la página después de tu intervención.'
      : 'NODAL OS reobservó la página y detectó que sigue habiendo una etapa humana pendiente.',
    whatDid: 'Observe la pagina actual antes de repetir cualquier accion.',
    whatSee: describeObservation(observation),
    whatNeed: 'Si todavia falta completar algo, hacelo manualmente. Cuando la pagina cambie, presiona "Ya lo resolvi, continuar".',
    bannerMessage: 'NODAL OS sigue esperando que completes la parte humana antes de continuar.',
    timeline: 'NODAL OS reobservó la página y sigue esperando',
    reason: 'credentialRequired',
    fingerprint: buildObservationFingerprint(observation)
  };
}

function looksLikeAccessScreen(observation) {
  return hasStrongCredentialSignal(observation);
}

function hasStrongCredentialSignal(observation) {
  return Boolean(
    observation &&
    (observation.hasPasswordField ||
      observation.hasCaptchaLike ||
      observation.hasTwoFactorLike ||
      observation.hasCredentialEntry)
  );
}

function buildObservationFingerprint(observation) {
  if (!observation) {
    return '';
  }
  return [
    observation.url || '',
    observation.title || '',
    observation.hasCredentialLike ? 'credential' : '',
    observation.hasPasswordField ? 'password' : '',
    observation.hasCaptchaLike ? 'captcha' : '',
    observation.hasTwoFactorLike ? '2fa' : ''
  ].join('|');
}

function describeObservation(observation) {
  if (!observation) {
    return 'No tengo una observacion reciente de la pagina.';
  }
  const parts = [];
  if (observation.title) {
    parts.push(`Titulo: ${observation.title}`);
  }
  if (observation.url) {
    parts.push(`URL: ${observation.url}`);
  }
  if (observation.hasPasswordField) {
    parts.push('La pagina muestra campos de password.');
  }
  if (observation.hasCredentialLike) {
    parts.push('La pagina parece pedir credenciales o acceso.');
  }
  if (observation.hasCaptchaLike) {
    parts.push('Detecte una validacion tipo captcha.');
  }
  if (observation.hasTwoFactorLike) {
    parts.push('Detecte una etapa de 2FA o codigo.');
  }
  return parts.join(' ') || 'Estoy viendo una pagina que requiere contexto humano.';
}

function delay(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

async function publishRuntimeSnapshot() {
  const config = await chrome.storage.local.get(DEFAULT_CONFIG);
  let tab = null;
  try {
    tab = await resolveTargetTab({ preferActive: shouldPreferActiveTab() });
  } catch {
    tab = null;
  }
  publish({
    type: 'runtimeSnapshot',
    runtime: {
      connection: {
        connected,
        state: connectionState,
        connectionStoppedByUser,
        reconnectBlocked,
        reconnectBlockedReason,
        clientId,
        lastConnectedAt,
        lastSeenAt,
        lastError: lastConnectionError,
        lastProtocolError,
        lastWsCloseCode,
        lastWsCloseReason,
        lastWsCloseWasClean,
        lastWsCloseAt,
        lastTransition: lastStateTransition,
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
        runtimeKind: EXTENSION_RUNTIME_MODE,
        coreGovernedMode: CORE_GOVERNED_MODE,
        capabilities: EXTENSION_CAPABILITIES,
        clientId,
        lastSeenAt,
        tabId: tab && tab.id ? String(tab.id) : '',
        url: tab ? tab.url || '' : '',
        controlContext: 'sidepanel',
        targetTabId: lastTargetTabSnapshot && lastTargetTabSnapshot.tabId ? lastTargetTabSnapshot.tabId : (tab && tab.id ? String(tab.id) : ''),
        targetUrl: lastTargetTabSnapshot && lastTargetTabSnapshot.url ? lastTargetTabSnapshot.url : (tab ? tab.url || '' : ''),
        targetTitle: lastTargetTabSnapshot && lastTargetTabSnapshot.title ? lastTargetTabSnapshot.title : (tab ? tab.title || '' : ''),
        targetActive: lastTargetTabSnapshot ? lastTargetTabSnapshot.active : Boolean(tab && tab.active),
        targetAllowed: lastTargetTabSnapshot ? lastTargetTabSnapshot.allowed : Boolean(tab && !restrictedUrl(tab.url || '')),
        targetReason: lastTargetTabSnapshot ? lastTargetTabSnapshot.reason : '',
        contentScriptActive: tab ? !restrictedUrl(tab.url || '') : false,
        permissions: ['activeTab', 'scripting', 'storage', 'tabs', 'sidePanel']
      },
      run: {
        runId: currentRunId,
        requestId: currentRequestId,
        status: runState,
        stopRequestedExplicitly,
        currentTool,
        lastToolResult,
        lastToolRequest,
        lastRunStatus,
        lastError: lastToolResult && !lastToolResult.success ? lastToolResult.error : '',
        recipeRun: recipeRunner
      },
      contentScript: {
        status: lastContentScriptStatus,
        message: lastContentScriptMessage,
        injectedAt: lastContentScriptInjectedAt,
        lastSeenAt: lastContentScriptLastSeenAt,
        lastUrl: lastContentScriptLastUrl,
        lastError: lastContentScriptLastError,
        lastSendMessageError,
        lastObserveRequestAt,
        lastObserveResultAt,
        lastObserveError,
        tabAllowed: lastResolvedTabAllowed
      }
    }
  });
}
