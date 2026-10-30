using UnityEngine;
using UnityEngine.UI;

namespace EquationShift
{
    public sealed class RoomUnavailableNotice : MonoBehaviour
    {
        public const string Message = "Opa, aparentemente essa sala ainda está sendo montada...";
        private static RoomUnavailableNotice instance;
        [SerializeField] private Font messageFont;
        [SerializeField, Min(1f)] private float displayDuration = 5f;
        private Canvas canvas;
        private float hideAt;

        public bool IsVisible => canvas != null && canvas.enabled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance() => instance = null;

        public static void Show()
        {
            if (instance == null)
            {
                var prefab = Resources.Load<GameObject>("UI/RoomUnavailableNotice");
                if (prefab == null)
                {
                    Debug.LogError("Prefab Resources/UI/RoomUnavailableNotice nao encontrado.");
                    return;
                }
                instance = Instantiate(prefab).GetComponent<RoomUnavailableNotice>();
            }
            instance.hideAt = Time.unscaledTime + instance.displayDuration;
            instance.canvas.enabled = true;
        }

        public static void Hide()
        {
            if (instance != null) instance.canvas.enabled = false;
        }

        private void Awake()
        {
            var overlay = new GameObject("Room notice overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            overlay.transform.SetParent(transform, false);
            canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = overlay.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            var panel = new GameObject("Black background", typeof(RectTransform), typeof(Image));
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

            var label = new GameObject("Message", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(panel.transform, false);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(24f, 16f);
            labelRect.offsetMax = new Vector2(-24f, -16f);
            var text = label.GetComponent<Text>();
            text.font = messageFont;
            text.text = Message;
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.supportRichText = false;
            text.raycastTarget = false;
            canvas.enabled = false;
        }

        private void Update()
        {
            if (IsVisible && Time.unscaledTime >= hideAt) canvas.enabled = false;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
