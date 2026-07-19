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

The player presses and holds on a worker, then moves more than six screen pixels or holds for 0.12 seconds to begin carrying. A simple click only selects. Valid release lowers the worker, issues a `WorkerCommand`, and walks the final segment; invalid release restores position and state without consuming gameplay resources. Escape and right-click cancel.

## Needs, productivity, and cash contract

- Player-facing needs are Energy and Mood (higher is better) plus Stress (lower is better), each clamped to 0-1. The former Focus and Morale needs no longer exist.
- Productivity is `skill × energy modifier × mood modifier × inverse stress modifier × workstation modifier × trait modifier × Focused Work modifier`, clamped to 0.10x-2.50x.
- Starter cash begins at $100. Desk work earns `$60/min × effective productivity` continuously in simulation time; pause stops income. Current cash and lifetime earned are tracked separately.
- Manual Work placement refreshes a +20% Focused Work modifier to 30 seconds; it never stacks.

## Activity contract

- Work drains Energy at 0.0018/s before trait persistence, raises Stress at 0.0012/s before workstation noise, and drains Mood at 0.0005/s above 0.70 Stress.
- Rest lasts 20 seconds and grants Energy +0.35, Mood +0.12, Stress -0.25.
- Get Water lasts 6 seconds and grants Energy +0.08, Mood +0.05, Stress -0.05, followed by a 35-second cooldown. A nearby worker permits a short social beat.
- Buy Snack charges $15 exactly once when use begins, lasts 8 seconds, and then enters a 45-second cooldown. Normal: Energy +0.25, Mood +0.15, Stress -0.08. Seeded 10% malfunction: Energy +0.05, Mood -0.05, no Stress change; the charge remains.
- Smoke lasts 12 seconds, grants Mood +0.05 and Stress -0.30, then enters a 45-second cooldown. Its worker-held cigarette and restrained particles are transient and always cleaned up.
- Leave Office walks through the real exit, selects Lunch, Errand, Long break, or Off-site task, hides only outside, recovers Energy +0.45, Mood +0.12, and Stress -0.35 over 30 seconds, then reappears at the entrance and returns to autonomous work.

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
