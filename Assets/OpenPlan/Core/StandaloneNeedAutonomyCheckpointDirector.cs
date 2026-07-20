using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OpenPlan
{
    public sealed class StandaloneNeedAutonomyCheckpointMenuDriver : MonoBehaviour
    {
        public void Initialize() => StartCoroutine(Run());

        private IEnumerator Run()
        {
            Screen.SetResolution(1920, 1080, false);
            yield return new WaitForSecondsRealtime(1f);
            if (StandaloneNeedAutonomyCheckpointDirector.Phase == 0)
            {
                StandaloneNeedAutonomyCheckpointDirector.Record("main menu launched");
                StandaloneNeedAutonomyCheckpointDirector.Phase = 1;
                GetComponent<MainMenuController>().StartStarterOffice();
            }
            else if (StandaloneNeedAutonomyCheckpointDirector.Phase == 2)
            {
                StandaloneNeedAutonomyCheckpointDirector.Record("returned to main menu and quit cleanly");
                StandaloneNeedAutonomyCheckpointDirector.WriteReport();
                Application.Quit(StandaloneNeedAutonomyCheckpointDirector.Failures == 0 ? 0 : 2);
            }
        }
    }

    /// <summary>Long public-gameplay Prompt 02 smoke and genuine capture flow.</summary>
    [DefaultExecutionOrder(-900)]
    public sealed class StandaloneNeedAutonomyCheckpointDirector : MonoBehaviour
    {
        public const string Argument = "-openplan-need-autonomy-smoke";
        public const string EvidenceRootArgument = "-openplan-evidence-root";
        public static bool Requested => Array.Exists(Environment.GetCommandLineArgs(),
            value => string.Equals(value, Argument, StringComparison.OrdinalIgnoreCase));
        public static int Phase { get; set; }
        public static int Failures { get; private set; }
        private static readonly List<string> Checks = new List<string>();

        private OfficeDirector office;
        private OfficeCameraRig rig;
        private WorkerAgent commandedWorker;
        private WorkerAgent phoneWorker;
        private bool urgentCaptured;
        private bool restroomCaptured;
        private bool foodCaptured;
        private bool navigationCaptured;
        private bool demandCaptured;
        private bool deskResumeCaptured;
        private bool phoneResumeCaptured;
        private bool inspectorCaptured;
        private bool commandCaptured;
        private bool commandCapturePending;
        private bool overrideCaptured;
        private bool passiveCaptured;
        private bool overviewCaptured;
        private readonly HashSet<WorkerAgent> observedDeskRecoveries = new HashSet<WorkerAgent>();
        private bool observedPhoneRecovery;

        public void Initialize(OfficeDirector director)
        {
            office = director;
            rig = Camera.main.GetComponent<OfficeCameraRig>();
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            Record(office.Stage == OfficeStage.StarterOffice && office.ActiveWorkerCount == 3 && office.DeskCount == 3,
                "Starter Office began with three workers and three desks");
            Record(Mathf.Abs(office.Cash.CurrentCash) < .001f, "campaign began with exactly $0");
            Record(office.Navigation != null && office.Reservations != null, "navigation and reservation services are active");
            office.Tutorial?.SkipTutorial();
            office.HUD?.CloseOwnedModals();
            commandedWorker = office.Workers[0];
            SimulationSpeedController.Instance.SetSpeed(4f);
            yield return WaitForUsefulState(commandedWorker, 8f);

            PlacementZone waterZone = FindZone("starter.water.cooler");
            PlacementZone sharedRestZone = FindZone("starter.rest.break-nook");
            bool firstSharedRestIssued = false;
            bool secondSharedRestIssued = false;
            float nextCommandReal = 0f;
            float deadline = Time.realtimeSinceStartup + 285f;
            while (Time.realtimeSinceStartup < deadline && office != null && office.SimulationTime < 790f)
            {
                NeedStatus bathroom = NeedCatalog.Status(commandedWorker.Runtime, NeedKind.Bathroom);
                bool emergencyBegan = commandedWorker.Decision.IsNeedRecovery &&
                                      commandedWorker.Decision.need == NeedKind.Bathroom &&
                                      commandedWorker.Decision.category == WorkerDecisionCategory.CriticalNeed &&
                                      office.AutonomyCounters.criticalOverrides > 0;
                bool authorityGap = !commandedWorker.HasPlayerCommandAuthority &&
                                    commandedWorker.Runtime.behavior != WorkerState.UseWaterCooler;
                if (!emergencyBegan && bathroom != NeedStatus.Critical &&
                    (authorityGap || Time.realtimeSinceStartup >= nextCommandReal) &&
                    commandedWorker.CanBeginPlayerCarry(out _))
                {
                    nextCommandReal = Time.realtimeSinceStartup + 4f;
                    PlacementZone command = commandedWorker.Runtime.bathroom < .72f &&
                                            commandedWorker.Runtime.waterCooldown <= .01f && waterZone != null ?
                        waterZone : commandedWorker.Desk;
                    bool placed = TryPlace(commandedWorker, command);
                    if (placed && !commandCaptured) commandCapturePending = true;
                }

                if (commandCapturePending && commandedWorker.HasPlayerCommandAuthority &&
                    commandedWorker.Decision.playerOrigin &&
                    commandedWorker.Decision.category == WorkerDecisionCategory.PlayerCommand)
                {
                    commandCapturePending = false;
                    commandCaptured = true;
                    WorkerSelection.Select(commandedWorker);
                    rig.FocusWorker(commandedWorker, false);
                    yield return Capture("09_Player_Issued_Command_1920x1080.png");
                }

                if (phoneWorker == null && office.Candidates.Count > 0 &&
                    office.Cash.CurrentCash >= office.Candidates[0].hiringCost)
                {
                    bool hired = office.TryHire(0, out string reason);
                    Record(hired, "naturally earned cash hired a deskless employee: " + reason);
                    if (hired) phoneWorker = office.Workers[office.Workers.Count - 1];
                }

                if (!demandCaptured && office.SimulationTime >= 90f && sharedRestZone != null)
                {
                    if (!firstSharedRestIssued && office.Workers[1].CanBeginPlayerCarry(out _))
                        firstSharedRestIssued = TryPlace(office.Workers[1], sharedRestZone);
                    else if (firstSharedRestIssued && !secondSharedRestIssued &&
                             office.Workers[2].CanBeginPlayerCarry(out _))
                        secondSharedRestIssued = TryPlace(office.Workers[2], sharedRestZone);
                }

                if (!urgentCaptured && bathroom == NeedStatus.Urgent)
                {
                    urgentCaptured = true;
                    WorkerSelection.Select(commandedWorker);
                    rig.FocusWorker(commandedWorker, false);
                    yield return Capture("01_Employee_Urgent_Need_1920x1080.png");
                }

                if (!overrideCaptured && emergencyBegan)
                {
                    overrideCaptured = true;
                    WorkerSelection.Select(commandedWorker);
                    rig.FocusWorker(commandedWorker, false);
                    yield return Capture("10_Critical_Override_1920x1080.png");
                }

                if (!inspectorCaptured && commandedWorker.Decision.IsNeedRecovery)
                {
                    inspectorCaptured = true;
                    WorkerSelection.Select(commandedWorker);
                    rig.FocusWorker(commandedWorker, false);
                    yield return Capture("08_Inspector_Autonomous_Reason_1920x1080.png");
                }

                for (int i = 0; i < office.Workers.Count; i++)
                {
                    WorkerAgent worker = office.Workers[i];
                    if (worker == null || worker.IsFired) continue;
                    if (worker.Decision.IsNeedRecovery && worker.Desk != null) observedDeskRecoveries.Add(worker);
                    if (worker == phoneWorker && worker.Decision.IsNeedRecovery) observedPhoneRecovery = true;

                    if (!foodCaptured && worker.Decision.activity == PlacementActivity.BuySnack && worker.Decision.IsNeedRecovery)
                    {
                        foodCaptured = true;
                        WorkerSelection.Select(worker);
                        rig.FocusWorker(worker, false);
                        yield return Capture("03_Autonomous_Food_Trip_1920x1080.png");
                    }
                    if (!restroomCaptured && worker.Decision.activity == PlacementActivity.UseRestroom && worker.Decision.IsNeedRecovery)
                    {
                        restroomCaptured = true;
                        WorkerSelection.Select(worker);
                        rig.FocusPoint(FindZone("starter.restroom.main").transform.position, 6f);
                        yield return Capture("02_Autonomous_Restroom_Walk_1920x1080.png");
                    }
                    if (!navigationCaptured && worker.IsMoving && worker.Decision.IsNeedRecovery)
                    {
                        navigationCaptured = true;
                        WorkerSelection.Select(worker);
                        rig.FocusWorker(worker, false);
                        yield return Capture("04_Navigation_Around_Partition_1920x1080.png");
                    }
                    if (!deskResumeCaptured && observedDeskRecoveries.Contains(worker) && worker.Desk != null &&
                        worker.Runtime.behavior == WorkerState.Work && worker.Decision.category == WorkerDecisionCategory.Work)
                    {
                        deskResumeCaptured = true;
                        WorkerSelection.Select(worker);
                        rig.FocusWorker(worker, false);
                        yield return Capture("06_Desk_Work_Resumed_1920x1080.png");
                    }
                }

                if (!phoneResumeCaptured && phoneWorker != null && observedPhoneRecovery && phoneWorker.IsPhoneWorking)
                {
                    phoneResumeCaptured = true;
                    WorkerSelection.Select(phoneWorker);
                    rig.FocusWorker(phoneWorker, false);
                    yield return Capture("07_Phone_Work_Resumed_1920x1080.png");
                }

                if (!demandCaptured && TryGetSharedDemandWorker(out WorkerAgent sharedDemandWorker))
                {
                    demandCaptured = true;
                    WorkerSelection.Select(sharedDemandWorker);
                    rig.FocusPoint(FindZone("starter.rest.break-nook").transform.position, 7f);
                    yield return Capture("05_Shared_Station_Demand_1920x1080.png");
                }

                if (!passiveCaptured && office.SimulationTime >= 420f && DistinctBehaviorCount() >= 2)
                {
                    passiveCaptured = true;
                    WorkerSelection.Clear();
                    rig.Overview();
                    yield return Capture("11_Passive_Mixed_Behaviors_1920x1080.png", .85f);
                }

                if (!overviewCaptured && office.SimulationTime >= 600f)
                {
                    overviewCaptured = true;
                    WorkerSelection.Clear();
                    rig.Overview();
                    yield return Capture("12_Overview_After_Ten_Minutes_1920x1080.png", .85f);
                }
                yield return null;
            }

            Record(urgentCaptured, "captured a naturally reached urgent need");
            Record(restroomCaptured, "captured autonomous restroom navigation");
            Record(foodCaptured, "captured autonomous food navigation");
            Record(navigationCaptured, "captured obstacle-aware need travel");
            Record(demandCaptured, "captured shared station demand");
            Record(deskResumeCaptured, "captured desk work resuming after recovery");
            Record(phoneResumeCaptured, "captured deskless phone work resuming after recovery");
            Record(inspectorCaptured, "captured structured autonomous inspector reasoning");
            Record(commandCaptured, "captured a player-issued instruction");
            Record(overrideCaptured, "captured critical Bathroom overriding the lower-priority instruction");
            Record(passiveCaptured, "captured passive mixed office behavior");
            Record(overviewCaptured, "captured overview after ten simulated minutes");
            Record(office.Reservations.Count <= office.ActiveWorkerCount, "reservation count remained bounded by roster");
            Record(office.AutonomyCounters.criticalOverrides > 0, "critical override instrumentation recorded the event");
            Record(office.AutonomyCounters.emergencySafetyCorrections == 0,
                "public smoke required no safety teleport" +
                (string.IsNullOrWhiteSpace(office.AutonomyCounters.lastSafetyCorrection) ? string.Empty :
                    ": " + office.AutonomyCounters.lastSafetyCorrection));
            SimulationSpeedController.Instance.SetSpeed(1f);
            Phase = 2;
            office.ReturnToMenu();
        }

        private bool TryPlace(WorkerAgent worker, PlacementZone zone)
        {
            if (worker == null || zone == null || !worker.CanBeginPlayerCarry(out _)) return false;
            Vector2 press = Camera.main.WorldToScreenPoint(worker.transform.position + Vector3.up * .8f);
            office.CarryController.ExternalPointerControl = true;
            bool began = office.CarryController.BeginPointerGesture(worker, press, false) &&
                         office.CarryController.EvaluateCarryStart(press + Vector2.right * 7f, .01f, worker.transform.position);
            if (!began) { office.CarryController.ExternalPointerControl = false; return false; }
            office.CarryController.UpdateCarriedPosition(zone.PlacementPoint.position, zone,
                Camera.main.WorldToScreenPoint(zone.PlacementPoint.position), true);
            if (!office.CarryController.HasValidTarget)
            {
                office.CarryController.CancelCarry(true);
                office.CarryController.ExternalPointerControl = false;
                return false;
            }
            office.CarryController.ReleaseAtZone(zone);
            office.CarryController.ExternalPointerControl = false;
            return true;
        }

        private IEnumerator WaitForUsefulState(WorkerAgent worker, float seconds)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (worker != null && worker.Runtime.behavior != WorkerState.Work &&
                   worker.Runtime.behavior != WorkerState.Unassigned && Time.realtimeSinceStartup < deadline) yield return null;
            Record(worker != null && (worker.Runtime.behavior == WorkerState.Work ||
                worker.Runtime.behavior == WorkerState.Unassigned), "starting worker reached useful work");
        }

        private bool TryGetSharedDemandWorker(out WorkerAgent selected)
        {
            selected = null;
            PlacementZone rest = FindZone("starter.rest.break-nook");
            if (rest == null || rest.EffectiveUsage < 2) return false;
            for (int i = 0; i < office.Workers.Count; i++)
            {
                WorkerAgent first = office.Workers[i];
                if (first == null || first.IsFired || first.ActivePlacementZone != rest ||
                    first.Runtime.behavior != WorkerState.TakeBreak) continue;
                for (int j = i + 1; j < office.Workers.Count; j++)
                {
                    WorkerAgent second = office.Workers[j];
                    if (second == null || second.IsFired || second.ActivePlacementZone != rest ||
                        second.Runtime.behavior != WorkerState.TakeBreak) continue;
                    Vector2 separation = new Vector2(first.transform.position.x - second.transform.position.x,
                        first.transform.position.z - second.transform.position.z);
                    if (separation.sqrMagnitude < .20f) continue;
                    selected = first;
                    return true;
                }
            }
            return false;
        }

        private int DistinctBehaviorCount()
        {
            HashSet<WorkerState> states = new HashSet<WorkerState>();
            for (int i = 0; i < office.Workers.Count; i++)
                if (office.Workers[i] != null && !office.Workers[i].IsFired) states.Add(office.Workers[i].Runtime.behavior);
            return states.Count;
        }

        private PlacementZone FindZone(string id)
        {
            for (int i = 0; i < office.PlacementZones.Count; i++)
                if (office.PlacementZones[i].StableIdentifier == id) return office.PlacementZones[i];
            return null;
        }

        private static IEnumerator Capture(string fileName, float cameraSettleSeconds = .45f)
        {
            string folder = Path.Combine(EvidenceRoot(), "captures");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, fileName);
            if (File.Exists(path)) File.Delete(path);

            SimulationSpeedController speedController = SimulationSpeedController.Instance;
            float previousSpeed = speedController != null ? speedController.Speed : 1f;
            speedController?.SetSpeed(0f);
            if (cameraSettleSeconds > 0f) yield return new WaitForSecondsRealtime(cameraSettleSeconds);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(path, 1);

            float deadline = Time.realtimeSinceStartup + 2f;
            while ((!File.Exists(path) || new FileInfo(path).Length <= 1024) &&
                   Time.realtimeSinceStartup < deadline)
                yield return new WaitForSecondsRealtime(.05f);
            bool captured = File.Exists(path) && new FileInfo(path).Length > 1024;
            speedController?.SetSpeed(previousSpeed);
            Record(captured, "captured " + fileName);
        }

        public static void Record(string description) => Record(true, description);

        public static void Record(bool passed, string description)
        {
            Checks.Add((passed ? "PASS  " : "FAIL  ") + description);
            Debug.Log("NEED AUTONOMY CHECKPOINT: " + (passed ? "PASS " : "FAIL ") + description);
            if (!passed) Failures++;
        }

        public static void WriteReport()
        {
            string root = EvidenceRoot();
            Directory.CreateDirectory(root);
            List<string> lines = new List<string>
            {
                Failures == 0 ? "STATUS PASS" : "STATUS FAIL",
                "CHECKPOINT 02_NeedAutonomy",
                "CAPTURE POLICY public gameplay APIs; fixed seed; 4x public speed; no direct need mutation or artificial cash",
                "FLOW main menu -> natural work/cash -> public Water/Work instructions -> natural urgent/critical Bathroom -> explained critical override -> food/restroom/shared demand -> phone recovery -> ten-minute overview -> menu -> quit",
                "CHECKS " + Checks.Count,
                "FAILURES " + Failures
            };
            lines.AddRange(Checks);
            File.WriteAllLines(Path.Combine(root, "NEED_AUTONOMY_SMOKE.txt"), lines);
        }

        private static string EvidenceRoot()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (string.Equals(arguments[i], EvidenceRootArgument, StringComparison.OrdinalIgnoreCase) && i + 1 < arguments.Length)
                    return Path.GetFullPath(arguments[i + 1]);
                string prefix = EvidenceRootArgument + "=";
                if (arguments[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return Path.GetFullPath(arguments[i].Substring(prefix.Length));
            }
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "NeedAutonomyEvidence"));
        }
    }
}
