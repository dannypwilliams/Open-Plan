# OPEN PLAN

OPEN PLAN is a five-minute isometric office simulation about watching a small team, understanding why its productivity changes, and making a few consequential management decisions. Six workers operate autonomously; the player can hire, fire, reassign desks, inspect influences, follow individuals, and review the company at day end.

The ready-to-run Windows executable is `C:\Users\danny\Documents\GitHub\OpenPlan\outputs\OpenPlan-Windows\OpenPlan.exe`. The verified portable package is `outputs/OpenPlan-Windows.zip`.

## Controls

- Mouse wheel: smooth zoom from close worker view to full-office view.
- Middle-mouse drag: pan the office.
- Click: select a worker or, while reassigning, choose a desk.
- `F`: follow the selected worker.
- `H`: open hiring.
- `Tab`: productivity overlay.
- `Space`: pause/resume; `1`, `2`, `3`: normal, 2×, and 4× speed.
- The HUD buttons expose the same essential actions.

## The workday

Each day lasts 300 simulation seconds. Workers contribute to a shared ordered task queue while energy, focus, morale, workstation quality, trait fit, and nearby coworkers alter effective productivity. Completed tasks pay revenue toward a $1,500 target. The report separates revenue, payroll, hiring costs, firing costs, task count, productivity, social time, and staff changes.

Hiring presents three replacement resumes with salary, fee, trait, strength, and weakness. Firing requires confirmation, charges $110 severance, removes payroll, and sends the worker to the elevator carrying a generated box. Reassigning a selected worker to an available desk changes their noise/light modifier and can materially change output.

Traits are Focused, Social, Ambitious, Lazy, Anxious, and Caffeinated. The inspector explains the current positive and negative influence so results are attributable rather than opaque.

## Tool versions and source workflow

- Unity `6000.5.1f1`, URP 17.5, Input System 1.19, TextMesh Pro/U GUI 2.5.
- Blender `5.2.0 LTS` for all visible environment, prop, and worker meshes.
- Open the folder in Unity Hub, or launch Unity with `-projectPath C:\Users\danny\Documents\GitHub\OpenPlan`.
- Rebuild with **OPEN PLAN → Build Windows Release**, or use the command in [BUILD_AND_RUN.md](Docs/BUILD_AND_RUN.md).
- Regenerate the Blender library with `Tools/Blender/generate_open_plan_assets.py`; exact commands and conventions are in [ASSET_PIPELINE_README.md](Docs/ASSET_PIPELINE_README.md).
- Run the command-line test suites using the commands in [TEST_REPORT.md](Docs/TEST_REPORT.md).

Screenshots are in `outputs/Screenshots`; the 108-second gameplay video is `outputs/Media/OpenPlan_Gameplay.mp4`; the contact sheet is `outputs/Media/OpenPlan_ContactSheet.png`.

## Known limitations

Navigation is lightweight deterministic steering rather than NavMesh pathfinding, worker animation is procedural rather than authored skeletal animation, settings are limited to in-game camera/speed controls, and audio is intentionally small. See [KNOWN_ISSUES.md](Docs/KNOWN_ISSUES.md) for the release assessment.
