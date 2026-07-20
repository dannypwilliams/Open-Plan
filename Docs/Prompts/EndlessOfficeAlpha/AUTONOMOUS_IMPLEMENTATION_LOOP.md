# SILLY OFFICE SIM - ENDLESS OFFICE ALPHA AUTONOMOUS IMPLEMENTATION LOOP

- Project: `C:\Users\danny\Documents\GitHub\Silly Office Sim`
- Working branch: `master` (explicit user direction for this run)
- Unity: `6000.5.1f1`
- Governing plan: `Docs/NEXT_30_DAYS_ROADMAP.md`
- Goal: build, validate, and package the complete Endless Office Alpha loop

## Purpose and authority

This is the single executable runbook for the remainder of the Endless Office Alpha. A Codex task launched with the prompt at the end must read this file completely, inspect the repository, resume the earliest incomplete checkpoint, and continue through every checkpoint without waiting for the user to say “next.”

This runbook supersedes the older instruction to stop after every prompt. It does not remove the requirement for a distinct playable build and verification gate after every checkpoint. Codex must run automated suites, deterministic simulations, exact-archive extraction, a public-build smoke flow, and an agent-operated gameplay check before advancing. Genuine human feedback is recorded when available but never invented.

The runbook authorizes local implementation, tests, builds, packaging, screenshots, documentation, commits, and local tags. It does not authorize publishing, pushing, opening a pull request, buying services, contacting testers, changing Steam configuration, or sending external messages.

## Product outcome

The finished Alpha lets the player repeatedly:

1. Start with Morgan, Alex, Sam, three aging desks, and exactly `$0`.
2. Choose a ten-minute contract and run at pause, 1x, 2x, or 4x.
3. Observe five needs, two qualifications, skill, productivity, and current behavior.
4. Let employees recover autonomously or accelerate recovery through placement.
5. Respond to temporary workplace incidents.
6. Earn cash/reputation, then pay payroll and operating costs.
7. Hire employees even when no desk is available.
8. Let deskless hires work by phone at approximately 50% workstation efficiency.
9. Buy, place, rotate, use, and sell essential furniture.
10. Purchase and furnish the neighboring office unit.
11. Finish a day, read the report, autosave, and start another day.
12. Quit and continue from an equivalent deterministic campaign state.
13. Continue generated workdays indefinitely after physical expansion is exhausted.

## Non-negotiable design contract

- New Campaign begins with three named employees, three desks, and `$0`.
- Hiring is affordability-gated, not expansion-, desk-, or roster-gated.
- Deskless employees work by phone with one exact `0.50` workstation modifier.
- Workers may be released on arbitrary valid unlocked ground; proximity influence triggers activities.
- The five needs are Happiness, Hunger, Bathroom, Inspiration, and Energy.
- Happiness, Inspiration, and Energy are high-good; Hunger and Bathroom are high-urgent.
- Stress is a temporary influence, never a sixth need.
- Every employee has exactly one strength and one liability with contextual effects.
- Incidents are temporary, fair, recoverable, and never permanently destroy employees/furniture.
- Contract failure may reduce rewards/reputation but never deletes the campaign.
- Negative cash prevents discretionary purchases but never prevents recovery work or another day.
- Furniture spends only after valid placement; canceling spends nothing.
- Campaign randomness remains seeded and serializable.
- No hard gameplay/UI employee maximum. Balance targets 3–30; stability targets 100.
- The neighboring unit is the only required additional physical space for this Alpha.
- Windows keyboard/mouse and English are the Alpha targets; strings remain localization-ready.

## Loop state

Codex owns and updates this block. A checked item is not trusted without evidence.

<!-- LOOP_STATE_START -->
- Loop status: `PAUSED_AFTER_DIRECT_PROMPT_02`
- Earliest incomplete checkpoint: `03_QualificationsGrowth`
- Last evidence-verified checkpoint: `02_NeedAutonomy`
- Last evidence audit: `2026-07-20`
- Active blocker: `none`
- Final Alpha status: `NOT_READY`
<!-- LOOP_STATE_END -->

## Completion ledger

### 00_Foundation

- [x] Feature implementation committed
- [x] Complete EditMode and PlayMode suites passed
- [x] Non-development Windows build packaged
- [x] Exact ZIP extraction completed public-gameplay smoke
- [x] Manifest/checksum and documentation recorded
- [ ] Human playtest feedback recorded — nonblocking
- Evidence: `outputs/Playtests/EndlessOfficeAlpha/00_Foundation/CHECKPOINT_MANIFEST.json`
- Source commit: `317343616df6fd9456e6102aceed3d9b1ae612b6`
- Status: `COMPLETE_AUTOMATED_GATE`

### 01_FiveNeeds

- [x] Feature implementation committed
- [x] Complete suites passed from committed source
- [x] Deterministic 3/10/30-worker matrix passed
- [x] Packaged build and exact-extraction smoke passed
- [x] Manifest/checksum and documentation recorded
- [x] Agent gameplay check recorded
- [ ] Human playtest feedback recorded — nonblocking
- Evidence: `outputs/Playtests/EndlessOfficeAlpha/01_FiveNeeds/manifest.md`
- Source commit: `567c4fe`
- Status: `COMPLETE_AUTOMATED_GATE`

### Remaining checkpoints

For each entry, all six mandatory boxes must pass before advancing. Human feedback is separate and nonblocking until the commercial-release gate.

| Checkpoint | Feature | Tests | Matrix | Package/smoke | Evidence/docs | Agent check | Status |
|---|---|---|---|---|---|---|---|
| 02_NeedAutonomy | [x] | [x] | [x] | [x] | [x] | [x] | COMPLETE_AUTOMATED_GATE |
| 03_QualificationsGrowth | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] | NOT_STARTED |
| 04_ContractWorkdays | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] | NOT_STARTED |
| 05_EndlessEconomy | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] | NOT_STARTED |
| 06_IncidentFramework | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] | NOT_STARTED |
| 07_TactileIncidents | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] | NOT_STARTED |
| 08_BuildMode | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] | NOT_STARTED |
| 09_FunctionalAmenities | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] | NOT_STARTED |
| 10_ExpansionProgression | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] | NOT_STARTED |
| 11_CampaignPersistence | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] | NOT_STARTED |
| 12_CohesionPolish | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] | NOT_STARTED |
| 13_EndlessOfficeAlphaCandidate | [ ] | [ ] | [ ] | [ ] | [ ] | [ ] | NOT_STARTED |

Human feedback for Checkpoints 02–12 is recorded in each checkpoint package when available and does not block the autonomous implementation loop. Genuine external human results remain required before a commercial-release claim.

## Evidence rule for checkboxes

Never check a box because prose says a feature exists. Verify:

1. The source commit exists on the current branch.
2. The worktree was clean when its package was built.
3. Complete EditMode and PlayMode XML has zero failures.
4. Checkpoint-specific deterministic/soak reports pass.
5. A non-development Windows build exists.
6. A fresh archive was created without overwriting prior evidence.
7. That exact archive was extracted separately and launched.
8. The extracted executable completed its checkpoint public-flow smoke.
9. Screenshots have expected dimensions and show genuine gameplay.
10. A manifest records commit, branch, Unity version, counts, hash, size, timestamp, and issues.
11. Source-of-truth documentation matches shipped behavior.
12. No P0 crash, data loss, permanent stuck state/modifier, invalid ledger, or replay divergence remains.

Check human boxes only when genuine answers/observations exist. Agent smoke, simulations, and automated UI flows must be labeled agent evidence.

## Autonomous execution algorithm

### Start or resume

1. Read this entire file.
2. Inspect branch/status/log, manifests, test results, `RUN_STATE`, `KNOWN_ISSUES`, and `TEST_REPORT`.
3. Preserve existing work. Never destructively reset or discard changes.
4. Verify checked mandatory boxes chronologically.
5. If evidence is missing or contradictory, uncheck it and dependent downstream boxes, record the audit, and resume there.
6. Select the earliest checkpoint with an unchecked mandatory box.
7. Update loop state to `RUNNING` and checkpoint status to `IN_PROGRESS`.
8. Continue compatible work before replacing anything.

