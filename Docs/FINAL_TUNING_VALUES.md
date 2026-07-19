# Final Tuning Values

## Economy and work

- Workday: 300 seconds; target: $1,500; starting cash: $4,000.
- Task work requirement: 74–108; replenished follow-ups: 82–118.
- Task revenue: $180–$324; follow-ups: $195–$309.
- Work contribution: `effectiveProductivity × deltaTime × 0.38`.
- Hiring fees: Riley $380, Cameron $520, Avery $690. Firing severance: $110.

## Productivity

The multiplicative formula is `skill × focus × energy × morale × workstation × nearby × trait`, clamped to 0.10–2.50. Focus maps 0.45–1.15, energy 0.55–1.10, and morale 0.70–1.10. Work energy decay is 0.0018/s before trait persistence. Coffee restores 0.42 energy (0.62 for Caffeinated) plus 0.18 focus. Water restores 0.16 morale plus 0.07 focus. Socializing raises morale 0.018/s and lowers focus 0.008/s.

Social need grows 0.006/s, or 0.010/s for Social workers. Social thresholds are 0.78 and 0.52 respectively; the cooldown is 36 seconds. Coffee cooldown is 52 seconds, or 34 for Caffeinated; water cooldown is 48 seconds. Decisions are reevaluated every 5.5–8.5 seconds.

## Camera and presentation

- Orthographic close size: 4.8; overview size: 18.5.
- Zoom sensitivity: 0.012; pan sensitivity: 0.018; smoothing: 0.16 seconds.
- Camera pitch/yaw: 58° / 45°; pan bounds: X ±11, Z ±8.
- Worker movement: 2.25 m/s; collider: 1.8 m tall, 0.42 m radius.
- Simulation speeds: paused, 1×, 2×, and 4×.
