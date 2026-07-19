using TMPro;
using UnityEngine;

namespace OpenPlan
{
    public sealed class WorkerVisuals : MonoBehaviour
    {
        private Transform body;
        private Transform head;
        private Transform armL;
        private Transform armR;
        private Transform legL;
        private Transform legR;
        private Transform visualRoot;
        private Vector3 visualRootBase;
        private Quaternion armLBase;
        private Quaternion armRBase;
        private LineRenderer selectionRing;
        private TextMeshPro stateIcon;
        private float phase;

        // Animator-driven visuals (e.g. the rigged Stickman). When a valid humanoid
        // Animator is present on the visual model we drive it by state instead of
        // rotating named body-part transforms. Falls back to the legacy procedural
        // path when no Animator exists (the original code-generated OP_Worker model).
        private Animator animator;
        private int currentStateHash;
        private static readonly int WalkHash = Animator.StringToHash("Walk");
        private static readonly int IdleHash = Animator.StringToHash("Idle");
        private static readonly int SittingHash = Animator.StringToHash("Sitting");
        private bool AnimatorMode => animator != null;

        public void Initialize(Color clothing, Material ringMaterial)
        {
            visualRoot = FindDeep(transform, "Worker_Visual");
            if (visualRoot != null) visualRootBase = visualRoot.localPosition;

            // Prefer an Animator on the visual model (rigged-character path).
            animator = GetComponentInChildren<Animator>(true);
            if (animator != null && (animator.avatar == null || !animator.avatar.isValid))
                animator = null; // invalid rig -> fall back to procedural path
            if (animator != null)
            {
                animator.applyRootMotion = false; // gameplay drives position/rotation
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            }

            // Legacy named body parts (present only on the old OP_Worker model).
            body = FindDeep(transform, "Body");
            head = FindDeep(transform, "Head");
            armL = FindDeep(transform, "Arm_L");
            armR = FindDeep(transform, "Arm_R");
            legL = FindDeep(transform, "Leg_L");
            legR = FindDeep(transform, "Leg_R");
            if (armL != null) armLBase = armL.localRotation;
            if (armR != null) armRBase = armR.localRotation;

            ApplyClothing(clothing);
            BuildSelectionRing(ringMaterial);
            BuildStateIcon();
        }

        private void ApplyClothing(Color clothing)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            {
                if (renderer is LineRenderer) continue; // skip our own selection ring
                bool isBodyPart = renderer.name.Contains("Body") || renderer.name.Contains("Arm");
                // Legacy model: tint only the named shirt/arm parts. Rigged model: a
                // single skinned mesh -> tint the whole figure with the clothing color.
                bool tint = AnimatorMode ? renderer is SkinnedMeshRenderer : isBodyPart;
                if (!tint) continue;
                renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor", clothing);
                block.SetColor("_Color", clothing);
                renderer.SetPropertyBlock(block);
            }
        }

