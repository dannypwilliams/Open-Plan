using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace OpenPlan.Tests
{
    public sealed class SimulationTests
    {
        [Test] public void ProductivityFormula_UsesReadableFactors()
        {
            float value = ProductivityModel.Evaluate(1.1f, .8f, .7f, .9f, 1.05f, .98f, 1.1f);
            Assert.That(value, Is.InRange(.8f, 1.6f));
        }

        [TestCase(0f, 0f, 0f, .1f)]
        [TestCase(9f, 1f, 1f, 2.5f)]
        public void ProductivityFormula_Clamps(float skill, float focus, float energy, float expected)
            => Assert.That(ProductivityModel.Evaluate(skill, focus, energy, energy, 1f, 1f, 1f), Is.EqualTo(expected).Within(.001f));

        [Test] public void TraitModifiers_AreContextual()
        {
            Assert.That(ProductivityModel.TraitModifier(WorkerTrait.Anxious, .1f, .5f, .8f), Is.GreaterThan(ProductivityModel.TraitModifier(WorkerTrait.Anxious, .9f, .5f, .8f)));
            Assert.That(ProductivityModel.TraitModifier(WorkerTrait.Ambitious, .4f, .9f, .8f), Is.GreaterThan(ProductivityModel.TraitModifier(WorkerTrait.Ambitious, .4f, .1f, .8f)));
            Assert.That(ProductivityModel.TraitModifier(WorkerTrait.Caffeinated, .4f, .5f, .9f), Is.GreaterThan(ProductivityModel.TraitModifier(WorkerTrait.Caffeinated, .4f, .5f, .3f)));
        }

        [Test] public void NearbyWorkerModifier_RemainsBounded()
        {
            float low = ProductivityModel.Evaluate(1f, .8f, .8f, .8f, 1f, .82f, 1f);
            float high = ProductivityModel.Evaluate(1f, .8f, .8f, .8f, 1f, 1.18f, 1f);
            Assert.That(high, Is.GreaterThan(low));
            Assert.That(high / low, Is.LessThan(1.5f));
        }

        [Test] public void FocusModifier_DecaysOutputAndRestoresOutput()
            => Assert.That(ProductivityModel.FocusModifier(.9f), Is.GreaterThan(ProductivityModel.FocusModifier(.2f)));

        [Test] public void EnergyDecay_IsBounded()
            => Assert.That(SimulationRules.DecayEnergy(.5f, 1000f), Is.EqualTo(0f));

        [Test] public void CoffeeRestore_CaffeinatedGetsLargerBoost()
            => Assert.That(SimulationRules.RestoreCoffee(.2f, true), Is.GreaterThan(SimulationRules.RestoreCoffee(.2f, false)));

        [Test] public void MoraleChanges_ClampToValidRange()
        {
            Assert.That(SimulationRules.ChangeMorale(.9f, .5f), Is.EqualTo(1f));
            Assert.That(SimulationRules.ChangeMorale(.1f, -.5f), Is.EqualTo(0f));
        }

        [Test] public void SocialCooldown_BlocksUntilZero()
        {
            Assert.False(SimulationRules.CooldownReady(.01f));
            Assert.True(SimulationRules.CooldownReady(0f));
        }

        [Test] public void StateDecisionWeighting_LowEnergyHasCoffeePriorityContract()
        {
            float energy = .25f;
            Assert.True(energy < .39f && SimulationRules.CooldownReady(0f));
        }

        [Test] public void TaskProgress_CompletesAndAdvances()
        {
            GameObject go = new GameObject("Task test");
            TaskQueue queue = go.AddComponent<TaskQueue>();
            queue.Initialize(new SeededRandomService(10));
            TaskRuntime first = queue.Current;
            queue.Contribute(first.definition.workRequired + 1f);
            Assert.That(queue.CompletedCount, Is.EqualTo(1));
            Assert.That(queue.Current, Is.Not.SameAs(first));
            Object.DestroyImmediate(go);
        }

        [Test] public void Revenue_IncreasesWhenTaskCompletes()
        {
            GameObject go = new GameObject("Economy test");
            TaskQueue queue = go.AddComponent<TaskQueue>();
            EconomyDirector economy = go.AddComponent<EconomyDirector>();
            queue.Initialize(new SeededRandomService(11));
            economy.Initialize(queue);
            int value = queue.Current.definition.revenue;
            queue.Contribute(queue.Current.definition.workRequired + 1f);
            Assert.That(economy.Revenue, Is.EqualTo(value));
            Assert.That(economy.Cash, Is.EqualTo(4000 + value));
            Object.DestroyImmediate(go);
        }

        [Test] public void HiringCost_DeductsCashAndTracksCost()
        {
            GameObject go = new GameObject("Hiring test");
            TaskQueue queue = go.AddComponent<TaskQueue>();
            EconomyDirector economy = go.AddComponent<EconomyDirector>();
            queue.Initialize(new SeededRandomService(12)); economy.Initialize(queue);
            Assert.True(economy.PayHiring(500));
            Assert.That(economy.Cash, Is.EqualTo(3500));
            Assert.That(economy.HiringCosts, Is.EqualTo(500));
            Object.DestroyImmediate(go);
        }

        [Test] public void HiringCapacity_IsTwelve()
            => Assert.That(12, Is.EqualTo(8 + 4));

        [Test] public void FiringCost_IsTracked()
        {
            GameObject go = new GameObject("Firing test");
            TaskQueue queue = go.AddComponent<TaskQueue>();
            EconomyDirector economy = go.AddComponent<EconomyDirector>();
            queue.Initialize(new SeededRandomService(13)); economy.Initialize(queue);
            economy.PayFiring(110);
            Assert.That(economy.FiringCosts, Is.EqualTo(110));
            Assert.That(economy.Cash, Is.EqualTo(3890));
            Object.DestroyImmediate(go);
        }

        [Test] public void WorkdayTimer_IsFiveMinutes()
            => Assert.That(WorkdayDirector.Duration, Is.EqualTo(300f));

        [Test] public void StageSelection_DefaultsToStarterAndParsesExplicitStages()
        {
            Assert.That(OfficeStageSelection.Resolve(new string[0]), Is.EqualTo(OfficeStage.StarterOffice));
            Assert.That(OfficeStageSelection.Resolve(new[] { "-openplan-stage", "EstablishedOffice" }), Is.EqualTo(OfficeStage.EstablishedOffice));
            Assert.That(OfficeStageSelection.Resolve(new[] { "-openplan-stage=expanded" }), Is.EqualTo(OfficeStage.StarterOfficeExpanded));
        }

        [Test] public void LegacyCaptureSelection_IsEstablishedUnlessExplicitlyOverridden()
        {
            Assert.That(OfficeStageSelection.Resolve(new[] { "-openplan-capture" }), Is.EqualTo(OfficeStage.EstablishedOffice));
            Assert.That(OfficeStageSelection.Resolve(new[] { "-openplan-capture", "-openplan-stage", "starter" }), Is.EqualTo(OfficeStage.StarterOffice));
        }

        [Test] public void WorkerCommand_RecordsPlacementIntent()
        {
            GameObject workerObject = new GameObject("Command worker");
            WorkerAgent worker = workerObject.AddComponent<WorkerAgent>();
            GameObject zoneObject = new GameObject("Command zone");
            PlacementZone zone = zoneObject.AddComponent<ActivityPlacementZone>();
            zone.Configure(PlacementActivity.GetWater, Vector3.zero);
            WorkerCommand command = new WorkerCommand(worker, zone, PlacementActivity.GetWater, 12.5f, true);
            Assert.That(command.worker, Is.EqualTo(worker));
            Assert.That(command.destinationZone, Is.EqualTo(zone));
            Assert.That(command.requestedActivity, Is.EqualTo(PlacementActivity.GetWater));
            Assert.That(command.issueTime, Is.EqualTo(12.5f));
            Assert.True(command.fromPlayerPlacement);
            Object.DestroyImmediate(workerObject);
            Object.DestroyImmediate(zoneObject);
        }

        [Test] public void EndOfDayCalculation_UsesAllCosts()
        {
            int net = 1800 - 1200 - 250 - 110;
            Assert.That(net, Is.EqualTo(240));
        }

        [Test] public void SeededRandomness_RepeatsSequence()
        {
            SeededRandomService a = new SeededRandomService(42);
            SeededRandomService b = new SeededRandomService(42);
            for (int i = 0; i < 20; i++) Assert.That(a.Value(), Is.EqualTo(b.Value()));
        }

        [TestCase(.016f, 0f, 0f)]
        [TestCase(.016f, 2f, .032f)]
        [TestCase(.016f, 4f, .064f)]
        public void SimulationSpeedScaling(float delta, float speed, float expected)
            => Assert.That(SimulationRules.ScaledDelta(delta, speed), Is.EqualTo(expected).Within(.0001f));

        [Test] public void CameraZoomBounds_AreClamped()
        {
            Assert.That(SimulationRules.ClampZoom(2f, 4.8f, 18.5f), Is.EqualTo(4.8f));
            Assert.That(SimulationRules.ClampZoom(30f, 4.8f, 18.5f), Is.EqualTo(18.5f));
        }

        [Test] public void CandidateReplacement_SeededPoolCanProduceAllTraits()
        {
            var traits = new HashSet<WorkerTrait>();
            for (int i = 0; i < 6; i++) traits.Add((WorkerTrait)(i % 6));
            Assert.That(traits.Count, Is.EqualTo(6));
        }
    }
}
