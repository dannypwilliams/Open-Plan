# Endless Office Alpha - Checkpoint 01 Five Needs

- Built UTC: 2026-07-20T16:51:32.384Z
- Branch: `codex/endless-office-alpha`
- Source commit: `567c4fee2f386daaeb4250b2cf85b0fa79fbc08f`
- Unity: `6000.5.1f1`
- Configuration: Windows x64, non-development, Mono scripting backend
- Executable: `Windows/OpenPlan.exe`
- Windows build size: 104153653 bytes
- ZIP: `SillyOfficeSim_01_FiveNeeds_Windows.zip`
- ZIP size: 39003138 bytes
- ZIP SHA-256: `7019609D2B62217EA8F37E508DDAD4518791D2AEAABC49E7E4A67432C790139D`
- EditMode: 98 passed, 0 failed, 0 skipped
- PlayMode: 70 passed, 0 failed, 0 skipped
- Total automated: 168 passed, 0 failed
- Exact verification copy: `VerifiedExtract/OpenPlan.exe`
- Exact extracted launch/smoke: PASS
- Gameplay captures: 9, dimension-checked
- Deterministic matrix: PASS; 3/10/30 workers, 20 seeds, six contexts, 100 simulated minutes per row

## Known limitations

- Comprehensive critical-need autonomy, destination selection, reservation arbitration, fallback behavior, and navigation recovery are intentionally deferred to Prompt 02. Manual proximity placement is the reliable intervention path in this checkpoint.
- Existing personality autonomy still reacts to legacy Energy/Happiness/Stress conditions; it is not the complete five-need recovery policy.
- The restroom is a compact readable entrance activity rather than a modeled interior, so the employee remains visible at the use point.
- Worker movement remains direct and deterministic rather than navmesh-routed and can look mechanical around crowded landmarks.
- There is no campaign save/load; expansion, hiring, tutorial, and need state reset when the session ends.
- Qualification pairs, training, contracts/workdays, payroll/reputation, incidents, furniture construction, and later office units remain inactive foundations.
- Audio, controller support, remappable controls, localization, and accessibility narration are not yet implemented.
- The deterministic report's managed-memory figures are coarse before/after observations, not profiler-grade per-frame allocation measurements.

## Source documents updated

- `README.md`
- `Docs/SIMULATION_RULES.md`
- `Docs/FINAL_TUNING_VALUES.md`
- `Docs/KNOWN_ISSUES.md`
- `Docs/TEST_REPORT.md`
- `Docs/BUILD_AND_RUN.md`
- `Docs/RUN_STATE.md`
- `Docs/MASTER_ROADMAP.md`
- `Docs/DECISION_LOG.md`
- Tutorial and Help runtime copy