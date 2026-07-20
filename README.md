# OPEN PLAN

OPEN PLAN, also known as Silly Office Sim, is a small isometric office-management game about guiding individual workers through a cramped workplace. Pick up Morgan, Alex, and Sam, place them at desks or restorative activity areas, respond to their different personalities, and earn enough cash to buy the neighboring unit.

The friend-demo build contains the complete worker-placement pivot. It starts with $100 and has no countdown or forced failure. Desk work earns `$60 per productivity-minute`; manually placing a worker at a desk grants a non-stacking +20% Focused Work bonus for 30 simulation seconds. Reaching $1,000 makes the neighboring unit purchasable. The confirmed purchase opens the connecting wall in-world, activates three new desk locations, unlocks hiring, and exposes the preserved Established Office preview.

## Playable stages

- `StarterOffice`: the normal entry path with three named workers, three desks, and Work, Rest, Water, Vending, Smoking, and Leave Office placement areas.
- `StarterOfficeExpanded`: the same office after the physical expansion, with six desk locations and room for three new hires.
- `EstablishedOffice`: the preserved larger-office simulation, available as an untimed future-stage preview.

## Controls

- Click a worker to select and inspect them.
- Hold and drag a worker to a labeled activity footprint.
- Escape or right-click cancels a carry safely.
- Mouse wheel zooms; middle-mouse drag pans; `F` follows a selected worker.
- `N` toggles names; `H` opens hiring; `Tab` toggles productivity.
- `Space` pauses; `1`, `2`, and `3` select 1x, 2x, and 4x speed.
- `HELP` explains controls, needs, activities, cash, and can replay the tutorial.

## Release status

- Unity 6000.5.1f1 Windows x64 build.
- 104/104 automated tests passing: 49 EditMode and 55 PlayMode.
- 100 deterministic balance scenarios across 20 fixed seeds passing.
- Active-manager expansion average: 7.67 minutes at 1x; passive average: 10.95 minutes.
- Twenty-minute accelerated standalone soak passing with no stuck worker, missing worker, stale carry, orphaned smoke, or capacity violation.
- Exact extracted package passed the final 86-check menu-to-expansion-to-hire-to-preview flow with zero failures.
- 1920x1080 performance: 119.88 fps average, 118.52 fps 1% low, zero measured peak per-frame GC allocation.

The final executable is under `outputs/OpenPlan-Windows`; the fresh friend-demo ZIP is `outputs/OpenPlan-Friend-Demo-Windows.zip`. See [Build and Run](Docs/BUILD_AND_RUN.md), [Test Report](Docs/TEST_REPORT.md), and the [Friend Playtest Guide](Docs/FRIEND_PLAYTEST_GUIDE.md).

Historical Established Office release evidence is preserved under `outputs/PreviousRelease/EstablishedOffice-a638304` rather than overwritten.
