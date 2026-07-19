# Prompt 2 - Build the Cramped Starter Office and Neighboring Unit

```text
Continue on branch `codex/small-office-placement-pivot` in:

C:\Users\danny\Documents\GitHub\OpenPlan

Implement this prompt fully. Confirm the previous prompt's commit exists and the worktree is clean. Inspect the procedural environment builder, asset catalog, Blender generation pipeline, material setup, camera bounds, FBX import conventions, and current tests before changing them.

Goal:

Create a visually appealing but clearly struggling Starter Office with every placement area required by `Game Demo Design.pdf`.

Starter office composition:

- Approximate usable interior: 14 m x 11 m.
- Three occupied desks.
- One additional desk location that begins unavailable.
- Three starting workers.
- Cramped break nook.
- Water cooler.
- Cheap vending machine.
- Entrance/exit door.
- Distinct exterior smoking area.
- Cardboard boxes and paper clutter.
- Plain walls.
- Cheaper, warmer, less polished lighting than the established office.
- Mismatched chairs.
- Older-looking computers.
- Limited decorations.
- Clear walking paths despite the cramped presentation.

The office should feel intentionally modest, not unfinished.

Starting workers:

- Morgan: hardworking/high-potential but stress-sensitive.
- Alex: social and easily distracted.
- Sam: inexpensive, lazy, and prone to long breaks.

Preserve their existing visual colors where practical.

Neighboring property:

- Build one visible adjacent unit sharing a removable wall with the Starter Office.
- It begins dim, closed, and unavailable.
- Include visible signage indicating it can eventually be purchased.
- The player should understand spatially that the office can expand into it.
- Include three future desk positions inside the neighboring unit.
- Include a cheap secondary break area or utility corner.
- Add surrounding low-detail shop or office facades to imply a business district.
- Other buildings are scenery only and should read as future opportunities without purchase interactions.

Required activity-zone locations:

- Three desk Work zones.
- Break-room Rest zone.
- Water-cooler Get Water zone.
- Vending-machine Buy Snack zone.
- Exit Leave Office zone.
- Exterior Smoking zone.

Each zone needs:

- A walk/use point.
- A placement footprint or collider.
- An activity label.
- A stable identifier.
- Enabled/disabled state.
- Visual highlight hooks.
- Occupancy or capacity rules.
- A method to validate whether a worker may use it.

Art pipeline:

Reuse existing assets where possible. Add only missing assets needed to communicate the pivot, such as:

- Cheap CRT-style monitor.
- Cheap or damaged desk variant.
- Ashtray/smoking marker.
- Small cigarette prop.
- Modest neighboring-business sign.
- Removable connecting-wall/doorway trim.

If new assets are required:

- Generate them through the existing Blender pipeline.
- Preserve editable `.blend` sources.
- Export FBX files.
- Register them in the asset catalog.
- Update both asset manifests.
- Preserve the unscaled gameplay-wrapper/imported-visual-child contract.
- Validate scale, pivots, materials, and triangle counts.

Camera:

- Recalculate pan bounds and overview framing for the starter office and neighboring unit.
- The overview must show the complete starter office, locked neighbor, entrance, and smoking area.
- Close zoom must clearly show an individual worker.
- Do not let the camera expose unfinished voids.

Testing:

- Every required zone exists exactly once.
- Three desks begin occupied.
- Locked neighbor zones reject workers.
- No station overlaps a wall or blocks a primary route.
- Camera overview contains all required gameplay spaces.
- Established Office still builds successfully.
- Asset-manifest validation passes.

Run all tests and capture overview and close-up screenshots for visual inspection. Fix framing or layout defects before committing.

Commit as:

art: build cramped starter office and neighboring unit

Report the commit, tests, new assets, zone inventory, and screenshot paths.
```
