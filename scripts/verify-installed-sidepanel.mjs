#!/usr/bin/env node
import { spawn } from 'node:child_process';
import { randomUUID } from 'node:crypto';
import { existsSync } from 'node:fs';
import { mkdir, mkdtemp, readFile, readdir, rm, writeFile } from 'node:fs/promises';
import http from 'node:http';
import net from 'node:net';
import os from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const repoRoot = path.resolve(__dirname, '..');
const extensionPath = path.join(repoRoot, 'browser-extension', 'onebrain-chrome-lab');
const manifestPath = path.join(extensionPath, 'manifest.json');
const outputDir = path.join(repoRoot, 'artifacts', 'local-verification');
const fixtureTitle = 'NODAL OS Browser Skills Fixture';
const demoMissionTitle = 'Repo Harness Installed Sidepanel Mission';
const demoMissionDescription = 'Repo-owned installed extension verification';
const runNote = 'Repo harness installed sidepanel note';
const timeoutMs = 30000;

const result = {
  status: 'RUNNING',
  decision: 'INSTALLED_SIDEPANEL_VERIFICATION_HARNESS_RUNNING',
  startedAt: new Date().toISOString(),
  browserUsed: '',
  extensionRegistered: false,
  extensionId: '',
  apiAvailability: {},
  missionFlow: {},
  browserSkills: {},
  elementCount: 0,
  frictionSummary: 'not evaluated',
  errors: [],
  checks: []
};

let chromeProcess = null;
let fixtureServer = null;
let profileDir = '';

main().catch(async (error) => {
  recordError(error);
  result.status = 'FAILED';
  result.decision = 'INSTALLED_SIDEPANEL_VERIFICATION_HARNESS_FAILED';
  await finish();
  process.exitCode = 1;
});

