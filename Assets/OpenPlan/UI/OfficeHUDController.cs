using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OpenPlan
{
    public sealed class OfficeHUDController : MonoBehaviour
    {
        public bool HasModalOpen => (hiringPanel != null && hiringPanel.gameObject.activeSelf) ||
                                    (confirmPanel != null && confirmPanel.gameObject.activeSelf) ||
                                    (reportPanel != null && reportPanel.gameObject.activeSelf);
        public bool NameTagsEnabled => WorkerVisuals.GlobalNameTagsVisible;

        private OfficeDirector office;
        private Canvas canvas;
        private TextMeshProUGUI hudText;
        private TextMeshProUGUI taskText;
        private TextMeshProUGUI inspectorText;
        private TextMeshProUGUI noticeText;
        private RectTransform inspector;
        private RectTransform hiringPanel;
        private RectTransform candidateContent;
        private RectTransform confirmPanel;
        private RectTransform reportPanel;
        private TextMeshProUGUI confirmText;
        private TextMeshProUGUI reportText;
        private WorkerAgent pendingFire;
        private float refresh;
        private float noticeUntil;

        public void Initialize(OfficeDirector director)
        {
            office = director;
            Build();
            WorkerSelection.Changed += OnSelection;
            office.RosterChanged += RefreshCandidates;
            office.Notice += ShowNotice;
            office.Workday.Ended += ShowReport;
            office.Tasks.TaskChanged += _ => RefreshAll();
            office.Tasks.TaskCompleted += task => ShowNotice($"TASK COMPLETE  +${task.revenue:N0}  {task.title}");
            OnSelection(null);
            RefreshAll();
        }

        private void OnDestroy()
        {
            WorkerSelection.Changed -= OnSelection;
            if (office != null)
            {
                office.RosterChanged -= RefreshCandidates;
                office.Notice -= ShowNotice;
                office.Workday.Ended -= ShowReport;
            }
        }

        private void Update()
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.hKey.wasPressedThisFrame) ToggleHiring();
                if (Keyboard.current.nKey.wasPressedThisFrame) ToggleNameTags();
            }
            refresh -= Time.unscaledDeltaTime;
            if (refresh <= 0f) { refresh = .16f; RefreshAll(); }
            if (noticeText != null && Time.unscaledTime > noticeUntil) noticeText.gameObject.SetActive(false);
        }

        private void Build()
        {
            canvas = OfficeUIFactory.CreateCanvas("Office HUD");
            RectTransform top = OfficeUIFactory.Panel(canvas.transform, "Top HUD", OfficeUIFactory.DarkPanel,
                new Vector2(.012f,.925f), new Vector2(.988f,.988f), Vector2.zero, Vector2.zero);
            hudText = OfficeUIFactory.Text(top, "Company readout", string.Empty, 24f, OfficeUIFactory.Paper,
                Vector2.zero, new Vector2(.67f,1f), new Vector2(18f,0f), new Vector2(-4f,0f), TextAlignmentOptions.MidlineLeft);
            Button hire = OfficeUIFactory.Button(top, "Hire", "H  HIRE", OfficeUIFactory.Burgundy, OfficeUIFactory.Paper,
                new Vector2(.68f,.12f), new Vector2(.755f,.88f), Vector2.zero, Vector2.zero);
            hire.onClick.AddListener(ToggleHiring);
            Button overlay = OfficeUIFactory.Button(top, "Overlay", "TAB  OVERLAY", new Color(.12f,.34f,.36f), OfficeUIFactory.Paper,
                new Vector2(.76f,.12f), new Vector2(.835f,.88f), Vector2.zero, Vector2.zero);
            overlay.onClick.AddListener(office.ToggleOverlay);
            Button names = OfficeUIFactory.Button(top, "Name tags", "N  NAMES", new Color(.20f,.31f,.42f), OfficeUIFactory.Paper,
                new Vector2(.84f,.12f), new Vector2(.915f,.88f), Vector2.zero, Vector2.zero);
            names.onClick.AddListener(ToggleNameTags);
            AddSpeedButton(top, ".92", "Ⅱ", 0f);
            AddSpeedButton(top, ".945", "1×", 1f);
            AddSpeedButton(top, ".97", "4×", 4f);

            RectTransform objective = OfficeUIFactory.Panel(canvas.transform, "Objective", new Color(.90f,.67f,.28f,.96f),
                new Vector2(.018f,.79f), new Vector2(.31f,.91f), Vector2.zero, Vector2.zero);
            taskText = OfficeUIFactory.Text(objective, "Task", string.Empty, 23f, OfficeUIFactory.Ink,
                Vector2.zero, Vector2.one, new Vector2(16f,8f), new Vector2(-12f,-8f), TextAlignmentOptions.MidlineLeft);

            inspector = OfficeUIFactory.Panel(canvas.transform, "Employee Card", new Color(.91f,.83f,.68f,.97f),
                new Vector2(.755f,.10f), new Vector2(.982f,.90f), Vector2.zero, Vector2.zero);
            OfficeUIFactory.Text(inspector, "Card Header", "EMPLOYEE ID / PERFORMANCE", 19f, OfficeUIFactory.Orange,
                new Vector2(.06f,.90f), new Vector2(.94f,.97f), Vector2.zero, Vector2.zero, TextAlignmentOptions.MidlineLeft);
            inspectorText = OfficeUIFactory.Text(inspector, "Details", string.Empty, 21f, OfficeUIFactory.Ink,
                new Vector2(.06f,.23f), new Vector2(.94f,.90f), Vector2.zero, Vector2.zero);
            Button follow = OfficeUIFactory.Button(inspector, "Follow", "FOLLOW", new Color(.12f,.42f,.44f), Color.white,
                new Vector2(.05f,.13f), new Vector2(.31f,.205f), Vector2.zero, Vector2.zero);
            follow.onClick.AddListener(() => Camera.main?.GetComponent<OfficeCameraRig>()?.FocusWorker(WorkerSelection.Selected, true));
            Button reassign = OfficeUIFactory.Button(inspector, "Reassign", "REASSIGN", OfficeUIFactory.Burgundy, Color.white,
                new Vector2(.36f,.13f), new Vector2(.68f,.205f), Vector2.zero, Vector2.zero);
            reassign.onClick.AddListener(office.BeginReassign);
            Button fire = OfficeUIFactory.Button(inspector, "Fire", "FIRE", OfficeUIFactory.Orange, Color.white,
                new Vector2(.73f,.13f), new Vector2(.95f,.205f), Vector2.zero, Vector2.zero);
            fire.onClick.AddListener(AskFire);
            Button closeInspector = OfficeUIFactory.Button(inspector, "Close", "×", OfficeUIFactory.Ink, OfficeUIFactory.Paper,
                new Vector2(.86f,.935f), new Vector2(.96f,.985f), Vector2.zero, Vector2.zero);
            closeInspector.onClick.AddListener(WorkerSelection.Clear);

            BuildHiringPanel();
            BuildConfirmation();
            BuildReport();
            noticeText = OfficeUIFactory.Text(canvas.transform, "Notice", string.Empty, 25f, Color.white,
                new Vector2(.24f,.035f), new Vector2(.76f,.09f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            noticeText.gameObject.AddComponent<Outline>().effectColor = new Color(0f,0f,0f,.9f);
            noticeText.gameObject.SetActive(false);
        }

        private void AddSpeedButton(Transform parent, string x, string label, float speed)
        {
            float left = float.Parse(x, System.Globalization.CultureInfo.InvariantCulture);
            Button button = OfficeUIFactory.Button(parent, label, speed == 0f ? "Ⅱ" : label, OfficeUIFactory.Ink, OfficeUIFactory.Paper,
                new Vector2(left,.12f), new Vector2(Mathf.Min(.995f,left+.022f),.88f), Vector2.zero, Vector2.zero);
            button.onClick.AddListener(() => SimulationSpeedController.Instance?.SetSpeed(speed));
        }

        private void BuildHiringPanel()
        {
            hiringPanel = OfficeUIFactory.Panel(canvas.transform, "Hiring", new Color(.055f,.035f,.030f,.97f),
                new Vector2(.08f,.12f), new Vector2(.72f,.89f), Vector2.zero, Vector2.zero);
            OfficeUIFactory.Text(hiringPanel, "Header", "INCOMING APPLICATIONS", 34f, OfficeUIFactory.Paper,
                new Vector2(.035f,.88f), new Vector2(.82f,.97f), Vector2.zero, Vector2.zero, TextAlignmentOptions.MidlineLeft);
            Button close = OfficeUIFactory.Button(hiringPanel, "Close", "CLOSE", OfficeUIFactory.Burgundy, Color.white,
                new Vector2(.84f,.90f), new Vector2(.965f,.965f), Vector2.zero, Vector2.zero);
            close.onClick.AddListener(HideHiringForCapture);
            candidateContent = OfficeUIFactory.Panel(hiringPanel, "Cards", Color.clear,
                new Vector2(.025f,.06f), new Vector2(.975f,.86f), Vector2.zero, Vector2.zero);
            RefreshCandidates();
            hiringPanel.gameObject.SetActive(false);
        }

        private void RefreshCandidates()
        {
            if (candidateContent == null) return;
            for (int i = candidateContent.childCount - 1; i >= 0; i--) Destroy(candidateContent.GetChild(i).gameObject);
            for (int i = 0; i < office.Candidates.Count; i++)
            {
                int index = i;
                CandidateDefinition candidate = office.Candidates[i];
                float left = .01f + i * .33f;
                RectTransform card = OfficeUIFactory.Panel(candidateContent, "Resume " + candidate.worker.displayName, new Color(.91f,.83f,.68f,1f),
                    new Vector2(left,.02f), new Vector2(left+.31f,.98f), Vector2.zero, Vector2.zero);
                OfficeUIFactory.Panel(card, "Portrait", candidate.worker.clothing,
                    new Vector2(.07f,.72f), new Vector2(.32f,.91f), Vector2.zero, Vector2.zero);
                OfficeUIFactory.Text(card, "Name", candidate.worker.displayName.ToUpperInvariant(), 29f, OfficeUIFactory.Burgundy,
                    new Vector2(.37f,.76f), new Vector2(.94f,.92f), Vector2.zero, Vector2.zero, TextAlignmentOptions.MidlineLeft);
                string copy = $"{candidate.worker.trait}\n\nSKILL  {candidate.worker.skill:0.00}\nSALARY  ${candidate.worker.salary:N0}\nHIRE FEE  ${candidate.hiringCost:N0}\n\n+ {candidate.worker.strength}\n\n– {candidate.worker.weakness}";
                OfficeUIFactory.Text(card, "Copy", copy, 21f, OfficeUIFactory.Ink,
                    new Vector2(.07f,.22f), new Vector2(.93f,.72f), Vector2.zero, Vector2.zero);
                Button hire = OfficeUIFactory.Button(card, "Hire", "HIRE", OfficeUIFactory.Orange, Color.white,
                    new Vector2(.12f,.07f), new Vector2(.88f,.17f), Vector2.zero, Vector2.zero);
                hire.onClick.AddListener(() =>
                {
                    if (office.Hiring.Hire(index, out string reason)) RefreshCandidates();
                    else ShowNotice(reason);
                });
            }
        }

        private void BuildConfirmation()
        {
            confirmPanel = OfficeUIFactory.Panel(canvas.transform, "Termination Form", new Color(.91f,.83f,.68f,.99f),
                new Vector2(.32f,.34f), new Vector2(.68f,.66f), Vector2.zero, Vector2.zero);
            confirmText = OfficeUIFactory.Text(confirmPanel, "Question", string.Empty, 26f, OfficeUIFactory.Ink,
                new Vector2(.08f,.38f), new Vector2(.92f,.88f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            Button cancel = OfficeUIFactory.Button(confirmPanel, "Cancel", "KEEP THEM", new Color(.12f,.42f,.44f), Color.white,
                new Vector2(.08f,.10f), new Vector2(.45f,.28f), Vector2.zero, Vector2.zero);
            cancel.onClick.AddListener(() => confirmPanel.gameObject.SetActive(false));
            Button confirm = OfficeUIFactory.Button(confirmPanel, "Confirm", "STAMP TERMINATION", OfficeUIFactory.Orange, Color.white,
                new Vector2(.52f,.10f), new Vector2(.92f,.28f), Vector2.zero, Vector2.zero);
            confirm.onClick.AddListener(ConfirmFire);
            confirmPanel.gameObject.SetActive(false);
        }

        private void BuildReport()
        {
            reportPanel = OfficeUIFactory.Panel(canvas.transform, "Report", new Color(.92f,.85f,.70f,.995f),
                new Vector2(.23f,.10f), new Vector2(.77f,.92f), Vector2.zero, Vector2.zero);
            OfficeUIFactory.Text(reportPanel, "Report Header", "OPEN PLAN / DAILY PERFORMANCE REPORT", 31f, OfficeUIFactory.Burgundy,
                new Vector2(.06f,.88f), new Vector2(.94f,.97f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            reportText = OfficeUIFactory.Text(reportPanel, "Report Copy", string.Empty, 24f, OfficeUIFactory.Ink,
                new Vector2(.09f,.20f), new Vector2(.91f,.86f), Vector2.zero, Vector2.zero);
            Button restart = OfficeUIFactory.Button(reportPanel, "Restart", "RESTART WORKDAY", OfficeUIFactory.Orange, Color.white,
                new Vector2(.08f,.07f), new Vector2(.47f,.15f), Vector2.zero, Vector2.zero);
            restart.onClick.AddListener(office.Restart);
            Button menu = OfficeUIFactory.Button(reportPanel, "Menu", "RETURN TO MENU", OfficeUIFactory.Burgundy, Color.white,
                new Vector2(.53f,.07f), new Vector2(.92f,.15f), Vector2.zero, Vector2.zero);
            menu.onClick.AddListener(office.ReturnToMenu);
            reportPanel.gameObject.SetActive(false);
        }

        private void OnSelection(WorkerAgent worker)
        {
            RefreshModalVisibility();
            RefreshInspector();
        }

        private void RefreshModalVisibility()
        {
            if (inspector == null) return;
            inspector.gameObject.SetActive(WorkerSelection.Selected != null && !HasModalOpen);
        }

        private void RefreshAll()
        {
            if (office == null) return;
            float remaining = office.Workday.IsTimed ? office.Workday.Remaining : 0f;
            int minutes = Mathf.FloorToInt(remaining / 60f);
            int seconds = Mathf.FloorToInt(remaining % 60f);
            string clock = office.Workday.IsTimed ? $"DAY  {minutes:00}:{seconds:00}" : "OPEN ENDED";
            string speed = SimulationSpeedController.Instance == null || SimulationSpeedController.Instance.IsPaused ? "PAUSED" : SimulationSpeedController.Instance.Speed + "×";
            TaskRuntime task = office.Tasks.Current;
            if (office.Stage == OfficeStage.EstablishedOffice)
            {
                hudText.text = $"{clock}     REVENUE  ${office.Economy.Revenue:N0} / ${office.Economy.DailyTarget:N0}     CASH  ${office.Economy.Cash:N0}     PAYROLL  ${office.Economy.Payroll:N0}     TEAM  {office.ActiveWorkerCount}/{office.WorkerCapacity}     {speed}";
                taskText.text = task == null ? "INBOX CLEAR" : $"REACH TODAY'S REVENUE TARGET\n{task.definition.title.ToUpperInvariant()}  {office.Tasks.Progress01:P0}  -  ${task.definition.revenue:N0}";
            }
            else
            {
                hudText.text = $"{clock}     CASH  ${office.Cash.CurrentCash:N2}     LIFETIME EARNED  ${office.Cash.LifetimeEarned:N2}     TEAM  {office.ActiveWorkerCount}/{office.WorkerCapacity}     {speed}";
                taskText.text = $"EARN $1,000 TO PURCHASE THE NEIGHBORING UNIT - NO TIME LIMIT\nDESK WORK EARNS $60/MIN × PRODUCTIVITY";
            }
            RefreshInspector();
        }

        private void RefreshInspector()
        {
            WorkerAgent worker = WorkerSelection.Selected;
            if (worker == null || inspectorText == null) return;
            WorkerRuntimeState state = worker.Runtime;
            string desk = worker.Desk != null ? $"Desk {worker.Desk.Index + 1} / {worker.Desk.ZoneLabel}" : "Unassigned";
            string away = state.behavior == WorkerState.Away ?
                $"\nAWAY         {worker.AwayReasonLabel}  •  RETURN {Mathf.CeilToInt(state.awaySecondsRemaining)}s" : string.Empty;
            string focused = state.focusedWorkSecondsRemaining > 0f ?
                $"\nFOCUSED WORK +20%  {Mathf.CeilToInt(state.focusedWorkSecondsRemaining)}s" : string.Empty;
            inspectorText.text = $"<size=36><b>{worker.Definition.displayName}</b></size>\n{worker.Definition.trait}\n\nSKILL        {worker.Definition.skill:0.00}\nPRODUCTIVITY {state.effectiveProductivity:0.00}×\nENERGY       {Bar(state.energy)} {state.energy:P0}\nMOOD         {Bar(state.mood)} {state.mood:P0}\nSTRESS       {Bar(state.stress)} {state.stress:P0}\n\nCURRENT      {Pretty(state.behavior)}{away}{focused}\n\n<size=19><color=#267C78>+ {state.positiveInfluence}</color>\n<color=#9C332B>– {state.negativeInfluence}</color>\n\nASSIGNED  {desk}</size>";
            inspectorText.text = BuildInspectorText(worker, state, desk, away, focused);
        }

        private static string BuildInspectorText(WorkerAgent worker, WorkerRuntimeState state,
            string desk, string away, string focused)
            => $"<size=36><b>{worker.Definition.displayName}</b></size>\nPERSONALITY  {worker.PersonalityLabel}\n\nSKILL        {worker.Definition.skill:0.00}\nPRODUCTIVITY {state.effectiveProductivity:0.00}x\nENERGY       {SafeBar(state.energy)} {state.energy:P0}\nMOOD         {SafeBar(state.mood)} {state.mood:P0}\nSTRESS       {SafeBar(state.stress)} {state.stress:P0}\n\nACTIVITY     {worker.CurrentActivityLabel}\nDESTINATION  {worker.CurrentDestinationLabel}{away}{focused}\n\n<size=19><color=#267C78>HELPS: {state.positiveInfluence}</color>\n<color=#9C332B>HURTS: {state.negativeInfluence}</color>\n\nASSIGNED  {desk}</size>";

        public void ToggleNameTags()
        {
            WorkerVisuals.SetGlobalNameTagsVisible(!WorkerVisuals.GlobalNameTagsVisible);
            ShowNotice(WorkerVisuals.GlobalNameTagsVisible ? "Worker name tags on." : "Worker name tags off.");
        }

        private void ToggleHiring()
        {
            bool visible = !hiringPanel.gameObject.activeSelf;
            if (visible) RefreshCandidates();
            hiringPanel.gameObject.SetActive(visible);
            RefreshModalVisibility();
        }

        public void ShowHiringForCapture()
        {
            RefreshCandidates();
            hiringPanel.gameObject.SetActive(true);
            RefreshModalVisibility();
        }

        public void HideHiringForCapture()
        {
            hiringPanel.gameObject.SetActive(false);
            RefreshModalVisibility();
        }

        private void AskFire()
        {
            pendingFire = WorkerSelection.Selected;
            if (pendingFire == null) return;
            confirmText.text = $"TERMINATION FORM\n\nFire {pendingFire.Definition.displayName}?\nThey will pack their desk, carry a box to the elevator, and payroll will fall. Severance is $110.";
            confirmPanel.gameObject.SetActive(true);
            RefreshModalVisibility();
        }

        private void ConfirmFire()
        {
            confirmPanel.gameObject.SetActive(false);
            if (pendingFire != null && !office.Firing.Fire(pendingFire, out string reason)) ShowNotice(reason);
            pendingFire = null;
            RefreshModalVisibility();
        }

        private void ShowReport(WorkdaySummary summary)
        {
            hiringPanel.gameObject.SetActive(false);
            confirmPanel.gameObject.SetActive(false);
            string stamp = summary.targetReached ? "<color=#287B62><size=42><b>TARGET APPROVED</b></size></color>" : "<color=#A13B2E><size=42><b>TARGET MISSED</b></size></color>";
            reportText.text = $"{stamp}\n\nRevenue generated                 ${summary.revenue:N0}\nPayroll                           –${summary.payroll:N0}\nHiring costs                      –${summary.hiringCosts:N0}\nFiring costs                      –${summary.firingCosts:N0}\n<size=30><b>NET                              ${summary.net:N0}</b></size>\n\nTasks completed                   {summary.tasksCompleted}\nAverage productivity              {summary.averageProductivity:0.00}×\nTime socializing                  {summary.socialSeconds:0}s\nTime lost to low energy           {summary.lowEnergySeconds:0}s\nBest employee                     {summary.bestEmployee}\nLeast productive                  {summary.leastProductiveEmployee}\nHires / Firings                   {summary.hires} / {summary.firings}";
            reportPanel.gameObject.SetActive(true);
            RefreshModalVisibility();
        }

        private void ShowNotice(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            noticeText.text = message;
            noticeText.gameObject.SetActive(true);
            noticeUntil = Time.unscaledTime + 4f;
        }

        private static string Bar(float value)
        {
            int filled = Mathf.RoundToInt(Mathf.Clamp01(value) * 8f);
            return new string('■', filled) + new string('·', 8 - filled);
        }

        private static string SafeBar(float value)
        {
            int filled = Mathf.RoundToInt(Mathf.Clamp01(value) * 8f);
            return new string('#', filled) + new string('-', 8 - filled);
        }

        private static string Pretty(WorkerState state)
        {
            string name = state.ToString();
            for (int i = 1; i < name.Length; i++) if (char.IsUpper(name[i])) { name = name.Insert(i, " "); i++; }
            return name;
        }
    }
}
