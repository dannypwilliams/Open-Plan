using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OpenPlan
{
    /// <summary>Starts and finishes the Checkpoint 00 public-API smoke flow from the real main menu.</summary>
    public sealed class StandaloneFoundationCheckpointMenuDriver : MonoBehaviour
    {
        public void Initialize() => StartCoroutine(Run());

        private IEnumerator Run()
        {
            Screen.SetResolution(1920, 1080, false);
            yield return new WaitForSecondsRealtime(1.2f);
            if (StandaloneFoundationCheckpointDirector.Phase == 0)
            {
                yield return StandaloneFoundationCheckpointDirector.Capture("01_Main_Menu_1920x1080.png");
                StandaloneFoundationCheckpointDirector.Record("main menu launched at 1920x1080");
                StandaloneFoundationCheckpointDirector.Phase = 1;
                GetComponent<MainMenuController>().StartStarterOffice();
                yield break;
            }

            if (StandaloneFoundationCheckpointDirector.Phase == 2)
            {
                StandaloneFoundationCheckpointDirector.Record("returned to the main menu");
                StandaloneFoundationCheckpointDirector.WriteReport();
                Application.Quit(StandaloneFoundationCheckpointDirector.Failures == 0 ? 0 : 2);
            }
        }
    }

    /// <summary>Checkpoint 00 smoke and screenshot flow. It never awards cash or mutates private gameplay state.</summary>
    public sealed class StandaloneFoundationCheckpointDirector : MonoBehaviour
    {
        public const string Argument = "-openplan-foundation-smoke";
        public const string EvidenceRootArgument = "-openplan-evidence-root";
        public static bool Requested => Array.Exists(Environment.GetCommandLineArgs(),
            value => string.Equals(value, Argument, StringComparison.OrdinalIgnoreCase));
        public static int Phase { get; set; }
        public static int Failures { get; private set; }

        private static readonly List<string> Checks = new List<string>();
        private OfficeDirector office;
        private OfficeCameraRig rig;

        public void Initialize(OfficeDirector director)
        {
            office = director;
            rig = Camera.main.GetComponent<OfficeCameraRig>();
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            Record(office.Stage == OfficeStage.StarterOffice, "new campaign selected Starter Office");
            Record(office.Workers.Count == 3, "new campaign created three original workers");
            Record(Mathf.Abs(office.Cash.CurrentCash) < .0001f, "new campaign started with exactly $0");
            Record(office.DeskCount == 3, "starter office exposed three active desks");
            office.Tutorial?.SkipTutorial();
            office.HUD?.CloseOwnedModals();

            yield return new WaitForSecondsRealtime(.8f);
            rig.Overview();
            WorkerSelection.Clear();
            yield return Capture("02_Starter_Overview_1920x1080.png");

            float overview = rig.TargetOrthographicSize;
            rig.ApplyZoom(120f);
            float oneNotch = rig.TargetOrthographicSize;
            for (int i = 1; i < 10; i++) rig.ApplyZoom(120f);
            yield return new WaitForSecondsRealtime(.5f);
            Record(overview - oneNotch > .5f, "one standard wheel notch produced a noticeable zoom change");
            Record(rig.TargetOrthographicSize <= 5.1f, "ten standard wheel notches reached close view");
            rig.Overview();

            WorkerAgent worker = FindWorker("Morgan");
            yield return WaitForPickable(worker, 8f);
            Workstation retainedDesk = worker == null ? null : worker.Desk;
            Vector3 ordinaryGround = new Vector3(-1f, 0f, .2f);
            PlacementZone ordinaryInfluence = WorkerCarryController.ResolveInfluencingZone(office.PlacementZones, ordinaryGround);
            Record(ordinaryInfluence == null && office.Layout.CanPlaceWorkerAt(ordinaryGround, out _),
                "ordinary floor point is walkable and outside activity influence");
            if (worker != null)
            {
                BeginCarry(worker, ordinaryGround, ordinaryInfluence);
                yield return Capture("03_Ordinary_Ground_Valid_1920x1080.png");
                office.CarryController.ReleaseAtGround(ordinaryGround, ordinaryInfluence);
                office.CarryController.ExternalPointerControl = false;
                yield return new WaitForSecondsRealtime(.35f);
                Record(office.LastGroundPlacementCommand != null &&
                       Vector3.Distance(office.LastGroundPlacementCommand.groundPoint, ordinaryGround) < .05f,
                    "ordinary-floor release created a ground-placement command");
                Record(worker.Desk == retainedDesk && retainedDesk != null && retainedDesk.Assigned == worker,
                    "ordinary-floor placement retained the assigned desk");
                Record(WorkerSelection.Selected == worker, "selection remained on the ground-placed worker");

                SimulationSpeedController.Instance.SetSpeed(4f);
                float resumeDeadline = Time.realtimeSinceStartup + 4f;
                while (worker.Runtime.behavior != WorkerState.ReturnToDesk &&
                       worker.Runtime.behavior != WorkerState.Work && Time.realtimeSinceStartup < resumeDeadline)
                    yield return null;
                Record(worker.Runtime.behavior == WorkerState.ReturnToDesk || worker.Runtime.behavior == WorkerState.Work,
                    "assigned worker resumed autonomous desk behavior after ground placement");
                SimulationSpeedController.Instance.SetSpeed(1f);
            }

            PlacementZone water = FindZone("starter.water.cooler");
            if (worker != null && water != null)
            {
                yield return WaitForPickable(worker, 8f);
                Vector3 influencedGround = water.PlacementPoint.position +
                    Vector3.left * (water.FootprintBounds.extents.x + .25f);
                PlacementZone influence = WorkerCarryController.ResolveInfluencingZone(office.PlacementZones, influencedGround);
                bool outsideFootprint = !water.FootprintBounds.Contains(
                    new Vector3(influencedGround.x, water.FootprintBounds.center.y, influencedGround.z));
                Record(outsideFootprint && influence == water,
                    "Water influence resolved beyond the visible footprint");
                BeginCarry(worker, influencedGround, influence);
                yield return Capture("04_Activity_Influence_1920x1080.png");
                office.CarryController.ReleaseAtGround(influencedGround, influence);
                office.CarryController.ExternalPointerControl = false;
                SimulationSpeedController.Instance.SetSpeed(4f);
                yield return WaitForState(worker, WorkerState.UseWaterCooler, 4f);
                Record(worker.Runtime.behavior == WorkerState.UseWaterCooler,
                    "proximity-influenced release entered Water activity");
                SimulationSpeedController.Instance.SetSpeed(1f);
            }

            if (worker != null)
            {
                yield return WaitForPickable(worker, 8f);
                Vector3 beforeLockedAttempt = worker.transform.position;
                Vector3 lockedGround = new Vector3(10f, 0f, 0f);
                BeginCarry(worker, lockedGround, null);
                Record(!office.CarryController.HasValidTarget &&
                       (office.CarryController.FeedbackText ?? string.Empty).Contains("LOCKED NEIGHBORING PROPERTY"),
                    "locked neighboring ground showed a useful invalid reason");
                yield return Capture("05_Locked_Ground_Invalid_1920x1080.png");
                office.CarryController.ReleaseAtGround(lockedGround);
                office.CarryController.ExternalPointerControl = false;
                yield return new WaitForSecondsRealtime(.4f);
                Record(!worker.IsPlayerCarried && Vector3.Distance(worker.transform.position, beforeLockedAttempt) < .08f,
                    "locked-ground rejection restored the worker without elevation");
            }

            WorkerSelection.Clear();
            if (water != null)
            {
                rig.FocusPoint(water.transform.position, 6.4f);
                yield return new WaitForSecondsRealtime(.7f);
                yield return Capture("06_Water_Cooler_Lighting_1920x1080.png");
            }
            PlacementZone smoking = FindZone("starter.smoke.exterior");
            if (smoking != null)
            {
                rig.FocusPoint(smoking.transform.position, 6.4f);
                yield return new WaitForSecondsRealtime(.7f);
                yield return Capture("07_Smoking_Alcove_1920x1080.png");
            }

            rig.Overview();
            WorkerSelection.Clear();
            yield return EarnUntilCheapestHire(120f);
            Record(office.Candidates.Count > 0 && office.Cash.CurrentCash >= office.Candidates[0].hiringCost,
                "real work earned the cheapest candidate cost without artificial funds");
            bool expansionWasLocked = !office.ExpansionComplete;
            int teamBefore = office.ActiveWorkerCount;
            bool hired = office.TryHire(0, out string hireReason);
            Record(hired, "pre-expansion hiring succeeded: " + hireReason);
            WorkerAgent phoneWorker = hired ? office.Workers[office.Workers.Count - 1] : null;
            Record(expansionWasLocked && office.ActiveWorkerCount == teamBefore + 1 && office.DeskCount == 3,
                "four-person team exceeded the three-desk count before expansion");

            SimulationSpeedController.Instance.SetSpeed(4f);
            if (phoneWorker != null) yield return WaitForState(phoneWorker, WorkerState.Unassigned, 5f);
            SimulationSpeedController.Instance.SetSpeed(1f);
            rig.Overview();
            WorkerSelection.Clear();
            yield return Capture("08_Four_Person_Three_Desks_1920x1080.png");

            if (phoneWorker != null)
            {
                float phoneCashBefore = office.Cash.CurrentCash;
                yield return new WaitForSecondsRealtime(.7f);
                Record(phoneWorker.IsPhoneWorking && phoneWorker.Productivity > 0f &&
                       phoneWorker.CurrentActivityLabel == "Working from phone",
                    "deskless hire entered the explicit phone-work state");
                Record(office.Cash.CurrentCash > phoneCashBefore,
                    "phone work accrued cash at effective productivity");
                WorkerSelection.Select(phoneWorker);
                rig.FocusWorker(phoneWorker, false);
                yield return new WaitForSecondsRealtime(.7f);
                yield return Capture("09_Deskless_Phone_Work_1920x1080.png");

                SimulationSpeedController.Instance.SetSpeed(0f);
                float pausedCash = office.Cash.CurrentCash;
                yield return new WaitForSecondsRealtime(.7f);
                Record(Mathf.Abs(office.Cash.CurrentCash - pausedCash) < .001f,
                    "pause stopped phone and workstation income");
                SimulationSpeedController.Instance.SetSpeed(4f);
                yield return new WaitForSecondsRealtime(.7f);
                Record(office.Cash.CurrentCash > pausedCash, "resume continued phone income");
            }

            rig.Overview();
            WorkerSelection.Clear();
            yield return new WaitForSecondsRealtime(.5f);
            Record((office.HUD.HeaderText ?? string.Empty).Contains("TEAM  4") &&
                   (office.HUD.HeaderText ?? string.Empty).Contains("DESKS  3"),
                "HUD displayed Team and Desks as separate values");
            yield return Capture("10_HUD_Team_And_Desks_1920x1080.png");

            Screen.SetResolution(1280, 720, false);
            yield return new WaitForSecondsRealtime(1f);
            yield return Capture("11_HUD_Team_And_Desks_1280x720.png");
            Screen.SetResolution(1920, 1080, false);
            yield return new WaitForSecondsRealtime(.5f);

            SimulationSpeedController.Instance.SetSpeed(1f);
            Phase = 2;
            office.ReturnToMenu();
        }

        private void BeginCarry(WorkerAgent worker, Vector3 groundPoint, PlacementZone influence)
        {
            Vector2 press = Camera.main.WorldToScreenPoint(worker.transform.position + Vector3.up * .8f);
            office.CarryController.ExternalPointerControl = true;
            bool beganGesture = office.CarryController.BeginPointerGesture(worker, press, false);
            bool beganCarry = beganGesture && office.CarryController.EvaluateCarryStart(
                press + Vector2.right * 7f, .01f, worker.transform.position);
            Record(beganCarry, "public carry gesture picked up " + worker.Definition.displayName);
            if (!beganCarry) return;
            office.CarryController.UpdateCarriedPosition(groundPoint, influence,
                Camera.main.WorldToScreenPoint(groundPoint), true);
        }

        private IEnumerator EarnUntilCheapestHire(float timeoutSeconds)
        {
            if (office.Candidates.Count == 0) yield break;
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            float nextFocus = 0f;
            int workerIndex = 0;
            SimulationSpeedController.Instance.SetSpeed(4f);
            while (office.Cash.CurrentCash < office.Candidates[0].hiringCost &&
                   Time.realtimeSinceStartup < deadline)
            {
                if (Time.realtimeSinceStartup >= nextFocus)
                {
                    WorkerAgent employee = office.Workers[workerIndex++ % office.Workers.Count];
                    if (employee != null && employee.Desk != null && employee.CanBeginPlayerCarry(out _) &&
                        employee.Desk.CanAcceptWorker(employee, out _))
                    {
                        bool began = employee.BeginPlayerCarry(out _);
                        if (began)
                        {
                            employee.SetPlayerCarryPosition(employee.Desk.PlacementPoint.position +
                                Vector3.up * WorkerCarryController.CarryLiftMeters);
                            if (!office.TryIssueWorkerCommand(employee, employee.Desk, out _, out _))
                                employee.CancelPlayerCarryImmediate();
                        }
                    }
                    nextFocus = Time.realtimeSinceStartup + 10f;
                }
                yield return null;
            }
        }

        private IEnumerator WaitForPickable(WorkerAgent worker, float seconds)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (worker != null && !worker.CanBeginPlayerCarry(out _) && Time.realtimeSinceStartup < deadline)
                yield return null;
            Record(worker != null && worker.CanBeginPlayerCarry(out _),
                worker == null ? "worker exists" : worker.Definition.displayName + " became pickable");
        }

        private IEnumerator WaitForState(WorkerAgent worker, WorkerState wanted, float seconds)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (worker != null && worker.Runtime.behavior != wanted && Time.realtimeSinceStartup < deadline)
                yield return null;
        }

        private WorkerAgent FindWorker(string displayName)
        {
            foreach (WorkerAgent worker in office.Workers)
                if (worker.Definition.displayName == displayName) return worker;
            Record(false, "found worker " + displayName);
            return null;
        }

        private PlacementZone FindZone(string stableIdentifier)
        {
            foreach (PlacementZone zone in office.PlacementZones)
                if (zone.StableIdentifier == stableIdentifier) return zone;
            Record(false, "found placement zone " + stableIdentifier);
            return null;
        }

        public static IEnumerator Capture(string fileName)
        {
            string folder = Path.Combine(EvidenceRoot(), "Screenshots");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, fileName);
            if (File.Exists(path)) File.Delete(path);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(path, 1);
            yield return new WaitForSecondsRealtime(.8f);
            Record(File.Exists(path) && new FileInfo(path).Length > 1024, "captured " + fileName);
        }

        public static void Record(string description) => Record(true, description);

        public static void Record(bool passed, string description)
        {
            Checks.Add((passed ? "PASS  " : "FAIL  ") + description);
            Debug.Log("FOUNDATION CHECKPOINT: " + (passed ? "PASS " : "FAIL ") + description);
            if (!passed) Failures++;
        }

        public static void WriteReport()
        {
            string root = EvidenceRoot();
            Directory.CreateDirectory(root);
            var lines = new List<string>
            {
                Failures == 0 ? "STATUS PASS" : "STATUS FAIL",
                "CHECKPOINT 00_Foundation",
                "CAPTURE POLICY public gameplay APIs only",
                "ARTIFICIAL FUNDS FALSE",
                "FLOW main menu -> new campaign -> $0/three workers -> zoom -> ground -> influence -> locked rejection -> live earnings -> pre-expansion hire -> phone work -> pause/resume -> menu -> quit",
                "CHECKS " + Checks.Count,
                "FAILURES " + Failures
            };
            lines.AddRange(Checks);
            File.WriteAllLines(Path.Combine(root, "FOUNDATION_SMOKE.txt"), lines);
        }

        private static string EvidenceRoot()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (string.Equals(arguments[i], EvidenceRootArgument, StringComparison.OrdinalIgnoreCase) &&
                    i + 1 < arguments.Length)
                    return Path.GetFullPath(arguments[i + 1]);
                string prefix = EvidenceRootArgument + "=";
                if (arguments[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return Path.GetFullPath(arguments[i].Substring(prefix.Length));
            }
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "CheckpointEvidence"));
        }
    }
}