### Execute one checkpoint

1. Read its specification and all current source/docs it references.
2. Add a concise implementation plan to the active task.
3. Implement only its scope plus prerequisite P0 repairs.
4. Keep data deterministic and future-saveable.
5. Add EditMode/PlayMode coverage through public gameplay paths.
6. Run focused tests while developing, then complete suites.
7. Run its simulation/performance/lifecycle matrix.
8. Update tuning, rules, known issues, tests, decision log, and run state.
9. Review the diff and commit with its feature message.

### Repair loop

If compile, test, simulation, build, extraction, or smoke fails:

1. Record the command, test, seed/state, and first useful error.
2. Reproduce with the smallest public-path case.
3. Diagnose ownership/lifecycle; do not weaken assertions.
4. Make the smallest coherent repair.
5. Rerun the focused failure, then affected complete suites.
6. Repeat until the complete gate passes.

Never delete tests, loosen invariants, skip cleanup, substitute sleeps for state ownership, or hide failure behind development-only behavior. After three materially different failed approaches, write a blocker audit and attempt a simpler architecture. Stop only for a genuine external blocker: missing credentials/licensed input, an essential human choice with materially different results, or broken external infrastructure with no local substitute.

### Package and inspect

1. Require a clean feature commit.
2. Generalize the checkpoint pipeline rather than cloning large scripts when practical.
3. Never overwrite a published checkpoint directory.
4. Publish under `outputs/Playtests/EndlessOfficeAlpha/<CheckpointId>/`.
5. Create a fresh non-development Windows ZIP.
6. Extract that exact ZIP once into a preserved verification directory.
7. Launch the extracted executable visibly.
8. Complete its smoke through public UI/gameplay APIs.
9. Capture required screenshots/reports; verify menu return and clean quit.

### Close and advance

1. Fill the manifest with actual results.
2. Update this ledger and loop state.
3. If evidence could not be recorded before the clean build, make `docs: record <checkpoint> completion` afterward.
4. Confirm no unexplained worktree change.
5. Mark `COMPLETE_AUTOMATED_GATE` only after every mandatory box passes.
6. Leave absent human feedback pending; never fabricate it.
7. Immediately start the next incomplete checkpoint without asking the user.
8. After Checkpoint 13, set status to `READY_FOR_HUMAN_ALPHA_REVIEW` or `BLOCKED`, never `COMMERCIAL_RELEASE_READY` without genuine external testing.

## Shared definition of done

Every checkpoint inherits:

- Preserve accepted behavior and unrelated user work.
- No new compiler error/warning caused by the checkpoint.
- Existing/new tests and deterministic replay pass.
- Pause freezes simulation-owned timers/transitions.
- Restart/menu/teardown release transient state.
- No cost/reward/recovery/reservation/effect applies twice.
- UI works at 1280x720 and 1920x1080 unless a four-resolution gate is specified.
- Public builds show no test controls/fake completion paths.
- Docs and known issues match the build.
- A playtest guide and raw-results template ship with each package.
- Unity caches and disposable generated files are not committed.

## Checkpoint 01 recovery — Five Needs

### Objective and contract

Finish and evidence the currently uncommitted implementation; do not discard it.

- Five authoritative `0–1` needs: Happiness, Hunger, Bathroom, Inspiration, Energy.
- Correct high-good/high-urgent semantics; Mood aliases Happiness; Stress is not a sixth row.
- Central definitions own IDs, defaults, direction, rates, thresholds, copy, colors, and improving activities.
- Scaled deterministic tick; pause freezes; 2x/4x proportional.
- Work, Rest, Water, Vending, Coffee, Smoking, Away, Social, and Restroom effects are centralized/exact-once.
- Visible reachable single-capacity restroom substantially reduces Bathroom and never strands/hides workers.
- Needs affect productivity without crushing normal output; phone factor remains `0.50` once.
- Five-row inspector, urgency wording, hover help, influences, and 1280x720 readability.

### Gate

- Preserve or exceed the reported 97 EditMode/70 PlayMode baseline using fresh XML.
- Run 3/10/30 workers, 20 seeds, six contexts, 100 simulated minutes; zero invalid values, pause drift, unexpected synchronization, or replay divergence.
- Verify activity transactions, carry preservation, restart cleanup, phone worker, restroom lifecycle.
- Package `01_FiveNeeds`; exact extracted public flow, nine captures including 1280x720, menu return, clean quit.
- Feature commit: `feat: activate five employee needs`
- Package: `outputs/Playtests/EndlessOfficeAlpha/01_FiveNeeds/`
- Then advance immediately to Checkpoint 02.

## Checkpoint 02 — Need Autonomy and Obstacle-Aware Navigation

### Objective

Employees recognize urgent needs, select reachable recovery, reserve capacity, navigate obstacles, recover from failures, and resume desk/phone work without player intervention. Placement remains the faster strategic intervention.

### Decision model

- Extend one worker state machine; record structured category, need, activity, destination, score/reason, origin, reservation, retries, and fallback.
- Evaluate healthy workers every 3–6 simulated seconds, caution 2–4, urgent 1–2, staggered deterministically.
- Priority: critical Bathroom, Hunger, Energy, Happiness/Inspiration; then urgent in that order; then Stress recovery, distraction, work.
- Critical Bathroom responds within one second and may override after a maximum three-second warning; critical Hunger overrides ordinary work within five.
- Use hysteresis so small score changes do not thrash destinations.
- Player activity commands receive 15–30 seconds authority or last through completion except explained emergencies.

### Selection and fallback

- Score urgency, recovery, multi-need value, travel, capacity/wait, cooldown, cost, duration, reachability, future trait hook, zone priority, and stable ID.
- Hunger uses affordable food or a slower free off-site meal. `$0` cannot create permanent starvation.
- Bathroom uses a restroom or slower emergency off-site fallback.
- Energy uses Rest, Coffee, or Away. High Bathroom makes Coffee less attractive.
- Happiness/Inspiration use relevant break/social/coffee/smoking/away actions without one universal optimum.
- Never select locked, disabled, full, cooldown-blocked, unaffordable, or unreachable targets.

### Reservations and navigation

- At most one reservation per worker; occupancy plus reservations never exceeds capacity.
- Completion, timeout, reroute, pickup, firing, disable, expansion, restart, and teardown release idempotently.
- Implement reliable runtime NavMesh, deterministic grid/graph, or waypoint routing around walls, furniture, voids, and locked units.
- Begin activity only after arrival and revalidation.
- Detect no progress after roughly 1.5–2.5 seconds; repath, try another arrival, release/reroute, then validated safe fallback.
- A final safety correction can restore the last reachable point only after bounded attempts and must be instrumented.
- Preserve desk ownership; return desk workers to their desk and deskless workers to phone work.

### Readability

- Inspector/world cues explain autonomous/player origin, addressed need, destination, waiting/rerouting, and critical override.
- Sleeping/wandering/social employees yield to critical needs and release partners/props safely.
- Ground-dropped workers settle at the chosen point, then resume autonomy without losing desk assignment.

### Tests and matrix

- EditMode: priority, scoring, tie-breaking, affordability, multi-need choice, hysteresis, authority, reservation invariants, deterministic staggering, reachability, path failure, safe fallback.
- PlayMode: all five recoveries, wall/locked routing, shared restroom, pickup/fire/restart cleanup, distraction interruption, desk return, phone return, pause, readable reason.
- Run 20 seeds at 3/10/30 employees for normal, shared-restroom, no-cash, disabled/unreachable station, distraction-heavy, passive, and active cases.
- Require zero permanent critical needs, stuck workers, orphan reservations, over-capacity, lost desks, duplicate charges, or divergence.
- Target active output advantage 10–25%, never above 35%.

### Evidence and handoff

- Capture autonomous restroom/food recovery, wall routing, shared station, desk/phone return, player authority, critical override, and inspector reason.
- Agent check: five passive minutes; autonomous recovery; ground/activity command; pause mid-route; deskless hire/return.
- Feature commit: `feat: add autonomous need recovery`
- Package: `outputs/Playtests/EndlessOfficeAlpha/02_NeedAutonomy/`
- Update simulation/navigation rules, tuning, tests, known issues, decision log, and run state.

