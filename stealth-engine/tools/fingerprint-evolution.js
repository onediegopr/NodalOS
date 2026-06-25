import { readFile, writeFile } from 'node:fs/promises';
import { chromium } from 'playwright';
import { FingerprintGenerator, FingerprintProfile } from '../src/fingerprint/FingerprintProfile.js';
import { FingerprintInjector } from '../src/fingerprint/FingerprintInjector.js';

const TEST_SITES = [
  {
    name: 'bot.sannysoft.com',
    url: 'https://bot.sannysoft.com',
    checks: {
      'WebDriver': { expected: ['missing', 'false', 'passed'], severity: 'critical' },
      'Chrome': { expected: ['present'], severity: 'high' },
      'Plugins': { expected: ['present'], severity: 'high' },
      'Languages': { expected: ['present'], severity: 'medium' },
      'UserAgent': { expected: ['present'], severity: 'medium' },
      'WebGL': { expected: ['present'], severity: 'medium' },
      'Canvas': { expected: ['present'], severity: 'low' },
      'Touch': { expected: ['present'], severity: 'low' },
    },
  },
  {
    name: 'fingerprint.com/demo',
    url: 'https://fingerprint.com/demo/',
    extractor: async (page) => {
      try {
        await page.waitForTimeout(5000);
        const result = await page.evaluate(() => {
          const el = document.querySelector('[class*="visitorId"], [class*="visitor-id"], [id*="visitorId"]');
          return el ? el.textContent.trim() : null;
        });
        return { visitorId: result, note: 'Fingerprint.com demo loaded' };
      } catch (e) { return { error: e.message }; }
    },
  },
  {
    name: 'browserleaks.com',
    url: 'https://browserleaks.com/',
    extractor: async (page) => {
      try {
        await page.waitForTimeout(3000);
        const results = await page.evaluate(() => {
          const items = {};
          document.querySelectorAll('tr').forEach(r => {
            const cells = r.querySelectorAll('td');
            if (cells.length >= 2) items[cells[0].textContent.trim()] = cells[1].textContent.trim();
          });
          return items;
        });
        return results;
      } catch (e) { return { error: e.message }; }
    },
  },
  {
    name: 'antoinevastel.com/bots',
    url: 'https://antoinevastel.com/bots/',
    extractor: async (page) => {
      try {
        await page.waitForTimeout(4000);
        const results = await page.evaluate(() => {
          const items = {};
          document.querySelectorAll('tr, .result-item, [class*="result"]').forEach(row => {
            const text = row.textContent.trim();
            if (text.includes(':')) {
              const [key, val] = text.split(':').map(s => s.trim());
              if (key && val) items[key] = val;
            }
          });
          return items;
        });
        return results;
      } catch (e) { return { error: e.message }; };
    },
  },
];

async function loadBaseline() {
  try {
    const data = await readFile('./config/fingerprint-baseline.json', 'utf-8');
    return JSON.parse(data);
  } catch { return { description: 'Real browser baseline (manual capture)', lastUpdated: null, metrics: {} }; }
}

async function testSite(page, site) {
  console.log(`\n  Testing: ${site.name}...`);
  try {
    await page.goto(site.url, { waitUntil: 'domcontentloaded', timeout: 30000 });

    let results = {};
    if (site.extractor) {
      results = await site.extractor(page);
    } else if (site.checks) {
      await page.waitForTimeout(3000);
      results = await page.evaluate((checkKeys) => {
        const items = {};
        document.querySelectorAll('table tr').forEach(row => {
          const cells = row.querySelectorAll('td');
          if (cells.length >= 2) {
            const test = cells[0].textContent.trim();
            const result = cells[1].textContent.trim();
            for (const key of checkKeys) {
              if (test.includes(key)) items[key] = result;
            }
          }
        });
        return items;
      }, Object.keys(site.checks));
    }

    const leaks = [];
    if (site.checks) {
      for (const [key, check] of Object.entries(site.checks)) {
        const value = results[key];
        const passed = value && check.expected.some(e => value.toLowerCase().includes(e.toLowerCase()));
        if (!passed) leaks.push({ check: key, actual: value || 'not found', expected: check.expected, severity: check.severity });
      }
    }

    return { site: site.name, url: site.url, results, leaks, leakCount: leaks.length };
  } catch (e) {
    return { site: site.name, url: site.url, error: e.message, results: {}, leaks: [], leakCount: 0 };
  }
}

