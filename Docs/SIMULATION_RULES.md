# Simulation Rules

## Productivity

`effective = clamp(skill × focus × energy × morale × workstation × nearby × trait, 0.1, 2.5)`

- Focus modifier: `lerp(0.45, 1.15, focus)`
- Energy modifier: `lerp(0.55, 1.10, energy)`
- Morale modifier: `lerp(0.70, 1.10, morale)`
- Workstation: 0.88–1.12 from noise/light/location
- Nearby influence: 0.82–1.18, aggregated and clamped
- Trait: contextual, normally 0.90–1.16

The inspector always exposes a positive and a negative plain-language factor.

## Decisions

Worker thinking occurs on staggered bounded intervals. Critical needs override optional choices. Coffee requires low energy and a cooldown; water is occasional; socializing needs social desire and an available coworker; breaks require low energy/morale. Every non-work behavior has a maximum duration and returns to the assigned desk.

## Traits

- Focused: quiet bonus, conversation resistance, longer work persistence.
- Social: raises nearby morale but initiates more interruptions.
- Ambitious: benefits from strong neighbors and company success.
- Lazy: low salary, more breaks, can be helped by focused neighbors.
- Anxious: high potential, strong quiet-desk preference, noise penalty.
- Caffeinated: larger coffee boost and a later mild energy dip.

## Economy

Completed shared tasks add revenue immediately. Hiring deducts the displayed fee. Firing deducts severance and removes salary after the exit sequence begins. End-of-day net is revenue minus payroll, hiring and firing costs. Restart reconstructs the seeded initial state.

