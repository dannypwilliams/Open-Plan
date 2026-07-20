# Build and Run

Open `C:\Users\danny\Documents\GitHub\Silly Office Sim` with Unity 6000.5.1f1. The normal Main Menu starts the Starter Office.

## Checkpoint 02 package

After committing a clean Prompt 02 source checkpoint, run:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\Packaging\Build-NeedAutonomyCheckpoint.ps1
```

The workflow refuses to overwrite an existing checkpoint. It reruns complete EditMode and PlayMode suites, runs the deterministic 20-seed autonomy matrix, builds Windows x64 with Development mode disabled, creates a fresh ZIP from `Windows/`, extracts that exact archive into `VerifiedExtract/`, launches the extracted executable in a normal rendered window, runs the public-gameplay smoke, validates 12 capture dimensions, records SHA-256, and publishes atomically to:

`outputs/Playtests/EndlessOfficeAlpha/02_NeedAutonomy/`

Expected layout:

```text
02_NeedAutonomy/
  Windows/
  SillyOfficeSim_02_NeedAutonomy_Windows.zip
  VerifiedExtract/
  manifest.md
  playtest-guide.md
  known-issues.md
  autonomy-simulation-report.md
  build.log
  NEED_AUTONOMY_SMOKE.txt
  test-results/
  captures/
```

Generated package evidence is ignored by Git. Source guides and packaging code are tracked. Checkpoint 00, Checkpoint 01, FriendDemo, PreviousRelease, ReleaseEvidence, and screenshot history are never deleted or overwritten.

## Verification arguments

```text
-openplan-stage StarterOffice | StarterOfficeExpanded | EstablishedOffice
-openplan-foundation-smoke -openplan-evidence-root <folder>
-openplan-five-needs-smoke -openplan-evidence-root <folder>
-openplan-need-autonomy-smoke -openplan-evidence-root <folder>
```

`-openplan-need-autonomy-smoke` starts at the rendered Main Menu and uses public placement, activity, simulation-speed, hiring, selection, menu, and quit APIs. Repeated real Water/Work instructions create natural Bathroom pressure, normal work earns candidate cash, a fourth employee is hired without a desk, and the flow observes autonomous recovery and return. It never injects cash or directly changes need values.

## Direct tests and matrix

Do not add `-quit` to `-runTests` on Unity 6000.5.1f1; the test runner exits when complete.

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.1f1\Editor\Unity.exe' -batchmode -nographics `
  -projectPath 'C:\Users\danny\Documents\GitHub\Silly Office Sim' -runTests -testPlatform EditMode `
  -testResults 'C:\Users\danny\Documents\GitHub\Silly Office Sim\outputs\TestResults\02_NeedAutonomy-EditMode.xml'

& 'C:\Program Files\Unity\Hub\Editor\6000.5.1f1\Editor\Unity.exe' -batchmode -nographics `
  -projectPath 'C:\Users\danny\Documents\GitHub\Silly Office Sim' -runTests -testPlatform PlayMode `
  -testResults 'C:\Users\danny\Documents\GitHub\Silly Office Sim\outputs\TestResults\02_NeedAutonomy-PlayMode.xml'

& 'C:\Program Files\Unity\Hub\Editor\6000.5.1f1\Editor\Unity.exe' -batchmode -nographics -quit `
  -projectPath 'C:\Users\danny\Documents\GitHub\Silly Office Sim' `
  -executeMethod OpenPlan.Editor.NeedAutonomyReportGenerator.GenerateFromCommandLine
```

Checkpoint 02 requires at least 138 EditMode and 110 PlayMode tests, zero failures, and a passing deterministic matrix before packaging.

## Manual local run

1. Open the project in Unity 6000.5.1f1.
2. Run `OpenPlan > Generate Release Assets` only when authored release assets genuinely need regeneration.
3. Open `Assets/OpenPlan/Scenes/MainMenu.unity` and press Play.
4. For a player build, launch `outputs/Playtests/EndlessOfficeAlpha/02_NeedAutonomy/VerifiedExtract/OpenPlan.exe`.

The user explicitly waived a human playtest for this checkpoint. The guide remains packaged as a repeatable optional script; no manual acceptance is claimed.
