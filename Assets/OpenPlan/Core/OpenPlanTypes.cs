using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenPlan
{
    public enum OfficeStage { StarterOffice, StarterOfficeExpanded, EstablishedOffice }

    public enum PlacementActivity { Work, Rest, GetWater, BuySnack, Smoke, LeaveOffice }

    public enum AwayReason { Lunch, Errand, LongBreak, OffSiteTask }

    public enum WorkerTrait { Focused, Social, Ambitious, Lazy, Anxious, Caffeinated, Hardworking }

    public enum DistractionKind
    {
        None, Phone, Wander, Confused, Sleep, ExtendedWater, ExtendedBreak,
        VendingInterest, ExtendedSmoke
    }

    public enum StatusEmote
    {
        Happy, Sad, Frustrated, Tired, Water, Snack, Cigarette, Money,
        Question, Exclamation, Social, Focus
    }

    public enum WorkerState
    {
        EnterOffice, WalkToDesk, Work, IdleAtDesk, SeekCoffee, UseCoffeeMachine,
        SeekWater, UseWaterCooler, SeekCoworker, Socialize, TakeBreak,
        WalkToMeeting, Meeting, ReturnToDesk, React, FiredReaction, PackDesk,
        CarryBox, ExitOffice, RecoverFromStuck, WalkToPlacement, BuySnack, Smoke,
        WalkOutForAway, Away, ReturnFromAway, LookAtPhone, Wander, StandConfused, Sleep
    }

    public enum StationKind { Coffee, Water, Break, Meeting, Elevator }

    [Serializable]
    public sealed class WorkerCommand
    {
        public WorkerAgent worker;
        public PlacementZone destinationZone;
        public PlacementActivity requestedActivity;
        public float issueTime;
        public bool fromPlayerPlacement;

        public WorkerCommand(WorkerAgent worker, PlacementZone destinationZone,
            PlacementActivity requestedActivity, float issueTime, bool fromPlayerPlacement)
        {
            this.worker = worker;
            this.destinationZone = destinationZone;
            this.requestedActivity = requestedActivity;
            this.issueTime = issueTime;
            this.fromPlayerPlacement = fromPlayerPlacement;
        }
    }

    [Serializable]
    public sealed class WorkerDefinition
    {
        public string displayName;
        public WorkerTrait trait;
        [Range(0.5f, 1.5f)] public float skill = 1f;
        public int salary = 220;
        public Color clothing = Color.cyan;
        [Range(0f, 1f)] public float sociability = 0.5f;
        public string strength;
        public string weakness;

        public WorkerDefinition Clone() => (WorkerDefinition)MemberwiseClone();
    }

    [Serializable]
    public sealed class WorkerRuntimeState
    {
        [Range(0f, 1f)] public float energy = 0.86f;
        [Range(0f, 1f)] public float mood = 0.78f;
        [Range(0f, 1f)] public float stress = 0.22f;
        [Range(0f, 1f)] public float socialNeed = 0.15f;
        public float effectiveProductivity = 1f;
        public float focusedWorkSecondsRemaining;
        public float waterCooldown;
        public float vendingCooldown;
        public float smokingCooldown;
        public float awaySecondsRemaining;
        public AwayReason awayReason;
        public float workSeconds;
        public float socialSeconds;
        public float lowEnergySeconds;
        public int autonomyDecisions;
        public int distractionsStarted;
        public int distractionsCompleted;
        public float distractionSeconds;
        public WorkerState behavior = WorkerState.EnterOffice;
        public string positiveInfluence = "Ready for the day";
        public string negativeInfluence = "Settling in";
    }

    [Serializable]
    public sealed class CandidateDefinition
    {
        public WorkerDefinition worker;
        public int hiringCost;
    }

    [Serializable]
    public sealed class TaskDefinition
    {
        public string title;
        public float workRequired;
        public int revenue;
        public string preference;
        public int priority;
    }

    [Serializable]
    public sealed class TaskRuntime
    {
        public TaskDefinition definition;
        public float progress;
    }

    [Serializable]
    public sealed class WorkdaySummary
    {
        public int revenue;
        public int payroll;
        public int hiringCosts;
        public int firingCosts;
        public int net;
        public int tasksCompleted;
        public float averageProductivity;
        public float socialSeconds;
        public float lowEnergySeconds;
        public string bestEmployee;
        public string leastProductiveEmployee;
        public int hires;
        public int firings;
        public bool targetReached;
    }

    public static class ProductivityModel
    {
        public const float Minimum = 0.10f;
        public const float Maximum = 2.50f;

        public static float EnergyModifier(float energy) => Mathf.Lerp(0.55f, 1.10f, Mathf.Clamp01(energy));
        public static float MoodModifier(float mood) => Mathf.Lerp(0.70f, 1.10f, Mathf.Clamp01(mood));
        public static float InverseStressModifier(float stress) => Mathf.Lerp(1.15f, 0.55f, Mathf.Clamp01(stress));
        public static float FocusedWorkModifier(float remainingSeconds) => remainingSeconds > 0f ? 1.20f : 1f;

        public static float TraitModifier(WorkerTrait trait, float noise, float companyProgress, float energy)
        {
            switch (trait)
            {
                case WorkerTrait.Focused: return noise < 0.35f ? 1.12f : 0.98f;
                case WorkerTrait.Social: return 0.98f;
                case WorkerTrait.Ambitious: return Mathf.Lerp(0.94f, 1.14f, Mathf.Clamp01(companyProgress));
                case WorkerTrait.Lazy: return 0.88f;
                case WorkerTrait.Anxious: return noise < 0.32f ? 1.16f : Mathf.Lerp(1.03f, 0.82f, noise);
                case WorkerTrait.Caffeinated: return energy > 0.72f ? 1.10f : 0.96f;
                case WorkerTrait.Hardworking: return noise < 0.35f ? 1.16f : Mathf.Lerp(1.08f, .86f, noise);
                default: return 1f;
            }
        }

        public static float Evaluate(float skill, float energy, float mood, float stress,
            float workstation, float trait, float manualFocusedWork)
        {
            float value = skill * EnergyModifier(energy) * MoodModifier(mood) *
                          InverseStressModifier(stress) * workstation * trait * manualFocusedWork;
            return Mathf.Clamp(value, Minimum, Maximum);
        }
    }

    public readonly struct PersonalityProfile
    {
        public readonly string Label;
        public readonly float DistractionChance;
        public readonly float WorkPreference;
        public readonly float NoiseStressMultiplier;
        public readonly float AvoidanceStressRecovery;
        public readonly float DecisionMin;
        public readonly float DecisionMax;

        public PersonalityProfile(string label, float distractionChance, float workPreference,
            float noiseStressMultiplier, float avoidanceStressRecovery, float decisionMin, float decisionMax)
        {
            Label = label;
            DistractionChance = distractionChance;
            WorkPreference = workPreference;
            NoiseStressMultiplier = noiseStressMultiplier;
            AvoidanceStressRecovery = avoidanceStressRecovery;
            DecisionMin = decisionMin;
            DecisionMax = decisionMax;
        }
    }

    /// <summary>Seeded, testable personality tuning used by live autonomy and soak tests.</summary>
    public static class PersonalityRules
    {
        public static PersonalityProfile For(WorkerTrait trait)
        {
            switch (trait)
            {
                case WorkerTrait.Hardworking: return new PersonalityProfile("Hardworking", .07f, .78f, 1.48f, .55f, 7.0f, 10.0f);
                case WorkerTrait.Social: return new PersonalityProfile("Social", .17f, .48f, 1.00f, 1.00f, 5.5f, 8.5f);
                case WorkerTrait.Lazy: return new PersonalityProfile("Lazy", .30f, .28f, .78f, 1.65f, 4.8f, 7.5f);
                default: return new PersonalityProfile(trait.ToString(), .14f, .52f, 1.00f, 1.00f, 5.5f, 8.5f);
            }
        }

        public static DistractionKind ChooseDistraction(WorkerTrait trait, SeededRandomService random)
        {
            float roll = random.Value();
            if (trait == WorkerTrait.Lazy)
            {
                if (roll < .26f) return DistractionKind.Sleep;
                if (roll < .54f) return DistractionKind.Wander;
                if (roll < .72f) return DistractionKind.ExtendedBreak;
                if (roll < .86f) return DistractionKind.Phone;
                if (roll < .94f) return DistractionKind.VendingInterest;
                return DistractionKind.Confused;
            }
            if (trait == WorkerTrait.Social)
            {
                if (roll < .34f) return DistractionKind.ExtendedWater;
                if (roll < .56f) return DistractionKind.Phone;
                if (roll < .75f) return DistractionKind.Wander;
                if (roll < .90f) return DistractionKind.Confused;
                return DistractionKind.ExtendedBreak;
            }
            if (trait == WorkerTrait.Hardworking)
            {
                if (roll < .36f) return DistractionKind.Phone;
                if (roll < .67f) return DistractionKind.Confused;
                if (roll < .88f) return DistractionKind.Wander;
                return DistractionKind.ExtendedBreak;
            }
            if (roll < .24f) return DistractionKind.Phone;
            if (roll < .47f) return DistractionKind.Wander;
            if (roll < .65f) return DistractionKind.Confused;
            if (roll < .80f) return DistractionKind.ExtendedWater;
            if (roll < .91f) return DistractionKind.ExtendedBreak;
            return DistractionKind.Sleep;
        }

        public static float DistractionDuration(WorkerTrait trait, DistractionKind kind)
        {
            float duration;
            switch (kind)
            {
                case DistractionKind.Confused: duration = 6f; break;
                case DistractionKind.Phone: duration = 8f; break;
                case DistractionKind.VendingInterest: duration = 8f; break;
                case DistractionKind.ExtendedWater: duration = trait == WorkerTrait.Social ? 16f : 11f; break;
                case DistractionKind.ExtendedBreak: duration = trait == WorkerTrait.Lazy ? 18f : 12f; break;
                case DistractionKind.ExtendedSmoke: duration = 18f; break;
                case DistractionKind.Sleep: duration = trait == WorkerTrait.Lazy ? 18f : 12f; break;
                default: duration = trait == WorkerTrait.Lazy ? 16f : trait == WorkerTrait.Hardworking ? 8f : 12f; break;
            }
            return Mathf.Clamp(duration, 6f, 18f);
        }

        public static int CountDistractions(WorkerTrait trait, int seed, int decisions)
        {
            SeededRandomService random = new SeededRandomService(seed);
            int count = 0;
            float chance = For(trait).DistractionChance;
            for (int i = 0; i < decisions; i++) if (random.Chance(chance)) count++;
            return count;
        }
    }

    /// <summary>One readable tuning table for all player-directed activity effects.</summary>
    public static class ActivityRules
    {
        public const float RestDuration = 20f;
        public const float WaterDuration = 6f;
        public const float VendingDuration = 8f;
        public const float SmokingDuration = 12f;
        public const float AwayDuration = 30f;
        public const float FocusedWorkDuration = 30f;
        public const float WaterCooldown = 35f;
        public const float VendingCooldown = 45f;
        public const float SmokingCooldown = 45f;
        public const float SnackCost = 15f;
        public const float VendingMalfunctionChance = .10f;
        public const float WorkEnergyDrainPerSecond = .0018f;
        public const float WorkStressGainPerSecond = .0012f;
        public const float HighStressThreshold = .70f;
        public const float HighStressMoodDrainPerSecond = .0005f;

        public static void ChangeNeeds(WorkerRuntimeState state, float energy, float mood, float stress)
        {
            if (state == null) return;
            state.energy = Mathf.Clamp01(state.energy + energy);
            state.mood = Mathf.Clamp01(state.mood + mood);
            state.stress = Mathf.Clamp01(state.stress + stress);
        }

        public static void ApplyRest(WorkerRuntimeState state) => ChangeNeeds(state, .35f, .12f, -.25f);
        public static void ApplyWater(WorkerRuntimeState state) => ChangeNeeds(state, .08f, .05f, -.05f);
        public static void ApplySnack(WorkerRuntimeState state, bool malfunction)
            => ChangeNeeds(state, malfunction ? .05f : .25f, malfunction ? -.05f : .15f, malfunction ? 0f : -.08f);
        public static void ApplySmoke(WorkerRuntimeState state) => ChangeNeeds(state, 0f, .05f, -.30f);
        public static void ApplyAwayStep(WorkerRuntimeState state, float simulationSeconds)
        {
            float fraction = Mathf.Max(0f, simulationSeconds) / AwayDuration;
            ChangeNeeds(state, .45f * fraction, .12f * fraction, -.35f * fraction);
        }
    }

    public static class SimulationRules
    {
        public static float DecayEnergy(float current, float deltaSeconds, float rate = .0018f)
            => Mathf.Clamp01(current - Mathf.Max(0f, deltaSeconds) * Mathf.Max(0f, rate));
        public static float RestoreCoffee(float current, bool caffeinated)
            => Mathf.Clamp01(current + (caffeinated ? .62f : .42f));
        public static float ChangeMood(float current, float amount) => Mathf.Clamp01(current + amount);
        public static bool CooldownReady(float cooldown) => cooldown <= 0f;
        public static float ScaledDelta(float unscaledDelta, float speed) => Mathf.Max(0f, unscaledDelta) * Mathf.Clamp(speed, 0f, 4f);
        public static float ClampZoom(float value, float close, float overview) => Mathf.Clamp(value, close, overview);
    }

    public sealed class SeededRandomService
    {
        private readonly System.Random random;
        public int Seed { get; }

        public SeededRandomService(int seed)
        {
            Seed = seed;
            random = new System.Random(seed);
        }

        public float Value() => (float)random.NextDouble();
        public float Range(float min, float max) => min + (max - min) * Value();
        public int Range(int min, int max) => random.Next(min, max);
        public bool Chance(float probability) => Value() < Mathf.Clamp01(probability);

        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