## Checkpoint 03 — Qualifications, Skill Growth, and Employee Readability

### Objective

Give every employee exactly one readable strength and one readable liability, make all 24 qualifications affect contextual behavior, and add gradual skill growth plus bounded paid training. With names hidden, Morgan, Alex, and Sam should remain distinguishable.

### Data contract

- Keep stable serialization-safe qualification IDs. Every worker/snapshot exposes exactly one Strength ID and one Liability ID.
- Starting pairs use the same system as hires. Default curated identities:
  - Morgan: `Extremely Hard Worker` + `Heavy Smoker`.
  - Alex: `Team Player` + `Office Gossip`.
  - Sam: `Quick on Their Feet` + `Lazy`.
- A better pair may replace a default only to preserve prior accepted character identity; document it.
- Hires roll one of each through campaign randomness. Same seed/hire order reproduces pairs.
- Reject missing IDs, duplicate polarity, two strengths/liabilities, and runtime rerolls.

### Contextual effects

- Refactor placeholder universal modifiers into allocation-free context hooks.
- Each of 24 definitions influences at least one: work persistence/quality, distraction resistance, a named need, walking, social behavior, recovery preference, training/skill, or future incident response.
- No strength wins every context; no liability creates a permanent failure loop.
- Do not stack equivalent legacy personality and qualification modifiers twice.
- Critical needs and reachability/affordability/capacity always outrank trait preferences.
- Inspector must name the currently active effect in concrete language.

Minimum examples:

- Hard Worker persists longer but still obeys critical needs.
- Organized reduces setup/recovery overhead.
- Fast Learner earns more experience.
- Team Player grants modest nearby collaboration value.
- Coffee Addict benefits more from Coffee but suffers a contextual deprivation tradeoff.
- Quick on Their Feet walks faster without shortening activity duration.
- Lazy avoids work more, not all work.
- Easily Distracted is mitigated by Inspiration.
- Frequently Hungry changes Hunger decay rather than universally nerfing output.
- Slow Walker never reaches zero speed.
- Heavy Smoker prefers Smoking with time opportunity cost.
- Constant Breaks changes persistence/need decay.
- Clumsy exposes bounded future incident susceptibility.
- Office Gossip makes social recovery longer and costlier in work time.
- Technophobe affects powered work context, never all work.

### Skill and training

- Track experience separately from displayed skill; grant only from actual productive contribution.
- Phone work grows in proportion to its lower output.
- Use diminishing returns and a documented Alpha range, recommended `0.65–1.50` with soft cap near `1.35`.
- Never reduce skill for failure.
- `Train` requires cash and time, stops work, grants deterministic experience once, and never changes qualifications.
- Define exact charge/reward boundary and interruption/refund behavior.
- Critical needs, pickup, firing, restart, report, and menu teardown cannot duplicate charge/reward or trap the worker.

### UI

- Hiring cards show cost, salary hook, strength, liability, skill, and practical summary.
- Inspector replaces legacy Personality with two polarity-labeled cards, active effect, skill progress, Train cost/duration/status.
- Tooltips use concrete magnitudes where useful and fit with five needs at 1280x720.

### Tests and matrix

- Exactly 12 unique strengths/12 liabilities; every definition reachable and effect registered.
- Curated pairs exact; hires deterministic; never wrong polarity or reroll.
- Test every primary effect and one interaction/boundary.
- Critical needs outrank preferences; speed traits preserve navigation/stuck recovery.
- Experience/training exact-once, pause-safe, bounded, interruption-safe, and insufficient-cash-safe.
- Run 20 seeds with 3/10/30 employees for 30 work-equivalent minutes; require reproducible pairs, bounded skill, no trait deadlock, and non-dominating output spread.
- Produce behavior-frequency comparison for the starting trio.

### Evidence and handoff

- Capture three starter cards/behaviors, deterministic hires, skill progress, training, and critical interruption.
- Agent check: compare starters, hire two candidates, train one, and verify identities without constant intervention.
- Feature commit: `feat: add employee qualifications and growth`
- Package: `outputs/Playtests/EndlessOfficeAlpha/03_QualificationsGrowth/`
- Update catalog, tuning, simulation/UI rules, tests, known issues, decision log, and run state.

## Checkpoint 04 — Contract Workdays and Repeated Day Flow

### Objective

Turn the untimed sandbox into repeatable ten-minute workdays: choose one of three contracts, contribute desk/phone work, resolve success/failure, read a report, and begin another day. Payroll, costs, and active reputation consequences arrive in Checkpoint 05.

### State machine

One owner exposes:

- `Preparing`: simulation paused; exactly three contract options; no work/needs.
- `Running`: selected contract; ten simulation minutes; pause/1x/2x/4x.
- `Reporting`: simulation stopped; outcome and reconciled pre-payroll summary.
- `Transitioning`: cleanup, next-day generation, worker/station normalization, future autosave hook.

Transitions are idempotent/event-driven. Contracts cannot start/resolve/reward twice. Pausing freezes deadline without changing state. Tutorial/modal ownership restores prior speed. Carry, Away, Restroom, Training, Social, and reservations receive one tested day-end policy. `Start Next Day` returns to Preparing; failure never ends the campaign.

### Contract generation

Generate deterministically from current conservative capacity:

- Safe: roughly 65–75% of passive capacity, smaller bonus.
- Standard: roughly 90–105% of managed capacity, normal bonus.
- Ambitious: roughly 115–135%, larger bonus/optional qualification preference.

Each runtime stores stable ID, localizable name/text, tier, required/current work, base/bonus cash, future reputation reward, preference, deadline, seed provenance, selection time, and resolution state. Projection uses desk/phone mix, skill, and conservative need/autonomy assumptions but never inspects future random choices or guarantees success.

### Contribution and time

- Desk/phone work uses the authoritative output event already used for cash/experience.
- Workstation/phone factor applies once. Continuous work income remains so `$0` opening is active.
- Contract and cash ledgers listen separately to the same contribution.
- Paused/training/travel/recovery/distraction/incident-blocked workers do not contribute unless explicitly defined.
- Preference bonus is modest/contextual.
- Exact deadline stops progress before one resolution; time results are invariant across speeds.

### HUD/report/day boundary

- HUD: Day, remaining time, contract/progress/projection, Cash, Team, Desks, speed.
- Cards: target, difficulty, bonus, preference, plain risk.
- Report: outcome, work income, bonus, required/completed, productivity, top contributor, notable need/training, hires/fires, Next Day.
- Omit or mark future payroll/reputation rows honestly.
- Active activity/training/away/carry/reservations at day end normalize through documented exact-once rules.
- Needs carry forward with only documented overnight normalization; no silent full reset.
- Report fits 1280x720 and owns pause/input safely.

### Tests and matrix

- Every legal/illegal transition; deterministic unique options; ordered/feasible tiers; double-input protection.
- Ten simulated minutes across 1x/2x/4x/pause; exact deadline; contributions counted once.
- Success bonus once; failure no bonus and campaign continues; report reconciles.
- Next Day preserves roster/state and generates deterministic new options.
- Run at least 20 public-transition days for Safe/Standard/Ambitious strategies without manual API shortcuts.
- Require no frozen report, duplicate payout, lost worker, negative timer, invalid state, or replay divergence.

### Evidence and handoff

- Capture three cards, running/paused HUD, success/failure reports, and third-day start.
- Agent check: Standard full day, next day, intentional failure, third day.
- Feature commit: `feat: add repeatable contract workdays`
- Package: `outputs/Playtests/EndlessOfficeAlpha/04_ContractWorkdays/`
- Update workday/contracts, tuning, tests, tutorial/help, known issues, decision log, and run state.

## Checkpoint 05 — Payroll, Reputation, Recovery, and Endless Economy

### Objective

Make money create durable decisions through payroll, operating costs, reputation, recovery contracts, pricing, contract scaling, and an endless economy that never permanently traps the campaign.

### Ledger and transactions

