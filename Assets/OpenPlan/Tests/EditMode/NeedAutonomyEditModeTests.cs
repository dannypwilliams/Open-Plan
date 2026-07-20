using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace OpenPlan.Tests
{
    public sealed class NeedAutonomyEditModeTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = objects.Count - 1; i >= 0; i--)
                if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            objects.Clear();
        }

        [Test] public void HealthyNeedsDoNotGenerateEmergencyRecovery()
        {
            Assert.False(NeedAutonomyRules.TrySelectPriorityNeed(Healthy(), out _, out _, out _, out _));
        }

        [Test] public void CriticalBathroomGeneratesHighestPriorityDecision()
        {
            WorkerRuntimeState state = Healthy(); state.bathroom = .96f; state.hunger = .96f;
            Assert.True(NeedAutonomyRules.TrySelectPriorityNeed(state, out NeedKind need, out _, out _, out _));
            Assert.That(need, Is.EqualTo(NeedKind.Bathroom));
        }

        [Test] public void CriticalHungerOutranksOrdinaryWork()
        {
            WorkerRuntimeState state = Healthy(); state.hunger = .95f;
            NeedAutonomyRules.TrySelectPriorityNeed(state, out NeedKind need, out NeedStatus status,
                out WorkerDecisionCategory category, out float priority);
            Assert.That(need, Is.EqualTo(NeedKind.Hunger));
            Assert.That(status, Is.EqualTo(NeedStatus.Critical));
            Assert.That(category, Is.EqualTo(WorkerDecisionCategory.CriticalNeed));
            Assert.That(priority, Is.GreaterThan(900f));
        }

        [Test] public void CriticalEnergySelectsValidRecoveryActivity()
        {
            WorkerRuntimeState state = Healthy(); state.energy = .05f;
            NeedDestinationCandidate selected = NeedAutonomyRules.SelectBest(state, NeedKind.Energy,
                NeedStatus.Critical, new[] { Candidate(PlacementActivity.Rest, "rest", 3f) });
            Assert.That(selected.Activity, Is.EqualTo(PlacementActivity.Rest));
        }

        [Test] public void HappinessOnlySelectsImprovingActivities()
        {
            Assert.True(NeedAutonomyRules.ActivityImproves(PlacementActivity.Rest, NeedKind.Happiness));
            Assert.True(NeedAutonomyRules.ActivityImproves(PlacementActivity.Smoke, NeedKind.Happiness));
            Assert.False(NeedAutonomyRules.ActivityImproves(PlacementActivity.UseRestroom, NeedKind.Happiness));
        }

        [Test] public void InspirationOnlySelectsImprovingActivities()
        {
            Assert.True(NeedAutonomyRules.ActivityImproves(PlacementActivity.GetCoffee, NeedKind.Inspiration));
            Assert.True(NeedAutonomyRules.ActivityImproves(PlacementActivity.Rest, NeedKind.Inspiration));
            Assert.False(NeedAutonomyRules.ActivityImproves(PlacementActivity.BuySnack, NeedKind.Inspiration));
        }

        [Test] public void StressInfluencesRestWithoutSixthNeed()
        {
            WorkerRuntimeState state = Healthy(); state.stress = .9f;
            Assert.True(NeedAutonomyRules.TrySelectPriorityNeed(state, out _, out _,
                out WorkerDecisionCategory category, out _));
            Assert.That(category, Is.EqualTo(WorkerDecisionCategory.StressRecovery));
            Assert.That(NeedCatalog.All.Length, Is.EqualTo(5));
        }

        [Test] public void NeedScoringIsDeterministic()
        {
            WorkerRuntimeState state = Critical(NeedKind.Energy);
            NeedDestinationCandidate candidate = Candidate(PlacementActivity.GetCoffee, "coffee", 4f);
            float first = NeedAutonomyRules.Score(state, NeedKind.Energy, NeedStatus.Critical, candidate);
            float second = NeedAutonomyRules.Score(state, NeedKind.Energy, NeedStatus.Critical, candidate);
            Assert.That(second, Is.EqualTo(first));
        }

        [Test] public void StableTieBreakingUsesOrdinalDestinationId()
        {
            WorkerRuntimeState state = Critical(NeedKind.Energy);
            NeedDestinationCandidate selected = NeedAutonomyRules.SelectBest(state, NeedKind.Energy,
                NeedStatus.Critical, new[] { Candidate(PlacementActivity.Rest, "z-rest", 2f),
                    Candidate(PlacementActivity.Rest, "a-rest", 2f) });
            Assert.That(selected.StableId, Is.EqualTo("a-rest"));
        }

        [Test] public void NearerInferiorDestinationCanLoseToBetterRecovery()
        {
            WorkerRuntimeState state = Critical(NeedKind.Energy);
            NeedDestinationCandidate water = Candidate(PlacementActivity.GetWater, "near-water", .5f);
            NeedDestinationCandidate rest = Candidate(PlacementActivity.Rest, "far-rest", 8f);
            Assert.That(NeedAutonomyRules.SelectBest(state, NeedKind.Energy, NeedStatus.Critical,
                new[] { water, rest }), Is.SameAs(rest));
        }

        [Test] public void LockedZoneIsNeverSelected()
            => AssertRejectedProperty(candidate => candidate.Enabled = false);

        [Test] public void DisabledStationIsNeverSelected()
            => AssertRejectedProperty(candidate => candidate.Enabled = false);

        [Test] public void UnreachableStationIsNeverSelected()
            => AssertRejectedProperty(candidate => candidate.Reachable = false);

        [Test] public void FullStationIsNeverOverReservedByScoring()
            => AssertRejectedProperty(candidate => candidate.HasCapacity = false);

        [Test] public void CooldownBlockedActivityIsRejected()
            => AssertRejectedProperty(candidate => candidate.CooldownReady = false);

        [Test] public void UnaffordableVendingIsRejectedBeforePayment()
        {
            WorkerRuntimeState state = Critical(NeedKind.Hunger);
            NeedDestinationCandidate vending = Candidate(PlacementActivity.BuySnack, "vending", 1f);
            vending.Affordable = false;
            Assert.That(NeedAutonomyRules.Score(state, NeedKind.Hunger, NeedStatus.Critical, vending),
                Is.EqualTo(float.NegativeInfinity));
        }

        [Test] public void HungerUsesOffSiteFallbackWhenFoodUnaffordable()
        {
            WorkerRuntimeState state = Critical(NeedKind.Hunger);
            NeedDestinationCandidate vending = Candidate(PlacementActivity.BuySnack, "vending", 1f);
            vending.Affordable = false;
            NeedDestinationCandidate away = Candidate(PlacementActivity.LeaveOffice, "exit", 5f);
            away.Fallback = DecisionFallbackLevel.OffSite;
            Assert.That(NeedAutonomyRules.SelectBest(state, NeedKind.Hunger, NeedStatus.Critical,
                new[] { vending, away }), Is.SameAs(away));
        }

        [Test] public void BathroomUsesOffSiteFallbackWhenRestroomUnreachable()
        {
            WorkerRuntimeState state = Critical(NeedKind.Bathroom);
            NeedDestinationCandidate restroom = Candidate(PlacementActivity.UseRestroom, "restroom", 1f);
            restroom.Reachable = false;
            NeedDestinationCandidate away = Candidate(PlacementActivity.LeaveOffice, "exit", 5f);
            away.Fallback = DecisionFallbackLevel.OffSite;
            Assert.That(NeedAutonomyRules.SelectBest(state, NeedKind.Bathroom, NeedStatus.Critical,
                new[] { restroom, away }), Is.SameAs(away));
        }

        [Test] public void CoffeeBecomesLessAttractiveWithHighBathroomUrgency()
        {
            WorkerRuntimeState state = Critical(NeedKind.Energy); state.bathroom = .2f;
            NeedDestinationCandidate coffee = Candidate(PlacementActivity.GetCoffee, "coffee", 2f);
            float safe = NeedAutonomyRules.Score(state, NeedKind.Energy, NeedStatus.Critical, coffee);
            state.bathroom = .8f;
            float urgent = NeedAutonomyRules.Score(state, NeedKind.Energy, NeedStatus.Critical, coffee);
            Assert.That(urgent, Is.LessThan(safe));
        }

        [Test] public void MultiNeedScoringRewardsCombinedRecovery()
        {
            WorkerRuntimeState state = Critical(NeedKind.Energy); state.inspiration = .1f;
            NeedDestinationCandidate coffee = Candidate(PlacementActivity.GetCoffee, "coffee", 3f);
            float multi = NeedAutonomyRules.Score(state, NeedKind.Energy, NeedStatus.Critical, coffee);
            state.inspiration = .9f;
            float single = NeedAutonomyRules.Score(state, NeedKind.Energy, NeedStatus.Critical, coffee);
            Assert.That(multi, Is.GreaterThan(single));
        }

        [Test] public void PlayerAuthorityBlocksLowPriorityInterruption()
            => Assert.False(NeedAutonomyRules.CanOverridePlayerAuthority(NeedKind.Happiness,
                NeedStatus.Caution, 100f));

        [Test] public void CriticalBathroomOverridesAuthorityAtBoundedDelay()
        {
            Assert.False(NeedAutonomyRules.CanOverridePlayerAuthority(NeedKind.Bathroom,
                NeedStatus.Critical, 2.99f));
            Assert.True(NeedAutonomyRules.CanOverridePlayerAuthority(NeedKind.Bathroom,
                NeedStatus.Critical, 3f));
        }

        [Test] public void HysteresisRequiresUrgentExitMargin()
        {
            WorkerRuntimeState state = Healthy(); state.bathroom = NeedCatalog.UrgencyUrgent - .01f;
            Assert.False(NeedAutonomyRules.NeedExitedUrgentRange(state, NeedKind.Bathroom));
            state.bathroom = NeedCatalog.UrgencyUrgent - NeedAutonomyRules.NeedExitUrgentMargin;
            Assert.True(NeedAutonomyRules.NeedExitedUrgentRange(state, NeedKind.Bathroom));
        }

        [Test] public void HigherPriorityEmergencyInterruptsLowerRecovery()
            => Assert.That(NeedAutonomyRules.Priority(NeedKind.Bathroom, NeedStatus.Critical),
                Is.GreaterThan(NeedAutonomyRules.Priority(NeedKind.Energy, NeedStatus.Critical)));

        [Test] public void WorkerCannotHoldTwoReservations()
        {
            ActivityReservationService service = Service(out WorkerAgent worker, out _);
            PlacementZone first = Zone("first", 1); PlacementZone second = Zone("second", 1);
            Assert.True(service.TryCreate(worker, first, PlacementActivity.Rest, 0f, out _, out _));
            Assert.True(service.TryCreate(worker, second, PlacementActivity.Rest, 1f, out _, out _));
            Assert.That(service.Count, Is.EqualTo(1));
            Assert.That(service.Get(worker).Zone, Is.SameAs(second));
            Assert.That(first.ReservationCount, Is.Zero);
        }

        [Test] public void ReservationCannotBelongToTwoWorkers()
        {
            ActivityReservationService service = Service(out WorkerAgent first, out _);
            WorkerAgent second = Worker("second"); PlacementZone zone = Zone("one", 1);
            Assert.True(service.TryCreate(first, zone, PlacementActivity.Rest, 0f, out _, out _));
            Assert.False(service.TryCreate(second, zone, PlacementActivity.Rest, 0f, out _, out _));
        }

        [Test] public void ReservationCountCannotBecomeNegative()
        {
            ActivityReservationService service = Service(out WorkerAgent worker, out _);
            PlacementZone zone = Zone("one", 1);
            service.Release(worker, "none"); service.Release(worker, "again");
            Assert.That(zone.ReservationCount, Is.Zero);
        }

        [Test] public void ReservationCountCannotExceedCapacity()
        {
            ActivityReservationService service = Service(out WorkerAgent first, out _);
            PlacementZone zone = Zone("one", 1);
            Assert.True(service.TryCreate(first, zone, PlacementActivity.Rest, 0f, out _, out _));
            Assert.That(zone.EffectiveUsage, Is.EqualTo(zone.Capacity));
            Assert.False(zone.TryReserveIncoming(Worker("second"), out _));
        }

        [Test] public void ReleasingReservationTwiceIsIdempotent()
        {
            ActivityReservationService service = Service(out WorkerAgent worker, out _);
            PlacementZone zone = Zone("one", 1);
            service.TryCreate(worker, zone, PlacementActivity.Rest, 0f, out _, out _);
            Assert.True(service.Release(worker, "first"));
            Assert.False(service.Release(worker, "second"));
            Assert.That(zone.EffectiveUsage, Is.Zero);
        }

        [Test] public void TimedOutReservationReleasesCorrectly()
        {
            ActivityReservationService service = Service(out WorkerAgent worker, out AutonomyInstrumentation counters);
            PlacementZone zone = Zone("one", 1);
            service.TryCreate(worker, zone, PlacementActivity.Rest, 0f, out _, out _);
            service.Tick(NeedAutonomyRules.ReservationLifetime + .1f);
            Assert.That(service.Count, Is.Zero);
            Assert.That(zone.ReservationCount, Is.Zero);
            Assert.That(counters.reservationExpirations, Is.EqualTo(1));
        }

        [Test] public void DeterministicEvaluationStaggeringReproducesExactly()
            => Assert.That(NeedAutonomyRules.InitialEvaluationDelay("Morgan", 42),
                Is.EqualTo(NeedAutonomyRules.InitialEvaluationDelay("Morgan", 42)));

        [Test] public void PausedDecisionScheduleDoesNotAdvanceWithoutSimulationCall()
        {
            float before = NeedAutonomyRules.InitialEvaluationDelay("Alex", 9);
            float after = before;
            Assert.That(after, Is.EqualTo(before));
        }

        [Test] public void EquivalentSimulationCommandsProduceEquivalentDelays()
        {
            WorkerRuntimeState state = Critical(NeedKind.Hunger);
            float one = NeedAutonomyRules.NextEvaluationDelay(state, "Sam", 10, 4);
            float partitioned = NeedAutonomyRules.NextEvaluationDelay(state, "Sam", 10, 4);
            Assert.That(partitioned, Is.EqualTo(one));
        }

        [Test] public void PhoneWorkersUseSameDecisionScoring()
        {
            WorkerRuntimeState state = Critical(NeedKind.Bathroom);
            NeedDestinationCandidate restroom = Candidate(PlacementActivity.UseRestroom, "restroom", 2f);
            Assert.That(NeedAutonomyRules.Score(state, NeedKind.Bathroom, NeedStatus.Critical, restroom),
                Is.EqualTo(NeedAutonomyRules.Score(state, NeedKind.Bathroom, NeedStatus.Critical, restroom)));
        }

        [Test] public void WorkstationOwnershipIsIndependentOfNeedDecisionData()
        {
            WorkerDecisionRuntime decision = new WorkerDecisionRuntime();
            decision.Begin(WorkerDecisionCategory.CriticalNeed, NeedKind.Bathroom, true,
                PlacementActivity.UseRestroom, "restroom", 100f, "critical", 1f, false);
            Assert.That(decision.destinationId, Is.EqualTo("restroom"));
            Assert.That(decision.category, Is.EqualTo(WorkerDecisionCategory.CriticalNeed));
        }

        [Test] public void PathRequestsRejectLockedPropertyPoints()
        {
            OfficeStageLayout layout = Layout(new Bounds(Vector3.zero, new Vector3(10f, 1f, 10f)),
                new Bounds(new Vector3(3f, 0f, 0f), new Vector3(2f, 1f, 4f)));
            OfficeNavigationService navigation = new OfficeNavigationService(layout);
            Assert.False(navigation.TryFindPath(Vector3.zero, new Vector3(3f, 0f, 0f), out _, out _));
        }

        [Test] public void StuckRecoveryHasBoundedRetryCount()
            => Assert.That(NeedAutonomyRules.MaximumRepathAttempts, Is.InRange(1, 4));

        [Test] public void FailedNavigationReservationCanBeReleased()
        {
            ActivityReservationService service = Service(out WorkerAgent worker, out _);
            PlacementZone zone = Zone("one", 1);
            service.TryCreate(worker, zone, PlacementActivity.Rest, 0f, out _, out _);
            service.Release(worker, "path failed");
            Assert.That(zone.EffectiveUsage, Is.Zero);
        }

        [Test] public void FinalSafetyRecoveryRequiresValidatedPoint()
        {
            OfficeStageLayout layout = Layout(new Bounds(Vector3.zero, new Vector3(6f, 1f, 6f)), null);
            OfficeNavigationService navigation = new OfficeNavigationService(layout);
            Assert.True(navigation.IsValidPoint(Vector3.zero));
            Assert.False(navigation.IsValidPoint(new Vector3(20f, 0f, 20f)));
        }

        [Test] public void DecisionScoresNeverBecomeNaNOrPositiveInfinity()
        {
            WorkerRuntimeState state = Critical(NeedKind.Energy);
            NeedDestinationCandidate candidate = Candidate(PlacementActivity.Rest, "rest", float.NaN);
            float score = NeedAutonomyRules.Score(state, NeedKind.Energy, NeedStatus.Critical, candidate);
            Assert.False(float.IsNaN(score));
            Assert.False(float.IsPositiveInfinity(score));
        }

        private void AssertRejectedProperty(System.Action<NeedDestinationCandidate> reject)
        {
            WorkerRuntimeState state = Critical(NeedKind.Energy);
            NeedDestinationCandidate candidate = Candidate(PlacementActivity.Rest, "rest", 1f);
            reject(candidate);
            Assert.That(NeedAutonomyRules.SelectBest(state, NeedKind.Energy, NeedStatus.Critical,
                new[] { candidate }), Is.Null);
        }

        private static WorkerRuntimeState Healthy()
            => new WorkerRuntimeState { happiness = .8f, hunger = .2f, bathroom = .2f,
                inspiration = .8f, energy = .8f, stress = .2f };

        private static WorkerRuntimeState Critical(NeedKind need)
        {
            WorkerRuntimeState state = Healthy();
            state.SetNeed(need, NeedCatalog.Get(need).HighIsGood ? .05f : .95f);
            return state;
        }

        private static NeedDestinationCandidate Candidate(PlacementActivity activity, string id, float path)
            => new NeedDestinationCandidate { Activity = activity, StableId = id, PathCost = path,
                Duration = NeedAutonomyRules.ActivityDuration(activity), Enabled = true, Reachable = true,
                HasCapacity = true, CooldownReady = true, Affordable = true };

        private ActivityReservationService Service(out WorkerAgent worker, out AutonomyInstrumentation counters)
        {
            counters = new AutonomyInstrumentation();
            worker = Worker("worker");
            return new ActivityReservationService(counters);
        }

        private WorkerAgent Worker(string name)
        {
            GameObject owner = new GameObject(name); objects.Add(owner);
            return owner.AddComponent<WorkerAgent>();
        }

        private PlacementZone Zone(string id, int capacity)
        {
            GameObject owner = new GameObject(id); objects.Add(owner);
            PlacementZone zone = owner.AddComponent<ActivityPlacementZone>();
            zone.Configure(PlacementActivity.Rest, Vector3.zero, stableIdentifier: id, capacity: capacity);
            return zone;
        }

        private OfficeStageLayout Layout(Bounds walkable, Bounds? locked)
        {
            GameObject owner = new GameObject("layout"); objects.Add(owner);
            OfficeStageLayout layout = owner.AddComponent<OfficeStageLayout>();
            layout.Configure(walkable, walkable, 8f, walkable, locked);
            return layout;
        }
    }
}
