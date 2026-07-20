using UnityEngine;
using UnityEngine.InputSystem;

namespace OpenPlan
{
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
        private Vector3 overviewCenter;

        public float OrthographicSize => cameraComponent != null ? cameraComponent.orthographicSize : targetSize;
        public float TargetOrthographicSize => targetSize;
        public bool IsFollowing => follow.Active;
        public Vector3 OverviewCenter => overviewCenter;
        public Bounds PanBounds => office != null && office.Layout != null ? office.Layout.PanBounds : new Bounds(Vector3.zero, new Vector3(22f, 1f, 16f));

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
            targetSize = director != null && director.Layout != null ? director.Layout.OverviewOrthographicSize : profile.overviewSize;
            overviewCenter = director != null && director.Layout != null ? director.Layout.OverviewCenter : Vector3.zero;
            cameraComponent.orthographicSize = targetSize;
            focus.Set(overviewCenter);
            transform.rotation = Quaternion.Euler(58f, 45f, 0f);
            UpdateTransform(overviewCenter);
        }

        private void Start()
        {
            if (office == null) office = FindFirstObjectByType<OfficeDirector>();
            if (cameraComponent == null) Initialize(office);
        }

        private void Update()
        {
            if (office != null && office.WorldInputBlocked)
            {
                UpdateTransform(CurrentPivot());
                return;
            }
            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > .0001f) ApplyZoom(scroll);
                if (mouse.middleButton.isPressed && (office == null || office.CarryController == null || !office.CarryController.BlocksWorldInput))
                {
                    Vector2 delta = mouse.delta.ReadValue();
                    follow.Stop();
                    Vector3 right = transform.right; right.y = 0f; right.Normalize();
                    Vector3 forward = Vector3.Cross(right, Vector3.up).normalized;
                    focus.Add((-right * delta.x - forward * delta.y) * (profile.panSensitivity * targetSize));
                    Bounds bounds = PanBounds;
                    focus.Set(new Vector3(Mathf.Clamp(focus.Target.x, bounds.min.x, bounds.max.x), 0f,
                        Mathf.Clamp(focus.Target.z, bounds.min.z, bounds.max.z)));
                }
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
                    bool carryOwnsInput = office != null && office.CarryController != null && office.CarryController.BlocksWorldInput;
                    if (!carryOwnsInput)
                    {
                        if (follow.Active) follow.Stop();
                        else WorkerSelection.Clear();
                    }
                }
            }
            Vector3 desired = follow.Active && follow.Target != null ? follow.Target.transform.position : focus.Target;
            if (follow.Active) focus.Set(desired);
            Vector3 smoothed = Vector3.SmoothDamp(CurrentPivot(), desired, ref focus.Velocity, profile.smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            cameraComponent.orthographicSize = Mathf.SmoothDamp(cameraComponent.orthographicSize, targetSize, ref zoomVelocity, .12f, Mathf.Infinity, Time.unscaledDeltaTime);
            UpdateTransform(smoothed);
        }

        public void HandleWorldClick(Vector2 screenPosition)
        {
            if (office != null && office.WorldInputBlocked) return;
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
            focus.Set(overviewCenter);
            targetSize = OverviewSize();
        }

        public void ApplyLayoutChange(bool showOverview)
        {
            if (office == null || office.Layout == null) return;
            overviewCenter = office.Layout.OverviewCenter;
            if (showOverview) Overview();
            else
            {
                focus.Set(overviewCenter);
                targetSize = OverviewSize();
                cameraComponent.orthographicSize = targetSize;
                UpdateTransform(overviewCenter);
            }
        }

        public void FocusPoint(Vector3 point, float size)
        {
            follow.Stop();
            focus.Set(point);
            targetSize = Mathf.Clamp(size, profile.closeSize, OverviewSize());
        }

        private float OverviewSize() => office != null && office.Layout != null ? office.Layout.OverviewOrthographicSize : profile.overviewSize;

        public void ApplyZoom(float scrollY)
        {
            targetSize = ApplyZoomInput(targetSize, scrollY, profile.closeSize, OverviewSize(), profile.zoomSensitivity);
        }

        public static float ApplyZoomInput(float currentSize, float scrollY, float closeSize, float overviewSize, float sensitivity)
        {
            if (Mathf.Abs(scrollY) <= .0001f) return Mathf.Clamp(currentSize, closeSize, overviewSize);
            float notches = scrollY / 120f;
            float factor = Mathf.Pow(1f - Mathf.Clamp(sensitivity, .01f, .45f), notches);
            return Mathf.Clamp(currentSize * factor, closeSize, overviewSize);
        }

        private Vector3 CurrentPivot() => transform.position + transform.forward * 38f;

        private void UpdateTransform(Vector3 pivot)
        {
            transform.position = pivot - transform.forward * 38f;
        }
    }
}
