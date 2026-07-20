# Final Tuning Values

These are the values shipped in Endless Office Alpha Checkpoint `02_NeedAutonomy`. They remain an Alpha playtest baseline.

## Five needs

| Need | Default | Direction | Passive / s | Caution | Urgent | Critical |
|---|---:|---|---:|---:|---:|---:|
| Happiness | 0.78 | high good | -0.00018 | <0.55 | <0.32 | <0.15 |
| Hunger | 0.18 | high urgent | +0.00045 | >0.44 | >0.68 | >0.85 |
| Bathroom | 0.15 | high urgent | +0.00035 | >0.44 | >0.68 | >0.85 |
| Inspiration | 0.72 | high good | -0.00026 | <0.55 | <0.32 | <0.15 |
| Energy | 0.86 | high good | -0.00025 | <0.55 | <0.32 | <0.15 |

Starting identity/seed offsets are limited to +/-0.02. Stress starts at 0.22 with the same offset limit. Work multipliers are Happiness 1.55, Hunger 1.10, Bathroom 1.10, Inspiration 1.85, and Energy 4.50. Work adds 0.0012 Stress/s.

## Autonomous timing and authority

| Value | Checkpoint 02 |
|---|---:|
| Healthy evaluation | 3-6 simulation s |
| Caution evaluation | 2-4 simulation s |
| Urgent evaluation | 1-2 simulation s |
| Critical evaluation | 0.35-1 simulation s |
| Player command authority | 22 simulation s |
| Critical Bathroom deferral | 3 simulation s maximum |
| Critical Hunger/other deferral | 5 simulation s maximum |
| Near-completion protection | 3 simulation s remaining |
| Hysteresis exit margin | 0.06 beyond urgent threshold |
| Reservation lifetime | 18 simulation s per incoming/arrival phase |
| Stuck progress timeout | 2 simulation s |
| Maximum repath attempts | 3 |
| Navigation grid | 0.45 m, four-neighbor |
| Worker navigation clearance | 0.28 m |

The priority constants are 1100/1000/900/800 for Critical Bathroom/Hunger/Energy/other, 700/600/500/400 for their Urgent equivalents, and 200 for Caution. Stress recovery begins above 0.70 Stress. Off-site fallback carries a 75-point utility penalty. Desk workers receive +18 utility for Coffee; deskless workers receive +24 for Rest.

## Activities

| Activity | Duration | Completion effect | Cooldown / cost |
|---|---:|---|---:|
| Work | immediate | Focused Work +20%, non-stacking | 30 s focused duration |
| Rest | 20 s | Energy +0.32; Happiness +0.14; Inspiration +0.12; Stress -0.22 | none |
| Water | 6 s | Energy +0.06; Happiness +0.04; Inspiration +0.03; Bathroom +0.08; Stress -0.04 | 35 s |
| Vending success | 8 s | Hunger -0.72; Happiness +0.08; Energy +0.06; Stress -0.05 | 45 s; $15 once |
| Vending malfunction | 8 s | Hunger -0.08; Happiness -0.04; Energy +0.01 | 45 s; $15 once; 10% seeded chance |
| Coffee | 2.8 s | Energy +0.34 (+0.50 Caffeinated); Inspiration +0.12; Happiness +0.04; Bathroom +0.06; Stress -0.08 | 52 s or 34 s Caffeinated |
| Smoking | 12 s | Happiness +0.07; Inspiration +0.06; Stress -0.30 | 45 s |
| Restroom | 8 s | Bathroom -0.78; Happiness +0.02; Stress -0.04 | single capacity |
| Leave Office | 30 s | Energy +0.38; Happiness +0.15; Inspiration +0.16; Hunger -0.35; Bathroom -0.40; Stress -0.35 over time | none |

## Productivity and economy

| Factor / value | Behavior |
|---|---|
| Energy | 0.55-1.10 linear |
| Happiness | 0.70-1.10 linear |
| Inspiration | 0.78-1.08 linear |
| Hunger or Bathroom | 1.00 while healthy; graduates to 0.55 at maximum urgency |
| Inverse Stress | 1.15-0.55 linear |
| Phone workstation | 0.50 exactly once |
| Focused Work | 1.20 while timer remains |
| Final productivity clamp | 0.10-2.50 |
| Starting cash / income | $0 / $60 per effective-productivity minute |
| First expansion / candidate | $1,000 / $380 |
| Employee cap | none |
| Starter / expanded desks | 3 / 6 |
| Speeds | pause, 1x, 2x, 4x |
| Zoom sensitivity | normalized exponential 0.13; about ten wheel notches |
| Evidence resolution | 1920x1080 |
