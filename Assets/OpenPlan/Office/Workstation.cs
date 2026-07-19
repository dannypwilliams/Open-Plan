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
        private Renderer[] renderers;

        public bool IsAvailable => Assigned == null;

        public void Configure(int index, float noise, float light, float modifier, string zone, bool expansion)
        {
            Index = index;
            Noise = Mathf.Clamp01(noise);
            Light = Mathf.Clamp01(light);
            Modifier = Mathf.Clamp(modifier, 0.88f, 1.12f);
            ZoneLabel = zone;
            IsExpansion = expansion;
            renderers = GetComponentsInChildren<Renderer>(true);
            BoxCollider clickCollider = gameObject.AddComponent<BoxCollider>();
            clickCollider.center = new Vector3(0f, 0.48f, 0f);
            clickCollider.size = new Vector3(1.85f, 1.0f, 1.05f);
            Configure(PlacementActivity.Work, new Vector3(0f, 0f, -0.95f), "Work");
            WorkPoint = PlacementPoint;
        }

        public void Assign(WorkerAgent worker)
        {
            if (Assigned == worker) return;
            if (Assigned != null) Assigned.SetDesk(null);
            Assigned = worker;
            if (worker != null) worker.SetDesk(this);
        }

        public void Release(WorkerAgent worker)
        {
            if (Assigned != worker) return;
            Assigned = null;
        }

        public void SetHighlight(bool value, Color color)
        {
            if (renderers == null) return;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].GetPropertyBlock(block);
                block.SetColor("_EmissionColor", value ? color * 1.4f : Color.black);
                renderers[i].SetPropertyBlock(block);
            }
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
