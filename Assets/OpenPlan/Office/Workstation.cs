using UnityEngine;

namespace OpenPlan
{
    public sealed class Workstation : PlacementZone
    {
        public int Index { get; private set; }
        public float Noise { get; private set; }
        public float Light { get; private set; }
        public float Modifier { get; private set; }
        public string ZoneLabel { get; private set; }
        public bool IsExpansion { get; private set; }
        public WorkerAgent Assigned { get; private set; }
        public Transform WorkPoint { get; private set; }

        public bool IsAvailable => IsZoneEnabled && Assigned == null;

        public void Configure(int index, float noise, float light, float modifier, string zone, bool expansion,
            string stableIdentifier = null, bool zoneEnabled = true)
        {
            Index = index;
            Noise = Mathf.Clamp01(noise);
            Light = Mathf.Clamp01(light);
            Modifier = Mathf.Clamp(modifier, 0.88f, 1.12f);
            ZoneLabel = zone;
            IsExpansion = expansion;
            BoxCollider clickCollider = gameObject.AddComponent<BoxCollider>();
            clickCollider.center = new Vector3(0f, 0.48f, 0f);
            clickCollider.size = new Vector3(1.85f, 1.0f, 1.05f);
            base.Configure(PlacementActivity.Work, new Vector3(0f, 0f, -0.95f), "Work",
                stableIdentifier, zoneEnabled, new Vector2(1.45f, 1.15f), 1);
            WorkPoint = PlacementPoint;
        }

        public void Assign(WorkerAgent worker)
        {
            if (Assigned == worker) return;
            if (worker != null && !TryOccupy(worker, out _)) return;
            if (Assigned != null) Assigned.SetDesk(null);
            if (Assigned != null) Vacate(Assigned);
            Assigned = worker;
            if (worker != null) worker.SetDesk(this);
        }

        public void Release(WorkerAgent worker)
        {
            if (Assigned != worker) return;
            Assigned = null;
            Vacate(worker);
        }
    }

    public class NeedStation : MonoBehaviour
    {
        public StationKind Kind { get; private set; }
        public Transform UsePoint { get; private set; }

        public void Configure(StationKind kind, Vector3 localUsePoint)
        {
            Kind = kind;
            GameObject point = new GameObject(kind + "UsePoint");
            point.transform.SetParent(transform, false);
            point.transform.localPosition = localUsePoint;
            UsePoint = point.transform;
        }
    }

    public sealed class CoffeeStation : NeedStation { }
    public sealed class WaterStation : NeedStation { }
    public sealed class MeetingStation : NeedStation { }
}