async function main() {
  const manifest = JSON.parse(await readFile(manifestPath, 'utf8'));
  addCheck('manifest.json readable', true, {
    name: manifest.name,
    sidePanelPath: manifest.side_panel && manifest.side_panel.default_path,
    permissions: manifest.permissions || []
  });

  const chromiumPath = await locatePlaywrightChromium();
  addCheck('Playwright Chromium executable found', Boolean(chromiumPath), {
    chromiumExecutable: chromiumPath ? path.basename(chromiumPath) : ''
  });
  if (!chromiumPath) {
    throw new Error('Playwright bundled Chromium executable was not found. Install Playwright browsers or set NODAL_CHROMIUM_PATH.');
  }

  const fixture = await startFixtureServer();
  result.fixture = {
    url: fixture.url
  };

  const debugPort = await getFreePort();
  profileDir = await mkdtemp(path.join(os.tmpdir(), 'nodal-installed-sidepanel-profile-'));
  chromeProcess = spawn(chromiumPath, [
    `--user-data-dir=${profileDir}`,
    `--disable-extensions-except=${extensionPath}`,
    `--load-extension=${extensionPath}`,
    `--remote-debugging-port=${debugPort}`,
    '--remote-debugging-address=127.0.0.1',
    '--no-first-run',
    '--no-default-browser-check',
    '--disable-background-networking',
    '--disable-component-update',
    '--disable-sync',
    '--disable-features=OptimizationGuideModelDownloading,OptimizationHintsFetching,AutofillServerCommunication',
    fixture.url
  ], {
    stdio: ['ignore', 'pipe', 'pipe'],
    windowsHide: true
  });

  const stderrLines = [];
  chromeProcess.stderr.on('data', (chunk) => {
    const text = String(chunk);
    for (const line of text.split(/\r?\n/)) {
      if (/extension|sidepanel|error|warning/i.test(line)) {
        stderrLines.push(redactPath(line));
      }
    }
  });

  const version = await waitForJson(`http://127.0.0.1:${debugPort}/json/version`, timeoutMs);
  result.browserUsed = version.Browser || 'Chromium';
  addCheck('CDP endpoint available', true, {
    browser: result.browserUsed
  });

  const browser = await CdpClient.connect(version.webSocketDebuggerUrl);
  try {
    const fixtureTarget = await waitForTarget(debugPort, (target) => target.type === 'page' && target.url === fixture.url);
    await browser.send('Target.activateTarget', { targetId: fixtureTarget.id });

    const extensionInfo = await findNodalExtension(debugPort);
    result.extensionRegistered = true;
    result.extensionId = redactExtensionId(extensionInfo.extensionId);
    addCheck('NODAL OS extension registered', true, {
      extensionId: result.extensionId,
      serviceWorker: 'service_worker.js'
    });

    const sidepanelUrl = `chrome-extension://${extensionInfo.extensionId}/sidepanel.html`;
    const created = await browser.send('Target.createTarget', { url: sidepanelUrl });
    const sidepanelTarget = await waitForTarget(debugPort, (target) => target.id === created.targetId || target.url === sidepanelUrl);
    const sidepanel = await connectToTarget(sidepanelTarget);
    try {
      await sidepanel.send('Runtime.enable');
      await sidepanel.send('Page.enable').catch(() => null);
      await delay(700);

      result.apiAvailability = await evaluate(sidepanel, `(() => ({
        chrome: typeof chrome,
        runtime: typeof chrome !== 'undefined' && typeof chrome.runtime,
        tabs: typeof chrome !== 'undefined' && typeof chrome.tabs,
        scripting: typeof chrome !== 'undefined' && typeof chrome.scripting,
        sidePanel: typeof chrome !== 'undefined' && typeof chrome.sidePanel,
        storage: typeof chrome !== 'undefined' && typeof chrome.storage,
        sidePanelSetOptions: typeof chrome !== 'undefined' && chrome.sidePanel ? typeof chrome.sidePanel.setOptions : 'undefined'
      }))()`);
      addCheck('sidepanel loaded with chrome APIs', apiCheck(result.apiAvailability), result.apiAvailability);

      await evaluate(sidepanel, `(() => {
        localStorage.removeItem('nodal-os.demoMissions.v1');
        localStorage.removeItem('nodal-os.browserSkills.snapshots.v1');
        localStorage.removeItem('nodal-os.demoGuidanceCollapsed.v1');
        return true;
      })()`);
      await sidepanel.send('Page.reload', { ignoreCache: true }).catch(() => null);
      await delay(1000);

      result.missionFlow = await runMissionFlow(sidepanel);
      addCheck('Mission Control flow works in installed extension page', result.missionFlow.ok, result.missionFlow);

      await browser.send('Target.activateTarget', { targetId: fixtureTarget.id });
      await delay(300);
      result.browserSkills = await runBrowserSkillsFlow(sidepanel);
      result.elementCount = result.browserSkills.afterIndex ? Number(result.browserSkills.afterIndex.elementCount) || 0 : 0;
      result.frictionSummary = result.browserSkills.afterIndex ? result.browserSkills.afterIndex.friction : 'not indexed';
      addCheck('Browser Skills captures active tab and indexes DOM', result.browserSkills.ok, result.browserSkills);
    } finally {
      sidepanel.close();
    }

    result.status = result.checks.every((check) => check.ok) ? 'PASS' : 'FAIL';
    result.decision = result.status === 'PASS'
      ? 'INSTALLED_SIDEPANEL_VERIFICATION_HARNESS_PASS'
      : 'INSTALLED_SIDEPANEL_VERIFICATION_HARNESS_FAIL';
    result.chromeStderrExtensionLines = stderrLines.slice(-20);
  } finally {
    browser.close();
  }

  await finish();
  if (result.status !== 'PASS') {
    process.exitCode = 1;
  }
}

async function runMissionFlow(sidepanel) {
  return evaluate(sidepanel, `(async () => {
    const delay = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
    const title = document.getElementById('missionTitleInput');
    const description = document.getElementById('missionDescriptionInput');
    title.value = ${JSON.stringify(demoMissionTitle)};
    description.value = ${JSON.stringify(demoMissionDescription)};
    document.getElementById('missionCreateForm').requestSubmit();
    await delay(250);
    document.getElementById('editMissionBtn').click();
    await delay(150);
    document.getElementById('editMissionTitleInput').value = ${JSON.stringify(demoMissionTitle)};
    document.getElementById('editMissionDescriptionInput').value = ${JSON.stringify(demoMissionDescription)};
    document.getElementById('saveMissionBtn').click();
    await delay(200);
    document.getElementById('runSafeDemoBtn').click();
    await delay(300);
    document.getElementById('runNoteInput').value = ${JSON.stringify(runNote)};
    document.getElementById('saveRunNoteBtn').click();
    await delay(200);
    document.getElementById('copyDemoReportBtn').click();
    await delay(200);
    const store = JSON.parse(localStorage.getItem('nodal-os.demoMissions.v1') || '{}');
    const missions = Array.isArray(store.missions) ? store.missions : [];
    const mission = missions.find((item) => item.title === ${JSON.stringify(demoMissionTitle)}) || missions[0] || null;
    const run = mission && Array.isArray(mission.runs) ? mission.runs[0] : null;
    return {
      ok: Boolean(mission && run && run.status === 'completed'),
      missionTitle: mission ? mission.title : '',
      missionCount: missions.length,
      runCount: mission && Array.isArray(mission.runs) ? mission.runs.length : 0,
      selectedRunId: store.selectedRunId || '',
      runNote: run ? run.note || '' : '',
      historyText: document.getElementById('demoRunHistory').innerText.slice(0, 600),
      reportTextPresent: Boolean(document.getElementById('demoTechnicalReport').innerText.includes('NODAL OS'))
    };
  })()`);
}

