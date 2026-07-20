using TMPro;
using UnityEngine;

namespace OpenPlan
{
    public sealed class WorkerVisuals : MonoBehaviour
    {
        public static bool GlobalNameTagsVisible { get; private set; } = true;

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
        private Quaternion headBase;
        private LineRenderer selectionRing;
        private TextMeshPro stateIcon;
        private TextMeshPro nameTag;
        private float phase;
        private bool selected;
        private bool hovered;
        private bool carried;
        private bool tutorialHighlighted;
        private string transientEmote;
        private float transientEmoteUntil;

        // Rigged-character path (e.g. the Stickman). When a valid humanoid Animator is
        // present on the visual model we drive it by state instead of rotating named
        // body-part transforms. The legacy code-generated worker (no Animator) keeps the
        // procedural path in TickProcedural.
        private Animator animator;
        private int currentStateHash;
        private static readonly int WalkHash = Animator.StringToHash("Walk");
        private static readonly int IdleHash = Animator.StringToHash("Idle");
        private static readonly int SittingHash = Animator.StringToHash("Sitting");
        private bool AnimatorMode => animator != null;

        public string CurrentEmote => transientEmoteUntil > Time.time ? transientEmote : string.Empty;
        public Transform NameTagTransform => nameTag == null ? null : nameTag.transform;
        public Transform EmoteTransform => stateIcon == null ? null : stateIcon.transform;
        public string NameTagText => nameTag == null ? string.Empty : nameTag.text;
        public float NameTagAlpha => nameTag == null ? 0f : nameTag.color.a;
        public float NameTagScale => nameTag == null ? 0f : nameTag.transform.localScale.x;
        public bool IsEmoteVisible => stateIcon != null && stateIcon.gameObject.activeSelf;

        public void Initialize(string workerName, Color clothing, Material ringMaterial)
        {
            visualRoot = FindDeep(transform, "Worker_Visual");
            if (visualRoot != null) visualRootBase = visualRoot.localPosition;

            // Prefer a valid humanoid Animator on the visual model (rigged path).
            animator = GetComponentInChildren<Animator>(true);
            if (animator != null && (animator.avatar == null || !animator.avatar.isValid))
                animator = null;
            if (animator != null)
            {
                animator.applyRootMotion = false; // gameplay drives position/rotation
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            }

            body = FindDeep(transform, "Body");
            head = FindDeep(transform, "Head");
            armL = FindDeep(transform, "Arm_L");
            armR = FindDeep(transform, "Arm_R");
            legL = FindDeep(transform, "Leg_L");
            legR = FindDeep(transform, "Leg_R");
            if (armL != null) armLBase = armL.localRotation;
            if (armR != null) armRBase = armR.localRotation;
            if (head != null) headBase = head.localRotation;

            ApplyClothing(clothing);
            BuildSelectionRing(ringMaterial);
            BuildNameTag(workerName);
            BuildStateIcon();
        }

        private void ApplyClothing(Color clothing)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            {
                if (renderer is LineRenderer) continue; // our own selection ring
                // Rigged model: one skinned mesh -> tint the whole figure. Legacy model:
                // tint only the named shirt/arm parts.
                bool tint = AnimatorMode ? renderer is SkinnedMeshRenderer
                                         : (renderer.name.Contains("Body") || renderer.name.Contains("Arm"));
                if (!tint) continue;
                renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor", clothing);
                block.SetColor("_Color", clothing);
                renderer.SetPropertyBlock(block);
            }
        }

        public static void SetGlobalNameTagsVisible(bool visible) => GlobalNameTagsVisible = visible;

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

        private void BuildNameTag(string workerName)
        {
            GameObject label = new GameObject("WorkerNameTag");
            label.transform.SetParent(transform, false);
            nameTag = label.AddComponent<TextMeshPro>();
            nameTag.font = OfficeUIFactory.EnsureFont();
            nameTag.alignment = TextAlignmentOptions.Center;
            nameTag.fontStyle = FontStyles.Bold;
            nameTag.fontSize = 3.0f;
            nameTag.color = new Color(1f, .94f, .78f, 1f);
            nameTag.outlineColor = new Color(.04f,.02f,.015f,1f);
            nameTag.outlineWidth = .22f;
            nameTag.text = SafeText(workerName, "WORKER");
            nameTag.rectTransform.sizeDelta = new Vector2(3.2f, .55f);
        }

