# OPEN PLAN

OPEN PLAN, also known as Silly Office Sim, is a small isometric office-management game about guiding individual workers through a cramped workplace. Pick up Morgan, Alex, and Sam, release them anywhere on valid unlocked ground, or use the proximity influence around a desk or restorative landmark.

Checkpoint `01_FiveNeeds` starts with $0 and has no countdown or forced failure. Every employee has live Happiness, Hunger, Bathroom, Inspiration, and Energy. Happiness, Inspiration, and Energy are healthier when full; Hunger and Bathroom are filling urgency meters. Stress remains a temporary influence, not a sixth need. Select an employee to inspect status words and hover a need for recovery help.

Rest, Water, Vending, Coffee, Smoking, and Away now have centralized five-need tradeoffs. A compact visible Restroom entrance is a single-capacity proximity activity. Desk work earns `$60 per productivity-minute`; hiring remains available before the $1,000 expansion, and deskless employees work visibly from phones at one exact 50% workstation factor while their five needs continue to change.

## Playable stages

- `StarterOffice`: the normal entry path with three named workers, three desks, and Work, Rest, Water, Vending, Smoking, Restroom, and Leave Office placement areas.
- `StarterOfficeExpanded`: the same office after physical expansion, with six desk locations and room for new hires.
- `EstablishedOffice`: the preserved larger-office simulation, available as an untimed future-stage preview.

## Controls

- Click a worker to select and inspect them; hover a need row for meaning and recovery help.
- Hold and drag a worker to ordinary unlocked ground or near a labeled activity landmark.
- Escape or right-click cancels a carry safely.
- Mouse wheel zooms; middle-mouse drag pans; `F` follows a selected worker.
- `N` toggles names; `H` opens hiring; `Tab` toggles productivity.
- `Space` pauses; `1`, `2`, and `3` select 1x, 2x, and 4x speed.
- `HELP` explains the five needs, placement, activities, cash, and tutorial replay.

## Checkpoint 01 status

- Unity 6000.5.1f1 Windows x64 build.
- 168/168 automated tests passing: 98 EditMode and 70 PlayMode.
- A deterministic 3/10/30-worker, 20-seed, six-context, 100-minute matrix records zero invalid values, zero paused changes, and no unexplained divergence.
- Prompt 00 zoom, ordinary-ground placement, deterministic influence, invalid restoration, `$0`, pre-expansion hiring, physical expansion, and phone work remain covered.
- The non-development Windows package is built from a clean commit, freshly zipped, preserved under `VerifiedExtract`, and driven through the Prompt 01 public gameplay smoke flow.
- Eight required 1920x1080 screenshots plus a 1280x720 inspector regression capture are stored with the checkpoint.

The checkpoint is under `outputs/Playtests/EndlessOfficeAlpha/01_FiveNeeds/`. See [Build and Run](Docs/BUILD_AND_RUN.md), [Test Report](Docs/TEST_REPORT.md), and the [Checkpoint 01 Playtest Guide](Docs/Playtests/01_FiveNeeds_PLAYTEST_GUIDE.md).

Historical Established Office and Checkpoint 00 release evidence remains preserved rather than overwritten.

Comprehensive critical-need autonomy and routing are deliberately reserved for Prompt 02. Assigned qualification pairs, training, workdays, contracts, payroll, reputation, incidents, furniture construction, campaign persistence, and final Alpha balancing remain later checkpoints in the [30-day roadmap](Docs/NEXT_30_DAYS_ROADMAP.md).
