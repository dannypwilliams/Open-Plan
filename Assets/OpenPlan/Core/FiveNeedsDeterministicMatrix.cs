using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;

namespace OpenPlan
{
    public enum NeedScenarioContext { ActiveWork, PhoneWork, Resting, MixedActivity, PauseResume, SpeedChanges }

    public sealed class NeedScenarioSummary
    {
        public int WorkerCount;
        public NeedScenarioContext Context;
        public int SeedCount;
        public readonly float[] Minimum = { 1f, 1f, 1f, 1f, 1f };
        public readonly float[] Maximum = { 0f, 0f, 0f, 0f, 0f };
        public float FirstCautionMinutes = float.PositiveInfinity;
        public float FirstUrgentMinutes = float.PositiveInfinity;
        public int MaximumCriticalNeedsOnOneWorker;
        public int InvalidValues;
        public int UnexpectedIdenticalWorkers;
        public int PausedChanges;
        public double ProductivitySum;
        public long ProductivitySamples;
        public long ElapsedMilliseconds;
        public long ApproximateManagedBytes;

        public float AverageProductivity => ProductivitySamples == 0 ? 0f : (float)(ProductivitySum / ProductivitySamples);
    }

    /// <summary>Pure deterministic soak matrix used by Prompt 01 verification and later balance work.</summary>
    public static class FiveNeedsDeterministicMatrix
    {
        public static readonly int[] WorkerCounts = { 3, 10, 30 };
        public const int SeedCount = 20;
        public const float SimulatedMinutes = 100f;
        private const float StepSeconds = 5f;

        public static List<NeedScenarioSummary> RunAll()
        {
            var results = new List<NeedScenarioSummary>(WorkerCounts.Length * 6);
            foreach (int workerCount in WorkerCounts)
                foreach (NeedScenarioContext context in Enum.GetValues(typeof(NeedScenarioContext)))
                    results.Add(Run(workerCount, context, SeedCount));
            return results;
        }

        public static NeedScenarioSummary Run(int workerCount, NeedScenarioContext context, int seedCount)
        {
            var result = new NeedScenarioSummary
            {
                WorkerCount = workerCount,
                Context = context,
                SeedCount = seedCount
            };
            long memoryBefore = GC.GetTotalMemory(false);
            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int seed = 0; seed < seedCount; seed++) RunSeed(result, seed + 1009);
            stopwatch.Stop();
            result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            result.ApproximateManagedBytes = Math.Max(0, GC.GetTotalMemory(false) - memoryBefore);
            return result;
        }

        private static void RunSeed(NeedScenarioSummary result, int seed)
        {
            WorkerRuntimeState[] workers = new WorkerRuntimeState[result.WorkerCount];
            int[] lastMixedPhase = new int[result.WorkerCount];
            for (int i = 0; i < workers.Length; i++)
            {
                workers[i] = new WorkerRuntimeState();
                NeedCatalog.Initialize(workers[i], "Matrix Worker " + i, seed);
                lastMixedPhase[i] = -1;
                Observe(result, workers[i], 0f);
            }

            float simulated = 0f;
            float wall = 0f;
            float nextIdentityCheck = 600f;
            while (simulated < SimulatedMinutes * 60f - .001f)
            {
                bool paused = result.Context == NeedScenarioContext.PauseResume && ((int)(wall / 30f) & 1) == 1;
                float speed = result.Context == NeedScenarioContext.SpeedChanges ? SpeedForWallTime(wall) : 1f;
                float delta = paused ? 0f : StepSeconds * speed;
                delta = Mathf.Min(delta, SimulatedMinutes * 60f - simulated);
                for (int i = 0; i < workers.Length; i++)
                {
                    WorkerRuntimeState state = workers[i];
                    float pausedHappiness = state.happiness;
                    float pausedHunger = state.hunger;
                    float pausedBathroom = state.bathroom;
                    float pausedInspiration = state.inspiration;
                    float pausedEnergy = state.energy;
                    WorkerState behavior = BehaviorFor(result.Context, simulated, i);
                    NeedSimulation.Tick(state, behavior, delta);
                    if (result.Context == NeedScenarioContext.Resting && CrossedInterval(simulated, delta, ActivityRules.RestDuration))
                        ActivityRules.ApplyRest(state);
                    if (result.Context == NeedScenarioContext.MixedActivity)
                        ApplyMixedCompletion(state, simulated, i, lastMixedPhase);
                    if (paused && (state.happiness != pausedHappiness || state.hunger != pausedHunger ||
                        state.bathroom != pausedBathroom || state.inspiration != pausedInspiration ||
                        state.energy != pausedEnergy)) result.PausedChanges++;
                    Observe(result, state, simulated + delta);
                    if (behavior == WorkerState.Work || behavior == WorkerState.Unassigned)
                    {
                        float workstation = behavior == WorkerState.Unassigned ? ProductivityModel.PhoneWorkstationModifier : 1f;
                        result.ProductivitySum += ProductivityModel.Evaluate(1f, state, workstation, 1f, 1f);
                        result.ProductivitySamples++;
                    }
                }
                simulated += delta;
                wall += StepSeconds;
                if (simulated >= nextIdentityCheck)
                {
                    CountUnexpectedIdentities(result, workers);
                    nextIdentityCheck += 600f;
                }
            }
        }

