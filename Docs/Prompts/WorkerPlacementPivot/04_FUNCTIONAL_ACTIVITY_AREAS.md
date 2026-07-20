# Prompt 4 - Make Every Placement Area Functional

```text
Continue from the worker-placement commit on branch `codex/small-office-placement-pivot`.

Project:

C:\Users\danny\Documents\GitHub\OpenPlan

Confirm the worktree is clean, inspect the worker state machine and station components, and implement this prompt fully.

Goal:

Make all six placement activities produce understandable worker behavior, readable animation, and meaningful needs or economic effects.

Simplify player-facing needs to:

- Energy: 0-1, higher is better.
- Mood: 0-1, higher is better.
- Stress: 0-1, lower is better.

Migrate existing Morale to Mood. Replace player-facing Focus with Stress. Hidden transient focus calculations may remain only if strictly necessary, but the UI and behavior model must be explainable through Energy, Mood, and Stress.

Productivity model:

skill x energy modifier x mood modifier x inverse stress modifier x workstation modifier x trait modifier x manual focused-work modifier

Clamp final productivity to a safe, documented range.

Add a simple `CashDirector`:

- Starting cash: $100.
- A worker at a desk earns `$60/min x effective productivity`.
- Income accrues continuously in simulation time.
- Pausing stops income.
- No payroll, overhead, daily report, or timed target is used in this pivot.
- Track lifetime earned separately from current cash.

Activity contracts:

WORK:

- Walk to an available desk.
- Sit and align at its use point.
- Perform the existing typing animation.
- Drain Energy.
- Increase Stress.
- Slowly reduce Mood when Stress is high.
- Generate cash continuously.
- Manual placement grants Focused Work: +20% productivity for 30 simulation seconds.
- Display a light-bulb or upward-arrow emote when Focused Work begins.
- Repeated placements refresh the effect up to 30 seconds rather than stacking it.
- Remain at the desk until a need, distraction, or player command changes activity.

REST:

- Walk to the break area.
- Rest for approximately 20 seconds.
- Recover 0.35 Energy.
- Increase Mood by 0.12.
- Reduce Stress by 0.25.
- Use sitting, stretching, phone, or idle gestures.
- Resume autonomous behavior afterward.

GET WATER:

- Walk to the cooler.
- Fill and drink for approximately 6 seconds.
- Recover 0.08 Energy.
- Increase Mood by 0.05.
- Reduce Stress by 0.05.
- Set a 35-second water cooldown.
- Allow a short social opportunity when another worker is present.
- Resume autonomous behavior afterward.

BUY SNACK:

- Validate that the company has at least $15.
- Charge exactly $15 once when use begins.
- Use the vending machine for approximately 8 seconds.
- Normal result: Energy +0.25, Mood +0.15, Stress -0.08.
- Ten-percent seeded malfunction: retain the charge, grant only Energy +0.05, reduce Mood by 0.05, and show a frustration emote plus a short machine-shake reaction.
- Set a 45-second vending cooldown.
- Insufficient cash rejects placement before charging.

SMOKE:

- Walk to the exterior designated smoking area.
- Produce a small cigarette prop.
- Play a readable smoking gesture for approximately 12 seconds.
- Emit restrained stylized smoke particles.
- Reduce Stress by 0.30.
- Increase Mood by 0.05.
- Set a 45-second smoking cooldown.
- Remove the cigarette and smoke effects on interruption, firing, stage change, or completion.

LEAVE OFFICE:

- Walk through the real exit.
- Choose a readable reason: Lunch, Errand, Long break, or Off-site task.
- Hide the worker only after they reach the exit.
- Mark them Away and show the reason and return countdown in the inspector.
- Remain away for 30 simulation seconds.
- While away, recover Energy +0.45, Mood +0.12, and Stress -0.35 over the visit.
- Generate no cash while away.
- Reappear at the entrance, walk back inside, and resume autonomous behavior.
- Firing, restart, and stage changes must safely cancel or resolve away state.

Animations may remain procedural but must clearly distinguish walking, working, sitting/resting, drinking, vending, smoking, leaving, returning, pickup, and placement.

Testing:

- Exact station effects and cooldowns.
- No duplicate vending charges.
- Seeded vending malfunction.
- Insufficient-cash rejection.
- Focused Work duration and non-stacking.
- Away and return lifecycle.
- Smoke and props clean up.
- Needs clamp correctly.
- Income pauses and scales correctly.
- Interrupted commands leave workers valid.

Run the complete test suite and observe at least one full activity cycle in a standalone build.

Commit as:

feat: make worker placement activities functional

Report the commit, tests, final numeric tuning, and the observed activity cycle.
```
