using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace OpenPlan
{
    public sealed class AutomatedVideoMenuDriver : MonoBehaviour
    {
        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(7f);
            UnityEngine.SceneManagement.SceneManager.LoadScene("Office");
        }
    }

    /// <summary>Drives a hands-off, continuous 105-second gameplay tour for release media.</summary>
    public sealed class AutomatedVideoDirector : MonoBehaviour
    {
        private OfficeDirector office;
        public static bool Requested => Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-openplan-video");

        public void Initialize(OfficeDirector director)
        {
            office = director;
            StartCoroutine(Tour());
        }

        private IEnumerator Tour()
        {
            OfficeCameraRig cameraRig = Camera.main.GetComponent<OfficeCameraRig>();
            OfficeHUDController hud = FindFirstObjectByType<OfficeHUDController>();
            SimulationSpeedController.Instance.SetSpeed(1f);
            cameraRig.Overview();
            yield return new WaitForSecondsRealtime(11f);

            WorkerAgent focusWorker = office.Workers[2];
            WorkerSelection.Select(focusWorker);
            cameraRig.FocusWorker(focusWorker, true);
            yield return new WaitForSecondsRealtime(12f);

            WorkerAgent waterWorker = office.Workers[1];
            waterWorker.ForceStateForCapture(StationKind.Water);
            WorkerSelection.Select(waterWorker);
            cameraRig.FocusWorker(waterWorker, true);
            yield return new WaitForSecondsRealtime(12f);

            WorkerAgent coffeeWorker = office.Workers[0];
            coffeeWorker.ForceStateForCapture(StationKind.Coffee);
            WorkerSelection.Select(coffeeWorker);
            cameraRig.FocusWorker(coffeeWorker, true);
            yield return new WaitForSecondsRealtime(10f);

            hud.ShowHiringForCapture();
            yield return new WaitForSecondsRealtime(7f);
            hud.HideHiringForCapture();
            office.TryHire(0, out _);
            WorkerSelection.Select(focusWorker);
            office.BeginReassign();
            office.ReassignSelected(office.Workstations[9]);
            cameraRig.FocusWorker(focusWorker, false);
            yield return new WaitForSecondsRealtime(4f);

            WorkerAgent socialWorker = office.Workers[4];
            socialWorker.ForceStateForCapture(StationKind.Meeting, waterWorker);
            WorkerSelection.Select(socialWorker);
            cameraRig.FocusWorker(socialWorker, true);
            yield return new WaitForSecondsRealtime(15f);

            WorkerSelection.Clear();
            cameraRig.Overview();
            office.ToggleOverlay();
            yield return new WaitForSecondsRealtime(12f);
            office.ToggleOverlay();

            WorkerAgent fired = office.Workers[5];
            WorkerSelection.Select(fired);
            office.TryFire(fired, out _);
            cameraRig.FocusWorker(fired, true);
            yield return new WaitForSecondsRealtime(14f);

            office.Workday.Finish();
            yield return new WaitForSecondsRealtime(8f);
            string media = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Media"));
            Directory.CreateDirectory(media);
            File.WriteAllText(Path.Combine(media, "video-tour-complete.txt"), DateTime.UtcNow.ToString("O"));
            Application.Quit(0);
        }
    }
}
