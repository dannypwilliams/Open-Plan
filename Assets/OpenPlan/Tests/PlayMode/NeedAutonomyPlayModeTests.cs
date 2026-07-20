using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OpenPlan.Tests
{
    public sealed class NeedAutonomyPlayModeTests
    {
        private static IEnumerator LoadStarter()
        {
            Time.timeScale = 1f;
            OfficeStageSelection.SelectForNextLoad(OfficeStage.StarterOffice);
            SceneManager.LoadScene("Office");
            yield return null;
            yield return null;
            Assert.NotNull(Object.FindFirstObjectByType<OfficeDirector>());
        }

        private static OfficeDirector Office() => Object.FindFirstObjectByType<OfficeDirector>();

        private static PlacementZone Zone(OfficeDirector office, string id)
        {
            for (int i = 0; i < office.PlacementZones.Count; i++)
                if (office.PlacementZones[i].StableIdentifier == id) return office.PlacementZones[i];
            Assert.Fail("Missing zone " + id);
            return null;
        }

        private static IEnumerator Ready(WorkerAgent worker, float timeout = 4f)
        {
            SimulationSpeedController.Instance.SetSpeed(4f);
            float deadline = Time.realtimeSinceStartup + timeout;
            while (worker != null && worker.Runtime.behavior != WorkerState.Work &&
                   worker.Runtime.behavior != WorkerState.Unassigned && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.NotNull(worker);
            Assert.That(worker.Runtime.behavior == WorkerState.Work || worker.Runtime.behavior == WorkerState.Unassigned,
                Is.True, worker.CurrentActivityLabel);
        }

        private static void SetCritical(WorkerAgent worker, NeedKind kind)
        {
            worker.Runtime.happiness = .8f;
            worker.Runtime.hunger = .2f;
            worker.Runtime.bathroom = .2f;
            worker.Runtime.inspiration = .8f;
            worker.Runtime.energy = .8f;
            worker.Runtime.stress = .2f;
            worker.Runtime.SetNeed(kind, NeedCatalog.Get(kind).HighIsGood ? .05f : .95f);
            worker.RequestImmediateNeedEvaluation();
        }

        private static IEnumerator WaitForDecision(WorkerAgent worker, PlacementActivity activity, float timeout = 5f)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            while (worker != null && worker.Decision.activity != activity && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.NotNull(worker);
            Assert.That(worker.Decision.activity, Is.EqualTo(activity), worker.DecisionReasonLabel);
        }

        private static IEnumerator WaitForAnyDecision(WorkerAgent worker, float timeout,
            params PlacementActivity[] activities)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            bool found = false;
            while (worker != null && Time.realtimeSinceStartup < deadline)
            {
                for (int i = 0; i < activities.Length; i++)
                    if (worker.Decision.activity == activities[i] && worker.Decision.IsNeedRecovery) found = true;
                if (found) break;
                yield return null;
            }
            Assert.True(found, worker == null ? "Worker missing" : worker.DecisionReasonLabel);
        }

        private static IEnumerator WaitForState(WorkerAgent worker, WorkerState state, float timeout = 8f)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            while (worker != null && worker.Runtime.behavior != state && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.NotNull(worker);
            Assert.That(worker.Runtime.behavior, Is.EqualTo(state), worker.CurrentActivityLabel);
        }

        [UnityTest] public IEnumerator HungryEmployeeAutonomouslySeeksFood()
        {
            yield return LoadStarter(); OfficeDirector office = Office(); WorkerAgent worker = office.Workers[0];
            yield return Ready(worker); office.Cash.AccrueDeskIncome(10f, 60f); SetCritical(worker, NeedKind.Hunger);
            yield return WaitForDecision(worker, PlacementActivity.BuySnack);
            Assert.That(worker.CurrentDestinationLabel, Does.Contain("Snack"));
        }

        [UnityTest] public IEnumerator UnaffordableHungryEmployeeUsesFreeFallback()
        {
            yield return LoadStarter(); WorkerAgent worker = Office().Workers[0]; yield return Ready(worker);
            Assert.That(Office().Cash.CurrentCash, Is.LessThan(ActivityRules.SnackCost));
            SetCritical(worker, NeedKind.Hunger); yield return WaitForDecision(worker, PlacementActivity.LeaveOffice);
            Assert.That(worker.Decision.fallbackLevel, Is.EqualTo(DecisionFallbackLevel.OffSite));
        }

        [UnityTest] public IEnumerator BathroomCriticalEmployeeSeeksRestroom()
        {
            yield return LoadStarter(); WorkerAgent worker = Office().Workers[0]; yield return Ready(worker);
            SetCritical(worker, NeedKind.Bathroom); yield return WaitForDecision(worker, PlacementActivity.UseRestroom);
            Assert.That(worker.Decision.category, Is.EqualTo(WorkerDecisionCategory.CriticalNeed));
        }

        [UnityTest] public IEnumerator BlockedRestroomTriggersValidOffSiteResponse()
        {
            yield return LoadStarter(); OfficeDirector office = Office(); WorkerAgent worker = office.Workers[0];
            yield return Ready(worker); Zone(office, "starter.restroom.main").SetZoneEnabled(false);
            SetCritical(worker, NeedKind.Bathroom); yield return WaitForDecision(worker, PlacementActivity.LeaveOffice);
        }

        [UnityTest] public IEnumerator TiredEmployeeSeeksRestOrCoffee()
        {
            yield return LoadStarter(); WorkerAgent worker = Office().Workers[0]; yield return Ready(worker);
            SetCritical(worker, NeedKind.Energy);
            yield return WaitForAnyDecision(worker, 5f, PlacementActivity.Rest, PlacementActivity.GetCoffee);
        }

        [UnityTest] public IEnumerator UninspiredEmployeeSelectsValidRecovery()
        {
            yield return LoadStarter(); WorkerAgent worker = Office().Workers[0]; yield return Ready(worker);
            SetCritical(worker, NeedKind.Inspiration);
            yield return WaitForAnyDecision(worker, 5f, PlacementActivity.Rest, PlacementActivity.GetCoffee,
                PlacementActivity.Smoke, PlacementActivity.LeaveOffice);
        }

        [UnityTest] public IEnumerator UnhappyEmployeeSelectsValidRecovery()
        {
            yield return LoadStarter(); WorkerAgent worker = Office().Workers[0]; yield return Ready(worker);
            SetCritical(worker, NeedKind.Happiness);
            yield return WaitForAnyDecision(worker, 5f, PlacementActivity.Rest, PlacementActivity.GetWater,
                PlacementActivity.Smoke, PlacementActivity.LeaveOffice);
        }

        [UnityTest] public IEnumerator WorkerMustReachStationBeforeActivityBegins()
        {
            yield return LoadStarter(); WorkerAgent worker = Office().Workers[0]; yield return Ready(worker);
            SetCritical(worker, NeedKind.Bathroom); yield return WaitForDecision(worker, PlacementActivity.UseRestroom);
            Assert.That(worker.Runtime.behavior, Is.EqualTo(WorkerState.WalkToPlacement));
            yield return WaitForState(worker, WorkerState.UseRestroom);
            Assert.That(Vector3.Distance(worker.transform.position, Zone(Office(), "starter.restroom.main").PlacementPoint.position),
                Is.LessThan(.4f));
        }

        [UnityTest] public IEnumerator WorkerRouteAvoidsRegisteredWallVolumes()
        {
            yield return LoadStarter(); OfficeDirector office = Office();
            Assert.True(office.Navigation.TryFindPath(new Vector3(-5f,0f,-3f), new Vector3(-4f,0f,4f),
                out Vector3[] path, out _));
            for (int i = 0; i < path.Length; i++)
                Assert.True(office.Layout.CanNavigateWorkerAt(path[i], OfficeNavigationService.WorkerClearance, out _));
        }

        [UnityTest] public IEnumerator WorkerRouteNeverCrossesLockedUnit()
        {
            yield return LoadStarter(); OfficeDirector office = Office();
            Assert.False(office.Navigation.TryFindPath(Vector3.zero, new Vector3(10f,0f,0f), out _, out _));
            for (int i = 0; i < 64; i++)
            {
                Vector3 wander = office.GetWanderPoint(office.Workers[0]);
                Assert.True(office.Navigation.IsValidPoint(wander));
                Assert.True(office.Layout.IsUnlockedRegion(wander));
            }
        }

        [UnityTest] public IEnumerator ArbitraryGroundWorkerCanReachNeedStation()
        {
            yield return LoadStarter(); OfficeDirector office = Office(); WorkerAgent worker = office.Workers[0];
            yield return Ready(worker); Assert.True(worker.BeginPlayerCarry(out _));
            Vector3 ground = new Vector3(3.8f,0f,-.2f);
            Assert.True(office.TryIssueGroundPlacementCommand(worker, ground, out _, out string reason), reason);
            SetCritical(worker, NeedKind.Bathroom); yield return WaitForDecision(worker, PlacementActivity.UseRestroom);
        }

        [UnityTest] public IEnumerator DeskWorkerResumesWorkAfterRecovery()
        {
            yield return LoadStarter(); WorkerAgent worker = Office().Workers[0]; yield return Ready(worker);
            SetCritical(worker, NeedKind.Bathroom); yield return WaitForState(worker, WorkerState.UseRestroom);
            yield return WaitForState(worker, WorkerState.Work, 8f);
            Assert.That(worker.Decision.category, Is.EqualTo(WorkerDecisionCategory.Work));
        }

        [UnityTest] public IEnumerator DesklessWorkerResumesPhoneWorkAfterRecovery()
        {
            yield return LoadStarter(); OfficeDirector office = Office(); office.Cash.AccrueDeskIncome(10f,60f);
            Assert.True(office.TryHire(0, out string reason), reason); WorkerAgent worker = office.Workers[3];
            yield return Ready(worker); SetCritical(worker, NeedKind.Bathroom);
            yield return WaitForState(worker, WorkerState.UseRestroom); yield return WaitForState(worker, WorkerState.Unassigned, 8f);
            Assert.True(worker.IsPhoneWorking);
        }

        [UnityTest] public IEnumerator DeskAssignmentSurvivesCompleteRecoveryTrip()
        {
            yield return LoadStarter(); WorkerAgent worker = Office().Workers[0]; yield return Ready(worker);
            Workstation desk = worker.Desk; SetCritical(worker, NeedKind.Bathroom);
            yield return WaitForState(worker, WorkerState.UseRestroom); yield return WaitForState(worker, WorkerState.Work, 8f);
            Assert.That(worker.Desk, Is.SameAs(desk)); Assert.That(desk.Assigned, Is.SameAs(worker));
        }

        [UnityTest] public IEnumerator TwoEmployeesNeverOverfillRestroom()
        {
            yield return LoadStarter(); OfficeDirector office = Office(); yield return Ready(office.Workers[0]);
            SetCritical(office.Workers[0], NeedKind.Bathroom); SetCritical(office.Workers[1], NeedKind.Bathroom);
            yield return new WaitForSecondsRealtime(.5f);
            PlacementZone restroom = Zone(office, "starter.restroom.main");
            Assert.That(restroom.EffectiveUsage, Is.LessThanOrEqualTo(restroom.Capacity));
        }

        [UnityTest] public IEnumerator FullStationWorkerReroutesOrUsesFallback()
        {
            yield return LoadStarter(); OfficeDirector office = Office(); yield return Ready(office.Workers[0]);
            SetCritical(office.Workers[0], NeedKind.Bathroom); yield return WaitForDecision(office.Workers[0], PlacementActivity.UseRestroom);
            SetCritical(office.Workers[1], NeedKind.Bathroom); yield return WaitForDecision(office.Workers[1], PlacementActivity.LeaveOffice);
        }

        [UnityTest] public IEnumerator PlayerPickupAndGroundDropReleasesReservation()
        {
            yield return LoadStarter(); OfficeDirector office = Office(); WorkerAgent worker = office.Workers[0]; yield return Ready(worker);
            SetCritical(worker, NeedKind.Bathroom); yield return WaitForDecision(worker, PlacementActivity.UseRestroom);
            Assert.True(worker.BeginPlayerCarry(out _)); Assert.That(worker.ReservationLabel, Is.EqualTo("Suspended"));
            Assert.True(office.TryIssueGroundPlacementCommand(worker, new Vector3(3f,0f,0f), out _, out string reason), reason);
            Assert.IsNull(office.Reservations.Get(worker));
        }

        [UnityTest] public IEnumerator PickupDuringNavigationDoesNotStrandStation()
        {
            yield return LoadStarter(); OfficeDirector office = Office(); WorkerAgent worker = office.Workers[0]; yield return Ready(worker);
            SetCritical(worker, NeedKind.Bathroom); yield return WaitForDecision(worker, PlacementActivity.UseRestroom);
            Assert.True(worker.BeginPlayerCarry(out _)); worker.CancelPlayerCarryImmediate();
            Assert.NotNull(office.Reservations.Get(worker)); Assert.That(Zone(office,"starter.restroom.main").EffectiveUsage, Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator FiringDuringNavigationReleasesReservation()
        {
            yield return LoadStarter(); OfficeDirector office = Office(); WorkerAgent worker = office.Workers[0]; yield return Ready(worker);
            SetCritical(worker, NeedKind.Bathroom); yield return WaitForDecision(worker, PlacementActivity.UseRestroom);
            Assert.True(office.TryFire(worker, out string reason), reason); Assert.IsNull(office.Reservations.Get(worker));
        }

        [UnityTest] public IEnumerator RestartClearsAllReservations()
        {
            yield return LoadStarter(); OfficeDirector office = Office(); WorkerAgent worker = office.Workers[0]; yield return Ready(worker);
            SetCritical(worker, NeedKind.Bathroom); yield return WaitForDecision(worker, PlacementActivity.UseRestroom);
            ActivityReservationService reservations = office.Reservations; office.Restart(); yield return null; yield return null;
            Assert.That(reservations.Count, Is.Zero);
        }

        [UnityTest] public IEnumerator SceneExitClearsAllReservations()
        {
            yield return LoadStarter(); OfficeDirector office = Office(); WorkerAgent worker = office.Workers[0]; yield return Ready(worker);
            SetCritical(worker, NeedKind.Bathroom); yield return WaitForDecision(worker, PlacementActivity.UseRestroom);
            ActivityReservationService reservations = office.Reservations; office.ReturnToMenu(); yield return null; yield return null;
            Assert.That(reservations.Count, Is.Zero);
        }

        [UnityTest] public IEnumerator StationDisablementReroutesIncomingWorker()
        {
            yield return LoadStarter(); OfficeDirector office = Office(); WorkerAgent worker = office.Workers[0]; yield return Ready(worker);
            SetCritical(worker, NeedKind.Bathroom); yield return WaitForDecision(worker, PlacementActivity.UseRestroom);
            Zone(office,"starter.restroom.main").SetZoneEnabled(false); yield return new WaitForSecondsRealtime(.2f);
            Assert.That(worker.DecisionReasonLabel, Does.Contain("Rerouting"));
        }

        [UnityTest] public IEnumerator PauseFreezesNavigationDecisionAndActivityTimers()
        {
            yield return LoadStarter(); WorkerAgent worker = Office().Workers[0]; yield return Ready(worker);
            SetCritical(worker, NeedKind.Bathroom); yield return WaitForDecision(worker, PlacementActivity.UseRestroom);
            Vector3 position = worker.transform.position; float age = worker.StateAge;
            SimulationSpeedController.Instance.SetSpeed(0f); yield return new WaitForSecondsRealtime(.4f);
            Assert.That(worker.transform.position, Is.EqualTo(position)); Assert.That(worker.StateAge, Is.EqualTo(age).Within(.0001f));
        }

        [UnityTest] public IEnumerator ResumeContinuesWithoutDuplicateRecovery()
        {
            yield return LoadStarter(); OfficeDirector office = Office(); WorkerAgent worker = office.Workers[0]; yield return Ready(worker);
            SetCritical(worker, NeedKind.Bathroom); yield return WaitForDecision(worker, PlacementActivity.UseRestroom);
            int decisions = office.AutonomyCounters.autonomousRecoveryDecisions; SimulationSpeedController.Instance.SetSpeed(0f);
            yield return new WaitForSecondsRealtime(.2f); SimulationSpeedController.Instance.SetSpeed(4f);
            yield return WaitForState(worker, WorkerState.UseRestroom);
            Assert.That(office.AutonomyCounters.autonomousRecoveryDecisions, Is.EqualTo(decisions));
        }

        [UnityTest] public IEnumerator CriticalBathroomInterruptsDistraction()
        {
            yield return LoadStarter(); WorkerAgent worker = Office().Workers[0]; yield return Ready(worker);
            worker.BeginDistractionForTesting(DistractionKind.Sleep); Assert.That(worker.Runtime.behavior, Is.EqualTo(WorkerState.Sleep));
            SetCritical(worker, NeedKind.Bathroom); yield return WaitForDecision(worker, PlacementActivity.UseRestroom);
            Assert.That(worker.Runtime.behavior, Is.Not.EqualTo(WorkerState.Sleep));
        }

        [UnityTest] public IEnumerator SocialRecoveryRemainsLowerPriorityThanEmergency()
        {
            yield return LoadStarter(); WorkerAgent worker = Office().Workers[1]; yield return Ready(worker);
            worker.Runtime.socialNeed = 1f; SetCritical(worker, NeedKind.Bathroom);
            yield return WaitForDecision(worker, PlacementActivity.UseRestroom);
            Assert.That(worker.Runtime.behavior, Is.Not.EqualTo(WorkerState.Socialize));
        }

        [UnityTest] public IEnumerator PlayerRestCommandSurvivesOrdinaryAutonomy()
        {
            yield return LoadStarter(); OfficeDirector office = Office(); WorkerAgent worker = office.Workers[0]; yield return Ready(worker);
            PlacementZone rest = Zone(office,"starter.rest.break-nook"); Assert.True(worker.BeginPlayerCarry(out _));
            Assert.True(office.TryIssueWorkerCommand(worker, rest, out _, out string reason), reason);
            yield return new WaitForSecondsRealtime(.5f);
            Assert.True(worker.HasPlayerCommandAuthority); Assert.That(worker.DecisionOwnerLabel, Is.EqualTo("Player instruction"));
        }

        [UnityTest] public IEnumerator CriticalBathroomExplainsPlayerCommandOverride()
        {
            yield return LoadStarter(); OfficeDirector office = Office(); WorkerAgent worker = office.Workers[0]; yield return Ready(worker);
            Assert.True(worker.BeginPlayerCarry(out _)); Assert.True(office.TryIssueWorkerCommand(worker,
                Zone(office,"starter.rest.break-nook"), out _, out string reason), reason);
            SetCritical(worker, NeedKind.Bathroom); yield return WaitForDecision(worker, PlacementActivity.UseRestroom, 6f);
            Assert.That(worker.DecisionReasonLabel, Does.Contain("Bathroom")); Assert.False(worker.Decision.playerOrigin);
        }

        [UnityTest] public IEnumerator RepeatedPathFailureExitsBoundedRecovery()
        {
            yield return LoadStarter(); WorkerAgent worker = Office().Workers[0]; yield return Ready(worker);
            for (int i = 0; i <= NeedAutonomyRules.MaximumRepathAttempts; i++) worker.ForceNavigationFailureForTesting();
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(worker.Runtime.behavior, Is.Not.EqualTo(WorkerState.RecoverFromStuck));
            Assert.That(worker.Decision.retryCount, Is.LessThanOrEqualTo(NeedAutonomyRules.MaximumRepathAttempts + 1));
        }

        [UnityTest] public IEnumerator WorkerNeverRemainsInRecoveryIndefinitely()
        {
            yield return LoadStarter(); WorkerAgent worker = Office().Workers[0]; yield return Ready(worker);
            worker.ForceNavigationFailureForTesting(); SimulationSpeedController.Instance.SetSpeed(4f);
            yield return new WaitForSecondsRealtime(1f);
            Assert.That(worker.Runtime.behavior, Is.Not.EqualTo(WorkerState.RecoverFromStuck));
        }

        [UnityTest] public IEnumerator InspectorDisplaysAutonomousDecisionReason()
        {
            yield return LoadStarter(); OfficeDirector office = Office(); WorkerAgent worker = office.Workers[0]; yield return Ready(worker);
            SetCritical(worker, NeedKind.Bathroom); yield return WaitForDecision(worker, PlacementActivity.UseRestroom);
            WorkerSelection.Select(worker); yield return new WaitForSecondsRealtime(.25f);
            Assert.That(office.HUD.InspectorText, Does.Contain("Autonomous")); Assert.That(office.HUD.InspectorText, Does.Contain("Bathroom"));
        }

        [UnityTest] public IEnumerator InspectorDistinguishesPlayerIssuedActivity()
        {
            yield return LoadStarter(); OfficeDirector office = Office(); WorkerAgent worker = office.Workers[0]; yield return Ready(worker);
            Assert.True(worker.BeginPlayerCarry(out _)); Assert.True(office.TryIssueWorkerCommand(worker,
                Zone(office,"starter.rest.break-nook"), out _, out string reason), reason);
            WorkerSelection.Select(worker); yield return new WaitForSecondsRealtime(.25f);
            Assert.That(office.HUD.InspectorText, Does.Contain("Player instruction"));
        }

        [UnityTest] public IEnumerator WorldFeedbackIdentifiesCriticalNeed()
        {
            yield return LoadStarter(); WorkerAgent worker = Office().Workers[0]; yield return Ready(worker);
            SetCritical(worker, NeedKind.Bathroom); yield return WaitForDecision(worker, PlacementActivity.UseRestroom);
            Assert.False(string.IsNullOrEmpty(worker.Visuals.CurrentEmote));
        }

        [UnityTest] public IEnumerator FreeGroundPlacementStillWorksWithAutonomy()
        {
            yield return LoadStarter(); OfficeDirector office = Office(); WorkerAgent worker = office.Workers[0]; yield return Ready(worker);
            Assert.True(worker.BeginPlayerCarry(out _)); Assert.True(office.TryIssueGroundPlacementCommand(worker,
                new Vector3(3f,0f,.5f), out _, out string reason), reason);
            Assert.NotNull(worker.LastGroundPlacementCommand);
        }

        [UnityTest] public IEnumerator ProximityPlacementStillStartsActivity()
        {
            yield return LoadStarter(); OfficeDirector office = Office(); WorkerAgent worker = office.Workers[0]; yield return Ready(worker);
            PlacementZone water = Zone(office,"starter.water.cooler"); Assert.True(worker.BeginPlayerCarry(out _));
            Assert.True(office.TryIssueWorkerCommand(worker, water, out _, out string reason), reason);
            Assert.That(worker.LastPlayerCommand.requestedActivity, Is.EqualTo(PlacementActivity.GetWater));
        }

        [UnityTest] public IEnumerator PhoneProductivityRemainsHalfWorkstationFactor()
        {
            yield return LoadStarter(); OfficeDirector office = Office(); office.Cash.AccrueDeskIncome(10f,60f);
            Assert.True(office.TryHire(0,out string reason),reason); WorkerAgent phone=office.Workers[3]; yield return Ready(phone);
            yield return null;
            float trait=ProductivityModel.TraitModifier(phone.Definition.trait,.5f,office.Workday.Progress01,phone.Runtime.energy);
            float expected=ProductivityModel.Evaluate(phone.Definition.skill,phone.Runtime,
                ProductivityModel.PhoneWorkstationModifier,trait,1f);
            Assert.That(phone.Productivity,Is.EqualTo(expected).Within(.03f));
        }

        [UnityTest] public IEnumerator HiringRemainsAvailableWheneverAffordable()
        {
            yield return LoadStarter(); OfficeDirector office=Office(); office.Cash.AccrueDeskIncome(10f,60f);
            Assert.True(office.CanHireWorkers); Assert.True(office.TryHire(0,out string reason),reason);
        }

        [UnityTest] public IEnumerator AutonomousVendingChargesExactlyOnce()
        {
            yield return LoadStarter(); OfficeDirector office=Office(); WorkerAgent worker=office.Workers[0]; yield return Ready(worker);
            office.Cash.AccrueDeskIncome(10f,60f); SetCritical(worker,NeedKind.Hunger);
            yield return WaitForState(worker,WorkerState.BuySnack); int charges=worker.VendingCharges;
            yield return new WaitForSecondsRealtime(.3f); Assert.That(worker.VendingCharges,Is.EqualTo(charges));
        }

        [UnityTest] public IEnumerator ExistingActivityCooldownsStillApply()
        {
            yield return LoadStarter(); OfficeDirector office=Office(); WorkerAgent worker=office.Workers[0]; yield return Ready(worker);
            PlacementZone water=Zone(office,"starter.water.cooler"); Assert.True(worker.BeginPlayerCarry(out _));
            Assert.True(office.TryIssueWorkerCommand(worker,water,out _,out string reason),reason);
            worker.transform.position=water.PlacementPoint.position; yield return WaitForState(worker,WorkerState.UseWaterCooler);
            yield return WaitForState(worker,WorkerState.ReturnToDesk,6f); Assert.That(worker.Runtime.waterCooldown,Is.GreaterThan(0f));
        }

        [UnityTest] public IEnumerator PassiveOfficeRunsWithoutPermanentWorkerFailure()
        {
            yield return LoadStarter(); OfficeDirector office=Office(); SimulationSpeedController.Instance.SetSpeed(4f);
            yield return new WaitForSecondsRealtime(5f);
            for(int i=0;i<office.Workers.Count;i++)
            {
                WorkerAgent worker=office.Workers[i]; Assert.False(worker.IsFired); Assert.True(worker.IsVisibleInOffice || worker.IsAway);
                Assert.That(worker.Decision.retryCount,Is.LessThanOrEqualTo(NeedAutonomyRules.MaximumRepathAttempts));
            }
            Assert.That(office.Reservations.Count,Is.LessThanOrEqualTo(office.ActiveWorkerCount));
        }
    }
}
