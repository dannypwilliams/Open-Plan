# Test Report - Endless Office Alpha Checkpoint 01

Checkpoint `01_FiveNeeds` on Unity 6000.5.1f1:

| Suite | Passed | Failed |
|---|---:|---:|
| EditMode | 98 | 0 |
| PlayMode | 70 | 0 |
| Total | 168 | 0 |

The package workflow reruns both complete suites from the final committed source and records exact durations in `test-results/` and `manifest.md` beside the build.

EditMode coverage includes exactly five unique definitions; directions, defaults, thresholds, tooltips, access/change APIs, bounds and invalid-number recovery; Mood/Happiness aliasing; deterministic offsets; passive/work/phone/rest/away effects; zero delta and speed partitioning; all centralized activity effects; monotonic five-need productivity; graduated urgency penalties; the exact-once phone factor; neutral future hooks; large deltas; a 100-minute 30-worker soak; and deterministic repeat equivalence. All 58 Checkpoint 00 EditMode contracts remain covered.

PlayMode coverage includes live changes to every need, pause/modal freezing, equivalent simulated partitions, selection and carry preservation, ordinary ground, Rest, Water, Vending, Smoking, Away, explicit Restroom influence/use/recovery/capacity/restart cleanup, phone-worker need changes and income, five inspector rows, urgency wording, no Stress row, 1280x720 warning readability, tutorial truthfulness, healthy non-synchronous starting offsets, hiring, expansion, proximity, invalid restoration, menu return, and preserved Established Office behavior.

## Deterministic acceptance matrix

`01_FiveNeeds_Deterministic_Report.md` covers 3, 10, and 30 workers, 20 seeds, 100 simulated minutes per row, and Active Work, Phone Work, Resting, Mixed Activity, Pause/Resume, and Speed Changes.

- Invalid, NaN, infinite, or out-of-range values: 0.
- Need changes while paused: 0.
- Unexpected identical workers: 0; fully saturated boundary states are explicitly classified as expected, not hidden.
- Deterministic repeat divergence: 0.
- Mixed activity: no caution/urgent state and no simultaneous critical need across the 100-minute run.
- Uninterrupted Active/Phone work first reached caution around 4.33 minutes and urgent around 7.75 minutes; those intentionally pathological 100-minute contexts eventually reached five simultaneous critical needs.
- Every 30-worker row completed in under 1.2 seconds on the verification machine; the coarse managed-memory delta showed no runaway growth. This is not a replacement for profiler-grade allocation capture.

## Exact package gate

- Windows x64, non-development, Mono player built from clean committed source.
- Fresh `SillyOfficeSim_01_FiveNeeds_Windows.zip` generated from `Windows/`.
- That exact ZIP extracted and preserved under `VerifiedExtract/`.
- Exact extracted executable launched visibly and followed public gameplay paths from main menu through `$0` start, live needs, natural Water-driven warning, restroom influence/use/recovery, pause, real earnings, pre-expansion deskless hire, phone work, 1280 check, menu return, and clean quit.
- No artificial cash, private need mutation, or editor-only screenshot mockup is used.
- Nine genuine gameplay screenshots are dimension-checked: eight required 1920x1080 views plus a 1280x720 inspector regression capture.

Historical friend-demo and Checkpoint 00 evidence remains preserved and is not presented as Prompt 01 evidence.
