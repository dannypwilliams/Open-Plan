using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenPlan
{
    public enum AutonomyMatrixScenario
    {
        NormalMixedNeeds, AllHungry, SharedRestroomDemand, LowEnergy,
        LowHappinessAndInspiration, DesklessPhoneWorkers, UnaffordableFood,
        DisabledRestroom, UnreachablePreferredStation, FullStationCapacity,
        HighDistractionFrequency, RepeatedGroundPlacement, PassiveManagement,
        ActiveManagement, PauseAndSpeedChanges
    }

    public sealed class NeedAutonomyMatrixReport
    {
        public bool Passed = true;
        public int Runs;
        public int CriticalNeeds;
        public float TotalCriticalResponseSeconds;
        public float TotalRecoverySeconds;
        public float MaximumCriticalSeconds;
        public readonly int[] ActivitySelections = new int[Enum.GetValues(typeof(PlacementActivity)).Length];
        public int ReservationsCreated;
        public int ReservationsReleased;
        public int ReservationTimeouts;
        public int Reroutes;
        public int OffSiteFallbacks;
        public int StuckRecoveries;
        public int SafetyCorrections;
        public int FailedToResume;
        public int LostDeskAssignments;
        public int DuplicateCharges;
        public int InvalidNeedValues;
        public int OrphanedReservations;
        public int CapacityViolations;
        public int NondeterministicRuns;
        public int NeedInterventions;
        public float WorkOutput;
        public float PhoneOutput;
        public float PassiveOutput;
        public float ActiveOutput;
        public long ManagedBytesBefore;
        public long ManagedBytesAfter;
        public float AverageCriticalResponseSeconds => CriticalNeeds == 0 ? 0f : TotalCriticalResponseSeconds / CriticalNeeds;
        public float AverageRecoverySeconds => CriticalNeeds == 0 ? 0f : TotalRecoverySeconds / CriticalNeeds;
        public float ActiveAdvantagePercent => PassiveOutput <= 0f ? 0f : (ActiveOutput / PassiveOutput - 1f) * 100f;
    }

    /// <summary>Deterministic 3/10/30-worker acceptance simulator for throttled need autonomy.</summary>
    public static class NeedAutonomyDeterministicMatrix
    {
        private sealed class SimWorker
        {
            public WorkerRuntimeState State;
            public bool Phone;
            public bool HadDesk;
            public bool Working = true;
            public float EvaluationTimer;
            public int EvaluationIndex;
            public PlacementActivity Activity;
            public NeedKind Need;
            public float TravelRemaining;
            public float ActivityRemaining;
            public float CriticalStarted = -1f;
            public float ResponseStarted = -1f;
            public bool Reserved;
            public float Output;
        }

        private sealed class RunResult
        {
            public string Signature;
            public int Critical;
            public float Response;
            public float Recovery;
            public float MaximumCritical;
            public readonly int[] Activities = new int[Enum.GetValues(typeof(PlacementActivity)).Length];
            public int Created;
            public int Released;
            public int Fallbacks;
            public int Reroutes;
            public int Invalid;
            public int Orphans;
            public int CapacityViolations;
            public int FailedResume;
            public int LostDesks;
            public int DuplicateCharges;
            public int Interventions;
            public float WorkOutput;
            public float PhoneOutput;
        }

        public static NeedAutonomyMatrixReport RunAcceptanceMatrix()
        {
            NeedAutonomyMatrixReport report = new NeedAutonomyMatrixReport
            {
                ManagedBytesBefore = GC.GetTotalMemory(false)
            };
            int[] workerCounts = { 3, 10, 30 };
            AutonomyMatrixScenario[] scenarios = (AutonomyMatrixScenario[])Enum.GetValues(typeof(AutonomyMatrixScenario));
            for (int seed = 1; seed <= 20; seed++)
            for (int c = 0; c < workerCounts.Length; c++)
            for (int s = 0; s < scenarios.Length; s++)
            {
                RunResult first = Run(seed, workerCounts[c], scenarios[s], 600);
                RunResult repeat = Run(seed, workerCounts[c], scenarios[s], 600);
                if (!string.Equals(first.Signature, repeat.Signature, StringComparison.Ordinal))
                    report.NondeterministicRuns++;
                Accumulate(report, first, scenarios[s]);
                report.Runs++;
            }
            for (int c = 0; c < workerCounts.Length; c++)
            {
                RunResult extended = Run(9000 + c, workerCounts[c], AutonomyMatrixScenario.NormalMixedNeeds, 6000);
                Accumulate(report, extended, AutonomyMatrixScenario.NormalMixedNeeds);
                report.Runs++;
            }
            report.ManagedBytesAfter = GC.GetTotalMemory(false);
            report.Passed = report.InvalidNeedValues == 0 && report.OrphanedReservations == 0 &&
                            report.CapacityViolations == 0 && report.FailedToResume == 0 &&
                            report.LostDeskAssignments == 0 && report.DuplicateCharges == 0 &&
                            report.NondeterministicRuns == 0 && report.CriticalNeeds > 0 &&
                            report.AverageCriticalResponseSeconds <= NeedAutonomyRules.CriticalHungerPlayerDeferral + .01f;
            return report;
        }

        private static RunResult Run(int seed, int count, AutonomyMatrixScenario scenario, int seconds)
        {
            SimWorker[] workers = new SimWorker[count];
            int[] usage = new int[Enum.GetValues(typeof(PlacementActivity)).Length];
            int[] capacity = new int[usage.Length];
            for (int i = 0; i < capacity.Length; i++) capacity[i] = 30;
            capacity[(int)PlacementActivity.UseRestroom] = 1;
            capacity[(int)PlacementActivity.BuySnack] = 1;
            capacity[(int)PlacementActivity.GetCoffee] = 1;
            capacity[(int)PlacementActivity.GetWater] = 1;
            capacity[(int)PlacementActivity.Rest] = 2;
            capacity[(int)PlacementActivity.Smoke] = 2;
            RunResult result = new RunResult();
            float cash = scenario == AutonomyMatrixScenario.UnaffordableFood ? 0f : 100000f;
            for (int i = 0; i < count; i++)
            {
                WorkerRuntimeState state = new WorkerRuntimeState();
                NeedCatalog.Initialize(state, "Matrix Autonomy " + i, seed);
                workers[i] = new SimWorker
                {
                    State = state,
                    Phone = scenario == AutonomyMatrixScenario.DesklessPhoneWorkers || i >= 3,
                    HadDesk = i < 3,
                    EvaluationTimer = NeedAutonomyRules.InitialEvaluationDelay("Matrix Autonomy " + i, seed)
                };
                ApplyScenarioPressure(workers[i], scenario, i, true);
            }

            for (int second = 0; second < seconds; second++)
            {
                bool paused = scenario == AutonomyMatrixScenario.PauseAndSpeedChanges && second % 120 >= 90 && second % 120 < 100;
                float dt = paused ? 0f : 1f;
                for (int i = 0; i < workers.Length; i++)
                {
                    SimWorker worker = workers[i];
                    float[] pausedSnapshot = paused ? Snapshot(worker.State) : null;
                    if (dt > 0f)
                    {
                        TickWorker(worker, scenario, seed, i, second, usage, capacity, ref cash, result);
                        if (second > 0 && second % 180 == 0) ApplyScenarioPressure(worker, scenario, i, false);
                    }
                    if (paused && !Same(pausedSnapshot, worker.State)) result.Invalid++;
                    Validate(worker, result);
                }
                for (int i = 0; i < usage.Length; i++)
                    if (usage[i] < 0 || usage[i] > capacity[i]) result.CapacityViolations++;
            }

            for (int i = 0; i < workers.Length; i++)
            {
                SimWorker worker = workers[i];
                if (worker.Reserved)
                {
                    usage[(int)worker.Activity]--;
                    worker.Reserved = false;
                    result.Released++;
                }
                if (!worker.Working && worker.ActivityRemaining <= 0f && worker.TravelRemaining <= 0f) result.FailedResume++;
                if (worker.HadDesk != (i < 3)) result.LostDesks++;
                if (worker.Phone) result.PhoneOutput += worker.Output;
                else result.WorkOutput += worker.Output;
            }
            for (int i = 0; i < usage.Length; i++) if (usage[i] != 0) result.Orphans += Mathf.Abs(usage[i]);
            result.Signature = Signature(workers, result);
            return result;
        }

        private static void TickWorker(SimWorker worker, AutonomyMatrixScenario scenario, int seed, int index,
            int second, int[] usage, int[] capacity, ref float cash, RunResult result)
        {
            WorkerState behavior = worker.Working ? (worker.Phone ? WorkerState.Unassigned : WorkerState.Work) :
                BehaviorFor(worker.Activity);
            NeedSimulation.Tick(worker.State, behavior, 1f);
            NeedStatus worst = NeedAutonomyRules.WorstStatus(worker.State);
            if (worst == NeedStatus.Critical)
            {
                if (worker.CriticalStarted < 0f) { worker.CriticalStarted = second; result.Critical++; }
            }

            if (worker.TravelRemaining > 0f)
            {
                worker.TravelRemaining = Mathf.Max(0f, worker.TravelRemaining - 1f);
                if (worker.TravelRemaining <= 0f) worker.ActivityRemaining = NeedAutonomyRules.ActivityDuration(worker.Activity);
                return;
            }
            if (worker.ActivityRemaining > 0f)
            {
                worker.ActivityRemaining = Mathf.Max(0f, worker.ActivityRemaining - 1f);
                if (worker.ActivityRemaining <= 0f)
                {
                    Complete(worker);
                    if (worker.Reserved)
                    {
                        usage[(int)worker.Activity]--;
                        worker.Reserved = false;
                        result.Released++;
                    }
                    worker.Working = true;
                    if (worker.CriticalStarted >= 0f)
                    {
                        float duration = second - worker.CriticalStarted;
                        result.Recovery += duration;
                        result.MaximumCritical = Mathf.Max(result.MaximumCritical, duration);
                        worker.CriticalStarted = -1f;
                        worker.ResponseStarted = -1f;
                    }
                }
                return;
            }

            if (worker.Working)
            {
                float workstation = worker.Phone ? ProductivityModel.PhoneWorkstationModifier : 1f;
                float focused = scenario == AutonomyMatrixScenario.ActiveManagement && second % 60 < 30 ? 1.2f : 1f;
                if (scenario == AutonomyMatrixScenario.ActiveManagement && second % 60 == 0) result.Interventions++;
                float output = ProductivityModel.Evaluate(1f, worker.State, workstation, 1f, focused);
                if (scenario == AutonomyMatrixScenario.HighDistractionFrequency && second % 20 < 4) output = 0f;
                worker.Output += output;
            }

            worker.EvaluationTimer -= 1f;
            if (worst == NeedStatus.Critical && worker.EvaluationTimer > NeedAutonomyRules.CriticalEvaluationMax)
                worker.EvaluationTimer = NeedAutonomyRules.CriticalEvaluationMax;
            if (worker.EvaluationTimer > 0f) return;
            worker.EvaluationTimer = NeedAutonomyRules.NextEvaluationDelay(worker.State,
                "Matrix Autonomy " + index, seed, worker.EvaluationIndex++);
            if (!NeedAutonomyRules.TrySelectPriorityNeed(worker.State, out NeedKind need,
                    out NeedStatus status, out _, out _)) return;

            List<NeedDestinationCandidate> candidates = BuildCandidates(worker, scenario, index, usage, capacity, cash, need);
            NeedDestinationCandidate selected = NeedAutonomyRules.SelectBest(worker.State, need, status, candidates);
            if (selected == null) { result.Reroutes++; return; }
            worker.Working = false;
            worker.Activity = selected.Activity;
            worker.Need = need;
            worker.TravelRemaining = Mathf.Max(1f, selected.PathCost / 2.25f);
            result.Activities[(int)selected.Activity]++;
            if (selected.Fallback == DecisionFallbackLevel.OffSite) result.Fallbacks++;
            if (selected.Activity != PlacementActivity.LeaveOffice)
            {
                usage[(int)selected.Activity]++;
                worker.Reserved = true;
                result.Created++;
            }
            if (selected.Activity == PlacementActivity.BuySnack)
            {
                if (cash < ActivityRules.SnackCost) result.DuplicateCharges++;
                else cash -= ActivityRules.SnackCost;
            }
            if (worker.CriticalStarted >= 0f && worker.ResponseStarted < 0f)
            {
                worker.ResponseStarted = second;
                result.Response += second - worker.CriticalStarted;
            }
        }

        private static List<NeedDestinationCandidate> BuildCandidates(SimWorker worker,
            AutonomyMatrixScenario scenario, int index, int[] usage, int[] capacity, float cash, NeedKind need)
        {
            List<NeedDestinationCandidate> candidates = new List<NeedDestinationCandidate>(7);
            PlacementActivity[] activities = { PlacementActivity.Rest, PlacementActivity.GetWater,
                PlacementActivity.BuySnack, PlacementActivity.GetCoffee, PlacementActivity.Smoke,
                PlacementActivity.UseRestroom, PlacementActivity.LeaveOffice };
            for (int i = 0; i < activities.Length; i++)
            {
                PlacementActivity activity = activities[i];
                if (!NeedAutonomyRules.ActivityImproves(activity, need)) continue;
                bool enabled = !(scenario == AutonomyMatrixScenario.DisabledRestroom && activity == PlacementActivity.UseRestroom);
                bool reachable = !(scenario == AutonomyMatrixScenario.UnreachablePreferredStation &&
                                   (activity == PlacementActivity.GetCoffee || activity == PlacementActivity.UseRestroom));
                bool capacityAvailable = activity == PlacementActivity.LeaveOffice || usage[(int)activity] < capacity[(int)activity];
                candidates.Add(new NeedDestinationCandidate
                {
                    Activity = activity,
                    StableId = "matrix." + activity.ToString().ToLowerInvariant(),
                    Enabled = enabled,
                    Reachable = reachable,
                    HasCapacity = capacityAvailable,
                    CooldownReady = true,
                    Affordable = activity != PlacementActivity.BuySnack || cash >= ActivityRules.SnackCost,
                    CashCost = activity == PlacementActivity.BuySnack ? ActivityRules.SnackCost : 0f,
                    Duration = NeedAutonomyRules.ActivityDuration(activity),
                    PathCost = 2f + index % 5 * .8f + i * .25f,
                    Reservations = usage[(int)activity],
                    Fallback = activity == PlacementActivity.LeaveOffice ? DecisionFallbackLevel.OffSite : DecisionFallbackLevel.None,
                    PreferenceBonus = worker.Phone && activity == PlacementActivity.Rest ? 24f : 0f
                });
            }
            return candidates;
        }

        private static void Complete(SimWorker worker)
        {
            switch (worker.Activity)
            {
                case PlacementActivity.Rest: ActivityRules.ApplyRest(worker.State); break;
                case PlacementActivity.GetWater: ActivityRules.ApplyWater(worker.State); break;
                case PlacementActivity.BuySnack: ActivityRules.ApplySnack(worker.State, false); break;
                case PlacementActivity.GetCoffee: ActivityRules.ApplyCoffee(worker.State, false); break;
                case PlacementActivity.Smoke: ActivityRules.ApplySmoke(worker.State); break;
                case PlacementActivity.UseRestroom: ActivityRules.ApplyRestroom(worker.State); break;
            }
        }

        private static WorkerState BehaviorFor(PlacementActivity activity)
        {
            switch (activity)
            {
                case PlacementActivity.Rest: return WorkerState.TakeBreak;
                case PlacementActivity.GetWater: return WorkerState.UseWaterCooler;
                case PlacementActivity.BuySnack: return WorkerState.BuySnack;
                case PlacementActivity.GetCoffee: return WorkerState.UseCoffeeMachine;
                case PlacementActivity.Smoke: return WorkerState.Smoke;
                case PlacementActivity.UseRestroom: return WorkerState.UseRestroom;
                case PlacementActivity.LeaveOffice: return WorkerState.Away;
                default: return WorkerState.IdleAtDesk;
            }
        }

        private static void ApplyScenarioPressure(SimWorker worker, AutonomyMatrixScenario scenario, int index, bool initial)
        {
            if (!initial && scenario == AutonomyMatrixScenario.NormalMixedNeeds)
            {
                NeedKind rotating = (NeedKind)(index % NeedCatalog.All.Length);
                worker.State.SetNeed(rotating, NeedCatalog.Get(rotating).HighIsGood ? .20f : .78f);
                return;
            }
            switch (scenario)
            {
                case AutonomyMatrixScenario.AllHungry:
                case AutonomyMatrixScenario.UnaffordableFood: worker.State.hunger = .92f; break;
                case AutonomyMatrixScenario.SharedRestroomDemand:
                case AutonomyMatrixScenario.DisabledRestroom:
                case AutonomyMatrixScenario.FullStationCapacity: worker.State.bathroom = .93f; break;
                case AutonomyMatrixScenario.LowEnergy:
                case AutonomyMatrixScenario.UnreachablePreferredStation: worker.State.energy = .08f; break;
                case AutonomyMatrixScenario.LowHappinessAndInspiration:
                    worker.State.happiness = .12f; worker.State.inspiration = .10f; break;
                case AutonomyMatrixScenario.NormalMixedNeeds:
                    worker.State.SetNeed((NeedKind)(index % NeedCatalog.All.Length), index % 2 == 0 ? .25f : .72f); break;
            }
        }

        private static void Validate(SimWorker worker, RunResult result)
        {
            for (int i = 0; i < NeedCatalog.All.Length; i++)
            {
                float value = worker.State.GetNeed(NeedCatalog.All[i].Kind);
                if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f || value > 1f) result.Invalid++;
            }
        }

        private static float[] Snapshot(WorkerRuntimeState state)
        {
            float[] values = new float[NeedCatalog.All.Length];
            for (int i = 0; i < values.Length; i++) values[i] = state.GetNeed(NeedCatalog.All[i].Kind);
            return values;
        }

        private static bool Same(float[] values, WorkerRuntimeState state)
        {
            for (int i = 0; i < values.Length; i++)
                if (Mathf.Abs(values[i] - state.GetNeed(NeedCatalog.All[i].Kind)) > .000001f) return false;
            return true;
        }

        private static string Signature(SimWorker[] workers, RunResult result)
        {
            long hash = 17;
            unchecked
            {
                for (int i = 0; i < workers.Length; i++)
                for (int n = 0; n < NeedCatalog.All.Length; n++)
                    hash = hash * 31 + Mathf.RoundToInt(workers[i].State.GetNeed(NeedCatalog.All[n].Kind) * 100000f);
                hash = hash * 31 + result.Created;
                hash = hash * 31 + result.Released;
                hash = hash * 31 + result.Fallbacks;
                hash = hash * 31 + Mathf.RoundToInt(result.WorkOutput * 100f);
                hash = hash * 31 + Mathf.RoundToInt(result.PhoneOutput * 100f);
            }
            return hash.ToString("X16");
        }

        private static void Accumulate(NeedAutonomyMatrixReport report, RunResult run, AutonomyMatrixScenario scenario)
        {
            report.CriticalNeeds += run.Critical;
            report.TotalCriticalResponseSeconds += run.Response;
            report.TotalRecoverySeconds += run.Recovery;
            report.MaximumCriticalSeconds = Mathf.Max(report.MaximumCriticalSeconds, run.MaximumCritical);
            for (int i = 0; i < run.Activities.Length; i++) report.ActivitySelections[i] += run.Activities[i];
            report.ReservationsCreated += run.Created;
            report.ReservationsReleased += run.Released;
            report.OffSiteFallbacks += run.Fallbacks;
            report.Reroutes += run.Reroutes;
            report.InvalidNeedValues += run.Invalid;
            report.OrphanedReservations += run.Orphans;
            report.CapacityViolations += run.CapacityViolations;
            report.FailedToResume += run.FailedResume;
            report.LostDeskAssignments += run.LostDesks;
            report.DuplicateCharges += run.DuplicateCharges;
            report.NeedInterventions += run.Interventions;
            report.WorkOutput += run.WorkOutput;
            report.PhoneOutput += run.PhoneOutput;
            if (scenario == AutonomyMatrixScenario.PassiveManagement) report.PassiveOutput += run.WorkOutput + run.PhoneOutput;
            if (scenario == AutonomyMatrixScenario.ActiveManagement) report.ActiveOutput += run.WorkOutput + run.PhoneOutput;
        }
    }
}
