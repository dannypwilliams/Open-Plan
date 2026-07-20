# Friend Demo Balance Scenarios

Generated UTC: 2026-07-20T00:39:47.7103114Z

100 deterministic runs: five play styles across the same 20 fixed seeds. Values come from the public release tuning tables used by live play.

| Scenario | Runs | Time to $1,000 min / mean / max | Earnings mean | Spend mean | Productivity mean | Work time mean | Distracted mean | Restorative mean | Commands mean | Focus uptime mean | Expanded / hired | Stuck |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| ACTIVE | 20 | 7.55 / 7.67 / 7.78 min | $901.01 | $0.00 | 1.96 | 19.84 worker-min | 2.15 worker-min | 1.00 worker-min | 41.7 | 56.9% | 0 / 0 | 0 |
| PASSIVE | 20 | 10.08 / 10.95 / 12.00 min | $900.58 | $0.00 | 1.37 | 24.07 worker-min | 8.02 worker-min | 0.75 worker-min | 0.0 | 0.0% | 0 / 0 | 0 |
| POOR | 20 | 8.18 / 8.76 / 9.42 min | $980.52 | $79.50 | 1.87 | 14.38 worker-min | 5.19 worker-min | 6.69 worker-min | 21.5 | 0.0% | 0 / 0 | 0 |
| RECOVERY | 20 | 11.20 / 11.41 / 11.68 min | $900.86 | $0.00 | 1.32 | 27.25 worker-min | 2.98 worker-min | 4.00 worker-min | 61.8 | 50.9% | 0 / 0 | 0 |
| EXPANSION | 20 | 7.55 / 7.67 / 7.78 min | $1591.30 | $0.00 | 1.96 | 35.92 worker-min | 4.00 worker-min | 2.66 worker-min | 76.9 | 54.3% | 20 / 20 | 0 |

Recovery productivity: 0.00 before intervention -> 1.41 after intervention.

Gate: PASS only when all ACTIVE seeds reach $1,000 in 6-10 minutes, PASSIVE is slower and finishes, POOR finishes despite avoidable losses, RECOVERY improves after redirection, EXPANSION hires and continues for two simulation minutes, and no run is permanently stuck.
