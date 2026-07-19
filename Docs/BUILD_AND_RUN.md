# Build and Run

## Run the release

Launch `C:\Users\danny\Documents\GitHub\OpenPlan\outputs\OpenPlan-Windows\OpenPlan.exe`, or extract `outputs/OpenPlan-Windows.zip` and run `OpenPlan.exe` from the extracted `OpenPlan-Windows` folder. No Unity, Blender, repository, or absolute source path is required by the player.

## Open and rebuild

Use Unity Hub with Unity 6000.5.1f1 and open `C:\Users\danny\Documents\GitHub\OpenPlan`. The editor menu **OPEN PLAN → Generate Complete Project** regenerates materials, catalogs, URP settings, and scenes. **OPEN PLAN → Build Windows Release** writes the required player.

Equivalent PowerShell command:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.1f1\Editor\Unity.exe' -batchmode -nographics -quit `
  -projectPath 'C:\Users\danny\Documents\GitHub\OpenPlan' `
  -executeMethod OpenPlan.Editor.ReleasePipeline.BuildWindows `
  -logFile 'C:\Users\danny\Documents\GitHub\OpenPlan\Logs\build-windows.log'
```

The pipeline builds a non-development Windows x64 player at `outputs/OpenPlan-Windows/OpenPlan.exe`.

## Tests

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.5.1f1\Editor\Unity.exe' -batchmode -nographics `
  -projectPath 'C:\Users\danny\Documents\GitHub\OpenPlan' -runTests -testPlatform EditMode `
  -testResults 'C:\Users\danny\Documents\GitHub\OpenPlan\Logs\EditMode-results.xml'

& 'C:\Program Files\Unity\Hub\Editor\6000.5.1f1\Editor\Unity.exe' -batchmode -nographics `
  -projectPath 'C:\Users\danny\Documents\GitHub\OpenPlan' -runTests -testPlatform PlayMode `
  -testResults 'C:\Users\danny\Documents\GitHub\OpenPlan\Logs\PlayMode-results.xml'
```

The final run passed 24 EditMode and 13 PlayMode tests.
