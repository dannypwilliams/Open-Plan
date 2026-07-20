using UnityEngine;

namespace OpenPlan
{
    [CreateAssetMenu(menuName = "Open Plan/Camera Zoom Profile")]
    public sealed class CameraZoomProfile : ScriptableObject
    {
        public float closeSize = 4.8f;
        public float overviewSize = 18.5f;
        [Range(.05f, .25f)] public float zoomSensitivity = .13f;
        public float panSensitivity = 0.018f;
        public float smoothTime = 0.16f;
    }
}
