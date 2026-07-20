# Simulation Rules

## Stage and time model

`OfficeStageSelection` resolves `StarterOffice`, `StarterOfficeExpanded`, or `EstablishedOffice`. The Main Menu starts `StarterOffice`. Player stages remain open-ended: there is no countdown, automatic purchase, forced success, or forced failure.

All continuous need updates use scaled `Time.deltaTime` through one `NeedSimulation.Tick` path. Pause and tutorial/help modals that pause the simulation produce zero need change. Two-times and four-times speed advance needs proportionally; equivalent simulated time is deterministic regardless of partitioning. Carrying does not reset or duplicate needs, and selection has no simulation effect.

## Authoritative five-need model

Exactly five player-facing definitions are centrally discoverable through `NeedCatalog`:

| Need | Healthy direction | Default | Passive change / simulation second |
|---|---|---:|---:|
| Happiness | high | 0.78 | -0.00018 |
| Hunger | low; filling urgency meter | 0.18 | +0.00045 |
| Bathroom | low; filling urgency meter | 0.15 | +0.00035 |
| Inspiration | high | 0.72 | -0.00026 |
| Energy | high | 0.86 | -0.00025 |

Employees receive a stable +/-0.02 offset derived from employee identity and campaign seed. Repeating the seed reproduces the same healthy starting values. `WorkerRuntimeState.mood` is a compatibility property that reads and writes authoritative Happiness; there is no independent Mood value. Stress starts near 0.22 and remains a separate temporary influence, not a need definition or sixth inspector bar.

Status thresholds are shared by simulation and UI. High-is-good needs are Caution below 0.55, Urgent below 0.32, and Critical below 0.15. Urgency needs are Caution above 0.44, Urgent above 0.68, and Critical above 0.85. Values and Stress clamp to 0-1, with invalid floating-point inputs restored to their defaults.

Qualification and incident modifier hooks exist but return the neutral multiplier `1.0` in this checkpoint.

## State and activity effects

Work multiplies Energy drain by 4.5, Inspiration drain by 1.85, Happiness drain by 1.55, and Hunger/Bathroom progression by 1.10. Desk and phone work share this path. Restorative states continue normal Hunger and Bathroom progression; they only slow passive decay of high-is-good needs before applying their completion effect.

Completion effects occur once:

- Rest: Energy +0.32, Happiness +0.14, Inspiration +0.12, Stress -0.22.
- Water: Energy +0.06, Happiness +0.04, Inspiration +0.03, Bathroom +0.08, Stress -0.04.
- Vending success: Hunger -0.72, Happiness +0.08, Energy +0.06, Stress -0.05. One use costs $15 once.
- Vending malfunction: Hunger -0.08, Happiness -0.04, Energy +0.01; the one-time $15 charge remains.
- Coffee: Energy +0.34, Inspiration +0.12, Happiness +0.04, Bathroom +0.06, Stress -0.08. Caffeinated Energy recovery is +0.50.
- Smoking: Stress -0.30, Happiness +0.07, Inspiration +0.06; Hunger is unchanged.
- Restroom: Bathroom -0.78, Happiness +0.02, Stress -0.04 after eight seconds.
- Social time per second: Happiness +0.018, Inspiration +0.008, Stress -0.009.
- Thirty seconds Away: Energy +0.38, Happiness +0.15, Inspiration +0.16, Hunger -0.35, Bathroom -0.40, Stress -0.35, applied continuously and never again on completion.

The restroom is an explicit single-capacity `UseRestroom` placement activity at `starter.restroom.main`. It has a proximity radius larger than its footprint. Completion, interruption, firing, restart, scene destruction, and returning to ordinary behavior all release transient occupancy and keep the worker visible.

Prompt 02 will add comprehensive critical-need destination selection, reservation arbitration, retry/fallback, and navigation recovery. Existing personality decisions remain limited and must not be treated as that system.

## Worker placement

The public placement activities are Work, Rest, Get Water, Buy Snack, Smoke, Leave Office, and Use Restroom. Zones expose an influence radius and priority. Overlap resolution is higher priority, shorter distance, then ordinal stable identifier.

Ordinary unlocked ground creates a `GroundPlacementCommand`, preserves all five needs and any desk assignment, pauses briefly, then resumes decisions. It does not spend, reserve capacity, or apply a cooldown. Walls, registered obstacles, voids, locked property, unavailable activities, and occupied stations reject with a specific explanation and restore the complete carry snapshot.

## Productivity and income

```text
effective = clamp(skill x Energy x Happiness x Inspiration x Hunger penalty x Bathroom penalty
                  x inverse Stress x workstation x trait x focused-work, 0.10, 2.50)
cash = effective productivity x simulation seconds x $60 / 60
```

Energy maps from 0.55 to 1.10, Happiness from 0.70 to 1.10, Inspiration from 0.78 to 1.08, and inverse Stress from 1.15 to 0.55. Hunger and Bathroom have no penalty while healthy, then graduate from 1.00 toward 0.55 across caution, urgent, and critical ranges. Skill, contextual trait, workstation, and the non-stacking 1.20 Focused Work modifier remain intact.

Deskless employees use `WorkerState.Unassigned` internally but are presented as `Working from phone`. Their workstation factor is exactly `0.50` once; displayed output, cash accrual, pause behavior, and need simulation use the same effective value.

## Expansion and hiring

Starter Office begins at $0. The neighboring unit costs exactly $1,000 and never purchases automatically. Hiring is available whenever the candidate is affordable and is not capped by desk count. The HUD reports Team and Desks separately. Physical expansion still opens the wall, unlocks ground, and activates three additional desks.