        private void BuildSelectionRing(Material ringMaterial)
        {
            GameObject ring = new GameObject("SelectionRing");
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.035f, 0f);
            selectionRing = ring.AddComponent<LineRenderer>();
            selectionRing.useWorldSpace = false;
            selectionRing.loop = true;
            selectionRing.positionCount = 40;
            selectionRing.widthMultiplier = 0.035f;
            selectionRing.sharedMaterial = ringMaterial;
            for (int i = 0; i < 40; i++)
            {
                float a = i * Mathf.PI * 2f / 40f;
                selectionRing.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.55f, 0f, Mathf.Sin(a) * 0.55f));
            }
            selectionRing.enabled = false;
        }

        private void BuildStateIcon()
        {
            GameObject label = new GameObject("StateIcon");
            label.transform.SetParent(transform, false);
            label.transform.localPosition = new Vector3(0f, 2.15f, 0f);
            stateIcon = label.AddComponent<TextMeshPro>();
            stateIcon.font = OfficeUIFactory.EnsureFont();
            stateIcon.alignment = TextAlignmentOptions.Center;
            stateIcon.fontSize = 3.5f;
            stateIcon.color = new Color(1f, 0.86f, 0.58f);
            stateIcon.text = "●";
            stateIcon.rectTransform.sizeDelta = new Vector2(2f, 0.5f);
        }

        public void SetSelected(bool selected) => selectionRing.enabled = selected;

        public void Tick(WorkerState state, bool moving, float productivity)
        {
            if (AnimatorMode)
                TickAnimator(state, moving);
            else
                TickProcedural(state, moving);
            TickStateIcon(state, productivity);
        }

        private void TickAnimator(WorkerState state, bool moving)
        {
            int target = IdleHash;
            if (moving)
            {
                target = WalkHash;
            }
            else
            {
                switch (state)
                {
                    case WorkerState.Work:
                    case WorkerState.IdleAtDesk:
                    case WorkerState.Meeting:
                        target = SittingHash;
                        break;
                    default:
                        target = IdleHash;
                        break;
                }
            }
            if (target != currentStateHash)
            {
                currentStateHash = target;
                animator.CrossFadeInFixedTime(target, 0.18f, 0);
            }
        }

        private void TickProcedural(WorkerState state, bool moving)
        {
            phase += Time.deltaTime * (moving ? 8f : 4f);
            float bob = moving ? Mathf.Abs(Mathf.Sin(phase)) * 0.075f : Mathf.Sin(phase * 0.35f) * 0.015f;
            // The FBX child retains a 100x importer scale. Offset its root from the
            // unscaled gameplay wrapper so a 7.5 cm bob stays 7.5 cm in world space.
            if (visualRoot != null) visualRoot.localPosition = visualRootBase + Vector3.up * bob;

            float gesture = Mathf.Sin(phase * 1.7f);
            if (armL != null && armR != null)
            {
                switch (state)
                {
                    case WorkerState.Work:
                        armL.localRotation = armLBase * Quaternion.Euler(55f + gesture * 10f, 0f, 12f);
                        armR.localRotation = armRBase * Quaternion.Euler(55f - gesture * 10f, 0f, -12f);
                        break;
                    case WorkerState.Socialize:
                    case WorkerState.React:
                        armL.localRotation = armLBase * Quaternion.Euler(gesture * 45f, 0f, 28f);
                        armR.localRotation = armRBase * Quaternion.Euler(-gesture * 32f, 0f, -22f);
                        break;
                    case WorkerState.UseCoffeeMachine:
                    case WorkerState.UseWaterCooler:
                        armR.localRotation = armRBase * Quaternion.Euler(78f, 0f, -20f);
                        armL.localRotation = armLBase;
                        break;
                    case WorkerState.CarryBox:
                        armL.localRotation = armLBase * Quaternion.Euler(72f, 0f, 24f);
                        armR.localRotation = armRBase * Quaternion.Euler(72f, 0f, -24f);
                        break;
                    default:
                        armL.localRotation = Quaternion.Slerp(armL.localRotation, armLBase, Time.deltaTime * 8f);
                        armR.localRotation = Quaternion.Slerp(armR.localRotation, armRBase, Time.deltaTime * 8f);
                        break;
                }
            }
            if (moving && legL != null && legR != null)
            {
                legL.localRotation = Quaternion.Euler(gesture * 20f, 0f, 0f);
                legR.localRotation = Quaternion.Euler(-gesture * 20f, 0f, 0f);
            }
        }

        private void TickStateIcon(WorkerState state, float productivity)
        {
            if (stateIcon == null) return;
            stateIcon.text = IconFor(state);
            stateIcon.color = productivity > 1.15f ? new Color(0.50f, 0.92f, 0.55f) :
                productivity < 0.65f ? new Color(0.95f, 0.38f, 0.30f) : new Color(1f, 0.84f, 0.52f);
            Camera camera = Camera.main;
            if (camera != null) stateIcon.transform.rotation = camera.transform.rotation;
        }

        private static string IconFor(WorkerState state)
        {
            switch (state)
            {
                case WorkerState.Work: return "W";
                case WorkerState.SeekCoffee:
                case WorkerState.UseCoffeeMachine: return "C";
                case WorkerState.SeekWater:
                case WorkerState.UseWaterCooler: return "◆";
                case WorkerState.Socialize:
                case WorkerState.SeekCoworker: return "•••";
                case WorkerState.TakeBreak: return "Z";
                case WorkerState.FiredReaction: return "!";
                case WorkerState.PackDesk:
                case WorkerState.CarryBox: return "BOX";
                default: return "●";
            }
        }

        private static Transform FindDeep(Transform parent, string sought)
        {
            foreach (Transform child in parent)
            {
                if (child.name == sought || child.name.StartsWith(sought)) return child;
                Transform found = FindDeep(child, sought);
                if (found != null) return found;
            }
            return null;
        }
    }
}
