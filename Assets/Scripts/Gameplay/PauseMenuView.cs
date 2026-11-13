using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EquationShift
{
    public sealed class PauseMenuView : MonoBehaviour
    {
        [SerializeField] private Font font;

        private const string Warning = "Tem certeza? Você perderá seu progresso até o momento e terá que recomeçar o jogo.";

        private static readonly Color Ink = new Color32(16, 22, 29, 255);
        private static readonly Color Panel = new Color32(24, 33, 42, 255);
        private static readonly Color Line = new Color32(72, 86, 97, 255);
        private static readonly Color Cream = new Color32(242, 231, 201, 255);
        private static readonly Color Amber = new Color32(206, 158, 88, 255);

        private GameObject menu;
        private GameObject confirmation;
        private Button continueButton;
        private Button cancelButton;

        internal Button ResetButton { get; private set; }

        internal void Build(Action resume, Action reset, Action returnToTitle, Action quit, Action cancel, Action confirm)
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1200;
            gameObject.AddComponent<GraphicRaycaster>();
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            var dim = new GameObject("Pause Backdrop", typeof(RectTransform), typeof(Image));
            dim.transform.SetParent(transform, false);
            var dimRect = (RectTransform)dim.transform;
            dimRect.anchorMax = Vector2.one;
            dimRect.anchorMin = dimRect.offsetMin = dimRect.offsetMax = Vector2.zero;
            dim.GetComponent<Image>().color = new Color32(8, 12, 16, 184);

            var mainPanel = Card("Pause Options", new Vector2(440f, 394f));
            menu = mainPanel.gameObject;
            Label(mainPanel, "Pausa", 42, Cream, new Vector2(0f, 137f), new Vector2(380f, 62f));
            continueButton = AddButton(mainPanel, "Continuar", new Vector2(0f, 50f), 356f, resume, true);
            ResetButton = AddButton(mainPanel, "Resetar fase", new Vector2(0f, -15f), 356f, reset);
            AddButton(mainPanel, "Voltar ao menu", new Vector2(0f, -80f), 356f, returnToTitle);
            AddButton(mainPanel, "Sair do Jogo", new Vector2(0f, -145f), 356f, quit);

            var dialog = Card("Pause Confirmation", new Vector2(620f, 300f));
            confirmation = dialog.gameObject;
            Label(dialog, Warning, 25, Cream, new Vector2(0f, 47f), new Vector2(520f, 142f));
            cancelButton = AddButton(dialog, "Cancelar", new Vector2(-128f, -86f), 224f, cancel);
            AddButton(dialog, "Confirmar", new Vector2(128f, -86f), 224f, confirm, true);
            confirmation.SetActive(false);
        }

        internal void ShowMenu(EventSystem events, Button selected = null)
        {
            confirmation.SetActive(false);
            menu.SetActive(true);
            events.SetSelectedGameObject((selected != null ? selected : continueButton).gameObject);
        }

        internal void ShowConfirmation(EventSystem events)
        {
            menu.SetActive(false);
            confirmation.SetActive(true);
            events.SetSelectedGameObject(cancelButton.gameObject);
        }

        private RectTransform Card(string name, Vector2 size)
        {
            var border = Rect(name, transform, Vector2.zero, size);
            border.gameObject.AddComponent<Image>().color = Line;
            var surface = Rect("Surface", border, Vector2.zero, size - Vector2.one * 4f);
            surface.gameObject.AddComponent<Image>().color = Panel;
            var accent = Rect("Amber Rule", border, new Vector2(0f, size.y / 2f - 3f), new Vector2(72f, 4f));
            accent.gameObject.AddComponent<Image>().color = Amber;
            return border;
        }

        private Button AddButton(RectTransform parent, string text, Vector2 position, float width, Action action,
            bool primary = false)
        {
            var rect = Rect(text, parent, position, new Vector2(width, 52f));
            var graphic = rect.gameObject.AddComponent<Image>();
            graphic.color = primary ? Amber : new Color32(51, 66, 77, 255);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = graphic;
            var colors = button.colors;
            colors.colorMultiplier = 1.25f;
            colors.normalColor = new Color(0.8f, 0.8f, 0.8f);
            colors.highlightedColor = colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.62f, 0.62f, 0.62f);
            button.colors = colors;
            button.onClick.AddListener(() => action());
            Label(rect, text, 24, primary ? Ink : Cream, Vector2.zero, rect.sizeDelta);
            return button;
        }

        private Text Label(RectTransform parent, string text, int size, Color color, Vector2 position, Vector2 dimensions)
        {
            var label = Rect("Label", parent, position, dimensions).gameObject.AddComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            label.supportRichText = false;
            return label;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }
    }
}
