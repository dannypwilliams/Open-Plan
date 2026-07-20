# Test Report - Endless Office Alpha Checkpoint 02

Checkpoint `02_NeedAutonomy` on Unity 6000.5.1f1:

| Suite | Passed | Failed |
|---|---:|---:|
| EditMode | 138 | 0 |
| PlayMode | 110 | 0 |
| Total | 248 | 0 |

The package workflow reruns both complete suites from the final committed source and records XML and logs beside the player. The totals preserve all 98/70 Checkpoint 01 contracts and add 40 EditMode plus 40 PlayMode autonomy cases.

EditMode coverage includes deterministic evaluation intervals, need priority, critical bounds, mappings, centralized scoring, stable ties, multi-need choices, eligibility filters, unaffordable/off-site fallbacks, player authority, hysteresis, emergency interruption, reservation ownership/capacity/idempotence/expiration, pause equivalence, phone scoring, desk preservation, locked/void path rejection, bounded stuck recovery, cleanup, safety points, and finite scores.

PlayMode coverage includes all five autonomous responses; affordable and unaffordable Hunger paths; Restroom contention and fallback; arrival-before-activity; wall/partition routing; locked-unit exclusion; arbitrary-ground recovery; desk and phone return; pickup, firing, disablement, restart, and scene-exit cleanup; pause/resume; critical interruption of distractions, social behavior, and player commands; bounded failure recovery; inspector/world explanations; preserved placement, proximity, phone productivity, hiring, exact-once costs, cooldowns; and a passive simulated workday.

## Deterministic autonomy matrix

`02_NeedAutonomy_Simulation_Report.md` covers 20 seeds, 3/10/30 workers, 15 scenarios, ten simulated minutes per row, and one extended 100-minute run for each roster size: 903 runs total.

- Outcome: PASS.
- Critical needs observed: 28,331.
- Average critical threshold-to-response: 0.74 s.
- Average response-to-recovery: 30.13 s.
- Maximum critical duration: 34.00 s, including documented slower off-site fallback.
- Reservations created/released: 30,036 / 30,036.
- Invalid need values, orphaned reservations, capacity violations, deterministic divergences, failed work resumes, lost desks, and duplicate charges: 0.
- Active-management output advantage: 10.1%, inside the 10-25% target.
- Runtime decision scans are staggered; stable stations are cached; selected paths are reused; no complete decision scan or path request runs every frame.
- The matrix reports setup/candidate managed allocations rather than profiler-grade runtime allocation data.

Live PlayMode tests separately force repath, stuck recovery, station disablement, cleanup, and final safety behavior. Zero matrix reroutes or stuck corrections means its normal deterministic scenarios did not require those exceptional paths, not that the paths are untested.

## Exact package gate

- Windows x64, non-development, Mono player built from clean committed source.
- Fresh `SillyOfficeSim_02_NeedAutonomy_Windows.zip` generated from `Windows/` without overwriting prior evidence.
- That exact ZIP is extracted and preserved under `VerifiedExtract/`.
- The exact extracted executable launches visibly at 1920x1080 and uses public gameplay APIs from Main Menu through natural cash, a deskless hire, natural need pressure, player commands, a critical Bathroom override, autonomous recovery, desk/phone return, ten simulated minutes, menu return, and clean quit.
- No artificial cash or direct need mutation is used.
- Twelve genuine 1920x1080 gameplay screenshots are dimension-checked and then visually inspected.

Historical Checkpoint 00, Checkpoint 01, friend-demo, previous-release, and release evidence remains preserved.
