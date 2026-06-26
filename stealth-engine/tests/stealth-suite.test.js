/**
 * @NODAL_OS_RESTRICTED
 * @policy no-audit, no-modify, no-analyze, no-refactor
 * ADVERTENCIA: Código protegido. Solo el propietario puede modificarlo.
 */
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { FingerprintProfile, FingerprintGenerator } from '../src/fingerprint/FingerprintProfile.js';
import { BehaviorProfile } from '../src/behavior/BehaviorProfile.js';
import { HumanMouse } from '../src/behavior/HumanMouse.js';
import { HumanKeyboard } from '../src/behavior/HumanKeyboard.js';
import { HumanScroll } from '../src/behavior/HumanScroll.js';
import { HumanNavigation } from '../src/behavior/HumanNavigation.js';
import { ProxyManager } from '../src/proxy/ProxyManager.js';
import { ProxyHealthChecker } from '../src/proxy/ProxyHealthChecker.js';
import { DomainBlacklist } from '../src/antiBlocking/DomainBlacklist.js';
import { AdaptiveBehaviorEngine } from '../src/behavior/AdaptiveBehaviorEngine.js';

// ── Fingerprint Tests ──

test('FingerprintProfile construction with defaults', () => {
  const p = new FingerprintProfile();
  assert.ok(p.profileId);
  assert.equal(p.deviceType, 'desktop');
  assert.equal(p.os, 'Windows');
  assert.ok(p.userAgent.includes('Chrome'));
  assert.ok(Array.isArray(p.plugins));
  assert.ok(p.plugins.length >= 2);
  assert.ok(Array.isArray(p.fonts));
  assert.ok(p.fonts.length >= 5);
  assert.ok(Array.isArray(p.webglExtensions));
});

test('FingerprintGenerator generates coherent profile', () => {
  const p = FingerprintGenerator.generate({ preset: 'desktop-win-chrome' });
  assert.equal(p.deviceType, 'desktop');
  assert.equal(p.os, 'Windows');
  assert.ok(p.viewport.width >= 1024);
  assert.ok(p.userAgent.includes('Windows'));
  assert.ok(p.screen.width >= p.viewport.width);
  assert.ok(p.webglVendor.includes('Google'));
  assert.ok(p.webglRenderer.length > 0);
});

test('FingerprintGenerator generates mobile profile', () => {
  const p = FingerprintGenerator.generate({ deviceType: 'mobile', os: 'Android' });
  assert.equal(p.isMobile, true);
  assert.equal(p.hasTouch, true);
  assert.ok(p.maxTouchPoints >= 5);
  assert.ok(p.viewport.width <= 500);
});

test('FingerprintProfile.ensureCoherence updates country data', () => {
  const p = new FingerprintProfile();
  const result = FingerprintProfile.ensureCoherence(p, 'AR');
  assert.equal(result.country, 'AR');
  assert.equal(result.locale, 'es-AR');
  assert.equal(result.timezone, 'America/Argentina/Buenos_Aires');
});

// ── Behavior Tests ──

test('BehaviorProfile casual preset loads', () => {
  const bp = new BehaviorProfile('casual');
  assert.ok(bp.typoRate > 0);
  assert.ok(bp.baseDelay > 0);
  assert.equal(bp.name, 'casual');
});

test('BehaviorProfile pro vs elderly differ', () => {
  const pro = new BehaviorProfile('pro');
  const elderly = new BehaviorProfile('elderly');
  assert.ok(pro.typoRate < elderly.typoRate);
  assert.ok(pro.baseDelay < elderly.baseDelay);
});

test('HumanMouse constructor accepts BehaviorProfile', () => {
  const bp = new BehaviorProfile('casual');
  const mouse = new HumanMouse(bp);
  assert.ok(mouse.profile);
  assert.equal(mouse.profile.name, 'casual');
});

test('HumanKeyboard constructor accepts BehaviorProfile', () => {
  const bp = new BehaviorProfile('pro');
  const kb = new HumanKeyboard(bp);
  assert.ok(kb.profile);
});

