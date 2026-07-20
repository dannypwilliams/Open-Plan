using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OpenPlan
{
    public sealed class OfficeHUDController : MonoBehaviour
    {
        public bool HasModalOpen => (hiringPanel != null && hiringPanel.gameObject.activeSelf) ||
                                    (confirmPanel != null && confirmPanel.gameObject.activeSelf) ||
                                    (purchasePanel != null && purchasePanel.gameObject.activeSelf) ||
                                    (milestonePanel != null && milestonePanel.gameObject.activeSelf) ||
                                    (office != null && office.Tutorial != null && office.Tutorial.HasBlockingPanel);
        public bool NameTagsEnabled => WorkerVisuals.GlobalNameTagsVisible;
        public bool PurchasePanelVisible => purchasePanel != null && purchasePanel.gameObject.activeSelf;
        public bool PurchaseButtonInteractable => purchaseButton != null && purchaseButton.interactable;
        public bool PreviewButtonVisible => previewButton != null && previewButton.gameObject.activeSelf;
        public string GoalText => taskText == null ? string.Empty : taskText.text;
        public string HeaderText => hudText == null ? string.Empty : hudText.text;
        public bool HiringPanelVisible => hiringPanel != null && hiringPanel.gameObject.activeSelf;
        public bool MilestonePanelVisible => milestonePanel != null && milestonePanel.gameObject.activeSelf;
        public bool InspectorVisible => inspector != null && inspector.gameObject.activeSelf;
        public string InspectorText => inspectorText == null ? string.Empty : inspectorText.text;
        public int VisibleNeedRowCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < needRows.Length; i++)
                    if (needRows[i] != null && needRows[i].gameObject.activeSelf) count++;
                return count;
            }
        }
        public bool ObjectiveVisible => objectivePanel != null && objectivePanel.gameObject.activeSelf;

        private OfficeDirector office;
        private Canvas canvas;
        private TextMeshProUGUI hudText;
        private TextMeshProUGUI taskText;
        private TextMeshProUGUI inspectorText;
        private TextMeshProUGUI influenceText;
        private TextMeshProUGUI needTooltipText;
        private RectTransform needTooltip;
        private readonly EmployeeNeedRow[] needRows = new EmployeeNeedRow[5];
        private TextMeshProUGUI noticeText;
        private RectTransform inspector;
        private RectTransform objectivePanel;
        private RectTransform hiringPanel;
        private RectTransform candidateContent;
        private RectTransform confirmPanel;
        private RectTransform purchasePanel;
        private RectTransform milestonePanel;
        private TextMeshProUGUI confirmText;
        private TextMeshProUGUI cashFeedbackText;
        private Button hireButton;
        private Button purchaseButton;
        private Button previewButton;
        private WorkerAgent pendingFire;
        private float refresh;
        private float noticeUntil;
        private float cashFeedbackUntil;
        private float lastPresentedEarnings;

        public void Initialize(OfficeDirector director)
        {
            office = director;
            Build();
            WorkerSelection.Changed += OnSelection;
            office.RosterChanged += RefreshCandidates;
            office.Notice += ShowNotice;
            office.ExpansionCompleted += ShowExpansionMilestone;
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
                office.ExpansionCompleted -= ShowExpansionMilestone;
            }
        }

        private void Update()
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.hKey.wasPressedThisFrame) ToggleHiring();
                if (Keyboard.current.nKey.wasPressedThisFrame) ToggleNameTags();
                if (Keyboard.current.escapeKey.wasPressedThisFrame &&
                    (office.CarryController == null || office.CarryController.Phase == WorkerCarryPhase.Idle))
                    CloseTopModal();
            }
            refresh -= Time.unscaledDeltaTime;
            if (refresh <= 0f) { refresh = .16f; RefreshAll(); }
            if (noticeText != null && Time.unscaledTime > noticeUntil) noticeText.gameObject.SetActive(false);
            if (cashFeedbackText != null && Time.unscaledTime > cashFeedbackUntil) cashFeedbackText.gameObject.SetActive(false);
        }

        private void Build()
        {
            canvas = OfficeUIFactory.CreateCanvas("Office HUD");
            RectTransform top = OfficeUIFactory.Panel(canvas.transform, "Top HUD", OfficeUIFactory.DarkPanel,
                new Vector2(.012f,.925f), new Vector2(.988f,.988f), Vector2.zero, Vector2.zero);
            hudText = OfficeUIFactory.Text(top, "Company readout", string.Empty, 24f, OfficeUIFactory.Paper,
                Vector2.zero, new Vector2(.61f,1f), new Vector2(18f,0f), new Vector2(-4f,0f), TextAlignmentOptions.MidlineLeft);
            cashFeedbackText = OfficeUIFactory.Text(top, "Cash Feedback", string.Empty, 21f, new Color(.55f,1f,.72f),
                new Vector2(.49f,0f), new Vector2(.61f,1f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            cashFeedbackText.gameObject.SetActive(false);
            hireButton = OfficeUIFactory.Button(top, "Hire", "H  HIRE", OfficeUIFactory.Burgundy, OfficeUIFactory.Paper,
                new Vector2(.615f,.12f), new Vector2(.685f,.88f), Vector2.zero, Vector2.zero);
            hireButton.onClick.AddListener(ToggleHiring);
            Button overlay = OfficeUIFactory.Button(top, "Overlay", "TAB  OVERLAY", new Color(.12f,.34f,.36f), OfficeUIFactory.Paper,
                new Vector2(.69f,.12f), new Vector2(.765f,.88f), Vector2.zero, Vector2.zero);
            overlay.onClick.AddListener(office.ToggleOverlay);
            Button names = OfficeUIFactory.Button(top, "Name tags", "N  NAMES", new Color(.20f,.31f,.42f), OfficeUIFactory.Paper,
                new Vector2(.77f,.12f), new Vector2(.84f,.88f), Vector2.zero, Vector2.zero);
            names.onClick.AddListener(ToggleNameTags);
            Button help = OfficeUIFactory.Button(top, "Help", "HELP", OfficeUIFactory.Teal, Color.white,
                new Vector2(.84f,.12f), new Vector2(.906f,.88f), Vector2.zero, Vector2.zero);
            help.onClick.AddListener(() => office.Tutorial?.OpenHelp());
            AddSpeedButton(top, ".91", "Ⅱ", 0f);
            AddSpeedButton(top, ".932", "1×", 1f);
            AddSpeedButton(top, ".954", "2×", 2f);
            AddSpeedButton(top, ".976", "4×", 4f);

            objectivePanel = OfficeUIFactory.Panel(canvas.transform, "Objective", new Color(.90f,.67f,.28f,.96f),
                new Vector2(.018f,.61f), new Vector2(.35f,.91f), Vector2.zero, Vector2.zero);
            taskText = OfficeUIFactory.Text(objectivePanel, "Task", string.Empty, 20f, OfficeUIFactory.Ink,
                new Vector2(0f,.28f), Vector2.one, new Vector2(16f,8f), new Vector2(-12f,-8f), TextAlignmentOptions.MidlineLeft);
            purchaseButton = OfficeUIFactory.Button(objectivePanel, "Purchase Next Door", "PURCHASE NEXT DOOR", OfficeUIFactory.Burgundy, Color.white,
                new Vector2(.05f,.05f), new Vector2(.95f,.24f), Vector2.zero, Vector2.zero);
            purchaseButton.onClick.AddListener(AskPurchaseExpansion);

            inspector = OfficeUIFactory.Panel(canvas.transform, "Employee Card", new Color(.91f,.83f,.68f,.97f),
                new Vector2(.755f,.10f), new Vector2(.982f,.90f), Vector2.zero, Vector2.zero);
            OfficeUIFactory.Text(inspector, "Card Header", "EMPLOYEE ID / PERFORMANCE", 19f, OfficeUIFactory.Orange,
                new Vector2(.06f,.90f), new Vector2(.94f,.97f), Vector2.zero, Vector2.zero, TextAlignmentOptions.MidlineLeft);
            inspectorText = OfficeUIFactory.Text(inspector, "Details", string.Empty, 18f, OfficeUIFactory.Ink,
                new Vector2(.06f,.65f), new Vector2(.94f,.90f), Vector2.zero, Vector2.zero);
            BuildNeedRows();
            influenceText = OfficeUIFactory.Text(inspector, "Influences", string.Empty, 16f, OfficeUIFactory.Ink,
                new Vector2(.06f,.215f), new Vector2(.94f,.39f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.TopLeft);
            needTooltip = OfficeUIFactory.Panel(inspector, "Need Tooltip", new Color(.08f,.12f,.14f,.98f),
                new Vector2(.035f,.205f), new Vector2(.965f,.405f), Vector2.zero, Vector2.zero);
            needTooltipText = OfficeUIFactory.Text(needTooltip, "Tooltip Copy", string.Empty, 15f, Color.white,
                new Vector2(.045f,.08f), new Vector2(.955f,.92f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.TopLeft);
            needTooltip.gameObject.SetActive(false);
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
            if (office.Stage != OfficeStage.EstablishedOffice) BuildExpansionConfirmation();
            if (office.Stage != OfficeStage.EstablishedOffice) BuildExpansionMilestone();
            if (office.IsEstablishedPreview) BuildPreviewBanner();
            else if (office.Stage != OfficeStage.EstablishedOffice) BuildPreviewButton();
            noticeText = OfficeUIFactory.Text(canvas.transform, "Notice", string.Empty, 25f, Color.white,
                new Vector2(.24f,.035f), new Vector2(.76f,.09f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            noticeText.gameObject.AddComponent<Outline>().effectColor = new Color(0f,0f,0f,.9f);
            noticeText.gameObject.SetActive(false);
        }

        private void BuildNeedRows()
        {
            const float top = .645f;
            const float height = .047f;
            for (int i = 0; i < NeedCatalog.All.Length; i++)
            {
                NeedDefinition definition = NeedCatalog.All[i];
                RectTransform row = OfficeUIFactory.Panel(inspector, definition.DisplayName + " Need",
                    new Color(.08f,.10f,.11f,.08f),
                    new Vector2(.05f, top - (i + 1) * height),
                    new Vector2(.95f, top - i * height - .004f), Vector2.zero, Vector2.zero);
                TextMeshProUGUI value = OfficeUIFactory.Text(row, "Value", string.Empty, 15f, OfficeUIFactory.Ink,
                    new Vector2(.02f,0f), new Vector2(.98f,1f), Vector2.zero, Vector2.zero,
                    TextAlignmentOptions.MidlineLeft);
                value.raycastTarget = false;
                EmployeeNeedRow view = row.gameObject.AddComponent<EmployeeNeedRow>();
                view.Initialize(definition, value, ShowNeedTooltip, HideNeedTooltip);
                needRows[i] = view;
            }
        }

        private void ShowNeedTooltip(NeedDefinition definition, float value)
        {
            if (needTooltip == null || needTooltipText == null) return;
            string direction = definition.HighIsGood ? "Higher is better." : "Urgency meter - lower is better.";
            needTooltipText.text = $"<b>{definition.DisplayName}: {definition.StatusText(value)}</b>\n" +
                                   $"{direction} {definition.Description}\n<size=13>{definition.RecoveryHint}</size>";
            needTooltip.gameObject.SetActive(true);
        }

        private void HideNeedTooltip()
        {
            if (needTooltip != null) needTooltip.gameObject.SetActive(false);
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

        private void BuildExpansionConfirmation()
        {
            purchasePanel = OfficeUIFactory.Panel(canvas.transform, "Expansion Purchase Confirmation", new Color(.91f,.83f,.68f,.995f),
                new Vector2(.25f,.22f), new Vector2(.75f,.78f), Vector2.zero, Vector2.zero);
            OfficeUIFactory.Text(purchasePanel, "Header", "PURCHASE THE UNIT NEXT DOOR?", 31f, OfficeUIFactory.Burgundy,
                new Vector2(.06f,.84f), new Vector2(.94f,.96f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            OfficeUIFactory.Text(purchasePanel, "Unlocks",
                "PRICE  $1,000\n\nUNLOCKS EXACTLY:\n• Adjacent floor space\n• Connecting wall removal and open doorway\n• Three additional desk locations\n• Capacity for three additional workers\n• Access to the Established Office preview",
                24f, OfficeUIFactory.Ink, new Vector2(.09f,.27f), new Vector2(.91f,.83f), Vector2.zero, Vector2.zero);
            Button cancel = OfficeUIFactory.Button(purchasePanel, "Cancel", "NOT YET", new Color(.12f,.42f,.44f), Color.white,
                new Vector2(.08f,.08f), new Vector2(.45f,.20f), Vector2.zero, Vector2.zero);
            cancel.onClick.AddListener(() => { purchasePanel.gameObject.SetActive(false); RefreshModalVisibility(); });
            Button confirm = OfficeUIFactory.Button(purchasePanel, "Confirm", "BUY FOR $1,000", OfficeUIFactory.Orange, Color.white,
                new Vector2(.52f,.08f), new Vector2(.92f,.20f), Vector2.zero, Vector2.zero);
            confirm.onClick.AddListener(ConfirmPurchaseExpansion);
            purchasePanel.gameObject.SetActive(false);
        }

        private void BuildPreviewButton()
        {
            previewButton = OfficeUIFactory.Button(canvas.transform, "Established Office Preview",
                "VISIT ESTABLISHED OFFICE PREVIEW", new Color(.12f,.42f,.44f), Color.white,
                new Vector2(.018f,.51f), new Vector2(.35f,.59f), Vector2.zero, Vector2.zero);
            previewButton.onClick.AddListener(office.VisitEstablishedOfficePreview);
            previewButton.gameObject.SetActive(false);
        }

        private void BuildExpansionMilestone()
        {
            milestonePanel = OfficeUIFactory.Panel(canvas.transform, "First Expansion Milestone", new Color(.91f,.83f,.68f,.995f),
                new Vector2(.29f,.27f), new Vector2(.71f,.73f), Vector2.zero, Vector2.zero);
            OfficeUIFactory.Text(milestonePanel, "Milestone Header", "FIRST EXPANSION COMPLETE", 37f, OfficeUIFactory.Burgundy,
                new Vector2(.06f,.77f), new Vector2(.94f,.94f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            OfficeUIFactory.Text(milestonePanel, "Milestone Copy",
                "THE WALL IS OPEN\n\nThree desk locations and the neighboring rest corner are now active. Hire up to three additional workers and place each new arrival at a desk. The Established Office preview is unlocked.",
                24f, OfficeUIFactory.Ink, new Vector2(.09f,.28f), new Vector2(.91f,.76f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            Button continuePlaying = OfficeUIFactory.Button(milestonePanel, "Continue Playing", "CONTINUE PLAYING", OfficeUIFactory.Teal, Color.white,
                new Vector2(.08f,.08f), new Vector2(.46f,.20f), Vector2.zero, Vector2.zero);
            continuePlaying.onClick.AddListener(CloseOwnedModals);
            Button preview = OfficeUIFactory.Button(milestonePanel, "Visit Preview From Milestone", "VISIT ESTABLISHED PREVIEW", OfficeUIFactory.Orange, Color.white,
                new Vector2(.52f,.08f), new Vector2(.92f,.20f), Vector2.zero, Vector2.zero);
            preview.onClick.AddListener(office.VisitEstablishedOfficePreview);
            milestonePanel.gameObject.SetActive(false);
        }

        private void ShowExpansionMilestone()
        {
            if (milestonePanel == null) return;
            office.Tutorial?.CloseHelpForAnotherModal();
            CloseOwnedModals();
            milestonePanel.gameObject.SetActive(true);
            RefreshModalVisibility();
        }

        private void BuildPreviewBanner()
        {
            RectTransform preview = OfficeUIFactory.Panel(canvas.transform, "Future Stage Banner", new Color(.90f,.67f,.28f,.97f),
                new Vector2(.34f,.82f), new Vector2(.66f,.91f), Vector2.zero, Vector2.zero);
            OfficeUIFactory.Text(preview, "Future Stage", "FUTURE BUSINESS STAGE PREVIEW", 25f, OfficeUIFactory.Ink,
                new Vector2(.03f,.16f), new Vector2(.68f,.84f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            Button back = OfficeUIFactory.Button(preview, "Return", "RETURN TO STARTER OFFICE MENU", OfficeUIFactory.Burgundy, Color.white,
                new Vector2(.70f,.14f), new Vector2(.97f,.86f), Vector2.zero, Vector2.zero);
            back.onClick.AddListener(office.ReturnFromPreviewToMenu);
        }

        private void AskPurchaseExpansion()
        {
            if (!office.CanPurchaseExpansion)
            {
                ShowNotice($"Need ${ExpansionRules.PurchasePrice:N0} cash to purchase the neighboring unit.");
                return;
            }
            office.Tutorial?.CloseHelpForAnotherModal();
            CloseOwnedModals();
            purchasePanel.gameObject.SetActive(true);
            RefreshModalVisibility();
        }

        private void ConfirmPurchaseExpansion()
        {
            purchasePanel.gameObject.SetActive(false);
            if (!office.TryPurchaseExpansion(out string reason)) ShowNotice(reason);
            RefreshModalVisibility();
        }

        private void OnSelection(WorkerAgent worker)
        {
            RefreshModalVisibility();
            RefreshInspector();
        }

        private void RefreshModalVisibility()
        {
            if (inspector == null) return;
            bool tutorialCardVisible = office != null && office.Tutorial != null && office.Tutorial.IsRunning;
            bool modalOpen = HasModalOpen;
            inspector.gameObject.SetActive(WorkerSelection.Selected != null && !modalOpen && !tutorialCardVisible);
            if (objectivePanel != null) objectivePanel.gameObject.SetActive(!modalOpen);
            if (previewButton != null) previewButton.gameObject.SetActive(!modalOpen && office.ExpansionComplete);
        }

        public void RefreshModalVisibilityForTutorial() => RefreshModalVisibility();

        public void CloseOwnedModals()
        {
            if (hiringPanel != null) hiringPanel.gameObject.SetActive(false);
            if (confirmPanel != null) confirmPanel.gameObject.SetActive(false);
            if (purchasePanel != null) purchasePanel.gameObject.SetActive(false);
            if (milestonePanel != null) milestonePanel.gameObject.SetActive(false);
            pendingFire = null;
            RefreshModalVisibility();
        }

        public bool CloseTopModal()
        {
            if (office.Tutorial != null && office.Tutorial.CloseTopPanel())
            {
                RefreshModalVisibility();
                return true;
            }
            if (!HasModalOpen) return false;
            CloseOwnedModals();
            return true;
        }

        private void RefreshAll()
        {
            if (office == null) return;
            string speed = SimulationSpeedController.Instance == null || SimulationSpeedController.Instance.IsPaused ? "PAUSED" : SimulationSpeedController.Instance.Speed + "×";
            if (office.Stage == OfficeStage.EstablishedOffice)
            {
                hudText.text = $"CASH  ${office.Economy.Cash:N0}    TEAM  {office.ActiveWorkerCount}/{office.WorkerCapacity}    STATUS  OPEN ENDED    SPEED  {speed}";
                taskText.text = office.IsEstablishedPreview ?
                    "FUTURE BUSINESS STAGE PREVIEW\nExplore the preserved large office, then return to the Starter Office menu." :
                    "ESTABLISHED OFFICE SANDBOX\nOpen-ended developer stage. Place workers and explore without a countdown.";
            }
            else
            {
                string away = AwaySummary();
                hudText.text = $"CASH  ${office.Cash.CurrentCash:N2}    EARNED  ${office.Cash.LifetimeEarned:N2}    " +
                    $"INCOME  ${office.CombinedIncomePerMinute:N2}/MIN    TEAM  {office.ActiveWorkerCount}    DESKS  {office.DeskCount}    {speed}{away}";
                float progress = office.ExpansionComplete ? 1f : ExpansionRules.PurchaseProgress(office.Cash.CurrentCash);
                string availability = office.ExpansionComplete ? "FIRST EXPANSION COMPLETE — continue growing at your pace." :
                    office.CanPurchaseExpansion ? "The neighboring unit is available." :
                    $"${Mathf.Max(0f, ExpansionRules.PurchasePrice - office.Cash.CurrentCash):N2} still needed.";
                taskText.text = $"OBJECTIVE: Earn $1,000 and purchase the neighboring unit.\n\n" +
                    $"NEXT DOOR  ${ExpansionRules.PurchasePrice:N0}     PROGRESS  {progress:P0}\n{availability}";

                float earnedDelta = office.Cash.LifetimeEarned - lastPresentedEarnings;
                if (earnedDelta >= 5f && cashFeedbackText != null)
                {
                    cashFeedbackText.text = $"+${earnedDelta:N2}";
                    cashFeedbackText.gameObject.SetActive(true);
                    cashFeedbackUntil = Time.unscaledTime + .9f;
                    lastPresentedEarnings = office.Cash.LifetimeEarned;
                }
            }
            if (purchaseButton != null)
            {
                purchaseButton.gameObject.SetActive(office.Stage != OfficeStage.EstablishedOffice && !office.ExpansionComplete);
                purchaseButton.interactable = office.CanPurchaseExpansion;
            }
            if (hireButton != null) hireButton.interactable = true;
            if (previewButton != null) previewButton.gameObject.SetActive(office.ExpansionComplete);
            RefreshInspector();
            RefreshModalVisibility();
        }

        private string AwaySummary()
        {
            foreach (WorkerAgent worker in office.Workers)
                if (worker != null && !worker.IsFired && worker.Runtime.behavior == WorkerState.Away)
                    return $"    AWAY  {worker.Definition.displayName} {Mathf.CeilToInt(worker.Runtime.awaySecondsRemaining)}s";
            return string.Empty;
        }

        private void RefreshInspector()
        {
            WorkerAgent worker = WorkerSelection.Selected;
            if (worker == null || inspectorText == null) return;
            WorkerRuntimeState state = worker.Runtime;
            string desk = worker.Desk != null ? $"Desk {worker.Desk.Index + 1} / {worker.Desk.ZoneLabel}" : "Unassigned";
            string away = state.behavior == WorkerState.Away ?
                $"\nAWAY  {worker.AwayReasonLabel} / RETURN {Mathf.CeilToInt(state.awaySecondsRemaining)}s" : string.Empty;
            string focused = state.focusedWorkSecondsRemaining > 0f ?
                $"\nFOCUSED WORK +20%  {Mathf.CeilToInt(state.focusedWorkSecondsRemaining)}s" : string.Empty;
            inspectorText.text = BuildInspectorText(worker, state, desk, away, focused);
            for (int i = 0; i < needRows.Length; i++) needRows[i]?.Refresh(state);
            if (influenceText != null)
                influenceText.text = $"<color=#267C78>HELPS: {state.positiveInfluence}</color>\n" +
                                     $"<color=#9C332B>HURTS: {state.negativeInfluence}</color>\n" +
                                     $"<size=14>ASSIGNED  {desk}</size>";
        }

        private static string BuildInspectorText(WorkerAgent worker, WorkerRuntimeState state,
            string desk, string away, string focused)
            => $"<size=31><b>{worker.Definition.displayName}</b></size>\n" +
               $"{worker.PersonalityLabel}  |  SKILL {worker.Definition.skill:0.00}  |  OUTPUT {state.effectiveProductivity:0.00}x\n" +
               $"ACTIVITY  {worker.CurrentActivityLabel}\nDESTINATION  {worker.CurrentDestinationLabel}{away}{focused}";

        public void ToggleNameTags()
        {
            WorkerVisuals.SetGlobalNameTagsVisible(!WorkerVisuals.GlobalNameTagsVisible);
            ShowNotice(WorkerVisuals.GlobalNameTagsVisible ? "Worker name tags on." : "Worker name tags off.");
        }

        private void ToggleHiring()
        {
            bool visible = !hiringPanel.gameObject.activeSelf;
            if (visible)
            {
                office.Tutorial?.CloseHelpForAnotherModal();
                CloseOwnedModals();
                RefreshCandidates();
            }
            hiringPanel.gameObject.SetActive(visible);
            RefreshModalVisibility();
        }

        public void ShowHiringForCapture()
        {
            office.Tutorial?.CloseHelpForAnotherModal();
            CloseOwnedModals();
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
            office.Tutorial?.CloseHelpForAnotherModal();
            CloseOwnedModals();
            pendingFire = WorkerSelection.Selected;
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

    public sealed class EmployeeNeedRow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private NeedDefinition definition;
        private TextMeshProUGUI valueText;
        private Action<NeedDefinition, float> showTooltip;
        private Action hideTooltip;
        private float currentValue;

        public NeedKind Kind => definition == null ? NeedKind.Happiness : definition.Kind;
        public NeedStatus Status => definition == null ? NeedStatus.Healthy : definition.Status(currentValue);
        public string DisplayedText => valueText == null ? string.Empty : valueText.text;

        public void Initialize(NeedDefinition needDefinition, TextMeshProUGUI text,
            Action<NeedDefinition, float> onShowTooltip, Action onHideTooltip)
        {
            definition = needDefinition;
            valueText = text;
            showTooltip = onShowTooltip;
            hideTooltip = onHideTooltip;
        }

        public void Refresh(WorkerRuntimeState state)
        {
            if (definition == null || valueText == null || state == null) return;
            currentValue = state.GetNeed(definition.Kind);
            NeedStatus status = definition.Status(currentValue);
            string label = definition.Kind == NeedKind.Happiness ? "HAPPY" :
                           definition.Kind == NeedKind.Bathroom ? "BATH URG" :
                           definition.Kind == NeedKind.Hunger ? "HUNGER URG" :
                           definition.DisplayName.ToUpperInvariant();
            valueText.text = $"{label,-10} [{SafeBar(currentValue)}] {definition.StatusText(currentValue),-11} {currentValue:P0}";
            switch (status)
            {
                case NeedStatus.Critical: valueText.color = new Color(.68f,.08f,.08f); break;
                case NeedStatus.Urgent: valueText.color = new Color(.88f,.24f,.12f); break;
                case NeedStatus.Caution: valueText.color = new Color(.65f,.38f,.04f); break;
                default: valueText.color = new Color(.08f,.25f,.23f); break;
            }
        }

        public void OnPointerEnter(PointerEventData eventData) => showTooltip?.Invoke(definition, currentValue);
        public void OnPointerExit(PointerEventData eventData) => hideTooltip?.Invoke();

        private static string SafeBar(float value)
        {
            int filled = Mathf.RoundToInt(Mathf.Clamp01(value) * 6f);
            return new string('#', filled) + new string('-', 6 - filled);
        }
    }
}
