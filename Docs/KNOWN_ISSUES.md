# Known Issues and Limitations

No P0 or P1 issue is known for Checkpoint `02_NeedAutonomy`.

## Honest limitations

- Navigation is a deterministic office grid with line-of-sight smoothing. Turns are readable but remain procedural and can look mechanical in dense traffic; there is no local crowd avoidance or authored locomotion animation.
- The navigation index rebuilds when the neighboring unit opens. Future furniture construction must call the exposed invalidation hook and will need its own placement-time performance pass.
- The simulation matrix exercises deterministic routing, reservation, fallback, pause, cost, and return contracts. Its managed-memory observation includes scenario setup allocations and is not profiler-grade per-frame allocation evidence.
- Qualification preference hooks are neutral. Live employees do not receive one strength and one liability until Prompt 03.
- The compact restroom is a readable entrance/use point rather than a modeled interior.
- The seeded vending malfunction is uncommon and may not appear in a short session.
- New hires do not have an onboarding queue or automatically claim a later-vacated desk.
- Hiring, needs, and office state are session-scoped because campaign save/load is not implemented.
- The first expansion is the only property purchase. Established Office remains a preserved future-stage sandbox and does not inherit Starter Office state.
- Characters use procedural gestures; audio/Foley breadth, local avoidance, controller support, remappable controls, graphics settings, localization, and accessibility narration remain incomplete.
- There is no live qualification growth, training, contract/workday loop, payroll, reputation, incident scheduling, furniture construction, or persistence.
- Human playtesting was explicitly waived for this run. Automated suites, deterministic simulations, and exact-package smoke provide the acceptance evidence; no manual acceptance is claimed.

## Current non-goals

Prompt 03 qualifications and growth, then contracts/workdays, payroll/reputation, incidents, furniture build mode, campaign persistence, and final Endless Office Alpha balance/packaging remain later checkpoints. Multiple cities/floors, manager roles, rival companies, promotions, relationships, and complex finance remain outside the current 30-day Alpha scope.
