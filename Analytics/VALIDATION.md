# Validation

Checked on October 19, 2026 (America/Los_Angeles).

- Data and reader tests: 15 passed. Covered exact sample values, valid zeroes, skipped sentinels, repeat visits, sessions, versions, malformed values, empty snapshots, preference totals, GET paths, permission failures, invalid responses, and network errors.
- Browser checks: 19 passed. Covered all four chart states, sample values, complete-snapshot replacement, JSON imports, mocked database loading, version filters, preference imports and isolation, plain-text level names, 390px layout, and clearing the report.
- Browser page errors: 0. External network requests from the page during the checks: 0.

Expected sample output: Level03 has 6 deaths, an average duration of 48.37 seconds, and one valid sample. Level04 has one skip and no death/time samples. Preference charts show an empty state until vote totals are supplied.

Firebase tests used mocked responses. These are local automated checks; they do not establish production database access or verify real player/survey data.
