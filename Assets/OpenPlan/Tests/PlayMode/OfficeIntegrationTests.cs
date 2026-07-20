using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;

namespace OpenPlan.Tests
{
    public sealed class OfficeIntegrationTests
    {
        private static IEnumerator LoadOffice(OfficeStage stage = OfficeStage.EstablishedOffice)
        {
            Time.timeScale = 1f;
            OfficeStageSelection.SelectForNextLoad(stage);
            SceneManager.LoadScene("Office");
            yield return null;
            yield return null;
            Assert.NotNull(Object.FindFirstObjectByType<OfficeDirector>());
        }

        private static IEnumerator WaitForPickable(WorkerAgent worker)
        {
            float deadline = Time.realtimeSinceStartup + 3f;
            while (worker != null && !worker.CanBeginPlayerCarry(out _) && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.NotNull(worker);
            Assert.True(worker.CanBeginPlayerCarry(out string reason), reason);
        }

        private static void StartCarry(WorkerCarryController controller, WorkerAgent worker)
        {
            Vector2 press = new Vector2(200f, 200f);
            Assert.True(controller.BeginPointerGesture(worker, press, false));
            Assert.True(controller.EvaluateCarryStart(press + new Vector2(7f,0f), .01f, worker.transform.position));
            Assert.True(controller.IsCarrying);
        }

        private static PlacementZone FindZone(OfficeDirector office, string stableIdentifier)
        {
            foreach (PlacementZone zone in office.PlacementZones)
                if (zone.StableIdentifier == stableIdentifier) return zone;
            Assert.Fail("Missing placement zone " + stableIdentifier);
            return null;
        }

        private static IEnumerator PlaceWorker(OfficeDirector office, WorkerAgent worker, PlacementZone zone)
        {
            yield return WaitForPickable(worker);
            StartCarry(office.CarryController, worker);
            office.CarryController.UpdateCarriedPosition(zone.PlacementPoint.position, zone,
                new Vector2(Screen.width * .5f, Screen.height * .5f), true);
            Assert.True(office.CarryController.HasValidTarget, office.CarryController.FeedbackText);
            office.CarryController.ReleaseAtZone(zone);
            yield return new WaitForSecondsRealtime(.20f);
            Assert.That(office.LastIssuedCommand?.destinationZone, Is.EqualTo(zone));
            worker.transform.position = zone.PlacementPoint.position;
            yield return null;
        }

        private static IEnumerator WaitForState(WorkerAgent worker, WorkerState state, float realSeconds = 8f)
        {
            float deadline = Time.realtimeSinceStartup + realSeconds;
            while (worker != null && worker.Runtime.behavior != state && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.NotNull(worker);
            Assert.That(worker.Runtime.behavior, Is.EqualTo(state));
        }

        private static IEnumerator WaitForTutorialStep(TutorialController tutorial, TutorialStep step, float realSeconds = 5f)
        {
            float deadline = Time.realtimeSinceStartup + realSeconds;
            while (tutorial != null && tutorial.CurrentStep != step && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.NotNull(tutorial);
            Assert.That(tutorial.CurrentStep, Is.EqualTo(step));
        }

        [UnityTest] public IEnumerator MainMenuLoads()
        {
            SceneManager.LoadScene("MainMenu");
            yield return null;
            Assert.NotNull(Object.FindFirstObjectByType<MainMenuController>());
        }

        [UnityTest] public IEnumerator MainMenuStartEntersStarterOffice()
        {
            OfficeStageSelection.ClearPendingSelection();
            SceneManager.LoadScene("MainMenu");
            yield return null;
            Button start = GameObject.Find("Start").GetComponent<Button>();
            Assert.NotNull(start);
            start.onClick.Invoke();
            yield return null;
            yield return null;
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            Assert.NotNull(office);
            Assert.That(office.Stage, Is.EqualTo(OfficeStage.StarterOffice));
        }

        [UnityTest] public IEnumerator StarterOfficeInitializesIndependentlyWithThreeWorkers()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            Assert.That(office.Stage, Is.EqualTo(OfficeStage.StarterOffice));
            Assert.That(office.Workers.Count, Is.EqualTo(3));
            Assert.That(office.Workstations.Count, Is.EqualTo(7));
            Assert.That(office.WorkerCapacity, Is.EqualTo(3));
            Assert.That(office.Cash.CurrentCash, Is.Zero);
            Assert.NotNull(GameObject.Find("2×"));
            Assert.False(office.Workday.IsTimed);
            var expectedActiveZones = new Dictionary<PlacementActivity, int>
            {
                { PlacementActivity.Work, 3 }, { PlacementActivity.Rest, 1 },
                { PlacementActivity.GetWater, 1 }, { PlacementActivity.BuySnack, 1 },
                { PlacementActivity.Smoke, 1 }, { PlacementActivity.LeaveOffice, 1 },
                { PlacementActivity.UseRestroom, 1 }
            };
            foreach (KeyValuePair<PlacementActivity, int> expected in expectedActiveZones)
            {
                int actual = 0;
                foreach (PlacementZone zone in office.PlacementZones)
                    if (zone.IsZoneEnabled && zone.Activity == expected.Key) actual++;
                Assert.That(actual, Is.EqualTo(expected.Value), expected.Key.ToString());
            }

            var identifiers = new HashSet<string>();
            foreach (PlacementZone zone in office.PlacementZones)
            {
                Assert.False(string.IsNullOrWhiteSpace(zone.StableIdentifier));
                Assert.True(identifiers.Add(zone.StableIdentifier), "Duplicate zone ID " + zone.StableIdentifier);
                Assert.False(string.IsNullOrWhiteSpace(zone.ActivityLabel));
                Assert.NotNull(zone.PlacementPoint);
                Assert.NotNull(zone.FootprintCollider);
                Assert.That(zone.Capacity, Is.GreaterThan(0));
            }

            int occupiedDesks = 0;
            foreach (Workstation desk in office.Workstations)
                if (desk.IsZoneEnabled && desk.Assigned != null) occupiedDesks++;
            Assert.That(occupiedDesks, Is.EqualTo(3));
            Assert.That(office.HUD.HeaderText, Does.Contain("TEAM  3"));
            Assert.That(office.HUD.HeaderText, Does.Contain("DESKS  3"));
            Assert.That(office.Workers[0].Definition.displayName, Is.EqualTo("Morgan"));
            Assert.That(office.Workers[1].Definition.displayName, Is.EqualTo("Alex"));
            Assert.That(office.Workers[2].Definition.displayName, Is.EqualTo("Sam"));
        }

        [UnityTest] public IEnumerator LockedNeighborZonesRejectWorkersAndRoutesStayClear()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            int lockedNeighborZones = 0;
            foreach (PlacementZone zone in office.PlacementZones)
            {
                if (!zone.StableIdentifier.StartsWith("neighbor.")) continue;
                lockedNeighborZones++;
                Assert.False(zone.IsZoneEnabled);
                Assert.False(zone.CanAcceptWorker(office.Workers[0], out string reason));
                Assert.That(reason, Does.Contain("locked"));
            }
            Assert.That(lockedNeighborZones, Is.EqualTo(4));
            Assert.True(office.Layout.ValidateZoneGeometry(office.PlacementZones, out string geometryReason), geometryReason);
        }

        [UnityTest] public IEnumerator StarterCameraOverviewContainsEveryRequiredSpace()
        {
            Screen.SetResolution(1920, 1080, false);
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            OfficeCameraRig rig = Camera.main.GetComponent<OfficeCameraRig>();
            rig.Overview();
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(rig.OrthographicSize, Is.EqualTo(office.Layout.OverviewOrthographicSize).Within(.1f));
            Assert.True(office.Layout.CameraContainsRequiredSpaces(Camera.main), "Overview clips a required Starter Office space.");
            Assert.That(rig.PanBounds, Is.EqualTo(office.Layout.PanBounds));
        }

        [UnityTest] public IEnumerator WorkerClickSelectsWithoutStartingCarryAndUiPressIsIgnored()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            WorkerAgent worker = office.Workers[0];
            yield return WaitForPickable(worker);
            WorkerSelection.Clear();

            Assert.True(office.CarryController.BeginPointerGesture(worker, new Vector2(200f,200f), false));
            office.CarryController.ReleaseAtZone(null);
            Assert.That(WorkerSelection.Selected, Is.EqualTo(worker));
            Assert.False(office.CarryController.IsCarrying);
            Assert.That(office.CarryController.Phase, Is.EqualTo(WorkerCarryPhase.Idle));
            Assert.True(GameObject.Find("Employee Card").activeInHierarchy);

            WorkerSelection.Clear();
            Assert.False(office.CarryController.BeginPointerGesture(worker, new Vector2(1900f,1040f), true));
            Assert.False(office.CarryController.IsCarrying);
            Assert.IsNull(WorkerSelection.Selected);
            office.CarryController.CancelCarry(true);
        }

        [UnityTest] public IEnumerator CarryStartsOnlyPastThresholdAndImmediateCancelRestoresState()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            WorkerAgent worker = office.Workers[0];
            yield return WaitForPickable(worker);
            Vector3 original = worker.transform.position;
            WorkerState originalState = worker.Runtime.behavior;
            Vector2 press = new Vector2(320f,240f);

            Assert.True(office.CarryController.BeginPointerGesture(worker, press, false));
            Assert.False(office.CarryController.EvaluateCarryStart(press + new Vector2(6f,0f), .119f, original));
            Assert.False(worker.IsPlayerCarried);
            Assert.True(office.CarryController.EvaluateCarryStart(press + new Vector2(6.1f,0f), .01f, original));
            Assert.That(worker.transform.position.y, Is.EqualTo(original.y + WorkerCarryController.CarryLiftMeters).Within(.01f));
            Assert.That(worker.Runtime.behavior, Is.EqualTo(originalState));

            office.CarryController.CancelCarry(true);
            Assert.False(worker.IsPlayerCarried);
            Assert.That(Vector3.Distance(worker.transform.position, original), Is.LessThan(.001f));
            Assert.That(worker.Runtime.behavior, Is.EqualTo(originalState));
        }

        [UnityTest] public IEnumerator ValidDropIssuesCommandAndWorkerWalksFinalSegment()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            WorkerAgent worker = office.Workers[0];
            yield return WaitForPickable(worker);
            PlacementZone rest = FindZone(office, "starter.rest.break-nook");
            bool movingWhenIssued = false;
            office.WorkerCommandIssued += _ => movingWhenIssued = worker.IsMoving;

            StartCarry(office.CarryController, worker);
            Vector3 drop = rest.PlacementPoint.position + new Vector3(.78f,0f,.62f);
            office.CarryController.UpdateCarriedPosition(drop, rest, new Vector2(620f,430f), true);
            Assert.True(office.CarryController.HasValidTarget);
            Assert.That(rest.CarryVisualState, Is.EqualTo(PlacementZoneVisualState.HoveredValid));
            Assert.That(office.CarryController.FeedbackText, Does.Contain("REST"));
            office.CarryController.ReleaseAtZone(rest);
            yield return new WaitForSeconds(.18f);

