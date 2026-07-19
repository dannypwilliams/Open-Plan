# Prompt 7 - Build the Tutorial and Presentation Pass

```text
Continue from the completed physical-expansion checkpoint on branch `codex/small-office-placement-pivot`.

Project:

C:\Users\danny\Documents\GitHub\OpenPlan

Confirm the worktree is clean and implement this prompt fully.

Goal:

Make the pivot understandable and satisfying for a friend playing without developer guidance.

Add a dismissible first-run tutorial driven by actual gameplay events.

Tutorial sequence:

1. MEET THE TEAM
   - Introduce Morgan, Alex, and Sam by name.
   - Ask the player to select a worker.

2. PICK THEM UP
   - Ask the player to click-drag the selected worker.
   - Highlight valid zones.

3. PUT THEM TO WORK
   - Ask the player to release the worker at a desk.
   - Explain the 30-second Focused Work bonus.
   - Show money beginning to accrue.

4. MANAGE THEIR NEEDS
   - Wait for or safely accelerate a readable need.
   - Ask the player to place a worker at Rest or Water.
   - Explain Energy, Mood, and Stress.

5. REDIRECT A DISTRACTION
   - Trigger or wait for one deterministic tutorial distraction.
   - Ask the player to move that worker to a productive or restorative activity.

6. TRY THE OFFICE
   - Introduce Water, Vending, Exit, and Smoking labels without forcing every action.

7. EXPAND
   - Explain the $1,000 neighboring-unit objective.
   - Do not artificially award the purchase money.
   - End the tutorial while normal play continues.

Tutorial rules:

- Allow SKIP TUTORIAL at all times.
- Pause for reading panels.
- Restore the prior speed afterward.
- Do not obscure the highlighted worker or zone.
- Advance from actual events rather than assumed button clicks.
- Add a Help button to replay instructions.
- Restart resets tutorial state for the new session.
- No persistent save is required.
- Handle actions completed before their corresponding tutorial step.
- Handle the highlighted worker becoming unavailable without softlocking.

UI pass:

- Remove obsolete daily-target language.
- Replace dense prototype HUD text with a cleaner cash, objective, status, and speed presentation.
- Keep camera and speed controls.
- Ensure worker inspector, hiring, confirmation, Help, and milestone panels cannot overlap.
- Add a placement legend that appears only while carrying.
- Add clear unavailable and occupied labels.
- Show away-worker return time.
- Use consistent warm paper, burgundy, orange, and teal styling.

Presentation pass:

- Add pickup, valid-hover, successful-placement, and invalid-placement sounds.
- Add restrained cash feedback while working without spamming every frame.
- Improve worker pose readability for every activity.
- Add a subtle vending malfunction reaction.
- Improve smoke-particle scale and lifetime.
- Add wall-opening light and audio feedback.
- Preserve the miniature-diorama aesthetic.
- Avoid introducing unrelated systems or broad new art scope.

Accessibility and clarity:

- Escape cancels carrying or closes the top modal.
- Controls appear on the main menu and Help panel.
- Important behavior must not rely only on color.
- Test text and interaction at 1280x720 and 1920x1080.
- Verify name tags and emotes remain readable against bright windows and dark floor areas.

Testing:

- Complete tutorial path.
- Skip and replay.
- Early action handling.
- Restart reset.
- Highlighted-worker invalidation.
- Modal input blocking.
- Both target resolutions.
- Complete manual tutorial-to-expansion playthrough.

Run the full suite and fix all regressions before committing.

Commit as:

polish: teach and present the worker placement loop

Report the commit, tests, final tutorial copy, manual playthrough result, and screenshot paths for the major tutorial states.
```
