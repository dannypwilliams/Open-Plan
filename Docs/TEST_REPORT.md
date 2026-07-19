# Test Report

Worker-placement pivot checkpoint 4 on Unity 6000.5.1f1:

| Suite | Passed | Failed | Duration |
|---|---:|---:|---:|
| EditMode | 35 | 0 | 0.082 s |
| PlayMode | 36 | 0 | 73.265 s |
| Total | 71 | 0 | 73.347 s |

The pre-edit baseline passed 24 EditMode and 13 PlayMode tests (37 total).

EditMode coverage verifies the placement thresholds plus the Energy/Mood/Stress productivity formula, 0.10x-2.50x clamp, non-stacking Focused Work modifier, all exact activity deltas, durations, cooldowns, $15 snack cost, deterministic 10% malfunction rule, needs clamping, and continuous cash math.

PlayMode coverage additionally completes Work, Rest, Water, normal and malfunctioning vending, Smoke, and Leave Office lifecycles. It verifies exact effects and cooldowns, a single vending charge, insufficient-cash rejection, Focused Work refresh, paused income, water social opportunity, smoke prop/particle cleanup, interruption safety, away recovery/return, firing while away, and prior Starter/Established integration behavior.

The Windows player build and Blender validation (54/54) also passed. The packaged player completed the existing 12-check Input System mouse smoke at both target resolutions and a 9-observation full Get Water activity cycle. Reports and screenshots are preserved under `outputs/Screenshots`. Historical release evidence from `a638304` remains preserved.