        private static WorkerState BehaviorFor(NeedScenarioContext context, float simulated, int workerIndex)
        {
            switch (context)
            {
                case NeedScenarioContext.PhoneWork: return WorkerState.Unassigned;
                case NeedScenarioContext.Resting: return WorkerState.TakeBreak;
                case NeedScenarioContext.MixedActivity:
                    switch (((int)(simulated / 60f) + workerIndex) % 8)
                    {
                        case 0: return WorkerState.Work;
                        case 1: return WorkerState.Unassigned;
                        case 2: return WorkerState.TakeBreak;
                        case 3: return WorkerState.UseWaterCooler;
                        case 4: return WorkerState.BuySnack;
                        case 5: return WorkerState.UseRestroom;
                        case 6: return WorkerState.Smoke;
                        default: return WorkerState.Away;
                    }
                default: return WorkerState.Work;
            }
        }

        private static void ApplyMixedCompletion(WorkerRuntimeState state, float simulated, int workerIndex, int[] lastPhase)
        {
            int phase = ((int)(simulated / 60f) + workerIndex) % 8;
            if (lastPhase[workerIndex] == phase) return;
            lastPhase[workerIndex] = phase;
            switch (phase)
            {
                case 2: ActivityRules.ApplyRest(state); break;
                case 3: ActivityRules.ApplyWater(state); break;
                case 4: ActivityRules.ApplySnack(state, false); break;
                case 5: ActivityRules.ApplyRestroom(state); break;
                case 6: ActivityRules.ApplySmoke(state); break;
            }
        }