            Assert.NotNull(office.LastIssuedCommand);
            Assert.That(office.LastIssuedCommand.worker, Is.EqualTo(worker));
            Assert.That(office.LastIssuedCommand.destinationZone, Is.EqualTo(rest));
            Assert.That(office.LastIssuedCommand.requestedActivity, Is.EqualTo(PlacementActivity.Rest));
            Assert.True(office.LastIssuedCommand.fromPlayerPlacement);
            Assert.True(movingWhenIssued, "Worker movement must resume when the command is issued.");
            Assert.That(office.Audio.LastCue, Is.EqualTo("placement-success"));
            Assert.That(WorkerSelection.Selected, Is.EqualTo(worker));
            yield return new WaitForSeconds(.7f);
            Vector2 flatDelta = new Vector2(worker.transform.position.x - rest.PlacementPoint.position.x,
                worker.transform.position.z - rest.PlacementPoint.position.z);
            Assert.That(flatDelta.magnitude, Is.LessThan(.12f));
        }

        [UnityTest] public IEnumerator FloorOccupiedAndLockedDropsRejectAndRestoreWorker()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            WorkerAgent worker = office.Workers[1];
            yield return WaitForPickable(worker);
            Vector3 original = worker.transform.position;
            Workstation assignedDesk = worker.Desk;
            int commandCount = 0;
            office.WorkerCommandIssued += _ => commandCount++;

            StartCarry(office.CarryController, worker);
            Vector3 freeDrop = original + Vector3.right * 1.2f;
            office.CarryController.UpdateCarriedPosition(freeDrop, null, new Vector2(500f,400f), true);
            Assert.True(office.CarryController.HasValidTarget);
            Assert.That(office.CarryController.FeedbackText, Does.Contain("AUTONOMY"));
            office.CarryController.ReleaseAtGround(freeDrop);
            yield return new WaitForSeconds(.18f);
            Assert.NotNull(office.LastGroundPlacementCommand);
            Assert.That(Vector3.Distance(worker.transform.position, freeDrop), Is.LessThan(.08f));
            Assert.That(worker.Desk, Is.SameAs(assignedDesk));
            Assert.That(assignedDesk.Assigned, Is.SameAs(worker));
            Assert.That(WorkerSelection.Selected, Is.SameAs(worker));

            Workstation occupied = office.Workers[0].Desk;
            StartCarry(office.CarryController, worker);
            office.CarryController.UpdateCarriedPosition(occupied.PlacementPoint.position, occupied, new Vector2(540f,400f), true);
            Assert.That(office.CarryController.FeedbackText, Does.Contain("DESK OCCUPIED"));
            office.CarryController.ReleaseAtZone(occupied);
            yield return new WaitForSeconds(.28f);
            Assert.That(office.CarryController.LastRejectionReason, Is.EqualTo("Desk occupied."));

            PlacementZone locked = FindZone(office, "neighbor.work.01");
            Vector3 lockedOriginal = worker.transform.position;
            WorkerState lockedState = worker.Runtime.behavior;
            float lockedEnergy = worker.Runtime.energy;
            StartCarry(office.CarryController, worker);
            office.CarryController.UpdateCarriedPosition(locked.PlacementPoint.position, locked, new Vector2(1000f,400f), true);
            Assert.That(locked.CarryVisualState, Is.EqualTo(PlacementZoneVisualState.HoveredInvalid));
            Assert.That(office.CarryController.FeedbackText, Does.Contain("AREA LOCKED"));
            office.CarryController.ReleaseAtZone(locked);
            yield return new WaitForSeconds(.28f);

            Assert.That(commandCount, Is.Zero);
            Assert.That(office.Audio.LastCue, Is.EqualTo("placement-rejected"));
            Assert.False(worker.IsPlayerCarried);
            Assert.That(Vector3.Distance(worker.transform.position, lockedOriginal), Is.LessThan(.08f));
            Assert.That(worker.LastRestoredCarryState, Is.EqualTo(lockedState));
            Assert.That(worker.Runtime.energy, Is.EqualTo(lockedEnergy).Within(.003f));
            Assert.That(locked.Occupancy, Is.Zero);
        }

        [UnityTest] public IEnumerator OrdinaryGroundPlacementPreservesStateAndResumesAssignedWork()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            WorkerAgent worker = office.Workers[0];
            yield return WaitForPickable(worker);
            yield return WaitForPickable(office.Workers[1]);
            yield return WaitForPickable(office.Workers[2]);
            Assert.True(office.Workers[1].BeginPlayerCarry(out _));
            Assert.True(office.Workers[2].BeginPlayerCarry(out _));

            Workstation desk = worker.Desk;
            StartCarry(office.CarryController, worker);
            worker.Runtime.waterCooldown = 12f;
            worker.Runtime.vendingCooldown = 23f;
            worker.Runtime.smokingCooldown = 34f;
            float energy = worker.Runtime.energy;
            float mood = worker.Runtime.mood;
            float hunger = worker.Runtime.hunger;
            float bathroom = worker.Runtime.bathroom;
            float inspiration = worker.Runtime.inspiration;
            float stress = worker.Runtime.stress;
            float cash = office.Cash.CurrentCash;
            float committedWaterCooldown = -1f;
            float committedVendingCooldown = -1f;
            float committedSmokingCooldown = -1f;
            office.GroundPlacementCommandIssued += _ =>
            {
                committedWaterCooldown = worker.Runtime.waterCooldown;
                committedVendingCooldown = worker.Runtime.vendingCooldown;
                committedSmokingCooldown = worker.Runtime.smokingCooldown;
            };
            Vector3 ground = new Vector3(-.6f, 0f, .2f);
            office.CarryController.UpdateCarriedPosition(ground, null, new Vector2(610f, 410f), true);
            Assert.True(office.CarryController.HasValidTarget, office.CarryController.FeedbackText);
            office.CarryController.ReleaseAtGround(ground);
            yield return new WaitForSeconds(.18f);

            Assert.That(worker.Desk, Is.SameAs(desk));
            Assert.That(desk.Assigned, Is.SameAs(worker));
            Assert.That(worker.Runtime.behavior, Is.EqualTo(WorkerState.Unassigned));
            Assert.That(committedWaterCooldown, Is.EqualTo(12f).Within(.001f));
            Assert.That(committedVendingCooldown, Is.EqualTo(23f).Within(.001f));
            Assert.That(committedSmokingCooldown, Is.EqualTo(34f).Within(.001f));
            Assert.That(worker.Runtime.energy, Is.EqualTo(energy).Within(.001f));
            Assert.That(worker.Runtime.mood, Is.EqualTo(mood).Within(.001f));
            Assert.That(worker.Runtime.hunger, Is.EqualTo(hunger).Within(.001f));
            Assert.That(worker.Runtime.bathroom, Is.EqualTo(bathroom).Within(.001f));
            Assert.That(worker.Runtime.inspiration, Is.EqualTo(inspiration).Within(.001f));
            Assert.That(worker.Runtime.stress, Is.EqualTo(stress).Within(.001f));
            Assert.That(office.Cash.CurrentCash, Is.EqualTo(cash).Within(.001f));
            Assert.That(WorkerSelection.Selected, Is.SameAs(worker));

            office.Workers[1].CancelPlayerCarryImmediate();
            office.Workers[2].CancelPlayerCarryImmediate();
            SimulationSpeedController.Instance.SetSpeed(4f);
            float deadline = Time.realtimeSinceStartup + 4f;
            while (worker.Runtime.behavior != WorkerState.ReturnToDesk &&
                   worker.Runtime.behavior != WorkerState.Work && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(worker.Runtime.behavior,
                Is.EqualTo(WorkerState.ReturnToDesk).Or.EqualTo(WorkerState.Work));
        }

        [UnityTest] public IEnumerator ProximityInfluenceSelectsActivityOutsideVisibleFootprint()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            WorkerAgent worker = office.Workers[0];
            yield return WaitForPickable(worker);
            PlacementZone water = FindZone(office, "starter.water.cooler");
            Vector3 drop = water.PlacementPoint.position + Vector3.left * (water.FootprintBounds.extents.x + .25f);
            Assert.False(water.FootprintBounds.Contains(new Vector3(drop.x, water.FootprintBounds.center.y, drop.z)));
            PlacementZone influenced = WorkerCarryController.ResolveInfluencingZone(office.PlacementZones, drop);
            Assert.That(influenced, Is.SameAs(water));

            StartCarry(office.CarryController, worker);
            office.CarryController.UpdateCarriedPosition(drop, influenced, new Vector2(720f, 420f), true);
            Assert.That(office.CarryController.FeedbackText, Does.Contain("GET WATER"));
            office.CarryController.ReleaseAtGround(drop, influenced);
            yield return new WaitForSeconds(.18f);
            Assert.That(office.LastIssuedCommand.destinationZone, Is.SameAs(water));
            SimulationSpeedController.Instance.SetSpeed(4f);
            yield return WaitForState(worker, WorkerState.UseWaterCooler, 3f);
        }

        [UnityTest] public IEnumerator LockedObstacleAndVoidGroundDropsExplainAndRestore()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            WorkerAgent worker = office.Workers[0];
            yield return WaitForPickable(worker);

            Vector3 original = worker.transform.position;
            StartCarry(office.CarryController, worker);
            Vector3 locked = new Vector3(10f, 0f, 0f);
            office.CarryController.UpdateCarriedPosition(locked, null, new Vector2(1020f, 420f), true);
            Assert.False(office.CarryController.HasValidTarget);
            Assert.That(office.CarryController.FeedbackText, Does.Contain("LOCKED NEIGHBORING PROPERTY"));
            office.CarryController.ReleaseAtGround(locked);
            yield return new WaitForSeconds(.28f);
            Assert.That(Vector3.Distance(worker.transform.position, original), Is.LessThan(.08f));

            StartCarry(office.CarryController, worker);
            Vector3 wall = new Vector3(-8f, 0f, .5f);
            office.CarryController.UpdateCarriedPosition(wall, null, new Vector2(380f, 420f), true);
            Assert.That(office.CarryController.FeedbackText, Does.Contain("BLOCKED BY WALL.STARTER.WEST.MIDDLE"));
            office.CarryController.CancelCarry(true);

            StartCarry(office.CarryController, worker);
            Vector3 voidPoint = new Vector3(0f, 0f, 7f);
            office.CarryController.UpdateCarriedPosition(voidPoint, null, new Vector2(680f, 180f), true);
            Assert.That(office.CarryController.FeedbackText, Does.Contain("OUTSIDE THE UNLOCKED OFFICE"));
            office.CarryController.CancelCarry(true);
            Assert.False(worker.IsPlayerCarried);
        }

        [UnityTest] public IEnumerator ModalOpeningCancelsCarryAndReturnsWorkerToGround()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            WorkerAgent worker = office.Workers[0];
            yield return WaitForPickable(worker);
            Vector3 original = worker.transform.position;
            StartCarry(office.CarryController, worker);
            office.HUD.ShowHiringForCapture();
            yield return null;
            Assert.That(office.CarryController.Phase, Is.EqualTo(WorkerCarryPhase.Returning));
            yield return new WaitForSeconds(.28f);
            Assert.That(office.CarryController.Phase, Is.EqualTo(WorkerCarryPhase.Idle));
            Assert.False(worker.IsPlayerCarried);
            Assert.That(worker.transform.position.y, Is.EqualTo(original.y).Within(.001f));
            office.HUD.HideHiringForCapture();
        }

        [UnityTest] public IEnumerator PauseFreezesPlacementAnimationThenPlacementCompletes()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            WorkerAgent worker = office.Workers[0];
            yield return WaitForPickable(worker);
            PlacementZone water = FindZone(office, "starter.water.cooler");
            StartCarry(office.CarryController, worker);
            Vector3 drop = water.PlacementPoint.position + new Vector3(.45f,0f,.30f);
            office.CarryController.UpdateCarriedPosition(drop, water, new Vector2(700f,420f), true);
            office.CarryController.ReleaseAtZone(water);
            Vector3 frozen = worker.transform.position;
            SimulationSpeedController.Instance.SetSpeed(0f);
            yield return new WaitForSecondsRealtime(.22f);
            Assert.That(office.CarryController.Phase, Is.EqualTo(WorkerCarryPhase.Placing));
            Assert.That(Vector3.Distance(worker.transform.position, frozen), Is.LessThan(.001f));
            SimulationSpeedController.Instance.SetSpeed(1f);
            yield return new WaitForSeconds(.18f);
            Assert.That(office.LastIssuedCommand.destinationZone, Is.EqualTo(water));
        }

        [UnityTest] public IEnumerator AwayAndFiredWorkersCannotBePickedUp()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            WorkerAgent worker = office.Workers[0];
            Vector2 press = new Vector2(300f,300f);
            Assert.True(office.CarryController.BeginPointerGesture(worker, press, false));
            Assert.False(office.CarryController.EvaluateCarryStart(press + Vector2.right * 7f, .01f, worker.transform.position));
            Assert.That(office.CarryController.LastRejectionReason, Does.Contain("away"));

            yield return WaitForPickable(worker);
            Assert.True(office.TryFire(worker, out string reason), reason);
            Assert.True(office.CarryController.BeginPointerGesture(worker, press, false));
            Assert.False(office.CarryController.EvaluateCarryStart(press + Vector2.right * 7f, .01f, worker.transform.position));
            Assert.That(office.CarryController.LastRejectionReason, Does.Contain("leaving the company"));
            Assert.False(worker.IsPlayerCarried);
        }

        [UnityTest] public IEnumerator RestartAndSceneChangeClearCarryWithoutElevatedWorkers()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            yield return WaitForPickable(office.Workers[0]);
            StartCarry(office.CarryController, office.Workers[0]);
            Vector3 ground = new Vector3(-.4f, 0f, .35f);
            office.CarryController.UpdateCarriedPosition(ground, null, new Vector2(620f, 410f), true);
            office.CarryController.ReleaseAtGround(ground);
            yield return new WaitForSeconds(.18f);
            Assert.NotNull(office.LastGroundPlacementCommand);
            yield return WaitForPickable(office.Workers[0]);
            StartCarry(office.CarryController, office.Workers[0]);
            office.Restart();
            yield return null;
            yield return null;
            OfficeDirector restarted = Object.FindFirstObjectByType<OfficeDirector>();
            Assert.NotNull(restarted);
            Assert.That(restarted.CarryController.Phase, Is.EqualTo(WorkerCarryPhase.Idle));
            Assert.IsNull(restarted.LastGroundPlacementCommand);
            foreach (WorkerAgent worker in restarted.Workers)
                Assert.That(worker.transform.position.y, Is.LessThan(.1f));

            yield return WaitForPickable(restarted.Workers[0]);
            StartCarry(restarted.CarryController, restarted.Workers[0]);
            restarted.ReturnToMenu();
            yield return null;
            Assert.IsNull(Object.FindFirstObjectByType<WorkerCarryController>());
            Assert.IsNull(WorkerSelection.Selected);
        }

        [UnityTest] public IEnumerator FocusedWorkRefreshesWithoutStackingAndCashPauses()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            WorkerAgent worker = office.Workers[0];
            yield return PlaceWorker(office, worker, worker.Desk);
            yield return WaitForState(worker, WorkerState.Work);
            Assert.That(worker.Runtime.focusedWorkSecondsRemaining, Is.InRange(29f, ActivityRules.FocusedWorkDuration));
            Assert.That(ProductivityModel.FocusedWorkModifier(worker.Runtime.focusedWorkSecondsRemaining), Is.EqualTo(1.2f));
            Assert.That(worker.Visuals.CurrentEmote, Is.EqualTo("FOCUS"));

            yield return PlaceWorker(office, worker, worker.Desk);
            yield return WaitForState(worker, WorkerState.Work);
            Assert.That(worker.Runtime.focusedWorkSecondsRemaining, Is.InRange(29f, ActivityRules.FocusedWorkDuration));

            float cash = office.Cash.CurrentCash;
            SimulationSpeedController.Instance.SetSpeed(0f);
            yield return new WaitForSecondsRealtime(.35f);
            Assert.That(office.Cash.CurrentCash, Is.EqualTo(cash).Within(.0001f));
            SimulationSpeedController.Instance.SetSpeed(1f);
            yield return new WaitForSeconds(.35f);
            Assert.That(office.Cash.CurrentCash, Is.GreaterThan(cash));
            Assert.That(office.Cash.LifetimeEarned, Is.GreaterThan(0f));
        }

        [UnityTest] public IEnumerator RestAppliesExactNeedsAfterTwentySeconds()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            WorkerAgent worker = office.Workers[0];
            yield return PlaceWorker(office, worker, FindZone(office, "starter.rest.break-nook"));
            yield return WaitForState(worker, WorkerState.TakeBreak);
            worker.Runtime.energy = .40f;
            worker.Runtime.mood = .40f;
            worker.Runtime.inspiration = .40f;
            worker.Runtime.stress = .60f;
            Assert.That(worker.ActivitySecondsRemaining, Is.GreaterThan(19f));
            SimulationSpeedController.Instance.SetSpeed(4f);
            yield return WaitForState(worker, WorkerState.ReturnToDesk, 7f);
            Assert.That(worker.Runtime.energy, Is.EqualTo(.718f).Within(.004f));
            Assert.That(worker.Runtime.mood, Is.EqualTo(.538f).Within(.004f));
            Assert.That(worker.Runtime.inspiration, Is.EqualTo(.518f).Within(.004f));
            Assert.That(worker.Runtime.stress, Is.EqualTo(.38f).Within(.002f));
        }

        [UnityTest] public IEnumerator WaterAppliesExactEffectsCooldownAndSocialOpportunity()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            WorkerAgent worker = office.Workers[0];
            WorkerAgent partner = office.Workers[1];
            PlacementZone water = FindZone(office, "starter.water.cooler");
            yield return WaitForPickable(partner);
            Assert.True(partner.BeginPlayerCarry(out _));
            partner.SetPlayerCarryPosition(water.PlacementPoint.position + Vector3.right);
            yield return PlaceWorker(office, worker, water);
            yield return WaitForState(worker, WorkerState.UseWaterCooler);
            worker.Runtime.energy = .40f;
            worker.Runtime.mood = .40f;
            worker.Runtime.bathroom = .15f;
            worker.Runtime.stress = .60f;
            Assert.True(worker.HadWaterSocialOpportunity);
            SimulationSpeedController.Instance.SetSpeed(4f);
            yield return WaitForState(worker, WorkerState.ReturnToDesk, 4f);
            Assert.That(worker.Runtime.energy, Is.EqualTo(.459f).Within(.003f));
            Assert.That(worker.Runtime.mood, Is.EqualTo(.440f).Within(.003f));
            Assert.That(worker.Runtime.bathroom, Is.EqualTo(.232f).Within(.004f));
            Assert.That(worker.Runtime.stress, Is.EqualTo(.56f).Within(.002f));
            Assert.That(worker.Runtime.waterCooldown, Is.InRange(34f, ActivityRules.WaterCooldown));
            partner.CancelPlayerCarryImmediate();
        }

        [UnityTest] public IEnumerator VendingChargesOnceAndAppliesNormalResult()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            WorkerAgent worker = office.Workers[0];
            yield return WaitForPickable(office.Workers[1]);
            yield return WaitForPickable(office.Workers[2]);
            Assert.True(office.Workers[1].BeginPlayerCarry(out _));
            Assert.True(office.Workers[2].BeginPlayerCarry(out _));
            office.Cash.AccrueDeskIncome(1f, 15f);
            worker.QueueVendingOutcome(false);
            float cashBefore = office.Cash.CurrentCash;
            yield return PlaceWorker(office, worker, FindZone(office, "starter.snack.vending"));
            yield return WaitForState(worker, WorkerState.BuySnack);
            Assert.That(office.Cash.CurrentCash, Is.EqualTo(cashBefore - 15f).Within(.02f));
            Assert.That(worker.VendingCharges, Is.EqualTo(1));
            worker.Runtime.energy = .40f;
            worker.Runtime.mood = .40f;
            worker.Runtime.stress = .60f;
            yield return null;
            Assert.That(worker.VendingCharges, Is.EqualTo(1));
            Assert.That(office.Cash.CurrentCash, Is.EqualTo(cashBefore - 15f).Within(.02f));
            SimulationSpeedController.Instance.SetSpeed(4f);
            yield return WaitForState(worker, WorkerState.ReturnToDesk, 5f);
            Assert.False(worker.LastVendingMalfunction);
            Assert.That(worker.Runtime.energy, Is.EqualTo(.459f).Within(.004f));
            Assert.That(worker.Runtime.mood, Is.EqualTo(.479f).Within(.004f));
            Assert.That(worker.Runtime.hunger, Is.EqualTo(0f).Within(.002f));
            Assert.That(worker.Runtime.stress, Is.EqualTo(.55f).Within(.002f));
            Assert.That(worker.Runtime.vendingCooldown, Is.InRange(44f, ActivityRules.VendingCooldown));
            office.Workers[1].CancelPlayerCarryImmediate();
            office.Workers[2].CancelPlayerCarryImmediate();
        }

        [UnityTest] public IEnumerator SeededVendingMalfunctionRetainsChargeAndShowsFrustration()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            WorkerAgent worker = office.Workers[0];
            yield return WaitForPickable(office.Workers[1]);
            yield return WaitForPickable(office.Workers[2]);
            Assert.True(office.Workers[1].BeginPlayerCarry(out _));
            Assert.True(office.Workers[2].BeginPlayerCarry(out _));
            office.Cash.AccrueDeskIncome(1f, 15f);
            worker.QueueVendingOutcome(true);
            float cashBefore = office.Cash.CurrentCash;
            yield return PlaceWorker(office, worker, FindZone(office, "starter.snack.vending"));
            yield return WaitForState(worker, WorkerState.BuySnack);
            worker.Runtime.energy = .40f;
            worker.Runtime.mood = .40f;
            worker.Runtime.stress = .60f;
            Assert.True(worker.LastVendingMalfunction);
            Assert.That(worker.Visuals.CurrentEmote, Is.EqualTo("!"));
            Assert.That(office.Cash.CurrentCash, Is.LessThan(cashBefore - 14.8f));
            SimulationSpeedController.Instance.SetSpeed(4f);
            yield return WaitForState(worker, WorkerState.ReturnToDesk, 5f);
            Assert.That(worker.Runtime.energy, Is.EqualTo(.409f).Within(.004f));
            Assert.That(worker.Runtime.mood, Is.EqualTo(.359f).Within(.004f));
            Assert.That(worker.Runtime.stress, Is.EqualTo(.60f).Within(.002f));
            Assert.That(worker.VendingCharges, Is.EqualTo(1));
            office.Workers[1].CancelPlayerCarryImmediate();
            office.Workers[2].CancelPlayerCarryImmediate();
        }

        [UnityTest] public IEnumerator InsufficientCashRejectsVendingBeforeCharge()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            WorkerAgent worker = office.Workers[0];
            yield return WaitForPickable(worker);
            yield return WaitForPickable(office.Workers[1]);
            yield return WaitForPickable(office.Workers[2]);
            Assert.True(office.Workers[1].BeginPlayerCarry(out _));
            Assert.True(office.Workers[2].BeginPlayerCarry(out _));
            float cash = office.Cash.CurrentCash;
            Assert.That(cash, Is.LessThan(15f));
            PlacementZone vending = FindZone(office, "starter.snack.vending");
            StartCarry(office.CarryController, worker);
            office.CarryController.UpdateCarriedPosition(vending.PlacementPoint.position, vending,
                new Vector2(700f,420f), true);
            Assert.False(office.CarryController.HasValidTarget);
            Assert.That(office.CarryController.FeedbackText, Does.Contain("NEED $15 CASH"));
            office.CarryController.ReleaseAtZone(vending);
            yield return new WaitForSeconds(.28f);
            Assert.That(office.Cash.CurrentCash, Is.EqualTo(cash).Within(.0001f));
            Assert.That(worker.VendingCharges, Is.Zero);
            Assert.That(office.CarryController.LastRejectionReason, Does.Contain("$15"));
            office.Workers[1].CancelPlayerCarryImmediate();
            office.Workers[2].CancelPlayerCarryImmediate();
        }

        [UnityTest] public IEnumerator SmokeCreatesAndCleansPropsWithExactEffects()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            WorkerAgent worker = office.Workers[0];
            yield return PlaceWorker(office, worker, FindZone(office, "starter.smoke.exterior"));
            yield return WaitForState(worker, WorkerState.Smoke);
            worker.Runtime.energy = .40f;
            worker.Runtime.mood = .40f;
            worker.Runtime.stress = .60f;
            Assert.True(worker.HasSmokingProp);
            Assert.True(worker.HasSmokeParticles);
            Assert.That(worker.ActivitySecondsRemaining, Is.GreaterThan(11f));
            SimulationSpeedController.Instance.SetSpeed(4f);
            yield return WaitForState(worker, WorkerState.ReturnToDesk, 6f);
            Assert.False(worker.HasSmokingProp);
            Assert.False(worker.HasSmokeParticles);
            Assert.That(worker.Runtime.energy, Is.EqualTo(.399f).Within(.003f));
            Assert.That(worker.Runtime.mood, Is.EqualTo(.469f).Within(.003f));
            Assert.That(worker.Runtime.stress, Is.EqualTo(.30f).Within(.002f));
            Assert.That(worker.Runtime.smokingCooldown, Is.InRange(44f, ActivityRules.SmokingCooldown));
        }

        [UnityTest] public IEnumerator InterruptedSmokeCleansEffectsAndLeavesWorkerValid()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            WorkerAgent worker = office.Workers[0];
            yield return PlaceWorker(office, worker, FindZone(office, "starter.smoke.exterior"));
            yield return WaitForState(worker, WorkerState.Smoke);
            Assert.True(worker.HasSmokingProp);
            yield return PlaceWorker(office, worker, FindZone(office, "starter.rest.break-nook"));
            Assert.False(worker.HasSmokingProp);
            Assert.False(worker.HasSmokeParticles);
            Assert.That(worker.Runtime.smokingCooldown, Is.InRange(44f, ActivityRules.SmokingCooldown));
            Assert.That(worker.Runtime.behavior, Is.EqualTo(WorkerState.TakeBreak));
            Assert.False(worker.IsFired);
            Assert.False(worker.IsPlayerCarried);
        }

        [UnityTest] public IEnumerator LeaveOfficeCompletesAwayRecoveryAndReturnLifecycle()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            WorkerAgent worker = office.Workers[0];
            yield return WaitForPickable(office.Workers[1]);
            yield return WaitForPickable(office.Workers[2]);
            Assert.True(office.Workers[1].BeginPlayerCarry(out _));
            Assert.True(office.Workers[2].BeginPlayerCarry(out _));
            yield return PlaceWorker(office, worker, FindZone(office, "starter.exit.main"));
            yield return WaitForState(worker, WorkerState.WalkOutForAway);
            Assert.True(worker.IsVisibleInOffice);
            worker.transform.position = office.ExitOutsidePoint;
            yield return null;
            yield return WaitForState(worker, WorkerState.Away);
            worker.Runtime.energy = .20f;
            worker.Runtime.mood = .30f;
            worker.Runtime.hunger = .80f;
            worker.Runtime.bathroom = .80f;
            worker.Runtime.inspiration = .30f;
            worker.Runtime.stress = .80f;
            Assert.False(worker.IsVisibleInOffice);
            Assert.That(worker.Runtime.awaySecondsRemaining, Is.InRange(29f, ActivityRules.AwayDuration));
            Assert.That(new[] { "Lunch", "Errand", "Long break", "Off-site task" }, Does.Contain(worker.AwayReasonLabel));
            float cash = office.Cash.CurrentCash;
            SimulationSpeedController.Instance.SetSpeed(4f);
            yield return WaitForState(worker, WorkerState.ReturnFromAway, 10f);
            Assert.True(worker.IsVisibleInOffice);
            Assert.That(worker.Runtime.energy, Is.EqualTo(.58f).Within(.006f));
            Assert.That(worker.Runtime.mood, Is.EqualTo(.45f).Within(.006f));
            Assert.That(worker.Runtime.hunger, Is.EqualTo(.45f).Within(.006f));
            Assert.That(worker.Runtime.bathroom, Is.EqualTo(.40f).Within(.006f));
            Assert.That(worker.Runtime.inspiration, Is.EqualTo(.46f).Within(.006f));
            Assert.That(worker.Runtime.stress, Is.EqualTo(.45f).Within(.006f));
            Assert.That(office.Cash.CurrentCash, Is.EqualTo(cash).Within(.01f));
            worker.transform.position = office.EntranceInsidePoint;
            yield return null;
            yield return WaitForState(worker, WorkerState.ReturnToDesk);
            office.Workers[1].CancelPlayerCarryImmediate();
            office.Workers[2].CancelPlayerCarryImmediate();
        }

        [UnityTest] public IEnumerator FiringDuringAwaySafelyResolvesHiddenState()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            WorkerAgent worker = office.Workers[0];
            yield return PlaceWorker(office, worker, FindZone(office, "starter.exit.main"));
            yield return WaitForState(worker, WorkerState.WalkOutForAway);
            worker.transform.position = office.ExitOutsidePoint;
            yield return null;
            yield return WaitForState(worker, WorkerState.Away);
            Assert.False(worker.IsVisibleInOffice);
            Assert.True(office.TryFire(worker, out string reason), reason);
            Assert.True(worker.IsVisibleInOffice);
            Assert.True(worker.IsFired);
            Assert.That(worker.Runtime.awaySecondsRemaining, Is.Zero);
            Assert.False(worker.HasSmokingProp);
            Assert.False(worker.HasSmokeParticles);
        }

        [UnityTest] public IEnumerator ExpandedStarterInitializesWithAdditionalSpace()
        {
            yield return LoadOffice(OfficeStage.StarterOfficeExpanded);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            Assert.That(office.Stage, Is.EqualTo(OfficeStage.StarterOfficeExpanded));
            Assert.That(office.Workers.Count, Is.EqualTo(3));
            Assert.That(office.Workstations.Count, Is.EqualTo(7));
            Assert.That(office.WorkerCapacity, Is.EqualTo(6));
            Assert.False(office.Workday.IsTimed);
            Assert.True(office.ExpansionComplete);
            Assert.True(office.Expansion.ConnectingWallOpen);
            Assert.True(office.Expansion.DoorwayTrimVisible);
        }

        [UnityTest] public IEnumerator ExpansionAffordabilityTracksCashAndVendingSpendCanDisableIt()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            SimulationSpeedController.Instance.SetSpeed(0f);
            Assert.False(office.CanPurchaseExpansion);
            Assert.False(office.TryPurchaseExpansion(out _));
            office.Cash.AccrueDeskIncome(16.666667f, 60f);
            Assert.True(office.CanPurchaseExpansion);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(office.Audio.LastCue, Is.EqualTo("cash-earned"));
            Assert.True(office.HUD.PurchaseButtonInteractable);
            Assert.That(office.HUD.GoalText, Does.Contain("OBJECTIVE: Earn $1,000 and purchase the neighboring unit."));
            Assert.That(office.HUD.GoalText, Does.Contain("NEXT DOOR  $1,000"));
            Assert.That(office.HUD.HeaderText, Does.Contain("INCOME"));
            Assert.That(office.HUD.GoalText, Does.Contain("The neighboring unit is available."));
            Button purchase = GameObject.Find("Purchase Next Door").GetComponent<Button>();
            purchase.onClick.Invoke();
            Assert.True(office.HUD.PurchasePanelVisible);
            Component unlocks = GameObject.Find("Unlocks").GetComponent("TextMeshProUGUI");
            string unlockCopy = (string)unlocks.GetType().GetProperty("text").GetValue(unlocks);
            Assert.That(unlockCopy, Does.Contain("Adjacent floor space"));
            Assert.That(unlockCopy, Does.Contain("Connecting wall removal and open doorway"));
            Assert.That(unlockCopy, Does.Contain("Three additional desk locations"));
            Assert.That(unlockCopy, Does.Contain("Capacity for three additional workers"));
            Assert.That(unlockCopy, Does.Contain("Access to the Established Office preview"));
            Assert.True(office.Cash.TrySpend(ActivityRules.SnackCost));
            Assert.False(office.CanPurchaseExpansion);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.False(office.HUD.PurchaseButtonInteractable);
        }

        [UnityTest] public IEnumerator PurchaseDeductsExactlyOnceAndPhysicallyUnlocksNeighbor()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            SimulationSpeedController.Instance.SetSpeed(0f);
            float oldPanWidth = office.Layout.PanBounds.size.x;
            office.Cash.AccrueDeskIncome(16.666667f, 60f);
            float before = office.Cash.CurrentCash;
            yield return new WaitForSecondsRealtime(.2f);
            GameObject.Find("Purchase Next Door").GetComponent<Button>().onClick.Invoke();
            GameObject.Find("Confirm").GetComponent<Button>().onClick.Invoke();
            Assert.That(before - office.Cash.CurrentCash, Is.EqualTo(ExpansionRules.PurchasePrice).Within(.001f));
            Assert.False(office.TryPurchaseExpansion(out _));
            Assert.That(office.Expansion.PurchaseCount, Is.EqualTo(1));
            yield return new WaitForSecondsRealtime(1.5f);

            Assert.True(office.ExpansionComplete);
            Assert.That(office.Audio.LastCue, Is.EqualTo("wall-open"));
            Assert.That(office.Stage, Is.EqualTo(OfficeStage.StarterOfficeExpanded));
            Assert.True(office.Expansion.ConnectingWallOpen);
            Assert.True(office.Expansion.DoorwayTrimVisible);
            Assert.True(office.Expansion.NavigationEnabled);
            Assert.That(office.WorkerCapacity, Is.EqualTo(6));
            Assert.That(FindZone(office, "neighbor.rest.utility").IsZoneEnabled, Is.True);
            Assert.That(office.Layout.PanBounds.size.x, Is.GreaterThan(oldPanWidth));
            Assert.That(Camera.main.GetComponent<OfficeCameraRig>().OverviewCenter, Is.EqualTo(office.Layout.OverviewCenter));
            Assert.True(office.HUD.MilestonePanelVisible);
            Assert.False(office.HUD.PreviewButtonVisible);
            Assert.True(office.HUD.CloseTopModal());
            yield return null;
            Assert.True(office.HUD.PreviewButtonVisible);

            Vector3 unlockedGround = new Vector3(10f, 0f, 0f);
            Assert.True(office.Layout.CanPlaceWorkerAt(unlockedGround, out string groundReason), groundReason);
            SimulationSpeedController.Instance.SetSpeed(1f);
            WorkerAgent worker = office.Workers[0];
            yield return WaitForPickable(worker);
            StartCarry(office.CarryController, worker);
            office.CarryController.UpdateCarriedPosition(unlockedGround, null, new Vector2(1060f, 430f), true);
            Assert.True(office.CarryController.HasValidTarget, office.CarryController.FeedbackText);
            office.CarryController.ReleaseAtGround(unlockedGround);
            yield return new WaitForSeconds(.18f);
            Assert.That(office.LastGroundPlacementCommand.groundPoint.x, Is.EqualTo(10f).Within(.01f));
        }

        [UnityTest] public IEnumerator ExpandedHiringCreatesUnassignedEntranceWorkerWhoCanBePlaced()
        {
            yield return LoadOffice(OfficeStage.StarterOfficeExpanded);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            office.Cash.AccrueDeskIncome(7f, 60f);
            float cashBefore = office.Cash.CurrentCash;
            string candidateName = office.Candidates[0].worker.displayName;
            Assert.True(office.TryHire(0, out string reason), reason);
            Assert.That(cashBefore - office.Cash.CurrentCash, Is.EqualTo(380f).Within(.001f));
            Assert.That(office.ActiveWorkerCount, Is.EqualTo(4));
            WorkerAgent hire = office.Workers[office.Workers.Count - 1];
            Assert.That(hire.Definition.displayName, Is.EqualTo(candidateName));
            Assert.IsNull(hire.Desk);
            yield return WaitForState(hire, WorkerState.Unassigned, 3f);

            Workstation neighborDesk = FindZone(office, "neighbor.work.01") as Workstation;
            yield return PlaceWorker(office, hire, neighborDesk);
            Assert.That(hire.Desk, Is.EqualTo(neighborDesk));
            Assert.That(neighborDesk.Assigned, Is.EqualTo(hire));
            SimulationSpeedController.Instance.SetSpeed(4f);
            yield return WaitForState(hire, WorkerState.Work, 3f);
            Assert.False(hire.IsPhoneWorking);
            Assert.That(hire.CurrentActivityLabel, Is.EqualTo("Work"));
            float earnedAtPlacement = office.Cash.LifetimeEarned;
            yield return new WaitForSeconds(2f);
            Assert.False(office.Workday.IsEnded);
            Assert.That(office.ActiveWorkerCount, Is.EqualTo(4));
            Assert.That(office.Cash.LifetimeEarned, Is.GreaterThan(earnedAtPlacement));
        }

        [UnityTest] public IEnumerator StarterHiringAllowsDesklessPhoneWorkBeforeExpansion()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            office.Cash.AccrueDeskIncome(7f, 60f);
            Assert.True(office.TryHire(0, out string reason), reason);
            Assert.That(office.ActiveWorkerCount, Is.EqualTo(4));
            Assert.That(office.ActiveWorkerCount, Is.GreaterThan(office.WorkerCapacity));
            WorkerAgent hire = office.Workers[office.Workers.Count - 1];
            Assert.IsNull(hire.Desk);
            SimulationSpeedController.Instance.SetSpeed(4f);
            yield return WaitForState(hire, WorkerState.Unassigned, 2f);
            yield return null;
            Assert.That(office.HUD.HeaderText, Does.Contain("TEAM  4"));
            Assert.That(office.HUD.HeaderText, Does.Contain("DESKS  3"));
            Assert.True(hire.IsPhoneWorking);
            Assert.That(hire.CurrentActivityLabel, Is.EqualTo("Working from phone"));
            Assert.That(hire.CurrentDestinationLabel, Is.EqualTo("Phone work"));
            float before = office.Cash.CurrentCash;
            yield return new WaitForSeconds(.5f);
            Assert.That(hire.Productivity, Is.GreaterThan(0f));
            Assert.That(hire.Runtime.positiveInfluence, Does.Contain("Phone work"));
            Assert.That(office.Cash.CurrentCash, Is.GreaterThan(before));
            float summedProductivity = 0f;
            foreach (WorkerAgent worker in office.Workers) summedProductivity += worker.Productivity;
            Assert.That(office.CombinedIncomePerMinute,
                Is.EqualTo(summedProductivity * CashDirector.IncomePerProductivityMinute).Within(.01f));

            SimulationSpeedController.Instance.SetSpeed(0f);
            float pausedCash = office.Cash.CurrentCash;
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(office.Cash.CurrentCash, Is.EqualTo(pausedCash).Within(.0001f));
            SimulationSpeedController.Instance.SetSpeed(4f);
            yield return new WaitForSeconds(.25f);
            Assert.That(office.Cash.CurrentCash, Is.GreaterThan(pausedCash));

            hire.Runtime.energy = .1f;
            yield return WaitForState(hire, WorkerState.TakeBreak, 4f);
            Assert.That(hire.Runtime.autonomyDecisions, Is.GreaterThan(0));
        }

        [UnityTest] public IEnumerator EstablishedPreviewLoadsUntimedAndReturnsToStarterMenu()
        {
            yield return LoadOffice(OfficeStage.StarterOfficeExpanded);
            OfficeDirector starter = Object.FindFirstObjectByType<OfficeDirector>();
            Assert.True(starter.HUD.PreviewButtonVisible);
            starter.VisitEstablishedOfficePreview();
            yield return null;
            yield return null;
            OfficeDirector preview = Object.FindFirstObjectByType<OfficeDirector>();
            Assert.True(preview.IsEstablishedPreview);
            Assert.That(preview.Stage, Is.EqualTo(OfficeStage.EstablishedOffice));
            Assert.False(preview.Workday.IsTimed);
            Assert.NotNull(GameObject.Find("Future Stage Banner"));
            preview.ReturnFromPreviewToMenu();
            yield return null;
            yield return null;
            Assert.NotNull(Object.FindFirstObjectByType<MainMenuController>());
        }

        [UnityTest] public IEnumerator RestartReconstructsLockedAndExpandedStarterStates()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector locked = Object.FindFirstObjectByType<OfficeDirector>();
            locked.Restart();
            yield return null;
            yield return null;
            locked = Object.FindFirstObjectByType<OfficeDirector>();
            Assert.That(locked.Stage, Is.EqualTo(OfficeStage.StarterOffice));
            Assert.That(locked.WorkerCapacity, Is.EqualTo(3));
            Assert.False(locked.Expansion.ConnectingWallOpen);

            locked.Cash.AccrueDeskIncome(16.666667f, 60f);
            Assert.True(locked.TryPurchaseExpansion(out string reason), reason);
            yield return new WaitForSecondsRealtime(1.5f);
            OfficeDirector expanded = locked;
            expanded.Restart();
            yield return null;
            yield return null;
            expanded = Object.FindFirstObjectByType<OfficeDirector>();
            Assert.That(expanded.Stage, Is.EqualTo(OfficeStage.StarterOfficeExpanded));
            Assert.That(expanded.WorkerCapacity, Is.EqualTo(6));
            Assert.True(expanded.Expansion.ConnectingWallOpen);
        }

        [UnityTest] public IEnumerator OfficeSceneLoadsAndWorkersSpawn()
        {
            yield return LoadOffice();
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            Assert.That(office.Stage, Is.EqualTo(OfficeStage.EstablishedOffice));
            Assert.That(office.Workers.Count, Is.EqualTo(6));
            Assert.That(office.Workstations.Count, Is.EqualTo(12));
            Assert.NotNull(office.Coffee);
            Assert.NotNull(office.Water);
        }

        [UnityTest] public IEnumerator WorkersReachDesksAndWork()
        {
            yield return LoadOffice();
            SimulationSpeedController.Instance.SetSpeed(4f);
            yield return new WaitForSeconds(8f);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            Assert.That(office.Workers[0].Desk, Is.Not.Null);
            Assert.That(office.Workers[0].Runtime.workSeconds, Is.GreaterThan(0f));
        }

        [UnityTest] public IEnumerator LowEnergyWorkerSeeksCoffee()
        {
            yield return LoadOffice();
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            office.Workers[0].Runtime.energy = .1f;
            SimulationSpeedController.Instance.SetSpeed(4f);
            bool observed = false;
            for (int i = 0; i < 20; i++)
            {
                yield return new WaitForSeconds(.5f);
                WorkerState state = office.Workers[0].Runtime.behavior;
                if (state == WorkerState.SeekCoffee || state == WorkerState.UseCoffeeMachine) { observed = true; break; }
            }
            Assert.True(observed);
        }

        [UnityTest] public IEnumerator SocialWorkersEventuallySocializeAndReturn()
        {
            yield return LoadOffice();
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            office.Workers[1].Runtime.socialNeed = 1f;
            SimulationSpeedController.Instance.SetSpeed(4f);
            bool social = false;
            for (int i = 0; i < 24; i++)
            {
                yield return new WaitForSeconds(.5f);
                if (office.Workers[1].Runtime.behavior == WorkerState.Socialize || office.Workers[1].Runtime.behavior == WorkerState.SeekCoworker) { social = true; break; }
            }
            Assert.True(social);
            yield return new WaitForSeconds(12f);
            Assert.That(office.Workers[1].Runtime.behavior, Is.Not.EqualTo(WorkerState.Socialize));
        }

        [UnityTest] public IEnumerator HireCandidateChangesRosterAndCandidate()
        {
            yield return LoadOffice();
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            string name = office.Candidates[0].worker.displayName;
            Assert.True(office.TryHire(0, out string reason), reason);
            Assert.That(office.ActiveWorkerCount, Is.EqualTo(7));
            Assert.That(office.Candidates[0].worker.displayName, Is.Not.EqualTo(name));
        }

        [UnityTest] public IEnumerator FireWorkerStartsBoxExitAndReducesPayroll()
        {
            yield return LoadOffice();
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            WorkerAgent worker = office.Workers[5];
            int payroll = office.Economy.Payroll;
            Assert.True(office.TryFire(worker, out string reason), reason);
            Assert.True(worker.IsFired);
            Assert.That(office.Economy.Payroll, Is.LessThan(payroll));
            SimulationSpeedController.Instance.SetSpeed(4f);
            yield return new WaitForSeconds(26f);
            Assert.That(office.ActiveWorkerCount, Is.EqualTo(5));
        }

        [UnityTest] public IEnumerator ReassignDeskChangesWorkerDesk()
        {
            yield return LoadOffice();
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            WorkerAgent worker = office.Workers[0];
            Workstation destination = office.Workstations[8];
            WorkerSelection.Select(worker);
            office.BeginReassign();
            Assert.True(office.ReassignSelected(destination));
            Assert.That(worker.Desk, Is.EqualTo(destination));
        }

        [UnityTest] public IEnumerator TaskCompletionIncreasesRevenue()
        {
            yield return LoadOffice();
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            office.Tasks.Contribute(office.Tasks.Current.definition.workRequired + 1f);
            Assert.That(office.Economy.Revenue, Is.GreaterThan(0));
            Assert.That(office.Tasks.CompletedCount, Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator EstablishedOfficeIsUntimedAndHasNoDailyResultSurface()
        {
            yield return LoadOffice();
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            Assert.False(office.Workday.IsTimed);
            office.Workday.Finish();
            yield return null;
            GameObject report = GameObject.Find("Report");
            Assert.IsNull(report);
            Assert.That(office.HUD.GoalText, Does.Not.Contain("TARGET"));
            Assert.That(office.HUD.GoalText, Does.Not.Contain("REPORT"));
        }

        [UnityTest] public IEnumerator TutorialCompletesFromObservedSelectionCarryPlacementIncomeNeedsAndRedirect()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            TutorialController tutorial = office.Tutorial;
            Assert.NotNull(tutorial);
            Assert.That(tutorial.CurrentStep, Is.EqualTo(TutorialStep.NotStarted));
            SimulationSpeedController.Instance.SetSpeed(4f);

            tutorial.StartTutorial(true);
            Assert.That(tutorial.CurrentStep, Is.EqualTo(TutorialStep.MeetTheTeam));
            Assert.True(tutorial.IsReading);
            Assert.That(SimulationSpeedController.Instance.Speed, Is.Zero);
            tutorial.ContinueFromReading();

            WorkerAgent morgan = office.Workers[0];
            WorkerSelection.Select(morgan);
            Assert.That(tutorial.CurrentStep, Is.EqualTo(TutorialStep.PickThemUp));
            Assert.That(tutorial.HighlightedWorker, Is.EqualTo(morgan));
            tutorial.ContinueFromReading();
            yield return WaitForPickable(morgan);
            StartCarry(office.CarryController, morgan);
            Assert.That(tutorial.CurrentStep, Is.EqualTo(TutorialStep.PutThemToWork));
            Assert.True(office.CarryController.PlacementLegendVisible);
            Assert.False(office.HUD.InspectorVisible);
            Rect tutorialAnchors = new Rect(tutorial.TutorialPanelRect.anchorMin,
                tutorial.TutorialPanelRect.anchorMax - tutorial.TutorialPanelRect.anchorMin);
            Assert.False(tutorialAnchors.Contains(Camera.main.WorldToViewportPoint(morgan.transform.position + Vector3.up)));
            Assert.False(tutorialAnchors.Contains(Camera.main.WorldToViewportPoint(tutorial.HighlightedZone.PlacementPoint.position)));
            office.CarryController.UpdateCarriedPosition(morgan.Desk.PlacementPoint.position, morgan.Desk,
                new Vector2(Screen.width * .5f, Screen.height * .5f), true);
            office.CarryController.ReleaseAtZone(morgan.Desk);
            yield return WaitForTutorialStep(tutorial, TutorialStep.ManageTheirNeeds);
            Assert.That(office.Cash.LifetimeEarned, Is.GreaterThan(0f));
            Assert.That(morgan.Runtime.focusedWorkSecondsRemaining, Is.GreaterThan(0f));

            tutorial.ContinueFromReading();
            WorkerAgent alex = office.Workers[1];
            yield return PlaceWorker(office, alex, FindZone(office, "starter.water.cooler"));
            yield return WaitForTutorialStep(tutorial, TutorialStep.RedirectADistraction);
            tutorial.ContinueFromReading();
            WorkerAgent distracted = tutorial.HighlightedWorker;
            Assert.NotNull(distracted);
            Assert.That(distracted.Definition.displayName, Is.EqualTo("Sam"));
            Assert.That(distracted.Runtime.behavior, Is.EqualTo(WorkerState.LookAtPhone));
            yield return PlaceWorker(office, distracted, distracted.Desk);
            yield return WaitForTutorialStep(tutorial, TutorialStep.TryTheOffice);
            Assert.That(tutorial.CurrentBody, Does.Contain("EXIT sends a worker away temporarily"));

            tutorial.ContinueFromReading();
            Assert.That(tutorial.CurrentStep, Is.EqualTo(TutorialStep.Expand));
            Assert.That(tutorial.CurrentBody, Does.Contain("never spends automatically"));
            tutorial.ContinueFromReading();
            Assert.True(tutorial.WasCompleted);
            Assert.False(office.WorldInputBlocked);
            Assert.That(SimulationSpeedController.Instance.Speed, Is.EqualTo(4f));
        }

        [UnityTest] public IEnumerator TutorialSkipHelpAndReplayRestoreTheExactPriorSpeed()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            TutorialController tutorial = Object.FindFirstObjectByType<OfficeDirector>().Tutorial;
            SimulationSpeedController.Instance.SetSpeed(4f);
            tutorial.StartTutorial(true);
            Assert.That(SimulationSpeedController.Instance.Speed, Is.Zero);
            tutorial.SkipTutorial();
            Assert.True(tutorial.WasSkipped);
            Assert.That(SimulationSpeedController.Instance.Speed, Is.EqualTo(4f));

            tutorial.OpenHelp();
            Assert.True(tutorial.HelpOpen);
            Assert.That(SimulationSpeedController.Instance.Speed, Is.Zero);
            Assert.True(tutorial.CloseTopPanel());
            Assert.False(tutorial.HelpOpen);
            Assert.That(SimulationSpeedController.Instance.Speed, Is.EqualTo(4f));

            tutorial.OpenHelp();
            tutorial.ReplayTutorial();
            Assert.That(tutorial.ReplayCount, Is.EqualTo(1));
            Assert.That(tutorial.CurrentStep, Is.EqualTo(TutorialStep.MeetTheTeam));
            Assert.True(tutorial.IsReading);
            Assert.False(tutorial.HelpOpen);
            tutorial.SkipTutorial();
            Assert.That(SimulationSpeedController.Instance.Speed, Is.EqualTo(4f));
        }

        [UnityTest] public IEnumerator TutorialCreditsUsefulActionsTakenBeforeItStarts()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            TutorialController tutorial = office.Tutorial;
            SimulationSpeedController.Instance.SetSpeed(4f);
            WorkerAgent morgan = office.Workers[0];
            WorkerSelection.Select(morgan);
            yield return PlaceWorker(office, morgan, morgan.Desk);
            float deadline = Time.realtimeSinceStartup + 3f;
            while (office.Cash.LifetimeEarned <= 0f && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(office.Cash.LifetimeEarned, Is.GreaterThan(0f));
            yield return PlaceWorker(office, office.Workers[1], FindZone(office, "starter.water.cooler"));

            tutorial.StartTutorial();
            tutorial.ContinueFromReading();
            Assert.That(tutorial.CurrentStep, Is.EqualTo(TutorialStep.PickThemUp));
            tutorial.ContinueFromReading();
            Assert.That(tutorial.CurrentStep, Is.EqualTo(TutorialStep.PutThemToWork));
            tutorial.ContinueFromReading();
            Assert.That(tutorial.CurrentStep, Is.EqualTo(TutorialStep.ManageTheirNeeds));
            tutorial.ContinueFromReading();
            Assert.That(tutorial.CurrentStep, Is.EqualTo(TutorialStep.RedirectADistraction));
            tutorial.SkipTutorial();
        }

        [UnityTest] public IEnumerator ReloadingStarterOfficeResetsFirstRunTutorialState()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            TutorialController first = Object.FindFirstObjectByType<OfficeDirector>().Tutorial;
            first.StartTutorial(true);
            first.ContinueFromReading();
            WorkerSelection.Select(Object.FindFirstObjectByType<OfficeDirector>().Workers[0]);
            Assert.That(first.CurrentStep, Is.EqualTo(TutorialStep.PickThemUp));
            first.ReplayTutorial();
            Assert.That(first.ReplayCount, Is.EqualTo(1));

            yield return LoadOffice(OfficeStage.StarterOffice);
            TutorialController restarted = Object.FindFirstObjectByType<OfficeDirector>().Tutorial;
            Assert.That(restarted.CurrentStep, Is.EqualTo(TutorialStep.NotStarted));
            Assert.That(restarted.ReplayCount, Is.Zero);
            Assert.False(restarted.HelpOpen);
            Assert.False(restarted.WasSkipped);
        }

        [UnityTest] public IEnumerator TutorialReplacesAHighlightedWorkerWhoBecomesInvalid()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            TutorialController tutorial = office.Tutorial;
            tutorial.StartTutorial(true);
            tutorial.ContinueFromReading();
            WorkerAgent original = office.Workers[0];
            WorkerSelection.Select(original);
            Assert.That(tutorial.HighlightedWorker, Is.EqualTo(original));
            Assert.True(office.TryFire(original, out string reason), reason);
            yield return null;
            Assert.NotNull(tutorial.HighlightedWorker);
            Assert.That(tutorial.HighlightedWorker, Is.Not.EqualTo(original));
            Assert.False(tutorial.HighlightedWorker.IsFired);
            Assert.True(tutorial.HighlightedWorker.Visuals != null);
            tutorial.SkipTutorial();
        }

        [UnityTest] public IEnumerator TutorialAndHelpOwnTheTopModalWithoutOverlaps()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            TutorialController tutorial = office.Tutorial;
            tutorial.StartTutorial(true);
            Assert.True(office.WorldInputBlocked);
            Assert.False(office.CarryController.BeginPointerGesture(office.Workers[0], new Vector2(100f,100f), false));

            tutorial.OpenHelp();
            Assert.True(tutorial.HelpOpen);
            Assert.False(tutorial.TutorialPanelRect.gameObject.activeSelf);
            Assert.False(office.HUD.ObjectiveVisible);
            Assert.True(office.HUD.CloseTopModal());
            Assert.False(tutorial.HelpOpen);
            Assert.True(tutorial.TutorialPanelRect.gameObject.activeSelf);
            Assert.True(office.HUD.CloseTopModal());
            Assert.False(office.WorldInputBlocked);
            tutorial.SkipTutorial();

            office.HUD.ShowHiringForCapture();
            Assert.True(office.HUD.HiringPanelVisible);
            tutorial.OpenHelp();
            Assert.False(office.HUD.HiringPanelVisible);
            Assert.True(tutorial.HelpOpen);
            office.HUD.CloseTopModal();
            tutorial.OpenHelp();
            office.HUD.ShowHiringForCapture();
            Assert.False(tutorial.HelpOpen);
            Assert.True(office.HUD.HiringPanelVisible);
            office.HUD.CloseTopModal();

            office.Cash.AccrueDeskIncome(16.666667f, 60f);
            GameObject.Find("Purchase Next Door").GetComponent<Button>().onClick.Invoke();
            Assert.True(office.HUD.PurchasePanelVisible);
            tutorial.OpenHelp();
            Assert.False(office.HUD.PurchasePanelVisible);
            office.HUD.CloseTopModal();

            office.MarkExpansionComplete();
            Assert.True(office.HUD.MilestonePanelVisible);
            Assert.False(office.HUD.ObjectiveVisible);
            Assert.False(office.HUD.PreviewButtonVisible);
            tutorial.OpenHelp();
            Assert.False(office.HUD.MilestonePanelVisible);
            Assert.True(tutorial.HelpOpen);
            office.HUD.CloseTopModal();
            Assert.False(office.HUD.HasModalOpen);
        }

        [UnityTest] public IEnumerator CarryModeShowsTextLegendAndAvailabilityLabelsThenClearsThem()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            WorkerAgent worker = office.Workers[0];
            yield return WaitForPickable(worker);
            Vector2 press = new Vector2(200f,200f);
            Assert.True(office.CarryController.BeginPointerGesture(worker, press, false));
            Assert.True(office.CarryController.EvaluateCarryStart(press + Vector2.right * 7f, .01f,
                new Vector3(100f, worker.transform.position.y, 100f)));
            Assert.That(office.Audio.LastCue, Is.EqualTo("pickup"));
            office.CarryController.UpdateCarriedPosition(worker.Desk.PlacementPoint.position, worker.Desk,
                new Vector2(Screen.width * .5f, Screen.height * .5f), true);
            Assert.That(office.Audio.LastCue, Is.EqualTo("valid-hover"));
            Assert.True(office.CarryController.PlacementLegendVisible);
            bool sawValid = false;
            bool sawUnavailable = false;
            bool sawOccupied = false;
            foreach (PlacementZone zone in office.PlacementZones)
            {
                sawValid |= zone.AvailabilityLabel.Contains("VALID");
                sawUnavailable |= zone.AvailabilityLabel.Contains("UNAVAILABLE");
                sawOccupied |= zone.AvailabilityLabel.Contains("OCCUPIED");
            }
            Assert.True(sawValid, "A valid destination needs a text label.");
            Assert.True(sawUnavailable, "A locked destination needs a text label.");
            Assert.True(sawOccupied, "An occupied destination needs a text label.");
            office.CarryController.CancelCarry(true);
            Assert.False(office.CarryController.PlacementLegendVisible);
            foreach (PlacementZone zone in office.PlacementZones)
                Assert.That(zone.CarryVisualState, Is.EqualTo(PlacementZoneVisualState.None));
        }

        [UnityTest] public IEnumerator WorkerLabelsAndEmotesUseHighContrastOutlines()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            foreach (WorkerAgent worker in office.Workers)
            {
                TextMeshPro name = worker.Visuals.NameTagTransform.GetComponent<TextMeshPro>();
                TextMeshPro emote = worker.Visuals.EmoteTransform.GetComponent<TextMeshPro>();
                Assert.That(name.outlineWidth, Is.GreaterThanOrEqualTo(.20f));
                Assert.That(emote.outlineWidth, Is.GreaterThanOrEqualTo(.20f));
                Assert.That(name.outlineColor.a, Is.GreaterThan(.9f));
                Assert.That(emote.outlineColor.a, Is.GreaterThan(.9f));
            }
        }

        [UnityTest] public IEnumerator UiSmokeAt1280x720()
        {
            Screen.SetResolution(1280, 720, false);
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            yield return WaitForPickable(office.Workers[0]);
            StartCarry(office.CarryController, office.Workers[0]);
            PlacementZone rest = FindZone(office, "starter.rest.break-nook");
            office.CarryController.UpdateCarriedPosition(rest.PlacementPoint.position, rest, new Vector2(1260f,700f), true);
            Assert.NotNull(Object.FindFirstObjectByType<Canvas>());
            Assert.That(Screen.width, Is.GreaterThanOrEqualTo(640));
            Assert.True(office.CarryController.FeedbackVisible);
            Assert.That(office.CarryController.FeedbackScreenPosition.x, Is.InRange(0f,(float)Screen.width));
            office.CarryController.CancelCarry(true);
            office.Tutorial.StartTutorial(true);
            Assert.That(office.Tutorial.ReferenceResolution, Is.EqualTo(new Vector2(1920f,1080f)));
            Assert.That(office.Tutorial.TutorialPanelRect.anchorMin.x, Is.InRange(0f,1f));
            Assert.That(office.Tutorial.TutorialPanelRect.anchorMax.x, Is.InRange(0f,1f));
            office.Tutorial.SkipTutorial();
        }

        [UnityTest] public IEnumerator UiSmokeAt1920x1080()
        {
            Screen.SetResolution(1920, 1080, false);
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            yield return WaitForPickable(office.Workers[0]);
            StartCarry(office.CarryController, office.Workers[0]);
            PlacementZone rest = FindZone(office, "starter.rest.break-nook");
            office.CarryController.UpdateCarriedPosition(rest.PlacementPoint.position, rest, new Vector2(1900f,1060f), true);
            Assert.NotNull(Object.FindFirstObjectByType<Canvas>());
            Assert.That(Screen.height, Is.GreaterThanOrEqualTo(480));
            Assert.True(office.CarryController.FeedbackVisible);
            Assert.That(office.CarryController.FeedbackScreenPosition.y, Is.InRange(0f,(float)Screen.height));
            office.CarryController.CancelCarry(true);
            office.Tutorial.StartTutorial(true);
            Assert.That(office.Tutorial.ReferenceResolution, Is.EqualTo(new Vector2(1920f,1080f)));
            Assert.That(office.Tutorial.TutorialPanelRect.anchorMin.y, Is.InRange(0f,1f));
            Assert.That(office.Tutorial.TutorialPanelRect.anchorMax.y, Is.InRange(0f,1f));
            office.Tutorial.SkipTutorial();
        }

        [UnityTest] public IEnumerator NoWorkerStaysInRecoveryDuringScriptedSession()
        {
            yield return LoadOffice();
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            SimulationSpeedController.Instance.SetSpeed(4f);
            yield return new WaitForSeconds(32f);
            foreach (WorkerAgent worker in office.Workers)
                Assert.That(worker.Runtime.behavior, Is.Not.EqualTo(WorkerState.RecoverFromStuck));
        }

        [UnityTest] public IEnumerator StarterWorkersExposeNamedPersonalityAndReadableNameTags()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            Assert.That(office.Workers[0].Definition.trait, Is.EqualTo(WorkerTrait.Hardworking));
            Assert.That(office.Workers[1].Definition.trait, Is.EqualTo(WorkerTrait.Social));
            Assert.That(office.Workers[2].Definition.trait, Is.EqualTo(WorkerTrait.Lazy));
            foreach (WorkerAgent worker in office.Workers)
            {
                Assert.That(worker.Visuals.NameTagText, Is.EqualTo(worker.Definition.displayName));
                Assert.NotNull(worker.Visuals.NameTagTransform);
                Assert.NotNull(worker.Visuals.EmoteTransform);
                Assert.That(worker.Visuals.EmoteTransform.position.y - worker.Visuals.NameTagTransform.position.y,
                    Is.GreaterThan(.60f));
            }
        }

        [UnityTest] public IEnumerator NameTagsTrackFaceScaleFadeAndToggle()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            WorkerAgent worker = office.Workers[0];
            yield return null;
            Vector3 before = worker.Visuals.NameTagTransform.position;
            worker.transform.position += Vector3.right * 1.5f;
            yield return null;
            Assert.That(worker.Visuals.NameTagTransform.position.x - before.x, Is.EqualTo(1.5f).Within(.08f));
            Assert.That(Quaternion.Angle(worker.Visuals.NameTagTransform.rotation, Camera.main.transform.rotation), Is.LessThan(.5f));

            worker.Visuals.ComputeNameTagPresentation(6f);
            float closeScale = worker.Visuals.NameTagScale;
            float closeAlpha = worker.Visuals.NameTagAlpha;
            worker.Visuals.ComputeNameTagPresentation(14f);
            Assert.That(worker.Visuals.NameTagScale, Is.LessThan(closeScale));
            Assert.That(worker.Visuals.NameTagAlpha, Is.LessThan(closeAlpha));
            office.HUD.ToggleNameTags();
            yield return null;
            Assert.False(office.HUD.NameTagsEnabled);
            Assert.False(worker.Visuals.NameTagTransform.gameObject.activeSelf);
            office.HUD.ToggleNameTags();
            yield return null;
            Assert.True(office.HUD.NameTagsEnabled);
        }

        [UnityTest] public IEnumerator EmotesExpireCleanlyAndUseSafeText()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            WorkerAgent worker = Object.FindFirstObjectByType<OfficeDirector>().Workers[0];
            yield return WaitForPickable(worker);
            Assert.True(worker.BeginPlayerCarry(out _));
            worker.Visuals.ShowEmote(StatusEmote.Happy, .10f);
            yield return null;
            Assert.That(worker.Visuals.CurrentEmote, Is.EqualTo("HAPPY"));
            Assert.True(worker.Visuals.IsEmoteVisible);
            yield return new WaitForSeconds(.16f);
            Assert.That(worker.Visuals.CurrentEmote, Is.Empty);
            Assert.False(worker.Visuals.IsEmoteVisible);
            Assert.That(WorkerVisuals.SafeText("\u25A1", "?"), Is.EqualTo("?"));
            worker.CancelPlayerCarryImmediate();
        }

        [UnityTest] public IEnumerator OptionalDistractionCanBeRedirectedByManualCommand()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            WorkerAgent worker = office.Workers[2];
            yield return WaitForPickable(worker);
            worker.BeginDistractionForTesting(DistractionKind.Phone);
            Assert.That(worker.Runtime.behavior, Is.EqualTo(WorkerState.LookAtPhone));
            Assert.That(worker.Productivity, Is.Zero);
            yield return PlaceWorker(office, worker, FindZone(office, "starter.rest.break-nook"));
            yield return WaitForState(worker, WorkerState.TakeBreak);
            Assert.That(worker.CurrentDistraction, Is.EqualTo(DistractionKind.None));
            Assert.True(worker.HasPlayerCommandAuthority);
        }

        [UnityTest] public IEnumerator ManualFocusedWorkOverridesAutonomyForThirtySecondWindow()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            WorkerAgent worker = office.Workers[2];
            yield return PlaceWorker(office, worker, worker.Desk);
            yield return WaitForState(worker, WorkerState.Work);
            SimulationSpeedController.Instance.SetSpeed(4f);
            yield return new WaitForSeconds(12f);
            Assert.That(worker.Runtime.behavior, Is.EqualTo(WorkerState.Work));
            Assert.True(worker.HasPlayerCommandAuthority);
            Assert.That(worker.Runtime.focusedWorkSecondsRemaining, Is.InRange(17f, 19f));
        }

        [UnityTest] public IEnumerator ActivityZoneCapacityRejectsWorkerBeyondCapacity()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            PlacementZone rest = FindZone(office, "starter.rest.break-nook");
            yield return PlaceWorker(office, office.Workers[0], rest);
            yield return WaitForState(office.Workers[0], WorkerState.TakeBreak);
            yield return PlaceWorker(office, office.Workers[1], rest);
            yield return WaitForState(office.Workers[1], WorkerState.TakeBreak);
            Assert.That(rest.Occupancy, Is.EqualTo(rest.Capacity));
            WorkerAgent third = office.Workers[2];
            yield return WaitForPickable(third);
            StartCarry(office.CarryController, third);
            office.CarryController.UpdateCarriedPosition(rest.PlacementPoint.position, rest,
                new Vector2(Screen.width * .5f, Screen.height * .5f), true);
            Assert.False(office.CarryController.HasValidTarget);
            Assert.That(office.CarryController.FeedbackText, Does.Contain("OCCUPIED"));
            office.CarryController.ReleaseAtZone(rest);
            yield return new WaitForSeconds(.28f);
            Assert.That(rest.Occupancy, Is.EqualTo(rest.Capacity));
            Assert.False(third.IsPlayerCarried);
        }

        [UnityTest] public IEnumerator FiveNeedsInitializeHealthyAndAllChangeDuringSimulation()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            office.Tutorial.SkipTutorial();
            WorkerAgent worker = office.Workers[0];
            float[] before = SnapshotNeeds(worker.Runtime);
            foreach (NeedDefinition definition in NeedCatalog.All)
                Assert.That(definition.Status(worker.Runtime.GetNeed(definition.Kind)), Is.EqualTo(NeedStatus.Healthy));
            SimulationSpeedController.Instance.SetSpeed(4f);
            yield return new WaitForSecondsRealtime(.7f);
            float[] after = SnapshotNeeds(worker.Runtime);
            for (int i = 0; i < after.Length; i++)
                Assert.That(after[i], Is.Not.EqualTo(before[i]).Within(.00001f), NeedCatalog.All[i].DisplayName);
        }

        [UnityTest] public IEnumerator PauseAndTutorialModalFreezeEveryNeedExactly()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            WorkerAgent worker = office.Workers[0];
            SimulationSpeedController.Instance.SetSpeed(0f);
            float[] before = SnapshotNeeds(worker.Runtime);
            office.Tutorial.StartTutorial(true);
            yield return new WaitForSecondsRealtime(.4f);
            AssertNeedsEqual(before, worker.Runtime, .000001f);
            office.Tutorial.SkipTutorial();
            SimulationSpeedController.Instance.SetSpeed(1f);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(SnapshotNeeds(worker.Runtime), Is.Not.EqualTo(before));
        }

        [UnityTest] public IEnumerator EquivalentSimulatedPartitionsProduceEquivalentNeedsInPlayMode()
        {
            var single = new WorkerRuntimeState();
            var partitioned = new WorkerRuntimeState();
            var twoTimes = new WorkerRuntimeState();
            var fourTimes = new WorkerRuntimeState();
            NeedCatalog.Initialize(single, "Partition", 44);
            NeedCatalog.Initialize(partitioned, "Partition", 44);
            NeedCatalog.Initialize(twoTimes, "Partition", 44);
            NeedCatalog.Initialize(fourTimes, "Partition", 44);
            NeedSimulation.Tick(single, WorkerState.Work, 40f);
            for (int i = 0; i < 40; i++) NeedSimulation.Tick(partitioned, WorkerState.Work, 1f);
            for (int i = 0; i < 20; i++)
                NeedSimulation.Tick(twoTimes, WorkerState.Work, SimulationRules.ScaledDelta(1f, 2f));
            for (int i = 0; i < 10; i++)
                NeedSimulation.Tick(fourTimes, WorkerState.Work, SimulationRules.ScaledDelta(1f, 4f));
            AssertNeedsEqual(SnapshotNeeds(single), partitioned, .00002f);
            AssertNeedsEqual(SnapshotNeeds(single), twoTimes, .00002f);
            AssertNeedsEqual(SnapshotNeeds(single), fourTimes, .00002f);
            yield return null;
        }

        [UnityTest] public IEnumerator SelectingEmployeeDoesNotResetOrMutateNeedsWhilePaused()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            WorkerAgent worker = office.Workers[1];
            SimulationSpeedController.Instance.SetSpeed(0f);
            float[] before = SnapshotNeeds(worker.Runtime);
            WorkerSelection.Select(worker);
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(WorkerSelection.Selected, Is.SameAs(worker));
            AssertNeedsEqual(before, worker.Runtime, .000001f);
        }

        [UnityTest] public IEnumerator RestroomInfluenceStartsUseRecoversBathroomAndReleasesCapacity()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            WorkerAgent worker = office.Workers[0];
            PlacementZone restroom = FindZone(office, "starter.restroom.main");
            Vector3 influencedPoint = restroom.PlacementPoint.position + Vector3.right *
                (restroom.FootprintBounds.extents.x + .20f);
            Assert.False(restroom.FootprintBounds.Contains(new Vector3(influencedPoint.x,
                restroom.FootprintBounds.center.y, influencedPoint.z)));
            Assert.That(WorkerCarryController.ResolveInfluencingZone(office.PlacementZones, influencedPoint), Is.SameAs(restroom));
            yield return PlaceWorker(office, worker, restroom);
            yield return WaitForState(worker, WorkerState.UseRestroom);
            worker.Runtime.bathroom = .90f;
            Assert.That(restroom.Occupancy, Is.EqualTo(1));
            Assert.True(worker.IsVisibleInOffice);
            Assert.That(worker.Visuals.CurrentEmote, Is.EqualTo("WC"));
            SimulationSpeedController.Instance.SetSpeed(4f);
            yield return WaitForState(worker, WorkerState.ReturnToDesk, 4f);
            Assert.That(worker.Runtime.bathroom, Is.InRange(.115f,.135f));
            Assert.That(restroom.Occupancy, Is.Zero);
            Assert.True(worker.IsVisibleInOffice);
            Assert.False(worker.IsPlayerCarried);
        }

        [UnityTest] public IEnumerator RestartDuringRestroomUseLeavesNoReservationOrHiddenWorker()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            WorkerAgent worker = office.Workers[0];
            yield return PlaceWorker(office, worker, FindZone(office, "starter.restroom.main"));
            yield return WaitForState(worker, WorkerState.UseRestroom);
            office.Restart();
            yield return null;
            yield return null;
            OfficeDirector restarted = Object.FindFirstObjectByType<OfficeDirector>();
            PlacementZone restroom = FindZone(restarted, "starter.restroom.main");
            Assert.That(restroom.Occupancy, Is.Zero);
            foreach (WorkerAgent current in restarted.Workers)
            {
                Assert.True(current.IsVisibleInOffice);
                Assert.That(current.Runtime.behavior, Is.Not.EqualTo(WorkerState.UseRestroom));
            }
        }

        [UnityTest] public IEnumerator InspectorShowsFiveRowsUrgencyCopyAndNoStressGauge()
        {
            Screen.SetResolution(1920, 1080, false);
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            WorkerAgent worker = office.Workers[0];
            WorkerSelection.Select(worker);
            yield return new WaitForSecondsRealtime(.25f);
            Assert.True(office.HUD.InspectorVisible);
            Assert.That(office.HUD.VisibleNeedRowCount, Is.EqualTo(5));
            EmployeeNeedRow[] rows = Object.FindObjectsByType<EmployeeNeedRow>(FindObjectsSortMode.None);
            Assert.That(rows.Length, Is.EqualTo(5));
            Assert.That(System.Array.Exists(rows, row => row.DisplayedText.Contains("HUNGER URG")), Is.True);
            Assert.That(System.Array.Exists(rows, row => row.DisplayedText.Contains("BATH URG")), Is.True);
            Assert.IsNull(GameObject.Find("Stress Need"));
            Assert.That(office.HUD.InspectorText, Does.Contain("ACTIVITY"));
            Assert.That(office.HUD.InspectorText, Does.Contain("OUTPUT"));
        }

        [UnityTest] public IEnumerator UrgentNeedStateRemainsTextReadableAt1280x720()
        {
            Screen.SetResolution(1280, 720, false);
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            WorkerAgent worker = office.Workers[0];
            worker.Runtime.hunger = .74f;
            worker.Runtime.bathroom = .72f;
            WorkerSelection.Select(worker);
            yield return new WaitForSecondsRealtime(.25f);
            EmployeeNeedRow[] rows = Object.FindObjectsByType<EmployeeNeedRow>(FindObjectsSortMode.None);
            EmployeeNeedRow hunger = System.Array.Find(rows, row => row.Kind == NeedKind.Hunger);
            EmployeeNeedRow bathroom = System.Array.Find(rows, row => row.Kind == NeedKind.Bathroom);
            Assert.That(hunger.Status, Is.EqualTo(NeedStatus.Urgent));
            Assert.That(hunger.DisplayedText, Does.Contain("Hungry"));
            Assert.That(bathroom.Status, Is.EqualTo(NeedStatus.Urgent));
            Assert.That(bathroom.DisplayedText, Does.Contain("Urgent"));
            RectTransform card = GameObject.Find("Employee Card").GetComponent<RectTransform>();
            Assert.That(card.anchorMin.x, Is.GreaterThanOrEqualTo(0f));
            Assert.That(card.anchorMax.x, Is.LessThanOrEqualTo(1f));
        }

        [UnityTest] public IEnumerator DesklessPhoneWorkerUsesFiveNeedsAndStillEarns()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            office.Cash.AccrueDeskIncome(7f, 60f);
            Assert.True(office.TryHire(0, out string reason), reason);
            WorkerAgent phone = office.Workers[office.Workers.Count - 1];
            SimulationSpeedController.Instance.SetSpeed(4f);
            yield return WaitForState(phone, WorkerState.Unassigned, 3f);
            float[] needs = SnapshotNeeds(phone.Runtime);
            float cash = office.Cash.CurrentCash;
            yield return new WaitForSecondsRealtime(.6f);
            Assert.True(phone.IsPhoneWorking);
            AssertNeedsChanged(needs, phone.Runtime);
            Assert.Greater(office.Cash.CurrentCash, cash);
            WorkerSelection.Select(phone);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(office.HUD.VisibleNeedRowCount, Is.EqualTo(5));
        }

        [UnityTest] public IEnumerator StartingWorkersHaveRepeatableNonidenticalHealthyNeedOffsets()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            float[] morgan = SnapshotNeeds(office.Workers[0].Runtime);
            float[] alex = SnapshotNeeds(office.Workers[1].Runtime);
            float[] sam = SnapshotNeeds(office.Workers[2].Runtime);
            Assert.That(morgan, Is.Not.EqualTo(alex));
            Assert.That(alex, Is.Not.EqualTo(sam));
            foreach (WorkerAgent worker in office.Workers)
                foreach (NeedDefinition definition in NeedCatalog.All)
                    Assert.That(definition.Status(worker.Runtime.GetNeed(definition.Kind)), Is.EqualTo(NeedStatus.Healthy));
        }

        [UnityTest] public IEnumerator TutorialCopyNamesFiveNeedsUrgenciesStressAndFutureAutonomy()
        {
            yield return LoadOffice(OfficeStage.StarterOffice);
            string copy = TutorialCopy.Body(TutorialStep.ManageTheirNeeds);
            Assert.That(copy, Does.Contain("five live needs"));
            Assert.That(copy, Does.Contain("Hunger and Bathroom are urgency meters"));
            Assert.That(copy, Does.Contain("Stress is a separate temporary influence"));
            Assert.That(copy, Does.Contain("next milestone"));
        }

        private static float[] SnapshotNeeds(WorkerRuntimeState state)
        {
            float[] values = new float[NeedCatalog.All.Length];
            for (int i = 0; i < values.Length; i++) values[i] = state.GetNeed(NeedCatalog.All[i].Kind);
            return values;
        }

        private static void AssertNeedsEqual(float[] expected, WorkerRuntimeState actual, float tolerance)
        {
            for (int i = 0; i < expected.Length; i++)
                Assert.That(actual.GetNeed(NeedCatalog.All[i].Kind), Is.EqualTo(expected[i]).Within(tolerance),
                    NeedCatalog.All[i].DisplayName);
        }

        private static void AssertNeedsChanged(float[] expected, WorkerRuntimeState actual)
        {
            for (int i = 0; i < expected.Length; i++)
                Assert.That(actual.GetNeed(NeedCatalog.All[i].Kind), Is.Not.EqualTo(expected[i]).Within(.00001f),
                    NeedCatalog.All[i].DisplayName);
        }
    }
}
