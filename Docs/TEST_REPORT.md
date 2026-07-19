# Test Report

Worker-placement pivot checkpoint 1 on Unity 6000.5.1f1:

| Suite | Passed | Failed | Duration |
|---|---:|---:|---:|
| EditMode | 27 | 0 | 0.046 s |
| PlayMode | 16 | 0 | 25.852 s |
| Total | 43 | 0 | 25.898 s |

The pre-edit baseline passed 24 EditMode and 13 PlayMode tests (37 total).

New EditMode coverage verifies deterministic default/explicit stage parsing, legacy capture defaults with explicit overrides, and complete `WorkerCommand` placement intent data.

New PlayMode coverage verifies that the Main Menu enters Starter Office, Starter Office independently creates three workers and all six placement activities without a timer, and Starter Office Expanded independently creates three workers with six desks. The original thirteen integration tests now explicitly run against Established Office and continue to cover scene construction, six-worker simulation, amenities, hiring, firing, reassignment, economy, reporting, UI, and stuck recovery.

Final command-line results are written outside the worktree during verification. The historical release evidence and package-verification records from `a638304` remain unchanged.