        private void BuildStateIcon()
        {
            GameObject label = new GameObject("StateIcon");
            label.transform.SetParent(transform, false);
            stateIcon = label.AddComponent<TextMeshPro>();
            stateIcon.font = OfficeUIFactory.EnsureFont();
            stateIcon.alignment = TextAlignmentOptions.Center;
            stateIcon.fontStyle = FontStyles.Bold;
            stateIcon.fontSize = 3.5f;
            stateIcon.color = new Color(1f, 0.86f, 0.58f);
            stateIcon.outlineColor = new Color(.04f,.02f,.015f,1f);
            stateIcon.outlineWidth = .24f;
            stateIcon.text = string.Empty;
            stateIcon.rectTransform.sizeDelta = new Vector2(2.8f, 0.55f);
            stateIcon.gameObject.SetActive(false);
        }

        public void SetSelected(bool value)
        {
            selected = value;
            RefreshInteractionVisual();
        }

        public void SetHovered(bool value)
        {
            hovered = value;
            RefreshInteractionVisual();
        }

        public void SetCarried(bool value)
        {
            carried = value;
            RefreshInteractionVisual();
        }

        public void SetTutorialHighlighted(bool value)
        {
            tutorialHighlighted = value;
            RefreshInteractionVisual();
        }

        public void ShowEmote(string text, float duration)
        {
            transientEmote = SafeText(text, "!");
            transientEmoteUntil = Time.time + Mathf.Max(.1f, duration);
        }

        public void ShowEmote(StatusEmote emote, float duration) => ShowEmote(TextFor(emote), duration);

        public static string TextFor(StatusEmote emote)
        {
            switch (emote)
            {
                case StatusEmote.Happy: return "HAPPY";
                case StatusEmote.Sad: return "SAD";
                case StatusEmote.Frustrated: return "!";
                case StatusEmote.Tired: return "Zzz";
                case StatusEmote.Water: return "H2O";
                case StatusEmote.Snack: return "SNACK";
                case StatusEmote.Cigarette: return "SMOKE";
                case StatusEmote.Money: return "$";
                case StatusEmote.Question: return "?";
                case StatusEmote.Exclamation: return "!";
                case StatusEmote.Social: return "...";
                case StatusEmote.Focus: return "FOCUS";
                case StatusEmote.Restroom: return "WC";
                default: return "!";
            }
        }

        public static string SafeText(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            for (int i = 0; i < value.Length; i++)
                if (value[i] < 32 || value[i] > 126) return fallback;
            return value;
        }

        private void RefreshInteractionVisual()
        {
            if (selectionRing == null) return;
            selectionRing.enabled = selected || hovered || carried || tutorialHighlighted;
            Color color = carried ? new Color(.24f, .96f, .82f) :
                tutorialHighlighted ? new Color(1f,.58f,.18f) :
                hovered ? new Color(1f, .72f, .24f) : new Color(.30f, .88f, .78f);
            selectionRing.startColor = color;
            selectionRing.endColor = color;
            selectionRing.widthMultiplier = carried ? .085f : tutorialHighlighted ? .072f : hovered ? .060f : .035f;
            selectionRing.transform.localScale = carried ? Vector3.one * 1.22f :
                tutorialHighlighted ? Vector3.one * (1.12f + Mathf.Sin(Time.unscaledTime * 4f) * .05f) :
                hovered ? Vector3.one * 1.10f : Vector3.one;
        }

        public void Tick(WorkerState state, bool moving, float productivity)
        {
            if (AnimatorMode) TickAnimator(state, moving);
            else TickProcedural(state, moving);

            if (stateIcon != null)
            {
                bool showing = transientEmoteUntil > Time.time;
                stateIcon.gameObject.SetActive(showing);
                stateIcon.text = showing ? transientEmote : string.Empty;
                stateIcon.color = productivity > 1.15f ? new Color(0.50f, 0.92f, 0.55f) :
                    productivity < 0.65f ? new Color(0.95f, 0.38f, 0.30f) : new Color(1f, 0.84f, 0.52f);
            }
            PositionLabels(Camera.main);
        }

