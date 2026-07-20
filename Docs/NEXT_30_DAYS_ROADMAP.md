# SILLY OFFICE SIM - NEXT 30 DAYS

- Period: July 20-August 19, 2026
- Primary production window: four five-day sprints, July 20-August 14
- Stabilization buffer: August 15-19
Target: Endless Office Alpha

## Month-end goal

Deliver a saved, replayable Windows alpha in which a player can start with three employees and $0, choose and complete repeated workdays, respond to employee needs and workplace incidents, hire indefinitely, place essential furniture, buy the existing neighboring unit, and continue playing after expansion without running out of goals.

The alpha is successful when the player repeatedly experiences this loop without developer explanation:

1. Observe employees, needs, traits, contract progress, and office problems.
2. Decide whether to intervene, spend money, or trust employee autonomy.
3. Earn money and reputation from productive work and contract completion.
4. Pay payroll and operating costs at the end of the day.
5. Hire distinctive employees, including deskless phone workers.
6. Buy and place desks or amenities, then purchase the neighboring unit.
7. Start another increasingly demanding day with a larger, stranger office.

This is an alpha of the finished game's endless loop, not a Steam release candidate. Steam integration, six to eight office units, full controller support, localization, achievements, the complete twelve-incident set, and final marketing assets remain later milestones.

## Success measures

### Player experience

- A first-time player makes a meaningful management decision within 90 seconds.
- The first affordable hire appears in 3-5 minutes; the first expansion normally occurs in 12-18 minutes if the player hires first.
- At least one workplace incident occurs every 4-6 minutes after the opening grace period, with no incidents less than 90 seconds apart.
- Active management improves completed work by 15-30% over passive play without requiring constant dragging.
- In a 30-minute test, at least 80% of players can explain why two employees behave differently.
- At least 70% of testers voluntarily begin one more workday after their first end-of-day report.
- No tester experiences more than 45 seconds with no visible choice, progress, or character behavior.

### Technical quality

- All existing tests remain green; every new system ships with EditMode and PlayMode coverage.
- A campaign can save, quit, continue, and produce the same roster, cash, furniture, office ownership, needs, contract, reputation, and random sequence.
- Ten deterministic simulations complete 30 workdays at 3, 10, and 30 workers without a stuck worker, permanent incident, negative reservation count, or unrecoverable economy.
- The normal late-alpha office maintains 60 fps at 1920x1080 with 30 workers on the existing reference PC.
- After warmup, no new system performs avoidable per-frame allocations.
- The Windows build completes a two-hour unattended real-time soak and returns to the menu cleanly.

## Team operating model

The schedule assumes four active development lanes. With three developers, combine Lanes C and D and move P1 items below the cut line. Unlimited AI tools accelerate implementation, test generation, content variants, analysis, and documentation; a human owner still reviews every merge and owns every product decision.

| Lane | Primary ownership | Secondary ownership |
|---|---|---|
| A - People Simulation | Five needs, autonomy, qualifications, navigation behavior, phone workers | Simulation tests and tuning scenarios |
| B - Economy and Persistence | Workdays, contracts, payroll, reputation, purchases, save/load | Balance simulator and migration tests |
| C - Incidents and UI | Incident director, response interactions, HUD, employee inspector, reports | Tutorial changes and accessibility |
| D - Build, Art, Audio, QA | Furniture mode, incident visuals, office density, sound, build pipeline | Performance, captures, external playtests |

Every feature has one directly responsible owner. Cross-lane interfaces are agreed in code and data definitions before parallel implementation begins. No lane may silently create a second cash model, random service, modal stack, worker state machine, or save format.

## Locked alpha design

### Workday and contracts

- A standard workday lasts 10 simulation minutes at 1x and supports pause, 1x, 2x, and 4x.
- At the start of each day, offer three deterministic contract cards: Safe, Standard, and Ambitious.
- A contract defines required work, completion reward, reputation change, optional trait preference, and day deadline.
- Desk and phone work contribute to both continuous cash income and the selected contract.
- At day end, stop simulation, resolve contract results, charge payroll and operating costs, show the report, autosave, and offer Start Next Day.
- Missing a contract reduces reputation and forfeits its bonus but never deletes the campaign.
- Negative cash disables new hiring and purchases but not work. An always-available recovery contract prevents a permanent debt spiral.
- Contract work requirements and rewards scale from current productive capacity and reputation, not from elapsed calendar days alone.
- Endless progression continues through generated contracts, reputation levels, roster growth, furniture optimization, and incidents after the current physical expansion is exhausted.

