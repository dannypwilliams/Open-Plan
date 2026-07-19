# Test Report

Worker-placement pivot checkpoint 6 on Unity 6000.5.1f1:

| Suite | Passed | Failed | Duration |
|---|---:|---:|---:|
| EditMode | 41 | 0 | 0.066 s |
| PlayMode | 47 | 0 | 88.319 s |
| Total | 88 | 0 | 88.385 s |

The pre-edit baseline passed 24 EditMode and 13 PlayMode tests (37 total).

EditMode coverage retains the full placement, needs, personality, activity, and cash model and adds the exact $1,000 affordability boundary, purchase progress, no-repeat condition, and 6.26-minute starting-team affordability estimate.

PlayMode coverage retains all prior activity and behavior checks and adds below/above-price availability, vending disabling affordability, exact one-time purchase deduction, the live wall/light/trim/zone/navigation/camera transition, three-desk capacity increase, unassigned entrance hiring and drag placement, indefinite post-expansion stability, untimed Established preview return, and locked/expanded restart reconstruction.

The Windows player build also passed. Its expansion evidence run deducted exactly $1,000, reported the wall open, trim visible, navigation enabled, capacity six, and pan width 26. Before/after overviews and the machine-readable report are preserved under `outputs/Screenshots`. Historical release evidence from `a638304` remains preserved.
