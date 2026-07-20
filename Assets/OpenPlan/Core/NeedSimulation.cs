using System;
using UnityEngine;

namespace OpenPlan
{
    /// <summary>Single tuning catalog for the five live needs. Stress is deliberately separate.</summary>
    public static class NeedCatalog
    {
        public const float DefaultStress = .22f;
        public const float HighGoodCaution = .55f;
        public const float HighGoodUrgent = .32f;
        public const float HighGoodCritical = .15f;
        public const float UrgencyCaution = .44f;
        public const float UrgencyUrgent = .68f;
        public const float UrgencyCritical = .85f;

        private static readonly NeedDefinition[] definitions =
        {
            new NeedDefinition("happiness", NeedKind.Happiness, "Happiness", true, .78f, -.00018f,
                HighGoodCaution, HighGoodUrgent, HighGoodCritical,
                "General morale. Low Happiness gradually reduces productivity.",
                "Content", "Unhappy", "Very unhappy", "Miserable",
                "Rest, social time, snacks, and time away improve Happiness.", new Color(.88f, .38f, .58f),
                "Rest", "Water", "Vending", "Coffee", "Smoking", "Away"),
            new NeedDefinition("hunger", NeedKind.Hunger, "Hunger", false, .18f, .00045f,
                UrgencyCaution, UrgencyUrgent, UrgencyCritical,
                "Hunger is an urgency meter: higher values are worse.",
                "Fed", "Peckish", "Hungry", "Starving",
                "Buy a snack at the vending machine or spend time away.", new Color(.96f, .68f, .18f),
                "Vending", "Away"),
            new NeedDefinition("bathroom", NeedKind.Bathroom, "Bathroom", false, .15f, .00035f,
                UrgencyCaution, UrgencyUrgent, UrgencyCritical,
                "Bathroom is an urgency meter: higher values are worse.",
                "Comfortable", "Needs a break", "Urgent", "Critical",
                "Place the employee near the restroom entrance.", new Color(.22f, .76f, .82f),
                "Restroom", "Away"),
            new NeedDefinition("inspiration", NeedKind.Inspiration, "Inspiration", true, .72f, -.00026f,
                HighGoodCaution, HighGoodUrgent, HighGoodCritical,
                "Creative momentum. Low Inspiration gently reduces productive output.",
                "Inspired", "Flat", "Uninspired", "Drained",
                "Rest, coffee, social time, and time away restore Inspiration.", new Color(.48f, .62f, 1f),
                "Rest", "Water", "Coffee", "Smoking", "Social", "Away"),
            new NeedDefinition("energy", NeedKind.Energy, "Energy", true, .86f, -.00025f,
                HighGoodCaution, HighGoodUrgent, HighGoodCritical,
                "Physical stamina. Work drains Energy faster than passive time.",
                "Energized", "Tired", "Exhausted", "Spent",
                "Rest, coffee, or time away restores Energy.", new Color(.48f, .88f, .42f),
                "Rest", "Water", "Vending", "Coffee", "Away")
        };

        public static NeedDefinition[] All => definitions;

        public static NeedDefinition Get(NeedKind kind)
        {
            int index = (int)kind;
            if (index >= 0 && index < definitions.Length && definitions[index].Kind == kind)
                return definitions[index];
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown employee need");
        }

        public static NeedStatus Status(WorkerRuntimeState state, NeedKind kind)
            => Get(kind).Status(state == null ? Get(kind).DefaultValue : state.GetNeed(kind));

        public static float Benefit01(WorkerRuntimeState state, NeedKind kind)
        {
            NeedDefinition definition = Get(kind);
            float value = state == null ? definition.DefaultValue : state.GetNeed(kind);
            return definition.HighIsGood ? Mathf.Clamp01(value) : 1f - Mathf.Clamp01(value);
        }

        public static bool IsIncreaseBeneficial(NeedKind kind) => Get(kind).HighIsGood;

        public static string ChangeText(NeedKind kind, float amount)
        {
            NeedDefinition definition = Get(kind);
            bool improved = definition.HighIsGood ? amount > 0f : amount < 0f;
            return (improved ? "+ " : "- ") + definition.DisplayName;
        }

