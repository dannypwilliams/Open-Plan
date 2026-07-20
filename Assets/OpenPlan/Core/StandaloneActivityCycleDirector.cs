using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OpenPlan
{
    /// <summary>Observes a full placement-to-water-to-autonomy cycle in the packaged player.</summary>
    public sealed class StandaloneActivityCycleDirector : MonoBehaviour
    {
        public static bool Requested => Array.Exists(Environment.GetCommandLineArgs(),
            argument => string.Equals(argument, "-openplan-activity-smoke", StringComparison.OrdinalIgnoreCase));

        private readonly List<string> observations = new List<string>();
        private OfficeDirector office;
        private int failures;

        public void Initialize(OfficeDirector director)
        {
            office = director;
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            SimulationSpeedController.Instance.SetSpeed(4f);
            yield return new WaitForSecondsRealtime(.8f);
            WorkerAgent worker = office.Workers[0];
            float deadline = Time.realtimeSinceStartup + 5f;
            while (!worker.CanBeginPlayerCarry(out _) && Time.realtimeSinceStartup < deadline) yield return null;
            Check(worker.CanBeginPlayerCarry(out _), "worker became available for placement");

            PlacementZone water = FindZone("starter.water.cooler");
            float energy = worker.Runtime.energy;
            float mood = worker.Runtime.mood;
            float stress = worker.Runtime.stress;
            Check(worker.BeginPlayerCarry(out string carryReason), "pickup began" +
                (string.IsNullOrEmpty(carryReason) ? string.Empty : ": " + carryReason));
            worker.SetPlayerCarryPosition(water.PlacementPoint.position + Vector3.up * WorkerCarryController.CarryLiftMeters);
            bool issued = office.TryIssueWorkerCommand(worker, water, out WorkerCommand command, out string issueReason);
            Check(issued && command != null && command.requestedActivity == PlacementActivity.GetWater,
                "GET WATER command issued" + (string.IsNullOrEmpty(issueReason) ? string.Empty : ": " + issueReason));
            worker.transform.position = water.PlacementPoint.position;

            deadline = Time.realtimeSinceStartup + 4f;
            while (worker.Runtime.behavior != WorkerState.UseWaterCooler && Time.realtimeSinceStartup < deadline) yield return null;
            Check(worker.Runtime.behavior == WorkerState.UseWaterCooler, "worker arrived and began six-second drinking activity");
            string output = OutputDirectory();
            Directory.CreateDirectory(output);
            yield return new WaitForEndOfFrame();
            CaptureCamera(Path.Combine(output, "StarterOffice_Water_Activity.png"));

            deadline = Time.realtimeSinceStartup + 5f;
            while (worker.Runtime.behavior == WorkerState.UseWaterCooler && Time.realtimeSinceStartup < deadline) yield return null;
            Check(worker.Runtime.behavior == WorkerState.ReturnToDesk, "worker completed activity and resumed autonomous return");
            Check(Mathf.Abs(worker.Runtime.energy - Mathf.Clamp01(energy + .08f)) < .012f,
                "Energy recovered by 0.08");
            Check(Mathf.Abs(worker.Runtime.mood - Mathf.Clamp01(mood + .05f)) < .012f,
                "Mood increased by 0.05");
            Check(Mathf.Abs(worker.Runtime.stress - Mathf.Clamp01(stress - .05f)) < .012f,
                "Stress reduced by 0.05");
            Check(worker.Runtime.waterCooldown > 34f && worker.Runtime.waterCooldown <= ActivityRules.WaterCooldown,
                "35-second water cooldown set");

            yield return null;
            WriteReport(output);
            Application.Quit(failures == 0 ? 0 : 2);
        }

        private PlacementZone FindZone(string stableIdentifier)
        {
            foreach (PlacementZone zone in office.PlacementZones)
                if (zone.StableIdentifier == stableIdentifier) return zone;
            return null;
        }

        private void Check(bool passed, string description)
        {
            observations.Add((passed ? "PASS  " : "FAIL  ") + description);
            Debug.Log("OPEN PLAN ACTIVITY CYCLE: " + (passed ? "PASS " : "FAIL ") + description);
            if (!passed) failures++;
        }

        private static string OutputDirectory()
            => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Screenshots"));

        private static void CaptureCamera(string path)
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            int width = Mathf.Max(640, Screen.width);
            int height = Mathf.Max(360, Screen.height);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(target);
            Destroy(image);
        }

        private void WriteReport(string output)
        {
            var lines = new List<string>
            {
                failures == 0 ? "STATUS PASS" : "STATUS FAIL",
                "CYCLE PICKUP -> WALK -> GET WATER -> RETURN TO DESK",
                "SIMULATION SPEED 4x",
                "OBSERVATIONS " + observations.Count
            };
            lines.AddRange(observations);
            File.WriteAllLines(Path.Combine(output, "StarterOffice_ActivityCycle.txt"), lines);
        }
    }
}
