using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OpenPlan
{
    public enum WorkerCarryPhase { Idle, Pressed, Carrying, Placing, Returning }

    /// <summary>Owns the complete pointer gesture for selecting, carrying, and placing workers.</summary>
    public sealed class WorkerCarryController : MonoBehaviour
    {
        public const float DragThresholdPixels = 6f;
        public const float HoldThresholdSeconds = .12f;
        public const float CarryLiftMeters = .65f;
        public const float PlacementDurationSeconds = .15f;
        public const float ReturnDurationSeconds = .25f;

        public WorkerCarryPhase Phase { get; private set; }
        public WorkerAgent CarriedWorker { get; private set; }
        public WorkerAgent HoveredWorker { get; private set; }
        public PlacementZone TargetZone { get; private set; }
        public bool HasValidTarget { get; private set; }
        public bool HasValidGround { get; private set; }
        public Vector3 CurrentGroundPoint { get; private set; }
        public string CurrentGroundRejectionReason { get; private set; }
        public string FeedbackText { get; private set; }
        public string LastRejectionReason { get; private set; }
        public bool BlocksWorldInput => Phase != WorkerCarryPhase.Idle || uiGesture;
        public bool IsCarrying => Phase == WorkerCarryPhase.Carrying;
        public bool FeedbackVisible => feedbackRoot != null && feedbackRoot.gameObject.activeSelf;
        public Vector2 FeedbackScreenPosition => feedbackRoot != null ? feedbackRoot.position : Vector2.zero;
        public bool ExternalPointerControl { get; set; }
        public Mouse InputMouseOverride { get; set; }
        public bool PlacementLegendVisible => placementLegend != null && placementLegend.gameObject.activeSelf;
        public event System.Action<WorkerAgent> CarryStarted;
        public event System.Action<WorkerAgent, bool> CarryFinished;

        private OfficeDirector office;
        private OfficeCameraRig cameraRig;
        private OfficeHUDController hud;
        private AudioDirector audioDirector;
        private Camera worldCamera;
        private WorkerAgent pressedWorker;
        private Vector2 pressScreenPosition;
        private Vector2 pointerScreenPosition;
        private float pressTime;
        private bool uiGesture;
        private Vector3 animationStart;
        private Vector3 animationEnd;
        private float animationElapsed;
        private PlacementZone pendingDestination;
        private bool pendingGroundPlacement;
        private Vector3 pendingGroundPoint;
        private string pendingFailureReason;
        private RectTransform feedbackRoot;
        private Image feedbackBackground;
        private TextMeshProUGUI feedbackLabel;
        private RectTransform placementLegend;

        public void Initialize(OfficeDirector director, OfficeCameraRig rig, OfficeHUDController officeHud, AudioDirector audio)
        {
            office = director;
            cameraRig = rig;
            hud = officeHud;
            audioDirector = audio;
            worldCamera = rig != null ? rig.GetComponent<Camera>() : Camera.main;
            BuildFeedbackUi();
            ClearFeedback();
        }

        private void Update()
        {
            if (office == null) return;
            if (office.InputLocked)
            {
                if (Phase != WorkerCarryPhase.Idle) CancelCarry(true, "Input paused during expansion.");
                return;
            }
            if (worldCamera == null) worldCamera = Camera.main;

            if (Phase == WorkerCarryPhase.Placing || Phase == WorkerCarryPhase.Returning)
            {
                TickAnimation();
                return;
            }

            Mouse mouse = InputMouseOverride ?? Mouse.current;
            Keyboard keyboard = Keyboard.current;
            if (Phase == WorkerCarryPhase.Carrying && ShouldCancel(
                keyboard != null && keyboard.escapeKey.wasPressedThisFrame,
                mouse != null && mouse.rightButton.wasPressedThisFrame))
            {
                CancelCarry(false, "Placement cancelled.");
                return;
            }

            if ((Phase == WorkerCarryPhase.Pressed || Phase == WorkerCarryPhase.Carrying) && hud != null && hud.HasModalOpen)
            {
                CancelCarry(false, "Placement cancelled while a panel is open.");
                return;
            }

            if (ExternalPointerControl) return;

            if (mouse == null) return;
            pointerScreenPosition = mouse.position.ReadValue();

            if (mouse.leftButton.wasPressedThisFrame)
            {
                bool overUi = PointerOverUI();
                WorkerAgent worker = overUi ? null : WorkerUnderPointer(pointerScreenPosition);
                BeginPointerGesture(worker, pointerScreenPosition, overUi);
            }

            if (Phase == WorkerCarryPhase.Pressed && mouse.leftButton.isPressed)
            {
                float held = Time.unscaledTime - pressTime;
                Vector3 ground = GroundPoint(pointerScreenPosition, pressedWorker != null ? pressedWorker.transform.position : Vector3.zero);
                EvaluateCarryStart(pointerScreenPosition, held, ground);
            }

            if (Phase == WorkerCarryPhase.Carrying)
            {
                Vector3 ground = GroundPoint(pointerScreenPosition, CarriedWorker.transform.position);
                PlacementZone zone = ZoneAtGroundPoint(ground);
                UpdateCarriedPosition(ground, zone, pointerScreenPosition, false);
            }
            else if (Phase == WorkerCarryPhase.Idle && !uiGesture)
            {
                SetHoveredWorker(PointerOverUI() ? null : WorkerUnderPointer(pointerScreenPosition));
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                if (uiGesture)
                {
                    uiGesture = false;
                    return;
                }
                if (Phase == WorkerCarryPhase.Carrying) ReleaseAtGround(CurrentGroundPoint, TargetZone);
                else if (Phase == WorkerCarryPhase.Pressed)
                {
                    Phase = WorkerCarryPhase.Idle;
                    pressedWorker = null;
                    cameraRig?.HandleWorldClick(pointerScreenPosition);
                }
            }
        }

        public static bool ShouldBeginCarry(Vector2 pressPosition, Vector2 currentPosition, float heldSeconds)
            => (currentPosition - pressPosition).sqrMagnitude > DragThresholdPixels * DragThresholdPixels ||
               heldSeconds >= HoldThresholdSeconds;

        public static bool ShouldCancel(bool escapePressed, bool rightMousePressed)
            => escapePressed || rightMousePressed;

        public bool BeginPointerGesture(WorkerAgent worker, Vector2 screenPosition, bool pointerOverUi)
        {
            if (Phase != WorkerCarryPhase.Idle || (office != null && office.WorldInputBlocked)) return false;
            pointerScreenPosition = pressScreenPosition = screenPosition;
            pressTime = Time.unscaledTime;
            uiGesture = pointerOverUi;
            if (pointerOverUi) return false;

            pressedWorker = worker;
            Phase = WorkerCarryPhase.Pressed;
            if (worker != null)
            {
                WorkerSelection.Select(worker);
                SetHoveredWorker(worker);
            }
            return true;
        }

        public bool EvaluateCarryStart(Vector2 currentScreenPosition, float heldSeconds, Vector3 groundPoint)
        {
            if (Phase != WorkerCarryPhase.Pressed || pressedWorker == null ||
                !ShouldBeginCarry(pressScreenPosition, currentScreenPosition, heldSeconds)) return false;
            if (!pressedWorker.BeginPlayerCarry(out string reason))
            {
                LastRejectionReason = reason;
                office.ShowNotice(reason);
                audioDirector?.PlayPlacementRejected();
                Phase = WorkerCarryPhase.Idle;
                pressedWorker = null;
                return false;
            }

            CarriedWorker = pressedWorker;
            pressedWorker = null;
            Phase = WorkerCarryPhase.Carrying;
            SetHoveredWorker(null);
            WorkerSelection.Select(CarriedWorker);
            audioDirector?.PlayPickup();
            CarryStarted?.Invoke(CarriedWorker);
            ShowAllZoneStates();
            UpdateCarriedPosition(groundPoint, ZoneAtGroundPoint(groundPoint), currentScreenPosition, true);
            return true;
        }

        public void UpdateCarriedPosition(Vector3 groundPoint, PlacementZone zoneUnderPointer,
            Vector2 screenPosition, bool immediate)
        {
            if (Phase != WorkerCarryPhase.Carrying || CarriedWorker == null) return;
            pointerScreenPosition = screenPosition;
            float groundY = CarriedWorker.PreCarryPosition.y;
            Vector3 desired = new Vector3(groundPoint.x, groundY + CarryLiftMeters, groundPoint.z);
            Vector3 position = immediate ? desired : Vector3.Lerp(CarriedWorker.transform.position, desired,
                1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
            CarriedWorker.SetPlayerCarryPosition(position);
            CurrentGroundPoint = new Vector3(groundPoint.x, groundY, groundPoint.z);
            string groundReason = null;
            HasValidGround = office != null && office.Layout != null &&
                office.Layout.CanPlaceWorkerAt(CurrentGroundPoint, out groundReason);
            CurrentGroundRejectionReason = HasValidGround ? null : groundReason;
            SetTargetZone(zoneUnderPointer);
            PositionFeedback(screenPosition);
        }

        public void ReleaseAtZone(PlacementZone destination)
        {
            if (Phase == WorkerCarryPhase.Pressed)
            {
                Phase = WorkerCarryPhase.Idle;
                pressedWorker = null;
                return;
            }
            if (Phase != WorkerCarryPhase.Carrying || CarriedWorker == null) return;

            if (destination == null)
            {
                BeginReturn("Drop on a marked activity area.", true);
                return;
            }
            if (!destination.CanAcceptWorker(CarriedWorker, out string reason))
            {
                BeginReturn(reason, true);
                return;
            }

            pendingDestination = destination;
            animationStart = CarriedWorker.transform.position;
            animationEnd = new Vector3(animationStart.x, CarriedWorker.PreCarryPosition.y, animationStart.z);
            animationElapsed = 0f;
            Phase = WorkerCarryPhase.Placing;
            ClearZoneStates();
            ShowFeedback("PLACE  " + destination.ActivityLabel.ToUpperInvariant(), true, pointerScreenPosition);
        }

        public void ReleaseAtGround(Vector3 groundPoint, PlacementZone destination = null)
        {
            if (destination != null)
            {
                ReleaseAtZone(destination);
                return;
            }
            if (Phase != WorkerCarryPhase.Carrying || CarriedWorker == null) return;
            string reason;
            if (office == null || office.Layout == null)
            {
                BeginReturn("The office floor is unavailable.", true);
                return;
            }
            if (!office.Layout.CanPlaceWorkerAt(groundPoint, out reason))
            {
                BeginReturn(reason ?? "That point is not walkable.", true);
                return;
            }

            pendingDestination = null;
            pendingGroundPlacement = true;
            pendingGroundPoint = groundPoint;
            animationStart = CarriedWorker.transform.position;
            animationEnd = new Vector3(groundPoint.x, CarriedWorker.PreCarryPosition.y, groundPoint.z);
            animationElapsed = 0f;
            Phase = WorkerCarryPhase.Placing;
            ClearZoneStates();
            ShowFeedback("PLACE HERE  AUTONOMY", true, pointerScreenPosition);
        }

        public void CancelCarry(bool immediate, string reason = null)
        {
            if (Phase == WorkerCarryPhase.Pressed)
            {
                Phase = WorkerCarryPhase.Idle;
                pressedWorker = null;
                uiGesture = false;
                ClearFeedback();
                return;
            }
            if (CarriedWorker == null)
            {
                ResetController();
                return;
            }
            if (immediate)
            {
                WorkerAgent cancelled = CarriedWorker;
                CarriedWorker.CancelPlayerCarryImmediate();
                ResetController();
                CarryFinished?.Invoke(cancelled, false);
                return;
            }
            BeginReturn(reason ?? "Placement cancelled.", false);
        }

        public void CancelIfWorker(WorkerAgent worker)
        {
            if (worker != null && (CarriedWorker == worker || pressedWorker == worker)) CancelCarry(true);
        }

        private void BeginReturn(string reason, bool rejected)
        {
            if (CarriedWorker == null) { ResetController(); return; }
            pendingFailureReason = reason;
            LastRejectionReason = rejected ? reason : LastRejectionReason;
            animationStart = CarriedWorker.transform.position;
            animationEnd = CarriedWorker.PreCarryPosition;
            animationElapsed = 0f;
            Phase = WorkerCarryPhase.Returning;
            ClearZoneStates();
            ShowFeedback((rejected ? "INVALID  " : "CANCELLED  ") + reason.ToUpperInvariant(), false, pointerScreenPosition);
            if (rejected) audioDirector?.PlayPlacementRejected();
        }

        private void TickAnimation()
        {
            if (CarriedWorker == null) { ResetController(); return; }
            float duration = Phase == WorkerCarryPhase.Placing ? PlacementDurationSeconds : ReturnDurationSeconds;
            animationElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(animationElapsed / duration);
            float eased = t * t * (3f - 2f * t);
            CarriedWorker.SetPlayerCarryPosition(Vector3.Lerp(animationStart, animationEnd, eased));
            if (t < 1f) return;

            if (Phase == WorkerCarryPhase.Placing)
            {
                WorkerAgent worker = CarriedWorker;
                PlacementZone destination = pendingDestination;
                bool issued = pendingGroundPlacement
                    ? office.TryIssueGroundPlacementCommand(worker, pendingGroundPoint, out _, out string reason)
                    : office.TryIssueWorkerCommand(worker, destination, out _, out reason);
                if (issued)
                {
                    audioDirector?.PlayPlacementSuccess();
                    ResetController(false);
                    CarryFinished?.Invoke(worker, true);
                }
                else
                {
                    pendingFailureReason = reason;
                    animationStart = worker.transform.position;
                    animationEnd = worker.PreCarryPosition;
                    animationElapsed = 0f;
                    Phase = WorkerCarryPhase.Returning;
                    LastRejectionReason = reason;
                    audioDirector?.PlayPlacementRejected();
                    ShowFeedback("INVALID  " + reason.ToUpperInvariant(), false, pointerScreenPosition);
                }
            }
            else
            {
                WorkerAgent returned = CarriedWorker;
                CarriedWorker.CancelPlayerCarryImmediate();
                if (!string.IsNullOrWhiteSpace(pendingFailureReason)) office.ShowNotice(pendingFailureReason);
                ResetController();
                CarryFinished?.Invoke(returned, false);
            }
        }

        private void SetTargetZone(PlacementZone zone)
        {
            if (TargetZone == zone)
            {
                RefreshTargetFeedback();
                return;
            }
            if (TargetZone != null)
                ApplyZoneBaseState(TargetZone);
            TargetZone = zone;
            RefreshTargetFeedback();
            if (HasValidTarget) audioDirector?.PlayValidHover();
        }

        private void RefreshTargetFeedback()
        {
            if (CarriedWorker == null) return;
            if (TargetZone == null)
            {
                HasValidTarget = HasValidGround;
                string feedback = HasValidGround ? "PLACE HERE  AUTONOMY" :
                    "INVALID  " + (CurrentGroundRejectionReason ?? "NOT WALKABLE").ToUpperInvariant();
                ShowFeedback(feedback,
                    HasValidGround, pointerScreenPosition);
                return;
            }
            HasValidTarget = TargetZone.CanAcceptWorker(CarriedWorker, out string reason);
            TargetZone.SetCarryVisualState(HasValidTarget ? PlacementZoneVisualState.HoveredValid : PlacementZoneVisualState.HoveredInvalid);
            ShowFeedback(HasValidTarget ? TargetZone.ActivityLabel.ToUpperInvariant() : "INVALID  " + reason.ToUpperInvariant(),
                HasValidTarget, pointerScreenPosition);
        }

        private void ShowAllZoneStates()
        {
            if (placementLegend != null) placementLegend.gameObject.SetActive(true);
            foreach (PlacementZone zone in office.PlacementZones) ApplyZoneBaseState(zone);
        }

        private void ApplyZoneBaseState(PlacementZone zone)
        {
            if (zone == null || CarriedWorker == null) return;
            bool valid = zone.CanAcceptWorker(CarriedWorker, out _);
            zone.SetCarryVisualState(valid ? PlacementZoneVisualState.Valid : PlacementZoneVisualState.Invalid);
        }

        private void ClearZoneStates()
        {
            if (office != null)
                foreach (PlacementZone zone in office.PlacementZones)
                    if (zone != null) zone.SetCarryVisualState(PlacementZoneVisualState.None);
            TargetZone = null;
            HasValidTarget = false;
            if (placementLegend != null) placementLegend.gameObject.SetActive(false);
        }

        private WorkerAgent WorkerUnderPointer(Vector2 screenPosition)
        {
            if (worldCamera == null) return null;
            Ray ray = worldCamera.ScreenPointToRay(screenPosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 120f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                WorkerAgent worker = hit.collider.GetComponentInParent<WorkerAgent>();
                if (worker != null) return worker;
            }
            return null;
        }

        private PlacementZone ZoneAtGroundPoint(Vector3 point)
            => ResolveInfluencingZone(office == null ? null : office.PlacementZones, point);

        public static PlacementZone ResolveInfluencingZone(IEnumerable<PlacementZone> zones, Vector3 point)
        {
            if (zones == null) return null;
            PlacementZone best = null;
            int bestPriority = int.MinValue;
            float bestDistance = float.MaxValue;
            foreach (PlacementZone zone in zones)
            {
                if (zone == null || !zone.ContainsInfluence(point)) continue;
                float distance = (zone.PlacementPoint.position - point).sqrMagnitude;
                bool higherPriority = zone.InfluencePriority > bestPriority;
                bool nearerAtSamePriority = zone.InfluencePriority == bestPriority && distance < bestDistance - .0001f;
                bool stableTieBreak = zone.InfluencePriority == bestPriority &&
                    Mathf.Abs(distance - bestDistance) <= .0001f &&
                    string.CompareOrdinal(zone.StableIdentifier ?? string.Empty,
                        best == null ? string.Empty : best.StableIdentifier ?? string.Empty) < 0;
                if (best != null && !higherPriority && !nearerAtSamePriority && !stableTieBreak) continue;
                best = zone;
                bestPriority = zone.InfluencePriority;
                bestDistance = distance;
            }
            return best;
        }

        private Vector3 GroundPoint(Vector2 screenPosition, Vector3 fallback)
        {
            if (worldCamera == null) return fallback;
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = worldCamera.ScreenPointToRay(screenPosition);
            return plane.Raycast(ray, out float distance) ? ray.GetPoint(distance) : fallback;
        }

        private void SetHoveredWorker(WorkerAgent worker)
        {
            if (HoveredWorker == worker) return;
            HoveredWorker?.Visuals?.SetHovered(false);
            HoveredWorker = worker;
            HoveredWorker?.Visuals?.SetHovered(true);
        }

        private void BuildFeedbackUi()
        {
            Canvas canvas = OfficeUIFactory.CreateCanvas("Worker Placement Feedback");
            canvas.sortingOrder = 80;
            feedbackRoot = OfficeUIFactory.Panel(canvas.transform, "Cursor Placement Label",
                new Color(.055f,.038f,.035f,.90f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            feedbackRoot.sizeDelta = new Vector2(330f, 54f);
            feedbackRoot.pivot = new Vector2(0f, 1f);
            feedbackBackground = feedbackRoot.GetComponent<Image>();
            feedbackBackground.raycastTarget = false;
            feedbackLabel = OfficeUIFactory.Text(feedbackRoot, "Placement Status", string.Empty, 22f, Color.white,
                Vector2.zero, Vector2.one, new Vector2(14f,4f), new Vector2(-14f,-4f), TextAlignmentOptions.MidlineLeft);
            feedbackRoot.gameObject.SetActive(false);

            placementLegend = OfficeUIFactory.Panel(canvas.transform, "Placement Legend",
                new Color(.055f,.038f,.035f,.94f), new Vector2(.25f,.018f), new Vector2(.75f,.073f), Vector2.zero, Vector2.zero);
            Image legendBackground = placementLegend.GetComponent<Image>();
            legendBackground.raycastTarget = false;
            TextMeshProUGUI legendText = OfficeUIFactory.Text(placementLegend, "Legend Copy",
                "✓ VALID: RELEASE TO PLACE     × UNAVAILABLE: LOCKED     × OCCUPIED: CHOOSE ANOTHER",
                19f, OfficeUIFactory.Paper, Vector2.zero, Vector2.one, new Vector2(12f,0f), new Vector2(-12f,0f),
                TextAlignmentOptions.Center);
            legendText.raycastTarget = false;
            placementLegend.gameObject.SetActive(false);
        }

        private void ShowFeedback(string message, bool valid, Vector2 screenPosition)
        {
            FeedbackText = message;
            if (feedbackRoot == null) return;
            feedbackRoot.gameObject.SetActive(true);
            feedbackBackground.color = valid ? new Color(.05f,.30f,.25f,.94f) : new Color(.43f,.10f,.08f,.94f);
            feedbackLabel.color = valid ? new Color(.68f,1f,.86f) : new Color(1f,.72f,.58f);
            feedbackLabel.text = message;
            PositionFeedback(screenPosition);
        }

        private void PositionFeedback(Vector2 screenPosition)
        {
            if (feedbackRoot == null) return;
            Vector2 position = screenPosition + new Vector2(22f,-20f);
            position.x = Mathf.Clamp(position.x, 12f, Mathf.Max(12f, Screen.width - 342f));
            position.y = Mathf.Clamp(position.y, 66f, Mathf.Max(66f, Screen.height - 12f));
            feedbackRoot.position = position;
        }

        private void ClearFeedback()
        {
            FeedbackText = null;
            if (feedbackRoot != null) feedbackRoot.gameObject.SetActive(false);
        }

        private void ResetController(bool restoreWorker = true)
        {
            if (restoreWorker && CarriedWorker != null && CarriedWorker.IsPlayerCarried)
                CarriedWorker.CancelPlayerCarryImmediate();
            ClearZoneStates();
            SetHoveredWorker(null);
            Phase = WorkerCarryPhase.Idle;
            CarriedWorker = null;
            pressedWorker = null;
            pendingDestination = null;
            pendingGroundPlacement = false;
            pendingGroundPoint = Vector3.zero;
            pendingFailureReason = null;
            HasValidGround = false;
            CurrentGroundRejectionReason = null;
            uiGesture = false;
            ClearFeedback();
        }

        private static bool PointerOverUI()
            => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        private void OnDisable()
        {
            CancelCarry(true);
        }

        private void OnDestroy()
        {
            CancelCarry(true);
            WorkerSelection.Clear();
        }
    }
}
