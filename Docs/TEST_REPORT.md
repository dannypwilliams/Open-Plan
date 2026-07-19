# Test Report

Worker-placement pivot checkpoint 3 on Unity 6000.5.1f1:

| Suite | Passed | Failed | Duration |
|---|---:|---:|---:|
| EditMode | 30 | 0 | 0.070 s |
| PlayMode | 26 | 0 | 38.632 s |
| Total | 56 | 0 | 38.702 s |

The pre-edit baseline passed 24 EditMode and 13 PlayMode tests (37 total).

EditMode coverage now also verifies the strict greater-than-six-pixel drag threshold, 0.12-second hold threshold, and both cancellation inputs.

PlayMode coverage verifies click-only selection, UI suppression, carry-state suspension, valid command issue, final walking movement, ordinary-floor rejection, occupied and locked rejection, exact restoration, modal cancellation, pause behavior, away/fired safety, restart and scene cleanup, and feedback layout at 1280x720 and 1920x1080. Prior Starter and Established Office integration coverage remains in the same suite.

The Windows player build and Blender validation (54/54) also passed. The packaged player completed a 12-check Input System mouse smoke at both target resolutions; reports and valid/invalid screenshots are preserved under `outputs/Screenshots`. Historical release evidence from `a638304` remains preserved.
