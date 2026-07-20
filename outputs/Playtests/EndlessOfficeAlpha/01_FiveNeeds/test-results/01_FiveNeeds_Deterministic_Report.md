# Prompt 01 Deterministic Five-Need Simulation Report

Matrix: 3, 10, and 30 workers; 20 seeds; 100 simulated minutes per row. Values are sampled every 5 simulated seconds.
Deterministic repeat check: PASS (same seed, population, context, and duration). No global or frame-time random input is used.

| Workers | Context | Need min..max (Happiness / Hunger / Bathroom / Inspiration / Energy) | First caution | First urgent | Max critical together | Invalid | Unexpected identical | Avg productivity | Pause changes | Runtime | Managed delta |
|---:|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 3 | ActiveWork | 0.000..0.799 / 0.160..1.000 / 0.130..1.000 / 0.000..0.740 / 0.000..0.879 | 4.42 min | 7.83 min | 5 | 0 | 0 | 0.167x | 0 | 54 ms | 4140.0 KiB |
| 3 | PhoneWork | 0.000..0.799 / 0.160..1.000 / 0.130..1.000 / 0.000..0.740 / 0.000..0.879 | 4.42 min | 7.83 min | 5 | 0 | 0 | 0.123x | 0 | 61 ms | 3932.0 KiB |
| 3 | Resting | 0.759..1.000 / 0.160..1.000 / 0.130..1.000 / 0.699..1.000 / 0.842..1.000 | 8.92 min | 17.83 min | 2 | 0 | 0 | 0.000x | 0 | 42 ms | 11244.0 KiB |
| 3 | MixedActivity | 0.728..1.000 / 0.000..0.312 / 0.000..0.359 / 0.648..1.000 / 0.708..1.000 | none | none | 0 | 0 | 0 | 1.030x | 0 | 43 ms | 11152.0 KiB |
| 3 | PauseResume | 0.000..0.799 / 0.160..1.000 / 0.130..1.000 / 0.000..0.740 / 0.000..0.879 | 4.42 min | 7.83 min | 5 | 0 | 0 | 0.166x | 0 | 77 ms | 22828.0 KiB |
| 3 | SpeedChanges | 0.000..0.799 / 0.160..1.000 / 0.130..1.000 / 0.000..0.740 / 0.000..0.879 | 4.67 min | 7.83 min | 5 | 0 | 0 | 0.176x | 0 | 21 ms | 8236.0 KiB |
| 10 | ActiveWork | 0.000..0.800 / 0.160..1.000 / 0.130..1.000 / 0.000..0.740 / 0.000..0.880 | 4.33 min | 7.75 min | 5 | 0 | 0 | 0.167x | 0 | 187 ms | 22896.0 KiB |
| 10 | PhoneWork | 0.000..0.800 / 0.160..1.000 / 0.130..1.000 / 0.000..0.740 / 0.000..0.880 | 4.33 min | 7.75 min | 5 | 0 | 0 | 0.123x | 0 | 192 ms | 13436.0 KiB |
| 10 | Resting | 0.759..1.000 / 0.160..1.000 / 0.130..1.000 / 0.698..1.000 / 0.838..1.000 | 8.92 min | 17.83 min | 2 | 0 | 0 | 0.000x | 0 | 138 ms | 16052.0 KiB |
| 10 | MixedActivity | 0.728..1.000 / 0.000..0.312 / 0.000..0.359 / 0.643..1.000 / 0.706..1.000 | none | none | 0 | 0 | 0 | 1.038x | 0 | 129 ms | 24.0 KiB |
| 10 | PauseResume | 0.000..0.800 / 0.160..1.000 / 0.130..1.000 / 0.000..0.740 / 0.000..0.880 | 4.33 min | 7.75 min | 5 | 0 | 0 | 0.167x | 0 | 283 ms | 28.0 KiB |
| 10 | SpeedChanges | 0.000..0.800 / 0.160..1.000 / 0.130..1.000 / 0.000..0.740 / 0.000..0.880 | 4.33 min | 7.75 min | 5 | 0 | 0 | 0.176x | 0 | 79 ms | 20.0 KiB |
| 30 | ActiveWork | 0.000..0.800 / 0.160..1.000 / 0.130..1.000 / 0.000..0.740 / 0.000..0.880 | 4.33 min | 7.75 min | 5 | 0 | 0 | 0.167x | 0 | 516 ms | 76.0 KiB |
| 30 | PhoneWork | 0.000..0.800 / 0.160..1.000 / 0.130..1.000 / 0.000..0.740 / 0.000..0.880 | 4.33 min | 7.75 min | 5 | 0 | 0 | 0.123x | 0 | 486 ms | 68.0 KiB |
| 30 | Resting | 0.759..1.000 / 0.160..1.000 / 0.130..1.000 / 0.698..1.000 / 0.838..1.000 | 8.92 min | 17.83 min | 2 | 0 | 0 | 0.000x | 0 | 368 ms | 72.0 KiB |
| 30 | MixedActivity | 0.728..1.000 / 0.000..0.313 / 0.000..0.359 / 0.643..1.000 / 0.705..1.000 | none | none | 0 | 0 | 0 | 1.041x | 0 | 372 ms | 72.0 KiB |
| 30 | PauseResume | 0.000..0.800 / 0.160..1.000 / 0.130..1.000 / 0.000..0.740 / 0.000..0.880 | 4.33 min | 7.75 min | 5 | 0 | 0 | 0.167x | 0 | 735 ms | 80.0 KiB |
| 30 | SpeedChanges | 0.000..0.800 / 0.160..1.000 / 0.130..1.000 / 0.000..0.740 / 0.000..0.880 | 4.33 min | 7.75 min | 5 | 0 | 0 | 0.176x | 0 | 211 ms | 64.0 KiB |

Acceptance: invalid/out-of-range values must be zero; pause changes must be zero; identical-worker counts exclude fully saturated boundary states; active and phone rows intentionally model uninterrupted 100-minute work and therefore eventually reach critical values. Mixed activity demonstrates recoverability without implementing Prompt 02 autonomy.

Performance note: managed-memory delta is a coarse before/after observation, not an allocation profiler. The 30-worker rows are required to finish without runaway runtime or memory growth.