Use one cash owner. Every record includes day, type, signed amount, stable related ID, and reason. Categories cover work income, contract bonus, hiring, training, payroll, operating cost, expansion, future furniture sale/purchase, incident expense, and recovery help.

- Transactions commit exactly once; current cash, lifetime totals, daily gross/net, and report reconcile.
- Discretionary spending rejects when unaffordable; day-end obligations may produce negative cash.
- Negative cash never stops work, free need recovery, contract selection, or Next Day.
- Consolidate/adapt `CashDirector`; never introduce a competing cash model.

### Payroll/costs/reputation

- Each employee has bounded salary derived from role/skill/qualifications; starting payroll is survivable but meaningful.
- Payroll posts once after contract outcome using a documented order and roster snapshot so deadline hiring/firing cannot exploit it.
- Costs begin with owned-unit base plus active amenity hooks.
- Reputation is bounded and visible with Cramped Startup, Established Office, Chaotic Corporate Floor tiers.
- Success gains reputation by risk; failure loses modestly; Safe farming has diminishing value.
- Reputation influences contract scaling and future unlocks without circular locking.
- Tier messages queue safely.

### Recovery and pacing

- Negative cash or low capacity guarantees a feasible no-upfront Recovery contract.
- Never generate three impossible/unaffordable choices; `$0`, phone-only, overcrowded, or poor prior play retain progress.
- Prevent reopen-selection payout, pre-payroll firing, training cancel/reward, and future buy/sell exploits.
- Target first decision under 90 seconds, hire 3–5 minutes, neighboring unit 12–18 minutes if hiring first, active output 15–30% over passive, and no 45-second dead period.
- Safe grows slowly, Standard steadily, Ambitious volatile/recoverable. Thirty days should not become trivial infinite wealth or unrecoverable debt.

### UI/report

- HUD: Cash, income/minute, Reputation/tier, Team/Desks, contract/time/speed.
- Hiring/training show cost, salary, affordability, expected impact.
- Report reconciles opening cash, work, bonus, payroll, costs, purchases, closing cash, and reputation.
- Negative cash copy explains recovery rather than game-over.

### Tests and matrix

- Ledger arithmetic/exactness, affordability, ordering, roster snapshot, salary/cost scaling, reputation bounds/tiers, report reconciliation.
- Negative cash disables discretionary actions but not work/day progression.
- Recovery always exists/is feasible; no transaction exploit; future hooks neutral safely.
- Same seed/strategy reproduces 30-day ledger.
- Run active, passive, poor, phone-only, overcrowded, recovery, Safe-only, Standard-only, Ambitious over 30 days at 3/10/30 workers.
- Report milestone times, cash/reputation distributions, outcomes, payroll ratio, recovery days, hires, expansion affordability, output.
- Zero NaN, duplicate charge/reward, debt trap, unavailable Next Day, or replay divergence.

### Evidence and handoff

- Capture reconciled report, hire, unaffordable action, negative-cash Recovery, tier change, three-day ledger.
- Agent check: start `$0`, reach hire, complete/fail, pay payroll, take public negative-cash/recovery path, continue.
- Feature commit: `feat: add endless office economy`
- Package: `outputs/Playtests/EndlessOfficeAlpha/05_EndlessEconomy/`
- Update economy tuning/rules, report contract, tests, known issues, decision log, and run state.

## Checkpoint 06 — Incident Director and Timed-Choice Framework

### Objective

Create the deterministic incident framework and ship Internet Outage and Power Failure end to end. Events create temporary readable choices and can never leave permanent modifiers.

### Definitions/runtime

Definitions own stable ID/kind, localizable copy, eligibility, weight, tier/worker/asset requirements, duration/deadline, effect, 2–3 responses, success/failure/timeout, cue IDs, trait tags, accessibility alternative, and cooldown family. Runtime owns instance/variant, start/day, phase/timers, response/progress, affected stable IDs, owned modifier handles, resolution state, and random provenance. Never use scene references as stable identity.

### Fairness director

- No incident during first three simulated campaign minutes, Preparing/report/tutorial modal, expansion, or unsafe transition.
- One major incident at a time; post-resolution cooldown deterministically random 180–300 seconds.
- Cooldowns/deadlines use simulation time and pause.
- Alternate the two families while both are eligible; later use recent-family exclusions.
- Eligibility may consider tier/workers/assets/Clumsy hook without guaranteeing punishment.
- No eligible event defers cheaply without spinning or consuming fake history.
- Pre-save day end safely auto-resolves active incident through one tested rule.

### Modifier ownership and UI

- Incident modifiers are instance-owned, additive/composed over base state, removed only by owner, and cleaned idempotently on every resolution/timeout/restart/menu/teardown/day-end route.
- Never restore stale guessed base values over legitimate later changes.
- Shared panel shows title, consequence, affected system, time, choices with cost/risk, progress/outcome.
- One click cannot choose/charge twice; modal ownership prevents world/build/carry leakage; pause freezes; 1280x720 fits; color is not sole cue.
- Public settings: Extended Timers (at least 1.5x with same rewards) and Choice-Only Events. Persistence comes later.

### Internet Outage

- Reduce desk/phone productivity substantially; optional documented offline fraction; apply phone/outage factors once each.
- Responses: Restart Router, Call Provider (cost/safer), Offline Work (bounded penalty).
- Router visibly blinks/offline; HUD/inspector explains influence; needs/navigation/nonpowered actions work.
- Resolution restores exact output.

### Power Failure

- Powered desk/phone work pauses or nearly stops; navigation/needs/nonpowered recovery remain safe.
- Office uses readable emergency cyan/amber lighting and never turns pure black.
- Responses: Backup Generator (cost/action), Everyone on Break (lost work/need recovery), Wait for Utility (slower).
- Workers exit impossible powered states; restoration returns lights, availability, modifiers, and behavior exactly once.

### Tests and matrix

- Definition/eligibility/grace/one-active/cooldown/fairness/determinism/pause/modal/day-end/no-eligible.
- Modifier add/remove/idempotence/stale-base/restart/menu cleanup.
- Response exactness, cost, Extended/Choice-Only, timeout/success/failure/input ownership.
- Both family effects on desk/phone, visuals/influences, worker safety, full restoration.
- Schedule/resolve at least 100 incidents over 20 seeds at 3/10/30 employees.
- Zero early/overlapping/cooldown-violating events, orphan modifier, stuck worker, duplicate expense/reward, pause drift, or divergence.

### Evidence and handoff

- Capture warning, panels, outage/router, emergency light, every outcome type, restored office.
- Agent check: encounter both via public deterministic flow, different responses, pause one, timeout one, verify next day/restoration.
- Feature commit: `feat: add fair workplace incident framework`
- Package: `outputs/Playtests/EndlessOfficeAlpha/06_IncidentFramework/`
- Update incident/accessibility rules, tuning, tests, report hooks, known issues, decision log, run state.

## Checkpoint 07 — Four Tactile Incidents and Complete Alpha Event Set

### Objective

Add Printer Jam, Coffee Spill, Water Leak, and Animal Intruder for six complete families. Each has two deterministic text/visual variants, trait reactions, accessible alternatives, and complete success/failure/timeout cleanup.

### Shared rules

- Reuse one director/modal/modifier/cleanup system.
- Variants alter prop/copy/target/weights but preserve family identity.
- Qualification reactions add story/modest outcomes; none is mandatory.
- Failure creates a funny 45–120-second slowdown/expense/need/distraction, never permanent destruction/injury/removal.
- Every temporary prop/effect belongs to the incident instance.
- Incidents are distinguishable muted and without title; color is never the only cue.

### Printer Jam

- Disable/penalize printer landmark; show paper jam/warning/clutter.
- Responses: ordered three-click Clear Carefully; fast risky Hit It; paid safe Call Service.
- Choice-Only replaces ordered interaction.
- Random clicks do not progress; paper/station/input always clean up.
- Organized/Tech Savvy/Technophobe/Clumsy create bounded reactions.

### Coffee Spill

- Temporarily disable one eligible workstation with visible spill; preserve employee desk ownership.
- Responses: Save Equipment, Clean First, Move Employee via existing placement with Choice-Only alternative.
- Never target already-owned disabled station; modal and placement input do not leak.
- Cleanup restores workstation/removes puddle and preserves needs/reservations/work ledger.

