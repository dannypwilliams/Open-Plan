using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace OpenPlan
{
    public sealed class OfficeDirector : MonoBehaviour
    {
        public OfficeStage Stage { get; private set; }
        public OfficeAssetCatalog Catalog { get; private set; }
        public SeededRandomService Random { get; private set; }
        public TaskQueue Tasks { get; private set; }
        public EconomyDirector Economy { get; private set; }
        public CashDirector Cash { get; private set; }
        public WorkdayDirector Workday { get; private set; }
        public HiringDirector Hiring { get; private set; }
        public FiringDirector Firing { get; private set; }
        public OfficeHUDController HUD { get; private set; }
        public AudioDirector Audio { get; private set; }
        public WorkerCarryController CarryController { get; private set; }
        public WorkerCommand LastIssuedCommand { get; private set; }
        public CoffeeStation Coffee { get; private set; }
        public WaterStation Water { get; private set; }
        public NeedStation Break { get; private set; }
        public NeedStation Elevator { get; private set; }
        public OfficeStageLayout Layout { get; private set; }
        public OfficeExpansionController Expansion { get; private set; }
        public bool InputLocked { get; private set; }
        public bool IsEstablishedPreview { get; private set; }
        public bool ExpansionComplete => Stage == OfficeStage.StarterOfficeExpanded ||
                                         (Expansion != null && Expansion.IsExpanded);
        public bool CanPurchaseExpansion => Expansion != null &&
            ExpansionRules.CanPurchase(Cash.CurrentCash, Expansion.IsExpanded) && !Expansion.IsAnimating;
        public bool CanHireWorkers => Stage == OfficeStage.EstablishedOffice || ExpansionComplete;
        public float CombinedIncomePerMinute
        {
            get
            {
                float total = 0f;
                foreach (WorkerAgent worker in workers)
                    if (worker != null && !worker.IsFired) total += worker.Productivity;
                return total * CashDirector.IncomePerProductivityMinute;
            }
        }
        public IReadOnlyList<WorkerAgent> Workers => workers;
        public IReadOnlyList<Workstation> Workstations => workstations;
        public IReadOnlyList<PlacementZone> PlacementZones => placementZones;
        public IReadOnlyList<CandidateDefinition> Candidates => candidates;
        public Vector3 ExitOutsidePoint => Elevator == null ? Vector3.zero : Elevator.UsePoint.position +
            (Stage == OfficeStage.EstablishedOffice ? Vector3.forward * 2.1f : Vector3.forward * 2.0f);
        public Vector3 EntranceInsidePoint => Elevator == null ? Vector3.zero : Elevator.UsePoint.position +
            (Stage == OfficeStage.EstablishedOffice ? Vector3.back * .8f : Vector3.back * 1.25f);
        public int WorkerCapacity
        {
            get
            {
                int count = 0;
                foreach (Workstation workstation in workstations)
                    if (workstation != null && workstation.IsZoneEnabled) count++;
                return count;
            }
        }
        public int ActiveWorkerCount { get { int count = 0; foreach (WorkerAgent worker in workers) if (worker != null && !worker.IsFired) count++; return count; } }
        public int Hires { get; private set; }
        public int Firings { get; private set; }
        public bool OverlayEnabled { get; private set; }
        public bool Reassigning { get; private set; }
        public event Action RosterChanged;
        public event Action<string> Notice;
        public event Action<WorkerCommand> WorkerCommandIssued;

        private readonly List<WorkerAgent> workers = new List<WorkerAgent>();
        private readonly List<Workstation> workstations = new List<Workstation>();
        private readonly List<PlacementZone> placementZones = new List<PlacementZone>();
        private readonly List<CandidateDefinition> candidates = new List<CandidateDefinition>();
        private int candidateSerial;
        private float overlayTick;

        private void Awake()
        {
            Stage = OfficeStageSelection.ConsumeForOffice();
            IsEstablishedPreview = OfficeStageSelection.ConsumePreviewForOffice();
            Catalog = Resources.Load<OfficeAssetCatalog>("OpenPlanAssetCatalog");
            if (Catalog == null) { Debug.LogError("OPEN PLAN asset catalog missing. Run the release pipeline."); enabled = false; return; }
            Random = new SeededRandomService(19680412);
            Tasks = GetComponent<TaskQueue>() ?? gameObject.AddComponent<TaskQueue>();
            Economy = GetComponent<EconomyDirector>() ?? gameObject.AddComponent<EconomyDirector>();
            Cash = GetComponent<CashDirector>() ?? gameObject.AddComponent<CashDirector>();
            Workday = GetComponent<WorkdayDirector>() ?? gameObject.AddComponent<WorkdayDirector>();
            Hiring = GetComponent<HiringDirector>() ?? gameObject.AddComponent<HiringDirector>();
            Firing = GetComponent<FiringDirector>() ?? gameObject.AddComponent<FiringDirector>();
        }

        private void Start()
        {
            Transform world = new GameObject(Stage + " Environment").transform;
            IOfficeEnvironmentBuilder environment = CreateEnvironment(world);
            environment.Build();
            workstations.AddRange(environment.Workstations);
            placementZones.AddRange(environment.PlacementZones);
            Coffee = environment.Coffee;
            Water = environment.Water;
            Break = environment.Break;
            Elevator = environment.Elevator;
            Layout = environment.Layout;
            Tasks.Initialize(Random);
            Economy.Initialize(Tasks);
            Cash.Initialize();
            Workday.Initialize(this, Economy, Tasks);
            Hiring.Initialize(this);
            Firing.Initialize(this);
            BuildCandidates();
            SpawnStartingRoster();
            Economy.RecalculatePayroll(workers);
            HUD = gameObject.AddComponent<OfficeHUDController>();
            HUD.Initialize(this);
            Audio = gameObject.AddComponent<AudioDirector>();
            Audio.Initialize(this);
            EnsureCamera();
            CarryController = gameObject.AddComponent<WorkerCarryController>();
            CarryController.Initialize(this, Camera.main.GetComponent<OfficeCameraRig>(), HUD, Audio);
            Expansion = world.GetComponent<OfficeExpansionController>();
            Expansion?.Initialize(this);
            if (StandaloneInputSmokeDirector.Requested)
                gameObject.AddComponent<StandaloneInputSmokeDirector>().Initialize(this);
            if (StandaloneActivityCycleDirector.Requested)
                gameObject.AddComponent<StandaloneActivityCycleDirector>().Initialize(this);
            if (StandaloneBehaviorSoakDirector.Requested)
                gameObject.AddComponent<StandaloneBehaviorSoakDirector>().Initialize(this);
            if (StandaloneExpansionCaptureDirector.Requested)
                gameObject.AddComponent<StandaloneExpansionCaptureDirector>().Initialize(this);
            if (AutomatedCaptureDirector.Requested)
                gameObject.AddComponent<AutomatedCaptureDirector>().Initialize(this);
            else if (AutomatedVideoDirector.Requested)
                gameObject.AddComponent<AutomatedVideoDirector>().Initialize(this);
            if (PackageVerificationDirector.Requested)
                gameObject.AddComponent<PackageVerificationDirector>().Initialize(this);
            if (AutomatedPerformanceDirector.Requested)
                gameObject.AddComponent<AutomatedPerformanceDirector>().Initialize();
        }

        private IOfficeEnvironmentBuilder CreateEnvironment(Transform world)
        {
            if (Stage == OfficeStage.EstablishedOffice)
                return new OfficeEnvironmentBuilder(Catalog, world);
            return new StarterOfficeEnvironmentBuilder(Catalog, world, Stage == OfficeStage.StarterOfficeExpanded);
        }

        private void EnsureCamera()
        {
            if (Camera.main != null)
            {
                OfficeCameraRig existingRig = Camera.main.GetComponent<OfficeCameraRig>() ?? Camera.main.gameObject.AddComponent<OfficeCameraRig>();
                existingRig.Initialize(this);
                return;
            }
            GameObject cameraObject = new GameObject("Office Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = Stage == OfficeStage.EstablishedOffice ? 17.8f : 11.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.035f,.018f,.015f);
            cameraObject.AddComponent<OfficeCameraRig>().Initialize(this);
        }

        private void Update()
        {
            if (InputLocked) return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.tabKey.wasPressedThisFrame) ToggleOverlay();
                if (keyboard.deleteKey.wasPressedThisFrame && WorkerSelection.Selected != null)
                    Notice?.Invoke("Use FIRE in the employee card to confirm termination.");
            }
            if (OverlayEnabled && (overlayTick -= Time.unscaledDeltaTime) <= 0f)
            {
                overlayTick = .18f;
                UpdateOverlay();
            }
        }

        private void SpawnStartingRoster()
        {
            if (Stage != OfficeStage.EstablishedOffice)
            {
                WorkerDefinition[] starterDefinitions =
                {
                    Def("Morgan", WorkerTrait.Hardworking, 1.34f, 390, .32f, "Prefers focused work and quiet rest", "Noise quickly raises stress", new Color(.90f,.22f,.18f)),
                    Def("Alex", WorkerTrait.Social, .98f, 235, .92f, "Raises nearby mood", "Starts long conversations", new Color(.12f,.72f,.68f)),
                    Def("Sam", WorkerTrait.Lazy, .72f, 105, .44f, "Very low payroll cost", "Takes frequent breaks", new Color(.42f,.60f,.74f))
                };
                for (int i = 0; i < starterDefinitions.Length; i++)
                    SpawnWorker(starterDefinitions[i], workstations[i], Elevator.UsePoint.position + new Vector3(i * .22f, 0f, 0f));
                return;
            }

            WorkerDefinition[] definitions =
            {
                Def("Morgan", WorkerTrait.Hardworking, 1.34f, 390, .32f, "Prefers focused work and quiet rest", "Noise quickly raises stress", new Color(.90f,.22f,.18f)),
                Def("Alex", WorkerTrait.Social, .98f, 235, .92f, "Raises nearby mood", "Starts long conversations", new Color(.12f,.72f,.68f)),
                Def("Casey", WorkerTrait.Focused, .88f, 165, .30f, "Reliable deep work", "Dislikes loud clusters", new Color(.95f,.64f,.14f)),
                Def("Jordan", WorkerTrait.Caffeinated, 1.02f, 240, .48f, "Strong post-coffee sprint", "Creates coffee traffic", new Color(.35f,.82f,.58f)),
                Def("Taylor", WorkerTrait.Ambitious, 1.28f, 365, .52f, "Thrives beside strong peers", "Mood follows company results", new Color(.55f,.22f,.42f)),
                Def("Sam", WorkerTrait.Lazy, .72f, 105, .44f, "Very low payroll cost", "Takes frequent breaks", new Color(.42f,.60f,.74f))
            };
            int[] deskIndices = { 0, 6, 4, 3, 5, 7 };
            for (int i = 0; i < definitions.Length; i++) SpawnWorker(definitions[i], workstations[deskIndices[i]], Elevator.UsePoint.position + new Vector3(i * .22f, 0f, 0f));
        }

        private WorkerAgent SpawnWorker(WorkerDefinition definition, Workstation desk, Vector3 spawn)
        {
            GameObject workerObject = Catalog.Spawn("Worker", transform, spawn, Quaternion.identity, Vector3.one);
            workerObject.name = "Worker_" + definition.displayName;
            WorkerAgent agent = workerObject.AddComponent<WorkerAgent>();
            desk?.Assign(agent);
            agent.Initialize(this, definition, desk, spawn);
            workers.Add(agent);
            return agent;
        }

        private void BuildCandidates()
        {
            candidates.Clear();
            candidates.Add(Candidate(Def("Riley", WorkerTrait.Focused, .94f, 175, .28f, "Dependable and quiet", "Not exceptional at rush work", new Color(.92f,.48f,.24f)), 380));
            candidates.Add(Candidate(Def("Cameron", WorkerTrait.Social, 1.05f, 245, .94f, "Excellent mood influence", "Can create a social pile-up", new Color(.26f,.72f,.78f)), 520));
            candidates.Add(Candidate(Def("Avery", WorkerTrait.Anxious, 1.38f, 405, .35f, "Outstanding potential output", "Needs a genuinely quiet seat", new Color(.74f,.28f,.38f)), 690));
            candidateSerial = 0;
        }

        public bool TryHire(int candidateIndex, out string reason)
        {
            reason = null;
            if (candidateIndex < 0 || candidateIndex >= candidates.Count) { reason = "Candidate is no longer available."; return false; }
            if (!CanHireWorkers) { reason = "Purchase the neighboring unit to unlock hiring."; return false; }
            if (ActiveWorkerCount >= WorkerCapacity) { reason = $"Office capacity reached ({WorkerCapacity})."; return false; }
            Workstation desk = FindAvailableDesk();
            if (desk == null) { reason = "No available desk."; return false; }
            CandidateDefinition candidate = candidates[candidateIndex];
            bool starter = Stage != OfficeStage.EstablishedOffice;
            if (starter)
            {
                if (!Cash.TrySpend(candidate.hiringCost)) { reason = $"Need ${candidate.hiringCost:N0} cash to hire."; return false; }
                SpawnWorker(candidate.worker, null, Elevator.UsePoint.position);
            }
            else
            {
                if (!Economy.PayHiring(candidate.hiringCost)) { reason = $"Need ${candidate.hiringCost:N0} cash to hire."; return false; }
                SpawnWorker(candidate.worker, desk, Elevator.UsePoint.position);
            }
            Hires++;
            candidates[candidateIndex] = NextCandidate();
            Economy.RecalculatePayroll(workers);
            RosterChanged?.Invoke();
            Notice?.Invoke(starter ? $"{candidate.worker.displayName} hired — drag them from the entrance to an open desk." :
                $"{candidate.worker.displayName} hired — heading to desk {desk.Index + 1}.");
            return true;
        }

        public bool TryFire(WorkerAgent worker, out string reason)
        {
            reason = null;
            if (worker == null || worker.IsFired) { reason = "Select an active employee."; return false; }
            CarryController?.CancelIfWorker(worker);
            const int severance = 110;
            Economy.PayFiring(severance);
            worker.Fire();
            Firings++;
            bool relief = worker.Definition.trait == WorkerTrait.Social && worker.Runtime.socialSeconds > 18f;
            foreach (WorkerAgent other in workers)
                if (other != null && other != worker && !other.IsFired && Vector3.Distance(other.transform.position, worker.transform.position) < 5.5f)
                    other.ReactToFiring(relief);
            Economy.RecalculatePayroll(workers);
            RosterChanged?.Invoke();
            Notice?.Invoke($"{worker.Definition.displayName} is packing a tiny box. Severance: ${severance}.");
            return true;
        }

        public void CompleteFiring(WorkerAgent worker)
        {
            CarryController?.CancelIfWorker(worker);
            ReleaseTransientPlacement(worker);
            if (WorkerSelection.Selected == worker) WorkerSelection.Clear();
            workers.Remove(worker);
            Destroy(worker.gameObject);
            Economy.RecalculatePayroll(workers);
            RosterChanged?.Invoke();
        }

        public void BeginReassign()
        {
            if (WorkerSelection.Selected == null) { Notice?.Invoke("Select a worker first."); return; }
            Reassigning = true;
            foreach (Workstation desk in workstations) desk.SetHighlight(desk.IsAvailable || desk.Assigned == WorkerSelection.Selected, new Color(.15f,.88f,.72f));
            Notice?.Invoke("Choose a glowing available desk.");
        }

        public bool ReassignSelected(Workstation destination)
        {
            WorkerAgent worker = WorkerSelection.Selected;
            if (!Reassigning || worker == null || destination == null) return false;
            if (!destination.IsAvailable && destination.Assigned != worker) { Notice?.Invoke("That desk is occupied."); return false; }
            if (!destination.CanAcceptWorker(worker, out string validationReason))
            {
                Notice?.Invoke(validationReason);
                return false;
            }
            Workstation previous = worker.Desk;
            if (previous != null && previous != destination) previous.Release(worker);
            destination.Assign(worker);
            Reassigning = false;
            foreach (Workstation desk in workstations) desk.SetHighlight(false, Color.black);
            Notice?.Invoke($"{worker.Definition.displayName} reassigned to {destination.ZoneLabel}.");
            return true;
        }

        public bool TryIssueWorkerCommand(WorkerAgent worker, PlacementZone destination,
            out WorkerCommand command, out string reason)
        {
            command = null;
            if (worker == null || !workers.Contains(worker)) { reason = "Worker is not part of this office."; return false; }
            if (!worker.IsPlayerCarried) { reason = "Worker is not being carried."; return false; }
            if (destination == null) { reason = "Drop on a marked activity area."; return false; }
            if (!destination.CanAcceptWorker(worker, out reason)) return false;

            PlacementZone previousActivity = worker.ActivePlacementZone;
            if (destination is Workstation destinationDesk)
            {
                Workstation previousDesk = worker.Desk;
                if (previousDesk != null && previousDesk != destinationDesk) previousDesk.Release(worker);
                destinationDesk.Assign(worker);
                if (destinationDesk.Assigned != worker)
                {
                    previousDesk?.Assign(worker);
                    reason = "Desk occupied.";
                    return false;
                }
            }
            else if (!destination.TryOccupy(worker, out reason))
            {
                return false;
            }

            if (previousActivity != null && previousActivity != destination && previousActivity is not Workstation)
                previousActivity.Vacate(worker);
            worker.SetActivePlacementZone(destination);
            command = new WorkerCommand(worker, destination, destination.Activity, Time.time, true);
            if (!worker.CommitPlayerCommand(command))
            {
                if (destination is not Workstation) destination.Vacate(worker);
                reason = "Worker could not accept the placement command.";
                return false;
            }

            LastIssuedCommand = command;
            WorkerCommandIssued?.Invoke(command);
            reason = null;
            return true;
        }

        public void ReleaseTransientPlacement(WorkerAgent worker)
        {
            if (worker == null) return;
            PlacementZone current = worker.ActivePlacementZone;
            if (current != null && current is not Workstation) current.Vacate(worker);
            worker.SetActivePlacementZone(worker.Desk);
        }

        public void ShowNotice(string message) => Notice?.Invoke(message);

        public bool TryPurchaseExpansion(out string reason)
            => Expansion != null ? Expansion.TryPurchase(out reason) : FailExpansion(out reason);

        private static bool FailExpansion(out string reason)
        {
            reason = "The neighboring unit is unavailable.";
            return false;
        }

        public void SetInputLocked(bool locked) => InputLocked = locked;

        public void MarkExpansionComplete()
        {
            if (Stage != OfficeStage.EstablishedOffice) Stage = OfficeStage.StarterOfficeExpanded;
            RosterChanged?.Invoke();
        }

        public void VisitEstablishedOfficePreview()
        {
            CarryController?.CancelCarry(true);
            Time.timeScale = 1f;
            OfficeStageSelection.SelectEstablishedPreview();
            SceneManager.LoadScene("Office");
        }

        public void ReturnFromPreviewToMenu()
        {
            CarryController?.CancelCarry(true);
            Time.timeScale = 1f;
            OfficeStageSelection.ClearPendingSelection();
            SceneManager.LoadScene("MainMenu");
        }

        public WorkerAgent FindSocialPartner(WorkerAgent asker)
        {
            WorkerAgent best = null;
            float bestScore = float.MaxValue;
            foreach (WorkerAgent worker in workers)
            {
                if (worker == null || worker == asker || worker.IsFired || worker.IsAway || worker.IsPlayerCarried ||
                    worker.HasPlayerCommandAuthority || worker.Runtime.behavior == WorkerState.Socialize ||
                    worker.Runtime.behavior == WorkerState.CarryBox) continue;
                float score = Vector3.SqrMagnitude(worker.transform.position - asker.transform.position) + Random.Range(0f, 4f);
                if (score < bestScore) { best = worker; bestScore = score; }
            }
            return best;
        }

        public WorkerAgent FindWorkerNear(WorkerAgent asker, Vector3 point, float radius)
        {
            WorkerAgent best = null;
            float bestDistance = radius * radius;
            foreach (WorkerAgent worker in workers)
            {
                if (worker == null || worker == asker || worker.IsFired || worker.IsAway) continue;
                float distance = (worker.transform.position - point).sqrMagnitude;
                if (distance > bestDistance) continue;
                bestDistance = distance;
                best = worker;
            }
            return best;
        }

        public float ComputeNearbyModifier(WorkerAgent worker, out string positive, out string negative)
        {
            positive = null;
            negative = null;
            float modifier = 1f;
            foreach (WorkerAgent other in workers)
            {
                if (other == null || other == worker || other.IsFired) continue;
                float distance = Vector3.Distance(worker.transform.position, other.transform.position);
                if (distance > 4.2f) continue;
                if (other.Runtime.behavior == WorkerState.Socialize)
                {
                    float penalty = worker.Definition.trait == WorkerTrait.Focused ? .025f :
                        worker.Definition.trait == WorkerTrait.Hardworking || worker.Definition.trait == WorkerTrait.Anxious ? .10f : .06f;
                    modifier -= penalty;
                    negative = "Distracted by conversation";
                    if (other.Definition.trait == WorkerTrait.Social)
                    {
                        worker.Runtime.mood = Mathf.Clamp01(worker.Runtime.mood + Time.deltaTime * .0030f);
                        worker.Runtime.stress = Mathf.Clamp01(worker.Runtime.stress - Time.deltaTime * .0008f);
                        positive = $"Cheered by {other.Definition.displayName}'s conversation";
                        modifier += .025f;
                    }
                }
                if (other.Definition.trait == WorkerTrait.Focused && other.Runtime.effectiveProductivity > 1f)
                {
                    modifier += worker.Definition.trait == WorkerTrait.Lazy ? .055f : .022f;
                    positive = $"Focused coworker {other.Definition.displayName}";
                }
                if (worker.Definition.trait == WorkerTrait.Ambitious && other.Runtime.effectiveProductivity > 1.15f)
                {
                    modifier += .045f;
                    positive = $"Inspired by {other.Definition.displayName}";
                }
            }
            return Mathf.Clamp(modifier, .82f, 1.18f);
        }

        public bool TryReserveAutonomousZone(WorkerAgent worker, PlacementActivity activity,
            PlacementZone avoid, out PlacementZone reserved)
        {
            reserved = null;
            for (int pass = 0; pass < 2; pass++)
            {
                foreach (PlacementZone zone in placementZones)
                {
                    if (zone == null || zone is Workstation || zone.Activity != activity) continue;
                    if (pass == 0 && zone == avoid) continue;
                    if (pass == 1 && zone != avoid) continue;
                    if (!zone.TryOccupy(worker, out _)) continue;
                    reserved = zone;
                    return true;
                }
            }
            return false;
        }

        public Vector3 GetWanderPoint(WorkerAgent worker)
        {
            Bounds bounds = Layout != null ? Layout.OverviewBounds : new Bounds(Vector3.zero, new Vector3(12f, 1f, 8f));
            Vector3 extents = bounds.extents * .72f;
            Vector3 point = bounds.center + new Vector3(Random.Range(-extents.x, extents.x), 0f,
                Random.Range(-extents.z, extents.z));
            point.y = worker == null ? 0f : worker.transform.position.y;
            return point;
        }

        public void ToggleOverlay()
        {
            OverlayEnabled = !OverlayEnabled;
            if (!OverlayEnabled) foreach (Workstation desk in workstations) desk.SetHighlight(false, Color.black);
            else UpdateOverlay();
            Notice?.Invoke(OverlayEnabled ? "Productivity overlay on." : "Productivity overlay off.");
        }

        private void UpdateOverlay()
        {
            foreach (Workstation desk in workstations)
            {
                if (Reassigning) continue;
                float productivity = desk.Assigned != null ? desk.Assigned.Productivity : 0f;
                Color color = desk.Assigned == null ? new Color(.30f,.31f,.32f) :
                    desk.Assigned.Runtime.behavior == WorkerState.Socialize ? new Color(.95f,.28f,.20f) :
                    desk.Assigned.Runtime.energy < .35f ? new Color(.28f,.46f,.62f) :
                    productivity > 1.1f ? new Color(.45f,.86f,.48f) : new Color(.95f,.72f,.28f);
                desk.SetHighlight(true, color);
            }
        }

        private Workstation FindAvailableDesk()
        {
            foreach (Workstation desk in workstations) if (desk.IsAvailable) return desk;
            return null;
        }

        private CandidateDefinition NextCandidate()
        {
            string[] names = { "Quinn", "Drew", "Blair", "Rowan", "Jamie", "Robin" };
            WorkerTrait trait = (WorkerTrait)(candidateSerial % 6);
            string name = names[candidateSerial % names.Length];
            candidateSerial++;
            float skill = Random.Range(.78f, 1.34f);
            int salary = Mathf.RoundToInt(130 + skill * 175 + (trait == WorkerTrait.Anxious ? 55 : 0));
            Color color = Color.HSVToRGB(Random.Value(), .56f, .88f);
            return Candidate(Def(name, trait, skill, salary, Random.Range(.25f,.85f),
                trait + " placement upside", trait == WorkerTrait.Social ? "Conversation risk" : "Needs the right desk", color), Mathf.RoundToInt(220 + salary * 1.15f));
        }

        private static WorkerDefinition Def(string name, WorkerTrait trait, float skill, int salary, float social, string strength, string weakness, Color clothing)
            => new WorkerDefinition { displayName = name, trait = trait, skill = skill, salary = salary, sociability = social, strength = strength, weakness = weakness, clothing = clothing };
        private static CandidateDefinition Candidate(WorkerDefinition worker, int cost) => new CandidateDefinition { worker = worker, hiringCost = cost };

        public void Restart()
        {
            CarryController?.CancelCarry(true);
            Time.timeScale = 1f;
            if (IsEstablishedPreview) OfficeStageSelection.SelectEstablishedPreview();
            else OfficeStageSelection.SelectForNextLoad(Stage);
            SceneManager.LoadScene("Office");
        }

        public void ReturnToMenu()
        {
            CarryController?.CancelCarry(true);
            Time.timeScale = 1f;
            OfficeStageSelection.ClearPendingSelection();
            SceneManager.LoadScene("MainMenu");
        }
    }
}