async function run() {
  console.log('╔══════════════════════════════════════════╗');
  console.log('║  NODAL OS Fingerprint Evolution Pipeline  ║');
  console.log('╚══════════════════════════════════════════╝\n');

  const preset = process.argv[2] || 'desktop-win-chrome';
  console.log('Profile preset: ' + preset);

  const profile = FingerprintGenerator.generate({ preset });
  const baseline = await loadBaseline();
  console.log('Baseline: ' + (baseline.lastUpdated || 'not available'));

  console.log('\nLaunching browser...');
  const browser = await chromium.launch({
    headless: false,
    channel: 'chromium',
    args: [
      '--no-sandbox', '--disable-blink-features=AutomationControlled',
      '--disable-features=IsolateOrigins,site-per-process',
    ],
  });

  const context = await browser.newContext({
    viewport: profile.viewport,
    userAgent: profile.userAgent,
    locale: profile.locale,
    timezoneId: profile.timezone,
    bypassCSP: true,
    ignoreHTTPSErrors: true,
  });

  await context.addInitScript(FingerprintInjector.getFullInitScript(profile));
  const page = await context.newPage();

  const report = {
    generatedAt: new Date().toISOString(),
    profile: { preset, deviceType: profile.deviceType, os: profile.os, userAgent: profile.userAgent, viewport: profile.viewport, webglVendor: profile.webglVendor },
    baseline: { description: baseline.description, lastUpdated: baseline.lastUpdated },
    sites: [],
    summary: { totalTests: 0, passed: 0, warnings: 0, leaks: 0, criticalLeaks: 0 },
  };

  for (const site of TEST_SITES) {
    const result = await testSite(page, site);
    report.sites.push(result);
    report.summary.totalTests++;

    if (result.error) {
      console.log(`    ERROR: ${result.error}`);
      report.summary.warnings++;
    } else if (result.leakCount === 0) {
      console.log(`    PASS (0 leaks)`);
      report.summary.passed++;
    } else {
      console.log(`    LEAKS: ${result.leakCount}`);
      result.leaks.forEach(l => {
        console.log(`      - ${l.check} [${l.severity}]: got "${l.actual}", expected match for ${JSON.stringify(l.expected)}`);
        report.summary.leaks++;
        if (l.severity === 'critical') report.summary.criticalLeaks++;
      });
    }
  }

  await context.close();
  await browser.close();

  const reportPath = './fingerprint-report.json';
  await writeFile(reportPath, JSON.stringify(report, null, 2), 'utf-8');
  console.log('\nReport saved to: ' + reportPath);
  console.log('Summary: ' + report.summary.passed + ' passed, ' + report.summary.leaks + ' leaks (' + report.summary.criticalLeaks + ' critical), ' + report.summary.warnings + ' warnings');

  if (report.summary.leaks > 0) {
    console.log('\n╔══════════════════════════════════════════╗');
    console.log('║  RECOMMENDATIONS TO PATCH LEAKS:         ║');
    console.log('╚══════════════════════════════════════════╝');

    const allLeaks = report.sites.flatMap(s => s.leaks);
    allLeaks.forEach(l => {
      switch (l.check) {
        case 'WebDriver':
          console.log(`  WebDriver leak → Ensure navigator.webdriver is undefined (not false). Check FingerprintInjector getFullInitScript.`);
          break;
        case 'Chrome':
          console.log(`  Chrome runtime leak → Ensure window.chrome is properly spoofed with runtime.* methods.`);
          break;
        case 'Plugins':
          console.log(`  Plugins leak → Ensure navigator.plugins reflects the profile (not empty).`);
          break;
        case 'WebGL':
          console.log(`  WebGL leak → Verify getParameter hooks for UNMASKED_VENDOR_WEBGL and UNMASKED_RENDERER_WEBGL.`);
          break;
        default:
          console.log(`  ${l.check} leak → Review and extend FingerprintInjector for this property.`);
      }
    });
  }

  process.exit(report.summary.criticalLeaks > 0 ? 1 : 0);
}

run().catch(e => { console.error('Fatal:', e.message); process.exit(1); });
