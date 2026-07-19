# Performance Report

The final non-development Windows x64 player was measured at 1920×1080 for 20 seconds after a five-second warm-up on an AMD Radeon RX 7600. Six workers, all environment meshes, UI, lights, shadows, needs, and tasks were active.

| Metric | Result |
|---|---:|
| Frames sampled | 2,398 |
| Average | 119.88 fps |
| 1% low | 118.54 fps |
| Worst frame | 8.49 ms |
| Peak `GC Allocated In Frame` | 0 bytes |

The 60 fps target passed with substantial headroom. Evidence is `outputs/Performance/performance.json` and `Logs/performance-player.log`.

Runtime design keeps the worker count bounded at 12, uses shared material assets and property blocks, throttles worker decisions to 5.5–8.5-second intervals, updates the productivity overlay at 0.18-second intervals, refreshes UI readouts at 0.16-second intervals, and never rebuilds navigation data. Audio sources, state icons, lights, and task queue size are bounded.
