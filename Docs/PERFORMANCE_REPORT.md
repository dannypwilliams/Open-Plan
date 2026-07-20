# Performance Report

The final non-development Windows x64 player was measured from the exact extracted friend-demo package at 1920x1080. The Established Office ran for a five-second warm-up followed by a 20-second profiler sample on an AMD Radeon RX 7600.

| Metric | Result |
|---|---:|
| Frames sampled | 2,398 |
| Average | 119.88 fps |
| 1% low | 118.52 fps |
| Worst frame | 8.54 ms |
| Peak `GC Allocated In Frame` | 0 bytes |

The 60 fps target passed with substantial headroom. Evidence is `outputs/Performance/performance.json` and `outputs/ReleaseEvidence/Performance_Player.log`.

Runtime work stays bounded: six workers in the friend-demo office, twelve maximum in the Established Office, seeded decisions on multi-second intervals, throttled HUD refreshes, shared materials, bounded particles and audio, and no runtime navigation rebuild.
