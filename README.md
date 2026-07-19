# OPEN PLAN

OPEN PLAN is an isometric office-management simulation about guiding a tiny team and growing a struggling business. The current pivot starts with Morgan, Alex, and Sam in a cramped office. The player will pick workers up, place them at clear activity areas, earn money without a countdown, and purchase the neighboring unit to physically expand.

Checkpoint 6 completes the first demo progression. The Starter Office is open-ended: cash reaching $1,000 enables a deliberate purchase instead of ending the day. Confirming it spends exactly $1,000, lights the neighboring unit, opens the connecting wall, reveals doorway trim, enables its utility corner and three desk zones, and widens the live camera bounds without reloading the scene. Hiring then unlocks for three additional workers; new hires arrive unassigned and must be dragged to a desk. The milestone also exposes the preserved Established Office as a clearly marked future-stage preview.

The Starter Office begins with $100. Desk work earns `$60/min × effective productivity`; pausing stops simulation income. Manual desk placement grants a non-stacking +20% Focused Work bonus for 30 simulation seconds.

## Stages

- `StarterOffice`: normal entry path; three workers at three occupied desks, one unavailable desk, and six supporting activity areas.
- `StarterOfficeExpanded`: first expansion state; three workers and six available desk locations across both units, with capacity to hire and place three more.
- `EstablishedOffice`: preserved released large office with six workers, twelve desks, hiring, firing, reassignment, amenities, and its complete legacy simulation; the milestone preview is untimed.

Run a specific development stage with `-openplan-stage <stage name>`. Existing release automation defaults to Established Office so historical capture paths remain usable.

## Current controls

- Mouse wheel: zoom.
- Middle-mouse drag: pan.
- Click: select a worker or choose a desk while reassigning.
- Hold and drag a worker: lift and place them at Work, Rest, Water, Snack, Smoke, or Exit footprints.
- Escape or right-click while carrying: cancel and return the worker safely.
- `F`: follow the selected worker.
- `H`: open hiring.
- `Tab`: productivity overlay.
- `Space`: pause/resume; `1`, `2`, `3`: normal, 2x, and 4x speed.

## Source workflow

- Unity 6000.5.1f1, URP 17.5, Input System 1.19, TextMesh Pro/U GUI 2.5.
- Blender 5.2.0 LTS for the visible environment, prop, and worker meshes.
- Open `C:\Users\danny\Documents\GitHub\OpenPlan` in Unity Hub.
- Rebuild through **OPEN PLAN -> Build Windows Release**.
- See [BUILD_AND_RUN.md](Docs/BUILD_AND_RUN.md) and [TEST_REPORT.md](Docs/TEST_REPORT.md) for commands and verification.

The Blender catalog contains 54 validated assets. Placement, behavior-soak, and physical-expansion evidence are preserved under `outputs/Screenshots`.

The previous Windows executable, screenshots, gameplay video, and package evidence remain preserved as the `a638304` Established Office release.
