# Test Report

Worker-placement pivot checkpoint 2 on Unity 6000.5.1f1:

| Suite | Passed | Failed | Duration |
|---|---:|---:|---:|
| EditMode | 28 | 0 | 0.060 s |
| PlayMode | 18 | 0 | 26.611 s |
| Total | 46 | 0 | 26.671 s |

The pre-edit baseline passed 24 EditMode and 13 PlayMode tests (37 total).

EditMode coverage now also validates the complete 54-asset Blender/FBX/Unity manifest, including the seven new checkpoint 2 assets.

PlayMode coverage verifies the exact active-zone inventory, three occupied starter desks, disabled future locations, locked-neighbor rejection, non-overlapping zone geometry and primary routes, overview-camera containment, and the independent expanded stage. The original thirteen integration tests continue to run against Established Office and cover scene construction, six-worker simulation, amenities, hiring, firing, reassignment, economy, reporting, UI, and stuck recovery.

The Windows player build, Blender validation (54/54), overview capture, and worker close-up also passed. The historical release evidence and package-verification records from `a638304` remain preserved.