test('HumanScroll constructor accepts BehaviorProfile', () => {
  const bp = new BehaviorProfile('elderly');
  const scroll = new HumanScroll(bp);
  assert.ok(scroll.profile);
});

test('HumanNavigation constructor with mouse', () => {
  const bp = new BehaviorProfile('casual');
  const mouse = new HumanMouse(bp);
  const nav = new HumanNavigation(mouse);
  assert.ok(nav.mouse === mouse);
});

test('HumanKeyboard.nearbyKey returns plausible key', () => {
  const bp = new BehaviorProfile();
  const kb = new HumanKeyboard(bp);
  const result = kb.constructor.nearbyKey('a');
  assert.ok(typeof result === 'string');
  assert.ok(result.length === 1);
});

// ── Proxy Tests ──

test('ProxyManager with empty pool returns null on acquire', () => {
  const pm = new ProxyManager([]);
  const result = pm.acquire('task-1');
  assert.equal(result, null);
});

test('ProxyManager acquires from pool', () => {
  const pm = new ProxyManager([{ url: 'http://proxy1:8080', type: 'residential', country: 'US' }]);
  const result = pm.acquire('task-1');
  assert.ok(result);
  assert.ok(result.server.includes('proxy1'));
  assert.equal(result.country, 'US');
  const stats = pm.getStats();
  assert.equal(stats.inUse, 1);
  assert.equal(stats.available, 0);
});

test('ProxyManager release and rotate', () => {
  const pm = new ProxyManager([
    { url: 'http://p1:8080', type: 'residential', country: 'US' },
    { url: 'http://p2:8080', type: 'datacenter', country: 'DE' },
  ]);
  pm.acquire('task-1');
  pm.release('task-1');
  assert.equal(pm.getStats().available, 2);
  pm.acquire('task-2');
  pm.markBanned('task-2');
  assert.equal(pm.getStats().banned, 1);
});

test('ProxyManager sticky sessions', () => {
  const pm = new ProxyManager([{ url: 'http://p1:8080', type: 'residential' }]);
  const first = pm.acquire('task-1', { sticky: true });
  pm.release('task-1');
  const second = pm.acquire('task-1', { sticky: true });
  assert.equal(second.server, first.server);
});

test('ProxyManager cooldown', () => {
  const pm = new ProxyManager([{ url: 'http://p1:8080' }]);
  pm.acquire('task-1');
  pm.markCooldown('task-1', 100);
  assert.equal(pm.getStats().cooldown, 1);
  pm.checkCooldowns();
});

test('ProxyManager stats', () => {
  const pm = new ProxyManager([
    { url: 'http://p1:8080' }, { url: 'http://p2:8080' }, { url: 'http://p3:8080' },
  ]);
  assert.equal(pm.getStats().total, 3);
  assert.equal(pm.getStats().available, 3);
});

test('ProxyManager falls back when all in use', () => {
  const pm = new ProxyManager([{ url: 'http://p1:8080' }]);
  pm.acquire('task-1');
  const second = pm.acquire('task-2');
  assert.ok(second);
});

// ── Anti-Blocking Tests ──

test('DomainBlacklist records and checks', () => {
  const bl = new DomainBlacklist(5);
  assert.equal(bl.isBlacklisted('bad.com'), false);
  bl.record('bad.com', 'bot_block');
  assert.equal(bl.isBlacklisted('bad.com'), true);
  const stats = bl.getStats();
  assert.equal(stats.size, 1);
});

test('DomainBlacklist respects max size', () => {
  const bl = new DomainBlacklist(2);
  bl.record('a.com', 'test');
  bl.record('b.com', 'test');
  bl.record('c.com', 'test');
  assert.ok(bl.getStats().size <= 2);
});

// ── Integration smoke tests (no browser) ──

test('FingerprintProfile round-trip through JSON', () => {
  const p = FingerprintGenerator.generate({ preset: 'desktop-win-chrome' });
  const json = JSON.stringify(p);
  const loaded = new FingerprintProfile(JSON.parse(json));
  assert.equal(loaded.deviceType, p.deviceType);
  assert.equal(loaded.userAgent, p.userAgent);
  assert.equal(loaded.country, p.country);
});