### Employee needs

Five needs are live and visible:

| Need | Meaning | Autonomous response | Player acceleration |
|---|---|---|---|
| Happiness | High is good; affects persistence and social behavior | Break, conversation, or preferred amenity | Place at break area or compatible coworker |
| Hunger | Urgency; high is bad | Vending/food station when urgent | Place at vending machine |
| Bathroom | Urgency; high is bad | Restroom trip when urgent | Place at restroom entrance |
| Inspiration | High is good; affects work quality and distraction resistance | Break, coffee, social interaction, or inspiring decor | Place at preferred amenity |
| Energy | High is good; affects work speed | Coffee, break, or leave office | Place at coffee/rest/exit |

- Happiness, Inspiration, and Energy use full-as-good presentation; Hunger and Bathroom use filling red urgency presentation.
- Need changes use simulation time and pause correctly.
- Critical Hunger or Bathroom overrides normal work decisions after a short bounded delay.
- Autonomy always attempts to resolve a reachable critical need without player input.
- Player placement provides immediate routing and a short efficiency benefit but does not create exclusive content.
- Existing Mood data migrates to Happiness. Existing Stress becomes a temporary status influence and is not displayed as a sixth need.
- Needs are tuned so a typical employee requires attention or autonomous recovery one to three times per workday, not all at once.

### Qualifications and growth

- Every employee has exactly one strength and one liability from the existing 12+12 catalogs.
- Morgan, Alex, and Sam receive curated pairs; hired candidates roll deterministic pairs from the campaign random stream.
- Qualification effects apply contextually to productivity, need decay, walking speed, preferred activities, distraction likelihood, social choices, incident susceptibility, or response outcomes.
- No qualification may be a disguised universal win or a run-ending penalty.
- Employee cards display the qualification name, one-sentence effect, and the current behavior it is influencing.
- Skill grows slowly from completed productive work. Training consumes cash and work time to accelerate growth, with a soft cap for this alpha.
- Training never removes or rerolls qualifications.

### Incidents

The alpha ships six complete incident families with two text/visual variants each:

| Incident | Office effect | Default responses | Interaction |
|---|---|---|---|
| Printer jam | Printer-area productivity blocked | Clear carefully; hit it; call service | Three-step click sequence |
| Internet outage | Desk and phone productivity reduced | Restart router; call provider; use offline work | Timed choice card |
| Power failure | All powered work paused | Generator; send people on break; wait | Timed choice card |
| Coffee spill | One workstation disabled | Save equipment; clean first; move employee | Timed choice plus worker placement |
| Water leak | Water area disabled and nearby route penalized | Shut valve; mop; call maintenance | Short click/hold interaction |
| Pigeon/raccoon intrusion | Distractions spread through nearby workers | Lure outside; assign employee; ignore | Moving target click interaction |

- The incident director uses the seeded random service and data-driven definitions.
- First incident cannot begin during the opening three minutes or tutorial modal.
- Only one major incident is active at a time.
- Base cooldown is randomized from 180-300 simulation seconds and resets after resolution.
- Frequency weights consider owned amenities, office tier, active workers, and Clumsy-style traits.
- Success shortens or avoids the penalty. Failure applies a temporary 45-120 second setback.
- Every incident restores affected stations, routes, lighting, and modifiers when resolved or timed out.
- Incident timers pause while the game is paused or while an instructional modal owns the screen.
- Accessibility settings provide Extended Timers and Choice-Only Events; neither changes economic rewards.

### Furniture and office growth

The month ships a minimum build mode, not a complete construction game.

