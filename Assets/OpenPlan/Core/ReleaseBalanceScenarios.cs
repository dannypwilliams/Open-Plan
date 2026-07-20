using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenPlan
{
    public enum ReleaseScenarioMode { Active, Passive, Poor, Recovery, Expansion }

    /// <summary>One deterministic release-balance result produced from the public tuning tables.</summary>
    public sealed class ReleaseScenarioResult
    {
        public ReleaseScenarioMode mode;
        public int seed;
        public float timeToOneThousandSeconds;
        public float elapsedSeconds;
        public float earnings;
        public float vendingSpend;
        public float averageProductivity;
        public float workingSeconds;
        public float distractedSeconds;
        public float restorativeSeconds;
        public int commandsIssued;
        public float focusedUptime;
        public bool expansionComplete;
        public bool newHirePlaced;
        public bool permanentlyStuck;
        public int recoveries;
        public float recoveryProductivityBefore;
        public float recoveryProductivityAfter;
    }

    /// <summary>
    /// Fast, seeded friend-demo scenarios. The model deliberately calls the same ActivityRules,
    /// ProductivityModel, PersonalityRules, CashDirector, and ExpansionRules contracts as live play.
    /// It is a balance gate, not a replacement for packaged gameplay and soak verification.
    /// </summary>
    public static class ReleaseBalanceScenarios
    {
        private sealed class SimWorker
        {
            public WorkerTrait trait;
            public float skill;
            public float workstation;
            public WorkerRuntimeState needs;
            public float busyRemaining;
            public float distractionAge;
            public bool distracted;
            public bool restorative;
            public bool away;
            public float nextDecision;
            public float lastProductiveSecond;
        }

        public static readonly int[] FixedSeeds =
        {
            19680412, 73, 211, 509, 997, 1291, 1879, 2477, 3253, 4099,
            5003, 6007, 7013, 8017, 9011, 10009, 11003, 12007, 13001, 14009
        };

        public static ReleaseScenarioResult Run(ReleaseScenarioMode mode, int seed)
        {
            SeededRandomService random = new SeededRandomService(seed);
            var workers = new List<SimWorker>
            {
                NewWorker(WorkerTrait.Hardworking, 1.34f, .77f, random),
                NewWorker(WorkerTrait.Social, .98f, .81f, random),
                NewWorker(WorkerTrait.Lazy, .72f, .75f, random)
            };
            if (mode == ReleaseScenarioMode.Recovery)
            {
                foreach (SimWorker worker in workers)
                {
                    worker.needs.energy = .20f;
                    worker.needs.mood = .38f;
                    worker.needs.stress = .86f;
                }
            }

            var result = new ReleaseScenarioResult { mode = mode, seed = seed, timeToOneThousandSeconds = -1f };
            float cash = CashDirector.StartingCash;
            float productivityTotal = 0f;
            float recoveryBeforeTotal = 0f;
            float recoveryAfterTotal = 0f;
            int recoveryBeforeSamples = 0;
            int recoveryAfterSamples = 0;
            float nextManagerAction = mode == ReleaseScenarioMode.Recovery ? 0f : 18f;
            int managerWorker = 0;
            float expansionTime = -1f;
            float hireTime = -1f;
            const int maximumSeconds = 20 * 60;

            for (int second = 0; second < maximumSeconds; second++)
            {
                result.elapsedSeconds = second + 1f;
                foreach (SimWorker worker in workers)
                {
                    worker.needs.focusedWorkSecondsRemaining = Mathf.Max(0f, worker.needs.focusedWorkSecondsRemaining - 1f);
                    worker.needs.waterCooldown = Mathf.Max(0f, worker.needs.waterCooldown - 1f);
                    worker.needs.vendingCooldown = Mathf.Max(0f, worker.needs.vendingCooldown - 1f);
                    worker.needs.smokingCooldown = Mathf.Max(0f, worker.needs.smokingCooldown - 1f);
                    if (worker.busyRemaining <= 0f) continue;
                    worker.busyRemaining -= 1f;
                    if (worker.distracted)
                    {
                        worker.distractionAge += 1f;
                        result.distractedSeconds += 1f;
                    }
                    if (worker.restorative)
                    {
                        result.restorativeSeconds += 1f;
                        if (worker.away) ActivityRules.ApplyAwayStep(worker.needs, 1f);
                    }
                    if (worker.busyRemaining <= 0f)
                    {
                        worker.distracted = false;
                        worker.restorative = false;
                        worker.away = false;
                        worker.distractionAge = 0f;
                    }
                }

                if (mode == ReleaseScenarioMode.Recovery && second == 0)
                {
                    foreach (SimWorker worker in workers) StartRest(worker);
                    result.commandsIssued += workers.Count;
                    nextManagerAction = 24f;
                }
                else if ((mode == ReleaseScenarioMode.Active || mode == ReleaseScenarioMode.Expansion ||
                          mode == ReleaseScenarioMode.Recovery) && second >= nextManagerAction)
                {
                    SimWorker worker = workers[managerWorker++ % workers.Count];
                    if (worker.needs.energy < .42f || worker.needs.stress > .68f) StartRest(worker);
                    else StartFocusedWork(worker);
                    result.commandsIssued++;
                    nextManagerAction += 45f;
                }
                else if (mode == ReleaseScenarioMode.Poor && second >= nextManagerAction)
                {
                    SimWorker worker = workers[managerWorker++ % workers.Count];
                    float choice = random.Value();
                    if (choice < .34f) StartAway(worker);
                    else if (choice < .61f) StartRest(worker);
                    else if (choice < .86f && cash >= ActivityRules.SnackCost)
                    {
                        cash -= ActivityRules.SnackCost;
                        result.vendingSpend += ActivityRules.SnackCost;
                        StartVending(worker, random);
                    }
                    else StartSmoke(worker);
                    result.commandsIssued++;
                    nextManagerAction += 24f;
                }

                bool intervention = mode == ReleaseScenarioMode.Active || mode == ReleaseScenarioMode.Expansion ||
                                    mode == ReleaseScenarioMode.Recovery;
                foreach (SimWorker worker in workers)
                {
                    if (intervention && worker.distracted && worker.distractionAge >= 4f)
                    {
                        worker.busyRemaining = 0f;
                        worker.distracted = false;
                        worker.distractionAge = 0f;
                        StartFocusedWork(worker);
                        result.commandsIssued++;
                        result.recoveries++;
                    }

                    if (worker.busyRemaining <= 0f && second >= worker.nextDecision)
                    {
                        PersonalityProfile profile = PersonalityRules.For(worker.trait);
                        worker.nextDecision = second + random.Range(profile.DecisionMin, profile.DecisionMax);
                        float chance = profile.DistractionChance * (mode == ReleaseScenarioMode.Poor ? 1.18f : 1f);
                        if (random.Chance(chance))
                        {
                            DistractionKind kind = PersonalityRules.ChooseDistraction(worker.trait, random);
                            worker.busyRemaining = PersonalityRules.DistractionDuration(worker.trait, kind);
                            worker.distracted = true;
                            worker.distractionAge = 0f;
                        }
                    }

                    if (worker.busyRemaining <= 0f && (worker.needs.energy < .12f || worker.needs.stress > .92f))
                    {
                        StartRest(worker);
                        result.recoveries++;
                    }
                }

                float secondProductivity = 0f;
                foreach (SimWorker worker in workers)
                {
                    if (worker.busyRemaining > 0f) continue;
                    float noise = worker.trait == WorkerTrait.Hardworking ? .28f : .42f;
                    float trait = ProductivityModel.TraitModifier(worker.trait, noise, .5f, worker.needs.energy);
                    float focused = ProductivityModel.FocusedWorkModifier(worker.needs.focusedWorkSecondsRemaining);
                    float productivity = ProductivityModel.Evaluate(worker.skill, worker.needs.energy, worker.needs.mood,
                        worker.needs.stress, worker.workstation, trait, focused);
                    secondProductivity += productivity;
                    result.workingSeconds += 1f;
                    if (focused > 1f) result.focusedUptime += 1f;
                    worker.lastProductiveSecond = second;
                    worker.needs.energy = SimulationRules.DecayEnergy(worker.needs.energy, 1f, ActivityRules.WorkEnergyDrainPerSecond);
                    ActivityRules.ChangeNeeds(worker.needs, 0f, worker.needs.stress >= ActivityRules.HighStressThreshold ?
                        -ActivityRules.HighStressMoodDrainPerSecond : 0f, ActivityRules.WorkStressGainPerSecond);
                }
                cash += secondProductivity * CashDirector.IncomePerProductivityMinute / 60f;
                result.earnings += secondProductivity * CashDirector.IncomePerProductivityMinute / 60f;
                productivityTotal += secondProductivity;
                if (mode == ReleaseScenarioMode.Recovery && second < 20)
                {
                    recoveryBeforeTotal += secondProductivity;
                    recoveryBeforeSamples++;
                }
                if (mode == ReleaseScenarioMode.Recovery && second >= 25 && second < 85)
                {
                    recoveryAfterTotal += secondProductivity;
                    recoveryAfterSamples++;
                }

                if (result.timeToOneThousandSeconds < 0f && cash >= ExpansionRules.PurchasePrice)
                    result.timeToOneThousandSeconds = second + 1f;

                if (mode == ReleaseScenarioMode.Expansion && expansionTime < 0f && cash >= ExpansionRules.PurchasePrice)
                {
                    cash -= ExpansionRules.PurchasePrice;
                    result.expansionComplete = true;
                    expansionTime = second + 1f;
                }
                if (mode == ReleaseScenarioMode.Expansion && expansionTime >= 0f && hireTime < 0f && cash >= 380f)
                {
                    cash -= 380f;
                    workers.Add(NewWorker(WorkerTrait.Focused, .94f, .90f, random));
                    result.newHirePlaced = true;
                    hireTime = second + 1f;
                }

                bool basicComplete = mode != ReleaseScenarioMode.Expansion && result.timeToOneThousandSeconds >= 0f;
                bool expansionComplete = mode == ReleaseScenarioMode.Expansion && hireTime >= 0f && second + 1f >= hireTime + 120f;
                if (basicComplete || expansionComplete) break;
            }

            result.averageProductivity = productivityTotal / Mathf.Max(1f, result.elapsedSeconds);
            result.focusedUptime /= Mathf.Max(1f, result.workingSeconds);
            result.recoveryProductivityBefore = recoveryBeforeTotal / Mathf.Max(1, recoveryBeforeSamples);
            result.recoveryProductivityAfter = recoveryAfterTotal / Mathf.Max(1, recoveryAfterSamples);
            foreach (SimWorker worker in workers)
                if (result.elapsedSeconds - worker.lastProductiveSecond > 180f) result.permanentlyStuck = true;
            return result;
        }

        public static List<ReleaseScenarioResult> RunMatrix()
        {
            var results = new List<ReleaseScenarioResult>();
            foreach (int seed in FixedSeeds)
                foreach (ReleaseScenarioMode mode in Enum.GetValues(typeof(ReleaseScenarioMode)))
                    results.Add(Run(mode, seed));
            return results;
        }

        private static SimWorker NewWorker(WorkerTrait trait, float skill, float workstation, SeededRandomService random)
        {
            PersonalityProfile profile = PersonalityRules.For(trait);
            return new SimWorker
            {
                trait = trait,
                skill = skill,
                workstation = workstation,
                needs = new WorkerRuntimeState { energy = .86f, mood = .78f, stress = .22f },
                nextDecision = random.Range(profile.DecisionMin, profile.DecisionMax),
                lastProductiveSecond = 0f
            };
        }

        private static void StartFocusedWork(SimWorker worker)
        {
            worker.busyRemaining = 0f;
            worker.distracted = false;
            worker.restorative = false;
            worker.away = false;
            worker.needs.focusedWorkSecondsRemaining = ActivityRules.FocusedWorkDuration;
        }

        private static void StartRest(SimWorker worker)
        {
            worker.busyRemaining = ActivityRules.RestDuration;
            worker.restorative = true;
            worker.distracted = false;
            worker.away = false;
            ActivityRules.ApplyRest(worker.needs);
        }

        private static void StartVending(SimWorker worker, SeededRandomService random)
        {
            worker.busyRemaining = ActivityRules.VendingDuration;
            worker.restorative = true;
            worker.distracted = false;
            worker.away = false;
            ActivityRules.ApplySnack(worker.needs, random.Chance(ActivityRules.VendingMalfunctionChance));
            worker.needs.vendingCooldown = ActivityRules.VendingCooldown;
        }

        private static void StartSmoke(SimWorker worker)
        {
            worker.busyRemaining = ActivityRules.SmokingDuration;
            worker.restorative = true;
            worker.distracted = false;
            worker.away = false;
            ActivityRules.ApplySmoke(worker.needs);
            worker.needs.smokingCooldown = ActivityRules.SmokingCooldown;
        }

        private static void StartAway(SimWorker worker)
        {
            worker.busyRemaining = ActivityRules.AwayDuration;
            worker.restorative = true;
            worker.distracted = false;
            worker.away = true;
        }
    }
}
