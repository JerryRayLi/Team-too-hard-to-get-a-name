# Validation

Checked on October 28, 2026 (America/Los_Angeles), after switching deaths and completion time to box plots.

- Data and reader tests: 18 passed. Covered exact sample values, valid zeroes, skipped sentinels, repeat visits, sessions, versions, malformed values, empty snapshots, preference totals, quartiles and 1.5 × IQR outliers, median versus an outlier-inflated mean, sample-mode distributions, GET paths, permission failures, invalid responses, and network errors.
- Browser checks: 22 passed. Covered all four chart states, sample values, the total-deaths bar view, box plots drawn only at 5 or more samples, every sample drawn as a point, a pinned and labeled outlier, the median in the chart and table, complete-snapshot replacement, JSON imports, mocked database loading, version filters, preference imports and isolation, plain-text level names, 390px layout, and clearing the report.
- Browser page errors: 0. External network requests from the page during the checks: 0.

Expected sample output: Level03 has 6 deaths, a 48.37-second duration, and one valid sample, drawn as a point with its median (no box). Level04 has one skip and no death/time samples. Preference charts show an empty state until vote totals are supplied.

The box-plot checks use synthetic test-only data (six times of 40–50 s plus one 600 s outlier). Firebase tests used mocked responses. These are local automated checks; they do not establish production database access or verify real player/survey data.
