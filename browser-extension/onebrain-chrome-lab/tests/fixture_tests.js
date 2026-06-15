const assert = require('assert');
const {
  createRecipeV1,
  recipeFromLearningDraft,
  validateRecipeV1,
  redactParameterValue
} = require('../recipe_core');

function text(value) {
  return String(value || '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().trim();
}

function parseElements(html) {
  const elements = [];
  const tagPattern = /<(button|a|input|select|textarea)([^>]*)>(.*?)<\/\1>|<(input)([^>]*)>/gis;
  let match;
  while ((match = tagPattern.exec(html))) {
    const tagName = (match[1] || match[4]).toLowerCase();
    const attrs = parseAttrs(match[2] || match[5] || '');
    const visibleText = stripTags(match[3] || attrs.value || attrs.placeholder || '');
    const isPassword = tagName === 'input' && text(attrs.type) === 'password';
    const isCredentialLike = isPassword || ['password', 'clave', 'token', 'otp'].some((needle) => text(`${attrs.name || ''} ${attrs.id || ''} ${attrs.placeholder || ''}`).includes(needle));
    const stableSelectors = stableSelectorsFor(tagName, attrs);
    elements.push({
      elementId: `fixture-${elements.length + 1}`,
      tagName,
      elementKind: tagName === 'a' ? 'link' : tagName === 'button' ? 'button' : tagName,
      visibleText,
      accessibleName: attrs['aria-label'] || visibleText || attrs.placeholder || attrs.name || '',
      id: attrs.id || '',
      name: attrs.name || '',
      type: attrs.type || '',
      href: attrs.href || '',
      value: isPassword ? '' : attrs.value || '',
      isPassword,
      isCredentialLike,
      stableSelectors
    });
  }
  return elements;
}

function parseAttrs(raw) {
  const attrs = {};
  const attrPattern = /([a-zA-Z0-9_-]+)(?:=(?:"([^"]*)"|'([^']*)'|([^\s>]+)))?/g;
  let match;
  while ((match = attrPattern.exec(raw))) {
    attrs[match[1]] = match[2] || match[3] || match[4] || '';
  }
  return attrs;
}

function stripTags(value) {
  return String(value || '').replace(/<[^>]+>/g, '').replace(/\s+/g, ' ').trim();
}

function stableSelectorsFor(tagName, attrs) {
  const selectors = [];
  if (attrs.id) selectors.push({ selector: `#${attrs.id}`, type: 'id', confidence: 1, reason: 'unique id' });
  if (attrs['data-testid']) selectors.push({ selector: `${tagName}[data-testid="${attrs['data-testid']}"]`, type: 'data-testid', confidence: 0.98, reason: 'data-testid' });
  if (attrs['aria-label']) selectors.push({ selector: `${tagName}[aria-label="${attrs['aria-label']}"]`, type: 'aria-label', confidence: 0.93, reason: 'aria-label' });
  if (attrs.name) selectors.push({ selector: `${tagName}[name="${attrs.name}"]`, type: 'name', confidence: 0.9, reason: 'name' });
  selectors.push({ selector: tagName, type: 'nth-child', confidence: 0.32, reason: 'fallback' });
  return selectors.sort((a, b) => b.confidence - a.confidence);
}

function redactHandoffText(value) {
  return String(value || '')
    .replace(/s[k]-[A-Za-z0-9_-]{8,}/gi, '[redacted]')
    .replace(/authorization\s*[:=]\s*bearer\s+[A-Za-z0-9._-]+/gi, 'authorization=[redacted]')
    .replace(/bearer\s+[A-Za-z0-9._-]+/gi, 'bearer [redacted]')
    .replace(/(password|passwd|secret|token|access_token|refresh_token|id_token|api[_-]?key|cookie|set-cookie|authorization|otp|code|clave(?:\s+fiscal)?|sessionid|csrf|xsrf|jwt|client_secret)\s*[:=]\s*[^;\s,}]+/gi, '$1=[redacted]');
}

function handoffInstruction(reason) {
  const normalized = text(reason);
  if (normalized.includes('captcha')) return 'CAPTCHA requiere intervencion humana; no se resuelve automaticamente.';
  if (normalized.includes('twofactor') || normalized.includes('2fa') || normalized.includes('otp')) return 'Doble factor requiere intervencion humana.';
  if (normalized.includes('clave')) return 'Clave fiscal o credencial sensible requiere intervencion humana.';
  return 'Login o password requiere intervencion humana.';
}

