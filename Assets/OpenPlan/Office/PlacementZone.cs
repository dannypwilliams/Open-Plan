using System.Collections.Generic;
using UnityEngine;

namespace OpenPlan
{
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
        public int Capacity { get; private set; }
        public int Occupancy => occupants.Count;
        public Bounds FootprintBounds => FootprintCollider != null ? FootprintCollider.bounds : new Bounds(transform.position, Vector3.zero);

        private readonly HashSet<WorkerAgent> occupants = new HashSet<WorkerAgent>();
        private Renderer[] renderers;

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
        }

        public virtual bool CanAcceptWorker(WorkerAgent worker, out string reason)
        {
            if (worker == null) { reason = "A worker is required."; return false; }
            if (!IsZoneEnabled) { reason = ActivityLabel + " is locked."; return false; }
            if (occupants.Contains(worker)) { reason = null; return true; }
            if (occupants.Count >= Capacity) { reason = ActivityLabel + " is occupied."; return false; }
            reason = null;
            return true;
        }

        public bool TryOccupy(WorkerAgent worker, out string reason)
        {
            if (!CanAcceptWorker(worker, out reason)) return false;
            occupants.Add(worker);
            return true;
        }

        public void Vacate(WorkerAgent worker)
        {
            if (worker != null) occupants.Remove(worker);
        }

        public void SetZoneEnabled(bool value)
        {
            IsZoneEnabled = value;
            if (!value) SetHighlight(false, Color.black);
        }

        public virtual void SetHighlight(bool value, Color color)
        {
            IsHighlighted = value;
            if (renderers == null) return;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            foreach (Renderer renderer in renderers)
            {
                renderer.GetPropertyBlock(block);
                block.SetColor("_EmissionColor", value ? color * 1.4f : Color.black);
                renderer.SetPropertyBlock(block);
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
