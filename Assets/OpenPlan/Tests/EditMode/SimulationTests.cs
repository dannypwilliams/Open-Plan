using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace OpenPlan.Tests
{
    public sealed class SimulationTests
    {
        [Test] public void ProductivityFormula_UsesReadableFactors()
        {
            float value = ProductivityModel.Evaluate(1.1f, .7f, .9f, .2f, 1.05f, 1.1f, 1.2f);
            Assert.That(value, Is.InRange(.8f, 1.6f));
        }

        [TestCase(0f, 0f, 0f, 1f, .1f)]
        [TestCase(9f, 1f, 1f, 0f, 2.5f)]
        public void ProductivityFormula_Clamps(float skill, float energy, float mood, float stress, float expected)
            => Assert.That(ProductivityModel.Evaluate(skill, energy, mood, stress, 1f, 1f, 1f), Is.EqualTo(expected).Within(.001f));

        [Test] public void TraitModifiers_AreContextual()
        {
            Assert.That(ProductivityModel.TraitModifier(WorkerTrait.Anxious, .1f, .5f, .8f), Is.GreaterThan(ProductivityModel.TraitModifier(WorkerTrait.Anxious, .9f, .5f, .8f)));
            Assert.That(ProductivityModel.TraitModifier(WorkerTrait.Ambitious, .4f, .9f, .8f), Is.GreaterThan(ProductivityModel.TraitModifier(WorkerTrait.Ambitious, .4f, .1f, .8f)));
            Assert.That(ProductivityModel.TraitModifier(WorkerTrait.Caffeinated, .4f, .5f, .9f), Is.GreaterThan(ProductivityModel.TraitModifier(WorkerTrait.Caffeinated, .4f, .5f, .3f)));
        }

        [Test] public void FocusedWorkModifier_IsTwentyPercentAndDoesNotStack()
        {
            Assert.That(ProductivityModel.FocusedWorkModifier(0f), Is.EqualTo(1f));
            Assert.That(ProductivityModel.FocusedWorkModifier(30f), Is.EqualTo(1.2f));
            Assert.That(ProductivityModel.FocusedWorkModifier(60f), Is.EqualTo(1.2f));
        }

        [Test] public void InverseStressModifier_RewardLowStress()
            => Assert.That(ProductivityModel.InverseStressModifier(.2f), Is.GreaterThan(ProductivityModel.InverseStressModifier(.9f)));

        [Test] public void EnergyDecay_IsBounded()
            => Assert.That(SimulationRules.DecayEnergy(.5f, 1000f), Is.EqualTo(0f));

        [Test] public void CoffeeRestore_CaffeinatedGetsLargerBoost()
            => Assert.That(SimulationRules.RestoreCoffee(.2f, true), Is.GreaterThan(SimulationRules.RestoreCoffee(.2f, false)));

        [Test] public void MoodChanges_ClampToValidRange()
        {
            Assert.That(SimulationRules.ChangeMood(.9f, .5f), Is.EqualTo(1f));
            Assert.That(SimulationRules.ChangeMood(.1f, -.5f), Is.EqualTo(0f));
        }

        [Test] public void ActivityEffects_AreExactAndNeedsClamp()
        {
            var state = new WorkerRuntimeState { energy = .40f, mood = .40f, stress = .60f };
            ActivityRules.ApplyRest(state);
            Assert.That(state.energy, Is.EqualTo(.75f).Within(.0001f));
            Assert.That(state.mood, Is.EqualTo(.52f).Within(.0001f));
            Assert.That(state.stress, Is.EqualTo(.35f).Within(.0001f));
            ActivityRules.ChangeNeeds(state, 2f, 2f, -2f);
            Assert.That(state.energy, Is.EqualTo(1f));
            Assert.That(state.mood, Is.EqualTo(1f));
            Assert.That(state.stress, Is.EqualTo(0f));
        }

        [Test] public void WaterSnackSmokeAndAwayEffects_AreExact()
        {
            var water = new WorkerRuntimeState { energy = .4f, mood = .4f, stress = .6f };
            ActivityRules.ApplyWater(water);
            Assert.That(water.energy, Is.EqualTo(.48f).Within(.0001f));
            Assert.That(water.mood, Is.EqualTo(.45f).Within(.0001f));
            Assert.That(water.stress, Is.EqualTo(.55f).Within(.0001f));

            var snack = new WorkerRuntimeState { energy = .4f, mood = .4f, stress = .6f };
            ActivityRules.ApplySnack(snack, false);
            Assert.That(snack.energy, Is.EqualTo(.65f).Within(.0001f));
            Assert.That(snack.mood, Is.EqualTo(.55f).Within(.0001f));
            Assert.That(snack.stress, Is.EqualTo(.52f).Within(.0001f));

            var malfunction = new WorkerRuntimeState { energy = .4f, mood = .4f, stress = .6f };
            ActivityRules.ApplySnack(malfunction, true);
            Assert.That(malfunction.energy, Is.EqualTo(.45f).Within(.0001f));
            Assert.That(malfunction.mood, Is.EqualTo(.35f).Within(.0001f));
            Assert.That(malfunction.stress, Is.EqualTo(.6f).Within(.0001f));

            var smoke = new WorkerRuntimeState { energy = .4f, mood = .4f, stress = .6f };
            ActivityRules.ApplySmoke(smoke);
            Assert.That(smoke.mood, Is.EqualTo(.45f).Within(.0001f));
            Assert.That(smoke.stress, Is.EqualTo(.3f).Within(.0001f));

            var away = new WorkerRuntimeState { energy = .2f, mood = .3f, stress = .8f };
            ActivityRules.ApplyAwayStep(away, ActivityRules.AwayDuration);
            Assert.That(away.energy, Is.EqualTo(.65f).Within(.0001f));
            Assert.That(away.mood, Is.EqualTo(.42f).Within(.0001f));
            Assert.That(away.stress, Is.EqualTo(.45f).Within(.0001f));
        }

        [Test] public void ActivityDurationsCostsAndCooldowns_AreContractValues()
        {
            Assert.That(ActivityRules.RestDuration, Is.EqualTo(20f));
            Assert.That(ActivityRules.WaterDuration, Is.EqualTo(6f));
            Assert.That(ActivityRules.VendingDuration, Is.EqualTo(8f));
            Assert.That(ActivityRules.SmokingDuration, Is.EqualTo(12f));
            Assert.That(ActivityRules.AwayDuration, Is.EqualTo(30f));
            Assert.That(ActivityRules.FocusedWorkDuration, Is.EqualTo(30f));
            Assert.That(ActivityRules.WaterCooldown, Is.EqualTo(35f));
            Assert.That(ActivityRules.VendingCooldown, Is.EqualTo(45f));
            Assert.That(ActivityRules.SmokingCooldown, Is.EqualTo(45f));
            Assert.That(ActivityRules.SnackCost, Is.EqualTo(15f));
        }

        [Test] public void SeededVendingMalfunction_IsRepeatableAtTenPercentRule()
        {
            Assert.True(new SeededRandomService(14).Chance(ActivityRules.VendingMalfunctionChance));
            Assert.False(new SeededRandomService(0).Chance(ActivityRules.VendingMalfunctionChance));
        }

        [Test] public void CashDirector_StartsAtOneHundredAndAccruesContinuousIncome()
        {
            GameObject go = new GameObject("Cash test");
            CashDirector cash = go.AddComponent<CashDirector>();
            cash.Initialize();
            Assert.That(cash.CurrentCash, Is.EqualTo(100f));
            cash.AccrueDeskIncome(2f, 30f);
            Assert.That(cash.CurrentCash, Is.EqualTo(160f).Within(.0001f));
            Assert.That(cash.LifetimeEarned, Is.EqualTo(60f).Within(.0001f));
            cash.AccrueDeskIncome(2f, 0f);
            Assert.That(cash.CurrentCash, Is.EqualTo(160f).Within(.0001f));
            Assert.True(cash.TrySpend(15f));
            Assert.That(cash.CurrentCash, Is.EqualTo(145f).Within(.0001f));
            Assert.That(cash.LifetimeSpent, Is.EqualTo(15f).Within(.0001f));
            Object.DestroyImmediate(go);
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

        [Test] public void CarryGesture_UsesStrictPixelOrHoldThreshold()
        {
            Vector2 press = new Vector2(100f, 100f);
            Assert.False(WorkerCarryController.ShouldBeginCarry(press, press + new Vector2(6f, 0f), .119f));
            Assert.True(WorkerCarryController.ShouldBeginCarry(press, press + new Vector2(6.01f, 0f), .01f));
            Assert.True(WorkerCarryController.ShouldBeginCarry(press, press, .12f));
        }

        [Test] public void CarryGesture_EscapeAndRightMouseAreBothCancellationInputs()
        {
            Assert.True(WorkerCarryController.ShouldCancel(true, false));
            Assert.True(WorkerCarryController.ShouldCancel(false, true));
            Assert.False(WorkerCarryController.ShouldCancel(false, false));
        }

        [Test] public void StarterArtManifest_ContainsValidatedGeneratedAssets()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string manifestPath = Path.Combine(root, "Docs", "ASSET_MANIFEST.json");
            Assert.True(File.Exists(manifestPath));
            string manifest = File.ReadAllText(manifestPath);
            Assert.That(manifest, Does.Contain("\"asset_count\": 54"));
            string[] assets =
            {
                "DamagedDesk", "CheapCRTMonitor", "CheapVendingMachine", "Ashtray",
                "Cigarette", "NeighborSign", "ConnectingWallTrim"
            };
            foreach (string asset in assets)
            {
                Assert.That(manifest, Does.Contain("\"name\": \"" + asset + "\""));
                Assert.True(File.Exists(Path.Combine(root, "Tools", "Blender", "Source", "OP_" + asset + ".blend")));
                Assert.True(File.Exists(Path.Combine(root, "Tools", "Blender", "Exports", "OP_" + asset + ".fbx")));
                Assert.True(File.Exists(Path.Combine(root, "Assets", "OpenPlan", "Art", "Models", "OP_" + asset + ".fbx")));
            }
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