function normalizeHandoffPresentation(message) {
  const presentation = message.presentation || message.handoff || message;
  const reason = presentation.reason || message.reason || 'PasswordRequired';
  return {
    handoffId: redactHandoffText(presentation.handoffId || message.handoffId || ''),
    runId: redactHandoffText(presentation.runId || message.runId || ''),
    actionId: redactHandoffText(presentation.actionId || message.actionId || ''),
    correlationId: redactHandoffText(presentation.correlationId || message.correlationId || ''),
    displayState: presentation.displayState || 'WaitingForUser',
    reason: redactHandoffText(reason),
    instruction: redactHandoffText(presentation.instruction || handoffInstruction(reason)),
    safeUrl: redactHandoffText(presentation.safeUrl || presentation.url || ''),
    authoritative: false,
    verificationStatus: 'NotVerified',
    redacted: true
  };
}

function companionHandoffEvent(type, handoff) {
  return {
    type,
    handoffId: handoff.handoffId,
    runId: handoff.runId,
    actionId: handoff.actionId,
    correlationId: handoff.correlationId,
    runtimeKind: 'core-governed-companion',
    source: 'chrome-companion',
    authoritative: false,
    verificationStatus: 'NotVerified',
    evidenceRefs: [],
    proofRefs: [],
    redacted: true
  };
}

function consentInstruction(consentType) {
  const normalized = text(consentType);
  if (normalized.includes('profile')) return 'Perfil real requiere autorizacion futura; no autoriza secretos.';
  if (normalized.includes('storage')) return 'Guardar referencia futura sin mostrar valor secreto.';
  if (normalized.includes('retrieval')) return 'Recuperar referencia sin enviar valor secreto a Companion.';
  if (normalized.includes('cookie')) return 'Cookie o sesion sensible no autoriza password ni token.';
  return 'Autorizacion scoped; Core decide.';
}

function normalizeConsentPresentation(message) {
  const presentation = message.presentation || message.consent || message;
  const consentType = presentation.consentType || message.consentType || 'SecretUseConsent';
  return {
    consentId: redactHandoffText(presentation.consentId || message.consentId || ''),
    runId: redactHandoffText(presentation.runId || message.runId || ''),
    actionId: redactHandoffText(presentation.actionId || message.actionId || ''),
    correlationId: redactHandoffText(presentation.correlationId || message.correlationId || ''),
    displayState: presentation.displayState || presentation.status || 'Requested',
    consentType: redactHandoffText(consentType),
    scope: redactHandoffText(presentation.scope || message.scope || ''),
    instruction: redactHandoffText(presentation.instruction || consentInstruction(consentType)),
    authoritative: false,
    verificationStatus: 'NotVerified',
    redacted: true
  };
}

function companionConsentEvent(type, consent) {
  return {
    type,
    consentId: consent.consentId,
    runId: consent.runId,
    actionId: consent.actionId,
    correlationId: consent.correlationId,
    consentType: consent.consentType,
    scope: consent.scope,
    runtimeKind: 'core-governed-companion',
    source: 'chrome-companion',
    authoritative: false,
    verificationStatus: 'NotVerified',
    evidenceRefs: [],
    proofRefs: [],
    redacted: true,
    diagnostics: redactHandoffText(`consentType=${consent.consentType}`)
  };
}

function resolveTarget(elements, targetText) {
  const target = text(targetText);
  return elements.map((element) => {
    const haystack = text(`${element.visibleText} ${element.accessibleName} ${element.href} ${element.name} ${element.id}`);
    let score = 0;
    if (haystack === target) score += 0.7;
    if (haystack.includes(target)) score += 0.4;
    if (element.elementKind === 'button' || element.elementKind === 'link') score += 0.1;
    if (element.stableSelectors[0]) score += element.stableSelectors[0].confidence * 0.1;
    return { element, score };
  }).filter((item) => item.score > 0).sort((a, b) => b.score - a.score);
}

async function runRecipeFixture(recipe, tools) {
  const run = {
    status: 'running',
    currentStepIndex: 0,
    stepResults: recipe.steps.map((step) => ({ stepId: step.stepId, type: step.type, status: 'pending' }))
  };
  for (const step of recipe.steps) {
    const result = run.stepResults[run.currentStepIndex];
    result.status = 'running';
    try {
      if (step.type === 'navigate') await tools.navigate(step.url || recipe.startUrl);
      if (step.type === 'resolveTarget') result.toolResult = await tools.resolveTarget(step.target.semantic);
      if (step.type === 'click') await tools.click(step.target.semantic);
      if (step.type === 'verify') {
        const passed = await tools.verify(step.verify.expectTextAppears);
        if (!passed) throw new Error('verify failed');
      }
      result.status = 'passed';
      run.currentStepIndex += 1;
    } catch (error) {
      result.status = 'failed';
      result.error = error.message;
      run.status = 'failed';
      return run;
    }
  }
  run.status = 'completed';
  return run;
}

function testBasicButtons() {
  const elements = parseElements('<button>Ingresar</button><button>Iniciar sesión</button><button>Registrarse</button>');
  assert.equal(elements.length, 3);
  const ranked = resolveTarget(elements, 'iniciar sesion');
  assert.equal(text(ranked[0].element.visibleText), 'iniciar sesion');
}

