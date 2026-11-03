/* Optional local UI test. Pass a Playwright module path and Chrome path if not installed globally. */
const assert = require('node:assert/strict');
const path = require('node:path');
const { pathToFileURL } = require('node:url');
const { chromium } = require(process.argv[2] || 'playwright');
const example = require('./sample-data.js');

(async () => {
  const browser = await chromium.launch({ headless: true, ...(process.argv[3] ? { executablePath: process.argv[3] } : {}) });
  const page = await browser.newPage({ viewport: { width: 1366, height: 1100 } });
  const errors = [], externalRequests = [];
  page.on('pageerror', error => errors.push(error.message));
  page.on('request', request => { if (/^https?:/i.test(request.url())) externalRequests.push(request.url()); });
  let checks = 0;
  const check = (condition, label) => { assert.ok(condition, label); checks++; console.log('PASS ' + label); };
  try {
    await page.goto(pathToFileURL(path.join(__dirname, 'index.html')).href);
    check(await page.locator('.empty').count() === 4, 'empty state contains four honest placeholders');
    await page.locator('#example').click();
    const original = await page.evaluate(() => PlaytestDashboard.getSummary());
    check(original.levels[0].deathTotal === 6 && original.levels[0].durationMean === 48.37, 'teammate sample produces exact chart values');
    check(original.levels[1].durationMean === null && await page.locator('#death-chart .point').count() === 1 && await page.locator('#death-chart .box').count() === 0, 'skipped event has no substitute point; one sample shows a point without a box');
    await page.locator('#death-mode').selectOption('total');
    check(await page.locator('#death-chart .bar').count() === 1, 'total deaths mode still renders one bar per level with samples');
    await page.locator('#death-mode').selectOption('box');
    check(await page.locator('#source').innerText() === 'Sample data', 'example is visibly labeled');
    check(await page.locator('#favorite-chart .empty').count() === 1, 'no preference data is invented');
    await page.evaluate(() => { PlaytestDashboard.setData(TeammateExample.analytics_events); PlaytestDashboard.setData(TeammateExample.analytics_events); });
    check((await page.evaluate(() => PlaytestDashboard.getSummary())).levels[0].deathTotal === 6, 'full snapshot replacement does not double-count');
    await page.locator('#file').setInputFiles({ name: 'invalid.json', mimeType: 'application/json', buffer: Buffer.from('{invalid') });
    await page.waitForFunction(() => document.querySelector('#message').classList.contains('error'));
    check((await page.evaluate(() => PlaytestDashboard.getSummary())).events === 2, 'invalid JSON preserves last valid report');
    await page.locator('#file').setInputFiles({ name: 'events.json', mimeType: 'application/json', buffer: Buffer.from(JSON.stringify(example)) });
    await page.waitForFunction(() => document.querySelector('#source').textContent === 'Imported: events.json');
    check(await page.locator('#message.error').count() === 0, 'valid JSON file import succeeds after an error');
    await page.evaluate(() => {
      window.__originalFetchForTest = window.fetch;
      window.__readerCallsForTest = [];
      window.fetch = async (url, options) => {
        window.__readerCallsForTest.push({ url, method: options.method, credentials: options.credentials });
        return new Response(JSON.stringify(TeammateExample.analytics_events), { status: 200, headers: { 'Content-Type': 'application/json' } });
      };
    });
    await page.locator('#database').click();
    await page.waitForFunction(() => document.querySelector('#source').textContent === 'Database');
    const readerCalls = await page.evaluate(() => window.__readerCallsForTest);
    check(readerCalls.length === 1 && readerCalls[0].url.endsWith('/analytics_events.json') && readerCalls[0].method === 'GET' && readerCalls[0].credentials === 'omit', 'database button uses one explicit credential-free GET to the event node (mocked)');
    check((await page.evaluate(() => PlaytestDashboard.getSummary())).levels[0].deathTotal === 6, 'mocked database response updates charts through the existing adapter');
    await page.evaluate(() => { window.fetch = async () => new Response('{}', { status: 401 }); });
    await page.locator('#database').click();
    await page.waitForFunction(() => document.querySelector('#message').textContent.includes('access denied'));
    check((await page.evaluate(() => PlaytestDashboard.getSummary())).events === 2, 'permission-denied database read preserves the previous report (mocked)');
    await page.evaluate(() => { window.fetch = window.__originalFetchForTest; delete window.__originalFetchForTest; delete window.__readerCallsForTest; });
    // Synthetic fixtures below are test-only, never part of the demo dataset.
    await page.evaluate(() => {
      const base = TeammateExample.analytics_events['-ObA12ExamplePushId1'];
      PlaytestDashboard.setData({ a: base, b: { ...base, deaths: 2, game_version: '0.2.0', client_timestamp_utc: '2026-10-21T00:00:00Z' } });
    });
    await page.locator('#version').selectOption('0.2.0');
    check((await page.evaluate(() => PlaytestDashboard.getSummary())).levels[0].deathTotal === 2, 'version selector changes the actual chart denominator');
    await page.evaluate(() => PlaytestDashboard.setPreferences({ game_version: '0.2.0', favorites: [{ level_name: 'Level03', votes: 2 }], dislikes: [{ level_name: 'Level01', votes: 1 }, { level_name: 'Level02', votes: 4 }, { level_name: 'Level03', votes: 2 }, { level_name: 'Level04', votes: 3 }] }));
    check(await page.locator('#dislike-chart .bar').count() === 3 && await page.locator('#favorite-chart .bar').count() === 1, 'separate preference adapter renders top-three and favorites');
    await page.locator('#version').selectOption('0.1.0');
    check(await page.locator('#favorite-chart .empty').count() === 1, 'preference totals from another version are not mixed');
    await page.evaluate(() => {
      const base = TeammateExample.analytics_events['-ObA12ExamplePushId1'];
      const rows = {};
      [40, 42, 45, 47, 50, 600].forEach((seconds, index) => { rows['t' + index] = { ...base, session_id: 's' + index, duration_seconds: seconds, deaths: index === 5 ? 30 : 1 }; });
      [20, 25].forEach((seconds, index) => { rows['u' + index] = { ...base, level_name: 'Level04', session_id: 'u' + index, duration_seconds: seconds, deaths: index }; });
      PlaytestDashboard.setData(rows);
    });
    check(await page.locator('#time-chart .box').count() === 1 && await page.locator('#time-chart .point').count() === 8 && await page.locator('#time-chart .outlier').count() === 1 && (await page.locator('#time-chart .outlier-label').textContent()).includes('600'), 'box plot draws a box only at n >= 5, every sample as a point, and pins the labeled outlier above a whisker-scaled axis');
    check((await page.locator('#time-chart .value-label').allTextContents()).includes('46') && (await page.locator('#rows').innerText()).includes('46'), 'median (46 s), not the outlier-inflated mean, is labeled and tabulated');
    await page.screenshot({ path: path.join(__dirname, 'preview-boxplot.png'), fullPage: true });
    await page.evaluate(() => {
      const base = TeammateExample.analytics_events['-ObA12ExamplePushId1'];
      PlaytestDashboard.setData({ a: { ...base, level_name: '<img src=x onerror=alert(1)>' } });
    });
    check(await page.locator('img').count() === 0 && (await page.locator('#rows').innerText()).includes('<img'), 'untrusted level names are rendered only as text');
    await page.locator('#example').click();
    await page.screenshot({ path: path.join(__dirname, 'preview.png'), fullPage: true });
    await page.setViewportSize({ width: 390, height: 844 });
    check(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth), 'mobile page has no document-wide horizontal overflow');
    await page.screenshot({ path: path.join(__dirname, 'preview-mobile.png'), fullPage: true });
    await page.locator('#clear').click();
    check((await page.evaluate(() => PlaytestDashboard.getSummary())).events === 0 && await page.locator('.empty').count() === 4, 'clear resets report and preferences');
    check(errors.length === 0, 'no browser JavaScript errors');
    check(externalRequests.length === 0, 'page does not auto-connect; mocked-reader tests made no real external requests');
    console.log(JSON.stringify({ checks, errors, externalRequests }));
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
