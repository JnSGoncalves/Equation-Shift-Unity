using UnityEngine;
using UnityEngine.UI;

namespace EquationShift
{
    public sealed class VillageDialogue : MonoBehaviour
    {
        private static VillageDialogue instance;
        [SerializeField] private Font dialogueFont;
        [SerializeField, Min(1f)] private float displayDuration = 4f;

        private Canvas canvas;
        private Text label;
        private float hideAt;

        public static bool IsVisible => instance != null && instance.canvas.enabled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance() => instance = null;

        public static void Show(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || SceneTransition.IsTravelling) return;
            if (instance == null)
            {
                var prefab = Resources.Load<GameObject>("UI/VillageDialogue");
                if (prefab == null)
                {
                    Debug.LogError("Prefab Resources/UI/VillageDialogue não encontrado.");
                    return;
                }
                instance = Instantiate(prefab).GetComponent<VillageDialogue>();
            }

            instance.label.text = text;
            instance.hideAt = Time.time + instance.displayDuration;
            instance.canvas.enabled = true;
        }

        public static void Hide()
        {
            if (instance != null) instance.canvas.enabled = false;
        }

        private void Awake()
        {
            var overlay = new GameObject("Dialogue Overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            overlay.transform.SetParent(transform, false);
            canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            var scaler = overlay.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            var panel = new GameObject("Black Background", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(overlay.transform, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.04f, 0f);
            panelRect.anchorMax = new Vector2(0.96f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 24f);
            panelRect.sizeDelta = new Vector2(0f, 112f);
            var background = panel.GetComponent<Image>();
            background.color = Color.black;
            background.raycastTarget = false;

            var textObject = new GameObject("Dialogue Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(panel.transform, false);
            var labelRect = textObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(24f, 16f);
            labelRect.offsetMax = new Vector2(-24f, -16f);
            label = textObject.GetComponent<Text>();
            label.font = dialogueFont;
            label.fontSize = 24;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.supportRichText = false;
            label.raycastTarget = false;
            canvas.enabled = false;
        }

        private void Update()
        {
            if (canvas.enabled && (SceneTransition.IsTravelling || Time.time >= hideAt))
                canvas.enabled = false;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
