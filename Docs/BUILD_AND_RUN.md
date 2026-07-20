# Build and Run

## Open in Unity

Open `C:\Users\danny\Documents\GitHub\OpenPlan` with Unity 6000.5.1f1.

The normal Main Menu path starts the Starter Office. To open a stage directly from a development or automated player launch, add one of:

```text
-openplan-stage StarterOffice
-openplan-stage StarterOfficeExpanded
-openplan-stage EstablishedOffice
```

The existing `-openplan-capture`, `-openplan-video`, `-openplan-performance`, and `-openplan-verify-package` paths select Established Office by default unless paired with an explicit stage argument. `-openplan-expansion-capture` runs the Starter Office before/after purchase evidence pass. `-openplan-tutorial-playthrough` performs the event-driven tutorial and earns the expansion normally; add `-openplan-tutorial-capture-only` for the shorter tutorial-only resolution pass.

## Generate and build

The editor menu **OPEN PLAN -> Generate Complete Project** regenerates materials, catalogs, render settings, and the shared Main Menu and Office bootstrap scenes. **OPEN PLAN -> Build Windows Release** writes `outputs/OpenPlan-Windows/OpenPlan.exe`.

## Tests

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.1f1\Editor\Unity.exe' -batchmode -nographics `
  -projectPath 'C:\Users\danny\Documents\GitHub\OpenPlan' -runTests -testPlatform EditMode `
  -testResults "$env:TEMP\OpenPlan-EditMode.xml"

& 'C:\Program Files\Unity\Hub\Editor\6000.5.1f1\Editor\Unity.exe' -batchmode -nographics `
  -projectPath 'C:\Users\danny\Documents\GitHub\OpenPlan' -runTests -testPlatform PlayMode `
  -testResults "$env:TEMP\OpenPlan-PlayMode.xml"
```

Checkpoint 7 passes 43 EditMode and 55 PlayMode tests (98 total). Blender validation additionally passes all 54 assets. The packaged player supports `-openplan-input-smoke`, `-openplan-activity-smoke`, `-openplan-behavior-soak`, `-openplan-expansion-capture`, and `-openplan-tutorial-playthrough`; all write evidence under `outputs/Screenshots`.
