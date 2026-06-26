/**
 * Bridge ↔ Stealth Core integration test.
 *
 * Requiere:
 *  - Bridge compilado (`dotnet build .\OneBrain.slnx --no-restore`)
 *  - Playwright Chromium instalado
 *  - Puerto 8787 libre
 *
 * Limitaciones:
 *  - Sin OPENAI_API_KEY real, el bridge rechaza el inicio de tareas stealth
 *    con "OpenAI API key missing for stealth mode.".
 *  - Este test verifica el handshake WebSocket y la respuesta del endpoint
 *    /api/runs; no ejecuta una tarea completa sin API key.
 */
import { spawn } from 'node:child_process';
import { readFile, writeFile } from 'node:fs/promises';
import { existsSync, readdirSync, statSync } from 'node:fs';
import { test } from 'node:test';
import assert from 'node:assert';
import path from 'node:path';
import os from 'node:os';

const REPO_ROOT = path.resolve('..');
const CONFIG_PATH = path.join(REPO_ROOT, 'config', 'chrome-lab.local.json');
const BRIDGE_URL = 'http://127.0.0.1:8787';
const WS_STEALTH_URL = 'ws://127.0.0.1:8787/ws/stealth';
const TIMEOUT_MS = 60000;

async function readConfig() {
  if (!existsSync(CONFIG_PATH)) return {};
  return JSON.parse(await readFile(CONFIG_PATH, 'utf-8'));
}

async function writeConfig(cfg) {
  await writeFile(CONFIG_PATH, JSON.stringify(cfg, null, 2));
}

function sleep(ms) {
  return new Promise(r => setTimeout(r, ms));
}

function captureProcessOutput(process) {
  const chunks = [];
  const onData = (data) => chunks.push(data.toString());
  process.stdout.on('data', onData);
  process.stderr.on('data', onData);
  return {
    getText: () => chunks.join(''),
    detach: () => {
      process.stdout.off('data', onData);
      process.stderr.off('data', onData);
    },
  };
}

function waitForLog(process, predicate, timeoutMs = TIMEOUT_MS) {
  return new Promise((resolve, reject) => {
    const chunks = [];
    const onData = (data) => {
      const text = data.toString();
      chunks.push(text);
      if (predicate(text)) {
        cleanup();
        resolve(chunks.join(''));
      }
    };
    const timer = setTimeout(() => {
      cleanup();
      reject(new Error('Timeout waiting for log. Captured:\n' + chunks.join('')));
    }, timeoutMs);
    const cleanup = () => {
      clearTimeout(timer);
      process.stdout.off('data', onData);
      process.stderr.off('data', onData);
    };
    process.stdout.on('data', onData);
    process.stderr.on('data', onData);
  });
}

async function postStealthTask(instruction) {
  const res = await fetch(`${BRIDGE_URL}/api/runs`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      Mode: 'stealth',
      Instruction: instruction,
    }),
  });
  return { status: res.status, body: await res.json().catch(() => ({})) };
}

async function healthCheck() {
  const res = await fetch(`${BRIDGE_URL}/health`);
  return res.ok ? res.json() : null;
}

function findChromiumExe() {
  const candidates = [
    path.join(os.homedir(), 'AppData', 'Local', 'ms-playwright'),
  ];
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

test('Bridge build exists', () => {
  const bridgeDll = path.join(
    REPO_ROOT,
    'src',
    'OneBrain.ChromeLab.Bridge',
    'bin',
    'Debug',
    'net11.0',
    'OneBrain.ChromeLab.Bridge.dll'
  );
  assert.ok(existsSync(bridgeDll), `Bridge DLL not found at ${bridgeDll}. Run dotnet build first.`);
});

test('Bridge WebSocket handshake with Stealth Engine', async () => {
  const originalConfig = await readConfig();
  const chromiumPath = findChromiumExe();
  assert.ok(chromiumPath, 'Playwright Chromium not found. Run npx playwright install chromium.');

  // Enable stealth mode for the test
  const testConfig = { ...originalConfig, stealth: { ...(originalConfig.stealth || {}), StealthEnabled: true } };
  await writeConfig(testConfig);
  const writtenConfig = await readConfig();
  assert.strictEqual(writtenConfig.stealth?.StealthEnabled, true, 'Config StealthEnabled was not written correctly');

  let bridgeProc = null;
  let stealthProc = null;
  let bridgeOut = null;
  let stealthOut = null;

  try {
    // Start bridge
    bridgeProc = spawn('dotnet', [
      path.join(REPO_ROOT, 'src', 'OneBrain.ChromeLab.Bridge', 'bin', 'Debug', 'net11.0', 'OneBrain.ChromeLab.Bridge.dll'),
    ], {
      cwd: REPO_ROOT,
      env: { ...process.env, ASPNETCORE_ENVIRONMENT: 'Development' },
      stdio: ['ignore', 'pipe', 'pipe'],
    });
    bridgeOut = captureProcessOutput(bridgeProc);

    // WebSocket endpoint is always mapped; wait for the app to start listening.
    await waitForLog(bridgeProc, (text) =>
      text.includes('Application started. Press Ctrl+C to shut down')
    );

    // Start stealth engine
    stealthProc = spawn('node', [path.join(REPO_ROOT, 'stealth-engine', 'src', 'index.js')], {
      cwd: REPO_ROOT,
      env: {
        ...process.env,
        NODAL_CLOAKBROWSER_RUNTIME_PATH: chromiumPath,
      },
      stdio: ['ignore', 'pipe', 'pipe'],
    });
    stealthOut = captureProcessOutput(stealthProc);

    await waitForLog(stealthProc, (text) =>
      text.includes('[StealthEngine] Connected')
    );

    // Verify /health reports a connected runner
    let health = null;
    for (let i = 0; i < 10; i++) {
      health = await healthCheck();
      if (health?.runnersConnected === 1) break;
      await sleep(500);
    }
    assert.ok(health, 'Bridge /health endpoint unreachable');
    assert.strictEqual(health.runnersConnected, 1, 'Bridge does not report a connected stealth runner');

    // Try to start a stealth task (will fail without OpenAI API key)
    const taskRes = await postStealthTask('Navegar a https://example.com y devolver el título');

    // Without API key the bridge returns 500 with "OpenAI API key missing for stealth mode."
    // With API key it would return 200 and dispatch stealth.task.
    assert.ok(
      taskRes.status === 200 || taskRes.status === 500,
      `Unexpected /api/runs status: ${taskRes.status}`
    );

    if (taskRes.status === 500) {
      console.log('[INTEGRATION] /api/runs returned 500 (expected without OPENAI_API_KEY):', taskRes.body);
    } else {
      assert.strictEqual(taskRes.body.status, 'running', 'Task did not start');
    }
  } catch (e) {
    console.error('\n==== BRIDGE OUTPUT ====\n', bridgeOut?.getText() || '(not captured)');
    console.error('\n==== STEALTH ENGINE OUTPUT ====\n', stealthOut?.getText() || '(not captured)');
    throw e;
  } finally {
    if (stealthProc) {
      stealthProc.kill('SIGTERM');
      await sleep(1000);
      if (!stealthProc.killed) stealthProc.kill('SIGKILL');
    }
    if (bridgeProc) {
      bridgeProc.kill('SIGTERM');
      await sleep(1000);
      if (!bridgeProc.killed) bridgeProc.kill('SIGKILL');
    }
    bridgeOut?.detach();
    stealthOut?.detach();
    // Restore original config
    await writeConfig(originalConfig);
  }
});
