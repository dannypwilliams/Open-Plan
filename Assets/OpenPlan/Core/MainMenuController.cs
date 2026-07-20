using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenPlan
{
    public sealed class MainMenuController : MonoBehaviour
    {
        private void Start()
        {
            Time.timeScale = 1f;
            if (AutomatedCaptureDirector.Requested || AutomatedPerformanceDirector.Requested ||
                StandaloneInputSmokeDirector.Requested || StandaloneActivityCycleDirector.Requested ||
                StandaloneBehaviorSoakDirector.Requested || StandaloneExpansionCaptureDirector.Requested ||
                StandaloneTutorialPlaythroughDirector.Requested)
            {
                OfficeStageSelection.SelectForNextLoad(OfficeStageSelection.Resolve(System.Environment.GetCommandLineArgs()));
                SceneManager.LoadScene("Office");
                return;
            }
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Menu Camera");
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
                camera.tag = "MainCamera";
            }
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.035f,.018f,.015f);
            Canvas canvas = OfficeUIFactory.CreateCanvas("Main Menu");
            RectTransform card = OfficeUIFactory.Panel(canvas.transform, "Menu Card", new Color(.08f,.05f,.045f,.96f),
                new Vector2(.18f,.12f), new Vector2(.82f,.88f), Vector2.zero, Vector2.zero);
            OfficeUIFactory.Text(card, "Logo", "OPEN PLAN", 92f, OfficeUIFactory.Paper,
                new Vector2(.08f,.64f), new Vector2(.92f,.90f), Vector2.zero, Vector2.zero, TMPro.TextAlignmentOptions.Center);
            OfficeUIFactory.Text(card, "Subtitle", "A TINY OFFICE WITH VERY LARGE OPINIONS", 24f, OfficeUIFactory.Orange,
                new Vector2(.10f,.58f), new Vector2(.90f,.68f), Vector2.zero, Vector2.zero, TMPro.TextAlignmentOptions.Center);
            OfficeUIFactory.Text(card, "Pitch", "Start small. Guide three workers. Earn the neighboring unit.\nThere is no countdown \u2014 expand when the business is ready.", 27f, OfficeUIFactory.Paper,
                new Vector2(.12f,.38f), new Vector2(.88f,.56f), Vector2.zero, Vector2.zero, TMPro.TextAlignmentOptions.Center);
            Button start = OfficeUIFactory.Button(card, "Start", "START WORKDAY", OfficeUIFactory.Orange, Color.white,
                new Vector2(.28f,.22f), new Vector2(.72f,.33f), Vector2.zero, Vector2.zero);
            start.onClick.AddListener(StartStarterOffice);
            Button quit = OfficeUIFactory.Button(card, "Quit", "QUIT", OfficeUIFactory.Burgundy, Color.white,
                new Vector2(.40f,.10f), new Vector2(.60f,.17f), Vector2.zero, Vector2.zero);
            quit.onClick.AddListener(Application.Quit);
            OfficeUIFactory.Text(card, "Controls", "CLICK select  •  HOLD + DRAG place  •  ESC / RIGHT CLICK cancel  •  WHEEL zoom  •  MIDDLE DRAG pan\nF follow  •  N name tags  •  SPACE pause  •  1 / 2 / 3 speed  •  HELP is always available in the office", 18f,
                new Color(.73f,.66f,.56f), new Vector2(.06f,.005f), new Vector2(.94f,.10f), Vector2.zero, Vector2.zero, TMPro.TextAlignmentOptions.Center);
            if (AutomatedVideoDirector.Requested)
                gameObject.AddComponent<AutomatedVideoMenuDriver>();
            if (PackageVerificationDirector.Requested)
                gameObject.AddComponent<PackageVerificationMenuDriver>().Initialize(PackageVerificationDirector.Stage >= 2);
            if (StandaloneFriendDemoDirector.Requested)
                gameObject.AddComponent<StandaloneFriendDemoMenuDriver>().Initialize();
            if (StandaloneFoundationCheckpointDirector.Requested)
                gameObject.AddComponent<StandaloneFoundationCheckpointMenuDriver>().Initialize();
            if (StandaloneFiveNeedsCheckpointDirector.Requested)
                gameObject.AddComponent<StandaloneFiveNeedsCheckpointMenuDriver>().Initialize();
            if (StandaloneNeedAutonomyCheckpointDirector.Requested)
                gameObject.AddComponent<StandaloneNeedAutonomyCheckpointMenuDriver>().Initialize();
        }

        public void StartStarterOffice()
        {
            OfficeStageSelection.SelectForNextLoad(OfficeStage.StarterOffice);
            SceneManager.LoadScene("Office");
        }
    }
}