### Water Leak

- Disable Water area and penalize nearby path/comfort without permanent blockage; show drip/puddle/caution/cyan reflection.
- Responses: click/hold Shut Valve, longer Mop, paid safe Maintenance.
- Hold pauses correctly; Choice-Only equivalent; navigation invalidates/restores.
- Critical needs retain water-independent fallback.

### Animal Intruder

- Pigeon/raccoon moving target creates bounded localized distraction; no unbounded reaction chain.
- Responses: Lure Outside moving-click sequence, Assign Employee opportunity cost, Ignore temporary wave.
- Target stays inside unlocked visible world and outside HUD; Reduced Motion limits movement; Choice-Only replaces clicks.
- Animal/reactions/modifiers clean on every path.

### Trait reactions and presentation

- Wire Organized, Tech Savvy, Technophobe, Clumsy, Quick on Their Feet, Team Player, Lazy, Office Gossip and relevant traits with deterministic fairness caps.
- Add paper, puddles, blink, emergency light, and animal visuals in the art-bible palette.
- Add owned/procedural/licensed audio only with provenance; otherwise document limitation.

### Tests and matrix

- Six families/two variants/stable text IDs/valid targets.
- Every response begin/success/failure/timeout/pause/Extended/Choice-Only/restart/report/menu/teardown.
- Temporary ownership; Spill desk preservation; Leak routes; ordered/hold/placement/moving-target input.
- Trait reactions/fairness; no UI leakage.
- At least 300 incidents over 50 seeds, 3/10/30 employees, economy conditions, accessibility modes.
- Zero orphan prop/modifier, permanent disabled station, lost desk, duplicate transaction, unresolved modal, chain/cooldown violation, stuck worker, divergence.

### Evidence and handoff

- Capture two variants where practical, each interaction type/accessibility alternative, trait reaction, failure penalty, restored office.
- Agent check: operate all families, pause, timeout/fail/succeed, both accessibility modes, inspect restoration.
- Feature commit: `feat: complete alpha workplace incidents`
- Package: `outputs/Playtests/EndlessOfficeAlpha/07_TactileIncidents/`
- Update catalog, art/audio provenance, accessibility, tuning, tests, known issues, decision log, run state.

## Checkpoint 08 — Furniture Build Mode Foundation

### Objective

Make money visibly change the office through catalog, ghost preview, rotation, validation, purchase, selection, and sale. Ship Workstation Bundle and Plant; need amenities arrive next.

### Ownership and input

One build controller owns Closed, Catalog, Preview, Valid/Invalid, Commit, Select, Sale Confirm, Cancel.

- Enter pauses and remembers legal prior speed; exit restores unless another modal owns pause.
- Build exclusively owns relevant input; carry/selection/incident/hiring/report cannot leak.
- Camera pan/zoom remains when compatible.
- Left commits; `Q/E` rotate 90°; right/Escape cancels without spending; Delete/sale uses confirmation.
- Day/report/incident transitions cannot bisect a transaction.

### Data and preview

Definitions own stable/localizable IDs, category, cost/sale rate, unlock, footprint/clearance, rotations, visual recipe, collision/route rules, provided station/activity, operating/incident hooks, cues. Instances own deterministic stable ID, definition, unit, transform, paid price, day, starting-fixture flag, enabled state, registered handles. Never rely on scene IDs.

- Ghost shows model/footprint/orientation, modest grid, price/cash, final use point and warnings.
- Invalid uses color-independent symbol/text/audio with specific reason.
- Structured validation result records transform, validity, reason, blocker/route, unit, warnings.
- Validate unlocked bounds, walls, furniture, worker/use clearance, entrance/exit, restroom/essential access, primary routes, incident clearance, floor/support, navigation feasibility.
- Distinguish locked property, outside, wall, furniture, entrance, route, station, floor, cash, unsafe transition.

### Atomic transactions

- Preview/cancel costs nothing; affordability rechecks at commit.
- Cash and object registration commit atomically; any registration failure rolls both back.
- Sale returns exactly 50% actual purchase price; starting fixtures reject sale.
- Sale unregisters every collider, obstacle, workstation/station, influence, incident tag, and effect.
- Buying/selling cannot profit; ledger/report records once.

### Items

- Workstation Bundle: aged desk/chair/CRT/task light/cables/papers, dynamic workstation and Work zone, reachable use point. Provide safe temporary manual assignment until automatic claiming next.
- Plant: low-poly decor with modest capped Happiness/Inspiration influence through indexed hooks, not per-frame scans.
- Selling an active item follows one blocked-or-safe-displace policy and cannot strand a worker/reservation.

### Tests and matrix

- State/input/pause, definitions/IDs, snapping/rotations, every rejection reason.
- Cancel/commit/rollback/sale/start-fixture exact transactions.
- Workstation/plant registration, navigation/incident/report hooks, cleanup under restart/menu/report/incident.
- Run 100 deterministic place/cancel/rotate/sell cycles: exact cash, zero orphan object/collider/zone, stable navigation, bounded allocation.
- Stress-place reasonable counts without full office scans each frame.

### Evidence and handoff

- Capture catalog, valid/invalid ghosts/reasons, rotations, workstation/plant use, sale, cancel unchanged cash, dense route.
- Agent check: earn, place/cancel/rotate, exercise invalid categories, use workstation, sell purchases, complete day.
- Feature commit: `feat: add office furniture build mode`
- Package: `outputs/Playtests/EndlessOfficeAlpha/08_BuildMode/`
- Update catalog/controls/tuning/transactions/tests/issues/decision log/run state.

## Checkpoint 09 — Functional Amenities and Automatic Desk Claiming

### Objective

Complete five P0 furniture items with Break Chair, Water Cooler, and Vending Machine as dynamic stations. Deskless employees automatically claim workstations safely; players can inspect/override.

### P0 scope

Required:

1. Workstation Bundle
2. Break Chair
3. Water Cooler
4. Vending Machine
5. Plant

P1 Coffee Machine, Filing Cabinet, Trash/Recycling, Desk Lamp, Cubicle Divider remain cut-line items until all P0/persistence gates pass.

### Dynamic registration

Each functional item atomically registers instance, footprint/collider, navigation invalidation, station/capacity, activity, influence, arrival/wait points, central need effects, operating-cost hook, incident tags, and landmark/highlight. Selling/disabling reverses in safe order; no cache retains destroyed references.

- Break Chair: one-capacity Rest, existing effects/scoring, readable reservation/disabled status, reachable use point.
- Water Cooler: Water effects/Bathroom tradeoff, cyan-white light, Water Leak tag, deterministic multi-cooler scoring, safe reroute on sale/disable.
- Vending: paid food/malfunction if retained, clear price, affordability before reservation/payment, powered/food tags, safe sell-during-use policy.

### Desk claiming

- Ordered deskless queue uses stable hire sequence.
- After a workstation transaction, earliest eligible worker claims deterministically, navigates there, leaves phone work, applies normal workstation factor once.
- Sold/disabled assigned desk returns worker safely to phone work while preserving employee data and queue order.
- Firing offers vacancy to next eligible worker; starting desks share registry.
- Never two workers/desk or two desks/worker.
- Inspector/workstation shows assignment/source; allow confirmed manual reassign without making it mandatory.
- Manual override preserves invariants; optional auto-assign setting remains future-persistable.

### Placement/navigation

- Preview shows use point, influence, route clearance, capacity.
- Reject inaccessible use point even if footprint fits.
- Rebuild/invalidate navigation before reservations target the new station.
- Locked-unit dynamic stations remain disabled before expansion.

### Tests and matrix

- All five definitions; dynamic register/unregister exactness.
- Rest/Water/Vending effects, cooldown, costs, capacity, affordability, malfunction, incident disable.
- Multi-station deterministic scoring; sale free/reserved/in-use/disabled cleanup.
- Deskless queue/new desk/firing/sale/disable/manual override/simultaneous availability one-to-one invariants.
- Phone-to-desk `0.50`→normal once and reverse safely.
- Run 100 register/reserve/use/sell cycles and 30-worker desk churn: zero orphan station/zone/collider, overcapacity, duplicate charge, lost worker, invalid path/assignment/divergence.

