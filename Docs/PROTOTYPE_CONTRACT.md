# Prototype Contract

## Current product direction

OPEN PLAN is now a worker-placement-driven office simulation. The normal playable path starts in a cramped Level One office with Morgan, Alex, and Sam. The player observes the team, directs workers toward clear activity areas, earns cash without a countdown, and works toward purchasing the neighboring unit.

The released large office from commit `a638304` remains intact as the Established Office stage. It is a future-stage preview, not the normal starting environment.

## Stage contract

- `StarterOffice`: the default authored stage, with three workers, three occupied desks, one unavailable desk, and a visible locked neighboring unit.
- `StarterOfficeExpanded`: the same business after its first physical expansion, with both units and seven available desk locations.
- `EstablishedOffice`: the preserved six-worker, twelve-desk released office and its management systems.

The Main Menu always starts `StarterOffice`. Explicit developer and automation launches can select a stage with `-openplan-stage <StarterOffice|StarterOfficeExpanded|EstablishedOffice>`. Existing capture, video, performance, and package-verification arguments default to `EstablishedOffice` when no stage override is supplied so prior release evidence remains reproducible.

## Placement architecture contract

Every player-directed placement is represented by a `WorkerCommand` containing the worker, destination `PlacementZone`, requested `PlacementActivity`, issue time, and player-placement origin. Supported activities are Work, Rest, Get Water, Buy Snack, Smoke, and Leave Office.

The runtime boundaries and authored placement geometry are established. Click-and-drag interaction and complete activity behavior are delivered by later worker-placement checkpoints.

## Simulation contract

- Starter stages have no countdown and do not finish or fail automatically.
- The first objective is to earn enough cash to purchase the neighboring unit.
- The Established Office retains its released worker simulation, hiring, firing, reassignment, task economy, amenities, camera composition, and optional five-minute legacy workday.
- Core worker, task, economy, UI, audio, and camera systems are shared across stages.

## Current non-goals

- Multiple purchasable properties.
- Multiple cities or districts.
- Multiple floors.
- Managers and specialized roles.
- Rival companies.
- Promotions and relationships.
- Complex finance.
- Furniture-placement mode.
- Save-game persistence.

## Completion evidence

Each checkpoint must compile, run the complete EditMode and PlayMode suites, preserve prior release assets and evidence, document intentional migrations, and end with only intentional source changes.
