# Build and Run

Open `C:\Users\danny\Documents\GitHub\Silly Office Sim` with Unity 6000.5.1f1. The normal Main Menu starts the Starter Office.

## Checkpoint packaging

Commit a clean source checkpoint, then run:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\Packaging\Build-Checkpoint.ps1
```

The reusable workflow runs the complete EditMode and PlayMode suites, builds Windows x64 with Development mode disabled, copies the checkpoint guide, creates a fresh ZIP, extracts it to a separate temporary directory, launches that exact extracted executable in a normal rendered window, runs the public-API smoke flow, validates all screenshot dimensions, records SHA-256, and publishes atomically to:

`outputs/Playtests/EndlessOfficeAlpha/00_Foundation/`

It refuses to overwrite an existing checkpoint directory and verifies that project generation/building leaves the committed source clean. Temporary staging and verification directories are constrained to the checkpoint output root. The original `outputs/Screenshots/FriendDemo` and all prior release evidence are untouched.

Direct stage arguments are:

```text
-openplan-stage StarterOffice
-openplan-stage StarterOfficeExpanded
-openplan-stage EstablishedOffice
```

Release verification arguments are:

```text
-openplan-input-smoke
-openplan-activity-smoke
-openplan-behavior-soak
-openplan-expansion-capture
-openplan-tutorial-playthrough
-openplan-friend-demo
-openplan-foundation-smoke -openplan-evidence-root <folder>
-openplan-performance
-openplan-verify-package
```

`-openplan-foundation-smoke` starts at the real main menu and uses public gameplay APIs to verify `$0`, zoom, ordinary-floor placement, autonomous resumption, proximity influence, locked-ground restoration, real earnings, pre-expansion hiring, deskless phone work, pause/resume, menu return, and clean quit. It does not inject cash or call a capture-only gameplay setter.

## Tests

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.1f1\Editor\Unity.exe' -batchmode -nographics `
  -projectPath 'C:\Users\danny\Documents\GitHub\Silly Office Sim' -runTests -testPlatform EditMode `
  -testResults 'C:\Users\danny\Documents\GitHub\Silly Office Sim\outputs\TestResults\00_Foundation-EditMode.xml'

& 'C:\Program Files\Unity\Hub\Editor\6000.5.1f1\Editor\Unity.exe' -batchmode -nographics `
  -projectPath 'C:\Users\danny\Documents\GitHub\Silly Office Sim' -runTests -testPlatform PlayMode `
  -testResults 'C:\Users\danny\Documents\GitHub\Silly Office Sim\outputs\TestResults\00_Foundation-PlayMode.xml'
```

Checkpoint 00 passes 58 EditMode and 59 PlayMode tests. The package-specific XML, logs, summary, smoke report, manifest, and screenshots are stored beside the checkpoint player.