        private static void Observe(NeedScenarioSummary result, WorkerRuntimeState state, float seconds)
        {
            int critical = 0;
            for (int i = 0; i < NeedCatalog.All.Length; i++)
            {
                NeedDefinition definition = NeedCatalog.All[i];
                float value = state.GetNeed(definition.Kind);
                if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f || value > 1f)
                {
                    result.InvalidValues++;
                    continue;
                }
                result.Minimum[i] = Mathf.Min(result.Minimum[i], value);
                result.Maximum[i] = Mathf.Max(result.Maximum[i], value);
                NeedStatus status = definition.Status(value);
                if (status >= NeedStatus.Caution)
                    result.FirstCautionMinutes = Mathf.Min(result.FirstCautionMinutes, seconds / 60f);
                if (status >= NeedStatus.Urgent)
                    result.FirstUrgentMinutes = Mathf.Min(result.FirstUrgentMinutes, seconds / 60f);
                if (status == NeedStatus.Critical) critical++;
            }
            result.MaximumCriticalNeedsOnOneWorker = Mathf.Max(result.MaximumCriticalNeedsOnOneWorker, critical);
        }

        private static void CountUnexpectedIdentities(NeedScenarioSummary result, WorkerRuntimeState[] workers)
        {
            for (int i = 1; i < workers.Length; i++)
            {
                if (!AllNeedValuesMatch(workers[i - 1], workers[i])) continue;
                bool allAtBoundary = true;
                foreach (NeedDefinition definition in NeedCatalog.All)
                {
                    float value = workers[i].GetNeed(definition.Kind);
                    if (value > .0001f && value < .9999f) { allAtBoundary = false; break; }
                }
                if (!allAtBoundary) result.UnexpectedIdenticalWorkers++;
            }
        }

        private static bool AllNeedValuesMatch(WorkerRuntimeState a, WorkerRuntimeState b)
        {
            foreach (NeedDefinition definition in NeedCatalog.All)
                if (Mathf.Abs(a.GetNeed(definition.Kind) - b.GetNeed(definition.Kind)) > .000001f) return false;
            return true;
        }

        private static float SpeedForWallTime(float wall)
        {
            int phase = (int)(wall / 60f) % 3;
            return phase == 0 ? 1f : phase == 1 ? 2f : 4f;
        }

        private static bool CrossedInterval(float before, float delta, float interval)
            => delta > 0f && Mathf.FloorToInt(before / interval) != Mathf.FloorToInt((before + delta) / interval);

        public static string BuildMarkdown(List<NeedScenarioSummary> results)
        {
            var text = new StringBuilder();
            text.AppendLine("# Prompt 01 Deterministic Five-Need Simulation Report");
            text.AppendLine();
            text.AppendLine($"Matrix: 3, 10, and 30 workers; {SeedCount} seeds; {SimulatedMinutes:0} simulated minutes per row. Values are sampled every {StepSeconds:0} simulated seconds.");
            text.AppendLine($"Deterministic repeat check: {(ValidateDeterministicRepeat() ? "PASS" : "FAIL")} (same seed, population, context, and duration). No global or frame-time random input is used.");
            text.AppendLine();
            text.AppendLine("| Workers | Context | Need min..max (Happiness / Hunger / Bathroom / Inspiration / Energy) | First caution | First urgent | Max critical together | Invalid | Unexpected identical | Avg productivity | Pause changes | Runtime | Managed delta |");
            text.AppendLine("|---:|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|");
            foreach (NeedScenarioSummary row in results)
            {
                string ranges = string.Empty;
                for (int i = 0; i < 5; i++)
                {
                    if (i > 0) ranges += " / ";
                    ranges += $"{row.Minimum[i]:0.000}..{row.Maximum[i]:0.000}";
                }
                text.AppendLine($"| {row.WorkerCount} | {row.Context} | {ranges} | {Minutes(row.FirstCautionMinutes)} | {Minutes(row.FirstUrgentMinutes)} | {row.MaximumCriticalNeedsOnOneWorker} | {row.InvalidValues} | {row.UnexpectedIdenticalWorkers} | {row.AverageProductivity:0.000}x | {row.PausedChanges} | {row.ElapsedMilliseconds} ms | {row.ApproximateManagedBytes / 1024f:0.0} KiB |");
            }
            text.AppendLine();
            text.AppendLine("Acceptance: invalid/out-of-range values must be zero; pause changes must be zero; identical-worker counts exclude fully saturated boundary states; active and phone rows intentionally model uninterrupted 100-minute work and therefore eventually reach critical values. Mixed activity demonstrates recoverability without implementing Prompt 02 autonomy.");
            text.AppendLine();
            text.AppendLine("Performance note: managed-memory delta is a coarse before/after observation, not an allocation profiler. The 30-worker rows are required to finish without runaway runtime or memory growth.");
            return text.ToString();
        }

        private static string Minutes(float value) => float.IsInfinity(value) ? "none" : value.ToString("0.00") + " min";

        public static bool ValidateDeterministicRepeat()
        {
            NeedScenarioSummary first = Run(3, NeedScenarioContext.MixedActivity, 1);
            NeedScenarioSummary second = Run(3, NeedScenarioContext.MixedActivity, 1);
            for (int i = 0; i < 5; i++)
                if (first.Minimum[i] != second.Minimum[i] || first.Maximum[i] != second.Maximum[i]) return false;
            return first.FirstCautionMinutes == second.FirstCautionMinutes &&
                   first.FirstUrgentMinutes == second.FirstUrgentMinutes &&
                   first.MaximumCriticalNeedsOnOneWorker == second.MaximumCriticalNeedsOnOneWorker &&
                   first.InvalidValues == second.InvalidValues &&
                   first.UnexpectedIdenticalWorkers == second.UnexpectedIdenticalWorkers &&
                   first.PausedChanges == second.PausedChanges &&
                   first.ProductivitySum == second.ProductivitySum &&
                   first.ProductivitySamples == second.ProductivitySamples;
        }
    }
}
