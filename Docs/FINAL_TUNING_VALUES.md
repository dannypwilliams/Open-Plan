# Final Tuning Values

These are the values shipped in the friend-demo package. Prompt 8 did not require changing the live economy or activity numbers; the seeded release matrix confirmed the checkpoint 7 tuning already meets the targets.

## Starter economy and expansion

| Value | Final |
|---|---:|
| Starting cash | $100 |
| Desk income | $60 per effective-productivity minute |
| Expansion purchase | $1,000 |
| First hire / Riley | $380 |
| Expanded desk capacity | +3, six total |
| Expected starting-team income | $149.124/min before modeled snack overhead |
| Expected affordability | 6.26 minutes from the static starting-team estimate |
| Measured active affordability | 7.55-7.78 minutes, 7.67 mean across 20 seeds |
| Measured passive affordability | 10.08-12.00 minutes, 10.95 mean across 20 seeds |

## Player-directed activities

| Activity | Duration | Effect / cost | Cooldown |
|---|---:|---|---:|
| Work | immediate | Focused Work +20%, non-stacking | 30 s focused duration |
| Rest | 20 s | Energy +0.35, Mood +0.12, Stress -0.25 | none |
| Water | 6 s | Energy +0.08, Mood +0.05, Stress -0.05 | 35 s |
| Vending | 8 s | $15; normally Energy +0.25, Mood +0.15, Stress -0.08 | 45 s |
| Vending malfunction | same | 10% chance; Energy +0.05, Mood -0.05, no stress change | 45 s |
| Smoking | 12 s | Mood +0.05, Stress -0.30, no energy recovery | 45 s |
| Leave Office | 30 s | Energy +0.45, Mood +0.12, Stress -0.35 over the away period | none |

Work drains 0.0018 Energy/s and adds 0.0012 Stress/s. Stress at or above 0.70 drains Mood by 0.0005/s. Productivity remains clamped to 0.10-2.50.

## Starting personalities

| Worker | Trait | Distraction chance / decision | Work preference | Decision interval |
|---|---|---:|---:|---:|
| Morgan | Hardworking | 7% | 78% | 7.0-10.0 s |
| Alex | Social | 17% | 48% | 5.5-8.5 s |
| Sam | Lazy | 30% | 28% | 4.8-7.5 s |

Distractions last 6-18 seconds. The 20-minute live soak produced 5 Morgan, 11 Alex, and 41 Sam distractions, making the personality differences readable without permanently blocking progress.

## Presentation

- Simulation speeds: paused, 1x, 2x, 4x.
- Camera: close size 4.8, overview profile 18.5, zoom sensitivity 0.012, pan sensitivity 0.018, smoothing 0.16 s.
- Evidence resolutions: 1280x720 and 1920x1080.
