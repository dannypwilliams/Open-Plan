# Prompt 8 - Balance, Validate, Capture, and Package

```text
Continue from the completed tutorial and presentation checkpoint on branch `codex/small-office-placement-pivot`.

Project:

C:\Users\danny\Documents\GitHub\OpenPlan

This is the release gate for the worker-placement pivot. Work autonomously until the source, tests, media, Windows build, extracted package, and documentation are genuinely verified.

Goal:

Turn the pivot into a verified friend-test build and demonstrate that managing individual workers is entertaining on its own.

Balance targets:

- An engaged first-time player should afford the $1,000 expansion in approximately 6-10 real minutes at 1x.
- A mostly passive player should progress more slowly but must not become permanently stuck.
- Manual Focused Work creates a measurable 20% improvement.
- Rest is worthwhile before Energy reaches zero.
- Vending is helpful but not economically mandatory.
- Smoking is a stress-management alternative, not the universally best action.
- Leaving the office provides strong recovery at the cost of lost work time.
- Distractions invite intervention without becoming constant harassment.
- All three starting personalities feel meaningfully different.
- The first physical expansion feels like a strong demo payoff.

Create automated scenario tests:

ACTIVE MANAGER:

- Uses focused desk placement and appropriate restorative areas.
- Reaches expansion in the target window.

PASSIVE OBSERVER:

- Issues no commands after setup.
- Still earns money, but more slowly.

POOR MANAGER:

- Overuses breaks, snacks, or away behavior.
- Earns noticeably less without encountering a forced failure screen.

RECOVERY:

- Redirects tired or stressed workers.
- Demonstrates improved productivity after recovery.

EXPANSION:

- Purchases the neighboring unit.
- Continues operating with at least one new hire.

Run at least 20 fixed seeds and report:

- Time to $1,000.
- Lifetime earnings.
- Vending expenditure.
- Average productivity.
- Time spent working.
- Time distracted.
- Time in restorative activities.
- Manual commands issued.
- Focused Work uptime.
- Expansion completion rate.
- Stuck and recovery incidents.

Only tune existing numeric values needed to hit the targets. Record final values in `FINAL_TUNING_VALUES.md` and `SIMULATION_RULES.md`.

Regression and quality checks:

- Full EditMode suite.
- Full PlayMode suite.
- Twenty-minute accelerated standalone soak.
- 1280x720 UI smoke test.
- 1920x1080 UI smoke test.
- Starter Office package launch.
- Established Office preview launch.
- Drag, cancel, and invalid-placement stress test.
- Every activity-area lifecycle.
- Expansion purchase and wall opening.
- Hire after expansion.
- Restart before and after expansion.
- Return to menu.
- Clean quit.
- No exceptions, missing assets, stale carried state, orphaned smoke, missing worker, or permanent stuck state.

Release media:

Create a real-time gameplay capture showing:

1. Main menu.
2. Starter Office overview.
3. Worker names and personalities.
4. Pickup and valid-zone highlighting.
5. Focused desk placement and cash income.
6. Worker becoming tired or stressed.
7. Break-room recovery.
8. Water use.
9. Vending use and, if practical, a malfunction.
10. Smoking area.
11. Distraction and player redirection.
12. Leaving and returning through the exit.
13. Reaching the expansion amount.
14. Purchasing the neighboring unit.
15. Wall opening and office growth.
16. Hiring and placing a new worker.
17. Established Office preview.
18. Continued play with no forced end.

Do not fake economic or activity changes through private capture-only setters. A fixed seed and accelerated time are allowed, but events must use public gameplay APIs.

Capture and visually inspect screenshots of:

- Starter Office overview.
- Worker pickup.
- Valid placement zones.
- Invalid placement.
- Focused desk work.
- Break room.
- Water cooler.
- Vending machine.
- Smoking area.
- Distraction emote.
- Away-worker inspector.
- Three named workers.
- Expansion affordable.
- Wall-opening sequence.
- Expanded office.
- New-hire placement.
- Established Office preview.

Friend testing:

Create `FRIEND_PLAYTEST_GUIDE.md` and a plain-text copy beside the executable. Ask:

1. Was picking up and placing workers immediately understandable?
2. Did each activity area behave as expected?
3. Could you recognize individual workers?
4. Did Energy, Mood, and Stress explain their behavior?
5. Was redirecting distracted workers entertaining or annoying?
6. Did manual placement feel meaningfully better than passive observation?
7. Did the expansion feel rewarding?
8. Would you continue playing after expanding?
9. What was the most confusing element?
10. Is this concept worth further development?

Packaging:

- Build the final Windows executable.
- Create a fresh ZIP.
- Extract it into a separate verification directory.
- Launch the exact extracted executable.
- Complete the full pickup-to-expansion smoke flow.
- Calculate the ZIP SHA-256.
- Update README, BUILD_AND_RUN, TEST_REPORT, PERFORMANCE_REPORT, KNOWN_ISSUES, PRODUCT_READINESS_RUBRIC, RUN_STATE, MASTER_ROADMAP, and FINAL_IMPLEMENTATION_REPORT.
- Preserve old release evidence in a clearly labeled archive or previous-release folder rather than silently overwriting provenance.
- Document honest limitations.

Commit as:

release: package worker placement expansion demo

Final response requirements:

- Commit hash.
- Branch and worktree status.
- Test totals.
- Balance results.
- Performance and soak results.
- Extracted-package verification.
- ZIP SHA-256.
- Clickable paths to executable, ZIP, gameplay video, screenshots, playtest guide, and final report.
- Known limitations.
```
