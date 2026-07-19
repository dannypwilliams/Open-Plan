using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace OpenPlan
{
    public static class OfficeUIFactory
    {
        public static readonly Color Ink = new Color(.10f, .065f, .055f);
        public static readonly Color Paper = new Color(.90f, .82f, .66f);
        public static readonly Color Burgundy = new Color(.30f, .075f, .105f);
        public static readonly Color DarkPanel = new Color(.055f, .038f, .035f, .92f);
        public static readonly Color Teal = new Color(.18f, .68f, .68f);
        public static readonly Color Orange = new Color(.94f, .33f, .12f);
        public static TMP_FontAsset FontAsset { get; private set; }

        public static Canvas CreateCanvas(string name)
        {
            EnsureFont();
            GameObject canvasObject = new GameObject(name);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = .5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();
            return canvas;
        }

        public static TMP_FontAsset EnsureFont()
        {
            if (FontAsset != null) return FontAsset;
            Font font = Resources.Load<Font>("Fonts/Inter-Regular");
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            FontAsset = TMP_FontAsset.CreateFontAsset(font, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, TMPro.AtlasPopulationMode.Dynamic, true);
            if (FontAsset == null) throw new System.InvalidOperationException("OPEN PLAN could not create its bundled Inter TMP font asset.");
            FontAsset.name = "Open Plan Runtime Font";
            return FontAsset;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            GameObject eventObject = new GameObject("EventSystem");
            eventObject.AddComponent<EventSystem>();
            InputSystemUIInputModule module = eventObject.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        public static RectTransform Panel(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            Image image = go.GetComponent<Image>();
            image.color = color;
            return rect;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, string value, float size, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.font = FontAsset;
            text.fontSize = size;
            text.color = color;
            text.text = value;
            text.alignment = alignment;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            return text;
        }

        public static Button Button(Transform parent, string name, string label, Color background, Color foreground,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            RectTransform rect = Panel(parent, name, background, anchorMin, anchorMax, offsetMin, offsetMax);
            Button button = rect.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(background, Color.white, .18f);
            colors.pressedColor = Color.Lerp(background, Color.black, .18f);
            button.colors = colors;
            Text(rect, "Label", label, 22f, foreground, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            return button;
        }

        public static void SetVisible(Component component, bool visible)
        {
            if (component != null) component.gameObject.SetActive(visible);
        }
    }
}
