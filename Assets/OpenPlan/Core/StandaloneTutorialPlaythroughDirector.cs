using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace OpenPlan
{
    /// <summary>Packaged friend-flow evidence: real tutorial events, real earnings, and a live expansion.</summary>
    public sealed class StandaloneTutorialPlaythroughDirector : MonoBehaviour
    {
        public const string Argument = "-openplan-tutorial-playthrough";
        public const string CaptureOnlyArgument = "-openplan-tutorial-capture-only";
        public static bool Requested => HasArgument(Argument);
        public static bool CaptureOnly => HasArgument(CaptureOnlyArgument);

        private readonly List<string> checks = new List<string>();
        private OfficeDirector office;
        private string output;
        private string resolution;
        private int failures;

        public void Initialize(OfficeDirector director)
        {
            office = director;
            output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Screenshots"));
            Directory.CreateDirectory(output);
            resolution = Screen.width + "x" + Screen.height;
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(.4f);
            TutorialController tutorial = office.Tutorial;
            Check(tutorial.CurrentStep == TutorialStep.MeetTheTeam && tutorial.IsReading, "first-run tutorial opens at MEET THE TEAM");
            yield return Capture("Tutorial_01_MeetTeam_" + resolution + ".png");

            tutorial.ContinueFromReading();
            WorkerAgent morgan = FindWorker("Morgan");
            WorkerSelection.Select(morgan);
            yield return null;
            Check(tutorial.CurrentStep == TutorialStep.PickThemUp && tutorial.IsReading, "selection event advances to PICK THEM UP");
            tutorial.ContinueFromReading();
            yield return WaitForPickable(morgan);
            StartCarry(morgan);
            Check(tutorial.CurrentStep == TutorialStep.PutThemToWork && office.CarryController.PlacementLegendVisible,
                "carry event advances to PUT THEM TO WORK and shows legend");
            yield return Capture("Tutorial_02_PutToWork_" + resolution + ".png");
            yield return ReleaseAt(morgan.Desk);
            Check(tutorial.CurrentStep == TutorialStep.ManageTheirNeeds && tutorial.IsReading,
                "real Work command advances to MANAGE THEIR NEEDS");
            Check(morgan.Runtime.focusedWorkSecondsRemaining > 0f, "manual Work applies Focused Work");
            yield return Capture("Tutorial_03_ManageNeeds_" + resolution + ".png");

            tutorial.ContinueFromReading();
            yield return WaitForPickable(morgan);
            StartCarry(morgan);
            yield return ReleaseAt(FindZone("starter.rest.break-nook"));
            Check(tutorial.CurrentStep == TutorialStep.RedirectADistraction && tutorial.IsReading,
                "real Rest command advances to REDIRECT A DISTRACTION");
            WorkerAgent distracted = tutorial.HighlightedWorker;
            Check(distracted != null && distracted.CurrentDistraction == DistractionKind.Phone,
                "tutorial creates deterministic readable distraction");
            yield return Capture("Tutorial_04_RedirectDistraction_" + resolution + ".png");

            tutorial.ContinueFromReading();
            yield return WaitForPickable(distracted);
            StartCarry(distracted);
            yield return ReleaseAt(distracted.Desk);
            Check(tutorial.CurrentStep == TutorialStep.TryTheOffice && tutorial.IsReading,
                "redirect command advances to TRY THE OFFICE");
            yield return Capture("Tutorial_05_TryOffice_" + resolution + ".png");
            tutorial.ContinueFromReading();
            Check(tutorial.CurrentStep == TutorialStep.Expand && tutorial.IsReading, "information step advances to EXPAND");
            yield return Capture("Tutorial_06_Expand_" + resolution + ".png");
            tutorial.ContinueFromReading();
            Check(tutorial.WasCompleted && !tutorial.HasBlockingPanel, "tutorial completes and normal play resumes");

            if (!CaptureOnly)
            {
                SimulationSpeedController.Instance.SetSpeed(4f);
                float deadline = Time.realtimeSinceStartup + 150f;
                while (!office.CanPurchaseExpansion && Time.realtimeSinceStartup < deadline) yield return null;
                Check(office.CanPurchaseExpansion, "desk work earns $1,000 without artificial funds");
                float affordableCash = office.Cash.CurrentCash;
                GameObject.Find("Purchase Next Door").GetComponent<Button>().onClick.Invoke();
                GameObject.Find("Confirm").GetComponent<Button>().onClick.Invoke();
                Check(Mathf.Abs(affordableCash - office.Cash.CurrentCash - ExpansionRules.PurchasePrice) < .02f,
                    "confirmed purchase deducts exactly $1,000");
                yield return new WaitForSecondsRealtime(1.6f);
                Check(office.ExpansionComplete && office.WorkerCapacity == 6, "manual flow reaches live first expansion");
                SimulationSpeedController.Instance.SetSpeed(0f);
                Camera.main.GetComponent<OfficeCameraRig>().Overview();
                yield return new WaitForSecondsRealtime(.8f);
                yield return Capture("Tutorial_07_ExpansionComplete_" + resolution + ".png");
            }

            WriteReport();
            Application.Quit(failures == 0 ? 0 : 2);
        }

        private IEnumerator WaitForPickable(WorkerAgent worker)
        {
            float deadline = Time.realtimeSinceStartup + 5f;
            while (worker != null && !worker.CanBeginPlayerCarry(out _) && Time.realtimeSinceStartup < deadline)
                yield return null;
            Check(worker != null && worker.CanBeginPlayerCarry(out _), "highlighted worker remains pickable");
        }

        private void StartCarry(WorkerAgent worker)
        {
            Vector2 point = Camera.main.WorldToScreenPoint(worker.transform.position + Vector3.up * .8f);
            Check(office.CarryController.BeginPointerGesture(worker, point, false), "pointer press begins worker gesture");
            Check(office.CarryController.EvaluateCarryStart(point + Vector2.right * 7f, .01f, worker.transform.position),
                "drag threshold starts pickup");
        }

        private IEnumerator ReleaseAt(PlacementZone zone)
        {
            WorkerCarryController carry = office.CarryController;
            carry.UpdateCarriedPosition(zone.PlacementPoint.position, zone,
                Camera.main.WorldToScreenPoint(zone.PlacementPoint.position), true);
            Check(carry.HasValidTarget, "highlighted destination is valid");
            carry.ReleaseAtZone(zone);
            yield return new WaitForSecondsRealtime(.35f);
            Check(office.LastIssuedCommand != null && office.LastIssuedCommand.destinationZone == zone,
                "release issues gameplay placement command");
        }

        private WorkerAgent FindWorker(string name)
        {
            foreach (WorkerAgent worker in office.Workers)
                if (worker.Definition.displayName == name) return worker;
            return null;
        }

        private PlacementZone FindZone(string stableIdentifier)
        {
            foreach (PlacementZone zone in office.PlacementZones)
                if (zone.StableIdentifier == stableIdentifier) return zone;
            return null;
        }

        private IEnumerator Capture(string fileName)
        {
            string path = Path.Combine(output, fileName);
            if (File.Exists(path)) File.Delete(path);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(path, 1);
            yield return new WaitForSecondsRealtime(.55f);
            Check(File.Exists(path) && new FileInfo(path).Length > 1024, "captured " + fileName);
        }

        private void Check(bool passed, string description)
        {
            checks.Add((passed ? "PASS  " : "FAIL  ") + description);
            Debug.Log("OPEN PLAN TUTORIAL PLAYTHROUGH: " + (passed ? "PASS " : "FAIL ") + description);
            if (!passed) failures++;
        }

        private void WriteReport()
        {
            var lines = new List<string>
            {
                failures == 0 ? "STATUS PASS" : "STATUS FAIL",
                "RESOLUTION " + resolution,
                "ARTIFICIAL PURCHASE FUNDS FALSE",
                "TUTORIAL COMPLETE " + office.Tutorial.WasCompleted,
                "EXPANSION REQUIRED " + (!CaptureOnly),
                "EXPANSION COMPLETE " + office.ExpansionComplete,
                "CHECKS " + checks.Count
            };
            lines.AddRange(checks);
            File.WriteAllLines(Path.Combine(output, "StarterOffice_TutorialPlaythrough_" + resolution + ".txt"), lines);
        }

        private static bool HasArgument(string wanted)
            => Array.Exists(Environment.GetCommandLineArgs(), arg => string.Equals(arg, wanted, StringComparison.OrdinalIgnoreCase));
    }
}