        // Rigged-character animation: drive the Animator by state. Sitting reads well at
        // desks; Walk while moving; Idle otherwise. The rigged model has no named
        // body-part transforms, so the procedural pass is skipped for it.
        private void TickAnimator(WorkerState state, bool moving)
        {
            int target;
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
                    case WorkerState.TakeBreak:
                    case WorkerState.Sleep:
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
            float activityOffset = state == WorkerState.TakeBreak || state == WorkerState.Sleep ? -.32f :
                state == WorkerState.Work ? -.20f : 0f;
            if (visualRoot != null) visualRoot.localPosition = visualRootBase + Vector3.up * (bob + activityOffset);

            if (head != null)
                head.localRotation = state == WorkerState.Sleep ? headBase * Quaternion.Euler(24f, 0f, 8f) :
                    Quaternion.Slerp(head.localRotation, headBase, Time.deltaTime * 6f);

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
                    case WorkerState.UseRestroom:
                        armR.localRotation = armRBase * Quaternion.Euler(78f, 0f, -20f);
                        armL.localRotation = armLBase;
                        break;
                    case WorkerState.TakeBreak:
                        armL.localRotation = armLBase * Quaternion.Euler(-12f + gesture * 10f, 0f, 34f);
                        armR.localRotation = armRBase * Quaternion.Euler(-8f - gesture * 10f, 0f, -34f);
                        break;
                    case WorkerState.BuySnack:
                        armR.localRotation = armRBase * Quaternion.Euler(48f + gesture * 20f, 0f, -28f);
                        armL.localRotation = armLBase * Quaternion.Euler(18f, 0f, 18f);
                        break;
                    case WorkerState.Smoke:
                        armR.localRotation = armRBase * Quaternion.Euler(88f + gesture * 6f, 0f, -34f);
                        armL.localRotation = armLBase * Quaternion.Euler(8f, 0f, 18f);
                        break;
                    case WorkerState.LookAtPhone:
                        armL.localRotation = armLBase * Quaternion.Euler(70f, 8f, 18f);
                        armR.localRotation = armRBase * Quaternion.Euler(72f, -8f, -18f);
                        break;
                    case WorkerState.StandConfused:
                        armL.localRotation = armLBase * Quaternion.Euler(22f + gesture * 8f, 0f, 55f);
                        armR.localRotation = armRBase * Quaternion.Euler(22f - gesture * 8f, 0f, -55f);
                        break;
                    case WorkerState.Sleep:
                        armL.localRotation = armLBase * Quaternion.Euler(-18f, 0f, 26f);
                        armR.localRotation = armRBase * Quaternion.Euler(-18f, 0f, -26f);
                        break;
                    case WorkerState.CarryBox:
                        armL.localRotation = armLBase * Quaternion.Euler(72f, 0f, 24f);
                        armR.localRotation = armRBase * Quaternion.Euler(72f, 0f, -24f);
                        break;
                    case WorkerState.Unassigned:
                        armL.localRotation = armLBase * Quaternion.Euler(6f, 0f, 12f);
                        armR.localRotation = armRBase * Quaternion.Euler(6f, 0f, -12f);
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
            else if (legL != null && legR != null)
            {
                legL.localRotation = Quaternion.Slerp(legL.localRotation, Quaternion.identity, Time.deltaTime * 8f);
                legR.localRotation = Quaternion.Slerp(legR.localRotation, Quaternion.identity, Time.deltaTime * 8f);
            }
        }

        public void ComputeNameTagPresentation(float orthographicSize)
        {
            if (nameTag == null) return;
            float overview = Mathf.InverseLerp(8f, 13.5f, orthographicSize);
            float scale = Mathf.Lerp(1f, .70f, overview);
            float alpha = GlobalNameTagsVisible ? Mathf.Lerp(1f, .30f, overview) : 0f;
            nameTag.transform.localScale = Vector3.one * scale;
            Color color = nameTag.color;
            color.a = alpha;
            nameTag.color = color;
            nameTag.gameObject.SetActive(GlobalNameTagsVisible);
        }

        private void PositionLabels(Camera camera)
        {
            Vector3 anchor = head != null ? head.position : transform.position + Vector3.up * 1.55f;
            if (nameTag != null)
            {
                nameTag.transform.position = anchor + Vector3.up * .50f;
                if (camera != null) nameTag.transform.rotation = camera.transform.rotation;
                ComputeNameTagPresentation(camera != null && camera.orthographic ? camera.orthographicSize : 7f);
            }
            if (stateIcon != null)
            {
                stateIcon.transform.position = anchor + Vector3.up * 1.20f;
                if (camera != null) stateIcon.transform.rotation = camera.transform.rotation;
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
