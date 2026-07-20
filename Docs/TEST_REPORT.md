# Test Report

Worker-placement friend-demo release on Unity 6000.5.1f1:

| Suite | Passed | Failed | Duration |
|---|---:|---:|---:|
| EditMode | 49 | 0 | 0.222 s |
| PlayMode | 55 | 0 | 90.154 s |
| Total | 104 | 0 | 90.376 s |

EditMode coverage includes deterministic simulation speed, cash, productivity, exact activity effects, personalities, expansion affordability, tutorial contracts, the runtime camera-profile resource, and five release scenarios across 20 fixed seeds. PlayMode covers stage initialization, pickup/drag/cancel/reject, all activity lifecycles and cleanup, needs, personality status, cash, expansion and wall state, hiring after expansion, tutorial progression, modal ownership, restart behavior, UI at 1280x720 and 1920x1080, and the Established Office systems.

## Standalone and package gates

- Input smoke: 12/12 at 1280x720 and 12/12 at 1920x1080.
- Water lifecycle smoke: 9/9.
- Twenty-minute simulated behavior soak at 20x: 27 observations passed; 57 distractions; no permanent stuck/idle worker, stale carry, orphaned smoking effect, missing roster member, or capacity violation.
- Friend-demo extracted package: 86 checks, 0 failures, 21 real-game screenshots, no artificial purchase funds.
- Package lifecycle checks passed for Starter, pre-expanded Starter, and Established stages, including restart, menu return, and clean quit. The Established run also passed hire, reassignment, fire, and finish-day behavior.
- Build: non-development Windows x64 player succeeded at 103,051,945 reported bytes.

The exact extracted executable completed pickup, valid and invalid placement, all six player activities, a natural distraction and redirection, live income to $1,000, exact purchase deduction, wall opening, continued earnings, a new hire placement, Established preview launch, menu return, and clean quit.