- Build categories: Work, Needs, and Decor.
- P0 placeable items: workstation bundle, break chair, water cooler, vending machine, and plant.
- P1 items: coffee machine, filing cabinet, trash/recycling bin, desk lamp, and cubicle divider.
- Workstation bundles dynamically create a workstation and Work influence zone.
- Amenities dynamically create their corresponding need station and influence zone.
- Placement validates unlocked bounds, wall/furniture collision, entrance clearance, and primary walking routes.
- Controls: left click place, `Q/E` rotate 90 degrees, right click or Escape cancel, Delete sell selected item.
- Selling returns 50% of purchase price. Starting fixtures cannot be sold during this alpha.
- Purchases are committed only after valid placement; canceling never spends cash.
- Phone workers automatically claim newly available desks by hire order unless the player manually assigns one.
- The existing neighboring unit remains the only required physical property purchase this month.
- A second adjacent unit is a stretch goal only after save/load, incidents, and the core loop pass their gates.

### UI and persistence

- Compact HUD fields: Day, Time, Cash, Income/min, Reputation, Contract, Team, Desks, and speed.
- Employee inspector: name, current activity, two qualifications, skill, productivity, five needs, positive influence, negative influence, Follow, Reassign, Train, and Fire.
- Incident panel: title, consequence, remaining time, response controls, and accessibility-safe status cues.
- End-of-day report: contract outcome, gross work income, contract bonus, payroll, operating costs, net change, reputation, best employee, notable incident, and next-day button.
- Main menu adds Continue, New Campaign, and Load Autosave; Continue is disabled when no valid save exists.
- Save format version starts at `2` because the previous session-only build has no compatible campaign save.
- Save slots: three rotating autosaves plus one manual slot.
- Autosave at day start, day end, expansion purchase, hire/fire completion, and build-mode exit after a change.
- Save writes use a temporary file and atomic replace; a corrupt newest autosave falls back to the prior rotation.
- Save data includes campaign seed, random progression state, day/time, stage, cash, reputation, active contract, roster and qualifications, needs, desk assignments, owned unit, furniture transforms, incidents, tutorial state, and settings.
- No save occurs during an unresolved frame of worker carry, furniture placement, expansion animation, or modal transition.

## Four-week delivery schedule

## Week 1 - Employees become understandable systems

Goal: five needs and paired qualifications drive autonomous, readable behavior in the existing starter office.

Checkpoint status on 2026-07-20: Day 2's live five-need model, migrated activities, five-row inspector, restroom, and deterministic matrix ship in `01_FiveNeeds`. Day 3 comprehensive critical-need autonomy and every later item below remain future work; this roadmap is not complete.

### Day 1 - Integration contract and instrumentation

- Freeze the public alpha interfaces for need values, qualification pairs, worker placement results, incident state, contract state, and campaign snapshots.
- Add a feature-version constant and record deterministic campaign seed/state separately from individual worker randomness.
- Add structured debug counters for autonomy decisions, need interventions, work output, phone output, distractions, and stuck recovery.
- Create the 3/10/30-worker deterministic simulation fixtures used through the month.
- Establish the shared definition-of-done checklist and daily integration branch.
- Gate: clean compile, existing 109 tests pass, and a baseline 30-minute balance report is archived.

### Day 2 - Need model and migration

- Convert runtime worker state to the five live needs while retaining adapters for existing Energy/Mood/Stress callers.
- Implement clamped need decay/recovery functions and simulation-speed-safe ticking.
- Map Rest, Water, Vending, Coffee, Smoking, and Leave Office to the new need effects.
- Add restroom station and minimal restroom placement zone to the starter office.
- Add EditMode tests for every decay, recovery, clamp, pause, and migration rule.
- Gate: 100 deterministic simulated minutes produce no invalid values and no synchronized all-needs collapse.

### Day 3 - Need autonomy and path decisions

- Add bounded critical-need evaluation before normal distraction/work decisions.
- Define destination selection, reservation, capacity, retry, timeout, and fallback behavior for each need.
- Ensure a deskless phone worker also pauses work to satisfy critical needs.
- Preserve player command authority until its bounded benefit expires; critical bathroom urgency may override with explicit UI feedback.
- Add PlayMode coverage for autonomous food, restroom, inspiration, energy, and happiness recovery.
- Gate: every employee recovers each critical need without player input in a 20-seed scenario.

