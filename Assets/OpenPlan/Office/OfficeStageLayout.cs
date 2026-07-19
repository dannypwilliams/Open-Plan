using System.Collections.Generic;
using UnityEngine;

namespace OpenPlan
{
    public sealed class OfficeStageLayout : MonoBehaviour
    {
        public Bounds OverviewBounds { get; private set; }
        public Bounds PanBounds { get; private set; }
        public Vector3 OverviewCenter => OverviewBounds.center;
        public float OverviewOrthographicSize { get; private set; }
        public IReadOnlyList<Transform> RequiredOverviewPoints => overviewPoints;
        public IReadOnlyList<OfficeObstacleVolume> Obstacles => obstacles;
        public IReadOnlyList<PrimaryRouteVolume> PrimaryRoutes => primaryRoutes;

        private readonly List<Transform> overviewPoints = new List<Transform>();
        private readonly List<OfficeObstacleVolume> obstacles = new List<OfficeObstacleVolume>();
        private readonly List<PrimaryRouteVolume> primaryRoutes = new List<PrimaryRouteVolume>();

        public void Configure(Bounds overviewBounds, Bounds panBounds, float overviewOrthographicSize)
        {
            OverviewBounds = overviewBounds;
            PanBounds = panBounds;
            OverviewOrthographicSize = overviewOrthographicSize;
        }

        public Transform AddOverviewPoint(string identifier, Vector3 worldPosition)
        {
            GameObject point = new GameObject("OverviewPoint_" + identifier);
            point.transform.SetParent(transform, false);
            point.transform.position = worldPosition;
            overviewPoints.Add(point.transform);
            return point.transform;
        }

        public void RegisterObstacle(OfficeObstacleVolume obstacle)
        {
            if (obstacle != null && !obstacles.Contains(obstacle)) obstacles.Add(obstacle);
        }

        public void RegisterPrimaryRoute(PrimaryRouteVolume route)
        {
            if (route != null && !primaryRoutes.Contains(route)) primaryRoutes.Add(route);
        }

        public bool CameraContainsRequiredSpaces(Camera camera, float margin = .015f)
        {
            if (camera == null) return false;
            foreach (Transform point in overviewPoints)
            {
                Vector3 viewport = camera.WorldToViewportPoint(point.position);
                if (viewport.z <= 0f || viewport.x < margin || viewport.x > 1f - margin ||
                    viewport.y < margin || viewport.y > 1f - margin)
                    return false;
            }
            return true;
        }

        public bool ValidateZoneGeometry(IEnumerable<PlacementZone> zones, out string reason)
        {
            foreach (PlacementZone zone in zones)
            {
                if (zone == null) continue;
                foreach (OfficeObstacleVolume obstacle in obstacles)
                {
                    if (obstacle.VolumeBounds.Intersects(zone.FootprintBounds))
                    {
                        reason = zone.StableIdentifier + " overlaps " + obstacle.StableIdentifier;
                        return false;
                    }
                }
                foreach (PrimaryRouteVolume route in primaryRoutes)
                {
                    if (route.VolumeBounds.Intersects(zone.FootprintBounds))
                    {
                        reason = zone.StableIdentifier + " blocks " + route.StableIdentifier;
                        return false;
                    }
                }
            }
            reason = null;
            return true;
        }
    }

    public abstract class OfficeLayoutVolume : MonoBehaviour
    {
        public string StableIdentifier { get; private set; }
        public BoxCollider VolumeCollider { get; private set; }
        public Bounds VolumeBounds => VolumeCollider != null ? VolumeCollider.bounds : new Bounds(transform.position, Vector3.zero);

        public void Configure(string stableIdentifier, Vector3 localCenter, Vector3 size)
        {
            StableIdentifier = stableIdentifier;
            VolumeCollider = gameObject.AddComponent<BoxCollider>();
            VolumeCollider.isTrigger = true;
            VolumeCollider.center = localCenter;
            VolumeCollider.size = size;
        }
    }

    public sealed class OfficeObstacleVolume : OfficeLayoutVolume { }
    public sealed class PrimaryRouteVolume : OfficeLayoutVolume { }

    public sealed class FutureDeskLocation : MonoBehaviour
    {
        public string StableIdentifier { get; private set; }
        public bool IsNeighboringUnit { get; private set; }
        public bool IsAvailable { get; private set; }

        public void Configure(string stableIdentifier, bool neighboringUnit, bool available)
        {
            StableIdentifier = stableIdentifier;
            IsNeighboringUnit = neighboringUnit;
            IsAvailable = available;
        }
    }
}
