using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace OpenPlan
{
    public enum PlacementZoneVisualState { None, Valid, Invalid, HoveredValid, HoveredInvalid }

    /// <summary>Base component for a destination that translates placement into a clear worker activity.</summary>
    public class PlacementZone : MonoBehaviour
    {
        public PlacementActivity Activity { get; private set; }
        public string ActivityLabel { get; private set; }
        public string StableIdentifier { get; private set; }
        public Transform PlacementPoint { get; private set; }
        public BoxCollider FootprintCollider { get; private set; }
        public bool IsZoneEnabled { get; private set; }
        public bool IsHighlighted { get; private set; }
        public PlacementZoneVisualState CarryVisualState { get; private set; }
        public int Capacity { get; private set; }
        public int Occupancy => occupants.Count;
        public Bounds FootprintBounds => FootprintCollider != null ? FootprintCollider.bounds : new Bounds(transform.position, Vector3.zero);
        public string AvailabilityLabel => carryStateText == null ? string.Empty : carryStateText.text;
        public bool IsTutorialHighlighted { get; private set; }

        private readonly HashSet<WorkerAgent> occupants = new HashSet<WorkerAgent>();
        private readonly List<WorkerAgent> occupantOrder = new List<WorkerAgent>();
        private Renderer[] renderers;
        private MeshRenderer footprintRenderer;
        private Material footprintMaterial;
        private Mesh footprintMesh;
        private TextMeshPro carryStateText;
        private string unavailableReason;

        public virtual void Configure(PlacementActivity activity, Vector3 localPlacementPoint, string label = null,
            string stableIdentifier = null, bool zoneEnabled = true, Vector2 footprint = default, int capacity = 1)
        {
            Activity = activity;
            ActivityLabel = string.IsNullOrWhiteSpace(label) ? Pretty(activity) : label;
            StableIdentifier = string.IsNullOrWhiteSpace(stableIdentifier)
                ? $"{activity.ToString().ToLowerInvariant()}.{Sanitize(gameObject.name)}"
                : stableIdentifier;
            IsZoneEnabled = zoneEnabled;
            Capacity = Mathf.Max(1, capacity);
            GameObject point = new GameObject(activity + "PlacementPoint");
            point.transform.SetParent(transform, false);
            point.transform.localPosition = localPlacementPoint;
            PlacementPoint = point.transform;

            GameObject footprintObject = new GameObject(activity + "PlacementFootprint");
            footprintObject.transform.SetParent(transform, false);
            footprintObject.transform.localPosition = localPlacementPoint;
            FootprintCollider = footprintObject.AddComponent<BoxCollider>();
            FootprintCollider.isTrigger = true;
            FootprintCollider.center = new Vector3(0f, .06f, 0f);
            Vector2 size = footprint == default ? new Vector2(1.2f, 1.2f) : footprint;
            FootprintCollider.size = new Vector3(Mathf.Max(.2f, size.x), .12f, Mathf.Max(.2f, size.y));
            renderers = GetComponentsInChildren<Renderer>(true);
            BuildFootprintVisual(footprintObject.transform, size);
            BuildCarryStateLabel(localPlacementPoint);
        }

        public virtual bool CanAcceptWorker(WorkerAgent worker, out string reason)
        {
            if (worker == null) { reason = "A worker is required."; return false; }
            if (worker.IsFired || worker.IsLeavingCompany) { reason = "Worker is leaving the company."; return false; }
            if (!IsZoneEnabled) { reason = string.IsNullOrWhiteSpace(unavailableReason) ? ActivityLabel + " is locked." : unavailableReason; return false; }
            if (worker.Runtime != null && !worker.CanAcceptPlacementActivity(Activity, out reason)) return false;
            if (occupants.Contains(worker)) { reason = null; return true; }
            if (occupants.Count >= Capacity) { reason = ActivityLabel + " is occupied."; return false; }
            reason = null;
            return true;
        }

        public bool TryOccupy(WorkerAgent worker, out string reason)
        {
            if (!CanAcceptWorker(worker, out reason)) return false;
            if (occupants.Add(worker)) occupantOrder.Add(worker);
            return true;
        }

        public Vector3 PositionFor(WorkerAgent worker)
        {
            if (PlacementPoint == null) return transform.position;
            int index = occupantOrder.IndexOf(worker);
            if (index <= 0 || Capacity <= 1) return PlacementPoint.position;
            int side = index % 2 == 1 ? 1 : -1;
            int row = (index + 1) / 2;
            return PlacementPoint.position + transform.right * side * row * .72f;
        }

        public void Vacate(WorkerAgent worker)
        {
            if (worker == null) return;
            occupants.Remove(worker);
            occupantOrder.Remove(worker);
        }

        public void SetZoneEnabled(bool value)
        {
            IsZoneEnabled = value;
            if (!value) SetHighlight(false, Color.black);
        }

        public void SetUnavailableReason(string reason) => unavailableReason = reason;

        public void SetTutorialHighlight(bool value)
        {
            IsTutorialHighlighted = value;
            if (carryStateText != null)
            {
                carryStateText.text = value ? "TUTORIAL  " + ActivityLabel.ToUpperInvariant() : string.Empty;
                carryStateText.color = new Color(.38f, 1f, .88f);
                carryStateText.gameObject.SetActive(value || CarryVisualState != PlacementZoneVisualState.None);
            }
            SetHighlight(value, OfficeUIFactory.Teal);
        }

        public virtual void SetHighlight(bool value, Color color)
        {
            IsHighlighted = value;
            if (renderers == null) return;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;
                renderer.GetPropertyBlock(block);
                block.SetColor("_EmissionColor", value ? color * 1.4f : Color.black);
                renderer.SetPropertyBlock(block);
            }
        }

        public void SetCarryVisualState(PlacementZoneVisualState state)
        {
            CarryVisualState = state;
            if (state == PlacementZoneVisualState.None)
            {
                if (footprintRenderer != null) footprintRenderer.enabled = false;
                if (carryStateText != null)
                {
                    carryStateText.text = IsTutorialHighlighted ? "TUTORIAL  " + ActivityLabel.ToUpperInvariant() : string.Empty;
                    carryStateText.gameObject.SetActive(IsTutorialHighlighted);
                }
                SetHighlight(IsTutorialHighlighted, OfficeUIFactory.Teal);
                return;
            }

            bool valid = state == PlacementZoneVisualState.Valid || state == PlacementZoneVisualState.HoveredValid;
            bool hovered = state == PlacementZoneVisualState.HoveredValid || state == PlacementZoneVisualState.HoveredInvalid;
            Color color = valid ? new Color(.12f,.92f,.62f, hovered ? .52f : .24f) :
                new Color(1f,.24f,.14f, hovered ? .52f : .22f);
            if (footprintRenderer != null)
            {
                footprintRenderer.enabled = true;
                footprintMaterial.color = color;
            }
            if (carryStateText != null)
            {
                carryStateText.text = valid ? "✓ VALID  " + ActivityLabel.ToUpperInvariant() :
                    !IsZoneEnabled ? "× UNAVAILABLE" : Occupancy >= Capacity ? "× OCCUPIED" : "× NOT AVAILABLE";
                carryStateText.color = valid ? new Color(.38f,1f,.72f) : new Color(1f,.52f,.34f);
                carryStateText.gameObject.SetActive(true);
            }
            SetHighlight(true, new Color(color.r, color.g, color.b, 1f) * (hovered ? 1.15f : .72f));
        }

        private void BuildCarryStateLabel(Vector3 localPlacementPoint)
        {
            GameObject label = new GameObject("Placement Availability Label");
            label.transform.SetParent(transform, false);
            label.transform.localPosition = localPlacementPoint + Vector3.up * .42f;
            label.transform.localScale = Vector3.one * .32f;
            carryStateText = label.AddComponent<TextMeshPro>();
            carryStateText.font = OfficeUIFactory.EnsureFont();
            carryStateText.fontSize = 3.1f;
            carryStateText.fontStyle = FontStyles.Bold;
            carryStateText.alignment = TextAlignmentOptions.Center;
            carryStateText.rectTransform.sizeDelta = new Vector2(6.5f, 1.0f);
            if (Application.isPlaying)
            {
                carryStateText.outlineColor = new Color(.05f,.025f,.02f,1f);
                carryStateText.outlineWidth = .20f;
            }
            label.AddComponent<WorldSpaceOfficeLabel>();
            label.SetActive(false);
        }

        private void BuildFootprintVisual(Transform parent, Vector2 size)
        {
            GameObject visual = new GameObject("Placement Feedback Footprint");
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = new Vector3(0f,.075f,0f);
            footprintMesh = new Mesh { name = StableIdentifier + " Footprint" };
            float x = Mathf.Max(.2f,size.x) * .5f;
            float z = Mathf.Max(.2f,size.y) * .5f;
            footprintMesh.vertices = new[]
            {
                new Vector3(-x,0f,-z), new Vector3(-x,0f,z),
                new Vector3(x,0f,z), new Vector3(x,0f,-z)
            };
            footprintMesh.triangles = new[] { 0,1,2, 0,2,3 };
            footprintMesh.RecalculateNormals();
            visual.AddComponent<MeshFilter>().sharedMesh = footprintMesh;
            footprintRenderer = visual.AddComponent<MeshRenderer>();
            Shader shader = Shader.Find("Sprites/Default");
            footprintMaterial = new Material(shader) { name = StableIdentifier + " Placement Feedback" };
            footprintMaterial.color = Color.clear;
            footprintMaterial.renderQueue = 3100;
            footprintRenderer.sharedMaterial = footprintMaterial;
            footprintRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            footprintRenderer.receiveShadows = false;
            footprintRenderer.enabled = false;
        }

        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                if (footprintMaterial != null) Destroy(footprintMaterial);
                if (footprintMesh != null) Destroy(footprintMesh);
            }
            else
            {
                if (footprintMaterial != null) DestroyImmediate(footprintMaterial);
                if (footprintMesh != null) DestroyImmediate(footprintMesh);
            }
        }

        private static string Pretty(PlacementActivity activity)
        {
            switch (activity)
            {
                case PlacementActivity.GetWater: return "Get Water";
                case PlacementActivity.BuySnack: return "Buy Snack";
                case PlacementActivity.LeaveOffice: return "Leave Office";
                default: return activity.ToString();
            }
        }

        private static string Sanitize(string value)
        {
            char[] characters = (value ?? string.Empty).ToLowerInvariant().ToCharArray();
            for (int i = 0; i < characters.Length; i++)
                if (!char.IsLetterOrDigit(characters[i])) characters[i] = '-';
            return new string(characters).Trim('-');
        }
    }

    public sealed class ActivityPlacementZone : PlacementZone { }
}
