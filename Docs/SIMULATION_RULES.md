# Simulation Rules

## Deterministic stage selection

`OfficeStageSelection` resolves one of three stages: `StarterOffice`, `StarterOfficeExpanded`, or `EstablishedOffice`. A pending menu choice is consumed once by the Office scene. Otherwise, an explicit `-openplan-stage` argument is used. Without either, the deterministic default is `StarterOffice`.

Legacy automation arguments default to `EstablishedOffice` unless an explicit stage overrides them.

## Starter Office

- Starting roster: Morgan, Alex, and Sam.
- Starting capacity: three desks; the expanded variant exposes six.
- Time: open-ended. Elapsed time may be observed, but there is no countdown or automatic finish/failure.
- Progress: revenue toward the financial objective, rather than elapsed time.
- First milestone: earn enough cash to purchase the neighboring unit. The purchase interaction arrives in the physical-expansion checkpoint.

## Placement model

Placement activities are Work, Rest, Get Water, Buy Snack, Smoke, and Leave Office. Each destination is a `PlacementZone`. A `WorkerCommand` records the worker, destination, requested activity, issue time, and whether the command came from player placement.

This checkpoint defines and builds the zones; later checkpoints connect click-and-drag input and activity-specific state transitions.

## Established Office productivity

`effective = clamp(skill x focus x energy x morale x workstation x nearby x trait, 0.1, 2.5)`

- Focus modifier: `lerp(0.45, 1.15, focus)`
- Energy modifier: `lerp(0.55, 1.10, energy)`
- Morale modifier: `lerp(0.70, 1.10, morale)`
- Workstation: 0.88-1.12 from noise, light, and location
- Nearby influence: 0.82-1.18, aggregated and clamped
- Trait: contextual, normally 0.90-1.16

The inspector continues to expose a positive and negative plain-language influence.

## Established Office behavior and economy

Worker thinking remains seeded and occurs on bounded intervals. Critical needs override optional choices. Coffee, water, socializing, breaks, desk work, hiring, firing, reassignment, tasks, payroll, and the end-of-day report retain their released behavior. The legacy Established Office may still run its five-minute workday for preview and release-evidence compatibility.
