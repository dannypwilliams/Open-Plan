# OPEN PLAN

OPEN PLAN, also known as Silly Office Sim, is a small isometric office-management game about guiding opinionated workers through a cramped workplace. Pick up Morgan, Alex, and Sam, release them anywhere on valid unlocked ground, or use the proximity influence around a desk or restorative landmark.

Checkpoint `02_NeedAutonomy` starts with $0 and has no countdown or forced failure. Every employee has live Happiness, Hunger, Bathroom, Inspiration, and Energy. Happiness, Inspiration, and Energy are healthier when full; Hunger and Bathroom are filling urgency meters. Stress remains a temporary influence, not a sixth need.

Employees periodically evaluate those needs on a deterministic staggered schedule. They score reachable Rest, Water, Vending, Coffee, Smoking, Restroom, and off-site choices; reserve limited stations; follow obstacle-aware paths; apply each cost and recovery once; and return to assigned desk work or 50%-efficiency phone work. The inspector explains the decision owner, reason, addressed need, destination, and reservation state.

Player placement remains the faster intervention. An accepted activity command receives 22 simulation seconds of authority and ordinary autonomy does not cancel it. Critical needs can override after bounded deferral, including three seconds for Bathroom and five seconds for Hunger, with an explicit explanation. Ordinary-floor placement still preserves the worker's desk and resumes autonomy from the selected point.

## Playable stages

- `StarterOffice`: the normal entry path with three named workers, three desks, and Work, Rest, Water, Vending, Coffee, Smoking, Restroom, and Leave Office placement areas.
- `StarterOfficeExpanded`: the same office after the $1,000 physical expansion, with six desk locations and newly walkable neighboring ground.
- `EstablishedOffice`: the preserved larger-office simulation, available as an untimed future-stage preview.

## Controls

- Click a worker to select and inspect them; hover a need row for meaning and recovery help.
- Hold and drag a worker to ordinary unlocked ground or near a labeled activity landmark.
- Escape or right-click cancels a carry safely.
- Mouse wheel zooms; middle-mouse drag pans; `F` follows a selected worker.
- `N` toggles names; `H` opens hiring; `Tab` toggles productivity.
- `Space` pauses; `1`, `2`, and `3` select 1x, 2x, and 4x speed.
- `HELP` explains needs, autonomy, placement, activities, cash, and tutorial replay.

## Checkpoint 02 status

- Unity 6000.5.1f1 Windows x64 non-development build workflow.
- 248 automated tests: 138 EditMode and 110 PlayMode.
- A deterministic 20-seed matrix covers 3, 10, and 30 workers across 15 scenarios for ten simulated minutes, plus one 100-minute run at each roster size.
- The matrix records bounded critical response, matching reservation creation/release, no invalid values, no orphaned reservations, no capacity violations, no duplicate charges, no lost desks, and no deterministic divergence.
- Prompt 00 camera, placement, influence, `$0`, hiring, expansion, and phone work remain covered; Prompt 01's five-need model and exact-once activity effects remain authoritative.
- The reusable package workflow builds, zips, extracts, launches, smokes, and captures the exact verified player without direct need mutation or artificial cash.

The checkpoint is published under `outputs/Playtests/EndlessOfficeAlpha/02_NeedAutonomy/`. See [Build and Run](Docs/BUILD_AND_RUN.md), [Test Report](Docs/TEST_REPORT.md), and the [Checkpoint 02 Playtest Guide](Docs/Playtests/02_NeedAutonomy_PLAYTEST_GUIDE.md).

Human playtesting was explicitly waived for this run, so the package is automated-acceptance complete but is not described as manually accepted. Qualification pairs, training, workdays, contracts, payroll, reputation, incidents, furniture construction, campaign persistence, and final Alpha balancing remain later checkpoints in the [30-day roadmap](Docs/NEXT_30_DAYS_ROADMAP.md).
