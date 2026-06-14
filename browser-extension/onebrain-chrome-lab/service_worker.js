const PROTOCOL_VERSION = 'chrome-lab-v1';
const DEFAULT_CONFIG = { host: '127.0.0.1', port: '8787' };

let socket = null;
let connected = false;
let connectingPromise = null;
let stopped = false;
let currentRunId = '';
let targetTabId = null;
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
  const url = `http://${config.host || DEFAULT_CONFIG.host}:${config.port || DEFAULT_CONFIG.port}/health`;
  try {
    const response = await fetch(url);
    const body = await response.json();
    publish({ type: 'health', ok: response.ok && body.ok, body });
  } catch (error) {
    publish({ type: 'health', ok: false, error: String(error && error.message ? error.message : error) });
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
    publish({ type: 'runStatus', message });
    publishState(message.status || 'running', message.message || '');
    return;
  }

  if (message.type === 'run.stop') {
    stopAll(message.reason || 'engineStop');
    return;
  }

  if (message.type === 'tool.request') {
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
  sendToEngine({
    type: 'tool.result',
    runId: request.runId,
    requestId: request.requestId,
    success,
    result,
    error
  });
  publish({ type: 'toolResult', request, success, result, error });
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