        public static void Initialize(WorkerRuntimeState state, string stableWorkerId, int campaignSeed)
        {
            if (state == null) return;
            uint hash = StableHash(stableWorkerId ?? string.Empty, campaignSeed);
            for (int i = 0; i < definitions.Length; i++)
            {
                hash = hash * 1664525u + 1013904223u;
                float offset = ((hash >> 8) & 0xffff) / 65535f * .04f - .02f;
                state.SetNeed(definitions[i].Kind, definitions[i].DefaultValue + offset);
            }
            hash = hash * 1664525u + 1013904223u;
            state.stress = Mathf.Clamp01(DefaultStress + (((hash >> 8) & 0xffff) / 65535f * .04f - .02f));
        }

        private static uint StableHash(string value, int seed)
        {
            uint hash = 2166136261u ^ unchecked((uint)seed);
            for (int i = 0; i < value.Length; i++)
            {
                hash ^= value[i];
                hash *= 16777619u;
            }
            return hash;
        }
    }

    /// <summary>Neutral extension points reserved for later qualification and incident prompts.</summary>
    public static class NeedModifierHooks
    {
        public static float QualificationMultiplier(WorkerRuntimeState state, NeedKind kind) => 1f;
        public static float IncidentMultiplier(WorkerRuntimeState state, NeedKind kind) => 1f;
    }

    /// <summary>The only continuous path that changes employee needs during gameplay.</summary>
    public static class NeedSimulation
    {
        public static void Tick(WorkerRuntimeState state, WorkerState behavior, float simulationSeconds,
            float workStressMultiplier = 1f)
        {
            if (state == null || simulationSeconds <= 0f || float.IsNaN(simulationSeconds) ||
                float.IsInfinity(simulationSeconds)) return;
            float dt = simulationSeconds;
            bool away = behavior == WorkerState.Away;
            bool activeWork = behavior == WorkerState.Work || behavior == WorkerState.Unassigned;
            bool restorative = behavior == WorkerState.TakeBreak || behavior == WorkerState.UseCoffeeMachine ||
                               behavior == WorkerState.UseWaterCooler || behavior == WorkerState.BuySnack ||
                               behavior == WorkerState.Smoke || behavior == WorkerState.UseRestroom;

            foreach (NeedDefinition definition in NeedCatalog.All)
            {
                float rate;
                if (away)
                {
                    switch (definition.Kind)
                    {
                        case NeedKind.Happiness: rate = .15f / ActivityRules.AwayDuration; break;
                        case NeedKind.Hunger: rate = -.35f / ActivityRules.AwayDuration; break;
                        case NeedKind.Bathroom: rate = -.40f / ActivityRules.AwayDuration; break;
                        case NeedKind.Inspiration: rate = .16f / ActivityRules.AwayDuration; break;
                        default: rate = .38f / ActivityRules.AwayDuration; break;
                    }
                }
                else
                {
                    rate = definition.PassiveChangePerSecond;
                    if (activeWork)
                    {
                        if (definition.Kind == NeedKind.Energy) rate *= 4.5f;
                        else if (definition.Kind == NeedKind.Inspiration) rate *= 1.85f;
                        else if (definition.Kind == NeedKind.Happiness) rate *= 1.55f;
                        else rate *= 1.10f;
                    }
                    else if (restorative && definition.HighIsGood)
                    {
                        rate *= .45f;
                    }
                }
                rate *= NeedModifierHooks.QualificationMultiplier(state, definition.Kind);
                rate *= NeedModifierHooks.IncidentMultiplier(state, definition.Kind);
                state.ChangeNeed(definition.Kind, rate * dt);
            }

            if (away) state.stress = Mathf.Clamp01(state.stress - (.35f / ActivityRules.AwayDuration) * dt);
            else if (activeWork) state.stress = Mathf.Clamp01(state.stress + ActivityRules.WorkStressGainPerSecond *
                Mathf.Max(0f, workStressMultiplier) * dt);
            state.ClampNeeds();
        }
    }
}