async function runBrowserSkillsFlow(sidepanel) {
  return evaluate(sidepanel, `(async () => {
    const delay = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
    document.getElementById('captureBrowserTabBtn').click();
    await delay(1000);
    const afterCapture = {
      status: document.getElementById('browserSkillStatus').innerText,
      url: document.getElementById('browserSkillUrl').innerText,
      title: document.getElementById('browserSkillTitleValue').innerText,
      elementCount: document.getElementById('browserSkillElementCount').innerText,
      friction: document.getElementById('browserSkillFriction').innerText
    };
    document.getElementById('indexBrowserPageBtn').click();
    await delay(1500);
    const afterIndex = {
      status: document.getElementById('browserSkillStatus').innerText,
      url: document.getElementById('browserSkillUrl').innerText,
      title: document.getElementById('browserSkillTitleValue').innerText,
      elementCount: document.getElementById('browserSkillElementCount').innerText,
      friction: document.getElementById('browserSkillFriction').innerText,
      indexedText: document.getElementById('browserIndexedElements').innerText.slice(0, 900),
      evidenceText: document.getElementById('browserEvidencePanel').innerText.slice(0, 900),
      historyText: document.getElementById('browserSnapshotHistory').innerText.slice(0, 900)
    };
    document.getElementById('copyBrowserSkillSummaryBtn').click();
    await delay(200);
    const rawStore = JSON.parse(localStorage.getItem('nodal-os.browserSkills.snapshots.v1') || '{}');
    const snapshots = Array.isArray(rawStore.snapshots) ? rawStore.snapshots : [];
    return {
      ok: afterIndex.status === 'Página indexada' && Number(afterIndex.elementCount) > 0 && snapshots.length > 0,
      afterCapture,
      afterIndex,
      snapshotCount: snapshots.length,
      selectedSnapshotId: rawStore.selectedSnapshotId || '',
      snapshotSummaries: snapshots.slice(0, 3).map((snapshot) => ({
        title: snapshot.title || '',
        url: snapshot.url || '',
        status: snapshot.status || '',
        elements: Array.isArray(snapshot.elements) ? snapshot.elements.length : 0,
        friction: Array.isArray(snapshot.frictionEvents) ? snapshot.frictionEvents.map((item) => item.label || item.type) : []
      }))
    };
  })()`);
}