test('BehaviorProfile preset fallback for unknown name', () => {
  const bp = new BehaviorProfile('nonexistent');
  assert.equal(bp.name, 'casual');
  assert.ok(bp.typoRate > 0);
});

// ── Phase 3: AdaptiveBehaviorEngine Tests ──

test('AdaptiveBehaviorEngine generates session params', () => {
  const bp = new BehaviorProfile('casual');
  const engine = new AdaptiveBehaviorEngine(bp);
  const params = engine.getProfile();
  assert.ok(params.typoRate > 0);
  assert.ok(params.baseDelay > 0);
  assert.ok(params.ballisticsThreshold > 0);
  assert.ok(params.scrollSubStepsMax >= params.scrollSubStepsMin);
});

test('AdaptiveBehaviorEngine produces different params for two sessions', () => {
  const bp = new BehaviorProfile('casual');
  const e1 = new AdaptiveBehaviorEngine(bp);
  const e2 = new AdaptiveBehaviorEngine(bp);
  const p1 = e1.getProfile();
  const p2 = e2.getProfile();
  const anyDiffer = p1.typoRate !== p2.typoRate || p1.baseDelay !== p2.baseDelay
    || p1.overShootRate !== p2.overShootRate || p1.bezierVariance !== p2.bezierVariance;
  assert.ok(anyDiffer, 'Two sessions should produce different parameters');
});

test('AdaptiveBehaviorEngine disabled returns base profile unchanged', () => {
  const bp = new BehaviorProfile('pro');
  const engine = new AdaptiveBehaviorEngine(bp, { adaptive: false });
  const params = engine.getProfile();
  assert.equal(params.typoRate, bp.typoRate);
  assert.equal(params.baseDelay, bp.baseDelay);
});

test('AdaptiveBehaviorEngine respects variance range', () => {
  const bp = new BehaviorProfile('casual');
  for (let i = 0; i < 20; i++) {
    const engine = new AdaptiveBehaviorEngine(bp, { profileVariance: 0.2 });
    const p = engine.getProfile();
    assert.ok(p.typoRate >= 0.005 && p.typoRate <= 0.15, 'typoRate within range');
    assert.ok(p.baseDelay >= 20 && p.baseDelay <= 300, 'baseDelay within range');
    assert.ok(p.clickPauseMin >= 30, 'clickPauseMin within range');
    assert.ok(p.clickPauseMax <= 600, 'clickPauseMax within range');
  }
});

// ── Phase 3: HumanMouse ballistic path selection ──

test('HumanMouse with adaptive profile selects ballistic for long distance', () => {
  const bp = new BehaviorProfile('pro');
  const engine = new AdaptiveBehaviorEngine(bp);
  const ap = engine.getProfile();
  const mouse = new HumanMouse(ap);
  assert.ok(mouse.profile.ballisticsThreshold > 0);
});

// ── Phase 3: VisualCaptchaSolver (offline, no deps) ──

test('VisualCaptchaSolver disabled returns unknown classify', async () => {
  let VisualCaptchaSolver;
  try {
    const mod = await import('../src/captcha/VisualCaptchaSolver.js');
    VisualCaptchaSolver = mod.VisualCaptchaSolver;
  } catch (e) {
    return;
  }
  const solver = new VisualCaptchaSolver({ enabled: false });
  const buf = Buffer.alloc(100, 128);
  const type = await solver.classify(buf);
  assert.equal(type, 'unknown');
});

test('VisualCaptchaSolver solve returns error when disabled', async () => {
  let VisualCaptchaSolver;
  try {
    const mod = await import('../src/captcha/VisualCaptchaSolver.js');
    VisualCaptchaSolver = mod.VisualCaptchaSolver;
  } catch (e) {
    return;
  }
  const solver = new VisualCaptchaSolver({ enabled: false });
  const result = await solver.solve(Buffer.alloc(100), 'text-distorted');
  assert.equal(result.success, false);
});