### Evidence and handoff

- Capture five items, each amenity in use, influence/route preview, auto claim, output change, manual reassign, desk sale fallback.
- Agent check: hire deskless, place amenities, observe autonomy, place desk/auto-claim, override, sell/fallback, finish incident day.
- Feature commit: `feat: add functional office amenities`
- Package: `outputs/Playtests/EndlessOfficeAlpha/09_FunctionalAmenities/`
- Update amenity/desk rules, controls, tuning, tests, issues, decision log, run state.

## Checkpoint 10 — Expansion, Unlocks, and Endless Progression

### Objective

Integrate the neighboring-unit purchase with contracts, reputation, build mode, navigation, incidents, and employees. After physical expansion, generated contracts/reputation/roster/training/furniture continue indefinitely.

### Transaction and transition

- Keep `$1,000` only if economy simulations support 12–18-minute target; tune centrally otherwise.
- Confirmation shows cost/benefits/space; affordability and safe transition revalidate; charge/ownership exact-once by stable unit ID.
- Before transition: cancel uncommitted preview, safely resolve carry, defer scheduling, resolve/pause active incident by one rule, freeze unsafe decisions, prevent report overlap.
- During/after: remove wall/reveal readable lit room, then extend bounds; rebuild obstacles/routes/stations/furniture ownership/camera/incident eligibility.
- Revalidate every path/reservation. Preserve needs, traits, skill/training, desks/phone, cash/reputation, contract, furniture, and random sequence.
- Workers cannot cross before ownership or become trapped; autosave hook waits for completion.

### Tiers/unlocks

- Cramped Startup: starting area/P0/introduction.
- Established Office: reputation and/or expansion, broader variants/options.
- Chaotic Corporate Floor: higher reputation/roster after expansion, harder generated contracts, bounded incident breadth, optimization/prestige.
- Unit and reputation cannot circularly lock each other. Messages queue; locked catalog entries explain conditions.
- No second physical unit is required. Owning the neighbor never exhausts goals.

### Endless scaling

- Contract requirements/rewards use current capacity, desk/phone mix, reputation, recent outcomes—not day number alone.
- Costs use actual roster/furniture; incidents retain fairness; hiring remains uncapped/deterministic.
- Training/skill uses soft caps; gentle diminishing returns/expenses may prevent runaway wealth without hidden hard caps.
- Recovery option always remains under negative cash.
- Report includes furniture, expansion, desks/amenities, tier/unlocks, roster desk/phone counts, reputation, next goal.

### Tests and matrix

- Affordability/confirmation/exact charge/duplicate input/unsafe-state/animation/idempotent ownership.
- Locked/unlocked bounds, build area, camera, wall, lighting, route/station/incident indexes.
- Preserve all runtime systems under transition policy; build only inside owned space.
- Deterministic non-circular tier/unlocks; 20 meaningful generated post-expansion days.
- Public automated flow: earn, deskless hire, build, purchase, furnish, day/incident/next day.
- Zero lost worker/furniture/desk, invalid unit, early locked entry, orphan route/station, duplicate charge, stalled goal, debt trap, or divergence.

### Evidence and handoff

- Capture expansion promise/confirm/wall opening/light/routes/new-unit furniture/two-unit work/tier/report/post-expansion day.
- Agent check: fresh campaign to hire, furniture, purchase, furnishing, incident, report, two post-expansion days.
- Feature commit: `feat: integrate office expansion progression`
- Package: `outputs/Playtests/EndlessOfficeAlpha/10_ExpansionProgression/`
- Update unit/progression/tuning/tests/issues/decision log/roadmap/run state.

## Checkpoint 11 — Versioned Campaign Persistence and Continue

### Objective

Save, quit, relaunch, and continue an equivalent deterministic campaign. Ship three rotating autosaves, manual slot if safe, corrupt-file fallback, explicit migration, and non-destructive menu behavior.

### Schema and identity

Start schema at version `2`. Save explicit small data:

- Schema/build, campaign ID/seed/creation, deterministic random states/counters.
- Day/workday/time/safe pause state; cash ledger summary/transaction IDs; reputation/tier; contracts.
- Employees: stable IDs, definition/name, qualifications, five needs/Stress, skill/XP, salary/hire order, desk/phone, safe activity/cooldowns.
- Owned units/expansion; furniture stable IDs/definitions/unit/transforms/paid price/state.
- Stable station/workstation assignments; incident schedule/history and supported active incident.
- Tutorial/accessibility/gameplay settings; autosave sequence.

Never serialize scene/instance references, delegates, cached paths, UI/effect/material objects, or rebuildable indexes. Generated stable IDs use campaign counters, not replay-sensitive GUIDs. Validate uniqueness/references.

### Safe boundaries and incident policy

- Autosave after safe day start/end, expansion, hire/fire, and changed build-mode exit.
- Do not save during carry/lowering, preview/transaction, expansion animation, modal transition, partial ledger transaction, partial incident interaction/cleanup, teardown.
- Queue/coalesce to next safe boundary.
- Prefer serializing active incident definition/variant/timers/response/affected stable IDs/owned modifiers, restoring after graph ready. If unreliable, defer save but still test the active boundary. Never serialize temporary objects.

### Atomic rotation/security

- Write same-directory temp, close/flush, validate schema/size/checksum, atomically replace while preserving prior valid file.
- Three monotonic autosaves; manual P1 retained only after rotation gate.
- Bound file size/depth; reject malformed/future schema without modifying it; never deserialize arbitrary runtime types.
- Continue chooses newest valid, not newest timestamp. Corrupt newest falls back visibly to prior.
- New Campaign warns and does not delete the only valid old campaign until its first valid save succeeds.

### Restore order

1. Validate file/schema/checksum/IDs/ranges/definitions.
2. Choose valid rotation.
3. Load geometry/owned units.
4. Create starting/purchased furniture.
5. Register routes/stations/workstations.
6. Create employees/runtime.
7. Restore desk/phone/station relations.
8. Restore economy/reputation/contract/day.
9. Restore incident schedule/active instance.
10. Restore tutorial/settings.
11. Rebuild indexes and validate graph.
12. Enable input/simulation only after validation.

Failed graph load never enters a partial office; try prior autosave.

### Menu and migration

- Main Menu: Continue, New Campaign, Load Autosave; Continue explains disabled state.
- Autosave list shows day/cash/team/tier/time. Manual Save/Load in Pause only if shipped safely.
- Registry supports explicit `vN→vN+1`; synthetic legacy fixtures reject/migrate safely; future files remain untouched.

### Tests and matrix

- Round-trip all fields/IDs/transforms/random/assignments/cooldowns/contracts/ledger/tutorial/settings.
- Equivalence at New, Preparing, mid-day/paused, active incident, post hire/fire/train/build/sale/expansion, report, negative cash.
- Loaded next decisions/output equal uninterrupted control.
- Unsafe request queues once; rotation/atomic/temp failure/truncation/checksum/schema/missing definition/duplicate IDs/bad numbers/fallback.
- Restore produces no missing reference; repeated load duplicates nothing.
- At least 100 save/load cycles and 20 deterministic continuation comparisons.
- Zero invalid save accepted, prior valid lost, divergence, orphan object, duplicate transaction, partial office entry.

### Evidence and handoff

- Capture menu states, save list, mid-day Continue, expanded/furnished restore, active incident restore, corruption fallback.
- Agent check: build/hire/day/quit/Continue/expand/incident/quit/Continue, corrupt copied newest, verify fallback, preserve prior campaign.
- Feature commit: `feat: add versioned campaign persistence`
- Package: `outputs/Playtests/EndlessOfficeAlpha/11_CampaignPersistence/`
- Update schema/migration/file/cloud-readiness docs, tests, issues, decision log, run state.

## Checkpoint 12 — Cohesive UI, Onboarding, Art, Audio, and Accessibility

### Objective

