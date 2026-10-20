const { test } = require('node:test');
const assert = require('node:assert/strict');
const { DEFAULT_DATABASE_URL, buildEventsUrl, readEvents } = require('./firebase-reader.js');

test('database root becomes only the analytics_events REST path', () => {
  assert.equal(buildEventsUrl(), DEFAULT_DATABASE_URL + 'analytics_events.json');
  assert.equal(buildEventsUrl(DEFAULT_DATABASE_URL.slice(0, -1)), buildEventsUrl());
  for (const bad of ['http://example.firebaseio.com/', 'https://example.com/', 'https://user:pass@example.firebaseio.com/', DEFAULT_DATABASE_URL + '?auth=example', DEFAULT_DATABASE_URL + 'other/']) {
    assert.throws(() => buildEventsUrl(bad));
  }
});
test('explicit reader uses GET with no credentials, and never reads the database root', async () => {
  let calls = 0;
  const payload = { eventId: { event_type: 'level_result' } };
  const result = await readEvents({ fetchImpl: async (url, options) => {
    calls++; assert.equal(url, buildEventsUrl()); assert.equal(options.method, 'GET');
    assert.equal(options.credentials, 'omit'); assert.equal(options.redirect, 'error');
    return { ok: true, status: 200, json: async () => payload };
  } });
  assert.equal(calls, 1); assert.deepEqual(result, payload);
});
test('denied access is reported without suggesting public database access', async () => {
  for (const status of [401, 403]) {
    await assert.rejects(readEvents({ fetchImpl: async () => ({ ok: false, status }) }), /access denied/);
  }
});
test('empty snapshot is valid, while invalid JSON and scalar payloads are rejected', async () => {
  assert.equal(await readEvents({ fetchImpl: async () => ({ ok: true, json: async () => null }) }), null);
  await assert.rejects(readEvents({ fetchImpl: async () => ({ ok: true, json: async () => { throw new SyntaxError(); } }) }), /valid JSON/);
  await assert.rejects(readEvents({ fetchImpl: async () => ({ ok: true, json: async () => 42 }) }), /event collection/);
});
test('network failure and HTTP failure remain errors, never a fabricated empty dataset', async () => {
  await assert.rejects(readEvents({ fetchImpl: async () => { throw new Error('Network offline'); } }), /Network offline/);
  await assert.rejects(readEvents({ fetchImpl: async () => ({ ok: false, status: 500 }) }), /HTTP 500/);
});
