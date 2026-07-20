using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace OpenPlan
{
    /// <summary>Packaged-build evidence pass for the locked and physically expanded starter office.</summary>
    public sealed class StandaloneExpansionCaptureDirector : MonoBehaviour
    {
        public const string Argument = "-openplan-expansion-capture";
        public static bool Requested => Array.Exists(Environment.GetCommandLineArgs(), arg => arg == Argument);

        private OfficeDirector office;
        private string output;

        public void Initialize(OfficeDirector director)
        {
            office = director;
            output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Screenshots"));
            Directory.CreateDirectory(output);
            StartCoroutine(CaptureSequence());
        }

        private IEnumerator CaptureSequence()
        {
            yield return new WaitForSecondsRealtime(2.5f);
            OfficeCameraRig rig = Camera.main.GetComponent<OfficeCameraRig>();
            WorkerSelection.Clear();
            SimulationSpeedController.Instance?.SetSpeed(0f);
            rig.Overview();
            yield return new WaitForSecondsRealtime(.8f);
            CaptureCamera("StarterOffice_BeforeExpansion.png");

            float beforePurchase = office.Cash.CurrentCash;
            office.Cash.AccrueDeskIncome(15f, 60f);
            float affordableCash = office.Cash.CurrentCash;
            bool purchased = office.TryPurchaseExpansion(out string reason);
            float afterPurchase = office.Cash.CurrentCash;
            yield return new WaitForSecondsRealtime(1.7f);
            SimulationSpeedController.Instance?.SetSpeed(0f);
            rig.Overview();
            yield return new WaitForSecondsRealtime(.8f);
            CaptureCamera("StarterOffice_AfterExpansion.png");

            string report =
                $"captured_utc={DateTime.UtcNow:O}\n" +
                $"starting_capture_cash={beforePurchase:0.00}\n" +
                $"affordable_cash={affordableCash:0.00}\n" +
                $"purchased={purchased}\n" +
                $"purchase_reason={reason ?? "none"}\n" +
                $"purchase_deduction={affordableCash - afterPurchase:0.00}\n" +
                $"expanded={office.ExpansionComplete}\n" +
                $"wall_open={office.Expansion.ConnectingWallOpen}\n" +
                $"doorway_trim={office.Expansion.DoorwayTrimVisible}\n" +
                $"navigation={office.Expansion.NavigationEnabled}\n" +
                $"capacity={office.WorkerCapacity}\n" +
                $"pan_width={office.Layout.PanBounds.size.x:0.00}\n" +
                $"expected_minutes_to_afford={ExpansionRules.ExpectedMinutesToAfford():0.00}\n";
            File.WriteAllText(Path.Combine(output, "StarterOffice_ExpansionCapture.txt"), report);
            Application.Quit(purchased && office.ExpansionComplete ? 0 : 2);
        }

        private void CaptureCamera(string fileName)
        {
            Camera camera = Camera.main;
            const int width = 1920;
            const int height = 1080;
            RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            camera.targetTexture = target;
            RenderTexture.active = target;
            camera.Render();
            image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(output, fileName), image.EncodeToPNG());
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            Destroy(image);
            target.Release();
            Destroy(target);
        }
    }
}