Remove prototype seams so a first-time player can understand the Alpha loop without coaching. Consolidate UI ownership, rewrite onboarding, enforce the art bible, improve employee/incident/furniture feedback, add controlled audio, and validate four resolutions/accessibility modes.

### Unified UI

One modal owner coordinates Tutorial/Help/Pause, Inspector, Hiring, Contracts, Incidents, Build/Sale, Expansion, Report, Save/Load feedback, Settings.

- Exactly one blocking modal owns input. Nonblocking surfaces coexist only by documented layout.
- Modal transitions restore speed; incompatible surfaces close/queue/reject safely.
- No click leaks into carry/build/incident/world selection; menu/report/save transitions do not strand focus.
- Compact HUD: Day/time, Cash/income, Reputation/tier, Contract, Team/Desks, speed, compact incident.
- Inspector: activity/destination, two qualifications, skill/train, productivity, five needs, influences, command reason, desk/phone, Follow/Reassign/Train/Fire.

### Onboarding

Teach through observed real actions:

1. Select.
2. Drop anywhere valid.
3. Read five needs/directions.
4. Intervene near activity.
5. Understand qualifications/phone work.
6. Choose contract/time.
7. Respond to incident.
8. Hire.
9. Build/place furniture.
10. Expand.
11. Report/Next Day.
12. Quit/Continue.

- Closing text never fakes completion.
- Skip/replay changes no money/time/random/contract/save result.
- Contextual lessons defer until legitimate events; prompts stay short and do not cover targets.
- Help retains concise controls/needs/loop reference.

### Resolution/input matrix

Validate 1280x720, 1920x1080, 2560x1440, and 3840x2160:

- No overlap/clipping/off-screen controls/unreadable tooltip/hidden timer/inaccessible scroll.
- Carry/world selection/build ghost/cursor and incident target remain aligned; UI regions block world input.
- Font/UI scaling is crisp and screenshots prove every major surface at min/max.

### Art implementation

Enforce the art bible palette: carpet `#53616D`, partitions `#B8B5AA`, off-white `#D8D2BF`, desks `#343638`, shadows `#171C20`, controlled burgundy/teal/mustard/cyan/amber. Use cool fluorescent ambient plus dramatic amber/cyan landmarks, dense cubicle/desk clutter, darker routes, lit smoking alcove, focused water-cooler light.

Finish visuals for five P0 items, six incidents, expansion, contract/report milestones, emergency light, needs/training. Clutter is restrained/static/batched and never breaks navigation/placement.

### Animation/audio

- Refine walking/turns, typing, phone work, rest/eat/drink/coffee/restroom, social, training, trait idles, urgency, incidents, carry/drop. Props clean under speed/pause/restart.
- Add controlled ambience, typing/phone/footsteps, activity/build/day/incident/expansion/UI cues with provenance.
- Master/Music/Ambience/SFX/UI volumes work independently; mute never removes sole cue; 30-worker sounds pool/rate-limit/prioritize.

### Accessibility/settings

Implement/persist Font/UI Scale, independent volumes, Reduced Motion, Extended Incident Timers, Choice-Only Events, color-independent cues, window/fullscreen/resolution, scalable shadows/lights/effects/crowd animation, QTE hold/timing tolerance. Remapping is P1: ship safely if architecture supports it, otherwise honest fixed controls. Do not claim controller support.

### Tests and validation

- Modal transition-pair/input/speed matrix; tutorial observation/skip/replay neutrality/save restore.
- All major UI at four resolutions; pointer/world alignment and suppression.
- Important states readable muted, color-independent, Reduced Motion; incidents distinguishable without title; needs without color.
- Audio mixer/pooling; animation/prop cleanup under pause/4x/restart/fire/incident/report/load.
- Profile lights/particles/clutter; preserve final performance route.
- Public onboarding agent completes New Campaign through Continue without dev controls/coaching.

### Evidence and handoff

- Capture four-resolution matrix, panels, onboarding, items/incidents, employee states, expansion, report, settings, Continue.
- Agent check: use safe temporary save directory, launch first-time flow for two days/incident/build/expansion where feasible/quit/Continue; repeat resolution/accessibility checks.
- Feature commit: `feat: polish endless office alpha presentation`
- Package: `outputs/Playtests/EndlessOfficeAlpha/12_CohesionPolish/`
- Update art/audio provenance, controls, accessibility, tutorial, UI matrix, tests, issues, decision log, run state.

## Checkpoint 13 — Endless Office Alpha Candidate

### Objective

Freeze features, balance and optimize the complete endless loop, run release-grade automation/soaks/save/clean-install validation, fix all P0/P1 loop blockers, and produce the final trustworthy local Alpha package. Add no new major system.

### Feature freeze and triage

- Inventory every requirement and ledger box from Checkpoints 00–12.
- Classify unresolved issues P0 crash/data loss/permanent invalid state; P1 comprehension/loop/performance blocker; P2 polish; P3 backlog.
- Fix all P0 and P1 required for acceptance. Defer new systems/content.
- Cut in order if necessary: P1 furniture, second variants while retaining six families, training UI while retaining passive growth, manual slot while retaining three autosaves. Never cut persistence, full day loop, autonomous needs, cleanup, five P0 furniture, or recovery economy.
- Lock tuning only after failed acceptance metrics justify changes.

### Thirty-day balance matrix

Run deterministic 30-day scenarios at relevant 3/10/30-worker milestones:

- Active management
- Passive management
- Deliberately poor management
- Overcrowded/phone-heavy
- Low-cash/recovery
- High-incident
- Safe-only/Standard-only/Ambitious
- Build-first/hire-first/expand-first where viable
- Post-expansion endless play

Record by day/seed: cash/reputation/tier, contracts, payroll/costs, hires/fires, desks/phone, training/skill, need time urgent/critical, interventions, incidents/outcomes, furniture, expansion, work output, active/passive delta, dead time, recovery duration.

Acceptance targets:

- First decision under 90 seconds.
- First affordable hire 3–5 minutes.
- Expansion normally 12–18 minutes if hiring first.
- Incident every 4–6 minutes after grace; never under 90 seconds apart.
- Active output 15–30% over passive.
- No more than 45 seconds without visible choice/progress/behavior.
- At least 70% “one more day” intent is a later human metric; do not fabricate it.
- Zero permanent economy dead end or deterministic divergence.

### Performance and soak

- Profile 3, 10, 30 employees with late-Alpha furniture/incidents at 1920x1080 on reference PC.
- Target 60 fps for normal 30-employee late office with scalable shadows/lights/effects/crowd.
- Measure CPU/frame, GC allocations after warmup, memory trend, path/need/incident/UI/build hotspots, draw calls/lights/particles/audio voices.
- Batch/throttle/index/LOD only where evidence identifies cost.
- Run accelerated and real-time soaks including two-hour unattended final soak.
- Include 3, 30, and 100 employee stability scenarios. At 100, stable simulation/no unbounded allocation is mandatory even if visual frame target is reduced.
- Require no stuck worker, orphan reservation/modifier, lost furniture/desk, invalid save, unbounded allocation, modal deadlock, or crash.

### Save, hardware, and clean-install gates

- Run 100 save/load cycles across milestones, corruption fallback, future-schema rejection, deterministic continuation, active incident, expansion/furniture.
- Verify fresh Windows user-style save directory, New/Continue/Load, quit/relaunch, menu return, clean quit.
- Extract final ZIP to a clean path and run only that copy; no editor/dev assets or existing cache dependency.
- Test 1280x720, 1080p, 1440p, 4K; window/fullscreen; graphics tiers; mute/volume; accessibility combinations.
- Record reference hardware/OS/driver/build configuration and any minimum-spec risk without inventing untested claims.

### Agent playthrough and human kit

The agent performs a clean-install public build playthrough covering:

- New Campaign, onboarding, contract, autonomous/player recovery, incident success/failure, hire deskless, phone work, build five items, auto desk claim/sale fallback, payroll/reputation/recovery, expansion/furnish, report/next days, save/quit/Continue, post-expansion endless day.

