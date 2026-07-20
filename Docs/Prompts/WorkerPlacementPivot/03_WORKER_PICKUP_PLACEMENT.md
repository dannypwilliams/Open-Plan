# Prompt 3 - Implement Click-and-Drag Worker Placement

```text
Continue from the completed Starter Office checkpoint on branch `codex/small-office-placement-pivot`.

Project:

C:\Users\danny\Documents\GitHub\OpenPlan

Confirm the previous commit and a clean worktree, then implement this prompt fully.

Goal:

Make picking up and placing workers the primary, tactile management interaction.

Implement a `WorkerCarryController` using Unity's current Input System.

Interaction contract:

- Press and hold the left mouse button on a worker.
- Begin carrying after pointer movement exceeds 6 screen pixels or the button is held for 0.12 seconds.
- A simple click without dragging still selects the worker and opens their information panel.
- While carrying:
  - Lift the worker approximately 0.65 world meters.
  - Follow the mouse over the office ground plane smoothly.
  - Suspend autonomous state transitions.
  - Suspend worker movement.
  - Preserve the worker's pre-carry position and state.
  - Prevent camera pan or world selection from taking over the same gesture.
  - Allow Escape or right-click to cancel.
- Valid zones highlight.
- Invalid or locked zones remain visibly invalid.
- The nearest valid zone under the cursor displays its activity label: WORK, REST, GET WATER, BUY SNACK, SMOKE, or LEAVE OFFICE.
- Show a translucent destination footprint or worker preview.
- Change the cursor or a cursor-adjacent label to communicate valid and invalid placement.

Successful release:

- Lower the worker with a short 0.15-second placement animation.
- Issue a `WorkerCommand` through a public gameplay API.
- Have the worker walk the final segment into the zone's exact use point.
- Do not teleport directly into the activity pose.
- Play a restrained success sound.
- Keep the worker selected in the inspector.

Invalid release:

- Play a distinct soft rejection sound.
- Animate the worker back to the pre-carry location over approximately 0.25 seconds.
- Restore the prior valid state.
- Do not consume money, cooldowns, or needs.
- Display a short explanation such as Desk occupied, Area locked, Vending machine unavailable, or Worker is leaving the company.

Safety rules:

- Fired or box-carrying workers cannot be picked up.
- Away workers cannot be picked up.
- Workers cannot be dropped on ordinary floor space.
- A worker cannot be placed at an occupied single-capacity station.
- Break, smoking, and social-capable zones may define capacity greater than one.
- UI interaction must never begin carrying.
- Opening a modal while carrying cancels safely.
- Pause preserves the carry interaction but does not advance placement animation.
- Changing stage or restarting cancels carrying without leaving static global state.

Add clear selected, hovered, carried, valid-zone, and invalid-zone visual states.

Do not remove the existing click selection, follow camera, hiring, firing, or established-office desk controls. Resolve input conflicts deliberately.

Testing:

- Click selects without carrying.
- Drag begins only after the threshold.
- Valid placement creates the correct command.
- Invalid placement restores the worker.
- Locked and occupied zones reject placement.
- UI clicks never carry workers.
- Escape and right-click cancel.
- Restart and scene changes clear carry state.
- Worker movement resumes after placement.
- No worker remains elevated after an interrupted carry.
- Behavior is correct at 1280x720 and 1920x1080.

Run the full EditMode and PlayMode suite and perform a standalone input smoke test.

Commit as:

feat: add tactile worker pickup and placement

Report the commit, tests, control contract, and screenshot paths for valid and invalid placement feedback.
```
