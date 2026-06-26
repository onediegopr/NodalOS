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
const workspaceFixtureDir = path.join(outputDir, 'workspace-fixture');
const fixtureTitle = 'NODAL OS Browser Skills Fixture';
const demoMissionTitle = 'Repo Harness Installed Sidepanel Mission';
const demoMissionDescription = 'Repo-owned installed extension verification';
const runNote = 'Repo harness installed sidepanel note';
const timeoutMs = 30000;
const workspaceHarnessMaxFiles = 80;
const workspaceHarnessMaxDepth = 5;
const workspaceHarnessIgnoredDirs = new Set([
  '.cache',
  '.git',
  '.idea',
  '.next',
  '.turbo',
  '.vs',
  'bin',
  'build',
  'coverage',
  'dist',
  'node_modules',
  'obj'
]);
const workspaceHarnessIgnoredFiles = new Set(['.git']);
const workspaceHarnessIgnoredFilePrefixes = ['.env'];
const workspaceHarnessProtectedPrefixes = [
  'stealth-engine/',
  'stealth-panel/',
  'src/onebrain.chromelab.bridge/stealth/',
  'src/onebrain.chromelab.bridge/sessions/'
];
const workspaceHarnessProtectedPaths = new Set([
  'changelog.md',
  'docker-compose.yml',
  'docs/architecture.md',
  'docs/configuration.md',
  'docs/deployment.md',
  'docs/operations.md',
  'docs/roadmap.md',
  'docs/stealth-audit-report.md',
  'docs/stealth-engine-design.md',
  'docs/stealth-reaudit-report.md',
  'docs/unified-friction-integration-design.md',
  'scripts/deploy.ps1',
  'scripts/stop.ps1',
  'src/onebrain.browserexecutor.cdp/browsercredentialboundaryservice.cs',
  'src/onebrain.browserexecutor.contracts/browsercredentialboundarycontracts.cs',
  'src/onebrain.chromelab.bridge/chromelaboptions.cs',
  'src/onebrain.chromelab.bridge/chromelabprotocol.cs',
  'src/onebrain.chromelab.bridge/dockerfile',
  'src/onebrain.chromelab.bridge/program.cs'
]);

