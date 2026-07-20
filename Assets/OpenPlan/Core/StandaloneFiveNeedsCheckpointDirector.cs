using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OpenPlan
{
    public sealed class StandaloneFiveNeedsCheckpointMenuDriver : MonoBehaviour
    {
        public void Initialize() => StartCoroutine(Run());

        private IEnumerator Run()
        {
            Screen.SetResolution(1920, 1080, false);
            yield return new WaitForSecondsRealtime(1f);
            if (StandaloneFiveNeedsCheckpointDirector.Phase == 0)
            {
                StandaloneFiveNeedsCheckpointDirector.Record("main menu launched");
                StandaloneFiveNeedsCheckpointDirector.Phase = 1;
                GetComponent<MainMenuController>().StartStarterOffice();
                yield break;
            }
            if (StandaloneFiveNeedsCheckpointDirector.Phase == 2)
            {
                StandaloneFiveNeedsCheckpointDirector.Record("returned to main menu and quit cleanly");
                StandaloneFiveNeedsCheckpointDirector.WriteReport();
                Application.Quit(StandaloneFiveNeedsCheckpointDirector.Failures == 0 ? 0 : 2);
            }
        }
    }

    /// <summary>Prompt 01 standalone verification using the same public placement, hiring, speed, and menu paths as a player.</summary>
    public sealed class StandaloneFiveNeedsCheckpointDirector : MonoBehaviour
    {
        public const string Argument = "-openplan-five-needs-smoke";
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
            Record(office.Workers.Count == 3 && office.DeskCount == 3, "campaign created Morgan, Alex, Sam and three active desks");
            Record(Mathf.Abs(office.Cash.CurrentCash) < .0001f, "campaign started with exactly $0");
            Record(NeedCatalog.All.Length == 5, "exactly five player-facing need definitions are active");
            office.Tutorial?.SkipTutorial();
            office.HUD?.CloseOwnedModals();
            yield return new WaitForSecondsRealtime(.8f);

            rig.Overview();
            WorkerSelection.Clear();
            yield return Capture("01_Starter_Office_Overview_1920x1080.png");

            WorkerAgent worker = office.Workers[0];
            WorkerSelection.Select(worker);
            yield return new WaitForSecondsRealtime(.3f);
            Record(office.HUD.VisibleNeedRowCount == 5, "selected employee inspector displayed five need rows");
            Record(GameObject.Find("Stress Need") == null, "Stress was not presented as a sixth need row");
            yield return Capture("02_Five_Need_Inspector_1920x1080.png");

            float[] before = Snapshot(worker.Runtime);
            SimulationSpeedController.Instance.SetSpeed(4f);
            yield return new WaitForSecondsRealtime(.7f);
            Record(AllChanged(before, worker.Runtime), "all five needs changed during active simulation");

            PlacementZone water = FindZone("starter.water.cooler");
            for (int visit = 0; visit < 4; visit++)
            {
                yield return WaitForAvailable(worker, water, 25f);
                if (!TryPlace(worker, water)) break;
                yield return WaitForState(worker, WorkerState.UseWaterCooler, 5f);
                if (visit == 0)
                {
                    rig.FocusPoint(water.transform.position, 6.0f);
                    WorkerSelection.Select(worker);
                    yield return Capture("06_Employee_Recovery_At_Water_1920x1080.png");
                }
                yield return WaitUntilNotState(worker, WorkerState.UseWaterCooler, 5f);
            }

            WorkerSelection.Select(worker);
            rig.FocusWorker(worker, false);
            yield return new WaitForSecondsRealtime(.35f);
            NeedStatus bathroomStatus = NeedCatalog.Get(NeedKind.Bathroom).Status(worker.Runtime.bathroom);
            Record(bathroomStatus >= NeedStatus.Caution, "normal Water use produced a readable Bathroom warning without direct need mutation");
            yield return Capture("03_Bathroom_Warning_State_1920x1080.png");

            PlacementZone restroom = FindZone("starter.restroom.main");
            float beforeRestroom = worker.Runtime.bathroom;
            yield return WaitForAvailable(worker, restroom, 15f);
            Vector3 influencedPoint = restroom.PlacementPoint.position + Vector3.right *
                (restroom.FootprintBounds.extents.x + .20f);
            PlacementZone influence = WorkerCarryController.ResolveInfluencingZone(office.PlacementZones, influencedPoint);
            Record(influence == restroom && !restroom.FootprintBounds.Contains(new Vector3(influencedPoint.x,
                restroom.FootprintBounds.center.y, influencedPoint.z)), "restroom proximity influence extended beyond its footprint");
            if (TryBeginCarry(worker, influencedPoint, influence))
            {
                rig.FocusPoint(restroom.transform.position, 6.0f);
                yield return Capture("04_Restroom_Proximity_Influence_1920x1080.png");
                office.CarryController.ReleaseAtGround(influencedPoint, influence);
                office.CarryController.ExternalPointerControl = false;
            }
            yield return WaitForState(worker, WorkerState.UseRestroom, 5f);
            Record(restroom.Occupancy == 1 && worker.IsVisibleInOffice, "restroom use began visibly with one reservation");
            WorkerSelection.Select(worker);
            yield return Capture("05_Employee_Using_Restroom_1920x1080.png");
            yield return WaitUntilNotState(worker, WorkerState.UseRestroom, 5f);
            Record(worker.Runtime.bathroom < beforeRestroom - .5f, "restroom completion substantially lowered Bathroom urgency");
            Record(restroom.Occupancy == 0 && worker.IsVisibleInOffice && !worker.IsPlayerCarried,
                "restroom completion released capacity and left the employee visible");

            SimulationSpeedController.Instance.SetSpeed(0f);
            float[] paused = Snapshot(worker.Runtime);
            yield return new WaitForSecondsRealtime(.8f);
            Record(Matches(paused, worker.Runtime, .000001f), "pause froze every need");
            WorkerSelection.Select(worker);
            yield return Capture("07_Paused_Stable_Needs_1920x1080.png");
            SimulationSpeedController.Instance.SetSpeed(4f);

            yield return EarnUntilCheapestHire(120f);
            Record(office.Candidates.Count > 0 && office.Cash.CurrentCash >= office.Candidates[0].hiringCost,
                "real work earned the cheapest candidate cost");
            bool hired = office.TryHire(0, out string reason);
            Record(hired, "pre-expansion deskless hire succeeded: " + reason);
            WorkerAgent phone = hired ? office.Workers[office.Workers.Count - 1] : null;
            if (phone != null)
            {
                yield return WaitForState(phone, WorkerState.Unassigned, 5f);
                float[] phoneNeeds = Snapshot(phone.Runtime);
                float cash = office.Cash.CurrentCash;
                yield return new WaitForSecondsRealtime(.7f);
                Record(phone.IsPhoneWorking && phone.CurrentActivityLabel == "Working from phone",
                    "deskless hire entered explicit phone work");
                Record(AllChanged(phoneNeeds, phone.Runtime) && office.Cash.CurrentCash > cash,
                    "phone work changed all five needs and accrued cash");
                WorkerSelection.Select(phone);
                rig.FocusWorker(phone, false);
                yield return new WaitForSecondsRealtime(.4f);
                yield return Capture("08_Deskless_Phone_Work_Five_Needs_1920x1080.png");

                Screen.SetResolution(1280, 720, false);
                yield return new WaitForSecondsRealtime(.8f);
                Record(office.HUD.VisibleNeedRowCount == 5, "five-need inspector remained present at 1280x720");
                yield return Capture("09_Inspector_Regression_1280x720.png");
                Screen.SetResolution(1920, 1080, false);
                yield return new WaitForSecondsRealtime(.5f);
            }

            SimulationSpeedController.Instance.SetSpeed(1f);
            Phase = 2;
            office.ReturnToMenu();
        }

        private bool TryPlace(WorkerAgent worker, PlacementZone zone)
        {
            if (!TryBeginCarry(worker, zone.PlacementPoint.position, zone)) return false;
            office.CarryController.ReleaseAtZone(zone);
            office.CarryController.ExternalPointerControl = false;
            return true;
        }

        private bool TryBeginCarry(WorkerAgent worker, Vector3 point, PlacementZone influence)
        {
            Vector2 press = Camera.main.WorldToScreenPoint(worker.transform.position + Vector3.up * .8f);
            office.CarryController.ExternalPointerControl = true;
            bool began = office.CarryController.BeginPointerGesture(worker, press, false) &&
                         office.CarryController.EvaluateCarryStart(press + Vector2.right * 7f, .01f, worker.transform.position);
            if (!began)
            {
                Record(false, "public carry gesture began for " + worker.Definition.displayName);
                office.CarryController.ExternalPointerControl = false;
                return false;
            }
            office.CarryController.UpdateCarriedPosition(point, influence, Camera.main.WorldToScreenPoint(point), true);
            bool valid = office.CarryController.HasValidTarget;
            Record(valid, "placement target accepted: " + (influence == null ? "ordinary ground" : influence.ActivityLabel));
            if (!valid)
            {
                office.CarryController.CancelCarry(true);
                office.CarryController.ExternalPointerControl = false;
            }
            return valid;
        }

        private IEnumerator WaitForAvailable(WorkerAgent worker, PlacementZone zone, float seconds)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (worker != null && worker.CanBeginPlayerCarry(out _) && zone.CanAcceptWorker(worker, out _)) yield break;
                yield return null;
            }
            Record(false, zone.ActivityLabel + " became available");
        }

        private IEnumerator WaitForState(WorkerAgent worker, WorkerState state, float seconds)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (worker != null && worker.Runtime.behavior != state && Time.realtimeSinceStartup < deadline) yield return null;
            Record(worker != null && worker.Runtime.behavior == state, "entered " + state);
        }

        private IEnumerator WaitUntilNotState(WorkerAgent worker, WorkerState state, float seconds)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (worker != null && worker.Runtime.behavior == state && Time.realtimeSinceStartup < deadline) yield return null;
            Record(worker != null && worker.Runtime.behavior != state, "completed " + state);
        }

        private IEnumerator EarnUntilCheapestHire(float seconds)
        {
            if (office.Candidates.Count == 0) yield break;
            float deadline = Time.realtimeSinceStartup + seconds;
            while (office.Cash.CurrentCash < office.Candidates[0].hiringCost && Time.realtimeSinceStartup < deadline)
                yield return null;
        }

        private PlacementZone FindZone(string stableIdentifier)
        {
            foreach (PlacementZone zone in office.PlacementZones)
                if (zone.StableIdentifier == stableIdentifier) return zone;
            Record(false, "found zone " + stableIdentifier);
            return null;
        }

        private static float[] Snapshot(WorkerRuntimeState state)
        {
            float[] values = new float[5];
            for (int i = 0; i < values.Length; i++) values[i] = state.GetNeed(NeedCatalog.All[i].Kind);
            return values;
        }

        private static bool AllChanged(float[] before, WorkerRuntimeState state)
        {
            for (int i = 0; i < before.Length; i++)
                if (Mathf.Abs(before[i] - state.GetNeed(NeedCatalog.All[i].Kind)) < .00001f) return false;
            return true;
        }

        private static bool Matches(float[] before, WorkerRuntimeState state, float tolerance)
        {
            for (int i = 0; i < before.Length; i++)
                if (Mathf.Abs(before[i] - state.GetNeed(NeedCatalog.All[i].Kind)) > tolerance) return false;
            return true;
        }

        public static IEnumerator Capture(string fileName)
        {
            string folder = Path.Combine(EvidenceRoot(), "captures");
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
            Debug.Log("FIVE NEEDS CHECKPOINT: " + (passed ? "PASS " : "FAIL ") + description);
            if (!passed) Failures++;
        }

        public static void WriteReport()
        {
            string root = EvidenceRoot();
            Directory.CreateDirectory(root);
            var lines = new List<string>
            {
                Failures == 0 ? "STATUS PASS" : "STATUS FAIL",
                "CHECKPOINT 01_FiveNeeds",
                "CAPTURE POLICY public gameplay APIs; seeded runtime; no direct need mutation or artificial cash",
                "FLOW main menu -> new $0 campaign -> five rows -> live changes -> four Water uses -> natural warning -> restroom influence/use/recovery -> pause -> live earnings -> deskless hire/phone work -> 1280 check -> menu -> quit",
                "CHECKS " + Checks.Count,
                "FAILURES " + Failures
            };
            lines.AddRange(Checks);
            File.WriteAllLines(Path.Combine(root, "FIVE_NEEDS_SMOKE.txt"), lines);
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
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "FiveNeedsEvidence"));
        }
    }
}
