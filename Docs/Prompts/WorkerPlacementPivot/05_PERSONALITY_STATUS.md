# Prompt 5 - Add Personality, Distractions, Names, and Emotes

```text
Continue from the functional-activity checkpoint on branch `codex/small-office-placement-pivot`.

Project:

C:\Users\danny\Documents\GitHub\OpenPlan

Confirm the previous commit and a clean worktree. Implement this prompt fully.

Goal:

Make three workers individually recognizable and entertaining to manage even when the player is not issuing commands.

Starting personalities:

MORGAN:

- Trait: Hardworking.
- Higher base skill.
- Lower distraction chance.
- Stress rises faster in noisy conditions.
- Prefers work and quiet rest.

ALEX:

- Trait: Social.
- Improves nearby Mood while socializing.
- More likely to extend water-cooler conversations.
- Moderate work skill.

SAM:

- Trait: Lazy.
- Lower skill.
- Lower stress from avoiding work.
- More likely to take extended breaks, wander, or fall asleep.

Autonomous behavior:

Workers should independently:

- Work.
- Rest when Energy is low or Stress is high.
- Get water.
- Use the vending machine when useful and affordable.
- Socialize.
- Smoke occasionally when stressed and off cooldown.
- Wander briefly.
- Become distracted.
- Return to work afterward.

Player-command authority:

- A player-issued destination takes precedence over optional autonomous choices.
- Critical interruption, firing, bankruptcy, or an invalid destination may cancel it.
- Manual Work placement guarantees at least the 30-second Focused Work period unless critically interrupted.
- Non-work commands complete their minimum activity duration before normal autonomy resumes.
- Repeated commands must not corrupt state.

Distractions:

Implement readable seeded distractions:

- Looking at phone.
- Wandering.
- Standing confused.
- Falling asleep.
- Extended water-cooler conversation.
- Extended break.
- Repeated vending interest while respecting cooldown.
- Extended smoking break.

Distractions should:

- Last 6-18 seconds depending on type and personality.
- Stop productivity.
- Have an identifiable animation and emote.
- Be interruptible by pickup and placement.
- Occur often enough to create management decisions but not so often that workers seem uncontrollable.

Name tags:

Every worker needs a persistent world-space name tag that:

- Shows their name.
- Follows their head.
- Faces the camera.
- Remains readable at close and medium zoom.
- Scales down and fades at overview zoom.
- Does not overlap the status emote.
- Uses the existing bundled font.
- Can be disabled through a HUD toggle if needed.

Status emotes:

Use brief event-driven icons rather than permanent icon clutter:

- Happy.
- Sad.
- Angry or frustrated.
- Tired or sleep.
- Water.
- Snack.
- Cigarette.
- Money earned.
- Question mark.
- Exclamation mark.
- Social speech.
- Focus or light bulb.

Provide text-safe symbol fallbacks if a glyph is unavailable. Missing-glyph squares are not acceptable.

Employee inspector:

Show:

- Name.
- Personality.
- Current activity.
- Current destination.
- Energy.
- Mood.
- Stress.
- Productivity.
- Active Focused Work time.
- Away reason and return time.
- Plain-language positive and negative factors.

Office activity:

- Workers should use the full small office.
- Do not let every worker repeatedly choose the same station.
- Capacity and cooldown rules must prevent obvious stacking.
- Movement reasons must remain understandable.
- A worker who becomes distracted must remain easy to redirect through the core carry interaction.

Testing:

- Personality differences produce statistically distinct behavior with fixed seeds.
- Distractions can be redirected.
- Manual commands override optional distractions.
- Name tags track, face the camera, scale, and fade correctly.
- Emotes expire and clean up.
- Missing glyphs do not appear.
- Station capacity is respected.
- A 15-minute accelerated simulation produces varied behavior without permanent idle or stuck states.

Run the full test suite and the accelerated behavior soak.

Commit as:

feat: add worker personality and readable status feedback

Report the commit, test totals, distraction rates, soak result, and a short behavior summary for each starting worker.
```
