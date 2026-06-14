const PROTOCOL_VERSION = 'chrome-lab-v1';
const DEFAULT_CONFIG = { host: '127.0.0.1', port: '8787' };

let socket = null;
let connected = false;
let stopped = false;
let currentRunId = '';
const sidePorts = new Set();
const queuedToolRequests = [];

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
      connect(message.config);
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
  disconnect('reconnect');
  stopped = false;
  const url = `ws://${config.host || DEFAULT_CONFIG.host}:${config.port || DEFAULT_CONFIG.port}/ws/extension`;
  socket = new WebSocket(url);
  socket.addEventListener('open', () => {
    connected = true;
    publishState('connected', `Connected to ${url}`);
    sendToEngine({
      type: 'extension.hello',
      protocolVersion: PROTOCOL_VERSION,
      clientId: chrome.runtime.id,
      extensionVersion: chrome.runtime.getManifest().version,
      browser: 'chrome'
    });
  });
  socket.addEventListener('message', (event) => handleEngineMessage(event.data));
  socket.addEventListener('close', () => {
    connected = false;
    publishState('disconnected', 'Bridge connection closed');
  });
  socket.addEventListener('error', () => {
    connected = false;
    publishState('error', 'Bridge WebSocket error');
  });
}

function disconnect(reason) {
  if (socket) {
    socket.close(1000, reason);
  }
  socket = null;
  connected = false;
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
  queuedToolRequests.length = 0;
  chrome.tabs.query({ active: true, currentWindow: true }, (tabs) => {
    for (const tab of tabs) {
      if (tab.id) {
        chrome.tabs.sendMessage(tab.id, { type: 'local.stop', protocolVersion: PROTOCOL_VERSION }).catch(() => {});
      }
    }
  });
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

    const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
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
  const allowed = new Set(['observePage', 'getCurrentTab', 'navigate', 'query', 'read', 'click', 'setValue', 'selectOption', 'scrollIntoView', 'waitForSelector', 'highlight', 'clearHighlight', 'pauseForHuman', 'stop']);
  if (!allowed.has(message.tool)) {
    return { ok: false, error: 'Tool not allowed' };
  }
  if (message.tool === 'navigate' && !allowedUrl(String((message.args || {}).url || ''))) {
    return { ok: false, error: 'URL rejected' };
  }
  return { ok: true };
}

async function navigate(url) {
  if (!allowedUrl(url)) {
    throw new Error('URL rejected');
  }
  const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
  if (!tab || !tab.id) {
    throw new Error('No active tab');
  }
  await chrome.tabs.update(tab.id, { url });
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
    const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
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