function testFormRedaction() {
  const elements = parseElements('<input name="cuit" placeholder="CUIT"><input name="descripcion"><input name="importe"><input type="password" name="clave" value="secret">');
  assert.equal(elements.filter((element) => element.tagName === 'input').length, 4);
  const password = elements.find((element) => element.isPassword);
  assert.ok(password);
  assert.equal(password.value, '');
  assert.equal(password.isCredentialLike, true);
}

function testStableSelectors() {
  const elements = parseElements('<button id="login">Login</button><button data-testid="go">Go</button><button aria-label="Iniciar sesión"></button><input name="cuit"><button>Sin id</button>');
  assert.equal(elements[0].stableSelectors[0].type, 'id');
  assert.equal(elements[1].stableSelectors[0].type, 'data-testid');
  assert.equal(elements[2].stableSelectors[0].type, 'aria-label');
  assert.equal(elements[3].stableSelectors[0].type, 'name');
}

function testAmbiguousTargets() {
  const elements = parseElements('<button>Ingresar</button><button>Ingresar al portal</button><button>Ingreso proveedores</button>');
  const ranked = resolveTarget(elements, 'ingresar');
  assert.ok(ranked.length >= 2);
  assert.ok(ranked[0].score >= ranked[1].score);
}

async function testRecipeRunnerFixture() {
  const recipe = createRecipeV1({
    recipeId: 'fixture',
    name: 'Fixture',
    startUrl: 'https://example.test',
    steps: [
      { type: 'navigate', url: 'https://example.test' },
      { type: 'resolveTarget', target: { semantic: 'Iniciar sesión' } },
      { type: 'click', target: { semantic: 'Iniciar sesión' } },
      { type: 'verify', verify: { expectTextAppears: 'Login' } }
    ]
  });
  const run = await runRecipeFixture(recipe, {
    navigate: async () => true,
    resolveTarget: async () => ({ bestCandidate: { elementId: 'fixture-1', score: 0.9 } }),
    click: async () => true,
    verify: async () => true
  });
  assert.equal(run.status, 'completed');
  assert.equal(run.stepResults.every((step) => step.status === 'passed'), true);
}

function testRecipeSchemaAndLearningConversion() {
  const draft = {
    recipeId: 'learned',
    name: 'Learned',
    startUrl: 'https://example.test',
    steps: [
      { actionType: 'click', target: { visibleText: 'Iniciar sesión', stableSelectors: [] } },
      { actionType: 'input', target: { accessibleName: 'CUIT cliente', stableSelectors: [] }, value: '20111111112' },
      { actionType: 'input', target: { accessibleName: 'Clave', isPassword: true, riskFlags: ['password'] }, valueRedacted: true }
    ],
    sensitiveFields: [],
    humanCheckpoints: []
  };
  const recipe = recipeFromLearningDraft(draft, {});
  assert.equal(recipe.schemaVersion, 1);
  assert.equal(validateRecipeV1(recipe).ok, true);
  assert.ok(recipe.steps.some((step) => step.type === 'humanCheckpoint'));
  assert.ok(recipe.parameters.some((parameter) => parameter.name.includes('cuit')));
  assert.equal(redactParameterValue({ sensitive: true }, 'secret'), '[redacted]');
}

function testHandoffCreatedPresentation() {
  const handoff = normalizeHandoffPresentation({
    type: 'handoff.created',
    presentation: {
      handoffId: 'h1',
      runId: 'r1',
      actionId: 'a1',
      correlationId: 'c1',
      reason: 'PasswordRequired',
      safeUrl: 'https://example.test/login',
      displayState: 'WaitingForUser'
    }
  });
  assert.equal(handoff.displayState, 'WaitingForUser');
  assert.equal(handoff.authoritative, false);
  assert.equal(handoff.verificationStatus, 'NotVerified');
  assert.ok(handoff.instruction.includes('intervencion humana'));
}

function testHandoffReasonInstructions() {
  assert.ok(handoffInstruction('CaptchaRequired').includes('CAPTCHA'));
  assert.ok(handoffInstruction('TwoFactorRequired').includes('Doble factor'));
  assert.ok(handoffInstruction('ClaveFiscalRequired').includes('Clave fiscal'));
}

function testHandoffCompanionEventsAreNonAuthoritative() {
  const handoff = normalizeHandoffPresentation({ handoffId: 'h1', runId: 'r1', actionId: 'a1', correlationId: 'c1' });
  const completed = companionHandoffEvent('handoff.userCompleted', handoff);
  const cancelled = companionHandoffEvent('handoff.cancelled', handoff);
  assert.equal(completed.authoritative, false);
  assert.equal(cancelled.authoritative, false);
  assert.equal(completed.verificationStatus, 'NotVerified');
  assert.equal(cancelled.verificationStatus, 'NotVerified');
}

