# Build and Run

Open `C:\Users\danny\Documents\GitHub\OpenPlan` with Unity 6000.5.1f1. The normal Main Menu starts the Starter Office.

## Windows release

Use **OPEN PLAN -> Build Windows Release**. It writes `outputs/OpenPlan-Windows/OpenPlan.exe` and copies `FRIEND_PLAYTEST_GUIDE.txt` beside the executable. The packaged friend build is `outputs/OpenPlan-Friend-Demo-Windows.zip`.

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
-openplan-performance
-openplan-verify-package
```

`-openplan-friend-demo` uses public gameplay APIs and live earnings to drive the full menu, placement, activities, natural distraction, $1,000 purchase, wall opening, hire placement, Established preview, menu return, and clean quit flow. It does not award artificial cash or call a capture-only activity setter.

## Tests

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.1f1\Editor\Unity.exe' -batchmode -nographics `
  -projectPath 'C:\Users\danny\Documents\GitHub\OpenPlan' -runTests -testPlatform EditMode `
  -testResults 'C:\Users\danny\Documents\GitHub\OpenPlan\outputs\TestResults\Prompt8-EditMode.xml'

& 'C:\Program Files\Unity\Hub\Editor\6000.5.1f1\Editor\Unity.exe' -batchmode -nographics `
  -projectPath 'C:\Users\danny\Documents\GitHub\OpenPlan' -runTests -testPlatform PlayMode `
  -testResults 'C:\Users\danny\Documents\GitHub\OpenPlan\outputs\TestResults\Prompt8-PlayMode.xml'
```

The release gate passes 49 EditMode and 55 PlayMode tests. The packaged checks and generated evidence are under `outputs/ReleaseEvidence` and `outputs/Screenshots`.
