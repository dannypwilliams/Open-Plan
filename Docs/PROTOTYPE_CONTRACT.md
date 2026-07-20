# Prototype Contract

## Current product direction

OPEN PLAN is now a worker-placement-driven office simulation. The normal playable path starts in a cramped Level One office with Morgan, Alex, and Sam. The player observes the team, directs workers toward clear activity areas, earns cash without a countdown, and works toward purchasing the neighboring unit.

The released large office from commit `a638304` remains intact as the Established Office stage. It is a future-stage preview, not the normal starting environment.

## Stage contract

- `StarterOffice`: the default authored stage, with three workers, three occupied desks, one unavailable desk, and a visible locked neighboring unit.
- `StarterOfficeExpanded`: the same business after its first physical expansion, with both units and six available desk locations. Desk count is not an employee cap.
- `EstablishedOffice`: the preserved six-worker, twelve-desk released office and its management systems.

The Main Menu always starts `StarterOffice`. Explicit developer and automation launches can select a stage with `-openplan-stage <StarterOffice|StarterOfficeExpanded|EstablishedOffice>`. Existing capture, video, performance, and package-verification arguments default to `EstablishedOffice` when no stage override is supplied so prior release evidence remains reproducible.

## Tutorial and presentation contract

- A fresh Starter Office session opens a dismissible seven-step tutorial: Meet the Team, Pick Them Up, Put Them to Work, Manage Their Needs, Redirect a Distraction, Try the Office, and Expand.
- Selection, carry, placement, income, recovery, and redirect steps advance from observed gameplay events. Useful actions performed early are credited, and an invalid highlighted worker is replaced safely.
- Reading panels pause simulation and restore the exact prior speed. Skip is available from every tutorial surface; Help shows complete controls and can replay the tutorial. Restart creates fresh per-session state with no persistence requirement.
- Tutorial cards choose a clear screen quadrant away from both highlighted workers and zones. Only one tutorial, inspector, hiring, confirmation, purchase, Help, or expansion milestone surface owns modal focus at once.
- Carrying alone reveals the placement legend and world labels for valid, unavailable, occupied, and otherwise invalid destinations. These states use text and symbols as well as color.
- The HUD presents cash, earned cash, combined income, Team and Desks separately, speed, objective progress, restrained cash feedback, and away return time without daily-target language.

## Placement architecture contract

Activity placement is represented by a `WorkerCommand`; ordinary unlocked ground uses `GroundPlacementCommand`. Supported activities are Work, Rest, Get Water, Buy Snack, Smoke, Use Restroom, and Leave Office. Every activity has proximity influence beyond its visible footprint.

The player presses and holds on a worker, then moves more than six screen pixels or holds for 0.12 seconds to begin carrying. A simple click only selects. Valid release lowers the worker, issues a `WorkerCommand`, and walks the final segment; invalid release restores position and state without consuming gameplay resources. Escape and right-click cancel.

## Needs, productivity, and cash contract

- Player-facing needs are Happiness, Hunger, Bathroom, Inspiration, and Energy. Happiness/Inspiration/Energy are high-good; Hunger/Bathroom are filling urgency meters. Stress is a temporary influence, not a sixth need.
- Productivity combines all five needs with skill, inverse Stress, workstation, trait, and Focused Work, clamped to 0.10x-2.50x. Phone work applies one exact 0.50 workstation factor.
- Starter cash begins at $0. Desk and phone work earn `$60/min × effective productivity` continuously in simulation time; pause stops income. Current cash and lifetime earned are tracked separately.
- Manual Work placement refreshes a +20% Focused Work modifier to 30 seconds; it never stacks.

## Activity contract

- Activity effects and passive rates are centralized in `NeedCatalog`, `NeedSimulation`, and `ActivityRules`; exact shipping values live in `FINAL_TUNING_VALUES.md`.
- Rest, Water, Vending, Coffee, Smoking, Restroom, Social, and Away affect logical combinations of the five needs and Stress. Charges/effects occur once and all transient visuals/occupancy clean up through interruption, firing, restart, and scene exit.

## Personality and status contract

- Morgan is Hardworking: high skill, the strongest work preference, the lowest seeded distraction rate, and increased Stress gain from noisy workstations.
- Alex is Social: moderate skill, a mid-range distraction rate, longer water-cooler conversations, and a Mood benefit for nearby workers while socializing.
- Sam is Lazy: lower skill, the highest seeded distraction rate, extra Stress recovery while avoiding work, and weighted sleep, wandering, vending interest, and extended breaks.
- Autonomous workers can work, rest, get water, buy a useful affordable snack, socialize, smoke when stressed, wander, and enter seeded 6-18 second distractions before returning to work.
- A player placement overrides optional autonomy. Manual Work guarantees its 30-second Focused Work period; other manual activities retain their minimum duration unless interrupted by another player command or a critical invalidation.
- Every worker has a camera-facing head-following name tag using the bundled font. Tags scale and fade at overview zoom and can be toggled with `N` or the HUD button.
- Status emotes are brief event feedback, use ASCII-safe bundled-font text, expire cleanly, and remain separate from persistent name tags.
- The employee inspector reports personality, activity, destination, five need rows, productivity, focused time, away details, and plain-language positive/negative factors. Hunger and Bathroom state their urgency direction; Stress appears only when it is an influence.

## Simulation contract

- Starter stages have no countdown and do not finish or fail automatically.
- The first objective is `Earn $1,000 and purchase the neighboring unit.` Reaching $1,000 enables confirmation but never spends automatically; snack purchases can disable affordability again.
- Confirmed purchase deducts exactly $1,000 once and, in the current world, lights the adjacent floor, opens the connecting wall, reveals trim, enables navigation, three desk zones and the secondary rest corner, and updates camera bounds.
- The expanded starter stage unlocks three displayed-fee hires. Each enters unassigned and becomes productive only after the player drags them onto an available desk.
- Expansion unlocks `VISIT ESTABLISHED OFFICE PREVIEW`; that route is marked as a future business stage, is untimed, and returns to the Starter Office main menu.
- The Established Office retains its released worker simulation, hiring, firing, reassignment, task economy, amenities, and camera composition as an untimed sandbox.
- Core worker, task, economy, UI, audio, and camera systems are shared across stages.

## Current non-goals

- Multiple purchasable properties.
- Multiple cities or districts.
- Multiple floors.
- Managers and specialized roles.
- Rival companies.
- Promotions and relationships.
- Complex finance.
- Furniture-placement mode.
- Save-game persistence.

## Completion evidence

Each checkpoint must compile, run the complete EditMode and PlayMode suites, preserve prior release assets and evidence, document intentional migrations, and end with only intentional source changes.
