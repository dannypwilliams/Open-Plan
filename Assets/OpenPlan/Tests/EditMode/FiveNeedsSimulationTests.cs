using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace OpenPlan.Tests
{
    public sealed class FiveNeedsSimulationTests
    {
        [Test] public void CatalogContainsExactlyFiveUniqueDefinitions()
        {
            Assert.That(NeedCatalog.All.Length, Is.EqualTo(5));
            Assert.That(NeedCatalog.All.Select(value => value.Id).Distinct().Count(), Is.EqualTo(5));
            Assert.That(NeedCatalog.All.Select(value => value.Kind).Distinct().Count(), Is.EqualTo(5));
        }

        [Test] public void CatalogDefaultsMatchFiveNeedsContract()
        {
            Assert.That(NeedCatalog.Get(NeedKind.Happiness).DefaultValue, Is.EqualTo(.78f));
            Assert.That(NeedCatalog.Get(NeedKind.Hunger).DefaultValue, Is.EqualTo(.18f));
            Assert.That(NeedCatalog.Get(NeedKind.Bathroom).DefaultValue, Is.EqualTo(.15f));
            Assert.That(NeedCatalog.Get(NeedKind.Inspiration).DefaultValue, Is.EqualTo(.72f));
            Assert.That(NeedCatalog.Get(NeedKind.Energy).DefaultValue, Is.EqualTo(.86f));
            Assert.That(NeedCatalog.DefaultStress, Is.EqualTo(.22f));
        }

        [Test] public void PositiveNeedsAreHighGood()
        {
            Assert.True(NeedCatalog.Get(NeedKind.Happiness).HighIsGood);
            Assert.True(NeedCatalog.Get(NeedKind.Inspiration).HighIsGood);
            Assert.True(NeedCatalog.Get(NeedKind.Energy).HighIsGood);
        }

        [Test] public void UrgencyNeedsAreLowGood()
        {
            Assert.False(NeedCatalog.Get(NeedKind.Hunger).HighIsGood);
            Assert.False(NeedCatalog.Get(NeedKind.Bathroom).HighIsGood);
        }

        [Test] public void HighGoodThresholdsAreCentralized()
        {
            NeedDefinition value = NeedCatalog.Get(NeedKind.Energy);
            Assert.That(value.CautionThreshold, Is.EqualTo(.55f));
            Assert.That(value.UrgentThreshold, Is.EqualTo(.32f));
            Assert.That(value.CriticalThreshold, Is.EqualTo(.15f));
        }

        [Test] public void UrgencyThresholdsAreCentralized()
        {
            NeedDefinition value = NeedCatalog.Get(NeedKind.Hunger);
            Assert.That(value.CautionThreshold, Is.EqualTo(.44f));
            Assert.That(value.UrgentThreshold, Is.EqualTo(.68f));
            Assert.That(value.CriticalThreshold, Is.EqualTo(.85f));
        }

        [Test] public void BoundaryStatusRulesAreDeterministic()
        {
            NeedDefinition energy = NeedCatalog.Get(NeedKind.Energy);
            NeedDefinition hunger = NeedCatalog.Get(NeedKind.Hunger);
            Assert.That(energy.Status(.55f), Is.EqualTo(NeedStatus.Healthy));
            Assert.That(energy.Status(.549f), Is.EqualTo(NeedStatus.Caution));
            Assert.That(hunger.Status(.44f), Is.EqualTo(NeedStatus.Healthy));
            Assert.That(hunger.Status(.441f), Is.EqualTo(NeedStatus.Caution));
        }

        [Test] public void MoodIsAnAliasForHappiness()
        {
            var state = new WorkerRuntimeState { happiness = .31f };
            Assert.That(state.mood, Is.EqualTo(.31f));
            state.mood = .64f;
            Assert.That(state.happiness, Is.EqualTo(.64f));
        }

        [Test] public void GetSetAndChangeCoverEveryNeed()
        {
            var state = new WorkerRuntimeState();
            foreach (NeedDefinition definition in NeedCatalog.All)
            {
                state.SetNeed(definition.Kind, .25f);
                state.ChangeNeed(definition.Kind, .20f);
                Assert.That(state.GetNeed(definition.Kind), Is.EqualTo(.45f).Within(.0001f), definition.Kind.ToString());
            }
        }

        [Test] public void SetAndChangeClampEveryNeed()
        {
            var state = new WorkerRuntimeState();
            foreach (NeedDefinition definition in NeedCatalog.All)
            {
                state.SetNeed(definition.Kind, 2f);
                Assert.That(state.GetNeed(definition.Kind), Is.EqualTo(1f));
                state.ChangeNeed(definition.Kind, -3f);
                Assert.That(state.GetNeed(definition.Kind), Is.Zero);
            }
        }

        [Test] public void InvalidNumbersRecoverToDefaults()
        {
            var state = new WorkerRuntimeState();
            state.SetNeed(NeedKind.Hunger, float.NaN);
            state.stress = float.PositiveInfinity;
            state.ClampNeeds();
            Assert.That(state.hunger, Is.EqualTo(.18f));
            Assert.That(state.stress, Is.EqualTo(.22f));
        }

        [Test] public void SeededInitializationRepeats()
        {
            var first = new WorkerRuntimeState();
            var second = new WorkerRuntimeState();
            NeedCatalog.Initialize(first, "Morgan", 4242);
            NeedCatalog.Initialize(second, "Morgan", 4242);
            foreach (NeedDefinition definition in NeedCatalog.All)
                Assert.That(first.GetNeed(definition.Kind), Is.EqualTo(second.GetNeed(definition.Kind)));
            Assert.That(first.stress, Is.EqualTo(second.stress));
        }

        [Test] public void SeededInitializationVariesByWorkerIdentity()
        {
            var first = new WorkerRuntimeState();
            var second = new WorkerRuntimeState();
            NeedCatalog.Initialize(first, "Morgan", 4242);
            NeedCatalog.Initialize(second, "Alex", 4242);
            Assert.That(first.happiness, Is.Not.EqualTo(second.happiness));
        }

        [Test] public void SeededOffsetsRemainSmallAndHealthy()
        {
            var state = new WorkerRuntimeState();
            NeedCatalog.Initialize(state, "Sam", 4242);
            foreach (NeedDefinition definition in NeedCatalog.All)
            {
                Assert.That(Mathf.Abs(state.GetNeed(definition.Kind) - definition.DefaultValue), Is.LessThanOrEqualTo(.0201f));
                Assert.That(definition.Status(state.GetNeed(definition.Kind)), Is.EqualTo(NeedStatus.Healthy));
            }
        }

        [Test] public void PassiveTickMovesAllNeedsInTheirContractDirection()
        {
            var state = Defaults();
            NeedSimulation.Tick(state, WorkerState.IdleAtDesk, 10f);
            Assert.Less(state.happiness, .78f);
            Assert.Greater(state.hunger, .18f);
            Assert.Greater(state.bathroom, .15f);
            Assert.Less(state.inspiration, .72f);
            Assert.Less(state.energy, .86f);
        }

        [Test] public void WorkDrainsEnergyFasterThanIdle()
        {
            var idle = Defaults();
            var work = Defaults();
            NeedSimulation.Tick(idle, WorkerState.IdleAtDesk, 30f);
            NeedSimulation.Tick(work, WorkerState.Work, 30f);
            Assert.Less(work.energy, idle.energy);
        }

        [Test] public void PhoneWorkUsesTheSameContinuousNeedPathAsDeskWork()
        {
            var desk = Defaults();
            var phone = Defaults();
            NeedSimulation.Tick(desk, WorkerState.Work, 20f);
            NeedSimulation.Tick(phone, WorkerState.Unassigned, 20f);
            foreach (NeedDefinition definition in NeedCatalog.All)
                Assert.That(phone.GetNeed(definition.Kind), Is.EqualTo(desk.GetNeed(definition.Kind)).Within(.0001f));
        }

        [Test] public void ZeroDeltaFreezesNeedsAndStress()
        {
            var state = Defaults();
            NeedSimulation.Tick(state, WorkerState.Work, 0f);
            AssertState(state, .78f, .18f, .15f, .72f, .86f, .22f);
        }

        [Test] public void SimulationSpeedIsProportionalThroughScaledDelta()
        {
            var one = Defaults();
            var four = Defaults();
            NeedSimulation.Tick(one, WorkerState.Work, SimulationRules.ScaledDelta(1f, 1f));
            NeedSimulation.Tick(four, WorkerState.Work, SimulationRules.ScaledDelta(1f, 4f));
            Assert.That(.86f - four.energy, Is.EqualTo((.86f - one.energy) * 4f).Within(.0001f));
        }

        [Test] public void DifferentTimePartitionsProduceEquivalentResults()
        {
            var single = Defaults();
            var partitioned = Defaults();
            NeedSimulation.Tick(single, WorkerState.Work, 40f);
            for (int i = 0; i < 40; i++) NeedSimulation.Tick(partitioned, WorkerState.Work, 1f);
            foreach (NeedDefinition definition in NeedCatalog.All)
                Assert.That(partitioned.GetNeed(definition.Kind),
                    Is.EqualTo(single.GetNeed(definition.Kind)).Within(.00002f), definition.DisplayName);
            Assert.That(partitioned.stress, Is.EqualTo(single.stress).Within(.00002f));
        }

        [Test] public void AwayRecoversAllFiveNeedsAndStress()
        {
            var state = new WorkerRuntimeState { happiness=.3f, hunger=.8f, bathroom=.8f, inspiration=.3f, energy=.3f, stress=.8f };
            NeedSimulation.Tick(state, WorkerState.Away, ActivityRules.AwayDuration);
            AssertState(state, .45f, .45f, .40f, .46f, .68f, .45f);
        }

        [Test] public void RestRecoversPositiveNeedsWithoutResettingUrgencies()
        {
            var state = Defaults();
            ActivityRules.ApplyRest(state);
            Assert.That(state.happiness, Is.EqualTo(.92f).Within(.0001f));
            Assert.That(state.inspiration, Is.EqualTo(.84f).Within(.0001f));
            Assert.That(state.energy, Is.EqualTo(1f));
            Assert.That(state.hunger, Is.EqualTo(.18f));
            Assert.That(state.bathroom, Is.EqualTo(.15f));
        }

        [Test] public void WaterHydratesButRaisesBathroomUrgency()
        {
            var state = Defaults();
            ActivityRules.ApplyWater(state);
            Assert.That(state.energy, Is.GreaterThan(.86f));
            Assert.That(state.bathroom, Is.EqualTo(.23f).Within(.0001f));
        }

        [Test] public void SnackLowersHungerExactlyOnce()
        {
            var state = Defaults();
            ActivityRules.ApplySnack(state, false);
            Assert.That(state.hunger, Is.Zero);
            Assert.That(state.happiness, Is.EqualTo(.86f).Within(.0001f));
        }

        [Test] public void VendingMalfunctionDoesNotGrantFullRecovery()
        {
            var success = Defaults();
            var failure = Defaults();
            ActivityRules.ApplySnack(success, false);
            ActivityRules.ApplySnack(failure, true);
            Assert.Greater(failure.hunger, success.hunger);
            Assert.Less(failure.happiness, success.happiness);
        }

        [Test] public void CoffeeRecoversEnergyAndInspirationButRaisesBathroomUrgency()
        {
            var state = Defaults();
            ActivityRules.ApplyCoffee(state, false);
            Assert.That(state.energy, Is.EqualTo(1f));
            Assert.That(state.inspiration, Is.EqualTo(.84f).Within(.0001f));
            Assert.That(state.bathroom, Is.EqualTo(.21f).Within(.0001f));
        }

        [Test] public void CaffeinatedCoffeeHasStrongerEnergyEffect()
        {
            var normal = new WorkerRuntimeState { energy=.2f };
            var caffeinated = new WorkerRuntimeState { energy=.2f };
            ActivityRules.ApplyCoffee(normal, false);
            ActivityRules.ApplyCoffee(caffeinated, true);
            Assert.Greater(caffeinated.energy, normal.energy);
        }

        [Test] public void SmokingRelievesStressWithoutChangingHunger()
        {
            var state = Defaults();
            ActivityRules.ApplySmoke(state);
            Assert.That(state.stress, Is.EqualTo(0f));
            Assert.That(state.hunger, Is.EqualTo(.18f));
        }

        [Test] public void RestroomLowersBathroomUrgencyOnce()
        {
            var state = Defaults();
            state.bathroom = .9f;
            ActivityRules.ApplyRestroom(state);
            Assert.That(state.bathroom, Is.EqualTo(.12f).Within(.0001f));
        }

        [Test] public void SocialTimeImprovesHappinessAndInspiration()
        {
            var state = Defaults();
            ActivityRules.ApplySocialStep(state, 5f);
            Assert.Greater(state.happiness, .78f);
            Assert.Greater(state.inspiration, .72f);
            Assert.Less(state.stress, .22f);
        }

        [Test] public void EveryNeedInfluencesProductivityMonotonically()
        {
            float healthy = ProductivityModel.Evaluate(1f, Defaults(), 1f, 1f, 1f);
            foreach (NeedDefinition definition in NeedCatalog.All)
            {
                var poor = Defaults();
                poor.SetNeed(definition.Kind, definition.HighIsGood ? 0f : 1f);
                Assert.Less(ProductivityModel.Evaluate(1f, poor, 1f, 1f, 1f), healthy, definition.Kind.ToString());
            }
        }

        [Test] public void NeedProductivityPenaltyIsGraduatedNotImmediateCollapse()
        {
            var caution = Defaults(); caution.hunger = .50f;
            var urgent = Defaults(); urgent.hunger = .75f;
            var critical = Defaults(); critical.hunger = .95f;
            float a = ProductivityModel.Evaluate(1f, caution, 1f, 1f, 1f);
            float b = ProductivityModel.Evaluate(1f, urgent, 1f, 1f, 1f);
            float c = ProductivityModel.Evaluate(1f, critical, 1f, 1f, 1f);
            Assert.Greater(a, b);
            Assert.Greater(b, c);
            Assert.Greater(c, ProductivityModel.Minimum);
        }

        [Test] public void PhoneModifierAppliesExactlyOnceWithFiveNeeds()
        {
            var state = Defaults();
            float desk = ProductivityModel.Evaluate(1f, state, 1f, 1f, 1f);
            float phone = ProductivityModel.Evaluate(1f, state, ProductivityModel.PhoneWorkstationModifier, 1f, 1f);
            Assert.That(phone, Is.EqualTo(desk * .5f).Within(.0001f));
        }

        [Test] public void StressRemainsOutsideFiveNeedCatalog()
        {
            Assert.False(NeedCatalog.All.Any(value => value.DisplayName == "Stress"));
            Assert.That(typeof(NeedKind).GetEnumNames(), Does.Not.Contain("Stress"));
        }

        [Test] public void FutureSystemHooksAreNeutral()
        {
            var state = Defaults();
            foreach (NeedDefinition definition in NeedCatalog.All)
            {
                Assert.That(NeedModifierHooks.QualificationMultiplier(state, definition.Kind), Is.EqualTo(1f));
                Assert.That(NeedModifierHooks.IncidentMultiplier(state, definition.Kind), Is.EqualTo(1f));
            }
        }

        [Test] public void StatusAndRecoveryCopyExistsForEveryNeed()
        {
            foreach (NeedDefinition definition in NeedCatalog.All)
            {
                Assert.That(definition.Description, Is.Not.Empty);
                Assert.That(definition.RecoveryHint, Is.Not.Empty);
                Assert.That(definition.StatusText(definition.DefaultValue), Is.Not.Empty);
            }
        }

        [Test] public void ChangeCopyReflectsNeedDirection()
        {
            Assert.True(NeedCatalog.IsIncreaseBeneficial(NeedKind.Energy));
            Assert.False(NeedCatalog.IsIncreaseBeneficial(NeedKind.Hunger));
            Assert.That(NeedCatalog.ChangeText(NeedKind.Energy, .1f), Does.StartWith("+"));
            Assert.That(NeedCatalog.ChangeText(NeedKind.Hunger, -.1f), Does.StartWith("+"));
            Assert.That(NeedCatalog.ChangeText(NeedKind.Bathroom, .1f), Does.StartWith("-"));
        }

        [Test] public void VeryLargeDeltaRemainsFiniteAndBounded()
        {
            var state = Defaults();
            NeedSimulation.Tick(state, WorkerState.Work, 1000000f);
            foreach (NeedDefinition definition in NeedCatalog.All)
            {
                float value = state.GetNeed(definition.Kind);
                Assert.False(float.IsNaN(value) || float.IsInfinity(value));
                Assert.That(value, Is.InRange(0f, 1f));
            }
        }

        [Test] public void OneHundredMinuteThirtyWorkerMixedSoakHasNoInvalidOrPauseValues()
        {
            NeedScenarioSummary mixed = FiveNeedsDeterministicMatrix.Run(30, NeedScenarioContext.MixedActivity, 1);
            NeedScenarioSummary paused = FiveNeedsDeterministicMatrix.Run(30, NeedScenarioContext.PauseResume, 1);
            Assert.That(mixed.InvalidValues, Is.Zero);
            Assert.That(paused.InvalidValues, Is.Zero);
            Assert.That(paused.PausedChanges, Is.Zero);
        }

        [Test] public void DeterministicMatrixRepeatsWithoutDivergence()
            => Assert.True(FiveNeedsDeterministicMatrix.ValidateDeterministicRepeat());

        private static WorkerRuntimeState Defaults()
            => new WorkerRuntimeState { happiness=.78f, hunger=.18f, bathroom=.15f, inspiration=.72f, energy=.86f, stress=.22f };

        private static void AssertState(WorkerRuntimeState state, float happiness, float hunger,
            float bathroom, float inspiration, float energy, float stress)
        {
            Assert.That(state.happiness, Is.EqualTo(happiness).Within(.0001f));
            Assert.That(state.hunger, Is.EqualTo(hunger).Within(.0001f));
            Assert.That(state.bathroom, Is.EqualTo(bathroom).Within(.0001f));
            Assert.That(state.inspiration, Is.EqualTo(inspiration).Within(.0001f));
            Assert.That(state.energy, Is.EqualTo(energy).Within(.0001f));
            Assert.That(state.stress, Is.EqualTo(stress).Within(.0001f));
        }
    }
}