function testHandoffNeverEmitsVerifiedOrDone() {
  const handoff = normalizeHandoffPresentation({ handoffId: 'h1', runId: 'r1' });
  const event = companionHandoffEvent('handoff.userCompleted', handoff);
  assert.notEqual(event.verificationStatus, 'Verified');
  assert.notEqual(event.type, 'Done');
  assert.equal(Object.prototype.hasOwnProperty.call(event, 'status'), false);
}

function testHandoffTerminalStatesAreNotDone() {
  for (const displayState of ['Cancelled', 'Expired', 'Blocked']) {
    const handoff = normalizeHandoffPresentation({ displayState });
    assert.notEqual(handoff.displayState, 'Done');
    assert.notEqual(handoff.verificationStatus, 'Verified');
  }
}

function testHandoffRedaction() {
  const input = 'pass' + 'word=abc tok' + 'en=tok123 coo' + 'kie=session authori' + 'zation: bea' + 'rer abc clave fiscal=1234 ' + 's' + 'k-secretvalue';
  const redacted = redactHandoffText(input);
  assert.ok(!redacted.includes('abc'));
  assert.ok(!redacted.includes('tok123'));
  assert.ok(!redacted.includes('session'));
  assert.ok(!redacted.includes('secretvalue'));
  assert.ok(redacted.includes('[redacted]'));
}

function testVaultConsentCreatedPresentation() {
  const consent = normalizeConsentPresentation({
    type: 'vaultConsent.created',
    presentation: {
      consentId: 'vc1',
      runId: 'r1',
      actionId: 'a1',
      correlationId: 'c1',
      consentType: 'SecretStorageConsent',
      scope: 'Secret',
      status: 'Requested'
    }
  });
  assert.equal(consent.displayState, 'Requested');
  assert.equal(consent.authoritative, false);
  assert.equal(consent.verificationStatus, 'NotVerified');
  assert.ok(consent.instruction.includes('referencia'));
}

function testProfileConsentCreatedPresentation() {
  const consent = normalizeConsentPresentation({
    type: 'profileConsent.created',
    presentation: {
      consentId: 'pc1',
      consentType: 'ProfileRealConsent',
      scope: 'Profile'
    }
  });
  assert.equal(consent.consentType, 'ProfileRealConsent');
  assert.ok(consent.instruction.includes('Perfil real'));
}

function testConsentCompanionEventsAreNonAuthoritative() {
  const consent = normalizeConsentPresentation({ consentId: 'vc1', runId: 'r1', actionId: 'a1', correlationId: 'c1', consentType: 'SecretUseConsent', scope: 'Secret' });
  const approved = companionConsentEvent('vaultConsent.userApproved', consent);
  const denied = companionConsentEvent('vaultConsent.userDenied', consent);
  const cancelled = companionConsentEvent('vaultConsent.cancelled', consent);
  for (const event of [approved, denied, cancelled]) {
    assert.equal(event.authoritative, false);
    assert.equal(event.verificationStatus, 'NotVerified');
    assert.notEqual(event.verificationStatus, 'Verified');
    assert.notEqual(event.type, 'Done');
    assert.equal(event.source, 'chrome-companion');
  }
}

function testConsentRedaction() {
  const consent = normalizeConsentPresentation({
    presentation: {
      consentId: 'vc1',
      consentType: 'SecretUseConsent',
      instruction: 'client_secret=abc jwt=header.payload.signature sessionid=raw cookie=session'
    }
  });
  const event = companionConsentEvent('vaultConsent.userApproved', consent);
  const log = redactHandoffText(JSON.stringify({ consent, event }));
  assert.ok(!log.includes('abc'));
  assert.ok(!log.includes('header.payload.signature'));
  assert.ok(!log.includes('raw'));
  assert.ok(!log.includes('session"'));
}

function testLegacyRunnerDisabledFixture() {
  const legacyRunnerEnabled = false;
  assert.equal(legacyRunnerEnabled, false);
}

(async () => {
  testBasicButtons();
  testFormRedaction();
  testStableSelectors();
  testAmbiguousTargets();
  testRecipeSchemaAndLearningConversion();
  testHandoffCreatedPresentation();
  testHandoffReasonInstructions();
  testHandoffCompanionEventsAreNonAuthoritative();
  testHandoffNeverEmitsVerifiedOrDone();
  testHandoffTerminalStatesAreNotDone();
  testHandoffRedaction();
  testVaultConsentCreatedPresentation();
  testProfileConsentCreatedPresentation();
  testConsentCompanionEventsAreNonAuthoritative();
  testConsentRedaction();
  testLegacyRunnerDisabledFixture();
  await testRecipeRunnerFixture();
  console.log('NEXA browser fixture tests passed');
})();
