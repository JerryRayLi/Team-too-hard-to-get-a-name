# Level analytics

Open `index.html` in a browser. No installation, build, or server is required.

The dashboard reads the team's `analytics_events` format and provides four charts:

1. Deaths per completion by level, as a box plot, with a total-deaths bar view.
2. Completion time by level, in seconds, as a box plot.
3. The three least-liked levels, from imported vote totals.
4. Favorite levels, from imported vote totals.

Use **Load database** to read the configured Firebase event collection, **Import JSON** for an exported snapshot, and **Import votes** for preference totals. **Sample data** loads the two records supplied by the team: Level03 has 6 deaths and a 48.37-second completion; Level04 was skipped. Samples are labeled on the page.

## Connect data

`firebase-reader.js` contains the team's database URL. The button makes one GET request to `/analytics_events.json`, with a 15-second timeout. It does not authenticate, write data, or poll automatically. If the database requires authentication, connect the team's approved reader to `setData()` or import an exported JSON snapshot. Read failures preserve the previous chart.

Pass a complete snapshot each time; it replaces the previous snapshot:

```js
// Full database export or a bare event collection both work.
PlaytestDashboard.setData({ analytics_events: events }, { source: 'Team analytics' });
PlaytestDashboard.setData(events, { source: 'Team analytics' });
```

Call after the scripts load. Connection code can listen for `playtest-dashboard-ready`. For a child-added subscription, collect events by ID before passing the complete collection; this API does not append individual events.

Preference totals have a separate input because `level_result` events contain no preference fields. Pass actual aggregated survey results in this shape, or import the equivalent JSON:

```js
PlaytestDashboard.setPreferences({
  game_version: '0.1.0',
  favorites: [/* { level_name: 'Level03', votes: actualCount } */],
  dislikes: [/* { level_name: 'Level04', votes: actualCount } */]
});
```

Votes must be nonnegative integers. Duplicate level names are combined; the least-liked chart shows the three highest totals, with level-name ordering for ties. Preference versions must match the selected game version. Survey collection, ranking-to-score conversion, and duplicate-response handling belong to the data source.

## Calculation rules

- Only `level_result` events are used. Completed results supply death and time metrics; skipped `-1` values are excluded and shown as missing samples.
- By default, each session, level, and version contributes its first completion, ordered by visit number and then timestamp. **All completions** includes repeat completions.
- Versions are separate by default. Select **All versions** to combine them explicitly.
- Deaths must be nonnegative integers; durations must be nonnegative finite numbers. Zero is valid. The two metrics have separate sample counts.
- Deaths and completion time use medians and box plots rather than means, so one stuck player does not distort a level. The box spans Q1–Q3 (linear interpolation, as in Excel `QUARTILE.INC`), the line and label show the median, and whiskers reach the furthest values within 1.5 × IQR. Values beyond the whiskers are red outliers; they are kept, not removed.
- Every sample is drawn as a point. Levels with fewer than 5 samples show points and the median only, because quartiles of so few values are not meaningful. The axis is scaled to the whiskers; outliers above it are pinned to the top edge with their value.
- No valid samples are shown as **No sample**, rather than zero. Sessions may include repeat players. Duration uses the value reported by the game.
- Personal-best flags do not filter the dataset. Replacing a snapshot does not add its counts again. Duplicate events with different IDs still require upstream deduplication.
- Missing preference data is shown as an empty state. No player data or votes are invented.

## Files and checks

- `analytics.js`: parsing, filters, aggregation, box-plot statistics (`describe`), and preference totals.
- `dashboard.js`: SVG box plots and bar charts, imports, controls, and `PlaytestDashboard` API.
- `firebase-reader.js`: explicit Firebase GET reader.
- `sample-data.js`: the team's two example records.
- `index.html`, `styles.css`: page and responsive layout.

Run the data tests with Node.js:

```sh
node --test --test-isolation=none analytics.test.cjs firebase-reader.test.cjs
```

For optional browser checks, install Playwright and its Chromium browser, then run `node browser-check.cjs`. The script also accepts an installed Playwright module path and a Chrome executable path. It uses an isolated temporary browser profile and writes desktop/mobile previews.

See `VALIDATION.md` for the test results. Database responses in the automated checks are mocked; live database access and survey collection are outside these checks.
