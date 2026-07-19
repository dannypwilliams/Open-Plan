# Test Report

Final command-line run on Unity 6000.5.1f1:

| Suite | Passed | Failed | Duration |
|---|---:|---:|---:|
| EditMode | 24 | 0 | 0.047 s |
| PlayMode | 13 | 0 | 25.235 s |
| Total | 37 | 0 | 25.282 s |

EditMode coverage includes productivity/clamping, traits, nearby effects, needs, cooldowns, tasks, revenue, payroll, hiring cost/capacity, firing cost, desk assignment, summary arithmetic, seeded randomness, simulation speed, and zoom bounds.

PlayMode coverage loads both scenes and validates runtime construction, six-worker spawn, desk arrival/work, coffee, water, social completion, return-to-desk, hiring, firing/box exit, reassignment, task/revenue flow, report display, and 1280×720/1920×1080 UI construction.

Preserved final evidence: `Logs/EditMode-results-release.xml`, `Logs/PlayMode-results-release.xml`, `Logs/editmode-release.log`, and `Logs/playmode-release.log`.

The final standalone package also ran an end-to-end verification from a fresh ZIP extraction: main menu, start, select, 4× speed, hire, reassign, fire, finish, restart, return to menu, and normal close all logged PASS in `Logs/package-verification-release.log`.
