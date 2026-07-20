# Simulation Rules

## Stage and time model

`OfficeStageSelection` resolves `StarterOffice`, `StarterOfficeExpanded`, or `EstablishedOffice`. The Main Menu deliberately starts `StarterOffice`. Starter stages are open-ended: there is no countdown, automatic purchase, forced success, or forced failure. The Established Office remains available as an untimed preview while preserving its legacy testable workday systems.

## Worker placement

The public placement activities are Work, Rest, Get Water, Buy Snack, Smoke, and Leave Office. A valid release creates a `WorkerCommand` containing worker, destination, activity, issue time, and player-placement origin. Invalid, occupied, locked, cancelled, or modal-blocked drops never create a command and restore the worker safely.

Manual desk placement grants 30 simulation seconds of Focused Work. The productivity modifier is exactly 1.20 while time remains and 1.00 afterward; replacing a worker at a desk refreshes the timer but never stacks the multiplier.

## Productivity and income

```text
effective = clamp(skill x energy x mood x inverse-stress x workstation x trait x focused-work, 0.10, 2.50)
cash earned = effective productivity x simulation seconds x $60 / 60
```

- Energy maps linearly from 0.55 to 1.10.
- Mood maps linearly from 0.70 to 1.10.
- Inverse stress maps linearly from 1.15 at zero stress to 0.55 at full stress.
- Trait modifiers are contextual and normally range from 0.82 to 1.16.
- Pausing produces zero simulation delta and therefore zero income.

## Needs and restorative tradeoffs

Rest is the efficient early general recovery. Water is quick and modest. Vending costs $15, has a 10% malfunction chance, and is helpful rather than required. Smoking strongly reduces stress but restores no energy, so it is not universally best. Leaving gives the strongest combined recovery but removes a worker for 30 simulation seconds. All need values clamp to 0-1.

## Personality and autonomy

Workers reevaluate decisions on bounded, seeded intervals. Morgan has the highest work preference and lowest distraction chance; Alex is more social and moderately distractible; Sam has the lowest work preference and highest distraction chance. Critical needs can trigger autonomous recovery, and all timed activities return to desk/autonomy. Distractions last at most 18 seconds, so passive observation remains slower but cannot create a permanent stuck state.

## Expansion and hiring

The Starter Office begins with $100. The neighboring unit becomes purchasable at $1,000 and never spends automatically. Confirmation deducts exactly $1,000, opens the connecting wall, removes its obstacle, reveals doorway trim, enables neighbor lighting and navigation, activates three desk locations and a restorative corner, expands camera bounds, and raises worker capacity from three to six. Hiring then becomes available; each new hire arrives unassigned at the entrance and must be placed at an open desk.

## Release scenario contract

The release matrix uses the public tuning tables above for five scenarios and the same 20 fixed seeds. ACTIVE must reach $1,000 in 6-10 minutes. PASSIVE issues zero commands and finishes more slowly. POOR spends and loses work time but does not fail. RECOVERY must improve productivity after intervention. EXPANSION must purchase, hire, place, and continue for at least two simulation minutes. No run may remain stuck for 180 seconds.
