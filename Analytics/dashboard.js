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
  function draw(id, entries, title, unit) {
    if (!entries.length) { empty(id, 'No data', 'Load or import data.'); return; }
    const width = Math.max(470, entries.length * 82 + 72);
    const left = 54, right = 18, top = 28, bottom = 182;
    const plot = width - left - right;
    const max = Math.max(1, ...entries.map(item => item.value === null ? 0 : item.value));
    const power = 10 ** Math.floor(Math.log10(max / 4));
    const rough = max / 4 / power;
    const step = (rough <= 1 ? 1 : rough <= 2 ? 2 : rough <= 5 ? 5 : 10) * power;
    const limit = Math.ceil(max / step) * step;
    const svg = svgElement('svg', { viewBox: `0 0 ${width} 260`, role: 'img', 'aria-label': title });
    svg.style.minWidth = `${width}px`;
    svg.append(svgElement('title', {}, title));
    const ticks = Math.round(limit / step);
    for (let index = 0; index <= ticks; index++) {
      const tick = index * step;
      const y = bottom - tick / limit * (bottom - top);
      svg.append(svgElement('line', { x1: left, y1: y, x2: width - right, y2: y, class: index === 0 ? 'baseline' : 'gridline' }));
      svg.append(svgElement('text', { x: left - 10, y: y + 4, 'text-anchor': 'end', class: 'tick' }, format(tick)));
    }
    entries.forEach((item, index) => {
      const slot = plot / entries.length;
      const center = left + slot * (index + .5);
      const barWidth = Math.min(48, slot * .56);
      const missing = item.value === null;
      const description = `${item.level}: ${missing ? 'no valid sample' : format(item.value) + ' ' + unit}${item.n === undefined ? '' : ', ' + item.n + ' samples'}`;
      if (!missing) {
        const height = item.value / limit * (bottom - top);
        const mark = item.value === 0
          ? svgElement('circle', { cx: center, cy: bottom, r: 3, class: 'bar', tabindex: 0, 'aria-label': description })
          : svgElement('rect', { x: center - barWidth / 2, y: bottom - height, width: barWidth, height, rx: 3, class: 'bar', tabindex: 0, 'aria-label': description });
        mark.append(svgElement('title', {}, description));
        svg.append(mark);
        svg.append(svgElement('text', { x: center, y: bottom - height - 10, 'text-anchor': 'middle', class: 'value-label' }, format(item.value)));
      } else {
        svg.append(svgElement('text', { x: center, y: bottom - 12, 'text-anchor': 'middle', class: 'sample-label' }, 'No sample'));
      }
      const label = svgElement('text', { x: center, y: 208, 'text-anchor': 'middle', class: 'level-label' }, item.level.length > 16 ? item.level.slice(0, 14) + '…' : item.level);
      label.append(svgElement('title', {}, item.level));
      svg.append(label);
      if (item.n !== undefined) svg.append(svgElement('text', { x: center, y: 228, 'text-anchor': 'middle', class: 'sample-label' }, `n=${item.n}`));
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
    const average = $('death-mode').value === 'mean';
    $('death-description').textContent = average ? 'Average · completed visits' : 'Total · completed visits';
    draw('death-chart', summary.levels.map(row => ({ level: row.level, value: row.deathSamples ? (average ? row.deathMean : row.deathTotal) : null, n: row.deathSamples })), 'Deaths by level', 'deaths');
    draw('time-chart', summary.levels.map(row => ({ level: row.level, value: row.durationMean, n: row.timeSamples })), 'Mean completion time by level', 'seconds');
    drawPreferences();
    const rows = summary.levels.map(row => {
      const element = document.createElement('tr');
      for (const value of [row.level, row.visits, row.completedVisits, row.skipped, row.other, row.deathSamples, row.timeSamples, row.deathSamples ? row.deathTotal : null, row.durationMean]) {
        const cell = document.createElement('td');
        cell.textContent = typeof value === 'string' ? value : format(value);
        element.append(cell);
      }
      return element;
    });
    if (!rows.length) {
      const row = document.createElement('tr');
      const cell = document.createElement('td');
      cell.colSpan = 9; cell.textContent = 'No data.'; row.append(cell); rows.push(row);
    }
    $('rows').replaceChildren(...rows);
    const notes = [$('sample').value === 'all' ? 'All completed visits.' : 'First completion per session, level and version.', 'Skipped visits are excluded from both charts.'];
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
