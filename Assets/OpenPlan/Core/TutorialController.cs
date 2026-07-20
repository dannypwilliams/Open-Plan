using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenPlan
{
    public enum TutorialStep
    {
        NotStarted, MeetTheTeam, PickThemUp, PutThemToWork, ManageTheirNeeds,
        RedirectADistraction, TryTheOffice, Expand, Complete, Skipped
    }

    public static class TutorialCopy
    {
        public static string Title(TutorialStep step)
        {
            switch (step)
            {
                case TutorialStep.MeetTheTeam: return "1 / 7   MEET THE TEAM";
                case TutorialStep.PickThemUp: return "2 / 7   PICK THEM UP";
                case TutorialStep.PutThemToWork: return "3 / 7   PUT THEM TO WORK";
                case TutorialStep.ManageTheirNeeds: return "4 / 7   MANAGE THEIR NEEDS";
                case TutorialStep.RedirectADistraction: return "5 / 7   REDIRECT A DISTRACTION";
                case TutorialStep.TryTheOffice: return "6 / 7   TRY THE OFFICE";
                case TutorialStep.Expand: return "7 / 7   EXPAND";
                default: return "OPEN PLAN HELP";
            }
        }

        public static string Body(TutorialStep step)
        {
            switch (step)
            {
                case TutorialStep.MeetTheTeam:
                    return "Morgan is Hardworking, Alex is Social, and Sam is Lazy. Their names and personalities explain why they behave differently.\n\nSelect any worker to begin.";
                case TutorialStep.PickThemUp:
                    return "Hold the left mouse button on the selected worker, then drag. Marked areas show where that worker can and cannot go.\n\nPick up the selected worker.";
                case TutorialStep.PutThemToWork:
                    return "Release the worker at an available desk. Manual Work grants FOCUSED WORK: +20% productivity for 30 simulation seconds. Watch company cash begin to accrue.";
                case TutorialStep.ManageTheirNeeds:
                    return "Energy and Mood work best when high. Stress works best when low. Rest restores all three strongly; Water gives a smaller quick recovery.\n\nPlace a worker at Rest or Water.";
                case TutorialStep.RedirectADistraction:
                    return "Workers sometimes follow their personalities instead of the plan. The highlighted worker has entered a deterministic tutorial distraction.\n\nPick them up and redirect them to Work, Rest, or Water.";
                case TutorialStep.TryTheOffice:
                    return "The office remains yours to experiment with. WATER restores needs. VENDING costs $15 and can malfunction. EXIT sends a worker away temporarily. SMOKING lowers Stress but takes time. You do not need to try every action now.";
                case TutorialStep.Expand:
                    return "Earn $1,000 and purchase the neighboring unit. Reaching $1,000 only makes PURCHASE NEXT DOOR available—it never spends automatically. The tutorial ends here while normal play continues at your pace.";
                default: return string.Empty;
            }
        }

        public const string Controls =
            "CLICK select   •   HOLD + DRAG place   •   ESC / RIGHT CLICK cancel\n" +
            "WHEEL zoom   •   MIDDLE DRAG pan   •   F follow   •   N name tags\n" +
            "SPACE pause   •   1 / 2 / 3 simulation speed   •   TAB productivity overlay";
    }

    /// <summary>First-session guidance that advances only from observed selection, carry, and placement events.</summary>
    public sealed class TutorialController : MonoBehaviour
    {
        public TutorialStep CurrentStep { get; private set; } = TutorialStep.NotStarted;
        public bool IsReading { get; private set; }
        public bool HelpOpen => helpPanel != null && helpPanel.gameObject.activeSelf;
        public bool HasBlockingPanel => IsReading || HelpOpen;
        public bool IsRunning => CurrentStep >= TutorialStep.MeetTheTeam && CurrentStep <= TutorialStep.Expand;
        public bool WasSkipped => CurrentStep == TutorialStep.Skipped;
        public bool WasCompleted => CurrentStep == TutorialStep.Complete;
        public WorkerAgent HighlightedWorker { get; private set; }
        public PlacementZone HighlightedZone { get; private set; }
        public string CurrentTitle => TutorialCopy.Title(CurrentStep);
        public string CurrentBody => TutorialCopy.Body(CurrentStep);
        public RectTransform TutorialPanelRect => tutorialPanel;
        public RectTransform HelpPanelRect => helpPanel;
        public Vector2 ReferenceResolution => canvas == null ? Vector2.zero : canvas.GetComponent<CanvasScaler>().referenceResolution;
        public int ReplayCount { get; private set; }
        public event Action<TutorialStep> StepChanged;

        private OfficeDirector office;
        private Canvas canvas;
        private RectTransform tutorialPanel;
        private RectTransform helpPanel;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI bodyText;
        private TextMeshProUGUI stateText;
        private TextMeshProUGUI continueLabel;
        private Button continueButton;
        private float previousSpeed = 1f;
        private bool speedHeld;
        private bool selectedEver;
        private bool carryEver;
        private bool workPlacedEver;
        private bool workIncomeObservedEver;
        private bool needPlacedEver;
        private bool distractionRedirected;
        private WorkerAgent distractionWorker;
        private float workIncomeBaseline;
        private float recoveryCheck;

        public void Initialize(OfficeDirector director)
        {
            office = director;
            BuildUi();
            WorkerSelection.Changed += OnWorkerSelected;
            office.CarryController.CarryStarted += OnCarryStarted;
            office.WorkerCommandIssued += OnWorkerCommand;
            office.RosterChanged += EnsureHighlightedWorkerAvailable;
            office.Cash.Changed += OnCashChanged;
            bool anotherAutomationOwnsTheSession = StandaloneExpansionCaptureDirector.Requested ||
                                                    StandaloneInputSmokeDirector.Requested ||
                                                    StandaloneActivityCycleDirector.Requested ||
                                                    StandaloneBehaviorSoakDirector.Requested ||
                                                    AutomatedCaptureDirector.Requested ||
                                                    AutomatedVideoDirector.Requested ||
                                                    AutomatedPerformanceDirector.Requested ||
                                                    PackageVerificationDirector.Requested;
            bool shouldAutoStart = StandaloneTutorialPlaythroughDirector.Requested ||
                                   (!Application.isBatchMode && !anotherAutomationOwnsTheSession);
            if (office.Stage == OfficeStage.StarterOffice && shouldAutoStart)
                StartTutorial();
        }

        public void StartTutorial(bool resetObservedProgress = false)
        {
            office.CarryController?.CancelCarry(true);
            if (resetObservedProgress)
            {
                selectedEver = false;
                carryEver = false;
                workPlacedEver = false;
                workIncomeObservedEver = false;
                needPlacedEver = false;
                distractionRedirected = false;
            }
            EnterStep(TutorialStep.MeetTheTeam, true);
        }

        public void ReplayTutorial()
        {
            ReplayCount++;
            CloseHelp(false);
            StartTutorial(true);
        }

        public void SkipTutorial()
        {
            ClearHighlights();
            IsReading = false;
            CurrentStep = TutorialStep.Skipped;
            if (tutorialPanel != null) tutorialPanel.gameObject.SetActive(false);
            CloseHelp(false);
            RestoreSpeed();
            StepChanged?.Invoke(CurrentStep);
            office.HUD?.RefreshModalVisibilityForTutorial();
        }

        public void ContinueFromReading()
        {
            if (!IsReading) return;
            IsReading = false;
            RestoreSpeed();
            if (CurrentStep == TutorialStep.TryTheOffice)
            {
                EnterStep(TutorialStep.Expand, true);
                return;
            }
            if (CurrentStep == TutorialStep.Expand)
            {
                CompleteTutorial();
                return;
            }
            RefreshPanel();
            EvaluateCurrentStep();
        }

        public void OpenHelp()
        {
            office.HUD?.CloseOwnedModals();
            if (helpPanel == null) return;
            if (!IsReading) PauseSpeed();
            if (tutorialPanel != null) tutorialPanel.gameObject.SetActive(false);
            helpPanel.gameObject.SetActive(true);
            office.HUD?.RefreshModalVisibilityForTutorial();
        }

        public bool CloseTopPanel()
        {
            if (HelpOpen) { CloseHelp(true); return true; }
            if (IsReading) { ContinueFromReading(); return true; }
            return false;
        }

        public void CloseHelpForAnotherModal()
        {
            if (HelpOpen) CloseHelp(true);
        }

        private void CloseHelp(bool restore)
        {
            if (helpPanel != null) helpPanel.gameObject.SetActive(false);
            if (tutorialPanel != null) tutorialPanel.gameObject.SetActive(IsRunning);
            if (restore && !IsReading) RestoreSpeed();
            office.HUD?.RefreshModalVisibilityForTutorial();
        }

        private void Update()
        {
            if (!IsRunning) return;
            recoveryCheck -= Time.unscaledDeltaTime;
            if (recoveryCheck > 0f) return;
            recoveryCheck = .25f;
            EnsureHighlightedWorkerAvailable();
            PositionPanelAwayFromHighlight();
        }

        private void OnWorkerSelected(WorkerAgent worker)
        {
            if (worker == null) return;
            selectedEver = true;
            if (CurrentStep == TutorialStep.MeetTheTeam && !IsReading)
                EnterStep(TutorialStep.PickThemUp, true);
            else if (CurrentStep == TutorialStep.PickThemUp)
                SetHighlightedWorker(worker);
        }

        private void OnCarryStarted(WorkerAgent worker)
        {
            carryEver = true;
            SetHighlightedWorker(worker);
            if (CurrentStep == TutorialStep.PickThemUp && !IsReading)
                EnterStep(TutorialStep.PutThemToWork, false);
        }

        private void OnWorkerCommand(WorkerCommand command)
        {
            if (command == null) return;
            if (command.requestedActivity == PlacementActivity.Work) workPlacedEver = true;
            if (command.requestedActivity == PlacementActivity.Rest || command.requestedActivity == PlacementActivity.GetWater)
                needPlacedEver = true;

            if (CurrentStep == TutorialStep.PutThemToWork && !IsReading && command.requestedActivity == PlacementActivity.Work)
                workIncomeBaseline = office.Cash.LifetimeEarned;
            else if (CurrentStep == TutorialStep.ManageTheirNeeds && !IsReading &&
                     (command.requestedActivity == PlacementActivity.Rest || command.requestedActivity == PlacementActivity.GetWater))
                EnterStep(TutorialStep.RedirectADistraction, true);
            else if (CurrentStep == TutorialStep.RedirectADistraction && !IsReading && command.worker == distractionWorker &&
                     (command.requestedActivity == PlacementActivity.Work || command.requestedActivity == PlacementActivity.Rest ||
                      command.requestedActivity == PlacementActivity.GetWater))
            {
                distractionRedirected = true;
                EnterStep(TutorialStep.TryTheOffice, true);
            }
        }

        private void OnCashChanged()
        {
            if (!workPlacedEver || office.Cash.LifetimeEarned <= workIncomeBaseline + .05f) return;
            workIncomeObservedEver = true;
            if (CurrentStep == TutorialStep.PutThemToWork && !IsReading)
                EnterStep(TutorialStep.ManageTheirNeeds, true);
        }

        private void EvaluateCurrentStep()
        {
            if (IsReading) return;
            switch (CurrentStep)
            {
                case TutorialStep.MeetTheTeam:
                    if (selectedEver) EnterStep(TutorialStep.PickThemUp, true);
                    break;
                case TutorialStep.PickThemUp:
                    if (carryEver) EnterStep(TutorialStep.PutThemToWork, office.CarryController == null || !office.CarryController.IsCarrying);
                    break;
                case TutorialStep.PutThemToWork:
                    if (workIncomeObservedEver) EnterStep(TutorialStep.ManageTheirNeeds, true);
                    break;
                case TutorialStep.ManageTheirNeeds:
                    if (needPlacedEver) EnterStep(TutorialStep.RedirectADistraction, true);
                    break;
                case TutorialStep.RedirectADistraction:
                    if (distractionRedirected) EnterStep(TutorialStep.TryTheOffice, true);
                    break;
            }
        }

        private void EnterStep(TutorialStep step, bool reading)
        {
            ClearHighlights();
            CurrentStep = step;
            IsReading = reading;
            if (tutorialPanel != null) tutorialPanel.gameObject.SetActive(true);

            WorkerAgent selected = WorkerSelection.Selected;
            switch (step)
            {
                case TutorialStep.PickThemUp:
                case TutorialStep.PutThemToWork:
                    SetHighlightedWorker(IsUsable(selected) ? selected : FindUsableWorker(false));
                    if (step == TutorialStep.PutThemToWork) SetHighlightedZone(FindAvailableDesk());
                    break;
                case TutorialStep.ManageTheirNeeds:
                    SetHighlightedWorker(IsUsable(selected) ? selected : FindUsableWorker(false));
                    if (HighlightedWorker != null)
                    {
                        HighlightedWorker.Runtime.energy = Mathf.Min(HighlightedWorker.Runtime.energy, .38f);
                        HighlightedWorker.Runtime.mood = Mathf.Min(HighlightedWorker.Runtime.mood, .58f);
                        HighlightedWorker.Runtime.stress = Mathf.Max(HighlightedWorker.Runtime.stress, .62f);
                    }
                    SetHighlightedZone(FindZone("starter.rest.break-nook") ?? FindZone("starter.water.cooler"));
                    break;
                case TutorialStep.RedirectADistraction:
                    PrepareDistractionWorker();
                    break;
            }

            if (reading) PauseSpeed();
            else RestoreSpeed();
            RefreshPanel();
            PositionPanelAwayFromHighlight();
            StepChanged?.Invoke(CurrentStep);
            office.HUD?.RefreshModalVisibilityForTutorial();
        }

        private void PrepareDistractionWorker()
        {
            distractionWorker = FindWorkerNamed("Sam") ?? FindUsableWorker(false);
            if (!IsUsable(distractionWorker)) distractionWorker = FindUsableWorker(false);
            SetHighlightedWorker(distractionWorker);
            distractionWorker?.BeginDistractionForTesting(DistractionKind.Phone);
            SetHighlightedZone(FindAvailableDesk());
        }

        private void EnsureHighlightedWorkerAvailable()
        {
            if (CurrentStep != TutorialStep.PickThemUp && CurrentStep != TutorialStep.PutThemToWork &&
                CurrentStep != TutorialStep.ManageTheirNeeds && CurrentStep != TutorialStep.RedirectADistraction) return;
            if (IsUsable(HighlightedWorker)) return;
            WorkerAgent replacement = FindUsableWorker(false);
            SetHighlightedWorker(replacement);
            if (CurrentStep == TutorialStep.RedirectADistraction)
            {
                distractionWorker = replacement;
                distractionWorker?.BeginDistractionForTesting(DistractionKind.Phone);
            }
        }

        private void CompleteTutorial()
        {
            ClearHighlights();
            IsReading = false;
            RestoreSpeed();
            CurrentStep = TutorialStep.Complete;
            if (tutorialPanel != null) tutorialPanel.gameObject.SetActive(false);
            office.ShowNotice("TUTORIAL COMPLETE — EARN AND EXPAND AT YOUR PACE");
            StepChanged?.Invoke(CurrentStep);
            office.HUD?.RefreshModalVisibilityForTutorial();
        }

        private void SetHighlightedWorker(WorkerAgent worker)
        {
            HighlightedWorker?.Visuals?.SetTutorialHighlighted(false);
            HighlightedWorker = worker;
            HighlightedWorker?.Visuals?.SetTutorialHighlighted(true);
        }

        private void SetHighlightedZone(PlacementZone zone)
        {
            HighlightedZone?.SetTutorialHighlight(false);
            HighlightedZone = zone;
            HighlightedZone?.SetTutorialHighlight(true);
        }

        private void ClearHighlights()
        {
            SetHighlightedWorker(null);
            SetHighlightedZone(null);
        }

        private WorkerAgent FindUsableWorker(bool allowAway)
        {
            foreach (WorkerAgent worker in office.Workers)
                if (worker != null && !worker.IsFired && !worker.IsLeavingCompany && (allowAway || IsUsable(worker))) return worker;
            return null;
        }

        private WorkerAgent FindWorkerNamed(string name)
        {
            foreach (WorkerAgent worker in office.Workers)
                if (worker != null && worker.Definition.displayName == name && IsUsable(worker)) return worker;
            return null;
        }

        private static bool IsUsable(WorkerAgent worker)
            => worker != null && !worker.IsFired && !worker.IsLeavingCompany &&
               (!worker.IsAway || worker.Runtime.behavior == WorkerState.EnterOffice);

        private Workstation FindAvailableDesk()
        {
            foreach (Workstation desk in office.Workstations)
                if (desk != null && desk.IsZoneEnabled && (desk.IsAvailable || desk.Assigned == HighlightedWorker)) return desk;
            return null;
        }

        private PlacementZone FindZone(string stableIdentifier)
        {
            foreach (PlacementZone zone in office.PlacementZones)
                if (zone != null && zone.StableIdentifier == stableIdentifier) return zone;
            return null;
        }

        private void PauseSpeed()
        {
            if (SimulationSpeedController.Instance == null) return;
            if (!speedHeld)
            {
                previousSpeed = SimulationSpeedController.Instance.Speed;
                speedHeld = true;
            }
            SimulationSpeedController.Instance.SetSpeed(0f);
        }

        private void RestoreSpeed()
        {
            if (!speedHeld) return;
            if (SimulationSpeedController.Instance != null) SimulationSpeedController.Instance.SetSpeed(previousSpeed);
            speedHeld = false;
        }

        private void RefreshPanel()
        {
            if (titleText == null) return;
            titleText.text = CurrentTitle;
            bodyText.text = CurrentBody;
            stateText.text = IsReading ? "PAUSED FOR READING" : ActionPrompt(CurrentStep);
            continueButton.gameObject.SetActive(IsReading);
            continueLabel.text = CurrentStep == TutorialStep.Expand ? "FINISH TUTORIAL" :
                CurrentStep == TutorialStep.TryTheOffice ? "CONTINUE" : "START STEP";
        }

        private static string ActionPrompt(TutorialStep step)
        {
            switch (step)
            {
                case TutorialStep.MeetTheTeam: return "ACTION: SELECT A WORKER";
                case TutorialStep.PickThemUp: return "ACTION: HOLD + DRAG THE HIGHLIGHTED WORKER";
                case TutorialStep.PutThemToWork: return "ACTION: RELEASE AT AN AVAILABLE DESK";
                case TutorialStep.ManageTheirNeeds: return "ACTION: PLACE A WORKER AT REST OR WATER";
                case TutorialStep.RedirectADistraction: return "ACTION: REDIRECT THE HIGHLIGHTED WORKER";
                default: return string.Empty;
            }
        }

        private void PositionPanelAwayFromHighlight()
        {
            if (tutorialPanel == null || Camera.main == null) return;
            Rect[] candidates =
            {
                new Rect(.025f,.10f,.35f,.46f), new Rect(.625f,.10f,.35f,.46f),
                new Rect(.025f,.43f,.35f,.46f), new Rect(.625f,.43f,.35f,.46f)
            };
            Vector2 workerPoint = HighlightedWorker == null ? new Vector2(-10f,-10f) :
                Camera.main.WorldToViewportPoint(HighlightedWorker.transform.position + Vector3.up);
            Vector2 zonePoint = HighlightedZone == null ? new Vector2(-10f,-10f) :
                Camera.main.WorldToViewportPoint(HighlightedZone.PlacementPoint.position);
            int best = 1;
            float bestScore = float.NegativeInfinity;
            for (int i = 0; i < candidates.Length; i++)
            {
                Vector2 center = candidates[i].center;
                float score = 0f;
                if (HighlightedWorker != null)
                    score += candidates[i].Contains(workerPoint) ? -100f : (center - workerPoint).sqrMagnitude;
                if (HighlightedZone != null)
                    score += candidates[i].Contains(zonePoint) ? -100f : (center - zonePoint).sqrMagnitude;
                if (score > bestScore) { bestScore = score; best = i; }
            }
            Rect chosen = candidates[best];
            tutorialPanel.anchorMin = chosen.min;
            tutorialPanel.anchorMax = chosen.max;
            tutorialPanel.offsetMin = Vector2.zero;
            tutorialPanel.offsetMax = Vector2.zero;
        }

        private void BuildUi()
        {
            canvas = OfficeUIFactory.CreateCanvas("First Run Tutorial");
            canvas.sortingOrder = 65;
            tutorialPanel = OfficeUIFactory.Panel(canvas.transform, "Tutorial Card", new Color(.91f,.83f,.68f,.98f),
                new Vector2(.61f,.12f), new Vector2(.975f,.58f), Vector2.zero, Vector2.zero);
            titleText = OfficeUIFactory.Text(tutorialPanel, "Tutorial Title", string.Empty, 28f, OfficeUIFactory.Burgundy,
                new Vector2(.06f,.82f), new Vector2(.94f,.96f), Vector2.zero, Vector2.zero, TextAlignmentOptions.MidlineLeft);
            bodyText = OfficeUIFactory.Text(tutorialPanel, "Tutorial Body", string.Empty, 23f, OfficeUIFactory.Ink,
                new Vector2(.06f,.28f), new Vector2(.94f,.81f), Vector2.zero, Vector2.zero);
            stateText = OfficeUIFactory.Text(tutorialPanel, "Tutorial Action", string.Empty, 19f, new Color(.10f,.38f,.40f),
                new Vector2(.06f,.19f), new Vector2(.94f,.28f), Vector2.zero, Vector2.zero, TextAlignmentOptions.MidlineLeft);
            Button skip = OfficeUIFactory.Button(tutorialPanel, "Skip Tutorial", "SKIP TUTORIAL", OfficeUIFactory.Burgundy, Color.white,
                new Vector2(.06f,.05f), new Vector2(.40f,.16f), Vector2.zero, Vector2.zero);
            skip.onClick.AddListener(SkipTutorial);
            continueButton = OfficeUIFactory.Button(tutorialPanel, "Continue Tutorial", "START STEP", OfficeUIFactory.Orange, Color.white,
                new Vector2(.52f,.05f), new Vector2(.94f,.16f), Vector2.zero, Vector2.zero);
            continueLabel = continueButton.transform.Find("Label").GetComponent<TextMeshProUGUI>();
            continueButton.onClick.AddListener(ContinueFromReading);
            tutorialPanel.gameObject.SetActive(false);

            helpPanel = OfficeUIFactory.Panel(canvas.transform, "Help Panel", new Color(.055f,.035f,.030f,.985f),
                new Vector2(.17f,.16f), new Vector2(.83f,.84f), Vector2.zero, Vector2.zero);
            OfficeUIFactory.Text(helpPanel, "Help Header", "OPEN PLAN / HELP", 36f, OfficeUIFactory.Paper,
                new Vector2(.06f,.84f), new Vector2(.72f,.96f), Vector2.zero, Vector2.zero, TextAlignmentOptions.MidlineLeft);
            OfficeUIFactory.Text(helpPanel, "Help Copy",
                "PLACE PEOPLE, THEN WATCH THE OFFICE RESPOND\n\n" + TutorialCopy.Controls +
                "\n\nNEEDS\nEnergy and Mood: higher is better. Stress: lower is better.\n\nPLACEMENT\n✓ VALID means the activity can accept this worker. × UNAVAILABLE and × OCCUPIED explain blocked areas.\n\nECONOMY\nDesk work earns cash continuously. There is no countdown and no automatic spending.",
                23f, OfficeUIFactory.Paper, new Vector2(.06f,.20f), new Vector2(.94f,.83f), Vector2.zero, Vector2.zero);
            Button close = OfficeUIFactory.Button(helpPanel, "Close Help", "CLOSE", OfficeUIFactory.Teal, Color.white,
                new Vector2(.06f,.06f), new Vector2(.26f,.15f), Vector2.zero, Vector2.zero);
            close.onClick.AddListener(() => CloseHelp(true));
            Button skipFromHelp = OfficeUIFactory.Button(helpPanel, "Skip Tutorial From Help", "SKIP TUTORIAL", OfficeUIFactory.Burgundy, Color.white,
                new Vector2(.32f,.06f), new Vector2(.60f,.15f), Vector2.zero, Vector2.zero);
            skipFromHelp.onClick.AddListener(SkipTutorial);
            Button replay = OfficeUIFactory.Button(helpPanel, "Replay Tutorial", "REPLAY TUTORIAL", OfficeUIFactory.Orange, Color.white,
                new Vector2(.66f,.06f), new Vector2(.94f,.15f), Vector2.zero, Vector2.zero);
            replay.onClick.AddListener(ReplayTutorial);
            replay.gameObject.SetActive(office.Stage != OfficeStage.EstablishedOffice);
            skipFromHelp.gameObject.SetActive(office.Stage != OfficeStage.EstablishedOffice);
            helpPanel.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            WorkerSelection.Changed -= OnWorkerSelected;
            if (office != null)
            {
                if (office.CarryController != null) office.CarryController.CarryStarted -= OnCarryStarted;
                office.WorkerCommandIssued -= OnWorkerCommand;
                office.RosterChanged -= EnsureHighlightedWorkerAvailable;
                if (office.Cash != null) office.Cash.Changed -= OnCashChanged;
            }
            RestoreSpeed();
        }
    }
}
