# OPEN PLAN Blender Pipeline

The project consumes FBX only. Blender source files remain under `Source/`; exported copies live under `Exports/` and are copied to `Assets/OpenPlan/Art/Models/` by the generator.

Generate from PowerShell:

```powershell
& "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --factory-startup --python "C:\Users\danny\Documents\GitHub\OpenPlan\Tools\Blender\generate_open_plan_assets.py" -- --project-root "C:\Users\danny\Documents\GitHub\OpenPlan"
```

Validate:

```powershell
& "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --factory-startup --python "C:\Users\danny\Documents\GitHub\OpenPlan\Tools\Blender\validate_open_plan_assets.py" -- --project-root "C:\Users\danny\Documents\GitHub\OpenPlan"
```

One Blender unit is one meter. Export uses Unity-compatible `-Z forward / Y up`, stable `OP_` names, applied primitive dimensions, low segment counts, and small bevel modifiers. The Unity release pipeline replaces imported materials with its shared URP palette.

