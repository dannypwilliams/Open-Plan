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
            if (AutomatedCaptureDirector.Requested || AutomatedPerformanceDirector.Requested)
            {
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
            OfficeUIFactory.Text(card, "Pitch", "Observe the room. Hire carefully. Seat people wisely.\nReach $1,500 before the five-minute workday ends.", 27f, OfficeUIFactory.Paper,
                new Vector2(.12f,.38f), new Vector2(.88f,.56f), Vector2.zero, Vector2.zero, TMPro.TextAlignmentOptions.Center);
            Button start = OfficeUIFactory.Button(card, "Start", "START WORKDAY", OfficeUIFactory.Orange, Color.white,
                new Vector2(.28f,.22f), new Vector2(.72f,.33f), Vector2.zero, Vector2.zero);
            start.onClick.AddListener(() => SceneManager.LoadScene("Office"));
            Button quit = OfficeUIFactory.Button(card, "Quit", "QUIT", OfficeUIFactory.Burgundy, Color.white,
                new Vector2(.40f,.10f), new Vector2(.60f,.17f), Vector2.zero, Vector2.zero);
            quit.onClick.AddListener(Application.Quit);
            OfficeUIFactory.Text(card, "Controls", "WHEEL zoom  •  MIDDLE DRAG pan  •  CLICK select  •  F follow  •  H hire  •  TAB overlay  •  SPACE pause  •  1/2/3 speed", 18f,
                new Color(.73f,.66f,.56f), new Vector2(.06f,.01f), new Vector2(.94f,.08f), Vector2.zero, Vector2.zero, TMPro.TextAlignmentOptions.Center);
            if (AutomatedVideoDirector.Requested)
                gameObject.AddComponent<AutomatedVideoMenuDriver>();
            if (PackageVerificationDirector.Requested)
                gameObject.AddComponent<PackageVerificationMenuDriver>().Initialize(PackageVerificationDirector.Stage >= 2);
        }
    }
}
