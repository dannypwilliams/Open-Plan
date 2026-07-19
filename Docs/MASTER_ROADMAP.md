# OPEN PLAN - Master Roadmap

Status: worker-placement pivot checkpoint 4 implemented on 2026-07-19.

## Product direction

The first demo begins with a modest three-worker business and centers on picking up workers and placing them at readable activity areas. Work earns money without a time limit. The first milestone purchases and physically opens the neighboring unit. The released large office is retained as a later Established Office preview.

## Pivot checkpoints

| Checkpoint | Outcome | Gate |
|---|---|---|
| 1 | Establish stage architecture and preserve the released office | PASS - Starter and Established stages initialize; menu defaults to Starter; all tests pass |
| 2 | Build the authored Starter Office | PASS - modest office and locked neighbor are readable, attractive, correctly framed, and validated |
| 3 | Implement worker pickup and placement | PASS - click-and-drag placement is clear, cancellable, robust, and standalone-smoke tested |
| 4 | Make activity areas functional | PASS - all six loops have timed behavior, exact needs/economic effects, cleanup, and standalone evidence |
| 5 | Add personality and readable status | Needs, traits, names, states, and emotes explain behavior |
| 6 | Add physical expansion | A $1,000 purchase opens the neighboring unit in-world |
| 7 | Build tutorial and presentation | A new player can understand and complete the loop |
| 8 | Balance, validate, and package | Tests, soak, media, Windows ZIP, and friend-demo flow pass |

## Preserved release checkpoint

Commit `a638304` remains the reference release for the original Established Office. Its large environment builder, art, amenities, camera composition, hiring/firing flow, screenshots, media, and release evidence are preserved. The pivot reuses its core simulation rather than replacing it.

## Stable-checkpoint policy

Every checkpoint ends with updated run-state documentation, complete automated tests, an intentional worktree audit, and a Git checkpoint. Later work must not make the Established Office the default entry path.

## Current non-goals

Multiple purchasable properties; multiple cities or districts; multiple floors; managers and specialized roles; rival companies; promotions and relationships; complex finance; furniture-placement mode; and save-game persistence.
