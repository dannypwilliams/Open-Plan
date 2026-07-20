# Tutorial and Presentation Report

Checkpoint 7 completes the first-run teaching and presentation pass on 2026-07-19.

## Final tutorial sequence

1. **MEET THE TEAM** - Morgan is Hardworking, Alex is Social, and Sam is Lazy. Their names and personalities explain why they behave differently. Select any worker to begin.
2. **PICK THEM UP** - Hold the left mouse button on the selected worker, then drag. Marked areas show where that worker can and cannot go. Pick up the selected worker.
3. **PUT THEM TO WORK** - Release the worker at an available desk. Manual Work grants FOCUSED WORK: +20% productivity for 30 simulation seconds. Watch company cash begin to accrue.
4. **MANAGE THEIR NEEDS** - Energy and Mood work best when high. Stress works best when low. Rest restores all three strongly; Water gives a smaller quick recovery. Place a worker at Rest or Water.
5. **REDIRECT A DISTRACTION** - Workers sometimes follow their personalities instead of the plan. The highlighted worker has entered a deterministic tutorial distraction. Pick them up and redirect them to Work, Rest, or Water.
6. **TRY THE OFFICE** - The office remains yours to experiment with. WATER restores needs. VENDING costs $15 and can malfunction. EXIT sends a worker away temporarily. SMOKING lowers Stress but takes time. You do not need to try every action now.
7. **EXPAND** - Earn $1,000 and purchase the neighboring unit. Reaching $1,000 only makes PURCHASE NEXT DOOR available—it never spends automatically. The tutorial ends here while normal play continues at your pace.

## Verification

- Automated: 43/43 EditMode and 55/55 PlayMode tests passed.
- Windows build: PASS, Unity 6000.5.1f1.
- 1280x720 packaged tutorial: 31/31 checks passed; six screenshots verified.
- 1920x1080 packaged tutorial-to-expansion: 35/35 checks passed; seven screenshots verified.
- Manual-flow economy: no artificial purchase funds; live desk work reached affordability; confirmation deducted exactly $1,000; wall, light, doorway, navigation, capacity, and milestone completed in the live office.

Machine-readable reports are `outputs/Screenshots/StarterOffice_TutorialPlaythrough_1280x720.txt` and `outputs/Screenshots/StarterOffice_TutorialPlaythrough_1920x1080.txt`. Major-state screenshots use `outputs/Screenshots/Tutorial_01_MeetTeam_*` through `Tutorial_06_Expand_*`, with final expansion at `outputs/Screenshots/Tutorial_07_ExpansionComplete_1920x1080.png`.
