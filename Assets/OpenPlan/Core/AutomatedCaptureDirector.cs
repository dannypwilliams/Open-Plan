using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace OpenPlan
{
    public sealed class AutomatedCaptureDirector : MonoBehaviour
    {
        private OfficeDirector office;
        private string output;

        public static bool Requested => Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-openplan-capture");

        public void Initialize(OfficeDirector director)
        {
            office = director;
            output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Screenshots"));
            Directory.CreateDirectory(output);
            StartCoroutine(CaptureSequence());
        }

        private IEnumerator CaptureSequence()
        {
            if (office.Stage != OfficeStage.EstablishedOffice)
            {
                yield return new WaitForSecondsRealtime(2f);
                Camera.main.GetComponent<OfficeCameraRig>().Overview();
                WorkerSelection.Clear();
                yield return Capture(office.Stage + "_Overview.png");
                File.WriteAllText(Path.Combine(output, "capture_complete.txt"), DateTime.UtcNow.ToString("O"));
                Application.Quit(0);
                yield break;
            }

            yield return new WaitForSecondsRealtime(11f);
            OfficeCameraRig cameraRig = Camera.main.GetComponent<OfficeCameraRig>();
            WorkerSelection.Clear();
            cameraRig.Overview();
            yield return Capture("01_Full_Office_Overview.png");
            yield return Capture("12_Dark_Surrounding_Diorama.png");
            yield return Capture("11_Warm_Sunlight_Composition.png");
            cameraRig.FocusPoint(new Vector3(-9f, 0f, 7f), 7f);
            yield return new WaitForSecondsRealtime(.7f);
            yield return Capture("14_Reception_And_Elevator.png");
            cameraRig.FocusPoint(new Vector3(9f, 0f, 7f), 7f);
            yield return new WaitForSecondsRealtime(.7f);
            yield return Capture("15_Meeting_Room.png");

            WorkerAgent worker = office.Workers[2];
            WorkerSelection.Select(worker);
            cameraRig.FocusWorker(worker, true);
            yield return new WaitForSecondsRealtime(2f);
            yield return Capture("02_Close_Worker_Typing.png");
            yield return Capture("13_Worker_Follow_View.png");
            office.ToggleOverlay();
            yield return Capture("09_Productivity_Overlay.png");
            office.ToggleOverlay();

            SimulationSpeedController.Instance.SetSpeed(4f);
            WorkerAgent waterWorker = office.Workers[1];
            waterWorker.ForceStateForCapture(StationKind.Water);
            yield return WaitForState(waterWorker, WorkerState.UseWaterCooler, 5f);
            SimulationSpeedController.Instance.SetSpeed(0f);
            WorkerAgent waterFriend = office.Workers[3];
            waterFriend.transform.position = waterWorker.transform.position + new Vector3(1.15f, 0f, .35f);
            waterFriend.transform.rotation = Quaternion.LookRotation(waterWorker.transform.position - waterFriend.transform.position);
            waterWorker.transform.rotation = Quaternion.LookRotation(waterFriend.transform.position - waterWorker.transform.position);
            WorkerSelection.Select(waterWorker);
            cameraRig.FocusWorker(waterWorker, false);
            yield return new WaitForSecondsRealtime(.5f);
            yield return Capture("03_Water_Cooler_Conversation.png");

            SimulationSpeedController.Instance.SetSpeed(4f);
            WorkerAgent coffeeWorker = office.Workers[0];
            coffeeWorker.ForceStateForCapture(StationKind.Coffee);
            yield return WaitForState(coffeeWorker, WorkerState.UseCoffeeMachine, 6f);
            SimulationSpeedController.Instance.SetSpeed(0f);
            WorkerSelection.Select(coffeeWorker);
            cameraRig.FocusWorker(coffeeWorker, false);
            yield return new WaitForSecondsRealtime(.5f);
            yield return Capture("04_Coffee_Interaction.png");

            SimulationSpeedController.Instance.SetSpeed(4f);
            WorkerAgent socialWorker = office.Workers[4];
            socialWorker.ForceStateForCapture(StationKind.Meeting, office.Workers[1]);
            yield return WaitForState(socialWorker, WorkerState.Socialize, 4f);
            SimulationSpeedController.Instance.SetSpeed(0f);
            Vector3 conversation = new Vector3(2.5f, 0f, 1.4f);
            waterWorker.transform.position = conversation + new Vector3(-.65f, 0f, 0f);
            socialWorker.transform.position = conversation + new Vector3(.65f, 0f, 0f);
            waterWorker.transform.rotation = Quaternion.LookRotation(socialWorker.transform.position - waterWorker.transform.position);
            socialWorker.transform.rotation = Quaternion.LookRotation(waterWorker.transform.position - socialWorker.transform.position);
            WorkerSelection.Select(socialWorker);
            cameraRig.FocusWorker(socialWorker, false);
            yield return new WaitForSecondsRealtime(.5f);
            yield return Capture("05_Social_Cluster.png");

            WorkerSelection.Select(worker);
            cameraRig.FocusWorker(worker, false);
            yield return new WaitForSecondsRealtime(.5f);
            yield return Capture("06_Productive_Quiet_Desk.png");

            OfficeHUDController hud = FindFirstObjectByType<OfficeHUDController>();
            hud.ShowHiringForCapture();
            yield return new WaitForSecondsRealtime(.5f);
            yield return Capture("07_Hiring_Panel.png");
            hud.HideHiringForCapture();
            office.TryHire(0, out _);
            SimulationSpeedController.Instance.SetSpeed(4f);
            WorkerAgent fired = office.Workers[5];
            WorkerSelection.Select(fired);
            office.TryFire(fired, out _);
            yield return new WaitForSecondsRealtime(1.8f);
            SimulationSpeedController.Instance.SetSpeed(0f);
            cameraRig.FocusWorker(fired, true);
            yield return new WaitForSecondsRealtime(1f);
            yield return Capture("08_Fired_Worker_Carrying_Box.png");
            office.Workday.Finish();
            yield return new WaitForSecondsRealtime(1f);
            yield return Capture("10_End_Of_Day_Report.png");
            File.WriteAllText(Path.Combine(output, "capture_complete.txt"), DateTime.UtcNow.ToString("O"));
            Application.Quit(0);
        }

        private static IEnumerator WaitForState(WorkerAgent worker, WorkerState wanted, float timeout)
        {
            float end = Time.realtimeSinceStartup + timeout;
            while (worker != null && worker.Runtime.behavior != wanted && Time.realtimeSinceStartup < end)
                yield return null;
        }

        private IEnumerator Capture(string file)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output, file), 1);
            yield return new WaitForSecondsRealtime(.45f);
        }

    }
}
