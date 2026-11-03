const { test } = require('node:test');
const assert = require('node:assert/strict');
const metrics = require('./analytics.js');
const sample = require('./sample-data.js');
const event = (extra = {}) => ({
  session_id: 'session-a', event_type: 'level_result', level_name: 'Level03',
  result: 'completed', deaths: 6, duration_seconds: 48.37,
  visit_number: 1, game_version: '0.1.0', client_timestamp_utc: '2026-10-20T04:42:18Z', ...extra
});
const report = (rows, options) => metrics.summarize(metrics.normalize({ analytics_events: rows }), options);

test('provided example: 6 deaths / 48.37 seconds; skipped -1 never becomes a bar', () => {
  const result = metrics.summarize(metrics.normalize(sample));
  assert.equal(result.events, 2); assert.equal(result.sessions, 1); assert.equal(result.completed, 1); assert.equal(result.skipped, 1);
  assert.equal(result.levels[0].deathTotal, 6); assert.equal(result.levels[0].durationMean, 48.37);
  assert.equal(result.levels[1].deathMean, null); assert.equal(result.levels[1].durationMean, null);
  assert.equal(result.levels[1].deathSamples, 0);
});
test('zero deaths / zero seconds are valid data, distinct from no sample', () => {
  const row = report({ a: event({ deaths: 0, duration_seconds: 0 }) }).levels[0];
  assert.equal(row.deathMean, 0); assert.equal(row.durationMean, 0); assert.equal(row.timeSamples, 1);
});
test('first completed visit per session is the default; skipped first visits do not hide a completion', () => {
  const rows = { a: event({ result: 'skipped', deaths: -1, duration_seconds: -1 }), b: event({ visit_number: 2, deaths: 8, duration_seconds: 80 }), c: event({ visit_number: 3, deaths: 2, duration_seconds: 20 }) };
  assert.equal(report(rows).levels[0].deathTotal, 8);
  assert.equal(report(rows).excludedCompletions, 1);
  assert.equal(report(rows, { sample: 'all' }).levels[0].deathMean, 5);
});
test('different sessions remain independent; first visit uses visit number before timestamp', () => {
  const rows = { a: event({ visit_number: 2, deaths: 100 }), b: event({ visit_number: 1, deaths: 2 }), c: event({ session_id: 'b', deaths: 4 }) };
  const result = report(rows);
  assert.equal(result.sessions, 2); assert.equal(result.levels[0].deathTotal, 6); assert.equal(result.levels[0].deathMean, 3);
});
test('versions filter independently and new snapshots do not accumulate previous events', () => {
  const normalized = metrics.normalize({ a: event(), b: event({ game_version: '0.2.0', deaths: 3, client_timestamp_utc: '2026-10-21T00:00:00Z' }) });
  assert.deepEqual(metrics.versions(normalized.events), ['0.2.0', '0.1.0']);
  assert.equal(metrics.summarize(normalized, { version: '0.2.0' }).levels[0].deathTotal, 3);
  assert.equal(metrics.summarize(normalized).completed, 2);
  assert.deepEqual(metrics.summarize(normalized), metrics.summarize(normalized));
});
test('invalid numeric types, negative values and fractional death counts are not coerced', () => {
  const result = report({ a: event({ deaths: '6', duration_seconds: -1 }), b: event({ session_id: 'b', deaths: 1.5, duration_seconds: 20 }) });
  assert.equal(result.levels[0].deathSamples, 0); assert.equal(result.levels[0].durationMean, 20); assert.equal(result.invalidMetrics, 2);
});
test('skipped with positive metrics and non-completed results still do not count as completions', () => {
  const result = report({ a: event({ result: 'skipped' }), b: event({ result: 'abandoned' }) });
  assert.equal(result.completed, 0); assert.equal(result.levels[0].timeSamples, 0); assert.equal(result.levels[0].other, 1);
});
test('root snapshot, bare collection, arrays and empty Firebase snapshot are supported', () => {
  assert.equal(metrics.normalize({ a: event() }).events.length, 1);
  assert.equal(metrics.normalize([event()]).events.length, 1);
  assert.equal(metrics.normalize(null).events.length, 0);
  assert.equal(metrics.normalize({ analytics_events: null }).events.length, 0);
  assert.throws(() => metrics.normalize(event()), /complete event collection/);
  assert.throws(() => metrics.normalize({ analytics_events: 10 }), /object or array/);
});
test('malformed rows and unrelated events are reported, and level names sort naturally', () => {
  const result = report({ a: event({ level_name: 'Level10' }), b: event({ level_name: 'Level2' }), c: event({ session_id: '' }), d: null, e: { event_type: 'survey' } });
  assert.equal(result.invalid, 2); assert.equal(result.ignored, 1);
  assert.deepEqual(result.levels.map(row => row.level), ['Level2', 'Level10']);
});
test('preferences are separate, sorted, validated totals; no votes are inferred from deaths', () => {
  const votes = metrics.normalizeVotes({ game_version: '0.1.0', favorites: [{ level_name: 'Level03', votes: 2 }, { level_name: 'Level03', votes: 1 }], dislikes: [] });
  assert.deepEqual(votes.favorites, [{ level: 'Level03', votes: 3 }]);
  assert.equal(votes.version, '0.1.0');
  assert.throws(() => metrics.normalizeVotes(sample), /favorites and dislikes/);
  assert.throws(() => metrics.normalizeVotes({ favorites: [{ level_name: 'A', votes: -1 }], dislikes: [] }), /non-negative integer/);
});
