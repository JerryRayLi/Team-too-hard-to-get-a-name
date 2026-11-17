const test = require('node:test');
const assert = require('node:assert/strict');
const survey = require('./survey-pipeline.js');

test('survey CSV applies 5-3-1 improvement weights and counts favorites', () => {
  const csv = [
    'Timestamp,"Which 3 levels do you think need the most improvement in their design?\n [#1 ]","Which 3 levels do you think need the most improvement in their design?\n [#2]","Which 3 levels do you think need the most improvement in their design?\n [#3]",Top 1 Favorite Level!',
    '2026-11-09,Slow,Corpse2,Demo4,Reverse',
    '2026-11-09,Demo4,Spikes,Corpse1,Spikes',
    '2026-11-09,Slow,Spikes,Door,Demo Level 4'
  ].join('\n');
  const result = survey.parseCsv(csv);
  const improvement = Object.fromEntries(result.dislikes.map(row => [row.level_name, row.votes]));
  const favorites = Object.fromEntries(result.favorites.map(row => [row.level_name, row.votes]));
  assert.equal(result.response_count, 3);
  assert.equal(improvement['SLOW 1'], 10);
  assert.equal(improvement.INITIATION04, 6);
  assert.equal(improvement.SPIKES, 6);
  assert.equal(improvement.CORPSE01, 1);
  assert.equal(favorites.SPIKES, 1);
  assert.equal(favorites.REVERSE, 1);
  assert.equal(favorites.INITIATION04, 1);
});

test('survey parser handles quoted commas and newlines', () => {
  const csv = 'Timestamp,"Which 3 levels [#1]","Which 3 levels [#2]","Which 3 levels [#3]",Top 1 Favorite Level!,Comment\n' +
    'x,Demo1,Demo2,Demo3,Door,"line one, still one field\nline two"';
  const result = survey.parseCsv(csv);
  assert.equal(result.response_count, 1);
  assert.equal(result.dislikes.find(row => row.level_name === 'INITIATION01').votes, 5);
  assert.equal(result.favorites.find(row => row.level_name === 'DOOR01').votes, 1);
});