### Day 4 - Qualifications and inspector

- Store one strength and one liability on every worker definition and campaign snapshot.
- Assign curated pairs to the starting team and deterministic random pairs to hires.
- Wire all 24 definitions into at least one contextual calculation or behavior preference.
- Replace the legacy single Personality field in the inspector with two qualification cards and five need rows.
- Add tooltips that state the exact current effect without exposing unnecessary formulas.
- Gate: seeded hiring reproduces identical pairs; the inspector remains readable at 1280x720.

### Day 5 - Employee-system playtest gate

- Run internal 30-minute sessions with active, passive, and intentionally poor management.
- Measure intervention advantage, need frequency, phone output, autonomous recovery, and personality recognition.
- Fix only P0 comprehension, stuck-state, and runaway-balance defects.
- Capture a new starter-office overview, three employee inspectors, and one autonomous need recovery.
- Gate: three team members can identify Morgan, Alex, and Sam from behavior/stat cards with names hidden.

## Week 2 - Every workday creates decisions

Goal: repeated contracts, payroll, reports, and six incident families create a complete ten-minute day.

### Day 6 - Workday and contract state machine

- Convert the starter stage from untimed sandbox to explicit Preparing, Running, Reporting, and Transitioning states.
- Generate three seeded contract options using current capacity and reputation.
- Connect desk and phone contributions to active contract progress.
- Keep continuous cash income while adding completion bonuses.
- Add deterministic contract generation and day-transition tests.
- Gate: 20 automated days select, run, resolve, and advance without manual API shortcuts.

### Day 7 - Payroll, costs, reputation, and recovery economy

- Charge employee salaries and simple amenity operating costs at day end.
- Implement reputation gain/loss and three alpha reputation tiers.
- Add recovery contract generation for negative-cash campaigns.
- Tune starting cash, hire costs, contract work, bonus, and payroll toward the target progression windows.
- Add economy invariants: no NaN, no accidental double charge, no forced campaign termination, and no unavailable recovery path.
- Gate: active, passive, and poor scenarios remain distinguishable and recoverable across 30 seeded days.

### Day 8 - Incident director and response framework

- Implement the scheduler, eligibility filters, grace period, cooldown, one-active rule, pause/modal behavior, and cleanup contract.
- Add incident runtime state and temporary modifier ownership so cleanup cannot orphan a penalty.
- Build the shared incident panel and timed-choice response path.
- Implement Internet Outage and Power Failure end to end.
- Gate: 100 scheduled incidents obey cooldown/fairness rules and restore all modifiers.

### Day 9 - Tactile incidents and remaining content

- Implement Printer Jam, Coffee Spill, Water Leak, and Animal Intruder.
- Add temporary world props/effects and reusable interaction components.
- Add two variants per incident family and qualification-specific reaction lines.
- Add Extended Timers and Choice-Only accessibility modes.
- Gate: every success, failure, timeout, pause, save-block, and cleanup route passes independently.

### Day 10 - Complete-day playtest gate

- Run at least five blind internal workdays and two external sessions.
- Record first decision time, incidents per day, unresolved-event rate, contract clarity, net cash, and one-more-day intent.
- Tune event frequency and contract targets before adding more content.
- Cut any incident that cannot restore state reliably; content count never outranks simulation safety.
- Gate: a player can start, choose a contract, finish a day, understand the report, and voluntarily start the next day.

## Week 3 - Money turns into visible growth

Goal: players can spend earnings on useful furniture, support deskless hires, expand, and safely persist the result.

### Day 11 - Build-mode foundation

- Add build catalog, ghost preview, rotation, cancel, purchase transaction, selection, and sell transaction.
- Reuse office layout bounds, obstacle volumes, route volumes, and placement result messaging.
- Block simulation-affecting commands while build mode owns input; pausing simulation is the default alpha behavior.
- Implement workstation bundle and plant.
- Gate: 100 place/cancel/sell cycles preserve exact cash and leave no orphaned collider or zone.

### Day 12 - Functional amenities and desk claiming

