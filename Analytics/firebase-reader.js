/* Explicit GET-only Firebase reader. Loading this script makes no network request. */
(function (root, factory) {
  const api = factory();
  if (typeof module === 'object' && module.exports) module.exports = api;
  else root.FirebaseAnalyticsReader = api;
})(typeof globalThis !== 'undefined' ? globalThis : this, function () {
  'use strict';
  const DEFAULT_DATABASE_URL = 'https://color-blink-troll-default-rtdb.firebaseio.com/';
  function buildEventsUrl(databaseUrl = DEFAULT_DATABASE_URL) {
    const url = new URL(databaseUrl);
    if (url.protocol !== 'https:' || !/\.(firebaseio\.com|firebasedatabase\.app)$/.test(url.hostname) ||
        url.username || url.password || url.search || url.hash || url.port || url.pathname !== '/') {
      throw new TypeError('Expected an HTTPS Firebase database root URL without credentials or parameters.');
    }
    return new URL('analytics_events.json', url).href;
  }
  async function readEvents({ databaseUrl = DEFAULT_DATABASE_URL, fetchImpl = globalThis.fetch, signal } = {}) {
    const response = await fetchImpl(buildEventsUrl(databaseUrl), {
      method: 'GET', credentials: 'omit', cache: 'no-store', redirect: 'error',
      headers: { Accept: 'application/json' }, signal
    });
    if (response.status === 401 || response.status === 403) {
      throw new Error('Database access denied.');
    }
    if (!response.ok) throw new Error(`Firebase read failed (HTTP ${response.status}).`);
    let payload;
    try { payload = await response.json(); }
    catch { throw new Error('Firebase did not return valid JSON.'); }
    if (payload !== null && (typeof payload !== 'object' || Array.isArray(payload))) {
      throw new TypeError('Expected a Firebase event collection or null.');
    }
    return payload;
  }
  return { DEFAULT_DATABASE_URL, buildEventsUrl, readEvents };
});
