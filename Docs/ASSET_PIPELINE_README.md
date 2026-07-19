# Blender Asset Pipeline

All visible office, prop, and worker meshes are generated in Blender 5.2.0 LTS. Unity primitives are not used as final visible art.

Run from the repository root:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python Tools\Blender\generate_open_plan_assets.py -- --project-root 'C:\Users\danny\Documents\GitHub\OpenPlan'
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python Tools\Blender\validate_open_plan_assets.py -- --project-root 'C:\Users\danny\Documents\GitHub\OpenPlan'
```

The generator writes editable `.blend` sources to `Tools/Blender/Source`, interchange files to `Tools/Blender/Exports`, and copies FBX files into `Assets/OpenPlan/Art/Models`. It also refreshes the JSON manifest. Assets use meters, Blender Z-up, applied mesh scale, named child parts, `OP_` material names, bevels, and `-Z` forward / `Y` up FBX export.

Unity's model importer provides an axis-conversion rotation and a 100× FBX root scale. `OfficeAssetCatalog` deliberately preserves both on a visual child under an unscaled gameplay wrapper. Colliders, stations, selection rings, and movement therefore remain meter-scaled.

Validation checks the expected 47-asset set, source/FBX presence, non-empty objects, and required hero assets. See [ASSET_MANIFEST.md](ASSET_MANIFEST.md) and `Tools/Blender/Logs`.
