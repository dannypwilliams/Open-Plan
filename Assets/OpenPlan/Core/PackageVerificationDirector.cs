using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenPlan
{
    public sealed class PackageVerificationMenuDriver : MonoBehaviour
    {
        private bool finish;
        public void Initialize(bool shouldFinish) { finish = shouldFinish; StartCoroutine(Run()); }

        private IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(1.5f);
            if (!finish) { SceneManager.LoadScene("Office"); yield break; }
            string marker = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "package-verification-pass.txt"));
            File.WriteAllText(marker, DateTime.UtcNow.ToString("O"));
            Debug.Log("PACKAGE VERIFY: return to menu and close PASS");
            Application.Quit(0);
        }
    }

    /// <summary>Exercises the packaged player across scene reloads without editor-only APIs.</summary>
    public sealed class PackageVerificationDirector : MonoBehaviour
    {
        private OfficeDirector office;
        public static int Stage { get; private set; }
        public static bool Requested => Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-openplan-verify-package");

        public void Initialize(OfficeDirector director)
        {
            office = director;
            StartCoroutine(Stage == 0 ? ExerciseWorkday() : ExerciseRestart());
        }

        private IEnumerator ExerciseWorkday()
        {
            yield return new WaitForSecondsRealtime(2f);
            Debug.Log("PACKAGE VERIFY: main menu and start workday PASS");
            WorkerAgent selected = office.Workers[0];
            WorkerSelection.Select(selected);
            Debug.Log("PACKAGE VERIFY: select worker PASS");
            SimulationSpeedController.Instance.SetSpeed(4f);
            Debug.Log("PACKAGE VERIFY: speed controls PASS");
            if (!office.TryHire(0, out string hireReason)) throw new InvalidOperationException("Hire failed: " + hireReason);
            Debug.Log("PACKAGE VERIFY: hire PASS");
            office.BeginReassign();
            Workstation destination = office.Workstations[9];
            if (!office.ReassignSelected(destination)) throw new InvalidOperationException("Reassign failed");
            Debug.Log("PACKAGE VERIFY: reassign PASS");
            if (!office.TryFire(office.Workers[1], out string fireReason)) throw new InvalidOperationException("Fire failed: " + fireReason);
            Debug.Log("PACKAGE VERIFY: fire PASS");
            office.Workday.Finish();
            Debug.Log("PACKAGE VERIFY: finish day PASS");
            yield return new WaitForSecondsRealtime(1f);
            Stage = 1;
            office.Restart();
        }

        private IEnumerator ExerciseRestart()
        {
            yield return new WaitForSecondsRealtime(2f);
            Debug.Log("PACKAGE VERIFY: restart workday PASS");
            Stage = 2;
            office.ReturnToMenu();
        }
    }
}