- Add break chair, water cooler, and vending machine placement.
- Register/unregister need stations and influence zones dynamically.
- Implement automatic desk claiming for phone workers and safe reassignment when a desk is sold.
- Add visual route-clearance and collision rejection cues.
- Gate: a deskless hire claims a newly built desk and increases workstation output from 50% to normal without reload.

### Day 13 - Expansion integration and progression unlocks

- Integrate build mode with the neighboring-unit purchase animation and updated walkable bounds.
- Unlock furniture tiers by reputation and expansion state.
- Add purchase/sell/expand entries to the end-of-day report.
- Confirm workers, reservations, incidents, and furniture survive the wall-opening transition.
- Gate: earn, hire, expand, furnish, and continue the next day in one public-API automated flow.

### Day 14 - Save system

- Implement versioned campaign snapshots, atomic writes, three-slot autosave rotation, validation checksum, and corrupt-file fallback.
- Implement Continue/New Campaign/Load Autosave menu behavior.
- Restore furniture before workers, stations before assignments, and simulation only after the complete graph validates.
- Add snapshot equivalence tests at start, mid-day, active incident boundary, post-hire, post-build, and post-expansion.
- Gate: 100 save/load cycles reproduce gameplay state and deterministic next decisions.

### Day 15 - Growth-loop playtest gate

- Run fresh saves until first hire, first placed furniture, first expansion, and day three.
- Compare actual milestone timing to targets and tune costs/rewards rather than scripting outcomes.
- Verify negative-cash recovery and overcrowded phone-worker strategies.
- Capture before/after office images demonstrating visible growth.
- Gate: testers can state what they are saving for and see that purchase change worker behavior or output.

## Week 4 - Make the alpha coherent and replayable

Goal: remove prototype seams, balance the endless loop, and package a trustworthy team/playtest build.

### Day 16 - UI composition and tutorial rewrite

- Consolidate the HUD, employee inspector, contract panel, incident panel, build catalog, and report under one modal owner.
- Rewrite onboarding around Select, Drop Anywhere, Needs, Phone Work, Contract, Incident, Build, Expand, and Next Day.
- Allow tutorial replay and skip without changing campaign results or speed state.
- Validate at 1280x720, 1920x1080, 2560x1440, and 4K.
- Gate: no overlapping panel, clipped control, hidden worker target, or input leak at any supported resolution.

### Day 17 - Art, animation, and audio pass

- Create final alpha visuals for six incidents and five buildable P0 items.
- Add phone-work pose, need urgency emotes, incident reaction, and simple restroom transition.
- Add UI cues, incident stingers, build placement sounds, day start/end cues, and contextual office loops.
- Enforce the new art bible's cool fluorescent base and amber/cyan destination lighting.
- Gate: every important state is readable with sound muted and every incident is distinguishable without reading its title.

### Day 18 - Balance and performance lock

- Run deterministic 30-day scenarios for active, passive, poor, overcrowded, incident-heavy, and recovery strategies.
- Profile 3, 10, and 30 workers with late-alpha furniture and incident effects.
- Batch or throttle need decisions, inspector refresh, incident polling, and visual LOD where profiling identifies cost.
- Lock alpha tuning tables; further changes require a failed acceptance metric.
- Gate: all simulation, economy, performance, and save invariants pass.

### Day 19 - External playtest and triage

- Give a clean Windows package and one-page feedback form to at least five people who have not watched development.
- Observe without coaching for the first 20 minutes.
- Record comprehension, decision timing, personality recognition, incident enjoyment, economy pacing, one-more-day intent, and defects.
- Fix P0 crashes/data loss/stuck states and P1 comprehension/loop blockers. Defer requests for new systems.
- Gate: zero critical defects and at least four of five testers complete a day without help.

### Day 20 - Endless Office Alpha candidate

- Run all automated suites, seeded balance matrices, save migration/fallback tests, two-hour soak, clean install, restart, menu return, and clean quit.
- Build a non-development Windows package and verify the exact extracted copy.
- Capture the complete month-end evidence set and update run-state, known-issues, controls, tuning, and test reports.
- Tag unresolved items P0/P1/P2 and place all out-of-scope requests in the next milestone backlog.
- Gate: approve `Endless Office Alpha` only if every P0 acceptance condition below passes.