async function findNodalExtension(debugPort) {
  const deadline = Date.now() + timeoutMs;
  let lastTargets = [];
  while (Date.now() < deadline) {
    const targets = await getJson(`http://127.0.0.1:${debugPort}/json/list`);
    lastTargets = targets;
    for (const target of targets.filter((item) => item.type === 'service_worker' && /^chrome-extension:\/\//.test(item.url))) {
      const client = await connectToTarget(target);
      try {
        await client.send('Runtime.enable');
        const manifest = await evaluate(client, `(() => chrome.runtime.getManifest())()`);
        if (manifest && manifest.name === 'NODAL OS') {
          return {
            target,
            manifest,
            extensionId: extensionIdFromUrl(target.url)
          };
        }
      } catch {
        // Non-NODAL component extensions are expected in Chromium.
      } finally {
        client.close();
      }
    }
    await delay(300);
  }
  throw new Error(`NODAL OS extension service worker was not discovered. Targets: ${JSON.stringify(lastTargets.map((target) => ({ type: target.type, url: target.url })))}`);
}

async function connectToTarget(target) {
  if (!target.webSocketDebuggerUrl) {
    throw new Error(`Target has no websocket debugger URL: ${target.url}`);
  }
  return CdpClient.connect(target.webSocketDebuggerUrl);
}

async function waitForTarget(debugPort, predicate) {
  const deadline = Date.now() + timeoutMs;
  let targets = [];
  while (Date.now() < deadline) {
    targets = await getJson(`http://127.0.0.1:${debugPort}/json/list`);
    const match = targets.find(predicate);
    if (match) {
      return match;
    }
    await delay(250);
  }
  throw new Error(`Timed out waiting for target. Last targets: ${JSON.stringify(targets.map((target) => ({ type: target.type, url: target.url })))}`);
}

async function evaluate(client, expression) {
  const wrapped = typeof expression === 'function' ? `(${expression})()` : expression;
  const response = await client.send('Runtime.evaluate', {
    expression: typeof wrapped === 'string' ? wrapped : String(wrapped),
    awaitPromise: true,
    returnByValue: true,
    userGesture: true
  });
  if (response.exceptionDetails) {
    const details = response.exceptionDetails.exception && response.exceptionDetails.exception.description
      ? response.exceptionDetails.exception.description
      : response.exceptionDetails.text || 'Runtime.evaluate failed';
    throw new Error(details);
  }
  return response.result ? response.result.value : undefined;
}

class CdpClient {
  static connect(url) {
    return new Promise((resolve, reject) => {
      const ws = new WebSocket(url);
      const client = new CdpClient(ws);
      ws.addEventListener('open', () => resolve(client), { once: true });
      ws.addEventListener('error', (event) => reject(new Error(`CDP websocket error: ${event.message || 'unknown'}`)), { once: true });
    });
  }

  constructor(ws) {
    this.ws = ws;
    this.nextId = 1;
    this.pending = new Map();
    ws.addEventListener('message', (event) => {
      const message = JSON.parse(event.data);
      if (!message.id || !this.pending.has(message.id)) {
        return;
      }
      const { resolve, reject } = this.pending.get(message.id);
      this.pending.delete(message.id);
      if (message.error) {
        reject(new Error(`${message.error.message}${message.error.data ? `: ${message.error.data}` : ''}`));
      } else {
        resolve(message.result || {});
      }
    });
    ws.addEventListener('close', () => {
      for (const { reject } of this.pending.values()) {
        reject(new Error('CDP websocket closed'));
      }
      this.pending.clear();
    });
  }

  send(method, params = {}) {
    const id = this.nextId++;
    const payload = JSON.stringify({ id, method, params });
    return new Promise((resolve, reject) => {
      this.pending.set(id, { resolve, reject });
      this.ws.send(payload);
      setTimeout(() => {
        if (!this.pending.has(id)) {
          return;
        }
        this.pending.delete(id);
        reject(new Error(`CDP command timed out: ${method}`));
      }, timeoutMs).unref();
    });
  }

  close() {
    try {
      this.ws.close();
    } catch {
      // Ignore cleanup errors.
    }
  }
}

async function locatePlaywrightChromium() {
  const explicit = process.env.NODAL_CHROMIUM_PATH;
  if (explicit && existsSync(explicit)) {
    return explicit;
  }

  const candidates = [];
  const roots = [
    path.join(os.homedir(), 'AppData', 'Local', 'ms-playwright'),
    process.env.LOCALAPPDATA ? path.join(process.env.LOCALAPPDATA, 'ms-playwright') : ''
  ].filter(Boolean);

  for (const root of roots) {
    if (!existsSync(root)) {
      continue;
    }
    for (const entry of await readdir(root, { withFileTypes: true })) {
      if (!entry.isDirectory() || !entry.name.startsWith('chromium-')) {
        continue;
      }
      const chromePath = path.join(root, entry.name, 'chrome-win64', 'chrome.exe');
      if (existsSync(chromePath)) {
        candidates.push(chromePath);
      }
    }
  }

  candidates.sort().reverse();
  return candidates[0] || '';
}

async function startFixtureServer() {
  const port = await getFreePort();
  fixtureServer = http.createServer((request, response) => {
    if (request.url !== '/fixture') {
      response.writeHead(302, { Location: '/fixture' });
      response.end();
      return;
    }
    response.writeHead(200, {
      'content-type': 'text/html; charset=utf-8',
      'cache-control': 'no-store'
    });
    response.end(`<!doctype html>
<html>
<head><title>${fixtureTitle}</title></head>
<body>
  <main>
    <h1>${fixtureTitle}</h1>
    <a href="#alpha">Alpha link</a>
    <button id="cta">CTA Button</button>
    <form aria-label="Core Local Button">
      <label>Email <input id="email" type="email" placeholder="email@example.test"></label>
      <textarea name="notes" aria-label="notes"></textarea>
      <select name="tier" aria-label="tier"><option>local</option></select>
      <button type="submit">Submit</button>
    </form>
    <button type="button">Local Button</button>
    <div class="captcha-marker">captcha marker</div>
  </main>
</body>
</html>`);
  });
  await new Promise((resolve) => fixtureServer.listen(port, '127.0.0.1', resolve));
  return {
    port,
    url: `http://127.0.0.1:${port}/fixture`
  };
}

function getFreePort() {
  return new Promise((resolve, reject) => {
    const server = net.createServer();
    server.listen(0, '127.0.0.1', () => {
      const address = server.address();
      const port = address.port;
      server.close(() => resolve(port));
    });
    server.on('error', reject);
  });
}

async function waitForJson(url, timeout) {
  const deadline = Date.now() + timeout;
  let lastError = null;
  while (Date.now() < deadline) {
    try {
      return await getJson(url);
    } catch (error) {
      lastError = error;
      await delay(250);
    }
  }
  throw lastError || new Error(`Timed out waiting for ${url}`);
}

function getJson(url) {
  return new Promise((resolve, reject) => {
    http.get(url, (response) => {
      let body = '';
      response.setEncoding('utf8');
      response.on('data', (chunk) => {
        body += chunk;
      });
      response.on('end', () => {
        if (response.statusCode < 200 || response.statusCode >= 300) {
          reject(new Error(`HTTP ${response.statusCode} for ${url}`));
          return;
        }
        try {
          resolve(JSON.parse(body));
        } catch (error) {
          reject(error);
        }
      });
    }).on('error', reject);
  });
}

function apiCheck(api) {
  return api
    && api.chrome === 'object'
    && api.runtime === 'object'
    && api.tabs === 'object'
    && api.scripting === 'object'
    && api.sidePanel === 'object'
    && api.storage === 'object';
}

function extensionIdFromUrl(url) {
  const match = String(url).match(/^chrome-extension:\/\/([^/]+)/);
  if (!match) {
    throw new Error(`Could not parse extension id from ${url}`);
  }
  return match[1];
}

function redactExtensionId(value) {
  const text = String(value || '');
  return text ? `${text.slice(0, 6)}...${text.slice(-4)}` : '';
}

function redactPath(value) {
  return String(value || '')
    .replaceAll(repoRoot, '<repo>')
    .replaceAll(os.homedir(), '<home>')
    .replace(/[A-Z]:\\Users\\[^\\\s]+/gi, '<user-home>');
}

function recordError(error) {
  result.errors.push({
    message: redactPath(error && error.message ? error.message : String(error)),
    stack: error && error.stack ? redactPath(error.stack).split('\n').slice(0, 4) : []
  });
}

function addCheck(name, ok, details = {}) {
  result.checks.push({
    name,
    ok: Boolean(ok),
    details: redactObject(details)
  });
}

function redactObject(value) {
  if (Array.isArray(value)) {
    return value.map(redactObject);
  }
  if (value && typeof value === 'object') {
    return Object.fromEntries(Object.entries(value).map(([key, item]) => [key, redactObject(item)]));
  }
  if (typeof value === 'string') {
    return redactPath(value);
  }
  return value;
}

function delay(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

async function finish() {
  result.finishedAt = new Date().toISOString();
  result.generatedBy = 'scripts/verify-installed-sidepanel.mjs';
  result.outputPolicy = 'local redacted evidence; artifacts/ is gitignored';
  await cleanup();
  await mkdir(outputDir, { recursive: true });
  const outputPath = path.join(outputDir, `installed-sidepanel-${new Date().toISOString().replace(/[:.]/g, '-')}.redacted.json`);
  await writeFile(outputPath, `${JSON.stringify(result, null, 2)}\n`, 'utf8');
  console.log(`status=${result.status}`);
  console.log(`decision=${result.decision}`);
  console.log(`evidence=${outputPath}`);
}

async function cleanup() {
  if (chromeProcess && !chromeProcess.killed) {
    chromeProcess.kill();
  }
  if (fixtureServer) {
    await new Promise((resolve) => fixtureServer.close(resolve));
  }
  if (profileDir && profileDir.startsWith(os.tmpdir())) {
    await rm(profileDir, { recursive: true, force: true }).catch(() => null);
  }
}
