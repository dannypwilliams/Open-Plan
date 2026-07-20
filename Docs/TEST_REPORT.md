# Test Report

Worker-placement pivot checkpoint 7 on Unity 6000.5.1f1:

| Suite | Passed | Failed | Duration |
|---|---:|---:|---:|
| EditMode | 43 | 0 | 0.129 s |
| PlayMode | 55 | 0 | 92.658 s |
| Total | 98 | 0 | 92.787 s |

The pre-edit baseline passed 24 EditMode and 13 PlayMode tests (37 total).

EditMode coverage retains the full placement, needs, personality, activity, cash, and expansion model and adds the ordered seven-step tutorial copy plus complete mouse, keyboard, speed, and non-color help language.

PlayMode coverage retains all prior behavior and expansion checks and adds the complete observed-event tutorial path, exact speed restoration, skip/replay, early action credit, restart reset, highlighted-worker invalidation, top-modal ownership, input blocking, carry-only text legend, unavailable/occupied labels, high-contrast worker text, audio cues, and tutorial layout at 1280x720 and 1920x1080.

The Windows player build also passed. The packaged tutorial-only run passed 31/31 checks at 1280x720. The complete 1920x1080 friend flow passed 35/35 checks, generated all seven major-state screenshots, awarded no purchase funds, earned affordability from live desk work, deducted exactly $1,000, and reached the physical expansion. Reports and images are preserved under `outputs/Screenshots`. Historical release evidence from `a638304` remains preserved.
