/* Pure aggregation. No network calls, Firebase SDK, or credentials required. */
(function (root, factory) {
  const api = factory();
  if (typeof module === 'object' && module.exports) module.exports = api;
  else root.PlaytestMetrics = api;
})(typeof globalThis !== 'undefined' ? globalThis : this, function () {
  'use strict';
  const object = value => value !== null && typeof value === 'object' && !Array.isArray(value);
  const number = value => typeof value === 'number' && Number.isFinite(value) && value >= 0;
  const text = value => typeof value === 'string' && value.trim().length > 0;
  const collator = new Intl.Collator('en', { numeric: true });

  function normalize(payload) {
    if (payload === null) return { events: [], ignored: 0, invalid: 0 };
    if (!object(payload) && !Array.isArray(payload)) throw new TypeError('Expected a JSON object or event array.');
    const collection = object(payload) && Object.hasOwn(payload, 'analytics_events') ? payload.analytics_events : payload;
    if (collection === null) return { events: [], ignored: 0, invalid: 0 };
    if (!object(collection) && !Array.isArray(collection)) throw new TypeError('analytics_events must be an object or array.');
    // Catch a single event accidentally passed in place of a snapshot.
    if (object(collection) && typeof collection.event_type === 'string') throw new TypeError('Pass the complete event collection, not a single event.');
    const events = [];
    let ignored = 0, invalid = 0;
    for (const [id, row] of Object.entries(collection)) {
      if (!object(row)) { invalid++; continue; }
      if (row.event_type !== 'level_result') { ignored++; continue; }
      if (!text(row.session_id) || !text(row.level_name) || !text(row.result)) { invalid++; continue; }
      const stamp = typeof row.client_timestamp_utc === 'string' ? Date.parse(row.client_timestamp_utc) : NaN;
      events.push({
        id, session: row.session_id.trim(), level: row.level_name.trim(), result: row.result,
        version: text(row.game_version) ? row.game_version.trim() : '(not supplied)',
        deaths: number(row.deaths) && Number.isInteger(row.deaths) ? row.deaths : null,
        seconds: number(row.duration_seconds) ? row.duration_seconds : null,
        visit: Number.isInteger(row.visit_number) && row.visit_number > 0 ? row.visit_number : Infinity,
        timestamp: Number.isFinite(stamp) ? stamp : Infinity
      });
    }
    return { events, ignored, invalid };
  }

  function versions(events) {
    const timestamps = new Map();
    for (const event of events) {
      const stamp = Number.isFinite(event.timestamp) ? event.timestamp : 0;
      timestamps.set(event.version, Math.max(timestamps.get(event.version) || 0, stamp));
    }
    return [...timestamps].sort((a, b) => b[1] - a[1] || collator.compare(b[0], a[0])).map(([version]) => version);
  }

  // Linear-interpolation quantile (same as Excel QUARTILE.INC / NumPy default) on sorted values.
  function quantile(sorted, p) {
    const position = (sorted.length - 1) * p, low = Math.floor(position), high = Math.ceil(position);
    return sorted[low] + (sorted[high] - sorted[low]) * (position - low);
  }
  // Box-plot statistics. Whiskers reach the furthest values within 1.5 × IQR of the box (Tukey).
  function describe(values) {
    if (!values.length) return null;
    const sorted = [...values].sort((a, b) => a - b);
    const q1 = quantile(sorted, .25), median = quantile(sorted, .5), q3 = quantile(sorted, .75);
    const iqr = q3 - q1, lowFence = q1 - 1.5 * iqr, highFence = q3 + 1.5 * iqr;
    const inside = sorted.filter(value => value >= lowFence && value <= highFence);
    return {
      n: sorted.length, min: sorted[0], q1, median, q3, max: sorted[sorted.length - 1],
      whiskerLow: inside[0], whiskerHigh: inside[inside.length - 1],
      outliers: sorted.filter(value => value < lowFence || value > highFence), values: sorted
    };
  }

  function summarize(normalized, options = {}) {
    const events = normalized.events.filter(row => !options.version || row.version === options.version);
    const selected = events.filter(row => row.result === 'completed');
    const first = new Map();
    for (const row of selected) {
      const key = JSON.stringify([row.session, row.level, row.version]);
      const current = first.get(key);
      if (!current || row.visit < current.visit || (row.visit === current.visit && row.timestamp < current.timestamp)) first.set(key, row);
    }
    const completed = options.sample === 'all' ? selected : [...first.values()];
    const byLevel = new Map();
    for (const row of events) {
      if (!byLevel.has(row.level)) byLevel.set(row.level, {
        level: row.level, visits: 0, completedVisits: 0, skipped: 0, other: 0,
        samples: 0, deathSamples: 0, timeSamples: 0, deathTotal: 0, secondsTotal: 0, deathValues: [], durationValues: []
      });
      const item = byLevel.get(row.level);
      item.visits++;
      if (row.result === 'completed') item.completedVisits++;
      else if (row.result === 'skipped') item.skipped++;
      else item.other++;
    }
    for (const row of completed) {
      const item = byLevel.get(row.level);
      item.samples++;
      if (row.deaths !== null) { item.deathTotal += row.deaths; item.deathSamples++; item.deathValues.push(row.deaths); }
      if (row.seconds !== null) { item.secondsTotal += row.seconds; item.timeSamples++; item.durationValues.push(row.seconds); }
    }
    return {
      levels: [...byLevel.values()].sort((a, b) => collator.compare(a.level, b.level)).map(item => {
        const deathStats = describe(item.deathValues), durationStats = describe(item.durationValues);
        return {
          ...item,
          deathValues: deathStats ? deathStats.values : [], durationValues: durationStats ? durationStats.values : [],
          deathMean: item.deathSamples ? item.deathTotal / item.deathSamples : null,
          durationMean: item.timeSamples ? item.secondsTotal / item.timeSamples : null,
          deathMedian: deathStats ? deathStats.median : null, durationMedian: durationStats ? durationStats.median : null,
          deathStats, durationStats
        };
      }),
      events: events.length, sessions: new Set(events.map(row => row.session)).size,
      completed: completed.length, skipped: events.filter(row => row.result === 'skipped').length,
      excludedCompletions: selected.length - completed.length,
      invalidMetrics: completed.filter(row => row.deaths === null || row.seconds === null).length,
      ignored: normalized.ignored, invalid: normalized.invalid
    };
  }

  // A separate, PROPOSED adapter for charts 3/4. This is not part of the supplied event schema.
  function normalizeVotes(payload) {
    if (!object(payload) || !Array.isArray(payload.favorites) || !Array.isArray(payload.dislikes)) {
      throw new TypeError('Votes require favorites and dislikes arrays.');
    }
    function group(rows) {
      const result = new Map();
      for (const row of rows) {
        if (!object(row) || !text(row.level_name) || !number(row.votes) || !Number.isInteger(row.votes)) {
          throw new TypeError('Each vote entry needs level_name and a non-negative integer votes.');
        }
        const key = row.level_name.trim();
        result.set(key, (result.get(key) || 0) + row.votes);
      }
      return [...result].map(([level, votes]) => ({ level, votes })).sort((a, b) => b.votes - a.votes || collator.compare(a.level, b.level));
    }
    return { favorites: group(payload.favorites), dislikes: group(payload.dislikes), version: text(payload.game_version) ? payload.game_version : null };
  }
  return { normalize, summarize, versions, normalizeVotes, describe };
});
