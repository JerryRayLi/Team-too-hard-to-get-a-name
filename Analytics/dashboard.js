(function () {
  'use strict';
  const metrics = window.PlaytestMetrics;
  const $ = id => document.getElementById(id);
  const ns = 'http://www.w3.org/2000/svg';
  let data = metrics.normalize(null);
  let preferences = null;
  let isExample = false;
  let databaseAbort = null;
  const format = (value, decimals = 2) => value === null ? '—' : value.toLocaleString('en', { maximumFractionDigits: decimals });

  function message(value, error = false) {
    $('message').textContent = value;
    $('message').classList.toggle('error', error);
  }
  function empty(id, title, description) {
    const box = document.createElement('div');
    box.className = 'empty';
    const heading = document.createElement('strong');
    heading.textContent = title;
    const body = document.createElement('span');
    body.textContent = description;
    box.append(heading, body);
    $(id).replaceChildren(box);
  }
  function svgElement(tag, attributes = {}, text) {
    const element = document.createElementNS(ns, tag);
    for (const [key, value] of Object.entries(attributes)) element.setAttribute(key, String(value));
    if (text !== undefined) element.textContent = text;
    return element;
  }
  // Shared axis, gridlines and level labels. Returns the scale used by the marks.
  function frame(entries, title, max, integer = false) {
    const width = Math.max(470, entries.length * 82 + 72);
    const left = 54, right = 18, top = 28, bottom = 182;
    const plot = width - left - right;
    max = Math.max(1, max);
    const power = 10 ** Math.floor(Math.log10(max / 4));
    const rough = max / 4 / power;
    const nice = (rough <= 1 ? 1 : rough <= 2 ? 2 : rough <= 5 ? 5 : 10) * power;
    const step = integer ? Math.max(1, nice) : nice;
    const limit = Math.ceil(max / step) * step;
    const svg = svgElement('svg', { viewBox: `0 0 ${width} 260`, role: 'img', 'aria-label': title });
    svg.style.minWidth = `${width}px`;
    svg.append(svgElement('title', {}, title));
    const y = value => bottom - value / limit * (bottom - top);
    const ticks = Math.round(limit / step);
    for (let index = 0; index <= ticks; index++) {
      const tick = index * step;
      svg.append(svgElement('line', { x1: left, y1: y(tick), x2: width - right, y2: y(tick), class: index === 0 ? 'baseline' : 'gridline' }));
      svg.append(svgElement('text', { x: left - 10, y: y(tick) + 4, 'text-anchor': 'end', class: 'tick' }, format(tick)));
    }
    const slot = plot / entries.length;
    const center = index => left + slot * (index + .5);
    entries.forEach((item, index) => {
      const label = svgElement('text', { x: center(index), y: 208, 'text-anchor': 'middle', class: 'level-label' }, item.level.length > 16 ? item.level.slice(0, 14) + '…' : item.level);
      label.append(svgElement('title', {}, item.level));
      svg.append(label);
      if (item.n !== undefined) svg.append(svgElement('text', { x: center(index), y: 228, 'text-anchor': 'middle', class: 'sample-label' }, `n=${item.n}`));
    });
    return { svg, y, top, bottom, limit, slot, center };
  }
  function mark(svg, tag, attributes, description) {
    const element = svgElement(tag, { ...attributes, tabindex: 0, 'aria-label': description });
    element.append(svgElement('title', {}, description));
    svg.append(element);
  }
  function noSample(svg, x, bottom) {
    svg.append(svgElement('text', { x, y: bottom - 12, 'text-anchor': 'middle', class: 'sample-label' }, 'No sample'));
  }
  function draw(id, entries, title, unit) {
    if (!entries.length) { empty(id, 'No data', 'Load or import data.'); return; }
    const { svg, y, bottom, slot, center } = frame(entries, title, Math.max(...entries.map(item => item.value === null ? 0 : item.value)));
    const barWidth = Math.min(48, slot * .56);
    entries.forEach((item, index) => {
      const x = center(index);
      if (item.value === null) { noSample(svg, x, bottom); return; }
      const description = `${item.level}: ${format(item.value)} ${unit}${item.n === undefined ? '' : ', ' + item.n + ' samples'}`;
      if (item.value === 0) mark(svg, 'circle', { cx: x, cy: bottom, r: 3, class: 'bar' }, description);
      else mark(svg, 'rect', { x: x - barWidth / 2, y: y(item.value), width: barWidth, height: bottom - y(item.value), rx: 3, class: 'bar' }, description);
      svg.append(svgElement('text', { x, y: y(item.value) - 10, 'text-anchor': 'middle', class: 'value-label' }, format(item.value)));
    });
    $(id).replaceChildren(svg);
  }
  // Box plot with every sample drawn as a point. Below MIN_BOX samples quartiles are not meaningful,
  // so only the points and the median are shown. The axis is scaled to the whiskers so one extreme
  // value cannot flatten every box; outliers above the axis are pinned to the top edge and labeled.
  const MIN_BOX = 5;
  function drawBoxes(id, entries, title, unit, integer = false) {
    if (!entries.length) { empty(id, 'No data', 'Load or import data.'); return; }
    const reach = stats => stats.n >= MIN_BOX ? stats.whiskerHigh : stats.max;
    // 15% headroom keeps the median label clear of the top edge and pinned outliers.
    const frameParts = frame(entries, title, 1.15 * Math.max(0, ...entries.map(item => item.stats ? reach(item.stats) : 0)), integer);
    const { svg, top, bottom, limit, slot, center } = frameParts;
    const y = value => frameParts.y(Math.min(value, limit));
    const boxWidth = Math.min(44, slot * .5);
    entries.forEach((item, index) => {
      const x = center(index), stats = item.stats;
      if (!stats) { noSample(svg, x, bottom); return; }
      const boxed = stats.n >= MIN_BOX;
      if (boxed) {
        for (const [edge, end] of [[stats.q1, stats.whiskerLow], [stats.q3, stats.whiskerHigh]]) {
          svg.append(svgElement('line', { x1: x, y1: y(edge), x2: x, y2: y(end), class: 'whisker' }));
          svg.append(svgElement('line', { x1: x - boxWidth / 4, y1: y(end), x2: x + boxWidth / 4, y2: y(end), class: 'whisker' }));
        }
        mark(svg, 'rect', { x: x - boxWidth / 2, y: y(stats.q3), width: boxWidth, height: Math.max(1, y(stats.q1) - y(stats.q3)), class: 'box' },
          `${item.level}: median ${format(stats.median)} ${unit}, middle half ${format(stats.q1)}–${format(stats.q3)}, ${stats.n} samples, ${stats.outliers.length} outliers`);
      }
      svg.append(svgElement('line', { x1: x - boxWidth / 2, y1: y(stats.median), x2: x + boxWidth / 2, y2: y(stats.median), class: 'median' }));
      // Deterministic jitter: repeated values (common for death counts) fan out sideways instead of overlapping.
      const seen = new Map();
      const clipped = stats.values.filter(value => value > limit);
      for (const value of stats.values) {
        const k = seen.get(value) || 0;
        seen.set(value, k + 1);
        const offset = (k % 2 ? -1 : 1) * Math.ceil(k / 2) * 5;
        const outlier = boxed && stats.outliers.includes(value);
        const cx = x + Math.max(-boxWidth / 2, Math.min(boxWidth / 2, offset));
        const description = `${item.level}: ${format(value)} ${unit}${outlier ? ' (outlier)' : ''}`;
        if (value > limit) mark(svg, 'path', { d: `M${cx} ${top - 10}l-4.5 7h9z`, class: 'point outlier clipped' }, description + ', above axis');
        else mark(svg, 'circle', { cx, cy: y(value), r: 3.5, class: outlier ? 'point outlier' : 'point' }, description);
      }
      if (clipped.length) {
        const text = clipped.length === 1 ? format(clipped[0]) : `${clipped.length} up to ${format(clipped[clipped.length - 1])}`;
        svg.append(svgElement('text', { x: x + boxWidth / 2 + 6, y: top - 4, class: 'sample-label outlier-label' }, `↑ ${text}`));
      }
      svg.append(svgElement('text', { x, y: y(reach(stats)) - 10, 'text-anchor': 'middle', class: 'value-label' }, format(stats.median)));
    });
    $(id).replaceChildren(svg);
  }
  function getSummary() {
    return metrics.summarize(data, { version: $('version').value || null, sample: $('sample').value });
  }
  function drawPreferences() {
    const version = $('version').value;
    let reason = null;
    if (!preferences) reason = 'Import vote totals.';
    else if (version && preferences.version !== version) reason = preferences.version
      ? `Votes are for version ${preferences.version}. Select that version.`
      : 'Vote data has no version.';
    for (const [id, field, title] of [['dislike-chart', 'dislikes', 'Least liked (top 3)'], ['favorite-chart', 'favorites', 'Most liked']]) {
      if (reason) { empty(id, 'No vote data', reason); continue; }
      const ranked = preferences[field].filter(row => row.votes > 0);
      const rows = field === 'dislikes' ? ranked.slice(0, 3) : ranked;
      if (!rows.length) empty(id, 'No votes', 'No votes in this dataset.');
      else draw(id, rows.map(row => ({ level: row.level, value: row.votes })), title, 'votes');
    }
  }
  function render() {
    const summary = getSummary();
    for (const key of ['events', 'sessions', 'completed', 'skipped']) $(key).textContent = format(summary[key], 0);
    const total = $('death-mode').value === 'total';
    $('death-description').textContent = total ? 'Total · completed visits' : 'Box plot · median label · completed visits';
    if (total) draw('death-chart', summary.levels.map(row => ({ level: row.level, value: row.deathSamples ? row.deathTotal : null, n: row.deathSamples })), 'Total deaths by level', 'deaths');
    else drawBoxes('death-chart', summary.levels.map(row => ({ level: row.level, stats: row.deathStats, n: row.deathSamples })), 'Deaths per completion by level', 'deaths', true);
    drawBoxes('time-chart', summary.levels.map(row => ({ level: row.level, stats: row.durationStats, n: row.timeSamples })), 'Completion time by level', 'seconds');
    drawPreferences();
    const rows = summary.levels.map(row => {
      const element = document.createElement('tr');
      for (const value of [row.level, row.visits, row.completedVisits, row.skipped, row.other, row.deathSamples, row.timeSamples, row.deathSamples ? row.deathTotal : null, row.deathMedian, row.durationMedian]) {
        const cell = document.createElement('td');
        cell.textContent = typeof value === 'string' ? value : format(value);
        element.append(cell);
      }
      return element;
    });
    if (!rows.length) {
      const row = document.createElement('tr');
      const cell = document.createElement('td');
      cell.colSpan = 10; cell.textContent = 'No data.'; row.append(cell); rows.push(row);
    }
    $('rows').replaceChildren(...rows);
    const notes = [$('sample').value === 'all' ? 'All completed visits.' : 'First completion per session, level and version.', 'Skipped visits are excluded from both charts.',
      `Box plots: the box spans the middle half (Q1–Q3, linear interpolation), the line and label show the median, whiskers reach the furthest values within 1.5 × IQR, and red points are outliers (values above the axis are pinned to the top edge with their value). Every sample is drawn as a point; levels with fewer than ${MIN_BOX} samples show points and the median only.`];
    if (summary.excludedCompletions) notes.push(`Repeat completions excluded: ${summary.excludedCompletions}.`);
    if (summary.invalidMetrics) notes.push(`Completions with missing metrics: ${summary.invalidMetrics}.`);
    if (summary.ignored) notes.push(`Other events excluded: ${summary.ignored}.`);
    if (summary.invalid) notes.push(`Invalid records excluded: ${summary.invalid}.`);
    $('method').textContent = notes.join(' ');
    return summary;
  }
  function setData(payload, options = {}) {
    const next = metrics.normalize(payload); // Validate before replacing the current report.
    if (databaseAbort) databaseAbort.abort();
    const previousVersion = $('version').value;
    const available = metrics.versions(next.events);
    data = next;
    isExample = options.example === true;
    if (options.resetPreferences) preferences = null;
    const choices = available.map(value => {
      const option = document.createElement('option'); option.value = value; option.textContent = value; return option;
    });
    if (available.length > 1) {
      const all = document.createElement('option'); all.value = ''; all.textContent = 'All versions (combined)'; choices.push(all);
    }
    if (!choices.length) {
      const option = document.createElement('option'); option.value = ''; option.textContent = 'No versions'; choices.push(option);
    }
    $('version').replaceChildren(...choices);
    $('version').disabled = available.length < 2;
    $('version').value = available.includes(previousVersion) ? previousVersion : available[0] || '';
    $('source').textContent = isExample ? 'Sample data' : String(options.source || 'Supplied data').slice(0, 100);
    $('source').classList.toggle('example', isExample);
    const summary = render();
    message(`${next.events.length} ${isExample ? 'sample ' : ''}results loaded.`);
    return summary;
  }
  function setPreferences(payload) {
    const next = metrics.normalizeVotes(payload);
    preferences = next;
    drawPreferences();
    message('Vote totals loaded.');
    return next;
  }
  function clear() {
    if (databaseAbort) databaseAbort.abort();
    data = metrics.normalize(null); preferences = null; isExample = false;
    $('version').replaceChildren(new Option('No versions', '')); $('version').disabled = true;
    $('source').textContent = 'No data'; $('source').classList.remove('example');
    $('file').value = ''; $('votes-file').value = '';
    render(); message('Cleared.');
  }
  async function importFile(input, handler) {
    const file = input.files[0];
    if (!file) return;
    if (file.size > 25 * 1024 * 1024) { message('File limit: 25 MB.', true); input.value = ''; return; }
    try { handler(JSON.parse(await file.text()), file.name); }
    catch (error) { message(`Import failed: ${error instanceof SyntaxError ? 'Invalid JSON' : error.message}. Previous data kept.`, true); }
    finally { input.value = ''; }
  }
  async function readDatabase() {
    if (databaseAbort) databaseAbort.abort();
    const controller = new AbortController();
    databaseAbort = controller;
    $('database').disabled = true;
    let timedOut = false;
    const timeout = setTimeout(() => { timedOut = true; controller.abort(); }, 15000);
    message('Loading data…');
    try {
      const payload = await window.FirebaseAnalyticsReader.readEvents({ signal: controller.signal });
      if (controller.signal.aborted) return null;
      const summary = setData(payload, { source: 'Database', resetPreferences: true });
      message(summary.events ? `${summary.events} results loaded.` : 'No results in database.');
      return summary;
    } catch (error) {
      if (timedOut) message('Request timed out. Previous data kept.', true);
      else if (!controller.signal.aborted) message(`${error.message} Previous data kept.`, true);
      return null;
    } finally {
      clearTimeout(timeout);
      if (databaseAbort === controller) { databaseAbort = null; $('database').disabled = false; }
    }
  }
  $('file').addEventListener('change', () => importFile($('file'), (payload, name) => setData(payload, { source: `Imported: ${name}`, resetPreferences: true })));
  $('votes-file').addEventListener('change', () => importFile($('votes-file'), setPreferences));
  $('votes').addEventListener('click', () => $('votes-file').click());
  $('example').addEventListener('click', () => setData(window.TeammateExample, { example: true, resetPreferences: true }));
  $('clear').addEventListener('click', clear);
  $('database').addEventListener('click', readDatabase);
  for (const id of ['version', 'sample', 'death-mode']) $(id).addEventListener('change', render);
  window.PlaytestDashboard = Object.freeze({ setData, setPreferences, clear, getSummary, readDatabase });
  render();
  window.dispatchEvent(new Event('playtest-dashboard-ready'));
})();
