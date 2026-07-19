using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Profiling;
using UnityEngine;

namespace OpenPlan
{
    public sealed class AutomatedPerformanceDirector : MonoBehaviour
    {
        public static bool Requested => Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-openplan-performance");

        public void Initialize() => StartCoroutine(Measure());

        private IEnumerator Measure()
        {
            SimulationSpeedController.Instance.SetSpeed(1f);
            yield return new WaitForSecondsRealtime(5f);
            var frameTimes = new List<float>(2400);
            long peakGcBytes = 0;
            using (ProfilerRecorder gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 15))
            {
                float end = Time.realtimeSinceStartup + 20f;
                while (Time.realtimeSinceStartup < end)
                {
                    yield return null;
                    frameTimes.Add(Time.unscaledDeltaTime);
                    if (gc.Valid) peakGcBytes = Math.Max(peakGcBytes, gc.LastValue);
                }
            }
            frameTimes.Sort();
            float total = 0f;
            foreach (float value in frameTimes) total += value;
            float averageFps = frameTimes.Count / Mathf.Max(.001f, total);
            int tail = Mathf.Max(1, Mathf.CeilToInt(frameTimes.Count * .01f));
            float slowTotal = 0f;
            for (int i = frameTimes.Count - tail; i < frameTimes.Count; i++) slowTotal += frameTimes[i];
            float onePercentLow = tail / Mathf.Max(.001f, slowTotal);
            float worstMs = frameTimes.Count == 0 ? 0f : frameTimes[frameTimes.Count - 1] * 1000f;
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Performance"));
            Directory.CreateDirectory(folder);
            string json = $"{{\n  \"resolution\": \"{Screen.width}x{Screen.height}\",\n  \"sampleSeconds\": 20,\n  \"frames\": {frameTimes.Count},\n  \"averageFps\": {averageFps:0.00},\n  \"onePercentLowFps\": {onePercentLow:0.00},\n  \"worstFrameMs\": {worstMs:0.00},\n  \"peakGcAllocatedInFrameBytes\": {peakGcBytes}\n}}";
            File.WriteAllText(Path.Combine(folder, "performance.json"), json);
            Debug.Log($"PERFORMANCE PROBE PASS: {averageFps:0.0} fps average, {onePercentLow:0.0} fps 1% low, {peakGcBytes} peak GC bytes");
            Application.Quit(0);
        }
    }
}