## Integration and review cadence

- Daily 15-minute lane sync: yesterday's evidence, today's interface dependency, current blocker.
- Daily integration window: merge only green changes behind complete tests; no long-lived feature branch exceeds two working days without rebasing and smoke verification.
- Wednesday product review: play the latest build for 20 minutes before reviewing task status.
- Friday gate review: accept or reject the week's player-facing outcome using recorded evidence.
- Every pull request includes purpose, player-visible result, data/API change, tests, screenshot or recording when visual, performance note, and rollback note.
- AI-generated implementation receives human review for state ownership, lifecycle cleanup, serialization, input modality, accessibility, and test validity.

## P0 alpha acceptance checklist

- New Campaign starts with Morgan, Alex, Sam, three desks, and $0.
- Workers can be dropped on valid ground or near an activity; invalid locations explain rejection.
- All five needs change, display, trigger autonomous recovery, and respond to player intervention.
- Every worker has one strength and one liability that visibly affect behavior.
- Deskless hires work by phone at approximately 50% workstation efficiency and can later claim a desk.
- The player chooses and resolves repeated ten-minute contracts and receives an accurate end-of-day report.
- Payroll, costs, reputation, negative-cash recovery, and next-day transition cannot end the campaign permanently.
- Six incident families schedule fairly, accept responses, time out, pause correctly, and clean up completely.
- Five P0 furniture items can be bought, placed, rotated, used, and sold inside owned space.
- The neighboring unit can be purchased and furnished without breaking workers, incidents, or saves.
- Continue and autosave recovery reproduce a campaign accurately after quit/relaunch.
- The endless loop continues after expansion with generated contracts and no forced ending.
- 109 existing tests plus all new tests pass; no known crash, data loss, permanent stuck state, or permanent incident remains.

## Cut line and risk controls

If the schedule slips, cut in this order without compromising the endless loop:

1. Cut P1 furniture and retain the five P0 items.
2. Reduce incident variants from two to one while retaining all six functional families.
3. Cut training UI and retain passive skill growth.
4. Cut the manual save slot and retain three rotating autosaves plus Continue.
5. Cut the second-unit stretch goal; never cut persistence, complete day transitions, autonomous needs, or incident cleanup.

Primary risks and controls:

- State-machine collisions: use explicit ownership for carry, build, incident, tutorial, report, and expansion modes; test every transition pair.
- Save complexity: define snapshots on Day 1, implement before final polish, and never serialize scene object references.
- Too much micromanagement: cap need frequency, prioritize autonomy, and measure intervention advantage rather than command count.
- Event annoyance: enforce grace/cooldown/one-active rules and accessibility alternatives.
- Economy dead ends: keep recovery contracts available and test poor-play seeds.
- Feature breadth: protect the P0 cut line; new content cannot enter before the weekly gate passes.
- AI-generated volume: limit work in progress, require one human owner, and prefer smaller verified merges over large parallel rewrites.

## Month-end deliverables

1. `Endless Office Alpha` Windows build and extracted verification copy.
2. Versioned source implementation for needs, qualifications, contracts, incidents, furniture, workdays, and saves.
3. Updated art bible implementation with six incident states and five functional furniture items.
4. Three rotating autosaves, Continue flow, corruption fallback, and snapshot migration tests.
5. Complete automated test results, 30-day seeded balance report, performance report, and two-hour soak evidence.
6. Month-end screenshot/video set showing the starting office, five needs, two qualifications, phone work, an incident, build mode, expansion, end-of-day report, and a grown office.
7. External playtest summary with raw answers, observed behavior, metrics, prioritized defects, and explicit next-month recommendations.
8. Updated build/run guide, simulation rules, tuning table, known issues, and release-readiness score.

The next month begins only after the Endless Office Alpha gate is accepted. Its likely focus is additional office units, the remaining six incident families, deeper contracts and upgrades, animation/audio breadth, accessibility/settings completion, and Steam-facing systems.
