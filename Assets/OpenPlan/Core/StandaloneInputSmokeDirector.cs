using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace OpenPlan
{
    /// <summary>Drives the packaged player's real Input System path for release smoke verification.</summary>
    public sealed class StandaloneInputSmokeDirector : MonoBehaviour
    {
        public static bool Requested => Array.Exists(Environment.GetCommandLineArgs(),
            argument => string.Equals(argument, "-openplan-input-smoke", StringComparison.OrdinalIgnoreCase));

        private readonly List<string> checks = new List<string>();
        private OfficeDirector office;
        private Mouse smokeMouse;
        private int failures;

        public void Initialize(OfficeDirector director)
        {
            office = director;
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            SimulationSpeedController.Instance.SetSpeed(1f);
            Debug.Log("OPEN PLAN INPUT SMOKE: START " + Screen.width + "x" + Screen.height);
            yield return new WaitForSecondsRealtime(1f);
            WorkerAgent worker = office.Workers[0];
            float deadline = Time.realtimeSinceStartup + 5f;
            while (!worker.CanBeginPlayerCarry(out _) && Time.realtimeSinceStartup < deadline) yield return null;
            bool workerPickable = worker.CanBeginPlayerCarry(out string pickReason);
            Check(workerPickable, workerPickable ? "worker pickable" : "worker pickable: " + pickReason);

            smokeMouse = InputSystem.AddDevice<Mouse>("OpenPlan Smoke Mouse");
            WorkerCarryController carry = office.CarryController;
            carry.InputMouseOverride = smokeMouse;
            Camera camera = Camera.main;
            Vector2 workerScreen = camera.WorldToScreenPoint(worker.transform.position + Vector3.up * .8f);

            WorkerSelection.Clear();
            QueueMouse(workerScreen, false, false);
            yield return null;
            QueueMouse(workerScreen, true, false);
            yield return null;
            QueueMouse(workerScreen, false, false);
            yield return null;
            Check(WorkerSelection.Selected == worker && !carry.IsCarrying, "simple click selects without carry");

            PlacementZone rest = FindZone("starter.rest.break-nook");
            Vector2 restScreen = camera.WorldToScreenPoint(rest.PlacementPoint.position + new Vector3(.55f,0f,.40f));
            yield return BeginDrag(worker, workerScreen);
            QueueMouse(restScreen, true, false);
            yield return null;
            Check(carry.IsCarrying && carry.HasValidTarget, "drag enters valid carry feedback");
            QueueMouse(restScreen, false, false);
            SimulationSpeedController.Instance.SetSpeed(1f);
            yield return new WaitForSecondsRealtime(.35f);
            Check(office.LastIssuedCommand != null && office.LastIssuedCommand.destinationZone == rest,
                "valid release issues REST command");
            Check(!worker.IsPlayerCarried && worker.transform.position.y < WorkerCarryController.CarryLiftMeters,
                "successful release lowers worker");

            PlacementZone locked = FindZone("neighbor.work.01");
            workerScreen = camera.WorldToScreenPoint(worker.transform.position + Vector3.up * .8f);
            Vector2 lockedScreen = camera.WorldToScreenPoint(locked.PlacementPoint.position);
            WorkerCommand commandBeforeReject = office.LastIssuedCommand;
            yield return BeginDrag(worker, workerScreen);
            QueueMouse(lockedScreen, true, false);
            yield return null;
            Check(carry.IsCarrying && !carry.HasValidTarget && carry.FeedbackText.Contains("AREA LOCKED"),
                "locked destination shows invalid feedback");
            QueueMouse(lockedScreen, false, false);
            SimulationSpeedController.Instance.SetSpeed(1f);
            yield return new WaitForSecondsRealtime(.40f);
            Check(office.LastIssuedCommand == commandBeforeReject && carry.LastRejectionReason == "Area locked.",
                "locked release rejects without command");
            Check(!worker.IsPlayerCarried && Mathf.Abs(worker.transform.position.y) < .1f,
                "rejected release restores worker to ground");

            workerScreen = camera.WorldToScreenPoint(worker.transform.position + Vector3.up * .8f);
            yield return BeginDrag(worker, workerScreen);
            QueueMouse(workerScreen + new Vector2(50f,30f), true, true);
            yield return null;
            QueueMouse(workerScreen + new Vector2(50f,30f), false, false);
            SimulationSpeedController.Instance.SetSpeed(1f);
            yield return new WaitForSecondsRealtime(.35f);
            Check(carry.Phase == WorkerCarryPhase.Idle && !worker.IsPlayerCarried && Mathf.Abs(worker.transform.position.y) < .1f,
                "right-click cancels and restores worker");

            WriteReport();
            carry.InputMouseOverride = null;
            InputSystem.RemoveDevice(smokeMouse);
            smokeMouse = null;
            yield return null;
            Application.Quit(failures == 0 ? 0 : 2);
        }

        private IEnumerator BeginDrag(WorkerAgent worker, Vector2 workerScreen)
        {
            QueueMouse(workerScreen, false, false);
            yield return null;
            QueueMouse(workerScreen, true, false);
            yield return null;
            QueueMouse(workerScreen + Vector2.right * 7f, true, false);
            yield return null;
            Check(office.CarryController.IsCarrying, "seven-pixel drag begins carry");
        }

        private void QueueMouse(Vector2 position, bool left, bool right)
        {
            MouseState state = new MouseState { position = position };
            if (left) state = state.WithButton(MouseButton.Left);
            if (right) state = state.WithButton(MouseButton.Right);
            InputSystem.QueueStateEvent(smokeMouse, state);
        }

        private PlacementZone FindZone(string stableIdentifier)
        {
            foreach (PlacementZone zone in office.PlacementZones)
                if (zone.StableIdentifier == stableIdentifier) return zone;
            return null;
        }

        private void Check(bool passed, string description)
        {
            checks.Add((passed ? "PASS  " : "FAIL  ") + description);
            Debug.Log("OPEN PLAN INPUT SMOKE: " + (passed ? "PASS " : "FAIL ") + description);
            if (!passed) failures++;
        }

        private void WriteReport()
        {
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Screenshots"));
            Directory.CreateDirectory(output);
            string resolution = Screen.width + "x" + Screen.height;
            var lines = new List<string>
            {
                failures == 0 ? "STATUS PASS" : "STATUS FAIL",
                "RESOLUTION " + resolution,
                "INPUT Unity Input System virtual Mouse device",
                "CHECKS " + checks.Count,
            };
            lines.AddRange(checks);
            File.WriteAllLines(Path.Combine(output, "StarterOffice_InputSmoke_" + resolution + ".txt"), lines);
        }
    }
}
