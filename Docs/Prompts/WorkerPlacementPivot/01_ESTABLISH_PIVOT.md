# Prompt 1 - Establish the Pivot and Preserve the Existing Office

```text
Work in:

C:\Users\danny\Documents\GitHub\OpenPlan

Read the complete design source before editing:

C:\Users\danny\Downloads\Game Demo Design.pdf

This is an implementation task, not another planning exercise. Work autonomously through implementation, verification, documentation, and the requested commit. Do not stop after analysis while safe in-scope work remains.

We are pivoting from the released a638304 prototype to a smaller, worker-placement-driven first demo.

Before editing:

1. Run git status and preserve any user changes.
2. Confirm the current branch and latest commit.
3. Run the complete existing EditMode and PlayMode test suite.
4. Create and switch to `codex/small-office-placement-pivot`.

Product direction:

- The existing large office is visually strong but is no longer the starting level.
- Preserve its builder and gameplay as an Established Office stage/preview.
- The default playable scene must become a new cramped Level One office.
- The central player verb is picking up workers and placing them at activity areas.
- The simulation has no countdown or automatic failure.
- The first objective is to earn enough cash to purchase the neighboring unit.
- Purchasing it physically opens and enlarges the Level One office.
- The player may continue after expanding or visit the existing large office as a future-stage preview.

Create a revised architecture with these concepts:

- `OfficeStage`: `StarterOffice`, `StarterOfficeExpanded`, `EstablishedOffice`.
- `PlacementActivity`: `Work`, `Rest`, `GetWater`, `BuySnack`, `Smoke`, `LeaveOffice`.
- A `PlacementZone` base component.
- `WorkerCommand` data describing the worker, destination zone, requested activity, issue time, and whether it came from player placement.
- A stage-aware `OfficeDirector` that can initialize the starter or established environment without duplicating core worker systems.

Preserve the current large `OfficeEnvironmentBuilder` as the Established Office implementation. Do not delete its art, amenities, camera composition, hiring/firing systems, screenshots, or previous release evidence.

Change the normal entry path to:

Main Menu -> Starter Office

The Established Office should only become accessible after the first expansion milestone or through an explicit developer/capture argument.

Update:

- `PROTOTYPE_CONTRACT.md`
- `MASTER_ROADMAP.md`
- `SIMULATION_RULES.md`
- `DECISION_LOG.md`
- `RUN_STATE.md`
- `KNOWN_ISSUES.md`

Document today's non-goals:

- Multiple purchasable properties.
- Multiple cities or districts.
- Multiple floors.
- Managers and specialized roles.
- Rival companies.
- Promotions and relationships.
- Complex finance.
- Furniture-placement mode.
- Save-game persistence.

At this checkpoint, the Starter Office may use a temporary minimal environment, but it must load without errors and support three workers.

Testing requirements:

- Existing established office still initializes.
- Starter office initializes independently.
- Stage selection is deterministic.
- Main menu enters Starter Office.
- Automated release/capture paths can explicitly select a stage.
- Existing worker simulation tests remain operational or are intentionally migrated.

Run the complete test suite.

Completion gate:

- The project compiles without new errors.
- Both office stages initialize.
- The original office remains intact.
- The worktree contains only intentional changes.
- All tests pass before committing.

Commit as:

refactor: establish small-office placement pivot

In the final response, report the commit hash, test totals, stage architecture, changed files, and confirmation that the original office remains intact.
```
