# Build and Run

Open `C:\Users\danny\Documents\GitHub\Silly Office Sim` with Unity 6000.5.1f1. The normal Main Menu starts the Starter Office.

## Checkpoint 01 package

After committing a clean Prompt 01 source checkpoint, run:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\Packaging\Build-FiveNeedsCheckpoint.ps1
```

The workflow refuses to overwrite an existing checkpoint. It reruns complete EditMode and PlayMode suites, runs the deterministic 3/10/30-worker matrix, builds Windows x64 with Development mode disabled, creates a fresh ZIP from `Windows/`, extracts that exact archive into the preserved `VerifiedExtract/`, launches the extracted executable in a normal rendered window, runs the public gameplay smoke, validates all capture dimensions, records SHA-256, and publishes atomically to:

`outputs/Playtests/EndlessOfficeAlpha/01_FiveNeeds/`

Expected layout:

```text
01_FiveNeeds/
  Windows/
  SillyOfficeSim_01_FiveNeeds_Windows.zip
  VerifiedExtract/
  manifest.md
  playtest-guide.md
  known-issues.md
  build.log
  FIVE_NEEDS_SMOKE.txt
  test-results/
  captures/
```

Generated package evidence is ignored by Git. Source guides and packaging code are tracked. Checkpoint 00, FriendDemo, PreviousRelease, ReleaseEvidence, and screenshot history are never deleted or overwritten.

## Verification arguments

```text
-openplan-stage StarterOffice | StarterOfficeExpanded | EstablishedOffice
-openplan-foundation-smoke -openplan-evidence-root <folder>
-openplan-five-needs-smoke -openplan-evidence-root <folder>
```

`-openplan-five-needs-smoke` starts at the real menu and uses public placement, speed, hiring, and menu APIs. It creates a natural Bathroom warning through repeated Water uses, verifies Restroom influence and cleanup, earns candidate cash through normal work, and captures the real extracted player. It does not inject cash or directly set need values.

## Direct tests

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.1f1\Editor\Unity.exe' -batchmode -nographics `
  -projectPath 'C:\Users\danny\Documents\GitHub\Silly Office Sim' -runTests -testPlatform EditMode `
  -testResults 'C:\Users\danny\Documents\GitHub\Silly Office Sim\outputs\TestResults\01_FiveNeeds-EditMode.xml'

& 'C:\Program Files\Unity\Hub\Editor\6000.5.1f1\Editor\Unity.exe' -batchmode -nographics `
  -projectPath 'C:\Users\danny\Documents\GitHub\Silly Office Sim' -runTests -testPlatform PlayMode `
  -testResults 'C:\Users\danny\Documents\GitHub\Silly Office Sim\outputs\TestResults\01_FiveNeeds-PlayMode.xml'

& 'C:\Program Files\Unity\Hub\Editor\6000.5.1f1\Editor\Unity.exe' -batchmode -nographics `
  -projectPath 'C:\Users\danny\Documents\GitHub\Silly Office Sim' `
  -executeMethod OpenPlan.Editor.FiveNeedsReportGenerator.GenerateFromCommandLine
```

Checkpoint 01 passes 98 EditMode and 70 PlayMode tests before packaging; the package workflow must reproduce or exceed those totals with zero failures.
