/* EXACT teammate-provided example. These are example events, not verified playtest results. */
(function (root) {
  const data = {
    analytics_events: {
      '-ObA12ExamplePushId1': {
        session_id: '4f7e8e68dc51461c98ae76ae1628cb81', event_type: 'level_result',
        level_name: 'Level03', result: 'completed', deaths: 6, duration_seconds: 48.37,
        visit_number: 1, was_personal_best: true, game_version: '0.1.0',
        client_timestamp_utc: '2026-10-20T04:42:18.3812040Z'
      },
      '-ObA12ExamplePushId2': {
        session_id: '4f7e8e68dc51461c98ae76ae1628cb81', event_type: 'level_result',
        level_name: 'Level04', result: 'skipped', deaths: -1, duration_seconds: -1.0,
        visit_number: 1, was_personal_best: false, game_version: '0.1.0',
        client_timestamp_utc: '2026-10-20T04:44:03.1205310Z'
      }
    }
  };
  if (typeof module === 'object' && module.exports) module.exports = data;
  else root.TeammateExample = data;
})(typeof globalThis !== 'undefined' ? globalThis : this);
