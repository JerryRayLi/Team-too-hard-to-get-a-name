/* Google Forms response aggregation for the dashboard. */
(function (root, factory) {
  const api = factory();
  if (typeof module === 'object' && module.exports) module.exports = api;
  else root.SurveyPipeline = api;
})(typeof globalThis !== 'undefined' ? globalThis : this, function () {
  'use strict';
  const aliases = Object.freeze({
    demo1: 'INITIATION01', initiation01: 'INITIATION01',
    demo2: 'INITIATION02', initiation02: 'INITIATION02',
    demo3: 'INITIATION03', initiation03: 'INITIATION03',
    demo4: 'INITIATION04', demolevel4: 'INITIATION04', initiation04: 'INITIATION04',
    spikes: 'SPIKES', corpse1: 'CORPSE01', corpse01: 'CORPSE01',
    corpse2: 'CORPSE02', corpse02: 'CORPSE02', slow: 'SLOW 1', slow1: 'SLOW 1',
    door: 'DOOR01', door1: 'DOOR01', reverse: 'REVERSE'
  });
  const text = value => String(value == null ? '' : value).trim().replace(/\s+/g, ' ');
  const key = value => text(value).toLowerCase().replace(/[^a-z0-9]+/g, '');
  const levelName = value => aliases[key(value)] || text(value);

  function parseCsv(csv) {
    if (typeof csv !== 'string') throw new TypeError('Survey CSV must be text.');
    const rows = []; let row = [], cell = '', quoted = false;
    for (let i = 0; i < csv.length; i++) {
      const char = csv[i];
      if (quoted) {
        if (char === '"' && csv[i + 1] === '"') { cell += '"'; i++; }
        else if (char === '"') quoted = false;
        else cell += char;
      } else if (char === '"') quoted = true;
      else if (char === ',') { row.push(cell); cell = ''; }
      else if (char === '\n') { row.push(cell.replace(/\r$/, '')); rows.push(row); row = []; cell = ''; }
      else cell += char;
    }
    if (quoted) throw new TypeError('Survey CSV contains an unterminated quoted field.');
    if (cell.length || row.length) { row.push(cell.replace(/\r$/, '')); rows.push(row); }
    while (rows.length && rows[rows.length - 1].every(value => !text(value))) rows.pop();
    if (rows.length < 2) throw new TypeError('Survey CSV has no response rows.');
    return aggregateRows(rows[0], rows.slice(1));
  }

  function aggregateRows(headers, rows, weights = [5, 3, 1]) {
    if (!Array.isArray(headers) || !Array.isArray(rows)) throw new TypeError('Expected survey headers and rows.');
    const normalized = headers.map(value => text(value).toLowerCase());
    const rankColumns = weights.map((_, index) => {
      const rank = index + 1;
      return normalized.findIndex(header => header.includes('which 3 levels') && new RegExp(`#\\s*${rank}(?:\\s|\\]|$)`).test(header));
    });
    const favoriteColumn = normalized.findIndex(header => header.includes('top 1 favorite level'));
    if (rankColumns.some(index => index < 0)) throw new TypeError('Could not find the #1, #2 and #3 improvement columns.');
    if (favoriteColumn < 0) throw new TypeError('Could not find the favorite-level column.');

    const improvement = new Map(), favorites = new Map(), levels = new Set();
    let responseCount = 0;
    for (const row of rows) {
      if (!Array.isArray(row) || !row.some(value => text(value))) continue;
      responseCount++;
      rankColumns.forEach((column, index) => {
        const level = levelName(row[column]);
        if (!level) return;
        levels.add(level); improvement.set(level, (improvement.get(level) || 0) + weights[index]);
      });
      const favorite = levelName(row[favoriteColumn]);
      if (favorite) { levels.add(favorite); favorites.set(favorite, (favorites.get(favorite) || 0) + 1); }
    }
    const rowsFor = source => [...levels].map(level => ({ level_name: level, votes: source.get(level) || 0 }));
    return {
      game_version: null,
      response_count: responseCount,
      rank_weights: { first: weights[0], second: weights[1], third: weights[2] },
      dislikes: rowsFor(improvement),
      favorites: rowsFor(favorites)
    };
  }
  return { parseCsv, aggregateRows, levelName };
});
