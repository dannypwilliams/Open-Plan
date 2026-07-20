# OPEN PLAN

OPEN PLAN, also known as Silly Office Sim, is a small isometric office-management game about guiding individual workers through a cramped workplace. Pick up Morgan, Alex, and Sam, release them anywhere on valid unlocked ground, or use the proximity influence around a desk or restorative landmark.

Checkpoint `00_Foundation` starts with $0 and has no countdown or forced failure. The original three employees generate income immediately. Desk work earns `$60 per productivity-minute`; manually placing a worker at a desk grants a non-stacking +20% Focused Work bonus for 30 simulation seconds. Hiring is available before the $1,000 neighboring-unit expansion whenever the selected candidate is affordable. Team size is not capped by desks: employees without desks work visibly from their phones at 50% workstation efficiency and can still take autonomous breaks.

## Playable stages

- `StarterOffice`: the normal entry path with three named workers, three desks, and Work, Rest, Water, Vending, Smoking, and Leave Office placement areas.
- `StarterOfficeExpanded`: the same office after the physical expansion, with six desk locations and room for three new hires.
- `EstablishedOffice`: the preserved larger-office simulation, available as an untimed future-stage preview.

## Controls

- Click a worker to select and inspect them.
- Hold and drag a worker to ordinary unlocked ground or near a labeled activity landmark.
- Escape or right-click cancels a carry safely.
- Mouse wheel zooms; middle-mouse drag pans; `F` follows a selected worker.
- `N` toggles names; `H` opens hiring; `Tab` toggles productivity.
- `Space` pauses; `1`, `2`, and `3` select 1x, 2x, and 4x speed.
- `HELP` explains controls, needs, activities, cash, and can replay the tutorial.

## Checkpoint 00 status

- Unity 6000.5.1f1 Windows x64 build.
- 117/117 automated tests passing: 58 EditMode and 59 PlayMode.
- Normalized ten-notch zoom, continuous trackpad-scale input, ordinary-ground placement, deterministic proximity influence, locked/obstacle rejection, `$0` start, pre-expansion hiring, and phone-work pause/accrual are covered.
- The non-development Windows package is built from a clean commit, freshly zipped, extracted separately, and driven through the Checkpoint 00 public-API smoke flow.
- Required 1920×1080 screenshots plus a 1280×720 HUD regression capture are stored with the checkpoint.

The checkpoint is under `outputs/Playtests/EndlessOfficeAlpha/00_Foundation/`. See [Build and Run](Docs/BUILD_AND_RUN.md), [Test Report](Docs/TEST_REPORT.md), and the [Checkpoint 00 Playtest Guide](Docs/Playtests/00_Foundation_PLAYTEST_GUIDE.md).

Historical Established Office release evidence is preserved under `outputs/PreviousRelease/EstablishedOffice-a638304` rather than overwritten.

Live five-need simulation and recovery, assigned qualification pairs, training, workdays, contracts, payroll, reputation, incidents, furniture construction, campaign persistence, and final Alpha balancing remain later checkpoints in the [30-day roadmap](Docs/NEXT_30_DAYS_ROADMAP.md).