const result = {
  status: 'RUNNING',
  decision: 'INSTALLED_SIDEPANEL_VERIFICATION_HARNESS_RUNNING',
  startedAt: new Date().toISOString(),
  browserUsed: '',
  extensionRegistered: false,
  extensionId: '',
  apiAvailability: {},
  missionFlow: {},
  workspaceUnderstanding: {},
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
  const workspaceFixture = await createWorkspaceFixture();
  const repoWorkspaceFiles = await collectRepoWorkspaceFiles();
  const workspaceFiles = repoWorkspaceFiles.length > 0 ? repoWorkspaceFiles : workspaceFixture.files;
  result.fixture = {
    url: fixture.url,
    workspaceSelectionMode: repoWorkspaceFiles.length > 0 ? 'repo-limited-real-files' : 'fixture-fallback',
    workspaceFiles: workspaceFiles.length
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
        localStorage.removeItem('nodal-os.workspaceUnderstanding.v1');
        localStorage.removeItem('nodal-os.demoGuidanceCollapsed.v1');
        return true;
      })()`);
      await sidepanel.send('Page.reload', { ignoreCache: true }).catch(() => null);
      await delay(1000);

      result.missionFlow = await runMissionFlow(sidepanel);
      addCheck('Mission Control flow works in installed extension page', result.missionFlow.ok, result.missionFlow);

      result.workspaceUnderstanding = await runWorkspaceUnderstandingCompatibilityFlow(sidepanel, workspaceFiles);
      addCheck('Workspace Understanding compatibility selection works in installed extension page', result.workspaceUnderstanding.ok, result.workspaceUnderstanding);

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

async function runWorkspaceUnderstandingSurfaceCheck(sidepanel) {
  return evaluate(sidepanel, `(async () => {
    const workspace = document.getElementById('workspaceUnderstanding');
    const openButton = document.getElementById('openWorkspaceBtn');
    const evidence = document.getElementById('workspaceEvidence');
    const pickerAvailable = typeof window.showDirectoryPicker === 'function';
    const storeBefore = localStorage.getItem('nodal-os.workspaceUnderstanding.v1');
    return {
      ok: Boolean(workspace && openButton && evidence && workspace.innerText.includes('Proyecto activo')),
      pickerAvailable,
      title: workspace ? workspace.querySelector('h2').innerText : '',
      openButton: openButton ? openButton.innerText : '',
      statusText: document.getElementById('workspaceStatus') ? document.getElementById('workspaceStatus').innerText : '',
      evidenceText: evidence ? evidence.innerText.slice(0, 500) : '',
      storePresentBeforePicker: Boolean(storeBefore)
    };
  })()`);
}

async function runWorkspaceUnderstandingCompatibilityFlow(sidepanel, workspaceFiles) {
  await sidepanel.send('DOM.enable');
  const documentResult = await sidepanel.send('DOM.getDocument', { depth: 1, pierce: true });
  const inputResult = await sidepanel.send('DOM.querySelector', {
    nodeId: documentResult.root.nodeId,
    selector: '#workspaceDirectoryInput'
  });
  if (!inputResult.nodeId) {
    return {
      ok: false,
      error: 'workspaceDirectoryInput not found'
    };
  }

  await evaluate(sidepanel, `(() => {
    const input = document.getElementById('workspaceDirectoryInput');
    if (input) {
      input.removeAttribute('webkitdirectory');
      input.removeAttribute('directory');
      input.setAttribute('multiple', '');
      input.dataset.harnessFileListMode = 'true';
    }
    return true;
  })()`);
  await sidepanel.send('DOM.setFileInputFiles', {
    nodeId: inputResult.nodeId,
    files: workspaceFiles
  });
  const fileInputState = await evaluate(sidepanel, `(() => {
    const input = document.getElementById('workspaceDirectoryInput');
    return {
      fileCount: input && input.files ? input.files.length : 0,
      firstName: input && input.files && input.files[0] ? input.files[0].name : '',
      firstRelativePath: input && input.files && input.files[0] ? input.files[0].webkitRelativePath : ''
    };
  })()`);
  await evaluate(sidepanel, `(() => {
    const input = document.getElementById('workspaceDirectoryInput');
    if (input) {
      input.dispatchEvent(new Event('input', { bubbles: true }));
      input.dispatchEvent(new Event('change', { bubbles: true }));
    }
    return true;
  })()`);
  await delay(700);
  const planClearState = await evaluate(sidepanel, `(async () => {
    const delay = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
    const clear = document.getElementById('clearMissionPlanBtn');
    if (clear) clear.click();
    await delay(250);
    const missionStore = JSON.parse(localStorage.getItem('nodal-os.demoMissions.v1') || '{}');
    const mission = Array.isArray(missionStore.missions) ? missionStore.missions.find((item) => item.id === missionStore.activeMissionId) || missionStore.missions[0] : null;
    return {
      clearButtonPresent: Boolean(clear),
      planCleared: Boolean(mission && !mission.plan),
      taskGraphText: document.getElementById('missionTaskGraph') ? document.getElementById('missionTaskGraph').innerText.slice(0, 300) : ''
    };
  })()`);
  await evaluate(sidepanel, `(() => {
    const regenerate = document.getElementById('regenerateMissionPlanBtn');
    const copy = document.getElementById('copyMissionPlanBtn');
    if (regenerate) regenerate.click();
    if (copy) copy.click();
    return true;
  })()`);
  await delay(400);
  const proposalClearState = await evaluate(sidepanel, `(async () => {
    const delay = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
    const generate = document.getElementById('generateProposalBtn');
    const copy = document.getElementById('copyProposalBtn');
    const review = document.getElementById('reviewProposalBtn');
    const clear = document.getElementById('clearProposalBtn');
    if (generate) generate.click();
    await delay(250);
    if (copy) copy.click();
    await delay(150);
    if (review) review.click();
    await delay(150);
    if (clear) clear.click();
    await delay(250);
    const missionStore = JSON.parse(localStorage.getItem('nodal-os.demoMissions.v1') || '{}');
    const mission = Array.isArray(missionStore.missions) ? missionStore.missions.find((item) => item.id === missionStore.activeMissionId) || missionStore.missions[0] : null;
    return {
      generateButtonPresent: Boolean(generate),
      copyButtonPresent: Boolean(copy),
      reviewButtonPresent: Boolean(review),
      clearButtonPresent: Boolean(clear),
      proposalCleared: Boolean(mission && !mission.proposal),
      proposalText: document.getElementById('missionProposalCard') ? document.getElementById('missionProposalCard').innerText.slice(0, 500) : ''
    };
  })()`);
  await evaluate(sidepanel, `(() => {
    const generate = document.getElementById('generateProposalBtn');
    const copy = document.getElementById('copyProposalBtn');
    const review = document.getElementById('reviewProposalBtn');
    if (generate) generate.click();
    if (copy) copy.click();
    if (review) review.click();
    return true;
  })()`);
  await delay(400);
  const candidateClearState = await evaluate(sidepanel, `(async () => {
    const delay = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
    const generate = document.getElementById('generateCandidatesBtn');
    const copy = document.getElementById('copyCandidatesBtn');
    const review = document.getElementById('reviewCandidatesBtn');
    const clear = document.getElementById('clearCandidatesBtn');
    if (generate) generate.click();
    await delay(250);
    if (copy) copy.click();
    await delay(150);
    if (review) review.click();
    await delay(150);
    if (clear) clear.click();
    await delay(250);
    const missionStore = JSON.parse(localStorage.getItem('nodal-os.demoMissions.v1') || '{}');
    const mission = Array.isArray(missionStore.missions) ? missionStore.missions.find((item) => item.id === missionStore.activeMissionId) || missionStore.missions[0] : null;
    return {
      generateButtonPresent: Boolean(generate),
      copyButtonPresent: Boolean(copy),
      reviewButtonPresent: Boolean(review),
      clearButtonPresent: Boolean(clear),
      candidatesCleared: Boolean(mission && Array.isArray(mission.changeCandidates) && mission.changeCandidates.length === 0),
      candidateText: document.getElementById('changeCandidateCard') ? document.getElementById('changeCandidateCard').innerText.slice(0, 500) : ''
    };
  })()`);
  await evaluate(sidepanel, `(() => {
    const generate = document.getElementById('generateCandidatesBtn');
    const copy = document.getElementById('copyCandidatesBtn');
    const review = document.getElementById('reviewCandidatesBtn');
    const run = document.getElementById('runSafeDemoBtn');
    if (generate) generate.click();
    if (copy) copy.click();
    if (review) review.click();
    if (run) run.click();
    return true;
  })()`);
  await delay(700);

  return evaluate(sidepanel, `(() => {
    const workspace = document.getElementById('workspaceUnderstanding');
    const evidence = document.getElementById('workspaceEvidence');
    const store = JSON.parse(localStorage.getItem('nodal-os.workspaceUnderstanding.v1') || '{}');
    const context = store.context || {};
    const evidenceText = evidence ? evidence.innerText.slice(0, 900) : '';
    const treeText = document.getElementById('workspaceTree') ? document.getElementById('workspaceTree').innerText.slice(0, 900) : '';
    const keyFilesText = document.getElementById('workspaceKeyFiles') ? document.getElementById('workspaceKeyFiles').innerText.slice(0, 900) : '';
    const stacksText = document.getElementById('workspaceStack') ? document.getElementById('workspaceStack').innerText : '';
    const missionStore = JSON.parse(localStorage.getItem('nodal-os.demoMissions.v1') || '{}');
    const mission = Array.isArray(missionStore.missions) ? missionStore.missions.find((item) => item.id === missionStore.activeMissionId) || missionStore.missions[0] : null;
    const selectedRun = mission && Array.isArray(mission.runs)
      ? mission.runs.find((item) => item.id === missionStore.selectedRunId) || mission.runs[0]
      : null;
    const plan = mission && mission.plan ? mission.plan : null;
    const runPlan = selectedRun && selectedRun.plan ? selectedRun.plan : null;
    const proposal = mission && mission.proposal ? mission.proposal : null;
    const runProposal = selectedRun && selectedRun.proposal ? selectedRun.proposal : null;
    const candidates = mission && Array.isArray(mission.changeCandidates) ? mission.changeCandidates : [];
    const runCandidates = selectedRun && Array.isArray(selectedRun.changeCandidates) ? selectedRun.changeCandidates : [];
    return {
      ok: Boolean(
        workspace
        && store.name
        && store.source === 'file-directory-input'
        && context.readOnly === true
        && context.commandsExecuted === false
        && context.filesModified === false
        && Number(store.counts && store.counts.files) > 0
        && plan
        && plan.tasks
        && plan.tasks.length >= 3
        && runPlan
        && runPlan.tasks
        && runPlan.tasks.length >= 3
        && ${JSON.stringify(planClearState)}.clearButtonPresent === true
        && ${JSON.stringify(planClearState)}.planCleared === true
        && proposal
        && proposal.tasks
        && proposal.tasks.length >= 3
        && proposal.status === 'Revisado'
        && proposal.readOnly === true
        && proposal.diffGenerated === false
        && proposal.commandsExecuted === false
        && proposal.filesModified === false
        && runProposal
        && runProposal.tasks
        && runProposal.tasks.length >= 3
        && runProposal.diffGenerated === false
        && ${JSON.stringify(proposalClearState)}.generateButtonPresent === true
        && ${JSON.stringify(proposalClearState)}.copyButtonPresent === true
        && ${JSON.stringify(proposalClearState)}.reviewButtonPresent === true
        && ${JSON.stringify(proposalClearState)}.clearButtonPresent === true
        && ${JSON.stringify(proposalClearState)}.proposalCleared === true
        && candidates.length >= 2
        && candidates.every((candidate) => candidate.status === 'Revisado')
        && candidates.every((candidate) => candidate.readOnly === true)
        && candidates.every((candidate) => candidate.diffGenerated === false)
        && candidates.every((candidate) => candidate.patchGenerated === false)
        && candidates.every((candidate) => candidate.commandsExecuted === false)
        && candidates.every((candidate) => candidate.filesModified === false)
        && runCandidates.length >= 2
        && runCandidates.every((candidate) => candidate.patchGenerated === false)
        && ${JSON.stringify(candidateClearState)}.generateButtonPresent === true
        && ${JSON.stringify(candidateClearState)}.copyButtonPresent === true
        && ${JSON.stringify(candidateClearState)}.reviewButtonPresent === true
        && ${JSON.stringify(candidateClearState)}.clearButtonPresent === true
        && ${JSON.stringify(candidateClearState)}.candidatesCleared === true
        && evidenceText.includes('No se ejecutaron comandos')
        && evidenceText.includes('No se modificaron archivos')
      ),
      pickerAvailable: typeof window.showDirectoryPicker === 'function',
      compatibleButton: document.getElementById('openWorkspaceFallbackBtn') ? document.getElementById('openWorkspaceFallbackBtn').innerText : '',
      inputPresent: Boolean(document.getElementById('workspaceDirectoryInput')),
      harnessFileListMode: document.getElementById('workspaceDirectoryInput') ? document.getElementById('workspaceDirectoryInput').dataset.harnessFileListMode === 'true' : false,
      fileInputState: ${JSON.stringify(fileInputState)},
      title: workspace ? workspace.querySelector('h2').innerText : '',
      statusText: document.getElementById('workspaceStatus') ? document.getElementById('workspaceStatus').innerText : '',
      readModeText: document.getElementById('workspaceReadMode') ? document.getElementById('workspaceReadMode').innerText : '',
      name: store.name || '',
      source: store.source || '',
      contextSource: context.source || '',
      fileCount: store.counts ? store.counts.files : 0,
      folderCount: store.counts ? store.counts.folders : 0,
      stacksText,
      keyFilesText,
      treeText,
      evidenceText,
      planTaskCount: plan && Array.isArray(plan.tasks) ? plan.tasks.length : 0,
      planClearState: ${JSON.stringify(planClearState)},
      proposalTaskCount: proposal && Array.isArray(proposal.tasks) ? proposal.tasks.length : 0,
      proposalStatus: proposal ? proposal.status : '',
      proposalClearState: ${JSON.stringify(proposalClearState)},
      proposalText: document.getElementById('missionProposalCard') ? document.getElementById('missionProposalCard').innerText.slice(0, 900) : '',
      candidateCount: candidates.length,
      runCandidateCount: runCandidates.length,
      candidateClearState: ${JSON.stringify(candidateClearState)},
      candidateText: document.getElementById('changeCandidateCard') ? document.getElementById('changeCandidateCard').innerText.slice(0, 900) : '',
      taskGraphText: document.getElementById('missionTaskGraph') ? document.getElementById('missionTaskGraph').innerText.slice(0, 900) : '',
      planContextText: document.getElementById('missionPlanContext') ? document.getElementById('missionPlanContext').innerText : '',
      runHasPlan: Boolean(runPlan),
      runHasProposal: Boolean(runProposal),
      runHasMissionContext: Boolean(selectedRun && selectedRun.missionContext),
      proposalReadOnly: proposal ? proposal.readOnly === true : false,
      proposalDiffGenerated: proposal ? proposal.diffGenerated === true : null,
      proposalCommandsExecuted: proposal ? proposal.commandsExecuted === true : null,
      proposalFilesModified: proposal ? proposal.filesModified === true : null,
      candidatesReadOnly: candidates.every((candidate) => candidate.readOnly === true),
      candidatesDiffGenerated: candidates.some((candidate) => candidate.diffGenerated === true),
      candidatesPatchGenerated: candidates.some((candidate) => candidate.patchGenerated === true),
      candidatesCommandsExecuted: candidates.some((candidate) => candidate.commandsExecuted === true),
      candidatesFilesModified: candidates.some((candidate) => candidate.filesModified === true),
      readOnly: context.readOnly === true,
      commandsExecuted: context.commandsExecuted === true,
      filesModified: context.filesModified === true
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
    document.getElementById('copyCdpBrowserSkillSummaryBtn').click();
    await delay(200);
    const cdpSurface = {
      present: Boolean(document.getElementById('copyCdpBrowserSkillSummaryBtn')),
      runtimeLabel: document.getElementById('cdpRuntimeLabel').innerText,
      status: document.getElementById('cdpBrowserSkillStatus').innerText,
      extensionMode: document.getElementById('cdpExtensionMode').innerText,
      elementCount: document.getElementById('cdpElementCount').innerText,
      frictionCount: document.getElementById('cdpFrictionCount').innerText,
      actionMapCount: document.getElementById('cdpActionMapCount').innerText,
      source: document.getElementById('cdpSourceState').innerText,
      domIndex: document.getElementById('cdpDomIndexState').innerText,
      evidence: document.getElementById('cdpEvidenceState').innerText,
      screenshot: document.getElementById('cdpScreenshotState').innerText,
      extensionFree: document.getElementById('cdpExtensionUsedState').innerText,
      systemBrowserFree: document.getElementById('cdpSystemBrowserState').innerText,
      externalNavigationBlocked: document.getElementById('cdpExternalNavState').innerText,
      filesUnmodified: document.getElementById('cdpFilesModifiedState').innerText,
      copiedSummary: window.__nodalLastCdpBrowserSkillsSummary || ''
    };
    cdpSurface.ok = cdpSurface.runtimeLabel === 'CloakBrowser CDP'
      && cdpSurface.source === 'cloakbrowser-cdp-direct'
      && cdpSurface.extensionMode.includes('legacy')
      && cdpSurface.extensionFree === 'true'
      && cdpSurface.systemBrowserFree === 'true'
      && cdpSurface.externalNavigationBlocked === 'true'
      && cdpSurface.filesUnmodified === 'true'
      && cdpSurface.copiedSummary.includes('extensionUsed: false')
      && cdpSurface.copiedSummary.includes('systemBrowserUsed: false')
      && !/raw html|<html|document\.cookie|\blocalStorage\b|\bsessionStorage\b/i.test(cdpSurface.copiedSummary);
    const rawStore = JSON.parse(localStorage.getItem('nodal-os.browserSkills.snapshots.v1') || '{}');
    const snapshots = Array.isArray(rawStore.snapshots) ? rawStore.snapshots : [];
    return {
      ok: afterIndex.status === 'Página indexada' && Number(afterIndex.elementCount) > 0 && snapshots.length > 0 && cdpSurface.ok,
      afterCapture,
      afterIndex,
      cdpSurface,
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

async function createWorkspaceFixture() {
  await rm(workspaceFixtureDir, { recursive: true, force: true });
  const files = [
    ['package.json', '{ "name": "nodal-os-workspace-fixture", "private": true }\n'],
    ['tsconfig.json', '{ "compilerOptions": { "target": "ES2022" } }\n'],
    ['README.md', '# NODAL OS Workspace Fixture\n'],
    ['NodalOS.slnx', '<Solution></Solution>\n'],
    ['playwright.config.ts', 'export default {};\n'],
    ['Dockerfile', 'FROM scratch\n'],
    ['browser-extension/onebrain-chrome-lab/manifest.json', '{ "manifest_version": 3, "name": "Fixture Extension" }\n'],
    ['tests/sample.test.cs', 'namespace Fixture.Tests; public sealed class SampleTest {}\n'],
    ['scripts/check.mjs', 'console.log("fixture");\n'],
    ['node_modules/ignored/index.js', 'console.log("ignored");\n']
  ];
  const absoluteFiles = [];
  for (const [relativePath, content] of files) {
    const absolutePath = path.join(workspaceFixtureDir, relativePath);
    await mkdir(path.dirname(absolutePath), { recursive: true });
    await writeFile(absolutePath, content, 'utf8');
    absoluteFiles.push(absolutePath);
  }
  return {
    root: workspaceFixtureDir,
    files: absoluteFiles
  };
}

async function collectRepoWorkspaceFiles() {
  const selected = [];
  const seen = new Set();
  const preferred = [
    'OneBrain.slnx',
    'package.json',
    'tsconfig.json',
    'README.md',
    '.gitignore',
    'browser-extension/onebrain-chrome-lab/manifest.json',
    'browser-extension/onebrain-chrome-lab/sidepanel.html',
    'browser-extension/onebrain-chrome-lab/sidepanel.js',
    'scripts/verify-installed-sidepanel.mjs',
    'tests/OneBrain.Safety.Tests/NodalOsProductVisibleLocalDemoM1161M1172Tests.cs'
  ];

  const addFile = (relativePath) => {
    const normalized = normalizeHarnessPath(relativePath);
    if (!normalized || seen.has(normalized) || isHarnessProtectedPath(normalized) || isHarnessIgnoredFilePath(normalized)) {
      return;
    }
    const absolutePath = path.join(repoRoot, normalized);
    if (!existsSync(absolutePath)) {
      return;
    }
    seen.add(normalized);
    selected.push(absolutePath);
  };

  for (const relativePath of preferred) {
    addFile(relativePath);
  }

  async function walk(directory, depth) {
    if (selected.length >= workspaceHarnessMaxFiles || depth > workspaceHarnessMaxDepth) {
      return;
    }
    let entries = [];
    try {
      entries = await readdir(directory, { withFileTypes: true });
    } catch {
      return;
    }
    entries.sort((left, right) => left.name.localeCompare(right.name));
    for (const entry of entries) {
      if (selected.length >= workspaceHarnessMaxFiles) {
        return;
      }
      const absolutePath = path.join(directory, entry.name);
      const relativePath = normalizeHarnessPath(path.relative(repoRoot, absolutePath));
      if (!relativePath || isHarnessProtectedPath(relativePath)) {
        continue;
      }
      if (entry.isDirectory()) {
        if (!workspaceHarnessIgnoredDirs.has(entry.name.toLowerCase())) {
          await walk(absolutePath, depth + 1);
        }
        continue;
      }
      if (entry.isFile()) {
        addFile(relativePath);
      }
    }
  }

  await walk(repoRoot, 0);
  return selected.slice(0, workspaceHarnessMaxFiles);
}

function normalizeHarnessPath(value) {
  return String(value || '').replace(/\\/g, '/').replace(/^\/+/, '').toLowerCase();
}

function isHarnessProtectedPath(relativePath) {
  const normalized = normalizeHarnessPath(relativePath);
  return workspaceHarnessProtectedPaths.has(normalized)
    || workspaceHarnessProtectedPrefixes.some((prefix) => normalized.startsWith(prefix));
}

function isHarnessIgnoredFilePath(relativePath) {
  const fileName = path.basename(normalizeHarnessPath(relativePath));
  return workspaceHarnessIgnoredFiles.has(fileName)
    || workspaceHarnessIgnoredFilePrefixes.some((prefix) => fileName.startsWith(prefix));
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
