using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace OpenPlan
{
    [CreateAssetMenu(menuName = "Open Plan/Camera Zoom Profile")]
    public sealed class CameraZoomProfile : ScriptableObject
    {
        public float closeSize = 4.8f;
        public float overviewSize = 18.5f;
        public float zoomSensitivity = 0.012f;
        public float panSensitivity = 0.018f;
        public float smoothTime = 0.16f;
    }

    public sealed class CameraFocusController
    {
        public Vector3 Target { get; private set; }
        public Vector3 Velocity;
        public void Set(Vector3 value) => Target = value;
        public void Add(Vector3 value) => Target += value;
    }

    public sealed class WorkerFollowController
    {
        public WorkerAgent Target { get; private set; }
        public bool Active => Target != null;
        public void Follow(WorkerAgent worker) => Target = worker;
        public void Stop() => Target = null;
    }

    [RequireComponent(typeof(Camera))]
    public sealed class OfficeCameraRig : MonoBehaviour
    {
        private Camera cameraComponent;
        private OfficeDirector office;
        private CameraZoomProfile profile;
        private readonly CameraFocusController focus = new CameraFocusController();
        private readonly WorkerFollowController follow = new WorkerFollowController();
        private float targetSize = 17.8f;
        private float zoomVelocity;
        private float lastClickTime;
        private WorkerAgent lastClicked;

        public float OrthographicSize => cameraComponent != null ? cameraComponent.orthographicSize : targetSize;
        public bool IsFollowing => follow.Active;

        public void Initialize(OfficeDirector director)
        {
            office = director;
            cameraComponent = GetComponent<Camera>();
            profile = Resources.Load<CameraZoomProfile>("CameraZoomProfile");
            if (profile == null) profile = ScriptableObject.CreateInstance<CameraZoomProfile>();
            cameraComponent.orthographic = true;
            cameraComponent.clearFlags = CameraClearFlags.SolidColor;
            cameraComponent.backgroundColor = new Color(.035f, .018f, .015f);
            cameraComponent.nearClipPlane = .1f;
            cameraComponent.farClipPlane = 120f;
            cameraComponent.allowHDR = true;
            targetSize = profile.overviewSize;
            focus.Set(Vector3.zero);
            transform.rotation = Quaternion.Euler(58f, 45f, 0f);
            UpdateTransform(Vector3.zero);
        }

        private void Start()
        {
            if (office == null) office = FindFirstObjectByType<OfficeDirector>();
            if (cameraComponent == null) Initialize(office);
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > .1f)
                    targetSize = Mathf.Clamp(targetSize - scroll * profile.zoomSensitivity, profile.closeSize, profile.overviewSize);
                if (mouse.middleButton.isPressed)
                {
                    Vector2 delta = mouse.delta.ReadValue();
                    follow.Stop();
                    Vector3 right = transform.right; right.y = 0f; right.Normalize();
                    Vector3 forward = Vector3.Cross(right, Vector3.up).normalized;
                    focus.Add((-right * delta.x - forward * delta.y) * (profile.panSensitivity * targetSize));
                    focus.Set(new Vector3(Mathf.Clamp(focus.Target.x, -11f, 11f), 0f, Mathf.Clamp(focus.Target.z, -8f, 8f)));
                }
                if (mouse.leftButton.wasPressedThisFrame && !PointerOverUI()) SelectUnderPointer(mouse.position.ReadValue());
            }
            if (keyboard != null)
            {
                if (keyboard.fKey.wasPressedThisFrame && WorkerSelection.Selected != null)
                {
                    follow.Follow(WorkerSelection.Selected);
                    targetSize = Mathf.Min(targetSize, 6.2f);
                }
                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    if (follow.Active) follow.Stop();
                    else WorkerSelection.Clear();
                }
            }
            Vector3 desired = follow.Active && follow.Target != null ? follow.Target.transform.position : focus.Target;
            if (follow.Active) focus.Set(desired);
            Vector3 smoothed = Vector3.SmoothDamp(CurrentPivot(), desired, ref focus.Velocity, profile.smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            cameraComponent.orthographicSize = Mathf.SmoothDamp(cameraComponent.orthographicSize, targetSize, ref zoomVelocity, .12f, Mathf.Infinity, Time.unscaledDeltaTime);
            UpdateTransform(smoothed);
        }

        private void SelectUnderPointer(Vector2 screenPosition)
        {
            Ray ray = cameraComponent.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 120f)) { WorkerSelection.Clear(); return; }
            WorkerAgent worker = hit.collider.GetComponentInParent<WorkerAgent>();
            if (worker != null)
            {
                bool doubleClick = worker == lastClicked && Time.unscaledTime - lastClickTime < .34f;
                WorkerSelection.Select(worker);
                if (doubleClick)
                {
                    focus.Set(worker.transform.position);
                    targetSize = 5.4f;
                }
                lastClicked = worker;
                lastClickTime = Time.unscaledTime;
                return;
            }
            Workstation desk = hit.collider.GetComponentInParent<Workstation>();
            if (desk != null && office != null && office.Reassigning) office.ReassignSelected(desk);
        }

        public void FocusWorker(WorkerAgent worker, bool followWorker)
        {
            if (worker == null) return;
            focus.Set(worker.transform.position);
            targetSize = 5.4f;
            if (followWorker) follow.Follow(worker); else follow.Stop();
        }

        public void Overview()
        {
            follow.Stop();
            focus.Set(Vector3.zero);
            targetSize = profile.overviewSize;
        }

        public void FocusPoint(Vector3 point, float size)
        {
            follow.Stop();
            focus.Set(point);
            targetSize = Mathf.Clamp(size, profile.closeSize, profile.overviewSize);
        }

        private Vector3 CurrentPivot() => transform.position + transform.forward * 38f;

        private void UpdateTransform(Vector3 pivot)
        {
            transform.position = pivot - transform.forward * 38f;
        }

        private static bool PointerOverUI() => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
