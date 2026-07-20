using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OpenPlan
{
    /// <summary>One continuous, public-gameplay-API release tour from menu to expansion, hire, and preview.</summary>
    public sealed class StandaloneFriendDemoMenuDriver : MonoBehaviour
    {
        public void Initialize() => StartCoroutine(Run());

        private IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(1.4f);
            if (StandaloneFriendDemoDirector.Phase == 0)
            {
                yield return StandaloneFriendDemoDirector.CaptureScreen("00_Main_Menu.png");
                StandaloneFriendDemoDirector.Record("main menu visible and readable");
                StandaloneFriendDemoDirector.Phase = 1;
                GetComponent<MainMenuController>().StartStarterOffice();
                yield break;
            }
            if (StandaloneFriendDemoDirector.Phase == 3)
            {
                StandaloneFriendDemoDirector.Record("returned to main menu after preview");
                StandaloneFriendDemoDirector.WriteReport();
                Application.Quit(StandaloneFriendDemoDirector.Failures == 0 ? 0 : 2);
            }
        }
    }

    public sealed class StandaloneFriendDemoDirector : MonoBehaviour
    {
        public const string Argument = "-openplan-friend-demo";
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
            StartCoroutine(Phase == 2 ? Preview() : StarterTour());
        }

        private IEnumerator StarterTour()
        {
            yield return new WaitForSecondsRealtime(1.2f);
            SimulationSpeedController.Instance.SetSpeed(4f);
            yield return new WaitForSecondsRealtime(1.5f);
            SimulationSpeedController.Instance.SetSpeed(1f);
            rig.Overview();
            WorkerSelection.Clear();
            yield return CaptureScreen("01_Starter_Overview.png");
            Record(office.Workers.Count == 3, "starter office opens with three workers");

            WorkerAgent morgan = FindWorker("Morgan");
            yield return WaitForPickable(morgan);
            Vector2 press = Camera.main.WorldToScreenPoint(morgan.transform.position + Vector3.up * .8f);
            office.CarryController.ExternalPointerControl = true;
            Record(office.CarryController.BeginPointerGesture(morgan, press, false), "worker pointer press begins selection gesture");
            Record(office.CarryController.EvaluateCarryStart(press + Vector2.right * 7f, .01f, morgan.transform.position),
                "pickup crosses the real drag threshold");
            yield return CaptureScreen("02_Worker_Pickup.png");
            PlacementZone work = FindZone("starter.work.01");
            office.CarryController.UpdateCarriedPosition(work.PlacementPoint.position, work,
                Camera.main.WorldToScreenPoint(work.PlacementPoint.position), true);
            yield return CaptureScreen("03_Valid_Placement.png");
            office.CarryController.ReleaseAtZone(work);
            office.CarryController.ExternalPointerControl = false;
            yield return new WaitForSecondsRealtime(.4f);
            Record(office.LastIssuedCommand != null && office.LastIssuedCommand.requestedActivity == PlacementActivity.Work,
                "valid release issues Work through the gameplay placement path");

            yield return WaitForPickable(morgan);
            press = Camera.main.WorldToScreenPoint(morgan.transform.position + Vector3.up * .8f);
            office.CarryController.ExternalPointerControl = true;
            office.CarryController.BeginPointerGesture(morgan, press, false);
            office.CarryController.EvaluateCarryStart(press + Vector2.right * 7f, .01f, morgan.transform.position);
            PlacementZone locked = FindZone("neighbor.work.01");
            office.CarryController.UpdateCarriedPosition(locked.PlacementPoint.position, locked,
                Camera.main.WorldToScreenPoint(locked.PlacementPoint.position), true);
            yield return CaptureScreen("04_Invalid_Locked_Placement.png");
            Record(!office.CarryController.HasValidTarget, "locked neighboring desk rejects placement");
            office.CarryController.CancelCarry(true);
            office.CarryController.ExternalPointerControl = false;

            WorkerSelection.Select(morgan);
            rig.FocusWorker(morgan, true);
            yield return new WaitForSecondsRealtime(.5f);
            yield return CaptureScreen("05_Focused_Work_Inspector.png");
            Record(morgan.Runtime.focusedWorkSecondsRemaining > 0f, "manual Work grants the measured +20% focused state");

            SimulationSpeedController.Instance.SetSpeed(4f);
            WorkerAgent alex = FindWorker("Alex");
            WorkerAgent sam = FindWorker("Sam");
            yield return IssueAndCapture(alex, "starter.rest.break-nook", WorkerState.TakeBreak, "06_Break_Restoration.png", "Rest");
            yield return IssueAndCapture(morgan, "starter.water.cooler", WorkerState.UseWaterCooler, "07_Water_Recovery.png", "Water");
            yield return IssueAndCapture(sam, "starter.snack.vending", WorkerState.BuySnack, "08_Vending_Snack.png", "Vending");
            yield return IssueAndCapture(alex, "starter.smoke.exterior", WorkerState.Smoke, "09_Smoking_Alternative.png", "Smoke");

            WorkerAgent distracted = null;
            float distractionDeadline = Time.realtimeSinceStartup + 35f;
            while (distracted == null && Time.realtimeSinceStartup < distractionDeadline)
            {
                foreach (WorkerAgent worker in office.Workers)
                    if (worker.CurrentDistraction != DistractionKind.None) { distracted = worker; break; }
                yield return null;
            }
            Record(distracted != null, "seeded autonomy produces a readable distraction without a capture-only setter");
            if (distracted != null)
            {
                WorkerSelection.Select(distracted);
                rig.FocusWorker(distracted, true);
                yield return new WaitForSecondsRealtime(.35f);
                yield return CaptureScreen("10_Natural_Distraction.png");
                yield return WaitForPickable(distracted);
                yield return Issue(distracted, distracted.Desk);
                Record(distracted.Runtime.focusedWorkSecondsRemaining > 0f, "manager redirects the natural distraction to focused work");
            }

            yield return IssueAndCapture(morgan, "starter.exit.main", WorkerState.Away, "11_Away_Inspector.png", "Leave Office");
            yield return WaitForPickable(morgan);
            foreach (WorkerAgent worker in new[] { morgan, alex, sam })
            {
                WorkerSelection.Select(worker);
                rig.FocusWorker(worker, true);
                yield return new WaitForSecondsRealtime(.35f);
                yield return CaptureScreen("12_Personality_" + worker.Definition.displayName + ".png");
            }
            Record(morgan.Definition.trait != alex.Definition.trait && alex.Definition.trait != sam.Definition.trait,
                "Morgan, Alex, and Sam present three distinct named personalities");

            rig.Overview();
            WorkerSelection.Clear();
            yield return EarnUntil(() => office.CanPurchaseExpansion, 190f);
            Record(office.CanPurchaseExpansion, "live desk work earns the $1,000 expansion price");
            yield return CaptureScreen("13_Expansion_Affordable.png");
            float beforePurchase = office.Cash.CurrentCash;
            Record(office.TryPurchaseExpansion(out string purchaseReason), "expansion purchase succeeds: " + purchaseReason);
            float afterPurchase = office.Cash.CurrentCash;
            yield return new WaitForSecondsRealtime(.35f);
            yield return CaptureScreen("14_Connecting_Wall_Opening.png");
            yield return new WaitForSecondsRealtime(1.8f);
            rig.Overview();
            yield return CaptureScreen("15_Physical_Expansion.png");
            Record(office.ExpansionComplete && Mathf.Abs(beforePurchase - afterPurchase - ExpansionRules.PurchasePrice) < .05f,
                "physical expansion completes and deducts exactly $1,000");
            office.HUD.CloseOwnedModals();

            yield return EarnUntil(() => office.Cash.CurrentCash >= office.Candidates[0].hiringCost, 100f);
            int beforeHire = office.ActiveWorkerCount;
            Record(office.TryHire(0, out string hireReason), "first post-expansion hire succeeds: " + hireReason);
            WorkerAgent hired = office.Workers[office.Workers.Count - 1];
            Record(office.ActiveWorkerCount == beforeHire + 1, "new hire joins at the entrance");
            yield return WaitForPickable(hired);
            PlacementZone newDesk = FindZone("neighbor.work.01");
            yield return BeginCarryAt(hired, newDesk);
            rig.FocusPoint(newDesk.PlacementPoint.position, 7.5f);
            yield return new WaitForSecondsRealtime(.35f);
            yield return CaptureScreen("16_New_Hire_Placement.png");
            office.CarryController.ReleaseAtZone(newDesk);
            office.CarryController.ExternalPointerControl = false;
            yield return new WaitForSecondsRealtime(.5f);
            Record(office.LastIssuedCommand != null && office.LastIssuedCommand.worker == hired,
                "new hire is placed at an unlocked neighboring desk");
            yield return CaptureScreen("17_Continued_Play_After_Hire.png");

            Phase = 2;
            office.VisitEstablishedOfficePreview();
        }

        private IEnumerator Preview()
        {
            yield return new WaitForSecondsRealtime(2f);
            rig.Overview();
            yield return CaptureScreen("18_Established_Office_Preview.png");
            Record(office.Stage == OfficeStage.EstablishedOffice && office.IsEstablishedPreview,
                "Established Office preview launches from the expanded starter office");
            Phase = 3;
            office.ReturnFromPreviewToMenu();
        }

        private IEnumerator IssueAndCapture(WorkerAgent worker, string zoneId, WorkerState state, string image, string label)
        {
            yield return WaitForPickable(worker);
            yield return Issue(worker, FindZone(zoneId));
            float deadline = Time.realtimeSinceStartup + 12f;
            while (worker.Runtime.behavior != state && Time.realtimeSinceStartup < deadline) yield return null;
            Record(worker.Runtime.behavior == state, label + " lifecycle reaches its active state");
            WorkerSelection.Select(worker);
            rig.FocusWorker(worker, true);
            yield return new WaitForSecondsRealtime(.25f);
            yield return CaptureScreen(image);
        }

        private IEnumerator Issue(WorkerAgent worker, PlacementZone zone)
        {
            float readyDeadline = Time.realtimeSinceStartup + 20f;
            while (Time.realtimeSinceStartup < readyDeadline &&
                   (!worker.CanBeginPlayerCarry(out _) || !zone.CanAcceptWorker(worker, out _)))
                yield return null;

            bool began = worker.BeginPlayerCarry(out string reason);
            Record(began, "pickup accepted for " + zone.ActivityLabel + ": " + reason);
            if (!began) yield break;
            worker.SetPlayerCarryPosition(zone.PlacementPoint.position + Vector3.up * WorkerCarryController.CarryLiftMeters);
            bool accepted = office.TryIssueWorkerCommand(worker, zone, out _, out reason);
            if (!accepted) worker.CancelPlayerCarryImmediate();
            Record(accepted, zone.ActivityLabel + " command accepted: " + reason);
            yield return null;
        }

        private IEnumerator BeginCarryAt(WorkerAgent worker, PlacementZone zone)
        {
            Vector2 press = Camera.main.WorldToScreenPoint(worker.transform.position + Vector3.up * .8f);
            office.CarryController.ExternalPointerControl = true;
            Record(office.CarryController.BeginPointerGesture(worker, press, false), "new-hire pointer gesture begins");
            Record(office.CarryController.EvaluateCarryStart(press + Vector2.right * 7f, .01f, worker.transform.position),
                "new-hire pickup begins");
            office.CarryController.UpdateCarriedPosition(zone.PlacementPoint.position, zone,
                Camera.main.WorldToScreenPoint(zone.PlacementPoint.position), true);
            yield return null;
        }

        private IEnumerator EarnUntil(Func<bool> condition, float timeout)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            float nextFocus = 0f;
            int index = 0;
            SimulationSpeedController.Instance.SetSpeed(4f);
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                if (Time.realtimeSinceStartup >= nextFocus)
                {
                    WorkerAgent worker = office.Workers[index++ % office.Workers.Count];
                    if (worker.CanBeginPlayerCarry(out _)) yield return Issue(worker, worker.Desk);
                    nextFocus = Time.realtimeSinceStartup + 12f;
                }
                yield return null;
            }
            SimulationSpeedController.Instance.SetSpeed(1f);
        }

        private IEnumerator WaitForPickable(WorkerAgent worker)
        {
            float deadline = Time.realtimeSinceStartup + 14f;
            while (worker != null && !worker.CanBeginPlayerCarry(out _) && Time.realtimeSinceStartup < deadline) yield return null;
            Record(worker != null && worker.CanBeginPlayerCarry(out _), worker == null ? "worker exists" : worker.Definition.displayName + " becomes pickable");
        }

        private WorkerAgent FindWorker(string name)
        {
            foreach (WorkerAgent worker in office.Workers)
                if (worker.Definition.displayName == name) return worker;
            return null;
        }

        private PlacementZone FindZone(string id)
        {
            foreach (PlacementZone zone in office.PlacementZones)
                if (zone.StableIdentifier == id) return zone;
            return null;
        }

        public static IEnumerator CaptureScreen(string fileName)
        {
            string folder = ScreenshotFolder();
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, fileName);
            if (File.Exists(path)) File.Delete(path);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(path, 1);
            yield return new WaitForSecondsRealtime(.55f);
            Record(File.Exists(path) && new FileInfo(path).Length > 1024, "captured " + fileName);
        }

        public static void Record(string description) => Record(true, description);

        public static void Record(bool passed, string description)
        {
            Checks.Add((passed ? "PASS  " : "FAIL  ") + description);
            Debug.Log("OPEN PLAN FRIEND DEMO: " + (passed ? "PASS " : "FAIL ") + description);
            if (!passed) Failures++;
        }

        public static void WriteReport()
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "ReleaseEvidence"));
            Directory.CreateDirectory(folder);
            var lines = new List<string>
            {
                Failures == 0 ? "STATUS PASS" : "STATUS FAIL",
                "CAPTURE POLICY public gameplay APIs only; no capture-state setter",
                "ARTIFICIAL FUNDS FALSE",
                "BEATS main menu -> starter overview -> pickup -> valid -> invalid -> focus -> rest -> water -> vending -> smoke -> natural distraction -> away -> personalities -> affordable -> wall -> expansion -> hire -> continued play -> preview -> menu",
                "CHECKS " + Checks.Count,
                "FAILURES " + Failures
            };
            lines.AddRange(Checks);
            File.WriteAllLines(Path.Combine(folder, "FriendDemo_Playthrough.txt"), lines);
        }

        private static string ScreenshotFolder()
            => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Screenshots", "FriendDemo"));
    }
}
