using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace OpenPlan
{
    /// <summary>Runs twenty simulated minutes in the packaged starter office and records personality evidence.</summary>
    public sealed class StandaloneBehaviorSoakDirector : MonoBehaviour
    {
        public static bool Requested => Array.Exists(Environment.GetCommandLineArgs(),
            argument => string.Equals(argument, "-openplan-behavior-soak", StringComparison.OrdinalIgnoreCase));

        private OfficeDirector office;
        private readonly List<string> observations = new List<string>();
        private readonly Dictionary<string, HashSet<WorkerState>> observed = new Dictionary<string, HashSet<WorkerState>>();
        private int failures;

        public void Initialize(OfficeDirector director)
        {
            office = director;
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(.8f);
            Time.timeScale = 20f;
            float start = Time.time;
            float end = start + 20f * 60f;
            foreach (WorkerAgent worker in office.Workers)
                observed[worker.Definition.displayName] = new HashSet<WorkerState>();

            while (Time.time < end)
            {
                foreach (WorkerAgent worker in office.Workers)
                    if (worker != null) observed[worker.Definition.displayName].Add(worker.Runtime.behavior);
                yield return null;
            }

            Time.timeScale = 1f;
            int totalDistractions = 0;
            foreach (WorkerAgent worker in office.Workers)
            {
                WorkerRuntimeState runtime = worker.Runtime;
                totalDistractions += runtime.distractionsStarted;
                float rate = runtime.autonomyDecisions <= 0 ? 0f :
                    (float)runtime.distractionsStarted / runtime.autonomyDecisions;
                Check(observed[worker.Definition.displayName].Count >= 6,
                    $"{worker.Definition.displayName} showed varied behavior ({observed[worker.Definition.displayName].Count} states)");
                Check(runtime.distractionsStarted > 0,
                    $"{worker.Definition.displayName} had readable distractions ({runtime.distractionsStarted})");
                Check(worker.Runtime.behavior != WorkerState.RecoverFromStuck || worker.StateAge < 3f,
                    $"{worker.Definition.displayName} is not permanently stuck");
                Check(worker.Runtime.behavior == WorkerState.Work || worker.Runtime.behavior == WorkerState.Away ||
                      worker.StateAge < 45f, $"{worker.Definition.displayName} has no permanent idle state");
                Check(!worker.IsPlayerCarried, $"{worker.Definition.displayName} has no stale carried state");
                Check(worker.Runtime.behavior == WorkerState.Smoke || (!worker.HasSmokingProp && !worker.HasSmokeParticles),
                    $"{worker.Definition.displayName} has no orphaned cigarette or smoke effect");
                observations.Add($"RATE  {worker.Definition.displayName} {runtime.distractionsStarted}/{runtime.autonomyDecisions} decisions = {rate:P1}; " +
                    $"{runtime.distractionSeconds / 60f:0.00} distraction minutes; states " +
                    string.Join(", ", observed[worker.Definition.displayName].OrderBy(value => value.ToString())));
                observations.Add("KINDS " + worker.Definition.displayName + " " +
                    string.Join(", ", worker.DistractionCounts.OrderBy(pair => pair.Key.ToString())
                        .Select(pair => pair.Key + "=" + pair.Value)));
            }
            Check(totalDistractions >= 6, $"office produced management decisions ({totalDistractions} distractions)");
            Check(office.Workers.Count >= 3 && office.Workers.All(worker => worker != null),
                "the complete starting roster remains present");
            Check(office.PlacementZones.All(zone => zone.Occupancy <= zone.Capacity),
                "all activity-zone capacity limits held");

            string output = OutputDirectory();
            Directory.CreateDirectory(output);
            yield return new WaitForEndOfFrame();
            CaptureCamera(Path.Combine(output, "StarterOffice_BehaviorSoak.png"));
            OfficeCameraRig rig = Camera.main == null ? null : Camera.main.GetComponent<OfficeCameraRig>();
            if (rig != null && office.Workers.Count > 0)
            {
                rig.FocusWorker(office.Workers[0], true);
                yield return new WaitForSecondsRealtime(.8f);
                CaptureCamera(Path.Combine(output, "StarterOffice_NameTagClose.png"));
            }
            WriteReport(output, Time.time - start);
            Application.Quit(failures == 0 ? 0 : 2);
        }

        private void Check(bool passed, string description)
        {
            observations.Add((passed ? "PASS  " : "FAIL  ") + description);
            Debug.Log("OPEN PLAN BEHAVIOR SOAK: " + (passed ? "PASS " : "FAIL ") + description);
            if (!passed) failures++;
        }

        private static string OutputDirectory()
            => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Screenshots"));

        private static void CaptureCamera(string path)
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            int width = Mathf.Max(960, Screen.width);
            int height = Mathf.Max(540, Screen.height);
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

        private void WriteReport(string output, float simulatedSeconds)
        {
            var lines = new List<string>
            {
                failures == 0 ? "STATUS PASS" : "STATUS FAIL",
                $"SIMULATED MINUTES {simulatedSeconds / 60f:0.00}",
                "ACCELERATION 20x",
                "STARTING ROSTER Morgan=Hardworking, Alex=Social, Sam=Lazy",
                "OBSERVATIONS " + observations.Count
            };
            lines.AddRange(observations);
            File.WriteAllLines(Path.Combine(output, "StarterOffice_BehaviorSoak.txt"), lines);
        }
    }
}
