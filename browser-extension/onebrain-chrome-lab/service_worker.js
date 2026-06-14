const PROTOCOL_VERSION = 'chrome-lab-v1';
const DEFAULT_CONFIG = { host: '127.0.0.1', port: '8787' };
const RECIPES_KEY = 'nexaRecipes';
const LEARNING_DRAFT_KEY = 'nexaLearningDraft';

let socket = null;
let connected = false;
let connectingPromise = null;
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
const sidePorts = new Set();

chrome.runtime.onInstalled.addListener(() => {
  chrome.sidePanel.setPanelBehavior({ openPanelOnActionClick: true });
});

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
      runRecipe(message.recipeId);
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
    port: config.port || DEFAULT_CONFIG.port
  });
}

function connect(config) {
  return connectWebSocket(config);
}

function connectWebSocket(config) {
  if (socket && socket.readyState === WebSocket.OPEN && connected) {
    return Promise.resolve();
  }

  if (connectingPromise) {
    return connectingPromise;
  }

  disconnect('reconnect');
  stopped = false;
  const url = `ws://${config.host || DEFAULT_CONFIG.host}:${config.port || DEFAULT_CONFIG.port}/ws/extension`;

  connectingPromise = new Promise((resolve, reject) => {
    socket = new WebSocket(url);
    socket.addEventListener('open', () => {
      connected = true;
      connectingPromise = null;
      publishState('connected', `Connected to ${url}`);
      sendToEngine({
        type: 'extension.hello',
        protocolVersion: PROTOCOL_VERSION,
        clientId: chrome.runtime.id,
        extensionVersion: chrome.runtime.getManifest().version,
        browser: 'chrome'
      });
      resolve();
    });
    socket.addEventListener('message', (event) => handleEngineMessage(event.data));
    socket.addEventListener('close', () => {
      connected = false;
      connectingPromise = null;
      publishState('disconnected', 'Bridge connection closed');
      reject(new Error('Bridge connection closed'));
    });
    socket.addEventListener('error', () => {
      connected = false;
      connectingPromise = null;
      publishState('error', 'Bridge WebSocket error');
      reject(new Error('Bridge WebSocket error'));
    });
  });

  return connectingPromise;
}

function disconnect(reason) {
  if (socket) {
    socket.close(1000, reason);
  }
  socket = null;
  connected = false;
  connectingPromise = null;
  publishState('disconnected', reason);
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
    lastHealth = { ok: response.ok && body.ok, body: { ...body, publicConfig }, checkedAt: new Date().toISOString() };
    publish({ type: 'health', ok: response.ok && body.ok, body });
    publishRuntimeSnapshot();
  } catch (error) {
    lastHealth = { ok: false, error: String(error && error.message ? error.message : error), checkedAt: new Date().toISOString() };
    publish({ type: 'health', ok: false, error: String(error && error.message ? error.message : error) });
    publishRuntimeSnapshot();
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
  if (message.type === 'engine.hello') {
    publishState('connected', 'Engine hello received');
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
    publishState('error', 'Not connected to engine');
    return;
  }
  socket.send(JSON.stringify(message));
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

  const recipe = {
    recipeId: learningSession.recipeId || `recipe-${Date.now().toString(36)}`,
    name: payload.name || learningSession.name || 'Nueva receta',
    description: payload.description || learningSession.description || '',
    createdAt: learningSession.createdAt || new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    startUrl: learningSession.startUrl || '',
    steps: learningSession.steps || [],
    parameters: inferParameters(learningSession.steps || []),
    sensitiveFields: learningSession.sensitiveFields || [],
    humanCheckpoints: learningSession.humanCheckpoints || [],
    status: 'draft-v0'
  };
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
  await upsertRecipe({ ...recipe, updatedAt: new Date().toISOString() });
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
    ...source,
    recipeId: `recipe-${Date.now().toString(36)}`,
    name: `${source.name || 'Receta'} copia`,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString()
  };
  recipes.unshift(copy);
  await chrome.storage.local.set({ [RECIPES_KEY]: recipes });
  publishRecipes();
}

async function runRecipe(recipeId) {
  const recipes = await readRecipes();
  const recipe = recipes.find((item) => item.recipeId === recipeId);
  if (!recipe) {
    publish({ type: 'recipeError', message: 'Recipe not found.' });
    return;
  }

  const instruction = buildRecipeInstruction(recipe);
  await startRun(instruction);
}

function buildRecipeInstruction(recipe) {
  const steps = (recipe.steps || []).slice(0, 20).map((step, index) => {
    const label = step.target && (step.target.accessibleName || step.target.visibleText)
      ? step.target.accessibleName || step.target.visibleText
      : step.actionType;
    return `${index + 1}. ${step.actionType}: ${label}`;
  }).join('\n');
  return `Ejecuta esta receta V0 con soporte navigate/click/input/human checkpoint.\nNombre: ${recipe.name}\nStartUrl: ${recipe.startUrl}\nPasos:\n${steps}`;
}

async function readRecipes() {
  const data = await chrome.storage.local.get({ [RECIPES_KEY]: [] });
  return Array.isArray(data[RECIPES_KEY]) ? data[RECIPES_KEY] : [];
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
  return steps
    .filter((step) => (step.actionType === 'input' || step.actionType === 'select') && step.target && !step.valueRedacted)
    .map((step, index) => ({
      name: parameterNameFor(step, index),
      sourceStep: index,
      required: true
    }));
}

function parameterNameFor(step, index) {
  const label = step.target && (step.target.name || step.target.accessibleName || step.target.visibleText)
    ? step.target.name || step.target.accessibleName || step.target.visibleText
    : `param_${index + 1}`;
  return String(label).toLowerCase().replace(/[^a-z0-9]+/g, '_').replace(/^_+|_+$/g, '') || `param_${index + 1}`;
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
        stopped,
        host: config.host || DEFAULT_CONFIG.host,
        port: config.port || DEFAULT_CONFIG.port,
        health: lastHealth
      },
      ai: {
        provider: lastHealth && lastHealth.body && lastHealth.body.publicConfig ? lastHealth.body.publicConfig.aiProvider || 'OpenAI' : 'OpenAI',
        model: lastHealth && lastHealth.body && lastHealth.body.publicConfig ? lastHealth.body.publicConfig.model || '-' : '-',
        hasApiKeyLocal: lastHealth && lastHealth.body && lastHealth.body.publicConfig ? Boolean(lastHealth.body.publicConfig.hasApiKey) : null,
        lastError: lastAiError
      },
      extension: {
        webSocketConnected: connected,
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
        lastError: lastToolResult && !lastToolResult.success ? lastToolResult.error : ''
      }
    }
  });
}
