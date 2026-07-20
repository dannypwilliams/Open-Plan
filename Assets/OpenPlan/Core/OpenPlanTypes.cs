using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenPlan
{
    public enum OfficeStage { StarterOffice, StarterOfficeExpanded, EstablishedOffice }

    public enum PlacementActivity { Work, Rest, GetWater, BuySnack, Smoke, LeaveOffice, UseRestroom, GetCoffee }

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
        Question, Exclamation, Social, Focus, Restroom
    }

    public enum WorkerState
    {
        EnterOffice, WalkToDesk, Work, IdleAtDesk, SeekCoffee, UseCoffeeMachine,
        SeekWater, UseWaterCooler, SeekCoworker, Socialize, TakeBreak,
        WalkToMeeting, Meeting, ReturnToDesk, React, FiredReaction, PackDesk,
        CarryBox, ExitOffice, RecoverFromStuck, WalkToPlacement, BuySnack, Smoke,
        WalkOutForAway, Away, ReturnFromAway, LookAtPhone, Wander, StandConfused, Sleep,
        Unassigned, UseRestroom
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
    public sealed class GroundPlacementCommand
    {
        public WorkerAgent worker;
        public Vector3 groundPoint;
        public float issueTime;
        public bool fromPlayerPlacement;

        public GroundPlacementCommand(WorkerAgent worker, Vector3 groundPoint, float issueTime, bool fromPlayerPlacement)
        {
            this.worker = worker;
            this.groundPoint = groundPoint;
            this.issueTime = issueTime;
            this.fromPlayerPlacement = fromPlayerPlacement;
        }
    }

    public readonly struct PlacementResult
    {
        public readonly Vector3 GroundPoint;
        public readonly PlacementZone InfluencingZone;
        public readonly bool IsWalkable;
        public readonly string RejectionReason;

        public bool IsValid => IsWalkable && string.IsNullOrEmpty(RejectionReason);

        public PlacementResult(Vector3 groundPoint, PlacementZone influencingZone, bool isWalkable, string rejectionReason)
        {
            GroundPoint = groundPoint;
            InfluencingZone = influencingZone;
            IsWalkable = isWalkable;
            RejectionReason = rejectionReason;
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
        [Range(0f, 1f)] public float happiness = 0.78f;
        [Range(0f, 1f)] public float hunger = 0.18f;
        [Range(0f, 1f)] public float bathroom = 0.15f;
        [Range(0f, 1f)] public float inspiration = 0.72f;
        [Range(0f, 1f)] public float energy = 0.86f;
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
        public WorkerDecisionRuntime decision = new WorkerDecisionRuntime();
        public WorkerState behavior = WorkerState.EnterOffice;
        public string positiveInfluence = "Ready for the day";
        public string negativeInfluence = "Settling in";

        /// <summary>Legacy presentation alias. Happiness is the only stored value.</summary>
        public float mood
        {
            get => happiness;
            set => happiness = Mathf.Clamp01(value);
        }

        public float GetNeed(NeedKind kind)
        {
            switch (kind)
            {
                case NeedKind.Happiness: return happiness;
                case NeedKind.Hunger: return hunger;
                case NeedKind.Bathroom: return bathroom;
                case NeedKind.Inspiration: return inspiration;
                case NeedKind.Energy: return energy;
                default: return 0f;
            }
        }

        public void SetNeed(NeedKind kind, float value)
        {
            value = float.IsNaN(value) || float.IsInfinity(value) ? NeedCatalog.Get(kind).DefaultValue : Mathf.Clamp01(value);
            switch (kind)
            {
                case NeedKind.Happiness: happiness = value; break;
                case NeedKind.Hunger: hunger = value; break;
                case NeedKind.Bathroom: bathroom = value; break;
                case NeedKind.Inspiration: inspiration = value; break;
                case NeedKind.Energy: energy = value; break;
            }
        }

        public void ChangeNeed(NeedKind kind, float amount) => SetNeed(kind, GetNeed(kind) + amount);

        public void ClampNeeds()
        {
            foreach (NeedDefinition definition in NeedCatalog.All)
                SetNeed(definition.Kind, GetNeed(definition.Kind));
            stress = float.IsNaN(stress) || float.IsInfinity(stress) ? NeedCatalog.DefaultStress : Mathf.Clamp01(stress);
        }
    }

    public enum EmployeeTraitPolarity { Strength, Liability }
    public enum NeedKind { Happiness, Hunger, Bathroom, Inspiration, Energy }
    public enum NeedStatus { Healthy, Caution, Urgent, Critical }
    public enum IncidentKind
    {
        PrinterJam, InternetOutage, PowerFailure, CoffeeSpill, WaterLeak, CloggedRestroom,
        BrokenChair, FireAlarm, ElevatorProblem, BlockedEntrance, CustomerComplaint, AnimalIntruder
    }

    [Serializable]
    public sealed class EmployeeTraitDefinition
    {
        public string id;
        public string displayName;
        public string description;
        public EmployeeTraitPolarity polarity;
        public float productivityModifier = 1f;
        public float needDecayModifier = 1f;
        public float walkSpeedModifier = 1f;
        public float incidentChanceModifier = 1f;

        public EmployeeTraitDefinition(string id, string displayName, string description, EmployeeTraitPolarity polarity,
            float productivityModifier = 1f, float needDecayModifier = 1f, float walkSpeedModifier = 1f,
            float incidentChanceModifier = 1f)
        {
            this.id = id;
            this.displayName = displayName;
            this.description = description;
            this.polarity = polarity;
            this.productivityModifier = productivityModifier;
            this.needDecayModifier = needDecayModifier;
            this.walkSpeedModifier = walkSpeedModifier;
            this.incidentChanceModifier = incidentChanceModifier;
        }
    }

    [Serializable]
    public sealed class NeedDefinition
    {
        public string Id { get; }
        public NeedKind Kind { get; }
        public string DisplayName { get; }
        public bool HighIsGood { get; }
        public float DefaultValue { get; }
        public float PassiveChangePerSecond { get; }
        public float CautionThreshold { get; }
        public float UrgentThreshold { get; }
        public float CriticalThreshold { get; }
        public string Description { get; }
        public string HealthyText { get; }
        public string CautionText { get; }
        public string UrgentText { get; }
        public string CriticalText { get; }
        public string RecoveryHint { get; }
        public Color UiColor { get; }
        public string[] ImprovingActivities { get; }
        public string HealthyTooltip => HealthyText + ". " + RecoveryHint;
        public string WarningTooltip => CautionText + ". " + RecoveryHint;
        public string CriticalTooltip => CriticalText + ". " + RecoveryHint;

        public NeedDefinition(string id, NeedKind kind, string displayName, bool highIsGood,
            float defaultValue, float passiveChangePerSecond, float cautionThreshold,
            float urgentThreshold, float criticalThreshold, string description,
            string healthyText, string cautionText, string urgentText, string criticalText,
            string recoveryHint, Color uiColor, params string[] improvingActivities)
        {
            Id = id;
            Kind = kind;
            DisplayName = displayName;
            HighIsGood = highIsGood;
            DefaultValue = defaultValue;
            PassiveChangePerSecond = passiveChangePerSecond;
            CautionThreshold = cautionThreshold;
            UrgentThreshold = urgentThreshold;
            CriticalThreshold = criticalThreshold;
            Description = description;
            HealthyText = healthyText;
            CautionText = cautionText;
            UrgentText = urgentText;
            CriticalText = criticalText;
            RecoveryHint = recoveryHint;
            UiColor = uiColor;
            ImprovingActivities = improvingActivities ?? Array.Empty<string>();
        }

        public NeedStatus Status(float value)
        {
            value = Mathf.Clamp01(value);
            if (HighIsGood)
            {
                if (value < CriticalThreshold) return NeedStatus.Critical;
                if (value < UrgentThreshold) return NeedStatus.Urgent;
                if (value < CautionThreshold) return NeedStatus.Caution;
                return NeedStatus.Healthy;
            }
            if (value > CriticalThreshold) return NeedStatus.Critical;
            if (value > UrgentThreshold) return NeedStatus.Urgent;
            if (value > CautionThreshold) return NeedStatus.Caution;
            return NeedStatus.Healthy;
        }

        public string StatusText(float value)
        {
            switch (Status(value))
            {
                case NeedStatus.Critical: return CriticalText;
                case NeedStatus.Urgent: return UrgentText;
                case NeedStatus.Caution: return CautionText;
                default: return HealthyText;
            }
        }
    }

    [Serializable]
    public sealed class IncidentDefinition
    {
        public string id;
        public IncidentKind kind;
        public string title;
        public float durationSeconds;
        public float productivityMultiplier;
        public float weight;
        public string[] responseLabels;
    }

    [Serializable]
    public sealed class FurnitureDefinition
    {
        public string id;
        public string displayName;
        public int purchaseCost;
        public Vector2 footprint;
        public PlacementActivity? providedActivity;
    }

    [Serializable]
    public sealed class OfficeUnitDefinition
    {
        public string id;
        public string displayName;
        public int purchaseCost;
        public string[] adjacentUnitIds;
        public Bounds buildableBounds;
    }

    [Serializable]
    public sealed class ContractDefinition
    {
        public string id;
        public string displayName;
        public float workRequired;
        public int cashReward;
        public int reputationReward;
        public float durationSeconds;
    }

    public readonly struct EmployeeQualificationPair
    {
        public readonly EmployeeTraitDefinition Strength;
        public readonly EmployeeTraitDefinition Liability;

        public EmployeeQualificationPair(EmployeeTraitDefinition strength, EmployeeTraitDefinition liability)
        {
            Strength = strength;
            Liability = liability;
        }
    }

    public static class EmployeeQualificationCatalog
    {
        public static readonly EmployeeTraitDefinition[] Strengths =
        {
            Strength("hard-worker", "Extremely Hard Worker", 1.15f), Strength("elite-graduate", "Elite Graduate", 1.10f),
            Strength("fast-learner", "Fast Learner", 1.06f), Strength("organized", "Organized", 1.08f),
            Strength("motivated", "Motivated", 1.09f), Strength("team-player", "Team Player", 1.04f),
            Strength("problem-solver", "Problem Solver", 1.08f), Strength("coffee-addict", "Coffee Addict", 1.05f),
            Strength("workaholic", "Workaholic", 1.12f), Strength("tech-savvy", "Tech Savvy", 1.09f),
            Strength("people-person", "People Person", 1.04f), Strength("quick-on-feet", "Quick on Their Feet", 1.03f, 1.12f)
        };

        public static readonly EmployeeTraitDefinition[] Liabilities =
        {
            Liability("dropout", "High School Dropout", .94f), Liability("lazy", "Lazy", .86f),
            Liability("distracted", "Easily Distracted", .93f), Liability("frequently-hungry", "Frequently Hungry", .96f, 1.18f),
            Liability("slow-walker", "Slow Walker", .98f, 1f, .78f), Liability("messy", "Messy", .95f),
            Liability("procrastinator", "Procrastinator", .90f), Liability("heavy-smoker", "Heavy Smoker", .94f, 1.12f),
            Liability("constant-breaks", "Needs Constant Breaks", .91f, 1.17f), Liability("clumsy", "Clumsy", .96f, 1f, 1f, 1.45f),
            Liability("technophobe", "Technophobe", .92f), Liability("office-gossip", "Office Gossip", .94f)
        };

        public static EmployeeQualificationPair Roll(SeededRandomService random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            return new EmployeeQualificationPair(Strengths[random.Range(0, Strengths.Length)],
                Liabilities[random.Range(0, Liabilities.Length)]);
        }

        private static EmployeeTraitDefinition Strength(string id, string name, float productivity,
            float walkSpeed = 1f) => new EmployeeTraitDefinition(id, name, name, EmployeeTraitPolarity.Strength,
                productivity, 1f, walkSpeed, 1f);

        private static EmployeeTraitDefinition Liability(string id, string name, float productivity,
            float needDecay = 1f, float walkSpeed = 1f, float incidentChance = 1f) =>
            new EmployeeTraitDefinition(id, name, name, EmployeeTraitPolarity.Liability,
                productivity, needDecay, walkSpeed, incidentChance);
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
        public const float PhoneWorkstationModifier = 0.50f;

        public static float EnergyModifier(float energy) => Mathf.Lerp(0.55f, 1.10f, Mathf.Clamp01(energy));
        public static float MoodModifier(float mood) => Mathf.Lerp(0.70f, 1.10f, Mathf.Clamp01(mood));
        public static float InspirationModifier(float inspiration) => Mathf.Lerp(.78f, 1.08f, Mathf.Clamp01(inspiration));
        public static float UrgencyModifier(float urgency)
        {
            urgency = Mathf.Clamp01(urgency);
            if (urgency <= NeedCatalog.UrgencyCaution) return 1f;
            if (urgency <= NeedCatalog.UrgencyUrgent)
                return Mathf.Lerp(1f, .88f, Mathf.InverseLerp(NeedCatalog.UrgencyCaution, NeedCatalog.UrgencyUrgent, urgency));
            if (urgency <= NeedCatalog.UrgencyCritical)
                return Mathf.Lerp(.88f, .70f, Mathf.InverseLerp(NeedCatalog.UrgencyUrgent, NeedCatalog.UrgencyCritical, urgency));
            return Mathf.Lerp(.70f, .55f, Mathf.InverseLerp(NeedCatalog.UrgencyCritical, 1f, urgency));
        }
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

        public static float Evaluate(float skill, WorkerRuntimeState needs,
            float workstation, float trait, float manualFocusedWork)
        {
            if (needs == null) return Minimum;
            float value = skill * EnergyModifier(needs.energy) * MoodModifier(needs.happiness) *
                          InspirationModifier(needs.inspiration) * UrgencyModifier(needs.hunger) *
                          UrgencyModifier(needs.bathroom) * InverseStressModifier(needs.stress) *
                          workstation * trait * manualFocusedWork;
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
        public const float CoffeeDuration = 2.8f;
        public const float VendingDuration = 8f;
        public const float SmokingDuration = 12f;
        public const float RestroomDuration = 8f;
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

        public static void ChangeNeeds(WorkerRuntimeState state, float energy, float happiness, float stress)
        {
            if (state == null) return;
            state.ChangeNeed(NeedKind.Energy, energy);
            state.ChangeNeed(NeedKind.Happiness, happiness);
            state.stress = Mathf.Clamp01(state.stress + stress);
        }

        public static void ApplyRest(WorkerRuntimeState state)
        {
            ChangeNeeds(state, .32f, .14f, -.22f);
            state?.ChangeNeed(NeedKind.Inspiration, .12f);
        }

        public static void ApplyWater(WorkerRuntimeState state)
        {
            ChangeNeeds(state, .06f, .04f, -.04f);
            state?.ChangeNeed(NeedKind.Inspiration, .03f);
            state?.ChangeNeed(NeedKind.Bathroom, .08f);
        }

        public static void ApplySnack(WorkerRuntimeState state, bool malfunction)
        {
            if (state == null) return;
            ChangeNeeds(state, malfunction ? .01f : .06f, malfunction ? -.04f : .08f, malfunction ? 0f : -.05f);
            state.ChangeNeed(NeedKind.Hunger, malfunction ? -.08f : -.72f);
        }

        public static void ApplyCoffee(WorkerRuntimeState state, bool caffeinated)
        {
            if (state == null) return;
            ChangeNeeds(state, caffeinated ? .50f : .34f, .04f, -.08f);
            state.ChangeNeed(NeedKind.Inspiration, .12f);
            state.ChangeNeed(NeedKind.Bathroom, .06f);
        }

        public static void ApplySmoke(WorkerRuntimeState state)
        {
            ChangeNeeds(state, 0f, .07f, -.30f);
            state?.ChangeNeed(NeedKind.Inspiration, .06f);
        }

        public static void ApplyRestroom(WorkerRuntimeState state)
        {
            if (state == null) return;
            ChangeNeeds(state, 0f, .02f, -.04f);
            state.ChangeNeed(NeedKind.Bathroom, -.78f);
        }

        public static void ApplySocialStep(WorkerRuntimeState state, float simulationSeconds)
        {
            if (state == null || simulationSeconds <= 0f) return;
            state.ChangeNeed(NeedKind.Happiness, simulationSeconds * .018f);
            state.ChangeNeed(NeedKind.Inspiration, simulationSeconds * .008f);
            state.stress = Mathf.Clamp01(state.stress - simulationSeconds * .009f);
        }

        public static void ApplyAwayStep(WorkerRuntimeState state, float simulationSeconds)
        {
            NeedSimulation.Tick(state, WorkerState.Away, simulationSeconds);
        }
    }

    public static class ExpansionRules
    {
        public const float PurchasePrice = 1000f;
        public const int AdditionalDeskCapacity = 3;
        public const float ExpectedSnackOverheadPerMinute = 5.4f;

        public static bool CanPurchase(float cash, bool alreadyExpanded)
            => !alreadyExpanded && cash + .0001f >= PurchasePrice;

        public static float PurchaseProgress(float cash)
            => Mathf.Clamp01(Mathf.Max(0f, cash) / PurchasePrice);

        public static float ExpectedStartingIncomePerMinute()
        {
            float morgan = 1.34f * .94f;
            float alex = .98f * .81f;
            float sam = .72f * .60f;
            return (morgan + alex + sam) * CashDirector.IncomePerProductivityMinute;
        }

        public static float ExpectedMinutesToAfford(float startingCash = CashDirector.StartingCash)
        {
            float remaining = Mathf.Max(0f, PurchasePrice - startingCash);
            float expectedNetIncome = Mathf.Max(1f,
                ExpectedStartingIncomePerMinute() - ExpectedSnackOverheadPerMinute);
            return remaining / expectedNetIncome;
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
