# Prompt 6 - Add the Untimed Goal and Physical Expansion

```text
Continue from the worker-personality checkpoint on branch `codex/small-office-placement-pivot`.

Project:

C:\Users\danny\Documents\GitHub\OpenPlan

Confirm the previous commit and clean worktree, then implement this prompt fully.

Goal:

Complete the first-demo progression: earn money at the player's pace, purchase the neighboring business, and physically enlarge the office.

Financial objective:

- Starting cash: $100.
- Neighboring-unit purchase price: $1,000.
- No countdown.
- No automatic victory or failure.
- The player may observe and experiment indefinitely.
- When cash reaches $1,000, enable a PURCHASE NEXT DOOR action.
- Reaching the amount must not automatically spend it.
- Vending purchases can reduce cash below the goal and disable purchase again.
- Never display TARGET MISSED, TARGET APPROVED, daily report, or similar workday-result language.

Goal UI:

Show:

- Current cash.
- Lifetime earnings.
- Expansion price.
- Progress toward purchase.
- Current combined income per minute.
- Objective: `Earn $1,000 and purchase the neighboring unit.`
- Once affordable: `The neighboring unit is available.`

Purchase confirmation:

Explain exactly what unlocks:

- Adjacent floor space.
- Removal or opening of the connecting wall.
- Three additional desk locations.
- Capacity for three additional workers.
- Access to the Established Office preview.

Physical expansion sequence:

1. Pause ordinary input.
2. Play a short purchase sound and deduct cash.
3. Turn on lights in the neighboring unit.
4. Animate or remove the connecting wall.
5. Add or reveal doorway trim.
6. Enable navigation and placement inside the space.
7. Enable three desk zones.
8. Enable the secondary utility or break corner.
9. Update camera pan bounds and overview framing.
10. Resume play.
11. Display `FIRST EXPANSION COMPLETE`.

Do not reload the same scene to fake the expansion. The new area must visibly open in the current world.

After expansion:

- Allow the player to continue indefinitely.
- Unlock hiring for up to three additional workers.
- Supply at least three simple candidates derived from the existing trait system.
- Hiring requires a displayed one-time fee.
- Place new hires through the entrance.
- New hires begin unassigned and can be dragged to a desk.
- Preserve firing only if it remains stable and does not distract from the new core loop.

Established Office preview:

- Unlock a milestone or menu button labeled `VISIT ESTABLISHED OFFICE PREVIEW`.
- Loading it displays the preserved original large office.
- Mark it clearly as a future business stage.
- Allow return to the Starter Office main menu.
- No save persistence is required.

Testing:

- Purchase unavailable below $1,000.
- Purchase available at or above $1,000.
- Purchase deducts exactly $1,000 once.
- Vending spending can disable affordability.
- Locked zones become enabled after purchase.
- Connecting wall opens.
- Camera bounds update.
- New hires enter and can be placed.
- Continuing after expansion remains stable.
- Established Office preview loads and returns safely.
- Restart before and after expansion reconstructs the proper initial state.

Run the complete test suite and capture before and after expansion overviews.

Commit as:

feat: add untimed physical office expansion

Report the commit, tests, average expected time to afford expansion, and before/after screenshot paths.
```
