using UnityEngine;

namespace OpenPlan
{
    /// <summary>Base component for a destination that translates placement into a clear worker activity.</summary>
    public class PlacementZone : MonoBehaviour
    {
        public PlacementActivity Activity { get; private set; }
        public string ActivityLabel { get; private set; }
        public Transform PlacementPoint { get; private set; }

        public virtual void Configure(PlacementActivity activity, Vector3 localPlacementPoint, string label = null)
        {
            Activity = activity;
            ActivityLabel = string.IsNullOrWhiteSpace(label) ? Pretty(activity) : label;
            GameObject point = new GameObject(activity + "PlacementPoint");
            point.transform.SetParent(transform, false);
            point.transform.localPosition = localPlacementPoint;
            PlacementPoint = point.transform;
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
    }

    public sealed class ActivityPlacementZone : PlacementZone { }
}
