# OPEN PLAN Worker-Placement Pivot Prompts

These prompts translate `C:\Users\danny\Downloads\Game Demo Design.pdf` into eight chronological Codex implementation checkpoints.

## Locked defaults

- Click-and-drag is the primary worker-placement interaction.
- The first demo uses simple untimed earnings rather than payroll or client contracts.
- The player purchases one real neighboring unit and physically opens the office wall.
- The Starter Office begins with Morgan, Alex, and Sam.
- The released large office is preserved as the Established Office preview.
- Long-term PDF ideas such as multiple cities, multiple floors, managers, rivals, and complex finance remain roadmap items.

## How to use

1. Start with Prompt 1.
2. Give Codex only one prompt at a time.
3. Do not advance until Codex reports a passing test suite, a clean intentional worktree, and the requested commit.
4. Review screenshots or packaged artifacts at each visual/release gate.
5. If a prompt exposes a genuine blocker, resolve it before continuing rather than weakening the acceptance criteria.

## Execution order

1. [Establish the pivot](01_ESTABLISH_PIVOT.md)
2. [Build the Starter Office](02_BUILD_STARTER_OFFICE.md)
3. [Implement worker pickup and placement](03_WORKER_PICKUP_PLACEMENT.md)
4. [Make activity areas functional](04_FUNCTIONAL_ACTIVITY_AREAS.md)
5. [Add personality and readable status](05_PERSONALITY_STATUS.md)
6. [Add physical expansion](06_PHYSICAL_EXPANSION.md)
7. [Build tutorial and presentation](07_TUTORIAL_PRESENTATION.md)
8. [Balance, validate, and package](08_RELEASE_FRIEND_DEMO.md)

## Final definition of done

- The game starts in a modest office with three named workers.
- Workers can be picked up and placed at Work, Rest, Water, Vending, Smoking, and Exit areas.
- Energy, Mood, Stress, personalities, distractions, name tags, and emotes are readable.
- Desk work earns money with no timer or forced failure.
- The neighboring unit costs $1,000 and physically opens in the current world.
- Expansion unlocks new space, desks, worker capacity, and hiring.
- The original large office remains available as a future-stage preview.
- The final Windows ZIP passes tests, soak, extraction, launch, and the full friend-demo smoke flow.
