# OPEN PLAN

OPEN PLAN is an isometric office-management simulation about guiding a tiny team and growing a struggling business. The current pivot starts with Morgan, Alex, and Sam in a cramped office. The player will pick workers up, place them at clear activity areas, earn money without a countdown, and purchase the neighboring unit to physically expand.

Checkpoint 5 makes the starting team individually recognizable. Morgan is Hardworking, Alex is Social, and Sam is Lazy; each now makes visibly different seeded choices, can become briefly distracted, and remains easy to redirect. Persistent camera-facing name tags, temporary text-safe emotes, and a fuller employee inspector make their current activity and destination readable. All placement activities from checkpoint 4 remain functional, and the neighboring-unit purchase sequence remains assigned to checkpoint 6.

The Starter Office begins with $100. Desk work earns `$60/min × effective productivity`; pausing stops simulation income. Manual desk placement grants a non-stacking +20% Focused Work bonus for 30 simulation seconds.

## Stages

- `StarterOffice`: normal entry path; three workers at three occupied desks, one unavailable desk, and six supporting activity areas.
- `StarterOfficeExpanded`: first expansion state; three workers and seven available desk locations across both units.
- `EstablishedOffice`: preserved released large office with six workers, twelve desks, hiring, firing, reassignment, amenities, and its complete legacy simulation.

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

The Blender catalog contains 54 validated assets. Placement evidence and the checkpoint 4 packaged activity-cycle report are preserved under `outputs/Screenshots`.

The previous Windows executable, screenshots, gameplay video, and package evidence remain preserved as the `a638304` Established Office release.