Create a one-page human guide and raw feedback form for at least five people who did not watch development. Ask comprehension, decision timing, personality recognition, incident enjoyment, pacing, one-more-day intent, defects. Do not claim those sessions occurred unless real results are supplied. The local Alpha may be `READY_FOR_HUMAN_ALPHA_REVIEW`; commercial readiness remains blocked until genuine external results meet the roadmap.

### Final package/evidence

Create `outputs/Playtests/EndlessOfficeAlpha/13_EndlessOfficeAlphaCandidate/` containing:

- Non-development Windows build
- Fresh final ZIP
- Preserved exact extraction
- JSON/Markdown manifest and SHA-256
- Complete EditMode/PlayMode XML/logs
- Thirty-day balance report/raw data
- Performance report
- 3/30/100-worker soak reports
- Two-hour soak report/log
- Save/corruption/migration report
- Four-resolution/input/accessibility matrix
- Clean-install public-flow report
- Known issues and release-readiness score
- Human playtest guide/form
- Complete screenshot set and short raw gameplay capture where pipeline supports it
- Updated build/run, controls, tuning, simulation, save schema, test report, run state, roadmap, decision log, known issues, README

The manifest records branch/commit/Unity/configuration/hash/bytes/timestamps/test counts/seed matrix/hardware/evidence paths/limitations. Never overwrite Checkpoints 00–12.

### Final acceptance

- All mandatory ledger boxes through 13 pass.
- All automated tests green; zero known crash/data loss/permanent stuck/permanent incident/invalid save.
- Complete endless loop works from clean extracted build and survives quit/Continue.
- P0 five furniture, six incidents, five needs, two traits, contracts/economy, expansion, and persistence are integrated.
- Docs/store-neutral claims match actual Alpha.
- Feature commit: `release: build endless office alpha candidate`
- Package: `outputs/Playtests/EndlessOfficeAlpha/13_EndlessOfficeAlphaCandidate/`
- Optional local annotated tag: `endless-office-alpha-candidate` only after final package passes; do not push it.
- Set loop state to `COMPLETE` and final status `READY_FOR_HUMAN_ALPHA_REVIEW` when automated gates pass. Set `BLOCKED` with evidence if a hard gate cannot pass.

## Required package contract for every checkpoint

Each package must contain, directly or in clearly named subdirectories:

- The Windows player and all required runtime files
- One fresh ZIP built from that player
- The exact preserved extraction that was launched
- `CHECKPOINT_MANIFEST.json` and a readable Markdown summary
- EditMode and PlayMode XML/logs
- Checkpoint-specific simulation/lifecycle report and raw data where practical
- Public-flow smoke log
- Genuine gameplay screenshots at required resolutions
- Playtest guide
- Raw-results/defect template
- Known issues/limitations
- Build log and clean-quit evidence

The manifest must include:

- Product/checkpoint ID
- Branch and feature source commit
- Evidence-recording commit when separate
- Unity version and scripting/build configuration
- Build timestamp and byte size
- ZIP filename and SHA-256
- EditMode/PlayMode passed/failed/skipped/total/duration
- Simulation/soak seed counts and pass status
- Exact verification executable/path
- Public-flow start/end markers
- Screenshot names/dimensions
- Known limitations
- Source documents updated
- Whether human feedback exists, pending, or not applicable

Minimum test counts always equal the highest previously accepted counts plus the new checkpoint tests. A packaging script must not permit a lower count merely because an older prompt named a smaller baseline.

## Playtest guide contract

Every checkpoint guide must instruct the operator to launch the exact preserved extraction, use no Editor/developer controls, record resolution/speed/seed/save, follow the checkpoint’s agent-check sequence, return to menu, and quit cleanly. It must separate observed facts from opinions and include a P0–P3 defect form with reproduction, expected/actual, media, seed/save, resolution, and speed.

At minimum ask:

- 01: Which meters are high-good versus urgent? Did any need feel too fast/irrelevant? Was Restroom obvious?
- 02: Did workers recover without help, explain decisions, navigate correctly, and leave intervention useful rather than mandatory?
- 03: Could the player distinguish employees, explain both qualifications, and understand skill/training tradeoffs?
- 04: Were contract risks/progress/report clear, and did the operator voluntarily start another day?
- 05: Could the player reconcile cash, understand payroll/reputation/recovery, and name what they were saving for?
- 06: Were incident consequence/time/choices fair, pause-safe, and fully restored afterward?
- 07: Were all families distinct, enjoyable on repetition, accessible, and temporarily costly rather than destructive?
- 08: Could the player build/rotate/reject/cancel/sell without accidental spending or blocking routes?
- 09: Did amenities change behavior and did desk claiming/sale fallback make sense?
- 10: Did expansion feel earned, preserve the office, and reveal an obvious next goal?
- 11: Did Continue restore the expected campaign, and was corrupt-save fallback understandable?
- 12: Could a first-time player finish the loop without coaching at every target resolution/accessibility mode?
- 13: Was there any crash/data loss/stuck state, how long until key milestones, and was “one more day” appealing?

The autonomous loop records an agent-operated observation section after each package. Human questions remain unanswered until a real person responds; pending answers do not stop implementation but are required before claiming the corresponding human metric.

## Checkpoint run-log entry template

Append a short entry beneath the active ledger item or in its package summary:

- Started UTC/local:
- Starting commit/status:
- Prior evidence verified:
- Player-visible deliverable:
- Core architecture/data changes:
- Tests added:
- Full suite result:
- Matrix/soak result:
- First failure and repair summary:
- Feature commit:
- Package/hash:
- Exact extraction smoke:
- Agent gameplay observations:
- Human feedback source/status:
- Known P2/P3 issues:
- Completed UTC/local:
- Next checkpoint:

## Severity and stop rules

- P0: crash, data loss, corrupt valid save, permanent stuck/hidden worker, permanent incident/modifier, impossible next day, duplicated/destructive transaction, locked-area escape, nondeterministic replay, package cannot launch. Never advance.
- P1: player cannot understand or complete the checkpoint loop; severe performance at target; inaccessible primary control; economy/need tuning makes required action practically impossible. Never advance to final candidate; repair before leaving the owning checkpoint when feasible.
- P2: noticeable polish/clarity issue with a reliable workaround. Record and continue if outside current scope.
- P3: enhancement/content request. Backlog; do not expand scope.

When a user message arrives during the loop:

- Treat explicit override/cancel as authoritative and stop safely after recording state.
- Treat playtest feedback as an addition: classify, repair owning/current checkpoint if P0/P1, update evidence, then resume.
- Treat a status question as a request for a concise update; then continue unless asked to stop.

## Final completion report

When the loop finishes, report:

- Final status and what the Alpha now lets the player do
- Checkpoint ledger summary with commits/packages
- Final test totals
- Balance/performance/soak/save results
- Final ZIP path/hash and exact verification path
- Known issues by severity
- Which human gates remain genuinely pending
- Clear next recommended milestone

Do not call the game commercially finished. This runbook produces the month’s Endless Office Alpha, not the full 10–14 month Steam roadmap.

## Launch prompt

Copy the text below into a Codex task to start or resume the loop:

> Work autonomously through `C:\Users\danny\Documents\GitHub\Silly Office Sim\Docs\Prompts\EndlessOfficeAlpha\AUTONOMOUS_IMPLEMENTATION_LOOP.md`. Read the entire document before acting. Create a persistent goal for completing every mandatory checkpoint in that runbook. Audit the repository and evidence, preserve all current work, resume the earliest evidence-unverified checkbox, and follow the document’s implementation, repair, testing, packaging, exact-extraction, agent-playtest, documentation, commit, and ledger-update loop. Do not wait for me to say “next” between checkpoints. After a checkpoint passes its mandatory automated gate, create its playable package, update the checkoffs with real evidence, and immediately continue to the next checkpoint. Never fabricate human playtest results; record them as pending and keep implementing. Stop only if I explicitly stop you, all mandatory Checkpoints 01–13 are complete, or a genuine hard external blocker satisfies the runbook’s stop rules. Do not push, publish, open a pull request, contact people, spend money, or change external services. At completion, provide the final Alpha package, evidence summary, remaining genuine human gates, and mark the persistent goal complete only if every mandatory runbook deliverable actually passes.
