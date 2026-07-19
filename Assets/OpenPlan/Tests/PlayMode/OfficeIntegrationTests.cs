using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

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
            Assert.That(office.Workstations.Count, Is.EqualTo(3));
            Assert.False(office.Workday.IsTimed);
            foreach (PlacementActivity activity in System.Enum.GetValues(typeof(PlacementActivity)))
                Assert.That(office.PlacementZones, Has.Some.Property("Activity").EqualTo(activity));
        }

        [UnityTest] public IEnumerator ExpandedStarterInitializesWithAdditionalSpace()
        {
            yield return LoadOffice(OfficeStage.StarterOfficeExpanded);
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            Assert.That(office.Stage, Is.EqualTo(OfficeStage.StarterOfficeExpanded));
            Assert.That(office.Workers.Count, Is.EqualTo(3));
            Assert.That(office.Workstations.Count, Is.EqualTo(6));
            Assert.False(office.Workday.IsTimed);
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

        [UnityTest] public IEnumerator WorkdayEndDisplaysReportCanvas()
        {
            yield return LoadOffice();
            OfficeDirector office = Object.FindFirstObjectByType<OfficeDirector>();
            office.Workday.Finish();
            yield return null;
            GameObject report = GameObject.Find("Report");
            Assert.NotNull(report);
            Assert.True(report.activeInHierarchy);
        }

        [UnityTest] public IEnumerator UiSmokeAt1280x720()
        {
            Screen.SetResolution(1280, 720, false);
            yield return LoadOffice();
            Assert.NotNull(Object.FindFirstObjectByType<Canvas>());
            Assert.That(Screen.width, Is.GreaterThanOrEqualTo(640));
        }

        [UnityTest] public IEnumerator UiSmokeAt1920x1080()
        {
            Screen.SetResolution(1920, 1080, false);
            yield return LoadOffice();
            Assert.NotNull(Object.FindFirstObjectByType<Canvas>());
            Assert.That(Screen.height, Is.GreaterThanOrEqualTo(480));
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
    }
}
