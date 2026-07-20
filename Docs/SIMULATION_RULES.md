# Simulation Rules

## Stage and time model

`OfficeStageSelection` resolves `StarterOffice`, `StarterOfficeExpanded`, or `EstablishedOffice`. The Main Menu starts `StarterOffice`. Player stages are open-ended: there is no countdown, automatic purchase, forced success, or forced failure.

Continuous need updates, decision timers, navigation, reservations, activities, cooldowns, and income use scaled simulation time. Pause and blocking tutorial/help modals produce zero simulation progress. Two-times and four-times speed advance equivalent simulated time deterministically. Carrying suspends movement and reservation state without resetting needs.

## Authoritative five-need model

Exactly five player-facing definitions are centrally discoverable through `NeedCatalog`:

| Need | Healthy direction | Default | Passive change / simulation second |
|---|---|---:|---:|
| Happiness | high | 0.78 | -0.00018 |
| Hunger | low; filling urgency meter | 0.18 | +0.00045 |
| Bathroom | low; filling urgency meter | 0.15 | +0.00035 |
| Inspiration | high | 0.72 | -0.00026 |
| Energy | high | 0.86 | -0.00025 |

Employees receive a stable +/-0.02 identity/seed offset. High-is-good needs are Caution below 0.55, Urgent below 0.32, and Critical below 0.15. Urgency needs are Caution above 0.44, Urgent above 0.68, and Critical above 0.85. Values and Stress clamp to 0-1, with invalid floating-point inputs restored to defaults. `mood` is a compatibility alias for Happiness. Stress is a separate influence, never a sixth need.

Qualification and incident hooks remain neutral in Checkpoint 02.

## Autonomous decision ownership

`WorkerAgent` remains the single authoritative state machine. Its structured decision record identifies category, addressed need, selected activity and destination, score, reason, start time, player origin, reservation status, retry count, last progress, fallback, and remaining authority. Gameplay never parses player-facing strings.

Need evaluation is deterministic and staggered by employee identity, campaign seed, and evaluation index:

| Worst status | Reevaluation interval |
|---|---:|
| Healthy | 3-6 s |
| Caution | 2-4 s |
| Urgent | 1-2 s |
| Critical | 0.35-1 s |

The centralized hard-priority order is Critical Bathroom, Critical Hunger, Critical Energy, other Critical needs, Urgent Bathroom, Urgent Hunger, Urgent Energy, other Urgent needs, Stress recovery, ordinary personality behavior, then Work. Once recovery begins, hysteresis keeps its destination until success, invalidation, timeout, or a higher hard-priority emergency. A need decision clears only after it exits urgent range by 0.06.

Player activity placement has 22 seconds of protected authority or lasts through completion. Critical Bathroom may override after three deferred simulation seconds; critical Hunger and other critical needs after five. Nearly complete recovery activities with three seconds or less remaining are protected. Ordinary ground preserves the final position, settles briefly, and then re-enters this same decision path.

## Destination scoring and recovery mapping

The cached, stable-ID-sorted station catalog is scanned only on a scheduled decision. The centralized score combines hard priority, severity, recovery amount, multi-need benefit, path cost, activity duration, cash cost, reservation demand, stable priority, context preference, and off-site cost. Locked, disabled, full, cooldown-blocked, unaffordable, irrelevant, or unreachable candidates receive no score. Ties use ordinal stable ID.

- Happiness: Rest, Water, Vending, Coffee, Smoking, or time away.
- Hunger: affordable Vending; otherwise free off-site meal.
- Bathroom: Restroom; otherwise slower free off-site restroom use.
- Inspiration: Rest, Water, Coffee, Smoking, or time away.
- Energy: Coffee, Rest, Water, Vending, or time away.

Coffee loses utility when Bathroom urgency is elevated. Desk-assigned workers have a modest Coffee preference; phone workers have a Rest preference. These are current context hooks, not Prompt 03 qualifications.

## Reservation lifecycle

`ActivityReservationService` is owned by the current `OfficeDirector`; no reservation survives a scene. A worker holds at most one reservation. Incoming reservations count against zone capacity, convert to occupancy on arrival, become active when the activity begins, and release idempotently on completion, reroute, pickup, firing, disablement, timeout, restart, menu return, or scene destruction. Destroyed workers are cleaned by stored reservation identity. A full destination causes deterministic alternate-station, alternate-activity, off-site, or delayed retry behavior; workers never intentionally stack beyond capacity.

## Navigation and recovery

`OfficeNavigationService` uses a deterministic 0.45 m four-neighbor A* grid with 0.28 m worker clearance over unlocked walkable regions. Registered walls, partitions, desks, permanent obstacles, locked property, and voids are excluded. Exact destinations are validated, A* paths are line-of-sight smoothed, and paths are cached on the decision rather than recalculated each frame. Expansion exposes a safe invalidation/rebuild hook for future geometry work.

Workers must follow the path and reach arrival tolerance before an activity starts or charges. Lack of meaningful progress for two seconds triggers at most three deterministic repath attempts. Repeated failure releases the reservation, schedules a new decision, and only uses the last confirmed valid position as a final safety correction. This bounded recovery cannot loop permanently.

## Activity effects and return behavior

Completion effects remain exact-once: Rest (+0.32 Energy, +0.14 Happiness, +0.12 Inspiration, -0.22 Stress), Water (+0.06 Energy, +0.04 Happiness, +0.03 Inspiration, +0.08 Bathroom), Vending (-0.72 Hunger on success, $15 once), Coffee (+0.34 Energy or +0.50 Caffeinated), Smoking (-0.30 Stress, +0.07 Happiness, +0.06 Inspiration), Restroom (-0.78 Bathroom), and 30-second Away recovery across all needs. Full values are in `FINAL_TUNING_VALUES.md`.

After recovery, an assigned employee navigates back to the same desk. A deskless employee returns to readable `Working from phone` behavior. Phone output uses the exact 0.50 workstation factor once and the same five-need/autonomy path. Pause freezes both.

## Placement, economy, and expansion

Zones expose influence radius and priority; overlap resolution is higher priority, shorter distance, then ordinal stable ID. Ordinary ground creates a `GroundPlacementCommand`. Walls, registered obstacles, voids, locked property, unavailable activities, and occupied or reserved stations reject transactionally and restore the carry snapshot.

Starter Office begins at $0. Desk and phone work earn $60 per effective-productivity minute. Hiring is available whenever affordable and is not capped by desks. The HUD reports Team and Desks separately. The neighboring unit costs exactly $1,000 and never purchases automatically.
